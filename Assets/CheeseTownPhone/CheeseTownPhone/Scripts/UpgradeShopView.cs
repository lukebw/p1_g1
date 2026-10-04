// BEGIN ADDED: Rebuild only data rows; animate child offsets without disturbing scrolling or prefab layout.
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed class UpgradeShopView : MonoBehaviour
    {
        public UpgradeListScrollRect scroll;
        public RectTransform content, revealMask;
        public UpgradeTooltip tooltip;
        public Button[] categories;
        public Sprite[] selected, unselected;
        public Text empty;
        [Tooltip("Authoring preview only; edit the nested UpgradeRow prefab for all live rows.")]
        public UpgradeRowView preview;
        [Header("Card entrance - unscaled seconds")]
        [Min(.01f)] public float cardDuration = .24f;
        [Min(0)] public float cardStagger = .045f;
        [Min(0)] public float cardSlide = 8;
        public bool IsDealing { get; private set; }
        public IReadOnlyList<UpgradeRowView> Rows => views;
        readonly List<UpgradeRowView> views = new List<UpgradeRowView>();
        readonly List<RectTransform> slots = new List<RectTransform>();
        readonly List<CanvasGroup> groups = new List<CanvasGroup>();
        float elapsed;
        bool bound;
        Vector2 lastScrollPosition;

        public void Bind(Action<int> filter)
        {
            if (bound) return;
            bound = true;
            for (int i = 0; i < categories.Length; i++) { int value = i; categories[i].onClick.AddListener(() => filter(value)); }
            scroll.tooltip = tooltip;
            scroll.onValueChanged.AddListener(_ => {
                tooltip.Hide();
                var position = content.anchoredPosition;
                if (IsDealing && Vector2.Distance(position, lastScrollPosition) > .1f) FinishEntrance();
                content.anchoredPosition = new Vector2(0, Mathf.Round(position.y));
                lastScrollPosition = content.anchoredPosition;
            });
            if (preview != null) preview.gameObject.SetActive(false);
        }
        public void Rebuild(TabletSettings settings, int filter, Action<UpgradeOption> purchase, bool animate)
        {
            FinishEntrance(); tooltip.Hide();
            foreach (var slot in slots) { slot.gameObject.SetActive(false); if (Application.isPlaying) Destroy(slot.gameObject); else DestroyImmediate(slot.gameObject); }
            slots.Clear(); views.Clear(); groups.Clear();
            for (int i = 0; i < categories.Length; i++) categories[i].GetComponent<Image>().sprite = i == filter ? selected[i] : unselected[i];
            float cursor = 0, spacing = 0;
            foreach (var option in settings.upgrades)
            {
                if (settings.upgradeRowPrefab == null) break;
                if (option == null || !option.available || filter == 1 && option.Target != UpgradeTarget.Player || filter == 2 && option.Target != UpgradeTarget.Tree) continue;
                var slot = new GameObject("Card slot - " + option.id, typeof(RectTransform)).GetComponent<RectTransform>();
                slot.SetParent(content, false); slot.anchorMin = slot.anchorMax = slot.pivot = new Vector2(0, 1);
                slot.anchoredPosition = new Vector2(0, -cursor);
                var row = Instantiate(settings.upgradeRowPrefab, slot);
                var rect = (RectTransform)row.transform; rect.anchoredPosition = Vector2.zero;
                row.Bind(option, () => purchase(option), tooltip);
                slot.sizeDelta = new Vector2(content.rect.width, row.Height);
                slots.Add(slot); views.Add(row); groups.Add(slot.gameObject.AddComponent<CanvasGroup>());
                spacing = Mathf.Round(row.rowSpacing); cursor += Mathf.Round(row.Height) + spacing;
            }
            empty.gameObject.SetActive(views.Count == 0);
            empty.text = settings.upgradeRowPrefab == null ? "Assign an Upgrade Row Prefab in Tablet Settings." : "No upgrades in this category.";
            content.sizeDelta = new Vector2(content.sizeDelta.x, Mathf.Max(scroll.viewport.rect.height, cursor - spacing));
            scroll.StopMovement(); content.anchoredPosition = Vector2.zero;
            Canvas.ForceUpdateCanvases();
            if (animate && gameObject.activeInHierarchy) PlayEntrance();
        }
        public void PlayEntrance()
        {
            if (views.Count == 0) return;
            tooltip.Hide(); elapsed = 0; lastScrollPosition = content.anchoredPosition; IsDealing = true; AnimateCards();
        }
        public void FinishEntrance()
        {
            IsDealing = false;
            for (int i = 0; i < views.Count; i++)
            {
                ((RectTransform)views[i].transform).anchoredPosition = Vector2.zero;
                groups[i].alpha = 1; groups[i].interactable = groups[i].blocksRaycasts = true;
            }
        }
        void Update() { Advance(Time.unscaledDeltaTime); }
        public void Advance(float delta)
        {
            if (!IsDealing) return;
            elapsed += delta; AnimateCards();
        }
        void AnimateCards()
        {
            bool finished = true;
            for (int i = 0; i < views.Count; i++)
            {
                float y = -slots[i].anchoredPosition.y - content.anchoredPosition.y;
                bool visible = y + slots[i].rect.height >= 0 && y <= scroll.viewport.rect.height;
                // Offscreen cards stay at rest, so a large catalog does not extend the entrance.
                float progress = visible ? Mathf.Clamp01((elapsed - Mathf.Min(i, 5) * cardStagger) / Mathf.Max(.01f, cardDuration)) : 1;
                float eased = progress * progress * (3 - 2 * progress);
                ((RectTransform)views[i].transform).anchoredPosition = new Vector2(0, Mathf.Round((Mathf.Max(0, y) + cardSlide) * (1 - eased)));
                groups[i].alpha = eased; groups[i].interactable = groups[i].blocksRaycasts = progress == 1;
                finished &= progress == 1;
            }
            if (finished) FinishEntrance();
        }
        void OnDisable() { FinishEntrance(); if (tooltip != null) tooltip.Hide(); }
    }
}
// END ADDED
