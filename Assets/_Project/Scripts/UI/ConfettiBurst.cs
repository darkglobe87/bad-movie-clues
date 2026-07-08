using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Confetti and coin burst celebrations using plain UI Images animated with PrimeTween.
    /// Used in place of Shuriken ParticleSystem to allow overlay canvas sorting.
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

        /// <summary>Coins fly from a click position to the coin display icon.</summary>
        public static void PlayCoinBurst(RectTransform parent, Vector2 fromPosition, Vector2 toPosition, int count = 12)
        {
            var goldColor = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
            for (var i = 0; i < count; i++)
            {
                var coinGo = new GameObject("CoinParticle", typeof(RectTransform), typeof(Image));
                coinGo.transform.SetParent(parent, false);

                var rt = (RectTransform)coinGo.transform;
                var size = Random.Range(12f, 18f);
                rt.sizeDelta = new Vector2(size, size);
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = fromPosition;

                var image = coinGo.GetComponent<Image>();
                image.color = goldColor;

                // Burst outward first
                var angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
                var burstDist = Random.Range(40f, 80f);
                var burstOffset = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * burstDist;
                var peakPosition = fromPosition + burstOffset;

                var duration = Random.Range(0.6f, 0.9f);
                var delay = Random.Range(0f, 0.15f);

                Sequence.Create()
                    .Group(Tween.UIAnchoredPosition(rt, endValue: peakPosition, duration: 0.25f, ease: Ease.OutQuad))
                    .Chain(Tween.UIAnchoredPosition(rt, endValue: toPosition, duration: duration - 0.25f, ease: Ease.InQuad))
                    .Group(Tween.Scale(rt, endValue: 0.4f, duration: duration, ease: Ease.InCubic))
                    .Group(Tween.Alpha(image, endValue: 0f, duration: 0.1f, startDelay: duration - 0.1f))
                    .SetDelay(delay);

                Object.Destroy(coinGo, duration + delay + 0.1f);
            }
        }
    }
}
