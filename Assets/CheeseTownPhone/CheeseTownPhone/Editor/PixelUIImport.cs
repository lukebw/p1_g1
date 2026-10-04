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
        // BEGIN CHANGED: Import the shop's separate states with the same sharp settings.
        static readonly string[] Images = {
            "UI_main_frame.png", "UI_Back.png", "UI_colloect.png", "UI_massage.png", "UI_upgrade.png", "UI_reddot.png",
            "UI_Upgrade_frame.png", "UI_Upgrade_back.png", "UI_Upgrade_bar.png", "UI_UPGRADE_ICON_BOX.png",
            "UI_LEVEL.png", "UI_LEVEL_count.png", "UI_LEVEL_count_actived.png", "UI_UPGRADE_BUY.png", "UI_UPGRADE_BUY_disable.png",
            "UI_All_selected.png", "UI_All_unselect.png", "UI_Player_selected.png", "UI_Player_unselect.png",
            "UI_Tree_selected.png", "UI_Treel_unselect.png"
        };
        // END CHANGED

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
                // BEGIN ADDED: Resize blank centers while retaining pixel borders, icons and BUY lettering.
                if (name == "UI_Upgrade_bar.png") importSettings.spriteBorder = new Vector4(8, 8, 8, 8);
                if (name == "UI_UPGRADE_BUY.png" || name == "UI_UPGRADE_BUY_disable.png")
                    importSettings.spriteBorder = new Vector4(82, 8, 144, 8);
                // END ADDED
                importer.SetTextureSettings(importSettings);
                importer.SaveAndReimport();
            }
            // BEGIN CHANGED: Use the newly selected font and its native eight-pixel grid.
            var fontImporter = AssetImporter.GetAtPath(ArtPath + "Fonts/PressStart2P.ttf") as TrueTypeFontImporter;
            if (fontImporter == null) throw new InvalidOperationException("Missing selected Press Start 2P font.");
            fontImporter.fontSize = 8;
            // END CHANGED
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
            // BEGIN CHANGED: Bind the new font and each supplied shop state explicitly.
            settings.interfaceFont = AssetDatabase.LoadAssetAtPath<Font>(ArtPath + "Fonts/PressStart2P.ttf");
            settings.upgradeFrame = Sprite("UI_Upgrade_frame.png");
            settings.upgradeBackground = Sprite("UI_Upgrade_back.png");
            settings.upgradeRowArtwork = Sprite("UI_Upgrade_bar.png");
            settings.upgradeIconBox = Sprite("UI_UPGRADE_ICON_BOX.png");
            settings.upgradeLevelArtwork = Sprite("UI_LEVEL.png");
            settings.upgradeLevelInactive = Sprite("UI_LEVEL_count.png");
            settings.upgradeLevelActive = Sprite("UI_LEVEL_count_actived.png");
            settings.upgradeButtonArtwork = Sprite("UI_UPGRADE_BUY.png");
            settings.upgradeBuyDisabled = Sprite("UI_UPGRADE_BUY_disable.png");
            settings.allSelected = Sprite("UI_All_selected.png"); settings.allUnselected = Sprite("UI_All_unselect.png");
            settings.playerSelected = Sprite("UI_Player_selected.png"); settings.playerUnselected = Sprite("UI_Player_unselect.png");
            settings.treeSelected = Sprite("UI_Tree_selected.png"); settings.treeUnselected = Sprite("UI_Treel_unselect.png");
            // END CHANGED
            // BEGIN ADDED: Bake row glyphs at native resolution rather than re-rasterizing per screen size.
            var pixelFontPath = ArtPath + "Fonts/PressStart2P-Pixel.ttf";
            var pixelFontImporter = AssetImporter.GetAtPath(pixelFontPath) as TrueTypeFontImporter;
            if (pixelFontImporter == null) throw new InvalidOperationException("Missing pixel font source copy.");
            pixelFontImporter.fontSize = 8;
            // BEGIN CHANGED: Include the font's native triangle so the details cue stays on the pixel grid.
            pixelFontImporter.fontTextureCase = FontTextureCase.CustomSet;
            pixelFontImporter.customCharacters = new string(Enumerable.Range(32, 95).Select(code => (char)code).ToArray()) + "\u25B6";
            // END CHANGED
            pixelFontImporter.fontRenderingMode = FontRenderingMode.HintedRaster;
            pixelFontImporter.SaveAndReimport();
            settings.upgradePixelFont = AssetDatabase.LoadAssetAtPath<Font>(pixelFontPath);
            if (settings.upgradeRowPrefab == null) settings.upgradeRowPrefab = UpgradeRowPrefabBuilder.Ensure(settings);
            foreach (var option in settings.upgrades)
                if (option != null && option.id == "collect-range" && option.description == "No range at level 0. Unlock and expand collection radius.")
                    option.description = "No range at level 1. Unlock and expand collection radius.";
            // END ADDED
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
