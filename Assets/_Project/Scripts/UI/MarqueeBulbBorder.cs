using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using PrimeTween;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Spawns a chasing border of circular lights (bulbs) around UI panels to mimic a retro marquee sign.
    /// </summary>
    public class MarqueeBulbBorder : MonoBehaviour
    {
        private readonly List<Image> _bulbs = new List<Image>();
        private readonly List<Tween> _tweens = new List<Tween>();

        private void Start()
        {
            BuildBulbs();
            StartChasingAnimation();
        }

        private void BuildBulbs()
        {
            var rt = transform as RectTransform;
            if (rt == null) return;

            // Destroy existing if any
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Bulb_"))
                {
                    Destroy(child.gameObject);
                }
            }

            var rect = rt.rect;
            float spacing = 32f; // Distance between bulbs

            int horizontalCount = Mathf.Max(2, Mathf.RoundToInt(rect.width / spacing));
            int verticalCount = Mathf.Max(2, Mathf.RoundToInt(rect.height / spacing));

            // Generate bulbs on outer edges
            for (int i = 0; i < horizontalCount; i++)
            {
                float tx = (float)i / (horizontalCount - 1);
                CreateBulb(new Vector2(tx, 1f)); // Top edge
                CreateBulb(new Vector2(tx, 0f)); // Bottom edge
            }

            for (int i = 1; i < verticalCount - 1; i++)
            {
                float ty = (float)i / (verticalCount - 1);
                CreateBulb(new Vector2(0f, ty)); // Left edge
                CreateBulb(new Vector2(1f, ty)); // Right edge
            }
        }

        private void CreateBulb(Vector2 anchor)
        {
            var bulbGo = new GameObject("Bulb_" + _bulbs.Count, typeof(RectTransform), typeof(Image));
            bulbGo.transform.SetParent(transform, false);
            
            var bulbRt = (RectTransform)bulbGo.transform;
            bulbRt.sizeDelta = new Vector2(10f, 10f);
            
            bulbRt.anchorMin = anchor;
            bulbRt.anchorMax = anchor;
            bulbRt.anchoredPosition = Vector2.zero;

            var image = bulbGo.GetComponent<Image>();
            image.sprite = ProceduralIcons.RoundedRect;
            image.color = Color.white;
            _bulbs.Add(image);
        }

        private void StartChasingAnimation()
        {
            for (int i = 0; i < _bulbs.Count; i++)
            {
                var img = _bulbs[i];
                var delay = (i % 2 == 0) ? 0f : 0.4f;
                var gold = new Color32(0xFF, 0xC2, 0x4B, 0xFF);
                var white = new Color32(0xFF, 0xFF, 0xFF, 0x40);

                var t = Tween.Color(img, startValue: gold, endValue: white, duration: 0.4f, cycles: -1, cycleMode: CycleMode.Yoyo, startDelay: delay);
                _tweens.Add(t);
            }
        }

        private void OnDestroy()
        {
            foreach (var t in _tweens) t.Stop();
        }
    }
}
