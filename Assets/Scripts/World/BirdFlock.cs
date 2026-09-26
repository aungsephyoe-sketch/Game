using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>A small flock of birds circling in the sky with flapping wings.</summary>
    public class BirdFlock : MonoBehaviour
    {
        Transform[] birds;
        Transform[] wingsL, wingsR;
        float[] phase;
        public float Radius = 18f;
        public float Height = 14f;
        public float Speed = 0.25f;

        public static BirdFlock Create(Transform parent, Vector3 center, int count, Color color)
        {
            var go = new GameObject("Birds");
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            var f = go.AddComponent<BirdFlock>();
            var mat = MaterialFactory.Toon(color, 0f);
            f.birds = new Transform[count];
            f.wingsL = new Transform[count];
            f.wingsR = new Transform[count];
            f.phase = new float[count];
            for (int i = 0; i < count; i++)
            {
                var b = new GameObject("Bird").transform;
                b.SetParent(go.transform, false);
                MeshFactory.Primitive(PrimitiveType.Sphere, b, Vector3.zero, new Vector3(0.18f, 0.14f, 0.4f), mat).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var l = new GameObject("WingL").transform;
                l.SetParent(b, false);
                MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(-0.3f, 0f, 0f), new Vector3(0.6f, 0.02f, 0.22f), mat).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var r = new GameObject("WingR").transform;
                r.SetParent(b, false);
                MeshFactory.Primitive(PrimitiveType.Cube, r, new Vector3(0.3f, 0f, 0f), new Vector3(0.6f, 0.02f, 0.22f), mat).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                f.birds[i] = b;
                f.wingsL[i] = l;
                f.wingsR[i] = r;
                f.phase[i] = i * 0.35f + Random.value * 0.2f;
            }
            return f;
        }

        void Update()
        {
            float t = Time.time;
            for (int i = 0; i < birds.Length; i++)
            {
                float a = t * Speed + phase[i];
                float r = Radius + Mathf.Sin(a * 3f + i) * 2f;
                var pos = new Vector3(Mathf.Cos(a) * r, Height + Mathf.Sin(a * 2f + i) * 1.5f, Mathf.Sin(a) * r);
                var next = new Vector3(Mathf.Cos(a + 0.05f) * r, pos.y, Mathf.Sin(a + 0.05f) * r);
                birds[i].localPosition = pos;
                birds[i].localRotation = Quaternion.LookRotation(next - pos);
                float flap = Mathf.Sin(t * 12f + i) * 35f;
                wingsL[i].localRotation = Quaternion.Euler(0f, 0f, flap);
                wingsR[i].localRotation = Quaternion.Euler(0f, 0f, -flap);
            }
        }
    }
}
