// BEGIN ADDED: Validate the actual scene, cross-seam layout and transform-based boundary movement.
using System;
using System.IO;
using System.Linq;
using CheeseTown.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class LargeWorldChecks
{
    const string Key = "LargeWorldChecks.Active";
    static int frames, phase, count;
    static double next;
    static LargeMapAuthoring map;
    static PlayerController player;
    static Vector3 start;
    static LargeWorldChecks() { EditorApplication.update += Tick; }
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

    [MenuItem("Cheese Town/Maps/Check Large Wilderness")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before running checks.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            EditorSceneManager.OpenScene(LargeWorldBuilder.ScenePath);
            Validate();
            SessionState.SetBool(Key + ".FastPlay", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetFloat(Key + ".Time", (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Finish(error); }
    }

    static void Validate()
    {
        map = Object.FindAnyObjectByType<LargeMapAuthoring>(); Check(map != null, "Scene contains large map prefab");
        Check(PrefabUtility.IsPartOfPrefabInstance(map), "Scene retains editable world prefab connection");
        var blocks = map.terrainBlocks.GetComponentsInChildren<MapBlock>();
        Check(blocks.Length == 25 && blocks.Select(b => b.blockId).Distinct().Count() == 7, "All seven designs appear in 25 ground blocks");
        for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
        {
            int i = y * 5 + x;
            if (x > 0) Check(Mathf.Abs(blocks[i].ground.bounds.min.x - blocks[i - 1].ground.bounds.max.x) < .001f, "Horizontal ground seam");
            if (y > 0) Check(Mathf.Abs(blocks[i].ground.bounds.min.y - blocks[i - 5].ground.bounds.max.y) < .001f, "Vertical ground seam");
            Check(blocks[i].edgeMargin == 0, "Internal block margins removed");
        }
        var generated = map.generatedTrees.GetComponentsInChildren<Tree>();
        var fixedTrees = map.fixedTrees.GetComponentsInChildren<Tree>();
        Check(generated.Length == map.generatedTreeCount && fixedTrees.Length == 16, "Baked tree count and original grove preserved");
        var plan = LargeWorldBuilder.PlanTrees(map); var again = LargeWorldBuilder.PlanTrees(map);
        Check(plan.Count == generated.Length && plan.Select(p => p.Position).SequenceEqual(again.Select(p => p.Position)), "Deterministic global generation");
        int seamTrees = 0;
        for (int i = 0; i < generated.Length; i++)
        {
            Vector2 p = map.transform.InverseTransformPoint(generated[i].transform.position);
            Check(Vector2.Distance(p, plan[i].Position) < .001f, "Saved tree matches plan");
            Check(PrefabUtility.IsPartOfPrefabInstance(generated[i]), "Harvest tree keeps prefab connection");
            foreach (var other in fixedTrees.Concat(generated.Take(i)))
                Check(Vector2.Distance(generated[i].transform.position, other.transform.position) >= map.minimumSpacing - .001f, "Cross-block spacing and fixed-tree avoidance");
            for (int edge = 1; edge < 5; edge++)
                if (Mathf.Abs(p.x - (-128 + edge * 51.2f)) < 2 || Mathf.Abs(p.y - (-128 + edge * 51.2f)) < 2) { seamTrees++; break; }
        }
        Check(seamTrees > 15, "Trees can grow beside internal seams instead of forming empty rings");
        Check(map.borderForest.childCount > 500 && map.borderForest.GetComponentsInChildren<MonoBehaviour>().Length == 0,
            "Border trees are static decoration without harvest, update or respawn scripts");
        var boundary = map.boundary;
        Check(boundary.walls.GetComponentsInChildren<BoxCollider2D>().Length == 4, "Four physical air walls");
        Check(boundary.darkenOverlay != null && !boundary.darkenOverlay.raycastTarget && boundary.darkenOverlay.canvas.sortingOrder < 0,
            "Darkening never blocks input or covers foreground HUD");
        Check(Object.FindAnyObjectByType<PlayerController>().worldBoundary == boundary && Object.FindAnyObjectByType<PlayerCamera>().worldBoundary == boundary,
            "Player and camera are wired to same bounds");
        Vector3 center = map.transform.TransformPoint(boundary.walkable.center);
        Check(boundary.DarknessAt(center) == 0, "Interior stays bright");
        float middle = boundary.DarknessAt(map.transform.TransformPoint(new Vector2(boundary.walkable.xMax - 7, 0)));
        float edgeAlpha = boundary.DarknessAt(map.transform.TransformPoint(new Vector2(boundary.walkable.xMax - boundary.playerPadding, 0)));
        Check(middle > 0 && middle < edgeAlpha && Mathf.Abs(edgeAlpha - boundary.maximumDarkness) < .001f, "Continuous edge gradient");
        foreach (float aspect in new[] { 4f / 3, 16f / 9, 21f / 9 })
        {
            var cameraPoint = map.transform.InverseTransformPoint(boundary.ConstrainCamera(center + new Vector3(10000, 10000, -10), 16, aspect));
            Check(cameraPoint.x + 16 * aspect <= boundary.terrain.xMax + .001f && cameraPoint.y + 16 <= boundary.terrain.yMax + .001f, "Camera keeps common aspect ratios inside ground");
        }
        Debug.Log($"LARGE_WORLD_LAYOUT_PASS: {generated.Length} generated, {fixedTrees.Length} preserved, {seamTrees} near internal seams, {map.borderForest.childCount} border sprites.");
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Time", 0) > 90) { Finish(new TimeoutException("Large world checks timed out")); return; }
        if (!EditorApplication.isPlaying || ++frames < 8 || EditorApplication.timeSinceStartup < next) return;
        try
        {
            if (phase == 0)
            {
                map = Object.FindAnyObjectByType<LargeMapAuthoring>(); player = Object.FindAnyObjectByType<PlayerController>();
                start = player.transform.position; count = map.GetComponentsInChildren<Tree>().Length;
                var demo = Object.FindAnyObjectByType<CheeseTownPhone.CheeseTownDemo>();
                Check(demo != null && demo.HarvestTutorialActive && demo.TutorialTree.transform.IsChildOf(map.fixedTrees), "Original tutorial tree remains the initial target");
                Capture("LargeWorld-overview.png", map.transform.position, 130, 1600, 1600, false);
                var boundary = map.boundary;
                foreach (var delta in new[] { new Vector2(10000, 0), new Vector2(-10000, 0), new Vector2(0, 10000), new Vector2(0, -10000), new Vector2(10000, 10000), new Vector2(-10000, -10000) })
                {
                    player.transform.position = start; player.MoveWithinWorld(delta);
                    Vector3 p = map.transform.InverseTransformPoint(player.transform.position);
                    Check(boundary.walkable.Contains(p) && Vector3.Distance(player.transform.position, boundary.ConstrainPosition(player.transform.position)) < .001f,
                        "Actual movement cannot cross any wall/corner even with a large delta");
                    player.MoveWithinWorld(Vector2.zero);
                }
                Physics2D.SyncTransforms();
                foreach (var wall in boundary.walls.GetComponentsInChildren<BoxCollider2D>())
                    Check(wall.enabled && !wall.isTrigger && Physics2D.OverlapPointAll(wall.bounds.center).Contains(wall), "Air wall participates in physics");
                player.transform.position = map.transform.TransformPoint(new Vector2(boundary.walkable.xMax - boundary.playerPadding, 0));
                phase = 1; next = EditorApplication.timeSinceStartup + 1;
            }
            else if (phase == 1)
            {
                Check(Mathf.Abs(map.boundary.darkenOverlay.color.a - map.boundary.maximumDarkness) < .02f, "Runtime edge darkening reaches its target");
                var camera = Object.FindAnyObjectByType<PlayerCamera>().GetComponent<Camera>();
                Capture("LargeWorld-east-edge.png", camera.transform.position, camera.orthographicSize, 1280, 720, true);
                player.transform.position = start; phase = 2; next = EditorApplication.timeSinceStartup + 1;
            }
            else
            {
                Check(map.boundary.darkenOverlay.color.a < .01f, "Returning inland restores brightness");
                map.gameObject.SetActive(false); map.gameObject.SetActive(true);
                Check(map.GetComponentsInChildren<Tree>().Length == count, "Reactivation never spawns more harvest trees");
                Finish(null);
            }
        }
        catch (Exception error) { Finish(error); }
    }

    static void Capture(string filename, Vector3 center, float halfHeight, int width, int height, bool overlay)
    {
        var root = new GameObject("World validation camera"); var camera = root.AddComponent<Camera>();
        Vector3 position = new Vector3(center.x, center.y, -30);
        // Render with the capture's aspect ratio, which can differ from the editor Game view.
        camera.transform.position = overlay ? map.boundary.ConstrainCamera(position, halfHeight, (float)width / height) : position;
        camera.orthographic = true;
        camera.orthographicSize = halfHeight; camera.aspect = (float)width / height;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f, .14f, .12f);
        var target = new RenderTexture(width, height, 24); var image = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active; var canvas = map.boundary.darkenOverlay.canvas;
        try
        {
            if (overlay) { canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1; Canvas.ForceUpdateCanvases(); }
            target.Create(); camera.targetTexture = target;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, width, height), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + filename, image.EncodeToPNG());
        }
        finally
        {
            if (overlay) { canvas.worldCamera = null; canvas.renderMode = RenderMode.ScreenSpaceOverlay; }
            RenderTexture.active = previous; camera.targetTexture = null;
            Object.DestroyImmediate(image); Object.DestroyImmediate(target); Object.DestroyImmediate(root);
        }
    }

    static void Finish(Exception error)
    {
        bool running = SessionState.GetBool(Key, false); SessionState.SetBool(Key, false);
        if (running) EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".FastPlay", false);
        Directory.CreateDirectory("Logs");
        string report = error == null ? "PASS: scene/prefab wiring; 25 blocks and seven designs; original tutorial grove; deterministic global spacing; no seam exclusion rings; static tree03 border; four physics walls; actual movement at edges/corners; camera bounds; gradual darkening and recovery; nonblocking overlay below HUD; no runtime spawning. Unity " + Application.unityVersion : "FAIL: " + error;
        File.WriteAllText("Logs/large-world-checks.txt", report);
        if (error == null) Debug.Log(report); else Debug.LogException(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1); else EditorApplication.isPlaying = false;
    }
}
// END ADDED
