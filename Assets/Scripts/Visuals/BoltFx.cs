using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A pooled, flickering lightning bolt: a jagged line between two points that re-forks every few
    /// frames and thins out as it fades. Used by thunder slashes, strikes and hits.
    /// </summary>
    public class BoltFx : MonoBehaviour
    {
        static readonly List<BoltFx> pool = new List<BoltFx>();
        static Transform poolRoot;
        static Material mat;

        LineRenderer line;
        Vector3 a, b;
        Color color;
        float width, duration, age, nextJitter, jag;
        int segments;

        public static void Strike(Vector3 from, Vector3 to, Color color, float width, float duration, float jaggedness = 0.35f)
        {
            if (poolRoot == null)
            {
                var go = new GameObject("[Bolts]");
                Object.DontDestroyOnLoad(go);
                poolRoot = go.transform;
            }
            BoltFx fx = null;
            for (int i = 0; i < pool.Count; i++)
                if (pool[i] != null && !pool[i].gameObject.activeSelf) { fx = pool[i]; break; }
            if (fx == null)
            {
                if (pool.Count >= 48) return;
                var go = new GameObject("Bolt");
                go.transform.SetParent(poolRoot, false);
                fx = go.AddComponent<BoltFx>();
                fx.line = go.AddComponent<LineRenderer>();
                if (mat == null) mat = MaterialFactory.Additive(Color.white);
                fx.line.sharedMaterial = mat;
                fx.line.useWorldSpace = true;
                fx.line.numCapVertices = 2;
                fx.line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                fx.line.receiveShadows = false;
                pool.Add(fx);
            }
            fx.a = from;
            fx.b = to;
            fx.color = color;
            fx.width = width;
            fx.duration = Mathf.Max(0.05f, duration);
            fx.age = 0f;
            fx.jag = jaggedness;
            fx.segments = Mathf.Clamp(Mathf.RoundToInt(Vector3.Distance(from, to) * 3f), 5, 22);
            fx.gameObject.SetActive(true);
            fx.Jitter();
        }

        void Jitter()
        {
            line.positionCount = segments + 1;
            Vector3 dir = b - a;
            Vector3 side = Vector3.Cross(dir.normalized, Vector3.up);
            if (side.sqrMagnitude < 0.01f) side = Vector3.right;
            Vector3 up2 = Vector3.Cross(side, dir.normalized);
            float len = dir.magnitude;
            for (int i = 0; i <= segments; i++)
            {
                float t = (float)i / segments;
                float k = Mathf.Sin(t * Mathf.PI); // pinned at both ends
                Vector3 off = (side * Random.Range(-1f, 1f) + up2 * Random.Range(-1f, 1f)) * jag * len * 0.12f * k;
                line.SetPosition(i, a + dir * t + off);
            }
            nextJitter = age + 0.035f;
        }

        void Update()
        {
            age += Time.unscaledDeltaTime;
            if (age >= duration) { gameObject.SetActive(false); return; }
            if (age >= nextJitter) Jitter();
            float k = 1f - age / duration;
            // Flicker: the bolt briefly blinks out and back.
            float flicker = Random.value < 0.15f ? 0.2f : 1f;
            line.widthMultiplier = width * (0.4f + 0.6f * k);
            Color core = Color.Lerp(color, Color.white, 0.55f);
            line.startColor = new Color(core.r, core.g, core.b, k * flicker);
            line.endColor = new Color(color.r, color.g, color.b, k * flicker * 0.8f);
        }
    }
}
