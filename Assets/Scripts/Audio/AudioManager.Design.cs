using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The sound design pass: filtered noise sweeps for blade swings, FM rings for steel, layered impacts with sub
    /// weight, element textures (splashes, roaring fire, electric cracks, wind whistles, bell chimes, distorted
    /// dark booms), a full special-attack sequence (activation, element build-up, whoosh, release, finishing
    /// boom with a shimmering tail), softer UI sounds, a mission-clear fanfare, and the home-screen theme with its
    /// ambience bed. Everything is still synthesised at startup, so the game ships without audio files.
    /// </summary>
    public partial class AudioManager
    {
        // ------------------------------------------------------------------ DSP helpers

        /// <summary>RBJ biquad filter in place (0 = low-pass, 1 = high-pass, 2 = band-pass).</summary>
        static void Filter(float[] d, int type, float freq, float q, int start = 0, int count = -1)
        {
            if (count < 0) count = d.Length - start;
            float w = 2f * Mathf.PI * Mathf.Clamp(freq, 20f, Rate * 0.45f) / Rate;
            float cw = Mathf.Cos(w), sw = Mathf.Sin(w), alpha = sw / (2f * Mathf.Max(0.1f, q));
            float b0, b1, b2, a0 = 1f + alpha, a1 = -2f * cw, a2 = 1f - alpha;
            switch (type)
            {
                case 1: b0 = (1f + cw) * 0.5f; b1 = -(1f + cw); b2 = b0; break;
                case 2: b0 = alpha; b1 = 0f; b2 = -alpha; break;
                default: b0 = (1f - cw) * 0.5f; b1 = 1f - cw; b2 = b0; break;
            }
            b0 /= a0; b1 /= a0; b2 /= a0; a1 /= a0; a2 /= a0;
            float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;
            for (int i = start; i < start + count && i < d.Length; i++)
            {
                float x = d[i];
                float y = b0 * x + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                d[i] = y;
            }
        }

        /// <summary>Noise through a band-pass whose centre sweeps from f0 to f1: the classic "swish".</summary>
        void AddSweep(float[] d, float start, float length, float f0, float f1, float q, float amp, float decay, float attack = 0.01f)
        {
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            float x1 = 0f, x2 = 0f, y1 = 0f, y2 = 0f;
            float b0 = 0f, b2 = 0f, a1 = 0f, a2 = 0f;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                if (i % 16 == 0)
                {
                    float k = (float)i / n;
                    float f = f0 * Mathf.Pow(f1 / f0, k);
                    float w = 2f * Mathf.PI * Mathf.Clamp(f, 30f, Rate * 0.45f) / Rate;
                    float alpha = Mathf.Sin(w) / (2f * q), a0 = 1f + alpha;
                    b0 = alpha / a0; b2 = -alpha / a0; a1 = -2f * Mathf.Cos(w) / a0; a2 = (1f - alpha) / a0;
                }
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay);
                float x = Noise();
                float y = b0 * x + b2 * x2 - a1 * y1 - a2 * y2;
                x2 = x1; x1 = x; y2 = y1; y1 = y;
                d[s0 + i] += y * env * amp * 3f;
            }
        }

        /// <summary>FM tone: metallic rings (inharmonic ratios), bells and gongs.</summary>
        void AddFM(float[] d, float start, float length, float carrier, float ratio, float index, float amp, float decay, float attack = 0.002f)
        {
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            float pc = 0f, pm = 0f;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay);
                pm += 2f * Mathf.PI * carrier * ratio / Rate;
                pc += 2f * Mathf.PI * carrier / Rate;
                d[s0 + i] += Mathf.Sin(pc + index * env * Mathf.Sin(pm)) * env * amp;
            }
        }

        /// <summary>A breathy bamboo-flute note: soft sine with a touch of second harmonic, breath noise and vibrato.</summary>
        void AddFlute(float[] d, float start, float length, float freq, float amp)
        {
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            float ph = 0f, lp = 0f;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 0.12f) * Mathf.Min(1f, (length - t) / 0.25f);
                float vib = 1f + Mathf.Sin(t * 2f * Mathf.PI * 5.2f) * 0.006f * Mathf.Clamp01((t - 0.3f) * 2f);
                ph += 2f * Mathf.PI * freq * vib / Rate;
                float x = Noise();
                lp += (x - lp) * 0.15f;
                d[s0 + i] += (Mathf.Sin(ph) + 0.18f * Mathf.Sin(ph * 2f) + lp * 0.12f) * env * amp;
            }
        }

        /// <summary>A warm pad chord note: three detuned saws smoothed, with a slow swell and release.</summary>
        void AddPad(float[] d, float start, float length, float freq, float amp)
        {
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            float p1 = 0f, p2 = 0.3f, p3 = 0.6f, lp = 0f, lp2 = 0f;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / 1.2f) * Mathf.Min(1f, (length - t) / 1.5f);
                p1 = (p1 + freq / Rate) % 1f; p2 = (p2 + freq * 1.004f / Rate) % 1f; p3 = (p3 + freq * 0.996f / Rate) % 1f;
                float saw = (p1 + p2 + p3) * (2f / 3f) - 1f;
                lp += (saw - lp) * 0.06f;
                lp2 += (lp - lp2) * 0.06f;
                d[s0 + i] += lp2 * env * amp;
            }
        }

        /// <summary>A little bird call: a fast warbling chirp.</summary>
        void AddChirp(float[] d, float start, float freq, float amp, int notes)
        {
            for (int k = 0; k < notes; k++)
            {
                float t0 = start + k * 0.09f;
                float f = freq * (1f + (k % 2) * 0.18f);
                int s0 = (int)(t0 * Rate), n = (int)(0.07f * Rate);
                float ph = 0f;
                for (int i = 0; i < n && s0 + i < d.Length; i++)
                {
                    float t = (float)i / Rate, u = t / 0.07f;
                    float ff = f * (1f + 0.35f * Mathf.Sin(u * Mathf.PI)) * (1f + 0.04f * Mathf.Sin(t * 2f * Mathf.PI * 60f));
                    ph += 2f * Mathf.PI * ff / Rate;
                    d[s0 + i] += Mathf.Sin(ph) * Mathf.Sin(u * Mathf.PI) * amp;
                }
            }
        }

        /// <summary>
        /// A blade swing: a band-passed whoosh that rises to f1 then falls back as the blade passes (doppler), an airy
        /// low body (weight), a metallic ring at ringFreq and a short reverb tail.
        /// </summary>
        void AddBladeSwing(float[] d, float length, float f0, float f1, float ringFreq, float weight, float ring, float start = 0f)
        {
            float up = length * 0.55f;
            AddSweep(d, start, up, f0, f1, 2.4f, 1f, 3.5f / length, 0.012f);
            AddSweep(d, start + up * 0.7f, length - up * 0.5f, f1, f0 * 1.4f, 2.2f, 0.7f, 5f / length, 0.01f);
            if (weight > 0f) AddSweep(d, start, length, Mathf.Max(90f, f0 * 0.35f), Mathf.Max(300f, f0 * 1.2f), 0.9f, weight, 3f / length, 0.03f);
            if (ring > 0f)
            {
                AddFM(d, start + up * 0.6f, length * 1.2f, ringFreq, 2.76f, 1.3f, 0.1f * ring, 14f);
                AddFM(d, start + up * 0.6f, length, ringFreq * 1.5f, 1.41f, 0.8f, 0.05f * ring, 20f);
            }
            if (start <= 0f) Reverb(d, 0.16f, 0.45f);
        }

        static void Saturate(float[] d, float drive)
        {
            for (int i = 0; i < d.Length; i++) d[i] = (float)System.Math.Tanh(d[i] * drive);
        }

        /// <summary>Schroeder reverb: parallel combs into series all-passes. mix 0..1, room scales the delays.</summary>
        static void Reverb(float[] d, float mix, float room = 1f, float feedback = 0.78f)
        {
            int[] combs = { 1116, 1188, 1277, 1356 };
            int[] alls = { 556, 441 };
            float scale = Rate / 44100f * room;
            var wet = new float[d.Length];
            foreach (int c in combs)
            {
                int len = Mathf.Max(8, Mathf.RoundToInt(c * scale));
                var buf = new float[len];
                int idx = 0;
                float lp = 0f;
                for (int i = 0; i < d.Length; i++)
                {
                    float y = buf[idx];
                    lp = y * 0.6f + lp * 0.4f;
                    buf[idx] = d[i] + lp * feedback;
                    idx = (idx + 1) % len;
                    wet[i] += y * 0.25f;
                }
            }
            foreach (int a in alls)
            {
                int len = Mathf.Max(8, Mathf.RoundToInt(a * scale));
                var buf = new float[len];
                int idx = 0;
                for (int i = 0; i < wet.Length; i++)
                {
                    float bo = buf[idx];
                    float x = wet[i];
                    buf[idx] = x + bo * 0.5f;
                    wet[i] = bo - x * 0.5f;
                    idx = (idx + 1) % len;
                }
            }
            for (int i = 0; i < d.Length; i++) d[i] = d[i] * (1f - mix * 0.5f) + wet[i] * mix;
        }

        // ------------------------------------------------------------------ Sounds

        void GenerateSoundDesign()
        {
            float[] b;

            // Swings, one family per weapon.
            // Each swing is layered: a doppler whoosh (rising then falling as the blade passes), an airy body, a
            // bright steel "shing" and a short room tail, so blades sound sharp and sweeping instead of like hiss.
            b = Buffer(0.42f); AddBladeSwing(b, 0.26f, 600f, 4200f, 3150f, 0.35f, 0.9f); Make("sw_blade", b, 0.8f);
            b = Buffer(0.3f); AddBladeSwing(b, 0.17f, 1200f, 5600f, 4700f, 0.2f, 0.75f); Make("sw_quick", b, 0.75f);
            b = Buffer(0.6f); AddBladeSwing(b, 0.4f, 180f, 1500f, 1850f, 1f, 0.55f); AddTone(b, 0.03f, 0.4f, 90f, 45f, 0.5f, 7f, 0.04f); Saturate(b, 1.5f); Make("sw_heavy", b, 0.9f);
            b = Buffer(0.45f); AddBladeSwing(b, 0.3f, 320f, 1800f, 1320f, 0.6f, 0.35f); AddTone(b, 0f, 0.25f, 520f, 880f, 0.08f, 10f, 0.02f); Make("sw_staff", b, 0.75f);
            b = Buffer(0.28f); AddSweep(b, 0f, 0.1f, 300f, 1500f, 1.1f, 1f, 22f, 0.006f); AddSweep(b, 0.07f, 0.1f, 1500f, 500f, 1.2f, 0.6f, 30f, 0.004f); AddTone(b, 0f, 0.12f, 140f, 80f, 0.3f, 20f); Reverb(b, 0.15f, 0.4f); Make("sw_fist", b, 0.75f);
            b = Buffer(0.45f); for (int k = 0; k < 3; k++) AddBladeSwing(b, 0.12f, 1000f, 3200f, 2600f + k * 400f, 0.2f, 0.4f, k * 0.06f + 0.0001f); Reverb(b, 0.16f, 0.45f); Make("sw_fan", b, 0.7f);
            b = Buffer(0.5f); AddPluck(b, 0f, 196f, 0.7f, 0.3f, 0.985f); AddSweep(b, 0.02f, 0.2f, 1500f, 4500f, 3f, 0.6f, 22f); AddSweep(b, 0.08f, 0.2f, 4500f, 1800f, 3f, 0.35f, 22f); Reverb(b, 0.15f, 0.5f); Make("sw_bow", b, 0.72f);
            // The shared slash sounds used by skills and cutscenes.
            b = Buffer(0.42f); AddBladeSwing(b, 0.26f, 700f, 4400f, 3300f, 0.35f, 0.9f); Make("slash", b, 0.75f);
            b = Buffer(0.6f); AddBladeSwing(b, 0.4f, 240f, 1900f, 2100f, 0.9f, 0.6f); AddTone(b, 0f, 0.35f, 110f, 55f, 0.4f, 8f, 0.03f); Saturate(b, 1.4f); Make("slashHeavy", b, 0.85f);

            // Impacts.
            // Impacts: a sharp crack, a body thump, a sub boom you feel, and a short room tail.
            b = Buffer(0.5f); AddNoise(b, 0f, 0.012f, 1f, 200f, 1f, 0.001f); AddSweep(b, 0f, 0.08f, 3600f, 1400f, 3f, 0.9f, 40f, 0.002f);
            AddTone(b, 0f, 0.2f, 160f, 70f, 0.9f, 16f); AddTone(b, 0f, 0.3f, 62f, 38f, 0.55f, 10f, 0.004f); AddFM(b, 0.004f, 0.25f, 2650f, 2.76f, 1.5f, 0.16f, 22f);
            Saturate(b, 1.6f); Reverb(b, 0.18f, 0.45f); Make("hit_blade", b, 0.85f);
            b = Buffer(0.8f); AddNoise(b, 0f, 0.02f, 1f, 150f, 1f, 0.001f); AddTone(b, 0f, 0.5f, 100f, 34f, 1f, 7f); AddTone(b, 0f, 0.6f, 55f, 28f, 0.7f, 5f, 0.006f);
            AddNoise(b, 0f, 0.35f, 0.8f, 12f, 0.35f, 0.002f); AddSweep(b, 0f, 0.15f, 2500f, 600f, 1.5f, 0.5f, 20f, 0.002f); Saturate(b, 2.2f); Reverb(b, 0.25f, 0.7f); Make("hit_heavy", b, 1f);
            b = Buffer(0.4f); AddNoise(b, 0f, 0.01f, 0.9f, 200f, 1f, 0.001f); AddTone(b, 0f, 0.22f, 135f, 60f, 1f, 16f); AddTone(b, 0f, 0.28f, 60f, 36f, 0.5f, 11f, 0.004f);
            AddNoise(b, 0f, 0.14f, 0.6f, 28f, 0.45f, 0.002f); Saturate(b, 1.7f); Reverb(b, 0.14f, 0.4f); Make("hit_blunt", b, 0.9f);
            b = Buffer(0.3f); AddTone(b, 0f, 0.25f, 1800f, 500f, 0.5f, 16f); AddNoise(b, 0f, 0.1f, 0.6f, 40f, 0.95f); AddTone(b, 0f, 0.15f, 160f, 80f, 0.5f, 20f); Make("hit_magic", b, 0.75f);
            b = Buffer(0.7f); AddNoise(b, 0f, 0.015f, 1f, 150f, 1f, 0.001f); AddTone(b, 0f, 0.2f, 170f, 60f, 1f, 16f);
            AddFM(b, 0.01f, 0.6f, 1760f, 2.01f, 1.2f, 0.35f, 7f); AddFM(b, 0.04f, 0.55f, 2637f, 1.5f, 0.8f, 0.2f, 8f); AddSweep(b, 0f, 0.1f, 4000f, 1500f, 2f, 0.6f, 30f, 0.002f);
            Reverb(b, 0.35f, 0.6f); Make("crit", b, 0.9f);
            b = Buffer(0.4f); AddTone(b, 0f, 0.3f, 105f, 55f, 1f, 11f); AddNoise(b, 0f, 0.18f, 0.7f, 18f, 0.3f, 0.003f);
            AddTone(b, 0.01f, 0.16f, 190f, 140f, 0.25f, 14f, 0.01f, true); Filter(b, 0, 2400f, 0.8f); Make("hurt", b, 0.8f);

            // Element textures (played with swings, specials and element impacts).
            b = Buffer(0.55f); AddSweep(b, 0f, 0.5f, 3200f, 700f, 1.4f, 1f, 7f, 0.01f);
            for (int k = 0; k < 8; k++) { float t0 = Random.Range(0.02f, 0.4f), f = Random.Range(500f, 1100f); AddTone(b, t0, 0.06f, f, f * 1.8f, 0.2f, 40f); }
            AddTone(b, 0f, 0.4f, 180f, 110f, 0.25f, 8f, 0.03f); Reverb(b, 0.25f, 0.5f); Make("el_water", b, 0.75f);
            b = Buffer(0.6f); AddNoise(b, 0f, 0.55f, 1f, 5f, 0.3f, 0.06f); Filter(b, 0, 1400f, 0.7f); AddTone(b, 0f, 0.5f, 75f, 50f, 0.6f, 5f, 0.05f);
            for (int k = 0; k < 18; k++) AddNoise(b, Random.Range(0f, 0.5f), 0.012f, 0.6f, 200f, 1f); Saturate(b, 1.8f); Make("el_flame", b, 0.75f);
            b = Buffer(0.8f); AddNoise(b, 0f, 0.03f, 1.2f, 90f, 1f, 0.001f); AddTone(b, 0f, 0.18f, 4200f, 300f, 0.35f, 14f, 0.001f, true);
            for (int k = 0; k < 5; k++) AddNoise(b, 0.02f + k * 0.025f, 0.02f, 0.5f, 120f, 1f);
            AddNoise(b, 0.05f, 0.7f, 0.7f, 4f, 0.15f, 0.04f); Reverb(b, 0.3f, 0.9f); Make("el_thunder", b, 0.85f);
            b = Buffer(0.6f); AddSweep(b, 0f, 0.55f, 500f, 2400f, 5f, 0.8f, 5f, 0.12f); AddSweep(b, 0.05f, 0.5f, 300f, 900f, 1f, 0.6f, 6f, 0.08f); Make("el_wind", b, 0.65f);
            b = Buffer(0.9f); float[] bells = { 1318.5f, 1760f, 2217.5f, 2637f };
            for (int k = 0; k < bells.Length; k++) AddFM(b, k * 0.045f, 0.8f, bells[k], 3.5f, 0.9f, 0.25f, 5f);
            AddSweep(b, 0f, 0.4f, 5000f, 9000f, 2f, 0.2f, 6f, 0.05f); Reverb(b, 0.4f, 1f); Make("el_light", b, 0.65f);
            b = Buffer(0.7f); for (int i = 0; i < b.Length; i++) { float t = (float)i / Rate; float f = 62f - t * 25f; b[i] = (((t * f) % 1f) * 2f - 1f) * Mathf.Min(1f, t / 0.12f) * Mathf.Exp(-t * 4f); }
            AddTone(b, 0f, 0.6f, 48f, 32f, 0.6f, 4f, 0.08f); Saturate(b, 3f); Filter(b, 0, 900f, 0.7f); Reverb(b, 0.3f, 0.8f); Make("el_dark", b, 0.8f);

            // Movement and defence.
            b = Buffer(0.28f); AddSweep(b, 0f, 0.26f, 500f, 2100f, 0.9f, 1f, 12f, 0.03f); AddNoise(b, 0.18f, 0.06f, 0.3f, 50f, 0.3f); Make("dodge", b, 0.6f);
            b = Buffer(0.35f); AddFM(b, 0f, 0.3f, 1400f, 2.76f, 1.5f, 0.4f, 12f); AddTone(b, 0f, 0.15f, 140f, 90f, 0.5f, 20f); Make("guard", b, 0.55f);
            b = Buffer(0.5f); AddFM(b, 0f, 0.45f, 1250f, 2.76f, 2f, 0.6f, 9f); AddFM(b, 0f, 0.4f, 1870f, 1.41f, 1f, 0.3f, 10f); AddNoise(b, 0f, 0.03f, 0.8f, 90f, 1f);
            AddTone(b, 0f, 0.15f, 160f, 80f, 0.6f, 18f); Reverb(b, 0.2f, 0.5f); Make("block", b, 0.8f);
            b = Buffer(0.6f); AddFM(b, 0f, 0.5f, 1250f, 2.76f, 2f, 0.5f, 8f); AddNoise(b, 0f, 0.04f, 0.8f, 70f, 1f); AddTone(b, 0f, 0.2f, 140f, 70f, 0.7f, 14f); Reverb(b, 0.25f, 0.6f); Make("clang", b, 0.85f);

            // Enemies.
            b = Buffer(0.35f); AddSweep(b, 0f, 0.3f, 250f, 1000f, 1.1f, 0.9f, 10f, 0.03f);
            for (int i = 0; i < b.Length; i++) { float t = (float)i / Rate; b[i] += (((t * (110f - t * 90f)) % 1f) * 2f - 1f) * 0.25f * Mathf.Exp(-t * 9f); }
            Filter(b, 0, 2500f, 0.7f); Make("enemyAttack", b, 0.65f);
            b = Buffer(0.9f); AddNoise(b, 0f, 0.3f, 0.8f, 10f, 0.5f, 0.005f); AddTone(b, 0f, 0.3f, 180f, 60f, 0.6f, 10f);
            for (int k = 0; k < 6; k++) AddFM(b, 0.05f + k * 0.06f, 0.3f, 1400f - k * 150f, 2f, 0.6f, 0.12f, 10f);
            AddSweep(b, 0.1f, 0.7f, 3000f, 800f, 1.5f, 0.35f, 5f, 0.05f); Reverb(b, 0.3f, 0.7f); Make("enemyDeath", b, 0.6f);
            b = Buffer(1.3f); AddTone(b, 0f, 1.2f, 52f, 24f, 1f, 3f); AddNoise(b, 0f, 0.9f, 1f, 4.5f, 0.2f, 0.004f); AddNoise(b, 0f, 0.03f, 1f, 90f, 1f);
            for (int k = 0; k < 10; k++) AddNoise(b, 0.15f + Random.Range(0f, 0.6f), 0.02f, 0.4f, 90f, 0.6f);
            Saturate(b, 1.8f); Reverb(b, 0.25f, 1f); Make("bossSlam", b, 1f);
            b = Buffer(1.4f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 92f + Mathf.Sin(t * 9f) * 6f - t * 18f;
                float saw = ((t * f) % 1f) * 2f - 1f + (((t * f * 1.5f) % 1f) * 2f - 1f) * 0.4f;
                b[i] = saw * Mathf.Min(1f, t / 0.12f) * Mathf.Exp(-t * 1.6f);
            }
            AddNoise(b, 0f, 1.3f, 0.5f, 2.2f, 0.35f, 0.1f); Filter(b, 2, 520f, 0.8f); Saturate(b, 2.2f); Reverb(b, 0.3f, 1.1f); Make("roar", b, 0.85f);

            // Special attack sequence: activation → element build-up → whoosh → release → finish.
            b = Buffer(1.6f); AddFM(b, 0f, 1.5f, 170f, 1.4f, 3f, 0.8f, 2.4f); AddTaiko(b, 0f, 0.9f); AddSweep(b, 0f, 0.5f, 400f, 5000f, 1.5f, 0.5f, 5f, 0.1f);
            AddFM(b, 0.02f, 1f, 1320f, 3.5f, 0.6f, 0.15f, 4f); Reverb(b, 0.4f, 1.2f); Make("sp_activate", b, 0.9f);
            MakeBuild("build_water", 0); MakeBuild("build_flame", 1); MakeBuild("build_thunder", 2); MakeBuild("build_wind", 3); MakeBuild("build_light", 4); MakeBuild("build_dark", 5);
            b = Buffer(0.7f); AddSweep(b, 0f, 0.65f, 180f, 3200f, 1.3f, 1f, 5f, 0.12f); AddSweep(b, 0.05f, 0.6f, 600f, 6000f, 3f, 0.4f, 6f, 0.1f); Make("sp_whoosh", b, 0.8f);
            b = Buffer(2.4f); AddTone(b, 0f, 2.2f, 58f, 24f, 1f, 1.8f); AddNoise(b, 0f, 1.6f, 1f, 2.4f, 0.4f, 0.004f); AddNoise(b, 0f, 0.05f, 1f, 60f, 1f);
            AddTaiko(b, 0f, 1f); AddTaiko(b, 0.16f, 0.7f); AddFM(b, 0f, 1.6f, 880f, 1.5f, 1.2f, 0.2f, 2.5f); Saturate(b, 1.6f); Reverb(b, 0.35f, 1.3f); Make("specialRelease", b, 1f);
            b = Buffer(3f); AddTone(b, 0f, 2.5f, 50f, 22f, 1f, 1.6f); AddNoise(b, 0f, 1.2f, 1f, 3f, 0.35f, 0.003f); AddTaiko(b, 0f, 1f);
            float[] shimmer = { 1046.5f, 1318.5f, 1568f, 2093f, 2637f, 3136f };
            for (int k = 0; k < shimmer.Length; k++) AddFM(b, 0.1f + k * 0.07f, 2f, shimmer[k], 3.5f, 0.5f, 0.12f, 2f);
            Saturate(b, 1.4f); Reverb(b, 0.45f, 1.5f); Make("sp_finish", b, 1f);
            b = Buffer(2f); AddTone(b, 0f, 1.8f, 55f, 55f, 0.7f, 1.5f, 0.05f); AddSweep(b, 0f, 1f, 200f, 4000f, 1.4f, 0.5f, 2f, 0.4f);
            AddTaiko(b, 0.9f, 1f); AddNoise(b, 0.9f, 0.9f, 1f, 4f, 0.5f, 0.005f); Reverb(b, 0.4f, 1.3f); Make("ultimate", b, 0.95f);

            // UI.
            b = Buffer(0.06f); AddTone(b, 0f, 0.05f, 1150f, 1050f, 0.5f, 70f); AddNoise(b, 0f, 0.01f, 0.25f, 200f, 1f); Filter(b, 0, 5000f, 0.7f); Make("click", b, 0.35f);
            b = Buffer(0.4f); AddPluck(b, 0f, 880f, 0.6f, 0.35f, 0.99f); AddPluck(b, 0.07f, 1174.7f, 0.6f, 0.33f, 0.99f); Reverb(b, 0.25f, 0.5f); Make("switch", b, 0.5f);
            b = Buffer(1.1f); float[] up = { 1046.5f, 1318.5f, 1568f, 2093f };
            for (int k = 0; k < up.Length; k++) AddFM(b, k * 0.07f, 0.9f, up[k], 3.5f, 0.6f, 0.3f, 4f);
            AddSweep(b, 0.05f, 0.6f, 4000f, 9000f, 2f, 0.15f, 5f, 0.1f); Reverb(b, 0.4f, 0.9f); Make("perfect", b, 0.65f);
            b = Buffer(3.2f);
            float[] fan = { 293.66f, 392f, 440f, 587.33f };
            AddTaiko(b, 0f, 0.8f); AddTaiko(b, 0.2f, 0.6f); AddTaiko(b, 0.4f, 1f);
            for (int k = 0; k < fan.Length; k++) AddPluck(b, 0.4f + k * 0.12f, fan[k], 0.7f, 1.4f, 0.997f);
            AddFlute(b, 0.9f, 0.5f, 587.33f, 0.35f); AddFlute(b, 1.35f, 0.4f, 659.25f, 0.35f); AddFlute(b, 1.7f, 1.3f, 880f, 0.4f);
            AddPad(b, 0.4f, 2.8f, 146.8f, 0.3f); AddPad(b, 0.4f, 2.8f, 220f, 0.2f); AddTaiko(b, 1.7f, 1f);
            Reverb(b, 0.4f, 1.2f); Make("victory", b, 0.85f);
            b = Buffer(2.4f); AddFlute(b, 0f, 0.8f, 440f, 0.35f); AddFlute(b, 0.7f, 0.8f, 392f, 0.35f); AddFlute(b, 1.4f, 1f, 293.66f, 0.35f);
            AddPad(b, 0f, 2.3f, 146.8f, 0.25f); Reverb(b, 0.45f, 1.2f); Make("defeat", b, 0.7f);

            // Home ambience: wind, birds, leaves and a distant stream, all soft.
            b = Buffer(12f);
            AddNoise(b, 0f, 12f, 0.35f, 0f, 0.12f, 3f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.55f + 0.45f * Mathf.Sin((float)i / Rate * 0.5f + 1f);
            var stream = Buffer(12f);
            AddNoise(stream, 0f, 12f, 0.25f, 0f, 0.8f, 1f);
            Filter(stream, 2, 2200f, 0.6f);
            for (int k = 0; k < 90; k++) AddTone(stream, Random.Range(0f, 11.8f), 0.04f, Random.Range(700f, 1300f), Random.Range(1300f, 2000f), 0.05f, 60f);
            for (int i = 0; i < b.Length; i++) b[i] += stream[i] * 0.5f;
            for (int k = 0; k < 14; k++) AddSweep(b, Random.Range(0f, 11.5f), 0.4f, 1800f, 3500f, 1.5f, 0.12f, 8f, 0.1f);
            for (int k = 0; k < 8; k++) AddChirp(b, 0.6f + k * 1.4f + Random.Range(0f, 0.5f), Random.Range(2800f, 4200f), 0.08f, 2 + rng.Next(4));
            MakeLoopable(b); Make("amb_home", b, 0.4f);
        }

        /// <summary>An element-flavoured riser for the special build-up (1.4 s).</summary>
        void MakeBuild(string id, int kind)
        {
            var b = Buffer(1.5f);
            AddSweep(b, 0f, 1.4f, 150f, 2400f, 1.2f, 0.5f, 0.3f, 1f);
            AddTone(b, 0f, 1.4f, 80f, 160f, 0.35f, 0.3f, 0.8f);
            switch (kind)
            {
                case 0: for (int k = 0; k < 24; k++) { float t0 = k * 0.055f, f = 400f + k * 45f; AddTone(b, t0, 0.07f, f, f * 1.9f, 0.12f + k * 0.006f, 40f); } break;           // rising bubbles
                case 1: AddNoise(b, 0f, 1.4f, 0.5f, 0.2f, 0.3f, 1f); for (int k = 0; k < 40; k++) AddNoise(b, k * 0.034f, 0.012f, 0.3f + k * 0.012f, 200f, 1f); break;       // roaring, crackling
                case 2: for (int k = 0; k < 12; k++) { float t0 = k * 0.11f; AddTone(b, t0, 0.06f, 3200f, 600f, 0.1f + k * 0.012f, 40f, 0.001f, true); AddNoise(b, t0, 0.015f, 0.3f + k * 0.03f, 150f, 1f); } break; // gathering sparks
                case 3: AddSweep(b, 0f, 1.4f, 600f, 3200f, 6f, 0.35f, 0.2f, 1f); break;                                                                                      // whistling wind
                case 4: float[] bl = { 1046.5f, 1318.5f, 1568f, 2093f, 2637f }; for (int k = 0; k < 10; k++) AddFM(b, k * 0.13f, 0.5f, bl[k % 5], 3.5f, 0.6f, 0.08f + k * 0.01f, 6f); break; // bells
                default: for (int k = 0; k < 6; k++) { AddTone(b, k * 0.22f, 0.18f, 60f, 40f, 0.35f + k * 0.05f, 18f); AddTone(b, k * 0.22f + 0.16f, 0.14f, 55f, 38f, 0.25f + k * 0.04f, 18f); } Saturate(b, 1.5f); break; // heartbeat
            }
            Reverb(b, 0.3f, 1f);
            Make(id, b, 0.75f);
        }

        // ------------------------------------------------------------------ Home theme

        /// <summary>
        /// The home-screen theme (about 55 s, loops): a warm pad and koto arpeggios open it, a bamboo-flute melody
        /// carries the main motif, soft taiko and a low drum enter in the second half and the motif returns an
        /// octave up before it settles back. D minor pentatonic with a hopeful lift to F and C.
        /// </summary>
        AudioClip BuildHomeTheme()
        {
            const float bpm = 72f;
            float beat = 60f / bpm;
            int bars = 16;
            var b = Buffer(bars * 4 * beat);
            // Chords (root, fifth, octave third) for each bar: Dm Dm Bb C | Dm F C Dm, twice.
            float[][] chords =
            {
                new[] { 146.83f, 220f, 349.23f }, new[] { 146.83f, 220f, 293.66f }, new[] { 116.54f, 174.61f, 293.66f }, new[] { 130.81f, 196f, 329.63f },
                new[] { 146.83f, 220f, 349.23f }, new[] { 174.61f, 261.63f, 349.23f }, new[] { 130.81f, 196f, 329.63f }, new[] { 146.83f, 220f, 293.66f }
            };
            // The motif (in scale steps of D minor pentatonic: D F G A C D' F' G' A').
            float[] pent = { 293.66f, 349.23f, 392f, 440f, 523.25f, 587.33f, 698.46f, 783.99f, 880f };
            int[] motif = { 3, -1, 5, 4, 3, -1, 2, 1, 2, -1, 3, 1, 0, -1, -1, -1, 3, -1, 5, 6, 7, -1, 6, 5, 4, -1, 5, 3, 3, -1, -1, -1 };
            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                var ch = chords[bar % chords.Length];
                bool second = bar >= 8;
                foreach (var f in ch) AddPad(b, t0, 4 * beat + 0.6f, f, 0.14f);
                // Koto arpeggio in eighths.
                for (int k = 0; k < 8; k++)
                {
                    float f = ch[k % 3] * (k >= 4 ? 2f : 1f);
                    AddPluck(b, t0 + k * beat * 0.5f, f, k == 0 ? 0.34f : 0.22f, beat * 1.6f, 0.996f);
                }
                // Flute motif: two bars per half-phrase; an octave up in the second half.
                for (int k = 0; k < 2; k++)
                {
                    int idx = (bar * 2 + k) % motif.Length;
                    int note = motif[idx];
                    if (note < 0 || bar < 2) continue;
                    int len = 1;
                    while (len < 4 && motif[(idx + len) % motif.Length] < 0) len++;
                    float f = pent[Mathf.Min(note, pent.Length - 1)] * (second ? 2f : 1f);
                    if (second && f > 1400f) f *= 0.5f;
                    AddFlute(b, t0 + k * 2f * beat, len * 2f * beat * 0.95f, f, 0.26f);
                }
                if (second)
                {
                    AddTaiko(b, t0, 0.45f);
                    AddTaiko(b, t0 + 2.5f * beat, 0.3f);
                    AddTone(b, t0, 4 * beat, ch[0] * 0.5f, ch[0] * 0.5f, 0.12f, 0.6f, 0.3f);
                }
                if (bar % 4 == 3) AddFM(b, t0 + 3f * beat, 2.5f, pent[5] * 2f, 3.5f, 0.5f, 0.06f, 1.5f);
            }
            Reverb(b, 0.35f, 1.4f, 0.8f);
            MakeLoopable(b);
            Normalize(b, 0.55f);
            var clip = AudioClip.Create("home_theme", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }
    }
}
