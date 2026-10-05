// BEGIN ADDED: Baked trees remain normal prefab instances; this component never spawns on Play.
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CheeseTown.World
{
    public enum MapTreePlacement { ManualOnly, AreasAndManual }

    [Serializable]
    public sealed class MapTreeChoice
    {
        public Tree prefab;
        [Min(0)] public float weight = 1;
    }

    public sealed class MapBlock : MonoBehaviour
    {
        public string blockId;
        public Vector2 size = new Vector2(51.2f, 51.2f);
        public SpriteRenderer ground;
        public Transform authoredTrees, generatedTrees, regions, decorations;
        [Header("Editor generation - existing fixed trees count toward the target")]
        public MapTreePlacement placement = MapTreePlacement.AreasAndManual;
        public int seed = 5110;
        [Range(0, 128)] public int targetTreeCount = 10;
        [Min(.1f)] public float minimumSpacing = 8;
        [Min(0)] public float edgeMargin = 8;
        [Min(0)] public float exclusionPadding = 1.5f;
        [Min(.001f)] public float positionStep = .04f;
        public List<MapTreeChoice> treePalette = new List<MapTreeChoice>();

        public bool Allows(Vector2 localPoint)
        {
            if (localPoint.x < edgeMargin || localPoint.y < edgeMargin
                || localPoint.x > size.x - edgeMargin || localPoint.y > size.y - edgeMargin) return false;
            bool allowed = false;
            Vector3 world = transform.TransformPoint(localPoint);
            if (regions == null) return false;
            foreach (var area in regions.GetComponentsInChildren<MapTreeArea>())
            {
                if (!area.enabled) continue;
                if (area.kind == TreeAreaKind.Exclude && area.Contains(world, exclusionPadding)) return false;
                if (area.kind == TreeAreaKind.Allow && area.Contains(world)) allowed = true;
            }
            return allowed;
        }
        void OnDrawGizmosSelected()
        {
            var old = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(size * .5f, new Vector3(size.x, size.y, .01f));
            Gizmos.matrix = old;
        }
    }

    public readonly struct MapTreePlacementPoint
    {
        public readonly Tree Prefab;
        public readonly Vector2 Position;
        public MapTreePlacementPoint(Tree prefab, Vector2 position) { Prefab = prefab; Position = position; }
    }

    // Planning is side-effect free, deterministic, bounded, and independent of Unity's gameplay RNG.
    public static class MapTreeLayout
    {
        public static List<MapTreePlacementPoint> Plan(MapBlock block, IEnumerable<Vector3> neighbors = null)
        {
            var result = new List<MapTreePlacementPoint>();
            if (block.placement == MapTreePlacement.ManualOnly) return result;
            if (block.size.x <= 0 || block.size.y <= 0 || block.positionStep <= 0 || block.minimumSpacing <= 0)
                throw new InvalidOperationException("Map dimensions, spacing and pixel step must be positive.");
            var occupied = new List<Vector3>();
            if (block.authoredTrees != null)
                foreach (var tree in block.authoredTrees.GetComponentsInChildren<Tree>(true)) occupied.Add(tree.transform.position);
            int desired = Mathf.Max(0, Mathf.Clamp(block.targetTreeCount, 0, 128) - occupied.Count);
            if (neighbors != null) occupied.AddRange(neighbors);
            double totalWeight = 0;
            foreach (var choice in block.treePalette)
                if (choice != null && choice.prefab != null && choice.weight > 0 && !float.IsInfinity(choice.weight)) totalWeight += choice.weight;
            if (desired == 0) return result;
            if (totalWeight <= 0) throw new InvalidOperationException("Assign at least one weighted tree prefab.");
            var random = new System.Random(block.seed);
            float spacingSquared = block.minimumSpacing * block.minimumSpacing;
            for (int attempt = 0; attempt < desired * 200 && result.Count < desired; attempt++)
            {
                Vector2 point = new Vector2((float)random.NextDouble() * block.size.x, (float)random.NextDouble() * block.size.y);
                point.x = Mathf.Round(point.x / block.positionStep) * block.positionStep;
                point.y = Mathf.Round(point.y / block.positionStep) * block.positionStep;
                if (!block.Allows(point)) continue;
                Vector3 world = block.transform.TransformPoint(point);
                bool blocked = false;
                foreach (var other in occupied)
                    if (((Vector2)(world - other)).sqrMagnitude < spacingSquared) { blocked = true; break; }
                if (blocked) continue;
                double pick = random.NextDouble() * totalWeight;
                Tree selected = null;
                foreach (var choice in block.treePalette)
                {
                    if (choice == null || choice.prefab == null || choice.weight <= 0 || float.IsNaN(choice.weight) || float.IsInfinity(choice.weight)) continue;
                    selected = choice.prefab; pick -= choice.weight; if (pick < 0) break;
                }
                result.Add(new MapTreePlacementPoint(selected, point)); occupied.Add(world);
            }
            return result;
        }
    }
}
// END ADDED
