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

            // Flat rounded buttons, the same on every screen: dark navy for normal actions, red for the main one.
            Button = MakeButton(30, new Color(0.13f, 0.16f, 0.27f), new Color(0.2f, 0.24f, 0.38f));
            ButtonBig = MakeButton(44, new Color(0.84f, 0.13f, 0.13f), new Color(0.95f, 0.22f, 0.2f));
            ButtonSmall = MakeButton(22, new Color(0.13f, 0.16f, 0.27f), new Color(0.2f, 0.24f, 0.38f));
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
            s.normal.background = RoundedTex(normal, new Color(1f, 1f, 1f, 0.14f));
            s.hover.background = RoundedTex(hover, new Color(1f, 1f, 1f, 0.24f));
            s.active.background = RoundedTex(normal * 0.85f, new Color(1f, 1f, 1f, 0.3f));
            s.focused.background = s.normal.background;
            s.onNormal.background = s.normal.background;
            s.normal.textColor = s.focused.textColor = Color.white;
            s.hover.textColor = s.active.textColor = Color.white;
            s.border = new RectOffset(14, 14, 14, 14);
            s.padding = new RectOffset(12, 12, 8, 8);
            return s;
        }

        /// <summary>A 9-sliceable rounded rectangle: fill with a lighter top half and a thin rim.</summary>
        static Texture2D RoundedTex(Color fill, Color rim)
        {
            const int n = 48;
            const float rad = 13f;
            var t = new Texture2D(n, n, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var px = new Color[n * n];
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float cx = Mathf.Clamp(x + 0.5f, rad, n - rad), cy = Mathf.Clamp(y + 0.5f, rad, n - rad);
                    float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy)) - rad;
                    float a = Mathf.Clamp01(0.5f - d);
                    Color c = fill;
                    if (y > n / 2) c = Color.Lerp(c, Color.white, 0.07f); // texture y is bottom-up: this is the top half
                    if (d > -2f) c = Color.Lerp(c, new Color(rim.r, rim.g, rim.b, 1f), rim.a);
                    c.a = a * fill.a;
                    px[y * n + x] = c;
                }
            t.SetPixels(px);
            t.Apply();
            return t;
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
        /// <summary>The standard panel: dark translucent rounded rectangle, soft shadow, thin rim, optional accent strip.</summary>
        public static void PanelBox(Rect r, Color? accent = null)
        {
            if (Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(new UnityEngine.Rect(r.x, r.y + 6f, r.width, r.height), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0f, 0f, 0f, 0.35f), 0f, 16f);
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(0.05f, 0.06f, 0.1f, 0.9f), 0f, 16f);
                GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, new Color(1f, 1f, 1f, 0.08f), 2f, 16f);
                if (accent.HasValue)
                    GUI.DrawTexture(new UnityEngine.Rect(r.x, r.y, r.width, 8f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, accent.Value, 0f, 4f);
            }
        }

        /// <summary>Rounded progress bar.</summary>
        public static void Bar(Rect r, float fill, Color color, Color? back = null)
        {
            if (Event.current.type != EventType.Repaint) return;
            float rad = r.height * 0.5f;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, back ?? new Color(0f, 0f, 0f, 0.6f), 0f, rad);
            float w = (r.width - 4f) * Mathf.Clamp01(fill);
            if (w > 1f) GUI.DrawTexture(new UnityEngine.Rect(r.x + 2f, r.y + 2f, Mathf.Max(w, r.height - 4f), r.height - 4f), Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, color, 0f, rad - 2f);
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
