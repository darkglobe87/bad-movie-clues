using UnityEngine;
using UnityEditor;
using System.IO;

namespace BadMovieClues.EditorTools
{
    public static class KenneyAssetImporter
    {
        private const string KenneyFolder = "Assets/_Project/Content/UI/Kenney";
        private const string ThemePath = "Assets/_Project/Content/UITheme.asset";

        [MenuItem("Bad Movie Clues/Import Kenney UI Sprites")]
        public static void ImportSprites()
        {
            if (!Directory.Exists(KenneyFolder))
            {
                Directory.CreateDirectory(KenneyFolder);
                AssetDatabase.Refresh();
                Debug.LogWarning(
                    $"[KenneyAssetImporter] Created {KenneyFolder} — please extract additional Kenney UI Pack sprites there:\n" +
                    "  - buttonSquare_grey.png (toggle background)\n" +
                    "  - checkmark.png (toggle checkmark)\n" +
                    "  - barHorizontal_white.png (separator)\n" +
                    "Then run this tool again.");
                return;
            }

            var pngGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { KenneyFolder });
            if (pngGuids.Length == 0)
            {
                Debug.LogWarning($"[KenneyAssetImporter] No textures found in {KenneyFolder}.");
                return;
            }

            var imported = 0;
            Sprite toggleBg = null, checkmark = null, separator = null;

            foreach (var guid in pngGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var filename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null) continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;

                // Set 9-slice borders based on filename pattern
                if (filename.StartsWith("buttonsquare"))
                {
                    importer.spriteBorder = new Vector4(12, 12, 12, 12);
                }
                else if (filename.StartsWith("checkmark"))
                {
                    importer.spriteBorder = Vector4.zero;
                }
                else if (filename.StartsWith("barhorizontal"))
                {
                    importer.spriteBorder = new Vector4(8, 0, 8, 0);
                }

                importer.SaveAndReimport();
                imported++;
            }

            // Need to reload sprites after reimport
            AssetDatabase.Refresh();
            foreach (var guid in pngGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var filename = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
                if (filename.StartsWith("buttonsquare") && toggleBg == null)
                    toggleBg = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                else if (filename.StartsWith("checkmark") && checkmark == null)
                    checkmark = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                else if (filename.StartsWith("barhorizontal") && separator == null)
                    separator = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            }

            // Assign to UITheme
            var theme = AssetDatabase.LoadAssetAtPath<BadMovieClues.UI.UITheme>(ThemePath);
            if (theme != null)
            {
                Undo.RecordObject(theme, "Assign Kenney sprites to UITheme");
                if (toggleBg != null) theme.ToggleBackgroundSprite = toggleBg;
                if (checkmark != null) theme.ToggleCheckmarkSprite = checkmark;
                if (separator != null) theme.SeparatorSprite = separator;
                EditorUtility.SetDirty(theme);
                AssetDatabase.SaveAssets();
            }

            Debug.Log($"[KenneyAssetImporter] Imported {imported} sprites. " +
                      $"ToggleBg={toggleBg != null}, Checkmark={checkmark != null}, Separator={separator != null}");
        }
    }
}
