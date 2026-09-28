using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Builds the sculpted hair (<see cref="HairGeometry"/>, <see cref="HairStyles"/>) onto a head: one mesh per
    /// section and colour, the swinging sections on their own pivots with <see cref="HairSway"/>.
    /// </summary>
    public partial class CharacterVisual
    {
        /// <summary>Sculpted hair for this character, if it has a recipe. Returns false to fall back to the old hair.</summary>
        bool SculptedHair(CharacterDefinition def, Vector3 hc, Vector3 hr, Material cloth = null)
        {
            if (!HairStyles.Has(def.id) || head == null) return false;
            var g = new HairGeometry(hc, hr, def.id.Length * 7919);
            HairStyles.Build(def.id, g);

            Color c = def.hairColor;
            bool dark = c.r + c.g + c.b < 0.6f;
            bool light = c.r + c.g + c.b > 2.4f;
            // Smooth cel shading: a shade colour for the under layers and a soft highlight, never metallic.
            Color shadeC = light ? Color.Lerp(c, new Color(0.72f, 0.7f, 0.86f), 0.35f)
                : dark ? Color.Lerp(c, new Color(0.02f, 0.02f, 0.05f), 0.4f) : Color.Lerp(c, Color.black, 0.2f);
            Color shineC = dark ? Color.Lerp(c, new Color(0.45f, 0.5f, 0.7f), 0.35f) : Color.Lerp(c, Color.white, 0.4f);
            var mats = new Material[4];
            mats[HairGeometry.Cloth] = cloth != null ? cloth : PM(new Color(0.2f, 0.18f, 0.24f), 0.01f);
            mats[HairGeometry.Base] = PM(c, 0.009f);
            mats[HairGeometry.Shade] = PM(shadeC, 0.009f);
            mats[HairGeometry.Shine] = PM(shineC, 0f);

            var groups = new System.Collections.Generic.Dictionary<string, Transform>();
            foreach (var part in g.parts)
            {
                if (part.v.Count == 0) continue;
                Transform gt;
                if (!groups.TryGetValue(part.group, out gt))
                {
                    Vector3 pivot;
                    if (!g.pivots.TryGetValue(part.group, out pivot)) pivot = hc;
                    gt = J("Hair" + part.group, head, pivot);
                    groups[part.group] = gt;
                    if (g.pivots.ContainsKey(part.group))
                    {
                        var sway = gt.gameObject.AddComponent<HairSway>();
                        bool fall = part.group == "Fall";
                        bool bangs = part.group == "Bangs";
                        sway.amount = fall ? 1.3f : bangs ? 0.4f : 0.75f;
                        sway.maxAngle = fall ? 14f : bangs ? 4f : 8f;
                        sway.hang = fall ? 0.45f : part.group.StartsWith("Side") ? 0.25f : 0f;
                    }
                }
                Vector3 origin = gt.localPosition;
                var verts = new Vector3[part.v.Count];
                for (int i = 0; i < verts.Length; i++) verts[i] = part.v[i] - origin;
                var mesh = new Mesh { name = "Hair" + part.group + part.mat };
                mesh.vertices = verts;
                mesh.triangles = part.t.ToArray();
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                Add(MeshFactory.MeshObject(mesh, gt, Vector3.zero, Vector3.one, mats[part.mat]));
            }
            return true;
        }
    }

    /// <summary>
    /// Secondary motion for a section of hair: it lags behind the head when the character walks, runs, dodges,
    /// jumps or turns, springs back, and (for long hair) keeps hanging toward the ground when the head tilts.
    /// Angles are kept small so the hair never swings through the face or body.
    /// </summary>
    public class HairSway : MonoBehaviour
    {
        public float amount = 1f, maxAngle = 10f, stiffness = 55f, damping = 8f;
        /// <summary>0..1: how much the section keeps hanging straight down when the head tilts.</summary>
        public float hang;

        Quaternion baseRot;
        Vector3 lastPos, lastVel, lastFwd;
        Vector2 ang, angVel;
        bool ready;
        float seed;

        void Start()
        {
            baseRot = transform.localRotation;
            seed = Random.value * 10f;
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            var parent = transform.parent;
            if (dt <= 0f || parent == null) return;
            Vector3 pos = transform.position;
            Vector3 fwd = parent.forward;
            if (!ready) { lastPos = pos; lastFwd = fwd; ready = true; return; }
            Vector3 vel = (pos - lastPos) / dt;
            Vector3 acc = (vel - lastVel) / dt;
            lastPos = pos;
            lastVel = vel;
            // Turning rate (degrees per second around the vertical).
            float yawRate = Vector3.SignedAngle(lastFwd, fwd, Vector3.up) / dt;
            lastFwd = fwd;
            Vector3 lv = parent.InverseTransformDirection(vel);
            Vector3 la = parent.InverseTransformDirection(acc);
            // Moving forward swings the hair back; sideways and turning swing it the other way; a jump lifts it.
            Vector2 target = new Vector2(
                Mathf.Clamp(lv.z * 2.2f + la.z * 0.08f - la.y * 0.05f, -maxAngle, maxAngle),
                Mathf.Clamp(-lv.x * 2.2f - la.x * 0.08f - yawRate * 0.02f, -maxAngle, maxAngle)) * amount;
            // A breath of idle movement.
            float t = Time.time + seed;
            target += new Vector2(Mathf.Sin(t * 1.3f) * 0.6f, Mathf.Sin(t * 0.9f + 1f) * 0.5f) * amount;
            angVel += ((target - ang) * stiffness - angVel * damping) * dt;
            ang += angVel * dt;
            ang.x = Mathf.Clamp(ang.x, -maxAngle, maxAngle);
            ang.y = Mathf.Clamp(ang.y, -maxAngle, maxAngle);
            Quaternion hangRot = Quaternion.identity;
            if (hang > 0f)
            {
                Vector3 down = parent.InverseTransformDirection(Vector3.down);
                Quaternion toDown = Quaternion.FromToRotation(Vector3.down, down);
                hangRot = Quaternion.Slerp(Quaternion.identity, toDown, hang);
            }
            transform.localRotation = hangRot * baseRot * Quaternion.Euler(ang.x, 0f, ang.y);
        }
    }
}
