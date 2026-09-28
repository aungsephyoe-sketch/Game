using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Invisible collision for solid scenery (trees, poles, rocks, walls, fences, buildings, bridge rails). The game
    /// has no physics engine, so every obstacle is a flat circle or an oriented box on the ground, kept in a spatial
    /// grid. Anything moving through <see cref="BattleController.ClampToArena"/> is pushed back out of them; the
    /// push is repeated a few times, so a gap narrower than a character simply can't be squeezed through.
    /// </summary>
    public static class Obstacles
    {
        struct Ob
        {
            public Vector2 c;
            public float r;        // circle radius (0 for boxes)
            public Vector2 half;   // box half extents in its own frame
            public float cos, sin; // box rotation
        }

        const float Cell = 4f;
        /// <summary>Collision radius of a slayer or demon.</summary>
        public const float BodyRadius = 0.45f;
        static readonly List<Ob> all = new List<Ob>();
        static readonly Dictionary<long, List<int>> grid = new Dictionary<long, List<int>>();

        public static int Count { get { return all.Count; } }

        public static void Clear()
        {
            all.Clear();
            grid.Clear();
        }

        static long Key(int x, int z) { return ((long)x << 32) ^ (uint)z; }

        static void Insert(int index, Vector2 c, float extent)
        {
            int x0 = Mathf.FloorToInt((c.x - extent) / Cell), x1 = Mathf.FloorToInt((c.x + extent) / Cell);
            int z0 = Mathf.FloorToInt((c.y - extent) / Cell), z1 = Mathf.FloorToInt((c.y + extent) / Cell);
            for (int x = x0; x <= x1; x++)
                for (int z = z0; z <= z1; z++)
                {
                    List<int> list;
                    long k = Key(x, z);
                    if (!grid.TryGetValue(k, out list)) { list = new List<int>(); grid[k] = list; }
                    list.Add(index);
                }
        }

        public static void AddCircle(Vector3 p, float radius)
        {
            if (radius <= 0.05f) return;
            var o = new Ob { c = new Vector2(p.x, p.z), r = radius };
            all.Add(o);
            Insert(all.Count - 1, o.c, radius + BodyRadius);
        }

        /// <summary>A box on the ground: centre, half size along its own x/z, and its yaw in degrees.</summary>
        public static void AddBox(Vector3 center, Vector2 half, float yaw)
        {
            if (half.x <= 0.02f || half.y <= 0.02f) return;
            float a = yaw * Mathf.Deg2Rad;
            var o = new Ob { c = new Vector2(center.x, center.z), half = half, cos = Mathf.Cos(a), sin = Mathf.Sin(a) };
            all.Add(o);
            Insert(all.Count - 1, o.c, half.magnitude + BodyRadius);
        }

        /// <summary>Pushes a position out of every obstacle it overlaps.</summary>
        public static Vector3 Resolve(Vector3 p, float radius = BodyRadius)
        {
            if (all.Count == 0) return p;
            Vector2 q = new Vector2(p.x, p.z);
            for (int iter = 0; iter < 4; iter++)
            {
                List<int> list;
                if (!grid.TryGetValue(Key(Mathf.FloorToInt(q.x / Cell), Mathf.FloorToInt(q.y / Cell)), out list)) break;
                bool moved = false;
                for (int i = 0; i < list.Count; i++)
                {
                    var o = all[list[i]];
                    if (o.r > 0f)
                    {
                        Vector2 d = q - o.c;
                        float min = o.r + radius;
                        float m2 = d.sqrMagnitude;
                        if (m2 >= min * min) continue;
                        float m = Mathf.Sqrt(m2);
                        q = m > 0.0001f ? o.c + d / m * min : o.c + Vector2.right * min;
                        moved = true;
                    }
                    else
                    {
                        // Into the box's frame.
                        Vector2 d = q - o.c;
                        float lx = d.x * o.cos - d.y * o.sin, lz = d.x * o.sin + d.y * o.cos;
                        float ex = o.half.x + radius, ez = o.half.y + radius;
                        if (Mathf.Abs(lx) >= ex || Mathf.Abs(lz) >= ez) continue;
                        // Out along the shallowest side.
                        float px = ex - Mathf.Abs(lx), pz = ez - Mathf.Abs(lz);
                        if (px < pz) lx = Mathf.Sign(lx == 0f ? 1f : lx) * ex;
                        else lz = Mathf.Sign(lz == 0f ? 1f : lz) * ez;
                        q = o.c + new Vector2(lx * o.cos + lz * o.sin, -lx * o.sin + lz * o.cos);
                        moved = true;
                    }
                }
                if (!moved) break;
            }
            p.x = q.x;
            p.z = q.y;
            return p;
        }

        static readonly string[] SkipNames = { "Road", "Ground", "Terrain", "Water", "Flow", "Ring", "Foliage", "Rocks", "FarTrees", "Glow", "Ranges", "SkyDome", "Cloud", "Glow", "Pool", "Disc", "Sector", "Merged" };

        /// <summary>
        /// Registers the solid set pieces under <paramref name="root"/>: anything standing on the ground, at least
        /// knee high and no bigger than a building. Cubes become oriented boxes; everything else a circle.
        /// </summary>
        public static void Scan(Transform root, float minHeight = 0.7f)
        {
            if (root == null) return;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (mr.GetComponentInParent<NoCollision>() != null || mr.GetComponentInParent<Breakable>() != null || mr.GetComponentInParent<NpcWalker>() != null) continue;
                var mf = mr.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null) continue;
                string n = mr.gameObject.name;
                bool skip = false;
                foreach (var s in SkipNames) if (n.StartsWith(s)) { skip = true; break; }
                if (skip) continue;
                var mat = mr.sharedMaterial;
                if (mat != null && mat.shader != null && (mat.shader.name.Contains("Additive") || mat.shader.name.Contains("Transparent"))) continue;
                Bounds b = mr.bounds;
                if (b.min.y > 0.6f || b.size.y < minHeight) continue;
                if (b.size.x > 16f || b.size.z > 16f) continue;
                var t = mr.transform;
                string mesh = mf.sharedMesh.name;
                if (mesh.StartsWith("Cube") && Mathf.Abs(Vector3.Dot(t.up, Vector3.up)) > 0.9f)
                {
                    Vector3 s = t.lossyScale;
                    float yaw = t.eulerAngles.y;
                    AddBox(t.position, new Vector2(Mathf.Abs(s.x) * 0.5f, Mathf.Abs(s.z) * 0.5f), yaw);
                }
                else
                {
                    float r = Mathf.Min(b.extents.x, b.extents.z);
                    if (r < 0.08f) continue;
                    AddCircle(b.center, Mathf.Min(r * 0.92f, 4f));
                }
            }
        }
    }

    /// <summary>Marks a branch of scenery that should never block movement (bridge decks, flat decals).</summary>
    public class NoCollision : MonoBehaviour { }
}
