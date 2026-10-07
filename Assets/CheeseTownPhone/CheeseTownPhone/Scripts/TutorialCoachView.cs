using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // BEGIN CHANGED: Four shades leave a transparent hole; clicks advance without activating the highlighted button.
    public sealed class TutorialCoachView : MonoBehaviour
    {
        public Text heading, body;
        public Button next, skip;
        public RectTransform[] shades, focusEdges;
        [Range(0, 1)] public float shadeOpacity = .75f;
        [Min(0)] public float focusPadding = 3;
        public string[] titles;
        [TextArea(1, 2)] public string[] instructions;
        readonly Vector3[] corners = new Vector3[4];
        TutorialShadeMesh shadeMesh;

        public void Bind(Font pixelFont, UnityAction advance, UnityAction dismiss)
        {
            next.onClick.AddListener(advance); skip.onClick.AddListener(dismiss);
            if (next.GetComponent<UIButtonAudio>() == null) next.gameObject.AddComponent<UIButtonAudio>();
            if (skip.GetComponent<UIButtonAudio>() == null) skip.gameObject.AddComponent<UIButtonAudio>();
            foreach (var label in GetComponentsInChildren<Text>(true))
            {
                label.font = pixelFont; label.fontStyle = FontStyle.Normal;
                UpgradeRowView.Sharpen(label.font);
                var shadow = label.GetComponent<Shadow>();
                if (shadow == null) shadow = label.gameObject.AddComponent<Shadow>();
                shadow.effectColor = Color.black; shadow.effectDistance = new Vector2(1, -1);
            }
        }
        public void Present(int step, bool canAdvance, string unusedHint, params RectTransform[] targets)
        {
            body.text = instructions[step];
            heading.text = "CLICK TO CONTINUE";
            next.interactable = canAdvance;
            var bounds = ((RectTransform)transform).rect;
            bool focused = false;
            Vector2 min = new Vector2(float.PositiveInfinity, float.PositiveInfinity);
            Vector2 max = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
            if (targets != null) foreach (var target in targets)
            {
                if (target == null || !target.gameObject.activeInHierarchy) continue;
                target.GetWorldCorners(corners);
                min = Vector2.Min(min, transform.InverseTransformPoint(corners[0]));
                max = Vector2.Max(max, transform.InverseTransformPoint(corners[2]));
                focused = true;
            }
            if (!focused) min = max = bounds.center;
            if (focused)
            {
                min = new Vector2(Mathf.Clamp(Mathf.Floor(min.x) - focusPadding, bounds.xMin, bounds.xMax),
                    Mathf.Clamp(Mathf.Floor(min.y) - focusPadding, bounds.yMin, bounds.yMax));
                max = new Vector2(Mathf.Clamp(Mathf.Ceil(max.x) + focusPadding, min.x, bounds.xMax),
                    Mathf.Clamp(Mathf.Ceil(max.y) + focusPadding, min.y, bounds.yMax));
            }
            // Bleed only the outside edges; shared edges stay identical to avoid double shading.
            var canvas = GetComponentInParent<Canvas>();
            float bleed = 2 / Mathf.Max(.01f, canvas != null ? canvas.scaleFactor : 1);
            var coverage = Rect.MinMaxRect(bounds.xMin - bleed, bounds.yMin - bleed,
                bounds.xMax + bleed, bounds.yMax + bleed);
            Place(shades[0], coverage.min, new Vector2(min.x - coverage.xMin, coverage.height));
            Place(shades[1], new Vector2(max.x, coverage.yMin), new Vector2(coverage.xMax - max.x, coverage.height));
            Place(shades[2], new Vector2(min.x, coverage.yMin), new Vector2(max.x - min.x, min.y - coverage.yMin));
            Place(shades[3], new Vector2(min.x, max.y), new Vector2(max.x - min.x, coverage.yMax - max.y));
            // Retain the authored rectangles for inspection; draw their shared edges only once.
            foreach (var shade in shades) shade.GetComponent<Image>().enabled = false;
            if (shadeMesh == null) shadeMesh = TutorialShadeMesh.Create((RectTransform)transform);
            shadeMesh.SetCoverage(coverage, Rect.MinMaxRect(min.x, min.y, max.x, max.y),
                new Color(.22f, .22f, .22f, shadeOpacity));
            foreach (var edge in focusEdges) edge.gameObject.SetActive(focused);
            if (focused)
            {
                Place(focusEdges[0], min, new Vector2(1, max.y - min.y));
                Place(focusEdges[1], new Vector2(max.x - 1, min.y), new Vector2(1, max.y - min.y));
                Place(focusEdges[2], min, new Vector2(max.x - min.x, 1));
                Place(focusEdges[3], new Vector2(min.x, max.y - 1), new Vector2(max.x - min.x, 1));
            }
            // Use one lower-middle caption band for every step, above tablet footer controls.
            float y = Mathf.Round(bounds.center.y - 72);
            Place(body.rectTransform, new Vector2(bounds.xMin + 16, y), new Vector2(bounds.width - 32, 28));
            Place(heading.rectTransform, new Vector2(bounds.xMin + 16, y - 16), new Vector2(bounds.width - 32, 12));
        }
        static void Place(RectTransform rect, Vector2 position, Vector2 size)
        {
            rect.anchorMin = rect.anchorMax = new Vector2(.5f, .5f); rect.pivot = Vector2.zero;
            rect.anchoredPosition = position; rect.sizeDelta = size;
        }
    }
    // END CHANGED
}
