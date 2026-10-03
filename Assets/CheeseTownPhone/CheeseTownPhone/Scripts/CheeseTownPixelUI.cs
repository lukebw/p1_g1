// BEGIN ADDED: Native pixel layout replaces placeholders without changing game rules.
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed partial class CheeseTownDemo
    {
        const float ExportScale = 2f;
        static readonly Vector2 PixelLayoutSize = new Vector2(640, 360);
        readonly Color pixelInk = new Color32(102, 51, 34, 255);
        GameObject unreadDotImage;
        GameObject pixelTreeCaption;
        bool HasPixelSkin => settings != null && settings.mainFrame != null;

        // BEGIN ADDED: Integer enlargement keeps each drawn pixel equally sized.
        public static float PixelScaleFor(int width, int height)
        {
            float fit = Mathf.Min(width / PixelLayoutSize.x, height / PixelLayoutSize.y);
            return fit >= 1 ? Mathf.Floor(fit) : Mathf.Max(.01f, fit);
        }
        void ConfigurePixelLayout()
        {
            var scaler = ScreenCanvas.GetComponent<CanvasScaler>();
            ScreenCanvas.pixelPerfect = HasPixelSkin;
            scaler.referenceResolution = HasPixelSkin ? PixelLayoutSize : new Vector2(1600, 900);
            scaler.uiScaleMode = HasPixelSkin ? CanvasScaler.ScaleMode.ConstantPixelSize : CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            stage.sizeDelta = scaler.referenceResolution;
            UpdatePixelScale();
        }
        void UpdatePixelScale()
        {
            if (HasPixelSkin && ScreenCanvas != null)
                ScreenCanvas.GetComponent<CanvasScaler>().scaleFactor = PixelScaleFor(Screen.width, Screen.height);
        }
        // END ADDED

        // BEGIN ADDED: Two-times exports occupy half their PNG dimensions.
        Image PixelArtwork(Transform parent, string name, Sprite sprite, float x, float y)
        {
            if (sprite == null) return null;
            Vector2 size = sprite.rect.size / ExportScale;
            return Artwork(parent, name, sprite, "", x, y, size.x, size.y, Color.white, false);
        }
        Text PixelLabel(Transform parent, string name, string value, float x, float y, float w, float h,
            int size = 8, TextAnchor align = TextAnchor.UpperLeft, bool light = false)
        {
            return Label(parent, name, value, x, y, w, h, size, light ? paper : pixelInk, false, align);
        }
        Button PixelAction(Transform parent, string name, string text, float x, float y, float w, float h,
            UnityAction click, bool gold = false)
        {
            var button = Action(parent, name, text, x, y, w, h, click, gold);
            var label = button.GetComponentInChildren<Text>();
            label.fontSize = 8; label.fontStyle = FontStyle.Normal;
            label.rectTransform.anchoredPosition = Vector2.zero;
            label.rectTransform.sizeDelta = new Vector2(w, h);
            return button;
        }
        Button PixelSpriteButton(Transform parent, string name, Sprite sprite, string fallback,
            float x, float y, UnityAction click)
        {
            Vector2 size = sprite != null ? sprite.rect.size / ExportScale : new Vector2(40, 30);
            var button = PixelAction(parent, name, sprite != null ? "" : fallback, x, y, size.x, size.y, click);
            if (sprite != null)
            {
                var image = button.GetComponent<Image>();
                image.sprite = sprite; image.type = Image.Type.Simple; image.preserveAspect = false; image.color = Color.white;
                var colors = button.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(1, .95f, .85f);
                colors.pressedColor = new Color(.8f, .8f, .8f); colors.disabledColor = new Color(.55f, .55f, .55f, .8f);
                colors.fadeDuration = 0; button.colors = colors;
            }
            return button;
        }
        void BackFromPixelPage()
        {
            if (EndingOpen) return;
            if (page != 0) ShowPage(0); else TogglePhone();
        }
        void UpdateUnreadDot()
        {
            if (unreadDotImage != null) unreadDotImage.SetActive(Progress.UnreadCount > 0);
        }
        // BEGIN ADDED: Home-only text must not peek out below mailbox panels.
        void UpdatePixelPageVisibility()
        {
            if (!HasPixelSkin || tree == null) return;
            bool home = page == 0;
            tree.gameObject.SetActive(home);
            if (pixelTreeCaption != null) pixelTreeCaption.SetActive(home);
            stock.gameObject.SetActive(home);
        }
        // END ADDED
        // END ADDED

        // BEGIN ADDED: Place artwork against UI_Main_ref while keeping live readouts.
        void RenderPixelSkin(bool open)
        {
            foreach (Transform child in stage) { child.gameObject.SetActive(false); Release(child.gameObject); }
            rows.Clear(); revision = settings.Revision;
            font = settings.interfaceFont != null ? settings.interfaceFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!worldScene)
            {
                Box(stage, "Outside tablet", 0, 0, 640, 360, ink);
                PixelLabel(stage, "Closed title", "TOWN TABLET", 160, 135, 320, 28, 20, TextAnchor.MiddleCenter, true);
                PixelLabel(stage, "Closed hint", "Your town, messages and upgrades in one place.", 100, 178, 440, 20, 8, TextAnchor.MiddleCenter, true);
                tabletLauncher = PixelAction(stage, "Open tablet", "OPEN TABLET [TAB]", 220, 215, 200, 28, TogglePhone, true);
            }
            else tabletLauncher = PixelAction(stage, "Open tablet", "TABLET [TAB]", 492, 8, 120, 24, TogglePhone, true);
            welcomeGlow = tabletLauncher.gameObject.AddComponent<Outline>();
            welcomeGlow.effectDistance = new Vector2(1, -1);
            closedUnread = PixelLabel(stage, "Closed unread", "", 310, 35, 302, 20, 8, TextAnchor.MiddleRight, true);
            var shadow = closedUnread.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -1);

            tablet = Box(stage, "Landscape tablet", 0, 0, 640, 360, Color.clear).gameObject;
            tablet.GetComponent<Image>().raycastTarget = true;
            var frame = tablet.transform;
            Artwork(frame, "Town Background Sprite Slot", settings.townBackground, "", 31, 68, 578, 223, settings.backgroundColor, false);
            PixelArtwork(frame, "Pixel main frame", settings.mainFrame, 0, 0);
            PixelSpriteButton(frame, "Close tablet", settings.backButtonArtwork, "BACK", 37, 20, BackFromPixelPage);
            PixelSpriteButton(frame, "Shop button", settings.shopIcon, "UPGRADES", 76, 20, () => ShowPage(1));
            var lettersButton = PixelSpriteButton(frame, "Letters button", settings.envelopeIcon, "MAIL", 181, 20, () => ShowPage(2));
            var dot = PixelArtwork(lettersButton.transform, "Unread dot", settings.unreadDot, 37, -3);
            unreadDotImage = dot != null ? dot.gameObject : null;
            unreadBadge = PixelLabel(frame, "Unread mail", "", 235, 20, 100, 20, 7);
            unreadBadge.gameObject.SetActive(false);
            // BEGIN ADDED: The existing food sprite uses the reference icon's display size.
            Artwork(frame, "Wallet cheese icon", settings.walletIcon, "", 440, 22, 32, 29, Color.white, false);
            // END ADDED
            wallet = PixelLabel(frame, "Wallet", "", 480, 22, 116, 30, 12, TextAnchor.MiddleLeft);

            Vector2 treeSize = settings.cheeseTree != null ? settings.cheeseTree.rect.size / ExportScale : settings.treeSize * .4f;
            treeSize *= Mathf.Min(1, 114 / treeSize.y, 180 / treeSize.x);
            treeSize = new Vector2(Mathf.Round(treeSize.x), Mathf.Round(treeSize.y));
            var treeImage = Artwork(frame, "Cheese Tree Sprite Slot", settings.cheeseTree, "CHEESE\nTREE", 0, 0,
                treeSize.x, treeSize.y, settings.treeColor);
            tree = treeImage.rectTransform; tree.anchorMin = tree.anchorMax = tree.pivot = new Vector2(.5f, .5f);
            tree.anchoredPosition = new Vector2(0, 15);
            // BEGIN ADDED: The selected font needs room for its taller line metrics.
            pixelTreeCaption = PixelLabel(frame, "Tree caption", "CHEESE TREE", 180, 265, 280, 14, 8, TextAnchor.MiddleCenter, true).gameObject;
            // END ADDED
            stock = PixelLabel(frame, "Tree storage", "", 170, 280, 300, 10, 7, TextAnchor.MiddleCenter, true);
            stats = PixelLabel(frame, "Player attributes", "", 38, 308, 150, 28, 6);
            priceLabel = PixelLabel(frame, "Production attributes", "", 204, 308, 246, 28, 6);
            collectButton = PixelSpriteButton(frame, "Collect cheese", settings.collectButtonArtwork, "COLLECT [E]", 462, 306, TryHarvest);
            notice = PixelLabel(frame, "Notification", "", 120, 75, 400, 20, 8, TextAnchor.MiddleCenter, true);
            BuildPixelShop(frame); BuildPixelMail(frame);
            tablet.SetActive(open); ShowPage(page); Refresh();
            BuildPixelEnding(frame);
            if (EndingOpen) ShowEnding();
        }
        // END ADDED

        // BEGIN ADDED: Native rows reuse the existing filters, prices and purchase callbacks.
        void BuildPixelShop(Transform frame)
        {
            shop = Box(frame, "Upgrade shop panel", 31, 69, 578, 222, new Color32(39, 63, 31, 255)).gameObject;
            shop.GetComponent<Image>().raycastTarget = true;
            shopWallet = PixelLabel(shop.transform, "Shop wallet", "", 0, 0, 1, 1);
            shopWallet.gameObject.SetActive(false);
            PixelAction(shop.transform, "All filter", "ALL", 10, 7, 70, 22, () => ChangeFilter(0), filter == 0);
            PixelAction(shop.transform, "Player filter", "PLAYER", 86, 7, 70, 22, () => ChangeFilter(1), filter == 1);
            PixelAction(shop.transform, "Tree filter", "TREE", 162, 7, 70, 22, () => ChangeFilter(2), filter == 2);
            var viewport = Box(shop.transform, "Shop scroll viewport", 8, 35, 558, 181, Color.clear);
            viewport.GetComponent<Image>().raycastTarget = true; viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Upgrade rows", 0, 0, 558, 0);
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 16;
            var track = Box(shop.transform, "Scroll track", 568, 35, 3, 181, new Color(.15f, .22f, .25f));
            var handle = Box(track, "Scroll handle", 0, 0, 3, 30, muted);
            var bar = track.gameObject.AddComponent<Scrollbar>(); bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<Image>();
            // BEGIN ADDED: Stretched scrollbar anchors must not add fixed-size padding.
            handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
            // END ADDED
            handle.GetComponent<Image>().raycastTarget = true; bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar; scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            int index = 0;
            foreach (var option in settings.upgrades)
            {
                if (option == null || !option.available || filter == 1 && option.Target != UpgradeTarget.Player || filter == 2 && option.Target != UpgradeTarget.Tree) continue;
                var item = Box(content, "Upgrade " + option.id, 0, index * 36, 558, 34, new Color32(243, 235, 215, 255));
                // BEGIN ADDED: Small placeholder labels must fit native row dimensions.
                var icon = Artwork(item, "Upgrade Icon Sprite Slot", option.icon, "", 4, 3, 28, 28, new Color(.7f, .6f, .55f));
                if (option.icon == null) PixelLabel(icon.transform, "Placeholder label", "ICON", 0, 0, 28, 28, 6, TextAnchor.MiddleCenter);
                // END ADDED
                // BEGIN ADDED: Reserve one complete EAS VHS line above descriptions.
                PixelLabel(item, "Title", option.title, 38, 1, 164, 13, 8);
                // END ADDED
                PixelLabel(item, "Description", option.description, 38, 14, 164, 19, 6);
                PixelLabel(item, "Target", option.Target == UpgradeTarget.Player ? "PLAYER" : "TREE", 208, 3, 60, 10, 6);
                var row = new Row { option = option };
                row.level = PixelLabel(item, "Level", "", 208, 16, 66, 13, 6);
                row.current = PixelLabel(item, "Current value", "", 280, 3, 152, 12, 6);
                row.next = PixelLabel(item, "Next value", "", 280, 17, 152, 15, 6);
                row.buy = PixelAction(item, "Buy " + option.id, "", 440, 4, 112, 26, () => Buy(option), true);
                if (settings.upgradeButtonArtwork != null)
                {
                    var image = row.buy.GetComponent<Image>(); image.sprite = settings.upgradeButtonArtwork;
                    image.color = Color.white; image.type = Image.Type.Sliced;
                }
                row.cost = row.buy.GetComponentInChildren<Text>(); row.cost.fontSize = 6;
                rows.Add(row); index++;
            }
            content.sizeDelta = new Vector2(558, Mathf.Max(181, index * 36));
            // BEGIN ADDED: Avoid a transient scrollbar when all rows already fit.
            track.gameObject.SetActive(index * 36 > 181);
            // END ADDED
            if (index == 0) PixelLabel(content, "Empty shop", "No upgrades in this category.", 80, 65, 380, 24, 10, TextAnchor.MiddleCenter);
        }
        // END ADDED

        // BEGIN ADDED: Fit retained mailbox and ending actions inside the wooden frame.
        void BuildPixelMail(Transform frame)
        {
            mail = Box(frame, "Mayor mailbox", 45, 77, 550, 206, new Color32(243, 235, 215, 255)).gameObject;
            mail.GetComponent<Image>().raycastTarget = true;
            PixelLabel(mail.transform, "Mail title", "MAYOR'S LETTERS", 12, 8, 420, 20, 14);
            PixelAction(mail.transform, "Mail back", "BACK", 478, 8, 60, 20, () => ShowPage(0));
            PixelLabel(mail.transform, "Sender", "MAYOR ELLIS / TOWN HALL", 12, 30, 510, 13, 8);
            letter = PixelLabel(mail.transform, "Mayor message", "", 12, 48, 526, 99, 9);
            reply = PixelLabel(mail.transform, "Reply text", "", 12, 150, 526, 22, 7);
            previousLetter = PixelAction(mail.transform, "Previous letter", "PREVIOUS", 12, 178, 100, 22, () => SelectLetter(selectedLetter - 1));
            replyButton = PixelAction(mail.transform, "Reply to mayor", "MARK AS READ", 124, 178, 252, 22, ReplyToMayor, true);
            nextLetter = PixelAction(mail.transform, "Next letter", "NEXT", 388, 178, 150, 22, () => SelectLetter(selectedLetter + 1));
        }
        void BuildPixelEnding(Transform frame)
        {
            ending = Box(frame, "Game ending", 0, 0, 640, 360, Color.clear).gameObject;
            ending.GetComponent<Image>().raycastTarget = true;
            var banner = Box(ending.transform, "Ending banner", 31, 235, 578, 105, new Color(.04f, .06f, .08f, .88f));
            PixelLabel(banner, "Game over title", "GAME OVER", 10, 15, 558, 42, 28, TextAnchor.MiddleCenter, true);
            PixelLabel(banner, "Game over caption", "CHEESE TOWN", 10, 66, 558, 20, 12, TextAnchor.MiddleCenter, true);
            ending.SetActive(false);
        }
        // END ADDED
    }
}
// END ADDED
