using System;
using System.Linq;
using System.Reflection;
using CheeseTownPhone;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class GameplayTuningChecks
{
    const string Key = "GameplayTuningChecks.Active";
    static int frames;
    static Action pendingButtons;
    static int readyFrame;
    static GameplayTuningChecks() { EditorApplication.update += Tick; }
    public static void RunBatch()
    {
        SessionState.SetBool(Key, true);
        TreeHitAudioChecks.RunBatch();
    }
    static void Check(bool ok, string reason) { if (!ok) throw new Exception(reason); }
    static void Tick()
    {
        if (pendingButtons != null && Time.frameCount >= readyFrame)
        {
            var verify = pendingButtons; pendingButtons = null;
            try { verify(); Debug.Log("GAMEPLAY_TUNING_PASS: base radius retained; physical pickup radii 3/12; immediate button and child raycasts, disabled button, empty space; music at 999/1000 and after spending."); }
            catch (Exception error) { Debug.LogError("GAMEPLAY_TUNING_FAIL: " + error); EditorApplication.Exit(1); }
            return;
        }
        if (!SessionState.GetBool(Key, false) || !EditorApplication.isPlaying || ++frames < 8) return;
        SessionState.SetBool(Key, false);
        try
        {
            var session = TownSession.Instance; var p = session.Progress;
            var magnet = session.Settings.upgrades.Single(o => o.id == "collect-range");
            var collector = Object.FindAnyObjectByType<CollectionRadius>().GetComponent<CircleCollider2D>();
            Check(p.CollectRange == 0 && collector.radius > 0, "Initial physical pickup radius remains intact");
            p.Grant(90); Check(p.Buy(magnet), "First magnet purchase");
            Check(p.CollectRange == 3 && Mathf.Abs(collector.radius * collector.transform.lossyScale.x - 3) < .001f,
                "First upgrade must reach three world units");
            Check(p.Buy(magnet), "Second magnet purchase");
            Check(p.CollectRange == 12 && Mathf.Abs(collector.radius * collector.transform.lossyScale.x - 12) < .001f,
                "Final upgrade must reach twelve world units");
            var audio = TownAudio.Instance;
            p.Grant(999);
            Check(audio.mainMusic.isPlaying && !audio.endingMusic.isPlaying, "Music must not switch at 999");
            p.Grant(1);
            Check(!audio.mainMusic.isPlaying && audio.endingMusic.isPlaying, "Music must switch at 1000");
            Check(p.Buy(session.Settings.upgrades.Single(o => o.id == "move-speed")), "Spend after milestone");
            Check(p.Cheeses < 1000 && audio.endingMusic.isPlaying && !audio.mainMusic.isPlaying, "Spending must not revert music");
            CheckButtons();
        }
        catch (Exception error) { Debug.LogError("GAMEPLAY_TUNING_FAIL: " + error); EditorApplication.Exit(1); }
    }
    static void CheckButtons()
    {
        var canvases = Object.FindObjectsByType<Canvas>();
        var enabled = canvases.Select(c => c.enabled).ToArray();
        foreach (var canvas in canvases) canvas.enabled = false;
        var raycasters = Object.FindObjectsByType<GraphicRaycaster>();
        var raycasting = raycasters.Select(r => r.enabled).ToArray();
        foreach (var raycaster in raycasters) raycaster.enabled = false;
        var root = new GameObject("Button input check", typeof(RectTransform), typeof(Canvas), typeof(GraphicRaycaster));
            root.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var button = new GameObject("Button", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Button>();
            var rect = (RectTransform)button.transform; rect.SetParent(root.transform, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = Vector2.zero;
            rect.anchoredPosition = new Vector2(10,10); rect.sizeDelta = new Vector2(120,60);
            var child = new GameObject("Button graphic", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            child.SetParent(rect, false); child.anchorMin = Vector2.zero; child.anchorMax = Vector2.one;
            child.offsetMin = child.offsetMax = Vector2.zero;
            Canvas.ForceUpdateCanvases();
        // New test graphics need a rendered frame before Unity assigns their raycast depth.
        readyFrame = Time.frameCount + 2;
        pendingButtons = () => {
        try
        {
            var cursor = Object.FindAnyObjectByType<CursorController>();
            var method = typeof(CursorController).GetMethod("IsPointerOverButton", BindingFlags.Instance | BindingFlags.NonPublic);
            Func<Vector2,bool> blocked = point => (bool)method.Invoke(cursor, new object[] { point });
            Check(blocked(new Vector2(40,30)), "Immediate raycast must find a button through its child graphic");
            button.interactable = false;
            Check(blocked(new Vector2(40,30)), "Disabled buttons still shield world clicks");
            Check(!blocked(new Vector2(Screen.width - 10,Screen.height - 10)), "Empty world space must allow chopping");
        }
        finally
        {
            Object.DestroyImmediate(root);
            for (int i = 0; i < canvases.Length; i++) canvases[i].enabled = enabled[i];
            for (int i = 0; i < raycasters.Length; i++) raycasters[i].enabled = raycasting[i];
        }
        };
    }
}
