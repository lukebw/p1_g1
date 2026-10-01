// BEGIN ADDED: Integration checks catch broken prefab wiring and pickup timing.
using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[InitializeOnLoad]
public static class TreeFeedbackChecks
{
    // BEGIN ADDED: Persist the request across the Play mode domain reload.
    const string Request = "TreeFeedbackChecks.Running";
    static TreeFeedbackChecks()
    {
        EditorApplication.update += CheckRunner;
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode) CheckRunner();
            if (state == PlayModeStateChange.EnteredEditMode && SessionState.GetBool(Request + ".Restore", false))
            {
                EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Request + ".FastPlay", false);
                SessionState.SetBool(Request + ".Restore", false);
            }
        };
    }

    // BEGIN ADDED: A watchdog avoids lost callbacks and endless background validation.
    static void CheckRunner()
    {
        if (!SessionState.GetBool(Request, false)) return;
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Request + ".Started", 0) > 60)
        {
            Finish(new TimeoutException("Tree feedback checks did not finish within 60 seconds."));
            return;
        }
        if (!EditorApplication.isPlaying || SessionState.GetBool(Request + ".Runner", false)) return;
        SessionState.SetBool(Request + ".Runner", true);
        Debug.Log("TREE_FEEDBACK_CHECKS_START");
        TreeFeedbackCheckRunner runner = new GameObject("Tree feedback checks").AddComponent<TreeFeedbackCheckRunner>();
        runner.Completed = Finish;
    }
    // END ADDED

    [MenuItem("Cheese Town/Run Tree Feedback Checks")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play mode before running checks.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        SessionState.SetBool(Request + ".FastPlay", EditorSettings.enterPlayModeOptionsEnabled);
        SessionState.SetBool(Request + ".Restore", true);
        EditorSettings.enterPlayModeOptionsEnabled = false;
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        SessionState.SetBool(Request, true);
        SessionState.SetBool(Request + ".Runner", false);
        SessionState.SetFloat(Request + ".Started", (float)EditorApplication.timeSinceStartup);
        Debug.Log("TREE_FEEDBACK_CHECKS_REQUESTED");
        EditorApplication.isPlaying = true;
    }

    public static void Finish(Exception error)
    {
        Time.captureFramerate = 0;
        SessionState.SetBool(Request, false);
        Directory.CreateDirectory("Logs");
        string result = error == null
            ? "PASS: prefab imports; one hit per click after idle/re-enable; fixed root during shake; leaves emitted; aligned stump and smaller collision; no repeated payouts; inclusive 2-4 and fixed 3-3 counts; canopy descent and landing; pickup only after settling, including existing overlap."
            : "FAIL: " + error;
        File.WriteAllText("Logs/tree-feedback-checks.txt", result);
        if (error == null) Debug.Log(result); else Debug.LogException(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1);
        else EditorApplication.isPlaying = false;
    }
    // END ADDED
}

// END ADDED
