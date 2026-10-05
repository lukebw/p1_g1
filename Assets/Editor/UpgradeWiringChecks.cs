// BEGIN ADDED: Audit real purchases, spawned cheese, 2D pickup physics and upgrade reapplication.
using System;
using System.IO;
using System.Linq;
using CheeseTownPhone;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

[InitializeOnLoad]
public static class UpgradeWiringChecks
{
    const string Key = "UpgradeWiringChecks.Active";
    static int phase, frames, wallet;
    static double waitUntil;
    static CheeseTownDemo demo;
    static PlayerController player, newPlayer;
    static CollectionRadius collector;
    static CircleCollider2D circle;
    static GameObject pickup;
    static float baseRadius;
    static UpgradeWiringChecks() { EditorApplication.update += Tick; }
    static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    static void Near(float a, float b, string message) { Check(Mathf.Abs(a - b) < .015f, message + $" ({a} vs {b})"); }
    [MenuItem("Cheese Town/Run Upgrade Wiring Checks")]
    public static void RunBatch()
    {
        if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before checks.");
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        CheeseTownPhone.Editor.PhoneDemoChecks.DataChecks();
        EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
        SessionState.SetBool(Key + ".Fast", EditorSettings.enterPlayModeOptionsEnabled);
        EditorSettings.enterPlayModeOptionsEnabled = false;
        SessionState.SetFloat(Key + ".Time", (float)EditorApplication.timeSinceStartup);
        SessionState.SetBool(Key, true); EditorApplication.isPlaying = true;
    }
    static void Buy(string id)
    {
        var option = demo.Settings.upgrades.Single(o => o.id == id);
        int price = option.levels[demo.Progress.Level(option)].price, before = demo.Progress.Cheeses;
        if (!demo.PhoneOpen) demo.TogglePhone();
        demo.ShowPage(1); demo.CompleteUITransitions();
        Canvas.ForceUpdateCanvases();
        foreach (var row in demo.GetComponentsInChildren<UpgradeRowView>())
        {
            Check(UpgradeRowView.Fits(row.title.text, row.title), "Upgrade title fits: " + row.Option.id);
            Check(UpgradeRowView.Fits(row.effectValue.text, row.effectValue), "Effect values fit: " + row.Option.id);
        }
        var button = demo.GetComponentsInChildren<Button>().Single(b => b.name == "Buy " + id);
        Check(button.interactable, "Shop purchase is enabled: " + id); button.onClick.Invoke();
        demo.CompleteUITransitions();
        Check(demo.Progress.Cheeses == before - price, "Purchase debits once: " + id);
    }
    static void Drops(string path, int min, int max, float multiplier)
    {
        var tree = Object.Instantiate(AssetDatabase.LoadAssetAtPath<Tree>(path), new Vector3(80, 60, 0), Quaternion.identity);
        Near(tree.transform.localScale.x, demo.Progress.WildTreeScale, "New tree inherits purchased scale");
        var before = Object.FindObjectsByType<CheeseDropMotion>().ToHashSet();
        tree.TakeDamage(100);
        var drops = Object.FindObjectsByType<CheeseDropMotion>().Where(d => !before.Contains(d)).ToArray();
        Check(drops.Length >= Mathf.CeilToInt(min * multiplier) && drops.Length <= Mathf.CeilToInt(max * multiplier), path + " actual drop count at x" + multiplier + ": " + drops.Length);
        tree.TakeDamage(100);
        Check(Object.FindObjectsByType<CheeseDropMotion>().Length == before.Count + drops.Length, "Stump cannot pay twice");
        Debug.Log($"UPGRADE_DROP: {path}, multiplier {multiplier}, actual {drops.Length}");
        foreach (var drop in drops) Object.DestroyImmediate(drop.gameObject);
        Object.DestroyImmediate(tree.gameObject);
    }
    static void SpawnPickup(float closestDistance)
    {
        var tree = Object.FindAnyObjectByType<Tree>();
        pickup = Object.Instantiate(tree.cheesePrefab, player.transform.position + Vector3.right * 20, Quaternion.identity);
        pickup.SetActive(true); Physics2D.SyncTransforms();
        var c = pickup.GetComponent<Collider2D>();
        Vector3 point = circle.bounds.center + Vector3.right * (closestDistance + c.bounds.extents.x);
        pickup.transform.position += point - c.bounds.center;
        var body = pickup.GetComponent<Rigidbody2D>(); if (body != null) body.position = pickup.transform.position;
        Physics2D.SyncTransforms(); wallet = demo.Progress.Cheeses;
    }
    static void Tick()
    {
        if (!SessionState.GetBool(Key, false)) return;
        if (EditorApplication.timeSinceStartup - SessionState.GetFloat(Key + ".Time", 0) > 100) { Finish(new TimeoutException("Upgrade checks timed out")); return; }
        if (!EditorApplication.isPlaying || ++frames < 8 || EditorApplication.timeSinceStartup < waitUntil) return;
        try
        {
            if (phase == 0)
            {
                demo = Object.FindAnyObjectByType<CheeseTownDemo>(); player = Object.FindAnyObjectByType<PlayerController>();
                collector = player.GetComponentInChildren<CollectionRadius>(); circle = collector.GetComponent<CircleCollider2D>();
                TownSession.Instance.enabled = false; // Advance the economy explicitly for exact assertions.
                baseRadius = circle.radius; demo.Progress.Grant(100000); demo.Progress.CollectWorld(1);
                Near(player.MovementDelta(Vector2.one, 1).magnitude, 4, "Normalized base movement");
                Buy("move-speed"); Near(player.MovementDelta(Vector2.one, 1).magnitude, 6, "First movement upgrade");
                Buy("move-speed"); Near(player.MovementDelta(Vector2.one, 1).magnitude, 8, "Second movement upgrade");
                Check(player.playerAnimator.GetBool("isFast"), "Purchased fast animation");
                Drops("Assets/Prefab/Tree.prefab", 2, 4, 1); Drops("Assets/Prefab/Tree_02.prefab", 8, 10, 1);
                int before = demo.Progress.Cheeses; demo.Progress.Tick(1);
                Check(demo.Progress.Cheeses == before, "Passive stock does not credit before auto upgrade");
                Buy("auto-collect"); before = demo.Progress.Cheeses; demo.Progress.Tick(1);
                Check(demo.Progress.Cheeses == before + 1, "Base automatic collection pays shared-stock rate");
                float townArtScale = demo.View.tree.localScale.x;
                foreach (int production in new[] { 2, 4, 8 })
                {
                    Buy("tree-growth"); Near(demo.Progress.Production, production, "Town production");
                    Near(demo.Progress.WildTreeYieldMultiplier, 1, "Town upgrade preserves wild yield");
                    Near(demo.Progress.WildTreeScale, 1, "Town upgrade preserves wild size");
                    Drops("Assets/Prefab/Tree.prefab", 2, 4, 1);
                    Drops("Assets/Prefab/Tree_02.prefab", 8, 10, 1);
                    before = demo.Progress.Cheeses; demo.Progress.Tick(1);
                    Check(demo.Progress.Cheeses == before + production, "Auto follows town production");
                }
                foreach (float multiplier in new[] { 1.25f, 1.5f, 2f })
                {
                    Buy("wild-tree-growth"); Near(demo.Progress.WildTreeYieldMultiplier, multiplier, "Wild range multiplier");
                    Near(demo.Progress.Production, 8, "Wild upgrade preserves town production");
                    Near(demo.View.tree.localScale.x, townArtScale, "Town illustration stays fixed");
                    var option = demo.Settings.upgrades.Single(o => o.id == "wild-tree-growth");
                    Near(demo.Progress.WildTreeScale, option.levels[demo.Progress.Level(option)-1].treeScale, "Wild size follows its own level");
                    Drops("Assets/Prefab/Tree.prefab", 2, 4, multiplier);
                    Drops("Assets/Prefab/Tree_02.prefab", 8, 10, multiplier);
                    before = demo.Progress.Cheeses; demo.Progress.Tick(1);
                    Check(demo.Progress.Cheeses == before + 8, "Wild upgrade preserves auto rate");
                }
                demo.GetComponentsInChildren<Button>().Single(b => b.name == "Tree filter").onClick.Invoke();
                demo.CompleteUITransitions();
                Check(demo.GetComponentsInChildren<Button>().Any(b => b.name == "Buy tree-growth") &&
                    demo.GetComponentsInChildren<Button>().Any(b => b.name == "Buy wild-tree-growth"), "Both upgrades appear under TREE");
                demo.GetComponentsInChildren<Button>().Single(b => b.name == "Player filter").onClick.Invoke();
                demo.CompleteUITransitions();
                Check(!demo.GetComponentsInChildren<Button>().Any(b => b.name == "Buy tree-growth" || b.name == "Buy wild-tree-growth"), "Tree upgrades excluded from PLAYER");
                demo.GetComponentsInChildren<Button>().Single(b => b.name == "All filter").onClick.Invoke();
                demo.CompleteUITransitions();
                Buy("double-cheese"); Check(demo.Progress.UnitPrice == 2, "Value upgrade applies");
                before = demo.Progress.Cheeses; demo.Progress.Tick(1);
                Check(demo.Progress.Cheeses == before + 16, "Auto production and unit value combine once");
                Drops("Assets/Prefab/Tree.prefab", 2, 4, 2); Drops("Assets/Prefab/Tree_02.prefab", 8, 10, 2);
                demo.View.SetOpen(false, false);
                player.transform.position = new Vector3(65, -60, 0); Physics2D.SyncTransforms();
                SpawnPickup(1.75f); phase = 1;
            }
            else if (phase == 1)
            {
                Check(pickup != null && pickup.activeSelf && demo.Progress.Cheeses == wallet, "No distant pickup at base contact radius");
                Buy("collect-range"); Near(circle.radius * Mathf.Abs(circle.transform.lossyScale.x), 2, "Actual 2D radius grows to two world units");
                wallet = demo.Progress.Cheeses;
                phase = 2;
            }
            else if (phase == 2)
            {
                Check(pickup == null && demo.Progress.Cheeses == wallet + 2, "Upgraded trigger physically picks up existing nearby cheese with double value");
                SpawnPickup(3.5f); phase = 3;
            }
            else if (phase == 3)
            {
                Check(pickup != null && pickup.activeSelf && demo.Progress.Cheeses == wallet, "Radius two cannot reach distant cheese");
                Buy("collect-range"); Near(circle.radius * Mathf.Abs(circle.transform.lossyScale.x), 4, "Actual 2D radius grows to four world units");
                wallet = demo.Progress.Cheeses;
                phase = 4;
            }
            else if (phase == 4)
            {
                Check(pickup == null && demo.Progress.Cheeses == wallet + 2, "Radius four physically picks up cheese once");
                var hurtbox = Object.FindObjectsByType<TreeHurtbox>().First(h => h.enabled && !h.tree.IsChopped);
                var bounds = hurtbox.GetComponent<Collider2D>().bounds;
                Vector3 point = new Vector3(bounds.max.x + 3, bounds.center.y, 0);
                Near(hurtbox.DistanceFrom(point), 3, "Manual collection uses real 2D surface distance");
                player.transform.position = point; Check(player.CanCollect(hurtbox), "Manual collection inherits radius four");
                int before = demo.Progress.Cheeses; int collected = hurtbox.Collect();
                Check(collected > 0 && demo.Progress.Cheeses == before + collected * 2, "Manual stock collection respects upgraded value");
                // Reload the saved scene so runtime-mutated collider/animator values cannot mask missing wiring.
                EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Wilderness.unity",
                    new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                phase = 5;
            }
            else
            {
                demo = Object.FindAnyObjectByType<CheeseTownDemo>();
                newPlayer = player = Object.FindAnyObjectByType<PlayerController>();
                collector = player.GetComponentInChildren<CollectionRadius>(); circle = collector.GetComponent<CircleCollider2D>();
                Near(newPlayer.speed.x, 8, "New player inherits speed after Start");
                Check(newPlayer.playerAnimator.GetBool("isFast"), "Start preserves purchased fast animation");
                var newCircle = newPlayer.GetComponentInChildren<CollectionRadius>().GetComponent<CircleCollider2D>();
                Near(newCircle.radius * Mathf.Abs(newCircle.transform.lossyScale.x), 4, "New player inherits physical pickup radius");
                Near(Object.FindAnyObjectByType<Tree>().transform.localScale.x, demo.Progress.WildTreeScale, "Reloaded map trees inherit growth");
                // Disable options in a transient settings copy: existing subscriptions must also remove effects.
                var config = Object.Instantiate(demo.Settings);
                foreach (var option in config.upgrades) option.available = false;
                config.ValidateSettings(); TownSession.Instance.Configure(config);
                Near(player.speed.x, 4, "Disabled speed upgrade resets movement");
                Check(!player.playerAnimator.GetBool("isFast"), "Disabled speed upgrade resets animation");
                Near(circle.radius, baseRadius, "Disabled range restores authored contact radius");
                Near(demo.Progress.WildTreeYieldMultiplier, 1, "Disabled growth restores base drops");
                Check(!demo.Progress.AutoEnabled && demo.Progress.UnitPrice == 1, "Disabled auto/value upgrades reset");
                Finish(null); return;
            }
            waitUntil = EditorApplication.timeSinceStartup + .4;
        }
        catch (Exception error) { Finish(error); }
    }
    static void Finish(Exception error)
    {
        SessionState.SetBool(Key, false); EditorSettings.enterPlayModeOptionsEnabled = SessionState.GetBool(Key + ".Fast", false);
        Directory.CreateDirectory("Logs");
        string result = error == null ? "PASS: real shop purchases; movement 4/6/8 and animation; both rows under TREE; independent town production and wild size/drop range; ordinary/02 physical drops at 1x/1.25x/1.5x/2x; no stump double pay; stock production and auto income at all tiers; doubled value without doubled quantity; real 2D trigger pickups at base/2/4 range; existing drops collected after upgrade; 2D manual collection reach; newly spawned player inherits effects; disabled upgrades reset effects." : "FAIL: " + error;
        File.WriteAllText("Logs/upgrade-wiring-checks.txt", result);
        if (error == null) Debug.Log(result); else Debug.LogException(error);
        if (Application.isBatchMode) EditorApplication.Exit(error == null ? 0 : 1); else EditorApplication.isPlaying = false;
    }
}
// END ADDED
