using UnityEngine;

namespace HashiraChronicles
{
    public enum GraphicsTier { Low, Medium, High, Ultra }

    /// <summary>
    /// Player-facing settings (graphics tier, audio, camera shake, damage numbers), persisted in PlayerPrefs.
    /// Every system reads its budget from here so low-end phones stay at a stable frame rate.
    /// </summary>
    public static class GameSettings
    {
        public static GraphicsTier Tier = GraphicsTier.High;
        public static float MusicVolume = 0.7f;
        public static float SfxVolume = 0.9f;
        public static float ShakeIntensity = 1f;
        public static bool ShowDamageNumbers = true;

        public static event System.Action Changed;

        static bool loaded;

        /// <summary>Multiplier applied to particle counts.</summary>
        public static float ParticleScale
        {
            get
            {
                if (Tier == GraphicsTier.Low) return 0.35f;
                if (Tier == GraphicsTier.Medium) return 0.65f;
                if (Tier == GraphicsTier.High) return 1f;
                return 1.3f;
            }
        }

        /// <summary>Bloom, vignette, colour grading and radial blur.</summary>
        public static bool PostEffects { get { return Tier >= GraphicsTier.High; } }

        /// <summary>How many short-lived impact / lantern point lights may exist at once.</summary>
        public static int MaxDynamicLights
        {
            get
            {
                if (Tier == GraphicsTier.Low) return 0;
                if (Tier == GraphicsTier.Medium) return 2;
                if (Tier == GraphicsTier.High) return 4;
                return 8;
            }
        }

        /// <summary>Scenery density (props, trees, buildings) for the arena builder.</summary>
        public static float SceneryDensity
        {
            get
            {
                if (Tier == GraphicsTier.Low) return 0.45f;
                if (Tier == GraphicsTier.Medium) return 0.75f;
                return 1f;
            }
        }

        public static void Load()
        {
            if (loaded) return;
            loaded = true;
            Tier = (GraphicsTier)PlayerPrefs.GetInt("gfx_tier_v2", (int)DetectTier());
            MusicVolume = PlayerPrefs.GetFloat("vol_music", 0.7f);
            SfxVolume = PlayerPrefs.GetFloat("vol_sfx", 0.9f);
            ShakeIntensity = PlayerPrefs.GetFloat("cam_shake", 1f);
            ShowDamageNumbers = PlayerPrefs.GetInt("dmg_numbers", 1) == 1;
            Apply();
        }

        public static void Save()
        {
            PlayerPrefs.SetInt("gfx_tier_v2", (int)Tier);
            PlayerPrefs.SetFloat("vol_music", MusicVolume);
            PlayerPrefs.SetFloat("vol_sfx", SfxVolume);
            PlayerPrefs.SetFloat("cam_shake", ShakeIntensity);
            PlayerPrefs.SetInt("dmg_numbers", ShowDamageNumbers ? 1 : 0);
            PlayerPrefs.Save();
            Apply();
        }

        /// <summary>Picks a sensible default from the device's memory and GPU.</summary>
        public static GraphicsTier DetectTier()
        {
            if (!Application.isMobilePlatform) return GraphicsTier.Ultra;
            int mem = SystemInfo.systemMemorySize;
            int gpuMem = SystemInfo.graphicsMemorySize;
            if (mem < 3000 || gpuMem < 1024) return GraphicsTier.Low;
            if (mem < 6000) return GraphicsTier.Medium;
            return GraphicsTier.High;
        }

        public static void Apply()
        {
            switch (Tier)
            {
                case GraphicsTier.Low:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    QualitySettings.shadowDistance = 20f;
                    QualitySettings.antiAliasing = 0;
                    Application.targetFrameRate = 30;
                    break;
                case GraphicsTier.Medium:
                    // Soft, higher-resolution shadows over a shorter distance: no more stair-stepped edges.
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    QualitySettings.shadowDistance = 26f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.shadowProjection = ShadowProjection.StableFit;
                    QualitySettings.antiAliasing = 0;
                    Application.targetFrameRate = 60;
                    break;
                case GraphicsTier.High:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowDistance = 35f;
                    QualitySettings.shadowCascades = 2;
                    QualitySettings.antiAliasing = 4;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.Enable;
                    QualitySettings.pixelLightCount = 4;
                    Application.targetFrameRate = 60;
                    break;
                default:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.VeryHigh;
                    QualitySettings.shadowDistance = 60f;
                    QualitySettings.shadowCascades = 4;
                    QualitySettings.antiAliasing = 8;
                    QualitySettings.anisotropicFiltering = AnisotropicFiltering.ForceEnable;
                    QualitySettings.pixelLightCount = 8;
                    QualitySettings.softParticles = true;
                    QualitySettings.lodBias = 2f;
                    Application.targetFrameRate = 60;
                    break;
            }
            QualitySettings.vSyncCount = 0;
            var h = Changed;
            if (h != null) h();
        }
    }
}
