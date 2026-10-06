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
        Check(prompt.text.Contains("WASD") && prompt.text.Contains("LEFT CLICK"), "explicit movement and chopping inputs");
        Check(prompt.font == demo.Settings.upgradePixelFont && prompt.fontStyle == FontStyle.Normal, "tutorial shares the sharp UI font");
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
        // BEGIN ADDED: The icon HUD follows actual pickups and preserves the tutorial's Tab announcement.
        // BEGIN CHANGED: The real pickup advances guidance immediately; the displayed wallet waits for its flight.
        if (demo.View.pickupFeedback != null)
            yield return new WaitForSecondsRealtime(demo.View.pickupFeedback.flightPrefab.duration + .1f);
        // END CHANGED
        Check(demo.View.hudWallet.text == demo.Progress.Cheeses.ToString() && demo.View.hudUnreadDot.activeSelf, "HUD shares pickup currency and unread state");
        Check(demo.View.closedUnread.text == "Press TAB to open messages." && demo.View.closedUnread.gameObject.activeInHierarchy, "updated message tutorial hint");
        var hudPosition = demo.View.worldHud.anchoredPosition;
        Check(hudPosition.x < 0 && hudPosition.y > 0, "gameplay HUD is at the upper left");
        // END ADDED
        // BEGIN CHANGED: Each overlay click introduces one feature without invoking its real action.
        var coach = demo.GetComponentInChildren<TutorialCoachView>(true);
        Check(coach != null && coach.shades.Length == 4, "spotlight overlay exists");
        int balance = demo.Progress.Cheeses, unread = demo.Progress.UnreadCount;
        int[] expectedPages = { -1, 2, 2, 2, 1, 1, 0, 0 };
        for (int step = 0; step < TownProgress.InteractionGuideCount; step++)
        {
            demo.CompleteUITransitions(); yield return null;
            Check(demo.Progress.InteractionGuideStep == step && coach.gameObject.activeSelf, "one click per spotlight");
            Check(demo.BlocksWorldInput, "tour clicks cannot move or chop behind overlay");
            if (step > 0) Check(demo.View.Page == expectedPages[step] && demo.PhoneOpen, "tour previews the right page");
            Check(coach.body.font == demo.Settings.upgradePixelFont && UpgradeRowView.Fits(coach.body.text, coach.body), "short pixel caption fits");
            coach.next.onClick.Invoke(); yield return null;
        }
        demo.CompleteUITransitions(); yield return null;
        Check(!coach.gameObject.activeSelf && !demo.PhoneOpen && !demo.BlocksWorldInput, "tour returns to free exploration");
        Check(demo.Progress.Cheeses == balance && demo.Progress.UnreadCount == unread, "tour never buys, collects or marks mail read");
        demo.Progress.CollectWorld(10);
        demo.TogglePhone(); demo.CompleteUITransitions(); yield return null;
        Check(demo.View.letter.text == demo.Progress.Letters[demo.Progress.MailboxEntryIndex].Body, "mail entry selects latest unread");
        string reading = demo.View.letter.text;
        demo.Progress.CollectWorld(100); demo.Refresh();
        Check(demo.View.letter.text == reading, "incoming mail does not interrupt reading");
        demo.TogglePhone(); demo.CompleteUITransitions(); demo.TogglePhone(); demo.CompleteUITransitions(); yield return null;
        Check(demo.View.letter.text == demo.Progress.Letters[demo.Progress.MailboxEntryIndex].Body, "reopen selects new mail");
        Check(!coach.gameObject.activeSelf, "completed tour stays dismissed");
        // END CHANGED
        Debug.Log("TUTORIAL_CHECKS_PASS: nearest tree, English chop/pickup instructions, nonblocking overlay, actual drop/pickup, Tab highlight, click-through spotlights, page previews, no unintended actions, latest unread mail, completion.");
        Destroy(gameObject);
    }
}
#endif
