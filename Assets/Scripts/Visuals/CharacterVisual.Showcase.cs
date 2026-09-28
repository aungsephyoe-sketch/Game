using System.Collections;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The TEAM SCREEN state: how a slayer stands on the roster podium. It is its own animation state, separate from
    /// the combat idle, attacks, specials, victory and defeat. The body stands upright and faces forward, feet flat
    /// on the podium; the rig (<see cref="PremiumRig"/>) relaxes the arms and carries the weapon naturally for its
    /// kind; this routine adds the life — breathing, a slow weight shift, the odd glance and head tilt, blinking.
    /// Personality shows only in small touches (head angle, which leg takes the weight), never in an awkward pose.
    /// </summary>
    public partial class CharacterVisual
    {
        bool showcase;

        /// <summary>True while standing in the team-screen idle.</summary>
        public bool Showcase { get { return showcase && posing; } }

        /// <summary>Settle into the relaxed team-screen stance and hold it.</summary>
        public void HoldTeamIdle()
        {
            if (driver != null || dead) return;
            if (rig == null) { HoldSignaturePose(); return; }
            StartPose(TeamIdleRoutine());
        }

        /// <summary>Snap straight into the team-screen stance (portraits).</summary>
        public void ApplyTeamIdle()
        {
            if (rig == null) { ApplySignaturePose(); return; }
            Model.localRotation = Quaternion.identity;
            Model.localPosition = Vector3.zero;
            Vector3 he = TeamHead();
            if (head != null) head.localRotation = Quaternion.Euler(he);
            bool wasPosing = posing, wasShow = showcase;
            showcase = true;
            posing = true;
            rig.Solve(0f);
            posing = wasPosing;
            showcase = wasShow;
        }

        /// <summary>A small personal head angle (pitch, yaw, tilt): the only place personality shows in the stance.</summary>
        Vector3 TeamHead()
        {
            switch (Motion)
            {
                case MotionStyle.Confident: return new Vector3(-4f, 3f, 0f);
                case MotionStyle.Nervous: return new Vector3(3f, -4f, 3f);
                case MotionStyle.Graceful: return new Vector3(0f, 0f, 5f);
                case MotionStyle.Sly: return new Vector3(1f, -5f, -4f);
                case MotionStyle.Light: return new Vector3(-1f, 4f, -4f);
                case MotionStyle.Aggressive: return new Vector3(2f, 0f, 0f);
                default: return Vector3.zero;
            }
        }

        IEnumerator TeamIdleRoutine()
        {
            showcase = true;
            Vector3 he = TeamHead();
            float t = Random.Range(0f, 10f);
            float glance = 0f, glanceTarget = 0f, nextGlance = Random.Range(2.5f, 5f);
            while (true)
            {
                float dt = Time.deltaTime;
                t += dt;
                float k = 1f - Mathf.Exp(-dt * 6f);
                // Upright, feet on the podium: the whole body stays put; the rig breathes and shifts the weight.
                Model.localRotation = Quaternion.Slerp(Model.localRotation, Quaternion.identity, k);
                Model.localPosition = Vector3.Lerp(Model.localPosition, Vector3.zero, k);
                Model.localScale = Vector3.one * baseScale;
                // Every few seconds, a small glance to one side and back.
                nextGlance -= dt;
                if (nextGlance <= 0f)
                {
                    glanceTarget = Mathf.Approximately(glanceTarget, 0f) ? Random.Range(-9f, 9f) : 0f;
                    nextGlance = glanceTarget == 0f ? Random.Range(3f, 6f) : Random.Range(0.8f, 1.6f);
                }
                glance = Mathf.Lerp(glance, glanceTarget, 1f - Mathf.Exp(-dt * 3f));
                if (head != null)
                {
                    Vector3 e = he + new Vector3(Mathf.Sin(t * 1.6f) * 0.8f, glance + Mathf.Sin(t * 0.31f) * 1.5f, Mathf.Sin(t * 0.23f) * 1.2f);
                    head.localRotation = Quaternion.Slerp(head.localRotation, Quaternion.Euler(e), k);
                }
                UpdateFlash();
                yield return null;
            }
        }
    }

    /// <summary>Marks an eye so it stays a separate part (not merged) and can blink.</summary>
    public class EyeLid : MonoBehaviour { }

    /// <summary>Blinks every few seconds (both eyes together, now and then a double blink).</summary>
    public class Blinker : MonoBehaviour
    {
        Transform[] eyes;
        Vector3[] baseScale;
        float next, t = -1f;
        bool twice;

        void Start()
        {
            var lids = GetComponentsInChildren<EyeLid>(true);
            eyes = new Transform[lids.Length];
            baseScale = new Vector3[lids.Length];
            for (int i = 0; i < lids.Length; i++) { eyes[i] = lids[i].transform; baseScale[i] = eyes[i].localScale; }
            next = Random.Range(1f, 4f);
        }

        void Update()
        {
            if (eyes == null || eyes.Length == 0) return;
            float dt = Time.deltaTime;
            if (t < 0f)
            {
                next -= dt;
                if (next > 0f) return;
                t = 0f;
                twice = Random.value < 0.2f;
            }
            t += dt;
            const float dur = 0.16f;
            float c = t < dur ? Mathf.Sin(t / dur * Mathf.PI) : 0f;
            for (int i = 0; i < eyes.Length; i++)
                if (eyes[i] != null) eyes[i].localScale = new Vector3(baseScale[i].x, baseScale[i].y * (1f - 0.9f * c), baseScale[i].z);
            if (t >= dur)
            {
                t = -1f;
                next = twice ? 0.12f : Random.Range(2.5f, 5.5f);
                twice = false;
            }
        }
    }
}
