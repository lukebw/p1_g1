// BEGIN ADDED: Migrate only mail and HUD once; preserve the artist's other prefab edits.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class MailPrefabUpgrade
    {
        public static void Ensure(TabletSettings settings)
        {
            if (settings.tabletPrefab == null || settings.tabletPrefab.worldHud != null) return;
            string path = AssetDatabase.GetAssetPath(settings.tabletPrefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Apply(root.GetComponent<TabletView>(), settings);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        public static void Apply(TabletView view, TabletSettings settings)
        {
            if (view.worldHud != null) return;
            if (settings.mailPaper == null || settings.mailRead == null || settings.mailNext == null || settings.mailPrevious == null)
                throw new InvalidOperationException("Import the supplied mail artwork before migrating the UI.");
            var footerClip = Rect(view.footerMotion, "Footer controls clip", 30, 303, 580, 37);
            footerClip.gameObject.AddComponent<RectMask2D>();
            var controls = Rect(footerClip, "Footer controls placement", -30, -303, 640, 360);
            view.townFooter = Rect(controls, "Town footer motion", 0, 0, 640, 360).gameObject.AddComponent<CanvasGroup>();
            view.mailFooter = Rect(controls, "Mail footer motion", 0, 0, 640, 360).gameObject.AddComponent<CanvasGroup>();
            foreach (var child in new Transform[] { view.stats.transform, view.production.transform, view.collect.transform })
                child.SetParent(view.townFooter.transform, false);
            Skin(view.previous, view.mailFooter.transform, settings.mailPrevious, 39, 307, view.back.colors);
            Skin(view.read, view.mailFooter.transform, settings.mailRead, 223, 307, view.back.colors);
            Skin(view.next, view.mailFooter.transform, settings.mailNext, 490, 307, view.back.colors);

            var page = (RectTransform)view.mailGroup.transform;
            Place(page, 0, 0, 640, 360);
            var oldBackground = page.GetComponent<Image>();
            if (oldBackground != null) UnityEngine.Object.DestroyImmediate(oldBackground);
            foreach (Transform child in page.Cast<Transform>().ToArray())
                if (child != view.letter.transform && child != view.reply.transform && child != view.mailBack.transform)
                    UnityEngine.Object.DestroyImmediate(child.gameObject);
            view.mailBack.gameObject.SetActive(false); // The shared header Back remains the only return control.
            var clip = Rect(page, "Letter clip", 30, 68, 580, 222);
            clip.gameObject.AddComponent<RectMask2D>();
            var paper = Rect(clip, "Letter placement", -30, -68, 640, 360);
            Art(paper, "Letter paper", settings.mailPaper, 0, 0);
            Label(paper, "Letter heading", "FROM MAYOR ELLIS", 75, 85, 490, 20, settings);
            view.reply.transform.SetParent(paper, false); Place(view.reply.rectTransform, 75, 109, 490, 14);
            Style(view.reply, settings); view.reply.alignment = TextAnchor.UpperLeft;
            view.letter.transform.SetParent(paper, false); Place(view.letter.rectTransform, 75, 142, 490, 136);
            Style(view.letter, settings); view.letter.lineSpacing = 1.2f;
            // Keep the mail-ended status concise enough for the paper's header band.
            view.reply.horizontalOverflow = HorizontalWrapMode.Wrap;

            view.worldHud = Rect(view.transform, "Gameplay HUD", 20, 20, 260, 106);
            Art(view.worldHud, "HUD cheese icon", settings.walletIcon, 0, 0, 32, 28);
            view.hudWallet = Label(view.worldHud, "HUD cheese count", "500", 40, 0, 180, 28, settings);
            view.hudWallet.alignment = TextAnchor.MiddleLeft; view.hudWallet.color = new Color(1, .94f, .73f, 1);
            Shadow(view.hudWallet);
            Skin(view.launcher, view.worldHud, settings.envelopeIcon, 0, 38, view.back.colors);
            view.hudUnreadDot = Art(view.launcher.transform, "HUD unread dot", settings.unreadDot, 39, -2, 12, 12).gameObject;
            view.closedUnread.transform.SetParent(view.worldHud, false);
            Place(view.closedUnread.rectTransform, 0, 78, 244, 26);
            Style(view.closedUnread, settings); view.closedUnread.color = new Color(1, .94f, .73f, 1);
            view.closedUnread.text = "Press TAB to open messages.";
            view.worldHud.pivot = new Vector2(0, 1);
            view.SetPage(0, false); view.SetOpen(true, false);
        }

        static void Skin(Button button, Transform parent, Sprite sprite, float x, float y, ColorBlock colors)
        {
            button.transform.SetParent(parent, false);
            foreach (Transform child in button.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
            Place((RectTransform)button.transform, x, y, sprite.rect.width / 2, sprite.rect.height / 2);
            var image = button.GetComponent<Image>(); image.sprite = sprite; image.color = Color.white;
            image.type = Image.Type.Simple; image.preserveAspect = true; image.raycastTarget = true;
            button.targetGraphic = image; button.transition = Selectable.Transition.ColorTint;
            colors.disabledColor = new Color(.55f, .55f, .55f, 1); button.colors = colors;
        }
        static Image Art(Transform parent, string name, Sprite sprite, float x, float y, float w = 0, float h = 0)
        {
            var image = Rect(parent, name, x, y, w > 0 ? w : sprite.rect.width / 2, h > 0 ? h : sprite.rect.height / 2).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.preserveAspect = true; image.raycastTarget = false; return image;
        }
        static Text Label(Transform parent, string name, string value, float x, float y, float w, float h, TabletSettings settings)
        {
            var text = Rect(parent, name, x, y, w, h).gameObject.AddComponent<Text>();
            Style(text, settings); text.text = value; return text;
        }
        static void Style(Text text, TabletSettings settings)
        {
            text.font = settings.upgradePixelFont; text.fontSize = 8; text.fontStyle = FontStyle.Normal;
            text.resizeTextForBestFit = false; text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            text.color = new Color(.40f, .22f, .17f, 1); text.raycastTarget = false;
        }
        static void Shadow(Text text)
        {
            var shadow = text.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.2f, .12f, .07f, 1);
            shadow.effectDistance = new Vector2(1, -1);
        }
        static RectTransform Rect(Transform parent, string name, float x, float y, float w, float h)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            rect.SetParent(parent, false); Place(rect, x, y, w, h); return rect;
        }
        static void Place(RectTransform rect, float x, float y, float w, float h)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(w, h);
            rect.localScale = Vector3.one;
        }
    }
}
// END ADDED
