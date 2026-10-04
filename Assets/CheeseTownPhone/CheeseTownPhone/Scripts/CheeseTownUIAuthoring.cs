// BEGIN ADDED: One-time migration bakes the existing layout; runtime uses serialized prefab references.
#if UNITY_EDITOR
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed partial class CheeseTownDemo
    {
        public TabletView CreateEditableTablet(TabletSettings configuration)
        {
            settings = configuration; Progress = new TownProgress(settings); worldScene = true;
            var canvas = new GameObject("Temporary authoring canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            canvas.transform.SetParent(transform, false); ScreenCanvas = canvas.GetComponent<Canvas>();
            stage = Rect(canvas.transform, "TabletView", 0, 0, 640, 360);
            stage.anchorMin = stage.anchorMax = stage.pivot = new Vector2(.5f, .5f);
            RenderPixelSkin(true);
            var view = stage.gameObject.AddComponent<TabletView>();
            view.panel = (RectTransform)tablet.transform; view.panelGroup = tablet.AddComponent<CanvasGroup>();
            view.launcher = tabletLauncher; view.welcomeGlow = welcomeGlow;
            view.back = Child<UnityEngine.UI.Button>(tablet.transform, "Close tablet");
            view.shopButton = Child<UnityEngine.UI.Button>(tablet.transform, "Shop button");
            view.mailButton = Child<UnityEngine.UI.Button>(tablet.transform, "Letters button");
            view.mailBack = Child<UnityEngine.UI.Button>(mail.transform, "Mail back");
            view.collect = collectButton; view.previous = previousLetter; view.next = nextLetter; view.read = replyButton;
            view.wallet = wallet; view.stock = stock; view.stats = stats; view.production = priceLabel; view.notice = notice;
            view.letter = letter; view.reply = reply; view.unread = unreadBadge; view.closedUnread = closedUnread; view.shopWallet = shopWallet;
            view.tree = tree; view.treeCaption = pixelTreeCaption; view.unreadDot = unreadDotImage; view.ending = ending;
            view.background = Child<Image>(tablet.transform, "Town Background Sprite Slot").gameObject;
            view.homeGroup = Group(tablet.transform, "Town page");
            Move(tree, view.homeGroup.transform); Move(stock.transform, view.homeGroup.transform); Move(pixelTreeCaption.transform, view.homeGroup.transform);
            view.shopGroup = shop.AddComponent<CanvasGroup>(); view.mailGroup = mail.AddComponent<CanvasGroup>();
            if (pixelShopBackground != null)
            {
                Move(pixelShopBackground.transform, shop.transform); pixelShopBackground.transform.SetAsFirstSibling();
                // The page now owns visibility, so its background must not retain the old hidden state.
                pixelShopBackground.SetActive(true);
            }
            var header = Group(tablet.transform, "Header controls"); view.header = header.gameObject;
            foreach (var item in new Transform[] { view.back.transform, view.shopButton.transform, view.mailButton.transform, wallet.transform,
                unreadBadge.transform, Child<Image>(tablet.transform, "Wallet cheese icon").transform }) Move(item, header.transform);

            // Crop the supplied frame by UV, leaving original PNGs untouched and borders at native scale.
            var fixedFrame = Rect(tablet.transform, "Wood frame", 0, 0, 640, 360); view.frame = fixedFrame.gameObject;
            Texture texture = settings.upgradeFrame != null ? settings.upgradeFrame.texture : settings.mainFrame.texture;
            Slice(fixedFrame, "Header wood", texture, 0, 0, 1280, 136);
            Slice(fixedFrame, "Left wood", texture, 0, 136, 62, 544);
            Slice(fixedFrame, "Right wood", texture, 1218, 136, 62, 544);
            Slice(fixedFrame, "Bottom wood", texture, 0, 680, 1280, 40);
            // BEGIN CHANGED: Reach source columns 60..1219, including the bevel beside each upright.
            var clip = Rect(tablet.transform, "Footer clip", 30, 68, 580, 272);
            clip.gameObject.AddComponent<RectMask2D>(); view.footerGroup = clip.gameObject.AddComponent<CanvasGroup>();
            view.footerMotion = Rect(clip, "Footer motion", -30, -68, 640, 360);
            view.footerHomePosition = view.footerMotion.anchoredPosition;
            Slice(view.footerMotion, "Sliding footer wood", settings.mainFrame.texture, 60, 580, 1160, 100);
            // END CHANGED
            Move(stats.transform, view.footerMotion); Move(priceLabel.transform, view.footerMotion); Move(collectButton.transform, view.footerMotion);
            Release(pixelFrame.gameObject); pixelFrame = null;

            view.upgrades = shop.AddComponent<UpgradeShopView>(); var list = view.upgrades;
            list.scroll = shop.GetComponentInChildren<UpgradeListScrollRect>(true); list.content = list.scroll.content; list.tooltip = list.scroll.tooltip;
            list.categories = new[] { Child<Button>(shop.transform, "All filter"), Child<Button>(shop.transform, "Player filter"), Child<Button>(shop.transform, "Tree filter") };
            list.selected = new[] { settings.allSelected, settings.playerSelected, settings.treeSelected };
            list.unselected = new[] { settings.allUnselected, settings.playerUnselected, settings.treeUnselected };
            var viewport = list.scroll.viewport;
            var placement = Rect(shop.transform, "List placement", viewport.anchoredPosition.x, -viewport.anchoredPosition.y, viewport.sizeDelta.x, viewport.sizeDelta.y);
            Move(viewport, placement); viewport.anchorMin = Vector2.zero; viewport.anchorMax = Vector2.one;
            viewport.offsetMin = viewport.offsetMax = Vector2.zero; list.revealMask = viewport;
            for (int i = list.content.childCount - 1; i >= 0; i--) Release(list.content.GetChild(i).gameObject);
            list.content.sizeDelta = new Vector2(564, 221);
            list.empty = PixelLabel(list.content, "Empty shop", "No upgrades in this category.", 60, 65, 444, 28, 8, TextAnchor.MiddleCenter);
            list.empty.gameObject.SetActive(false);
            if (settings.upgradeRowPrefab != null)
            {
                list.preview = ((GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(settings.upgradeRowPrefab.gameObject, list.content)).GetComponent<UpgradeRowView>();
                list.preview.name = "UpgradeRow - authoring preview";
                ((RectTransform)list.preview.transform).anchoredPosition = Vector2.zero;
            }
            view.background.transform.SetAsFirstSibling(); view.homeGroup.transform.SetSiblingIndex(1);
            shop.transform.SetSiblingIndex(2); mail.transform.SetSiblingIndex(3);
            clip.SetSiblingIndex(4); fixedFrame.SetSiblingIndex(5); header.transform.SetSiblingIndex(6);
            // BEGIN ADDED: The opaque interior stays beneath both pages throughout their crossfade.
            Child<Image>(tablet.transform, "Interior backing").transform.SetAsFirstSibling();
            // END ADDED
            notice.transform.SetAsLastSibling(); ending.transform.SetAsLastSibling();
            view.townNoticePlacement = Rect(tablet.transform, "Town notification placement", 120, 75, 400, 20);
            view.shopNoticePlacement = Rect(tablet.transform, "Shop notification placement", 302, 78, 297, 22);
            view.SetPage(0, false); view.SetOpen(true, false);
            wallet.text = "500"; notice.text = "";
            return view;
        }
        static T Child<T>(Transform root, string name) where T : Component
            => root.GetComponentsInChildren<T>(true).Single(item => item.name == name);
        CanvasGroup Group(Transform parent, string name) => Rect(parent, name, 0, 0, 640, 360).gameObject.AddComponent<CanvasGroup>();
        static void Move(Transform child, Transform parent) { child.SetParent(parent, true); }
        void Slice(Transform parent, string name, Texture texture, int x, int y, int width, int height)
        {
            var image = Rect(parent, name, x / 2f, y / 2f, width / 2f, height / 2f).gameObject.AddComponent<RawImage>();
            image.texture = texture; image.raycastTarget = false;
            image.uvRect = new Rect(x / (float)texture.width, 1 - (y + height) / (float)texture.height,
                width / (float)texture.width, height / (float)texture.height);
        }
    }
}
#endif
// END ADDED
