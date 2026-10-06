using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTownPhone
{
    // BEGIN CHANGED: Append new effects while preserving the serialized values of existing effects.
    public enum UpgradeEffect { MoveSpeed = 0, CollectRange = 1, TownTreeProduction = 2, AutoCollect = 3, CheeseValue = 4, WildTreeGrowth = 5,
        TownTreeStart = 6, TownTreeBatch = 7, TownTreeInterval = 8, TownTreeCapacity = 9 }
    // END CHANGED
    public enum UpgradeTarget { Player, Tree }

    [Serializable]
    public sealed class UpgradeLevel
    {
        [Min(0)] public int price;
        [Tooltip("Absolute upgraded value: speed, pickup radius, batch count, maturation seconds, or storage capacity. WildTreeGrowth multiplies the drop range. TownTreeStart is a one-time unlock. Legacy effects retain their previous semantics.")]
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
        // BEGIN ADDED: Keep main-tree upgrades under TREE and gate all three tracks behind startup.
        public bool RequiresTreeStart => effect == UpgradeEffect.TownTreeBatch || effect == UpgradeEffect.TownTreeInterval || effect == UpgradeEffect.TownTreeCapacity;
        public UpgradeTarget Target => effect == UpgradeEffect.TownTreeProduction || effect == UpgradeEffect.WildTreeGrowth || effect == UpgradeEffect.CheeseValue
            || effect == UpgradeEffect.TownTreeStart || RequiresTreeStart ? UpgradeTarget.Tree : UpgradeTarget.Player;
        // END ADDED
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
        [Header("Opening narrative")]
        public PrologueView prologuePrefab;
        // END ADDED
        // END ADDED
        [Header("Placeholder colors and layout")]
        public Color backgroundColor = new Color(.20f, .30f, .34f, 1);
        public Color treeColor = new Color(.90f, .73f, .31f, 1);
        public Vector2 treeSize = new Vector2(150, 200);
        [Header("Starting values - editable demonstration defaults")]
        [UnityEngine.Serialization.FormerlySerializedAs("startingCoins")] [Min(0)] public int startingCheeses = 0;
        [Min(0)] public float baseMoveSpeed = 4;
        [Min(0)] public float baseCollectRange = 0;
        [Min(0)] public float baseTreeProduction = 1;
        [Min(1)] public int baseCheesePrice = 1;
        [Min(1)] public int treeCapacity = 200;
        [Min(0)] public float initialTreeStock = 0;
        // BEGIN ADDED: The 10k-cheese curve retains legacy economy support for old profiles and regressions.
        [Header("Batch production playtest - quantities, not currency value")]
        public bool useBatchProduction = true;
        [Min(1)] public int baseBatchSize = 5;
        [Min(.1f)] public float baseProductionInterval = 5;
        [Tooltip("Multiply the final wild-tree drop roll after upgrade scaling and rounding.")]
        [Range(1, 10)] public int wildDropCountMultiplier = 1;
        [Min(1)] public int collectionGoal = 10000;
        // END ADDED

        [Header("Shop list - add/remove/reorder entries and levels")]
        public List<UpgradeOption> upgrades = Defaults();
        public int Revision { get; private set; }

        // BEGIN ADDED: Prices are per-tier costs; values are absolute upgraded values, not chained multipliers.
        public static List<UpgradeOption> Defaults() => new List<UpgradeOption>
        {
            new UpgradeOption { id = "move-speed", title = "Speed Boots", description = "Light-footed boots for the long road home. Move faster through the wild groves.", effect = UpgradeEffect.MoveSpeed,
                levels = new List<UpgradeLevel> { new UpgradeLevel(20, 6), new UpgradeLevel(50, 8) } },
            new UpgradeOption { id = "collect-range", title = "Cheese Magnet", description = "No morsel left behind. Draw in cheese from farther away while roaming the wilds.", effect = UpgradeEffect.CollectRange,
                levels = new List<UpgradeLevel> { new UpgradeLevel(30, 2), new UpgradeLevel(60, 4) } },
            new UpgradeOption { id = "tree-start", title = "Spring Tonic", description = "Revive the Great Cheese Tree, guardian of Mousetown. Its branches will bear cheese again. Return to gather the harvest.", effect = UpgradeEffect.TownTreeStart,
                levels = new List<UpgradeLevel> { new UpgradeLevel(30, 1) } },
            new UpgradeOption { id = "tree-batch", title = "Growth Potion", description = "A hearty brew for hungry roots. The Great Tree bears more cheese each harvest. Revive it with Spring Tonic first.", effect = UpgradeEffect.TownTreeBatch,
                levels = new List<UpgradeLevel> { new UpgradeLevel(30, 8), new UpgradeLevel(60, 12), new UpgradeLevel(140, 20), new UpgradeLevel(450, 60), new UpgradeLevel(1400, 180) } },
            new UpgradeOption { id = "tree-interval", title = "Haste Potion", description = "A taste of spring in every drop. The Great Tree ripens cheese sooner. Revive it first; gather full stores to keep it growing.", effect = UpgradeEffect.TownTreeInterval,
                levels = new List<UpgradeLevel> { new UpgradeLevel(40, 4), new UpgradeLevel(100, 3), new UpgradeLevel(300, 2), new UpgradeLevel(1000, 1) } },
            new UpgradeOption { id = "tree-capacity", title = "Cozy Cellar", description = "Make room for a richer harvest. The Great Tree can hold more cheese before resting. Revive it first; return to gather your stores.", effect = UpgradeEffect.TownTreeCapacity,
                levels = new List<UpgradeLevel> { new UpgradeLevel(30, 500), new UpgradeLevel(90, 1200), new UpgradeLevel(250, 3000), new UpgradeLevel(650, 8000) } },
            new UpgradeOption { id = "wild-tree-growth", title = "Forest Charm", description = "A little woodland magic. Wild cheese trees grow larger and drop more cheese when felled. Each kind keeps its natural bounty.", effect = UpgradeEffect.WildTreeGrowth,
                levels = new List<UpgradeLevel> { new UpgradeLevel(20, 1.25f, 1.2f), new UpgradeLevel(30, 1.5f, 1.45f), new UpgradeLevel(60, 2, 1.7f) } }
        };
        // END ADDED

        public static List<UpgradeOption> LegacyDefaults() => new List<UpgradeOption>
        {
            new UpgradeOption { id = "move-speed", title = "Move Speed", description = "Light-footed boots for the long road home. Move faster through the wild groves.", effect = UpgradeEffect.MoveSpeed,
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
            baseBatchSize = Mathf.Clamp(baseBatchSize, 1, 100000);
            baseProductionInterval = Safe(baseProductionInterval, .1f, 3600);
            wildDropCountMultiplier = Mathf.Clamp(wildDropCountMultiplier, 1, 10);
            collectionGoal = Mathf.Clamp(collectionGoal, 1, 1000000000);
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
