using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Gentle wind sway for trees, banners and grass.</summary>
    public class Sway : MonoBehaviour
    {
        public float Amount = 3f;
        public float Speed = 0.8f;
        Quaternion baseRot;
        float seed;

        void Start()
        {
            baseRot = transform.localRotation;
            seed = Random.value * 10f;
        }

        void Update()
        {
            float t = Time.time * Speed + seed;
            transform.localRotation = baseRot * Quaternion.Euler(Mathf.Sin(t) * Amount, 0f, Mathf.Sin(t * 0.7f + 1.3f) * Amount * 0.6f);
        }
    }
}
