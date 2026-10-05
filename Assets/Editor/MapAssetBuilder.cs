// BEGIN ADDED: Create missing native assets once without overwriting authored block layouts.
using System;
using System.Linq;
using CheeseTown.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapAssetBuilder
{
    public const string Folder = "Assets/Prefab/Maps";
    public const string DatabasePath = "Assets/Settings/MapBlockDatabase.asset";
    public const string WorldPath = Folder + "/MapWorld_3x3.prefab";

    [MenuItem("Cheese Town/Maps/Create Missing Map Prefabs")]
    public static void Build()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before authoring maps.");
        AssetDatabase.Refresh();
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/Prefab", "Maps");
        var ordinary = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Tree.prefab").GetComponent<Tree>();
        var rich = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Tree_02.prefab").GetComponent<Tree>();
        var material = ordinary.GetComponentsInChildren<SpriteRenderer>().First(s => s.enabled).sharedMaterial;
        var sprites = new Sprite[7];
        for (int i = 0; i < sprites.Length; i++)
        {
            string path = $"Assets/Art/MAP_{i + 1:00}.png";
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException("Missing map art: " + path);
            // Keep the existing MAP_03 slice and GUID used by Wilderness intact.
            if (i != 2)
            {
                importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100; importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false; importer.npotScale = TextureImporterNPOTScale.None;
                importer.alphaIsTransparency = true; importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
                settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteAlignment = (int)SpriteAlignment.Center;
                importer.SetTextureSettings(settings); importer.SaveAndReimport();
            }
            sprites[i] = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().First();
        }
        var scene = EditorSceneManager.NewPreviewScene();
        try
        {
            string basePath = Folder + "/MapBlock_Base.prefab";
            var template = AssetDatabase.LoadAssetAtPath<MapBlock>(basePath);
            if (template == null)
            {
                var root = new GameObject("MapBlock_Base"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var block = root.AddComponent<MapBlock>(); block.blockId = "base";
                block.ground = Child(root.transform, "Ground").gameObject.AddComponent<SpriteRenderer>();
                block.ground.sprite = sprites[0]; block.ground.sharedMaterial = material; block.ground.sortingLayerName = "Background";
                block.authoredTrees = Child(root.transform, "Fixed Trees"); block.generatedTrees = Child(root.transform, "Generated Trees");
                block.regions = Child(root.transform, "Tree Regions"); block.decorations = Child(root.transform, "Decorations");
                block.treePalette.Add(new MapTreeChoice { prefab = ordinary, weight = 9 });
                block.treePalette.Add(new MapTreeChoice { prefab = rich, weight = 1 });
                Area(block, "Allowed grass", TreeAreaKind.Allow, new Rect(Vector2.zero, block.size));
                Area(block, "North-south passage", TreeAreaKind.Exclude, new Rect(24, 0, 3.2f, 51.2f));
                Area(block, "East-west passage", TreeAreaKind.Exclude, new Rect(0, 24, 51.2f, 3.2f));
                MapBlockAuthoring.FitGround(block);
                PrefabUtility.SaveAsPrefabAsset(root, basePath); UnityEngine.Object.DestroyImmediate(root);
                template = AssetDatabase.LoadAssetAtPath<MapBlock>(basePath);
            }
            var database = AssetDatabase.LoadAssetAtPath<MapBlockDatabase>(DatabasePath);
            if (database == null) { database = ScriptableObject.CreateInstance<MapBlockDatabase>(); AssetDatabase.CreateAsset(database, DatabasePath); }
            for (int i = 0; i < 7; i++)
            {
                string path = $"{Folder}/MapBlock_{i + 1:00}.prefab";
                var saved = AssetDatabase.LoadAssetAtPath<MapBlock>(path);
                if (saved == null)
                {
                    var root = (GameObject)PrefabUtility.InstantiatePrefab(template.gameObject, scene);
                    root.name = $"MapBlock_{i + 1:00}";
                    var block = root.GetComponent<MapBlock>(); block.blockId = $"grass-{i + 1:00}"; block.seed = 5110 + i * 137;
                    block.ground.sprite = sprites[i];
                    AddLandmarkExclusions(block, i + 1);
                    MapBlockAuthoring.FitGround(block); MapBlockAuthoring.GenerateTrees(block);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(block);
                    PrefabUtility.RecordPrefabInstancePropertyModifications(block.ground);
                    PrefabUtility.SaveAsPrefabAsset(root, path); UnityEngine.Object.DestroyImmediate(root);
                    saved = AssetDatabase.LoadAssetAtPath<MapBlock>(path);
                }
                if (!database.entries.Any(e => e != null && e.prefab == saved)) database.entries.Add(new MapBlockEntry { prefab = saved });
            }
            EditorUtility.SetDirty(database); AssetDatabase.SaveAssets();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WorldPath) == null)
            {
                var root = new GameObject("MapWorld_3x3"); UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(root, scene);
                var assembly = root.AddComponent<MapAssembly>(); assembly.database = database;
                assembly.generatedBlocks = Child(root.transform, "Generated Blocks");
                MapBlockAuthoring.GenerateBlocks(assembly); PrefabUtility.SaveAsPrefabAsset(root, WorldPath);
                UnityEngine.Object.DestroyImmediate(root);
            }
            AssetDatabase.SaveAssets();
            Debug.Log("MAP_ASSETS_READY: base template, seven authored variants, weighted database and baked 3x3 assembly.");
        }
        finally { EditorSceneManager.ClosePreviewScene(scene); }
    }

    static Transform Child(Transform parent, string name)
    {
        var child = new GameObject(name).transform; child.SetParent(parent, false); return child;
    }
    static void Area(MapBlock block, string name, TreeAreaKind kind, Rect rect)
    {
        var area = Child(block.regions, name).gameObject.AddComponent<MapTreeArea>(); area.kind = kind; area.rectangle = rect;
    }
    static void AvoidPixels(MapBlock block, string name, float x, float y, float width, float height)
        => Area(block, name, TreeAreaKind.Exclude, new Rect(x * .04f, (1280 - y - height) * .04f, width * .04f, height * .04f));

    static void AddLandmarkExclusions(MapBlock block, int map)
    {
        // Coordinates refer to the supplied artwork; artists can resize these red regions in Scene view.
        if (map == 1) { AvoidPixels(block, "West sand", 0, 565, 220, 120); AvoidPixels(block, "North sand", 810, 0, 225, 95); }
        if (map == 2) { AvoidPixels(block, "Central rock", 615, 495, 135, 70); AvoidPixels(block, "South rock", 1000, 1070, 110, 65); }
        if (map == 3) { AvoidPixels(block, "North pond", 145, 155, 220, 160); AvoidPixels(block, "South pond", 910, 850, 225, 155); }
        if (map == 5) { AvoidPixels(block, "North rocks", 175, 255, 175, 85); AvoidPixels(block, "Center rocks", 380, 690, 150, 95); AvoidPixels(block, "East rocks", 825, 790, 145, 100); }
        if (map == 6) { AvoidPixels(block, "Cheese landmark", 895, 185, 135, 95); AvoidPixels(block, "South rocks", 745, 1025, 285, 130); }
        if (map == 7) AvoidPixels(block, "Large clearing", 155, 220, 480, 250);
    }
}
// END ADDED
