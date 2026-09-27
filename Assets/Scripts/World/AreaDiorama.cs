using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Bright low-poly dioramas for the chapter maps, modelled on the reference sheets:
    /// Meadow (pine forest, torii, river and bridge, treasure), Snow (icy cliffs, shrine, purple portal,
    /// wooden bridges), Crimson (red moon, dead trees, lava cracks, pagoda on a cliff) and Coast (sea,
    /// sandy island, ship, village houses, torii, lighthouse). A trail of stepping stones winds across
    /// each one; missions sit on dark stone pedestals along it.
    /// </summary>
    public static class AreaDiorama
    {
        public enum Look { Meadow, Snow, Crimson, Coast, Village, Ruins, Shrine, DarkForest }

        public class Result
        {
            public GameObject root;
            public readonly List<Vector3> nodes = new List<Vector3>();
            public Look look;
            public Color sky, fog, ambient, sun;
            public float sunIntensity;
        }

        public static Look LookFor(string regionId)
        {
            switch (regionId)
            {
                case "village": return Look.Village;
                case "forest": return Look.Meadow;
                case "mountain": return Look.Snow;
                case "kingdom": return Look.Coast;
                case "demonland": return Look.Crimson;
                case "temple": return Look.Shrine;
                case "fallen": return Look.Ruins;
                case "castle": return Look.DarkForest;
                default: return Look.Village;
            }
        }

        static System.Random rng;
        static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static Transform root;
        static readonly Dictionary<Color, Material> mats = new Dictionary<Color, Material>();

        static Material M(Color c) { Material m; if (!mats.TryGetValue(c, out m) || m == null) { m = MaterialFactory.Toon(c, 0.012f); mats[c] = m; } return m; }
        static Material Glow(Color c) { return MaterialFactory.Toon(c, 0f, c); }

        static GameObject Put(UnityEngine.Mesh mesh, Vector3 pos, Vector3 scale, Material m, float yaw = 0f, bool shadows = true)
        {
            var go = MeshFactory.MeshObject(mesh, root, pos, scale, m, shadows);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        static GameObject Box(Vector3 pos, Vector3 scale, Material m, float yaw = 0f, float roll = 0f)
        {
            var go = MeshFactory.Primitive(PrimitiveType.Cube, root, pos, scale, m);
            go.transform.localRotation = Quaternion.Euler(0f, yaw, roll);
            return go;
        }

        // Trail shape for each look, in map space (x across, z into the screen), start to finish.
        static Vector3[] TrailFor(Look look)
        {
            switch (look)
            {
                case Look.Meadow: return new[] { V(-9.5f, -3.5f), V(-6.5f, -1.5f), V(-3.5f, -2.5f), V(-1f, 0.5f), V(-2.5f, 3.5f), V(0.5f, 5.5f), V(4f, 4.2f), V(6.5f, 1.5f), V(9f, 3.5f) };
                case Look.Snow: return new[] { V(-9f, -2.5f), V(-5.5f, -3.5f), V(-2.5f, -1.5f), V(-1.5f, 2.5f), V(1.5f, 5.5f), V(4.5f, 3f), V(5.5f, -0.5f), V(8.5f, -3f) };
                case Look.Crimson: return new[] { V(-9.5f, -1.5f), V(-6.5f, 0.5f), V(-3.5f, 2.5f), V(-0.5f, 1f), V(2f, -1f), V(4.5f, -2.5f), V(7f, -3.5f), V(9f, -5.5f) };
                case Look.Village: return new[] { V(-9f, -4f), V(-6f, -2f), V(-2.5f, -3f), V(0f, 0f), V(-1f, 3.5f), V(2.5f, 5f), V(6f, 3f), V(9f, 4.5f) };
                case Look.Ruins: return new[] { V(-9.5f, -3f), V(-6f, -1f), V(-3f, -3f), V(0f, -1f), V(2f, 2f), V(5f, 3.5f), V(7.5f, 1.5f), V(9.5f, 4f) };
                case Look.Shrine: return new[] { V(0f, -6.5f), V(-3f, -4f), V(-1f, -1.5f), V(2.5f, 0f), V(1f, 2.5f), V(-1.5f, 4f), V(0f, 6f) };
                case Look.DarkForest: return new[] { V(-9.5f, -2f), V(-6.5f, -3.5f), V(-3.5f, -1f), V(-0.5f, -2.5f), V(2f, 0.5f), V(4.5f, -1f), V(7f, 1.5f), V(9f, 5f) };
                default: return new[] { V(-7.5f, -3.5f), V(-4.5f, -2f), V(-1.5f, -2.5f), V(0.5f, 0.5f), V(2.5f, 2.5f), V(5f, 1f), V(7f, 3.5f), V(8.5f, 5.5f) };
            }
        }

        static Vector3 V(float x, float z) { return new Vector3(x, 0f, z); }

        /// <summary>Builds the diorama for a region at <paramref name="origin"/> with <paramref name="stops"/> mission pedestals.</summary>
        public static Result Build(string regionId, int stops, Vector3 origin, Transform parent)
        {
            var res = new Result();
            res.look = LookFor(regionId);
            rng = new System.Random(regionId.GetHashCode() & 0x7fffffff);
            var go = new GameObject("AreaDiorama_" + regionId);
            go.transform.SetParent(parent, false);
            go.transform.position = origin;
            root = go.transform;
            res.root = go;

            // Smooth trail through the control points.
            var ctrl = TrailFor(res.look);
            var trail = new List<Vector3>();
            for (int i = 0; i < ctrl.Length - 1; i++)
            {
                Vector3 p0 = ctrl[Mathf.Max(0, i - 1)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(ctrl.Length - 1, i + 2)];
                for (int k = 0; k < 12; k++) trail.Add(CatmullRom(p0, p1, p2, p3, k / 12f));
            }
            trail.Add(ctrl[ctrl.Length - 1]);
            float total = 0f;
            var cum = new List<float> { 0f };
            for (int i = 1; i < trail.Count; i++) { total += Vector3.Distance(trail[i - 1], trail[i]); cum.Add(total); }

            // Mission stops, evenly spaced along the trail.
            for (int i = 0; i < stops; i++)
            {
                float t = stops > 1 ? Mathf.Lerp(0.04f, 0.96f, (float)i / (stops - 1)) : 0.5f;
                res.nodes.Add(root.TransformPoint(At(trail, cum, t * total)));
            }

            switch (res.look)
            {
                case Look.Meadow: Meadow(res, trail); break;
                case Look.Snow: Snow(res, trail, cum, total); break;
                case Look.Crimson: Crimson(res, trail); break;
                case Look.Village: Village(res, trail); break;
                case Look.Ruins: Ruins(res, trail); break;
                case Look.Shrine: Shrine(res, trail); break;
                case Look.DarkForest: DarkForest(res, trail); break;
                default: Coast(res, trail); break;
            }

            DetailPass(res.look, trail);

            // Stepping stones.
            bool hot = res.look == Look.Crimson;
            var stone = hot ? Glow(new Color(1f, 0.35f, 0.2f)) : M(res.look == Look.Snow ? new Color(0.97f, 0.98f, 1f) : new Color(0.97f, 0.94f, 0.84f));
            for (float d = 0.6f; d < total - 0.3f; d += 0.85f)
            {
                Vector3 p = At(trail, cum, d);
                bool nearStop = false;
                foreach (var n in res.nodes) if ((root.InverseTransformPoint(n) - p).sqrMagnitude < 1.1f * 1.1f) nearStop = true;
                if (nearStop) continue;
                Put(MeshFactory.FacetCylinder(7), p + Vector3.up * 0.02f, new Vector3(0.5f, 0.07f, 0.36f), stone, R(0f, 180f), false);
            }

            // Pedestals.
            var ped = M(new Color(0.24f, 0.25f, 0.3f));
            var pedTop = M(new Color(0.36f, 0.37f, 0.43f));
            foreach (var n in res.nodes)
            {
                Vector3 p = root.InverseTransformPoint(n);
                Put(MeshFactory.FacetCylinder(8), p, new Vector3(1.5f, 0.32f, 1.5f), ped, 22.5f);
                Put(MeshFactory.FacetCylinder(8), p + Vector3.up * 0.32f, new Vector3(1.3f, 0.05f, 1.3f), pedTop, 22.5f, false);
            }
            return res;
        }

        /// <summary>
        /// A small version of a region for the whole-world overview: a raised plateau in the region's colours
        /// dressed with a handful of its props (trees, snowy peaks, houses, torii, lava, ruins...), with the
        /// centre left clear for the region marker.
        /// </summary>
        public static GameObject BuildMini(string regionId, Transform parent, Vector3 at)
        {
            var look = LookFor(regionId);
            rng = new System.Random(regionId.GetHashCode() & 0x7fffffff);
            var go = new GameObject("Mini_" + regionId);
            go.transform.SetParent(parent, false);
            go.transform.position = at;
            go.transform.localScale = Vector3.one * 0.72f;
            root = go.transform;
            Color ground, rim;
            switch (look)
            {
                case Look.Snow: ground = new Color(0.88f, 0.92f, 0.98f); rim = new Color(0.6f, 0.7f, 0.85f); break;
                case Look.Crimson: ground = new Color(0.32f, 0.09f, 0.08f); rim = new Color(0.18f, 0.06f, 0.06f); break;
                case Look.Coast: ground = new Color(0.95f, 0.86f, 0.62f); rim = new Color(0.75f, 0.65f, 0.45f); break;
                case Look.Ruins: ground = new Color(0.5f, 0.46f, 0.42f); rim = new Color(0.36f, 0.33f, 0.3f); break;
                case Look.Shrine: ground = new Color(0.42f, 0.6f, 0.36f); rim = new Color(0.5f, 0.45f, 0.38f); break;
                case Look.DarkForest: ground = new Color(0.2f, 0.2f, 0.28f); rim = new Color(0.12f, 0.1f, 0.16f); break;
                case Look.Village: ground = new Color(0.5f, 0.68f, 0.3f); rim = new Color(0.45f, 0.35f, 0.24f); break;
                default: ground = new Color(0.36f, 0.62f, 0.24f); rim = new Color(0.4f, 0.32f, 0.22f); break;
            }
            Put(MeshFactory.FacetCylinder(10), -Vector3.up * 0.5f, new Vector3(20f, 0.8f, 20f), M(rim), 9f);
            Put(MeshFactory.FacetCylinder(10), Vector3.up * 0.3f, new Vector3(19f, 0.06f, 19f), M(ground), 9f, false);
            var spots = new List<Vector3>();
            int guard = 0;
            while (spots.Count < 22 && guard++ < 400)
            {
                var p = V(R(-8.5f, 8.5f), R(-8.5f, 8.5f));
                if (p.magnitude > 8.8f || p.magnitude < 3.4f) continue;
                bool close = false;
                foreach (var q in spots) if ((q - p).sqrMagnitude < 2.2f) close = true;
                if (!close) spots.Add(p);
            }
            Vector3 up = Vector3.up * 0.36f;
            for (int i = 0; i < spots.Count; i++)
            {
                Vector3 p = spots[i] + up;
                double r = rng.NextDouble();
                switch (look)
                {
                    case Look.Snow:
                        if (r < 0.4) { float h = R(3f, 5.5f); Put(MeshFactory.FacetCone(5), p, new Vector3(3f, h, 3f), M(new Color(0.62f, 0.72f, 0.88f)), R(0f, 72f)); Put(MeshFactory.FacetCone(5), p + Vector3.up * h * 0.6f, new Vector3(1.3f, h * 0.4f, 1.3f), M(Color.white), R(0f, 72f), false); }
                        else Pine(p, R(1f, 1.5f), new Color(0.2f, 0.36f, 0.42f), true);
                        break;
                    case Look.Crimson:
                        if (r < 0.45) DeadTree(p, R(1f, 1.5f));
                        else if (r < 0.75) Put(MeshFactory.FacetCone(5), p, new Vector3(1.4f, R(2f, 3.5f), 1.4f), M(new Color(0.15f, 0.06f, 0.06f)), R(0f, 72f));
                        else Put(MeshFactory.Disc(), p + Vector3.up * 0.02f, new Vector3(1.2f, 1f, 1.2f), Glow(new Color(1f, 0.4f, 0.08f)), 0f, false);
                        break;
                    case Look.Coast:
                        if (r < 0.35) House(p, 1.1f, R(0f, 360f), new Color(0.95f, 0.92f, 0.85f), new Color(0.75f, 0.3f, 0.2f));
                        else if (r < 0.7) Bush(p, R(0.8f, 1.3f), new Color(0.3f, 0.6f, 0.22f));
                        else Pine(p, R(0.9f, 1.2f), new Color(0.15f, 0.45f, 0.2f), false);
                        break;
                    case Look.Ruins:
                        if (r < 0.5) Put(MeshFactory.FacetCylinder(8), p, new Vector3(1f, R(1.2f, 3f), 1f), M(new Color(0.62f, 0.6f, 0.56f)), R(0f, 45f));
                        else Box(p + Vector3.up * 0.4f, new Vector3(R(1f, 2.4f), R(0.6f, 1.6f), 0.6f), M(new Color(0.55f, 0.52f, 0.48f)), R(0f, 180f));
                        break;
                    case Look.Shrine:
                        if (i == 0) Torii(p, 1.2f, R(0f, 360f), new Color(0.88f, 0.2f, 0.15f));
                        else if (r < 0.5) { Put(MeshFactory.FacetCylinder(5), p, new Vector3(0.3f, 1.3f, 0.3f), M(new Color(0.35f, 0.24f, 0.2f))); Bush(p + Vector3.up * 1.2f, R(1.4f, 2f), new Color(0.98f, 0.75f, 0.85f)); }
                        else Pine(p, R(1f, 1.4f), new Color(0.16f, 0.42f, 0.24f), false);
                        break;
                    case Look.DarkForest:
                        if (r < 0.7) Pine(p, R(1.2f, 1.9f), new Color(0.14f, 0.16f, 0.26f), false);
                        else { Put(MeshFactory.FacetCylinder(6), p, new Vector3(0.15f, 0.4f, 0.15f), M(new Color(0.85f, 0.85f, 0.9f))); Put(MeshFactory.FacetCone(8), p + Vector3.up * 0.35f, new Vector3(0.7f, 0.3f, 0.7f), Glow(new Color(0.45f, 0.9f, 1f))); }
                        break;
                    case Look.Village:
                        if (r < 0.4) House(p, 1.1f, R(0f, 360f), new Color(0.92f, 0.86f, 0.74f), new Color(0.45f, 0.3f, 0.2f));
                        else if (r < 0.8) Bush(p, R(0.8f, 1.3f), new Color(0.32f, 0.6f, 0.22f));
                        else Pine(p, R(1f, 1.3f), new Color(0.2f, 0.48f, 0.2f), false);
                        break;
                    default:
                        if (r < 0.85) Pine(p, R(1.1f, 1.7f), new Color(0.13f, R(0.38f, 0.5f), 0.16f), false);
                        else RockAt(p, R(0.8f, 1.4f), new Color(0.5f, 0.52f, 0.55f));
                        break;
                }
            }
            return go;
        }

        /// <summary>
        /// Fills the world overview's land between the regions with props in the style of the nearest region,
        /// keeping clear of region plateaus and the stone roads; adds hills, lakes and small sea islands.
        /// </summary>
        public static void DecorateWorld(Transform parent, List<string> ids, List<Vector3> nodes, List<List<Vector3>> roads, Vector3[] lands, Vector3[] sizes)
        {
            rng = new System.Random(4242);
            var go = new GameObject("Countryside");
            go.transform.SetParent(parent, false);
            root = go.transform;
            int placed = 0, guard = 0;
            while (placed < 420 && guard++ < 6000)
            {
                int li = rng.Next(lands.Length);
                Vector3 c = lands[li];
                float ax = sizes[li].x * 0.92f, az = sizes[li].z * 0.92f;
                var p = new Vector3(c.x + R(-ax, ax), 0f, c.z + R(-az, az));
                if (((p.x - c.x) / ax) * ((p.x - c.x) / ax) + ((p.z - c.z) / az) * ((p.z - c.z) / az) > 1f) continue;
                // Nearest region decides the style; keep clear of its plateau.
                int near = 0; float best = float.MaxValue;
                for (int i = 0; i < nodes.Count; i++) { float d = (Flat(nodes[i]) - p).sqrMagnitude; if (d < best) { best = d; near = i; } }
                if (best < 9.5f * 9.5f) continue;
                bool onRoad = false;
                foreach (var road in roads) foreach (var q in road) if ((Flat(q) - p).sqrMagnitude < 2.2f * 2.2f) { onRoad = true; break; }
                if (onRoad) continue;
                placed++;
                var look = LookFor(ids[near]);
                double r = rng.NextDouble();
                switch (look)
                {
                    case Look.Snow:
                        if (r < 0.3) { float h = R(3f, 6f); Put(MeshFactory.FacetCone(5), p, new Vector3(3.5f, h, 3.5f), M(new Color(0.62f, 0.72f, 0.88f)), R(0f, 72f)); Put(MeshFactory.FacetCone(5), p + Vector3.up * h * 0.62f, new Vector3(1.4f, h * 0.38f, 1.4f), M(Color.white), R(0f, 72f), false); }
                        else Pine(p, R(1f, 1.6f), new Color(0.2f, 0.36f, 0.42f), true);
                        break;
                    case Look.Crimson:
                    case Look.DarkForest:
                        if (r < 0.5) DeadTree(p, R(1f, 1.6f));
                        else if (r < 0.8) RockAt(p, R(0.8f, 1.8f), new Color(0.22f, 0.12f, 0.14f));
                        else Pine(p, R(1f, 1.5f), new Color(0.14f, 0.16f, 0.24f), false);
                        break;
                    case Look.Ruins:
                        if (r < 0.4) Put(MeshFactory.FacetCylinder(8), p, new Vector3(1f, R(1f, 2.5f), 1f), M(new Color(0.6f, 0.58f, 0.54f)), R(0f, 45f));
                        else if (r < 0.7) RockAt(p, R(0.8f, 1.5f), new Color(0.5f, 0.48f, 0.46f));
                        else Bush(p, R(0.7f, 1.1f), new Color(0.4f, 0.5f, 0.3f));
                        break;
                    case Look.Shrine:
                        if (r < 0.35) { Put(MeshFactory.FacetCylinder(5), p, new Vector3(0.3f, 1.3f, 0.3f), M(new Color(0.35f, 0.24f, 0.2f))); Bush(p + Vector3.up * 1.2f, R(1.4f, 2f), new Color(0.98f, 0.75f, 0.85f)); }
                        else Pine(p, R(1f, 1.6f), new Color(0.16f, 0.42f, 0.24f), false);
                        break;
                    case Look.Village:
                    case Look.Coast:
                        if (r < 0.08) House(p, 1f, R(0f, 360f), new Color(0.92f, 0.86f, 0.74f), new Color(0.5f, 0.3f, 0.2f));
                        else if (r < 0.2) Put(MeshFactory.FacetCylinder(4), p, new Vector3(3.2f, 0.05f, 2.4f), M(new Color(0.62f, 0.7f, 0.3f)), R(0f, 90f), false); // field
                        else if (r < 0.6) Bush(p, R(0.8f, 1.4f), new Color(0.3f, R(0.55f, 0.66f), 0.22f));
                        else Pine(p, R(1f, 1.5f), new Color(0.18f, 0.46f, 0.2f), false);
                        break;
                    default:
                        if (r < 0.8) Pine(p, R(1.1f, 1.8f), new Color(0.13f, R(0.36f, 0.5f), 0.16f), false);
                        else RockAt(p, R(0.8f, 1.5f), new Color(0.5f, 0.52f, 0.55f));
                        break;
                }
            }
            // Rolling hills and mountain ranges along the far edges of the land.
            for (int i = 0; i < 26; i++)
            {
                int li = rng.Next(lands.Length);
                float a = R(0f, Mathf.PI * 2f);
                var p = lands[li] + new Vector3(Mathf.Cos(a) * sizes[li].x * 0.86f, 0f, Mathf.Sin(a) * sizes[li].z * 0.86f);
                if (p.z < lands[li].z - sizes[li].z * 0.3f) continue; // keep the near shore open for the camera
                float h = R(4f, 9f);
                Put(MeshFactory.FacetCone(6), p, new Vector3(R(8f, 13f), h, R(8f, 13f)), M(new Color(0.46f, 0.52f, 0.42f)), R(0f, 60f));
                if (h > 7f) Put(MeshFactory.FacetCone(6), p + Vector3.up * h * 0.66f, new Vector3(3.2f, h * 0.34f, 3.2f), M(new Color(0.95f, 0.96f, 1f)), R(0f, 60f), false);
            }
            // Lakes.
            var water = M(new Color(0.25f, 0.55f, 0.82f));
            for (int i = 0; i < 5; i++)
            {
                var p = new Vector3(R(-45f, 45f), 0.03f, R(-25f, 25f));
                bool ok = true;
                for (int k = 0; k < nodes.Count; k++) if ((Flat(nodes[k]) - Flat(p)).sqrMagnitude < 12f * 12f) ok = false;
                if (!ok) continue;
                float sz = R(3f, 6f);
                Put(MeshFactory.Disc(), p, new Vector3(sz, 1f, sz * R(0.6f, 0.9f)), water, R(0f, 180f), false);
            }
            // Little islands out at sea.
            for (int i = 0; i < 9; i++)
            {
                float a = R(0f, Mathf.PI * 2f);
                var p = new Vector3(Mathf.Cos(a) * R(95f, 120f), 0f, Mathf.Sin(a) * R(70f, 95f));
                float sz = R(3f, 7f);
                Put(MeshFactory.Disc(), p + Vector3.up * -0.15f, new Vector3(sz + 1.5f, 1f, sz + 1.5f), M(new Color(0.9f, 0.82f, 0.6f)), 0f, false);
                Put(MeshFactory.Rock(rng.Next(8)), p, new Vector3(sz, sz * 0.4f, sz), M(new Color(0.4f, 0.56f, 0.3f)), R(0f, 360f));
                if (rng.NextDouble() < 0.6) Pine(p + Vector3.up * sz * 0.35f, R(1f, 1.4f), new Color(0.18f, 0.46f, 0.2f), false);
            }
            StaticBatchingUtility.Combine(go);
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        /// <summary>
        /// Ground-level detail for every region: a worn path under the stepping stones, and hundreds of small
        /// touches — grass tufts, flowers, pebbles, snow drifts, glowing embers, mushrooms or rubble.
        /// </summary>
        static void DetailPass(Look look, List<Vector3> trail)
        {
            Color dirt;
            switch (look)
            {
                case Look.Snow: dirt = new Color(0.74f, 0.8f, 0.9f); break;
                case Look.Crimson: dirt = new Color(0.2f, 0.06f, 0.06f); break;
                case Look.Coast: dirt = new Color(0.86f, 0.78f, 0.55f); break;
                case Look.Ruins: dirt = new Color(0.38f, 0.35f, 0.32f); break;
                case Look.DarkForest: dirt = new Color(0.16f, 0.15f, 0.2f); break;
                default: dirt = new Color(0.52f, 0.42f, 0.28f); break;
            }
            var pts = new List<Vector3>();
            for (int i = 0; i < trail.Count; i += 3) pts.Add(trail[i]);
            pts.Add(trail[trail.Count - 1]);
            Ribbon(pts.ToArray(), 0.95f, M(dirt), 0.012f);

            var grassA = M(look == Look.Snow ? new Color(0.9f, 0.94f, 1f) : look == Look.Crimson ? new Color(0.35f, 0.12f, 0.1f) : look == Look.DarkForest ? new Color(0.22f, 0.26f, 0.32f) : new Color(0.3f, 0.58f, 0.2f));
            var grassB = M(look == Look.Snow ? new Color(0.8f, 0.86f, 0.95f) : look == Look.Crimson ? new Color(0.28f, 0.08f, 0.08f) : look == Look.DarkForest ? new Color(0.18f, 0.2f, 0.28f) : new Color(0.4f, 0.68f, 0.26f));
            Color[] flowerCols = { new Color(1f, 0.85f, 0.3f), new Color(1f, 0.55f, 0.7f), new Color(0.95f, 0.95f, 1f), new Color(0.6f, 0.55f, 1f) };
            var pebble = M(new Color(0.6f, 0.6f, 0.62f));
            for (int i = 0; i < 170; i++)
            {
                var p = V(R(-14f, 14f), R(-9.5f, 9.5f));
                float dt = DistToTrail(trail, p);
                if (dt < 0.9f) continue;
                if (look == Look.Coast && ((p.x - 1f) / 11f) * ((p.x - 1f) / 11f) + ((p.z - 1f) / 7.5f) * ((p.z - 1f) / 7.5f) > 1f) continue; // not on the sea
                double r = rng.NextDouble();
                if (r < 0.5)
                {
                    // Grass tuft (or a small snow drift / dead grass).
                    if (look == Look.Snow) Put(MeshFactory.SmoothSphere(), p, new Vector3(R(0.5f, 1f), 0.2f, R(0.4f, 0.8f)), grassA, R(0f, 180f), false);
                    else for (int k = 0; k < 3; k++) Put(MeshFactory.FacetCone(3), p + new Vector3(R(-0.15f, 0.15f), 0f, R(-0.15f, 0.15f)), new Vector3(0.1f, R(0.25f, 0.45f), 0.1f), k % 2 == 0 ? grassA : grassB, R(0f, 120f), false);
                }
                else if (r < 0.72)
                {
                    switch (look)
                    {
                        case Look.Crimson: Put(MeshFactory.SmoothSphere(), p, Vector3.one * R(0.08f, 0.16f), Glow(new Color(1f, 0.45f, 0.1f)), 0f, false); break;
                        case Look.DarkForest: Put(MeshFactory.FacetCone(6), p + Vector3.up * 0.1f, new Vector3(0.22f, 0.12f, 0.22f), Glow(new Color(0.45f, 0.9f, 1f)), 0f, false); break;
                        case Look.Snow: case Look.Ruins: Put(MeshFactory.Rock(rng.Next(8)), p, Vector3.one * R(0.15f, 0.3f), pebble, R(0f, 360f), false); break;
                        default:
                            // A little flower: stem and a coloured head.
                            Put(MeshFactory.FacetCylinder(4), p, new Vector3(0.02f, 0.2f, 0.02f), grassB, 0f, false);
                            Put(MeshFactory.SmoothSphere(), p + Vector3.up * 0.22f, Vector3.one * 0.1f, M(flowerCols[rng.Next(flowerCols.Length)]), 0f, false);
                            break;
                    }
                }
                else if (dt < 2.4f) Put(MeshFactory.Rock(rng.Next(8)), p, Vector3.one * R(0.12f, 0.25f), pebble, R(0f, 360f), false);
            }
        }

        static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * ((2f * p1) + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        static Vector3 At(List<Vector3> trail, List<float> cum, float d)
        {
            for (int i = 1; i < trail.Count; i++)
                if (cum[i] >= d)
                {
                    float seg = Mathf.Max(0.0001f, cum[i] - cum[i - 1]);
                    return Vector3.Lerp(trail[i - 1], trail[i], (d - cum[i - 1]) / seg);
                }
            return trail[trail.Count - 1];
        }

        static float DistToTrail(List<Vector3> trail, Vector3 p)
        {
            float best = float.MaxValue;
            for (int i = 1; i < trail.Count; i++)
            {
                Vector3 a = trail[i - 1], b = trail[i];
                Vector3 ab = b - a;
                float t = Mathf.Clamp01(Vector3.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, (a + ab * t - p).magnitude);
            }
            return best;
        }

        /// <summary>Random spots in the map rectangle that keep clear of the trail and the given keep-out spots.</summary>
        static List<Vector3> Scatter(List<Vector3> trail, int count, float clearance, Rect area, List<Vector3> keepOut = null, float keepR = 0f)
        {
            var list = new List<Vector3>();
            int guard = 0;
            while (list.Count < count && guard++ < count * 30)
            {
                var p = V(R(area.xMin, area.xMax), R(area.yMin, area.yMax));
                if (DistToTrail(trail, p) < clearance) continue;
                bool bad = false;
                if (keepOut != null) foreach (var k in keepOut) if ((k - p).sqrMagnitude < keepR * keepR) bad = true;
                foreach (var q in list) if ((q - p).sqrMagnitude < 0.8f) bad = true;
                if (!bad) list.Add(p);
            }
            return list;
        }

        static void Ground(Color c, Color patch, int patches)
        {
            Put(MeshFactory.Disc(), Vector3.zero, new Vector3(60f, 1f, 45f), M(c), 0f, false).GetComponent<Renderer>().receiveShadows = true;
            for (int i = 0; i < patches; i++)
            {
                float s = R(2f, 5f);
                Put(MeshFactory.Disc(), V(R(-14f, 14f), R(-9f, 10f)) + Vector3.up * 0.005f, new Vector3(s, 1f, s * R(0.6f, 1f)), M(patch), R(0f, 180f), false);
            }
        }

        // ------------------------------------------------------------------ Props

        static void Pine(Vector3 p, float h, Color leaf, bool snowy)
        {
            var trunk = M(new Color(0.4f, 0.26f, 0.15f));
            Put(MeshFactory.FacetCylinder(5), p, new Vector3(0.22f * h, 0.35f * h, 0.22f * h), trunk);
            var l1 = M(leaf);
            var l2 = M(Color.Lerp(leaf, Color.white, 0.12f));
            float yaw = R(0f, 60f);
            Put(MeshFactory.FacetCone(6), p + Vector3.up * 0.3f * h, new Vector3(1.3f * h, 1.1f * h, 1.3f * h), l1, yaw);
            Put(MeshFactory.FacetCone(6), p + Vector3.up * 0.8f * h, new Vector3(1f * h, 0.95f * h, 1f * h), l2, yaw + 30f);
            if (snowy) Put(MeshFactory.FacetCone(6), p + Vector3.up * 1.25f * h, new Vector3(0.55f * h, 0.52f * h, 0.55f * h), M(new Color(0.96f, 0.98f, 1f)), yaw);
            else Put(MeshFactory.FacetCone(6), p + Vector3.up * 1.25f * h, new Vector3(0.6f * h, 0.6f * h, 0.6f * h), l1, yaw);
        }

        static void DeadTree(Vector3 p, float h)
        {
            var bark = M(new Color(0.09f, 0.05f, 0.06f));
            Put(MeshFactory.FacetCylinder(5), p, new Vector3(0.28f * h, 1.6f * h, 0.28f * h), bark);
            for (int i = 0; i < 4; i++)
            {
                var b = MeshFactory.MeshObject(MeshFactory.FacetCylinder(4), root, p + Vector3.up * h * R(0.9f, 1.5f), new Vector3(0.1f * h, 0.8f * h, 0.1f * h), bark);
                b.transform.localRotation = Quaternion.Euler(R(30f, 60f), i * 90f + R(-20f, 20f), 0f);
            }
        }

        static void RockAt(Vector3 p, float s, Color c)
        {
            Put(MeshFactory.Rock(rng.Next(8)), p, new Vector3(s * R(0.9f, 1.3f), s * R(0.6f, 0.9f), s), M(c), R(0f, 360f));
        }

        static void Bush(Vector3 p, float s, Color c)
        {
            Put(MeshFactory.Rock(rng.Next(8)), p, new Vector3(s, s * 0.7f, s), M(c), R(0f, 360f));
        }

        static void Torii(Vector3 p, float s, float yaw, Color red)
        {
            var g = new GameObject("Torii").transform;
            g.SetParent(root, false);
            g.localPosition = p;
            g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var r = M(red);
            var dark = M(new Color(0.12f, 0.08f, 0.08f));
            for (int i = -1; i <= 1; i += 2)
                MeshFactory.MeshObject(MeshFactory.FacetCylinder(8), g, new Vector3(i * 0.9f * s, 0f, 0f), new Vector3(0.24f * s, 2.3f * s, 0.24f * s), r);
            MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, 1.8f * s, 0f), new Vector3(2.2f * s, 0.16f * s, 0.2f * s), r);
            MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, 2.35f * s, 0f), new Vector3(2.9f * s, 0.2f * s, 0.3f * s), r);
            MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, 2.5f * s, 0f), new Vector3(3.1f * s, 0.12f * s, 0.34f * s), dark);
        }

        static void House(Vector3 p, float s, float yaw, Color wall, Color roof)
        {
            var g = new GameObject("House").transform;
            g.SetParent(root, false);
            g.localPosition = p;
            g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, 0.55f * s, 0f), new Vector3(1.6f * s, 1.1f * s, 1.3f * s), M(wall));
            var rf = MeshFactory.MeshObject(MeshFactory.FacetCone(4), g, new Vector3(0f, 1.1f * s, 0f), new Vector3(2.5f * s, 0.9f * s, 2.1f * s), M(roof));
            rf.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
            MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, 0.35f * s, -0.66f * s), new Vector3(0.35f * s, 0.7f * s, 0.04f * s), M(new Color(0.35f, 0.22f, 0.12f)));
            MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0.45f * s, 0.7f * s, -0.66f * s), new Vector3(0.3f * s, 0.26f * s, 0.04f * s), Glow(new Color(1f, 0.8f, 0.45f)));
        }

        static void Pagoda(Vector3 p, float s, float yaw, Color wall, Color roof, Color window, int floors)
        {
            var g = new GameObject("Pagoda").transform;
            g.SetParent(root, false);
            g.localPosition = p;
            g.localRotation = Quaternion.Euler(0f, yaw, 0f);
            float y = 0f;
            for (int f = 0; f < floors; f++)
            {
                float w = (2f - f * 0.35f) * s;
                MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, y + 0.45f * s, 0f), new Vector3(w, 0.9f * s, w * 0.8f), M(wall));
                MeshFactory.Primitive(PrimitiveType.Cube, g, new Vector3(0f, y + 0.45f * s, -w * 0.41f), new Vector3(w * 0.6f, 0.4f * s, 0.04f * s), Glow(window));
                var rf = MeshFactory.MeshObject(MeshFactory.FacetCone(4), g, new Vector3(0f, y + 0.85f * s, 0f), new Vector3(w * 1.75f, 0.55f * s, w * 1.5f), M(roof));
                rf.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                y += 1.05f * s;
            }
        }

        static void Bridge(Vector3 a, Vector3 b, float width)
        {
            var wood = M(new Color(0.55f, 0.36f, 0.2f));
            var dark = M(new Color(0.36f, 0.22f, 0.12f));
            Vector3 dir = b - a;
            float len = dir.magnitude;
            float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
            int n = Mathf.Max(4, Mathf.RoundToInt(len / 0.45f));
            for (int i = 0; i <= n; i++)
            {
                float t = (float)i / n;
                Vector3 p = Vector3.Lerp(a, b, t) + Vector3.up * (0.12f + Mathf.Sin(t * Mathf.PI) * 0.45f);
                Box(p, new Vector3(width, 0.1f, 0.34f), wood, yaw, 0f);
            }
            Vector3 side = Vector3.Cross(Vector3.up, dir.normalized) * width * 0.5f;
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i <= 4; i++)
                {
                    float t = i / 4f;
                    Vector3 p = Vector3.Lerp(a, b, t) + side * s + Vector3.up * (0.4f + Mathf.Sin(t * Mathf.PI) * 0.45f);
                    Box(p, new Vector3(0.12f, 0.7f, 0.12f), dark, yaw);
                }
            for (int s = -1; s <= 1; s += 2)
                for (int i = 0; i < 4; i++)
                {
                    float t0 = i / 4f, t1 = (i + 1) / 4f;
                    Vector3 p0 = Vector3.Lerp(a, b, t0) + side * s + Vector3.up * (0.72f + Mathf.Sin(t0 * Mathf.PI) * 0.45f);
                    Vector3 p1 = Vector3.Lerp(a, b, t1) + side * s + Vector3.up * (0.72f + Mathf.Sin(t1 * Mathf.PI) * 0.45f);
                    var rail = Box((p0 + p1) * 0.5f, new Vector3(0.08f, 0.08f, Vector3.Distance(p0, p1) + 0.05f), dark, yaw);
                    rail.transform.localRotation = Quaternion.LookRotation(p1 - p0);
                }
        }

        /// <summary>A flat winding ribbon (river, lava stream).</summary>
        static void Ribbon(Vector3[] pts, float width, Material m, float y)
        {
            var line = new List<Vector3>();
            for (int i = 0; i < pts.Length - 1; i++)
            {
                Vector3 p0 = pts[Mathf.Max(0, i - 1)], p1 = pts[i], p2 = pts[i + 1], p3 = pts[Mathf.Min(pts.Length - 1, i + 2)];
                for (int k = 0; k < 10; k++) line.Add(CatmullRom(p0, p1, p2, p3, k / 10f));
            }
            line.Add(pts[pts.Length - 1]);
            var v = new List<Vector3>();
            var t = new List<int>();
            for (int i = 0; i < line.Count; i++)
            {
                Vector3 dir = (i < line.Count - 1 ? line[i + 1] - line[i] : line[i] - line[i - 1]).normalized;
                Vector3 side = Vector3.Cross(Vector3.up, dir) * width * (0.85f + 0.15f * Mathf.Sin(i * 0.7f));
                v.Add(line[i] - side + Vector3.up * y);
                v.Add(line[i] + side + Vector3.up * y);
                if (i > 0)
                {
                    int b = i * 2;
                    t.Add(b - 2); t.Add(b); t.Add(b - 1);
                    t.Add(b - 1); t.Add(b); t.Add(b + 1);
                }
            }
            var mesh = new UnityEngine.Mesh { name = "Ribbon" };
            mesh.SetVertices(v);
            mesh.SetTriangles(t, 0);
            mesh.RecalculateNormals();
            if (mesh.normals.Length > 0 && mesh.normals[0].y < 0f) { t.Reverse(); mesh.SetTriangles(t, 0); mesh.RecalculateNormals(); }
            mesh.RecalculateBounds();
            MeshFactory.MeshObject(mesh, root, Vector3.zero, Vector3.one, m, false);
        }

        static void Chest(Vector3 p, float yaw)
        {
            Box(p + Vector3.up * 0.28f, new Vector3(0.8f, 0.55f, 0.55f), M(new Color(0.62f, 0.34f, 0.14f)), yaw);
            Box(p + Vector3.up * 0.62f, new Vector3(0.82f, 0.18f, 0.57f), M(new Color(0.55f, 0.28f, 0.1f)), yaw);
            Box(p + Vector3.up * 0.4f, new Vector3(0.84f, 0.1f, 0.59f), Glow(new Color(1f, 0.78f, 0.25f)), yaw);
            Box(p + Vector3.up * 0.42f, new Vector3(0.14f, 0.18f, 0.6f), Glow(new Color(1f, 0.85f, 0.3f)), yaw);
        }

        static void Lantern(Vector3 p, Color light, float range)
        {
            var lg = new GameObject("Lamp");
            lg.transform.SetParent(root, false);
            lg.transform.localPosition = p;
            var l = lg.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = light;
            l.range = range;
            l.intensity = 1.6f;
            lg.AddComponent<LanternFlicker>();
        }

        // ------------------------------------------------------------------ Looks

        static void Meadow(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.36f, 0.62f, 0.24f);
            res.fog = new Color(0.55f, 0.75f, 0.5f);
            res.ambient = new Color(0.62f, 0.68f, 0.58f);
            res.sun = new Color(1f, 0.96f, 0.82f);
            res.sunIntensity = 1.35f;
            Ground(new Color(0.42f, 0.7f, 0.24f), new Color(0.5f, 0.78f, 0.3f), 18);
            // River along the bottom right with a bridge.
            var river = new[] { V(-3f, -12f), V(0f, -7.5f), V(4f, -6f), V(7.5f, -6.8f), V(11f, -4.5f), V(15f, -5.5f) };
            Ribbon(river, 1.9f, M(new Color(0.55f, 0.82f, 0.95f)), 0.01f);
            Ribbon(river, 1.45f, M(new Color(0.18f, 0.55f, 0.92f)), 0.02f);
            Bridge(V(4.8f, -7.9f), V(4.3f, -4.2f), 1.3f);
            var keep = new List<Vector3> { V(-8.5f, 6f), V(4.5f, -6f), V(2.5f, 1.5f), V(-9f, -7.5f) };
            Torii(V(-8.5f, 6f), 1.1f, 15f, new Color(0.85f, 0.14f, 0.12f));
            Chest(V(2.2f, 1.8f), -20f);
            RockAt(V(-9f, -7.5f), 3f, new Color(0.45f, 0.47f, 0.5f));
            RockAt(V(-7f, -8.5f), 1.4f, new Color(0.5f, 0.52f, 0.55f));
            foreach (var p in Scatter(trail, 70, 1.6f, new Rect(-14f, -10f, 28f, 20f), keep, 2.2f))
            {
                if (DistToRiver(river, p) < 2.2f) continue;
                if (rng.NextDouble() < 0.8) Pine(p, R(0.9f, 1.6f), new Color(0.13f, R(0.38f, 0.5f), 0.16f), false);
                else Bush(p, R(0.5f, 0.9f), new Color(0.24f, 0.55f, 0.18f));
            }
            for (int i = 0; i < 26; i++)
            {
                var p = V(R(-13f, 13f), R(-9f, 9f));
                if (DistToTrail(trail, p) < 1f) continue;
                Put(MeshFactory.FacetCone(4), p, new Vector3(0.2f, 0.25f, 0.2f), M(i % 3 == 0 ? new Color(1f, 0.85f, 0.3f) : new Color(0.3f, 0.6f, 0.2f)), R(0f, 90f), false);
            }
        }

        static float DistToRiver(Vector3[] river, Vector3 p)
        {
            var l = new List<Vector3>(river);
            return DistToTrail(l, p);
        }

        static void Snow(Result res, List<Vector3> trail, List<float> cum, float total)
        {
            res.sky = new Color(0.55f, 0.72f, 0.92f);
            res.fog = new Color(0.72f, 0.82f, 0.95f);
            res.ambient = new Color(0.7f, 0.76f, 0.88f);
            res.sun = new Color(0.95f, 0.97f, 1f);
            res.sunIntensity = 1.25f;
            Ground(new Color(0.86f, 0.9f, 0.97f), new Color(0.78f, 0.85f, 0.95f), 16);
            // Deep blue chasms crossing the trail, spanned by wooden bridges.
            float[] gaps = { 0.33f, 0.72f };
            var chasm = M(new Color(0.16f, 0.3f, 0.55f));
            var chasmDeep = M(new Color(0.08f, 0.16f, 0.35f));
            foreach (var g in gaps)
            {
                Vector3 c = At(trail, cum, total * g);
                Vector3 dir = (At(trail, cum, total * g + 0.5f) - c).normalized;
                float yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
                var slab = Box(c + Vector3.up * 0.004f, new Vector3(14f, 0.01f, 2.4f), chasm, yaw + 90f);
                slab.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Box(c + Vector3.up * 0.006f, new Vector3(13f, 0.01f, 1.2f), chasmDeep, yaw + 90f).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                Bridge(c - dir * 1.6f, c + dir * 1.6f, 1.2f);
            }
            var keep = new List<Vector3> { V(-8f, 6.5f), V(8.5f, 6.5f) };
            // Shrine top-left, glowing.
            Pagoda(V(-8f, 6.5f), 1f, 10f, new Color(0.45f, 0.28f, 0.2f), new Color(0.28f, 0.3f, 0.4f), new Color(1f, 0.62f, 0.25f), 2);
            Lantern(new Vector3(-8f, 1.5f, 5.2f), new Color(1f, 0.6f, 0.3f), 7f);
            // Purple portal top-right.
            var gate = M(new Color(0.22f, 0.22f, 0.3f));
            for (int i = -1; i <= 1; i += 2) Box(V(8.5f + i * 1.1f, 6.5f) + Vector3.up * 1.4f, new Vector3(0.5f, 2.8f, 0.5f), gate);
            Box(V(8.5f, 6.5f) + Vector3.up * 2.95f, new Vector3(3f, 0.4f, 0.6f), gate);
            var portal = Put(MeshFactory.Disc(), V(8.5f, 6.4f) + Vector3.up * 1.35f, new Vector3(0.85f, 1f, 1.3f), MaterialFactory.Additive(new Color(0.75f, 0.3f, 1f, 0.9f)), 0f, false);
            portal.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            portal.AddComponent<Pulse>().Speed = 3f;
            Lantern(V(8.5f, 5.6f) + Vector3.up * 1.4f, new Color(0.7f, 0.3f, 1f), 7f);
            // Icy spires and cliffs around the edges, snowy pines between.
            var ice = M(new Color(0.62f, 0.74f, 0.9f));
            var iceDark = M(new Color(0.42f, 0.55f, 0.78f));
            foreach (var p in Scatter(trail, 60, 1.7f, new Rect(-14f, -10f, 28f, 20f), keep, 2.4f))
            {
                double r = rng.NextDouble();
                if (r < 0.35)
                {
                    float h = R(2f, 4.5f);
                    Put(MeshFactory.FacetCone(5), p, new Vector3(R(1.4f, 2.4f), h, R(1.4f, 2.4f)), r < 0.17 ? ice : iceDark, R(0f, 72f));
                    Put(MeshFactory.FacetCone(5), p + Vector3.up * h * 0.62f, new Vector3(0.7f, h * 0.38f, 0.7f), M(new Color(0.97f, 0.98f, 1f)), R(0f, 72f), false);
                }
                else if (r < 0.85) Pine(p, R(0.8f, 1.4f), new Color(0.2f, 0.36f, 0.42f), true);
                else RockAt(p, R(0.6f, 1.2f), new Color(0.72f, 0.8f, 0.92f));
            }
        }

        static void Crimson(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.22f, 0.04f, 0.05f);
            res.fog = new Color(0.4f, 0.06f, 0.06f);
            res.ambient = new Color(0.55f, 0.28f, 0.28f);
            res.sun = new Color(1f, 0.45f, 0.35f);
            res.sunIntensity = 1.1f;
            Ground(new Color(0.3f, 0.08f, 0.08f), new Color(0.38f, 0.1f, 0.09f), 14);
            // The red moon hangs over the back of the valley.
            var moon = Put(MeshFactory.Disc(), V(-7f, 12f) + Vector3.up * 7f, new Vector3(6f, 1f, 6f), MaterialFactory.Toon(new Color(1f, 0.32f, 0.26f), 0f, new Color(0.95f, 0.25f, 0.2f)), 0f, false);
            moon.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            var halo = Put(MeshFactory.Disc(), V(-7f, 12.1f) + Vector3.up * 7f, new Vector3(8.5f, 1f, 8.5f), MaterialFactory.Additive(new Color(1f, 0.25f, 0.2f, 0.35f)), 0f, false);
            halo.transform.localRotation = Quaternion.Euler(-60f, 0f, 0f);
            // Lava cracks.
            var lava = Glow(new Color(1f, 0.4f, 0.1f));
            for (int i = 0; i < 40; i++)
            {
                var p = V(R(-14f, 14f), R(-9f, 9f));
                if (DistToTrail(trail, p) < 0.9f) continue;
                float yaw = R(0f, 180f);
                for (int k = 0; k < 3; k++)
                {
                    float len = R(0.6f, 1.5f);
                    Vector3 d = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                    Box(p + d * len * 0.5f + Vector3.up * 0.01f, new Vector3(0.09f, 0.02f, len), lava, yaw).GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                    p += d * len;
                    yaw += R(-50f, 50f);
                }
            }
            // Pagoda on a cliff, top right; torii gates, top left.
            var keep = new List<Vector3> { V(7.5f, 6.5f), V(-8.5f, 5f) };
            var cliff = M(new Color(0.16f, 0.07f, 0.09f));
            Put(MeshFactory.FacetCylinder(6), V(7.5f, 6.5f), new Vector3(6.5f, 2.2f, 5f), cliff, 12f);
            Pagoda(V(7.5f, 6.5f) + Vector3.up * 2.2f, 1.1f, -10f, new Color(0.18f, 0.08f, 0.1f), new Color(0.1f, 0.05f, 0.07f), new Color(1f, 0.25f, 0.2f), 3);
            Lantern(V(7.5f, 5f) + Vector3.up * 3.2f, new Color(1f, 0.3f, 0.2f), 8f);
            Torii(V(-8.5f, 5f), 1f, 12f, new Color(0.35f, 0.05f, 0.07f));
            Torii(V(-6.3f, 5.8f), 0.8f, 8f, new Color(0.35f, 0.05f, 0.07f));
            foreach (var p in Scatter(trail, 38, 1.6f, new Rect(-14f, -10f, 28f, 20f), keep, 3f))
            {
                double r = rng.NextDouble();
                if (r < 0.5) DeadTree(p, R(0.9f, 1.6f));
                else if (r < 0.8) RockAt(p, R(0.7f, 1.8f), new Color(0.2f, 0.09f, 0.1f));
                else
                {
                    // Stone lanterns glowing red.
                    Put(MeshFactory.FacetCylinder(6), p, new Vector3(0.35f, 0.7f, 0.35f), M(new Color(0.3f, 0.26f, 0.28f)), R(0f, 60f));
                    Put(MeshFactory.FacetCylinder(6), p + Vector3.up * 0.7f, new Vector3(0.45f, 0.3f, 0.45f), Glow(new Color(1f, 0.4f, 0.25f)), R(0f, 60f));
                    Put(MeshFactory.FacetCone(6), p + Vector3.up * 1f, new Vector3(0.7f, 0.35f, 0.7f), M(new Color(0.2f, 0.17f, 0.19f)), R(0f, 60f));
                }
            }
        }

        static void Village(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.55f, 0.7f, 0.4f);
            res.fog = new Color(0.9f, 0.78f, 0.6f);
            res.ambient = new Color(0.66f, 0.64f, 0.56f);
            res.sun = new Color(1f, 0.88f, 0.7f);
            res.sunIntensity = 1.25f;
            Ground(new Color(0.48f, 0.66f, 0.28f), new Color(0.56f, 0.72f, 0.32f), 16);
            // Rice paddies, a stream, houses, a well and a big old tree.
            var paddy = M(new Color(0.45f, 0.7f, 0.75f));
            for (int i = 0; i < 4; i++) Box(V(-9f + i * 2.4f, 7f) + Vector3.up * 0.01f, new Vector3(2.1f, 0.02f, 2.6f), paddy);
            var stream = new[] { V(-14f, -8f), V(-8f, -6.5f), V(-2f, -8.5f), V(4f, -7f), V(14f, -9f) };
            Ribbon(stream, 1.2f, M(new Color(0.3f, 0.62f, 0.9f)), 0.02f);
            Bridge(V(-2.2f, -9.6f), V(-2f, -7f), 1.2f);
            var keep = new List<Vector3> { V(-9f, 7f), V(4f, 7f), V(7f, 6.5f), V(9.5f, -1.5f), V(-6f, 2.5f), V(4.5f, -2.5f) };
            House(V(4f, 7f), 1.1f, 5f, new Color(0.92f, 0.86f, 0.74f), new Color(0.45f, 0.3f, 0.2f));
            House(V(7f, 6.5f), 1f, -12f, new Color(0.9f, 0.84f, 0.72f), new Color(0.55f, 0.28f, 0.2f));
            House(V(9.5f, -1.5f), 1f, 30f, new Color(0.92f, 0.86f, 0.74f), new Color(0.4f, 0.28f, 0.18f));
            House(V(-6f, 2.5f), 0.9f, -20f, new Color(0.9f, 0.84f, 0.72f), new Color(0.5f, 0.3f, 0.2f));
            // The well.
            Put(MeshFactory.FacetCylinder(10), V(4.5f, -2.5f), new Vector3(1f, 0.6f, 1f), M(new Color(0.55f, 0.55f, 0.58f)));
            Put(MeshFactory.FacetCone(4), V(4.5f, -2.5f) + Vector3.up * 1.4f, new Vector3(1.6f, 0.6f, 1.6f), M(new Color(0.45f, 0.3f, 0.2f)), 45f);
            // The great tree.
            Put(MeshFactory.FacetCylinder(7), V(-10.5f, -1f), new Vector3(0.8f, 2.4f, 0.8f), M(new Color(0.42f, 0.28f, 0.16f)));
            Bush(V(-10.5f, -1f) + Vector3.up * 2.4f, 3.4f, new Color(0.3f, 0.58f, 0.22f));
            foreach (var p in Scatter(trail, 36, 1.5f, new Rect(-14f, -10f, 28f, 20f), keep, 2.2f))
            {
                double r = rng.NextDouble();
                if (r < 0.4) Bush(p, R(0.6f, 1.2f), new Color(0.32f, R(0.55f, 0.65f), 0.22f));
                else if (r < 0.7) Pine(p, R(0.9f, 1.3f), new Color(0.2f, 0.48f, 0.2f), false);
                else if (r < 0.85) RockAt(p, R(0.4f, 0.8f), new Color(0.55f, 0.55f, 0.52f));
                else Box(p + Vector3.up * 0.3f, new Vector3(1.4f, 0.6f, 0.1f), M(new Color(0.5f, 0.36f, 0.22f)), R(0f, 180f)); // fence
            }
            Lantern(V(0f, 1f) + Vector3.up * 1.5f, new Color(1f, 0.75f, 0.45f), 6f);
        }

        static void Ruins(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.45f, 0.4f, 0.42f);
            res.fog = new Color(0.55f, 0.48f, 0.45f);
            res.ambient = new Color(0.58f, 0.55f, 0.55f);
            res.sun = new Color(1f, 0.8f, 0.65f);
            res.sunIntensity = 1.1f;
            Ground(new Color(0.46f, 0.42f, 0.38f), new Color(0.52f, 0.47f, 0.4f), 16);
            var stone = M(new Color(0.62f, 0.6f, 0.56f));
            var stoneDark = M(new Color(0.42f, 0.4f, 0.38f));
            var keep = new List<Vector3> { V(7f, 6f), V(-8f, 6f) };
            // A broken castle keep and fallen walls.
            Put(MeshFactory.FacetCylinder(8), V(7f, 6f), new Vector3(3.5f, 4.5f, 3.5f), stone, 10f);
            Put(MeshFactory.FacetCylinder(8), V(7f, 6f) + Vector3.up * 4.5f, new Vector3(3.8f, 0.4f, 3.8f), stoneDark, 10f);
            for (int i = 0; i < 5; i++) Box(V(-10f + i * 1.6f, 6f) + Vector3.up * R(0.6f, 1.6f), new Vector3(1.4f, R(1.2f, 3.2f), 0.6f), stone, 5f);
            for (int i = 0; i < 12; i++)
            {
                var p = V(R(-13f, 13f), R(-9f, 8f));
                if (DistToTrail(trail, p) < 1.6f) continue;
                if (i % 3 == 0)
                {
                    // Broken pillar and its fallen top.
                    float h = R(1f, 2.6f);
                    Put(MeshFactory.FacetCylinder(8), p, new Vector3(0.7f, h, 0.7f), stone, R(0f, 45f));
                    var top = Put(MeshFactory.FacetCylinder(8), p + V(1f, 0.3f) + Vector3.up * 0.35f, new Vector3(0.7f, 1f, 0.7f), stone, R(0f, 45f));
                    top.transform.localRotation = Quaternion.Euler(90f, R(0f, 180f), 0f);
                }
                else for (int k = 0; k < 4; k++) Box(p + new Vector3(R(-0.8f, 0.8f), 0.2f, R(-0.8f, 0.8f)), Vector3.one * R(0.3f, 0.7f), k % 2 == 0 ? stone : stoneDark, R(0f, 90f), R(-20f, 20f));
            }
            foreach (var p in Scatter(trail, 20, 1.5f, new Rect(-14f, -10f, 28f, 20f), keep, 3f))
            {
                if (rng.NextDouble() < 0.5) Bush(p, R(0.5f, 0.9f), new Color(0.4f, 0.5f, 0.3f));
                else DeadTree(p, R(0.8f, 1.3f));
            }
            Lantern(V(7f, 3.5f) + Vector3.up * 2f, new Color(1f, 0.6f, 0.3f), 7f);
        }

        static void Shrine(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.62f, 0.72f, 0.66f);
            res.fog = new Color(0.8f, 0.85f, 0.8f);
            res.ambient = new Color(0.62f, 0.66f, 0.62f);
            res.sun = new Color(1f, 0.92f, 0.8f);
            res.sunIntensity = 1.2f;
            Ground(new Color(0.4f, 0.58f, 0.34f), new Color(0.46f, 0.64f, 0.38f), 14);
            // Stone courtyard and the shrine at the top of the trail, a row of torii along it.
            Put(MeshFactory.FacetCylinder(8), V(0f, 7f), new Vector3(9f, 0.25f, 5f), M(new Color(0.7f, 0.68f, 0.62f)), 22.5f);
            Pagoda(V(0f, 8.5f) + Vector3.up * 0.25f, 1.2f, 180f, new Color(0.85f, 0.25f, 0.2f), new Color(0.25f, 0.28f, 0.3f), new Color(1f, 0.8f, 0.45f), 2);
            Torii(V(-1.2f, -4.5f), 0.8f, 20f, new Color(0.88f, 0.2f, 0.15f));
            Torii(V(2.2f, -0.6f), 0.8f, -60f, new Color(0.88f, 0.2f, 0.15f));
            Torii(V(-0.8f, 3.5f), 0.8f, 40f, new Color(0.88f, 0.2f, 0.15f));
            var keep = new List<Vector3> { V(0f, 7.5f) };
            // Stone lanterns lining the path.
            for (int i = 0; i < 8; i++)
            {
                var p = V(R(-6f, 6f), R(-7f, 5f));
                if (DistToTrail(trail, p) < 1.2f || DistToTrail(trail, p) > 2.6f) continue;
                Put(MeshFactory.FacetCylinder(6), p, new Vector3(0.3f, 0.8f, 0.3f), M(new Color(0.6f, 0.6f, 0.58f)));
                Put(MeshFactory.FacetCylinder(6), p + Vector3.up * 0.8f, new Vector3(0.45f, 0.3f, 0.45f), Glow(new Color(1f, 0.8f, 0.45f)));
                Put(MeshFactory.FacetCone(6), p + Vector3.up * 1.1f, new Vector3(0.7f, 0.35f, 0.7f), M(new Color(0.5f, 0.5f, 0.48f)));
            }
            foreach (var p in Scatter(trail, 50, 1.6f, new Rect(-14f, -10f, 28f, 20f), keep, 5f))
            {
                // Cherry blossoms and pines.
                if (rng.NextDouble() < 0.45)
                {
                    Put(MeshFactory.FacetCylinder(5), p, new Vector3(0.25f, 1.2f, 0.25f), M(new Color(0.35f, 0.24f, 0.2f)));
                    Bush(p + Vector3.up * 1.1f, R(1.2f, 1.8f), new Color(0.98f, R(0.7f, 0.8f), 0.85f));
                }
                else Pine(p, R(0.9f, 1.4f), new Color(0.16f, 0.42f, 0.24f), false);
            }
        }

        static void DarkForest(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.12f, 0.1f, 0.2f);
            res.fog = new Color(0.2f, 0.16f, 0.3f);
            res.ambient = new Color(0.42f, 0.38f, 0.55f);
            res.sun = new Color(0.75f, 0.7f, 1f);
            res.sunIntensity = 0.95f;
            Ground(new Color(0.2f, 0.22f, 0.26f), new Color(0.24f, 0.2f, 0.3f), 14);
            // The castle of the eclipse on the horizon, glowing mushrooms and twisted trees.
            var castle = M(new Color(0.14f, 0.1f, 0.18f));
            for (int i = 0; i < 5; i++)
            {
                float h = 6f - Mathf.Abs(i - 2) * 1.5f;
                Put(MeshFactory.FacetCylinder(6), V(-4f + i * 2f, 10.5f), new Vector3(1.4f, h, 1.4f), castle);
                Put(MeshFactory.FacetCone(6), V(-4f + i * 2f, 10.5f) + Vector3.up * h, new Vector3(1.8f, 1.6f, 1.8f), castle);
                Put(MeshFactory.FacetCylinder(4), V(-4f + i * 2f, 10.1f) + Vector3.up * h * 0.6f, new Vector3(0.3f, 0.4f, 0.3f), Glow(new Color(0.8f, 0.3f, 1f)));
            }
            var ring = Put(MeshFactory.Ring(0.8f), V(0f, 11f) + Vector3.up * 9f, Vector3.one * 3f, Glow(new Color(0.8f, 0.2f, 0.35f)), 0f, false);
            ring.transform.localRotation = Quaternion.Euler(-80f, 0f, 0f);
            var mush = Glow(new Color(0.45f, 0.9f, 1f));
            var stem = M(new Color(0.85f, 0.85f, 0.9f));
            foreach (var p in Scatter(trail, 60, 1.5f, new Rect(-14f, -10f, 28f, 19f), null, 0f))
            {
                double r = rng.NextDouble();
                if (r < 0.5) Pine(p, R(1f, 1.7f), new Color(0.14f, 0.16f, 0.26f), false);
                else if (r < 0.75) DeadTree(p, R(1f, 1.5f));
                else
                {
                    Put(MeshFactory.FacetCylinder(6), p, new Vector3(0.12f, 0.35f, 0.12f), stem);
                    Put(MeshFactory.FacetCone(8), p + Vector3.up * 0.3f, new Vector3(0.5f, 0.22f, 0.5f), mush);
                }
            }
            Lantern(V(0f, 2f) + Vector3.up * 1.5f, new Color(0.6f, 0.5f, 1f), 9f);
        }

        static void Coast(Result res, List<Vector3> trail)
        {
            res.sky = new Color(0.2f, 0.55f, 0.85f);
            res.fog = new Color(0.55f, 0.78f, 0.95f);
            res.ambient = new Color(0.65f, 0.72f, 0.8f);
            res.sun = new Color(1f, 0.97f, 0.88f);
            res.sunIntensity = 1.35f;
            // Sea, shallows, then the island.
            Put(MeshFactory.Disc(), Vector3.zero, new Vector3(60f, 1f, 45f), M(new Color(0.12f, 0.5f, 0.85f)), 0f, false);
            var shallow = M(new Color(0.3f, 0.8f, 0.9f));
            var sand = M(new Color(0.95f, 0.86f, 0.62f));
            var grass = M(new Color(0.38f, 0.7f, 0.28f));
            Put(MeshFactory.Disc(), V(1f, 1f) + Vector3.up * 0.005f, new Vector3(14f, 1f, 10f), shallow, 0f, false);
            Put(MeshFactory.FacetCylinder(10), V(1f, 1f) - Vector3.up * 0.1f, new Vector3(24f, 0.2f, 17f), sand, 8f);
            Put(MeshFactory.FacetCylinder(9), V(2.5f, 3f), new Vector3(17f, 0.2f, 12f), grass, 20f);
            // Green hill at the back with the torii on top.
            Put(MeshFactory.Rock(3), V(-1f, 8f), new Vector3(9f, 3.5f, 6f), grass, 0f);
            Torii(V(-1f, 8f) + Vector3.up * 3.1f, 0.9f, 0f, new Color(0.85f, 0.14f, 0.12f));
            // Village houses.
            House(V(1.5f, 5f), 1f, 10f, new Color(0.95f, 0.92f, 0.85f), new Color(0.75f, 0.3f, 0.2f));
            House(V(4f, 6f), 0.9f, -15f, new Color(0.92f, 0.88f, 0.8f), new Color(0.55f, 0.32f, 0.2f));
            House(V(3.2f, 3.6f), 0.8f, 25f, new Color(0.95f, 0.9f, 0.82f), new Color(0.78f, 0.35f, 0.22f));
            // Lighthouse on the right.
            var white = M(new Color(0.95f, 0.95f, 0.95f));
            var red = M(new Color(0.8f, 0.18f, 0.15f));
            for (int i = 0; i < 4; i++) Put(MeshFactory.FacetCylinder(10), V(10f, 5f) + Vector3.up * i * 1f, new Vector3(1.3f - i * 0.12f, 1f, 1.3f - i * 0.12f), i % 2 == 0 ? white : red);
            Put(MeshFactory.FacetCylinder(8), V(10f, 5f) + Vector3.up * 4f, new Vector3(0.9f, 0.6f, 0.9f), Glow(new Color(1f, 0.9f, 0.5f)));
            Put(MeshFactory.FacetCone(8), V(10f, 5f) + Vector3.up * 4.6f, new Vector3(1.2f, 0.7f, 1.2f), red);
            // The ship on the left.
            var hull = M(new Color(0.45f, 0.26f, 0.14f));
            var ship = new GameObject("Ship").transform;
            ship.SetParent(root, false);
            ship.localPosition = V(-11f, 1f);
            ship.localRotation = Quaternion.Euler(0f, 70f, 0f);
            MeshFactory.Primitive(PrimitiveType.Cube, ship, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.8f, 4f), hull);
            MeshFactory.Primitive(PrimitiveType.Cube, ship, new Vector3(0f, 0.9f, -1.4f), new Vector3(1.4f, 0.6f, 1f), hull);
            MeshFactory.MeshObject(MeshFactory.FacetCylinder(6), ship, new Vector3(0f, 0.6f, 0.2f), new Vector3(0.14f, 3.6f, 0.14f), M(new Color(0.3f, 0.2f, 0.12f)));
            MeshFactory.Primitive(PrimitiveType.Cube, ship, new Vector3(0f, 2.6f, 0.2f), new Vector3(0.05f, 1.8f, 2.2f), white);
            MeshFactory.Primitive(PrimitiveType.Cube, ship, new Vector3(0f, 3.8f, 0.2f), new Vector3(0.05f, 0.8f, 1.6f), white);
            ship.gameObject.AddComponent<Sway>().Amount = 2f;
            // Docks.
            var wood = M(new Color(0.55f, 0.36f, 0.2f));
            Box(V(-7.5f, 1f) + Vector3.up * 0.1f, new Vector3(3.5f, 0.12f, 1f), wood, 70f);
            Box(V(6.5f, -3.8f) + Vector3.up * 0.1f, new Vector3(1f, 0.12f, 3f), wood, 20f);
            var keep = new List<Vector3> { V(-1f, 8f), V(1.5f, 5f), V(4f, 6f), V(3.2f, 3.6f), V(10f, 5f) };
            // Rocks in the sea with grassy tops, palms and bushes on the island.
            for (int i = 0; i < 7; i++)
            {
                var p = V(R(-13f, 13f), R(-10f, -5.5f));
                float s = R(1f, 2.4f);
                RockAt(p, s, new Color(0.45f, 0.48f, 0.52f));
                Bush(p + Vector3.up * s * 0.55f, s * 0.6f, new Color(0.3f, 0.6f, 0.22f));
            }
            foreach (var p in Scatter(trail, 26, 1.5f, new Rect(-7f, -4f, 17f, 10f), keep, 1.8f))
            {
                if (rng.NextDouble() < 0.35)
                {
                    // Palm.
                    var trunk = MeshFactory.MeshObject(MeshFactory.FacetCylinder(5), root, p, new Vector3(0.18f, 2f, 0.18f), M(new Color(0.55f, 0.4f, 0.22f)));
                    trunk.transform.localRotation = Quaternion.Euler(R(-12f, 12f), 0f, R(-12f, 12f));
                    for (int k = 0; k < 5; k++)
                    {
                        var leaf = Box(p + Vector3.up * 2f, new Vector3(0.3f, 0.05f, 1.4f), M(new Color(0.2f, 0.6f, 0.2f)), k * 72f);
                        leaf.transform.localRotation = Quaternion.Euler(20f, k * 72f, 0f);
                    }
                }
                else if (rng.NextDouble() < 0.6) Bush(p, R(0.5f, 0.9f), new Color(0.24f, 0.55f, 0.18f));
                else Pine(p, R(0.7f, 1.1f), new Color(0.15f, 0.45f, 0.2f), false);
            }
        }
    }
}
