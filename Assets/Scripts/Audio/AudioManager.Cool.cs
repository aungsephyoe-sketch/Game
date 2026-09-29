using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The "cool" pass: the sounds you hear most in a fight rebuilt with more layers — a whip-crack transient,
    /// a doppler body, a sub drop you feel, a bright metallic shimmer and a room tail — so dashes snap, crits ring,
    /// elements crackle and ultimates land like thunder.
    /// </summary>
    public partial class AudioManager
    {
        void GenerateCoolPass()
        {
            float[] b;
            // Dash: a sharp air-rip that swells and passes.
            b = Buffer(0.45f); AddNoise(b, 0f, 0.01f, 0.8f, 250f, 1f, 0.001f); AddBladeSwing(b, 0.28f, 400f, 5200f, 0f, 0.5f, 0f);
            AddTone(b, 0f, 0.2f, 180f, 90f, 0.3f, 14f); Saturate(b, 1.3f); Make("dash", b, 0.75f);
            // Crit: whip-crack, heavy thump and a ringing blade shimmer.
            b = Buffer(0.9f); AddNoise(b, 0f, 0.008f, 1f, 300f, 1f, 0.0005f); AddSweep(b, 0f, 0.06f, 6000f, 2000f, 3f, 0.8f, 50f, 0.001f);
            AddTone(b, 0f, 0.25f, 150f, 55f, 1f, 13f); AddTone(b, 0f, 0.4f, 60f, 32f, 0.6f, 8f, 0.004f);
            AddFM(b, 0.01f, 0.8f, 2093f, 2.76f, 1.4f, 0.28f, 5f); AddFM(b, 0.03f, 0.7f, 3136f, 1.41f, 0.9f, 0.16f, 6f);
            Saturate(b, 1.5f); Reverb(b, 0.3f, 0.7f); Make("crit", b, 0.95f);
            // Slam / impact: ground-shaking boom with debris.
            b = Buffer(1.1f); AddNoise(b, 0f, 0.02f, 1f, 120f, 1f, 0.001f); AddTone(b, 0f, 0.8f, 85f, 28f, 1f, 5f); AddTone(b, 0f, 0.9f, 45f, 24f, 0.8f, 3.5f, 0.006f);
            AddNoise(b, 0.02f, 0.6f, 0.6f, 6f, 0.3f, 0.01f); for (int i = 0; i < 6; i++) AddNoise(b, 0.08f + i * 0.07f, 0.05f, 0.25f, 60f, 0.7f);
            Saturate(b, 2f); Reverb(b, 0.35f, 1f); Make("slam", b, 1f); Make("impact", b, 1f);
            // Ultimate start: rising power swell.
            b = Buffer(1.4f); AddSweep(b, 0f, 1.3f, 120f, 2400f, 1.2f, 1f, 1.2f, 0.4f); AddTone(b, 0f, 1.3f, 55f, 110f, 0.6f, 1.5f, 0.3f);
            for (int k = 0; k < 4; k++) AddFM(b, 0.3f + k * 0.2f, 0.6f, 660f * (1f + k * 0.25f), 2f, 0.8f, 0.12f, 5f, 0.02f);
            Saturate(b, 1.4f); Reverb(b, 0.4f, 1.3f); Make("ultimate", b, 0.9f);
            // Ultimate finish: thunderclap + sub drop + shimmering tail.
            b = Buffer(2.2f); AddNoise(b, 0f, 0.03f, 1f, 90f, 1f, 0.001f); AddNoise(b, 0f, 1.2f, 0.9f, 3f, 0.25f, 0.01f);
            AddTone(b, 0f, 1.5f, 70f, 22f, 1f, 2.2f); AddTone(b, 0f, 1.2f, 110f, 40f, 0.6f, 3f);
            AddFM(b, 0.05f, 1.8f, 1760f, 2.01f, 1f, 0.2f, 2.2f); AddFM(b, 0.1f, 1.6f, 2637f, 1.5f, 0.6f, 0.12f, 2.6f);
            Saturate(b, 1.8f); Reverb(b, 0.5f, 1.6f); Make("sp_finish", b, 1f);
            // Elements.
            b = Buffer(0.7f); AddNoise(b, 0f, 0.015f, 1f, 200f, 1f, 0.0005f); for (int k = 0; k < 5; k++) AddNoise(b, k * 0.03f, 0.02f, 0.8f, 150f, 1f, 0.0005f);
            AddTone(b, 0f, 0.6f, 90f, 40f, 0.7f, 4f); AddNoise(b, 0.05f, 0.6f, 0.5f, 5f, 0.2f, 0.02f); Saturate(b, 1.8f); Reverb(b, 0.35f, 1f); Make("el_thunder", b, 0.9f);
            b = Buffer(0.8f); AddSweep(b, 0f, 0.7f, 300f, 1200f, 0.8f, 1f, 3f, 0.05f); AddNoise(b, 0f, 0.7f, 0.7f, 3f, 0.55f, 0.05f);
            for (int k = 0; k < 10; k++) { float t0 = Random.Range(0.05f, 0.6f); AddNoise(b, t0, 0.02f, 0.4f, 120f, 0.9f); }
            AddTone(b, 0f, 0.5f, 80f, 50f, 0.4f, 5f, 0.03f); Saturate(b, 1.5f); Reverb(b, 0.25f, 0.8f); Make("el_flame", b, 0.8f);
            b = Buffer(0.8f); AddSweep(b, 0f, 0.6f, 3400f, 600f, 1.5f, 1f, 5f, 0.01f); for (int k = 0; k < 10; k++) { float t0 = Random.Range(0.02f, 0.5f), f = Random.Range(600f, 1400f); AddTone(b, t0, 0.07f, f, f * 1.9f, 0.18f, 35f); }
            AddTone(b, 0f, 0.4f, 140f, 80f, 0.4f, 7f, 0.02f); Reverb(b, 0.35f, 0.8f); Make("el_water", b, 0.8f);
            b = Buffer(0.7f); AddBladeSwing(b, 0.5f, 250f, 3200f, 0f, 0.7f, 0f); AddSweep(b, 0.1f, 0.5f, 900f, 2400f, 6f, 0.3f, 4f, 0.05f); Make("el_wind", b, 0.8f);
            b = Buffer(0.9f); AddTone(b, 0f, 0.7f, 70f, 30f, 1f, 4f); AddNoise(b, 0f, 0.5f, 0.8f, 6f, 0.15f, 0.005f);
            for (int k = 0; k < 8; k++) AddNoise(b, 0.05f + k * 0.06f, 0.04f, 0.35f, 70f, 0.5f); Saturate(b, 2f); Reverb(b, 0.3f, 1f); Make("el_earth", b, 0.9f);
        }
    }
}
