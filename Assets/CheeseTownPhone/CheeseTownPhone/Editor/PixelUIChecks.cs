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
            // BEGIN ADDED: The supplied font and every shop state must be bound and sharp.
            Check(AssetDatabase.GetAssetPath(config.interfaceFont) == PixelUIImport.ArtPath + "Fonts/PressStart2P.ttf", "replacement font selected");
            // BEGIN ADDED: A saved template and native bitmap font must ship with the runtime settings.
            Check(config.upgradeRowPrefab != null && PrefabUtility.IsPartOfPrefabAsset(config.upgradeRowPrefab), "real upgrade prefab assigned");
            Check(config.upgradePixelFont != null && !config.upgradePixelFont.dynamic && config.upgradePixelFont.fontSize == 8, "native bitmap glyph atlas");
            // BEGIN ADDED: The details cue must be a real glyph in the same sharp atlas.
            Check(config.upgradePixelFont.HasCharacter('\u25B6'), "right triangle glyph imported");
            // END ADDED
            Check(config.upgradeRowArtwork.rect.height == 110, "updated taller source bar");
            // END ADDED
            foreach (var sprite in new[] { config.upgradeFrame, config.upgradeBackground, config.upgradeRowArtwork, config.upgradeIconBox,
                config.upgradeLevelArtwork, config.upgradeLevelInactive, config.upgradeLevelActive, config.upgradeButtonArtwork,
                config.upgradeBuyDisabled, config.allSelected, config.allUnselected, config.playerSelected,
                config.playerUnselected, config.treeSelected, config.treeUnselected })
                Check(sprite != null && sprite.texture.filterMode == FilterMode.Point && sprite.texture.mipmapCount == 1, "shop artwork imported");
            // END ADDED
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
                // BEGIN ADDED: Compare against an uncropped generator to catch missing last lines.
                foreach (var text in demo.GetComponentsInChildren<Text>().Where(t => t.name == "Title" || t.name == "Description"))
                {
                    var full = new TextGenerator();
                    var generation = text.GetGenerationSettings(text.rectTransform.rect.size);
                    // BEGIN ADDED: Offscreen masked rows have no rendered cache yet.
                    var clipped = new TextGenerator(); clipped.Populate(text.text, generation);
                    // END ADDED
                    generation.verticalOverflow = VerticalWrapMode.Overflow;
                    full.Populate(text.text, generation);
                    Check(clipped.characterCountVisible == full.characterCountVisible,
                        "complete " + text.name + ": " + text.text + " (" + clipped.characterCountVisible + "/" + full.characterCountVisible + ")");
                }
                // END ADDED
                // BEGIN ADDED: Wheel events must bubble from row buttons to the clipped list.
                Check(Button(demo, "All filter").GetComponent<Image>().sprite == config.allSelected, "selected category sprite");
                Check(Button(demo, "Player filter").GetComponent<Image>().sprite == config.playerUnselected, "unselected category sprite");
                var scroll = demo.GetComponentsInChildren<ScrollRect>().Single();
                Check(scroll.content.rect.height > scroll.viewport.rect.height, "default list overflows for scrolling");
                // BEGIN CHANGED: Nine-slicing widens the blank price area for four large digits.
                Check(Button(demo, "Buy move-speed").GetComponent<RectTransform>().sizeDelta == new Vector2(176, 37), "four-digit buy layout");
                // END CHANGED
                Check(Button(demo, "Buy move-speed").GetComponentsInChildren<Text>().Single(t => t.name == "Price").text == "20", "price fits beside baked BUY text");
                var wheelTarget = Button(demo, "Buy move-speed");
                var wheelPoint = RectTransformUtility.WorldToScreenPoint(null, wheelTarget.transform.TransformPoint(wheelTarget.GetComponent<RectTransform>().rect.center));
                var wheelHits = new List<RaycastResult>();
                var wheel = new PointerEventData(EventSystem.current) { position = wheelPoint, scrollDelta = new Vector2(0, -1) };
                demo.ScreenCanvas.GetComponent<GraphicRaycaster>().Raycast(wheel, wheelHits);
                Check(wheelHits.Count > 0 && wheelHits[0].gameObject == wheelTarget.gameObject, "wheel hits a row button");
                ExecuteEvents.ExecuteHierarchy(wheelHits[0].gameObject, wheel, ExecuteEvents.scrollHandler);
                Canvas.ForceUpdateCanvases();
                Check(scroll.content.anchoredPosition.y > 0, "wheel scrolls from a buy button");
                for (int i = 0; i < 20; i++) scroll.OnScroll(wheel);
                Canvas.ForceUpdateCanvases();
                Check(Mathf.Abs(scroll.content.anchoredPosition.y - (scroll.content.rect.height - scroll.viewport.rect.height)) < 1, "wheel clamps at bottom");
                Capture(demo, "PixelUI-Shop-Scrolled-1080p.png");
                wheel.scrollDelta = new Vector2(0, 1);
                for (int i = 0; i < 20; i++) scroll.OnScroll(wheel);
                Canvas.ForceUpdateCanvases();
                Check(Mathf.Abs(scroll.content.anchoredPosition.y) < 1, "wheel returns to top");
                // END ADDED
                // BEGIN ADDED: Hover retains full text outside the row mask and dismisses on list motion.
                var firstView = Button(demo, "Buy move-speed").GetComponentInParent<UpgradeRowView>();
                Check(firstView.DisplayLevel == 1 && firstView.DisplayMaxLevel == 3 && firstView.level.text == "LV 1/3", "one-based starting level");
                // BEGIN ADDED: The default four-level tree fits one marker line at the new art height.
                Check(Button(demo, "Buy tree-growth").GetComponentInParent<UpgradeRowView>().Height == 55, "four markers fit without an extra row");
                // END ADDED
                // BEGIN CHANGED: Validate the artist's saved font sizes instead of enforcing the initial defaults.
                Check(firstView.title.font.material.mainTexture.filterMode == FilterMode.Point
                    && firstView.description.fontSize == config.upgradeRowPrefab.description.fontSize
                    && firstView.price.fontSize == config.upgradeRowPrefab.price.fontSize, "sharp row text preserves prefab font sizes");
                // END CHANGED
                var hoverView = Button(demo, "Buy auto-collect").GetComponentInParent<UpgradeRowView>();
                // BEGIN CHANGED: Truncated effect text advertises its description-only hover target.
                Check(hoverView.description.text.EndsWith("... \u25B6"), "preview ends with a details cue");
                var tooltip = ((UpgradeListScrollRect)scroll).tooltip;
                Check(!ExecuteEvents.CanHandleEvent<IPointerEnterHandler>(firstView.gameObject), "row itself has no hover trigger");
                ExecuteEvents.ExecuteHierarchy(firstView.buy.gameObject, wheel, ExecuteEvents.pointerEnterHandler);
                tooltip.Tick(Time.unscaledTime + 1); Check(!tooltip.IsVisible, "Buy hover does not open details");
                var descriptionPoint = RectTransformUtility.WorldToScreenPoint(null,
                    firstView.description.transform.TransformPoint(firstView.description.rectTransform.rect.center));
                var descriptionPointer = new PointerEventData(EventSystem.current) { position = descriptionPoint };
                var descriptionHits = new List<RaycastResult>();
                demo.ScreenCanvas.GetComponent<GraphicRaycaster>().Raycast(descriptionPointer, descriptionHits);
                Check(descriptionHits.Count > 0 && descriptionHits[0].gameObject == firstView.description.gameObject, "description receives the pointer");
                ExecuteEvents.Execute(descriptionHits[0].gameObject, descriptionPointer, ExecuteEvents.pointerEnterHandler);
                tooltip.Tick(Time.unscaledTime + 1);
                Check(tooltip.IsVisible && tooltip.FullText == firstView.Option.description, "tooltip contains only the full effect text");
                Check(!tooltip.transform.IsChildOf(scroll.viewport), "tooltip is outside the list mask");
                Capture(demo, "PixelUI-Shop-Tooltip-1080p.png");
                Check(!tooltip.GetComponent<CanvasGroup>().blocksRaycasts
                    && tooltip.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), "visible tooltip is entirely click-through");
                var savedPopupPosition = tooltip.transform.position;
                tooltip.transform.position = firstView.buy.transform.position;
                Canvas.ForceUpdateCanvases();
                var purchaseHits = new List<RaycastResult>();
                demo.ScreenCanvas.GetComponent<GraphicRaycaster>().Raycast(wheel, purchaseHits);
                Check(purchaseHits.Count > 0 && purchaseHits[0].gameObject == firstView.buy.gameObject, "Buy remains hittable even under the popup");
                tooltip.transform.position = savedPopupPosition; Canvas.ForceUpdateCanvases();
                // END CHANGED
                // BEGIN ADDED: The popup must stay within the content frame and clear on navigation.
                var popupRect = (RectTransform)tooltip.transform;
                Check(popupRect.anchoredPosition.x >= 32 && popupRect.anchoredPosition.x + popupRect.rect.width <= 608,
                    "tooltip horizontal bounds");
                Check(-popupRect.anchoredPosition.y >= 72 && -popupRect.anchoredPosition.y + popupRect.rect.height <= 338,
                    "tooltip vertical bounds");
                demo.ShowPage(0); demo.ShowPage(1); Check(!tooltip.IsVisible, "returning to shop has no stale tooltip");
                // END ADDED
                // BEGIN CHANGED: Exercise the dedicated description component, not the row.
                var descriptionHover = firstView.description.GetComponent<UpgradeDescriptionHover>();
                descriptionHover.OnPointerEnter(descriptionPointer); tooltip.Tick(Time.unscaledTime + 1);
                descriptionHover.OnPointerExit(descriptionPointer);
                Check(!tooltip.IsVisible, "pointer exit hides tooltip");
                descriptionHover.OnPointerEnter(descriptionPointer); tooltip.Tick(Time.unscaledTime + 1);
                scroll.OnScroll(wheel); Check(!tooltip.IsVisible, "wheel hides tooltip at the top boundary");
                // END CHANGED
                // END ADDED
                // BEGIN ADDED: Font metrics must not silently crop upgrade titles.
                Check(demo.GetComponentsInChildren<Text>().Where(t => t.name == "Title")
                    .All(t => t.cachedTextGenerator.characterCountVisible > 0), "upgrade titles are visible");
                // BEGIN CHANGED: The full-height shop hides the town footer entirely.
                Check(!demo.GetComponentsInChildren<Button>().Any(b => b.name == "Collect cheese"), "shop hides collection footer");
                // END CHANGED
                // END ADDED
                before = demo.Progress.Cheeses;
                // BEGIN ADDED: Prefab purchases must still reach actual world components.
                var testPlayer = new GameObject("Prefab purchase player").AddComponent<PlayerController>();
                var testTreeRoot = new GameObject("Prefab purchase tree"); testTreeRoot.tag = "Tree";
                var testHurtbox = new GameObject("Hurtbox"); testHurtbox.transform.SetParent(testTreeRoot.transform);
                testHurtbox.AddComponent<BoxCollider>(); testHurtbox.AddComponent<TreeHurtbox>();
                // END ADDED
                Click(demo, "Buy move-speed"); Check(demo.Progress.MoveSpeed == 6 && demo.Progress.Cheeses == before - 20, "buy effect and debit");
                // BEGIN ADDED: Visual level two is exactly one paid upgrade.
                Check(testPlayer.speed.x == 6 && firstView.DisplayLevel == 2, "world player receives prefab purchase");
                // END ADDED
                Check(demo.GetComponentsInChildren<Text>().Single(t => t.name == "Wallet").text == demo.Progress.Cheeses.ToString(), "live numeric wallet");
                Click(demo, "Buy move-speed"); Check(!Button(demo, "Buy move-speed").interactable, "max-level purchase disabled");
                // BEGIN ADDED: Sprite state and level markers must follow actual purchase progress.
                Check(Button(demo, "Buy move-speed").GetComponent<Image>().overrideSprite == config.upgradeBuyDisabled, "max-level disabled artwork");
                Check(Button(demo, "Buy move-speed").transform.parent.GetComponentsInChildren<Image>()
                    .Where(i => i.name.StartsWith("Level marker ")).All(i => i.sprite == config.upgradeLevelActive), "purchased level markers");
                Capture(demo, "PixelUI-Shop-Purchased-1080p.png");
                // END ADDED
                // BEGIN ADDED: Tree listeners receive the same shared purchase event.
                Click(demo, "Buy tree-growth");
                Check(Mathf.Abs(testTreeRoot.transform.localScale.x - 1.2f) < .001f && demo.Progress.Production == 2,
                    "world tree growth and production remain connected");
                UnityEngine.Object.DestroyImmediate(testPlayer.gameObject); UnityEngine.Object.DestroyImmediate(testTreeRoot);
                // END ADDED
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
                // BEGIN ADDED: Unaffordable purchases use the supplied disabled state too.
                edited.upgrades = TabletSettings.Defaults();
                edited.upgrades.Single(o => o.id == "collect-range").levels[0].price = 1000000000;
                demo.ApplyConfiguration();
                Check(!Button(demo, "Buy collect-range").interactable && Button(demo, "Buy collect-range").GetComponent<Image>().overrideSprite == config.upgradeBuyDisabled,
                    "insufficient funds disables artwork");
                // END ADDED
                // BEGIN ADDED: Newly configured options reuse the prefab, including longer level chains and four-digit prices.
                var extra = new UpgradeOption { id = "prefab-extra", title = "Extra Speed", effect = UpgradeEffect.MoveSpeed,
                    description = string.Join(" ", Enumerable.Repeat("A longer explanation remains available in full when the row is hovered.", 20)),
                    levels = new List<UpgradeLevel> { new UpgradeLevel(9999, 22), new UpgradeLevel(20, 24),
                        new UpgradeLevel(20, 26), new UpgradeLevel(20, 28), new UpgradeLevel(20, 30) } };
                edited.upgrades.Add(extra); demo.ApplyConfiguration();
                Check(BuyCount(demo) == 6, "data-only addition creates another prefab row");
                var extraView = Button(demo, "Buy prefab-extra").GetComponentInParent<UpgradeRowView>();
                Check(extraView.DisplayLevel == 1 && extraView.DisplayMaxLevel == 6 && extraView.Height > 55, "extra levels expand marker rows");
                Check(extraView.price.text == "9999" && UpgradeRowView.Fits("9999", extraView.price), "four-digit price fits at sixteen pixels");
                // BEGIN ADDED: Capture the exact four-digit case instead of only measuring it.
                var extraList = demo.GetComponentsInChildren<UpgradeListScrollRect>().Single();
                extraList.verticalNormalizedPosition = 0;
                Capture(demo, "PixelUI-Shop-FourDigits-1080p.png");
                // END ADDED
                var extraScroll = demo.GetComponentsInChildren<UpgradeListScrollRect>().Single();
                // BEGIN CHANGED: Long details remain readable without making the popup capture input.
                var extraHover = extraView.description.GetComponent<UpgradeDescriptionHover>();
                extraHover.OnPointerEnter(wheel); extraScroll.tooltip.Tick(Time.unscaledTime + 1);
                Check(extraScroll.tooltip.IsVisible && extraScroll.tooltip.FullText == extra.description, "extended effect text retained without extra stats");
                var tipScroll = extraScroll.tooltip.GetComponentInChildren<ScrollRect>();
                Check(tipScroll.content.rect.height > tipScroll.viewport.rect.height, "very long tooltip text remains scrollable");
                float listBeforeDetailsScroll = extraScroll.content.anchoredPosition.y;
                wheel.scrollDelta = new Vector2(0, -1); extraHover.OnScroll(wheel);
                Check(tipScroll.content.anchoredPosition.y > 0 && extraScroll.content.anchoredPosition.y == listBeforeDetailsScroll,
                    "wheel over the description reads long details without moving the list");
                // END CHANGED
                extraScroll.tooltip.Hide();
                demo.Progress.Grant(10000); demo.Refresh(); Click(demo, "Buy prefab-extra");
                Check(demo.Progress.MoveSpeed == 22 && extraView.DisplayLevel == 2, "new option uses the original purchase path");
                // END ADDED
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
                // BEGIN CHANGED: Include the new skin, wheel and state checks in the report.
                ? "PASS: description-only hover; effect-only tooltip; triangle glyph/preview cue; click-through popup/Buy pointer hits; immediate dismissal; long details scrolling; prefab; bitmap Point font; one-based levels; four-digit prices; data-only upgrades; player/tree purchase connections; 2x artwork; integer UI scale; pointer/wheel bounds; filters/disabled states; navigation; payout; buy/debit/max; wallet; mail; empty shop; ending. Unity " + Application.unityVersion
                // END CHANGED
                : "FAIL: " + error;
            File.WriteAllText("Logs/pixel-ui-checks.txt", result);
            if (error == null) Debug.Log(result); else Debug.LogException(error);
            if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
            else EditorApplication.isPlaying = false;
        }
    }
}
// END ADDED
