using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Drives a rigged character's Animator (e.g. a Higgsfield/Meshy/Tripo model rigged and animated with Mixamo).
    /// Controllers built by the "Build Character Prefabs" editor tool expose:
    ///   floats  Speed (0 idle → 1 sprint)
    ///   bools   Guard
    ///   triggers Attack1..Attack5, Heavy, DashAttack, Skill1..Skill3, Ultimate, Dodge, Hit, Knockdown, GetUp, Victory, Defeat
    /// Missing parameters are ignored, so partial animation sets still work.
    /// </summary>
    public class AnimatorDriver
    {
        public readonly Animator Animator;
        readonly HashSet<string> parameters = new HashSet<string>();
        float speed;

        public AnimatorDriver(Animator animator)
        {
            Animator = animator;
            Animator.applyRootMotion = false;
            Animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
            foreach (var p in animator.parameters) parameters.Add(p.name);
        }

        public bool Has(string name) { return parameters.Contains(name); }

        public void Trigger(string name)
        {
            if (!parameters.Contains(name)) return;
            Animator.ResetTrigger("Hit");
            Animator.SetTrigger(name);
        }

        public void SetBool(string name, bool value)
        {
            if (parameters.Contains(name)) Animator.SetBool(name, value);
        }

        /// <summary>Smoothed locomotion speed so walk/run/sprint blend instead of snapping.</summary>
        public void SetSpeed(float target, float dt)
        {
            speed = Mathf.MoveTowards(speed, target, dt * 6f);
            if (parameters.Contains("Speed")) Animator.SetFloat("Speed", speed);
        }

        public void SetPlaybackSpeed(float s) { Animator.speed = s; }

        public Transform Bone(HumanBodyBones bone)
        {
            return Animator.isHuman ? Animator.GetBoneTransform(bone) : null;
        }
    }
}
