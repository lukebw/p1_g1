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
        public bool MailStopped => TotalCollected >= 500;
        public const string FinalLetterId = "all-max";
        public bool GameEnded { get; private set; }
        public int UnreadCount => letters.FindAll(l => !l.IsRead).Count;
        public void ReadLetter(int index)
        {
            if (index < 0 || index >= letters.Count || letters[index].IsRead) return;
            letters[index].IsRead = true;
            if (letters[index].Id == FinalLetterId) GameEnded = true;
            Changed?.Invoke();
        }
        void SendLetter(string id, string body)
        {
            if (sentLetters.Add(id)) letters.Add(new Letter(id, body));
        }
        void CheckLetters()
        {
            if (TotalCollected >= 10)
                SendLetter("shop", "Your first 10 cheeses!\n\nVisit UPGRADES to improve your movement, collection range and cheese tree.\n\nMayor Ellis");
            if (TotalCollected >= 100)
                SendLetter("reserve", "Our reserves are sufficient.\n\nThank you for collecting 100 cheeses. You can keep playing and improving the town.\n\nMayor Ellis");
            if (TotalCollected >= 350)
                SendLetter("stop-request", "Please stop collecting.\n\nYou have collected 350 cheeses. We have more than enough for the town; please give the tree a rest.\n\nMayor Ellis");
            // No further ordinary letters after 500; upgrade completion remains independent.
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
        public int Cheeses { get; private set; }
        public float Stock { get; private set; }
        public int HarvestCount { get; private set; }
        public bool WelcomeClaimed { get; private set; }
        public bool TaskClaimed { get; private set; }
        public bool TaskReady => WelcomeClaimed && HarvestCount >= 3;
        public float MoveSpeed { get; private set; }
        public float CollectRange { get; private set; }
        public float TreeScale { get; private set; }
        public float Production { get; private set; }
        public bool AutoEnabled { get; private set; }
        public float AutoRate => AutoEnabled ? Production : 0;
        public float ValueMultiplier { get; private set; }
        public int UnitPrice => Mathf.Clamp(Mathf.RoundToInt(config.baseCheesePrice * ValueMultiplier), 1, 1000000);
        public int Capacity => config.treeCapacity;
        public event Action Changed;

        public TownProgress(TabletSettings settings)
        {
            config = settings;
            config.ValidateSettings();
            Cheeses = config.startingCheeses;
            Stock = config.initialTreeStock;
            SendLetter("welcome", "Welcome to Cheese Town! I'm Mayor Ellis.\n\nWe're happy to have you here. Use your axe on a cheese tree, then walk over the dropped cheese to collect it. Gather 10 cheeses, and I'll write again.\n\nUse your tablet to read letters and visit UPGRADES. Enjoy your first day!\n\nMayor Ellis");
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
            Stock = Mathf.Min(Stock, Capacity);
            Recalculate(); CheckLetters(); Changed?.Invoke();
        }
        public int Level(UpgradeOption o) => o != null && purchases.TryGetValue(o.id, out int value) ? Mathf.Min(value, o.levels.Count) : 0;
        public bool CanBuy(UpgradeOption o) => !GameEnded && o != null && o.available && config.upgrades.Contains(o) && Level(o) < o.levels.Count && Cheeses >= o.levels[Level(o)].price;
        public bool Buy(UpgradeOption o)
        {
            if (!CanBuy(o)) return false;
            int current = Level(o);
            Cheeses -= o.levels[current].price; purchases[o.id] = current + 1;
            Recalculate(); CheckLetters(); Changed?.Invoke(); return true;
        }
        void Recalculate()
        {
            MoveSpeed = config.baseMoveSpeed; CollectRange = config.baseCollectRange;
            TreeScale = 1; Production = config.baseTreeProduction; AutoEnabled = false; ValueMultiplier = 1;
            foreach (var o in config.upgrades)
            {
                if (o == null || !o.available || Level(o) == 0) continue;
                var v = o.levels[Level(o) - 1];
                // Same-effect options use the strongest purchased value; no accidental stacking.
                switch (o.effect)
                {
                    case UpgradeEffect.MoveSpeed: MoveSpeed = Mathf.Max(MoveSpeed, v.value); break;
                    case UpgradeEffect.CollectRange: CollectRange = Mathf.Max(CollectRange, v.value); break;
                    case UpgradeEffect.TreeGrowth: Production = Mathf.Max(Production, v.value); TreeScale = Mathf.Max(TreeScale, v.treeScale); break;
                    case UpgradeEffect.AutoCollect: AutoEnabled = true; break;
                    case UpgradeEffect.CheeseValue: ValueMultiplier = Mathf.Max(ValueMultiplier, v.value); break;
                }
            }
            if (!AutoEnabled) { autoTimer = 0; autoCredit = 0; }
        }
        public void Tick(float delta)
        {
            if (GameEnded || delta <= 0 || float.IsNaN(delta) || float.IsInfinity(delta)) return;
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
        public void Grant(int amount) { Cheeses = (int)Math.Min(1000000000L, (long)Cheeses + Mathf.Max(0, amount)); }
        public bool ClaimWelcome()
        { if (WelcomeClaimed) return false; WelcomeClaimed = true; Grant(40); Changed?.Invoke(); return true; }
        public bool ClaimTask()
        { if (!TaskReady || TaskClaimed) return false; TaskClaimed = true; Grant(80); Changed?.Invoke(); return true; }
    }
}
