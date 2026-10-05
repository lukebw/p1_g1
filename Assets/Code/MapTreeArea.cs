// BEGIN ADDED: Artist-authored regions guide editor generation without runtime colliders.
using UnityEngine;

namespace CheeseTown.World
{
    public enum TreeAreaKind { Allow, Exclude }

    public sealed class MapTreeArea : MonoBehaviour
    {
        public TreeAreaKind kind;
        public Rect rectangle = new Rect(-10, -10, 20, 20);

        public bool Contains(Vector3 worldPoint, float padding = 0)
        {
            Vector3 local = transform.InverseTransformPoint(worldPoint);
            var bounds = new Rect(rectangle.xMin - padding, rectangle.yMin - padding,
                rectangle.width + padding * 2, rectangle.height + padding * 2);
            return bounds.Contains(new Vector2(local.x, local.y));
        }
        void OnDrawGizmosSelected()
        {
            var old = Gizmos.matrix; Gizmos.matrix = transform.localToWorldMatrix;
            Color color = kind == TreeAreaKind.Allow ? Color.green : Color.red;
            Gizmos.color = new Color(color.r, color.g, color.b, .12f);
            Gizmos.DrawCube(rectangle.center, new Vector3(rectangle.width, rectangle.height, .01f));
            Gizmos.color = color; Gizmos.DrawWireCube(rectangle.center, new Vector3(rectangle.width, rectangle.height, .01f));
            Gizmos.matrix = old;
        }
    }
}
// END ADDED
