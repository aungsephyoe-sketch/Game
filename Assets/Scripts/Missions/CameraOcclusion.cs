using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Keeps the fighting slayer visible: any tall piece of scenery (a tree canopy, a house, a big rock) that
    /// stands between the camera and the active slayer is hidden until it's out of the way again.
    /// </summary>
    /// <summary>Marks a set piece whose parts the camera should hide or show together.</summary>
    public class OcclusionGroup : MonoBehaviour
    {
        /// <summary>Never hide (things you stand on, like bridges).</summary>
        public bool neverHide;
    }

    public class CameraOcclusion : MonoBehaviour
    {
        struct Blocker { public Renderer[] rs; public Vector3 c; public float radius; public bool hidden; }
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
            // Parts of one set piece (a gate, a lantern, a tent) hide and show together, so a beam or a post is
            // never cut out of the middle of it.
            var groups = new Dictionary<OcclusionGroup, List<Renderer>>();
            foreach (var r in root.GetComponentsInChildren<Renderer>(true))
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer || r is LineRenderer) continue;
                var g = r.GetComponentInParent<OcclusionGroup>();
                if (g != null)
                {
                    if (g.neverHide) continue;
                    List<Renderer> list;
                    if (!groups.TryGetValue(g, out list)) { list = new List<Renderer>(); groups[g] = list; }
                    list.Add(r);
                    continue;
                }
                Add(new[] { r }, r.bounds);
            }
            foreach (var kv in groups)
            {
                Bounds b = kv.Value[0].bounds;
                for (int i = 1; i < kv.Value.Count; i++) b.Encapsulate(kv.Value[i].bounds);
                Add(kv.Value.ToArray(), b);
            }
        }

        void Add(Renderer[] rs, Bounds b)
        {
            // Only things tall enough to hide a slayer, and not ground sheets or far mountains.
            if (b.max.y < 1.4f || b.size.y > 25f || b.size.x > 30f || b.size.z > 30f) return;
            blockers.Add(new Blocker { rs = rs, c = b.center, radius = Mathf.Max(0.5f, new Vector2(b.extents.x, b.extents.z).magnitude * 0.85f) });
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
                if (b.rs == null || b.rs.Length == 0 || b.rs[0] == null) continue;
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
                    foreach (var r in b.rs) if (r != null) r.enabled = !hide;
                    blockers[i] = b;
                }
            }
        }
    }
}
