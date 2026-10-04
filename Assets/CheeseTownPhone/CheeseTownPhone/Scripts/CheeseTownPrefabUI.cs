// BEGIN ADDED: Connect editable views to the same session, upgrade IDs and controller actions.
using UnityEngine;
using UnityEngine.EventSystems;

namespace CheeseTownPhone
{
    public sealed partial class CheeseTownDemo
    {
        TabletView tabletView;
        public TabletView View => tabletView;
        public bool BlocksWorldInput => EndingOpen || (tabletView != null ? tabletView.IsVisible : PhoneOpen);
        public void CompleteUITransitions() { if (tabletView != null) tabletView.CompleteTransitions(); }

        void RenderPrefab(bool open)
        {
            if (tabletView == null)
            {
                foreach (Transform child in stage) { child.gameObject.SetActive(false); Release(child.gameObject); }
                tabletView = Instantiate(settings.tabletPrefab, stage);
                tabletView.name = "Tablet View";
                var layout = (RectTransform)tabletView.transform;
                layout.anchorMin = layout.anchorMax = layout.pivot = new Vector2(.5f, .5f);
                layout.anchoredPosition = Vector2.zero;
                tablet = tabletView.panel.gameObject; shop = tabletView.shopGroup.gameObject; mail = tabletView.mailGroup.gameObject;
                ending = tabletView.ending; tree = tabletView.tree;
                tabletLauncher = tabletView.launcher; welcomeGlow = tabletView.welcomeGlow;
                wallet = tabletView.wallet; stock = tabletView.stock; stats = tabletView.stats; priceLabel = tabletView.production;
                notice = tabletView.notice; letter = tabletView.letter; reply = tabletView.reply;
                unreadBadge = tabletView.unread; closedUnread = tabletView.closedUnread; shopWallet = tabletView.shopWallet;
                collectButton = tabletView.collect; replyButton = tabletView.read;
                previousLetter = tabletView.previous; nextLetter = tabletView.next;
                unreadDotImage = tabletView.unreadDot; pixelTreeCaption = tabletView.treeCaption;
                tabletView.launcher.onClick.AddListener(TogglePhone); tabletView.back.onClick.AddListener(BackFromPixelPage);
                tabletView.shopButton.onClick.AddListener(() => ShowPage(1)); tabletView.mailButton.onClick.AddListener(() => ShowPage(2));
                tabletView.collect.onClick.AddListener(TryHarvest); tabletView.mailBack.onClick.AddListener(() => ShowPage(0));
                tabletView.previous.onClick.AddListener(() => SelectLetter(selectedLetter - 1));
                tabletView.next.onClick.AddListener(() => SelectLetter(selectedLetter + 1)); tabletView.read.onClick.AddListener(ReplyToMayor);
                tabletView.upgrades.Bind(ChangeFilter);
                foreach (var text in tabletView.GetComponentsInChildren<UnityEngine.UI.Text>(true)) UpgradeRowView.Sharpen(text.font);
            }
            revision = settings.Revision;
            tabletView.SetPage(page, false); tabletView.SetOpen(open, false);
            RebuildPrefabRows(false); Refresh();
            if (EndingOpen) ShowEnding();
        }
        void RebuildPrefabRows(bool animate)
        {
            rows.Clear(); pixelUpgradeRows.Clear();
            tabletView.upgrades.Rebuild(settings, filter, Buy, animate);
            foreach (var view in tabletView.upgrades.Rows)
            {
                var row = new Row { option = view.Option };
                rows.Add(row); pixelUpgradeRows.Add(row, view);
            }
            Refresh();
        }
        void TogglePrefab()
        {
            bool open = !tabletView.IsOpen;
            if (open)
            {
                if (WelcomeUnread) selectedLetter = 0;
                page = WelcomeUnread ? 2 : 0;
                tabletView.SetPage(page, false);
            }
            tabletView.SetOpen(open); EventSystem.current?.SetSelectedGameObject(null); Refresh();
        }
    }
}
// END ADDED
