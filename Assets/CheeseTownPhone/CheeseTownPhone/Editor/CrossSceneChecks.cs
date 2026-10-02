using System;
using System.Collections;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    [InitializeOnLoad]
    public static class CrossSceneChecks
    {
        static string Request => Path.GetFullPath("Temp/cheese-checks-request");
        static string Result => Path.GetFullPath("Logs/cheese-cross-scene-checks.txt");
        static CrossSceneChecks()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(Request)) return;
                File.Delete(Request);
                Run();
            };
            EditorApplication.playModeStateChanged += state =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool("CheeseChecks", false))
                    new GameObject("Cross-scene test runner").AddComponent<CheeseCheckRunner>();
            };
        }
        [MenuItem("Cheese Town/Run Cross Scene Checks")]
        public static void Run()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Stop Play before running cross-scene checks."); return; }
            try { PhoneDemoChecks.DataChecks(); }
            catch (Exception e) { File.WriteAllText(Result, "FAIL data: " + e); Debug.LogException(e); return; }
            SessionState.SetBool("CheeseChecks", true);
            EditorApplication.isPlaying = true;
        }
        public static void Finish(Exception error)
        {
            Time.timeScale = 1;
            SessionState.SetBool("CheeseChecks", false);
            string result = error == null
                ? "PASS: data checks; real Wilderness player; normalized movement at 4/6/8; shop purchases debit cheeses; collection radius 0/2/4; distant collection blocked; nearby collection pays shared wallet; tree scales 1.2x; production 2/s; auto collection and double cheese payout; scene reload retains wallet, levels, speed, range, tree size; tablet scene return shares progress; no Coins UI; one session and one tablet."
                : "FAIL: " + error;
            File.WriteAllText(Result, result);
            if (error == null) Debug.Log(result); else Debug.LogException(error);
            EditorApplication.isPlaying = false;
        }
    }
    public sealed class CheeseCheckRunner : MonoBehaviour
    {
        void Start() { DontDestroyOnLoad(gameObject); StartCoroutine(Guard()); }
        IEnumerator Guard()
        {
            var steps = Steps();
            while (true)
            {
                object current;
                try { if (!steps.MoveNext()) break; current = steps.Current; }
                catch (Exception e) { CrossSceneChecks.Finish(e); yield break; }
                yield return current;
            }
            CrossSceneChecks.Finish(null);
        }
        static void Check(bool value, string message) { if (!value) throw new Exception(message); }
        static void Near(float actual, float expected, string message) { Check(Mathf.Abs(actual-expected)<.01f, message+": "+actual+" expected "+expected); }
        static void Buy(CheeseTownDemo demo, string id)
        {
            var option = demo.Settings.upgrades.Single(o => o.id == id);
            int price = option.levels[demo.Progress.Level(option)].price;
            int before = demo.Progress.Cheeses;
            demo.Refresh();
            var button = demo.GetComponentsInChildren<Button>(true).Single(b => b.name == "Buy " + id);
            Check(button.interactable, id + " buy enabled");
            button.onClick.Invoke();
            Check(demo.Progress.Cheeses == before-price, id + " debits cheeses");
        }
        IEnumerator Steps()
        {
            Time.timeScale = 0;
            yield return SceneManager.LoadSceneAsync("Wilderness");
            yield return null;
            var session = TownSession.Instance;
            var progress = session.Progress;
            var player = FindAnyObjectByType<PlayerController>();
            var tree = FindAnyObjectByType<TreeHurtbox>();
            var demo = FindAnyObjectByType<CheeseTownDemo>();
            Check(player != null && tree != null && demo != null, "real scene player/tree/tablet");
            Check(!demo.PhoneOpen, "new session leaves world visible");
            Check(progress.Letters[0].Id == "welcome" && !progress.Letters[0].IsRead, "welcome arrives unread");
            demo.TogglePhone();
            Check(demo.PhoneOpen && demo.GetComponentsInChildren<Text>().Any(t => t.text.Contains("Welcome to Cheese Town!")), "tablet opens welcome in mailbox");
            demo.ReplyToMayor();
            Check(progress.Letters[0].IsRead && !progress.GameEnded, "welcome read without ending game");
            demo.TogglePhone();
            Check(ReferenceEquals(progress, demo.Progress), "shared economy");
            Near(player.MovementDelta(Vector2.right, 1).x, 4, "base movement");
            Near(player.MovementDelta(Vector2.one, 1).magnitude, 4, "diagonal normalized");
            Near(player.CollectRange, 0, "base range");
            demo.TogglePhone(); demo.ShowPage(1);
            Buy(demo, "move-speed"); Near(player.MovementDelta(Vector2.right, 1).x, 6, "speed level one");
            Buy(demo, "move-speed"); Near(player.MovementDelta(Vector2.right, 1).x, 8, "speed level two");
            var collider = tree.GetComponent<Collider>();
            player.transform.position = new Vector3(collider.bounds.max.x+3, collider.bounds.center.y, 0);
            Check(!player.CanCollect(tree), "range zero excludes distant tree");
            Buy(demo, "collect-range"); Near(player.CollectRange, 2, "range level one");
            Check(!player.CanCollect(tree), "range two excludes tree three units away");
            int before = progress.Cheeses;
            Check(player.CollectNearby()==0 && progress.Cheeses==before, "out of range cannot collect");
            Buy(demo, "collect-range"); Near(player.CollectRange, 4, "range level two");
            Check(player.CanCollect(tree), "range four includes tree three units away");
            before = progress.Cheeses;
            int amount = player.CollectNearby();
            Check(amount>0 && progress.Cheeses==before+amount*progress.UnitPrice, "world harvest pays shared wallet");
            Vector3 original = tree.transform.parent.localScale;
            Buy(demo, "tree-growth"); Near(tree.transform.parent.localScale.x, original.x*1.2f, "tree grows");
            Near(progress.Production, 2, "production upgraded");
            progress.Grant(1000);
            Buy(demo, "auto-collect"); Buy(demo, "double-cheese");
            before = progress.Cheeses; progress.Tick(1);
            Check(progress.Cheeses == before+4, "auto uses doubled cheese payout");
            Check(demo.GetComponentsInChildren<Text>(true).All(t=>t.text.IndexOf("coin",StringComparison.OrdinalIgnoreCase)<0), "no coins UI");
            progress.ReadLetter(0);
            int letterCount = progress.Letters.Count, unreadCount = progress.UnreadCount;
            long collected = progress.TotalCollected;
            int wallet = progress.Cheeses;
            yield return SceneManager.LoadSceneAsync("CheeseTownPhone");
            yield return null;
            Check(ReferenceEquals(FindAnyObjectByType<CheeseTownDemo>().Progress, progress), "tablet scene retains progress");
            Check(!FindAnyObjectByType<CheeseTownDemo>().PhoneOpen, "mail stays closed after scene change");
            yield return SceneManager.LoadSceneAsync("Wilderness");
            yield return null;
            player = FindAnyObjectByType<PlayerController>(); tree = FindAnyObjectByType<TreeHurtbox>();
            Check(ReferenceEquals(TownSession.Instance.Progress, progress), "scene reload retains same progress");
            Check(progress.Cheeses==wallet, "wallet retained");
            Check(progress.TotalCollected == collected && progress.Letters.Count == letterCount && progress.UnreadCount == unreadCount && progress.Letters[0].IsRead, "mail history, unread and quantity survive scene reload");
            Near(player.speed.x, 8, "new player inherits speed"); Near(player.CollectRange, 4, "new player inherits range");
            Near(tree.transform.parent.localScale.x, original.x*1.2f, "new tree inherits growth");
            Check(FindObjectsByType<TownSession>(FindObjectsSortMode.None).Length==1, "single session");
            Check(FindObjectsByType<CheeseTownDemo>(FindObjectsSortMode.None).Length==1, "single tablet");
            demo = FindAnyObjectByType<CheeseTownDemo>();
            progress.Grant(100000);
            foreach (var option in demo.Settings.upgrades) while (progress.CanBuy(option)) progress.Buy(option);
            Check(!demo.EndingOpen, "final letter delivery does not show ending");
            demo.TogglePhone(); demo.ShowPage(2);
            int finalIndex = progress.Letters.ToList().FindIndex(l => l.Id == TownProgress.FinalLetterId);
            for (int i = 0; i < finalIndex; i++)
                demo.GetComponentsInChildren<Button>().Single(b => b.name == "Next letter").onClick.Invoke();
            demo.GetComponentsInChildren<Button>().Single(b => b.name == "Reply to mayor").onClick.Invoke();
            Check(demo.EndingOpen && demo.PhoneOpen, "reading final letter opens town ending");
            Check(demo.GetComponentsInChildren<Text>().Any(t => t.text == "GAME OVER"), "ending title visible");
            Check(!demo.GetComponentsInChildren<Button>().Any(), "ending hides gameplay controls");
            demo.TogglePhone(); demo.ShowPage(1);
            Check(demo.PhoneOpen && !demo.GetComponentsInChildren<Button>().Any(), "ending cannot reopen gameplay");
            yield return SceneManager.LoadSceneAsync("CheeseTownPhone");
            yield return null;
            demo = FindAnyObjectByType<CheeseTownDemo>();
            Check(demo.EndingOpen && demo.PhoneOpen && demo.GetComponentsInChildren<Text>().Any(t => t.text == "GAME OVER"),
                "ending survives scene reload");
            Time.timeScale = 1;
        }
    }
}
