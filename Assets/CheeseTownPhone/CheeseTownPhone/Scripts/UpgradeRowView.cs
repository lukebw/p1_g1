// BEGIN ADDED: A reusable prefab presents upgrade data without owning gameplay effects.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // BEGIN CHANGED: Only the description owns hover input; the row and Buy button stay independent.
    public sealed class UpgradeRowView : MonoBehaviour
    // END CHANGED
    {
        [Header("Editable layout - child RectTransforms control placement")]
        [Min(0)] public float rowSpacing = 8;
        [Min(0)] public float hoverDelay = .35f;
        public Image icon;
        public Text iconPlaceholder, title, description, level, effectName, effectValue, price;
        public Button buy;
        public RectTransform markerArea;
        public Image markerTemplate;
        [Header("Progress artwork")]
        public Sprite inactiveMarker, activeMarker, buyArtwork, disabledArtwork;
        public UpgradeOption Option { get; private set; }
        public float Height => ((RectTransform)transform).rect.height;
        public int DisplayLevel { get; private set; }
        public int DisplayMaxLevel { get; private set; }
        readonly List<Image> markers = new List<Image>();
        UpgradeTooltip tooltip;

        // BEGIN ADDED: Binding preserves the original option ID and purchase callback.
        public void Bind(UpgradeOption option, UnityAction purchase, UpgradeTooltip sharedTooltip)
        {
            Option = option; tooltip = sharedTooltip;
            name = "Upgrade " + option.id;
            title.text = option.title.ToUpperInvariant();
            description.text = Preview(option.description, description);
            // BEGIN ADDED: Attach behavior at runtime without rewriting the artist's prefab layout.
            description.raycastTarget = true;
            var hover = description.GetComponent<UpgradeDescriptionHover>();
            if (hover == null) hover = description.gameObject.AddComponent<UpgradeDescriptionHover>();
            hover.row = this;
            // END ADDED
            icon.sprite = option.icon; icon.gameObject.SetActive(option.icon != null);
            iconPlaceholder.gameObject.SetActive(option.icon == null);
            buy.name = "Buy " + option.id;
            // BEGIN CHANGED: Buying dismisses the current effect tooltip immediately.
            buy.onClick.RemoveAllListeners();
            buy.onClick.AddListener(() => { if (tooltip != null) tooltip.Hide(); purchase(); });
            // END CHANGED
            foreach (var text in GetComponentsInChildren<Text>(true)) Sharpen(text.font);
            // Fit extra levels into additional rows without stretching their pixel art.
            int count = option.levels.Count + 1;
            // BEGIN CHANGED: The final marker needs no trailing gap, so four fit in 67 pixels.
            int columns = Mathf.Max(1, Mathf.FloorToInt((markerArea.rect.width + 2) / 17));
            // END CHANGED
            int lines = Mathf.CeilToInt(count / (float)columns);
            if (lines > 1)
            {
                var root = (RectTransform)transform;
                root.sizeDelta += new Vector2(0, (lines - 1) * 17);
                markerArea.sizeDelta = new Vector2(markerArea.sizeDelta.x, lines * 17);
            }
            for (int i = 0; i < count; i++)
            {
                var marker = Instantiate(markerTemplate, markerArea);
                marker.name = "Level marker " + (i + 1);
                marker.rectTransform.anchoredPosition = new Vector2(i % columns * 17, -(i / columns) * 17);
                marker.gameObject.SetActive(true); markers.Add(marker);
            }
            markerTemplate.gameObject.SetActive(false);
        }
        // END ADDED

        // BEGIN ADDED: One-based labels leave purchase counts and level costs untouched.
        public void RefreshDisplay(int purchased, int upgradeCount, bool canBuy, string effect,
            string current, string next)
        {
            DisplayLevel = purchased + 1; DisplayMaxLevel = upgradeCount + 1;
            level.text = "LV " + DisplayLevel + "/" + DisplayMaxLevel;
            effectName.text = effect;
            effectValue.text = purchased < upgradeCount ? current + " > " + next : current + " (MAX)";
            buy.interactable = canBuy;
            buy.GetComponent<Image>().sprite = !canBuy && disabledArtwork != null ? disabledArtwork : buyArtwork;
            int cost = purchased < upgradeCount ? Option.levels[purchased].price : 0;
            price.text = purchased >= upgradeCount ? "MAX" : cost <= 9999 ? cost.ToString() : "10K+";
            // BEGIN CHANGED: Tooltip content comes directly from the effect description, without repeated stats.
            // END CHANGED
            for (int i = 0; i < markers.Count; i++) markers[i].sprite = i < DisplayLevel ? activeMarker : inactiveMarker;
        }
        // END ADDED

        // BEGIN ADDED: Truncate only the preview; the original description remains in the tooltip.
        public static string Preview(string value, Text label)
        {
            value = value ?? "";
            if (Fits(value, label)) return value;
            // BEGIN CHANGED: The triangle marks truncated text as an entry point to full details.
            const string suffix = "... \u25B6";
            int length = value.Length;
            while (length > 0 && !Fits(value.Substring(0, length).TrimEnd() + suffix, label)) length--;
            if (length > 0)
            {
                int boundary = value.LastIndexOf(' ', length - 1, length);
                if (boundary > length / 2) length = boundary;
            }
            return value.Substring(0, length).TrimEnd() + suffix;
            // END CHANGED
        }
        public static bool Fits(string value, Text label)
        {
            var settings = label.GetGenerationSettings(label.rectTransform.rect.size);
            var clipped = new TextGenerator(); clipped.Populate(value, settings);
            settings.verticalOverflow = VerticalWrapMode.Overflow;
            var full = new TextGenerator(); full.Populate(value, settings);
            return clipped.characterCountVisible == full.characterCountVisible;
        }
        public static void Sharpen(Font font)
        {
            if (font != null && font.material != null && font.material.mainTexture != null)
                font.material.mainTexture.filterMode = FilterMode.Point;
        }
        // END ADDED

        // BEGIN CHANGED: Description hover reveals only the card-style effect text.
        public void ShowDescription(PointerEventData data)
        {
            if (tooltip != null && Option != null && !string.IsNullOrWhiteSpace(Option.description))
                tooltip.Request(this, Option.description, data.position, data.enterEventCamera, hoverDelay);
        }
        public void HideDescription() { if (tooltip != null) tooltip.Hide(this); }
        public bool ScrollDescription(PointerEventData data) => tooltip != null && tooltip.ScrollDescription(this, data);
        void OnDisable() { if (tooltip != null) tooltip.Hide(this); }
        // END CHANGED
    }
}
// END ADDED
