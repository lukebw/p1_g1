// BEGIN ADDED: A finite, baked world uses one boundary for movement, camera and edge feedback.
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTown.World
{
    public sealed class WorldBoundary : MonoBehaviour
    {
        [Tooltip("Local XY coordinates. Keep the world root unrotated and at unit scale.")]
        public Rect terrain = new Rect(-128, -128, 256, 256);
        // Bottom-facing canopies extend into the playfield farther than their trunk pivots.
        public Rect walkable = new Rect(-112, -106, 224, 222);
        [Min(0)] public float playerPadding = .8f;
        [Min(.1f)] public float darkenDistance = 14;
        [Range(0, .8f)] public float maximumDarkness = .42f;
        [Min(.01f)] public float fadeSpeed = 1.2f;
        public Image darkenOverlay;
        public Transform walls;
        Transform player;

        public Vector3 ConstrainPosition(Vector3 desired)
        {
            Vector3 local = transform.InverseTransformPoint(desired);
            local.x = ClampAxis(local.x, walkable.xMin, walkable.xMax, playerPadding);
            local.y = ClampAxis(local.y, walkable.yMin, walkable.yMax, playerPadding);
            return transform.TransformPoint(local);
        }

        public Vector3 ConstrainCamera(Vector3 desired, float halfHeight, float aspect)
        {
            Vector3 local = transform.InverseTransformPoint(desired);
            local.x = ClampAxis(local.x, terrain.xMin, terrain.xMax, halfHeight * aspect);
            local.y = ClampAxis(local.y, terrain.yMin, terrain.yMax, halfHeight);
            return transform.TransformPoint(local);
        }

        static float ClampAxis(float value, float min, float max, float inset)
            => max - min <= inset * 2 ? (min + max) * .5f : Mathf.Clamp(value, min + inset, max - inset);

        public float DarknessAt(Vector3 position)
        {
            Vector3 p = transform.InverseTransformPoint(position);
            float distance = Mathf.Min(p.x - walkable.xMin, walkable.xMax - p.x,
                p.y - walkable.yMin, walkable.yMax - p.y) - playerPadding;
            float t = 1 - Mathf.Clamp01(distance / Mathf.Max(.1f, darkenDistance));
            return Mathf.SmoothStep(0, maximumDarkness, t);
        }

        void Start()
        {
            var controller = FindAnyObjectByType<PlayerController>();
            if (controller != null) player = controller.transform;
        }

        void LateUpdate()
        {
            if (darkenOverlay == null) return;
            float target = player != null ? DarknessAt(player.position) : 0;
            float alpha = Mathf.MoveTowards(darkenOverlay.color.a, target, fadeSpeed * Time.unscaledDeltaTime);
            darkenOverlay.color = new Color(0, 0, 0, alpha);
        }

        void OnDisable()
        {
            if (darkenOverlay != null) darkenOverlay.color = Color.clear;
        }

        void OnDrawGizmosSelected()
        {
            var previous = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.cyan; Gizmos.DrawWireCube(terrain.center, terrain.size);
            Gizmos.color = Color.yellow; Gizmos.DrawWireCube(walkable.center, walkable.size);
            Gizmos.matrix = previous;
        }
    }
}
// END ADDED
