// BEGIN ADDED: Exercise authored layouts, deterministic generation and the actual high-yield tree.
using System;
using System.IO;
using System.Linq;
using CheeseTown.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class MapPrefabChecks
{
    const string Key = "MapPrefabChecks.Running";
    static int phase, originalTreeCount, initialDropCount;
    static double waitUntil;
    static GameObject world;
    static Tree rich;
    static Vector3 rootPosition;
    static Sprite stump;

    static MapPrefabChecks() { EditorApplication.update += Tick; }
    static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException("Map check failed: " + message); }

    [MenuItem("Cheese Town/Maps/Build and Check Map Prefabs")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before running checks.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        try
        {
            MapAssetBuilder.Build(); ValidateAssets();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            SessionState.SetBool(Key + ".FastPlay", EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetBool(Key, true); SessionState.SetFloat(Key + ".Started", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Finish(error); }
    }
    static void ValidateAssets()
    {
        var database = AssetDatabase.LoadAssetAtPath<MapBlockDatabase>(MapAssetBuilder.DatabasePath);
        Check(database.entries.Count >= 7, "all seven maps are in the database");
        var preview = EditorSceneManager.NewPreviewScene();
        try
        {
            foreach (var entry in database.entries)
            {
                var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(entry.prefab));
                try
                {
                var block = root.GetComponent<MapBlock>();
                Check(block.ground.sprite.texture.filterMode == FilterMode.Point && block.ground.sprite.rect.size == new Vector2(1280, 1280), "native map sprite import");
                Check(Vector2.Distance(block.ground.bounds.size, block.size) < .001f, "ground fits block dimensions");
                Check(Vector2.Distance(block.ground.bounds.min, root.transform.position) < .001f, "ground begins at block origin");
                var first = MapTreeLayout.Plan(block); var second = MapTreeLayout.Plan(block);
                Check(first.Count == block.targetTreeCount && second.Count == first.Count, "default density fits " + block.name);
                for (int i = 0; i < first.Count; i++)
                {
                    Check(first[i].Position == second[i].Position && first[i].Prefab == second[i].Prefab, "seed reproduces tree positions and types");
                    Check(block.Allows(first[i].Position), "tree respects allowed regions, exclusions and margins");
                    Check(PrefabUtility.IsPartOfPrefabInstance(block.generatedTrees.GetChild(i).gameObject), "generated tree keeps its prefab connection");
                }
                var randomState = UnityEngine.Random.state; MapTreeLayout.Plan(block);
                Check(JsonUtility.ToJson(randomState) == JsonUtility.ToJson(UnityEngine.Random.state), "planning does not alter gameplay RNG");
                block.seed++; var changed = MapTreeLayout.Plan(block);
                Check(changed.Count > 0 && changed[0].Position != first[0].Position, "changing seed changes the layout"); block.seed--;
                var fixedTree = block.generatedTrees.GetChild(0); var position = fixedTree.position;
                fixedTree.SetParent(block.authoredTrees, true); MapBlockAuthoring.GenerateTrees(block);
                Check(fixedTree != null && fixedTree.position == position && block.GetComponentsInChildren<Tree>().Length == block.targetTreeCount, "regeneration preserves fixed trees and total density");
                var fixedInstances = block.generatedTrees.Cast<Transform>().ToArray();
                block.placement = MapTreePlacement.ManualOnly; MapBlockAuthoring.GenerateTrees(block);
                Check(fixedInstances.SequenceEqual(block.generatedTrees.Cast<Transform>()), "manual mode does not regenerate");
                block.placement = MapTreePlacement.AreasAndManual;
                var area = new GameObject("Block all").AddComponent<MapTreeArea>(); area.transform.SetParent(block.regions, false);
                area.kind = TreeAreaKind.Exclude; area.rectangle = new Rect(Vector2.zero, block.size);
                Check(MapTreeLayout.Plan(block).Count == 0, "blocked regions terminate without spawning");
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            var assemblyRoot = PrefabUtility.LoadPrefabContents(MapAssetBuilder.WorldPath);
            try
            {
            var assembly = assemblyRoot.GetComponent<MapAssembly>();
            var blocks = assembly.GetComponentsInChildren<MapBlock>();
            Check(blocks.Length == 9, "3x3 assembly is saved");
            var trees = assembly.GetComponentsInChildren<Tree>();
            for (int i = 0; i < trees.Length; i++) for (int j = i + 1; j < trees.Length; j++)
                Check(Vector2.Distance(trees[i].transform.position, trees[j].transform.position) >= 7.999f, "tree spacing also holds across neighboring blocks");
            var positions = blocks.Select(b => b.transform.localPosition).ToArray();
            var identities = blocks.Select(b => b.blockId).ToArray(); MapBlockAuthoring.GenerateBlocks(assembly);
            blocks = assembly.GetComponentsInChildren<MapBlock>();
            Check(positions.SequenceEqual(blocks.Select(b => b.transform.localPosition)) && identities.SequenceEqual(blocks.Select(b => b.blockId)), "database selection and grid positions reproduce");
            for (int y = 0; y < 3; y++) for (int x = 0; x < 3; x++)
            {
                int i = y * 3 + x;
                if (x > 0) Check(blocks[i].blockId != blocks[i - 1].blockId && Mathf.Abs(blocks[i].ground.bounds.min.x - blocks[i - 1].ground.bounds.max.x) < .001f, "horizontal neighbors differ and meet exactly");
                if (y > 0) Check(blocks[i].blockId != blocks[i - 3].blockId && Mathf.Abs(blocks[i].ground.bounds.min.y - blocks[i - 3].ground.bounds.max.y) < .001f, "vertical neighbors differ and meet exactly");
            }
            }
            finally { PrefabUtility.UnloadPrefabContents(assemblyRoot); }
        }
        finally { EditorSceneManager.ClosePreviewScene(preview); }
    }

    static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Started", 0) > 75) { Finish(new TimeoutException("Map play checks timed out.")); return; }
        if (!EditorApplication.isPlaying || EditorApplication.timeSinceStartup < waitUntil) return;
        try
        {
            if (phase == 0)
            {
                world = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MapAssetBuilder.WorldPath));
                originalTreeCount = world.GetComponentsInChildren<Tree>().Length;
                Check(originalTreeCount == 90, "all baked trees enter Play exactly once");
                rich = world.GetComponentsInChildren<Tree>().First(t => t.name.StartsWith("Tree_02"));
                rootPosition = rich.transform.position;
                var serialized = new SerializedObject(rich);
                stump = serialized.FindProperty("stumpSprite").objectReferenceValue as Sprite;
                Check(serialized.FindProperty("minCheeseCount").intValue == 8 && serialized.FindProperty("maxCheeseCount").intValue == 10, "high-yield prefab keeps its 8-10 range");
                Capture(world, "MapWorld-3x3.png", Vector2.zero, 80, 1600);
                world.SetActive(false);
                var sample = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<MapBlock>(MapAssetBuilder.Folder + "/MapBlock_07.prefab"));
                Capture(sample.gameObject, "MapBlock-07.png", (Vector2)sample.transform.position + sample.size * .5f, 27, 1350);
                UnityEngine.Object.DestroyImmediate(sample.gameObject); world.SetActive(true);
                rich.TakeDamage(100);
                initialDropCount = UnityEngine.Object.FindObjectsByType<CheeseDropMotion>().Length;
                Check(initialDropCount >= 8 && initialDropCount <= 10, "02 tree drops 8-10 actual cheese objects");
                phase = 1; waitUntil = EditorApplication.timeSinceStartup + 1.7;
            }
            else
            {
                Check(rich.IsChopped && rich.transform.position == rootPosition, "baked high-yield tree leaves a stationary stump");
                Check(rich.GetComponentsInChildren<SpriteRenderer>().Any(r => r.enabled && r.sprite == stump), "02 stump art appears");
                rich.TakeDamage(100);
                Check(UnityEngine.Object.FindObjectsByType<CheeseDropMotion>().Length == initialDropCount, "stump cannot pay twice");
                world.SetActive(false); world.SetActive(true);
                Check(world.GetComponentsInChildren<Tree>().Length == originalTreeCount && rich.IsChopped, "reactivation preserves stump state and never generates extra trees");
                Finish(null);
            }
        }
        catch (Exception error) { Finish(error); }
    }
    static void Capture(GameObject root, string filename, Vector2 center, float size, int pixels)
    {
        var cameraObject = new GameObject("Map check camera"); var camera = cameraObject.AddComponent<Camera>();
        camera.transform.position = new Vector3(center.x, center.y, -30); camera.orthographic = true;
        camera.orthographicSize = size; camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.1f, .14f, .12f);
        var target = new RenderTexture(pixels, pixels, 24); var image = new Texture2D(pixels, pixels, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            target.Create(); camera.targetTexture = target;
            RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest { destination = target });
            RenderTexture.active = target; image.ReadPixels(new Rect(0, 0, pixels, pixels), 0, 0); image.Apply();
            Directory.CreateDirectory("Logs"); File.WriteAllBytes("Logs/" + filename, image.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(cameraObject);
        }
    }
    static void Finish(Exception error)
    {
        bool ranPlay = SessionState.GetBool(Key, false); SessionState.SetBool(Key, false);
        if (ranPlay) EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".FastPlay", false);
        Directory.CreateDirectory("Logs");
        string report = error == null ? "PASS: seven native map variants; saved database/3x3 assembly; exact ground seams; deterministic weighted placement; minimum spacing including block boundaries; exclusion zones; fixed-tree preservation; bounded attempts; manual mode; gameplay RNG isolation; prefab connections; 90 baked trees without runtime respawn; 02 tree's actual 8-10 drops and stump behavior. Unity " + Application.unityVersion : "FAIL: " + error;
        File.WriteAllText("Logs/map-prefab-checks.txt", report);
        if (error == null) Debug.Log(report); else Debug.LogException(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
}
// END ADDED
