using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Central theme asset for the "campy B-movie theatre" visual pass:
    /// button/panel/tile sprites from the chosen UI kit, fonts, and the
    /// project's palette. Sprite/font fields may be empty until M7 imports
    /// the kit - everything falls back to the current plain look.
    /// </summary>
    [CreateAssetMenu(fileName = "UITheme", menuName = "Bad Movie Clues/UI Theme")]
    public class UITheme : ScriptableObject
    {
        [Header("Buttons (9-slice)")]
        public Sprite ButtonNormalSprite;
        public Sprite ButtonPressedSprite;
        public Sprite ButtonDisabledSprite;

        [Header("Panels")]
        public Sprite PanelSprite;

        [Header("Keyboard / Blanks Row")]
        public Sprite KeySprite;
        public Sprite TileSprite;

        [Header("Fonts")]
        public TMP_FontAsset HeadingFont;
        public TMP_FontAsset BodyFont;

        [Header("Palette - \"campy B-movie theatre\"")]
        public Color BackgroundTop = new Color32(0x2A, 0x1A, 0x3E, 0xFF);
        public Color BackgroundBottom = new Color32(0x3D, 0x24, 0x59, 0xFF);
        public Color AccentGold = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
        public Color AccentMagenta = new Color32(0xFF, 0x4E, 0x8B, 0xFF);
        public Color AccentLime = new Color32(0xB6, 0xFF, 0x3C, 0xFF);
        public Color NeutralLight = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
        public Color DangerRed = new Color32(0xC6, 0x41, 0x3B, 0xFF);

        /// <summary>Applies the 9-slice button sprite/colors to a Button + Image, if the theme has them assigned.</summary>
        public void ApplyButton(Button button, Image image)
        {
            if (ButtonNormalSprite == null) return;

            image.sprite = ButtonNormalSprite;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = NeutralLight;
            colors.disabledColor = new Color(1f, 1f, 1f, 0.5f);
            button.colors = colors;
        }

        /// <summary>Applies the tile sprite/color to a keyboard key or blanks-row tile background, if assigned.</summary>
        public void ApplyTile(Image image, bool isKeyboardKey)
        {
            var sprite = isKeyboardKey ? KeySprite : TileSprite;
            if (sprite == null) return;

            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = NeutralLight;
        }
    }
}
