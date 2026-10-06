using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    // Explicit creation command: rebuilding never overwrites an artist-edited opening prefab.
    public static class OpeningAssets
    {
        public const string Art = "Assets/Art/OpeningV1/";
        public const string Prefab = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/PrologueView.prefab";
        static RectTransform Rect(Transform parent, string name, Vector2 min, Vector2 max)
        {
            var r = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            r.SetParent(parent, false); r.anchorMin = min; r.anchorMax = max; r.offsetMin = r.offsetMax = Vector2.zero;
            return r;
        }
        static Image Image(Transform parent, string name, Color color)
        {
            var image = Rect(parent,name,Vector2.zero,Vector2.one).gameObject.AddComponent<Image>();
            image.color=color; image.raycastTarget=false; return image;
        }
        static Text Text(Transform parent,string name,string value,Font font,int size,Vector2 min,Vector2 max)
        {
            var text=Rect(parent,name,min,max).gameObject.AddComponent<Text>();
            text.font=font;text.text=value;text.fontSize=size;text.alignment=TextAnchor.MiddleCenter;
            text.color=new Color(1,.94f,.78f);text.raycastTarget=false;text.lineSpacing=1.5f;
            text.horizontalOverflow=HorizontalWrapMode.Wrap;text.verticalOverflow=VerticalWrapMode.Truncate;
            return text;
        }
        static Sprite Import(string name,bool icon)
        {
            string path=Art+name+".png";
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new Exception("Missing generated artwork: "+path);
            importer.textureType=TextureImporterType.Sprite;importer.spriteImportMode=SpriteImportMode.Single;
            importer.spritePixelsPerUnit=100;importer.alphaIsTransparency=icon;importer.mipmapEnabled=false;
            importer.filterMode=FilterMode.Point;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.maxTextureSize=icon?128:2048;importer.npotScale=TextureImporterNPOTScale.None;
            importer.SaveAndReimport();return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        [MenuItem("Cheese Town/Create Opening Assets")]
        public static void Build()
        {
            if(EditorApplication.isPlaying)throw new InvalidOperationException("Stop Play before creating assets.");
            var settings=Resources.Load<TabletSettings>("TabletSettings");
            string[] ids={"move-speed","collect-range","tree-start","tree-batch","tree-interval","tree-capacity","wild-tree-growth"};
            string[] icons={"speed-boots","cheese-magnet","spring-tonic","growth-potion","haste-potion","cozy-cellar","forest-charm"};
            for(int i=0;i<ids.Length;i++)settings.upgrades.Single(o=>o.id==ids[i]).icon=Import(icons[i],true);
            var backgrounds=new[]{Import("autumn",false),Import("winter",false),Import("harvest",false)};
            var saved=AssetDatabase.LoadAssetAtPath<PrologueView>(Prefab);
            if(saved==null)
            {
                var root=new GameObject("Opening narrative",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster),typeof(PrologueView));
                try
                {
                    var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=100;
                    var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution=new Vector2(640,360);scaler.matchWidthOrHeight=.5f;
                    var view=root.GetComponent<PrologueView>();
                    var blocker=Image(root.transform,"Black backdrop",Color.black);blocker.raycastTarget=true;
                    view.advance=blocker.gameObject.AddComponent<Button>();view.advance.transition=Selectable.Transition.None;view.advance.targetGraphic=blocker;
                    view.page=Rect(blocker.transform,"Chapter",Vector2.zero,Vector2.one).gameObject.AddComponent<CanvasGroup>();
                    view.background=Image(view.page.transform,"Atmosphere",Color.white);
                    var fit=view.background.gameObject.AddComponent<AspectRatioFitter>();fit.aspectRatio=16f/9;fit.aspectMode=AspectRatioFitter.AspectMode.EnvelopeParent;
                    Image(view.page.transform,"Readability shade",new Color(0,0,0,.28f));
                    var font=settings.upgradePixelFont;
                    view.title=Text(view.page.transform,"Chapter title","",font,8,new Vector2(.12f,.63f),new Vector2(.88f,.72f));
                    view.body=Text(view.page.transform,"Centered story","",font,10,new Vector2(.12f,.34f),new Vector2(.88f,.66f));
                    view.counter=Text(view.page.transform,"Chapter number","",font,6,new Vector2(.4f,.2f),new Vector2(.6f,.25f));
                    view.hint=Text(blocker.transform,"Continue hint","",font,6,new Vector2(.15f,.06f),new Vector2(.85f,.12f));
                    var skipRect=Rect(root.transform,"Skip opening",new Vector2(.76f,.89f),new Vector2(.98f,.98f));
                    var skipImage=skipRect.gameObject.AddComponent<Image>();skipImage.color=new Color(.10f,.12f,.13f,.75f);
                    view.skip=skipRect.gameObject.AddComponent<Button>();view.skip.targetGraphic=skipImage;
                    Text(skipRect,"Skip label","SKIP [ESC]",font,6,Vector2.zero,Vector2.one);
                    view.chapters=StoryChapters(backgrounds);
                    view.Begin(null);view.Step(1); // Save a readable first chapter for prefab editing.
                    PrefabUtility.SaveAsPrefabAsset(root,Prefab);
                }
                finally{UnityEngine.Object.DestroyImmediate(root);}
                saved=AssetDatabase.LoadAssetAtPath<PrologueView>(Prefab);
            }
            settings.prologuePrefab=saved;EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
            Debug.Log("OPENING_ASSETS_READY");
        }
        public static PrologueView.Chapter[] StoryChapters(Sprite[] backgrounds) => new[]{
            new PrologueView.Chapter{title="",body="For generations, the Great Cheese Tree\nhas sheltered the mice of Mousetown.\nNow its branches have fallen silent.",background=backgrounds[0]},
            new PrologueView.Chapter{title="",body="Winter is drawing near.\nThe storerooms are empty,\nand our old guardian is fading.",background=backgrounds[1]},
            new PrologueView.Chapter{title="",body="Leo, gather cheese beyond the town.\nHelp us weather the cold...\nand bring our Great Tree back to life.",background=backgrounds[2]}};

        // Explicit migration only: normal play never rewrites artist-edited prefab layouts.
        [MenuItem("Cheese Town/Apply Story Polish To Prefabs")]
        public static void ApplyStoryPolish()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing prefabs.");
            var settings = Resources.Load<TabletSettings>("TabletSettings");
            var opening = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var view = opening.GetComponent<PrologueView>();
                view.chapters = StoryChapters(view.chapters.Select(c => c.background).ToArray());
                view.body.rectTransform.anchorMin = new Vector2(.12f, .34f);
                view.body.rectTransform.anchorMax = new Vector2(.88f, .66f);
                view.Begin(null); view.Step(1);
                PrefabUtility.SaveAsPrefabAsset(opening, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(opening); }

            string rowPath = AssetDatabase.GetAssetPath(settings.upgradeRowPrefab);
            var rowRoot = PrefabUtility.LoadPrefabContents(rowPath);
            try
            {
                var row = rowRoot.GetComponent<UpgradeRowView>();
                var box = rowRoot.transform.Find("Upgrade icon box");
                var icon = row.icon.rectTransform;
                icon.SetParent(box, false);
                icon.anchorMin = icon.anchorMax = icon.pivot = new Vector2(.5f, .5f);
                icon.anchoredPosition = Vector2.zero; icon.sizeDelta = new Vector2(32, 32);
                row.icon.preserveAspect = true;
                PrefabUtility.SaveAsPrefabAsset(rowRoot, rowPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(rowRoot); }

            string tabletPath = AssetDatabase.GetAssetPath(settings.tabletPrefab);
            var tabletRoot = PrefabUtility.LoadPrefabContents(tabletPath);
            try
            {
                var view = tabletRoot.GetComponent<TabletView>();
                // The supplied button Sprite already contains its SUPPLIES lettering.
                view.stock.text = "REVIVE OUR GREAT TREE";
                view.production.text = "THE GREAT TREE SLUMBERS\nFIND SPRING TONIC IN SUPPLIES";
                foreach (var label in tabletRoot.GetComponentsInChildren<Text>(true))
                    if (label.text == "CHEESE TREE") label.text = "GREAT CHEESE TREE";
                view.letter.text = new TownProgress(settings).Letters[0].Body;
                PrefabUtility.SaveAsPrefabAsset(tabletRoot, tabletPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(tabletRoot); }
            AssetDatabase.SaveAssets();
        }
        public static void ApplyStoryPolishAndCheck()
        {
            ApplyStoryPolish(); OpeningChecks.RunBatch();
        }
    }
}
