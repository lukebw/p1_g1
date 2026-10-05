// BEGIN ADDED: Bake a finite world once, then allow artists to preserve and edit its trees.
using System;
using System.Collections.Generic;
using System.Linq;
using CheeseTown.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class LargeWorldBuilder
{
    public const string WorldPath = "Assets/Prefab/Maps/MapWorld_Large.prefab";
    public const string BorderPath = "Assets/Prefab/Tree_03_Border.prefab";
    public const string ScenePath = "Assets/Scenes/Wilderness.unity";

    [MenuItem("Cheese Town/Maps/Create Large Wilderness (once)")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before authoring the world.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath) != null)
        {
            Debug.Log("Large world already exists. Open its prefab to edit; the builder does not overwrite authored work.");
            return;
        }
        var scene = EditorSceneManager.OpenScene(ScenePath);
        var player = Object.FindAnyObjectByType<PlayerController>();
        var camera = Object.FindAnyObjectByType<PlayerCamera>();
        var originalGround = scene.GetRootGameObjects().Single(g => g.name == "Background").GetComponent<SpriteRenderer>();
        var existingTrees = Object.FindObjectsByType<Tree>().ToArray();
        Vector3 center = originalGround.bounds.center; center.z = 0;
        var borderPrefab = MakeBorderTree();
        var root = new GameObject("MapWorld_Large"); root.transform.position = center;
        var map = root.AddComponent<LargeMapAuthoring>();
        map.boundary = root.AddComponent<WorldBoundary>();
        map.terrainBlocks = Child(root.transform, "Terrain Blocks");
        map.fixedTrees = Child(root.transform, "Fixed Trees - original tutorial grove");
        map.generatedTrees = Child(root.transform, "Generated Trees - editable");
        map.borderForest = Child(root.transform, "Border Forest - decorative tree03");
        map.borderTreePrefab = borderPrefab;
        map.ordinaryTree = AssetDatabase.LoadAssetAtPath<Tree>("Assets/Prefab/Tree.prefab");
        map.richTree = AssetDatabase.LoadAssetAtPath<Tree>("Assets/Prefab/Tree_02.prefab");
        Vector2 start = root.transform.InverseTransformPoint(player.transform.position);
        map.startClearing = new Rect(start - Vector2.one * 4, Vector2.one * 8);
        // Preserve the original scene's tree positions, overrides and tutorial target.
        foreach (var tree in existingTrees) tree.transform.SetParent(map.fixedTrees, true);
        var database = AssetDatabase.LoadAssetAtPath<MapBlockDatabase>(MapAssetBuilder.DatabasePath);
        var choices = new MapBlock[5, 5]; var random = new System.Random(map.seed);
        for (int y = 0; y < 5; y++) for (int x = 0; x < 5; x++)
        {
            var choice = x == 2 && y == 2 ? database.entries.Single(e => e.prefab.blockId == "grass-03").prefab
                : database.Pick(random, x > 0 ? choices[x - 1, y].blockId : null, y > 0 ? choices[x, y - 1].blockId : null);
            choices[x, y] = choice;
            var blockRoot = (GameObject)PrefabUtility.InstantiatePrefab(choice.gameObject, map.terrainBlocks);
            // Bake a snapshot so cross-block edits do not alter the seven source variants.
            // Nested tree prefabs remain connected; the old per-block trees are replaced below.
            PrefabUtility.UnpackPrefabInstance(blockRoot, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
            var block = blockRoot.GetComponent<MapBlock>(); block.name = $"Ground {x},{y} - {choice.blockId}";
            block.transform.localPosition = new Vector3((x - 2.5f) * 51.2f, (y - 2.5f) * 51.2f, 0);
            foreach (var tree in block.GetComponentsInChildren<Tree>(true)) Object.DestroyImmediate(tree.gameObject);
            foreach (var area in block.regions.GetComponentsInChildren<MapTreeArea>())
                if (area.name == "North-south passage" || area.name == "East-west passage") Object.DestroyImmediate(area.gameObject);
            block.edgeMargin = 0; block.placement = MapTreePlacement.ManualOnly;
            MapBlockAuthoring.FitGround(block);
        }
        RegenerateTrees(map); RegenerateForest(map); RebuildWalls(map.boundary); CreateFeedback(map.boundary);
        var saved = PrefabUtility.SaveAsPrefabAssetAndConnect(root, WorldPath, InteractionMode.AutomatedAction);
        if (saved == null) throw new InvalidOperationException("Large world prefab could not be saved.");
        originalGround.gameObject.SetActive(false);
        player.worldBoundary = map.boundary; camera.worldBoundary = map.boundary;
        EditorUtility.SetDirty(player); EditorUtility.SetDirty(camera);
        EditorSceneManager.SaveScene(scene); AssetDatabase.SaveAssets();
        Debug.Log($"LARGE_WORLD_READY: 25 blocks, {existingTrees.Length} original trees, {map.generatedTrees.childCount} generated trees, {map.borderForest.childCount} decorative border trees.");
    }

    static GameObject MakeBorderTree()
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(BorderPath); if (existing != null) return existing;
        const string art = "Assets/Art/Cheese_tree_03.png";
        AssetDatabase.ImportAsset(art);
        var importer = (TextureImporter)AssetImporter.GetAtPath(art);
        importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.npotScale = TextureImporterNPOTScale.None;
        var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(.5f, 34f / 256f); importer.SetTextureSettings(settings); importer.SaveAndReimport();
        var root = new GameObject("Tree_03_Border");
        try
        {
            var renderer = root.AddComponent<SpriteRenderer>(); renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(art);
            renderer.sortingLayerName = "Foreground"; renderer.spriteSortPoint = SpriteSortPoint.Pivot;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Tree>("Assets/Prefab/Tree.prefab")
                .GetComponentsInChildren<SpriteRenderer>().First(r => r.enabled).sharedMaterial;
            root.transform.localScale = Vector3.one * 4;
            return PrefabUtility.SaveAsPrefabAsset(root, BorderPath);
        }
        finally { Object.DestroyImmediate(root); }
    }

    // One global spacing check crosses internal seams; only the actual outer edge has a margin.
    public static List<MapTreePlacementPoint> PlanTrees(LargeMapAuthoring map)
    {
        if (map.minimumSpacing <= 0 || map.generatedTreeCount < 0 || map.ordinaryTree == null || map.richTree == null)
            throw new InvalidOperationException("Assign tree prefabs and positive spacing before generating.");
        var random = new System.Random(map.seed); var result = new List<MapTreePlacementPoint>();
        var occupied = map.fixedTrees.GetComponentsInChildren<Tree>(true)
            .Select(t => (Vector2)map.transform.InverseTransformPoint(t.transform.position)).ToList();
        var blocks = map.terrainBlocks.GetComponentsInChildren<MapBlock>();
        var rect = map.boundary.walkable;
        rect.xMin += map.harvestTreeEdgeInset; rect.xMax -= map.harvestTreeEdgeInset;
        rect.yMin += map.harvestTreeEdgeInset; rect.yMax -= map.harvestTreeEdgeInset;
        if (rect.width <= 0 || rect.height <= 0) throw new InvalidOperationException("Tree margin consumes the playable area.");
        float spacingSquared = map.minimumSpacing * map.minimumSpacing;
        for (int attempt = 0; attempt < map.generatedTreeCount * 300 && result.Count < map.generatedTreeCount; attempt++)
        {
            Vector2 point = new Vector2(Mathf.Lerp(rect.xMin, rect.xMax, (float)random.NextDouble()),
                Mathf.Lerp(rect.yMin, rect.yMax, (float)random.NextDouble()));
            point = Snap(point);
            if (map.startClearing.Contains(point) || occupied.Any(p => (p - point).sqrMagnitude < spacingSquared)) continue;
            Vector3 world = map.transform.TransformPoint(point);
            bool allowed = blocks.Any(b => b.Allows(b.transform.InverseTransformPoint(world)));
            if (!allowed) continue;
            // Broad density variation avoids a uniformly spaced grid or identical block-sized rings.
            float density = Mathf.PerlinNoise((point.x + map.seed % 997) * .025f, (point.y + 317) * .025f);
            if (random.NextDouble() > .3f + density * .7f) continue;
            var tree = random.NextDouble() < map.richTreeChance ? map.richTree : map.ordinaryTree;
            result.Add(new MapTreePlacementPoint(tree, point)); occupied.Add(point);
        }
        return result;
    }

    public static void RegenerateTrees(LargeMapAuthoring map)
    {
        var plan = PlanTrees(map); // Validate before replacing any generated objects.
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        Clear(map.generatedTrees);
        foreach (var item in plan) Place(item.Prefab.gameObject, map.generatedTrees, item.Position);
        Undo.CollapseUndoOperations(group);
        if (plan.Count < map.generatedTreeCount) Debug.LogWarning($"Placed {plan.Count}/{map.generatedTreeCount}; reduce density or enlarge allowed regions.");
    }

    public static void RegenerateForest(LargeMapAuthoring map)
    {
        if (map.borderTreePrefab == null || map.forestSpacing < 1) throw new InvalidOperationException("Assign a border tree and spacing >= 1.");
        var positions = new List<Vector2>(); var random = new System.Random(map.seed + 3);
        var rect = map.boundary.terrain;
        // Staggered rows fill gaps at both sides and corners; no per-tree runtime behaviours.
        for (int row = 0; row < 3; row++)
        {
            float inset = 3 + row * 4;
            float left = rect.xMin + inset, right = rect.xMax - inset;
            float bottom = rect.yMin + inset, top = rect.yMax - inset;
            int nx = Mathf.CeilToInt((right - left) / map.forestSpacing);
            int ny = Mathf.CeilToInt((top - bottom) / map.forestSpacing);
            for (int i = 0; i <= nx; i++)
            {
                float x = Mathf.Lerp(left, right, (float)i / nx);
                positions.Add(Snap(new Vector2(x, bottom + Jitter(random))));
                positions.Add(Snap(new Vector2(x, top + Jitter(random))));
            }
            for (int i = 1; i < ny; i++)
            {
                float y = Mathf.Lerp(bottom, top, (float)i / ny);
                positions.Add(Snap(new Vector2(left + Jitter(random), y)));
                positions.Add(Snap(new Vector2(right + Jitter(random), y)));
            }
        }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Clear(map.borderForest);
        foreach (var point in positions) Place(map.borderTreePrefab, map.borderForest, point);
        Undo.CollapseUndoOperations(group);
    }

    public static void RebuildWalls(WorldBoundary boundary)
    {
        if (boundary.walls == null) boundary.walls = Child(boundary.transform, "Invisible Walls");
        Clear(boundary.walls); var r = boundary.walkable; const float thickness = 2;
        Wall(boundary.walls, "West", new Vector2(r.xMin - thickness / 2, r.center.y), new Vector2(thickness, r.height + thickness * 2));
        Wall(boundary.walls, "East", new Vector2(r.xMax + thickness / 2, r.center.y), new Vector2(thickness, r.height + thickness * 2));
        Wall(boundary.walls, "South", new Vector2(r.center.x, r.yMin - thickness / 2), new Vector2(r.width + thickness * 2, thickness));
        Wall(boundary.walls, "North", new Vector2(r.center.x, r.yMax + thickness / 2), new Vector2(r.width + thickness * 2, thickness));
    }

    static void CreateFeedback(WorldBoundary boundary)
    {
        var root = new GameObject("Boundary Darken - below gameplay HUD", typeof(RectTransform), typeof(Canvas));
        root.transform.SetParent(boundary.transform, false);
        var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = -20;
        var image = new GameObject("Darken", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image)).GetComponent<Image>();
        image.transform.SetParent(root.transform, false); image.raycastTarget = false; image.color = Color.clear;
        image.rectTransform.anchorMin = Vector2.zero; image.rectTransform.anchorMax = Vector2.one;
        image.rectTransform.offsetMin = image.rectTransform.offsetMax = Vector2.zero;
        boundary.darkenOverlay = image;
    }

    static void Wall(Transform parent, string name, Vector2 position, Vector2 size)
    {
        var wall = Child(parent, name); wall.localPosition = position;
        wall.gameObject.AddComponent<BoxCollider2D>().size = size;
        Undo.RegisterCreatedObjectUndo(wall.gameObject, "Create boundary wall");
    }
    static float Jitter(System.Random random) => ((float)random.NextDouble() - .5f) * .8f;
    static Vector2 Snap(Vector2 point) => new Vector2(Mathf.Round(point.x / .04f) * .04f, Mathf.Round(point.y / .04f) * .04f);
    static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform; child.SetParent(parent, false); return child;
    }
    static void Clear(Transform root)
    {
        foreach (var child in root.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
    }
    static void Place(GameObject prefab, Transform parent, Vector2 position)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        Undo.RegisterCreatedObjectUndo(instance, "Place world tree"); instance.transform.localPosition = position;
        PrefabUtility.RecordPrefabInstancePropertyModifications(instance.transform);
    }
}

[CustomEditor(typeof(LargeMapAuthoring))]
public sealed class LargeMapInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector(); var map = (LargeMapAuthoring)target;
        EditorGUILayout.HelpBox("Baked world: move important trees to Fixed Trees before regenerating. Original map variants are unchanged. Edit landmark regions under Terrain Blocks. Open the world prefab to use these buttons.", MessageType.Info);
        var stage = PrefabStageUtility.GetPrefabStage(map.gameObject);
        bool locked = PrefabUtility.IsPartOfPrefabInstance(map) && (stage == null || stage.prefabContentsRoot != map.gameObject);
        using (new EditorGUI.DisabledScope(Application.isPlaying || EditorUtility.IsPersistent(map) || locked))
        {
            if (GUILayout.Button("Regenerate harvestable trees only")) LargeWorldBuilder.RegenerateTrees(map);
            if (GUILayout.Button("Keep generated trees as fixed"))
            {
                Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
                foreach (Transform tree in map.generatedTrees.Cast<Transform>().ToArray()) Undo.SetTransformParent(tree, map.fixedTrees, "Keep world trees");
                Undo.CollapseUndoOperations(group);
            }
            if (GUILayout.Button("Regenerate decorative border forest only")) LargeWorldBuilder.RegenerateForest(map);
            if (GUILayout.Button("Rebuild walls from boundary settings")) LargeWorldBuilder.RebuildWalls(map.boundary);
        }
    }
}
// END ADDED
