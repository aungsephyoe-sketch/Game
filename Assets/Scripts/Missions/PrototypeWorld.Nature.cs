using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Plants, trees and rocks for the prototype worlds. Everything small is combined into a few wind-swayed,
    /// vertex-coloured meshes; the trees near the road are separate objects so the camera can fade them out of
    /// the way; the forest walls beyond are combined into large low-cost meshes.
    /// </summary>
    public static partial class PrototypeWorld
    {
        static Mesh tuftMesh, fernMesh, lowSphere, trunkMesh;
        static WorldMeshBuilder fol, solid, farTrees, glowB;
        static Material folMat, solidMat, glowMat;
        static Transform treeRoot;
        static readonly List<Vector4> reserved = new List<Vector4>();

        /// <summary>Keeps scatter out of a circle (landmarks, bridges, camps).</summary>
        static void Reserve(Vector3 p, float r) { reserved.Add(new Vector4(p.x, p.z, r, 0f)); }

        static bool IsReserved(float x, float z)
        {
            foreach (var r in reserved)
                if ((x - r.x) * (x - r.x) + (z - r.y) * (z - r.y) < r.z * r.z) return true;
            return false;
        }

        static bool NearChannel(float x, float z, float pad)
        {
            foreach (var c in channels)
                if (ChannelDist(c, x, z) < c.half + pad) return true;
            return false;
        }

        static Color Jitter(Color c, float amount)
        {
            float k = 1f + R(-amount, amount);
            return new Color(Mathf.Clamp01(c.r * k + R(-amount, amount) * 0.3f), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);
        }

        // ------------------------------------------------------------------ Template meshes

        /// <summary>A tuft of thin double-sided grass blades, bending outward.</summary>
        static Mesh MakeTuft(int blades, float width, float bend, int seed)
        {
            var r = new System.Random(seed);
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var t = new List<int>();
            for (int k = 0; k < blades; k++)
            {
                float yaw = k * 360f / blades + (float)r.NextDouble() * 40f;
                float h = 0.7f + (float)r.NextDouble() * 0.3f;
                float lean = 0.15f + (float)r.NextDouble() * bend;
                var q = Quaternion.Euler(0f, yaw, 0f);
                Vector3 off = q * new Vector3(0f, 0f, 0.08f);
                Vector3[] p =
                {
                    off + q * new Vector3(-width, 0f, 0f), off + q * new Vector3(width, 0f, 0f),
                    off + q * new Vector3(-width * 0.6f, h * 0.55f, lean * 0.5f), off + q * new Vector3(width * 0.6f, h * 0.55f, lean * 0.5f),
                    off + q * new Vector3(0f, h, lean * 1.4f)
                };
                Vector3 nn = (q * new Vector3(0f, 0.8f, 0.6f)).normalized;
                for (int side = 0; side < 2; side++)
                {
                    int b = v.Count;
                    for (int i = 0; i < 5; i++) { v.Add(p[i]); n.Add(side == 0 ? nn : new Vector3(-nn.x, nn.y, -nn.z)); }
                    if (side == 0) t.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3, b + 2, b + 4, b + 3 });
                    else t.AddRange(new[] { b, b + 1, b + 2, b + 1, b + 3, b + 2, b + 2, b + 3, b + 4 });
                }
            }
            var m = new Mesh { name = "Tuft" };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetTriangles(t, 0);
            m.RecalculateBounds();
            return m;
        }

        static Mesh MakeLowSphere(int lat, int lon)
        {
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int a = 0; a <= lat; a++)
            {
                float el = -Mathf.PI * 0.5f + a * Mathf.PI / lat;
                for (int b = 0; b <= lon; b++)
                {
                    float az = b * Mathf.PI * 2f / lon;
                    v.Add(new Vector3(Mathf.Cos(el) * Mathf.Cos(az), Mathf.Sin(el), Mathf.Cos(el) * Mathf.Sin(az)) * 0.5f);
                    if (a < lat && b < lon)
                    {
                        int i0 = a * (lon + 1) + b, i1 = i0 + 1, i2 = i0 + lon + 1, i3 = i2 + 1;
                        t.Add(i0); t.Add(i2); t.Add(i1);
                        t.Add(i1); t.Add(i2); t.Add(i3);
                    }
                }
            }
            var m = new Mesh { name = "LowSphere" };
            m.SetVertices(v);
            m.SetTriangles(t, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        static void Templates()
        {
            if (tuftMesh == null) tuftMesh = MakeTuft(7, 0.05f, 0.25f, 3);
            if (fernMesh == null) fernMesh = MakeTuft(6, 0.12f, 0.6f, 9);
            if (lowSphere == null) lowSphere = MakeLowSphere(7, 12);
            if (trunkMesh == null)
                trunkMesh = MeshFactory.Lathe("pw_trunk", new[] { new Vector2(1f, 0f), new Vector2(0.72f, 0.12f), new Vector2(0.58f, 0.45f), new Vector2(0.42f, 1f), new Vector2(0f, 1.02f) }, 9);
        }

        // ------------------------------------------------------------------ Trees

        static void BroadTree(WorldMeshBuilder b, Vector3 p, float h, Color leaf)
        {
            float lean = R(-4f, 4f);
            var q = Quaternion.Euler(lean, R(0f, 360f), R(-4f, 4f));
            b.Add(trunkMesh, p + Vector3.down * 0.2f, q, new Vector3(h * 0.06f, h * 0.66f, h * 0.06f), Jitter(P.trunk, 0.08f));
            // Two branches reaching into the crown.
            for (int k = 0; k < 2; k++)
            {
                var bq = q * Quaternion.Euler(0f, k * 150f + R(0f, 40f), 0f) * Quaternion.Euler(0f, 0f, 48f);
                b.Add(trunkMesh, p + q * Vector3.up * h * (0.4f + k * 0.08f), bq, new Vector3(h * 0.025f, h * 0.28f, h * 0.025f), P.trunk * 0.9f);
            }
            Color dark = Color.Lerp(leaf, P.leafDark, 0.55f), light = Color.Lerp(leaf, P.leafLight, 0.55f);
            Vector3 top = p + q * Vector3.up * h * 0.72f;
            b.Add(lowSphere, top, Quaternion.identity, Vector3.one * h * 0.58f, Jitter(leaf, 0.05f), 0.14f, 0.5f, 0.5f);
            for (int k = 0; k < 5; k++)
            {
                float a = k * 72f + R(0f, 30f);
                Vector3 o = Quaternion.Euler(0f, a, 0f) * new Vector3(h * 0.24f, -h * 0.1f + R(-0.1f, 0.2f) * h * 0.2f, 0f);
                b.Add(lowSphere, top + o, Quaternion.Euler(0f, a, 0f), new Vector3(h * 0.38f, h * 0.32f, h * 0.38f), Jitter(dark, 0.06f), 0.12f, 0.5f, 0.4f);
            }
            b.Add(lowSphere, top + new Vector3(R(-0.1f, 0.1f) * h, h * 0.2f, R(-0.1f, 0.1f) * h), Quaternion.identity, Vector3.one * h * 0.36f, Jitter(light, 0.05f), 0.16f, 0.5f, 0.5f);
            b.Add(lowSphere, top + new Vector3(h * 0.12f, h * 0.1f, h * 0.14f), Quaternion.identity, Vector3.one * h * 0.24f, Jitter(Color.Lerp(light, Color.white, 0.12f), 0.04f), 0.16f, 0.5f, 0.5f);
        }

        /// <summary>
        /// A cherry tree in bloom: a leaning, forked trunk with limbs that spread wide, and a crown made of many
        /// small blossom clusters at the limb ends (paler on top, deeper pink underneath) instead of a few big balls.
        /// </summary>
        static void CherryTree(WorldMeshBuilder b, Vector3 p, float h)
        {
            Color bark = Jitter(new Color(0.3f, 0.21f, 0.21f), 0.06f);
            var lean = Quaternion.Euler(R(-6f, 6f), R(0f, 360f), R(-6f, 6f));
            b.Add(trunkMesh, p + Vector3.down * 0.2f, lean, new Vector3(h * 0.055f, h * 0.42f, h * 0.055f), bark);
            Vector3 fork = p + lean * Vector3.up * h * 0.36f;
            int limbs = 3 + rng.Next(2);
            var ends = new System.Collections.Generic.List<Vector3>();
            for (int k = 0; k < limbs; k++)
            {
                float yaw = k * 360f / limbs + R(-25f, 25f);
                var q = Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(0f, 0f, R(32f, 55f));
                float len = h * R(0.36f, 0.48f);
                b.Add(trunkMesh, fork, q, new Vector3(h * 0.026f, len, h * 0.026f), bark * 0.95f);
                Vector3 end = fork + q * Vector3.up * len;
                ends.Add(end);
                // A side branch off each limb.
                var q2 = q * Quaternion.Euler(0f, R(-60f, 60f), R(20f, 35f));
                Vector3 mid = fork + q * Vector3.up * len * 0.55f;
                b.Add(trunkMesh, mid, q2, new Vector3(h * 0.014f, len * 0.5f, h * 0.014f), bark * 0.9f);
                ends.Add(mid + q2 * Vector3.up * len * 0.5f);
            }
            foreach (var e in ends)
            {
                // Cartoon crowns: one or two big, clean blossom balls per limb (a bold, simple silhouette).
                int n = 1 + rng.Next(2);
                for (int k = 0; k < n; k++)
                {
                    Vector3 o = new Vector3(R(-1f, 1f), R(-0.2f, 0.6f), R(-1f, 1f)) * h * 0.08f;
                    float sz = h * R(0.3f, 0.4f);
                    bool under = o.y < 0f;
                    Color c = under ? Color.Lerp(P.leafDark, P.leafMid, R(0f, 0.5f)) : Color.Lerp(P.leafMid, P.leafLight, R(0f, 0.8f));
                    b.Add(lowSphere, e + o, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(sz, sz * 0.72f, sz), Jitter(c, 0.04f), 0.16f, 0.5f, 0.45f);
                }
            }
            // A few pale highlights on top and petals drooping below the outer clusters.
            for (int k = 0; k < 4; k++)
            {
                Vector3 e = ends[rng.Next(ends.Count)];
                b.Add(lowSphere, e + Vector3.up * h * 0.1f, Quaternion.identity, Vector3.one * h * R(0.07f, 0.1f), Jitter(P.leafLight, 0.03f), 0.18f, 0.5f, 0.5f);
                b.Add(lowSphere, e + Vector3.down * h * 0.12f, Quaternion.identity, new Vector3(h * 0.06f, h * 0.1f, h * 0.06f), Jitter(P.leafMid, 0.04f), 0.25f, 0.5f, 0.2f);
            }
        }

        static void Cedar(WorldMeshBuilder b, Vector3 p, float h, Color leaf)
        {
            b.Add(trunkMesh, p + Vector3.down * 0.2f, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(h * 0.045f, h * 0.5f, h * 0.045f), P.trunk);
            for (int k = 0; k < 4; k++)
            {
                float y = h * (0.22f + k * 0.18f), th = h * (0.42f - k * 0.06f), rad = h * (0.5f - k * 0.1f);
                Color c = Color.Lerp(P.leafDark, leaf, k / 3f);
                b.Add(MeshFactory.Cone(), p + Vector3.up * y, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(rad, th, rad), Jitter(c, 0.05f), 0.1f, 1f, 0.2f);
            }
        }

        static void SnowPine(WorldMeshBuilder b, Vector3 p, float h)
        {
            b.Add(trunkMesh, p + Vector3.down * 0.3f, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(h * 0.045f, h * 0.45f, h * 0.045f), P.trunk);
            Color snow = new Color(0.95f, 0.97f, 1f);
            for (int k = 0; k < 5; k++)
            {
                float y = h * (0.16f + k * 0.15f), th = h * (0.36f - k * 0.045f), rad = h * (0.52f - k * 0.09f);
                var yaw = Quaternion.Euler(0f, R(0f, 360f), 0f);
                b.Add(MeshFactory.Cone(), p + Vector3.up * y, yaw, new Vector3(rad, th, rad), Jitter(Color.Lerp(P.leafDark, P.leafMid, k / 4f), 0.05f), 0.06f, 1f, 0.2f);
                b.Add(MeshFactory.Cone(), p + Vector3.up * (y + th * 0.3f), yaw, new Vector3(rad * 0.78f, th * 0.72f, rad * 0.78f), Jitter(snow, 0.02f), 0.06f, 1f, 0.3f);
            }
        }

        static void FrostTree(WorldMeshBuilder b, Vector3 p, float h)
        {
            var q = Quaternion.Euler(R(-4f, 4f), R(0f, 360f), 0f);
            Color bark = new Color(0.36f, 0.34f, 0.4f), frost = new Color(0.9f, 0.95f, 1f);
            b.Add(trunkMesh, p + Vector3.down * 0.2f, q, new Vector3(h * 0.05f, h * 0.7f, h * 0.05f), bark);
            for (int k = 0; k < 6; k++)
            {
                var bq = q * Quaternion.Euler(0f, k * 60f + R(0f, 30f), 0f) * Quaternion.Euler(0f, 0f, R(35f, 60f));
                b.Add(trunkMesh, p + q * Vector3.up * h * R(0.35f, 0.65f), bq, new Vector3(h * 0.02f, h * R(0.25f, 0.4f), h * 0.02f), k % 2 == 0 ? frost : bark);
            }
            b.Add(lowSphere, p + Vector3.up * h * 0.75f, Quaternion.identity, new Vector3(h * 0.5f, h * 0.3f, h * 0.5f), new Color(0.88f, 0.94f, 1f), 0.05f, 0.5f, 0.5f);
        }

        static void BurntTree(WorldMeshBuilder b, WorldMeshBuilder glow, Vector3 p, float h)
        {
            var q = Quaternion.Euler(R(-8f, 8f), R(0f, 360f), R(-6f, 6f));
            Color bark = new Color(0.08f, 0.06f, 0.06f);
            b.Add(trunkMesh, p + Vector3.down * 0.2f, q, new Vector3(h * 0.07f, h * 0.75f, h * 0.07f), bark);
            for (int k = 0; k < 5; k++)
            {
                var bq = q * Quaternion.Euler(0f, k * 72f + R(0f, 30f), 0f) * Quaternion.Euler(0f, 0f, R(30f, 65f));
                Vector3 at = p + q * Vector3.up * h * R(0.4f, 0.72f);
                float len = h * R(0.25f, 0.4f);
                b.Add(trunkMesh, at, bq, new Vector3(h * 0.025f, len, h * 0.025f), bark);
                // Embers still glowing at the broken tips.
                glow.Add(lowSphere, at + bq * Vector3.up * len, Quaternion.identity, Vector3.one * h * 0.035f, new Color(1f, 0.45f, 0.1f), 1f, 1f, 1f);
            }
            glow.Add(lowSphere, p + q * Vector3.up * h * 0.3f + q * Vector3.forward * h * 0.06f, Quaternion.identity, new Vector3(h * 0.03f, h * 0.2f, h * 0.02f), new Color(1f, 0.4f, 0.08f), 1f, 1f, 1f);
        }

        /// <summary>A tree near the road as its own object (so the camera can fade it when it gets in the way).</summary>
        static void NearTree(Vector3 p, float h, int type)
        {
            Obstacles.AddCircle(p, K == Kind.Volcano ? h * 0.05f + 0.15f : h * 0.07f + 0.2f);
            var b = new WorldMeshBuilder(treeRoot, K == Kind.Volcano ? solidMat : folMat, "Tree", true);
            switch (K)
            {
                case Kind.Snow:
                    if (type == 2) FrostTree(b, p, h * 0.8f); else SnowPine(b, p, h);
                    break;
                case Kind.Volcano:
                    BurntTree(b, glowB, p, h * 0.7f);
                    break;
                case Kind.Village:
                    // Cherry trees in blossom among dark cedars and a few evergreen oaks.
                    if (type == 1) Cedar(b, p, h * 1.25f, VillageGreen);
                    else if (type == 2) BroadTree(b, p, h, VillageGreen * 1.15f);
                    else CherryTree(b, p, h * 0.85f);
                    break;
                default:
                    if (type == 1) Cedar(b, p, h * 1.25f, P.leafMid);
                    else if (type == 2) BroadTree(b, p, h, new Color(0.85f, 0.72f, 0.25f));
                    else BroadTree(b, p, h, Color.Lerp(P.leafMid, P.leafLight, R(0f, 0.4f)));
                    break;
            }
            b.Flush();
        }

        // ------------------------------------------------------------------ Small plants and rocks

        static void Tuft(Vector3 p, float size, Color c)
        {
            fol.Add(tuftMesh, p, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(size, size * R(0.8f, 1.2f), size), Jitter(c, 0.08f), 0.14f, 1f, 0f);
        }

        static void Flower(Vector3 p, Color petal)
        {
            float h = R(0.3f, 0.5f);
            fol.Add(tuftMesh, p, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(0.25f, h, 0.25f), Jitter(P.grassA * 0.9f, 0.05f), 0.1f, 1f, 0f);
            Vector3 head = p + Vector3.up * h;
            fol.Add(lowSphere, head, Quaternion.identity, new Vector3(0.16f, 0.07f, 0.16f), Jitter(petal, 0.05f), 0.08f, 0f, 1f);
            fol.Add(lowSphere, head + Vector3.up * 0.02f, Quaternion.identity, Vector3.one * 0.06f, new Color(1f, 0.9f, 0.4f), 0.08f, 0f, 1f);
        }

        static void Rock(Vector3 p, float size, Color c, bool cap)
        {
            var q = Quaternion.Euler(R(-8f, 8f), R(0f, 360f), R(-8f, 8f));
            if (size > 0.5f) Obstacles.AddCircle(p, size * 0.5f);
            solid.Add(MeshFactory.Rock(rng.Next(8)), p + Vector3.down * size * 0.15f, q, new Vector3(size * R(0.9f, 1.3f), size * R(0.6f, 0.9f), size * R(0.9f, 1.2f)), Jitter(c, 0.06f));
            if (cap)
            {
                Color capC = K == Kind.Snow ? new Color(0.95f, 0.97f, 1f) : K == Kind.Volcano ? new Color(0.3f, 0.26f, 0.25f) : Jitter(Color.Lerp(P.grassA, P.leafDark, 0.3f), 0.05f);
                solid.Add(lowSphere, p + Vector3.up * size * 0.5f, q, new Vector3(size * 0.95f, size * 0.3f, size * 0.85f), capC);
            }
        }

        static void Bush(Vector3 p, float size, Color c)
        {
            for (int k = 0; k < 4; k++)
            {
                Vector3 o = new Vector3(R(-0.5f, 0.5f), R(0.2f, 0.5f), R(-0.5f, 0.5f)) * size;
                Color cc = k == 3 ? Color.Lerp(c, P.leafLight, 0.5f) : Color.Lerp(c, P.leafDark, R(0f, 0.4f));
                fol.Add(lowSphere, p + o, Quaternion.identity, Vector3.one * size * R(0.7f, 1f), Jitter(cc, 0.05f), 0.06f, 0.5f, 0.3f);
            }
        }

        static void Mushroom(Vector3 p, float size)
        {
            solid.Add(WorldKit.Prim(PrimitiveType.Cylinder), p + Vector3.up * size * 0.3f, Quaternion.identity, new Vector3(size * 0.25f, size * 0.3f, size * 0.25f), new Color(0.95f, 0.92f, 0.85f));
            solid.Add(lowSphere, p + Vector3.up * size * 0.62f, Quaternion.identity, new Vector3(size, size * 0.55f, size), rng.Next(3) == 0 ? new Color(0.95f, 0.8f, 0.4f) : new Color(0.85f, 0.2f, 0.18f));
        }

        static void Crystal(Vector3 p, float size, Color c)
        {
            if (size > 0.7f) Obstacles.AddCircle(p, size * 0.3f);
            for (int k = 0; k < 4; k++)
            {
                var q = Quaternion.Euler(R(-25f, 25f), R(0f, 360f), R(-25f, 25f));
                solid.Add(MeshFactory.FacetCone(5), p, q, new Vector3(size * 0.35f, size * R(0.8f, 1.6f), size * 0.35f), Jitter(c, 0.05f));
            }
        }

        static void Shard(Vector3 p, float size)
        {
            if (size > 0.8f) Obstacles.AddCircle(p, size * 0.22f);
            var q = Quaternion.Euler(R(-20f, 20f), R(0f, 360f), R(-20f, 20f));
            solid.Add(MeshFactory.FacetCone(4), p + Vector3.down * 0.1f, q, new Vector3(size * 0.4f, size * R(1.2f, 2.4f), size * 0.4f), new Color(0.07f, 0.05f, 0.08f));
        }

        static void Basalt(Vector3 p, float size)
        {
            Obstacles.AddCircle(p, size * 0.55f);
            int n = 3 + rng.Next(4);
            for (int k = 0; k < n; k++)
            {
                Vector3 o = new Vector3(R(-0.5f, 0.5f), 0f, R(-0.5f, 0.5f)) * size;
                solid.Add(MeshFactory.FacetCylinder(6), p + o + Vector3.down * 0.2f, Quaternion.Euler(0f, R(0f, 60f), 0f), new Vector3(size * 0.45f, size * R(0.6f, 2f), size * 0.45f), Jitter(P.rock, 0.12f));
            }
        }

        // ------------------------------------------------------------------ Scatter

        static void Nature()
        {
            Templates();
            folMat = MaterialFactory.World(Color.white, 1f, null, P.shadow, P.rim);
            solidMat = MaterialFactory.World(Color.white, 0f, null, P.shadow, P.rim);
            glowMat = MaterialFactory.World(Color.white, 0f, new Color(1f, 0.5f, 0.12f), P.shadow, P.rim);
            fol = new WorldMeshBuilder(root, folMat, "Foliage", false);
            solid = new WorldMeshBuilder(root, solidMat, "Rocks", true);
            farTrees = new WorldMeshBuilder(root, K == Kind.Volcano ? solidMat : folMat, "FarTrees", false);
            glowB = new WorldMeshBuilder(root, glowMat, "Glow", false);
            treeRoot = new GameObject("Trees").transform;
            treeRoot.SetParent(root, false);
            reserved.Clear();
            // Landmarks claim their space first.
            Reserve(boss, bossR + 10f);
            foreach (var pl in J.places) Reserve(pl.pos, pl.radius + 3f);
            ReserveLandmarks();

            // Road edges: grass, flowers, pebbles and stepping stones hugging the path.
            for (float s = 0f; s < J.Length; s += 0.9f)
            {
                Vector3 p = Journey.Flat(J.PointAt(s));
                Vector3 d = Journey.Flat(J.PointAt(s + 1f)) - p;
                if (d.sqrMagnitude < 0.0001f) continue;
                d.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, d);
                for (int sgn = -1; sgn <= 1; sgn += 2)
                {
                    float off = J.halfWidth + R(-0.3f, 3.2f);
                    Vector3 at = p + side * sgn * off + d * R(-0.4f, 0.4f);
                    if (NearChannel(at.x, at.z, 1f)) continue;
                    JourneyPlace pl;
                    if (PlaceDist(at.x, at.z, out pl) < 0.5f) continue;
                    at.y = Ground(at);
                    float patch = WorldKit.Noise(at.x * 0.15f, at.z * 0.15f);
                    EdgeDetail(at, patch, off - J.halfWidth);
                }
            }
            // Clearing rims.
            foreach (var pl in J.places)
            {
                int n = Mathf.RoundToInt(pl.radius * 5f);
                for (int i = 0; i < n; i++)
                {
                    float a = R(0f, Mathf.PI * 2f);
                    Vector3 at = Journey.Flat(pl.pos) + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (pl.radius + R(-0.4f, 3f));
                    if (PathDist(at.x, at.z) < J.halfWidth - 0.5f || NearChannel(at.x, at.z, 1f)) continue;
                    at.y = Ground(at);
                    EdgeDetail(at, WorldKit.Noise(at.x * 0.15f, at.z * 0.15f), 1f);
                }
            }
            // The land around: undergrowth, trees near the road, dense forest walls further out.
            Vector3 mn = J.places[0].pos, mx = mn;
            foreach (var p in J.path) { mn = Vector3.Min(mn, p); mx = Vector3.Max(mx, p); }
            mn -= new Vector3(60f, 0f, 60f);
            mx += new Vector3(60f, 0f, 70f);
            var treeCells = new HashSet<long>();
            for (float z = mn.z; z < mx.z; z += 1.3f)
                for (float x = mn.x; x < mx.x; x += 1.3f)
                {
                    float jx = x + R(-0.6f, 0.6f), jz = z + R(-0.6f, 0.6f);
                    float g = Gap(jx, jz);
                    if (g < 0.3f || g > 55f) continue;
                    if (NearChannel(jx, jz, 1.5f)) continue;
                    bool res = IsReserved(jx, jz);
                    Vector3 at = new Vector3(jx, 0f, jz);
                    at.y = Height(jx, jz);
                    float dens = WorldKit.Noise(jx * 0.05f, jz * 0.05f);
                    float roll = (float)rng.NextDouble();
                    if (g < 9f)
                    {
                        if (!res) Undergrowth(at, dens, roll, g);
                    }
                    // Trees on a coarse grid, in clumps.
                    if (g > 5f && !res)
                    {
                        long cell = ((long)Mathf.FloorToInt(jx / 5.5f) << 32) ^ (long)(Mathf.FloorToInt(jz / 5.5f) & 0xffffffff);
                        if (treeCells.Contains(cell)) continue;
                        float clump = WorldKit.Noise(jx * 0.04f + 7f, jz * 0.04f);
                        float chance = g < 16f ? SS(0.38f, 0.62f, clump) * 0.5f : SS(0.25f, 0.5f, clump) * 0.75f;
                        if (roll > chance) continue;
                        treeCells.Add(cell);
                        float h = R(7f, 12f) * (g < 16f ? 1f : 1.15f);
                        int type = K == Kind.Village ? (clump > 0.6f ? 1 : rng.Next(5) == 0 ? 2 : 0) : K == Kind.Forest ? (clump > 0.62f ? 1 : rng.Next(9) == 0 ? 2 : 0) : K == Kind.Snow ? (rng.Next(6) == 0 ? 2 : 0) : 0;
                        if (g < 16f) NearTree(at, h, type);
                        else FarTree(at, h, type);
                    }
                }
        }

        static void FlushNature()
        {
            fol.Flush();
            solid.Flush();
            farTrees.Flush();
            glowB.Flush();
        }

        /// <summary>The space each set piece will need, claimed before anything is scattered.</summary>
        static void ReserveLandmarks()
        {
            if (hasBridge) Reserve(bridgeCenter, 9f);
            if (K == Kind.Village) { VillageReserve(); return; }
            for (int i = 0; i < J.places.Count; i++)
            {
                var pl = J.places[i];
                Vector3 c = Journey.Flat(pl.pos);
                Vector3 dir = i > 0 ? (c - Journey.Flat(J.places[i - 1].pos)).normalized : Vector3.forward;
                if (pl.isBossArena)
                {
                    if (K == Kind.Forest) Reserve(c + dir * (pl.radius + 10f), 17f);
                    else if (K == Kind.Snow) { Reserve(c + dir * (pl.radius + 12f), 10f); Reserve(c + dir * (pl.radius + 26f), 16f); }
                    else Reserve(c, pl.radius + 14f);
                }
                else if (pl.name.Contains("Camp")) Reserve(c, pl.radius + 4f);
                else if (pl.name.Contains("Ruins") || pl.name.Contains("Shrine") || pl.name.Contains("Temple")) Reserve(c, pl.radius + 7f);
                else Reserve(c, pl.radius + 5f);
            }
        }

        static void FarTree(Vector3 p, float h, int type)
        {
            switch (K)
            {
                case Kind.Snow: SnowPine(farTrees, p, h); break;
                case Kind.Volcano: BurntTree(farTrees, glowB, p, h * 0.7f); break;
                case Kind.Village:
                    if (type == 1) Cedar(farTrees, p, h * 1.25f, VillageGreen);
                    else
                    {
                        farTrees.Add(trunkMesh, p + Vector3.down * 0.2f, Quaternion.identity, new Vector3(h * 0.06f, h * 0.6f, h * 0.06f), P.trunk);
                        Color leaf = type == 2 ? VillageGreen : Color.Lerp(P.leafDark, P.leafMid, R(0.2f, 1f));
                        farTrees.Add(lowSphere, p + Vector3.up * h * 0.7f, Quaternion.identity, Vector3.one * h * 0.62f, Jitter(leaf, 0.05f), 0.12f, 0.5f, 0.4f);
                        farTrees.Add(lowSphere, p + new Vector3(h * 0.15f, h * 0.9f, 0f), Quaternion.identity, Vector3.one * h * 0.36f, Jitter(Color.Lerp(leaf, P.leafLight, 0.4f), 0.05f), 0.14f, 0.5f, 0.4f);
                    }
                    break;
                default:
                    if (type == 1) Cedar(farTrees, p, h * 1.25f, P.leafMid);
                    else
                    {
                        // Cheaper crown for the forest wall: three big clusters.
                        farTrees.Add(trunkMesh, p + Vector3.down * 0.2f, Quaternion.identity, new Vector3(h * 0.06f, h * 0.6f, h * 0.06f), P.trunk);
                        Color leaf = Color.Lerp(P.leafDark, P.leafMid, R(0f, 1f));
                        farTrees.Add(lowSphere, p + Vector3.up * h * 0.7f, Quaternion.identity, Vector3.one * h * 0.62f, Jitter(leaf, 0.05f), 0.12f, 0.5f, 0.4f);
                        farTrees.Add(lowSphere, p + new Vector3(h * 0.15f, h * 0.9f, 0f), Quaternion.identity, Vector3.one * h * 0.36f, Jitter(Color.Lerp(leaf, P.leafLight, 0.5f), 0.05f), 0.14f, 0.5f, 0.4f);
                    }
                    break;
            }
        }

        /// <summary>Evergreen foliage between the cherry trees of Kiriha (dark teal under moonlight).</summary>
        static readonly Color VillageGreen = new Color(0.16f, 0.34f, 0.32f);

        static void EdgeDetail(Vector3 at, float patch, float fromEdge)
        {
            float roll = (float)rng.NextDouble();
            // Village streets have tidy edges: fewer weeds, and none in front of the houses.
            if (K == Kind.Village && (roll < 0.5f || IsReserved(at.x, at.z))) return;
            switch (K)
            {
                case Kind.Snow:
                    if (patch > 0.55f && roll < 0.5f) Tuft(at, R(0.35f, 0.55f), P.flowerA);
                    else if (roll < 0.12f) Rock(at, R(0.3f, 0.7f), P.rock, true);
                    else if (roll < 0.2f) solid.Add(lowSphere, at, Quaternion.identity, new Vector3(R(0.8f, 1.6f), R(0.3f, 0.5f), R(0.8f, 1.4f)), new Color(0.96f, 0.98f, 1f));
                    else if (roll < 0.24f && fromEdge > 1.2f) Crystal(at, R(0.4f, 0.8f), new Color(0.65f, 0.88f, 1f));
                    break;
                case Kind.Volcano:
                    if (roll < 0.14f) Rock(at, R(0.3f, 0.7f), P.rockDark, false);
                    else if (roll < 0.22f && fromEdge > 1f) Shard(at, R(0.3f, 0.6f));
                    else if (patch > 0.6f && roll < 0.4f) Tuft(at, R(0.3f, 0.45f), new Color(0.25f, 0.2f, 0.16f));
                    else if (roll < 0.26f) glowB.Add(lowSphere, at + Vector3.down * 0.05f, Quaternion.identity, new Vector3(R(0.2f, 0.5f), 0.08f, R(0.2f, 0.5f)), new Color(1f, 0.45f, 0.1f), 1f, 1f, 1f);
                    break;
                default:
                    if (patch > 0.45f && roll < 0.8f) Tuft(at, R(0.35f, 0.6f), Color.Lerp(P.grassA, P.grassC, R(0f, 1f)));
                    if (patch > 0.62f && roll < 0.35f) Flower(at + new Vector3(R(-0.3f, 0.3f), 0f, R(-0.3f, 0.3f)), roll < 0.12f ? P.flowerA : roll < 0.24f ? P.flowerB : P.flowerC);
                    else if (roll > 0.94f) Rock(at, R(0.25f, 0.55f), P.rock, roll > 0.97f);
                    else if (roll > 0.9f && fromEdge < 0.6f) solid.Add(lowSphere, at, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(R(0.5f, 0.8f), 0.12f, R(0.4f, 0.7f)), Jitter(P.rock, 0.05f)); // stepping stone
                    break;
            }
        }

        static void Undergrowth(Vector3 at, float dens, float roll, float g)
        {
            switch (K)
            {
                case Kind.Snow:
                    if (roll < 0.05f) Rock(at, R(0.6f, 1.6f), P.rock, true);
                    else if (roll < 0.1f) solid.Add(lowSphere, at, Quaternion.Euler(0f, R(0f, 360f), 0f), new Vector3(R(1.2f, 2.6f), R(0.4f, 0.9f), R(1.2f, 2.2f)), new Color(0.95f, 0.97f, 1f));
                    else if (roll < 0.13f) Crystal(at, R(0.5f, 1.2f), new Color(0.62f, 0.86f, 1f));
                    else if (dens > 0.55f && roll < 0.3f) Tuft(at, R(0.4f, 0.6f), P.flowerA);
                    break;
                case Kind.Volcano:
                    if (roll < 0.05f) Basalt(at, R(0.6f, 1.2f));
                    else if (roll < 0.1f) Rock(at, R(0.6f, 1.5f), P.rockDark, false);
                    else if (roll < 0.15f) Shard(at, R(0.5f, 1.1f));
                    else if (dens > 0.6f && roll < 0.25f) Tuft(at, R(0.35f, 0.5f), new Color(0.22f, 0.17f, 0.13f));
                    break;
                default:
                    if (dens > 0.4f && roll < 0.45f) Tuft(at, R(0.45f, 0.75f), Color.Lerp(P.grassA, P.bank, R(0f, 1f)));
                    if (roll < 0.035f) Bush(at, R(0.6f, 1.1f), Color.Lerp(P.leafMid, P.leafDark, R(0f, 0.6f)));
                    else if (roll < 0.06f) fol.Add(fernMesh, at, Quaternion.Euler(0f, R(0f, 360f), 0f), Vector3.one * R(0.7f, 1.1f), Jitter(P.leafMid, 0.08f), 0.12f, 1f, 0f);
                    else if (roll < 0.075f) Rock(at, R(0.4f, 1.2f), P.rock, true);
                    else if (roll < 0.085f && g > 1.5f) Mushroom(at, R(0.2f, 0.35f));
                    else if (dens > 0.6f && roll < 0.12f) Flower(at, roll < 0.1f ? P.flowerB : P.flowerC);
                    break;
            }
        }
    }
}
