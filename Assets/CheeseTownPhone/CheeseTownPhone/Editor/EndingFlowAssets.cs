using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class EndingFlowAssets
    {
        [MenuItem("Cheese Town/UI/Install Ending Choice and Tree Navigation")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before editing UI assets.");
            const string art = "Assets/Art/UI/UI_cancel.png";
            var importer = (TextureImporter)AssetImporter.GetAtPath(art);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            const string frameArt = "Assets/Art/UI/UI_empty_frame-export.png";
            var frameImporter = (TextureImporter)AssetImporter.GetAtPath(frameArt);
            frameImporter.textureType = TextureImporterType.Sprite; frameImporter.spriteImportMode = SpriteImportMode.Single;
            frameImporter.filterMode = FilterMode.Point; frameImporter.mipmapEnabled = false;
            frameImporter.textureCompression = TextureImporterCompression.Uncompressed;
            frameImporter.alphaIsTransparency = true; frameImporter.npotScale = TextureImporterNPOTScale.None;
            frameImporter.spriteBorder = Vector4.zero; frameImporter.SaveAndReimport();
            var frameSprite = AssetDatabase.LoadAssetAtPath<Sprite>(frameArt);
            var settings = Resources.Load<TabletSettings>("TabletSettings");
            settings.backButtonArtwork = AssetDatabase.LoadAssetAtPath<Sprite>(art);
            var path = AssetDatabase.GetAssetPath(settings.tabletPrefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = root.GetComponent<TabletView>();
                view.back.GetComponent<Image>().sprite = settings.backButtonArtwork;
                view.back.gameObject.name = "Close tablet";
                if (view.treePageButton == null)
                {
                    var button = UnityEngine.Object.Instantiate(view.treeLauncher, view.mailButton.transform.parent);
                    button.name = "Visit Great Tree"; button.onClick = new Button.ButtonClickedEvent();
                    var rect = (RectTransform)button.transform;
                    var mail = (RectTransform)view.mailButton.transform;
                    rect.anchorMin = mail.anchorMin; rect.anchorMax = mail.anchorMax; rect.pivot = mail.pivot;
                    rect.anchoredPosition = mail.anchoredPosition + new Vector2(mail.rect.width + 8, 0);
                    view.treePageButton = button;
                }
                if (view.hudTreeFullDot == null) view.hudTreeFullDot = Dot(view.hudUnreadDot, view.treeLauncher.transform);
                if (view.pageTreeFullDot == null) view.pageTreeFullDot = Dot(view.hudUnreadDot, view.treePageButton.transform);
                if (view.shopLauncher == null)
                {
                    view.shopLauncher = UnityEngine.Object.Instantiate(view.shopButton, view.worldHud);
                    view.shopLauncher.name = "Open Supplies";
                    view.shopLauncher.onClick = new Button.ButtonClickedEvent();
                }
                // Keep the exit button untouched; all destinations share the same left-to-right order.
                var exit = (RectTransform)view.back.transform;
                Row(exit.anchoredPosition.x + exit.rect.width + 8, exit.anchoredPosition.y, 8,
                    view.treePageButton, view.mailButton, view.shopButton);
                Row(0, 0, 6, view.treeLauncher, view.launcher, view.shopLauncher);
                view.readAttention = view.read.GetComponent<CanvasGroup>();
                if (view.readAttention == null) view.readAttention = view.read.gameObject.AddComponent<CanvasGroup>();
                view.readAttention.alpha = 1;
                // Rebuild only the ending panel; the surrounding authored UI stays intact.
                foreach (Transform child in view.ending.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                var overlay = (RectTransform)view.ending.transform;
                overlay.SetParent(view.transform, false); overlay.SetAsLastSibling();
                overlay.anchorMin = overlay.anchorMax = overlay.pivot = new Vector2(.5f,.5f);
                overlay.anchoredPosition = Vector2.zero; overlay.sizeDelta = new Vector2(640,360);
                var shade = view.ending.GetComponent<Image>(); shade.sprite = null; shade.type = Image.Type.Simple;
                shade.color = new Color(0,0,0,.62f); shade.raycastTarget = true;
                var paper = Rect(view.ending.transform, "Ending choice", 0, 0, 510, 274);
                paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(.5f,.5f); paper.anchoredPosition = Vector2.zero;
                var image = paper.gameObject.AddComponent<Image>(); image.sprite = frameSprite;
                image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = false;
                var question = Text(paper, "Continue question", "You can keep gathering cheese.\nThough no mouse needs it anymore.\nKeep going?", 40, 65, 430, 82, view.hudWallet.font, 12);
                question.color = new Color(1,.94f,.78f);
                var shadow = question.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(0,0,0,.8f);
                shadow.effectDistance = new Vector2(1,-1);
                view.continueEnding = Choice(paper, "Continue gathering", "YES", 128, view, settings);
                view.quitEnding = Choice(paper, "Leave Mousetown", "NO", 286, view, settings);
                view.ending.SetActive(false);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            string endingPath = AssetDatabase.GetAssetPath(settings.epiloguePrefab);
            var story = PrefabUtility.LoadPrefabContents(endingPath);
            try { story.GetComponent<PrologueView>().finalHint = "CLICK / SPACE TO CONTINUE"; PrefabUtility.SaveAsPrefabAsset(story, endingPath); }
            finally { PrefabUtility.UnloadPrefabContents(story); }
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
        }
        static void Row(float x, float y, float gap, params Button[] buttons)
        {
            foreach (var button in buttons)
            {
                var rect = (RectTransform)button.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
                rect.anchoredPosition = new Vector2(Mathf.Round(x), Mathf.Round(y));
                x += rect.rect.width + gap;
            }
        }
        static GameObject Dot(GameObject source, Transform parent)
        {
            var dot = UnityEngine.Object.Instantiate(source, parent); dot.name = "Tree stock full";
            dot.GetComponent<Image>().raycastTarget = false; dot.SetActive(false); return dot;
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
            rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h); return rect;
        }
        static Text Text(Transform parent, string name, string value, float x, float y, float w, float h, Font font, int size)
        {
            var text = Rect(parent,name,x,y,w,h).gameObject.AddComponent<Text>(); text.font = font; text.fontSize = size;
            text.text = value; text.color = new Color(.36f,.2f,.15f); text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false; return text;
        }
        static Button Choice(Transform parent, string name, string title, float x, TabletView view, TabletSettings settings)
        {
            var rect = Rect(parent,name,x,184,96,30);
            var image = rect.gameObject.AddComponent<Image>(); image.sprite = settings.upgradeRowArtwork;
            image.type = Image.Type.Sliced; image.pixelsPerUnitMultiplier = 2;
            image.color = title == "YES" ? new Color(1,.8f,.32f) : Color.white;
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.colors = view.back.colors;
            rect.gameObject.AddComponent<UIButtonAudio>();
            Text(rect,"Label",title,0,0,96,30,view.hudWallet.font,12);
            return button;
        }
    }
}
