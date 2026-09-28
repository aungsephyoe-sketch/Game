using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Collision for solid scenery (trees, poles, rocks, walls, fences, buildings, bridge rails). Every obstacle exists
    /// twice, with the same shape: as a real Unity collider (a <see cref="CapsuleCollider"/> for trunks, poles and
    /// rocks, a <see cref="BoxCollider"/> for walls, fences and buildings) that the slayers' CharacterController sweeps
    /// against, and as a flat circle / oriented box in a spatial grid that <see cref="BattleController.ClampToArena"/>
    /// uses for demons, allies and as the final safety pass. A gap narrower than a body can't be squeezed through
    /// because both the capsule and the analytic pass include the body radius.
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

        /// <summary>Layer the slayers' capsules live on ("Ignore Raycast"): they collide with scenery, not each other.</summary>
        public const int CharacterLayer = 2;
        /// <summary>How tall the generated colliders are: well above any slayer, so nothing can hop over a wall.</summary>
        const float ColliderHeight = 4f;
        static Transform colliderRoot;
        static readonly HashSet<MeshRenderer> scanned = new HashSet<MeshRenderer>();

        /// <summary>True while the game runs: real colliders are generated and CharacterControllers can sweep.</summary>
        public static bool PhysicsReady { get { return Application.isPlaying; } }

        public static void Clear()
        {
            all.Clear();
            grid.Clear();
            scanned.Clear();
            if (colliderRoot != null) Object.Destroy(colliderRoot.gameObject);
            colliderRoot = null;
        }

        public static void SetupCharacterLayer(GameObject go)
        {
            go.layer = CharacterLayer;
            Physics.IgnoreLayerCollision(CharacterLayer, CharacterLayer, true);
        }

        static Transform Root()
        {
            if (!PhysicsReady) return null;
            if (colliderRoot == null)
            {
                var go = new GameObject("SceneryColliders");
                colliderRoot = go.transform;
            }
            return colliderRoot;
        }

        static void AddCapsule(Vector2 c, float radius)
        {
            var root = Root();
            if (root == null) return;
            var go = new GameObject("Solid");
            go.transform.SetParent(root, false);
            go.transform.position = new Vector3(c.x, 0f, c.y);
            var cap = go.AddComponent<CapsuleCollider>();
            cap.direction = 1;
            cap.radius = radius;
            cap.height = Mathf.Max(ColliderHeight, radius * 2f + 0.1f);
            cap.center = new Vector3(0f, cap.height * 0.5f, 0f);
        }

        static void AddBoxCollider(Vector2 c, Vector2 half, float yaw)
        {
            var root = Root();
            if (root == null) return;
            var go = new GameObject("Solid");
            go.transform.SetParent(root, false);
            go.transform.SetPositionAndRotation(new Vector3(c.x, 0f, c.y), Quaternion.Euler(0f, yaw, 0f));
            var box = go.AddComponent<BoxCollider>();
            box.size = new Vector3(half.x * 2f, ColliderHeight, half.y * 2f);
            box.center = new Vector3(0f, ColliderHeight * 0.5f, 0f);
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
            AddCapsule(o.c, radius);
        }

        /// <summary>A box on the ground: centre, half size along its own x/z, and its yaw in degrees.</summary>
        public static void AddBox(Vector3 center, Vector2 half, float yaw)
        {
            if (half.x <= 0.02f || half.y <= 0.02f) return;
            float a = yaw * Mathf.Deg2Rad;
            var o = new Ob { c = new Vector2(center.x, center.z), half = half, cos = Mathf.Cos(a), sin = Mathf.Sin(a) };
            all.Add(o);
            Insert(all.Count - 1, o.c, half.magnitude + BodyRadius);
            AddBoxCollider(o.c, half, yaw);
        }

        /// <summary>
        /// Moves from <paramref name="from"/> toward <paramref name="to"/> in short steps, pushing out of solids after
        /// each one, so a fast dash can't skip clean over a thin pole (the analytic twin of a capsule sweep).
        /// </summary>
        public static Vector3 Sweep(Vector3 from, Vector3 to, float radius = BodyRadius)
        {
            if (all.Count == 0) return to;
            Vector3 d = to - from;
            d.y = 0f;
            int n = Mathf.Clamp(Mathf.CeilToInt(d.magnitude / (radius * 0.5f)), 1, 64);
            Vector3 p = from;
            for (int i = 1; i <= n; i++) p = Resolve(p + d / n, radius);
            p.y = to.y;
            return p;
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
        /// Registers the solid set pieces under <paramref name="root"/>: anything standing on the ground that reaches
        /// into a slayer's body. The shape follows the mesh: cubes (walls, fences, rails, house bodies) become
        /// oriented boxes, cylinders and capsules (trunks, poles, pillars) circles of their true radius, anything else
        /// (rocks, statues, stumps) a circle that fits its footprint. Builders call this before static batching,
        /// which renames every mesh; renderers already batched or scanned are skipped.
        /// </summary>
        public static void Scan(Transform root, float minHeight = 0.7f)
        {
            if (root == null) return;
            foreach (var mr in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!scanned.Add(mr)) continue;
                if (mr.isPartOfStaticBatch) continue;
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
                var t = mr.transform;
                string mesh = mf.sharedMesh.name;
                bool upright = Mathf.Abs(Vector3.Dot(t.up, Vector3.up)) > 0.9f;
                if (mesh.StartsWith("Cube") && upright)
                {
                    // Walls, fence rails, low garden walls and house bodies: anything from knee height up blocks.
                    if (b.min.y > 1f || b.max.y < 0.5f) continue;
                    if (b.size.x > 40f || b.size.z > 40f) continue;
                    Vector3 s = t.lossyScale;
                    AddBox(new Vector3(b.center.x, 0f, b.center.z), new Vector2(Mathf.Abs(s.x) * 0.5f, Mathf.Abs(s.z) * 0.5f), t.eulerAngles.y);
                    continue;
                }
                if (b.min.y > 0.6f || b.size.y < minHeight) continue;
                if (b.size.x > 16f || b.size.z > 16f) continue;
                float r;
                if ((mesh.StartsWith("Cylinder") || mesh.StartsWith("Capsule")) && upright)
                {
                    Vector3 s = t.lossyScale;
                    r = Mathf.Max(Mathf.Abs(s.x), Mathf.Abs(s.z)) * 0.5f;
                }
                else if (mesh.StartsWith("Sphere") && upright)
                {
                    // Boulders and bushes: an ellipse footprint, covered by a row of circles along its long axis.
                    Vector3 s = t.lossyScale;
                    float ax = Mathf.Abs(s.x) * 0.5f, az = Mathf.Abs(s.z) * 0.5f;
                    Vector3 dir = ax >= az ? t.right : t.forward;
                    dir.y = 0f;
                    dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector3.right;
                    float lo = Mathf.Min(ax, az) * 0.95f, reach = Mathf.Max(ax, az) * 0.95f - lo;
                    Vector3 c = new Vector3(b.center.x, 0f, b.center.z);
                    if (lo < 0.08f) continue;
                    lo = Mathf.Min(lo, 4f);
                    AddCircle(c, lo);
                    if (reach > 0.05f)
                    {
                        AddCircle(c + dir * reach, lo);
                        AddCircle(c - dir * reach, lo);
                    }
                    continue;
                }
                else
                {
                    float ex = b.extents.x, ez = b.extents.z;
                    // Long and thin (a log, a bench, a cart): a box of its footprint instead of a huge circle.
                    if (Mathf.Max(ex, ez) > Mathf.Min(ex, ez) * 1.8f)
                    {
                        AddBox(new Vector3(b.center.x, 0f, b.center.z), new Vector2(ex * 0.9f, ez * 0.9f), 0f);
                        continue;
                    }
                    // Rounded silhouettes don't fill their bounding square: use the mean extent, slightly inset.
                    r = (ex + ez) * 0.5f * 0.88f;
                }
                if (r < 0.08f) continue;
                AddCircle(new Vector3(b.center.x, 0f, b.center.z), Mathf.Min(r, 4f));
            }
        }
    }

    /// <summary>Marks a branch of scenery that should never block movement (bridge decks, flat decals).</summary>
    public class NoCollision : MonoBehaviour { }
}
