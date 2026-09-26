using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Scales and fades a one-shot effect, then destroys it.</summary>
    public class FlashFx : MonoBehaviour
    {
        Material mat;
        Color color;
        Vector3 from, to;
        float duration, t;

        public void Setup(Material m, Color c, Vector3 fromScale, Vector3 toScale, float dur)
        {
            mat = m;
            color = c;
            from = fromScale;
            to = toScale;
            duration = Mathf.Max(0.01f, dur);
        }

        void Update()
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / duration);
            float ease = 1f - (1f - k) * (1f - k);
            transform.localScale = Vector3.LerpUnclamped(from, to, ease);
            if (mat != null)
            {
                var c = color;
                c.a = color.a * (1f - k * k);
                mat.color = c;
            }
            if (k >= 1f) Destroy(gameObject);
        }

        void OnDestroy()
        {
            if (mat != null) Destroy(mat);
        }
    }
}
