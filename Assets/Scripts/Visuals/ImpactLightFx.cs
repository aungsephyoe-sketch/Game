using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Pooled flash of dynamic light for impacts, parries and ultimates. Capped by GameSettings.MaxDynamicLights.</summary>
    public class ImpactLightFx : MonoBehaviour
    {
        static readonly List<ImpactLightFx> all = new List<ImpactLightFx>();
        static Transform root;

        Light lightComp;
        float intensity, duration, t;

        public static void Spawn(Vector3 pos, Color color, float range, float duration)
        {
            int max = GameSettings.MaxDynamicLights;
            if (max <= 0) return;
            ImpactLightFx fx = null;
            int active = 0;
            foreach (var l in all)
            {
                if (l == null) continue;
                if (l.gameObject.activeSelf) active++;
                else if (fx == null) fx = l;
            }
            if (active >= max) return;
            if (fx == null)
            {
                if (root == null)
                {
                    var r = new GameObject("[ImpactLights]");
                    DontDestroyOnLoad(r);
                    root = r.transform;
                }
                var go = new GameObject("ImpactLight");
                go.transform.SetParent(root, false);
                fx = go.AddComponent<ImpactLightFx>();
                fx.lightComp = go.AddComponent<Light>();
                fx.lightComp.type = LightType.Point;
                fx.lightComp.shadows = LightShadows.None;
                fx.lightComp.renderMode = LightRenderMode.ForcePixel;
                all.Add(fx);
            }
            fx.transform.position = pos;
            fx.lightComp.color = color;
            fx.lightComp.range = range;
            fx.intensity = 3.5f;
            fx.duration = Mathf.Max(0.05f, duration);
            fx.t = 0f;
            fx.lightComp.intensity = fx.intensity;
            fx.gameObject.SetActive(true);
        }

        void Update()
        {
            t += Time.unscaledDeltaTime;
            float k = t / duration;
            lightComp.intensity = intensity * (1f - Mathf.Clamp01(k)) * (1f - Mathf.Clamp01(k));
            if (k >= 1f) gameObject.SetActive(false);
        }
    }
}
