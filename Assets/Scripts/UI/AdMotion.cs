using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// "Video ad" motion for promo slides and event pages. Each slide plays a short intro every time it appears —
    /// a white flash, the art zooming in from the side behind speed lines, the title slamming in with a little
    /// screen shake, a stamp thumping down, a burst of sparkles — then keeps living: a slow push-in on the art,
    /// light sweeps and a pulsing call-to-action. Nothing here uses rotated long shapes, so it clips to the slide.
    /// </summary>
    public partial class UIManager
    {
        /// <summary>Seconds since the promo being drawn appeared (99 = no intro, e.g. a slide passing by).</summary>
        float promoAdT = 99f;

        static float AdEase(float x) { x = Mathf.Clamp01(x); return 1f - (1f - x) * (1f - x) * (1f - x); }

        static float AdBack(float x)
        {
            x = Mathf.Clamp01(x);
            const float c1 = 1.70158f, c3 = c1 + 1f;
            float y = x - 1f;
            return 1f + c3 * y * y * y + c1 * y * y;
        }

        /// <summary>Horizontal speed streaks rushing across an area during an intro.</summary>
        void AdSpeedLines(Rect area, float t, Color c, float until = 0.55f)
        {
            if (t >= until) return;
            float fade = 1f - t / until;
            for (int i = 0; i < 12; i++)
            {
                float y = area.y + area.height * Mathf.Repeat(i * 0.377f, 1f);
                float len = area.width * (0.25f + 0.2f * Mathf.Repeat(i * 0.53f, 1f));
                float x = area.xMax - Mathf.Repeat(t * (1800f + i * 120f) + i * 97f, area.width + len);
                UIStyles.Rect(new Rect(x, y, len, 2f + (i % 3)), new Color(c.r, c.g, c.b, 0.55f * fade));
            }
        }

        /// <summary>A stamp that thumps down at t0: big and faint, then snapping to size with a little tilt.</summary>
        void AdStamp(Rect r, string text, Color c, float t0, float t, float tilt = -10f)
        {
            if (t < t0) return;
            float k = Mathf.Clamp01((t - t0) / 0.22f);
            float s = Mathf.Lerp(1.9f, 1f, AdEase(k));
            var rr = new Rect(r.center.x - r.width * s * 0.5f, r.center.y - r.height * s * 0.5f, r.width * s, r.height * s);
            var saved = GUI.matrix;
            PromoRotate(tilt, rr.center - promoOrigin);
            RoundFrame(rr, new Color(c.r, c.g, c.b, k), 4f, 10f);
            Round(Grow(rr, -6f), new Color(c.r * 0.3f, c.g * 0.3f, c.b * 0.3f, 0.55f * k), 8f);
            UIStyles.Outlined(rr, text, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(rr.height * 0.48f)), new Color(1f, 1f, 1f, k), 2f);
            GUI.matrix = saved;
        }

        /// <summary>A soft band of light sweeping across a rect every few seconds (upright, so it stays clipped).</summary>
        void AdSweep(Rect r, float period, float offset, float strength = 1f)
        {
            float k = Mathf.Repeat(Time.unscaledTime + offset, period) / period;
            if (k > 0.35f) return;
            float x = Mathf.Lerp(r.x - 80f, r.xMax + 20f, k / 0.35f);
            for (int i = 0; i < 5; i++)
                UIStyles.Rect(new Rect(x + i * 12f, r.y, 12f, r.height), new Color(1f, 1f, 1f, 0.12f * strength * (1f - Mathf.Abs(i - 2) / 3f)));
        }

        /// <summary>Sparkles bursting out of a point at t0.</summary>
        void AdSparkBurst(Vector2 c, float t, float t0, Color col, float radius = 90f)
        {
            float k = (t - t0) / 0.6f;
            if (k < 0f || k > 1f) return;
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f + i * 0.3f;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.6f) * radius * AdEase(k);
                float sz = 7f * (1f - k);
                var cc = new Color(col.r, col.g, col.b, 1f - k);
                UIStyles.Rect(new Rect(p.x - sz, p.y - 1f, sz * 2f, 2f), cc);
                UIStyles.Rect(new Rect(p.x - 1f, p.y - sz, 2f, sz * 2f), cc);
            }
        }

        /// <summary>A pulsing call-to-action pill with a bouncing arrow.</summary>
        void AdCta(Rect r, string text, Color c, float t, float t0)
        {
            if (t < t0) return;
            float k = AdBack(Mathf.Clamp01((t - t0) / 0.3f));
            float pulse = 1f + 0.05f * Mathf.Sin(Time.unscaledTime * 6f);
            var rr = new Rect(r.center.x - r.width * 0.5f * k * pulse, r.center.y - r.height * 0.5f * k * pulse, r.width * k * pulse, r.height * k * pulse);
            if (rr.width < 4f) return;
            FlatBtnLook(rr, c);
            float bounce = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 6f;
            UIStyles.Outlined(new Rect(rr.x, rr.y - 2f, rr.width - 10f, rr.height), text, UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(rr.height * 0.44f)), Color.white, 2f);
            UIStyles.Outlined(new Rect(rr.xMax - 34f + bounce, rr.y - 2f, 30f, rr.height), "▶", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(rr.height * 0.44f)), Color.white, 2f);
        }
    }
}
