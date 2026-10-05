// BEGIN ADDED: Create the UI asset once; future artwork imports never overwrite artist edits.
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseTownPhone.Editor
{
    public static class TabletPrefabBuilder
    {
        public const string Path = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/TabletView.prefab";
        public static TabletView Ensure(TabletSettings settings)
        {
            var saved = AssetDatabase.LoadAssetAtPath<TabletView>(Path);
            if (saved != null) return saved;
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Create the tablet prefab outside Play mode.");
            var scene = EditorSceneManager.NewPreviewScene();
            GameObject host = null;
            try
            {
                host = new GameObject("UI migration");
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(host, scene);
                var demo = host.AddComponent<CheeseTownDemo>();
                var view = demo.CreateEditableTablet(settings);
                if (settings.mailPaper != null) MailPrefabUpgrade.Apply(view, settings);
                PrefabUtility.SaveAsPrefabAsset(view.gameObject, Path);
                return AssetDatabase.LoadAssetAtPath<TabletView>(Path);
            }
            finally
            {
                if (host != null) UnityEngine.Object.DestroyImmediate(host);
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
        [MenuItem("Cheese Town/Edit Tablet UI Prefab")]
        public static void Open()
        {
            var settings = AssetDatabase.LoadAssetAtPath<TabletSettings>(PixelUIImport.SettingsPath);
            if (settings == null) return;
            if (settings.tabletPrefab == null)
            {
                settings.tabletPrefab = Ensure(settings); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            }
            AssetDatabase.OpenAsset(settings.tabletPrefab.gameObject);
        }
        public static void GenerateBatch()
        {
            try
            {
                var settings = AssetDatabase.LoadAssetAtPath<TabletSettings>(PixelUIImport.SettingsPath);
                settings.tabletPrefab = Ensure(settings); EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
                Debug.Log("Editable tablet prefab created without replacing the existing upgrade row.");
                if (Application.isBatchMode) EditorApplication.Exit(0);
            }
            catch (Exception error) { Debug.LogException(error); if (Application.isBatchMode) EditorApplication.Exit(1); }
        }
    }

    [CustomEditor(typeof(TabletView))]
    public sealed class TabletViewInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            EditorGUILayout.HelpBox("Edit child RectTransforms and graphics for layout. Runtime resets preview visibility and motion offsets. Use the buttons below to reveal each page; edit placement objects, not motion offsets.", MessageType.Info);
            if (Application.isPlaying) return;
            EditorGUILayout.BeginHorizontal();
            for (int i = 0; i < 3; i++)
            {
                int page = i;
                if (GUILayout.Button(new[] { "Town", "Upgrades", "Mail" }[i]))
                {
                    var view = (TabletView)target;
                    Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Preview tablet page");
                    view.SetPage(page, false); view.SetOpen(true, false); SceneView.RepaintAll();
                }
            }
            // BEGIN ADDED: Reveal the serialized HUD for visual placement without entering Play mode.
            if (GUILayout.Button("HUD"))
            {
                var view = (TabletView)target;
                Undo.RegisterFullObjectHierarchyUndo(view.gameObject, "Preview gameplay HUD");
                view.SetOpen(false, false); SceneView.RepaintAll();
            }
            // END ADDED
            EditorGUILayout.EndHorizontal();
        }
    }
}
// END ADDED
