// BEGIN ADDED: Editor-only placement settings; entering Play never generates more objects.
using UnityEngine;

namespace CheeseTown.World
{
    public sealed class LargeMapAuthoring : MonoBehaviour
    {
        public WorldBoundary boundary;
        public Transform terrainBlocks, fixedTrees, generatedTrees, borderForest;
        public GameObject borderTreePrefab;
        public Tree ordinaryTree, richTree;
        public int seed = 511004;
        [Min(0)] public int generatedTreeCount = 280;
        [Min(1)] public float minimumSpacing = 8;
        [Range(0, 1)] public float richTreeChance = .12f;
        [Min(0)] public float harvestTreeEdgeInset = 7;
        [Min(1)] public float forestSpacing = 4.4f;
        public Rect startClearing = new Rect(-4, -4, 8, 8);
    }
}
// END ADDED
