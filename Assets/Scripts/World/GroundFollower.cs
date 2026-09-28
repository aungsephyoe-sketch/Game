using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Character ground placement and recovery, on every fighter (slayers, party AI, demons, bosses).
    ///
    /// Gameplay code moves characters on a flat plane where y is "height above the ground" (0 when standing, the arc
    /// height mid-leap). This component keeps that contract and puts the body on the real surface:
    ///   • Before any gameplay runs each frame (execution order −1000) it takes the ground height back out, so every
    ///     movement script sees the flat plane it was written for.
    ///   • After gameplay (LateUpdate, still before the camera) it adds the height of the drawn ground under the
    ///     character's feet — terrain or bridge deck (<see cref="Ground"/>) — so feet sit on the surface on slopes,
    ///     banks, dips and platforms, whatever the map. Heights change smoothly, never by a fixed offset.
    ///
    /// Recovery (checked a few times a second, and only acts when something is actually wrong):
    ///   • position not a number, or flung far outside the map → put back at the last good spot;
    ///   • fallen below the plane (negative height) → back on the ground, vertical motion dropped;
    ///   • standing inside something solid or outside the walkable area → eased out to the nearest valid ground
    ///     over a few frames (a hard snap only if it's badly stuck).
    /// Health, state and everything else are left alone.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public class GroundFollower : MonoBehaviour
    {
        float applied;
        float checkTimer;
        Vector3 lastGood;
        bool hasGood;
        Vector3? easeTo;

        /// <summary>How far above the ground the feet are right now (0 = standing).</summary>
        public float Airborne { get; private set; }

        public static void Ensure(Component c)
        {
            if (c != null && c.GetComponent<GroundFollower>() == null) c.gameObject.AddComponent<GroundFollower>();
        }

        void OnEnable() { applied = 0f; }

        void Update()
        {
            // Back to the flat gameplay plane.
            if (applied != 0f)
            {
                var p = transform.position;
                p.y -= applied;
                transform.position = p;
                applied = 0f;
            }
        }

        void LateUpdate()
        {
            var p = transform.position;
            float dt = Time.deltaTime;

            // ---- Recovery.
            if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsNaN(p.z) || float.IsInfinity(p.x) || float.IsInfinity(p.z))
            {
                p = hasGood ? lastGood : Vector3.zero;
                easeTo = null;
            }
            if (p.y < -0.01f) p.y = 0f; // below the ground plane: stand back up on it
            if (p.y > 60f) p.y = 0f;    // flung into the sky by some bad maths
            checkTimer -= dt;
            if (checkTimer <= 0f && BattleController.Current != null)
            {
                checkTimer = 0.2f;
                var valid = BattleController.ClampToArena(p);
                float off = new Vector2(valid.x - p.x, valid.z - p.z).magnitude;
                if (off > 25f) { p.x = hasGood ? lastGood.x : valid.x; p.z = hasGood ? lastGood.z : valid.z; easeTo = null; }
                else if (off > 0.05f) easeTo = valid;
                else { easeTo = null; lastGood = new Vector3(p.x, 0f, p.z); hasGood = true; }
            }
            if (easeTo.HasValue)
            {
                // Out of the solid / back onto walkable ground, smoothly.
                Vector3 e = easeTo.Value;
                Vector3 flat = Vector3.MoveTowards(new Vector3(p.x, 0f, p.z), new Vector3(e.x, 0f, e.z), dt * 8f);
                p.x = flat.x;
                p.z = flat.z;
                if (new Vector2(e.x - p.x, e.z - p.z).sqrMagnitude < 0.0004f) easeTo = null;
            }

            // ---- Onto the real ground.
            Airborne = p.y;
            float g = Ground.HeightAt(p.x, p.z);
            p.y += g;
            applied = g;
            transform.position = p;
        }
    }
}
