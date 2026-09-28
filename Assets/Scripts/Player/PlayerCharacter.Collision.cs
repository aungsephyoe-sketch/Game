using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Collision-aware movement. Every way a slayer changes place (walking, sprinting, dodging, lunges, knockback,
    /// special-move dashes and blinks) goes through <see cref="MoveTo"/>, which sweeps a real
    /// <see cref="CharacterController"/> capsule through the physics world and slides along whatever it touches, so a
    /// fast step can't tunnel through a pole or clip through a wall. The analytic <see cref="Obstacles"/> pass then
    /// keeps the result inside the playable area and out of any solid the sweep grazed.
    /// </summary>
    public partial class PlayerCharacter
    {
        /// <summary>Capsule radius: the chibi body is widest at the head and shoulders, about 0.45 m either side.</summary>
        public const float BodyRadius = Obstacles.BodyRadius;
        public const float BodyHeight = 1.7f;

        CharacterController body;

        void EnsureBody()
        {
            if (body != null) return;
            body = GetComponent<CharacterController>();
            if (body == null) body = gameObject.AddComponent<CharacterController>();
            body.radius = BodyRadius;
            body.height = BodyHeight;
            body.center = new Vector3(0f, BodyHeight * 0.5f + 0.02f, 0f);
            body.skinWidth = 0.03f;
            body.minMoveDistance = 0f;
            // Slayers never climb onto scenery: a solid is a wall, whatever its height.
            body.stepOffset = 0f;
            body.slopeLimit = 0f;
            Obstacles.SetupCharacterLayer(gameObject);
        }

        /// <summary>
        /// Moves toward <paramref name="target"/> without passing through anything solid. The target's height is kept
        /// (hops and leaps), the ground position is swept.
        /// </summary>
        public void MoveTo(Vector3 target)
        {
            EnsureBody();
            Vector3 from = transform.position;
            Vector3 d = target - from;
            d.y = 0f;
            Vector3 p;
            if (body != null && body.enabled && Obstacles.PhysicsReady)
            {
                // Something may have placed us directly (spawn, cutscene): make the physics world agree first.
                transform.position = new Vector3(from.x, 0f, from.z);
                Physics.SyncTransforms();
                // A few sub-steps keep the slide along curved trunks and rocks smooth on long dashes.
                int n = Mathf.Clamp(Mathf.CeilToInt(d.magnitude / 0.35f), 1, 24);
                Vector3 step = d / n;
                for (int i = 0; i < n; i++) body.Move(step);
                p = transform.position;
            }
            else p = Obstacles.Sweep(from, from + d);
            p = BattleController.ClampToArena(p);
            p.y = target.y;
            transform.position = p;
        }
    }
}
