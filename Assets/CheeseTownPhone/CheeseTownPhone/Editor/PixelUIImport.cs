// BEGIN ADDED: Reproducible imports preserve the user's two-times pixel exports.
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace CheeseTownPhone.Editor
{
    public static class PixelUIImport
    {
        public const string ArtPath = "Assets/Art/UI/";
        public const string SettingsPath = "Assets/CheeseTownPhone/CheeseTownPhone/Resources/TabletSettings.asset";
        static readonly string[] Images = { "UI_main_frame.png", "UI_Back.png", "UI_colloect.png", "UI_massage.png", "UI_upgrade.png", "UI_reddot.png" };

        [MenuItem("Cheese Town/Apply Pixel UI Artwork")]
        public static void Apply()
        {
            AssetDatabase.Refresh();
            foreach (string name in Images)
            {
                var importer = AssetImporter.GetAtPath(ArtPath + name) as TextureImporter;
                if (importer == null) throw new InvalidOperationException("Missing UI artwork: " + name);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 100;
                importer.filterMode = FilterMode.Point;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = 2048;
                var importSettings = new TextureImporterSettings();
                importer.ReadTextureSettings(importSettings);
                importSettings.spriteMeshType = SpriteMeshType.FullRect;
                importer.SetTextureSettings(importSettings);
                importer.SaveAndReimport();
            }
            var fontImporter = AssetImporter.GetAtPath(ArtPath + "Fonts/eas-vhs.ttf") as TrueTypeFontImporter;
            if (fontImporter == null) throw new InvalidOperationException("Missing selected EAS VHS font.");
            fontImporter.fontRenderingMode = FontRenderingMode.HintedRaster;
            fontImporter.includeFontData = true;
            fontImporter.SaveAndReimport();
            var settings = AssetDatabase.LoadAssetAtPath<TabletSettings>(SettingsPath);
            if (settings == null) throw new InvalidOperationException("Missing tablet configuration.");
            Undo.RecordObject(settings, "Apply supplied pixel UI");
            settings.mainFrame = Sprite("UI_main_frame.png");
            settings.backButtonArtwork = Sprite("UI_Back.png");
            settings.collectButtonArtwork = Sprite("UI_colloect.png");
            settings.envelopeIcon = Sprite("UI_massage.png");
            settings.shopIcon = Sprite("UI_upgrade.png");
            settings.unreadDot = Sprite("UI_reddot.png");
            settings.interfaceFont = AssetDatabase.LoadAssetAtPath<Font>(ArtPath + "Fonts/eas-vhs.ttf");
            if (settings.walletIcon == null) settings.walletIcon = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cheese_01.png").OfType<Sprite>().FirstOrDefault();
            if (settings.cheeseTree == null) settings.cheeseTree = AssetDatabase.LoadAllAssetsAtPath("Assets/Art/Cheese_tree_01.png").OfType<Sprite>().FirstOrDefault();
            settings.ValidateSettings();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("PIXEL_UI_ARTWORK_APPLIED: full-rect Point sprites; selected raster font; 2x exports bound to 640x360 layout.");
        }
        static Sprite Sprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtPath + name);
    }
}
// END ADDED
