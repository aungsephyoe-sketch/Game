using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Animates a rigged model that ships without animation clips (e.g. an auto-rigged Higgsfield/Meshy GLB):
    /// breathing idle, walk/run cycles with arm counter-swing, a guard stance, and a sword arm that follows the
    /// blade during attacks. Bones are found by common naming conventions (Mixamo, Meshy, Blender, UE) and every
    /// rotation is applied around the character's own axes, so it works regardless of each bone's local frame.
    /// Replace with real animation clips (see docs/ART_PIPELINE.md) for final quality.
    /// </summary>
    public class ProceduralRig : MonoBehaviour
    {
        CharacterVisual visual;
        Transform hips, spine, chest, head, lUpperArm, rUpperArm, lForeArm, rForeArm, lUpperLeg, rUpperLeg, lLowerLeg, rLowerLeg;
        readonly Dictionary<Transform, Quaternion> rest = new Dictionary<Transform, Quaternion>();
        float phase;
        float armDrop = 32f;

        public bool Bind(CharacterVisual v)
        {
            visual = v;
            var root = transform;
            hips = Or(FindBone(root, "hips", 0), FindBone(root, "pelvis", 0));
            spine = FindBone(root, "spine", 0);
            chest = Or(FindBone(root, "chest", 0), FindBone(root, "spine2", 0), FindBone(root, "spine02", 0), FindBone(root, "spine1", 0));
            head = FindBone(root, "head", 0);
            lUpperArm = Or(FindBone(root, "upperarm", -1), FindBone(root, "arm", -1));
            rUpperArm = Or(FindBone(root, "upperarm", 1), FindBone(root, "arm", 1));
            lForeArm = Or(FindBone(root, "forearm", -1), FindBone(root, "lowerarm", -1));
            rForeArm = Or(FindBone(root, "forearm", 1), FindBone(root, "lowerarm", 1));
            lUpperLeg = Or(FindBone(root, "upleg", -1), FindBone(root, "thigh", -1), FindBone(root, "upperleg", -1));
            rUpperLeg = Or(FindBone(root, "upleg", 1), FindBone(root, "thigh", 1), FindBone(root, "upperleg", 1));
            lLowerLeg = Or(FindBone(root, "leg", -1), FindBone(root, "calf", -1), FindBone(root, "shin", -1));
            rLowerLeg = Or(FindBone(root, "leg", 1), FindBone(root, "calf", 1), FindBone(root, "shin", 1));
            int found = 0;
            foreach (var b in new[] { hips, spine, chest, head, lUpperArm, rUpperArm, lForeArm, rForeArm, lUpperLeg, rUpperLeg, lLowerLeg, rLowerLeg })
                if (b != null) { rest[b] = b.localRotation; found++; }
            return found >= 6 && lUpperLeg != null && rUpperLeg != null;
        }

        /// <summary>Finds a bone whose name contains <paramref name="part"/>; side -1 = left, 1 = right, 0 = either.</summary>
        public static Transform FindBone(Transform root, string part, int side)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>())
            {
                string n = Normalize(t.name);
                if (!n.Contains(part)) continue;
                // Avoid partial matches: "leg" must not match "upleg"/"upperleg", "arm" must not match "forearm"/"upperarm".
                if (part == "leg" && (n.Contains("upleg") || n.Contains("upperleg") || n.Contains("thigh"))) continue;
                if (part == "arm" && (n.Contains("forearm") || n.Contains("lowerarm") || n.Contains("upperarm") || n.Contains("armor"))) continue;
                if (n.Contains("end") || n.Contains("twist") || n.Contains("roll")) continue;
                int s = SideOf(t.name);
                if (side == 0 || s == side) return t;
            }
            return null;
        }

        static string Normalize(string name)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char c in name.ToLowerInvariant())
                if (char.IsLetterOrDigit(c)) sb.Append(c);
            string s = sb.ToString();
            // Strip common rig prefixes.
            foreach (var p in new[] { "mixamorig", "bip01", "bip001", "def", "armature" })
                if (s.StartsWith(p)) s = s.Substring(p.Length);
            return s;
        }

        static int SideOf(string raw)
        {
            string n = raw.ToLowerInvariant();
            if (n.Contains("left") || n.EndsWith("_l") || n.EndsWith(".l") || n.StartsWith("l_") || n.Contains("_l_") || n.EndsWith(" l")) return -1;
            if (n.Contains("right") || n.EndsWith("_r") || n.EndsWith(".r") || n.StartsWith("r_") || n.Contains("_r_") || n.EndsWith(" r")) return 1;
            return 0;
        }

        void LateUpdate()
        {
            if (visual == null || visual.IsDead) return;
            foreach (var kv in rest) kv.Key.localRotation = kv.Value;

            Transform body = visual.transform;
            Vector3 right = body.right, fwd = body.forward;
            float move = visual.MoveAmount;
            bool sprint = visual.IsSprinting;
            float dt = Time.deltaTime;
            phase += dt * (move > 0.05f ? (sprint ? 11f : 8f) : 0f);
            float t = Time.time;
            float s = Mathf.Sin(phase);

            // Breathing and a slight forward lean when running.
            Rot(Or(chest, spine), right, -Mathf.Sin(t * 2f) * 1.5f + move * (sprint ? 10f : 5f));
            Rot(hips, fwd, s * 4f * move);

            // Legs: swing forward/back, knees bend on the back-swing.
            float legAmp = (sprint ? 38f : 26f) * move;
            Rot(lUpperLeg, right, -s * legAmp);
            Rot(rUpperLeg, right, s * legAmp);
            Rot(lLowerLeg, right, Mathf.Max(0f, -Mathf.Cos(phase)) * legAmp * 1.3f);
            Rot(rLowerLeg, right, Mathf.Max(0f, Mathf.Cos(phase)) * legAmp * 1.3f);

            // Arms: drop out of the A-pose, counter-swing when moving.
            float armAmp = (sprint ? 35f : 22f) * move;
            Rot(lUpperArm, fwd, armDrop);
            Rot(rUpperArm, fwd, -armDrop);
            Rot(lUpperArm, right, s * armAmp);
            Rot(rUpperArm, right, -s * armAmp);
            Rot(lForeArm, right, -15f - move * 20f);
            Rot(rForeArm, right, -15f - move * 20f);

            if (visual.IsGuarding)
            {
                Rot(lUpperArm, right, -60f);
                Rot(rUpperArm, right, -60f);
                Rot(lForeArm, right, -50f);
                Rot(rForeArm, right, -50f);
            }

            // Sword arm follows the blade while it swings.
            float w = visual.SwingWeight;
            if (w > 0.01f && rUpperArm != null && rForeArm != null && visual.SwordPivot != null)
            {
                Vector3 cur = (rForeArm.position - rUpperArm.position).normalized;
                Vector3 target = (visual.SwordPivot.forward * 0.85f + Vector3.down * 0.15f).normalized;
                var q = Quaternion.FromToRotation(cur, target);
                rUpperArm.rotation = Quaternion.Slerp(Quaternion.identity, q, w * 0.85f) * rUpperArm.rotation;
                Rot(Or(chest, spine), Vector3.up, Vector3.SignedAngle(fwd, Vector3.ProjectOnPlane(visual.SwordPivot.forward, Vector3.up), Vector3.up) * 0.25f * w);
            }

            // Head keeps looking ahead.
            Rot(head, right, -move * 6f);
        }

        static Transform Or(params Transform[] options)
        {
            foreach (var t in options) if (t != null) return t;
            return null;
        }

        static void Rot(Transform bone, Vector3 axis, float degrees)
        {
            if (bone == null || Mathf.Abs(degrees) < 0.01f) return;
            bone.rotation = Quaternion.AngleAxis(degrees, axis) * bone.rotation;
        }
    }
}
