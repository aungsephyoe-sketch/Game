using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The setting of a slayer's card poster: a painted backdrop (sunset hills, moonlit night, sakura dusk, sunny
    /// ocean, deep forest, snowy peaks, volcano, thunderstorm, lantern rooftops) and the matching light on the
    /// slayer (key colour and direction, a coloured rim from behind, ambient). Picked from their element with a
    /// personal twist, so neighbouring cards look different. Backdrops are painted once into small textures.
    /// </summary>
    public struct PosterEnv
    {
        public enum Kind { SunsetHills, MoonNight, SakuraDusk, OceanDay, Forest, SnowPeaks, Volcano, Storm, Rooftops }

        public Kind kind;
        public Color key, rim, ambient;
        public Vector3 keyEuler;
        public float keyIntensity;

        static int Seed(string id)
        {
            int h = 17;
            foreach (char c in id) h = h * 31 + c;
            return h & 0x7fffffff;
        }

        public static PosterEnv For(CharacterDefinition def)
        {
            bool alt = ((Seed(def.id) >> 2) & 1) == 1;
            Kind k;
            switch (def.element)
            {
                case Element.Flame: k = alt ? Kind.Volcano : Kind.SunsetHills; break;
                case Element.Water: k = alt ? Kind.MoonNight : Kind.OceanDay; break;
                case Element.Beast: k = alt ? Kind.SakuraDusk : Kind.Forest; break;
                case Element.Thunder: k = alt ? Kind.Rooftops : Kind.Storm; break;
                case Element.Earth: k = alt ? Kind.SnowPeaks : Kind.SunsetHills; break;
                case Element.Light: k = alt ? Kind.OceanDay : Kind.SakuraDusk; break;
                default: k = alt ? Kind.Storm : Kind.MoonNight; break;
            }
            return Of(k);
        }

        public static PosterEnv Of(Kind k)
        {
            var e = new PosterEnv { kind = k, keyIntensity = 1.15f, keyEuler = new Vector3(30f, 150f, 0f) };
            switch (k)
            {
                case Kind.SunsetHills: e.key = new Color(1f, 0.78f, 0.55f); e.rim = new Color(1f, 0.45f, 0.25f); e.ambient = new Color(0.62f, 0.5f, 0.55f); e.keyEuler = new Vector3(18f, 120f, 0f); break;
                case Kind.MoonNight: e.key = new Color(0.72f, 0.8f, 1f); e.rim = new Color(0.45f, 0.65f, 1f); e.ambient = new Color(0.4f, 0.44f, 0.62f); e.keyEuler = new Vector3(40f, 200f, 0f); e.keyIntensity = 1f; break;
                case Kind.SakuraDusk: e.key = new Color(1f, 0.86f, 0.9f); e.rim = new Color(1f, 0.55f, 0.78f); e.ambient = new Color(0.62f, 0.52f, 0.62f); break;
                case Kind.OceanDay: e.key = new Color(1f, 0.97f, 0.9f); e.rim = new Color(0.5f, 0.85f, 1f); e.ambient = new Color(0.6f, 0.66f, 0.72f); e.keyEuler = new Vector3(45f, 160f, 0f); e.keyIntensity = 1.25f; break;
                case Kind.Forest: e.key = new Color(0.92f, 1f, 0.8f); e.rim = new Color(0.55f, 1f, 0.55f); e.ambient = new Color(0.45f, 0.56f, 0.45f); e.keyEuler = new Vector3(55f, 130f, 0f); break;
                case Kind.SnowPeaks: e.key = new Color(0.95f, 0.97f, 1f); e.rim = new Color(0.7f, 0.85f, 1f); e.ambient = new Color(0.66f, 0.7f, 0.78f); e.keyIntensity = 1.2f; break;
                case Kind.Volcano: e.key = new Color(1f, 0.62f, 0.4f); e.rim = new Color(1f, 0.3f, 0.1f); e.ambient = new Color(0.5f, 0.36f, 0.36f); e.keyEuler = new Vector3(10f, 190f, 0f); break;
                case Kind.Storm: e.key = new Color(0.82f, 0.84f, 1f); e.rim = new Color(0.8f, 0.65f, 1f); e.ambient = new Color(0.42f, 0.42f, 0.55f); e.keyEuler = new Vector3(60f, 140f, 0f); break;
                default: e.key = new Color(1f, 0.82f, 0.6f); e.rim = new Color(1f, 0.55f, 0.35f); e.ambient = new Color(0.46f, 0.42f, 0.55f); e.keyEuler = new Vector3(15f, 210f, 0f); break;
            }
            return e;
        }

        // ------------------------------------------------------------------ Painted backdrops

        static readonly Dictionary<Kind, Texture2D> backdrops = new Dictionary<Kind, Texture2D>();
        const int Size = 160;

        public Texture2D Backdrop()
        {
            Texture2D t;
            if (backdrops.TryGetValue(kind, out t) && t != null) return t;
            t = Paint(kind);
            backdrops[kind] = t;
            return t;
        }

        static float Hash(float x) { float s = Mathf.Sin(x * 127.1f) * 43758.5453f; return s - Mathf.Floor(s); }

        static Texture2D Paint(Kind k)
        {
            var px = new Color[Size * Size];
            Color top, bot, far, near, glow;
            float sunX = 0.7f, sunY = 0.66f, sunR = 0.1f;
            bool moon = false, stars = false;
            switch (k)
            {
                case Kind.SunsetHills: top = new Color(0.98f, 0.5f, 0.32f); bot = new Color(1f, 0.85f, 0.5f); far = new Color(0.72f, 0.36f, 0.42f); near = new Color(0.36f, 0.18f, 0.3f); glow = new Color(1f, 0.95f, 0.7f); sunY = 0.42f; sunR = 0.14f; break;
                case Kind.MoonNight: top = new Color(0.05f, 0.07f, 0.2f); bot = new Color(0.2f, 0.26f, 0.5f); far = new Color(0.14f, 0.18f, 0.36f); near = new Color(0.06f, 0.08f, 0.18f); glow = new Color(0.95f, 0.96f, 1f); moon = true; stars = true; sunX = 0.72f; sunY = 0.75f; sunR = 0.09f; break;
                case Kind.SakuraDusk: top = new Color(0.55f, 0.42f, 0.78f); bot = new Color(1f, 0.72f, 0.82f); far = new Color(0.78f, 0.5f, 0.68f); near = new Color(0.4f, 0.24f, 0.4f); glow = new Color(1f, 0.9f, 0.85f); sunY = 0.5f; sunR = 0.12f; break;
                case Kind.OceanDay: top = new Color(0.3f, 0.62f, 1f); bot = new Color(0.78f, 0.92f, 1f); far = new Color(0.55f, 0.72f, 0.9f); near = new Color(0.1f, 0.45f, 0.75f); glow = new Color(1f, 1f, 0.9f); sunY = 0.8f; sunR = 0.08f; break;
                case Kind.Forest: top = new Color(0.45f, 0.72f, 0.6f); bot = new Color(0.85f, 0.95f, 0.7f); far = new Color(0.3f, 0.52f, 0.38f); near = new Color(0.1f, 0.26f, 0.18f); glow = new Color(1f, 1f, 0.8f); sunY = 0.78f; sunR = 0.07f; break;
                case Kind.SnowPeaks: top = new Color(0.4f, 0.58f, 0.92f); bot = new Color(0.86f, 0.92f, 1f); far = new Color(0.52f, 0.6f, 0.8f); near = new Color(0.78f, 0.85f, 0.96f); glow = new Color(1f, 1f, 1f); sunY = 0.78f; sunR = 0.06f; break;
                case Kind.Volcano: top = new Color(0.16f, 0.06f, 0.08f); bot = new Color(0.8f, 0.28f, 0.12f); far = new Color(0.3f, 0.12f, 0.12f); near = new Color(0.12f, 0.05f, 0.06f); glow = new Color(1f, 0.6f, 0.2f); sunR = 0f; stars = false; break;
                case Kind.Storm: top = new Color(0.14f, 0.12f, 0.26f); bot = new Color(0.42f, 0.36f, 0.62f); far = new Color(0.26f, 0.22f, 0.4f); near = new Color(0.1f, 0.08f, 0.18f); glow = new Color(0.9f, 0.85f, 1f); sunR = 0f; break;
                default: top = new Color(0.12f, 0.1f, 0.3f); bot = new Color(0.95f, 0.55f, 0.4f); far = new Color(0.32f, 0.2f, 0.36f); near = new Color(0.12f, 0.08f, 0.16f); glow = new Color(1f, 0.8f, 0.45f); moon = true; stars = true; sunX = 0.25f; sunY = 0.8f; sunR = 0.07f; break;
            }
            for (int y = 0; y < Size; y++)
            {
                float v = y / (float)(Size - 1);
                for (int x = 0; x < Size; x++)
                {
                    float u = x / (float)(Size - 1);
                    Color c = Color.Lerp(bot, top, Mathf.SmoothStep(0.15f, 1f, v));
                    // Sun or moon with a soft halo.
                    if (sunR > 0f)
                    {
                        float d = Mathf.Sqrt((u - sunX) * (u - sunX) + (v - sunY) * (v - sunY));
                        c = Color.Lerp(c, glow, Mathf.Clamp01(1f - (d - sunR) / (sunR * 2.5f)) * 0.35f);
                        if (d < sunR) c = moon && Mathf.Sqrt((u - sunX - sunR * 0.4f) * (u - sunX - sunR * 0.4f) + (v - sunY - sunR * 0.2f) * (v - sunY - sunR * 0.2f)) < sunR * 0.85f ? c : glow;
                    }
                    if (stars && v > 0.45f && Hash(x * 0.37f + y * 11.3f) > 0.992f) c = Color.Lerp(c, Color.white, 0.8f);
                    // Clouds: soft bands.
                    if (k == Kind.Storm || k == Kind.OceanDay || k == Kind.SnowPeaks)
                    {
                        float cl = Mathf.Sin(u * 9f + v * 3f) * 0.5f + Mathf.Sin(u * 23f - v * 5f) * 0.25f;
                        float band = Mathf.Clamp01(1f - Mathf.Abs(v - 0.72f) * 7f) * Mathf.Clamp01(cl + 0.3f);
                        c = Color.Lerp(c, k == Kind.Storm ? new Color(0.3f, 0.26f, 0.44f) : Color.white, band * 0.6f);
                    }
                    float hFar, hNear;
                    Silhouettes(k, u, out hFar, out hNear);
                    if (v < hFar) c = Color.Lerp(far, c, 0.15f * (hFar - v) / Mathf.Max(0.01f, hFar));
                    if (k == Kind.SnowPeaks && v < hFar && v > hFar - 0.06f) c = Color.Lerp(c, Color.white, 0.8f); // snow caps
                    if (v < hNear) c = near;
                    // Water: sparkling lines under the horizon.
                    if (k == Kind.OceanDay && v < 0.3f && v > hNear && Mathf.Sin(u * 60f + v * 200f) > 0.93f) c = Color.Lerp(c, Color.white, 0.6f);
                    // Lava glow at the volcano's foot and in the crater.
                    if (k == Kind.Volcano && v < hFar && v > hFar - 0.05f && Mathf.Abs(u - 0.5f) < 0.1f) c = Color.Lerp(c, glow, 0.9f);
                    if (k == Kind.Volcano && v < 0.2f) c = Color.Lerp(c, glow, Mathf.Clamp01(0.2f - v) * 2.5f);
                    // Lantern windows on the rooftops.
                    if (k == Kind.Rooftops && v < hNear - 0.03f && Hash(Mathf.Floor(u * 40f) * 3.1f + Mathf.Floor(v * 40f)) > 0.9f) c = glow;
                    px[y * Size + x] = c;
                }
            }
            var t = new Texture2D(Size, Size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear, name = "Poster_" + k };
            t.SetPixels(px);
            t.Apply(false, true);
            return t;
        }

        /// <summary>Heights (0–1) of the far and near silhouettes at u.</summary>
        static void Silhouettes(Kind k, float u, out float far, out float near)
        {
            switch (k)
            {
                case Kind.SunsetHills:
                    far = 0.36f + 0.06f * Mathf.Sin(u * 7f + 1f) + 0.03f * Mathf.Sin(u * 17f);
                    near = 0.2f + 0.05f * Mathf.Sin(u * 5f + 2f);
                    break;
                case Kind.MoonNight:
                    far = 0.34f + 0.12f * (1f - Mathf.Abs(Mathf.Repeat(u * 2.2f, 1f) - 0.5f) * 2f);
                    near = 0.16f + 0.04f * Mathf.Sin(u * 9f);
                    break;
                case Kind.SakuraDusk:
                    far = 0.3f + 0.04f * Mathf.Sin(u * 6f);
                    near = 0.18f + 0.1f * Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 11f)), 0.4f); // blossom tree crowns
                    break;
                case Kind.OceanDay:
                    // The horizon with two soft distant islands.
                    far = 0.3f + 0.07f * Mathf.Exp(-Mathf.Pow((u - 0.14f) / 0.09f, 2f)) + 0.045f * Mathf.Exp(-Mathf.Pow((u - 0.86f) / 0.11f, 2f));
                    near = 0.12f + 0.01f * Mathf.Sin(u * 40f);
                    break;
                case Kind.Forest:
                    far = 0.4f + 0.08f * Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 14f)), 0.5f);
                    near = 0.22f + 0.14f * Mathf.Pow(Mathf.Abs(Mathf.Sin(u * 7f + 1f)), 0.6f);
                    break;
                case Kind.SnowPeaks:
                    far = 0.3f + 0.3f * (1f - Mathf.Abs(Mathf.Repeat(u * 1.6f + 0.2f, 1f) - 0.5f) * 2f);
                    near = 0.14f + 0.03f * Mathf.Sin(u * 8f);
                    break;
                case Kind.Volcano:
                    far = Mathf.Clamp(0.62f - Mathf.Abs(u - 0.5f) * 1.3f, 0.25f, 0.55f);
                    near = 0.15f + 0.04f * Mathf.Sin(u * 12f);
                    break;
                case Kind.Storm:
                    far = 0.3f + 0.08f * Mathf.Sin(u * 4f) + 0.03f * Mathf.Sin(u * 19f);
                    near = 0.16f + 0.03f * Mathf.Sin(u * 7f);
                    break;
                default:
                    // Rooftops: stepped buildings with pitched roofs.
                    float cell = Mathf.Floor(u * 7f);
                    float fu = u * 7f - cell;
                    float bh = 0.22f + Hash(cell + 3f) * 0.12f;
                    near = bh + 0.07f * (1f - Mathf.Abs(fu - 0.5f) * 2f);
                    far = 0.32f + Hash(Mathf.Floor(u * 12f) + 9f) * 0.1f;
                    break;
            }
        }
    }
}
