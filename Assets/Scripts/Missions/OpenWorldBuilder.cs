using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The open world: Kiriha Village on a peaceful day, free to walk around. A plaza with a well and cherry trees,
    /// four roads lined with houses, a market street, a dojo, a shrine up the steps, a river with a bridge, a pond,
    /// farm fields, groves of trees, villagers going about their day, and a few treasure chests to find.
    /// Houses, trees and big props are solid: <see cref="Obstacles"/> keeps the team from walking through them.
    /// </summary>
    public static class OpenWorldBuilder
    {
        public const float Radius = 62f;

        /// <summary>Solid circles (x, z = centre, w = radius) the team can't walk through.</summary>
        public static readonly List<Vector3> Obstacles = new List<Vector3>();

        static System.Random rng;
        static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static bool Chance(double p) { return rng.NextDouble() < p; }

        static Material grass, grassDark, road, wall, wallAlt, timber, roofA, roofB, roofC, window, leaf, leafDark, blossom, bark, stone, water, wood, cloth1, cloth2, cloth3, gold, crop;

        public static ArenaTheme Theme()
        {
            return new ArenaTheme
            {
                kind = EnvironmentKind.Village, night = false, burning = false, petals = true,
                ground = new Color(0.42f, 0.62f, 0.32f), groundAccent = new Color(0.55f, 0.45f, 0.3f),
                sky = new Color(0.52f, 0.74f, 0.95f), fog = new Color(0.72f, 0.84f, 0.95f), fogStart = 45f, fogEnd = 120f,
                lantern = new Color(1f, 0.75f, 0.4f), foliage = new Color(1f, 0.72f, 0.82f),
                sun = new Color(1f, 0.96f, 0.88f), sunIntensity = 1.15f
            };
        }

        public static GameObject Build(Transform parent)
        {
            rng = new System.Random(20260928);
            Obstacles.Clear();
            var root = new GameObject("OpenWorld");
            root.transform.SetParent(parent, false);
            var dynGo = new GameObject("OpenWorldDynamic");
            dynGo.transform.SetParent(parent, false);
            var dyn = dynGo.transform;
            var t = root.transform;
            Materials();

            // Ground, plaza and roads.
            Flat(t, MeshFactory.PlanarDisc(), Vector3.zero, Radius + 40f, grass);
            for (int i = 0; i < 40; i++)
            {
                Vector2 p = Random2(Radius);
                Flat(t, MeshFactory.PlanarDisc(), new Vector3(p.x, 0.004f, p.y), R(2f, 6f), grassDark);
            }
            Flat(t, MeshFactory.PlanarDisc(), new Vector3(0f, 0.01f, 0f), 11f, road);
            for (int i = 0; i < 4; i++)
            {
                var rd = MeshFactory.MeshObject(MeshFactory.Line(), t, new Vector3(0f, 0.012f, 0f), new Vector3(5f, 1f, Radius - 4f), road, false);
                rd.transform.localRotation = Quaternion.Euler(0f, i * 90f, 0f);
            }

            Plaza(t, dyn);
            // Houses along the four roads, both sides.
            for (int arm = 0; arm < 4; arm++)
            {
                Quaternion q = Quaternion.Euler(0f, arm * 90f, 0f);
                for (float d = 16f; d < Radius - 10f; d += R(10f, 13f))
                    for (int side = -1; side <= 1; side += 2)
                    {
                        if (arm == 0 && d > 40f) continue; // the shrine steps
                        if (arm == 1 && d < 46f) continue; // the market street
                        if (arm == 3 && d > 34f && d < 54f) continue; // the river
                        if (Chance(0.15)) continue;
                        Vector3 p = q * new Vector3(side * R(8f, 10f), 0f, d);
                        House(t, dyn, p, Quaternion.LookRotation(q * new Vector3(-side, 0f, 0f)), Chance(0.5));
                    }
            }
            Market(t, dyn);
            Dojo(t, dyn, new Vector3(-30f, 0f, 24f));
            Shrine(t, dyn, new Vector3(0f, 0f, 50f));
            River(t, dyn);
            Pond(t, new Vector3(28f, 0f, 30f));
            Farms(t, new Vector3(34f, 0f, -34f));
            Groves(t, dyn);
            Chests(dyn);
            Villagers(dyn);
            BirdFlock.Create(dyn, new Vector3(0f, 0f, 10f), 8, Color.white).Radius = 40f;
            CloudDrift.CreateLayer(dyn, new Vector3(0f, 0f, 30f), 8, 28f, new Color(1f, 1f, 1f, 0.6f), 90f);
            EnvFx.Weather(dyn, Vector3.zero, "leaves", 60f);

            StaticBatchingUtility.Combine(root);
            return root;
        }

        static void Materials()
        {
            grass = MaterialFactory.Toon(new Color(0.42f, 0.64f, 0.32f), 0f);
            grassDark = MaterialFactory.Toon(new Color(0.36f, 0.56f, 0.28f), 0f);
            road = MaterialFactory.Toon(new Color(0.72f, 0.6f, 0.44f), 0f);
            wall = MaterialFactory.Toon(new Color(0.93f, 0.88f, 0.78f));
            wallAlt = MaterialFactory.Toon(new Color(0.85f, 0.76f, 0.62f));
            timber = MaterialFactory.Toon(new Color(0.32f, 0.2f, 0.12f));
            roofA = MaterialFactory.Toon(new Color(0.3f, 0.36f, 0.5f));
            roofB = MaterialFactory.Toon(new Color(0.62f, 0.25f, 0.2f));
            roofC = MaterialFactory.Toon(new Color(0.35f, 0.3f, 0.26f));
            window = MaterialFactory.Toon(new Color(1f, 0.88f, 0.6f), 0f, new Color(0.5f, 0.4f, 0.2f));
            leaf = MaterialFactory.Toon(new Color(0.3f, 0.58f, 0.3f));
            leafDark = MaterialFactory.Toon(new Color(0.2f, 0.42f, 0.24f));
            blossom = MaterialFactory.Toon(new Color(1f, 0.74f, 0.84f));
            bark = MaterialFactory.Toon(new Color(0.42f, 0.3f, 0.2f));
            stone = MaterialFactory.Toon(new Color(0.62f, 0.62f, 0.64f));
            water = MaterialFactory.Toon(new Color(0.35f, 0.62f, 0.9f), 0f, new Color(0.05f, 0.12f, 0.2f));
            wood = MaterialFactory.Toon(new Color(0.55f, 0.38f, 0.22f));
            cloth1 = MaterialFactory.Toon(new Color(0.85f, 0.25f, 0.25f));
            cloth2 = MaterialFactory.Toon(new Color(0.25f, 0.5f, 0.85f));
            cloth3 = MaterialFactory.Toon(new Color(0.95f, 0.8f, 0.3f));
            gold = MaterialFactory.Toon(new Color(1f, 0.8f, 0.25f), 0.01f, new Color(0.4f, 0.28f, 0f));
            crop = MaterialFactory.Toon(new Color(0.55f, 0.72f, 0.25f), 0f);
        }

        static Vector2 Random2(float r) { return Random2(r, rng); }
        static Vector2 Random2(float r, System.Random rand)
        {
            float a = (float)rand.NextDouble() * Mathf.PI * 2f, d = Mathf.Sqrt((float)rand.NextDouble()) * r;
            return new Vector2(Mathf.Cos(a) * d, Mathf.Sin(a) * d);
        }

        static void Flat(Transform t, Mesh m, Vector3 p, float radius, Material mat)
        {
            var go = MeshFactory.MeshObject(m, t, p, new Vector3(radius, 1f, radius), mat, false);
            go.GetComponent<Renderer>().receiveShadows = true;
        }

        static GameObject Box(Transform t, Vector3 p, Vector3 s, Material m, float yaw = 0f)
        {
            var go = MeshFactory.Primitive(PrimitiveType.Cube, t, p, s, m);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static void Solid(Vector3 p, float r) { Obstacles.Add(new Vector3(p.x, 0f, p.z) + Vector3.up * r); }

        /// <summary>Pushes a point out of every solid thing and keeps it inside the village.</summary>
        public static Vector3 Clamp(Vector3 p)
        {
            p.y = 0f;
            float m = new Vector2(p.x, p.z).magnitude;
            if (m > Radius) p *= Radius / m;
            foreach (var o in Obstacles)
            {
                Vector3 d = new Vector3(p.x - o.x, 0f, p.z - o.z);
                float r = o.y;
                if (d.sqrMagnitude < r * r)
                {
                    if (d.sqrMagnitude < 0.0001f) d = Vector3.forward;
                    p = new Vector3(o.x, 0f, o.z) + d.normalized * r;
                }
            }
            return p;
        }

        // ------------------------------------------------------------------ Pieces

        static void House(Transform t, Transform dyn, Vector3 p, Quaternion facing, bool big)
        {
            var h = new GameObject("House").transform;
            h.SetParent(t, false);
            h.position = p;
            h.rotation = facing;
            float w = big ? R(6f, 7.5f) : R(4.5f, 5.5f), ht = R(2.8f, 3.4f), d = R(4f, 5f);
            var wm = Chance(0.5) ? wall : wallAlt;
            var rm = rng.Next(3) == 0 ? roofA : rng.Next(2) == 0 ? roofB : roofC;
            MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(0f, 0.2f, 0f), new Vector3(w + 0.6f, 0.4f, d + 0.6f), stone);
            MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(0f, ht * 0.5f + 0.3f, 0f), new Vector3(w, ht, d), wm);
            foreach (float sx in new[] { -0.5f, 0.5f })
                foreach (float sz in new[] { -0.5f, 0.5f })
                    MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(w * sx, ht * 0.5f + 0.3f, d * sz), new Vector3(0.22f, ht, 0.22f), timber);
            MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(0f, ht + 0.3f, 0f), new Vector3(w + 0.1f, 0.2f, d + 0.1f), timber);
            for (int s = -1; s <= 1; s += 2)
            {
                var roof = MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(0f, ht + 1.05f, s * d * 0.27f), new Vector3(w + 1.2f, 0.22f, d * 0.66f), rm);
                roof.transform.localRotation = Quaternion.Euler(s * 30f, 0f, 0f);
            }
            MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(0f, ht + 1.62f, 0f), new Vector3(w + 1.3f, 0.2f, 0.3f), timber);
            // Door and windows on the road side (+Z).
            MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(0f, 1.2f, d * 0.5f + 0.02f), new Vector3(1.1f, 1.8f, 0.06f), timber);
            for (int s = -1; s <= 1; s += 2)
            {
                var win = MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(s * w * 0.3f, ht * 0.6f + 0.3f, d * 0.5f + 0.02f), new Vector3(0.9f, 0.7f, 0.05f), window);
                win.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // A hanging lantern and a flower box or barrel.
            var lant = MeshFactory.Primitive(PrimitiveType.Sphere, h, new Vector3(w * 0.5f - 0.4f, 2.2f, d * 0.5f + 0.5f), new Vector3(0.4f, 0.55f, 0.4f), cloth1);
            lant.AddComponent<Sway>().Amount = 6f;
            if (Chance(0.5)) MeshFactory.Primitive(PrimitiveType.Cylinder, h, new Vector3(-w * 0.5f - 0.6f, 0.45f, d * 0.5f), new Vector3(0.7f, 0.45f, 0.7f), wood);
            else MeshFactory.Primitive(PrimitiveType.Cube, h, new Vector3(-w * 0.25f, 0.5f, d * 0.5f + 0.4f), new Vector3(1.4f, 0.3f, 0.4f), blossom);
            Solid(p, Mathf.Max(w, d) * 0.55f);
        }

        static void Tree(Transform t, Vector3 p, float hgt, bool cherry)
        {
            MeshFactory.Primitive(PrimitiveType.Cylinder, t, p + Vector3.up * hgt * 0.5f, new Vector3(0.45f, hgt * 0.5f, 0.45f), bark);
            var lm = cherry ? blossom : Chance(0.5) ? leaf : leafDark;
            for (int k = 0; k < 4; k++)
                MeshFactory.Primitive(PrimitiveType.Sphere, t, p + new Vector3(R(-1f, 1f), hgt + R(-0.2f, 1f), R(-1f, 1f)), Vector3.one * R(2f, 3.2f), lm);
            Solid(p, 0.7f);
        }

        static void Pine(Transform t, Vector3 p, float hgt)
        {
            MeshFactory.Primitive(PrimitiveType.Cylinder, t, p + Vector3.up * 0.6f, new Vector3(0.4f, 0.6f, 0.4f), bark);
            for (int k = 0; k < 3; k++)
                MeshFactory.MeshObject(MeshFactory.Cone(), t, p + Vector3.up * (1f + k * hgt * 0.22f), new Vector3(hgt * (0.55f - k * 0.13f), hgt * 0.4f, hgt * (0.55f - k * 0.13f)), leafDark);
            Solid(p, 0.8f);
        }

        static void Plaza(Transform t, Transform dyn)
        {
            // A stone well in the middle, benches and cherry trees around it.
            MeshFactory.Primitive(PrimitiveType.Cylinder, t, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 0.5f, 2.4f), stone);
            Flat(t, MeshFactory.Disc(), new Vector3(0f, 1.02f, 0f), 1f, water);
            for (int s = -1; s <= 1; s += 2) MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(s * 1f, 1.6f, 0f), new Vector3(0.18f, 1.2f, 0.18f), wood);
            var roof = MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(0f, 2.3f, 0f), new Vector3(2.8f, 0.2f, 1.6f), roofB);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            Solid(Vector3.zero, 1.8f);
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward * 8.5f;
                Tree(t, p, 3.2f, true);
                Vector3 b = Quaternion.Euler(0f, 45f + i * 90f, 0f) * Vector3.forward * 5.5f;
                var bench = Box(t, b + Vector3.up * 0.45f, new Vector3(2f, 0.15f, 0.6f), wood, 45f + i * 90f + 90f);
                bench.name = "Bench";
            }
            // Stone lanterns at the plaza corners (they light up at dusk... someday).
            for (int i = 0; i < 4; i++)
            {
                Vector3 p = Quaternion.Euler(0f, i * 90f, 0f) * new Vector3(4f, 0f, 10.5f);
                MeshFactory.Primitive(PrimitiveType.Cylinder, t, p + Vector3.up * 0.45f, new Vector3(0.3f, 0.45f, 0.3f), stone);
                MeshFactory.Primitive(PrimitiveType.Cube, t, p + Vector3.up * 1.05f, new Vector3(0.5f, 0.38f, 0.5f), window);
                var cap = MeshFactory.Primitive(PrimitiveType.Cube, t, p + Vector3.up * 1.38f, new Vector3(0.85f, 0.16f, 0.85f), stone);
                cap.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            }
        }

        static void Market(Transform t, Transform dyn)
        {
            // A row of stalls with striped awnings down the east road.
            Material[] cloths = { cloth1, cloth2, cloth3 };
            for (int i = 0; i < 6; i++)
            {
                Vector3 p = new Vector3(16f + i * 5.2f, 0f, i % 2 == 0 ? 4.5f : -4.5f);
                var s = new GameObject("Stall").transform;
                s.SetParent(t, false);
                s.position = p;
                s.rotation = Quaternion.Euler(0f, i % 2 == 0 ? 180f : 0f, 0f);
                MeshFactory.Primitive(PrimitiveType.Cube, s, new Vector3(0f, 0.5f, 0f), new Vector3(3.2f, 1f, 1.4f), wood);
                foreach (float sx in new[] { -1.5f, 1.5f })
                    MeshFactory.Primitive(PrimitiveType.Cylinder, s, new Vector3(sx, 1.2f, -0.6f), new Vector3(0.12f, 1.2f, 0.12f), wood);
                var aw = MeshFactory.Primitive(PrimitiveType.Cube, s, new Vector3(0f, 2.45f, 0f), new Vector3(3.6f, 0.12f, 2f), cloths[i % 3]);
                aw.transform.localRotation = Quaternion.Euler(12f, 0f, 0f);
                for (int k = 0; k < 4; k++)
                    MeshFactory.Primitive(PrimitiveType.Sphere, s, new Vector3(-1.1f + k * 0.7f, 1.15f, 0.1f), Vector3.one * 0.4f, k % 2 == 0 ? cloth1 : cloth3);
                MeshFactory.Primitive(PrimitiveType.Cube, s, new Vector3(1.9f, 0.35f, 0.5f), new Vector3(0.7f, 0.7f, 0.7f), wood);
                Solid(p, 1.8f);
            }
        }

        static void Dojo(Transform t, Transform dyn, Vector3 p)
        {
            var d = new GameObject("Dojo").transform;
            d.SetParent(t, false);
            d.position = p;
            d.rotation = Quaternion.LookRotation(-p.normalized);
            MeshFactory.Primitive(PrimitiveType.Cube, d, new Vector3(0f, 0.3f, 0f), new Vector3(12f, 0.6f, 9f), stone);
            MeshFactory.Primitive(PrimitiveType.Cube, d, new Vector3(0f, 2.4f, 0f), new Vector3(11f, 3.6f, 8f), wall);
            for (int s = -1; s <= 1; s += 2)
            {
                var roof = MeshFactory.Primitive(PrimitiveType.Cube, d, new Vector3(0f, 5f, s * 2.3f), new Vector3(13.5f, 0.3f, 5.6f), roofA);
                roof.transform.localRotation = Quaternion.Euler(s * 28f, 0f, 0f);
            }
            MeshFactory.Primitive(PrimitiveType.Cube, d, new Vector3(0f, 2f, 4.02f), new Vector3(3f, 2.6f, 0.06f), timber);
            MeshFactory.Primitive(PrimitiveType.Cube, d, new Vector3(0f, 3.9f, 4.1f), new Vector3(2.4f, 0.6f, 0.08f), gold);
            // Training posts in the yard.
            for (int i = 0; i < 3; i++) MeshFactory.Primitive(PrimitiveType.Cylinder, d, new Vector3(-3f + i * 3f, 0.9f, 7f), new Vector3(0.35f, 0.9f, 0.35f), wood);
            Solid(p, 6.5f);
        }

        static void Shrine(Transform t, Transform dyn, Vector3 p)
        {
            // Steps up to a small shrine with a red torii gate.
            for (int i = 0; i < 6; i++)
                MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 0.12f + i * 0.12f, -8f + i * 1.1f), new Vector3(7f - i * 0.4f, 0.24f + i * 0.24f, 1.1f), stone);
            var red = MaterialFactory.Toon(new Color(0.85f, 0.18f, 0.15f));
            for (int s = -1; s <= 1; s += 2) MeshFactory.Primitive(PrimitiveType.Cylinder, t, p + new Vector3(s * 2.4f, 2.4f, -9.5f), new Vector3(0.45f, 2.4f, 0.45f), red);
            MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 4.6f, -9.5f), new Vector3(6.6f, 0.4f, 0.6f), red);
            MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 5.1f, -9.5f), new Vector3(7.6f, 0.3f, 0.8f), timber);
            MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 0.9f, 0f), new Vector3(8f, 1.8f, 7f), stone);
            MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 3.4f, 0f), new Vector3(6f, 3.2f, 5f), wall);
            for (int s = -1; s <= 1; s += 2)
            {
                var roof = MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 5.7f, s * 1.5f), new Vector3(7.6f, 0.3f, 3.8f), red);
                roof.transform.localRotation = Quaternion.Euler(s * 32f, 0f, 0f);
            }
            Solid(p, 5f);
            for (int s = -1; s <= 1; s += 2) Solid(p + new Vector3(s * 2.4f, 0f, -9.5f), 0.5f);
            for (int i = 0; i < 10; i++) Pine(t, p + new Vector3(R(-14f, 14f), 0f, R(3f, 10f)) + (i % 2 == 0 ? Vector3.left * 3f : Vector3.right * 3f), R(4f, 6.5f));
        }

        static void River(Transform t, Transform dyn)
        {
            // A river across the west side, with a wooden bridge where the west road crosses.
            for (int i = -6; i <= 6; i++)
            {
                Vector3 p = new Vector3(-44f + Mathf.Sin(i * 0.5f) * 3f, 0.02f, i * 9f);
                var seg = MeshFactory.MeshObject(MeshFactory.Disc(), t, p, new Vector3(5f, 1f, 7f), water, false);
                seg.GetComponent<Renderer>().receiveShadows = true;
                if (Mathf.Abs(i) > 0) { Solid(p + Vector3.left * 1.5f, 3.2f); Solid(p + Vector3.right * 1.5f, 3.2f); }
            }
            MeshFactory.Primitive(PrimitiveType.Cube, t, new Vector3(-44f, 0.35f, 0f), new Vector3(11f, 0.3f, 5f), wood);
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < 5; k++)
                    MeshFactory.Primitive(PrimitiveType.Cylinder, t, new Vector3(-49f + k * 2.5f, 0.9f, s * 2.4f), new Vector3(0.18f, 0.55f, 0.18f), wood);
            for (int i = 0; i < 8; i++) MeshFactory.Primitive(PrimitiveType.Sphere, t, new Vector3(-44f + R(-4f, 4f), 0.1f, R(-50f, 50f)), new Vector3(R(0.6f, 1.2f), 0.4f, R(0.6f, 1.2f)), stone);
        }

        static void Pond(Transform t, Vector3 p)
        {
            Flat(t, MeshFactory.Disc(), p + Vector3.up * 0.02f, 6f, water);
            for (int i = 0; i < 14; i++)
            {
                Vector3 s = p + Quaternion.Euler(0f, i * 360f / 14f, 0f) * Vector3.forward * 6.2f;
                MeshFactory.Primitive(PrimitiveType.Sphere, t, s + Vector3.up * 0.1f, new Vector3(R(0.8f, 1.4f), 0.5f, R(0.8f, 1.4f)), stone);
            }
            for (int i = 0; i < 5; i++)
            {
                Vector2 o = Random2(4f);
                MeshFactory.MeshObject(MeshFactory.Disc(), t, p + new Vector3(o.x, 0.04f, o.y), new Vector3(0.6f, 1f, 0.6f), leaf, false);
            }
            Solid(p, 5.6f);
        }

        static void Farms(Transform t, Vector3 p)
        {
            for (int f = 0; f < 2; f++)
            {
                Vector3 c = p + new Vector3(f * 12f - 6f, 0f, 0f);
                Flat(t, MeshFactory.PlanarDisc(), c + Vector3.up * 0.008f, 5f, MaterialFactory.Toon(new Color(0.48f, 0.36f, 0.24f), 0f));
                for (int row = 0; row < 6; row++)
                    for (int k = 0; k < 7; k++)
                        MeshFactory.Primitive(PrimitiveType.Capsule, t, c + new Vector3(k * 1.1f - 3.3f, 0.3f, row * 1.1f - 2.7f), new Vector3(0.35f, 0.3f, 0.35f), crop);
            }
            // A scarecrow.
            MeshFactory.Primitive(PrimitiveType.Cylinder, t, p + new Vector3(0f, 1.2f, 5f), new Vector3(0.12f, 1.2f, 0.12f), wood);
            MeshFactory.Primitive(PrimitiveType.Cube, t, p + new Vector3(0f, 1.8f, 5f), new Vector3(1.6f, 0.12f, 0.12f), wood);
            MeshFactory.Primitive(PrimitiveType.Sphere, t, p + new Vector3(0f, 2.5f, 5f), Vector3.one * 0.5f, cloth3);
        }

        static void Groves(Transform t, Transform dyn)
        {
            // Trees fill the gaps between neighbourhoods, and a thick forest rings the village edge.
            int placed = 0;
            for (int attempt = 0; attempt < 400 && placed < 70; attempt++)
            {
                Vector2 q = Random2(Radius - 4f);
                Vector3 p = new Vector3(q.x, 0f, q.y);
                if (Mathf.Abs(p.x) < 6f || Mathf.Abs(p.z) < 6f) continue; // keep the roads clear
                if (p.magnitude < 13f) continue;
                if (Blocked(p, 2.5f)) continue;
                if (Chance(0.25)) Pine(t, p, R(4f, 6.5f)); else Tree(t, p, R(2.6f, 3.8f), Chance(0.15));
                placed++;
            }
            for (int i = 0; i < 64; i++)
            {
                float a = i * 360f / 64f + R(-2f, 2f);
                Vector3 p = Quaternion.Euler(0f, a, 0f) * Vector3.forward * (Radius + R(2f, 8f));
                if (Chance(0.5)) Pine(t, p, R(6f, 9f)); else Tree(t, p, R(3.5f, 5f), false);
            }
            // Bushes and flowers.
            for (int i = 0; i < 90; i++)
            {
                Vector2 q = Random2(Radius - 2f);
                Vector3 p = new Vector3(q.x, 0f, q.y);
                if (Mathf.Abs(p.x) < 3f || Mathf.Abs(p.z) < 3f || Blocked(p, 1f)) continue;
                var b = MeshFactory.Primitive(PrimitiveType.Sphere, t, p + Vector3.up * 0.3f, new Vector3(R(0.8f, 1.4f), R(0.6f, 0.9f), R(0.8f, 1.4f)), Chance(0.3) ? blossom : leaf);
                b.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static bool Blocked(Vector3 p, float margin)
        {
            foreach (var o in Obstacles)
                if (new Vector2(p.x - o.x, p.z - o.z).magnitude < o.y + margin) return true;
            return false;
        }

        // ------------------------------------------------------------------ Life

        public static readonly List<Transform> ChestSpots = new List<Transform>();

        static void Chests(Transform dyn)
        {
            ChestSpots.Clear();
            Vector3[] spots = { new Vector3(-30f, 0f, 14f), new Vector3(38f, 0f, 22f), new Vector3(20f, 0f, -44f), new Vector3(-52f, 0f, -30f), new Vector3(6f, 0f, 44f), new Vector3(-20f, 0f, -48f) };
            foreach (var s in spots)
            {
                Vector3 p = Clamp(s);
                var c = new GameObject("Chest").transform;
                c.SetParent(dyn, false);
                c.position = p;
                c.rotation = Quaternion.Euler(0f, R(0f, 360f), 0f);
                MeshFactory.Primitive(PrimitiveType.Cube, c, new Vector3(0f, 0.35f, 0f), new Vector3(1f, 0.7f, 0.7f), wood);
                var lid = MeshFactory.Primitive(PrimitiveType.Cube, c, new Vector3(0f, 0.78f, 0f), new Vector3(1.05f, 0.18f, 0.75f), gold);
                lid.name = "Lid";
                MeshFactory.Primitive(PrimitiveType.Cube, c, new Vector3(0f, 0.5f, 0.36f), new Vector3(0.2f, 0.25f, 0.05f), gold);
                MeshFactory.MeshObject(MeshFactory.Ring(0.8f), c, Vector3.up * 0.05f, Vector3.one * 1.3f, MaterialFactory.Additive(new Color(1f, 0.85f, 0.3f, 0.7f)), false).AddComponent<Pulse>().Speed = 3f;
                ChestSpots.Add(c);
            }
        }

        static readonly string[][] Talk =
        {
            new[] { "Beautiful day, isn't it?", "The cherry trees bloomed early this year.", "My grandson wants to be a slayer. I told him to eat his vegetables first." },
            new[] { "Fresh dumplings! Two for one!", "Have you tried the river fish? Best in the valley.", "Business is good now that the demons are gone." },
            new[] { "The shrine up the steps is very old. Older than the village.", "I heard a treasure chest is hidden near the pond...", "Thank you for saving us, slayers!" },
            new[] { "I'm on patrol. Very serious patrol.", "All quiet on the east road.", "Stay sharp — the forest is never fully safe." },
            new[] { "Keep your stance low and your heart steady.", "Visit the dojo when you have time. The floor needs sweeping.", "You've grown strong." },
            new[] { "Want to trade? I have ribbons, bells and very shiny rocks.", "Diamonds? You want the summoning shrine for those.", "Buy three, get... well, three." },
        };

        static void Villagers(Transform dyn)
        {
            string[] ids = { "npc_villager", "npc_merchant", "npc_villager2", "npc_soldier", "npc_tessai", "npc_merchant" };
            Vector3[] hubs =
            {
                Vector3.zero, new Vector3(22f, 0f, 0f), new Vector3(0f, 0f, 24f), new Vector3(0f, 0f, -26f), new Vector3(-26f, 0f, 18f),
                new Vector3(-22f, 0f, -2f), new Vector3(30f, 0f, -30f), new Vector3(0f, 0f, 38f), new Vector3(34f, 0f, 20f), new Vector3(-36f, 0f, -24f)
            };
            int n = 0;
            foreach (var hub in hubs)
            {
                int count = hub == Vector3.zero ? 5 : 2 + rng.Next(2);
                for (int i = 0; i < count; i++, n++)
                {
                    int kind = n % ids.Length;
                    if (kind == 4 && hub != new Vector3(-26f, 0f, 18f)) kind = 0; // Master Tessai stays near the dojo
                    var w = NpcWalker.Spawn(dyn, ids[kind], hub, 2f, hub == Vector3.zero ? 9f : 6f, Talk[kind]);
                    if (w != null) w.Speed = R(1.1f, 1.7f);
                }
            }
        }
    }
}
