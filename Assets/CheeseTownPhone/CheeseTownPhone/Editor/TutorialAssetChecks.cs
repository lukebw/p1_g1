using System;
using System.IO;
using CheeseTownPhone;
using UnityEditor;
using UnityEngine;

namespace CheeseTownPhone.Editor
{
    // BEGIN ADDED: Inspect the real saved prefab in an isolated preview scene, without changing the open scene.
    public static class TutorialAssetChecks
    {
        [MenuItem("Cheese Town/Run Tutorial Asset Checks")]
        public static void Run()
        {
            PhoneDemoChecks.DataChecks();
            const string path = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/TutorialCoach.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = root.GetComponent<TutorialCoachView>();
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Art/UI/Fonts/PressStart2P-Pixel.ttf");
                if (view == null || font == null || view.body.font != font || view.heading.font != font)
                    throw new Exception("Tutorial prefab/font binding is missing.");
                if (view.instructions.Length != 8 || view.titles.Length != 8 || view.focusEdges.Length != 4 || view.shades.Length != 4)
                    throw new Exception("Tutorial step or focus bindings are incomplete.");
                foreach (var edge in view.focusEdges)
                    if (edge.GetComponent<UnityEngine.UI.Image>().raycastTarget)
                        throw new Exception("A focus edge intercepts input.");
                var mail = new TownProgress(ScriptableObject.CreateInstance<TabletSettings>());
                mail.CollectWorld(100);
                if (mail.MailboxEntryIndex != 2) throw new Exception("Newest unread priority failed.");
                mail.ReadLetter(2);
                if (mail.MailboxEntryIndex != 1) throw new Exception("Older unread mail was skipped.");
                mail.ReadLetter(1); mail.ReadLetter(0);
                if (mail.MailboxEntryIndex != 2) throw new Exception("All-read fallback failed.");
                for (int i = 0; i < 8; i++)
                {
                    view.Present(i, true, null, null);
                    if (!UpgradeRowView.Fits(view.heading.text, view.heading) || !UpgradeRowView.Fits(view.body.text, view.body))
                        throw new Exception("Tutorial text is clipped at step " + (i + 1));
                }
                // Verify real shade geometry: cover everything except the highlighted rectangle.
                var focus = new GameObject("Spotlight test target", typeof(RectTransform)).GetComponent<RectTransform>();
                focus.SetParent(root.transform, false);
                focus.anchorMin = focus.anchorMax = new Vector2(.5f, .5f);
                focus.anchoredPosition = new Vector2(80, 20); focus.sizeDelta = new Vector2(100, 40);
                view.Present(0, true, null, focus);
                float shadedArea = 0;
                foreach (var shade in view.shades)
                {
                    var rectangle = new Rect(shade.anchoredPosition, shade.sizeDelta);
                    if (rectangle.Contains(focus.anchoredPosition)) throw new Exception("Shade covers the highlighted control.");
                    if (shade.GetComponent<UnityEngine.UI.Image>().raycastTarget) throw new Exception("Shade steals full-screen advance clicks.");
                    shadedArea += rectangle.width * rectangle.height;
                }
                var size = ((RectTransform)root.transform).rect.size;
                float hole = (100 + 2 * view.focusPadding) * (40 + 2 * view.focusPadding);
                if (Mathf.Abs(shadedArea + hole - size.x * size.y) > .1f) throw new Exception("Spotlight coverage has gaps or overlap.");
                if (view.next.gameObject != root || !root.GetComponent<UnityEngine.UI.Image>().raycastTarget)
                    throw new Exception("Tour must advance from a full-screen click target.");
                UnityEngine.Object.DestroyImmediate(focus.gameObject);
                int next = 0, skipped = 0;
                view.Bind(font, () => next++, () => skipped++);
                view.next.onClick.Invoke(); view.skip.onClick.Invoke();
                if (next != 1 || skipped != 1) throw new Exception("Guide buttons are not bound correctly.");
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/tutorial-asset-checks.txt", "PASS: native Unity data/mail checks; real prefab and bitmap-font references; eight short captions fit; full-screen advance/skip callbacks; spotlight hole and shade coverage; input interception.");
                Debug.Log("TUTORIAL_ASSET_CHECKS_PASS");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }
    }
    // END ADDED
}
