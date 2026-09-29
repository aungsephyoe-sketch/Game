using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Painted icons for every piece of gear, generated once and cached: a rarity-coloured glow and frame behind
    /// the item itself — a blade with its guard and wrapped grip, a haori with sleeves and a trim, or an
    /// accessory (charm, mask, bell, beads, ribbon, pin, amulet or heart). Mythic gear shimmers with sparkles.
    /// </summary>
    public static class GearArt
    {
        const int S = 192;
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(EquipmentDefinition e)
        {
            if (e == null) return null;
            Texture2D t;
            if (cache.TryGetValue(e.id, out t) && t != null) return t;
            // Painted artwork (tools/gen_accessory_art.py) when there is one, else the procedural icon.
            t = Resources.Load<Texture2D>("Gear/" + e.id);
            if (t == null) t = Paint(e);
            cache[e.id] = t;
            return t;
        }

        static float Seg(Vector2 p, Vector2 a, Vector2 b)
        {
            Vector2 ab = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
            return (p - a - ab * h).magnitude;
        }

        static float Hash(string s)
        {
            unchecked { int h = 17; foreach (char c in s) h = h * 31 + c; return Mathf.Abs(h % 1000) / 1000f; }
        }

        static Texture2D Paint(EquipmentDefinition e)
        {
            var tex = new Texture2D(S, S, TextureFormat.RGBA32, true) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Trilinear };
            var px = new Color[S * S];
            Color rc = RarityInfo.Color(e.rarity);
            float hue = Hash(e.id);
            Color tint = Color.HSVToRGB(hue, 0.55f, 0.95f);
            bool mythic = e.rarity >= 6, legend = e.rarity >= 5;
            var rng = new System.Random(e.id.GetHashCode());
            var sparkles = new List<Vector3>();
            for (int i = 0; i < (mythic ? 14 : legend ? 7 : 0); i++) sparkles.Add(new Vector3((float)rng.NextDouble(), (float)rng.NextDouble(), 0.01f + (float)rng.NextDouble() * 0.02f));
            string id = e.id;

            for (int y = 0; y < S; y++)
                for (int x = 0; x < S; x++)
                {
                    var p = new Vector2((x + 0.5f) / S, (y + 0.5f) / S); // 0..1, y up
                    // Background: rarity glow in a rounded tile.
                    float corner = RoundedBox(p, 0.47f, 0.12f);
                    if (corner > 0f) { px[y * S + x] = new Color(0f, 0f, 0f, 0f); continue; }
                    float d = (p - new Vector2(0.5f, 0.5f)).magnitude;
                    Color c = Color.Lerp(Color.Lerp(rc, Color.black, 0.25f), new Color(0.06f, 0.06f, 0.1f), Mathf.Clamp01(d * 1.6f));
                    // Light rays for legendary and mythic.
                    if (legend)
                    {
                        float ang = Mathf.Atan2(p.y - 0.5f, p.x - 0.5f);
                        float ray = Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 6f)), 12f) * Mathf.Clamp01(1f - d * 1.8f);
                        c = Color.Lerp(c, Color.Lerp(rc, Color.white, 0.4f), ray * 0.5f);
                    }
                    // The item.
                    Color item; float cover;
                    switch (e.slot)
                    {
                        case EquipSlot.Sword: Sword(p, rc, tint, mythic, out item, out cover); break;
                        case EquipSlot.Haori: Haori(p, rc, tint, out item, out cover); break;
                        default: Accessory(p, id, rc, tint, out item, out cover); break;
                    }
                    c = Color.Lerp(c, item, cover);
                    foreach (var s in sparkles)
                    {
                        Vector2 sp = new Vector2(s.x, s.y);
                        Vector2 q = p - sp;
                        float star = Mathf.Clamp01(1f - (Mathf.Abs(q.x) * Mathf.Abs(q.y) * 4000f + q.magnitude * 30f));
                        c = Color.Lerp(c, Color.white, star);
                    }
                    // Frame.
                    float edge = -RoundedBox(p, 0.47f, 0.12f);
                    if (edge < 0.018f) c = Color.Lerp(rc, Color.white, 0.25f);
                    else if (edge < 0.026f) c = Color.Lerp(c, Color.black, 0.5f);
                    px[y * S + x] = new Color(c.r, c.g, c.b, 1f);
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        /// <summary>Signed distance to a rounded square centred in the tile (negative inside).</summary>
        static float RoundedBox(Vector2 p, float half, float r)
        {
            Vector2 q = new Vector2(Mathf.Abs(p.x - 0.5f), Mathf.Abs(p.y - 0.5f)) - new Vector2(half - r, half - r);
            return new Vector2(Mathf.Max(q.x, 0f), Mathf.Max(q.y, 0f)).magnitude + Mathf.Min(Mathf.Max(q.x, q.y), 0f) - r;
        }

        static void Sword(Vector2 p, Color rc, Color tint, bool mythic, out Color col, out float cover)
        {
            // Diagonal blade from bottom-left grip to top-right tip.
            Vector2 hilt = new Vector2(0.28f, 0.28f), guard = new Vector2(0.36f, 0.36f), tip = new Vector2(0.8f, 0.8f);
            col = Color.clear; cover = 0f;
            float blade = Seg(p, guard, tip) - Mathf.Lerp(0.045f, 0.012f, Mathf.Clamp01(Vector2.Dot(p - guard, (tip - guard).normalized) / (tip - guard).magnitude));
            if (mythic && blade < 0.03f) { col = Color.Lerp(rc, Color.white, 0.3f); cover = Mathf.Clamp01(1f - blade / 0.03f) * 0.6f; }
            if (blade < 0f)
            {
                // Steel with a bright edge line and a tint along the fuller.
                Vector2 n = new Vector2(-0.7071f, 0.7071f);
                float side = Vector2.Dot(p - guard, n);
                col = side > 0f ? new Color(0.92f, 0.94f, 0.98f) : new Color(0.68f, 0.72f, 0.8f);
                if (Mathf.Abs(side) < 0.008f) col = Color.Lerp(col, tint, 0.6f);
                cover = 1f;
            }
            float g = Seg(p, guard + new Vector2(0.07f, -0.07f), guard + new Vector2(-0.07f, 0.07f)) - 0.026f;
            if (g < 0f) { col = Color.Lerp(new Color(0.85f, 0.7f, 0.3f), rc, 0.3f); cover = 1f; }
            float grip = Seg(p, hilt - new Vector2(0.08f, 0.08f), guard) - 0.028f;
            if (grip < 0f)
            {
                float wrap = Mathf.Sin((p.x + p.y) * 90f);
                col = wrap > 0f ? new Color(0.2f, 0.12f, 0.1f) : Color.Lerp(tint, Color.black, 0.3f);
                cover = 1f;
            }
            float pommel = (p - (hilt - new Vector2(0.09f, 0.09f))).magnitude - 0.032f;
            if (pommel < 0f) { col = new Color(0.85f, 0.7f, 0.3f); cover = 1f; }
        }

        static void Haori(Vector2 p, Color rc, Color tint, out Color col, out float cover)
        {
            col = Color.clear; cover = 0f;
            // Body: a trapezoid, wider at the hem; sleeves: two wide wings.
            float y = p.y, x = Mathf.Abs(p.x - 0.5f);
            bool body = y > 0.18f && y < 0.74f && x < Mathf.Lerp(0.2f, 0.14f, (y - 0.18f) / 0.56f);
            bool sleeves = y > 0.46f && y < 0.72f && x < 0.34f && x > 0.1f && (y - 0.46f) > (x - 0.1f) * 0.2f;
            if (!(body || sleeves)) return;
            Color cloth = Color.Lerp(tint, rc, 0.35f);
            // Pattern: diamond checks near the hem, a lighter collar, a dark centre opening.
            float check = Mathf.Sin(p.x * 60f) * Mathf.Sin(p.y * 60f);
            col = y < 0.32f ? (check > 0f ? cloth : Color.Lerp(cloth, Color.black, 0.35f)) : cloth;
            if (x < 0.015f && y > 0.2f) col = new Color(0.1f, 0.08f, 0.1f);
            float collar = Mathf.Abs(x - (0.74f - y) * 0.4f);
            if (y > 0.5f && collar < 0.02f) col = new Color(0.95f, 0.92f, 0.85f);
            // Soft shading from the left.
            col = Color.Lerp(col, Color.black, Mathf.Clamp01((p.x - 0.5f) * 0.6f));
            cover = 1f;
        }

        static void Accessory(Vector2 p, string id, Color rc, Color tint, out Color col, out float cover)
        {
            col = Color.clear; cover = 0f;
            Vector2 c = new Vector2(0.5f, 0.48f);
            float d = (p - c).magnitude;
            if (id.Contains("mask"))
            {
                // Fox mask: a white face with ears and red markings.
                float face = (new Vector2((p.x - 0.5f) * 1.1f, p.y - 0.45f)).magnitude - 0.24f;
                float ear = Mathf.Min(Tri(p, new Vector2(0.32f, 0.62f), new Vector2(0.4f, 0.64f), new Vector2(0.3f, 0.82f)), Tri(p, new Vector2(0.6f, 0.64f), new Vector2(0.68f, 0.62f), new Vector2(0.7f, 0.82f)));
                if (face < 0f || ear < 0f) { col = new Color(0.97f, 0.95f, 0.92f); cover = 1f; }
                if ((face < 0f) && (Mathf.Abs(p.y - 0.5f) < 0.02f) && Mathf.Abs(Mathf.Abs(p.x - 0.5f) - 0.1f) < 0.05f) col = new Color(0.85f, 0.15f, 0.15f);
                return;
            }
            if (id.Contains("bell"))
            {
                float bell = Mathf.Max((new Vector2(p.x - 0.5f, (p.y - 0.46f) * 1.2f)).magnitude - 0.2f, 0.26f - p.y);
                if (bell < 0f) { col = Color.Lerp(new Color(0.95f, 0.8f, 0.35f), tint, 0.2f); cover = 1f; if (Mathf.Abs(p.y - 0.4f) < 0.012f) col = new Color(0.5f, 0.35f, 0.1f); }
                if ((p - new Vector2(0.5f, 0.3f)).magnitude < 0.035f) { col = new Color(0.4f, 0.28f, 0.1f); cover = 1f; }
                if (Seg(p, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.8f)) < 0.012f) { col = new Color(0.85f, 0.2f, 0.2f); cover = 1f; }
                return;
            }
            if (id.Contains("beads"))
            {
                for (int i = 0; i < 12; i++)
                {
                    float a = i * Mathf.PI * 2f / 12f;
                    Vector2 bc = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 0.22f;
                    float bd = (p - bc).magnitude;
                    if (bd < 0.05f) { col = Color.Lerp(new Color(0.25f, 0.7f, 0.45f), Color.white, bd < 0.02f ? 0.4f : 0f); cover = 1f; }
                }
                return;
            }
            if (id.Contains("ribbon"))
            {
                float bow = Mathf.Min(Tri(p, c, new Vector2(0.22f, 0.66f), new Vector2(0.22f, 0.32f)), Tri(p, c, new Vector2(0.78f, 0.66f), new Vector2(0.78f, 0.32f)));
                float tail = Mathf.Min(Seg(p, c, new Vector2(0.4f, 0.18f)), Seg(p, c, new Vector2(0.6f, 0.18f))) - 0.03f;
                if (bow < 0f || tail < 0f) { col = Color.Lerp(tint, new Color(0.9f, 0.3f, 0.5f), 0.5f); cover = 1f; }
                if (d < 0.05f) { col = Color.Lerp(tint, Color.white, 0.3f); cover = 1f; }
                return;
            }
            if (id.Contains("heart"))
            {
                Vector2 q = new Vector2(Mathf.Abs(p.x - 0.5f) * 1.2f, p.y - 0.4f);
                float heart = Mathf.Min((q - new Vector2(0.11f, 0.12f)).magnitude - 0.13f, Tri(p, new Vector2(0.24f, 0.5f), new Vector2(0.76f, 0.5f), new Vector2(0.5f, 0.2f)));
                if (heart < 0f)
                {
                    float flame = Mathf.Sin(p.x * 40f + p.y * 25f) * 0.5f + 0.5f;
                    col = Color.Lerp(new Color(1f, 0.35f, 0.1f), new Color(1f, 0.85f, 0.3f), flame * Mathf.Clamp01(p.y * 1.5f));
                    cover = 1f;
                }
                return;
            }
            if (id.Contains("earrings") || id.Contains("suncrest") || id.Contains("charm"))
            {
                // A crest / charm disc with a coloured rim and an emblem.
                if (d < 0.25f) { col = Color.Lerp(new Color(0.95f, 0.85f, 0.5f), tint, 0.25f); cover = 1f; }
                if (d < 0.19f) { col = Color.Lerp(tint, rc, 0.4f); }
                float star = Mathf.Abs(Mathf.Cos(Mathf.Atan2(p.y - c.y, p.x - c.x) * 4f));
                if (d < 0.05f + 0.1f * Mathf.Pow(star, 8f)) col = new Color(1f, 0.95f, 0.75f);
                if (Seg(p, new Vector2(0.5f, 0.73f), new Vector2(0.5f, 0.86f)) < 0.012f) { col = new Color(0.8f, 0.2f, 0.25f); cover = 1f; }
                return;
            }
            // Amulet: a chain loop and a faceted gem.
            float chain = Mathf.Abs((new Vector2(p.x - 0.5f, (p.y - 0.62f) * 1.3f)).magnitude - 0.2f) - 0.01f;
            if (chain < 0f && p.y > 0.5f) { col = new Color(0.85f, 0.75f, 0.45f); cover = 1f; }
            float gem = Mathf.Abs(p.x - 0.5f) + Mathf.Abs(p.y - 0.38f) * 0.8f - 0.17f;
            if (gem < 0f)
            {
                bool facet = (p.x < 0.5f) ^ (p.y > 0.38f);
                col = Color.Lerp(Color.Lerp(tint, rc, 0.5f), Color.white, facet ? 0.35f : 0f);
                cover = 1f;
            }
        }

        static float Tri(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            // Negative inside the triangle.
            float d1 = Cross(p - a, b - a), d2 = Cross(p - b, c - b), d3 = Cross(p - c, a - c);
            bool neg = d1 < 0f || d2 < 0f || d3 < 0f, pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return (neg && pos) ? 1f : -1f;
        }

        static float Cross(Vector2 a, Vector2 b) { return a.x * b.y - a.y * b.x; }
    }
}
