using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheeseTownPhone.Editor
{
    // A saturated clear color makes uncovered UI pixels measurable, including one-pixel edges.
    public static class UICoverageChecks
    {
        static Camera camera;
        static RenderTexture target;
        static int width, height, checks;
        static void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); checks++; }
        public static void RunBatch()
        {
            try
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                camera = new GameObject("Coverage camera", typeof(Camera)).GetComponent<Camera>();
                camera.transform.position = new Vector3(0, 0, -10);
                camera.orthographic = true; camera.orthographicSize = 5;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.magenta;
                camera.GetUniversalAdditionalCameraData().renderPostProcessing = false;
                var settings = Resources.Load<TabletSettings>("TabletSettings");
                foreach (var size in new[] { new Vector2Int(1280,720), new Vector2Int(1920,1080),
                    new Vector2Int(2560,1277), new Vector2Int(1365,767), new Vector2Int(1023,769),
                    new Vector2Int(641,361), new Vector2Int(479,271) })
                {
                    width = size.x; height = size.y;
                    Debug.Log("UI_COVERAGE_SIZE_START: " + width + "x" + height);
                    target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
                    Story(settings.prologuePrefab, "opening"); Story(settings.epiloguePrefab, "ending");
                    Tablet(settings);
                    camera.targetTexture = null; target.Release(); Object.DestroyImmediate(target);
                }
                Debug.Log("UI_COVERAGE_PASS: " + checks + " checks; seven viewport sizes; opening/ending chapters and fades; tablet pages; ending shade; tutorial shade seams.");
                EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogError("UI_COVERAGE_FAIL: " + error); EditorApplication.Exit(1); }
        }
        static void Configure(Canvas canvas, bool story)
        {
            var scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null) scaler.enabled = false;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
            canvas.sortingLayerName = "Foreground"; canvas.sortingOrder = 32760;
            canvas.scaleFactor = story ? Mathf.Sqrt(width / 640f * height / 360f) : CheeseTownDemo.PixelScaleFor(width, height);
            Canvas.ForceUpdateCanvases(); Render(); Canvas.ForceUpdateCanvases();
        }
        static void Render() => RenderPipeline.SubmitRenderRequest(camera,
            new UniversalRenderPipeline.SingleCameraRequest { destination = target });
        static Color32[] Pixels(string name)
        {
            Canvas.ForceUpdateCanvases(); Render();
            var previous = RenderTexture.active; RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            if (width == 1280 || width == 1365 || width == 2560)
            {
                Directory.CreateDirectory("Logs/UICoverage");
                File.WriteAllBytes("Logs/UICoverage/" + name + "-" + width + "x" + height + ".png", image.EncodeToPNG());
            }
            var pixels = image.GetPixels32(); Object.DestroyImmediate(image); RenderTexture.active = previous;
            return pixels;
        }
        static bool IsClear(Color32 c) => c.r > 245 && c.g < 8 && c.b > 245;
        static void Border(Color32[] pixels, string name, bool black = false)
        {
            int uncovered = 0;
            for (int y = 0; y < height; y++) for (int x = 0; x < width; x++)
            {
                if (x > 1 && x < width - 2 && y > 1 && y < height - 2) continue;
                var c = pixels[y * width + x];
                if (black ? c.r > 2 || c.g > 2 || c.b > 2 : IsClear(c)) uncovered++;
            }
            Check(uncovered == 0, name + " exposed " + uncovered + " border pixels at " + width + "x" + height);
        }
        static void Story(PrologueView prefab, string name)
        {
            var view = Object.Instantiate(prefab);
            try
            {
                var canvas = view.GetComponent<Canvas>(); Configure(canvas, true);
                view.Begin(null); view.RefreshCoverage(); Canvas.ForceUpdateCanvases();
                Border(Pixels(name + "-fade"), name + " black backdrop", true);
                view.Step(10);
                for (int chapter = 0; chapter < view.chapters.Length; chapter++)
                {
                    view.RefreshCoverage(); var pixels = Pixels(name + "-chapter-" + chapter); Border(pixels, name);
                    int visible = 0;
                    foreach (var c in pixels) if (c.r > 20 || c.g > 20 || c.b > 20) visible++;
                    Check(visible > width * height / 4, name + " artwork did not render");
                    if (chapter + 1 < view.chapters.Length) { view.Step(10); view.Advance(); view.Step(10); view.Step(10); }
                }
            }
            finally { Object.DestroyImmediate(view.gameObject); }
        }
        static void Tablet(TabletSettings settings)
        {
            var root = new GameObject("Coverage UI", typeof(RectTransform), typeof(Canvas));
            var canvas = root.GetComponent<Canvas>(); canvas.pixelPerfect = true; Configure(canvas, false);
            var view = Object.Instantiate(settings.tabletPrefab, root.transform);
            var rect = (RectTransform)view.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.one * .5f; rect.anchoredPosition = Vector2.zero;
            try
            {
                view.SetOpen(true, false);
                for (int page = 0; page < 3; page++)
                {
                    view.SetPage(page, false); view.PositionHud((RectTransform)root.transform);
                    var pixels = Pixels("tablet-page-" + page);
                    // The center of the wood enclosure must be opaque on every settled page.
                    Rect bounds = view.panel.rect;
                    var low = RectTransformUtility.WorldToScreenPoint(camera, view.panel.TransformPoint(new Vector3(bounds.xMin+31,bounds.yMin+21,0)));
                    var high = RectTransformUtility.WorldToScreenPoint(camera, view.panel.TransformPoint(new Vector3(bounds.xMax-31,bounds.yMax-69,0)));
                    int leaked = 0;
                    for (int y = Mathf.Max(0,Mathf.CeilToInt(low.y)); y < Mathf.Min(height,Mathf.FloorToInt(high.y)); y++)
                        for (int x = Mathf.Max(0,Mathf.CeilToInt(low.x)); x < Mathf.Min(width,Mathf.FloorToInt(high.x)); x++)
                            if (IsClear(pixels[y * width + x])) leaked++;
                    Check(leaked == 0, "Tablet page " + page + " interior leak: " + leaked);
                }
                view.ShowEnding(); view.PositionHud((RectTransform)root.transform);
                Border(Pixels("ending-choice"), "Ending shade");
                view.gameObject.SetActive(false);
                var coach = Object.Instantiate(settings.tutorialCoachPrefab, root.transform);
                coach.gameObject.SetActive(true);
                var coachRect = (RectTransform)coach.transform;
                coachRect.anchorMin = Vector2.zero; coachRect.anchorMax = Vector2.one; coachRect.offsetMin = coachRect.offsetMax = Vector2.zero;
                Canvas.ForceUpdateCanvases(); coach.Present(0, true, null);
                var shades = Pixels("tutorial-no-focus");
                var shadeMesh = coach.GetComponentInChildren<TutorialShadeMesh>();
                var geometry = shadeMesh.canvasRenderer.GetMesh();
                Debug.Log("UI_SHADE_GEOMETRY: root=" + coachRect.rect + " mesh=" + shadeMesh.rectTransform.rect +
                    " vertices=" + geometry.vertexCount + " culled=" + shadeMesh.canvasRenderer.cull + " color=" + shadeMesh.color);
                Border(shades, "Tutorial shade");
                // With no focus target there must be neither a bright crack nor double shade at the joins.
                Color32 expected = shades[width * 10 + 10];
                Check(expected.r > 20 && expected.r < 240, "Tutorial shade did not render over the clear color");
                for (int x = 0; x < width; x++)
                {
                    var c = shades[(height / 2) * width + x];
                    Check(Mathf.Abs(c.r-expected.r) <= 2 && Mathf.Abs(c.g-expected.g) <= 2 && Mathf.Abs(c.b-expected.b) <= 2,
                        "Tutorial shade join differs at x=" + x);
                }
                var focus = new GameObject("Fractional spotlight target", typeof(RectTransform)).GetComponent<RectTransform>();
                focus.SetParent(root.transform, false); focus.anchorMin = focus.anchorMax = Vector2.one * .5f;
                focus.anchoredPosition = new Vector2(-70.35f, ((RectTransform)root.transform).rect.height * .5f - 70.2f);
                focus.sizeDelta = new Vector2(101.3f, 37.7f);
                coach.Present(3, true, null, focus);
                shades = Pixels("tutorial-focused"); Border(shades, "Focused tutorial shade");
                expected = shades[width * 10 + 10];
                // The two vertical joins extend below this top-aligned spotlight.
                for (int x = 0; x < width; x++)
                {
                    var c = shades[(height / 2) * width + x];
                    Check(Mathf.Abs(c.r-expected.r) <= 2 && Mathf.Abs(c.g-expected.g) <= 2 && Mathf.Abs(c.b-expected.b) <= 2,
                        "Focused tutorial shade join differs at x=" + x);
                }
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
