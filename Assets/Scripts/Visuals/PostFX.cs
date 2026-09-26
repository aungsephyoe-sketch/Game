using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Camera post-processing for the Built-in pipeline: bloom (so slashes, lanterns and ultimates glow),
    /// anime-style colour grade, vignette, and a radial blur pulse for ultimates / heavy impacts.
    /// Enabled on High/Ultra; Low/Medium skip it entirely to protect frame rate.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PostFX : MonoBehaviour
    {
        public static PostFX Instance { get; private set; }

        public float Threshold = 0.85f;
        public float Knee = 0.35f;
        public float BloomIntensity = 0.75f;
        public float Saturation = 1.12f;
        public float Contrast = 1.06f;
        public float Vignette = 0.9f;
        public Color Tint = new Color(1.02f, 1f, 0.98f);

        Material mat;
        float radial, radialUntil, radialDuration;
        readonly RenderTexture[] chain = new RenderTexture[6];

        void Awake()
        {
            Instance = this;
            var shader = Resources.Load<Shader>("Shaders/PostFX");
            if (shader != null && shader.isSupported) mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            GameEvents.UltimateStarted += OnUltimate;
            GameEvents.Impact += OnImpact;
        }

        void OnDestroy()
        {
            GameEvents.UltimateStarted -= OnUltimate;
            GameEvents.Impact -= OnImpact;
            if (mat != null) Destroy(mat);
        }

        void OnUltimate(PlayerCharacter pc, AbilityDefinition ab) { Radial(0.12f, 1.1f); }
        void OnImpact(float strength) { Radial(0.05f * strength, 0.18f); }

        /// <summary>Zoom-blur pulse toward the screen centre.</summary>
        public void Radial(float amount, float duration)
        {
            if (Time.unscaledTime < radialUntil && amount < radial) return;
            radial = amount;
            radialDuration = Mathf.Max(0.05f, duration);
            radialUntil = Time.unscaledTime + radialDuration;
        }

        void OnRenderImage(RenderTexture src, RenderTexture dst)
        {
            if (mat == null || !GameSettings.PostEffects)
            {
                Graphics.Blit(src, dst);
                return;
            }

            float remaining = radialUntil - Time.unscaledTime;
            float blur = remaining > 0f ? radial * Mathf.Clamp01(remaining / radialDuration) : 0f;

            mat.SetFloat("_Threshold", Threshold);
            mat.SetFloat("_Knee", Knee);
            mat.SetFloat("_BloomIntensity", BloomIntensity);
            mat.SetFloat("_Saturation", Saturation);
            mat.SetFloat("_Contrast", Contrast);
            mat.SetFloat("_Vignette", Vignette);
            mat.SetFloat("_RadialBlur", blur);
            mat.SetColor("_Tint", Tint);

            int iterations = GameSettings.Tier == GraphicsTier.Ultra ? 6 : 5;
            int w = src.width / 2, h = src.height / 2;
            var format = src.format;
            int count = 0;
            chain[0] = RenderTexture.GetTemporary(w, h, 0, format);
            Graphics.Blit(src, chain[0], mat, 0);
            count = 1;
            for (int i = 1; i < iterations; i++)
            {
                w /= 2;
                h /= 2;
                if (w < 2 || h < 2) break;
                chain[i] = RenderTexture.GetTemporary(w, h, 0, format);
                Graphics.Blit(chain[i - 1], chain[i], mat, 1);
                count++;
            }
            for (int i = count - 2; i >= 0; i--) Graphics.Blit(chain[i + 1], chain[i], mat, 2);

            mat.SetTexture("_BloomTex", chain[0]);
            Graphics.Blit(src, dst, mat, 3);

            for (int i = 0; i < count; i++)
            {
                RenderTexture.ReleaseTemporary(chain[i]);
                chain[i] = null;
            }
        }
    }
}
