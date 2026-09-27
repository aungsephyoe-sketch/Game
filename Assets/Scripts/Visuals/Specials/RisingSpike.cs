using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>A stone (or ice / crystal) spike that bursts out of the ground, holds, then sinks back.</summary>
    public class RisingSpike : MonoBehaviour
    {
        float t, height, hold;
        Vector3 basePos;

        public static void Burst(Vector3 at, Color color, float height, float hold = 0.6f)
        {
            var go = new GameObject("Spike");
            var s = go.AddComponent<RisingSpike>();
            s.basePos = new Vector3(at.x, 0f, at.z);
            s.height = height;
            s.hold = hold;
            go.transform.position = s.basePos - Vector3.up * height;
            go.transform.rotation = Quaternion.Euler(Random.Range(-12f, 12f), Random.Range(0f, 360f), Random.Range(-12f, 12f));
            var mat = MaterialFactory.Toon(color, 0.02f, color * 0.2f);
            MeshFactory.MeshObject(MeshFactory.FacetCone(5), go.transform, Vector3.zero, new Vector3(0.8f, height, 0.8f) * (0.8f + height * 0.1f), mat);
            VFX.Dust(s.basePos, 5);
        }

        void Update()
        {
            t += Time.deltaTime;
            float y;
            if (t < 0.12f) y = Mathf.Lerp(-height, 0f, t / 0.12f);
            else if (t < 0.12f + hold) y = 0f;
            else y = Mathf.Lerp(0f, -height, (t - 0.12f - hold) / 0.3f);
            transform.position = basePos + Vector3.up * y;
            if (t > hold + 0.45f) Destroy(gameObject);
        }
    }
}
