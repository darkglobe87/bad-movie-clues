using UnityEngine;

namespace BadMovieClues.UI
{
    /// <summary>
    /// Generates crisp procedural icon sprites at runtime so the game
    /// looks premium even when external sprite assets (Kenney pack icons)
    /// haven't been imported yet. All sprites are cached as static
    /// instances — created once on first access, reused everywhere.
    ///
    /// When the full Kenney UI Pack sprites are wired into UITheme, the
    /// theme's assigned sprites take priority (code in UITheme checks
    /// its own fields first). These procedural icons serve as the
    /// always-available fallback.
    /// </summary>
    public static class ProceduralIcons
    {
        private static Sprite _starFilled;
        private static Sprite _starEmpty;
        private static Sprite _checkmark;
        private static Sprite _roundedRect;

        /// <summary>Solid white 5-point star on transparent background (64×64).</summary>
        public static Sprite StarFilled => _starFilled ??= GenerateStar(filled: true);

        /// <summary>Outline-only 5-point star on transparent background (64×64).</summary>
        public static Sprite StarEmpty => _starEmpty ??= GenerateStar(filled: false);

        /// <summary>White checkmark tick on transparent background (48×48).</summary>
        public static Sprite Checkmark => _checkmark ??= GenerateCheckmark();

        /// <summary>White rounded rectangle for toggle/button backgrounds (64×36, 9-slice safe).</summary>
        public static Sprite RoundedRect => _roundedRect ??= GenerateRoundedRectangle();

        // ────────────────────────────────────────────
        //  Star generation
        // ────────────────────────────────────────────

        private static Sprite GenerateStar(bool filled)
        {
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var clear = new Color(0, 0, 0, 0);
            var white = Color.white;
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            float cx = size / 2f;
            float cy = size / 2f;
            float outerR = size * 0.46f;
            float innerR = size * 0.18f;

            // 10 vertices: alternating outer tip / inner notch
            var verts = new Vector2[10];
            for (int i = 0; i < 10; i++)
            {
                // Start from top (π/2) and go clockwise
                float angle = Mathf.PI / 2f + i * Mathf.PI / 5f;
                float r = (i % 2 == 0) ? outerR : innerR;
                verts[i] = new Vector2(cx + r * Mathf.Cos(angle), cy + r * Mathf.Sin(angle));
            }

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    bool inside = PointInPolygon(p, verts);
                    float edgeDist = DistToPolygonEdge(p, verts);

                    if (filled)
                    {
                        // Filled star with anti-aliased edge
                        if (inside)
                            pixels[y * size + x] = edgeDist < 1.2f
                                ? new Color(1, 1, 1, Mathf.Clamp01(edgeDist / 1.2f))
                                : white;
                    }
                    else
                    {
                        // Outline star: ~2px stroke along the edge
                        const float strokeWidth = 2.2f;
                        if (edgeDist < strokeWidth)
                        {
                            float alpha = Mathf.Clamp01((strokeWidth - edgeDist) / 1.2f);
                            pixels[y * size + x] = new Color(1, 1, 1, alpha);
                        }
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ────────────────────────────────────────────
        //  Checkmark generation
        // ────────────────────────────────────────────

        private static Sprite GenerateCheckmark()
        {
            const int size = 48;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var clear = new Color(0, 0, 0, 0);
            var pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            // Checkmark as two line segments:
            //   From (10, 22) to (20, 12)  — the short down-stroke
            //   From (20, 12) to (38, 36)  — the long up-stroke
            DrawThickLine(pixels, size, new Vector2(10, 22), new Vector2(20, 12), 3.5f);
            DrawThickLine(pixels, size, new Vector2(20, 12), new Vector2(38, 36), 3.5f);

            tex.SetPixels(pixels);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        }

        // ────────────────────────────────────────────
        //  Rounded rectangle generation
        // ────────────────────────────────────────────

        private static Sprite GenerateRoundedRectangle()
        {
            const int w = 64;
            const int h = 36;
            const float radius = 8f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            var clear = new Color(0, 0, 0, 0);
            var white = Color.white;
            var pixels = new Color[w * h];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = clear;

            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dist = RoundedRectSDF(x + 0.5f, y + 0.5f, w, h, radius);
                    if (dist < 0f)
                    {
                        // Inside: fully opaque
                        pixels[y * w + x] = white;
                    }
                    else if (dist < 1.2f)
                    {
                        // Anti-aliased edge
                        pixels[y * w + x] = new Color(1, 1, 1, Mathf.Clamp01(1.2f - dist));
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            // 9-slice borders for proper scaling
            var border = new Vector4(radius + 2, radius + 2, radius + 2, radius + 2);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border);
        }

        // ────────────────────────────────────────────
        //  Geometry helpers
        // ────────────────────────────────────────────

        private static bool PointInPolygon(Vector2 p, Vector2[] polygon)
        {
            bool inside = false;
            int n = polygon.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                var a = polygon[i];
                var b = polygon[j];
                if ((a.y > p.y) != (b.y > p.y) &&
                    p.x < (b.x - a.x) * (p.y - a.y) / (b.y - a.y) + a.x)
                {
                    inside = !inside;
                }
            }
            return inside;
        }

        private static float DistToPolygonEdge(Vector2 p, Vector2[] polygon)
        {
            float minDist = float.MaxValue;
            int n = polygon.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                float d = DistToSegment(p, polygon[i], polygon[j]);
                if (d < minDist) minDist = d;
            }
            return minDist;
        }

        private static float DistToSegment(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Vector2.Dot(ab, ab));
            var projection = a + t * ab;
            return Vector2.Distance(p, projection);
        }

        private static void DrawThickLine(Color[] pixels, int texSize, Vector2 a, Vector2 b, float thickness)
        {
            var dir = (b - a).normalized;
            float len = Vector2.Distance(a, b);
            int minX = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.x, b.x) - thickness - 1));
            int maxX = Mathf.Min(texSize - 1, Mathf.CeilToInt(Mathf.Max(a.x, b.x) + thickness + 1));
            int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - thickness - 1));
            int maxY = Mathf.Min(texSize - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + thickness + 1));

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    var p = new Vector2(x + 0.5f, y + 0.5f);
                    float d = DistToSegment(p, a, b);
                    if (d < thickness)
                    {
                        float alpha = Mathf.Clamp01((thickness - d) / 1.2f);
                        ref var pixel = ref pixels[y * texSize + x];
                        pixel.a = Mathf.Max(pixel.a, alpha);
                        pixel.r = pixel.g = pixel.b = 1f;
                    }
                }
            }
        }

        private static float RoundedRectSDF(float px, float py, float w, float h, float r)
        {
            // Signed distance to a rounded rectangle centered in (0..w, 0..h)
            float cx = w * 0.5f;
            float cy = h * 0.5f;
            float dx = Mathf.Abs(px - cx) - (cx - r);
            float dy = Mathf.Abs(py - cy) - (cy - r);

            float outside = new Vector2(Mathf.Max(dx, 0), Mathf.Max(dy, 0)).magnitude;
            float inside = Mathf.Min(Mathf.Max(dx, dy), 0);
            return outside + inside - r;
        }
    }
}
