// BEGIN ADDED: A dedicated text target keeps hover details away from purchase input.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed class UpgradeDescriptionHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IScrollHandler
    {
        public UpgradeRowView row;
        public void OnPointerEnter(PointerEventData data) { if (row != null) row.ShowDescription(data); }
        public void OnPointerExit(PointerEventData data) { if (row != null) row.HideDescription(); }
        public void OnScroll(PointerEventData data)
        {
            // Read unusually long descriptions from their text target; otherwise scroll the list.
            if (row != null && row.ScrollDescription(data)) return;
            GetComponentInParent<ScrollRect>()?.OnScroll(data);
        }
        void OnDisable() { if (row != null) row.HideDescription(); }
    }
}
// END ADDED
