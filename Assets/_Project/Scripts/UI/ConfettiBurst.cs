using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Win-celebration confetti burst. Built from plain UI Images animated
    /// with PrimeTween, not a real Shuriken ParticleSystem like
    /// AmbientDustBackground - deliberate deviation from the plan's literal
    /// "ConfettiBurst particle prefab" wording: a ParticleSystem is rendered
    /// by a camera, and a camera can never draw on top of a Screen Space
    /// Overlay Canvas (confirmed the hard way by M9's background-behind-UI
    /// fix - camera output is always behind Overlay canvases, never in
    /// front). Since this needs to appear on top of the LevelCompleteScreen
    /// overlay itself, UI-space animated Images are the only approach that
    /// actually renders in the right place.
    /// </summary>
    public static class ConfettiBurst
    {
        private static readonly Color32[] Palette =
        {
            new Color32(0xFF, 0xC2, 0x4B, 0xFF), // marquee gold
            new Color32(0xFF, 0x4E, 0x8B, 0xFF), // B-movie magenta
            new Color32(0xB6, 0xFF, 0x3C, 0xFF), // radioactive lime
        };

        public static void Play(RectTransform parent, int count = 60)
        {
            for (var i = 0; i < count; i++)
            {
                var pieceGo = new GameObject("Confetti", typeof(RectTransform), typeof(Image));
                pieceGo.transform.SetParent(parent, false);

                var rt = (RectTransform)pieceGo.transform;
                var size = Random.Range(8f, 16f);
                rt.sizeDelta = new Vector2(size, size * Random.Range(0.4f, 1f));
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.55f);
                rt.anchoredPosition = Vector2.zero;

                var image = pieceGo.GetComponent<Image>();
                image.color = Palette[Random.Range(0, Palette.Length)];

                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var burstSpeed = Random.Range(150f, 350f);
                var burstTarget = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * burstSpeed;
                var fallTarget = burstTarget + new Vector2(0f, -Random.Range(250f, 450f));
                var duration = Random.Range(1.1f, 1.8f);

                Tween.UIAnchoredPosition(rt, endValue: fallTarget, duration: duration, ease: Ease.OutCubic);
                Tween.Rotation(rt, endValue: new Vector3(0f, 0f, Random.Range(-720f, 720f)), duration: duration);
                Tween.Alpha(image, endValue: 0f, duration: 0.4f, startDelay: duration - 0.4f);

                Object.Destroy(pieceGo, duration + 0.1f);
            }
        }
    }
}
