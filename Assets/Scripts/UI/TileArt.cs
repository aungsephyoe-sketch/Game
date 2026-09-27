using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Small painted backgrounds for the big home tiles, generated once at startup: a sunset over a
    /// torii gate for STORY, a red glow with a light streak for PLAY. Drawn with rounded corners.
    /// </summary>
    public static class TileArt
    {
        static Texture2D story, play;

        public static Texture2D Story { get { if (story == null) story = PaintStory(); return story; } }
        public static Texture2D Play { get { if (play == null) play = PaintPlay(); return play; } }

        static Color Mix(Color a, Color b, float t) { return Color.Lerp(a, b, Mathf.Clamp01(t)); }

        static Texture2D NewTex(int w, int h)
        {
            var t = new Texture2D(w, h, TextureFormat.RGBA32, true);
            t.wrapMode = TextureWrapMode.Clamp;
            t.filterMode = FilterMode.Trilinear;
            return t;
        }

        static Texture2D PaintStory()
        {
            const int W = 420, H = 272;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            Color top = new Color(0.22f, 0.16f, 0.38f), mid = new Color(0.95f, 0.45f, 0.3f), low = new Color(1f, 0.78f, 0.4f);
            Vector2 sun = new Vector2(0.5f, 0.42f);
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / W, v = (float)y / H; // v = 0 bottom
                    // Sky.
                    Color c = v > 0.55f ? Mix(mid, top, (v - 0.55f) / 0.45f) : Mix(low, mid, (v - 0.3f) / 0.25f);
                    // Sun and glow.
                    float d = Vector2.Distance(new Vector2(u * 1.55f, v), new Vector2(sun.x * 1.55f, sun.y));
                    c = Mix(c, new Color(1f, 0.9f, 0.55f), Mathf.Clamp01(0.35f - d) * 1.6f);
                    if (d < 0.09f) c = new Color(1f, 0.93f, 0.65f);
                    // Far mountains, then near hills.
                    float far = 0.33f + 0.1f * Mathf.Sin(u * 7f + 1f) + 0.05f * Mathf.Sin(u * 19f);
                    if (v < far) c = Mix(new Color(0.55f, 0.28f, 0.35f), c, 0.25f);
                    float near = 0.2f + 0.06f * Mathf.Sin(u * 5f + 3f) + 0.03f * Mathf.Sin(u * 23f);
                    if (v < near) c = new Color(0.16f, 0.09f, 0.14f);
                    // Pines on the sides.
                    for (int i = 0; i < 6; i++)
                    {
                        float tx = i < 3 ? 0.04f + i * 0.07f : 0.8f + (i - 3) * 0.07f;
                        float th = 0.32f + 0.08f * ((i * 37) % 5) / 5f;
                        float half = (th - v) * 0.25f;
                        if (v < th && v > 0.12f && Mathf.Abs(u - tx) < half) c = new Color(0.12f, 0.07f, 0.1f);
                    }
                    // Torii gate silhouette.
                    Color gate = new Color(0.1f, 0.05f, 0.07f);
                    bool post = (Mathf.Abs(u - 0.4f) < 0.018f || Mathf.Abs(u - 0.6f) < 0.018f) && v > 0.14f && v < 0.64f;
                    bool beam1 = Mathf.Abs(v - 0.57f) < 0.018f && u > 0.37f && u < 0.63f;
                    float curve = 0.02f * Mathf.Pow((u - 0.5f) / 0.16f, 2f);
                    bool beam2 = Mathf.Abs(v - (0.66f + curve)) < 0.025f && u > 0.33f && u < 0.67f;
                    if (post || beam1 || beam2) c = gate;
                    // Path of light up to the gate.
                    if (v < 0.14f && Mathf.Abs(u - 0.5f) < 0.04f + (0.14f - v) * 0.8f) c = Mix(c, new Color(1f, 0.75f, 0.45f), 0.35f);
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }

        static Texture2D PaintPlay()
        {
            const int W = 440, H = 272;
            var tex = NewTex(W, H);
            var px = new Color[W * H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    float u = (float)x / W, v = (float)y / H;
                    float glow = Mathf.Clamp01(1f - Vector2.Distance(new Vector2(u, v), new Vector2(0.72f, 0.55f)) * 1.4f);
                    Color c = Mix(new Color(0.42f, 0.03f, 0.06f), new Color(0.95f, 0.18f, 0.16f), glow);
                    // Diagonal light streak, like a sword cut.
                    float line = Mathf.Abs((v - 0.1f) - (u - 0.35f) * 0.9f);
                    c = Mix(c, new Color(1f, 0.75f, 0.6f), Mathf.Clamp01(0.03f - line) * 25f * Mathf.Clamp01(u * 2f - 0.5f));
                    // Faint embers.
                    float n = Mathf.PerlinNoise(u * 28f, v * 18f);
                    if (n > 0.78f) c = Mix(c, new Color(1f, 0.5f, 0.3f), (n - 0.78f) * 3f);
                    px[y * W + x] = c;
                }
            tex.SetPixels(px);
            tex.Apply(true, true);
            return tex;
        }
    }
}
