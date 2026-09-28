using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The flat, rounded, colour-coded widgets of the reference menus: big menu tiles with an icon and label,
    /// currency pills, portrait cards, progress bars and CLAIM buttons.
    /// </summary>
    public partial class UIManager
    {
        public static readonly Color TileRed = new Color(0.86f, 0.17f, 0.2f);
        public static readonly Color TilePurple = new Color(0.5f, 0.28f, 0.82f);
        public static readonly Color TileBlue = new Color(0.2f, 0.45f, 0.86f);
        public static readonly Color TileGreen = new Color(0.22f, 0.66f, 0.36f);
        public static readonly Color TileOrange = new Color(0.93f, 0.52f, 0.16f);
        public static readonly Color TileMaroon = new Color(0.55f, 0.13f, 0.2f);
        public static readonly Color TileTeal = new Color(0.12f, 0.6f, 0.62f);
        public static readonly Color TileGrey = new Color(0.4f, 0.42f, 0.48f);
        public static readonly Color TileNavy = new Color(0.14f, 0.16f, 0.3f);
        public static readonly Color ProgressYellow = new Color(1f, 0.8f, 0.2f);

        /// <summary>Filled rounded rectangle.</summary>
        public static void Round(Rect r, Color c, float radius = 14f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, 0f, radius);
        }

        /// <summary>Rounded outline.</summary>
        public static void RoundFrame(Rect r, Color c, float width = 3f, float radius = 14f)
        {
            if (Event.current.type != EventType.Repaint) return;
            GUI.DrawTexture(r, Texture2D.whiteTexture, ScaleMode.StretchToFill, true, 0f, c, width, radius);
        }

        /// <summary>
        /// A menu tile: a coloured rounded block with a drop shadow, a lighter top band, a big icon glyph (or
        /// rendered art) and the label in the lower-left. Pops in, swells on hover.
        /// </summary>
        bool Tile(Rect r, string label, string icon, Color c, float delay, Texture art = null, string sub = null, int badge = 0, bool pulse = false)
        {
            float k = Enter(delay);
            if (k <= 0f) return false;
            var rr = Offset(r, 0f, (1f - k) * 40f);
            bool hover = rr.Contains(Event.current.mousePosition);
            float grow = (hover ? 5f : 0f) + (pulse ? (Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f) * 4f : 0f);
            rr = Grow(rr, grow);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, k);

            Round(Offset(rr, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(rr, hover ? Color.Lerp(c, Color.white, 0.12f) : c, 16f);
            Round(new Rect(rr.x, rr.y, rr.width, rr.height * 0.45f), new Color(1f, 1f, 1f, 0.1f), 16f);
            if (pulse) RoundFrame(Grow(rr, 4f), new Color(1f, 0.85f, 0.4f, 0.5f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f)), 3f, 18f);

            if (art != null)
            {
                float h = rr.height * 1.08f;
                float w = h * art.width / Mathf.Max(1f, art.height);
                var ar = new Rect(rr.xMax - w + w * 0.08f, rr.yMax - h, w, h);
                GUI.BeginGroup(rr);
                GUI.DrawTexture(new Rect(ar.x - rr.x, ar.y - rr.y, ar.width, ar.height), art, ScaleMode.ScaleToFit, true);
                GUI.EndGroup();
            }
            else if (!string.IsNullOrEmpty(icon))
            {
                int size = Mathf.RoundToInt(Mathf.Min(rr.height * 0.5f, 90f));
                UIStyles.Colored(new Rect(rr.x, rr.y + rr.height * 0.06f, rr.width, rr.height * 0.6f), icon, UIStyles.Sized(UIStyles.Center, size), new Color(1f, 1f, 1f, 0.95f));
            }

            int labelSize = rr.height > 200f ? 64 : rr.height > 110f ? 30 : 26;
            UIStyles.Outlined(new Rect(rr.x + 18f, rr.yMax - labelSize - (sub != null ? 44f : 18f), rr.width - 24f, labelSize + 10f), label, UIStyles.Sized(UIStyles.H2, labelSize), Color.white, 2f);
            if (sub != null) GUI.Label(new Rect(rr.x + 20f, rr.yMax - 44f, rr.width - 24f, 34f), sub, UIStyles.Sized(UIStyles.Small, 22));
            if (badge > 0) Badge(new Vector2(rr.xMax - 10f, rr.y + 10f), badge > 9 ? "!" : badge.ToString(), UIStyles.Crimson);

            bool clicked = GUI.Button(rr, GUIContent.none, GUIStyle.none);
            GUI.color = old;
            if (clicked && k > 0.6f && gm != null) gm.Audio.Play("click", 0.5f);
            return clicked && k > 0.6f;
        }

        /// <summary>Gold coin with a $ stamp.</summary>
        public static void CoinIcon(Vector2 c, float size)
        {
            UIStyles.CircleTex(c, size * 0.5f, new Color(0.85f, 0.55f, 0.08f));
            UIStyles.CircleTex(c, size * 0.42f, new Color(1f, 0.8f, 0.2f));
            UIStyles.CircleTex(c + new Vector2(-size * 0.08f, -size * 0.1f), size * 0.18f, new Color(1f, 0.93f, 0.55f, 0.6f));
            UIStyles.Colored(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), "$", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(size * 0.55f)), new Color(0.7f, 0.42f, 0.05f));
        }

        /// <summary>XP: a purple orb with "XP" on it.</summary>
        public static void XpIcon(Vector2 c, float size)
        {
            UIStyles.CircleTex(c, size * 0.5f, new Color(0.42f, 0.22f, 0.7f));
            UIStyles.CircleTex(c, size * 0.42f, new Color(0.66f, 0.45f, 1f));
            UIStyles.Colored(new Rect(c.x - size * 0.5f, c.y - size * 0.5f, size, size), "<b>XP</b>", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(size * 0.36f)), Color.white);
        }

        /// <summary>
        /// Rotates later GUI drawing around a point given in the 1080-tall virtual space. (GUIUtility.RotateAroundPivot
        /// takes the pivot in screen pixels, which put rotated icons in the wrong place under the UI scale.)
        /// </summary>
        public static void RotateGui(float angle, Vector2 pivot)
        {
            var p = new Vector3(pivot.x, pivot.y, 0f);
            GUI.matrix = GUI.matrix * Matrix4x4.TRS(p, Quaternion.Euler(0f, 0f, angle), Vector3.one) * Matrix4x4.TRS(-p, Quaternion.identity, Vector3.one);
        }

        /// <summary>Blue faceted crystal.</summary>
        public static void DiamondIcon(Vector2 c, float size)
        {
            var old = GUI.matrix;
            RotateGui(45f, c);
            float s = size * 0.62f;
            Round(new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s, s), new Color(0.12f, 0.45f, 0.95f), s * 0.12f);
            Round(new Rect(c.x - s * 0.5f, c.y - s * 0.5f, s * 0.5f, s * 0.5f), new Color(0.55f, 0.85f, 1f), s * 0.1f);
            Round(new Rect(c.x, c.y, s * 0.5f, s * 0.5f), new Color(0.08f, 0.3f, 0.75f), s * 0.1f);
            GUI.matrix = old;
        }

        /// <summary>Padlock: rounded body, shackle and keyhole.</summary>
        public static void LockIcon(Vector2 c, float h, Color col)
        {
            RoundFrame(new Rect(c.x - h * 0.3f, c.y - h * 0.52f, h * 0.6f, h * 0.8f), col, h * 0.14f, h * 0.3f);
            Round(new Rect(c.x - h * 0.45f, c.y - h * 0.08f, h * 0.9f, h * 0.62f), col, h * 0.12f);
            UIStyles.CircleTex(new Vector2(c.x, c.y + h * 0.16f), h * 0.09f, new Color(0.15f, 0.15f, 0.2f));
            Round(new Rect(c.x - h * 0.04f, c.y + h * 0.18f, h * 0.08f, h * 0.18f), new Color(0.15f, 0.15f, 0.2f), h * 0.03f);
        }

        /// <summary>
        /// A home-screen tile in the reference style: vertical gradient, soft rim, big icon above a centred label,
        /// or a painted background with the label bottom-left and a chevron (PLAY / STORY).
        /// </summary>
        bool HomeTile(Rect r, string label, Texture2D icon, Color c, float delay, Texture2D art = null, Texture portrait = null, int badge = 0, bool glow = false, System.Action<Rect> picture = null)
        {
            float k = Enter(delay, 0.5f);
            if (k <= 0f) return false;
            float t = Time.unscaledTime;
            float seed = r.x * 0.013f + r.y * 0.007f;
            // Entrance: rise and pop in; idle: a gentle float, each tile on its own rhythm.
            float pop = k < 1f ? Mathf.Lerp(0.85f, 1f, k) + Mathf.Sin(k * Mathf.PI) * 0.05f : 1f;
            var rr = Offset(r, 0f, (1f - k) * 50f + Mathf.Sin(t * 1.4f + seed * 7f) * 3f);
            bool hover = rr.Contains(Event.current.mousePosition);
            bool pressed = hover && Input.GetMouseButton(0);
            float scale = pop * (hover ? 1.035f : 1f) * (pressed ? 0.96f : 1f);
            rr = new Rect(rr.center.x - rr.width * scale * 0.5f, rr.center.y - rr.height * scale * 0.5f, rr.width * scale, rr.height * scale);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, k);
            // Soft coloured glow behind (always a little, stronger for the featured tile and on hover).
            float g = 0.5f + 0.5f * Mathf.Sin(t * 2.2f + seed * 5f);
            int rings = glow ? 4 : hover ? 3 : 2;
            for (int i = rings; i >= 1; i--) Round(Grow(rr, i * 5f), new Color(c.r, c.g, c.b, (glow || hover ? 0.08f : 0.04f) + 0.04f * g), 16f + i * 5f);
            Round(Offset(rr, 0f, 7f), new Color(0f, 0f, 0f, 0.45f), 16f);
            if (art != null)
            {
                if (Event.current.type == EventType.Repaint) GUI.DrawTexture(rr, art, ScaleMode.ScaleAndCrop, false, 0f, Color.white, 0f, 16f);
                // Dark gradient at the bottom so the label reads.
                for (int i = 0; i < 5; i++) Round(new Rect(rr.x, rr.yMax - rr.height * (0.12f + i * 0.07f), rr.width, rr.height * (0.12f + i * 0.07f)), new Color(0f, 0f, 0f, 0.1f), 16f);
            }
            else
            {
                Color bottom = Color.Lerp(c, Color.black, 0.4f);
                Round(rr, bottom, 16f);
                for (int i = 0; i < 6; i++)
                {
                    float f = i / 6f;
                    Round(new Rect(rr.x, rr.y, rr.width, rr.height * (1f - f)), new Color(c.r, c.g, c.b, 0.22f), 16f);
                }
                // Big faint emblem in the corner.
                if (icon != null)
                {
                    var o3 = GUI.color;
                    GUI.color = new Color(1f, 1f, 1f, 0.08f * k);
                    float es = rr.height * 0.9f;
                    GUI.DrawTexture(new Rect(rr.xMax - es * 0.75f, rr.yMax - es * 0.8f, es, es), icon, ScaleMode.ScaleToFit, true);
                    GUI.color = o3;
                }
            }
            Round(new Rect(rr.x + 3f, rr.y + 3f, rr.width - 6f, rr.height * 0.4f), new Color(1f, 1f, 1f, hover ? 0.13f : 0.07f), 14f);
            if (portrait != null)
            {
                // The leader in full colour, breathing, with a warm rim light behind.
                float breathe = 1f + Mathf.Sin(t * 1.8f) * 0.012f;
                float h = rr.height * 1.02f * breathe, w = h * portrait.width / Mathf.Max(1f, portrait.height);
                var pr = new Rect(rr.x + rr.width * 0.66f - w * 0.5f, rr.yMax - h, w, h);
                UIStyles.CircleTex(new Vector2(pr.center.x, pr.center.y - h * 0.08f), h * 0.36f, new Color(1f, 0.85f, 0.5f, 0.18f + 0.06f * g));
                GUI.BeginGroup(rr);
                var o2 = GUI.color;
                GUI.color = new Color(0f, 0f, 0f, 0.35f * k);
                GUI.DrawTexture(new Rect(pr.x - rr.x + 8f, pr.y - rr.y + 4f, pr.width, pr.height), portrait, ScaleMode.ScaleToFit, true);
                GUI.color = new Color(1f, 1f, 1f, k);
                GUI.DrawTexture(new Rect(pr.x - rr.x, pr.y - rr.y, pr.width, pr.height), portrait, ScaleMode.ScaleToFit, true);
                GUI.color = o2;
                GUI.EndGroup();
            }
            // A band of light sweeps across every few seconds (clipped to the tile).
            float period = 4.5f;
            float ph = Mathf.Repeat(t + seed * 3f, period) / period;
            if (ph < 0.35f)
            {
                float bx = Mathf.Lerp(-rr.width * 0.4f, rr.width * 1.2f, ph / 0.35f);
                GUI.BeginGroup(rr);
                int slices = 14;
                float sh = rr.height / slices;
                for (int i = 0; i < slices; i++)
                    UIStyles.Rect(new Rect(bx - i * sh * 0.45f, i * sh, rr.width * 0.12f, sh + 1f), new Color(1f, 1f, 1f, 0.16f));
                GUI.EndGroup();
            }
            // Twinkles.
            for (int i = 0; i < 3; i++)
            {
                float tw = Mathf.Repeat(t * 0.6f + i * 0.37f + seed, 1f);
                float a = Mathf.Sin(tw * Mathf.PI);
                var sp = new Vector2(rr.x + rr.width * Mathf.Repeat(0.2f + i * 0.31f + seed, 0.9f) + 10f, rr.y + rr.height * (0.15f + 0.2f * i));
                UIStyles.CircleTex(sp, 2.5f + 3f * a, new Color(1f, 1f, 1f, 0.55f * a));
            }
            // Animated border: the tile colour breathing toward white.
            RoundFrame(rr, Color.Lerp(c, Color.white, (hover ? 0.7f : 0.35f) + 0.25f * g), glow ? 3.5f : 2.5f, 16f);
            if (art != null || portrait != null)
            {
                UIStyles.Outlined(new Rect(rr.x + 26f, rr.yMax - 78f, rr.width - 60f, 62f), label, UIStyles.Sized(UIStyles.H1, 46), Color.white, 2.5f);
                float nudge = Mathf.Sin(t * 4f + seed) * 4f;
                UIStyles.Outlined(new Rect(rr.xMax - 56f + nudge, rr.yMax - 78f, 40f, 62f), "›", UIStyles.Sized(UIStyles.H1, 52), Color.white, 2f);
            }
            else
            {
                float isz = Mathf.Min(rr.height * 0.42f, 86f) * (hover ? 1.1f : 1f);
                if (picture != null)
                {
                    // A picture of what's inside (faces, the team, gear...) instead of a plain icon.
                    float bob = Mathf.Sin(t * 1.8f + seed * 4f) * 3f;
                    GUI.BeginGroup(new Rect(rr.x + 6f, rr.y + 6f, rr.width - 12f, rr.height - 66f));
                    picture(new Rect(0f, bob, rr.width - 12f, rr.height - 66f));
                    GUI.EndGroup();
                }
                else if (icon != null)
                {
                    float bob = Mathf.Sin(t * 2.4f + seed * 4f) * 4f;
                    var ir = new Rect(rr.center.x - isz * 0.5f, rr.y + rr.height * 0.18f + bob, isz, isz);
                    var o2 = GUI.color;
                    UIStyles.CircleTex(ir.center, isz * 0.62f, new Color(1f, 1f, 1f, 0.1f + 0.05f * g));
                    GUI.color = new Color(0f, 0f, 0f, 0.35f * k);
                    GUI.DrawTexture(Offset(ir, 2f, 4f), icon, ScaleMode.ScaleToFit, true);
                    GUI.color = new Color(1f, 1f, 1f, k);
                    GUI.DrawTexture(ir, icon, ScaleMode.ScaleToFit, true);
                    GUI.color = o2;
                }
                UIStyles.Outlined(new Rect(rr.x, rr.yMax - 60f, rr.width, 46f), label, UIStyles.Sized(UIStyles.Center, 28), Color.white, 2f);
            }
            if (badge > 0)
            {
                float bp = 1f + 0.12f * Mathf.Abs(Mathf.Sin(t * 3f));
                Badge(new Vector2(rr.xMax - 8f, rr.y + 8f), badge > 9 ? "!" : badge.ToString(), UIStyles.Crimson, bp);
            }
            bool clicked = GUI.Button(rr, GUIContent.none, GUIStyle.none);
            GUI.color = old;
            if (clicked && k > 0.6f && gm != null) gm.Audio.Play("click", 0.5f);
            return clicked && k > 0.6f;
        }

        /// <summary>Reference-style resource pill: icon, amount, and a + button.</summary>
        void PlusPill(Rect r, bool coins, string value, System.Action onPlus) { PlusPill(r, coins ? 0 : 1, value, onPlus); }

        /// <summary>kind: 0 gold, 1 diamonds, 2 XP.</summary>
        void PlusPill(Rect r, int kind, string value, System.Action onPlus)
        {
            Round(r, new Color(0.05f, 0.06f, 0.1f, 0.75f), 12f);
            RoundFrame(r, new Color(1f, 1f, 1f, 0.1f), 2f, 12f);
            Vector2 ic = new Vector2(r.x + r.height * 0.55f, r.center.y);
            if (kind == 0) CoinIcon(ic, r.height * 0.62f); else if (kind == 1) DiamondIcon(ic, r.height * 0.7f); else XpIcon(ic, r.height * 0.66f);
            // The bigger the number, the smaller the text, so it always fits on one line.
            var vr = new Rect(r.x + r.height, r.y, r.width - r.height * 2f + 6f, r.height);
            GUI.Label(vr, value, FitStyle(value, vr.width, 30, 14));
            var pr = new Rect(r.xMax - r.height, r.y, r.height, r.height);
            GUI.Label(pr, "+", UIStyles.Sized(UIStyles.Center, 34));
            if (GUI.Button(pr, GUIContent.none, GUIStyle.none) && onPlus != null) { gm.Audio.Play("click", 0.5f); onPlus(); }
        }

        static readonly System.Collections.Generic.Dictionary<int, GUIStyle> fitStyles = new System.Collections.Generic.Dictionary<int, GUIStyle>();

        /// <summary>A one-line style whose font shrinks (from max down to min) until the text fits the width.</summary>
        static GUIStyle FitStyle(string text, float width, int max, int min)
        {
            GUIStyle st = null;
            for (int size = max; size >= min; size -= 2)
            {
                if (!fitStyles.TryGetValue(size, out st))
                {
                    st = new GUIStyle(UIStyles.Sized(UIStyles.Body, size)) { wordWrap = false, clipping = TextClipping.Overflow, alignment = TextAnchor.MiddleLeft };
                    fitStyles[size] = st;
                }
                if (st.CalcSize(new GUIContent(text)).x <= width) return st;
            }
            return st;
        }

        bool IconButton(Rect r, Texture2D icon)
        {
            bool hover = r.Contains(Event.current.mousePosition);
            Round(r, hover ? new Color(0.2f, 0.24f, 0.34f, 0.9f) : new Color(0.12f, 0.16f, 0.24f, 0.85f), 12f);
            RoundFrame(r, new Color(1f, 1f, 1f, 0.12f), 2f, 12f);
            GUI.DrawTexture(new Rect(r.x + r.width * 0.22f, r.y + r.height * 0.22f, r.width * 0.56f, r.height * 0.56f), icon, ScaleMode.ScaleToFit, true);
            bool c = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (c) gm.Audio.Play("click", 0.5f);
            return c;
        }

        /// <summary>Currency pill: dark rounded capsule, coloured icon disc, value.</summary>
        void Pill(Rect r, string icon, Color c, string value)
        {
            Round(r, new Color(0.05f, 0.05f, 0.1f, 0.78f), r.height * 0.5f);
            UIStyles.CircleTex(new Vector2(r.x + r.height * 0.5f, r.center.y), r.height * 0.5f - 3f, c);
            GUI.Label(new Rect(r.x, r.y, r.height, r.height), icon, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(r.height * 0.5f)));
            var vr = new Rect(r.x + r.height + 6f, r.y, r.width - r.height - 16f, r.height);
            var st = new GUIStyle(FitStyle(value, vr.width, Mathf.RoundToInt(r.height * 0.5f), 12)) { alignment = TextAnchor.MiddleRight };
            GUI.Label(vr, value, st);
        }

        /// <summary>A portrait card: element-coloured frame, rendered portrait, optional level tag.</summary>
        void PortraitCard(Rect r, CharacterDefinition def, bool fullBody, string topLeft = null, bool selected = false)
        {
            Color ec = ElementChart.ColorOf(def.element);
            Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.35f), 12f);
            Round(r, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), ec, 0.28f), 12f);
            var tex = fullBody ? ArtLibrary.CharacterFull(def) : ArtLibrary.Character(def);
            if (tex != null) GUI.DrawTexture(new Rect(r.x + 4f, r.y + 4f, r.width - 8f, r.height - 8f), tex, ScaleMode.ScaleAndCrop, true);
            else GUI.Label(r, def.displayName.Substring(0, 1), UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(r.height * 0.4f)));
            RoundFrame(r, selected ? UIStyles.Gold : ec, selected ? 5f : 3f, 12f);
            // Element badge in the corner.
            UIStyles.CircleTex(new Vector2(r.x + 18f, r.y + 18f), 15f, ec);
            GUI.Label(new Rect(r.x + 3f, r.y + 3f, 30f, 30f), ElementChart.Icon(def.element), UIStyles.Sized(UIStyles.Center, 18));
            if (topLeft != null) UIStyles.Outlined(new Rect(r.x + 38f, r.y + 4f, r.width - 40f, 30f), topLeft, UIStyles.Sized(UIStyles.Body, 20), Color.white, 2f);
        }

        /// <summary>Thick yellow progress bar on a dark track, with the count on it.</summary>
        void ProgressBar(Rect r, float fill, string text)
        {
            Round(r, new Color(0f, 0f, 0f, 0.5f), r.height * 0.5f);
            if (fill > 0.01f) Round(new Rect(r.x, r.y, Mathf.Max(r.height, r.width * Mathf.Clamp01(fill)), r.height), ProgressYellow, r.height * 0.5f);
            if (text != null) UIStyles.Outlined(r, text, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(r.height * 0.7f)), Color.white, 2f);
        }

        /// <summary>Flat colour button in the reference style (CLAIM, SELECT, TRAVEL &amp; PLAY...).</summary>
        static readonly System.Collections.Generic.Dictionary<int, float> bounceAt = new System.Collections.Generic.Dictionary<int, float>();
        static readonly Color Ink = new Color(0.06f, 0.05f, 0.12f, 0.96f);

        static int RectKey(Rect r) { return Mathf.RoundToInt(r.x * 0.2f) * 73856093 ^ Mathf.RoundToInt(r.width) * 19349663 ^ Mathf.RoundToInt(r.height) * 83492791; }

        /// <summary>
        /// A chunky cartoon button body: thick dark outline, a raised face on a darker slab (the 3D extrusion), a
        /// glossy top, a drop shadow; it sinks when pressed and bounces when released. Returns the face rect.
        /// </summary>
        Rect ChunkyBody(Rect r, Color c, bool enabled, float radius, out bool hover)
        {
            hover = enabled && r.Contains(Event.current.mousePosition);
            bool pressed = hover && Input.GetMouseButton(0);
            float depth = Mathf.Clamp(r.height * 0.1f, 5f, 12f);
            float bounce = 1f;
            float t0;
            if (bounceAt.TryGetValue(RectKey(r), out t0))
            {
                float t = (Time.unscaledTime - t0) / 0.28f;
                if (t < 1f) bounce = 1f + 0.07f * Mathf.Sin(t * Mathf.PI) * (1f - t);
            }
            if (hover && !pressed) bounce *= 1.02f;
            Vector2 cc = r.center;
            r = new Rect(cc.x - r.width * 0.5f * bounce, cc.y - r.height * 0.5f * bounce, r.width * bounce, r.height * bounce);
            Color body = enabled ? c : Color.Lerp(c, new Color(0.35f, 0.36f, 0.42f), 0.65f);
            float sink = pressed ? depth * 0.75f : 0f;
            var face = new Rect(r.x, r.y + sink, r.width, r.height - depth);
            var slab = new Rect(r.x, r.y + depth, r.width, r.height - depth);
            Round(Offset(Grow(r, 3f), 0f, 6f), new Color(0f, 0f, 0f, 0.3f), radius + 3f);
            Round(Grow(new Rect(r.x, r.y + sink, r.width, r.height - sink), 4f), Ink, radius + 4f);
            Round(slab, Color.Lerp(body, Color.black, 0.45f), radius);
            Round(face, hover ? Color.Lerp(body, Color.white, 0.12f) : body, radius);
            Round(new Rect(face.x, face.y + face.height * 0.52f, face.width, face.height * 0.48f), new Color(0f, 0f, 0f, 0.12f), radius);
            Round(new Rect(face.x + 7f, face.y + 5f, face.width - 14f, face.height * 0.34f), new Color(1f, 1f, 1f, 0.28f), Mathf.Max(4f, radius - 4f));
            return face;
        }

        void Bounce(Rect r) { bounceAt[RectKey(r)] = Time.unscaledTime; }

        /// <summary>A chunky panel in a colour (name plates, tags) without button behaviour.</summary>
        void FlatBtnLook(Rect r, Color c)
        {
            float depth = Mathf.Clamp(r.height * 0.1f, 4f, 8f);
            Round(Offset(Grow(r, 3f), 0f, 5f), new Color(0f, 0f, 0f, 0.3f), 16f);
            Round(Grow(r, 4f), new Color(0.06f, 0.05f, 0.12f, 0.96f), 18f);
            Round(new Rect(r.x, r.y + depth, r.width, r.height - depth), Color.Lerp(c, Color.black, 0.45f), 14f);
            Round(new Rect(r.x, r.y, r.width, r.height - depth), c, 14f);
            Round(new Rect(r.x + 6f, r.y + 4f, r.width - 12f, (r.height - depth) * 0.36f), new Color(1f, 1f, 1f, 0.28f), 10f);
        }

        /// <summary>Chunky colour button (CLAIM, SELECT, PLAY...): see <see cref="ChunkyBody"/>.</summary>
        bool FlatBtn(Rect r, string label, Color c, bool enabled = true, int size = 28)
        {
            bool hover;
            var face = ChunkyBody(r, c, enabled, Mathf.Min(16f, r.height * 0.3f), out hover);
            UIStyles.Outlined(face, label, UIStyles.Sized(UIStyles.Center, size), enabled ? Color.white : new Color(1f, 1f, 1f, 0.6f), size >= 24 ? 3f : 2f);
            bool clicked = GUI.Button(r, GUIContent.none, GUIStyle.none) && enabled;
            if (clicked)
            {
                Bounce(r);
                if (gm != null) gm.Audio.Play("click", 0.5f);
            }
            return clicked;
        }
    }
}
