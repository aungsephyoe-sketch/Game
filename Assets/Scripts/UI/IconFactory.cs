using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Crisp white icons drawn from signed-distance shapes at startup (tint them with GUI.color):
    /// sword, fist, arrow, staff, shield, dodge, lock-on target, the six element symbols and the
    /// skill shapes (dash, spin, wave, burst, multi-slash, heal).
    /// </summary>
    public static class IconFactory
    {
        const int Size = 128;
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        public static Texture2D Get(string name)
        {
            Texture2D t;
            if (cache.TryGetValue(name, out t) && t != null) return t;
            System.Func<Vector2, float> sd = Shape(name);
            t = Render(sd);
            t.name = "Icon_" + name;
            cache[name] = t;
            return t;
        }

        public static string ForElement(Element e)
        {
            switch (e)
            {
                case Element.Water: return "drop";
                case Element.Flame: return "flame";
                case Element.Thunder: return "bolt";
                case Element.Beast: return "claw";
                case Element.Light: return "sun";
                case Element.Earth: return "leaf";
                default: return "moon";
            }
        }

        public static string ForAttack(CombatStyle s, WeaponKind w)
        {
            if (s == CombatStyle.Brawler || w == WeaponKind.Fists) return "fist";
            if (s == CombatStyle.Healer) return "staff";
            if (s == CombatStyle.Ranged) return w == WeaponKind.Bow ? "arrow" : "orb";
            return "sword";
        }

        public static string ForSkill(AbilityShape shape)
        {
            switch (shape)
            {
                case AbilityShape.Dash: return "dash";
                case AbilityShape.Spin: return "spin";
                case AbilityShape.Wave: return "wave";
                case AbilityShape.Burst: return "burst";
                case AbilityShape.MultiSlash: return "slashes";
                default: return "heal";
            }
        }

        static Texture2D Render(System.Func<Vector2, float> sd)
        {
            var tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Trilinear;
            tex.anisoLevel = 4;
            var px = new Color32[Size * Size];
            float pixel = 2f / Size;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    // 2x2 supersampling for smooth edges.
                    float cov = 0f;
                    for (int sy = 0; sy < 2; sy++)
                        for (int sx = 0; sx < 2; sx++)
                        {
                            var p = new Vector2((x + 0.25f + sx * 0.5f) / Size * 2f - 1f, (y + 0.25f + sy * 0.5f) / Size * 2f - 1f);
                            float d = sd(p);
                            cov += Mathf.Clamp01(0.5f - d / pixel);
                        }
                    byte a = (byte)Mathf.RoundToInt(cov * 0.25f * 255f);
                    px[y * Size + x] = new Color32(255, 255, 255, a);
                }
            tex.SetPixels32(px);
            tex.Apply(true, true);
            return tex;
        }

        // ------------------------------------------------------------------ SDF primitives

        static float Circle(Vector2 p, Vector2 c, float r) { return (p - c).magnitude - r; }

        static float Seg(Vector2 p, Vector2 a, Vector2 b, float r)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, ba.sqrMagnitude));
            return (pa - ba * h).magnitude - r;
        }

        /// <summary>Tapered segment: radius ra at a, rb at b.</summary>
        static float Taper(Vector2 p, Vector2 a, Vector2 b, float ra, float rb)
        {
            Vector2 pa = p - a, ba = b - a;
            float h = Mathf.Clamp01(Vector2.Dot(pa, ba) / Mathf.Max(1e-6f, ba.sqrMagnitude));
            return (pa - ba * h).magnitude - Mathf.Lerp(ra, rb, h);
        }

        static float Box(Vector2 p, Vector2 c, Vector2 half, float round = 0f)
        {
            Vector2 d = new Vector2(Mathf.Abs(p.x - c.x), Mathf.Abs(p.y - c.y)) - half + Vector2.one * round;
            return new Vector2(Mathf.Max(d.x, 0f), Mathf.Max(d.y, 0f)).magnitude + Mathf.Min(Mathf.Max(d.x, d.y), 0f) - round;
        }

        static float Poly(Vector2 p, Vector2[] v)
        {
            float d = (p - v[0]).sqrMagnitude;
            float s = 1f;
            for (int i = 0, j = v.Length - 1; i < v.Length; j = i, i++)
            {
                Vector2 e = v[j] - v[i], w = p - v[i];
                Vector2 b = w - e * Mathf.Clamp01(Vector2.Dot(w, e) / Mathf.Max(1e-6f, e.sqrMagnitude));
                d = Mathf.Min(d, b.sqrMagnitude);
                bool c1 = p.y >= v[i].y, c2 = p.y < v[j].y, c3 = e.x * w.y > e.y * w.x;
                if ((c1 && c2 && c3) || (!c1 && !c2 && !c3)) s = -s;
            }
            return s * Mathf.Sqrt(d);
        }

        static float Ring(Vector2 p, Vector2 c, float r, float w) { return Mathf.Abs((p - c).magnitude - r) - w; }

        /// <summary>Ring limited to an arc between two angles (degrees, counter-clockwise from +x).</summary>
        static float Arc(Vector2 p, Vector2 c, float r, float w, float fromDeg, float toDeg)
        {
            Vector2 d = p - c;
            float a = Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg;
            float span = Mathf.Repeat(toDeg - fromDeg, 360f);
            float rel = Mathf.Repeat(a - fromDeg, 360f);
            if (rel <= span) return Mathf.Abs(d.magnitude - r) - w;
            Vector2 e0 = c + new Vector2(Mathf.Cos(fromDeg * Mathf.Deg2Rad), Mathf.Sin(fromDeg * Mathf.Deg2Rad)) * r;
            Vector2 e1 = c + new Vector2(Mathf.Cos(toDeg * Mathf.Deg2Rad), Mathf.Sin(toDeg * Mathf.Deg2Rad)) * r;
            return Mathf.Min((p - e0).magnitude, (p - e1).magnitude) - w;
        }

        static Vector2 Rot(Vector2 p, float deg)
        {
            float a = deg * Mathf.Deg2Rad, c = Mathf.Cos(a), s = Mathf.Sin(a);
            return new Vector2(c * p.x - s * p.y, s * p.x + c * p.y);
        }

        static Vector2 V(float x, float y) { return new Vector2(x, y); }

        // ------------------------------------------------------------------ Icons

        static System.Func<Vector2, float> Shape(string name)
        {
            switch (name)
            {
                case "sword":
                    return p =>
                    {
                        var q = Rot(p, 45f);
                        float blade = Poly(q, new[] { V(0f, 0.98f), V(0.13f, 0.72f), V(0.13f, -0.3f), V(-0.13f, -0.3f), V(-0.13f, 0.72f) });
                        float guard = Box(q, V(0f, -0.38f), V(0.4f, 0.07f), 0.04f);
                        float grip = Box(q, V(0f, -0.62f), V(0.075f, 0.2f), 0.03f);
                        float pommel = Circle(q, V(0f, -0.88f), 0.1f);
                        float fuller = Box(q, V(0f, 0.2f), V(0.025f, 0.42f));
                        return Mathf.Min(Mathf.Min(Mathf.Max(blade, -fuller), guard), Mathf.Min(grip, pommel));
                    };
                case "fist":
                    return p =>
                    {
                        float palm = Box(p, V(0.05f, -0.3f), V(0.48f, 0.38f), 0.22f);
                        float f = 1e9f;
                        for (int i = 0; i < 4; i++) f = Mathf.Min(f, Box(p, V(-0.3f + i * 0.245f, 0.3f), V(0.105f, 0.26f), 0.1f));
                        float thumb = Box(Rot(p - V(-0.12f, -0.02f), 12f), Vector2.zero, V(0.4f, 0.11f), 0.11f);
                        float wrist = Box(p, V(0.05f, -0.82f), V(0.36f, 0.1f), 0.04f);
                        float gapLine = Box(p, V(0.05f, -0.1f), V(0.6f, 0.03f));
                        return Mathf.Min(Mathf.Min(Mathf.Max(Mathf.Min(palm, f), -gapLine), thumb), wrist);
                    };
                case "arrow":
                    return p =>
                    {
                        var q = Rot(p, 45f);
                        float shaft = Box(q, V(0f, -0.05f), V(0.05f, 0.7f));
                        float head = Poly(q, new[] { V(0f, 0.98f), V(0.24f, 0.58f), V(-0.24f, 0.58f) });
                        float f1 = Poly(q, new[] { V(0.05f, -0.5f), V(0.26f, -0.8f), V(0.26f, -0.98f), V(0.05f, -0.72f) });
                        float f2 = Poly(q, new[] { V(-0.05f, -0.5f), V(-0.26f, -0.8f), V(-0.26f, -0.98f), V(-0.05f, -0.72f) });
                        return Mathf.Min(Mathf.Min(shaft, head), Mathf.Min(f1, f2));
                    };
                case "orb":
                    return p => Mathf.Min(Circle(p, V(0.15f, 0.15f), 0.42f), Mathf.Min(Taper(p, V(-0.2f, -0.2f), V(-0.85f, -0.85f), 0.18f, 0.02f), Taper(p, V(0.3f, -0.3f), V(-0.35f, -0.9f), 0.1f, 0.01f)));
                case "staff":
                    return p =>
                    {
                        float pole = Seg(p, V(-0.7f, -0.9f), V(0.25f, 0.3f), 0.07f);
                        float gem = Circle(p, V(0.42f, 0.5f), 0.26f);
                        float halo = Ring(p, V(0.42f, 0.5f), 0.42f, 0.04f);
                        float plusV = Box(p, V(-0.45f, 0.5f), V(0.07f, 0.25f), 0.03f);
                        float plusH = Box(p, V(-0.45f, 0.5f), V(0.25f, 0.07f), 0.03f);
                        return Mathf.Min(Mathf.Min(pole, gem), Mathf.Min(halo, Mathf.Min(plusV, plusH)));
                    };
                case "shield":
                    return p =>
                    {
                        var outer = new[] { V(-0.72f, 0.78f), V(0.72f, 0.78f), V(0.7f, 0.05f), V(0f, -0.92f), V(-0.7f, 0.05f) };
                        var inner = new[] { V(-0.52f, 0.6f), V(0.52f, 0.6f), V(0.5f, 0.08f), V(0f, -0.66f), V(-0.5f, 0.08f) };
                        float rim = Mathf.Max(Poly(p, outer), -Poly(p, inner));
                        float cross = Mathf.Min(Box(p, V(0f, 0.02f), V(0.07f, 0.5f)), Box(p, V(0f, 0.25f), V(0.36f, 0.07f)));
                        return Mathf.Min(rim, Mathf.Max(cross, Poly(p, inner)));
                    };
                case "dodge":
                    return p =>
                    {
                        float chev1 = Mathf.Min(Seg(p, V(0.05f, 0.55f), V(0.55f, 0f), 0.1f), Seg(p, V(0.55f, 0f), V(0.05f, -0.55f), 0.1f));
                        float chev2 = Mathf.Min(Seg(p, V(-0.35f, 0.4f), V(0.05f, 0f), 0.08f), Seg(p, V(0.05f, 0f), V(-0.35f, -0.4f), 0.08f));
                        float l1 = Taper(p, V(-0.95f, 0.35f), V(-0.5f, 0.35f), 0.01f, 0.05f);
                        float l2 = Taper(p, V(-0.95f, -0.35f), V(-0.55f, -0.35f), 0.01f, 0.05f);
                        return Mathf.Min(Mathf.Min(chev1, chev2), Mathf.Min(l1, l2));
                    };
                case "target":
                    return p =>
                    {
                        float ring = Ring(p, Vector2.zero, 0.55f, 0.07f);
                        float ticks = Mathf.Min(Mathf.Min(Box(p, V(0f, 0.8f), V(0.06f, 0.18f)), Box(p, V(0f, -0.8f), V(0.06f, 0.18f))),
                            Mathf.Min(Box(p, V(0.8f, 0f), V(0.18f, 0.06f)), Box(p, V(-0.8f, 0f), V(0.18f, 0.06f))));
                        return Mathf.Min(Mathf.Min(ring, ticks), Circle(p, Vector2.zero, 0.14f));
                    };
                case "leaf":
                    return p =>
                    {
                        // A rounded leaf with a stem and a vein.
                        float a = Circle(p, V(-0.32f, -0.05f), 0.72f), b = Circle(p, V(0.32f, 0.05f), 0.72f);
                        float leaf = Mathf.Max(a, b);
                        float stem = Seg(p, V(-0.35f, -0.85f), V(0.35f, 0.75f), 0.07f);
                        float vein = Seg(p, V(-0.2f, -0.45f), V(0.25f, 0.55f), 0.035f);
                        return Mathf.Min(Mathf.Max(leaf, -vein), stem);
                    };
                case "drop":
                    return p =>
                    {
                        float body = Circle(p, V(0f, -0.25f), 0.52f);
                        float tip = Poly(p, new[] { V(0f, 0.95f), V(0.46f, -0.02f), V(-0.46f, -0.02f) });
                        float shine = Arc(p, V(0f, -0.25f), 0.32f, 0.06f, 190f, 250f);
                        return Mathf.Max(Mathf.Min(body, tip), -shine);
                    };
                case "flame":
                    return p =>
                    {
                        float body = Circle(p, V(0f, -0.38f), 0.52f);
                        float tip = Poly(p, new[] { V(0.12f, 0.98f), V(0.52f, -0.3f), V(-0.52f, -0.3f), V(-0.2f, 0.2f) });
                        float side = Poly(p, new[] { V(-0.52f, 0.5f), V(-0.12f, -0.3f), V(-0.52f, -0.3f) });
                        float side2 = Poly(p, new[] { V(0.55f, 0.28f), V(0.52f, -0.3f), V(0.2f, -0.2f) });
                        float inner = Mathf.Min(Circle(p, V(0.02f, -0.5f), 0.22f), Poly(p, new[] { V(0.05f, 0.18f), V(0.22f, -0.5f), V(-0.2f, -0.5f) }));
                        return Mathf.Max(Mathf.Min(Mathf.Min(body, tip), Mathf.Min(side, side2)), -inner);
                    };
                case "bolt":
                    return p => Poly(p, new[] { V(0.22f, 0.98f), V(-0.52f, -0.08f), V(-0.02f, -0.08f), V(-0.24f, -0.98f), V(0.55f, 0.2f), V(0.05f, 0.2f) });
                case "claw":
                    return p =>
                    {
                        float d = 1e9f;
                        for (int i = 0; i < 3; i++)
                        {
                            float o = (i - 1) * 0.36f;
                            d = Mathf.Min(d, Taper(p, V(-0.45f + o, 0.8f), V(0.35f + o, -0.85f), 0.02f, 0.11f));
                        }
                        return d;
                    };
                case "sun":
                    return p =>
                    {
                        float d = Circle(p, Vector2.zero, 0.36f);
                        for (int i = 0; i < 8; i++)
                        {
                            Vector2 dir = Rot(V(1f, 0f), i * 45f);
                            d = Mathf.Min(d, Taper(p, dir * 0.52f, dir * 0.92f, 0.1f, 0.02f));
                        }
                        return d;
                    };
                case "moon":
                    return p => Mathf.Max(Circle(p, V(-0.05f, 0f), 0.78f), -Circle(p, V(0.32f, 0.22f), 0.64f));
                case "dash":
                    return p =>
                    {
                        float head = Poly(p, new[] { V(0.95f, 0f), V(0.35f, 0.5f), V(0.35f, -0.5f) });
                        float body = Box(p, V(-0.05f, 0f), V(0.42f, 0.13f));
                        float l1 = Taper(p, V(-0.95f, 0.45f), V(-0.2f, 0.45f), 0.01f, 0.06f);
                        float l2 = Taper(p, V(-0.95f, -0.45f), V(-0.2f, -0.45f), 0.01f, 0.06f);
                        return Mathf.Min(Mathf.Min(head, body), Mathf.Min(l1, l2));
                    };
                case "spin":
                    return p =>
                    {
                        float arc = Arc(p, Vector2.zero, 0.58f, 0.11f, 80f, 350f);
                        Vector2 radial = Rot(V(1f, 0f), 350f), tangent = Rot(V(0f, 1f), 350f);
                        Vector2 at = radial * 0.58f;
                        float head = Poly(p, new[] { at + radial * 0.3f, at + tangent * 0.38f, at - radial * 0.3f });
                        return Mathf.Min(Mathf.Min(arc, head), Circle(p, Vector2.zero, 0.15f));
                    };
                case "wave":
                    return p =>
                    {
                        var q = Rot(p, -20f);
                        return Mathf.Max(Circle(q, V(-0.1f, 0f), 0.85f), -Circle(q, V(-0.45f, 0f), 0.8f));
                    };
                case "burst":
                    return p =>
                    {
                        var pts = new Vector2[16];
                        for (int i = 0; i < 16; i++) pts[i] = Rot(V(0f, i % 2 == 0 ? 0.95f : 0.4f), i * 22.5f);
                        return Poly(p, pts);
                    };
                case "slashes":
                    return p =>
                    {
                        float d = 1e9f;
                        for (int i = 0; i < 3; i++)
                        {
                            float o = (i - 1) * 0.42f;
                            d = Mathf.Min(d, Taper(p, V(-0.75f + o, -0.75f + -o * 0.1f), V(0.75f + o, 0.75f - o * 0.1f), 0.02f, 0.09f));
                        }
                        return d;
                    };
                case "heal":
                    return p => Mathf.Min(Box(p, Vector2.zero, V(0.2f, 0.7f), 0.08f), Box(p, Vector2.zero, V(0.7f, 0.2f), 0.08f));
                case "person":
                    return p => Mathf.Min(Circle(p, V(0f, 0.42f), 0.3f), Mathf.Max(Circle(p, V(0f, -0.62f), 0.62f), -(p.y + 0.85f)));
                case "people":
                    return p =>
                    {
                        float a = Mathf.Min(Circle(p, V(-0.36f, 0.3f), 0.24f), Mathf.Max(Circle(p, V(-0.36f, -0.62f), 0.5f), -(p.y + 0.85f)));
                        float b = Mathf.Min(Circle(p, V(0.36f, 0.3f), 0.24f), Mathf.Max(Circle(p, V(0.36f, -0.62f), 0.5f), -(p.y + 0.85f)));
                        float gap = Mathf.Min(Circle(p, V(0.36f, 0.3f), 0.31f), Mathf.Max(Circle(p, V(0.36f, -0.62f), 0.57f), -(p.y + 0.95f)));
                        return Mathf.Min(Mathf.Max(a, -gap), b);
                    };
                case "group":
                    return p =>
                    {
                        float c = Mathf.Min(Circle(p, V(0f, 0.36f), 0.26f), Mathf.Max(Circle(p, V(0f, -0.6f), 0.55f), -(p.y + 0.85f)));
                        float cOut = Mathf.Min(Circle(p, V(0f, 0.36f), 0.33f), Mathf.Max(Circle(p, V(0f, -0.6f), 0.62f), -(p.y + 0.95f)));
                        float l = Mathf.Min(Circle(p, V(-0.55f, 0.22f), 0.2f), Mathf.Max(Circle(p, V(-0.55f, -0.6f), 0.42f), -(p.y + 0.85f)));
                        float r = Mathf.Min(Circle(p, V(0.55f, 0.22f), 0.2f), Mathf.Max(Circle(p, V(0.55f, -0.6f), 0.42f), -(p.y + 0.85f)));
                        return Mathf.Min(c, Mathf.Max(Mathf.Min(l, r), -cOut));
                    };
                case "bag":
                    return p =>
                    {
                        float body = Box(p, V(0f, -0.2f), V(0.62f, 0.52f), 0.18f);
                        float handle = Arc(p, V(0f, 0.32f), 0.3f, 0.08f, 0f, 180f);
                        float strap = Box(p, V(0f, -0.08f), V(0.64f, 0.05f));
                        float clasp = Box(p, V(0f, -0.08f), V(0.12f, 0.12f), 0.03f);
                        return Mathf.Min(Mathf.Min(Mathf.Max(body, -strap), handle), clasp);
                    };
                case "scroll":
                    return p =>
                    {
                        float sheet = Box(p, V(0f, 0f), V(0.46f, 0.66f), 0.04f);
                        float lines = 1e9f;
                        for (int i = 0; i < 4; i++) lines = Mathf.Min(lines, Box(p, V(0f, 0.36f - i * 0.22f), V(0.28f, 0.035f)));
                        float top = Box(p, V(0f, 0.72f), V(0.6f, 0.1f), 0.1f);
                        float bottom = Box(p, V(0f, -0.72f), V(0.6f, 0.1f), 0.1f);
                        return Mathf.Min(Mathf.Max(sheet, -lines), Mathf.Min(top, bottom));
                    };
                case "cart":
                    return p =>
                    {
                        float basket = Poly(p, new[] { V(-0.55f, 0.45f), V(0.8f, 0.45f), V(0.62f, -0.2f), V(-0.4f, -0.2f) });
                        float handle = Seg(p, V(-0.9f, 0.65f), V(-0.55f, 0.45f), 0.06f);
                        float rail = Seg(p, V(-0.4f, -0.2f), V(-0.45f, -0.4f), 0.05f);
                        float bar = Seg(p, V(-0.45f, -0.4f), V(0.62f, -0.4f), 0.05f);
                        float w1 = Circle(p, V(-0.3f, -0.68f), 0.13f), w2 = Circle(p, V(0.5f, -0.68f), 0.13f);
                        return Mathf.Min(Mathf.Min(Mathf.Min(basket, handle), Mathf.Min(rail, bar)), Mathf.Min(w1, w2));
                    };
                case "gear":
                    return p =>
                    {
                        float a = Mathf.Atan2(p.y, p.x);
                        float teeth = p.magnitude - (0.62f + 0.16f * Mathf.Clamp(Mathf.Cos(a * 8f) * 2.5f, -1f, 1f));
                        return Mathf.Max(teeth, -Circle(p, Vector2.zero, 0.26f));
                    };
                case "mail":
                    return p =>
                    {
                        float env = Box(p, Vector2.zero, V(0.75f, 0.5f), 0.08f);
                        float flap = Mathf.Min(Seg(p, V(-0.66f, 0.4f), V(0f, -0.08f), 0.06f), Seg(p, V(0f, -0.08f), V(0.66f, 0.4f), 0.06f));
                        return Mathf.Max(env, -flap);
                    };
                case "menu":
                    return p => Mathf.Min(Box(p, V(0f, 0.45f), V(0.62f, 0.09f), 0.09f), Mathf.Min(Box(p, V(0f, 0f), V(0.62f, 0.09f), 0.09f), Box(p, V(0f, -0.45f), V(0.62f, 0.09f), 0.09f)));
                case "swords":
                    return p =>
                    {
                        System.Func<Vector2, float> one = q =>
                        {
                            float blade = Poly(q, new[] { V(0f, 0.95f), V(0.09f, 0.78f), V(0.09f, -0.25f), V(-0.09f, -0.25f), V(-0.09f, 0.78f) });
                            float guard = Box(q, V(0f, -0.32f), V(0.26f, 0.05f), 0.03f);
                            float grip = Box(q, V(0f, -0.55f), V(0.055f, 0.18f), 0.02f);
                            return Mathf.Min(blade, Mathf.Min(guard, grip));
                        };
                        return Mathf.Min(one(Rot(p, 40f)), one(Rot(p, -40f)));
                    };
                case "heart":
                    return p => Mathf.Min(Mathf.Min(Circle(p, V(-0.3f, 0.25f), 0.36f), Circle(p, V(0.3f, 0.25f), 0.36f)),
                        Poly(p, new[] { V(-0.62f, 0.12f), V(0.62f, 0.12f), V(0f, -0.82f) }));
                case "lock":
                    return p => Mathf.Min(Box(p, V(0f, -0.25f), V(0.55f, 0.42f), 0.1f), Mathf.Max(Arc(p, V(0f, 0.2f), 0.34f, 0.1f, 0f, 180f), -Box(p, V(0f, -0.25f), V(0.55f, 0.42f))));
                case "all":
                    return p => Mathf.Min(Mathf.Min(Box(p, V(-0.4f, 0.4f), V(0.3f, 0.3f), 0.08f), Box(p, V(0.4f, 0.4f), V(0.3f, 0.3f), 0.08f)),
                        Mathf.Min(Box(p, V(-0.4f, -0.4f), V(0.3f, 0.3f), 0.08f), Box(p, V(0.4f, -0.4f), V(0.3f, 0.3f), 0.08f)));
                case "up":
                    return p => Mathf.Min(Mathf.Min(Seg(p, V(-0.6f, 0.05f), V(0f, 0.65f), 0.13f), Seg(p, V(0f, 0.65f), V(0.6f, 0.05f), 0.13f)),
                        Mathf.Min(Seg(p, V(-0.6f, -0.55f), V(0f, 0.05f), 0.13f), Seg(p, V(0f, 0.05f), V(0.6f, -0.55f), 0.13f)));
                case "plus":
                    return p => Mathf.Min(Box(p, Vector2.zero, V(0.14f, 0.62f), 0.06f), Box(p, Vector2.zero, V(0.62f, 0.14f), 0.06f));
                default:
                    return p => Circle(p, Vector2.zero, 0.6f);
            }
        }
    }
}
