using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;

namespace BadMovieClues.EditorTools
{
    public static class FontImporter
    {
        private const string FontsFolder = "Assets/_Project/Content/Fonts";
        private const string ThemePath = "Assets/_Project/Content/UITheme.asset";

        [MenuItem("Bad Movie Clues/Import Display Font")]
        public static void ImportFont()
        {
            if (!Directory.Exists(FontsFolder))
            {
                Directory.CreateDirectory(FontsFolder);
                AssetDatabase.Refresh();
            }

            // Find Fredoka font or any font in the folder as fallback
            var ttfGuids = AssetDatabase.FindAssets("Fredoka t:Font", new[] { FontsFolder });
            if (ttfGuids.Length == 0)
            {
                ttfGuids = AssetDatabase.FindAssets("t:Font", new[] { FontsFolder });
            }

            if (ttfGuids.Length == 0)
            {
                Debug.LogWarning(
                    $"[FontImporter] No TTF font found in {FontsFolder}.\n" +
                    "Please download Fredoka from https://fonts.google.com/specimen/Fredoka\n" +
                    $"Place the .ttf file in {FontsFolder} and run this tool again.");
                return;
            }

            var ttfPath = AssetDatabase.GUIDToAssetPath(ttfGuids[0]);
            var importer = AssetImporter.GetAtPath(ttfPath) as TrueTypeFontImporter;
            if (importer != null && !importer.includeFontData)
            {
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }

            var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[FontImporter] Could not load font at {ttfPath}");
                return;
            }

            // Generate TMP SDF asset
            var tmpFont = TMP_FontAsset.CreateFontAsset(sourceFont);
            if (tmpFont == null)
            {
                Debug.LogError("[FontImporter] TMP_FontAsset.CreateFontAsset() returned null.");
                return;
            }

            var sdfPath = $"{FontsFolder}/Fredoka SDF.asset";
            AssetDatabase.CreateAsset(tmpFont, sdfPath);

            // Add material and atlas textures as sub-assets so they are saved persistently to disk
            if (tmpFont.material != null)
            {
                AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFont);
            }
            if (tmpFont.atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(tmpFont.atlasTexture, tmpFont);
            }
            if (tmpFont.atlasTextures != null)
            {
                foreach (var tex in tmpFont.atlasTextures)
                {
                    if (tex != null && tex != tmpFont.atlasTexture)
                    {
                        AssetDatabase.AddObjectToAsset(tex, tmpFont);
                    }
                }
            }

            EditorUtility.SetDirty(tmpFont);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[FontImporter] Created TMP font asset at {sdfPath}");

            // Assign to UITheme
            var theme = AssetDatabase.LoadAssetAtPath<BadMovieClues.UI.UITheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogWarning($"[FontImporter] UITheme not found at {ThemePath} - assign the font manually.");
                return;
            }

            Undo.RecordObject(theme, "Assign Fredoka font to UITheme");
            theme.HeadingFont = tmpFont;
            theme.BodyFont = tmpFont;
            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log("[FontImporter] Assigned Fredoka to UITheme.HeadingFont and UITheme.BodyFont.");
        }
    }
}
