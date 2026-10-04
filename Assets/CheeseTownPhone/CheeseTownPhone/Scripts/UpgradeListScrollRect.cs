// BEGIN ADDED: List motion dismisses descriptions even at the scroll boundary.
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed class UpgradeListScrollRect : ScrollRect
    {
        public UpgradeTooltip tooltip;
        public override void OnScroll(PointerEventData data)
        {
            // BEGIN ADDED: Manual scrolling settles card offsets before moving their fixed slots.
            GetComponentInParent<UpgradeShopView>()?.FinishEntrance();
            // END ADDED
            if (tooltip != null) tooltip.Hide();
            base.OnScroll(data);
        }
        public override void OnBeginDrag(PointerEventData data)
        {
            // BEGIN ADDED: Dragging also cancels entrance motion at a scroll boundary.
            GetComponentInParent<UpgradeShopView>()?.FinishEntrance();
            // END ADDED
            if (tooltip != null) tooltip.Hide();
            base.OnBeginDrag(data);
        }
    }
}
// END ADDED
