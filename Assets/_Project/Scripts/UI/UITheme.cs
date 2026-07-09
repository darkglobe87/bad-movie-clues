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

        [Header("Toggles")]
        public Sprite ToggleBackgroundSprite;
        public Sprite ToggleCheckmarkSprite;

        [Header("Icons")]
        public Sprite CoinIconSprite;
        public Sprite LockIconSprite;
        public Sprite StarFilledSprite;
        public Sprite StarEmptySprite;

        [Header("Separators")]
        public Sprite SeparatorSprite;

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

        [Header("Button Customization")]
        public Color ButtonColor = new Color32(0xFF, 0x4E, 0x8B, 0xFF);
        public Color ButtonTextColor = new Color32(0xF5, 0xEC, 0xD9, 0xFF);
        public Color KeyboardKeyColor = new Color32(0x56, 0x3A, 0x7A, 0xFF);
        public Color KeyboardKeyTextColor = new Color32(0xF5, 0xEC, 0xD9, 0xFF);

        [Header("Extended Palette")]
        public Color CardBackground = new Color32(0x35, 0x20, 0x4E, 0xFF);
        public Color ShadowColor = new Color32(0x15, 0x0D, 0x22, 0xCC);
        public Color GlowGold = new Color32(0xFF, 0xD7, 0x70, 0x80);
        public Color SeparatorColor = new Color32(0x4A, 0x30, 0x6A, 0xFF);
        public Color StarFilled = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
        public Color StarEmpty = new Color32(0x4A, 0x30, 0x6A, 0xFF);
        public Color CoinTextColor = new Color32(0xFF, 0xD7, 0x70, 0xFF);
        public Color CorrectGreen = new Color32(0x5C, 0xD6, 0x5C, 0xFF);
        public Color LockedOverlay = new Color32(0x1A, 0x10, 0x2A, 0xCC);
        public Color ButtonGradientTop = new Color32(0xFF, 0xFF, 0xFF, 0x1A);

        /// <summary>
        /// Called by Unity when the asset is loaded. Fixes any extended palette
        /// colors that are still at the unserialised default of (0,0,0,0).
        /// </summary>
        private void OnEnable()
        {
            FixColor(ref ButtonColor, new Color32(0xFF, 0x4E, 0x8B, 0xFF));
            FixColor(ref ButtonTextColor, new Color32(0xF5, 0xEC, 0xD9, 0xFF));
            FixColor(ref KeyboardKeyColor, new Color32(0x56, 0x3A, 0x7A, 0xFF));
            FixColor(ref KeyboardKeyTextColor, new Color32(0xF5, 0xEC, 0xD9, 0xFF));

            FixColor(ref CardBackground, new Color32(0x35, 0x20, 0x4E, 0xFF));
            FixColor(ref ShadowColor, new Color32(0x15, 0x0D, 0x22, 0xCC));
            FixColor(ref GlowGold, new Color32(0xFF, 0xD7, 0x70, 0x80));
            FixColor(ref SeparatorColor, new Color32(0x4A, 0x30, 0x6A, 0xFF));
            FixColor(ref StarFilled, new Color32(0xFF, 0xC2, 0x4B, 0xFF));
            FixColor(ref StarEmpty, new Color32(0x4A, 0x30, 0x6A, 0xFF));
            FixColor(ref CoinTextColor, new Color32(0xFF, 0xD7, 0x70, 0xFF));
            FixColor(ref CorrectGreen, new Color32(0x5C, 0xD6, 0x5C, 0xFF));
            FixColor(ref LockedOverlay, new Color32(0x1A, 0x10, 0x2A, 0xCC));
            FixColor(ref ButtonGradientTop, new Color32(0xFF, 0xFF, 0xFF, 0x1A));
        }

        private static void FixColor(ref Color color, Color fallback)
        {
            // A color with all channels at exactly zero was never serialised
            if (color.r == 0f && color.g == 0f && color.b == 0f && color.a == 0f)
                color = fallback;
        }

        /// <summary>Applies the 9-slice button sprite/colors to a Button + Image.
        /// Falls back to a solid tinted look if no sprite is assigned.</summary>
        public void ApplyButton(Button button, Image image)
        {
            bool isKey = button.name.StartsWith("Key_");
            Color baseColor = isKey ? KeyboardKeyColor : ButtonColor;
            Color textColor = isKey ? KeyboardKeyTextColor : ButtonTextColor;

            // Force using procedural 3D rounded button texture for optimal rounded styling
            image.sprite = ProceduralIcons.RoundedRect;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            button.targetGraphic = image; // Ensure Unity UI color transitions are applied!

            var colors = button.colors;
            colors.normalColor = baseColor;
            colors.highlightedColor = new Color(
                Mathf.Min(baseColor.r * 1.15f, 1f),
                Mathf.Min(baseColor.g * 1.15f, 1f),
                Mathf.Min(baseColor.b * 1.15f, 1f),
                baseColor.a
            );
            colors.pressedColor = NeutralLight;
            colors.disabledColor = new Color(baseColor.r, baseColor.g, baseColor.b, 0.5f);
            button.colors = colors;

            // Automatically find and style the TMPro label inside the button
            var text = button.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
            {
                text.color = textColor;
                text.fontStyle = FontStyles.Bold;
                text.outlineColor = ShadowColor;
                text.outlineWidth = 0.15f;
                if (BodyFont != null) text.font = BodyFont;
            }

            // Add tactile feedback component
            var tactile = button.gameObject.GetComponent<TactileButton>();
            if (tactile == null)
            {
                tactile = button.gameObject.AddComponent<TactileButton>();
            }
        }

        public void ApplyTile(Image image, bool isKeyboardKey)
        {
            var sprite = isKeyboardKey ? KeySprite : TileSprite;
            sprite = sprite != null ? sprite : ProceduralIcons.RoundedRect;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = isKeyboardKey ? NeutralLight : CardBackground;
        }

        /// <summary>Applies the panel sprite to an Image for backgrounds/cards.
        /// Falls back to a solid CardBackground color if no sprite is assigned.</summary>
        public void ApplyPanel(Image image)
        {
            var sprite = PanelSprite != null ? PanelSprite : ProceduralIcons.RoundedRect;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = CardBackground;

            // Add marquee bulb border
            var marquee = image.gameObject.GetComponent<MarqueeBulbBorder>();
            if (marquee == null)
            {
                marquee = image.gameObject.AddComponent<MarqueeBulbBorder>();
            }
        }

        /// <summary>Applies card styling: panel sprite with optional interactivity tint.</summary>
        public void ApplyCard(Image image, bool isInteractive)
        {
            var sprite = PanelSprite != null ? PanelSprite : ProceduralIcons.RoundedRect;
            image.sprite = sprite;
            image.type = Image.Type.Sliced;
            image.color = isInteractive ? CardBackground : LockedOverlay;
        }

        /// <summary>Applies heading font, color, and size to a TMP text element.</summary>
        public void ApplyHeadingText(TextMeshProUGUI text, float fontSize = 36f)
        {
            if (HeadingFont != null) text.font = HeadingFont;
            text.fontSize = fontSize;
            text.fontStyle = FontStyles.Bold;
            text.color = NeutralLight;

            // Add neon glow marquee effect
            var neon = text.gameObject.GetComponent<NeonMarquee>();
            if (neon == null)
            {
                neon = text.gameObject.AddComponent<NeonMarquee>();
            }
        }

        /// <summary>Applies body font, color, and size to a TMP text element.</summary>
        public void ApplyBodyText(TextMeshProUGUI text, float fontSize = 22f)
        {
            if (BodyFont != null) text.font = BodyFont;
            text.fontSize = fontSize;
            text.color = NeutralLight;
        }

        /// <summary>Creates a code-generated vertical gradient texture (top to bottom).</summary>
        public static Texture2D CreateGradientTexture(Color top, Color bottom, int height = 256)
        {
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false);
            texture.wrapMode = TextureWrapMode.Clamp;
            texture.filterMode = FilterMode.Bilinear;
            for (var y = 0; y < height; y++)
            {
                var t = (float)y / (height - 1);
                texture.SetPixel(0, y, Color.Lerp(bottom, top, t));
            }
            texture.Apply();
            return texture;
        }

        /// <summary>Builds a star display string using safe ASCII characters.</summary>
        public string StarDisplay(int earned, int max = 3)
        {
            var sb = new System.Text.StringBuilder();
            for (var i = 0; i < max; i++)
                sb.Append(i < earned ? "*" : "-");
            return sb.ToString();
        }

        /// <summary>Gets the star filled sprite — Kenney asset if assigned, procedural fallback otherwise.</summary>
        public Sprite GetStarFilledSprite()
        {
            return StarFilledSprite != null ? StarFilledSprite : ProceduralIcons.StarFilled;
        }

        /// <summary>Gets the star empty sprite — Kenney asset if assigned, procedural fallback otherwise.</summary>
        public Sprite GetStarEmptySprite()
        {
            return StarEmptySprite != null ? StarEmptySprite : ProceduralIcons.StarEmpty;
        }

        /// <summary>Gets the toggle background sprite — Kenney asset if assigned, procedural fallback.</summary>
        public Sprite GetToggleBackgroundSprite()
        {
            return ToggleBackgroundSprite != null ? ToggleBackgroundSprite : ProceduralIcons.RoundedRect;
        }

        /// <summary>Gets the checkmark sprite — Kenney asset if assigned, procedural fallback.</summary>
        public Sprite GetCheckmarkSprite()
        {
            return ToggleCheckmarkSprite != null ? ToggleCheckmarkSprite : ProceduralIcons.Checkmark;
        }

        /// <summary>Creates a horizontal separator line as a UI Image.</summary>
        public GameObject CreateSeparator(Transform parent, float height = 2f)
        {
            var go = new GameObject("Separator", typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            if (SeparatorSprite != null)
            {
                image.sprite = SeparatorSprite;
                image.type = Image.Type.Sliced;
            }
            image.color = SeparatorColor;
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
            le.flexibleHeight = 0f;
            le.flexibleWidth = 1f;
            return go;
        }
    }
}
