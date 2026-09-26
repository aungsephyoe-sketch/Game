using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Shared look for the IMGUI screens: palette, fonts, generated textures and draw helpers.</summary>
    public static class UIStyles
    {
        public static readonly Color Gold = new Color(1f, 0.8f, 0.35f);
        public static readonly Color Crimson = new Color(0.85f, 0.15f, 0.2f);
        public static readonly Color Panel = new Color(0.06f, 0.05f, 0.1f, 0.86f);
        public static readonly Color PanelLight = new Color(0.14f, 0.11f, 0.2f, 0.92f);
        public static readonly Color TextDim = new Color(0.75f, 0.72f, 0.82f);
        public static readonly Color Good = new Color(0.45f, 1f, 0.55f);
        public static readonly Color Bad = new Color(1f, 0.4f, 0.4f);

        public static GUIStyle Title, H1, H2, Body, Small, Center, CenterSmall, Right, Button, ButtonBig, ButtonSmall, Big;
        public static Texture2D White, Circle, Ring, Vignette;

        static readonly Dictionary<int, GUIStyle> sized = new Dictionary<int, GUIStyle>();

        public static void Ensure()
        {
            if (Title != null && White != null) return;
            White = MaterialFactory.White;
            Circle = MakeCircle(128, 0f);
            Ring = MakeCircle(128, 0.84f);
            Vignette = MakeVignette(128);

            Body = Label(30, FontStyle.Normal, TextAnchor.UpperLeft, Color.white);
            Small = Label(24, FontStyle.Normal, TextAnchor.UpperLeft, TextDim);
            H2 = Label(36, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            H1 = Label(50, FontStyle.Bold, TextAnchor.UpperLeft, Gold);
            Title = Label(96, FontStyle.Bold, TextAnchor.UpperLeft, Color.white);
            Big = Label(72, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            Center = Label(32, FontStyle.Bold, TextAnchor.MiddleCenter, Color.white);
            CenterSmall = Label(24, FontStyle.Normal, TextAnchor.MiddleCenter, Color.white);
            Right = Label(30, FontStyle.Bold, TextAnchor.MiddleRight, Color.white);

            // Lacquer-and-gold buttons: the same palette as the painted UI frame and the characters' art.
            Button = MakeButton(32, new Color(0.13f, 0.1f, 0.2f), new Color(0.24f, 0.17f, 0.32f));
            ButtonBig = MakeButton(48, new Color(0.55f, 0.08f, 0.14f), new Color(0.75f, 0.15f, 0.2f));
            ButtonSmall = MakeButton(24, new Color(0.13f, 0.1f, 0.2f), new Color(0.24f, 0.17f, 0.32f));
        }

        static GUIStyle Label(int size, FontStyle style, TextAnchor anchor, Color color)
        {
            var s = new GUIStyle(GUI.skin.label)
            {
                fontSize = size,
                fontStyle = style,
                alignment = anchor,
                wordWrap = true,
                richText = true,
                clipping = TextClipping.Overflow
            };
            s.normal.textColor = color;
            return s;
        }

        static GUIStyle MakeButton(int size, Color normal, Color hover)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize = size,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                richText = true
            };
            s.normal.background = Lacquer(normal, new Color(0.78f, 0.62f, 0.3f));
            s.hover.background = Lacquer(hover, new Color(1f, 0.85f, 0.45f));
            s.active.background = Lacquer(hover * 1.25f, new Color(1f, 0.9f, 0.6f));
            s.focused.background = s.normal.background;
            s.normal.textColor = s.focused.textColor = new Color(1f, 0.96f, 0.88f);
            s.hover.textColor = s.active.textColor = Color.white;
            s.border = new RectOffset(8, 8, 8, 8);
            s.padding = new RectOffset(12, 12, 8, 8);
            return s;
        }

        public static GUIStyle Sized(GUIStyle baseStyle, int size)
        {
            int key = baseStyle.GetHashCode() * 31 + size;
            GUIStyle s;
            if (!sized.TryGetValue(key, out s))
            {
                s = new GUIStyle(baseStyle) { fontSize = size };
                sized[key] = s;
            }
            return s;
        }

        /// <summary>Button face: vertical lacquer gradient, a thin gold border and small corner notches.</summary>
        static Texture2D Lacquer(Color face, Color gold)
        {
            const int n = 32;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float k = (float)y / (n - 1);
                    Color c = Color.Lerp(face * 0.7f, face * 1.25f, k);
                    bool edge = x < 2 || y < 2 || x >= n - 2 || y >= n - 2;
                    bool inner = x == 3 || y == 3 || x == n - 4 || y == n - 4;
                    bool corner = (x < 6 && y < 6) || (x >= n - 6 && y < 6) || (x < 6 && y >= n - 6) || (x >= n - 6 && y >= n - 6);
                    if (edge) c = gold;
                    else if (inner) c = Color.Lerp(c, gold, 0.35f);
                    else if (corner && (x + y) % 2 == 0) c = Color.Lerp(c, gold, 0.5f);
                    c.a = 0.96f;
                    px[y * n + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1);
            c.a = 1f;
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        static Texture2D MakeVignette(int n)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                    float d = Mathf.Clamp01((Mathf.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.6f);
                    px[y * n + x] = new Color(1f, 1f, 1f, d * d);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Texture2D MakeCircle(int n, float inner)
        {
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
            t.wrapMode = TextureWrapMode.Clamp;
            var px = new Color[n * n];
            float half = n * 0.5f;
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - half) * (x + 0.5f - half) + (y + 0.5f - half) * (y + 0.5f - half)) / half;
                    float a = Mathf.Clamp01((1f - d) * half * 0.5f);
                    if (inner > 0f) a *= Mathf.Clamp01((d - inner) * half * 0.5f);
                    px[y * n + x] = new Color(1f, 1f, 1f, a);
                }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        // ------------------------------------------------------------------ Draw helpers

        public static void Rect(Rect r, Color c)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = old;
        }

        public static void Frame(Rect r, Color c, float thickness = 3f)
        {
            Rect(new Rect(r.x, r.y, r.width, thickness), c);
            Rect(new Rect(r.x, r.yMax - thickness, r.width, thickness), c);
            Rect(new Rect(r.x, r.y, thickness, r.height), c);
            Rect(new Rect(r.xMax - thickness, r.y, thickness, r.height), c);
        }

        /// <summary>
        /// Menu panel in the game's lacquer-and-gold style: dark lacquer, the painted Higgsfield panel texture
        /// underneath (when downloaded), a double gold border with corner ornaments, and an accent line.
        /// </summary>
        public static void PanelBox(Rect r, Color? accent = null)
        {
            Rect(r, Panel);
            var tex = ArtLibrary.Surface("ui_panel");
            if (tex != null)
            {
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, 0.38f);
                GUI.DrawTextureWithTexCoords(r, tex, new UnityEngine.Rect(0.18f, 0.18f, 0.64f, 0.64f));
                GUI.color = old;
            }
            var gold = new Color(Gold.r, Gold.g, Gold.b, 0.75f);
            Frame(r, gold, 2f);
            Frame(new Rect(r.x + 5f, r.y + 5f, r.width - 10f, r.height - 10f), new Color(Gold.r, Gold.g, Gold.b, 0.25f), 1f);
            float c = Mathf.Min(18f, Mathf.Min(r.width, r.height) * 0.2f);
            foreach (var p in new[] { new Vector2(r.x, r.y), new Vector2(r.xMax - c, r.y), new Vector2(r.x, r.yMax - c), new Vector2(r.xMax - c, r.yMax - c) })
            {
                Rect(new Rect(p.x, p.y, c, 3f), Gold);
                Rect(new Rect(p.x, p.y + c - 3f, c, 3f), Gold);
                Rect(new Rect(p.x, p.y, 3f, c), Gold);
                Rect(new Rect(p.x + c - 3f, p.y, 3f, c), Gold);
            }
            if (accent.HasValue) Rect(new Rect(r.x + c, r.y, r.width - c * 2f, 4f), accent.Value);
        }

        public static void Bar(Rect r, float fill, Color color, Color? back = null)
        {
            Rect(r, back ?? new Color(0f, 0f, 0f, 0.6f));
            Rect(new Rect(r.x + 2f, r.y + 2f, Mathf.Max(0f, (r.width - 4f) * Mathf.Clamp01(fill)), r.height - 4f), color);
        }

        public static void CircleTex(Vector2 center, float radius, Color c, Texture2D tex = null)
        {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(center.x - radius, center.y - radius, radius * 2f, radius * 2f), tex ?? Circle);
            GUI.color = old;
        }

        /// <summary>Circle filled from the bottom up (cooldowns, ultimate gauge).</summary>
        public static void CircleFill(Vector2 center, float radius, float fill, Color c)
        {
            fill = Mathf.Clamp01(fill);
            if (fill <= 0f) return;
            float h = radius * 2f * fill;
            GUI.BeginGroup(new Rect(center.x - radius, center.y + radius - h, radius * 2f, h));
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(0f, h - radius * 2f, radius * 2f, radius * 2f), Circle);
            GUI.color = old;
            GUI.EndGroup();
        }

        public static void Outlined(Rect r, string text, GUIStyle style, Color color, float outline = 3f)
        {
            var old = style.normal.textColor;
            style.normal.textColor = new Color(0f, 0f, 0f, color.a * 0.9f);
            GUI.Label(new Rect(r.x - outline, r.y, r.width, r.height), text, style);
            GUI.Label(new Rect(r.x + outline, r.y, r.width, r.height), text, style);
            GUI.Label(new Rect(r.x, r.y - outline, r.width, r.height), text, style);
            GUI.Label(new Rect(r.x, r.y + outline, r.width, r.height), text, style);
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        public static void Colored(Rect r, string text, GUIStyle style, Color color)
        {
            var old = style.normal.textColor;
            style.normal.textColor = color;
            GUI.Label(r, text, style);
            style.normal.textColor = old;
        }

        public static string Hex(Color c) { return ColorUtility.ToHtmlStringRGB(c); }
    }
}
