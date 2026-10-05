// BEGIN ADDED: Exercise pickup credit, pooled bursts and interrupted UI, and render a real animation preview.
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
    public static class PickupFeedbackChecks
    {
        const string Key = "PickupFeedbackChecks.Running";
        static int frames;
        static PickupFeedbackChecks() { EditorApplication.update += Tick; }
        static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        [MenuItem("Cheese Town/UI/Check Pickup Feedback and Render Preview")]
        public static void RunBatch()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play first.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            SessionState.SetBool(Key + ".Fast", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetFloat(Key + ".Time", (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Time", 0) > 120) { Finish(new TimeoutException("Pickup tests timed out")); return; }
            if (!EditorApplication.isPlaying || ++frames < 10) return;
            try { RunChecks(); Finish(null); } catch (Exception error) { Finish(error); }
        }
        static void Pickup(CheeseTownDemo demo, Vector3 position)
        {
            var template = Object.FindAnyObjectByType<Tree>().cheesePrefab;
            var cheese = Object.Instantiate(template, position, Quaternion.identity); cheese.SetActive(true);
            var collider = cheese.GetComponent<Collider2D>();
            var collector = Object.FindAnyObjectByType<CollectionRadius>();
            int before = demo.Progress.Cheeses;
            int credit = Mathf.Min(demo.Progress.UnitPrice, 1000000000 - before);
            collector.SendMessage("OnTriggerEnter2D", collider);
            collector.SendMessage("OnTriggerStay2D", collider);
            Check(demo.Progress.Cheeses == before + credit, "Real pickup awards once even with duplicate enter/stay callbacks");
            Check(!cheese.activeSelf, "Collected world object is immediately removed from interaction");
        }
        static void RunChecks()
        {
            var demo = Object.FindAnyObjectByType<CheeseTownDemo>(); var fx = demo.View.pickupFeedback;
            var player = Object.FindAnyObjectByType<PlayerController>(); var progress = demo.Progress;
            Check(fx != null && fx.flightPrefab != null, "Tablet references editable flight prefab");
            Check(fx.GetComponentsInChildren<CheesePickupFlight>(true).Length == fx.poolSize, "Pool is prewarmed and bounded");
            Check(fx.GetComponentsInChildren<Graphic>(true).All(g => !g.raycastTarget), "All FX graphics ignore pointer input");
            int before = progress.Cheeses;
            Pickup(demo, player.transform.position);
            Check(fx.PendingCredit == progress.UnitPrice && demo.View.hudWallet.text == before.ToString(), "HUD defers only visual credit");
            fx.Advance(.25f);
            Check(fx.ActiveFlights == 1 && fx.GetComponentsInChildren<CheesePickupFlight>().Single().trail.Any(i => i.color.a > 0), "Flight has visible trail before arrival");
            fx.Advance(1);
            Check(fx.PendingCredit == 0 && demo.View.hudWallet.text == progress.Cheeses.ToString(), "Flight settles displayed balance exactly");
            RenderPreview(demo, fx, player);
            int created = fx.GetComponentsInChildren<CheesePickupFlight>(true).Length;
            for (int i = 0; i < 40; i++) Pickup(demo, player.transform.position);
            Check(fx.ActiveFlights <= fx.poolSize && fx.PendingCredit == 40 * progress.UnitPrice, "Dense pickups merge without losing credit");
            Check(fx.GetComponentsInChildren<CheesePickupFlight>(true).Length == created, "Dense pickup does not allocate extra flight objects");
            fx.Advance(2);
            Check(fx.PendingCredit == 0 && demo.View.hudWallet.text == progress.Cheeses.ToString(), "Merged flights settle every pickup");
            Pickup(demo, player.transform.position); demo.View.SetOpen(true);
            Check(fx.ActiveFlights == 0 && fx.PendingCredit == 0 && demo.View.hudWallet.text == progress.Cheeses.ToString(), "Opening tablet cancels effects and synchronizes balance immediately");
            demo.View.SetOpen(false, false);
            progress.Grant(1000); progress.Buy(demo.Settings.upgrades.First(o => o.effect == UpgradeEffect.CheeseValue));
            Check(progress.UnitPrice >= 2, "Double-value upgrade is active");
            Pickup(demo, player.transform.position);
            Check(fx.PendingCredit == progress.UnitPrice, "Displayed credit respects actual upgraded value");
            fx.enabled = false;
            Check(fx.PendingCredit == 0 && demo.View.hudWallet.text == progress.Cheeses.ToString(), "Disabling effect safely settles its HUD");
            fx.enabled = true;
            progress.Grant(1000000000); fx.RefreshWallet(); Pickup(demo, player.transform.position);
            Check(fx.PendingCredit == 0 && progress.Cheeses == 1000000000, "Wallet cap creates no phantom delayed credit");
        }
        static void RenderPreview(CheeseTownDemo demo, CheesePickupFeedback fx, PlayerController player)
        {
            const int width = 1280, height = 720;
            Directory.CreateDirectory("Logs/PickupFrames");
            var camera = Camera.main; var canvas = demo.ScreenCanvas;
            var oldMode = canvas.renderMode; var oldCamera = canvas.worldCamera; float oldDistance = canvas.planeDistance;
            int oldLayer = canvas.sortingLayerID, oldOrder = canvas.sortingOrder; float oldScale = canvas.scaleFactor;
            var oldTarget = camera.targetTexture; var oldActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24); var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                // Match the runtime overlay's foreground order and integer scale for this capture size.
                canvas.sortingLayerName = "Foreground"; canvas.sortingOrder = 32760;
                canvas.scaleFactor = CheeseTownDemo.PixelScaleFor(width, height);
                var tutorial = canvas.transform.Find("First cheese tutorial");
                if (tutorial != null && !demo.HarvestTutorialActive) tutorial.gameObject.SetActive(false);
                Canvas.ForceUpdateCanvases(); demo.View.PositionHud((RectTransform)canvas.transform); Canvas.ForceUpdateCanvases();
                // Use actual pickup callbacks and the runtime effect's stepping path for each rendered frame.
                for (int frame = 0; frame < 48; frame++)
                {
                    if (frame == 4 || frame == 8 || frame == 12)
                    {
                        Vector3 source = player.transform.position + new Vector3((frame - 8) * .45f, -.5f, 0);
                        for (int i = 0; i < 3; i++) Pickup(demo, source + Vector3.right * i * .4f);
                    }
                    fx.Advance(1f / 30); Canvas.ForceUpdateCanvases();
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                    RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                    File.WriteAllBytes($"Logs/PickupFrames/frame-{frame:000}.png", image.EncodeToPNG());
                }
            }
            finally
            {
                fx.CancelAndSync(); canvas.renderMode = oldMode; canvas.worldCamera = oldCamera; canvas.planeDistance = oldDistance;
                canvas.sortingLayerID = oldLayer; canvas.sortingOrder = oldOrder; canvas.scaleFactor = oldScale;
                camera.targetTexture = oldTarget; RenderTexture.active = oldActive;
                Object.DestroyImmediate(image); Object.DestroyImmediate(target);
            }
        }
        static void Finish(Exception error)
        {
            SessionState.SetBool(Key, false); EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".Fast", false);
            Directory.CreateDirectory("Logs");
            string report = error == null ? "PASS: actual pickup callbacks; duplicate protection; immediate real balance; delayed HUD settlement; visible trail; bounded pool; 40-pickup merge; interrupted page transition; upgraded cheese value; disabled FX; wallet cap. Rendered 48 actual Unity preview frames." : "FAIL: " + error;
            File.WriteAllText("Logs/pickup-feedback-checks.txt", report);
            if (error == null) Debug.Log(report); else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
// END ADDED
