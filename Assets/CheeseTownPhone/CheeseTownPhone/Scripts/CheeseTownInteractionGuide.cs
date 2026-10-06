using UnityEngine;

namespace CheeseTownPhone
{
    public sealed partial class CheeseTownDemo
    {
        // BEGIN CHANGED: A click-through spotlight tour introduces controls without requiring gameplay actions.
        TutorialCoachView interactionGuide;
        int displayedGuideStep = -1;
        public bool InteractionGuideVisible => interactionGuide != null && interactionGuide.gameObject.activeInHierarchy;
        public bool IsPointerOverInteractionGuide(Vector2 screenPoint) => InteractionGuideVisible;

        void BuildInteractionGuide()
        {
            if (!worldScene || interactionGuide != null || settings.tutorialCoachPrefab == null) return;
            interactionGuide = Instantiate(settings.tutorialCoachPrefab, ScreenCanvas.transform);
            interactionGuide.name = "Interaction guide";
            var overlay = (RectTransform)interactionGuide.transform;
            overlay.anchorMin = Vector2.zero; overlay.anchorMax = Vector2.one;
            overlay.offsetMin = overlay.offsetMax = Vector2.zero;
            interactionGuide.Bind(settings.upgradePixelFont != null ? settings.upgradePixelFont : font,
                AdvanceInteractionGuide, DismissInteractionGuide);
            interactionGuide.gameObject.SetActive(false);
        }
        void AdvanceInteractionGuide()
        {
            if (!InteractionGuideVisible || tabletView != null && tabletView.IsTransitioning) return;
            Progress.AdvanceInteractionGuide();
            if (Progress.InteractionGuideStep >= TownProgress.InteractionGuideCount) DismissInteractionGuide();
            else UpdateInteractionGuide();
        }
        void DismissInteractionGuide()
        {
            Progress.SkipInteractionGuide();
            if (interactionGuide != null) interactionGuide.gameObject.SetActive(false);
            if (tabletView != null) tabletView.SetOpen(false);
            else if (tablet != null) tablet.SetActive(false);
            Refresh();
        }
        void UpdateInteractionGuide()
        {
            BuildInteractionGuide();
            if (interactionGuide == null) return;
            int step = Progress.InteractionGuideStep;
            bool visible = Progress.TotalCollected > 0 && !EndingOpen && !TownSession.Instance.PrologueActive && step < TownProgress.InteractionGuideCount;
            interactionGuide.gameObject.SetActive(visible);
            if (!visible) return;
            interactionGuide.transform.SetAsLastSibling();
            if (displayedGuideStep != step)
            {
                displayedGuideStep = step;
                // Page previews never invoke Read, Buy or Collect. A tour click only advances the introduction.
                if (tabletView != null)
                {
                    if (step == 0) tabletView.SetOpen(false);
                    else
                    {
                        if (!tabletView.IsOpen) tabletView.SetOpen(true);
                        ShowPage(step <= 3 ? 2 : step <= 5 ? 1 : 0);
                    }
                }
            }
            RectTransform[] targets = null;
            if (tabletView != null)
            {
                switch (step)
                {
                    case 0: targets = new[] { (RectTransform)tabletView.launcher.transform }; break;
                    case 1: targets = new[] { tabletView.letter.rectTransform }; break;
                    case 2: targets = new[] { (RectTransform)tabletView.previous.transform, (RectTransform)tabletView.read.transform, (RectTransform)tabletView.next.transform }; break;
                    case 3: targets = new[] { (RectTransform)tabletView.shopButton.transform }; break;
                    case 4: targets = new[] { (RectTransform)tabletView.upgrades.categories[0].transform, tabletView.upgrades.scroll.viewport }; break;
                    case 5:
                        if (tabletView.upgrades.Rows.Count > 0) targets = new[] { (RectTransform)tabletView.upgrades.Rows[0].buy.transform };
                        // BEGIN ADDED: Highlight main-tree startup first without purchasing on the player's behalf.
                        if (Progress.UsesBatchProduction)
                            foreach (var row in tabletView.upgrades.Rows)
                                if (row.Option.effect == UpgradeEffect.TownTreeStart) { targets = new[] { (RectTransform)row.buy.transform }; break; }
                        // END ADDED
                        break;
                    case 6: targets = new[] { (RectTransform)tabletView.collect.transform }; break;
                    case 7: targets = new[] { (RectTransform)tabletView.back.transform }; break;
                }
            }
            interactionGuide.Present(step, tabletView == null || !tabletView.IsTransitioning, null, targets);
        }
        // END CHANGED
    }
}
