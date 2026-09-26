using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Gentle candle-like flicker for lantern point lights.</summary>
    [RequireComponent(typeof(Light))]
    public class LanternFlicker : MonoBehaviour
    {
        Light lightComp;
        float baseIntensity;
        float seed;

        void Start()
        {
            lightComp = GetComponent<Light>();
            baseIntensity = lightComp.intensity;
            seed = Random.value * 100f;
        }

        void Update()
        {
            float n = Mathf.PerlinNoise(seed, Time.time * 3f);
            lightComp.intensity = baseIntensity * (0.8f + 0.4f * n);
        }
    }
}
