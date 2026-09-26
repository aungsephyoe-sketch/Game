using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Cinematic lighting moods: during ultimates the world dims and the key light takes the element's colour,
    /// then everything eases back. Driven by a tiny runner so it works during slow motion.
    /// </summary>
    public static class SceneLighting
    {
        static Runner runner;

        class Runner : MonoBehaviour
        {
            public Light sun;
            public float baseIntensity;
            public Color baseColor;
            public Color baseAmbient;
            public Color moodColor;
            public float until;
            public float blend;
            public bool captured;

            void Update()
            {
                if (sun == null) return;
                bool active = Time.unscaledTime < until;
                blend = Mathf.MoveTowards(blend, active ? 1f : 0f, Time.unscaledDeltaTime * (active ? 4f : 1.5f));
                if (!captured) return;
                sun.intensity = Mathf.Lerp(baseIntensity, baseIntensity * 0.45f, blend);
                sun.color = Color.Lerp(baseColor, Color.Lerp(baseColor, moodColor, 0.6f), blend);
                RenderSettings.ambientLight = Color.Lerp(baseAmbient, baseAmbient * 0.4f + moodColor * 0.15f, blend);
                if (!active && blend <= 0f) captured = false;
            }
        }

        static Runner Get()
        {
            if (runner == null)
            {
                var go = new GameObject("[SceneLighting]");
                Object.DontDestroyOnLoad(go);
                runner = go.AddComponent<Runner>();
            }
            return runner;
        }

        public static void UltimateMood(Color color, float duration)
        {
            var r = Get();
            if (!r.captured)
            {
                r.sun = RenderSettings.sun;
                if (r.sun == null) return;
                r.baseIntensity = r.sun.intensity;
                r.baseColor = r.sun.color;
                r.baseAmbient = RenderSettings.ambientLight;
                r.captured = true;
            }
            r.moodColor = color;
            r.until = Time.unscaledTime + duration;
        }
    }
}
