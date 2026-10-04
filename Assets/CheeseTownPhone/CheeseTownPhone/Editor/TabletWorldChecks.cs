// BEGIN ADDED: Reuse the real tutorial/drop runner to guard the new prefab's connection to Wilderness.
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CheeseTownPhone.Editor
{
    [InitializeOnLoad]
    public static class TabletWorldChecks
    {
        const string Key = "TabletWorldChecks.Live";
        static int frames;
        static TabletWorldChecks()
        {
            EditorApplication.update += Tick;
            Application.logMessageReceived += (message, trace, type) => {
                if (!SessionState.GetBool(Key, false)) return;
                if (message.StartsWith("TUTORIAL_CHECKS_PASS")) Finish(true, message);
                else if (message.Contains("TUTORIAL_CHECK_FAILED") || type == LogType.Exception && Application.isPlaying
                    && SessionState.GetBool(Key + ".Started", false)) Finish(false, message + "\n" + trace);
            };
        }
        [MenuItem("Cheese Town/Run Tablet World Checks")]
        public static void RunBatch()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before running checks.");
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            SessionState.SetBool(Key + ".FastPlay", EditorSettings.enterPlayModeOptionsEnabled);
            SessionState.SetBool(Key + ".Started", false);
            SessionState.SetFloat(Key + ".Time", (float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Key, true); EditorSettings.enterPlayModeOptionsEnabled = false; EditorApplication.isPlaying = true;
        }
        static void Tick()
        {
            if (!SessionState.GetBool(Key, false)) return;
            if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Time", 0) > 90) { Finish(false, "World checks timed out."); return; }
            if (!EditorApplication.isPlaying || ++frames < 5 || SessionState.GetBool(Key + ".Started", false)) return;
            SessionState.SetBool(Key + ".Started", true);
            var demo = UnityEngine.Object.FindAnyObjectByType<CheeseTownDemo>();
            if (demo == null || demo.View == null) { Finish(false, "Wilderness did not instantiate the tablet prefab."); return; }
            new GameObject("Prefab world integration check").AddComponent<TutorialCheckRunner>();
        }
        static void Finish(bool success, string message)
        {
            SessionState.SetBool(Key, false);
            EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".FastPlay", false);
            Directory.CreateDirectory("Logs"); File.WriteAllText("Logs/tablet-world-checks.txt", (success ? "PASS: " : "FAIL: ") + message);
            if (Application.isBatchMode) EditorApplication.Exit(success ? 0 : 1); else EditorApplication.isPlaying = false;
        }
    }
}
// END ADDED
