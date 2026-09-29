using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Promotional banner art, painted live in layers: an element-themed gradient, rotating speed rays and a
    /// pulsing glow behind the featured character, element particles (embers, bubbles, lightning, wisps,
    /// sparkles, leaves), the character art large with a coloured halo, a diagonal title slash, bold event title,
    /// NEW / LIMITED labels, rarity stars and a live countdown. Fire is red and orange, water blue and cyan,
    /// thunder purple and yellow, dark black and purple, light white and gold.
    /// </summary>
    public partial class UIManager
    {
        /// <summary>Top-left of the GUI group the banner is drawn in (rotations need top-level coordinates).</summary>
        Vector2 promoOrigin;

        void PromoRotate(float angle, Vector2 pivot) { RotateGui(angle, pivot + promoOrigin); }

        struct PromoTheme
        {
            public Color top, bottom, glow, particle;
            public int fx; // 0 embers, 1 bubbles, 2 bolts, 3 wisps, 4 sparkles, 5 leaves
        }

        static PromoTheme ThemeFor(Element e)
        {
            switch (e)
            {
                case Element.Flame: return new PromoTheme { top = new Color(0.32f, 0.02f, 0.04f), bottom = new Color(1f, 0.42f, 0.06f), glow = new Color(1f, 0.55f, 0.15f), particle = new Color(1f, 0.75f, 0.25f), fx = 0 };
                case Element.Water: return new PromoTheme { top = new Color(0.02f, 0.07f, 0.3f), bottom = new Color(0.08f, 0.66f, 0.95f), glow = new Color(0.3f, 0.85f, 1f), particle = new Color(0.8f, 0.97f, 1f), fx = 1 };
                case Element.Thunder: return new PromoTheme { top = new Color(0.18f, 0.04f, 0.38f), bottom = new Color(0.95f, 0.78f, 0.15f), glow = new Color(1f, 0.9f, 0.35f), particle = new Color(1f, 0.95f, 0.5f), fx = 2 };
                case Element.Dark: return new PromoTheme { top = new Color(0.02f, 0f, 0.05f), bottom = new Color(0.42f, 0.08f, 0.58f), glow = new Color(0.7f, 0.3f, 1f), particle = new Color(0.8f, 0.5f, 1f), fx = 3 };
                case Element.Earth: return new PromoTheme { top = new Color(0.12f, 0.1f, 0.03f), bottom = new Color(0.55f, 0.72f, 0.2f), glow = new Color(0.75f, 0.95f, 0.35f), particle = new Color(0.8f, 0.95f, 0.5f), fx = 5 };
                case Element.Light: return new PromoTheme { top = new Color(0.55f, 0.36f, 0.1f), bottom = new Color(1f, 0.95f, 0.78f), glow = new Color(1f, 0.92f, 0.6f), particle = Color.white, fx = 4 };
                default: return new PromoTheme { top = new Color(0.03f, 0.2f, 0.12f), bottom = new Color(0.45f, 0.88f, 0.3f), glow = new Color(0.6f, 1f, 0.5f), particle = new Color(0.8f, 1f, 0.6f), fx = 5 };
            }
        }

        /// <summary>Time left until the weekly rotation (Sunday night), as "3d 04h" / "04h 12m".</summary>
        static string Countdown()
        {
            var now = System.DateTime.Now;
            int days = ((int)System.DayOfWeek.Sunday - (int)now.DayOfWeek + 7) % 7;
            var end = now.Date.AddDays(days).AddHours(23).AddMinutes(59);
            var left = end - now;
            if (left.TotalDays >= 1) return (int)left.TotalDays + "d " + left.Hours.ToString("00") + "h";
            return left.Hours.ToString("00") + "h " + left.Minutes.ToString("00") + "m";
        }

        /// <summary>The layered background: gradient, rays, glow and particles, focused on focus (the character).</summary>
        void PromoBackdrop(Rect r, PromoTheme th, Vector2 focus, float seed, float radius)
        {
            // Gradient (diagonal feel: darker top-left, bright bottom-right).
            const int bands = 14;
            for (int i = 0; i < bands; i++)
            {
                float f = i / (float)(bands - 1);
                Color c = Color.Lerp(th.top, th.bottom, Mathf.Pow(f, 1.3f));
                UIStyles.Rect(new Rect(r.x, r.y + r.height * i / bands, r.width, r.height / bands + 1f), c);
            }
            // Rotating speed rays from the character.
            var saved = GUI.matrix;
            float t = Time.unscaledTime;
            for (int k = 0; k < 14; k++)
            {
                GUI.matrix = saved;
                PromoRotate(k * (360f / 14f) + t * 10f + seed * 20f, focus);
                UIStyles.Rect(new Rect(focus.x, focus.y - radius * 0.07f, radius * 2.2f, radius * 0.14f), new Color(th.glow.r, th.glow.g, th.glow.b, k % 2 == 0 ? 0.12f : 0.06f));
            }
            GUI.matrix = saved;
            // Glow.
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2.2f + seed);
            for (int k = 5; k >= 1; k--) UIStyles.CircleTex(focus, radius * (0.3f + k * 0.14f + pulse * 0.03f), new Color(th.glow.r, th.glow.g, th.glow.b, 0.07f));
            UIStyles.CircleTex(focus, radius * 0.45f, new Color(1f, 1f, 1f, 0.06f + 0.04f * pulse));
            PromoParticles(r, th, seed);
        }

        void PromoParticles(Rect r, PromoTheme th, float seed)
        {
            var rng = new System.Random(Mathf.RoundToInt(seed * 100f) + 11);
            float t = Time.unscaledTime;
            int n = th.fx == 2 ? 6 : 34;
            for (int i = 0; i < n; i++)
            {
                float x0 = (float)rng.NextDouble(), sp = 0.05f + (float)rng.NextDouble() * 0.12f, ph = (float)rng.NextDouble();
                float size = 2f + (float)rng.NextDouble() * 5f;
                switch (th.fx)
                {
                    case 0: // embers rising and flickering
                    case 3: // wisps drifting up
                    case 5: // leaves drifting
                    {
                        float y = 1f - Mathf.Repeat(ph + t * sp, 1f);
                        float x = x0 + Mathf.Sin(t * (th.fx == 5 ? 1.2f : 2f) + i) * 0.02f;
                        float a = Mathf.Sin(y * Mathf.PI) * (th.fx == 3 ? 0.35f : 0.85f) * (0.6f + 0.4f * Mathf.Sin(t * 9f + i));
                        var p = new Vector2(r.x + x * r.width, r.y + y * r.height);
                        if (th.fx == 3) UIStyles.CircleTex(p, size * 3f, new Color(th.particle.r, th.particle.g, th.particle.b, a * 0.4f));
                        else if (th.fx == 5) Round(new Rect(p.x, p.y, size * 2.2f, size), new Color(th.particle.r, th.particle.g, th.particle.b, a), size * 0.5f);
                        else UIStyles.CircleTex(p, size, new Color(th.particle.r, th.particle.g, th.particle.b, a));
                        break;
                    }
                    case 1: // bubbles
                    {
                        float y = 1f - Mathf.Repeat(ph + t * sp * 0.8f, 1f);
                        var p = new Vector2(r.x + (x0 + Mathf.Sin(t + i) * 0.015f) * r.width, r.y + y * r.height);
                        UIStyles.CircleTex(p, size * 1.6f, new Color(1f, 1f, 1f, 0.35f * Mathf.Sin(y * Mathf.PI)), UIStyles.Ring);
                        break;
                    }
                    case 2: // lightning bolts that flash now and then
                    {
                        float cyc = Mathf.Repeat(t * 0.7f + ph * 3f, 3f);
                        if (cyc > 0.18f) break;
                        var saved = GUI.matrix;
                        Vector2 a = new Vector2(r.x + x0 * r.width, r.y);
                        float segs = 5f, seg = r.height / segs;
                        for (int s = 0; s < 5; s++)
                        {
                            float off = ((s * 37 + i * 13) % 7 - 3) * 8f;
                            Vector2 b = a + new Vector2(off, seg);
                            float ang = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                            GUI.matrix = saved;
                            PromoRotate(ang, a);
                            UIStyles.Rect(new Rect(a.x, a.y - 2.5f, (b - a).magnitude, 5f), new Color(1f, 0.97f, 0.7f, 0.9f));
                            a = b;
                        }
                        GUI.matrix = saved;
                        break;
                    }
                    default: // sparkles
                    {
                        float tw = Mathf.Max(0f, Mathf.Sin(t * (2f + sp * 20f) + i));
                        var p = new Vector2(r.x + x0 * r.width, r.y + ph * r.height);
                        UIStyles.Outlined(new Rect(p.x - 10f, p.y - 10f, 20f, 20f), "✦", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(10 + size * 2f)), new Color(1f, 1f, 0.9f, tw), 0f);
                        break;
                    }
                }
            }
        }

        /// <summary>A promo theme from any accent colour (events): near-black top, the accent burning at the bottom.</summary>
        static PromoTheme ThemeForColor(Color a)
        {
            float h, sat, v;
            Color.RGBToHSV(a, out h, out sat, out v);
            int fx = h < 0.07f || h > 0.93f ? 0 : h < 0.17f ? 0 : h < 0.45f ? 5 : h < 0.62f ? 4 : 3;
            return new PromoTheme
            {
                top = new Color(a.r * 0.08f, a.g * 0.05f, a.b * 0.1f + 0.02f), bottom = Color.Lerp(a, Color.black, 0.25f),
                glow = Color.Lerp(a, Color.white, 0.25f), particle = Color.Lerp(a, Color.white, 0.5f), fx = fx
            };
        }

        /// <summary>
        /// Makes a banner look dangerous: a dark vignette, three claw slashes torn across it, jagged cracks glowing
        /// in the accent colour, and a slow red pulse around the rim. Draw inside the banner's GUI group.
        /// </summary>
        void DangerOverlay(Rect r, Color accent, float seed, float strength = 1f)
        {
            float t = Time.unscaledTime;
            var saved = GUI.matrix;
            // Vignette.
            for (int i = 0; i < 6; i++)
            {
                float a = 0.1f * strength;
                float e = r.height * (0.05f + i * 0.04f);
                UIStyles.Rect(new Rect(r.x, r.y, r.width, e), new Color(0f, 0f, 0f, a));
                UIStyles.Rect(new Rect(r.x, r.yMax - e, r.width, e), new Color(0f, 0f, 0f, a));
                UIStyles.Rect(new Rect(r.x, r.y, e, r.height), new Color(0f, 0f, 0f, a));
            }
            // Claw slashes: three tapered tears, dark inside with a hot edge.
            var rng = new System.Random(Mathf.RoundToInt(seed * 131f) + 7);
            float cx = r.x + r.width * (0.55f + (float)rng.NextDouble() * 0.2f), cy = r.y + r.height * 0.5f;
            float ang = -58f + (float)rng.NextDouble() * 16f;
            float len = r.height * 1.25f, shimmer = 0.6f + 0.4f * Mathf.Sin(t * 3f + seed);
            for (int k = 0; k < 3; k++)
            {
                GUI.matrix = saved;
                var c = new Vector2(cx + (k - 1) * r.height * 0.13f, cy + (k - 1) * 4f);
                PromoRotate(ang, c);
                float w = (k == 1 ? 11f : 8f) * Mathf.Max(0.6f, r.height / 220f);
                for (int s = 0; s < 3; s++)
                {
                    // Tapered: three stacked bars, shorter and thicker toward the middle.
                    float f = 1f - s * 0.28f, ww = w * (0.5f + s * 0.35f);
                    UIStyles.Rect(new Rect(c.x - len * 0.5f * f, c.y - ww * 0.5f, len * f, ww), new Color(0.05f, 0f, 0.02f, 0.55f * strength));
                }
                UIStyles.Rect(new Rect(c.x - len * 0.42f, c.y - w * 0.5f - 2f, len * 0.84f, 2f), new Color(accent.r, accent.g * 0.6f, accent.b * 0.6f, 0.8f * shimmer * strength));
            }
            GUI.matrix = saved;
            // Cracks from the lower-left corner.
            for (int c = 0; c < 3; c++)
            {
                Vector2 a = new Vector2(r.x + (float)rng.NextDouble() * r.width * 0.25f, r.yMax);
                for (int sgm = 0; sgm < 5; sgm++)
                {
                    Vector2 b = a + new Vector2(8f + (float)rng.NextDouble() * 26f, -(10f + (float)rng.NextDouble() * 22f)) * Mathf.Max(0.7f, r.height / 220f);
                    float an = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
                    GUI.matrix = saved;
                    PromoRotate(an, a);
                    float glow = 0.35f + 0.35f * Mathf.Sin(t * 2.5f + c + sgm * 0.7f);
                    UIStyles.Rect(new Rect(a.x, a.y - 1.5f, (b - a).magnitude, 3f), new Color(Mathf.Min(1f, accent.r + 0.3f), accent.g * 0.7f, accent.b * 0.5f, glow * strength));
                    a = b;
                }
            }
            GUI.matrix = saved;
            // Slow red pulse around the rim.
            float pulse = 0.5f + 0.5f * Mathf.Sin(t * 2f + seed);
            RoundFrame(new Rect(r.x + 1f, r.y + 1f, r.width - 2f, r.height - 2f), new Color(1f, 0.15f, 0.12f, (0.25f + 0.35f * pulse) * strength), 3f, 14f);
        }

        /// <summary>A chunky label chip (NEW EVENT, LIMITED SUMMON...).</summary>
        void PromoChip(Rect r, string text, Color c)
        {
            FlatBtnLook(r, c);
            UIStyles.Outlined(new Rect(r.x, r.y - 2f, r.width, r.height), text, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(r.height * 0.46f)), Color.white, 2f);
        }

        /// <summary>
        /// A full promo banner (home carousel): themed backdrop, character art on the right with a halo, a slash
        /// behind the title, chips, stars and countdown.
        /// </summary>
        void PromoBanner(Rect lr, Vector2 groupOrigin, string chip, string headline, string title, string sub, Texture art, System.Action<Rect> picture, Element element, int stars, bool limited, float seed)
        {
            var th = ThemeFor(element);
            promoOrigin = groupOrigin;
            float t = promoAdT;
            float X = lr.x, Y = lr.y;
            // A little shake as the title slams in.
            if (t > 0.3f && t < 0.46f) { X += Random.Range(-3f, 3f); Y += Random.Range(-2f, 2f); }
            Vector2 focus = new Vector2(X + lr.width * 0.74f, Y + lr.height * 0.55f);
            PromoBackdrop(lr, th, focus, seed, lr.height);
            AdSpeedLines(new Rect(X + lr.width * 0.35f, Y, lr.width * 0.65f, lr.height), t, th.glow);
            if (art != null)
            {
                float bob = Mathf.Sin(Time.unscaledTime * 1.6f + seed) * 4f;
                // Zooms in from the side, then keeps pushing in slowly (a camera move, like an ad).
                float ei = AdEase(t / 0.45f);
                float zoom = Mathf.Lerp(1.35f, 1f, ei) * (1f + 0.06f * Mathf.Clamp01((t - 0.45f) / 5f));
                float w0 = lr.width * 0.58f, h0 = lr.height * 1.3f;
                var ar = new Rect(X + lr.width * 0.46f + (1f - ei) * lr.width * 0.35f - w0 * (zoom - 1f) * 0.5f, Y - lr.height * 0.12f + bob - h0 * (zoom - 1f) * 0.5f, w0 * zoom, h0 * zoom);
                // Halo: the art drawn tinted and slightly larger behind itself.
                var old = GUI.color;
                GUI.color = new Color(th.glow.r, th.glow.g, th.glow.b, 0.55f * ei);
                GUI.DrawTexture(Grow(ar, 8f), art, ScaleMode.ScaleAndCrop, true);
                GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(ei * 1.5f));
                GUI.DrawTexture(ar, art, ScaleMode.ScaleAndCrop, true);
                GUI.color = old;
            }
            else if (picture != null) picture(new Rect(X + lr.width * 0.56f + (1f - AdEase(t / 0.45f)) * 200f, Y + 16f, lr.width * 0.4f, lr.height - 32f));
            // Title slash: a dark diagonal band for readability.
            var saved = GUI.matrix;
            PromoRotate(-6f, new Vector2(X + lr.width * 0.3f, Y + lr.height * 0.55f));
            UIStyles.Rect(new Rect(X - 40f, Y + lr.height * 0.3f, lr.width * 0.68f, lr.height * 0.5f), new Color(0f, 0f, 0f, 0.42f));
            UIStyles.Rect(new Rect(X - 40f, Y + lr.height * 0.3f - 5f, lr.width * 0.68f, 5f), th.glow);
            GUI.matrix = saved;
            // Chip pops, headline slides, title slams in from the left with an overshoot and a size punch.
            float cp = AdBack(t / 0.25f);
            float cw = Mathf.Max(150f, chip.Length * 12f + 40f);
            if (cp > 0.05f) PromoChip(new Rect(X + 16f, Y + 14f + (1f - cp) * 10f, cw * cp, 36f), cp > 0.6f ? chip : "", limited ? new Color(0.95f, 0.18f, 0.25f) : new Color(0.2f, 0.55f, 1f));
            if (!string.IsNullOrEmpty(headline))
                UIStyles.Outlined(new Rect(X + 18f - (1f - AdEase((t - 0.1f) / 0.3f)) * 160f, Y + 56f, lr.width * 0.62f, 30f), headline, UIStyles.Sized(UIStyles.Body, 20), th.glow, 2f);
            float tk = AdBack((t - 0.15f) / 0.35f);
            float punch = t > 0.3f && t < 0.5f ? Mathf.Sin((t - 0.3f) / 0.2f * Mathf.PI) * 0.22f : 0f;
            UIStyles.Outlined(new Rect(X + 18f - (1f - tk) * lr.width * 0.6f, Y + lr.height * 0.34f, lr.width * 0.6f, 84f), title,
                new GUIStyle(UIStyles.Sized(UIStyles.H1, Mathf.RoundToInt(34f * (1f + punch)))) { wordWrap = true }, Color.white, 4f);
            AdSparkBurst(new Vector2(X + lr.width * 0.3f, Y + lr.height * 0.45f), t, 0.35f, th.glow, lr.width * 0.25f);
            if (stars > 0)
            {
                string st = "";
                for (int i = 0; i < stars; i++) st += "★";
                UIStyles.Outlined(new Rect(X + 20f, Y + lr.height * 0.34f + 80f, 300f, 30f), st, UIStyles.Sized(UIStyles.Body, 22), new Color(1f, 0.85f, 0.3f), 2f);
            }
            GUI.Label(new Rect(X + 20f, Y + lr.height - 44f, lr.width * 0.55f, 40f), "<color=#F2EEFF>" + sub + "</color>", new GUIStyle(UIStyles.Sized(UIStyles.Small, 15)) { wordWrap = true });
            if (limited)
            {
                var cd = new Rect(X + lr.width - 170f, Y + lr.height - 42f, 156f, 32f);
                Round(cd, new Color(0f, 0f, 0f, 0.55f), 12f);
                UIStyles.Outlined(cd, "⏱ " + Countdown(), UIStyles.Sized(UIStyles.Center, 17), Color.white, 1.5f);
            }
            if (limited) DangerOverlay(lr, th.glow, seed, 0.8f);
            // The rest of the ad: a stamp thumping down, the call to action, light sweeps and the opening flash.
            if (limited) AdStamp(new Rect(X + lr.width * 0.42f, Y + 12f, 116f, 40f), "LIMITED!", new Color(1f, 0.85f, 0.3f), 0.5f, t, -8f);
            AdCta(new Rect(X + lr.width - 212f, Y + lr.height - 86f, 196f, 38f), "TAP TO PLAY", limited ? new Color(0.95f, 0.2f, 0.28f) : new Color(0.2f, 0.6f, 1f), t, 0.75f);
            AdSweep(lr, 3f, seed);
            if (t < 0.15f) UIStyles.Rect(lr, new Color(1f, 1f, 1f, 0.6f * (1f - t / 0.15f)));
            RoundFrame(new Rect(X + 2f, Y + 2f, lr.width - 4f, lr.height - 4f), new Color(1f, 1f, 1f, 0.25f), 2f, 14f);
            promoAdT = 99f;
        }
    }
}
