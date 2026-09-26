using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Continuous rotation + optional bobbing (crystals, magic circles, floating stones).</summary>
    public class Spinner : MonoBehaviour
    {
        public Vector3 DegreesPerSecond = new Vector3(0f, 40f, 0f);
        public float BobHeight;
        public float BobSpeed = 1.5f;
        public bool Unscaled;
        Vector3 basePos;
        float seed;

        void Start()
        {
            basePos = transform.localPosition;
            seed = Random.value * 10f;
        }

        void Update()
        {
            float dt = Unscaled ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Unscaled ? Time.unscaledTime : Time.time;
            transform.Rotate(DegreesPerSecond * dt, Space.Self);
            if (BobHeight > 0f) transform.localPosition = basePos + Vector3.up * Mathf.Sin(t * BobSpeed + seed) * BobHeight;
        }
    }
}
