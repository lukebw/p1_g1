// BEGIN ADDED: Editable uGUI references and reversible motion keep layout out of game rules.
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    [DisallowMultipleComponent]
    public sealed class TabletView : MonoBehaviour
    {
        [Header("Motion - seconds and logical pixels")]
        [Min(.01f)] public float openDuration = .24f;
        [Min(.01f)] public float pageDuration = .30f;
        [Min(0)] public float openSlide = 12;
        [Min(0)] public float footerTravel = 50;
        [Header("World launcher - screen edge placement")]
        public bool anchorLauncherToScreen = true;
        public Vector2 launcherScreenInset = new Vector2(20, 20);
        [Min(0)] public float unreadGap = 5;
        [Header("Layout - edit child RectTransforms in Prefab Mode")]
        public RectTransform panel, footerMotion;
        public RectTransform townNoticePlacement, shopNoticePlacement;
        public Vector2 footerHomePosition;
        public CanvasGroup panelGroup, homeGroup, shopGroup, mailGroup, footerGroup;
        public GameObject frame, header, background, ending;
        public UpgradeShopView upgrades;
        [Header("Controller bindings")]
        public Button launcher, back, shopButton, mailButton, collect, mailBack, previous, next, read;
        public Outline welcomeGlow;
        public Text wallet, stock, stats, production, notice, letter, reply, unread, closedUnread, shopWallet;
        public RectTransform tree;
        public GameObject treeCaption, unreadDot;
        // BEGIN ADDED: Separate motion wrappers preserve editable footer and HUD placement.
        [Header("Mail page and gameplay HUD")]
        public CanvasGroup townFooter, mailFooter;
        [Min(0)] public float footerTurnDistance = 40;
        [Min(0)] public float mailSlide = 16;
        public RectTransform worldHud;
        public Text hudWallet;
        public GameObject hudUnreadDot;
        float footerMailAmount;
        public void PositionHud(RectTransform canvas)
        {
            if (worldHud == null || !anchorLauncherToScreen) return;
            worldHud.anchorMin = worldHud.anchorMax = new Vector2(.5f, .5f);
            worldHud.anchoredPosition = new Vector2(Mathf.Round(-canvas.rect.width / 2 + launcherScreenInset.x),
                Mathf.Round(canvas.rect.height / 2 - launcherScreenInset.y));
        }
        // END ADDED

        public bool IsOpen { get; private set; }
        public bool IsVisible => panel.gameObject.activeSelf;
        public bool IsTransitioning => openAmount != (IsOpen ? 1 : 0) || PageMoving;
        public int Page { get; private set; }
        float openAmount, expansion, homeAmount = 1, shopAmount, mailAmount;
        bool ended;
        // MoveTowards reaches exact endpoints; tolerances could leave input locked just before arrival.
        bool PageMoving => expansion != (Page == 1 ? 1 : 0)
            || homeAmount != (Page == 0 ? 1 : 0)
            || shopAmount != (Page == 1 ? 1 : 0)
            || mailAmount != (Page == 2 ? 1 : 0) || footerMailAmount != (Page == 2 ? 1 : 0);

        public void SetOpen(bool value, bool animate = true)
        {
            if (ended) return;
            IsOpen = value; upgrades.tooltip.Hide();
            if (value) panel.gameObject.SetActive(true);
            if (!animate) openAmount = value ? 1 : 0;
            if (!value) upgrades.FinishEntrance();
            ApplyMotion();
        }
        public void SetPage(int value, bool animate = true)
        {
            if (ended) return;
            bool changed = Page != value;
            Page = Mathf.Clamp(value, 0, 2); upgrades.tooltip.Hide();
            notice.transform.SetParent(Page == 1 ? shopNoticePlacement : townNoticePlacement, false);
            notice.rectTransform.anchorMin = Vector2.zero; notice.rectTransform.anchorMax = Vector2.one;
            notice.rectTransform.offsetMin = notice.rectTransform.offsetMax = Vector2.zero;
            notice.alignment = Page == 1 ? TextAnchor.MiddleRight : TextAnchor.MiddleCenter;
            if (!animate) SnapPage();
            ApplyMotion();
            if (changed && Page == 1 && IsOpen && animate) { shopGroup.gameObject.SetActive(true); upgrades.PlayEntrance(); }
            else if (Page != 1) upgrades.FinishEntrance();
        }
        void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float delta)
        {
            if (ended || !IsTransitioning) return;
            openAmount = Mathf.MoveTowards(openAmount, IsOpen ? 1 : 0, delta / Mathf.Max(.01f, openDuration));
            float step = delta / Mathf.Max(.01f, pageDuration);
            expansion = Mathf.MoveTowards(expansion, Page == 1 ? 1 : 0, step);
            homeAmount = Mathf.MoveTowards(homeAmount, Page == 0 ? 1 : 0, step);
            shopAmount = Mathf.MoveTowards(shopAmount, Page == 1 ? 1 : 0, step);
            mailAmount = Mathf.MoveTowards(mailAmount, Page == 2 ? 1 : 0, step);
            footerMailAmount = Mathf.MoveTowards(footerMailAmount, Page == 2 ? 1 : 0, step);
            ApplyMotion();
        }
        void SnapPage()
        {
            expansion = shopAmount = Page == 1 ? 1 : 0;
            homeAmount = Page == 0 ? 1 : 0; mailAmount = Page == 2 ? 1 : 0;
            footerMailAmount = mailAmount;
        }
        public void CompleteTransitions()
        {
            if (ended) return;
            openAmount = IsOpen ? 1 : 0; SnapPage(); ApplyMotion(); upgrades.FinishEntrance();
        }
        static float Ease(float value) => value * value * (3 - 2 * value);
        static void PageVisibility(CanvasGroup group, float amount, bool input)
        {
            group.gameObject.SetActive(amount > 0);
            group.alpha = Ease(amount); group.interactable = input; group.blocksRaycasts = input;
        }
        void ApplyMotion()
        {
            panel.gameObject.SetActive(IsOpen || openAmount > 0);
            panel.anchoredPosition = new Vector2(0, -Mathf.Round(openSlide * (1 - Ease(openAmount))));
            panelGroup.alpha = Ease(openAmount);
            // The panel keeps intercepting clicks until the close animation has finished.
            panelGroup.blocksRaycasts = IsVisible; panelGroup.interactable = IsOpen && openAmount == 1;
            bool ready = IsOpen && openAmount == 1 && !PageMoving;
            notice.gameObject.SetActive(Page != 2);
            PageVisibility(homeGroup, homeAmount, ready && Page == 0);
            PageVisibility(shopGroup, shopAmount, ready && Page == 1);
            PageVisibility(mailGroup, mailAmount, ready && Page == 2);
            footerGroup.gameObject.SetActive(expansion < 1);
            footerGroup.interactable = ready && Page != 1;
            footerGroup.blocksRaycasts = footerGroup.interactable;
            footerMotion.anchoredPosition = footerHomePosition + new Vector2(0, -Mathf.Round(footerTravel * Ease(expansion)));
            upgrades.revealMask.offsetMin = new Vector2(0, Mathf.Round(footerTravel * (1 - Ease(expansion))));
            // BEGIN ADDED: Only one footer face exists visually at a time, including rapid reversals.
            if (townFooter != null && mailFooter != null)
            {
                float town = Mathf.Clamp01(1 - footerMailAmount * 2);
                float mail = Mathf.Clamp01(footerMailAmount * 2 - 1);
                PageVisibility(townFooter, town, ready && Page == 0);
                PageVisibility(mailFooter, mail, ready && Page == 2);
                ((RectTransform)townFooter.transform).anchoredPosition = new Vector2(0, -Mathf.Round(footerTurnDistance * (1 - Ease(town))));
                ((RectTransform)mailFooter.transform).anchoredPosition = new Vector2(0, Mathf.Round(footerTurnDistance * (1 - Ease(mail))));
                ((RectTransform)mailGroup.transform).anchoredPosition = new Vector2(Mathf.Round(mailSlide * (1 - Ease(mailAmount))), 0);
            }
            if (worldHud != null) worldHud.gameObject.SetActive(!IsVisible && !ended);
            // END ADDED
        }
        public void ShowEnding()
        {
            CompleteTransitions(); ended = true; IsOpen = true;
            panel.gameObject.SetActive(true); panel.anchoredPosition = Vector2.zero; panelGroup.alpha = 1;
            panelGroup.blocksRaycasts = true; panelGroup.interactable = false;
            homeGroup.gameObject.SetActive(false); shopGroup.gameObject.SetActive(false); mailGroup.gameObject.SetActive(false);
            footerGroup.gameObject.SetActive(false); frame.SetActive(false); header.SetActive(false);
            notice.gameObject.SetActive(false); launcher.gameObject.SetActive(false); closedUnread.gameObject.SetActive(false);
            if (worldHud != null) worldHud.gameObject.SetActive(false);
            background.SetActive(true); ending.SetActive(true); upgrades.tooltip.Hide();
        }
    }
}
// END ADDED
