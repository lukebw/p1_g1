using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // One mesh shares exact edges around the spotlight. Separate Images can round
    // their positions and sizes differently on a pixel-perfect Canvas.
    [ExecuteAlways]
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class TutorialShadeMesh : MaskableGraphic
    {
        Rect coverage, opening;
        public static TutorialShadeMesh Create(RectTransform parent)
        {
            var shade = new GameObject("Seamless tutorial shade", typeof(RectTransform), typeof(CanvasRenderer), typeof(TutorialShadeMesh))
                .GetComponent<TutorialShadeMesh>();
            var rect = shade.rectTransform;
            rect.SetParent(parent, false); rect.SetAsFirstSibling();
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.pivot = parent.pivot; rect.offsetMin = rect.offsetMax = Vector2.zero;
            shade.raycastTarget = false;
            return shade;
        }
        public void SetCoverage(Rect outer, Rect hole, Color tint)
        {
            if (coverage == outer && opening == hole && color == tint) return;
            coverage = outer; opening = hole; color = tint; SetVerticesDirty();
        }
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear();
            Quad(mesh, coverage.xMin, coverage.yMin, opening.xMin, coverage.yMax);
            Quad(mesh, opening.xMax, coverage.yMin, coverage.xMax, coverage.yMax);
            Quad(mesh, opening.xMin, coverage.yMin, opening.xMax, opening.yMin);
            Quad(mesh, opening.xMin, opening.yMax, opening.xMax, coverage.yMax);
        }
        void Quad(VertexHelper mesh, float left, float bottom, float right, float top)
        {
            if (right <= left || top <= bottom) return;
            int first = mesh.currentVertCount;
            mesh.AddVert(new Vector3(left, bottom), color, Vector2.zero);
            mesh.AddVert(new Vector3(left, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, top), color, Vector2.zero);
            mesh.AddVert(new Vector3(right, bottom), color, Vector2.zero);
            mesh.AddTriangle(first, first+1, first+2); mesh.AddTriangle(first, first+2, first+3);
        }
    }
}
