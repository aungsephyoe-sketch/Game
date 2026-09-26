using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>2.5D angled follow camera with trauma-based shake and cinematic zoom for ultimates.</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 11f, -8.5f);

        float trauma;
        float zoom = 1f, zoomTarget = 1f;
        float zoomSpeed = 4f;
        Vector3 focus;
        Vector3 fixedPosition;
        Vector3 fixedLookAt;
        bool fixedMode;

        public Camera Cam { get; private set; }

        void Awake()
        {
            Instance = this;
            Cam = GetComponent<Camera>();
        }

        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        /// <summary>1 = normal, &lt;1 = closer. Uses unscaled time so it works during slow motion.</summary>
        public void SetZoom(float value, float speed = 4f)
        {
            zoomTarget = value;
            zoomSpeed = speed;
        }

        public void Follow(Transform target, bool snap)
        {
            fixedMode = false;
            Target = target;
            if (snap && target != null)
            {
                focus = target.position;
                zoom = zoomTarget = 1f;
                Place(0f);
            }
        }

        public void SetFixed(Vector3 position, Vector3 lookAt)
        {
            fixedMode = true;
            fixedPosition = position;
            fixedLookAt = lookAt;
            Target = null;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            if (fixedMode)
            {
                transform.position = Vector3.Lerp(transform.position, fixedPosition, 1f - Mathf.Exp(-dt * 5f));
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(fixedLookAt - transform.position), 1f - Mathf.Exp(-dt * 5f));
                return;
            }
            if (Target != null) focus = Vector3.Lerp(focus, Target.position, 1f - Mathf.Exp(-dt * 10f));
            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-dt * zoomSpeed));
            Place(dt);
        }

        void Place(float dt)
        {
            Vector3 pos = focus + Offset * zoom;
            Vector3 look = focus + Vector3.up * 1f;
            trauma = Mathf.Max(0f, trauma - dt * 1.8f);
            float shake = trauma * trauma;
            if (shake > 0f)
            {
                float t = Time.unscaledTime * 30f;
                pos += new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * shake * 1.2f;
            }
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos);
        }
    }
}
