using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CheeseTownPhone.Editor
{
    [InitializeOnLoad]
    public static class OpeningChecks
    {
        const string Key="OpeningChecks.Active";
        static int frames,phase;
        static Vector3 position;
        static void Check(bool ok,string reason){if(!ok)throw new Exception(reason);}
        static OpeningChecks(){EditorApplication.update+=Tick;}
        public static void RunVisualBatch()
        {
            SessionState.SetBool(Key + ".Visuals", true); RunBatch();
        }
        public static void RunBatch()
        {
            OpeningAssets.Build();
            var settings=Resources.Load<TabletSettings>("TabletSettings");
            Check(settings.upgrades.All(o=>o.icon!=null),"All seven icons bound");
            var icon = settings.upgradeRowPrefab.icon.rectTransform;
            Check(icon.parent.name == "Upgrade icon box" && icon.pivot == new Vector2(.5f,.5f)
                && icon.anchoredPosition == Vector2.zero, "Icon uses a centered pivot inside its own box");
            Check(settings.tabletPrefab.shopButton.GetComponent<Image>().sprite == settings.shopIcon, "Supplied shop artwork is bound");
            var view=UnityEngine.Object.Instantiate(settings.prologuePrefab);
            try
            {
                int completed=0;view.Begin(()=>completed++);view.Advance();Check(view.ChapterIndex==0,"No advance during fade");
                for(int i=0;i<3;i++)
                {
                    view.Step(1);view.Step(1);Canvas.ForceUpdateCanvases();
                    Check(view.ChapterIndex==i && view.background.sprite!=null,"Chapter background/order");
                    Check(!view.title.gameObject.activeSelf && !view.counter.gameObject.activeSelf, "No numbered headings or slide counters");
                    Check(!view.body.text.Contains("hundred") && !view.body.text.Contains("100"), "Opening does not promise 100 cheese is sufficient");
                    Check(UpgradeRowView.Fits(view.body.text,view.body)&&UpgradeRowView.Fits(view.title.text,view.title),"Centered text fits");
                    view.advance.onClick.Invoke();view.Step(1);
                }
                Check(completed==1&&!view.gameObject.activeSelf,"Final click completes exactly once");
                view.gameObject.SetActive(true);view.Begin(()=>completed++);view.skip.onClick.Invoke();view.Step(1);
                Check(completed==2&&!view.gameObject.activeSelf,"Skip during fade completes");
            }
            finally{UnityEngine.Object.DestroyImmediate(view.gameObject);}
            EditorSceneManager.OpenScene("Assets/Scenes/Wilderness.unity");
            SessionState.SetBool(Key+".Fast",EditorSettings.enterPlayModeOptionsEnabled);
            EditorSettings.enterPlayModeOptionsEnabled=false;
            SessionState.SetBool(Key,true);SessionState.SetFloat(Key+".Start",(float)EditorApplication.timeSinceStartup);
            EditorApplication.isPlaying=true;
        }
        static void Tick()
        {
            if(!SessionState.GetBool(Key,false))return;
            if(EditorApplication.timeSinceStartup-SessionState.GetFloat(Key+".Start",0)>120){Finish(new Exception("Opening checks timed out"));return;}
            if(!EditorApplication.isPlaying||++frames<15)return;
            try
            {
                var session=TownSession.Instance;var demo=UnityEngine.Object.FindAnyObjectByType<CheeseTownDemo>();
                var player=UnityEngine.Object.FindAnyObjectByType<PlayerController>();
                if(phase==0)
                {
                    Check(session.PrologueActive&&demo.BlocksWorldInput,"Fresh game blocks world controls during opening");
                    Check(!demo.ScreenCanvas.gameObject.activeInHierarchy, "Gameplay canvas and all HUD icons hidden during opening");
                    Check(!demo.PhoneOpen&&session.Progress.TotalCollected==0,"Tutorial/economy untouched");
                    if (SessionState.GetBool(Key + ".Visuals", false))
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            session.Prologue.Step(1); session.Prologue.Step(1);
                            Capture(session.Prologue.GetComponent<Canvas>(), "story-" + (i + 1));
                            if (i < 2) { session.Prologue.Advance(); session.Prologue.Step(1); }
                        }
                    }
                    position=player.transform.position;demo.TogglePhone();Check(!demo.PhoneOpen,"Tab cannot open behind opening");
                    session.Prologue.skip.onClick.Invoke();session.Prologue.Step(1);phase=1;frames=0;
                }
                else if(phase==1)
                {
                    Check(!session.PrologueActive&&!demo.BlocksWorldInput&&demo.HarvestTutorialActive,"Skip returns to harvesting tutorial");
                    Check(demo.ScreenCanvas.gameObject.activeInHierarchy, "HUD canvas returns after opening");
                    Check(player.transform.position==position&&session.Progress.Cheeses==0,"Opening does not move player or grant cheese");
                    EditorSceneManager.LoadSceneInPlayMode("Assets/Scenes/Wilderness.unity",new UnityEngine.SceneManagement.LoadSceneParameters(UnityEngine.SceneManagement.LoadSceneMode.Single));
                    phase=2;frames=0;
                }
                else
                {
                    Check(!session.PrologueActive,"Reload does not replay opening");
                    if (SessionState.GetBool(Key + ".Visuals", false))
                    {
                        session.Progress.CollectWorld(1); session.Progress.SkipInteractionGuide();
                        demo.Refresh(); demo.TogglePhone(); demo.ShowPage(1); demo.CompleteUITransitions();
                        demo.SendMessage("LateUpdate");
                        foreach (var row in demo.View.upgrades.Rows)
                        {
                            foreach (var text in new[]{row.title,row.level,row.effectName,row.effectValue,row.price})
                                Check(UpgradeRowView.Fits(text.text,text), "Equipment text fits: " + text.text);
                            if (row.Option.effect == UpgradeEffect.TownTreeStart)
                                Check(row.effectValue.text == "DORMANT\n> ALIVE", "Revival states do not split a word across lines");
                        }
                        Capture(demo.ScreenCanvas, "equipment");
                        demo.ShowPage(0); demo.CompleteUITransitions(); demo.Refresh();
                        foreach (var text in new[]{demo.View.stock, demo.View.stats, demo.View.production})
                            Check(UpgradeRowView.Fits(text.text,text), "Dormant tree copy fits: " + text.text);
                        Capture(demo.ScreenCanvas, "dormant-tree");
                    }
                    Finish(null);
                }
            }
            catch(Exception e){Finish(e);}
        }
        static void Finish(Exception error)
        {
            SessionState.SetBool(Key + ".Visuals", false);
            SessionState.SetBool(Key,false);EditorSettings.enterPlayModeOptionsEnabled=SessionState.GetBool(Key+".Fast",false);Directory.CreateDirectory("Logs");
            string result=error==null?"PASS: seven icon bindings and centered pivot; supplied SUPPLIES artwork; three backgrounds; unnumbered story and centered text fit; advance/fade/skip callbacks; hidden gameplay HUD and restoration; new-session input blocking; tutorial handoff; unchanged wallet; no replay on scene reload.":"FAIL: "+error;
            File.WriteAllText("Logs/opening-checks.txt",result);Debug.Log(result);
            if(Application.isBatchMode)EditorApplication.Exit(error==null?0:1);else EditorApplication.isPlaying=false;
        }
        static void Capture(Canvas canvas, string name)
        {
            var camera = Camera.main;
            var mode = canvas.renderMode; var worldCamera = canvas.worldCamera; float distance = canvas.planeDistance, scale = canvas.scaleFactor;
            int layer = canvas.sortingLayerID, order = canvas.sortingOrder;
            var previousTarget = camera.targetTexture; var previousActive = RenderTexture.active;
            var target = new RenderTexture(1280,720,24);
            var image = new Texture2D(1280,720,TextureFormat.RGB24,false);
            try
            {
                target.Create(); camera.targetTexture = target;
                canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1;
                canvas.scaleFactor = 2; // The capture is 1280x720, exactly twice the logical art size.
                canvas.sortingLayerName = "Foreground"; canvas.sortingOrder = 32760;
                Canvas.ForceUpdateCanvases();
                RenderPipeline.SubmitRenderRequest(camera, new UniversalRenderPipeline.SingleCameraRequest {destination=target});
                RenderTexture.active = target; image.ReadPixels(new Rect(0,0,1280,720),0,0); image.Apply();
                Directory.CreateDirectory("Logs/StoryPolish"); File.WriteAllBytes("Logs/StoryPolish/"+name+".png",image.EncodeToPNG());
            }
            finally
            {
                camera.targetTexture = previousTarget; RenderTexture.active = previousActive;
                canvas.renderMode = mode; canvas.worldCamera = worldCamera; canvas.planeDistance = distance;
                canvas.scaleFactor = scale;
                canvas.sortingLayerID = layer; canvas.sortingOrder = order;
                UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
            }
        }
    }
}
