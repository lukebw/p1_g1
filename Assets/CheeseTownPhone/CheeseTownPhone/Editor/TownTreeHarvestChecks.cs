using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheeseTownPhone.Editor
{
    [InitializeOnLoad]
    public static class TownTreeHarvestChecks
    {
        const string Key = "TownTreeHarvestChecks.Active";
        static int frames, phase, savedWallet;
        static double audioEnd;
        static float lastPickupTime;
        static int audibleArrivals;
        static bool pickupPlaying;
        static float PickupTime() => (float)typeof(TownAudio).GetField("nextPickupSoundTime",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(TownAudio.Instance);
        static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        static TownTreeHarvestChecks() { EditorApplication.update += Tick; }
        public static void RunBatch()
        {
            TownTreeHarvestBuilder.Build(); UpgradeCurveChecks.DataChecks();
            var settings = Object.Instantiate(Resources.Load<TabletSettings>("TabletSettings"));
            try
            {
                var p = new TownProgress(settings); int total = 0, calls = 0;
                p.BatchProduced += amount => { total += amount; calls++; };
                p.Tick(999); Check(calls == 0, "Dormant tree emits nothing");
                p.Grant(140); UpgradeCurveChecks.ChargeCharm(p, settings); p.Buy(settings.upgrades.Single(o => o.id == "tree-start"));
                p.Tick(4.9f); Check(calls == 0, "No output before batch matures");
                p.Tick(.1f); Check(total == 5 && calls == 1, "First batch emits its actual output");
                p.Tick(999); Check(total == 200 && calls == 2, "Lagged output is coalesced and capacity-clamped");
                p.Tick(999); Check(calls == 2, "Full storage emits no new drops");
            }
            finally { Object.DestroyImmediate(settings); }
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            SessionState.SetBool(Key + ".Fast", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetBool(Key, true); SessionState.SetFloat(Key + ".Start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Start", 0) > 120) { Finish(new TimeoutException()); return; }
            if (!EditorApplication.isPlaying || (++frames < 15 && phase != 3)) return;
            try
            {
                var session = TownSession.Instance; var demo = Object.FindAnyObjectByType<CheeseTownDemo>();
                if (phase == 0)
                {
                    if (session.Prologue != null) { session.Prologue.Skip(); session.Prologue.Step(1); }
                    phase = 1; frames = 0; return;
                }
                var p = demo.Progress; var fx = demo.View.harvestFeedback;
                if (phase == 1)
                {
                    Time.timeScale = 0;
                    var config = Object.Instantiate(demo.Settings); demo.UseSettings(config);
                    p.CollectWorld(1); p.SkipInteractionGuide(); p.Grant(6000);
                    demo.TogglePhone(); demo.ShowPage(0); demo.CompleteUITransitions(); demo.SendMessage("LateUpdate"); fx.Advance(0);
                    Check(fx != null && fx.flightPrefab != null, "Reusable harvest prefab is bound");
                    int objects = fx.GetComponentsInChildren<Transform>(true).Length;
                    Check(fx.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), "FX never intercept buttons");
                    Check(fx.VisibleDrops == 0, "No stock means no pile");
                    UpgradeCurveChecks.ChargeCharm(p, config);
                    demo.Buy(config.upgrades.Single(o => o.id == "tree-start")); p.Tick(5);
                    Check(p.Stock == 5 && fx.VisibleDrops == 5, "A real production batch creates drops");
                    CheckVerticalFall(fx);
                    fx.Advance(2); int before = p.Cheeses; demo.TryHarvest();
                    Check(p.Stock == 0 && p.Cheeses == before + 5 && fx.PendingCredit == 5, "Harvest credits exactly once before animation");
                    Check(demo.View.wallet.text == before.ToString(), "Only header display waits for arrival");
                    fx.Advance(.25f);
                    Check(fx.GetComponentsInChildren<CheesePickupFlight>().Any(f => f.trail.Any(i => i.color.a > 0)), "Harvest reuses visible pixel trails");
                    fx.Advance(3); Check(fx.PendingCredit == 0 && demo.View.wallet.text == p.Cheeses.ToString(), "Flights settle the exact balance");
                    p.Tick(5); demo.TryHarvest(); demo.ShowPage(1); demo.CompleteUITransitions();
                    Check(fx.PendingCredit == 0 && fx.ActiveFlights == 0 && fx.VisibleDrops == 0, "Page switch clears visuals and syncs credit");
                    p.Tick(20); Check(fx.VisibleDrops == 0, "Hidden production does not animate behind another page");
                    demo.ShowPage(0); demo.CompleteUITransitions(); fx.Advance(0);
                    Check(fx.VisibleDrops > 0, "Returning restores the current stored harvest");
                    foreach (var o in config.upgrades) while (p.CanBuy(o)) demo.Buy(o);
                    demo.TryHarvest(); fx.Advance(3); p.Tick(1);
                    for (int i = 0; i < 36; i++) { fx.Advance(1f / 30); Capture(demo, "drop-" + i.ToString("000")); }
                    p.Tick(1); fx.Advance(2); p.Tick(999);
                    Check(p.Stock == 8000 && fx.VisibleDrops <= fx.dropLimit, "Eight thousand cheese uses a bounded visual pile");
                    fx.Advance(3);
                    Capture(demo, "full-pile");
                    before = p.Cheeses; demo.TryHarvest(); p.Tick(1); demo.TryHarvest();
                    Check(fx.PendingCredit == 8180 && p.Cheeses == before + 8180, "Rapid collections merge without losing credit");
                    demo.TryHarvest(); Check(fx.PendingCredit == 8180, "Empty collect cannot replay credit");
                    Check(fx.ActiveFlights <= fx.flightLimit && fx.GetComponentsInChildren<Transform>(true).Length == objects, "Harvest spam never expands the pool");
                    for (int i = 0; i < 42; i++) { fx.Advance(1f / 30); Capture(demo, "flight-" + i.ToString("000")); }
                    fx.Advance(3); Check(fx.PendingCredit == 0 && demo.View.wallet.text == p.Cheeses.ToString(), "Large harvest finishes at the correct number");
                    // Record a fresh production burst and allow it to settle before another harvest.
                    p.Tick(1); fx.Advance(.3f); Capture(demo, "production-drop");
                    demo.TryHarvest(); fx.enabled = false;
                    Check(fx.PendingCredit == 0 && fx.ActiveFlights == 0, "Disabling presentation cannot lose income");
                    fx.enabled = true; p.Tick(1); fx.Advance(0); demo.TryHarvest();
                    demo.View.SetOpen(false, false);
                    Check(fx.PendingCredit == 0 && fx.VisibleDrops == 0, "Closing the tablet settles presentation immediately");
                    // Rendering many screenshots blocks one frame; let that long frame pass before testing cadence.
                    phase = 4; frames = 0;
                }
                else if (phase == 4)
                {
                    // Let real Unity frames advance so the shared unscaled audio cooldown is exercised.
                    demo.Settings.collectionGoal = 1000000000; p.Reconfigure(demo.Settings);
                    demo.View.SetOpen(true, false); demo.ShowPage(0); demo.CompleteUITransitions(); fx.Advance(0);
                    var audio = TownAudio.Instance;
                    Check(audio != null && audio.groundPickup != null && audio.groundPickup.clip != null,
                        "The existing pickup clip is connected");
                    audio.groundPickup.Stop(); lastPickupTime = PickupTime();
                    p.Tick(1); fx.Advance(.3f);
                    Check(PickupTime() == lastPickupTime, "Production alone never plays pickup audio");
                    demo.TryHarvest();
                    audibleArrivals = 0; pickupPlaying = false; audioEnd = EditorApplication.timeSinceStartup + 2;
                    phase = 3; frames = 0;
                }
                else if (phase == 3)
                {
                    float current = PickupTime();
                    if (current > lastPickupTime) { audibleArrivals++; lastPickupTime = current; }
                    pickupPlaying |= TownAudio.Instance.groundPickup.isPlaying;
                    if (EditorApplication.timeSinceStartup < audioEnd) return;
                    Check(audibleArrivals >= 3 && pickupPlaying, "Harvest plays multiple staggered pickup sounds through the real AudioSource");
                    Check(fx.PendingCredit == 0, "Real-time audio harvest settles all credit");
                    Debug.Log("Pickup audio accepted " + audibleArrivals + " staggered arrivals; AudioSource playing: " + pickupPlaying);
                    p.Tick(1); demo.TryHarvest(); demo.View.SetOpen(false, false);
                    Check(PickupTime() == lastPickupTime, "Cancelling flights does not play phantom pickup sounds");
                    savedWallet = p.Cheeses;
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Wilderness.unity", new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                    phase = 2; frames = 0;
                }
                else
                {
                    Check(p.Cheeses == savedWallet && fx.PendingCredit == 0, "Reload retains income with no stale flights");
                    demo.TogglePhone(); demo.ShowPage(0); demo.CompleteUITransitions(); fx.Advance(0);
                    demo.Settings.collectionGoal = 1000000000; p.Reconfigure(demo.Settings);
                    p.Grant(1000000000 - p.Cheeses - 1); p.Tick(1); demo.TryHarvest();
                    Check(p.GameEnded && p.Cheeses == 1000000000 && fx.PendingCredit == 0, "Ending and wallet cap cannot strand visual credit");
                    Finish(null);
                }
            }
            catch (Exception e) { Finish(e); }
        }
        static void CheckVerticalFall(TownTreeHarvestFeedback fx)
        {
            var images = fx.GetComponentsInChildren<Image>().Where(i => i.name.StartsWith("Stored cheese ")).OrderBy(i => i.name).ToArray();
            var start = images.Select(i => i.rectTransform.anchoredPosition).ToArray();
            float y0 = start[0].y;
            fx.Advance(.1f); float y1 = images[0].rectTransform.anchoredPosition.y;
            fx.Advance(.1f); float y2 = images[0].rectTransform.anchoredPosition.y;
            Check(y0 > y1 && y1 - y2 > y0 - y1, "Gravity accelerates the fall");
            bool bounced = false; float previous = y2;
            for (int step = 0; step < 100; step++)
            {
                fx.Advance(.02f);
                for (int i = 0; i < images.Length; i++)
                    Check(images[i].rectTransform.anchoredPosition.x == start[i].x, "Drops stay directly below their spawn throughout fall and bounce");
                float y = images[0].rectTransform.anchoredPosition.y;
                bounced |= y > previous; previous = y;
            }
            Check(bounced, "Landing has a physical rebound");
            var resting = images.Select(i => i.rectTransform.anchoredPosition).ToArray();
            fx.Advance(30);
            Check(images.Select((i, n) => i.rectTransform.anchoredPosition == resting[n]).All(same => same),
                "Drops settle and remain stable after a long frame");
        }
        static void Capture(CheeseTownDemo demo, string name)
        {
            demo.Refresh(); demo.View.notice.text = "";
            var canvas = demo.ScreenCanvas; var camera = Camera.main;
            var mode = canvas.renderMode; var uiCamera = canvas.worldCamera; float distance = canvas.planeDistance, scale = canvas.scaleFactor;
            int layer = canvas.sortingLayerID, order = canvas.sortingOrder;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(1280, 720, 24); var image = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; canvas.scaleFactor = 2;
                canvas.sortingLayerName = "Foreground"; canvas.sortingOrder = 32760; Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/TownTreeHarvest"); File.WriteAllBytes("Logs/TownTreeHarvest/" + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                canvas.renderMode = mode; canvas.worldCamera = uiCamera; canvas.planeDistance = distance; canvas.scaleFactor = scale;
                canvas.sortingLayerID = layer; canvas.sortingOrder = order;
                Object.DestroyImmediate(image); Object.DestroyImmediate(target);
            }
        }
        static void Finish(Exception error)
        {
            SessionState.SetBool(Key, false); Time.timeScale = 1;
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".Fast", false);
            string result = error == null ? "PASS: gravity acceleration; vertical-only falls and physical bounce; stable resting pile; repeated real-time pickup AudioSource playback; silent production/cancellation; exact capacity-clamped batch events; manual collection and trails; delayed header only; merged 8180-cheese harvest; bounded pools; page/close/disable cancellation; reload; wallet cap and ending; existing economy data checks." : "FAIL: " + error;
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/town-tree-harvest-checks.txt", result); Debug.Log(result);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
