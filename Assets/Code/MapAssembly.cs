// BEGIN ADDED: Editor-baked block selection preserves hand-edited content during gameplay.
using UnityEngine;

namespace CheeseTown.World
{
    public sealed class MapAssembly : MonoBehaviour
    {
        public MapBlockDatabase database;
        [Range(1, 12)] public int columns = 3, rows = 3;
        public int seed = 5110;
        public Transform generatedBlocks;
    }
}
// END ADDED
