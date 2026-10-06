// BEGIN ADDED: Supplied shop artwork reuses the existing upgrade rules and callbacks.
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    public sealed partial class CheeseTownDemo
    {
        // BEGIN CHANGED: Bind existing purchase rows to reusable prefab views.
        readonly Dictionary<Row, UpgradeRowView> pixelUpgradeRows = new Dictionary<Row, UpgradeRowView>();
        // END CHANGED

        // BEGIN ADDED: Keep cropped sprites at half export size and preserve category states.
        void PixelCategory(Transform parent, string name, string text, int category, float x,
            Sprite selected, Sprite unselected)
        {
            PixelSpriteButton(parent, name, filter == category ? selected : unselected, text,
                x, 74, () => ChangeFilter(category));
        }
        // BEGIN CHANGED: Row visuals and dimensions now come from UpgradeRow.prefab.
        // END CHANGED
        // END ADDED

        // BEGIN ADDED: Taller reference rows overflow naturally into a wheel-scrollable viewport.
        void BuildPixelShop(Transform frame)
        {
            shop = Box(frame, "Upgrade shop panel", 0, 0, 640, 360, Color.clear).gameObject;
            shopWallet = PixelLabel(shop.transform, "Shop wallet", "", 0, 0, 1, 1);
            shopWallet.gameObject.SetActive(false);
            PixelCategory(shop.transform, "All filter", "ALL", 0, 38, settings.allSelected, settings.allUnselected);
            PixelCategory(shop.transform, "Player filter", "PLAYER", 1, 127, settings.playerSelected, settings.playerUnselected);
            PixelCategory(shop.transform, "Tree filter", "TREE", 2, 214, settings.treeSelected, settings.treeUnselected);
            // BEGIN ADDED: The tooltip is a sibling of the masked viewport, so it stays unclipped.
            var tooltip = UpgradeTooltip.Create((RectTransform)shop.transform,
                settings.upgradePixelFont != null ? settings.upgradePixelFont : settings.interfaceFont, settings.upgradeRowArtwork);
            // END ADDED
            var viewport = Box(shop.transform, "Shop scroll viewport", 39, 117, 564, 221, Color.clear);
            viewport.GetComponent<Image>().raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect(viewport, "Upgrade rows", 0, 0, 564, 0);
            // BEGIN CHANGED: Wheel and drag input dismiss any open row description.
            var scroll = viewport.gameObject.AddComponent<UpgradeListScrollRect>(); scroll.tooltip = tooltip;
            // END CHANGED
            scroll.viewport = viewport; scroll.content = content;
            scroll.horizontal = false; scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.inertia = false; scroll.scrollSensitivity = 18;
            scroll.onValueChanged.AddListener(_ => {
                // BEGIN ADDED: Scrollbar movement also dismisses descriptions.
                tooltip.Hide();
                // END ADDED
                var position = content.anchoredPosition;
                content.anchoredPosition = new Vector2(0, Mathf.Round(position.y));
            });
            var track = Box(shop.transform, "Scroll track", 605, 117, 3, 221, new Color32(29, 45, 24, 255));
            var handle = Box(track, "Scroll handle", 0, 0, 3, 30, new Color32(176, 100, 47, 255));
            var bar = track.gameObject.AddComponent<Scrollbar>();
            bar.handleRect = handle; bar.targetGraphic = handle.GetComponent<Image>();
            handle.offsetMin = Vector2.zero; handle.offsetMax = Vector2.zero;
            handle.GetComponent<Image>().raycastTarget = true;
            bar.direction = Scrollbar.Direction.BottomToTop;
            scroll.verticalScrollbar = bar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
            // BEGIN CHANGED: Stack prefab heights instead of a fixed row stride.
            int index = 0; float cursor = 0, lastSpacing = 0;
            foreach (var option in settings.upgrades)
            {
                if (option == null || !option.available || filter == 1 && option.Target != UpgradeTarget.Player
                    || filter == 2 && option.Target != UpgradeTarget.Tree) continue;
                if (settings.upgradeRowPrefab == null) break;
                var view = Instantiate(settings.upgradeRowPrefab, content);
                ((RectTransform)view.transform).anchoredPosition = new Vector2(0, -cursor);
                view.Bind(option, () => Buy(option), tooltip);
                var row = new Row { option = option };
                pixelUpgradeRows.Add(row, view); rows.Add(row); index++;
                lastSpacing = Mathf.Round(view.rowSpacing); cursor += Mathf.Round(view.Height) + lastSpacing;
            }
            float height = Mathf.Max(221, cursor - lastSpacing);
            // END CHANGED
            content.sizeDelta = new Vector2(564, height);
            track.gameObject.SetActive(height > 221);
            scroll.verticalNormalizedPosition = 1;
            // BEGIN CHANGED: A missing template gives an actionable setup message.
            if (index == 0) PixelLabel(content, "Empty shop", settings.upgradeRowPrefab == null
                ? "Assign an Upgrade Row Prefab in Tablet Settings." : "No equipment in this category.", 60, 65, 444, 28, 8, TextAnchor.MiddleCenter);
            // END CHANGED
        }
        // END ADDED

        // BEGIN ADDED: Refresh only presentation; purchases still use TownProgress.CanBuy.
        bool RefreshPixelUpgradeRow(Row row, int level, int max)
        {
            // BEGIN CHANGED: The prefab updates presentation while the controller supplies shared progress.
            if (!HasPixelSkin || !pixelUpgradeRows.TryGetValue(row, out var view)) return false;
            view.RefreshDisplay(level, max, Progress.CanBuy(row.option), PixelEffectName(row.option.effect),
                PixelCurrentValue(row.option.effect), level < max ? PixelNextValue(row.option.effect, row.option.levels[level]) : "", Progress.IsUpgradeLocked(row.option));
            // END CHANGED
            view.buy.interactable = !Progress.GameEnded && !Progress.IsUpgradeLocked(row.option) && row.option.available && level < max;
            return true;
        }
        string PixelEffectName(UpgradeEffect effect)
        {
            switch (effect)
            {
                case UpgradeEffect.MoveSpeed: return "SPEED";
                case UpgradeEffect.CollectRange: return "RADIUS";
                // BEGIN CHANGED: Short labels leave room for full-size pixel glyphs.
                case UpgradeEffect.TownTreeProduction: return "PER SEC";
                case UpgradeEffect.TownTreeStart: return "REVIVE";
                case UpgradeEffect.TownTreeBatch: return "PER BATCH";
                case UpgradeEffect.TownTreeInterval: return "SECONDS";
                case UpgradeEffect.TownTreeCapacity: return "CAPACITY";
                case UpgradeEffect.WildTreeGrowth: return "DROPS x";
                case UpgradeEffect.AutoCollect: return "AUTO";
                // END CHANGED
                default: return "VALUE";
            }
        }
        string PixelCurrentValue(UpgradeEffect effect)
        {
            switch (effect)
            {
                case UpgradeEffect.MoveSpeed: return Progress.MoveSpeed.ToString("0.##");
                case UpgradeEffect.CollectRange: return Progress.CollectRange.ToString("0.##");
                case UpgradeEffect.TownTreeProduction: return Progress.Production.ToString("0.##");
                case UpgradeEffect.TownTreeStart: return Progress.MainTreeStarted ? "ALIVE" : "DORMANT";
                case UpgradeEffect.TownTreeBatch: return Progress.BatchSize.ToString();
                case UpgradeEffect.TownTreeInterval: return Progress.ProductionInterval.ToString("0.#");
                case UpgradeEffect.TownTreeCapacity: return Progress.Capacity.ToString();
                case UpgradeEffect.WildTreeGrowth: return Progress.WildTreeYieldMultiplier.ToString("0.##");
                case UpgradeEffect.AutoCollect: return Progress.AutoEnabled ? "ON" : "OFF";
                default: return Progress.UnitPrice.ToString();
            }
        }
        // BEGIN CHANGED: Preview the same effective values used by the shared economy.
        string PixelNextValue(UpgradeEffect effect, UpgradeLevel next)
        {
            switch (effect)
            {
                case UpgradeEffect.AutoCollect: return "ON";
                case UpgradeEffect.TownTreeStart: return "ALIVE";
                case UpgradeEffect.TownTreeBatch: return Mathf.Max(Progress.BatchSize, Mathf.RoundToInt(next.value)).ToString();
                case UpgradeEffect.TownTreeInterval: return Mathf.Min(Progress.ProductionInterval, Mathf.Max(.1f, next.value)).ToString("0.#");
                case UpgradeEffect.TownTreeCapacity: return Mathf.Max(Progress.Capacity, Mathf.RoundToInt(next.value)).ToString();
                case UpgradeEffect.WildTreeGrowth: return Mathf.Max(Progress.WildTreeYieldMultiplier, next.value).ToString("0.##");
                case UpgradeEffect.CheeseValue:
                    return Mathf.Clamp(Mathf.RoundToInt(settings.baseCheesePrice * Mathf.Max(Progress.ValueMultiplier, next.value)), 1, 1000000).ToString();
                case UpgradeEffect.MoveSpeed: return Mathf.Max(Progress.MoveSpeed, next.value).ToString("0.##");
                case UpgradeEffect.CollectRange: return Mathf.Max(Progress.CollectRange, next.value).ToString("0.##");
                default: return Mathf.Max(Progress.Production, next.value).ToString("0.##");
            }
        }
        // END CHANGED
        // END ADDED
    }
}
// END ADDED
