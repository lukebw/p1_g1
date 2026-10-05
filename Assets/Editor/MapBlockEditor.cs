// BEGIN ADDED: Generation is explicit, undoable and limited to generated content.
using System;
using System.Collections.Generic;
using System.Linq;
using CheeseTown.World;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class MapBlockAuthoring
{
    public static void FitGround(MapBlock block)
    {
        if (block.ground == null || block.ground.sprite == null) throw new InvalidOperationException("Assign a ground sprite.");
        var bounds = block.ground.sprite.bounds;
        var scale = new Vector3(block.size.x / bounds.size.x, block.size.y / bounds.size.y, 1);
        Undo.RecordObject(block.ground.transform, "Fit map ground");
        block.ground.transform.localScale = scale;
        block.ground.transform.localPosition = (Vector3)(block.size * .5f) - Vector3.Scale(bounds.center, scale);
        PrefabUtility.RecordPrefabInstancePropertyModifications(block.ground.transform);
    }

    public static void GenerateTrees(MapBlock block)
    {
        if (block.placement == MapTreePlacement.ManualOnly) return;
        if (block.generatedTrees == null || block.authoredTrees == null || block.generatedTrees == block.authoredTrees)
            throw new InvalidOperationException("Assign separate Fixed Trees and Generated Trees roots.");
        var neighbors = new List<Vector3>();
        foreach (var root in block.gameObject.scene.GetRootGameObjects())
            foreach (var other in root.GetComponentsInChildren<MapBlock>())
                if (other != block)
                    foreach (var tree in other.GetComponentsInChildren<Tree>()) neighbors.Add(tree.transform.position);
        // Plan first, so invalid configuration cannot erase the existing layout.
        var planned = MapTreeLayout.Plan(block, neighbors);
        foreach (var item in planned)
            if (item.Prefab == null || !PrefabUtility.IsPartOfPrefabAsset(item.Prefab))
                throw new InvalidOperationException("Tree palette entries must reference saved prefab assets.");
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Generate map trees");
        foreach (Transform child in block.generatedTrees.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
        foreach (var item in planned)
        {
            var tree = (GameObject)PrefabUtility.InstantiatePrefab(item.Prefab.gameObject, block.generatedTrees);
            Undo.RegisterCreatedObjectUndo(tree, "Place tree prefab");
            tree.transform.position = block.transform.TransformPoint(item.Position);
            tree.transform.localRotation = Quaternion.identity;
            PrefabUtility.RecordPrefabInstancePropertyModifications(tree.transform);
        }
        Undo.CollapseUndoOperations(group);
        int count = block.authoredTrees.GetComponentsInChildren<Tree>(true).Length + planned.Count;
        if (count < block.targetTreeCount)
            Debug.LogWarning($"{block.name}: placed {count}/{block.targetTreeCount} trees. Enlarge allowed regions or reduce spacing/density.", block);
    }

    public static void FreezeTrees(MapBlock block)
    {
        if (block.authoredTrees == null || block.generatedTrees == null) return;
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup();
        foreach (Transform child in block.generatedTrees.Cast<Transform>().ToArray())
            Undo.SetTransformParent(child, block.authoredTrees, "Keep generated trees as fixed placements");
        Undo.CollapseUndoOperations(group);
    }

    public static void GenerateBlocks(MapAssembly assembly)
    {
        if (assembly.database == null || assembly.generatedBlocks == null) throw new InvalidOperationException("Assign the map database and generated blocks root.");
        int columns = Mathf.Clamp(assembly.columns, 1, 12), rows = Mathf.Clamp(assembly.rows, 1, 12);
        var choices = new MapBlock[columns, rows]; var random = new System.Random(assembly.seed);
        Vector2 size = Vector2.zero;
        for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
        {
            var block = assembly.database.Pick(random, x > 0 ? choices[x - 1, y].blockId : null, y > 0 ? choices[x, y - 1].blockId : null);
            if (!PrefabUtility.IsPartOfPrefabAsset(block)) throw new InvalidOperationException("The database must reference saved map prefabs.");
            if (x == 0 && y == 0) size = block.size;
            if (block.size != size || size.x <= 0 || size.y <= 0) throw new InvalidOperationException("All selected map blocks must have the same positive dimensions.");
            choices[x, y] = block;
        }
        Undo.IncrementCurrentGroup(); int group = Undo.GetCurrentGroup(); Undo.SetCurrentGroupName("Assemble map blocks");
        foreach (Transform child in assembly.generatedBlocks.Cast<Transform>().ToArray()) Undo.DestroyObjectImmediate(child.gameObject);
        for (int y = 0; y < rows; y++) for (int x = 0; x < columns; x++)
        {
            var block = (GameObject)PrefabUtility.InstantiatePrefab(choices[x, y].gameObject, assembly.generatedBlocks);
            Undo.RegisterCreatedObjectUndo(block, "Place map block");
            block.name = $"Block {x},{y} - {choices[x, y].blockId}";
            block.transform.localPosition = new Vector3((x - columns * .5f) * size.x, (y - rows * .5f) * size.y, 0);
            block.transform.localRotation = Quaternion.identity; block.transform.localScale = Vector3.one;
            PrefabUtility.RecordPrefabInstancePropertyModifications(block.transform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(block);
        }
        Undo.CollapseUndoOperations(group);
    }
}

[CustomEditor(typeof(MapBlock))]
public sealed class MapBlockInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Green = allowed; red = excluded. Generation replaces only Generated Trees. Move important trees to Fixed Trees, or keep all generated trees with the button below. Nothing regenerates when entering Play. Each block starts at its lower-left corner.", MessageType.Info);
        var stage = PrefabStageUtility.GetPrefabStage(((MapBlock)target).gameObject);
        bool sceneInstance = PrefabUtility.IsPartOfPrefabInstance(target) && (stage == null || stage.prefabContentsRoot != ((MapBlock)target).gameObject);
        if (sceneInstance) EditorGUILayout.HelpBox("Open this map block prefab to regenerate or move trees between groups. Unity locks inherited child hierarchies in scene instances.", MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying || EditorUtility.IsPersistent(target) || sceneInstance))
        {
            var block = (MapBlock)target;
            if (GUILayout.Button("Fit ground to block size")) MapBlockAuthoring.FitGround(block);
            using (new EditorGUI.DisabledScope(block.placement == MapTreePlacement.ManualOnly))
                if (GUILayout.Button("Regenerate Generated Trees (same seed = same layout)")) MapBlockAuthoring.GenerateTrees(block);
            if (GUILayout.Button("Keep all generated trees as Fixed Trees")) MapBlockAuthoring.FreezeTrees(block);
        }
    }
}

[CustomEditor(typeof(MapTreeArea))]
public sealed class MapTreeAreaInspector : Editor
{
    readonly BoxBoundsHandle handle = new BoxBoundsHandle();
    void OnSceneGUI()
    {
        var area = (MapTreeArea)target;
        using (new Handles.DrawingScope(area.kind == TreeAreaKind.Allow ? Color.green : Color.red, area.transform.localToWorldMatrix))
        {
            handle.center = area.rectangle.center;
            handle.size = new Vector3(area.rectangle.width, area.rectangle.height, 0);
            handle.axes = PrimitiveBoundsHandle.Axes.X | PrimitiveBoundsHandle.Axes.Y;
            EditorGUI.BeginChangeCheck(); handle.DrawHandle();
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(area, "Resize tree placement region");
                area.rectangle = new Rect((Vector2)handle.center - (Vector2)handle.size * .5f, (Vector2)handle.size);
                PrefabUtility.RecordPrefabInstancePropertyModifications(area); EditorUtility.SetDirty(area);
            }
        }
    }
}

[CustomEditor(typeof(MapAssembly))]
public sealed class MapAssemblyInspector : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        EditorGUILayout.HelpBox("Selects complete saved block layouts from the database. Rebuild replaces Generated Blocks, including their instance edits; save important edits to their block prefab first. Play mode does not rebuild the map.", MessageType.Info);
        using (new EditorGUI.DisabledScope(Application.isPlaying || EditorUtility.IsPersistent(target)))
            if (GUILayout.Button("Rebuild Generated Blocks")) MapBlockAuthoring.GenerateBlocks((MapAssembly)target);
    }
}
// END ADDED
