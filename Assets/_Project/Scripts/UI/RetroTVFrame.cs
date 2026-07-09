using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Wraps a UI Image inside a retro wood TV bezel with a scanline texture overlay.
    /// Toggles visibility of the frame and screen elements dynamically based on the parent Image state.
    /// </summary>
    public class RetroTVFrame : MonoBehaviour
    {
        private GameObject _backing;
        private Image _myImage;

        private void Awake()
        {
            _myImage = GetComponent<Image>();
            var rt = transform as RectTransform;
            if (rt == null) return;

            // Wood panel backing
            _backing = new GameObject("TV_Backing", typeof(RectTransform), typeof(Image));
            _backing.transform.SetParent(transform.parent, false);
            var woodRt = (RectTransform)_backing.transform;
            
            // Backing is slightly larger than the image
            woodRt.anchorMin = rt.anchorMin;
            woodRt.anchorMax = rt.anchorMax;
            woodRt.anchoredPosition = rt.anchoredPosition;
            woodRt.sizeDelta = rt.sizeDelta + new Vector2(32f, 32f);

            var woodImg = _backing.GetComponent<Image>();
            woodImg.sprite = ProceduralIcons.RoundedRect;
            woodImg.color = new Color32(0x50, 0x30, 0x20, 0xFF); // Wood brown

            // Re-order so backing is behind the picture image
            transform.SetParent(woodRt, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(16f, 16f);
            rt.offsetMax = new Vector2(-16f, -16f);

            // Add retro scanline overlay on top
            var scanlineGo = new GameObject("TV_Scanlines", typeof(RectTransform), typeof(Image));
            scanlineGo.transform.SetParent(woodRt, false);
            var scanRt = (RectTransform)scanlineGo.transform;
            scanRt.anchorMin = Vector2.zero;
            scanRt.anchorMax = Vector2.one;
            scanRt.offsetMin = new Vector2(16f, 16f);
            scanRt.offsetMax = new Vector2(-16f, -16f);

            var scanImg = scanlineGo.GetComponent<Image>();
            scanImg.color = new Color(0f, 0f, 0f, 0.15f); // Subtle dark scanlines
            scanImg.sprite = CreateScanlineSprite();
            scanImg.type = Image.Type.Sliced;
            scanImg.raycastTarget = false;
        }

        private void Update()
        {
            if (_backing != null && _myImage != null)
            {
                // Only show backing frame if picture clue is revealed and visible
                _backing.SetActive(_myImage.enabled);
            }
        }

        private Sprite CreateScanlineSprite()
        {
            var tex = new Texture2D(1, 4, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.SetPixel(0, 0, new Color(1f, 1f, 1f, 0.4f));
            tex.SetPixel(0, 1, new Color(1f, 1f, 1f, 0.1f));
            tex.SetPixel(0, 2, new Color(1f, 1f, 1f, 0.4f));
            tex.SetPixel(0, 3, new Color(1f, 1f, 1f, 0.1f));
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 1, 4), new Vector2(0.5f, 0.5f));
        }
    }
}
