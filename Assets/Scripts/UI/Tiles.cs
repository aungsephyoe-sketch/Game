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
        bool HomeTile(Rect r, string label, Texture2D icon, Color c, float delay, Texture2D art = null, Texture portrait = null, int badge = 0, bool glow = false)
        {
            float k = Enter(delay);
            if (k <= 0f) return false;
            var rr = Offset(r, 0f, (1f - k) * 40f);
            bool hover = rr.Contains(Event.current.mousePosition);
            if (hover) rr = Grow(rr, 4f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, k);
            if (glow)
            {
                float g = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                for (int i = 3; i >= 1; i--) Round(Grow(rr, i * 5f), new Color(c.r, c.g, c.b, 0.07f + 0.05f * g), 16f + i * 5f);
            }
            Round(Offset(rr, 0f, 6f), new Color(0f, 0f, 0f, 0.4f), 14f);
            if (art != null)
            {
                if (Event.current.type == EventType.Repaint) GUI.DrawTexture(rr, art, ScaleMode.ScaleAndCrop, false, 0f, Color.white, 0f, 14f);
            }
            else
            {
                Color bottom = Color.Lerp(c, Color.black, 0.35f);
                Round(rr, bottom, 14f);
                for (int i = 0; i < 6; i++)
                {
                    float f = i / 6f;
                    Round(new Rect(rr.x, rr.y, rr.width, rr.height * (1f - f)), new Color(c.r, c.g, c.b, 0.22f), 14f);
                }
                Round(new Rect(rr.x + 3f, rr.y + 3f, rr.width - 6f, rr.height * 0.42f), new Color(1f, 1f, 1f, hover ? 0.12f : 0.07f), 12f);
            }
            if (portrait != null)
            {
                // A dark silhouette of the leader against the glow.
                float h = rr.height * 0.98f, w = h * portrait.width / Mathf.Max(1f, portrait.height);
                var pr = new Rect(rr.x + rr.width * 0.46f - w * 0.5f, rr.yMax - h, w, h);
                var o2 = GUI.color;
                GUI.color = new Color(0.12f, 0.01f, 0.03f, 0.92f * k);
                GUI.DrawTexture(Offset(pr, 6f, 0f), portrait, ScaleMode.ScaleToFit, true);
                GUI.color = new Color(0.25f, 0.02f, 0.05f, k);
                GUI.DrawTexture(pr, portrait, ScaleMode.ScaleToFit, true);
                GUI.color = o2;
            }
            RoundFrame(rr, Color.Lerp(c, Color.white, hover ? 0.6f : 0.4f), glow ? 3f : 2f, 14f);
            if (art != null || portrait != null)
            {
                UIStyles.Outlined(new Rect(rr.x + 26f, rr.yMax - 76f, rr.width - 60f, 60f), label, UIStyles.Sized(UIStyles.H1, 46), Color.white, 2f);
                UIStyles.Outlined(new Rect(rr.xMax - 56f, rr.yMax - 76f, 40f, 60f), "›", UIStyles.Sized(UIStyles.H1, 52), Color.white, 2f);
            }
            else
            {
                float isz = Mathf.Min(rr.height * 0.42f, 86f);
                if (icon != null)
                {
                    var ir = new Rect(rr.center.x - isz * 0.5f, rr.y + rr.height * 0.18f, isz, isz);
                    var o2 = GUI.color;
                    GUI.color = new Color(0f, 0f, 0f, 0.35f * k);
                    GUI.DrawTexture(Offset(ir, 2f, 3f), icon, ScaleMode.ScaleToFit, true);
                    GUI.color = new Color(1f, 1f, 1f, k);
                    GUI.DrawTexture(ir, icon, ScaleMode.ScaleToFit, true);
                    GUI.color = o2;
                }
                UIStyles.Outlined(new Rect(rr.x, rr.yMax - 58f, rr.width, 44f), label, UIStyles.Sized(UIStyles.Center, 28), Color.white, 2f);
            }
            if (badge > 0) Badge(new Vector2(rr.xMax - 8f, rr.y + 8f), badge > 9 ? "!" : badge.ToString(), UIStyles.Crimson);
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
            GUI.Label(new Rect(r.x + r.height, r.y, r.width - r.height * 2f, r.height), value, UIStyles.Sized(UIStyles.Body, 30));
            var pr = new Rect(r.xMax - r.height, r.y, r.height, r.height);
            GUI.Label(pr, "+", UIStyles.Sized(UIStyles.Center, 34));
            if (GUI.Button(pr, GUIContent.none, GUIStyle.none) && onPlus != null) { gm.Audio.Play("click", 0.5f); onPlus(); }
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
            GUI.Label(new Rect(r.x + r.height + 6f, r.y, r.width - r.height - 16f, r.height), value, UIStyles.Sized(UIStyles.Right, Mathf.RoundToInt(r.height * 0.5f)));
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
        bool FlatBtn(Rect r, string label, Color c, bool enabled = true, int size = 28)
        {
            bool hover = enabled && r.Contains(Event.current.mousePosition);
            var rr = hover ? Grow(r, 3f) : r;
            Round(Offset(rr, 0f, 4f), new Color(0f, 0f, 0f, 0.35f), 12f);
            Round(rr, enabled ? (hover ? Color.Lerp(c, Color.white, 0.15f) : c) : Color.Lerp(c, Color.gray, 0.6f), 12f);
            Round(new Rect(rr.x, rr.y, rr.width, rr.height * 0.45f), new Color(1f, 1f, 1f, 0.12f), 12f);
            UIStyles.Outlined(rr, label, UIStyles.Sized(UIStyles.Center, size), enabled ? Color.white : new Color(1f, 1f, 1f, 0.6f), 2f);
            bool clicked = GUI.Button(rr, GUIContent.none, GUIStyle.none) && enabled;
            if (clicked && gm != null) gm.Audio.Play("click", 0.5f);
            return clicked;
        }
    }
}
