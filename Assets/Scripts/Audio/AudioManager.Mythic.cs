using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Mythic sound set, layered for weight and space: a deep double heartbeat, a rising choir-like chord swell with
    /// a shimmer climbing over it, a reverse "sucked-in" whoosh, and a boom with a sub drop you feel, a glassy ringing
    /// tail and falling debris in a big hall reverb.
    /// </summary>
    public partial class AudioManager
    {
        void GenerateMythicPass()
        {
            float[] b;
            // Heartbeat: two deep thumps.
            b = Buffer(0.8f);
            AddTone(b, 0f, 0.3f, 62f, 38f, 1f, 11f, 0.004f); AddNoise(b, 0f, 0.02f, 0.4f, 90f, 0.2f);
            AddTone(b, 0.26f, 0.3f, 56f, 34f, 0.8f, 11f, 0.004f); AddNoise(b, 0.26f, 0.02f, 0.3f, 90f, 0.2f);
            Saturate(b, 1.6f); Reverb(b, 0.35f, 1.2f); Make("myth_heart", b, 1f);
            // Rise: a slow-blooming chord, a climbing shimmer and a sub that swells up under it.
            b = Buffer(2f);
            float[] chord = { 220f, 277.2f, 329.6f, 440f, 554.4f };
            for (int i = 0; i < chord.Length; i++) AddFM(b, 0f, 1.85f, chord[i], 1.002f, 0.35f, 0.11f, 0.6f, 0.9f);
            AddSweep(b, 0.1f, 1.7f, 500f, 7000f, 2.2f, 0.45f, 0.4f, 1.2f);
            AddTone(b, 0f, 1.8f, 38f, 85f, 0.55f, 0.4f, 1.1f);
            Saturate(b, 1.2f); Reverb(b, 0.55f, 1.7f, 0.84f); Make("myth_rise", b, 0.9f);
            // Reverse whoosh: air sucked in, snapping shut.
            b = Buffer(0.75f);
            AddSweep(b, 0f, 0.66f, 180f, 3600f, 1.3f, 1f, 0.05f, 0.62f);
            AddNoise(b, 0f, 0.66f, 0.5f, 0.1f, 0.6f, 0.6f);
            AddNoise(b, 0.64f, 0.03f, 1f, 150f, 1f, 0.0005f);
            Saturate(b, 1.3f); Make("myth_whoosh", b, 0.85f);
            // Boom: crack, sub drop, body, ringing glass tail and debris, in a big hall.
            b = Buffer(3.2f);
            AddNoise(b, 0f, 0.03f, 1f, 80f, 1f, 0.0005f);
            AddTone(b, 0f, 2.4f, 64f, 19f, 1f, 1.5f);
            AddTone(b, 0f, 1.5f, 125f, 44f, 0.7f, 2.4f);
            AddFM(b, 0.02f, 2.8f, 2093f, 2.76f, 1.2f, 0.18f, 1.4f);
            AddFM(b, 0.05f, 2.8f, 3520f, 1.41f, 0.8f, 0.1f, 1.7f);
            AddNoise(b, 0.02f, 1.6f, 0.6f, 2.2f, 0.3f, 0.01f);
            for (int i = 0; i < 8; i++) AddNoise(b, 0.12f + i * 0.09f, 0.05f, 0.22f, 60f, 0.7f);
            Saturate(b, 2f); Reverb(b, 0.55f, 1.9f, 0.86f); Make("myth_boom", b, 1f);
        }
    }
}
