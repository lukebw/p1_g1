using System;
using UnityEditor;
using UnityEngine;

namespace CheeseTownPhone.Editor
{
    public static class EndingAssets
    {
        public const string Path = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/EpilogueView.prefab";

        [MenuItem("Cheese Town/Create Ending Narrative")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before creating the ending.");
            var settings = Resources.Load<TabletSettings>("TabletSettings");
            var saved = AssetDatabase.LoadAssetAtPath<PrologueView>(Path);
            // Create an independent, editable asset; never overwrite an artist-edited ending or opening.
            if (saved == null)
            {
                var root = PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(settings.prologuePrefab));
                try
                {
                    root.name = "Ending narrative";
                    var view = root.GetComponent<PrologueView>();
                    var opening = view.chapters;
                    view.chapters = new[] {
                        new PrologueView.Chapter { title = "", background = opening[2].background,
                            body = "Winter's hunger had long since passed.\nStill, you brought more cheese home.\nThe shelves filled. Then the streets." },
                        new PrologueView.Chapter { title = "", background = opening[0].background,
                            body = "The cheese rose past windows and rooftops.\nThe mice fled while the roads were clear.\nSoon, even the Great Tree disappeared." },
                        new PrologueView.Chapter { title = "", background = opening[1].background,
                            body = "You gathered too much cheese.\nMousetown lay buried beneath the harvest.\nThere was food for everyone...\nand no home left to share it." }
                    };
                    view.finalHint = "CLICK / SPACE TO END";
                    view.fadeSeconds = .7f; view.minimumReadSeconds = 1.1f;
                    view.background.color = new Color(.65f, .65f, .7f, 1);
                    view.skip.gameObject.name = "Skip ending";
                    root.GetComponent<Canvas>().sortingOrder = 200;
                    view.Begin(null); view.Step(1);
                    PrefabUtility.SaveAsPrefabAsset(root, Path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
                saved = AssetDatabase.LoadAssetAtPath<PrologueView>(Path);
            }
            settings.epiloguePrefab = saved;
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        }
    }
}
