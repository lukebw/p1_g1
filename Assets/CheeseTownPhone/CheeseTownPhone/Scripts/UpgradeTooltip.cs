// BEGIN ADDED: Full descriptions stay readable outside the shop's row mask.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // BEGIN CHANGED: Details are a passive overlay that never intercepts purchase clicks.
    public sealed class UpgradeTooltip : MonoBehaviour
    // END CHANGED
    {
        RectTransform stage, panel, viewport, content;
        CanvasGroup visibility;
        Text body;
        ScrollRect scroll;
        UpgradeRowView owner;
        float showAt;
        Vector2 pointer;
        public bool IsVisible => gameObject.activeInHierarchy && visibility.alpha > 0;
        public string FullText => body.text;

        // BEGIN ADDED: Create one shared overlay after the scroll list, with bounded long-text scrolling.
        public static UpgradeTooltip Create(RectTransform parent, Font font, Sprite background)
        {
            var panel = MakeRect(parent, "Upgrade description tooltip", 0, 0, 300, 100);
            var image = panel.gameObject.AddComponent<Image>();
            image.sprite = background; image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 2; image.color = Color.white;
            // BEGIN CHANGED: Every graphic stays click-through, even while the panel is visible.
            image.raycastTarget = false;
            // END CHANGED
            var tooltip = panel.gameObject.AddComponent<UpgradeTooltip>();
            tooltip.panel = panel; tooltip.stage = parent;
            tooltip.visibility = panel.gameObject.AddComponent<CanvasGroup>();
            // BEGIN CHANGED: A single effect paragraph replaces the repeated name and statistics.
            tooltip.visibility.interactable = false;
            tooltip.visibility.blocksRaycasts = false;
            tooltip.viewport = MakeRect(panel, "Tooltip viewport", 10, 10, 280, 50);
            var hitArea = tooltip.viewport.gameObject.AddComponent<Image>(); hitArea.color = Color.clear;
            hitArea.raycastTarget = false;
            // END CHANGED
            tooltip.viewport.gameObject.AddComponent<RectMask2D>();
            tooltip.content = MakeRect(tooltip.viewport, "Tooltip body content", 0, 0, 280, 50);
            tooltip.body = MakeText(tooltip.content, "Full effect description", font, 0, 0, 280, 50);
            tooltip.scroll = tooltip.viewport.gameObject.AddComponent<ScrollRect>();
            tooltip.scroll.viewport = tooltip.viewport; tooltip.scroll.content = tooltip.content;
            tooltip.scroll.horizontal = false; tooltip.scroll.inertia = false;
            tooltip.scroll.movementType = ScrollRect.MovementType.Clamped; tooltip.scroll.scrollSensitivity = 16;
            tooltip.scroll.onValueChanged.AddListener(_ => tooltip.content.anchoredPosition =
                new Vector2(0, Mathf.Round(tooltip.content.anchoredPosition.y)));
            Sharpen(font); tooltip.gameObject.SetActive(false);
            return tooltip;
        }
        static void Sharpen(Font font) { UpgradeRowView.Sharpen(font); }
        static RectTransform MakeRect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        static Text MakeText(Transform parent, string name, Font font, float x, float y, float width, float height)
        {
            var text = MakeRect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = 8; text.fontStyle = FontStyle.Normal;
            text.color = new Color32(102, 51, 34, 255); text.raycastTarget = false;
            text.supportRichText = false; text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
        // END ADDED

        // BEGIN ADDED: Use unscaled time so hover descriptions work while gameplay is paused.
        // BEGIN CHANGED: The description itself is the only tooltip content and hover owner.
        public void Request(UpgradeRowView source, string description, Vector2 screenPoint, Camera camera, float delay)
        {
            owner = source;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(stage, screenPoint, camera, out pointer);
            pointer -= new Vector2(stage.rect.xMin, stage.rect.yMax);
            body.text = description;
            showAt = Time.unscaledTime + delay;
            visibility.alpha = 0; visibility.blocksRaycasts = false;
            gameObject.SetActive(true); transform.SetAsLastSibling();
        }
        // END CHANGED
        public void Hide(UpgradeRowView source = null)
        {
            if (source != null && owner != source) return;
            owner = null;
            visibility.alpha = 0; visibility.blocksRaycasts = false; gameObject.SetActive(false);
        }
        // BEGIN CHANGED: Long text scrolls through its description target, leaving the overlay passive.
        public bool ScrollDescription(UpgradeRowView source, PointerEventData data)
        {
            if (owner != source || !IsVisible || content.rect.height <= viewport.rect.height) return false;
            scroll.OnScroll(data); return true;
        }
        // END CHANGED
        void Update() { Tick(Time.unscaledTime); }
        public void Tick(float now)
        {
            if (owner == null || !owner.gameObject.activeInHierarchy) { Hide(); return; }
            // BEGIN CHANGED: Leaving the text cancels immediately through its hover component.
            if (!IsVisible && now >= showAt) Show();
            // END CHANGED
        }
        // BEGIN CHANGED: Reopening the tablet must not flash a stale description for one frame.
        void OnDisable()
        {
            owner = null;
            if (visibility != null) { visibility.alpha = 0; visibility.blocksRaycasts = false; }
        }
        // END CHANGED
        // END ADDED

        // BEGIN ADDED: Clamp to the tablet; exceptionally long descriptions remain scrollable.
        void Show()
        {
            // BEGIN CHANGED: Keep the popup to the left of the purchase column as well as click-through.
            var corners = new Vector3[4];
            owner.buy.GetComponent<RectTransform>().GetWorldCorners(corners);
            float buyLeft = stage.InverseTransformPoint(corners[0]).x - stage.rect.xMin;
            float width = Mathf.Clamp(buyLeft - 40, 120, 300);
            float rightLimit = Mathf.Max(32 + width, Mathf.Min(stage.rect.width - 32, buyLeft - 8));
            // END CHANGED
            body.rectTransform.sizeDelta = new Vector2(width - 20, 0);
            float bodyHeight = Mathf.Ceil(body.preferredHeight);
            // BEGIN CHANGED: Keep the entire overlay inside the wooden content frame.
            float visibleHeight = Mathf.Min(stage.rect.height - 72 - 22 - 20, bodyHeight);
            // END CHANGED
            float height = 20 + visibleHeight;
            panel.sizeDelta = new Vector2(width, height);
            viewport.sizeDelta = new Vector2(width - 20, visibleHeight);
            content.sizeDelta = new Vector2(width - 20, bodyHeight);
            body.rectTransform.sizeDelta = content.sizeDelta;
            content.anchoredPosition = Vector2.zero; scroll.verticalNormalizedPosition = 1;
            // BEGIN CHANGED: Respect the wood border and header when flipping the popup.
            float x = Mathf.Clamp(pointer.x + 12, 32, rightLimit - width);
            float y = -pointer.y + 16;
            if (y + height > stage.rect.height - 22) y = -pointer.y - height - 12;
            y = Mathf.Clamp(y, 72, stage.rect.height - height - 22);
            // END CHANGED
            panel.anchoredPosition = new Vector2(Mathf.Round(x), -Mathf.Round(y));
            // BEGIN CHANGED: Visibility must never enable input blocking.
            visibility.alpha = 1; visibility.blocksRaycasts = false;
            // END CHANGED
        }
        // END ADDED
    }
}
// END ADDED
