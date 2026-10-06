using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

// Exercises the saved scene wiring and real 2D contacts in an isolated test project.
[InitializeOnLoad]
public static class TreeHitAudioChecks
{
    const string Key = "TreeHitAudioChecks.Active";
    static int frames;
    static string runtimeError;
    static TreeHitAudioChecks() { EditorApplication.update += Tick; }
    static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }
    public static void RunBatch()
    {
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            var cursor = Object.FindAnyObjectByType<CursorController>();
            Check(cursor != null && cursor.chopHitbox != null, "Scene weapon reference missing");
            Check(cursor.chopHitbox.GetComponent<TreeHitAudio>() != null, "Saved scene listener is not on the real weapon");
            Check(cursor.GetComponent<TreeHitAudio>() == null, "Saved scene retains the obsolete cursor listener");
            // Simulate an old scene, then repeat setup to catch future regressions.
            Object.DestroyImmediate(cursor.chopHitbox.GetComponent<TreeHitAudio>());
            cursor.gameObject.AddComponent<TreeHitAudio>();
            TownAudioSetup.ConfigureTreeHits();
            TownAudioSetup.ConfigureTreeHits();
            Check(cursor.GetComponent<TreeHitAudio>() == null, "Legacy listener was not migrated");
            Check(cursor.chopHitbox.GetComponents<TreeHitAudio>().Length == 1, "Setup must be idempotent");
            Check(cursor.chopHitbox.treeHitAudio == cursor.chopHitbox.GetComponent<TreeHitAudio>(), "Setup did not cache the weapon audio");
            // Reopen the actual saved scene: runtime checks must not rely on setup running.
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            EditorSettings.enterPlayModeOptionsEnabled = false;
            SessionState.SetBool(Key, true);
            SessionState.SetFloat(Key + ".Start", (float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying = true;
        }
        catch (Exception error) { Finish(error); }
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Start", 0) > 90)
        { Finish(new TimeoutException("Tree-hit audio check timed out")); return; }
        if (!EditorApplication.isPlaying) return;
        if (++frames == 1) Application.logMessageReceived += CaptureError;
        if (frames < 20) return;
        try
        {
            var cursor = Object.FindAnyObjectByType<CursorController>();
            var hitbox = cursor.chopHitbox;
            var listener = hitbox.GetComponent<TreeHitAudio>();
            var audio = TownAudio.Instance;
            Check(runtimeError == null, "Runtime error: " + runtimeError);
            Check(hitbox.treeHitAudio == listener && hitbox.GetComponents<TreeHitAudio>().Length == 1,
                "Awake must cache the existing audio without adding a duplicate");
            var spawned = new GameObject("Runtime hitbox check").AddComponent<ChopHitbox>();
            Check(spawned.treeHitAudio != null && spawned.GetComponents<TreeHitAudio>().Length == 1,
                "Runtime-created hitbox needs exactly one audio component");
            spawned.enabled = false; spawned.enabled = true;
            Check(spawned.GetComponents<TreeHitAudio>().Length == 1, "Re-enabling duplicated runtime audio");
            Object.DestroyImmediate(spawned.gameObject);
            Check(audio != null && audio.axe != null && audio.axe.clip != null && audio.axe.volume > 0,
                "Axe AudioSource or imported clip is missing/muted");
            cursor.enabled = false;
            cursor.weaponAnimator.enabled = false;
            Physics2D.simulationMode = SimulationMode2D.Script;
            var tree = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Tree.prefab"),
                new Vector3(10000, 10000, 0), Quaternion.identity).GetComponent<Tree>();
            var hurtbox = tree.GetComponentInChildren<TreeHurtbox>();
            var weaponCollider = hitbox.GetComponent<Collider2D>();
            Check(hurtbox != null && weaponCollider != null, "Real tree/weapon colliders missing");
            weaponCollider.enabled = true;
            cursor.transform.position = new Vector3(10020, 10020, 0);
            Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            audio.axe.Stop();
            Physics2D.Simulate(.02f);
            Check(!audio.axe.isPlaying, "An empty swing should be silent");
            int health = tree.health;
            hitbox.transform.position = hurtbox.GetComponent<Collider2D>().bounds.center;
            Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            Check(tree.health == health - 1, "Real contact must deliver one hit and damage");
            Check(audio.axe.isPlaying, "Real contact did not play the axe clip");
            audio.axe.Stop();
            hitbox.transform.position += Vector3.right * 20;
            Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            listener.enabled = false;
            hitbox.transform.position = hurtbox.GetComponent<Collider2D>().bounds.center;
            Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            Check(tree.health == health - 2, "Disabling audio should not disable damage");
            Check(!audio.axe.isPlaying, "Disabled audio listener still plays");
            listener.enabled = true;
            listener.enabled = false;
            listener.enabled = true;
            hitbox.transform.position += Vector3.right * 20;
            Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            hitbox.transform.position = hurtbox.GetComponent<Collider2D>().bounds.center;
            Physics2D.SyncTransforms(); Physics2D.Simulate(.02f);
            Check(audio.axe.isPlaying, "Listener failed after re-enabling");
            Check(tree.health == health - 3 && hitbox.GetComponents<TreeHitAudio>().Length == 1,
                "Re-enabling duplicated damage or weapon audio");
            Check(runtimeError == null, "Runtime error: " + runtimeError);
            Finish(null);
        }
        catch (Exception error) { Finish(error); }
    }
    static void CaptureError(string message, string stack, LogType type)
    {
        // Fresh batch projects can fail editor search indexing independently of gameplay.
        if (message.StartsWith("ArgumentOutOfRangeException:") &&
            stack.Contains("UnityEditor.Search.SearchDatabase") && !stack.Contains("Assets/")) return;
        if (type == LogType.Error || type == LogType.Exception) runtimeError = message;
    }
    static void Finish(Exception error)
    {
        SessionState.SetBool(Key, false);
        Application.logMessageReceived -= CaptureError;
        if (error != null) Debug.LogError("TREE_HIT_AUDIO_FAIL: " + error);
        else Debug.Log("TREE_HIT_AUDIO_PASS: direct playback, cached authored component, runtime fallback, legacy migration, repeated setup, real physics damage and axe playback, silent misses, disable/re-enable without duplicates.");
        EditorApplication.Exit(error == null ? 0 : 1);
    }
}
