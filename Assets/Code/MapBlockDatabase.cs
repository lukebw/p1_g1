// BEGIN ADDED: A Unity asset serves as the reusable library of complete authored map blocks.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTown.World
{
    [Serializable]
    public sealed class MapBlockEntry
    {
        public MapBlock prefab;
        [Min(0)] public float weight = 1;
    }
    [CreateAssetMenu(menuName = "Cheese Town/Map Block Database")]
    public sealed class MapBlockDatabase : ScriptableObject
    {
        public List<MapBlockEntry> entries = new List<MapBlockEntry>();

        public MapBlock Pick(System.Random random, string leftId = null, string belowId = null)
        {
            var candidates = entries.FindAll(e => e != null && e.prefab != null && e.weight > 0
                && !float.IsInfinity(e.weight));
            if (candidates.Count == 0) throw new InvalidOperationException("The map database has no enabled blocks.");
            var distinct = candidates.FindAll(e => e.prefab.blockId != leftId && e.prefab.blockId != belowId);
            if (distinct.Count > 0) candidates = distinct;
            double sum = 0; foreach (var entry in candidates) sum += entry.weight;
            double pick = random.NextDouble() * sum;
            foreach (var entry in candidates) { pick -= entry.weight; if (pick < 0) return entry.prefab; }
            return candidates[candidates.Count - 1].prefab;
        }
    }
}
// END ADDED
