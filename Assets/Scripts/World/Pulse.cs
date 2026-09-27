using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Gently breathes an object's scale in and out (glows, auras, beacons).</summary>
    public class Pulse : MonoBehaviour
    {
        public float Speed = 2f;
        public float Amount = 0.12f;
        Vector3 baseScale;
        float phase;

        void Start()
        {
            baseScale = transform.localScale;
            phase = Random.Range(0f, 10f);
        }

        void Update()
        {
            transform.localScale = baseScale * (1f + Mathf.Sin(Time.time * Speed + phase) * Amount);
        }
    }
}
