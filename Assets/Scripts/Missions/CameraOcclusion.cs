using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Keeps the fighting slayer visible: any tall piece of scenery (a tree canopy, a house, a big rock) that
    /// stands between the camera and the active slayer is hidden until it's out of the way again.
    /// </summary>
    public class CameraOcclusion : MonoBehaviour
    {
        struct Blocker { public Renderer r; public Vector3 c; public float radius; public bool hidden; }
        readonly List<Blocker> blockers = new List<Blocker>();
        BattleController battle;

        public static void Attach(BattleController b)
        {
            var o = b.gameObject.AddComponent<CameraOcclusion>();
            o.battle = b;
            o.Collect(b.transform);
        }

        void Collect(Transform root)
        {
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var b = r.bounds;
                // Only things tall enough to hide a slayer, and not ground sheets or far mountains.
                if (b.max.y < 1.4f || b.size.y > 25f || b.size.x > 30f || b.size.z > 30f) continue;
                blockers.Add(new Blocker { r = r, c = b.center, radius = Mathf.Max(0.5f, new Vector2(b.extents.x, b.extents.z).magnitude * 0.85f) });
            }
        }

        void LateUpdate()
        {
            var cam = Camera.main;
            if (cam == null || battle == null || battle.Team == null || battle.Team.Active == null) return;
            Vector3 p = battle.Team.Active.Position + Vector3.up * 1.2f;
            Vector3 c = cam.transform.position;
            Vector3 seg = p - c;
            float len2 = seg.sqrMagnitude;
            for (int i = 0; i < blockers.Count; i++)
            {
                var b = blockers[i];
                if (b.r == null) continue;
                bool hide = false;
                Vector3 d = b.c - p;
                if (d.x * d.x + d.z * d.z < 400f)
                {
                    float t = Mathf.Clamp01(Vector3.Dot(b.c - c, seg) / Mathf.Max(0.001f, len2));
                    if (t > 0.02f && t < 0.97f)
                    {
                        Vector3 closest = c + seg * t;
                        hide = (b.c - closest).magnitude < b.radius + 0.6f;
                    }
                }
                if (hide != b.hidden)
                {
                    b.hidden = hide;
                    b.r.enabled = !hide;
                    blockers[i] = b;
                }
            }
        }
    }
}
