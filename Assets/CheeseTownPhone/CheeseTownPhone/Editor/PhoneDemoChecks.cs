using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class PhoneDemoChecks
    {
        static void Check(bool condition,string message) { if(!condition) throw new Exception("Tablet check failed: "+message); }
        static void Near(float actual,float expected,string message) { Check(Math.Abs(actual-expected)<.002f,message+" ("+actual+" != "+expected+")"); }
        static TabletSettings Config()
        {
            var c = ScriptableObject.CreateInstance<TabletSettings>();
            c.upgrades = TabletSettings.Defaults(); c.startingCheeses=10000; c.initialTreeStock=0; c.ValidateSettings(); return c;
        }
        [MenuItem("Cheese Town/Run Data Checks")]
        public static void DataChecks()
        {
            var c = Config();
            try
            {
                var p = new TownProgress(c);
                Check(c.upgrades.SelectMany(o=>o.levels).Select(l=>l.price).SequenceEqual(new[]{20,50,30,60,60,120,240,150,260}),"reference prices");
                Check(p.Level(c.upgrades[0])==0 && p.CollectRange==0,"zero-level defaults");
                Check(p.Buy(c.upgrades[0]),"speed purchase"); Near(p.MoveSpeed,6,"player speed");
                Check(p.Buy(c.upgrades[1]),"range purchase"); Near(p.CollectRange,2,"player range");
                Check(p.Buy(c.upgrades[2]),"tree purchase"); Near(p.Production,2,"tree production"); Near(p.TreeScale,1.2f,"tree size");
                Check(p.Buy(c.upgrades[3]),"auto purchase"); Near(p.AutoRate,p.Production,"auto follows production");
                int before=p.Cheeses; p.Tick(1); Check(p.Cheeses==before+2,"auto transfer after one second"); Near(p.Stock,0,"auto removes stock");
                Check(p.Buy(c.upgrades[2]),"tree second level"); Near(p.AutoRate,4,"auto updates with tree level");
                before=p.Cheeses; p.Tick(1); Check(p.Cheeses==before+4,"new auto rate");
                float production=p.Production;
                Check(p.Buy(c.upgrades[4]),"double value purchase");
                Near(p.Production,production,"double value does not change quantity"); Check(p.UnitPrice==2,"unit price doubles");
                before=p.Cheeses; p.Tick(1); Check(p.Cheeses==before+8,"auto earns doubled price");
                Check(!p.Buy(c.upgrades[4]),"max-level protection");
                c.upgrades[3].available=false; c.ValidateSettings(); p.Reconfigure(c);
                p.Tick(1); before=p.Cheeses; Check(p.Collect()==4 && p.Cheeses==before+8,"manual pickup uses doubled price");
                var speed=c.upgrades[0]; c.upgrades.Reverse(); c.ValidateSettings(); p.Reconfigure(c); Check(p.Level(speed)==1,"reorder preserves stable ID");
                speed.levels[0].value=7; c.ValidateSettings(); p.Reconfigure(c); Near(p.MoveSpeed,7,"live numeric edits");
                c.upgrades.Remove(speed); c.ValidateSettings(); p.Reconfigure(c); Near(p.MoveSpeed,c.baseMoveSpeed,"remove option safely");
                var extra=new UpgradeOption { title="Extra speed",effect=UpgradeEffect.MoveSpeed,levels=new System.Collections.Generic.List<UpgradeLevel>{new UpgradeLevel(1,9)} };
                c.upgrades.Add(extra); c.ValidateSettings(); p.Reconfigure(c); Check(p.Buy(extra),"added option"); Near(p.MoveSpeed,9,"new option effect");
                extra.levels.Clear(); c.ValidateSettings(); p.Reconfigure(c); Check(!p.CanBuy(extra),"empty levels safe");
                c.upgrades.Clear(); c.ValidateSettings(); p.Reconfigure(c); Check(!p.AutoEnabled && p.UnitPrice==1,"empty shop resets removed effects");
                var poor=Config(); poor.startingCheeses=0; var q=new TownProgress(poor);
                Check(!q.Buy(poor.upgrades[0]) && q.Cheeses==0,"insufficient funds");
                poor.upgrades.Add(poor.upgrades[0] == null ? null : new UpgradeOption{id=poor.upgrades[0].id});
                poor.upgrades[0].levels[0].price=-3; poor.upgrades[0].levels[0].value=float.NaN;
                poor.ValidateSettings(); Check(poor.upgrades[0].id!=poor.upgrades[5].id,"duplicate ID repair"); Check(poor.upgrades[0].levels[0].price==0 && poor.upgrades[0].levels[0].value==0,"invalid values sanitized");
                UnityEngine.Object.DestroyImmediate(poor);
                var fractional=Config(); fractional.baseTreeProduction=.5f; var f=new TownProgress(fractional); f.Buy(fractional.upgrades[3]);
                before=f.Cheeses; f.Tick(1); Check(f.Cheeses==before,"fractional cheese retained"); f.Tick(1); Check(f.Cheeses==before+1,"fractional rate accumulates");
                UnityEngine.Object.DestroyImmediate(fractional);
                File.WriteAllText(Output("Tablet-data-checks.txt"),"PASS: reference prices; player/tree effects; production-following automatic collection; double unit price with unchanged quantity; manual and automatic payout; zero levels; max levels; insufficient funds; stable IDs and reorder; add/remove/empty shop; live value edits; fractional production; invalid numeric input.\n");
                Debug.Log("TABLET_DATA_CHECKS_PASS");
            }
            finally { UnityEngine.Object.DestroyImmediate(c); }
        }
        [MenuItem("Cheese Town/Run Demo Checks (Play Mode)")]
        public static void Run()
        {
            DataChecks();
            var demo=UnityEngine.Object.FindAnyObjectByType<CheeseTownDemo>();
            if(!EditorApplication.isPlaying || demo==null) { Debug.LogWarning("Press Play in the tablet scene for UI checks."); return; }
            if(demo.PhoneOpen) demo.TogglePhone();
            var kb=InputSystem.AddDevice<Keyboard>("TabletTestKeyboard");
            try
            {
                Press(kb,Key.Tab); Check(demo.PhoneOpen,"Tab opens");
                Press(kb,Key.Tab); Check(!demo.PhoneOpen,"Tab closes");
                Click(demo,"Open tablet"); Check(demo.PhoneOpen,"launcher");
                Click(demo,"Shop button");
                Check(demo.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Buy "))==5,"five default shop rows");
                Click(demo,"Player filter");
                Check(demo.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Buy "))==3,"player category");
                Click(demo,"Tree filter");
                Check(demo.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Buy "))==2,"tree category");
                Click(demo,"All filter"); Click(demo,"Buy move-speed"); Check(demo.Progress.MoveSpeed==6,"buy button");
                var original = demo.Settings;
                var editable = UnityEngine.Object.Instantiate(original);
                var image = new Texture2D(4,4);
                var sprite = Sprite.Create(image,new Rect(0,0,4,4),Vector2.one*.5f);
                try
                {
                    demo.UseSettings(editable);
                    editable.upgrades.Add(new UpgradeOption { title="Extra option", levels=new System.Collections.Generic.List<UpgradeLevel>{new UpgradeLevel(1,9)} });
                    demo.ApplyConfiguration();
                    Check(demo.GetComponentsInChildren<Button>().Count(b=>b.name.StartsWith("Buy "))==6,"added row renders and scrolls");
                    editable.townBackground=sprite; editable.cheeseTree=sprite; editable.envelopeIcon=sprite; editable.upgrades[0].icon=sprite;
                    demo.ApplyConfiguration();
                    Check(demo.GetComponentsInChildren<Image>(true).Any(i=>i.gameObject.activeInHierarchy && i.name=="Town Background Sprite Slot" && i.sprite==sprite),"background art replacement");
                    Check(demo.GetComponentsInChildren<Image>(true).Any(i=>i.gameObject.activeInHierarchy && i.name=="Cheese Tree Sprite Slot" && i.sprite==sprite),"tree art replacement");
                    editable.upgrades.Clear(); demo.ApplyConfiguration();
                    Check(demo.GetComponentsInChildren<Button>().All(b=>!b.name.StartsWith("Buy ")),"empty shop renders safely");
                }
                finally { demo.UseSettings(original); UnityEngine.Object.DestroyImmediate(editable); UnityEngine.Object.DestroyImmediate(sprite); UnityEngine.Object.DestroyImmediate(image); }
                Press(kb,Key.Escape); Check(demo.PhoneOpen,"Escape returns from shop");
                Click(demo,"Letters button"); Click(demo,"Reply to mayor"); Check(demo.Progress.WelcomeClaimed,"mayor reply");
                Press(kb,Key.Escape); Press(kb,Key.Escape); Check(!demo.PhoneOpen,"Escape closes tablet from town");
                Press(kb,Key.Tab); Click(demo,"Shop button");
                File.WriteAllText(Output("Tablet-ui-checks.txt"),"PASS: scene load; Tab toggle; launcher; five shop rows; player/tree filters; buy button; mayor reply; Escape back/close. Unity "+Application.unityVersion+".\n");
                Debug.Log("TABLET_UI_CHECKS_PASS");
            }
            finally { InputSystem.RemoveDevice(kb); }
        }
        static void Press(Keyboard keyboard,Key key)
        { InputSystem.QueueStateEvent(keyboard,new KeyboardState(key)); InputSystem.Update(); InputSystem.QueueStateEvent(keyboard,new KeyboardState()); InputSystem.Update(); }
        static void Click(CheeseTownDemo demo,string name)
        {
            var button=demo.GetComponentsInChildren<Button>(true).Single(b=>b.name==name && b.gameObject.activeInHierarchy);
            Check(button.interactable,name+" enabled"); button.onClick.Invoke();
        }
        static string Output(string filename) => Path.GetFullPath(Path.Combine(Application.dataPath,"../../"+filename));
        [MenuItem("Cheese Town/Capture Demo Screenshot (Play Mode)")]
        public static void Capture()
        { if(EditorApplication.isPlaying) ScreenCapture.CaptureScreenshot(Output("Tablet-Preview.png"),2); }
    }
}
