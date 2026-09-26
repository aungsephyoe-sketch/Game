using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Builds the 3D world for a <see cref="Journey"/>: one continuous stretch of the region with a real road
    /// (dirt track, stone street, snowy trail, cracked demon road) winding between named clearings, roadside
    /// landmarks that guide the eye (lanterns, torches, flags, braziers), bridges over ravines, region scenery
    /// on both sides, a distinct boss arena at the destination, and the next region's landmark on the horizon.
    /// </summary>
    public static class JourneyBuilder
    {
        public class Result
        {
            public GameObject root;
            public ParticleSystem weather;
            public Transform follow;
        }

        static System.Random rng;
        static float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        public static Result Build(Journey j, MissionDefinition m, Transform parent)
        {
            rng = new System.Random(m.id.GetHashCode() ^ 0x5eed);
            var theme = m.theme;
            var res = new Result();
            var root = new GameObject("Journey");
            root.transform.SetParent(parent, false);
            res.root = root;
            var stat = new GameObject("Static").transform;
            stat.SetParent(root.transform, false);
            var dyn = new GameObject("Dynamic").transform;
            dyn.SetParent(root.transform, false);

            // Ground covering the whole route.
            Vector3 min = j.places[0].pos, max = min;
            foreach (var p in j.path) { min = Vector3.Min(min, p); max = Vector3.Max(max, p); }
            Vector3 center = (min + max) * 0.5f;
            float extent = Mathf.Max(max.x - min.x, max.z - min.z) * 0.5f + 90f;
            var ground = MeshFactory.MeshObject(MeshFactory.Disc(), stat, new Vector3(center.x, 0f, center.z), new Vector3(extent, 1f, extent), MaterialFactory.Toon(theme.ground, 0f), false);
            ground.GetComponent<Renderer>().receiveShadows = true;

            // The road itself: a darker border, then the surface; clearings at every place.
            Color roadC = RoadColor(theme);
            var roadMat = MaterialFactory.Toon(roadC, 0f);
            var edgeMat = MaterialFactory.Toon(Color.Lerp(roadC, theme.ground, 0.5f) * 0.85f, 0f);
            Ribbon(stat, j.path, j.halfWidth + 0.9f, 0.012f, edgeMat);
            Ribbon(stat, j.path, j.halfWidth - 0.4f, 0.02f, roadMat);
            foreach (var pl in j.places)
            {
                MeshFactory.MeshObject(MeshFactory.Disc(), stat, pl.pos + Vector3.up * 0.011f, new Vector3(pl.radius + 1f, 1f, pl.radius + 1f), edgeMat, false);
                MeshFactory.MeshObject(MeshFactory.Disc(), stat, pl.pos + Vector3.up * 0.018f, new Vector3(pl.radius, 1f, pl.radius), roadMat, false);
            }

            // Scenery: the region's set dressing around every place and along every stretch between them.
            ArenaDecor.Ambient = false;
            for (int i = 0; i < j.places.Count; i++)
            {
                var pl = j.places[i];
                Chunk(j, theme, stat, dyn, pl.pos, i * 17 + 3, !pl.isBossArena && i > 0, 0.55f);
                if (i < j.places.Count - 1) Chunk(j, theme, stat, dyn, (pl.pos + j.places[i + 1].pos) * 0.5f, i * 31 + 11, false, 0.45f);
            }
            ArenaDecor.Ambient = true;

            RoadsideGuides(j, theme, stat, dyn);
            foreach (var pl in j.places)
                if (pl.isBridge) Bridge(j, pl, theme, stat);
            foreach (var pl in j.places)
                if (pl.isBossArena) BossArena.Dress(j, m, pl, theme, stat, dyn);
            Vista(j, m, stat);

            // Sky.
            if (theme.night)
            {
                Color moonC = theme.kind == EnvironmentKind.DemonLand || theme.burning || theme.kind == EnvironmentKind.Castle ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.95f, 0.8f);
                var moon = MeshFactory.Primitive(PrimitiveType.Sphere, dyn, Vector3.zero, Vector3.one * 14f, MaterialFactory.Toon(moonC, 0f, moonC));
                moon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                moon.AddComponent<SkyAnchor>().Offset = new Vector3(-35f, 38f, 90f);
            }
            else CloudDrift.CreateLayer(dyn, center, 10, 26f, new Color(1f, 1f, 1f, 0.55f), extent);
            BirdFlock.Create(dyn, j.places[Mathf.Min(1, j.places.Count - 1)].pos, 5, theme.night ? new Color(0.12f, 0.1f, 0.1f) : new Color(0.95f, 0.95f, 0.95f)).Radius = 24f;

            // Weather that travels with the player (so it's always around them, never a static box).
            var follow = new GameObject("WeatherAnchor").transform;
            follow.SetParent(dyn, false);
            follow.position = j.Start;
            res.follow = follow;
            res.weather = EnvFx.Weather(follow, Vector3.zero, WeatherKind(theme), 36f);

            ArenaBuilder.ApplyLighting(theme);
            StaticBatchingUtility.Combine(stat.gameObject);
            return res;
        }

        static string WeatherKind(ArenaTheme t)
        {
            switch (t.kind)
            {
                case EnvironmentKind.Mountain: return "snow";
                case EnvironmentKind.DemonLand: case EnvironmentKind.FallenCity: case EnvironmentKind.Castle: return "embers";
                case EnvironmentKind.Temple: return "motes";
                case EnvironmentKind.Village: return t.burning ? "ash" : "leaves";
                default: return "leaves";
            }
        }

        public static Color RoadColor(ArenaTheme t)
        {
            switch (t.kind)
            {
                case EnvironmentKind.Kingdom: return new Color(0.72f, 0.68f, 0.6f);
                case EnvironmentKind.FallenCity: return new Color(0.4f, 0.34f, 0.3f);
                case EnvironmentKind.Temple: return new Color(0.45f, 0.47f, 0.46f);
                case EnvironmentKind.Castle: return new Color(0.16f, 0.13f, 0.17f);
                case EnvironmentKind.DemonLand: return new Color(0.2f, 0.12f, 0.1f);
                case EnvironmentKind.Mountain: return new Color(0.7f, 0.72f, 0.78f);
                default: return Color.Lerp(t.groundAccent, new Color(0.45f, 0.35f, 0.24f), 0.6f);
            }
        }

        /// <summary>Region dressing around a point, with anything that would block the road or another clearing removed.</summary>
        static void Chunk(Journey j, ArenaTheme theme, Transform stat, Transform dyn, Vector3 at, int seed, bool breakables, float density)
        {
            var s = new GameObject("ChunkStatic").transform;
            s.SetParent(stat, false);
            var d = new GameObject("ChunkDynamic").transform;
            d.SetParent(dyn, false);
            ArenaDecor.Build(theme, s, d, seed, breakables, density);
            s.position = at;
            d.position = at;
            foreach (var w in d.GetComponentsInChildren<NpcWalker>()) w.Center += at;
            Cull(j, s, at, true);
            Cull(j, d, at, false);
        }

        static void Cull(Journey j, Transform chunk, Vector3 at, bool isStatic)
        {
            var kill = new List<GameObject>();
            for (int i = 0; i < chunk.childCount; i++)
            {
                var c = chunk.GetChild(i);
                if (c.GetComponent<ParticleSystem>() != null || c.GetComponent<BirdFlock>() != null || c.GetComponent<NpcWalker>() != null) continue;
                float size = 0.5f;
                var rs = c.GetComponentsInChildren<Renderer>();
                if (rs.Length > 0)
                {
                    Bounds b = rs[0].bounds;
                    for (int k = 1; k < rs.Length; k++) b.Encapsulate(rs[k].bounds);
                    size = Mathf.Min(6f, Mathf.Max(b.extents.x, b.extents.z));
                }
                Vector3 p = c.position;
                bool inOwnClearing = (Journey.Flat(p) - Journey.Flat(at)).magnitude < 14f && c.GetComponent<Breakable>() != null;
                if (inOwnClearing) continue;
                bool blocked = j.DistanceToPath(p) < j.halfWidth + 1.2f + size;
                if (!blocked)
                    foreach (var pl in j.places)
                        if ((Journey.Flat(p) - Journey.Flat(pl.pos)).magnitude < pl.radius + 1f + size) { blocked = true; break; }
                // Tiny ground tufts may stay at the road edge; anything big must not.
                if (blocked && !(size < 0.35f && j.DistanceToPath(p) > j.halfWidth - 0.5f)) kill.Add(c.gameObject);
            }
            foreach (var k in kill) Object.DestroyImmediate(k);
        }

        /// <summary>A flat strip following the road.</summary>
        static void Ribbon(Transform parent, List<Vector3> pts, float half, float y, Material mat)
        {
            var verts = new List<Vector3>();
            var tris = new List<int>();
            var uvs = new List<Vector2>();
            float along = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 dir = i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1];
                dir.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, dir.normalized) * half;
                if (i > 0) along += Vector3.Distance(pts[i], pts[i - 1]);
                verts.Add(pts[i] - side + Vector3.up * y);
                verts.Add(pts[i] + side + Vector3.up * y);
                uvs.Add(new Vector2(0f, along * 0.2f));
                uvs.Add(new Vector2(1f, along * 0.2f));
                if (i > 0)
                {
                    int b = verts.Count - 4;
                    tris.AddRange(new[] { b, b + 2, b + 1, b + 1, b + 2, b + 3 });
                }
            }
            var mesh = new Mesh();
            if (verts.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.SetVertices(verts);
            mesh.SetTriangles(tris, 0);
            mesh.SetUVs(0, uvs);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            var go = new GameObject("Road");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.receiveShadows = true;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        /// <summary>Lanterns, torches, prayer flags or braziers along the road edges so the way forward is always readable.</summary>
        static void RoadsideGuides(Journey j, ArenaTheme theme, Transform stat, Transform dyn)
        {
            var post = MaterialFactory.Toon(theme.kind == EnvironmentKind.Castle || theme.kind == EnvironmentKind.DemonLand ? new Color(0.12f, 0.1f, 0.12f) : new Color(0.45f, 0.44f, 0.42f));
            var wood = MaterialFactory.Toon(new Color(0.35f, 0.24f, 0.15f));
            var glow = MaterialFactory.Toon(theme.lantern, 0f, theme.lantern);
            int lights = Mathf.Max(0, GameSettings.MaxDynamicLights - 2);
            float step = 11f;
            int n = 0;
            for (float d = 6f; d < j.Length - 6f; d += step, n++)
            {
                Vector3 p = j.PointAt(d);
                Vector3 ahead = j.PointAt(d + 1f) - p;
                ahead.y = 0f;
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized) * (j.halfWidth + 0.8f) * (n % 2 == 0 ? 1f : -1f);
                Vector3 at = p + side;
                bool nearPlace = false;
                foreach (var pl in j.places) if ((Journey.Flat(at) - Journey.Flat(pl.pos)).magnitude < pl.radius + 1.5f) nearPlace = true;
                if (nearPlace) continue;
                switch (theme.kind)
                {
                    case EnvironmentKind.Mountain:
                        // Prayer-flag poles.
                        MeshFactory.Primitive(PrimitiveType.Cylinder, stat, at + Vector3.up * 1.4f, new Vector3(0.1f, 1.4f, 0.1f), wood);
                        for (int f = 0; f < 4; f++)
                        {
                            var flag = MeshFactory.Primitive(PrimitiveType.Cube, dyn, at + new Vector3(0f, 2.5f - f * 0.35f, 0f) + ahead.normalized * 0.25f, new Vector3(0.04f, 0.25f, 0.4f),
                                MaterialFactory.Toon(Color.HSVToRGB((f * 0.21f + n * 0.07f) % 1f, 0.6f, 0.9f), 0f));
                            flag.AddComponent<Sway>().Amount = 12f;
                        }
                        break;
                    case EnvironmentKind.DemonLand:
                    case EnvironmentKind.Castle:
                    case EnvironmentKind.FallenCity:
                        // Iron braziers.
                        MeshFactory.Primitive(PrimitiveType.Cylinder, stat, at + Vector3.up * 0.6f, new Vector3(0.25f, 0.6f, 0.25f), post);
                        MeshFactory.Primitive(PrimitiveType.Cylinder, stat, at + Vector3.up * 1.25f, new Vector3(0.7f, 0.1f, 0.7f), post);
                        if (n % 2 == 0) EnvFx.Fire(dyn, at + Vector3.up * 1.35f, 0.45f, lights-- > 0);
                        break;
                    case EnvironmentKind.Kingdom:
                        // Street lamps.
                        MeshFactory.Primitive(PrimitiveType.Cylinder, stat, at + Vector3.up * 1.6f, new Vector3(0.12f, 1.6f, 0.12f), post);
                        MeshFactory.Primitive(PrimitiveType.Cube, stat, at + Vector3.up * 3.2f, new Vector3(0.4f, 0.45f, 0.4f), glow);
                        break;
                    default:
                        // Stone lanterns (village, forest, temple).
                        MeshFactory.Primitive(PrimitiveType.Cylinder, stat, at + Vector3.up * 0.45f, new Vector3(0.28f, 0.45f, 0.28f), post);
                        MeshFactory.Primitive(PrimitiveType.Cube, stat, at + Vector3.up * 1.05f, new Vector3(0.5f, 0.38f, 0.5f), glow);
                        var cap = MeshFactory.Primitive(PrimitiveType.Cube, stat, at + Vector3.up * 1.38f, new Vector3(0.85f, 0.16f, 0.85f), post);
                        cap.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
                        if (n % 3 == 0 && lights-- > 0)
                        {
                            var lg = new GameObject("Light");
                            lg.transform.SetParent(dyn, false);
                            lg.transform.position = at + Vector3.up * 1.1f;
                            var l = lg.AddComponent<Light>();
                            l.type = LightType.Point;
                            l.color = theme.lantern;
                            l.range = 8f;
                            l.intensity = 1.1f;
                            lg.AddComponent<LanternFlicker>();
                        }
                        break;
                }
            }
            // Wooden signposts at every junction into a named place point the way.
            for (int i = 1; i < j.places.Count; i++)
            {
                var pl = j.places[i];
                Vector3 toward = j.places[i - 1].pos - pl.pos;
                toward.y = 0f;
                Vector3 at = pl.pos + toward.normalized * (pl.radius + 1.5f) + Vector3.Cross(Vector3.up, toward.normalized) * (j.halfWidth + 1f);
                MeshFactory.Primitive(PrimitiveType.Cylinder, stat, at + Vector3.up * 1f, new Vector3(0.12f, 1f, 0.12f), wood);
                var board = MeshFactory.Primitive(PrimitiveType.Cube, stat, at + Vector3.up * 1.8f, new Vector3(1.1f, 0.3f, 0.06f), wood);
                board.transform.rotation = Quaternion.LookRotation(Vector3.Cross(Vector3.up, -toward.normalized));
            }
        }

        /// <summary>A plank bridge carrying the road over a misty ravine.</summary>
        static void Bridge(Journey j, JourneyPlace pl, ArenaTheme theme, Transform stat)
        {
            // Put the ravine on the road just before the place.
            float at = j.Progress(pl.pos) - pl.radius - 7f;
            if (at < 4f) return;
            Vector3 c = j.PointAt(at);
            Vector3 dir = j.PointAt(at + 1f) - c;
            dir.y = 0f;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            var rot = Quaternion.LookRotation(dir);
            var chasm = MaterialFactory.Toon(new Color(0.02f, 0.03f, 0.05f), 0f);
            var water = MaterialFactory.Toon(theme.kind == EnvironmentKind.DemonLand ? new Color(1f, 0.35f, 0.05f) : new Color(0.25f, 0.45f, 0.6f), 0f,
                theme.kind == EnvironmentKind.DemonLand ? new Color(1f, 0.3f, 0.05f) : (Color?)null);
            var gap = MeshFactory.Primitive(PrimitiveType.Cube, stat, c + Vector3.up * 0.022f, new Vector3(60f, 0.01f, 6f), chasm);
            gap.transform.rotation = Quaternion.LookRotation(side);
            var river = MeshFactory.Primitive(PrimitiveType.Cube, stat, c + Vector3.up * 0.026f, new Vector3(60f, 0.01f, 1.6f), water);
            river.transform.rotation = Quaternion.LookRotation(side);
            var wood = MaterialFactory.Toon(new Color(0.42f, 0.3f, 0.18f));
            var rope = MaterialFactory.Toon(new Color(0.55f, 0.48f, 0.35f), 0f);
            for (int k = -5; k <= 5; k++)
            {
                var plank = MeshFactory.Primitive(PrimitiveType.Cube, stat, c + dir * (k * 0.62f) + Vector3.up * 0.12f, new Vector3(j.halfWidth * 1.4f, 0.1f, 0.5f), wood);
                plank.transform.rotation = rot * Quaternion.Euler(0f, 0f, (k % 2) * 1.5f);
            }
            for (int s = -1; s <= 1; s += 2)
            {
                for (int e = -1; e <= 1; e += 2)
                    MeshFactory.Primitive(PrimitiveType.Cylinder, stat, c + side * s * j.halfWidth * 0.75f + dir * e * 3.6f + Vector3.up * 0.8f, new Vector3(0.18f, 0.8f, 0.18f), wood);
                var line = MeshFactory.Primitive(PrimitiveType.Cube, stat, c + side * s * j.halfWidth * 0.75f + Vector3.up * 1.3f, new Vector3(0.05f, 0.05f, 7.2f), rope);
                line.transform.rotation = rot;
            }
        }

        /// <summary>The next region's landmark rises on the horizon, beyond the destination.</summary>
        static void Vista(Journey j, MissionDefinition m, Transform stat)
        {
            var end = j.places[j.places.Count - 1].pos;
            float dist = Mathf.Clamp(m.theme.fogEnd * 0.9f, 45f, 80f);
            Vector3 c = end + j.EndDirection * dist;
            var kind = m.theme.kind;
            if (kind == EnvironmentKind.Village || kind == EnvironmentKind.Forest)
            {
                // Hakuro's snowy peaks.
                var rock = MaterialFactory.Toon(new Color(0.55f, 0.58f, 0.66f), 0f);
                var snow = MaterialFactory.Toon(new Color(0.95f, 0.96f, 1f), 0f);
                for (int i = 0; i < 3; i++)
                {
                    Vector3 p = c + Vector3.Cross(Vector3.up, j.EndDirection) * (i - 1) * 26f + j.EndDirection * (i == 1 ? 12f : 0f);
                    float h = i == 1 ? 55f : 38f;
                    MeshFactory.MeshObject(MeshFactory.Cone(), stat, p, new Vector3(32f, h, 32f), rock, false);
                    MeshFactory.MeshObject(MeshFactory.Cone(), stat, p + Vector3.up * h * 0.62f, new Vector3(12.5f, h * 0.38f, 12.5f), snow, false);
                }
            }
            else if (kind == EnvironmentKind.Mountain)
            {
                // The white capital in the valley.
                var wall = MaterialFactory.Toon(new Color(0.92f, 0.9f, 0.85f), 0f);
                var roof = MaterialFactory.Toon(new Color(0.25f, 0.38f, 0.65f), 0f);
                MeshFactory.Primitive(PrimitiveType.Cube, stat, c + Vector3.up * 5f, new Vector3(50f, 10f, 12f), wall);
                MeshFactory.Primitive(PrimitiveType.Cylinder, stat, c + Vector3.up * 16f, new Vector3(9f, 16f, 9f), wall);
                MeshFactory.MeshObject(MeshFactory.Cone(), stat, c + Vector3.up * 32f, new Vector3(13f, 10f, 13f), roof, false);
            }
            else
            {
                // The Castle of the Eclipse under its red sky.
                var black = MaterialFactory.Toon(new Color(0.08f, 0.05f, 0.1f), 0f);
                var red = MaterialFactory.Toon(new Color(0.8f, 0.1f, 0.25f), 0f, new Color(0.8f, 0.1f, 0.25f));
                for (int i = 0; i < 5; i++)
                {
                    Vector3 p = c + Vector3.Cross(Vector3.up, j.EndDirection) * (i - 2) * 9f;
                    float h = 40f - Mathf.Abs(i - 2) * 9f;
                    MeshFactory.Primitive(PrimitiveType.Cube, stat, p + Vector3.up * h * 0.5f, new Vector3(7f, h, 7f), black);
                    MeshFactory.MeshObject(MeshFactory.Cone(), stat, p + Vector3.up * h, new Vector3(9f, 12f, 9f), black, false);
                }
                var eclipse = MeshFactory.MeshObject(MeshFactory.Ring(0.8f), stat, c + Vector3.up * 58f + j.EndDirection * 10f, Vector3.one * 16f, red, false);
                eclipse.transform.rotation = Quaternion.LookRotation(Vector3.up, -j.EndDirection);
            }
        }
    }
}
