using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Creates materials from the shaders in Assets/Resources/Shaders (cel-shaded toon, additive, transparent).
    /// Loading through Resources guarantees they are included in mobile builds.
    /// </summary>
    public static class MaterialFactory
    {
        static Shader toon, additive, transparent;
        static Texture2D softDot, white;

        static Shader LoadShader(string resource, params string[] fallbacks)
        {
            var s = Resources.Load<Shader>(resource);
            if (s != null && s.isSupported) return s;
            foreach (var f in fallbacks)
            {
                s = Shader.Find(f);
                if (s != null) return s;
            }
            Debug.LogWarning("[MaterialFactory] No shader found for " + resource);
            return Shader.Find("Hidden/InternalErrorShader");
        }

        static Shader ToonShader { get { return toon != null ? toon : (toon = LoadShader("Shaders/Toon", "Standard", "Unlit/Color")); } }
        static Shader AdditiveShader { get { return additive != null ? additive : (additive = LoadShader("Shaders/UnlitAdditive", "Sprites/Default")); } }
        static Shader TransparentShader { get { return transparent != null ? transparent : (transparent = LoadShader("Shaders/UnlitTransparent", "Sprites/Default")); } }

        public static Texture2D White
        {
            get
            {
                if (white == null)
                {
                    white = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    white.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
                    white.Apply();
                }
                return white;
            }
        }

        /// <summary>Soft radial dot used for particles.</summary>
        public static Texture2D SoftDot
        {
            get
            {
                if (softDot == null)
                {
                    const int n = 64;
                    softDot = new Texture2D(n, n, TextureFormat.RGBA32, false);
                    softDot.wrapMode = TextureWrapMode.Clamp;
                    var px = new Color[n * n];
                    for (int y = 0; y < n; y++)
                        for (int x = 0; x < n; x++)
                        {
                            float dx = (x + 0.5f) / n * 2f - 1f, dy = (y + 0.5f) / n * 2f - 1f;
                            float a = Mathf.Clamp01(1f - Mathf.Sqrt(dx * dx + dy * dy));
                            px[y * n + x] = new Color(1f, 1f, 1f, a * a);
                        }
                    softDot.SetPixels(px);
                    softDot.Apply();
                }
                return softDot;
            }
        }

        public static Material Toon(Color color, float outline = 0.03f, Color? emission = null)
        {
            var m = new Material(ToonShader);
            m.color = color;
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", outline);
            if (m.HasProperty("_Emission")) m.SetColor("_Emission", emission ?? Color.black);
            if (m.HasProperty("_EmissionColor") && emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        public static Material Additive(Color color, bool textured = false)
        {
            var m = new Material(AdditiveShader);
            m.color = color;
            if (m.HasProperty("_MainTex")) m.mainTexture = textured ? SoftDot : White;
            return m;
        }

        public static Material Transparent(Color color, bool textured = false)
        {
            var m = new Material(TransparentShader);
            m.color = color;
            if (m.HasProperty("_MainTex")) m.mainTexture = textured ? SoftDot : White;
            return m;
        }
    }
}
