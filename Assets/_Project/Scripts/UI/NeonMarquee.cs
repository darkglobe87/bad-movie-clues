using UnityEngine;
using TMPro;
using PrimeTween;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Flickers headings randomly to simulate a campy old theatre neon sign.
    /// </summary>
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class NeonMarquee : MonoBehaviour
    {
        private TextMeshProUGUI _text;
        private Color _baseColor;
        private Color32 _glowColor = new Color32(0xFF, 0x4E, 0x8B, 0xFF); // AccentMagenta neon glow

        private void Awake()
        {
            _text = GetComponent<TextMeshProUGUI>();
            _baseColor = _text.color;
            
            // Add a thick neon outline using TMPro features
            _text.outlineWidth = 0.25f;
            _text.outlineColor = _glowColor;
            
            StartFlickerLoop();
        }

        private void StartFlickerLoop()
        {
            // Flickers randomly to simulate a campy old neon sign
            Sequence.Create(cycles: -1)
                .ChainDelay(UnityEngine.Random.Range(2f, 5f))
                .Chain(Tween.Alpha(_text, endValue: 0.3f, duration: 0.05f))
                .Chain(Tween.Alpha(_text, endValue: 1.0f, duration: 0.08f, cycles: 2, cycleMode: CycleMode.Yoyo))
                .ChainDelay(0.1f)
                .Chain(Tween.Alpha(_text, endValue: 0.2f, duration: 0.05f))
                .Chain(Tween.Alpha(_text, endValue: 1.0f, duration: 0.05f));
        }
    }
}
