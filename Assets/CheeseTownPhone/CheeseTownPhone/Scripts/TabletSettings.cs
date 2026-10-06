using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTownPhone
{
    // BEGIN CHANGED: Preserve serialized values; append wild growth without moving existing effects.
    public enum UpgradeEffect { MoveSpeed = 0, CollectRange = 1, TownTreeProduction = 2, AutoCollect = 3, CheeseValue = 4, WildTreeGrowth = 5 }
    // END CHANGED
    public enum UpgradeTarget { Player, Tree }

    [Serializable]
    public sealed class UpgradeLevel
    {
        [Min(0)] public int price;
        [Tooltip("Absolute speed/radius; TownTreeProduction = stock per second; WildTreeGrowth = drop-range multiplier; CheeseValue = unit value multiplier. AutoCollect follows town production.")]
        [Min(0)] public float value;
        [Tooltip("WildTreeGrowth only: size multiplier for harvestable world trees. Town main tree upgrades only change efficiency.")]
        [Min(.1f)] public float treeScale = 1;
        public UpgradeLevel(int price, float value, float scale = 1)
        { this.price = price; this.value = value; treeScale = scale; }
    }

    [Serializable]
    public sealed class UpgradeOption
    {
        [HideInInspector] public string id;
        public bool available = true;
        public string title = "New upgrade";
        [TextArea(1, 3)] public string description = "Describe this upgrade here.";
        public UpgradeEffect effect;
        public Sprite icon;
        public List<UpgradeLevel> levels = new List<UpgradeLevel>();
        public UpgradeTarget Target => effect == UpgradeEffect.TownTreeProduction || effect == UpgradeEffect.WildTreeGrowth || effect == UpgradeEffect.CheeseValue ? UpgradeTarget.Tree : UpgradeTarget.Player;
    }

    [CreateAssetMenu(menuName = "Cheese Town/Tablet Settings", fileName = "TabletSettings")]
    public sealed class TabletSettings : ScriptableObject
    {
        [Header("Artwork slots - optional Sprite assets")]
        public Sprite townBackground;
        public Sprite cheeseTree;
        public Sprite envelopeIcon;
        public Sprite shopIcon;
        public Sprite closeIcon;
        public Sprite upgradeButtonArtwork;
        public Font interfaceFont;
        // BEGIN ADDED: Separate skin slots keep two-times exports reusable.
        [Header("Pixel UI - 640 x 360 layout, artwork exported at 2x")]
        public Sprite mainFrame;
        public Sprite backButtonArtwork;
        public Sprite collectButtonArtwork;
        public Sprite unreadDot;
        public Sprite walletIcon;
        // END ADDED
        // BEGIN ADDED: Dedicated shop slots preserve supplied button states and row art.
        [Header("Pixel upgrade page - artwork exported at 2x")]
        public Sprite upgradeFrame;
        public Sprite upgradeBackground;
        public Sprite upgradeRowArtwork;
        public Sprite upgradeIconBox;
        public Sprite upgradeLevelArtwork;
        public Sprite upgradeLevelInactive;
        public Sprite upgradeLevelActive;
        public Sprite upgradeBuyDisabled;
        public Sprite allSelected, allUnselected;
        public Sprite playerSelected, playerUnselected;
        public Sprite treeSelected, treeUnselected;
        // BEGIN ADDED: Mail artwork uses the same native two-times export workflow.
        [Header("Pixel mail page")]
        public Sprite mailPaper, mailPrevious, mailNext, mailRead;
        // END ADDED
        // END ADDED
        // BEGIN ADDED: Reusable views keep layout editing separate from upgrade data.
        [Header("Reusable upgrade UI")]
        public UpgradeRowView upgradeRowPrefab;
        public Font upgradePixelFont;
        // BEGIN ADDED: Artists edit the saved tablet hierarchy instead of generated layout code.
        [Header("Editable tablet UI")]
        public TabletView tabletPrefab;
        [Header("First-session interaction guide")]
        public TutorialCoachView tutorialCoachPrefab;
        // END ADDED
        // END ADDED
        [Header("Placeholder colors and layout")]
        public Color backgroundColor = new Color(.20f, .30f, .34f, 1);
        public Color treeColor = new Color(.90f, .73f, .31f, 1);
        public Vector2 treeSize = new Vector2(150, 200);
        [Header("Starting values - editable demonstration defaults")]
        [UnityEngine.Serialization.FormerlySerializedAs("startingCoins")] [Min(0)] public int startingCheeses = 500;
        [Min(0)] public float baseMoveSpeed = 4;
        [Min(0)] public float baseCollectRange = 0;
        [Min(0)] public float baseTreeProduction = 1;
        [Min(1)] public int baseCheesePrice = 1;
        [Min(1)] public int treeCapacity = 100;
        [Min(0)] public float initialTreeStock = 10;

        [Header("Shop list - add/remove/reorder entries and levels")]
        public List<UpgradeOption> upgrades = Defaults();
        public int Revision { get; private set; }

        public static List<UpgradeOption> Defaults() => new List<UpgradeOption>
        {
            new UpgradeOption { id = "move-speed", title = "Move Speed", description = "Increase player movement speed.", effect = UpgradeEffect.MoveSpeed,
                levels = new List<UpgradeLevel> { new UpgradeLevel(20, 6), new UpgradeLevel(50, 8) } },
            // BEGIN CHANGED: The base state is displayed as level one.
            new UpgradeOption { id = "collect-range", title = "Collect Range", description = "Expand ground cheese pickup range and the reach of manual tree collection.", effect = UpgradeEffect.CollectRange,
            // END CHANGED
                levels = new List<UpgradeLevel> { new UpgradeLevel(30, 2), new UpgradeLevel(60, 4) } },
            new UpgradeOption { id = "tree-growth", title = "Town Main Tree", description = "Increase the town main tree's production per second. Auto collection keeps pace with its production.", effect = UpgradeEffect.TownTreeProduction,
                levels = new List<UpgradeLevel> { new UpgradeLevel(60, 2), new UpgradeLevel(120, 4), new UpgradeLevel(240, 8) } },
            new UpgradeOption { id = "auto-collect", title = "Auto Collect", description = "Automatically collect shared tree stock each second at its production rate, wherever you are.", effect = UpgradeEffect.AutoCollect,
                levels = new List<UpgradeLevel> { new UpgradeLevel(150, 1) } },
            new UpgradeOption { id = "double-cheese", title = "Double Cheese Value", description = "Double each cheese's cheese yield, not the number produced.", effect = UpgradeEffect.CheeseValue,
                levels = new List<UpgradeLevel> { new UpgradeLevel(260, 2) } },
            new UpgradeOption { id = "wild-tree-growth", title = "Wild Trees", description = "Increase wild cheese tree size and the minimum and maximum cheese dropped when chopped. Ordinary and high-value trees keep their own base drop ranges.", effect = UpgradeEffect.WildTreeGrowth,
                levels = new List<UpgradeLevel> { new UpgradeLevel(60, 1.25f, 1.2f), new UpgradeLevel(120, 1.5f, 1.45f), new UpgradeLevel(240, 2, 1.7f) } }
        };

        public void ValidateSettings()
        {
            startingCheeses = Mathf.Clamp(startingCheeses, 0, 1000000000);
            baseMoveSpeed = Safe(baseMoveSpeed, 0, 10000);
            baseCollectRange = Safe(baseCollectRange, 0, 10000);
            baseTreeProduction = Safe(baseTreeProduction, 0, 100000);
            baseCheesePrice = Mathf.Clamp(baseCheesePrice, 1, 100000);
            treeCapacity = Mathf.Clamp(treeCapacity, 1, 1000000);
            initialTreeStock = Safe(initialTreeStock, 0, treeCapacity);
            treeSize.x = Safe(treeSize.x, 20, 260); treeSize.y = Safe(treeSize.y, 20, 250);
            if (upgrades == null) upgrades = new List<UpgradeOption>();
            var ids = new HashSet<string>();
            foreach (var option in upgrades)
            {
                if (option == null) continue;
                if (string.IsNullOrEmpty(option.id) || !ids.Add(option.id))
                { option.id = Guid.NewGuid().ToString("N"); ids.Add(option.id); }
                if (option.levels == null) option.levels = new List<UpgradeLevel>();
                for (int i = 0; i < option.levels.Count; i++)
                {
                    if (option.levels[i] == null) option.levels[i] = new UpgradeLevel(0, 0);
                    var level = option.levels[i];
                    level.price = Mathf.Clamp(level.price, 0, 1000000000);
                    level.value = Safe(level.value, 0, 100000);
                    level.treeScale = Safe(level.treeScale, .1f, 2);
                }
            }
            Revision++;
        }
        static float Safe(float value, float min, float max)
        { return float.IsNaN(value) || float.IsInfinity(value) ? min : Mathf.Clamp(value, min, max); }
        void OnValidate() { ValidateSettings(); }
    }
}
