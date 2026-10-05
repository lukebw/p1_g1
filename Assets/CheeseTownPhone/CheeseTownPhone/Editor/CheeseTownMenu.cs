using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;

namespace CheeseTownPhone.Editor
{
    public static class CheeseTownMenu
    {
        // BEGIN CHANGED: Menu links must match the project's nested import folder.
        public const string ConfigPath = "Assets/CheeseTownPhone/CheeseTownPhone/Resources/TabletSettings.asset";
        // END CHANGED
        [MenuItem("Cheese Town/Open Phone Demo Scene")]
        public static void OpenDemo()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before changing scenes."); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                // BEGIN CHANGED: Open the existing scene rather than a missing path.
                EditorSceneManager.OpenScene("Assets/CheeseTownPhone/CheeseTownPhone/Scenes/CheeseTownPhone.unity");
                // END CHANGED
        }
        [MenuItem("Cheese Town/Select Tablet Configuration")]
        public static void SelectConfig()
        { Selection.activeObject = AssetDatabase.LoadAssetAtPath<TabletSettings>(ConfigPath); EditorGUIUtility.PingObject(Selection.activeObject); }
    }

    [CustomEditor(typeof(CheeseTownDemo))]
    public sealed class CheeseTownDemoInspector : UnityEditor.Editor
    {
        UnityEditor.Editor embedded;
        public override void OnInspectorGUI()
        {
            // BEGIN CHANGED: Describe the supplied header and native pixel layout accurately.
            EditorGUILayout.HelpBox("横向平板：Tab 打开/关闭；顶部信封与升级按钮。像素 UI 使用 640x360 逻辑尺寸，素材按 2 倍导入并整数放大。背景、树和按钮均可替换 Sprite；升级进度在所有场景共享。", MessageType.Info);
            // END CHANGED
            DrawDefaultInspector();
            if (GUILayout.Button("选择配置 / Select Tablet Configuration")) CheeseTownMenu.SelectConfig();
            var demo = (CheeseTownDemo)target;
            if (demo.Settings != null)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("配置预览与编辑 / Configuration",EditorStyles.boldLabel);
                CreateCachedEditor(demo.Settings,null,ref embedded); embedded.OnInspectorGUI();
            }
        }
        void OnDisable() { if (embedded != null) DestroyImmediate(embedded); }
    }

    [CustomEditor(typeof(TabletSettings))]
    public sealed class TabletSettingsInspector : UnityEditor.Editor
    {
        ReorderableList list;
        void OnEnable()
        {
            list = new ReorderableList(serializedObject,serializedObject.FindProperty("upgrades"),true,true,true,true);
            list.drawHeaderCallback = r => EditorGUI.LabelField(r,"升级选项 / Shop upgrades  (+ add, - remove, drag reorder)");
            list.elementHeightCallback = index => EditorGUI.GetPropertyHeight(list.serializedProperty.GetArrayElementAtIndex(index),true)+8;
            list.drawElementCallback = (r,index,active,focus) =>
            {
                var entry = list.serializedProperty.GetArrayElementAtIndex(index);
                r.y+=3; r.height-=6;
                string title = entry.FindPropertyRelative("title").stringValue;
                EditorGUI.PropertyField(r,entry,new GUIContent(string.IsNullOrEmpty(title) ? "New upgrade" : title),true);
            };
            list.onAddCallback = l =>
            {
                int index = l.serializedProperty.arraySize++;
                var entry = l.serializedProperty.GetArrayElementAtIndex(index);
                entry.FindPropertyRelative("id").stringValue = System.Guid.NewGuid().ToString("N");
                entry.FindPropertyRelative("title").stringValue = "New upgrade";
                entry.FindPropertyRelative("description").stringValue = "Describe this upgrade here.";
                entry.FindPropertyRelative("effect").enumValueIndex = 0;
                entry.FindPropertyRelative("available").boolValue = true;
                entry.FindPropertyRelative("icon").objectReferenceValue = null;
                var levels = entry.FindPropertyRelative("levels"); levels.arraySize = 1;
                levels.GetArrayElementAtIndex(0).FindPropertyRelative("price").intValue = 20;
                levels.GetArrayElementAtIndex(0).FindPropertyRelative("value").floatValue = 6;
                levels.GetArrayElementAtIndex(0).FindPropertyRelative("treeScale").floatValue = 1;
                entry.isExpanded = true; l.index = index;
            };
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.HelpBox("美术：把导入为 Sprite (2D and UI) 的图片拖入下方窗口。升级：展开条目和 Levels 修改每级价格/数值。按 + / - 增删。改动会刷新运行中的平板；资产配置会保存，购买进度跨场景共享，但不跨运行保存。",MessageType.Info);
            DrawPropertiesExcluding(serializedObject,"m_Script","upgrades");
            EditorGUILayout.Space();
            list.DoLayoutList();
            EditorGUILayout.HelpBox("Effect 决定升级归属。MoveSpeed / CollectRange / AutoCollect 属于玩家；TownTreeProduction / WildTreeGrowth / CheeseValue 同属 TREE 分类。AutoCollect 无视 Value，速率跟随树产量。CheeseValue 的 Value=2 表示单价翻倍，不增加产量。TownTreeProduction 的 Value=主树每秒产量；WildTreeGrowth 的 Value=野外掉落范围倍率，Tree Scale=野外树大小倍率。",MessageType.Info);
            serializedObject.ApplyModifiedProperties();
            var data = (TabletSettings)target;
            EditorGUILayout.LabelField("素材预览窗口 / Artwork previews",EditorStyles.boldLabel);
            Preview("Town background",data.townBackground,data.backgroundColor);
            Preview("Cheese tree",data.cheeseTree,data.treeColor);
            Preview("Envelope",data.envelopeIcon,new Color(.2f,.3f,.35f));
            Preview("Shop",data.shopIcon,new Color(.2f,.3f,.35f));
            Preview("Close",data.closeIcon,new Color(.2f,.3f,.35f));
            Preview("Upgrade button",data.upgradeButtonArtwork,new Color(.2f,.3f,.35f));
            // BEGIN ADDED: Expose the supplied skin in the existing artwork inspector.
            Preview("Main frame",data.mainFrame,Color.clear);
            Preview("Back button",data.backButtonArtwork,Color.clear);
            Preview("Collect button",data.collectButtonArtwork,Color.clear);
            Preview("Unread dot",data.unreadDot,Color.clear);
            Preview("Wallet icon",data.walletIcon,Color.clear);
            // END ADDED
            // BEGIN ADDED: Preview the new shop skin alongside its editable configuration.
            Preview("Upgrade frame",data.upgradeFrame,Color.clear);
            Preview("Upgrade background",data.upgradeBackground,Color.clear);
            Preview("Upgrade row",data.upgradeRowArtwork,Color.clear);
            Preview("Upgrade icon box",data.upgradeIconBox,Color.clear);
            Preview("Disabled buy button",data.upgradeBuyDisabled,Color.clear);
            // END ADDED
            foreach (var entry in data.upgrades) if (entry != null) Preview(entry.title+" icon",entry.icon,new Color(.2f,.3f,.35f));
        }
        static void Preview(string title,Sprite sprite,Color fallback)
        {
            EditorGUILayout.LabelField(title,EditorStyles.miniBoldLabel);
            var r = GUILayoutUtility.GetRect(80,78,GUILayout.ExpandWidth(true));
            EditorGUI.DrawRect(r,fallback);
            if (sprite == null) GUI.Label(r,"未设置素材 / Color + text placeholder",new GUIStyle(EditorStyles.centeredGreyMiniLabel){normal={textColor=Color.white}});
            else
            {
                var texture = AssetPreview.GetAssetPreview(sprite);
                if (texture != null) GUI.DrawTexture(r,texture,ScaleMode.ScaleToFit,true);
                else GUI.Label(r,sprite.name,EditorStyles.centeredGreyMiniLabel);
            }
        }
    }
}
