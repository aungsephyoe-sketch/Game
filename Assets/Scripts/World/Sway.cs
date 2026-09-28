using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Gentle swaying (foliage, banners, lanterns, capes, scarves, tassels). On top of the idle breeze it is
    /// inertial: when whatever carries it moves or turns, it lags behind and springs back, so cloth trails a
    /// running slayer and settles when they stop. Static scenery never moves, so it only breathes.
    /// </summary>
    public class Sway : MonoBehaviour
    {
        public float Amount = 3f;
        public float Speed = 0.8f;
        /// <summary>How strongly movement swings it (degrees per metre per second).</summary>
        public float Inertia = 3.5f;
        Quaternion baseRot;
        float seed;
        Vector3 lastPos, lastFwd;
        Vector2 lag, lagVel;
        bool ready;

        void Start()
        {
            baseRot = transform.localRotation;
            seed = Random.value * 10f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            float t = Time.time * Speed + seed;
            float ix = Mathf.Sin(t) * Amount, iz = Mathf.Sin(t * 0.7f + 1.3f) * Amount * 0.6f;
            var parent = transform.parent;
            if (dt > 0f && parent != null)
            {
                Vector3 pos = transform.position;
                Vector3 fwd = parent.forward;
                if (!ready) { lastPos = pos; lastFwd = fwd; ready = true; }
                Vector3 delta = pos - lastPos;
                float turn = Vector3.SignedAngle(lastFwd, fwd, Vector3.up);
                lastPos = pos;
                lastFwd = fwd;
                if (delta.sqrMagnitude > 1e-8f || Mathf.Abs(turn) > 0.01f || lag.sqrMagnitude > 0.0001f || lagVel.sqrMagnitude > 0.0001f)
                {
                    Vector3 lv = parent.InverseTransformDirection(delta / dt);
                    float max = 10f + Amount * 3f;
                    Vector2 target = new Vector2(Mathf.Clamp(lv.z * Inertia, -max, max), Mathf.Clamp(-lv.x * Inertia - turn / dt * 0.02f * Inertia, -max, max));
                    // A soft spring: trails the motion, overshoots a touch, settles.
                    lagVel += ((target - lag) * 40f - lagVel * 7f) * dt;
                    lag += lagVel * dt;
                }
            }
            transform.localRotation = baseRot * Quaternion.Euler(ix + lag.x, 0f, iz + lag.y);
        }
    }
}
