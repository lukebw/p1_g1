using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTownPhone
{
    // Purchase IDs survive reorder. Removing/disabling a row removes its effect without refund.
    public sealed class TownProgress
    {
        public sealed class Letter
        {
            public string Id { get; }
            public string Body { get; }
            public bool IsRead { get; internal set; }
            internal Letter(string id, string body) { Id = id; Body = body; }
        }
        readonly List<Letter> letters = new List<Letter>();
        readonly HashSet<string> sentLetters = new HashSet<string>();
        public IReadOnlyList<Letter> Letters => letters.AsReadOnly();
        public long TotalCollected { get; private set; }
        public bool MailStopped => config.useBatchProduction ? GameEnded : TotalCollected >= 500;
        public const string FinalLetterId = "all-max";
        public const string GoalLetterId = "collection-goal";
        public bool GameEnded { get; private set; }
        public int UnreadCount => letters.FindAll(l => !l.IsRead).Count;
        // BEGIN ADDED: Choose on mailbox entry only; incoming mail must not interrupt active reading.
        public int MailboxEntryIndex
        {
            get
            {
                for (int i = letters.Count - 1; i >= 0; i--) if (!letters[i].IsRead) return i;
                return letters.Count - 1;
            }
        }
        // Guidance belongs to the session so closing the tablet or reloading a scene never restarts it.
        public int InteractionGuideStep { get; private set; }
        public const int InteractionGuideCount = 8;
        public void AdvanceInteractionGuide() { InteractionGuideStep = Math.Min(InteractionGuideStep + 1, InteractionGuideCount); }
        public void SkipInteractionGuide() { InteractionGuideStep = InteractionGuideCount; }
        // END ADDED
        public void ReadLetter(int index)
        {
            if (index < 0 || index >= letters.Count || letters[index].IsRead) return;
            letters[index].IsRead = true;
            if (letters[index].Id == FinalLetterId || letters[index].Id == GoalLetterId) GameEnded = true;
            Changed?.Invoke();
        }
        void SendLetter(string id, string body)
        {
            if (sentLetters.Add(id)) letters.Add(new Letter(id, body));
        }
        void CheckLetters()
        {
            // BEGIN CHANGED: Narrative milestones use the current wallet; sent letters remain permanent.
            if (config.useBatchProduction)
            {
                if (Cheeses >= 10)
                    SendLetter("shop", "A promising start, Leo!\n\nThe outfitter has saved a Spring Tonic for our Great Tree. Bring 30 cheeses and we can wake its roots. Boots and charms will help you gather supplies in the wilds.\n\nMayor Ellis");
                if (Cheeses >= 100)
                    SendLetter("reserve", "The first shelves are filling.\n\nA welcome start, Leo. The little ones asked for seconds tonight. There is still a long winter ahead, but Mousetown has something to hope for again.\n\nKeep our old guardian in your care.\n\nMayor Ellis");
                if (Cheeses >= 350)
                    SendLetter("winter-feast", "Warm lights in the windows.\n\nThe neighbors are sharing recipes again. Tonight, we will set a table beneath the Great Tree, just as we used to.\n\nYou have brought more than food home, Leo. You have brought us hope.\n\nMayor Ellis");
                if (Cheeses >= 1000)
                    SendLetter("overflow", "Outside the storeroom.\n\nThere is another pile of cheese in the square this morning. We have started leaving the surplus outside.\n\nWinter is already taken care of. I am not sure where the next harvest will go.\n\nMayor Ellis");
                if (Cheeses >= 3000)
                    SendLetter("spoiling", "Please leave some room.\n\nThe children cannot play in the square anymore. The cheese outside is beginning to spoil, and the neighbors keep their windows shut.\n\nCould we let the next harvest wait, Leo?\n\nMayor Ellis");
                if (Cheeses >= 6000)
                    SendLetter("leaving", "Before the road closes.\n\nFamilies have been asking whether the north road is still clear. Some are packing before nightfall.\n\nIt is quiet without them. We have more food than ever, but fewer people at the table.\n\nMayor Ellis");
                // The flowchart ends in silence, without another letter or acknowledgement gate.
                if (Cheeses >= config.collectionGoal) GameEnded = true;
                return;
            }
            // END CHANGED
            if (TotalCollected >= 10)
                SendLetter("shop", "Your first 10 cheeses!\n\nVisit UPGRADES to improve your movement, collection range and cheese tree.\n\nMayor Ellis");
            if (TotalCollected >= 100)
                SendLetter("reserve", "Our reserves are sufficient.\n\nThank you for collecting 100 cheeses. You can keep playing and improving the town.\n\nMayor Ellis");
            if (TotalCollected >= 350)
                SendLetter("stop-request", "Please stop collecting.\n\nYou have collected 350 cheeses. We have more than enough for the town; please give the tree a rest.\n\nMayor Ellis");
            // Legacy profiles retain their original completion rule.
            bool hasUpgrades = false, allMax = true;
            foreach (var option in config.upgrades)
            {
                if (option == null || !option.available || option.levels.Count == 0) continue;
                hasUpgrades = true;
                if (Level(option) < option.levels.Count) allMax = false;
            }
            if (hasUpgrades && allMax)
                SendLetter("all-max", "Every upgrade is complete!\n\nYou have reached the highest level of every available upgrade. Thank you for all your work for the town.\n\nMayor Ellis");
        }
        readonly Dictionary<string, int> purchases = new Dictionary<string, int>();
        TabletSettings config;
        float autoTimer, autoCredit;
        double batchElapsed;
        public int Cheeses { get; private set; }
        public float Stock { get; private set; }
        public int HarvestCount { get; private set; }
        public bool WelcomeClaimed { get; private set; }
        public bool TaskClaimed { get; private set; }
        public bool TaskReady => WelcomeClaimed && HarvestCount >= 3;
        public float MoveSpeed { get; private set; }
        public float CollectRange { get; private set; }
        public float WildTreeScale { get; private set; }
        public float Production { get; private set; }
        // BEGIN ADDED: World drops and town production have independent purchased effects.
        public float WildTreeYieldMultiplier { get; private set; }
        // END ADDED
        public bool AutoEnabled { get; private set; }
        public float AutoRate => AutoEnabled ? Production : 0;
        public float ValueMultiplier { get; private set; }
        public int UnitPrice => config.useBatchProduction ? 1 : Mathf.Clamp(Mathf.RoundToInt(config.baseCheesePrice * ValueMultiplier), 1, 1000000);
        public int Capacity { get; private set; }
        public bool UsesBatchProduction => config.useBatchProduction;
        public bool MainTreeStarted { get; private set; }
        public int BatchSize { get; private set; }
        public float ProductionInterval { get; private set; }
        public float NextBatchIn => MainTreeStarted && Stock < Capacity ? Mathf.Max(0, ProductionInterval - (float)batchElapsed) : 0;
        public int CollectionGoal => config.collectionGoal;
        public bool IsUpgradeLocked(UpgradeOption option) => config.useBatchProduction && option != null && option.RequiresTreeStart && !MainTreeStarted;
        public int ScaleWildDropCount(int rolledCount) => (int)Math.Min(int.MaxValue - 1L,
            (long)Mathf.Max(0, rolledCount) * (config.useBatchProduction ? config.wildDropCountMultiplier : 1));
        public event Action Changed;
        public event Action Collected, Purchased, PurchaseFailed, ShopOpened;
        public void NotifyShopOpened() { ShopOpened?.Invoke(); }

        public TownProgress(TabletSettings settings)
        {
            config = settings;
            config.ValidateSettings();
            Cheeses = config.startingCheeses;
            Stock = config.useBatchProduction ? 0 : config.initialTreeStock;
            SendLetter("welcome", config.useBatchProduction
                ? "Leo, our Great Cheese Tree is fading.\n\nIt has sheltered Mousetown for generations. Now winter is near and our storerooms are bare.\n\nGather cheese from the wild groves. The outfitter may have a remedy for our guardian. Help us bring it back to life.\n\nWrite soon,\n\nMayor Ellis"
                : "Welcome to Cheese Town! I'm Mayor Ellis.\n\nWe're happy to have you here. Use your axe on a cheese tree, then walk over the dropped cheese to collect it. Gather 10 cheeses, and I'll write again.\n\nUse your tablet to read letters and visit UPGRADES. Enjoy your first day!\n\nMayor Ellis");
            Reconfigure(settings);
        }
        public void Reconfigure(TabletSettings settings)
        {
            config = settings;
            var live = new HashSet<string>();
            foreach (var o in config.upgrades)
            {
                if (o == null || !o.available) continue;
                live.Add(o.id);
                if (purchases.TryGetValue(o.id, out int level)) purchases[o.id] = Mathf.Clamp(level, 0, o.levels.Count);
            }
            foreach (var key in new List<string>(purchases.Keys)) if (!live.Contains(key)) purchases.Remove(key);
            Recalculate(); Stock = Mathf.Min(Stock, Capacity); CheckLetters(); Changed?.Invoke();
        }
        public int Level(UpgradeOption o) => o != null && purchases.TryGetValue(o.id, out int value) ? Mathf.Min(value, o.levels.Count) : 0;
        public bool CanBuy(UpgradeOption o) => !GameEnded && o != null && o.available && config.upgrades.Contains(o) && !IsUpgradeLocked(o)
            && Level(o) < o.levels.Count && Cheeses >= o.levels[Level(o)].price;
        public bool Buy(UpgradeOption o)
        {
            if (!CanBuy(o)) { if (!GameEnded && o != null && o.available && config.upgrades.Contains(o) && Level(o) < o.levels.Count && Cheeses < o.levels[Level(o)].price) PurchaseFailed?.Invoke(); return false; }
            int current = Level(o);
            Cheeses -= o.levels[current].price; purchases[o.id] = current + 1;
            Recalculate(); CheckLetters(); Changed?.Invoke(); Purchased?.Invoke(); return true;
        }
        void Recalculate()
        {
            double phase = ProductionInterval > 0 ? batchElapsed / ProductionInterval : 0;
            bool wasStarted = MainTreeStarted;
            MoveSpeed = config.baseMoveSpeed; CollectRange = config.baseCollectRange;
            WildTreeScale = 1; WildTreeYieldMultiplier = 1;
            Production = config.baseTreeProduction; AutoEnabled = false; ValueMultiplier = 1;
            MainTreeStarted = !config.useBatchProduction;
            BatchSize = config.baseBatchSize; ProductionInterval = config.baseProductionInterval; Capacity = config.treeCapacity;
            if (config.useBatchProduction)
                foreach (var o in config.upgrades)
                    if (o != null && o.available && o.effect == UpgradeEffect.TownTreeStart && Level(o) > 0) MainTreeStarted = true;
            foreach (var o in config.upgrades)
            {
                if (o == null || !o.available || Level(o) == 0 || IsUpgradeLocked(o)) continue;
                var v = o.levels[Level(o) - 1];
                // Same-effect options use the strongest purchased value; no accidental stacking.
                switch (o.effect)
                {
                    case UpgradeEffect.MoveSpeed: MoveSpeed = Mathf.Max(MoveSpeed, v.value); break;
                    case UpgradeEffect.CollectRange: CollectRange = Mathf.Max(CollectRange, v.value); break;
                    case UpgradeEffect.TownTreeProduction: Production = Mathf.Max(Production, v.value); break;
                    case UpgradeEffect.WildTreeGrowth:
                        WildTreeScale = Mathf.Max(WildTreeScale, v.treeScale);
                        WildTreeYieldMultiplier = Mathf.Max(WildTreeYieldMultiplier, v.value); break;
                    case UpgradeEffect.AutoCollect: AutoEnabled = true; break;
                    case UpgradeEffect.CheeseValue: ValueMultiplier = Mathf.Max(ValueMultiplier, v.value); break;
                    case UpgradeEffect.TownTreeBatch: BatchSize = Mathf.Max(BatchSize, Mathf.RoundToInt(v.value)); break;
                    case UpgradeEffect.TownTreeInterval: ProductionInterval = Mathf.Min(ProductionInterval, Mathf.Max(.1f, v.value)); break;
                    case UpgradeEffect.TownTreeCapacity: Capacity = Mathf.Max(Capacity, Mathf.RoundToInt(v.value)); break;
                }
            }
            if (!AutoEnabled) { autoTimer = 0; autoCredit = 0; }
            // BEGIN ADDED: Preserve cycle completion when upgrading without creating stock or restarting the timer.
            if (config.useBatchProduction)
            {
                AutoEnabled = false; ValueMultiplier = 1; autoTimer = autoCredit = 0;
                Production = MainTreeStarted ? BatchSize / ProductionInterval : 0;
                batchElapsed = MainTreeStarted && wasStarted && Stock < Capacity ? Math.Min(.999999999, Math.Max(0, phase)) * ProductionInterval : 0;
                if (!MainTreeStarted) Stock = 0;
            }
            // END ADDED
        }
        public void Tick(float delta)
        {
            if (GameEnded || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
            if (config.useBatchProduction) { TickBatches(delta); return; }
            // Step at one-second auto-collection boundaries so frame size cannot change the economy.
            while (delta > .00001f)
            {
                float step = AutoEnabled ? Mathf.Min(delta, 1 - autoTimer) : delta;
                Stock = Mathf.Min(Capacity, Stock + Production * step);
                delta -= step;
                if (!AutoEnabled) continue;
                autoTimer += step; autoCredit += Production * step;
                if (autoTimer >= .99999f)
                {
                    autoTimer = 0;
                    int quota = Mathf.FloorToInt(autoCredit + .00001f);
                    autoCredit -= quota;
                    Transfer(Mathf.Min(Mathf.FloorToInt(Stock + .00001f), quota));
                }
            }
        }
        // BEGIN ADDED: Mature whole batches and pause at capacity, independently of the tick subdivision.
        void TickBatches(float delta)
        {
            if (!MainTreeStarted || Stock >= Capacity) { batchElapsed = 0; return; }
            batchElapsed += delta;
            double batches = Math.Floor((batchElapsed + 1e-7) / ProductionInterval);
            if (batches < 1) return;
            Stock = (float)Math.Min(Capacity, Stock + batches * BatchSize);
            batchElapsed = Stock >= Capacity ? 0 : Math.Max(0, batchElapsed - batches * ProductionInterval);
            Changed?.Invoke();
        }
        // END ADDED
        void Transfer(int amount)
        {
            if (GameEnded || amount <= 0) return;
            Stock = Mathf.Max(0, Stock - amount);
            CollectWorld(amount);
        }
        // World pickups and tree transfers count physical pieces, before their sale value.
        public void CollectWorld(int amount)
        {
            if (GameEnded || amount <= 0) return;
            TotalCollected += amount;
            Collected?.Invoke();
            Cheeses = (int)Math.Min(1000000000L, (long)Cheeses + (long)amount * UnitPrice);
            CheckLetters(); Changed?.Invoke();
        }
        public int Collect()
        {
            if (GameEnded) return 0;
            int amount = Mathf.FloorToInt(Stock);
            if (amount <= 0) return 0;
            Transfer(amount); HarvestCount++; Changed?.Invoke(); return amount;
        }
        public void Grant(int amount)
        {
            if (GameEnded) return;
            Cheeses = (int)Math.Min(1000000000L, (long)Cheeses + Mathf.Max(0, amount));
            // Wallet grants obey the same narrative thresholds as pickups in the current profile.
            if (config.useBatchProduction) { CheckLetters(); Changed?.Invoke(); }
        }
        public bool ClaimWelcome()
        { if (WelcomeClaimed) return false; WelcomeClaimed = true; Grant(40); Changed?.Invoke(); return true; }
        public bool ClaimTask()
        { if (!TaskReady || TaskClaimed) return false; TaskClaimed = true; Grant(80); Changed?.Invoke(); return true; }
    }
}
