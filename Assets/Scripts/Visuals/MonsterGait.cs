using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Swings a creature's legs from the hip while it moves (beasts, the lava lion, walking trees).</summary>
    public class MonsterGait : MonoBehaviour
    {
        public CharacterVisual cv;
        public Transform[] legs;
        public float[] phases;
        float phase, move;

        void LateUpdate()
        {
            if (cv == null || legs == null) return;
            float dt = Time.deltaTime;
            move = Mathf.Lerp(move, cv.IsDead ? 0f : cv.MoveAmount, 1f - Mathf.Exp(-dt * 8f));
            float amp = 32f * Mathf.Clamp01(move * 1.5f) * (cv.IsSprinting ? 1.25f : 1f);
            phase += dt * (6f + 6f * move);
            for (int i = 0; i < legs.Length; i++)
            {
                if (legs[i] == null) continue;
                float p = phase + (phases != null && i < phases.Length ? phases[i] : 0f);
                // A little idle weight shift so they never look frozen.
                float idle = Mathf.Sin(Time.time * 1.3f + i) * 2f;
                legs[i].localRotation = Quaternion.Euler(Mathf.Sin(p) * amp + idle, 0f, 0f);
            }
        }
    }
}
