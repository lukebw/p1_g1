// BEGIN ADDED: Create the initial editable prefab once; preserve later artist edits.
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class UpgradeRowPrefabBuilder
    {
        public const string PrefabPath = "Assets/CheeseTownPhone/CheeseTownPhone/Prefabs/UpgradeRow.prefab";
        [MenuItem("Cheese Town/Select Upgrade Row Prefab")]
        public static void Select()
        {
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            EditorGUIUtility.PingObject(Selection.activeObject);
        }
        public static UpgradeRowView Ensure(TabletSettings settings)
        {
            var existing = AssetDatabase.LoadAssetAtPath<UpgradeRowView>(PrefabPath);
            if (existing != null) return existing;
            Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)); AssetDatabase.Refresh();
            var root = MakeRect(null, "UpgradeRow", 0, 0, 564, 55);
            try
            {
                var view = root.gameObject.AddComponent<UpgradeRowView>();
                var background = root.gameObject.AddComponent<Image>();
                background.sprite = settings.upgradeRowArtwork; background.type = Image.Type.Sliced;
                background.pixelsPerUnitMultiplier = 2; background.raycastTarget = true;
                MakeImage(root, "Upgrade icon box", settings.upgradeIconBox, 8, 9, 56, 37);
                view.icon = MakeImage(root, "Upgrade Icon Sprite Slot", null, 12, 11, 48, 33);
                view.icon.preserveAspect = true;
                view.iconPlaceholder = MakeText(root, "Placeholder label", "ICON", settings.upgradePixelFont, 8, 9, 56, 37, 8, TextAnchor.MiddleCenter);
                view.title = MakeText(root, "Title", "UPGRADE NAME", settings.upgradePixelFont, 71, 7, 137, 18);
                view.title.lineSpacing = 1.125f;
                view.description = MakeText(root, "Description", "Effect preview...", settings.upgradePixelFont, 71, 28, 137, 20);
                view.description.lineSpacing = 1.25f;
                MakeImage(root, "Level background", settings.upgradeLevelArtwork, 210, 9, 67, 14);
                view.level = MakeText(root, "Level", "LV 1/3", settings.upgradePixelFont, 210, 9, 67, 14, 8, TextAnchor.MiddleCenter);
                view.effectName = MakeText(root, "Current value", "SPEED", settings.upgradePixelFont, 284, 9, 87, 12);
                view.effectValue = MakeText(root, "Next value", "4 > 6", settings.upgradePixelFont, 284, 29, 87, 18);
                var buy = MakeImage(root, "Buy", settings.upgradeButtonArtwork, 379, 9, 176, 37);
                buy.type = Image.Type.Sliced; buy.pixelsPerUnitMultiplier = 2; buy.raycastTarget = true;
                view.buy = buy.gameObject.AddComponent<Button>(); view.buy.targetGraphic = buy;
                // BEGIN CHANGED: Match header hover/press tint; RefreshDisplay already supplies disabled artwork.
                view.buy.transition = Selectable.Transition.ColorTint;
                var colors = view.buy.colors;
                colors.normalColor = Color.white; colors.highlightedColor = new Color(1, .95f, .85f);
                colors.pressedColor = new Color(.8f, .8f, .8f); colors.disabledColor = Color.white;
                colors.fadeDuration = 0; view.buy.colors = colors;
                // END CHANGED
                view.price = MakeText(buy.transform, "Price", "9999", settings.upgradePixelFont, 39, 0, 64, 37, 16, TextAnchor.MiddleCenter);
                view.markerArea = MakeRect(root, "Level markers", 210, 29, 67, 15);
                view.markerTemplate = MakeImage(view.markerArea, "Marker template", settings.upgradeLevelInactive, 0, 0, 15, 15);
                view.markerTemplate.gameObject.SetActive(false);
                view.activeMarker = settings.upgradeLevelActive; view.inactiveMarker = settings.upgradeLevelInactive;
                view.buyArtwork = settings.upgradeButtonArtwork; view.disabledArtwork = settings.upgradeBuyDisabled;
                PrefabUtility.SaveAsPrefabAsset(root.gameObject, PrefabPath);
            }
            finally { Object.DestroyImmediate(root.gameObject); }
            return AssetDatabase.LoadAssetAtPath<UpgradeRowView>(PrefabPath);
        }
        static RectTransform MakeRect(Transform parent, string name, float x, float y, float width, float height)
        {
            var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            if (parent != null) rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y); rect.sizeDelta = new Vector2(width, height);
            return rect;
        }
        static Image MakeImage(Transform parent, string name, Sprite sprite, float x, float y, float width, float height)
        {
            var image = MakeRect(parent, name, x, y, width, height).gameObject.AddComponent<Image>();
            image.sprite = sprite; image.raycastTarget = false; return image;
        }
        static Text MakeText(Transform parent, string name, string value, Font font, float x, float y, float width, float height,
            int size = 8, TextAnchor align = TextAnchor.UpperLeft)
        {
            var text = MakeRect(parent, name, x, y, width, height).gameObject.AddComponent<Text>();
            text.font = font; text.fontSize = size; text.fontStyle = FontStyle.Normal;
            text.color = new Color32(102, 51, 34, 255); text.text = value;
            text.alignment = align; text.raycastTarget = false; text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Wrap; text.verticalOverflow = VerticalWrapMode.Truncate;
            return text;
        }
    }
}
// END ADDED
