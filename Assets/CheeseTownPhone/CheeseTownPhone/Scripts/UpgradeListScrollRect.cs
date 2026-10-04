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
            if (tooltip != null) tooltip.Hide();
            base.OnScroll(data);
        }
        public override void OnBeginDrag(PointerEventData data)
        {
            if (tooltip != null) tooltip.Hide();
            base.OnBeginDrag(data);
        }
    }
}
// END ADDED
