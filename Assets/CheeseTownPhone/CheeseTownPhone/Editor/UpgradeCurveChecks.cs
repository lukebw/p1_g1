using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CheeseTownPhone.Editor
{
    // BEGIN ADDED: Verify the new curve in an isolated project and expose manual regression menus.
    [InitializeOnLoad]
    public static class UpgradeCurveChecks
    {
        const string Key = "UpgradeCurveChecks.Active";
        static int frames, phase, savedWallet;
        static long savedCollected;
        static TownProgress savedProgress;
        static CheeseTownDemo demo;
        static void Check(bool value, string message) { if (!value) throw new Exception("Upgrade curve: " + message); }
        static void Near(float a, float b, string message) { Check(Mathf.Abs(a - b) < .002f, message + " (" + a + " vs " + b + ")"); }
        static UpgradeOption Option(TabletSettings c, string id) => c.upgrades.Single(o => o.id == id);
        static void Buy(TownProgress p, TabletSettings c, string id)
        {
            var o = Option(c, id); int cost = o.levels[p.Level(o)].price, wallet = p.Cheeses;
            Check(p.Buy(o), "buy " + id); Check(p.Cheeses == wallet - cost, "exact debit " + id);
        }
        static UpgradeCurveChecks() { EditorApplication.update += Tick; }

        [MenuItem("Cheese Town/Run New Upgrade Curve Data Checks")]
        public static void DataChecks()
        {
            var asset = Resources.Load<TabletSettings>("TabletSettings");
            Check(asset != null && asset.useBatchProduction, "new profile is assigned");
            Check(asset.startingCheeses == 0 && asset.initialTreeStock == 0 && asset.collectionGoal == 10000, "zero start and 10k goal");
            var defaults = TabletSettings.Defaults();
            Check(asset.upgrades.Select(o => o.id).SequenceEqual(defaults.Select(o => o.id)), "asset/default IDs");
            Check(asset.upgrades.Sum(o => o.levels.Sum(l => l.price)) == 4840, "total shop cost 4840");
            foreach (var expected in defaults)
            {
                var actual = Option(asset, expected.id);
                Check(actual.title == expected.title && actual.description == expected.description, "asset/default text " + expected.id);
                Check(actual.effect == expected.effect && actual.levels.Count == expected.levels.Count, "asset/default effect and tiers " + expected.id);
                for (int i = 0; i < expected.levels.Count; i++)
                {
                    Check(actual.levels[i].price == expected.levels[i].price, "tier price " + expected.id);
                    Near(actual.levels[i].value, expected.levels[i].value, "tier value " + expected.id);
                    Near(actual.levels[i].treeScale, expected.levels[i].treeScale, "tier size " + expected.id);
                }
            }
            Check(asset.upgrades.Count(o => o.Target == UpgradeTarget.Tree) == 5, "five TREE rows");
            Check(!asset.upgrades.Any(o => o.effect == UpgradeEffect.AutoCollect || o.effect == UpgradeEffect.CheeseValue), "old auto/value removed");
            var c = Object.Instantiate(asset);
            try
            {
                var p = new TownProgress(c);
                p.Tick(10000);
                Check(p.Cheeses == 0 && p.Stock == 0 && p.TotalCollected == 0 && p.Production == 0, "locked tree never generates");
                Check(!p.Buy(Option(c, "tree-start")), "cannot start without 30 cheese");
                p.Grant(6000);
                Check(!p.Buy(Option(c, "tree-batch")) && !p.Buy(Option(c, "tree-interval")) && !p.Buy(Option(c, "tree-capacity")), "three tracks require start");
                Buy(p,c,"tree-start"); Check(p.MainTreeStarted && p.Stock == 0, "startup has no free inventory");
                Check(!p.Buy(Option(c,"tree-start")), "startup charged once");
                p.Tick(4.9f); Near(p.Stock,0,"no partial batch"); p.Tick(.1f); Near(p.Stock,5,"first batch after five seconds");
                int wallet = p.Cheeses; Check(p.TotalCollected == 0 && !p.AutoEnabled, "stock is not income");
                Check(p.Collect() == 5 && p.Cheeses == wallet + 5 && p.TotalCollected == 5, "one stock equals one cheese");
                Check(p.Collect() == 0, "no duplicate collection");

                var timer = new TownProgress(c); timer.Grant(6000); Buy(timer,c,"tree-start");
                timer.Tick(2.5f); Buy(timer,c,"tree-interval"); Near(timer.NextBatchIn,2,"interval edit preserves half cycle");
                timer.Tick(1.99f); Near(timer.Stock,0,"not mature early"); timer.Tick(.01f); Near(timer.Stock,5,"shortened cycle completes");
                timer.Tick(10000); Near(timer.Stock,200,"capacity clamps stock"); timer.Collect();
                timer.Tick(3.9f); Near(timer.Stock,0,"full storage does not bank time"); timer.Tick(.1f); Near(timer.Stock,5,"production resumes after collection");
                timer.Tick(float.NaN); timer.Tick(float.PositiveInfinity); timer.Tick(-1); Near(timer.Stock,5,"invalid time ignored");

                // Match the six reviewed chart stages: cost, batch size, interval, and 30-second harvest.
                string[][] purchases = {
                    new[]{"tree-start"}, new[]{"tree-batch","tree-interval","tree-capacity"},
                    new[]{"tree-batch","tree-interval","tree-capacity"}, new[]{"tree-batch","tree-interval","tree-capacity"},
                    new[]{"tree-batch","tree-capacity"}, new[]{"tree-batch","tree-interval"}
                };
                int[] costs={30,130,380,1070,2170,4570}, batches={5,8,12,20,60,180}, caps={200,500,1200,3000,8000,8000}, harvests={30,56,120,300,900,5400};
                float[] intervals={5,4,3,2,2,1}, rates={1,2,4,10,30,180};
                var one = new TownProgress(c); var split = new TownProgress(c); one.Grant(6000); split.Grant(6000);
                int spend=0;
                for (int stage=0;stage<purchases.Length;stage++)
                {
                    // Isolate each stage from the previous harvest's wallet, goal, and timer changes.
                    one = new TownProgress(c); split = new TownProgress(c); one.Grant(6000); split.Grant(6000); spend=0;
                    for(int j=0;j<=stage;j++) foreach(var id in purchases[j])
                    { var o=Option(c,id); spend+=o.levels[one.Level(o)].price; Buy(one,c,id); Buy(split,c,id); }
                    Check(spend==costs[stage] && one.BatchSize==batches[stage] && one.Capacity==caps[stage],"stage amounts " + stage);
                    Near(one.ProductionInterval,intervals[stage],"stage interval"); Near(one.Production,rates[stage],"stage rate");
                    one.Tick(30); for(int j=0;j<300;j++) split.Tick(.1f);
                    Near(one.Stock,harvests[stage],"30-second yield " + stage); Near(split.Stock,one.Stock,"frame independent " + stage);
                    Check(one.TotalCollected==0 && one.Cheeses==6000-spend,"no automatic payout " + stage);
                }
                for(int i=2;i<=10;i++) Check(p.ScaleWildDropCount(i)==i,"halved wild drop quantity");
                Buy(p,c,"wild-tree-growth"); Near(p.WildTreeYieldMultiplier,1.25f,"wild growth"); Near(p.Production,1,"wild does not change town output");
                c.upgrades.Reverse(); p.Reconfigure(c); Check(p.MainTreeStarted,"stable purchase IDs survive reorder");

                // Spending delays unsent milestones even when lifetime collection is already higher.
                var story = new TownProgress(c); story.CollectWorld(99);
                Buy(story,c,"move-speed"); story.CollectWorld(1);
                Check(story.TotalCollected==100 && story.Cheeses==80 && !story.Letters.Any(l=>l.Id=="reserve"),"wallet, not lifetime collection, triggers mail");
                story.CollectWorld(20); Check(story.Letters.Any(l=>l.Id=="reserve"),"100 reserve acknowledges enough for winter");
                story.ReadLetter(story.MailboxEntryIndex); Check(!story.GameEnded,"enough ending leaves exploration available");
                Buy(story,c,"move-speed"); story.CollectWorld(50);
                Check(story.Letters.Count(l=>l.Id=="reserve")==1,"spending and recrossing do not repeat mail");
                foreach(var milestone in new[]{(350,"winter-feast"),(1000,"overflow"),(3000,"spoiling"),(6000,"leaving")})
                {
                    story.CollectWorld(milestone.Item1-story.Cheeses-1);
                    Check(!story.Letters.Any(l=>l.Id==milestone.Item2),"below narrative threshold " + milestone.Item1);
                    story.CollectWorld(1);
                    Check(story.Letters.Count(l=>l.Id==milestone.Item2)==1,"narrative boundary " + milestone.Item1);
                }
                var jump = new TownProgress(c); jump.CollectWorld(6000);
                Check(jump.Letters.Select(l=>l.Id).SequenceEqual(new[]{"welcome","shop","reserve","winter-feast","overflow","spoiling","leaving"}),"large harvest preserves ordered mail history");

                var goal = new TownProgress(c); goal.Grant(4840);
                Buy(goal,c,"tree-start"); foreach(var o in c.upgrades) while(goal.CanBuy(o)) goal.Buy(o);
                Check(goal.Cheeses==0 && !goal.GameEnded,"max upgrades do not end session");
                goal.Tick(100000); Check(goal.TotalCollected==0 && !goal.GameEnded,"full stock does not reach wallet goal"); goal.Collect();
                goal.CollectWorld(1999); Check(!goal.GameEnded,"9999 wallet boundary");
                goal.CollectWorld(1); Check(goal.GameEnded && !goal.Letters.Any(l=>l.Id==TownProgress.GoalLetterId),"10000 wallet ends without final dialogue");
                wallet=goal.Cheeses; float stock=goal.Stock; goal.Tick(99); goal.CollectWorld(10); goal.Collect(); goal.Grant(1);
                Check(goal.Cheeses==wallet && goal.Stock==stock,"silent ending freezes economy");
            }
            finally { Object.DestroyImmediate(c); }
            Directory.CreateDirectory("Logs");
            File.WriteAllText("Logs/upgrade-curve-data-checks.txt","PASS: serialized prices/effects; zero start; startup gate; discrete batches; proportional timer upgrades; storage cap/pause; no auto/value; six reviewed stages; frame independence; halved wild rolls; purchase/reorder; wallet milestones, spending, mail deduplication, and silent 10k ending.\n");
            Debug.Log("UPGRADE_CURVE_DATA_PASS");
        }

        [MenuItem("Cheese Town/Run New Upgrade Curve World Checks")]
        public static void RunBatch()
        {
            if(EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before checks.");
            if(!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            PhoneDemoChecks.DataChecks(); DataChecks();
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            SessionState.SetBool(Key+".Fast",EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled=false;
            SessionState.SetFloat(Key+".Time",(float)EditorApplication.timeSinceStartup);
            SessionState.SetBool(Key,true); EditorApplication.isPlaying=true;
        }
        static void ClickBuy(string id)
        {
            demo.ShowPage(1); demo.CompleteUITransitions(); demo.Refresh();
            var o=Option(demo.Settings,id); int price=o.levels[demo.Progress.Level(o)].price, before=demo.Progress.Cheeses;
            var row=demo.View.upgrades.Rows.Single(r=>r.Option.id==id);
            Check(row.buy.interactable,"real buy enabled " + id); row.buy.onClick.Invoke();
            Check(demo.Progress.Cheeses==before-price,"real buy debits " + id); CheckText();
        }
        static void CheckText()
        {
            Canvas.ForceUpdateCanvases();
            foreach(var row in demo.View.upgrades.Rows)
                foreach(var text in new[]{row.title,row.description,row.level,row.effectName,row.effectValue,row.price})
                    Check(UpgradeRowView.Fits(text.text,text),"pixel text fits " + row.Option.id + ": " + text.text);
        }
        static void Drops(string path,int minimum,int maximum)
        {
            var p=demo.Progress; var instance=Object.Instantiate(AssetDatabase.LoadAssetAtPath<Tree>(path),new Vector3(1000,1000,0),Quaternion.identity);
            var old=Object.FindObjectsByType<CheeseDropMotion>(FindObjectsSortMode.None).ToHashSet();
            instance.TakeDamage(100);
            var drops=Object.FindObjectsByType<CheeseDropMotion>(FindObjectsSortMode.None).Where(d=>!old.Contains(d)).ToArray();
            int min=p.ScaleWildDropCount(Mathf.CeilToInt(minimum*p.WildTreeYieldMultiplier)), max=p.ScaleWildDropCount(Mathf.CeilToInt(maximum*p.WildTreeYieldMultiplier));
            Check(drops.Length>=min && drops.Length<=max,"real halved drops " + path + ": " + drops.Length);
            instance.TakeDamage(100); Check(Object.FindObjectsByType<CheeseDropMotion>(FindObjectsSortMode.None).Length==old.Count+drops.Length,"no duplicate chopped-tree payout");
            foreach(var d in drops)Object.DestroyImmediate(d.gameObject); Object.DestroyImmediate(instance.gameObject);
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false))return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".Time",0)>150){Finish(new TimeoutException("World checks timed out"));return;}
            if(!EditorApplication.isPlaying || ++frames<12)return;
            try
            {
                Time.timeScale=0;
                demo=Object.FindAnyObjectByType<CheeseTownDemo>(); Check(demo!=null && demo.View!=null,"saved tablet instantiated");
                var p=demo.Progress;
                if(phase==0)
                {
                    // The economy regression starts after the independently tested opening.
                    var opening=TownSession.Instance.Prologue;
                    if(opening!=null&&opening.gameObject.activeSelf){opening.Skip();opening.Step(1);frames=0;return;}
                    if(TownSession.Instance.PrologueActive)return;
                    Check(p.Cheeses==0 && p.Stock==0 && !p.MainTreeStarted,"real scene starts at zero and locked");
                    p.CollectWorld(1); p.SkipInteractionGuide(); p.Grant(6000); demo.Refresh();
                    demo.TogglePhone(); demo.ShowPage(1); demo.CompleteUITransitions();
                    Check(demo.View.upgrades.Rows.Count==7,"seven rows");
                    Check(!demo.View.upgrades.Rows.Single(r=>r.Option.id=="tree-batch").buy.interactable,"locked buy disabled");
                    demo.View.upgrades.categories[1].onClick.Invoke(); Check(demo.View.upgrades.Rows.Count==2,"two PLAYER rows");
                    demo.View.upgrades.categories[2].onClick.Invoke(); Check(demo.View.upgrades.Rows.Count==5,"five TREE rows");
                    demo.View.upgrades.categories[0].onClick.Invoke(); demo.CompleteUITransitions();
                    Drops("Assets/Prefab/Tree.prefab",2,4); Drops("Assets/Prefab/Tree_02.prefab",8,10);
                    ClickBuy("tree-start"); ClickBuy("move-speed"); ClickBuy("collect-range");
                    var player=Object.FindAnyObjectByType<PlayerController>(); Near(player.speed.x,6,"world speed wiring"); Near(player.CollectRange,2,"world pickup range wiring");
                    var circle=player.GetComponentInChildren<CollectionRadius>().GetComponent<CircleCollider2D>();
                    Near(circle.radius*Mathf.Abs(circle.transform.lossyScale.x),2,"actual pickup collider radius");
                    p.Tick(5); Check(p.Stock==5,"real session stores first batch");
                    var tree=Object.FindAnyObjectByType<Tree>(); int before=p.Cheeses;
                    Check(tree.GetComponentInChildren<TreeHurtbox>().Collect()==0 && p.Cheeses==before && p.Stock==5,"wild E cannot drain town stock");
                    var cheese=Object.Instantiate(tree.cheesePrefab,circle.bounds.center,Quaternion.identity);
                    var oldSimulation=Physics2D.simulationMode;
                    try { Physics2D.simulationMode=SimulationMode2D.Script; Physics2D.SyncTransforms(); Physics2D.Simulate(.02f); }
                    finally { Physics2D.simulationMode=oldSimulation; }
                    Check(p.Cheeses==before+1 && p.Stock==5,"real ground pickup credits exactly one, leaves town stock");
                    if(cheese!=null)Object.DestroyImmediate(cheese);
                    demo.ShowPage(0);demo.CompleteUITransitions(); before=p.Cheeses;demo.View.collect.onClick.Invoke();
                    Check(p.Cheeses==before+5 && p.Stock==0,"real collect button credits stock once");
                    demo.ShowPage(1);demo.CompleteUITransitions();
                    foreach(var o in demo.Settings.upgrades)while(p.CanBuy(o))ClickBuy(o.id);
                    Check(!p.GameEnded && !p.Letters.Any(l=>l.Id==TownProgress.FinalLetterId),"all upgrades no longer end game");
                    Near(p.Production,180,"max actual output");Check(p.Capacity==8000,"max actual capacity");
                    Drops("Assets/Prefab/Tree.prefab",2,4);Drops("Assets/Prefab/Tree_02.prefab",8,10);
                    demo.View.upgrades.scroll.verticalNormalizedPosition=0;Canvas.ForceUpdateCanvases();
                    Check(demo.View.upgrades.content.rect.height>demo.View.upgrades.scroll.viewport.rect.height,"expanded level rows scroll");
                    demo.ShowPage(0);demo.CompleteUITransitions();demo.Refresh();
                    foreach(var text in new[]{demo.View.stock,demo.View.stats,demo.View.production})Check(UpgradeRowView.Fits(text.text,text),"main info fits: "+text.text);
                    p.Tick(.4f); savedWallet=p.Cheeses;savedCollected=p.TotalCollected;savedProgress=p;
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Wilderness.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                    phase=1;frames=0;
                }
                else
                {
                    Check(ReferenceEquals(p,savedProgress) && p.Cheeses==savedWallet && p.TotalCollected==savedCollected,"scene reload retains economy");
                    Near(p.NextBatchIn,.6f,"scene reload retains cycle progress");Near(p.Production,180,"reload retains upgrades");
                    p.Tick(50);Check(p.Stock==8000 && p.TotalCollected==savedCollected,"large batch stays in storage");
                    p.SkipInteractionGuide();demo.TogglePhone();demo.ShowPage(0);demo.CompleteUITransitions();demo.View.collect.onClick.Invoke();
                    Check(p.Cheeses==savedWallet+8000,"large manual harvest");
                    foreach(var message in p.Letters)
                        Check(UpgradeRowView.Fits(message.Body,demo.View.letter),"mail body fits: "+message.Id);
                    p.CollectWorld((int)(p.CollectionGoal-p.Cheeses));demo.Refresh();
                    Check(demo.EndingOpen,"10k wallet opens silent ending");Finish(null);
                }
            }
            catch(Exception e){Finish(e);}
        }
        static void Finish(Exception error)
        {
            SessionState.SetBool(Key,false);Time.timeScale=1;EditorSettings.enterPlayModeOptionsEnabled=SessionState.GetBool(Key+".Fast",false);
            Directory.CreateDirectory("Logs");string result=error==null?"PASS: real Wilderness zero start; startup and locked buttons; seven rows and filters; every tier via prefab Buy; pixel text; actual halved tree drops; actual pickup physics; separate town stock; manual harvest; scrolling; reload/timer retention; 10k ending.":"FAIL: "+error;
            File.WriteAllText("Logs/upgrade-curve-world-checks.txt",result);Debug.Log(result);
            if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);else EditorApplication.isPlaying=false;
        }
    }
    // END ADDED
}
