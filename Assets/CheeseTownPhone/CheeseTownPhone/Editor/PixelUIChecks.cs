// BEGIN ADDED: Real Play mode checks protect retained interactions and art placement.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    [InitializeOnLoad]
    public static class PixelUIChecks
    {
        const string Request = "PixelUIChecks.Running";
        static int readyFrames;
        static PixelUIChecks()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Request + ".Restore", false))
                {
                    EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Request + ".FastPlay", false);
                    SessionState.SetBool(Request + ".Restore", false);
                }
            };
        }
        [MenuItem("Cheese Town/Run Pixel UI Checks")]
        public static void RunBatch()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before checking UI.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PixelUIImport.Apply();
            PhoneDemoChecks.DataChecks();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Request + ".FastPlay", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetBool(Request + ".Restore", true);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetFloat(Request + ".Started", (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Request, true);
            EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Request, false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Request + ".Started", 0) > 60)
            { Finish(new TimeoutException("Pixel UI checks timed out.")); return; }
            if (!EditorApplication.isPlaying || ++readyFrames < 3) return;
            try { RunChecks(); Finish(null); }
            catch (Exception error) { Finish(error); }
        }
        static void Check(bool condition, string message)
        { if (!condition) throw new InvalidOperationException("Pixel UI check failed: " + message); }
        static Button Button(CheeseTownDemo demo, string name)
            => demo.GetComponentsInChildren<Button>(true).Single(b => b.name == name && b.gameObject.activeInHierarchy);
        static void Click(CheeseTownDemo demo, string name)
        {
            var button = Button(demo, name);
            Check(button.interactable, name + " is enabled"); button.onClick.Invoke();
        }
        static int BuyCount(CheeseTownDemo demo) => demo.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Buy "));
        static void Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
        }
        static void RunChecks()
        {
            Time.timeScale = 0;
            var demo = UnityEngine.Object.FindAnyObjectByType<CheeseTownDemo>();
            Check(demo != null, "tablet exists");
            var config = demo.Settings;
            Check(config.interfaceFont != null && config.mainFrame != null, "font and skin wired");
            Check(config.interfaceFont.HasCharacter('0') && config.interfaceFont.HasCharacter('A'), "font has UI glyphs");
            Check(config.mainFrame.rect.size == new Vector2(1280, 720), "full source frame retained");
            foreach (var sprite in new[] { config.mainFrame, config.backButtonArtwork, config.collectButtonArtwork, config.envelopeIcon, config.shopIcon, config.unreadDot })
            {
                Check(sprite != null && sprite.texture.filterMode == FilterMode.Point && sprite.texture.mipmapCount == 1, "sharp sprite import");
                Check(sprite.rect.width == sprite.texture.width && sprite.rect.height == sprite.texture.height, "no import trimming");
            }
            Check(demo.ScreenCanvas.pixelPerfect, "Canvas pixel alignment");
            Check(CheeseTownDemo.PixelScaleFor(1920, 1080) == 3 && CheeseTownDemo.PixelScaleFor(1280, 720) == 2
                && CheeseTownDemo.PixelScaleFor(2560, 1440) == 4 && CheeseTownDemo.PixelScaleFor(1366, 768) == 2, "integer scaling and letterboxing");
            var originalInput = InputSystem.settings;
            var checkInput = UnityEngine.Object.Instantiate(originalInput);
            checkInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            checkInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = checkInput;
            var keyboard = InputSystem.AddDevice<Keyboard>("PixelUICheckKeyboard");
            try
            {
                Press(keyboard, Key.Tab); Check(demo.PhoneOpen, "Tab opens welcome mailbox");
                Check(demo.GetComponentsInChildren<Text>().Any(t => t.name == "Mayor message"), "welcome mailbox retained");
                Click(demo, "Reply to mayor"); Click(demo, "Close tablet");
                Check(demo.PhoneOpen && !demo.GetComponentsInChildren<Text>().Any(t => t.name == "Mayor message"), "Back returns to main");
                var frame = demo.GetComponentsInChildren<Image>().Single(i => i.name == "Pixel main frame");
                Check(frame.rectTransform.sizeDelta == new Vector2(640, 360), "2x PNG maps to native size");
                var upgrade = Button(demo, "Shop button");
                Check(upgrade.GetComponent<RectTransform>().sizeDelta == new Vector2(99, 33), "button export scale");
                Capture(demo, "PixelUI-Main-1080p.png");
                Canvas.ForceUpdateCanvases();
                var point = RectTransformUtility.WorldToScreenPoint(null, upgrade.transform.TransformPoint(upgrade.GetComponent<RectTransform>().rect.center));
                var hits = new List<RaycastResult>();
                demo.ScreenCanvas.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current) { position = point }, hits);
                Check(hits.Count > 0 && hits[0].gameObject == upgrade.gameObject,
                    "art button receives pointer hits: point=" + point + ", screen=" + Screen.width + "x" + Screen.height
                    + ", depth=" + upgrade.GetComponent<Image>().depth + ", hits=" + string.Join(",", hits.Select(h => h.gameObject.name)));
                int before = demo.Progress.Cheeses;
                Click(demo, "Collect cheese"); Check(demo.Progress.Cheeses > before, "collect art calls existing payout");
                Check(demo.GetComponentsInChildren<Image>().Any(i => i.name == "Unread dot"), "new letter enables dot");
                Click(demo, "Shop button"); Check(BuyCount(demo) == 5, "five upgrades retained");
                Click(demo, "Player filter"); Check(BuyCount(demo) == 3, "player filter");
                Click(demo, "Tree filter"); Check(BuyCount(demo) == 2, "tree filter");
                Click(demo, "All filter"); Capture(demo, "PixelUI-Shop-1080p.png");
                // BEGIN ADDED: Font metrics must not silently crop upgrade titles.
                Check(demo.GetComponentsInChildren<Text>().Where(t => t.name == "Title")
                    .All(t => t.cachedTextGenerator.characterCountVisible > 0), "upgrade titles are visible");
                Check(!Button(demo, "Collect cheese").interactable, "subpages disable collection");
                // END ADDED
                before = demo.Progress.Cheeses;
                Click(demo, "Buy move-speed"); Check(demo.Progress.MoveSpeed == 6 && demo.Progress.Cheeses == before - 20, "buy effect and debit");
                Check(demo.GetComponentsInChildren<Text>().Single(t => t.name == "Wallet").text == demo.Progress.Cheeses.ToString(), "live numeric wallet");
                Click(demo, "Buy move-speed"); Check(!Button(demo, "Buy move-speed").interactable, "max-level purchase disabled");
                Press(keyboard, Key.Escape); Check(demo.PhoneOpen, "Escape returns from shop");
                Click(demo, "Letters button"); Capture(demo, "PixelUI-Mail-1080p.png");
                while (Button(demo, "Next letter").interactable) Click(demo, "Next letter");
                Click(demo, "Reply to mayor");
                Check(!demo.GetComponentsInChildren<Image>().Any(i => i.name == "Unread dot"), "read mail clears dot");
                Press(keyboard, Key.Escape); Press(keyboard, Key.Escape); Check(!demo.PhoneOpen, "Escape back and close");
                Press(keyboard, Key.Tab); Check(demo.PhoneOpen, "Tab reopens main");
                var originalConfig = demo.Settings;
                var edited = UnityEngine.Object.Instantiate(originalConfig);
                demo.UseSettings(edited);
                Click(demo, "Shop button");
                edited.upgrades.Clear(); demo.ApplyConfiguration(); Check(BuyCount(demo) == 0, "empty shop remains safe");
                demo.UseSettings(originalConfig); UnityEngine.Object.DestroyImmediate(edited);
                demo.Progress.Grant(10000);
                foreach (var option in demo.Settings.upgrades) while (demo.Progress.CanBuy(option)) demo.Progress.Buy(option);
                demo.Refresh(); Click(demo, "Letters button");
                while (Button(demo, "Next letter").interactable) Click(demo, "Next letter");
                Click(demo, "Reply to mayor"); Check(demo.EndingOpen, "final-letter ending retained");
                Capture(demo, "PixelUI-Ending-1080p.png");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard); InputSystem.settings = originalInput;
                UnityEngine.Object.DestroyImmediate(checkInput);
            }
        }
        // BEGIN ADDED: Offscreen Unity renders allow review at the target 1080p size.
        static void Capture(CheeseTownDemo demo, string name)
        {
            var canvas = demo.ScreenCanvas;
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            var previousTarget = camera.targetTexture;
            var scaler = canvas.GetComponent<CanvasScaler>(); float previousScale = scaler.scaleFactor;
            var texture = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(1920, 1080, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            try
            {
                texture.Create(); camera.targetTexture = texture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                scaler.scaleFactor = 3; Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name, image.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active = previousActive; camera.targetTexture = previousTarget;
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                scaler.scaleFactor = previousScale; Canvas.ForceUpdateCanvases();
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        // END ADDED
        static void Finish(Exception error)
        {
            SessionState.SetBool(Request, false); Time.timeScale = 1;
            // BEGIN ADDED: Batch exit must restore the user's fast-play preference too.
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Request + ".FastPlay", false);
            // END ADDED
            Directory.CreateDirectory("Logs");
            string result = error == null
                ? "PASS: 2x artwork imports; full frame and button dimensions; selected font; integer UI scaling; pointer raycast; Tab/Escape/Back; collect payout; category filters; buy/debit/max level; live wallet; unread/read dot; empty shop; final-letter ending. Unity " + Application.unityVersion
                : "FAIL: " + error;
            File.WriteAllText("Logs/pixel-ui-checks.txt", result);
            if (error == null) Debug.Log(result); else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
// END ADDED
