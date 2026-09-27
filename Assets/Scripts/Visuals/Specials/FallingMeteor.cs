using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>A burning rock (or a glowing arrow) that falls from the sky with a trail and calls back on impact.</summary>
    public class FallingMeteor : MonoBehaviour
    {
        Vector3 from, to;
        float duration, t;
        System.Action<Vector3> onImpact;
        Transform spin;

        public static FallingMeteor Drop(Vector3 target, Color color, float size, float duration, bool arrow, System.Action<Vector3> onImpact)
        {
            var go = new GameObject(arrow ? "SkyArrow" : "Meteor");
            var m = go.AddComponent<FallingMeteor>();
            m.to = target;
            m.from = target + new Vector3(Random.Range(-3f, 3f), 14f + Random.Range(0f, 4f), Random.Range(3f, 7f));
            m.duration = duration;
            m.onImpact = onImpact;
            go.transform.position = m.from;
            go.transform.rotation = Quaternion.LookRotation(m.to - m.from);
            m.spin = new GameObject("Body").transform;
            m.spin.SetParent(go.transform, false);
            if (arrow)
            {
                var glow = MaterialFactory.Toon(Color.Lerp(color, Color.white, 0.4f), 0f, color);
                MeshFactory.Primitive(PrimitiveType.Cube, m.spin, Vector3.zero, new Vector3(0.06f, 0.06f, 1.4f) * size, glow);
                var head = MeshFactory.MeshObject(MeshFactory.Cone(), m.spin, new Vector3(0f, 0f, 0.75f) * size, new Vector3(0.2f, 0.35f, 0.2f) * size, glow);
                head.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            }
            else
            {
                var rock = MaterialFactory.Toon(new Color(0.3f, 0.18f, 0.12f), 0.02f, color * 0.6f);
                MeshFactory.MeshObject(MeshFactory.Rock(Random.Range(0, 8)), m.spin, Vector3.down * 0.5f * size, Vector3.one * size, rock);
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), m.spin, Vector3.zero, Vector3.one * 1.8f * size, MaterialFactory.Additive(new Color(color.r, color.g, color.b, 0.45f)), false);
            }
            var tr = go.AddComponent<TrailRenderer>();
            tr.time = arrow ? 0.12f : 0.35f;
            tr.startWidth = (arrow ? 0.15f : 1.2f) * size;
            tr.endWidth = 0f;
            tr.material = MaterialFactory.Additive(Color.white);
            tr.startColor = new Color(1f, 0.95f, 0.8f, 0.9f);
            tr.endColor = new Color(color.r, color.g, color.b, 0f);
            tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return m;
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            transform.position = Vector3.Lerp(from, to, k * k);
            spin.Rotate(200f * Time.deltaTime, 0f, 90f * Time.deltaTime);
            if (k >= 1f)
            {
                if (onImpact != null) onImpact(to);
                Destroy(gameObject);
            }
        }
    }
}
