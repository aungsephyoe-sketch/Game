using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Creates materials from the shaders in Assets/Resources/Shaders (cel-shaded toon, additive, transparent).
    /// Loading through Resources guarantees they are included in mobile builds.
    /// </summary>
    public static class MaterialFactory
    {
        static Shader toon, additive, transparent, world, sky;
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
        static Shader WorldShader { get { return world != null ? world : (world = LoadShader("Shaders/ToonWorld", "Standard", "Unlit/Color")); } }
        static Shader SkyShader { get { return sky != null ? sky : (sky = LoadShader("Shaders/SkyGradient", "Unlit/Color")); } }

        /// <summary>Vertex-coloured cel material for combined world meshes; vertex alpha drives wind sway and (optionally) glow.</summary>
        public static Material World(Color tint, float wind = 0f, Color? alphaGlow = null, Color? shadow = null, Color? rim = null)
        {
            var m = new Material(WorldShader);
            m.color = tint;
            if (m.HasProperty("_Wind")) m.SetFloat("_Wind", wind);
            if (m.HasProperty("_AlphaEmit")) m.SetColor("_AlphaEmit", alphaGlow ?? Color.black);
            if (shadow.HasValue && m.HasProperty("_ShadowColor")) m.SetColor("_ShadowColor", shadow.Value);
            if (rim.HasValue && m.HasProperty("_RimColor")) m.SetColor("_RimColor", rim.Value);
            return m;
        }

        /// <summary>Unlit vertex-colour material for the sky dome.</summary>
        public static Material Sky() { return new Material(SkyShader); }

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
            // Cartoon look: thicker dark outlines.
            if (m.HasProperty("_OutlineWidth")) m.SetFloat("_OutlineWidth", GameConfig.CartoonStyle ? outline * 1.45f : outline);
            if (m.HasProperty("_Emission")) m.SetColor("_Emission", emission ?? Color.black);
            if (m.HasProperty("_EmissionColor") && emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
            }
            return m;
        }

        /// <summary>Toon material with a tiling hand-painted texture (falls back to flat colour if the art isn't downloaded).</summary>
        static readonly System.Collections.Generic.Dictionary<string, Material> painted = new System.Collections.Generic.Dictionary<string, Material>();

        public static Material Painted(string textureKey, Color tint, float tiling, float outline = 0f)
        {
            // Shared per (texture, tint, tiling) so static batching can merge the set pieces.
            string key = textureKey + ColorUtility.ToHtmlStringRGB(tint) + tiling.ToString("F2") + outline.ToString("F3");
            Material cached;
            if (painted.TryGetValue(key, out cached) && cached != null) return cached;
            var tex = ArtLibrary.Surface(textureKey);
            var m = Toon(tint, outline);
            if (tex != null)
            {
                m.mainTexture = tex;
                m.mainTextureScale = new Vector2(tiling, tiling);
            }
            painted[key] = m;
            return m;
        }

        /// <summary>Tint that keeps a region's mood while letting the painted texture show its own colours.</summary>
        public static Color TextureTint(Color regionColor, float strength = 0.45f)
        {
            Color bright = regionColor * (1f / Mathf.Max(0.05f, Mathf.Max(regionColor.r, Mathf.Max(regionColor.g, regionColor.b))));
            return Color.Lerp(Color.white, bright, strength) * Mathf.Lerp(1f, Mathf.Clamp(regionColor.maxColorComponent * 1.6f, 0.45f, 1f), 0.6f);
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
