using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Soft clouds drifting across the sky and wrapping around.</summary>
    public class CloudDrift : MonoBehaviour
    {
        public float Speed = 1.2f;
        public float WrapX = 70f;
        Vector3 origin;

        void Start() { origin = transform.localPosition; }

        void Update()
        {
            var p = transform.localPosition;
            p.x += Speed * Time.deltaTime;
            if (p.x > origin.x + WrapX) p.x -= WrapX * 2f;
            transform.localPosition = p;
        }

        public static void CreateLayer(Transform parent, Vector3 center, int count, float height, Color color, float spread)
        {
            var mat = MaterialFactory.Transparent(color, true);
            for (int i = 0; i < count; i++)
            {
                var c = new GameObject("Cloud");
                c.transform.SetParent(parent, false);
                c.transform.localPosition = center + new Vector3(Random.Range(-spread, spread), height + Random.Range(-2f, 3f), Random.Range(-spread * 0.4f, spread));
                for (int k = 0; k < 4; k++)
                {
                    var puff = MeshFactory.MeshObject(MeshFactory.Disc(), c.transform, new Vector3(k * 2.2f - 3f, Random.Range(-0.5f, 0.8f), Random.Range(-0.5f, 0.5f)),
                        Vector3.one * Random.Range(2.5f, 4.5f), mat, false);
                    puff.transform.localRotation = Quaternion.Euler(-70f, 0f, 0f);
                }
                var d = c.AddComponent<CloudDrift>();
                d.Speed = Random.Range(0.6f, 1.6f);
                d.WrapX = spread;
            }
        }
    }
}
