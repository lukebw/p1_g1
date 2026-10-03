#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using CheeseTownPhone;
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

public sealed class TutorialCheckRunner : MonoBehaviour
{
    [MenuItem("Cheese Town/Run Tutorial Checks (Play Mode)")]
    static void Run()
    {
        if (!EditorApplication.isPlaying) throw new Exception("Start a fresh Wilderness Play session first.");
        new GameObject("Tutorial integration checks").AddComponent<TutorialCheckRunner>();
    }
    static void Check(bool value, string message) { if (!value) throw new Exception("TUTORIAL_CHECK_FAILED: " + message); }
    IEnumerator Start()
    {
        var demo = FindAnyObjectByType<CheeseTownDemo>();
        var player = FindAnyObjectByType<PlayerController>();
        Check(demo != null && player != null && demo.HarvestTutorialActive, "initial harvest stage");
        var target = demo.TutorialTree;
        var closest = FindObjectsByType<Tree>(FindObjectsSortMode.None).Where(t => !t.IsChopped)
            .OrderBy(t => (t.transform.position - player.transform.position).sqrMagnitude).First();
        Check(target == closest, "nearest tree selected");
        var overlay = demo.ScreenCanvas.transform.Find("First cheese tutorial");
        Check(overlay.GetComponentsInChildren<Graphic>().All(g => !g.raycastTarget), "overlay does not block input");
        var prompt = overlay.GetComponentInChildren<Text>();
        Check(prompt.text == "Use WASD to move toward a cheese tree,\nthen chop it with your axe to get cheese.", "English movement and chopping instruction");
        var launcher = demo.GetComponentsInChildren<Button>().Single(b => b.name == "Open tablet");
        Check(!launcher.interactable && !launcher.GetComponent<Outline>().enabled, "mail waits for first pickup");
        demo.TogglePhone();
        Check(!demo.PhoneOpen, "early Tab keeps world visible");
        // Exercise the actual tree drop and physics pickup paths, without granting currency directly.
        target.TakeDamage(target.health);
        yield return new WaitForSeconds(2);
        Check(demo.Progress.TotalCollected == 0 && demo.HarvestTutorialActive, "chopping alone is not collection");
        Check(prompt.text == "Move closer to pick up the cheese.", "chopping switches to pickup instruction");
        var drop = FindObjectsByType<CheeseDropMotion>(FindObjectsSortMode.None).First(d => d.IsSettled);
        player.transform.position = drop.transform.position;
        Physics2D.SyncTransforms();
        yield return new WaitForSeconds(.5f);
        Check(demo.Progress.TotalCollected > 0 && !demo.HarvestTutorialActive, "real pickup advances tutorial");
        Check(!overlay.gameObject.activeSelf, "shade clears after pickup");
        Check(launcher.interactable && launcher.GetComponent<Outline>().enabled && !demo.PhoneOpen, "Tab glows without forced popup");
        launcher.onClick.Invoke();
        Check(demo.PhoneOpen && demo.GetComponentsInChildren<Text>().Any(t => t.name == "Mayor message" && t.text.Contains("Welcome to Cheese Town!")), "normal welcome letter opens");
        demo.ReplyToMayor();
        demo.TogglePhone();
        Check(!launcher.GetComponent<Outline>().enabled && !demo.HarvestTutorialActive, "reading completes guidance");
        Debug.Log("TUTORIAL_CHECKS_PASS: nearest tree, English chop/pickup instructions, nonblocking overlay, actual drop/pickup, Tab highlight, welcome letter, completion.");
        Destroy(gameObject);
    }
}
#endif
