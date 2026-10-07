using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone
{
    // Keeps the authored 16:9 stage aligned with the opening and tutorial canvases.
    public sealed class PixelAspectViewport : MonoBehaviour
    {
        Canvas canvas;
        RectTransform stage;
        Vector2 authoredSize;
        bool integerScale;
        Image[] bars;
        int lastWidth = -1;
        int lastHeight = -1;
        float lastScale = -1;

        public void Configure(Canvas targetCanvas, RectTransform targetStage, Vector2 targetSize, bool useIntegerScale)
        {
            canvas = targetCanvas;
            stage = targetStage;
            authoredSize = targetSize;
            integerScale = useIntegerScale;
            EnsureBars();
            Refresh(true);
        }

        public void Refresh(bool force = false)
        {
            if (canvas == null || stage == null || authoredSize.x <= 0 || authoredSize.y <= 0) return;
            int width = Mathf.Max(1, Screen.width);
            int height = Mathf.Max(1, Screen.height);
            float fit = Mathf.Min(width / authoredSize.x, height / authoredSize.y);
            float scale = integerScale && fit >= 1 ? Mathf.Floor(fit) : Mathf.Max(.01f, fit);
            if (!force && width == lastWidth && height == lastHeight && Mathf.Approximately(scale, lastScale)) return;
            lastWidth = width;
            lastHeight = height;
            lastScale = scale;

            float viewportWidth = Mathf.Clamp01(authoredSize.x * scale / width);
            float viewportHeight = Mathf.Clamp01(authoredSize.y * scale / height);
            float left = (1f - viewportWidth) * .5f;
            float bottom = (1f - viewportHeight) * .5f;
            Rect viewport = new Rect(left, bottom, viewportWidth, viewportHeight);
            SetBars(viewport);

            Camera view = Camera.main;
            if (view != null && view.targetTexture == null) view.rect = viewport;
        }

        void EnsureBars()
        {
            if (bars != null) return;
            bars = new Image[4];
            for (int i = 0; i < bars.Length; i++)
            {
                var bar = new GameObject("Pixel letterbox " + i, typeof(RectTransform), typeof(Image));
                bar.transform.SetParent(canvas.transform, false);
                var image = bar.GetComponent<Image>();
                image.color = Color.black;
                image.raycastTarget = false;
                bars[i] = image;
                bar.transform.SetAsFirstSibling();
            }
        }

        void SetBars(Rect viewport)
        {
            SetBar(bars[0].rectTransform, new Vector2(0, 0), new Vector2(viewport.xMin, 1));
            SetBar(bars[1].rectTransform, new Vector2(viewport.xMax, 0), new Vector2(1, 1));
            SetBar(bars[2].rectTransform, new Vector2(viewport.xMin, 0), new Vector2(viewport.xMax, viewport.yMin));
            SetBar(bars[3].rectTransform, new Vector2(viewport.xMin, viewport.yMax), new Vector2(viewport.xMax, 1));
        }

        static void SetBar(RectTransform bar, Vector2 min, Vector2 max)
        {
            bar.anchorMin = min;
            bar.anchorMax = max;
            bar.offsetMin = Vector2.zero;
            bar.offsetMax = Vector2.zero;
        }
    }
}
