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
        // BEGIN ADDED: The shop has its own full-height frame and background.
        Image pixelFrame;
        GameObject pixelShopBackground;
        // END ADDED
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
            {
                // Keep the supplied 2x artwork at its authored size in builds.
                // Smaller windows may scale down, but larger displays must not enlarge the UI again.
                float fit = Mathf.Min(Screen.width / PixelLayoutSize.x, Screen.height / PixelLayoutSize.y);
                ScreenCanvas.GetComponent<CanvasScaler>().scaleFactor = Mathf.Max(.01f, Mathf.Min(ExportScale, fit));
            }
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
            CloseTablet();
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
            // BEGIN ADDED: Match the reference by hiding the home footer on the shop page.
            if (pixelFrame != null) pixelFrame.sprite = page == 1 && settings.upgradeFrame != null ? settings.upgradeFrame : settings.mainFrame;
            if (pixelShopBackground != null) pixelShopBackground.SetActive(page == 1);
            stats.gameObject.SetActive(page != 1);
            priceLabel.gameObject.SetActive(page != 1);
            collectButton.gameObject.SetActive(page != 1);
            // BEGIN ADDED: Purchase feedback belongs in the clear space beside category buttons.
            notice.rectTransform.anchoredPosition = page == 1 ? new Vector2(302, -78) : new Vector2(120, -75);
            notice.rectTransform.sizeDelta = page == 1 ? new Vector2(297, 22) : new Vector2(400, 20);
            notice.alignment = page == 1 ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
            // END ADDED
            // END ADDED
        }
        // END ADDED
        // END ADDED

        // BEGIN ADDED: Place artwork against UI_Main_ref while keeping live readouts.
        void RenderPixelSkin(bool open)
        {
            foreach (Transform child in stage) { child.gameObject.SetActive(false); Release(child.gameObject); }
            rows.Clear(); revision = settings.Revision;
            // BEGIN ADDED: Discard row bindings when filters or settings rebuild the UI.
            pixelUpgradeRows.Clear();
            // END ADDED
            font = settings.interfaceFont != null ? settings.interfaceFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (!worldScene)
            {
                Box(stage, "Outside tablet", 0, 0, 640, 360, ink);
                PixelLabel(stage, "Closed title", "TOWN TABLET", 160, 135, 320, 28, 20, TextAnchor.MiddleCenter, true);
                PixelLabel(stage, "Closed hint", "Your town, messages and upgrades in one place.", 100, 178, 440, 20, 8, TextAnchor.MiddleCenter, true);
                tabletLauncher = PixelAction(stage, "Open tablet", "OPEN TABLET [TAB]", 220, 215, 200, 28, TogglePhone, true);
            }
            // BEGIN CHANGED: The wider font needs room for the welcome-letter launcher.
            else tabletLauncher = PixelAction(stage, "Open tablet", "TABLET [TAB]", 442, 8, 170, 24, TogglePhone, true);
            // END CHANGED
            welcomeGlow = tabletLauncher.gameObject.AddComponent<Outline>();
            welcomeGlow.effectDistance = new Vector2(1, -1);
            closedUnread = PixelLabel(stage, "Closed unread", "", 310, 35, 302, 20, 8, TextAnchor.MiddleRight, true);
            var shadow = closedUnread.gameObject.AddComponent<Shadow>();
            shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -1);

            tablet = Box(stage, "Landscape tablet", 0, 0, 640, 360, Color.clear).gameObject;
            tablet.GetComponent<Image>().raycastTarget = true;
            var frame = tablet.transform;
            // BEGIN CHANGED: Cover the transparent inner edge and the space revealed by the moving footer.
            Box(frame, "Interior backing", 29, 67, 582, 274, settings.backgroundColor);
            Artwork(frame, "Town Background Sprite Slot", settings.townBackground, "", 30, 68, 580, 223, settings.backgroundColor, false);
            // END CHANGED
            // BEGIN CHANGED: Keep the shop background below the shared frame and header controls.
            var shopBackdrop = PixelArtwork(frame, "Pixel shop background", settings.upgradeBackground, 0, 0);
            pixelShopBackground = shopBackdrop != null ? shopBackdrop.gameObject : null;
            pixelFrame = PixelArtwork(frame, "Pixel main frame", settings.mainFrame, 0, 0);
            // END CHANGED
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
            // BEGIN CHANGED: Ten wallet digits must fit the replacement font.
            wallet = PixelLabel(frame, "Wallet", "", 480, 22, 122, 30, 12, TextAnchor.MiddleLeft);
            // END CHANGED

            Vector2 treeSize = settings.cheeseTree != null ? settings.cheeseTree.rect.size / ExportScale : settings.treeSize * .4f;
            treeSize *= Mathf.Min(1, 114 / treeSize.y, 180 / treeSize.x);
            treeSize = new Vector2(Mathf.Round(treeSize.x), Mathf.Round(treeSize.y));
            var treeImage = Artwork(frame, "Cheese Tree Sprite Slot", settings.cheeseTree, "CHEESE\nTREE", 0, 0,
                treeSize.x, treeSize.y, settings.treeColor);
            tree = treeImage.rectTransform; tree.anchorMin = tree.anchorMax = tree.pivot = new Vector2(.5f, .5f);
            tree.anchoredPosition = new Vector2(0, 15);
            // BEGIN ADDED: Reserve a complete line for the selected font.
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

        // BEGIN CHANGED: The supplied upgrade layout lives in CheeseTownUpgradeUI.
        // END CHANGED

        // BEGIN ADDED: Fit retained mailbox and ending actions inside the wooden frame.
        void BuildPixelMail(Transform frame)
        {
            mail = Box(frame, "Mayor mailbox", 45, 77, 550, 206, new Color32(243, 235, 215, 255)).gameObject;
            mail.GetComponent<Image>().raycastTarget = true;
            PixelLabel(mail.transform, "Mail title", "MAYOR'S LETTERS", 12, 8, 420, 20, 14);
            PixelAction(mail.transform, "Mail back", "BACK", 478, 8, 60, 20, () => ShowPage(0));
            PixelLabel(mail.transform, "Sender", "MAYOR ELLIS / TOWN HALL", 12, 30, 510, 13, 8);
            // BEGIN CHANGED: Eight-pixel body text fits letters with the wider new font.
            letter = PixelLabel(mail.transform, "Mayor message", "", 12, 48, 526, 99, 8);
            // END CHANGED
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
            PixelLabel(banner, "Continue question", "You can keep gathering cheese.\nThough no mouse needs it anymore.\nKeep going?", 10, 10, 558, 45, 10, TextAnchor.MiddleCenter, true);
            PixelAction(banner, "Continue gathering", "YES", 170, 65, 96, 28, ContinueAfterEnding, true);
            PixelAction(banner, "Leave Mousetown", "NO", 312, 65, 96, 28, QuitAfterEnding);
            ending.SetActive(false);
        }
        // END ADDED
    }
}
// END ADDED
