using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Owns Time.timeScale: combines pause, slow-motion (ultimates, perfect dodges) and hit-stop.</summary>
    public static class TimeController
    {
        static float hitStopUntil;
        static float slowUntil;
        static float slowScale = 1f;

        public static bool Paused;

        public static void HitStop(float duration)
        {
            if (duration <= 0f) return;
            hitStopUntil = Mathf.Max(hitStopUntil, Time.unscaledTime + duration);
        }

        public static void SlowMotion(float scale, float duration)
        {
            if (Time.unscaledTime < slowUntil) scale = Mathf.Min(scale, slowScale);
            slowScale = scale;
            slowUntil = Mathf.Max(slowUntil, Time.unscaledTime + duration);
        }

        public static void Tick()
        {
            float s = 1f;
            if (Time.unscaledTime < slowUntil) s = slowScale;
            if (Time.unscaledTime < hitStopUntil) s = Mathf.Min(s, 0.04f);
            if (Paused) s = 0f;
            Time.timeScale = s;
        }

        public static void ResetAll()
        {
            hitStopUntil = 0f;
            slowUntil = 0f;
            slowScale = 1f;
            Paused = false;
            Time.timeScale = 1f;
        }
    }
}
