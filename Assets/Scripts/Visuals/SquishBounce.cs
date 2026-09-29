using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Makes characters feel soft and squishy: a volume-keeping squash and stretch on the whole body (taller and
    /// thinner, or shorter and wider) driven by a little spring. It breathes at rest, hops with each step while
    /// walking, and jiggles when poked — squashing on a hit or a heavy swing, stretching into an attack — then
    /// wobbles back. Scales only the visual, never the gameplay body.
    /// </summary>
    public class SquishBounce : MonoBehaviour
    {
        CharacterVisual visual;
        float squish, vel, t, hopTimer = 3f;

        public void Init(CharacterVisual v) { visual = v; t = Random.Range(0f, 10f); }

        /// <summary>Kick the spring: negative squashes, positive stretches.</summary>
        public void Poke(float amount) { vel += amount * 9f; }

        void LateUpdate()
        {
            float dt = Mathf.Min(Time.deltaTime, 0.05f);
            if (dt <= 0f) return;
            t += dt;
            float moving = visual != null ? visual.MovingAmount : 0f;
            // Idle life: every few seconds a little happy hop or wiggle.
            if (moving < 0.1f)
            {
                hopTimer -= dt;
                if (hopTimer <= 0f)
                {
                    hopTimer = Random.Range(2.5f, 6f);
                    Poke(Random.value < 0.5f ? 0.1f : -0.07f);
                }
            }
            // Spring toward rest (stiff and underdamped: it wobbles).
            float acc = -squish * 260f - vel * 11f;
            vel += acc * dt;
            squish += vel * dt;
            squish = Mathf.Clamp(squish, -0.3f, 0.3f);
            // Breathing at rest and a hop per step when walking.
            float breathe = Mathf.Sin(t * 2.4f) * 0.022f * (1f - moving);
            float step = (Mathf.Abs(Mathf.Sin(t * 9f)) - 0.5f) * 0.07f * moving;
            float s = squish + breathe + step;
            float side = 1f / Mathf.Sqrt(Mathf.Max(0.5f, 1f + s));
            transform.localScale = new Vector3(side, 1f + s, side);
        }
    }
}
