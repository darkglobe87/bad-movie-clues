using UnityEngine;
using UnityEditor;

namespace BadMovieClues.EditorTools
{
    public static class ThemeSetupUtility
    {
        private const string ThemePath = "Assets/_Project/Content/UITheme.asset";

        [MenuItem("Bad Movie Clues/Setup UI Theme Palette")]
        public static void SetupPalette()
        {
            var theme = AssetDatabase.LoadAssetAtPath<BadMovieClues.UI.UITheme>(ThemePath);
            if (theme == null)
            {
                Debug.LogError($"[ThemeSetupUtility] Could not find UITheme asset at {ThemePath}");
                return;
            }

            Undo.RecordObject(theme, "Setup UI Theme Palette");

            // Base colors (keeping if already set, otherwise setting default)
            if (theme.BackgroundTop == Color.clear) theme.BackgroundTop = new Color32(0x2A, 0x1A, 0x3E, 0xFF);
            if (theme.BackgroundBottom == Color.clear) theme.BackgroundBottom = new Color32(0x3D, 0x24, 0x59, 0xFF);
            if (theme.AccentGold == Color.clear) theme.AccentGold = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
            if (theme.AccentMagenta == Color.clear) theme.AccentMagenta = new Color32(0xFF, 0x4E, 0x8B, 0xFF);
            if (theme.AccentLime == Color.clear) theme.AccentLime = new Color32(0xB6, 0xFF, 0x3C, 0xFF);
            if (theme.NeutralLight == Color.clear) theme.NeutralLight = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
            if (theme.DangerRed == Color.clear) theme.DangerRed = new Color32(0xC6, 0x41, 0x3B, 0xFF);

            // Extended colors
            theme.CardBackground = new Color32(0x35, 0x20, 0x4E, 0xFF);
            theme.ShadowColor = new Color32(0x15, 0x0D, 0x22, 0xCC);
            theme.GlowGold = new Color32(0xFF, 0xD7, 0x70, 0x80);
            theme.SeparatorColor = new Color32(0x4A, 0x30, 0x6A, 0xFF);
            theme.StarFilled = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
            theme.StarEmpty = new Color32(0x4A, 0x30, 0x6A, 0xFF);
            theme.CoinTextColor = new Color32(0xFF, 0xD7, 0x70, 0xFF);
            theme.CorrectGreen = new Color32(0x5C, 0xD6, 0x5C, 0xFF);
            theme.LockedOverlay = new Color32(0x1A, 0x10, 0x2A, 0xCC);
            theme.ButtonGradientTop = new Color32(0xFF, 0xFF, 0xFF, 0x1A);

            EditorUtility.SetDirty(theme);
            AssetDatabase.SaveAssets();
            Debug.Log($"[ThemeSetupUtility] Successfully populated extended color palette on UITheme at {ThemePath}.");
        }
    }
}
