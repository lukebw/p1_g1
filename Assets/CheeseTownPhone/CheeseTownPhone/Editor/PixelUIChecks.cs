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
            // BEGIN ADDED: Existing interaction checks assert settled UI; motion is exercised separately.
            demo.CompleteUITransitions();
            // END ADDED
        }
        static int BuyCount(CheeseTownDemo demo) => demo.GetComponentsInChildren<Button>().Count(b => b.name.StartsWith("Buy "));
        static void Press(Keyboard keyboard, Key key)
        {
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(key)); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
            // BEGIN ADDED: Complete only presentation, leaving the input/economy path unchanged.
            UnityEngine.Object.FindAnyObjectByType<CheeseTownDemo>()?.CompleteUITransitions();
            // END ADDED
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
                CheckMailPresentation(demo);
                Click(demo, "Reply to mayor"); Click(demo, "Close tablet");
                Check(demo.PhoneOpen && !demo.GetComponentsInChildren<Text>().Any(t => t.name == "Mayor message"), "Back returns to main");
                // BEGIN CHANGED: Native frame slices allow the footer to move independently.
                Check(config.tabletPrefab != null && PrefabUtility.IsPartOfPrefabAsset(config.tabletPrefab), "editable tablet prefab assigned");
                Check(demo.View.frame.GetComponent<RectTransform>().sizeDelta == new Vector2(640, 360), "2x PNG maps to native size");
                Check(demo.View.frame.GetComponentsInChildren<RawImage>().Length == 4, "frame uses four unchanged texture crops");
                CheckMotion(demo);
                // BEGIN ADDED: A contrasting world color exposes gaps that a dark preview background hides.
                CheckFrameCoverage(demo);
                // END ADDED
                // END CHANGED
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
                // BEGIN ADDED: Baking the hidden home layout must not leave the shop background disabled.
                Check(demo.View.shopGroup.GetComponentsInChildren<Image>().Any(i => i.name == "Pixel shop background" && i.sprite == config.upgradeBackground), "shop retains its full-height green background");
                // END ADDED
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
                // BEGIN ADDED: Exercise real pointer states, including refresh and disabled purchases.
                CheckPurchaseFeedback(demo, firstView);
                // END ADDED
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
                // BEGIN CHANGED: Assert tooltip dismissal after the reversible page transition settles.
                demo.ShowPage(0); demo.ShowPage(1); demo.CompleteUITransitions(); Check(!tooltip.IsVisible, "returning to shop has no stale tooltip");
                // END CHANGED
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
                Check(demo.View.hudWallet.text == demo.Progress.Cheeses.ToString(), "HUD reflects purchase debit while hidden");
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
                Check(!demo.View.hudUnreadDot.activeSelf, "reading all mail clears the gameplay badge too");
                Press(keyboard, Key.Escape); Press(keyboard, Key.Escape); Check(!demo.PhoneOpen, "Escape back and close");
                Press(keyboard, Key.Tab); Check(demo.PhoneOpen && demo.View.Page == 2, "Tab reopens messages after welcome is read");
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
                foreach (var message in demo.Progress.Letters) CheckTextFits(demo.View.letter, message.Body);
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
        // BEGIN ADDED: Exercise reversals and input gating at intermediate states, with scaled time stopped.
        static void CheckMotion(CheeseTownDemo demo)
        {
            var view = demo.View; var panel = view.panel; var wallet = view.wallet;
            demo.ShowPage(1); view.Advance(.1f); view.upgrades.Advance(.1f);
            Check(view.IsTransitioning && view.footerMotion.anchoredPosition.y < view.footerHomePosition.y, "footer moves down while timeScale is zero");
            Check(view.footerMotion.anchoredPosition.y > view.footerHomePosition.y - view.footerTravel, "footer has a real intermediate pose");
            Check(view.upgrades.IsDealing && !view.shopGroup.interactable, "cards unfold while page input is gated");
            Capture(demo, "PixelUI-Shop-Transition-1080p.png");
            demo.ShowPage(0); view.Advance(.05f); demo.ShowPage(1); demo.CompleteUITransitions();
            Check(view.panel == panel && view.wallet == wallet && !view.footerGroup.gameObject.activeSelf, "rapid navigation keeps component identity and reaches shop");
            var scroll = view.upgrades.scroll; scroll.content.anchoredPosition = new Vector2(0, 12);
            demo.Refresh(); Check(scroll.content.anchoredPosition.y == 12 && !view.upgrades.IsDealing, "refresh preserves scrolling without replaying cards");
            Click(demo, "Player filter"); Check(demo.View == view && view.panel == panel && view.wallet == wallet, "filter rebuilds only rows");
            Click(demo, "All filter"); demo.ShowPage(0); demo.CompleteUITransitions();
            demo.TogglePhone(); view.Advance(.08f);
            Check(!demo.PhoneOpen && demo.BlocksWorldInput && view.panelGroup.alpha > 0 && view.panelGroup.alpha < 1, "closing fades and blocks world input");
            demo.TogglePhone(); view.Advance(.04f); demo.TogglePhone(); demo.CompleteUITransitions();
            Check(!view.IsVisible && !demo.BlocksWorldInput, "reversed close releases gameplay at completion");
            demo.TogglePhone(); view.Advance(view.openDuration - .00005f);
            Check(view.IsTransitioning, "near-complete opening still advances to its input-ready endpoint");
            view.Advance(.001f);
            Check(demo.PhoneOpen && !view.IsTransitioning && view.mailGroup.interactable, "reopen restores message interaction");
            demo.ShowPage(0); demo.CompleteUITransitions();
        }
        // BEGIN ADDED: Validate native mail art, mutually exclusive footer faces and shared HUD data.
        static void CheckMailPresentation(CheeseTownDemo demo)
        {
            var view = demo.View;
            foreach (var sprite in new[] { demo.Settings.mailPaper, demo.Settings.mailPrevious, demo.Settings.mailNext, demo.Settings.mailRead })
                Check(sprite != null && sprite.texture.filterMode == FilterMode.Point && sprite.texture.mipmapCount == 1, "mail art stays sharp");
            Check(view.mailFooter.gameObject.activeInHierarchy && !view.townFooter.gameObject.activeInHierarchy, "mail replaces collection footer");
            Check(view.read.GetComponentsInChildren<Text>().Length == 0 && view.launcher.GetComponentsInChildren<Text>(true).Length == 0,
                "baked button lettering is not duplicated and old Tab launcher text is removed");
            foreach (var letter in demo.Progress.Letters)
                CheckTextFits(view.letter, letter.Body);
            Capture(demo, "MailUI-Welcome.png");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            view.read.OnPointerEnter(pointer);
            Check(view.read.targetGraphic.canvasRenderer.GetColor() == view.back.colors.highlightedColor, "mail buttons share hover feedback");
            view.read.OnPointerDown(pointer);
            Check(view.read.targetGraphic.canvasRenderer.GetColor() == view.back.colors.pressedColor, "mail buttons share press feedback");
            view.read.OnPointerUp(pointer); view.read.OnPointerExit(pointer); EventSystem.current.SetSelectedGameObject(null);
            for (int turn = 0; turn < 3; turn++)
            {
                demo.ShowPage(turn == 1 ? 2 : 0);
                for (int frame = 0; frame < 8; frame++)
                {
                    view.Advance(view.pageDuration / 8);
                    Check(!(view.townFooter.gameObject.activeSelf && view.mailFooter.gameObject.activeSelf), "footer faces never overlap");
                    if (view.IsTransitioning) Check(!view.footerGroup.interactable, "moving controls reject input");
                    if (turn == 1 && frame == 5) Capture(demo, "MailUI-Footer-Turn.png");
                }
            }
            demo.ShowPage(2); view.Advance(.08f); demo.ShowPage(0); view.Advance(.04f);
            demo.ShowPage(2); demo.CompleteUITransitions();
            Check(view.mailFooter.interactable && view.read.IsInteractable() && !view.townFooter.gameObject.activeSelf, "rapid reversal settles on readable mail");
            Canvas.ForceUpdateCanvases();
            var point = RectTransformUtility.WorldToScreenPoint(null, view.read.transform.TransformPoint(((RectTransform)view.read.transform).rect.center));
            var hits = new List<RaycastResult>();
            demo.ScreenCanvas.GetComponent<GraphicRaycaster>().Raycast(new PointerEventData(EventSystem.current) { position = point }, hits);
            Check(hits.Count > 0 && hits[0].gameObject == view.read.gameObject, "read button is not covered by collection UI or paper");
            demo.TogglePhone(); demo.CompleteUITransitions();
            Check(view.worldHud.gameObject.activeInHierarchy && view.hudWallet.text == demo.Progress.Cheeses.ToString()
                && view.hudUnreadDot.activeSelf, "closed HUD shows live cheese count and unread badge");
            Check(view.closedUnread.text == "Press TAB to open messages." && view.closedUnread.gameObject.activeInHierarchy, "tutorial retains the new Tab hint");
            Capture(demo, "MailUI-Gameplay-HUD.png");
            Capture(demo, "MailUI-Gameplay-HUD-OddViewport.png", 1601, 901);
            view.launcher.onClick.Invoke(); demo.CompleteUITransitions();
            Check(view.Page == 2 && !view.worldHud.gameObject.activeSelf, "HUD envelope opens mail and hides behind panel");
        }
        static void CheckTextFits(Text text, string value)
        {
            var clipped = new TextGenerator(); var full = new TextGenerator();
            var generation = text.GetGenerationSettings(text.rectTransform.rect.size);
            clipped.Populate(value, generation); generation.verticalOverflow = VerticalWrapMode.Overflow; full.Populate(value, generation);
            Check(clipped.characterCountVisible == full.characterCountVisible, "complete mail body fits paper: " + value);
        }
        // END ADDED
        // END ADDED
        // BEGIN ADDED: Inspect rendered seam pixels at integer scales and an odd-size centered viewport.
        static void CheckFrameCoverage(CheeseTownDemo demo)
        {
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            var previous = camera.backgroundColor; camera.backgroundColor = Color.magenta;
            try
            {
                foreach (var size in new[] { new Vector2Int(1280, 720), new Vector2Int(1920, 1080),
                    new Vector2Int(2560, 1440), new Vector2Int(1601, 901) })
                {
                    Capture(demo, "PixelUI-Frame-" + size.x + "x" + size.y + ".png", size.x, size.y, CheckEdgePixels);
                }
                demo.ShowPage(1); demo.View.Advance(.15f); demo.View.upgrades.Advance(.15f);
                Capture(demo, "PixelUI-Frame-Transition.png", 1920, 1080, CheckEdgePixels);
                demo.CompleteUITransitions();
                Capture(demo, "PixelUI-Frame-Shop.png", 1920, 1080, CheckEdgePixels);
                demo.ShowPage(0); demo.CompleteUITransitions();
            }
            finally { camera.backgroundColor = previous; }
        }
        static void CheckEdgePixels(Texture2D image)
        {
            var outside = image.GetPixel(0, 0);
            Check(outside.r > .9f && outside.g < .1f && outside.b > .9f, "seam probe renders the contrasting world background");
            float scale = CheeseTownDemo.PixelScaleFor(image.width, image.height);
            float left = (image.width - 640 * scale) / 2, top = (image.height + 360 * scale) / 2;
            foreach (float x in new[] { 30.5f, 609.5f })
                foreach (float y in new[] { 100.5f, 200.5f, 289.5f, 290.5f, 299.5f, 320.5f, 338.5f })
                {
                    var pixel = image.GetPixel(Mathf.FloorToInt(left + x * scale), Mathf.FloorToInt(top - y * scale));
                    Check(!(pixel.r > .9f && pixel.g < .1f && pixel.b > .9f),
                        "opaque inner frame at " + x + "," + y + " / " + image.width + "x" + image.height);
                }
        }
        static void CheckPurchaseFeedback(CheeseTownDemo demo, UpgradeRowView row)
        {
            var button = row.buy; var header = demo.View.shopButton;
            Check(button.transition == Selectable.Transition.ColorTint && button.colors.fadeDuration == header.colors.fadeDuration
                && button.colors.highlightedColor == header.colors.highlightedColor && button.colors.pressedColor == header.colors.pressedColor,
                "buy uses the same hover/press tint as header buttons");
            var pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            EventSystem.current.SetSelectedGameObject(null); button.OnPointerExit(pointer);
            Check(button.targetGraphic.canvasRenderer.GetColor() == button.colors.normalColor, "buy rests at its normal tint");
            button.OnPointerEnter(pointer); demo.Refresh();
            Check(button.targetGraphic.canvasRenderer.GetColor() == button.colors.highlightedColor, "buy hover survives data refresh");
            Capture(demo, "PixelUI-Buy-Hover.png");
            button.OnPointerDown(pointer); demo.Refresh();
            Check(button.targetGraphic.canvasRenderer.GetColor() == button.colors.pressedColor, "buy darkens on pointer down");
            Capture(demo, "PixelUI-Buy-Pressed.png");
            button.OnPointerUp(pointer); EventSystem.current.SetSelectedGameObject(null);
            Check(button.targetGraphic.canvasRenderer.GetColor() == button.colors.highlightedColor, "release restores hover");
            row.RefreshDisplay(0, row.Option.levels.Count, false, "SPEED", "4", "6");
            Check(button.targetGraphic.canvasRenderer.GetColor() == Color.white && button.GetComponent<Image>().sprite == row.disabledArtwork,
                "disabled artwork remains untinted");
            int coins = demo.Progress.Cheeses, level = demo.Progress.Level(row.Option);
            button.OnPointerClick(pointer);
            Check(demo.Progress.Cheeses == coins && demo.Progress.Level(row.Option) == level, "disabled pointer click cannot purchase");
            demo.Refresh(); button.OnPointerExit(pointer); EventSystem.current.SetSelectedGameObject(null);
            Check(button.targetGraphic.canvasRenderer.GetColor() == button.colors.normalColor, "pointer exit restores normal tint");
        }
        // END ADDED
        // BEGIN CHANGED: Variable render sizes and pixel inspection verify coverage without changing game settings.
        static void Capture(CheeseTownDemo demo, string name, int width = 1920, int height = 1080, Action<Texture2D> inspect = null)
        {
            var canvas = demo.ScreenCanvas;
            var camera = UnityEngine.Object.FindAnyObjectByType<Camera>();
            var previousMode = canvas.renderMode; var previousCamera = canvas.worldCamera;
            float previousDistance = canvas.planeDistance;
            var previousTarget = camera.targetTexture;
            var scaler = canvas.GetComponent<CanvasScaler>(); float previousScale = scaler.scaleFactor;
            var texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
            var previousActive = RenderTexture.active;
            try
            {
                texture.Create(); camera.targetTexture = texture;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                scaler.scaleFactor = CheeseTownDemo.PixelScaleFor(width, height); Canvas.ForceUpdateCanvases();
                demo.View.PositionHud((RectTransform)canvas.transform); Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = texture });
                RenderTexture.active = texture; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
                Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + name, image.EncodeToPNG());
                inspect?.Invoke(image);
            }
            finally
            {
                RenderTexture.active = previousActive; camera.targetTexture = previousTarget;
                canvas.renderMode = previousMode; canvas.worldCamera = previousCamera; canvas.planeDistance = previousDistance;
                scaler.scaleFactor = previousScale; Canvas.ForceUpdateCanvases();
                demo.View.PositionHud((RectTransform)canvas.transform);
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(texture);
            }
        }
        // END CHANGED
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
                ? "PASS: supplied mail paper/buttons; full letter text fit; exclusive sliding footer faces; rapid mail reversals; mail button pointer/hover/press; gameplay HUD currency/unread data; envelope/Tab routing; retained tutorial hint; opaque frame seams at 720p/1080p/1440p/odd viewport and during transition; buy hover/press/release/exit tint; tint survives refresh; disabled artwork/click guard; editable tablet prefab; cropped wood frame; green shop background; intermediate footer/card animation; unscaled motion; rapid reversals; closing world-input lock; component identity across filtering; scroll preserved on refresh; description-only hover; effect-only tooltip; triangle glyph/preview cue; click-through popup/Buy pointer hits; immediate dismissal; long details scrolling; prefab; bitmap Point font; one-based levels; four-digit prices; data-only upgrades; player/tree purchase connections; 2x artwork; integer UI scale; pointer/wheel bounds; filters/disabled states; navigation; payout; buy/debit/max; wallet; mail; empty shop; ending. Unity " + Application.unityVersion
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
