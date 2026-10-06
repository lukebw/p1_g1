using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace CheeseTownPhone.Editor
{
    public static class TreeHudButtonBuilder
    {
        const string Art = "Assets/Art/UI/UI_main tree.png";

        [MenuItem("Cheese Town/UI/Install Tree HUD Button")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play before installing the tree button.");
            var importer = (TextureImporter)AssetImporter.GetAtPath(Art);
            importer.textureType = TextureImporterType.Sprite; importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point; importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.alphaIsTransparency = true; importer.npotScale = TextureImporterNPOTScale.None;
            importer.SaveAndReimport();
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art);
            var settings = Resources.Load<TabletSettings>("TabletSettings");
            string path = AssetDatabase.GetAssetPath(settings.tabletPrefab);
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var view = root.GetComponent<TabletView>();
                // Install once; later runs preserve authored layout and button settings.
                if (view.treeLauncher != null) return;
                var button = UnityEngine.Object.Instantiate(view.launcher, view.worldHud);
                button.name = "Open Great Tree";
                foreach (Transform child in button.transform.Cast<Transform>().ToArray()) UnityEngine.Object.DestroyImmediate(child.gameObject);
                foreach (var outline in button.GetComponents<Outline>()) UnityEngine.Object.DestroyImmediate(outline);
                button.onClick = new Button.ButtonClickedEvent();
                var artwork = button.GetComponent<Image>(); artwork.sprite = sprite; artwork.overrideSprite = null;
                artwork.preserveAspect = true; artwork.raycastTarget = true; button.targetGraphic = artwork;
                button.colors = view.launcher.colors; button.interactable = true;
                if (button.GetComponent<UIButtonAudio>() == null) button.gameObject.AddComponent<UIButtonAudio>();
                button.GetComponent<UIButtonAudio>().shopButton = false;
                var rect = (RectTransform)button.transform; var mail = (RectTransform)view.launcher.transform;
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
                rect.sizeDelta = sprite.rect.size * .5f;
                mail.anchoredPosition = Vector2.zero;
                rect.anchoredPosition = new Vector2(Mathf.Round(mail.rect.width + 6), 0);
                view.treeLauncher = button;
                // Keep both launchers on one row, with the introductory hint underneath.
                var hint = view.closedUnread.rectTransform;
                hint.anchoredPosition = new Vector2(0, -Mathf.Max(mail.rect.height, rect.rect.height) - 6);
                view.worldHud.sizeDelta = new Vector2(view.worldHud.sizeDelta.x, -hint.anchoredPosition.y + hint.rect.height);
                // Right-side anchors keep the pickup target and wallet together as the viewport changes.
                var wallet = view.hudWallet.rectTransform;
                wallet.anchorMin = wallet.anchorMax = new Vector2(1, 1);
                wallet.anchoredPosition = new Vector2(-104, -2);
                wallet.sizeDelta = new Vector2(104, 28);
                var coin = (RectTransform)view.worldHud.Find("HUD cheese icon");
                coin.anchorMin = coin.anchorMax = new Vector2(1, 1);
                coin.anchoredPosition = new Vector2(-144, -2);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
        }
    }
}
