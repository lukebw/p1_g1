using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CheeseTownPhone.Editor
{
    [InitializeOnLoad]
    public static class EndingChecks
    {
        const string Key = "EndingChecks.Active";
        static int phase, frames, wallet;
        static float endingStock;
        static bool WorldPickup => SessionState.GetBool(Key + ".World", false);
        static PrologueView story;
        static Vector3 playerPosition;
        static void Check(bool value, string reason) { if (!value) throw new Exception(reason); }
        static EndingChecks() { EditorApplication.update += Tick; }

        public static void RunWorldBatch()
        {
            SessionState.SetBool(Key + ".World", true);
            UpgradeCurveChecks.DataChecks(); RunBatch();
        }
        public static void RunBatch()
        {
            EndingAssets.Build(); EndingFlowAssets.Build();
            var settings = Resources.Load<TabletSettings>("TabletSettings");
            Check(settings.epiloguePrefab != null && settings.epiloguePrefab != settings.prologuePrefab, "Separate editable ending asset");
            Check(settings.prologuePrefab.finalHint == "CLICK / SPACE TO SET OUT", "Opening final hint unchanged");
            var view = Object.Instantiate(settings.epiloguePrefab);
            try
            {
                int completed = 0; view.Begin(() => completed++);
                view.skip.onClick.Invoke(); view.Step(10); view.Step(10);
                Check(completed == 1 && !view.gameObject.activeSelf, "Skip finishes once during fade");
            }
            finally { Object.DestroyImmediate(view.gameObject); }
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            SessionState.SetBool(Key + ".Fast", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetBool(Key, true); SessionState.SetFloat(Key + ".Start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        static void Reload()
        {
            EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Wilderness.unity",
                new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
            frames = 0;
        }
        static void CheckChapter(int chapter)
        {
            story.Step(1); story.Step(1.2f); Canvas.ForceUpdateCanvases();
            Check(story.ChapterIndex == chapter && story.background.sprite != null, "Correct ending chapter and atmosphere");
            Check(!story.title.gameObject.activeSelf && !story.counter.gameObject.activeSelf, "No numbered chapter headings");
            Check(!story.body.text.Contains("10000") && !story.hint.text.Contains("SET OUT"), "No numeric objective or opening prompt");
            Check(UpgradeRowView.Fits(story.body.text, story.body), "Ending text fits: " + chapter);
            Capture(story.GetComponent<Canvas>(), "ending-" + (chapter + 1));
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Start", 0) > 120)
            { Finish(new TimeoutException()); return; }
            if (!EditorApplication.isPlaying || ++frames < 15) return;
            try
            {
                var session = TownSession.Instance; var p = session.Progress;
                var demo = Object.FindAnyObjectByType<CheeseTownDemo>();
                if (phase == 0)
                {
                    Check(session.PrologueActive && !session.EpilogueActive, "Fresh game starts with the opening only");
                    session.Prologue.Skip(); session.Prologue.Step(10); phase = 1; frames = 0; return;
                }
                if (phase == 1)
                {
                    Time.timeScale = 0;
                    p.CollectWorld(1); p.SkipInteractionGuide(); p.Grant(500);
                    UpgradeCurveChecks.ChargeCharm(p, session.Settings);
                    p.Buy(session.Settings.upgrades.Find(o => o.id == "tree-start")); p.Tick(1000);
                    demo.Refresh();
                    Check(p.TreeStockFull && demo.View.hudTreeFullDot.activeSelf && demo.View.pageTreeFullDot.activeSelf, "Both tree launchers show full storage");
                    CheckOrder(demo.View.treeLauncher, demo.View.launcher, demo.View.shopLauncher);
                    CheckOrder(demo.View.treePageButton, demo.View.mailButton, demo.View.shopButton);
                    Capture(demo.ScreenCanvas, "ordered-hud");
                    demo.View.shopLauncher.onClick.Invoke(); demo.CompleteUITransitions();
                    Check(demo.PhoneOpen && demo.View.Page == 1, "HUD supplies icon opens the supply shop");
                    demo.View.back.onClick.Invoke(); demo.CompleteUITransitions();
                    demo.ToggleDetails(); demo.CompleteUITransitions();
                    demo.ShowPage(1); demo.CompleteUITransitions(); demo.View.treePageButton.onClick.Invoke(); demo.CompleteUITransitions();
                    Check(demo.PhoneOpen && demo.View.Page == 0, "Header tree button returns to details");
                    demo.ShowPage(2); demo.CompleteUITransitions();
                    var attention = demo.View.readAttention;
                    Check(attention != null && demo.View.read.IsInteractable(), "Unread mail exposes the read prompt");
                    demo.View.RefreshReadAttention(demo.View.readBlinkPeriod * .25f); float bright = attention.alpha;
                    Capture(demo.ScreenCanvas, "unread-prompt-bright");
                    demo.View.RefreshReadAttention(demo.View.readBlinkPeriod * .75f);
                    Check(bright - attention.alpha > .3f, "Unread read button visibly pulses");
                    Capture(demo.ScreenCanvas, "unread-prompt-dim");
                    demo.View.read.onClick.Invoke(); demo.View.RefreshReadAttention(0);
                    Check(!demo.View.read.interactable && attention.alpha == 1, "Reading stops pulse immediately");
                    demo.View.back.onClick.Invoke(); demo.CompleteUITransitions();
                    demo.View.RefreshReadAttention(0); Check(attention.alpha == 1, "Hidden mail does not pulse");
                    Check(!demo.PhoneOpen, "Exit closes directly from mail");
                    demo.ToggleDetails(); demo.CompleteUITransitions();
                    p.Grant(9999 - p.Cheeses); playerPosition = Object.FindAnyObjectByType<PlayerController>().transform.position;
                    Capture(demo.ScreenCanvas, "full-stock-header");
                    if (WorldPickup)
                    {
                        demo.CloseTablet(); demo.CompleteUITransitions();
                        var drop = Object.Instantiate(Object.FindAnyObjectByType<Tree>().cheesePrefab, playerPosition, Quaternion.identity);
                        drop.SetActive(true);
                        Object.FindAnyObjectByType<CollectionRadius>().SendMessage("OnTriggerEnter2D", drop.GetComponent<Collider2D>());
                    }
                    else demo.TryHarvest();
                    wallet = p.Cheeses; endingStock = p.Stock;
                    Check(p.EndingLetterPending && !p.GameEnded && !session.EpilogueActive, "Crossing threshold queues mail without starting narrative");
                    Check((WorldPickup ? demo.View.pickupFeedback.IsAnimating : demo.View.harvestFeedback.IsAnimating) && !p.EndingLetterSent, "Pickup keeps its flight and sound sequence");
                    session.SendMessage("LateUpdate"); Check(!p.EndingLetterSent, "Final mail waits for visible animation");
                    if (!WorldPickup) Check(!p.TreeStockFull && !demo.View.hudTreeFullDot.activeSelf && !demo.View.pageTreeFullDot.activeSelf, "Harvest clears both stock dots");
                    Capture(demo.ScreenCanvas, "harvest-before-letter");
                    demo.View.harvestFeedback.Advance(10); demo.View.pickupFeedback.Advance(10); session.SendMessage("LateUpdate");
                    Check(p.EndingLetterSent && !p.GameEnded && !session.EpilogueActive, "Settled harvest delivers mail, not the ending");
                    // Repeated threshold checks cannot duplicate the final letter or end before acknowledgement.
                    p.Reconfigure(session.Settings); Check(p.Letters.Count(l => l.Id == TownProgress.GoalLetterId) == 1, "Final mail deduplicates");
                    if (!demo.PhoneOpen) demo.TogglePhone();
                    demo.ShowPage(2); demo.CompleteUITransitions(); Capture(demo.ScreenCanvas, "last-letter");
                    Check(demo.View.letter.text == p.Letters.Last().Body && UpgradeRowView.Fits(demo.View.letter.text, demo.View.letter), "Opening mail selects the full readable final letter");
                    Check(!session.EpilogueActive, "Viewing final mail alone does not play ending");
                    demo.View.read.onClick.Invoke(); story = session.Epilogue;
                    Check(p.GameEnded && session.EpilogueActive && demo.BlocksWorldInput, "Mark as read starts narrative and blocks input");
                    Check(!demo.ScreenCanvas.gameObject.activeInHierarchy && !demo.View.ending.activeSelf, "Narrative hides HUD and choice until complete");
                    p.Tick(100); p.CollectWorld(10); p.Grant(10); Check(p.Cheeses == wallet && p.Stock == endingStock, "Economy freezes during ending");
                    CheckChapter(0); story.advance.onClick.Invoke(); story.Step(1); CheckChapter(1);
                    phase = 2; Reload(); return;
                }
                if (phase == 2)
                {
                    Check(session.Epilogue == story && story.ChapterIndex == 1 && session.EpilogueActive, "Reload preserves active ending chapter");
                    story.advance.onClick.Invoke(); story.Step(1); CheckChapter(2);
                    story.advance.onClick.Invoke(); story.Step(1); phase = 3; frames = 0; return;
                }
                if (phase == 3)
                {
                    Check(session.EndingReady && demo.View.ending.activeInHierarchy, "Completed story displays the choice");
                    Check(demo.View.continueEnding.IsInteractable() && demo.View.quitEnding.IsInteractable(), "Both choices receive input");
                    Check(!demo.View.ending.GetComponentsInChildren<UnityEngine.UI.Text>().Any(t=>t.text.Contains("GAME OVER")), "Old game-over panel removed");
                    Check(!demo.View.background.activeSelf && !demo.View.panel.gameObject.activeSelf, "No opaque tablet backing behind the choice");
                    var shade = demo.View.ending.GetComponent<UnityEngine.UI.Image>();
                    Check(shade.color == new Color(0,0,0,.62f), "Ending uses a translucent black shade");
                    foreach (var label in demo.View.ending.GetComponentsInChildren<UnityEngine.UI.Text>())
                        Check(UpgradeRowView.Fits(label.text,label), "Choice text fits the supplied frame");
                    Capture(demo.ScreenCanvas, "continue-choice");
                    Capture(demo.ScreenCanvas, "continue-choice-4x3", 1024, 768);
                    Check(Object.FindAnyObjectByType<PlayerController>().transform.position == playerPosition, "Player stays still throughout narrative");
                    demo.View.continueEnding.onClick.Invoke();
                    Check(p.EndingResolved && !p.GameEnded && !demo.PhoneOpen && !demo.View.ending.activeSelf, "Yes restores gameplay and dismisses choice");
                    int before = p.Cheeses; p.CollectWorld(3); p.Tick(5);
                    Check(p.Cheeses == before + 3 && p.Stock > 0 && !p.EndingLetterPending, "Yes resumes collection and production without another ending");
                    phase = 4; Reload(); return;
                }
                Check(p.EndingResolved && !p.GameEnded && !session.EpilogueActive && !demo.View.ending.activeSelf, "Continuation survives scene reload without replay");
                // Validate the No decision without terminating the test editor before reporting results.
                var stop = new TownProgress(session.Settings); stop.CollectWorld(stop.CollectionGoal);
                Check(stop.EndingLetterPending && !stop.GameEnded, "World pickup also queues mail");
                stop.PublishEndingLetter(); stop.ReadLetter(stop.MailboxEntryIndex); stop.ResolveEnding(false);
                int stoppedWallet = stop.Cheeses; stop.CollectWorld(10); stop.Tick(10);
                Check(stop.EndingResolved && stop.GameEnded && stop.Cheeses == stoppedWallet, "No keeps economy ended");
                Finish(null);
            }
            catch (Exception e) { Finish(e); }
        }
        static void CheckOrder(params UnityEngine.UI.Button[] buttons)
        {
            float edge = float.NegativeInfinity;
            foreach (var button in buttons)
            {
                Check(button != null, "All three navigation entries exist");
                var rect = (RectTransform)button.transform;
                Check(rect.anchoredPosition.x >= edge, "Navigation order has no overlapping entries");
                edge = rect.anchoredPosition.x + rect.rect.width;
            }
        }
        static void Capture(Canvas canvas, string name, int width = 1280, int height = 720)
        {
            var camera = Camera.main;
            var mode = canvas.renderMode; var worldCamera = canvas.worldCamera;
            float distance = canvas.planeDistance, scale = canvas.scaleFactor;
            int layer = canvas.sortingLayerID, order = canvas.sortingOrder;
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var target = new RenderTexture(width, height, 24);
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                canvas.scaleFactor = CheeseTownDemo.PixelScaleFor(width,height); canvas.sortingLayerName = "Foreground"; canvas.sortingOrder = 32760;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                var demo = Object.FindAnyObjectByType<CheeseTownDemo>();
                if (demo != null && canvas == demo.ScreenCanvas)
                {
                    Canvas.ForceUpdateCanvases(); demo.View.PositionHud((RectTransform)canvas.transform); Canvas.ForceUpdateCanvases();
                    Vector2 shadeSize = ((RectTransform)demo.View.ending.transform).rect.size;
                    Vector2 viewportSize = ((RectTransform)canvas.transform).rect.size;
                    Check(shadeSize.x >= viewportSize.x && shadeSize.y >= viewportSize.y, "Ending shade covers the full viewport with edge bleed");
                    RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
                }
                RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs/EndingStory"); File.WriteAllBytes("Logs/EndingStory/" + (WorldPickup ? "world-" : "") + name + ".png", image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                canvas.renderMode = mode; canvas.worldCamera = worldCamera; canvas.planeDistance = distance;
                canvas.scaleFactor = scale; canvas.sortingLayerID = layer; canvas.sortingOrder = order;
                Object.DestroyImmediate(image); Object.DestroyImmediate(target);
            }
        }
        static void Finish(Exception error)
        {
            SessionState.SetBool(Key, false); Time.timeScale = 1;
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".Fast", false);
            string result = error == null ? "PASS: ordered three-button navigation; supplies HUD route; unread-only blinking; full-screen shade; transparent supplied frame; harvest animation before final mail; acknowledged-mail narrative; fitted final letter; header tree/exit navigation; both full-stock dots; frozen narrative; interactive Yes/No; resumed production/collection; no replay after scene reload; No economy shutdown." : "FAIL: " + error;
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/ending-story-checks.txt", result); Debug.Log(result);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
