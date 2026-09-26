using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// 2.5D action camera:
    ///  • follows the player smoothly and leans toward nearby demons / the boss so fights stay framed
    ///  • zoom punches on heavy impacts, trauma-based shake (scaled by the player's shake setting)
    ///  • ultimate cinematic: low orbiting side angle close to the slayer, then eases back to gameplay
    /// All motion uses unscaled time so it stays smooth through hit-stop and slow motion.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        public static CameraController Instance { get; private set; }

        public Transform Target;
        public Vector3 Offset = new Vector3(0f, 11f, -8.5f);

        float trauma;
        float zoom = 1f, zoomTarget = 1f;
        float zoomSpeed = 4f;
        float punch, punchTime, punchDuration;
        Vector3 focus;
        Vector3 framingOffset;
        Vector3 fixedPosition;
        Vector3 fixedLookAt;
        bool fixedMode;

        // Cinematic state.
        Transform cineTarget;
        float cineStart, cineDuration;
        float cineBlend; // 0 = gameplay, 1 = cinematic
        float cineYaw;

        public Camera Cam { get; private set; }

        void Awake()
        {
            Instance = this;
            Cam = GetComponent<Camera>();
        }

        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount * GameSettings.ShakeIntensity);
        }

        /// <summary>Quick zoom-in that springs back – sells heavy hits and parries.</summary>
        public void Punch(float amount, float duration)
        {
            punch = Mathf.Max(punch, amount * GameSettings.ShakeIntensity);
            punchTime = Time.unscaledTime;
            punchDuration = Mathf.Max(0.05f, duration);
        }

        /// <summary>1 = normal, &lt;1 = closer.</summary>
        public void SetZoom(float value, float speed = 4f)
        {
            zoomTarget = value;
            zoomSpeed = speed;
        }

        public void PlayUltimateCinematic(Transform target, float duration)
        {
            cineTarget = target;
            cineStart = Time.unscaledTime;
            cineDuration = duration;
            // Pick the side that keeps the most enemies in frame behind the slayer.
            cineYaw = target != null && Vector3.Dot(target.right, Vector3.right) >= 0f ? 60f : -60f;
        }

        public void EndCinematic()
        {
            cineTarget = null;
        }

        public void Follow(Transform target, bool snap)
        {
            fixedMode = false;
            Target = target;
            if (snap && target != null)
            {
                focus = target.position;
                framingOffset = Vector3.zero;
                zoom = zoomTarget = 1f;
                cineBlend = 0f;
                cineTarget = null;
                Place(0f);
            }
        }

        public void SetFixed(Vector3 position, Vector3 lookAt)
        {
            fixedMode = true;
            fixedPosition = position;
            fixedLookAt = lookAt;
            Target = null;
            cineTarget = null;
            cineBlend = 0f;
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
            if (Target != null)
            {
                focus = Vector3.Lerp(focus, Target.position, 1f - Mathf.Exp(-dt * 10f));
                framingOffset = Vector3.Lerp(framingOffset, ComputeFraming(), 1f - Mathf.Exp(-dt * 2.5f));
            }
            zoom = Mathf.Lerp(zoom, zoomTarget, 1f - Mathf.Exp(-dt * zoomSpeed));

            bool cineActive = cineTarget != null && Time.unscaledTime - cineStart < cineDuration + 0.6f;
            cineBlend = Mathf.MoveTowards(cineBlend, cineActive ? 1f : 0f, dt * (cineActive ? 5f : 2.2f));
            Place(dt);
        }

        /// <summary>Leans the view toward the centre of nearby enemies (weighted) so fights are framed.</summary>
        Vector3 ComputeFraming()
        {
            Vector3 sum = Vector3.zero;
            float weight = 0f;
            foreach (var c in Combatant.All)
            {
                if (c.Team != CombatTeam.Enemy || !c.IsAlive) continue;
                Vector3 d = c.Position - focus;
                d.y = 0f;
                float dist = d.magnitude;
                if (dist > 12f) continue;
                float w = (c is BossController) ? 3f : 1f;
                w *= 1f - dist / 12f;
                sum += d * w;
                weight += w;
            }
            if (weight <= 0f) return Vector3.zero;
            Vector3 offset = sum / weight * 0.35f;
            return Vector3.ClampMagnitude(offset, 3.5f);
        }

        void Place(float dt)
        {
            float p = 0f;
            if (punch > 0f)
            {
                float k = (Time.unscaledTime - punchTime) / punchDuration;
                if (k >= 1f) punch = 0f;
                else p = punch * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * 0.18f;
            }

            Vector3 center = focus + framingOffset;
            Vector3 pos = center + Offset * zoom * (1f - p);
            Vector3 look = center + Vector3.up * 1f;

            if (cineBlend > 0.001f && cineTarget != null)
            {
                // Low three-quarter angle that slowly orbits the slayer.
                float t = Time.unscaledTime - cineStart;
                float yaw = cineTarget.eulerAngles.y + cineYaw + t * 25f;
                Vector3 dir = Quaternion.Euler(12f, yaw, 0f) * Vector3.back;
                Vector3 cinePos = cineTarget.position + Vector3.up * 1.4f + dir * 4.2f;
                Vector3 cineLook = cineTarget.position + Vector3.up * 1.2f;
                float b = Mathf.SmoothStep(0f, 1f, cineBlend);
                pos = Vector3.Lerp(pos, cinePos, b);
                look = Vector3.Lerp(look, cineLook, b);
            }

            trauma = Mathf.Max(0f, trauma - dt * 1.8f);
            float shake = trauma * trauma;
            if (shake > 0f)
            {
                float t = Time.unscaledTime * 30f;
                pos += new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, Mathf.PerlinNoise(t, t) - 0.5f) * shake * 1.2f;
            }
            // Never dip below the ground plane.
            if (pos.y < 0.6f) pos.y = 0.6f;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(look - pos);
        }
    }
}
