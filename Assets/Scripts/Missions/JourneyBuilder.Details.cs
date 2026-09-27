using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Small-scale dressing that makes each road feel lived in (or died in): region props scattered along the
    /// verges, cracks in the road, drifting fog, layered hills in the distance and demon silhouettes prowling
    /// beyond the treeline. Everything stays in the flat low-poly look of the rest of the world.
    /// </summary>
    public static partial class JourneyBuilder
    {
        class PropKit
        {
            public Material rock, rockDark, wood, woodDark, leaf, grass, accent, glow, bone, snow, metal, cloth;
        }

        static PropKit Kit(ArenaTheme t)
        {
            var k = new PropKit();
            k.rock = MaterialFactory.Toon(Color.Lerp(new Color(0.5f, 0.5f, 0.52f), t.ground, 0.3f));
            k.rockDark = MaterialFactory.Toon(Color.Lerp(new Color(0.28f, 0.27f, 0.3f), t.ground, 0.3f));
            k.wood = MaterialFactory.Toon(new Color(0.4f, 0.28f, 0.17f));
            k.woodDark = MaterialFactory.Toon(new Color(0.22f, 0.15f, 0.1f));
            k.leaf = MaterialFactory.Toon(t.foliage);
            k.grass = MaterialFactory.Toon(Color.Lerp(t.groundAccent, t.foliage, 0.4f), 0f);
            k.accent = MaterialFactory.Toon(new Color(0.85f, 0.2f, 0.18f));
            k.glow = MaterialFactory.Toon(t.lantern, 0f, t.lantern);
            k.bone = MaterialFactory.Toon(new Color(0.86f, 0.83f, 0.74f));
            k.snow = MaterialFactory.Toon(new Color(0.95f, 0.96f, 1f), 0f);
            k.metal = MaterialFactory.Toon(new Color(0.3f, 0.3f, 0.34f));
            k.cloth = MaterialFactory.Toon(new Color(0.6f, 0.2f, 0.18f));
            return k;
        }

        static GameObject Prim(PrimitiveType t, Transform p, Vector3 pos, Vector3 scale, Material m, Vector3 euler)
        {
            var go = MeshFactory.Primitive(t, p, pos, scale, m);
            go.transform.rotation = Quaternion.Euler(euler);
            return go;
        }

        static bool InPlace(Journey j, Vector3 at, float margin)
        {
            foreach (var pl in j.places)
                if ((Journey.Flat(at) - Journey.Flat(pl.pos)).magnitude < pl.radius + margin) return true;
            return false;
        }

        /// <summary>Props along both verges of the road, chosen by region.</summary>
        static void RoadsideScatter(Journey j, ArenaTheme theme, Transform stat, Transform dyn)
        {
            var k = Kit(theme);
            int n = 0;
            for (float d = 3f; d < j.Length - 3f; d += R(2.6f, 4.2f), n++)
            {
                Vector3 p = j.PointAt(d);
                Vector3 ahead = j.PointAt(d + 1f) - p;
                ahead.y = 0f;
                if (ahead.sqrMagnitude < 0.0001f) continue;
                ahead.Normalize();
                Vector3 side = Vector3.Cross(Vector3.up, ahead);
                for (int s = -1; s <= 1; s += 2)
                {
                    if (rng.NextDouble() < 0.3) continue;
                    Vector3 at = p + side * s * (j.halfWidth + R(1.2f, 7f)) + ahead * R(-1f, 1f);
                    at.y = 0f;
                    if (InPlace(j, at, 0.5f)) continue;
                    Prop(theme, k, stat, dyn, at, R(0f, 360f));
                }
                // Cracks and debris on the road itself in ruined regions.
                if ((theme.kind == EnvironmentKind.FallenCity || theme.kind == EnvironmentKind.DemonLand || theme.kind == EnvironmentKind.Castle || theme.burning) && rng.NextDouble() < 0.35)
                {
                    Vector3 c = p + side * R(-j.halfWidth * 0.7f, j.halfWidth * 0.7f);
                    if (!InPlace(j, c, 0f)) Crack(theme, k, stat, c);
                }
                // Low drifting fog every so often in the wilder regions.
                if (n % 6 == 0 && (theme.kind == EnvironmentKind.Forest || theme.kind == EnvironmentKind.Mountain || theme.kind == EnvironmentKind.Temple || theme.kind == EnvironmentKind.DemonLand))
                {
                    var fog = EnvFx.Smoke(dyn, p + side * R(-8f, 8f) + Vector3.up * 0.3f, 1.6f);
                    if (fog != null)
                    {
                        var m = fog.main;
                        m.startColor = theme.kind == EnvironmentKind.DemonLand ? new Color(0.3f, 0.1f, 0.12f, 0.25f) : new Color(0.85f, 0.88f, 0.92f, 0.22f);
                    }
                }
            }
        }

        static void Prop(ArenaTheme theme, PropKit k, Transform stat, Transform dyn, Vector3 at, float yaw)
        {
            double r = rng.NextDouble();
            switch (theme.kind)
            {
                case EnvironmentKind.Forest:
                case EnvironmentKind.Village:
                    if (theme.burning && r < 0.35) { Rubble(k, stat, at, yaw); break; }
                    if (r < 0.25) Rock(k, stat, at, false);
                    else if (r < 0.42) BrokenBranch(k, stat, at, yaw);
                    else if (r < 0.55) Log(k, stat, at, yaw);
                    else if (r < 0.68) Mushrooms(k, stat, at);
                    else if (r < 0.74) RuinPillar(k, stat, at, yaw);
                    else GrassTuft(k, dyn, at);
                    break;
                case EnvironmentKind.Mountain:
                    if (r < 0.45) Rock(k, stat, at, true);
                    else if (r < 0.65) DeadTree(k, stat, at, yaw, true);
                    else if (r < 0.8) IceShards(stat, at, yaw);
                    else BrokenBranch(k, stat, at, yaw);
                    break;
                case EnvironmentKind.Kingdom:
                case EnvironmentKind.FallenCity:
                    if (r < 0.3) Rubble(k, stat, at, yaw);
                    else if (r < 0.45) BrokenWall(k, stat, at, yaw);
                    else if (r < 0.57) Cart(k, stat, at, yaw, theme.kind == EnvironmentKind.FallenCity);
                    else if (r < 0.69) Sign(k, stat, at, yaw);
                    else if (r < 0.77 && theme.kind == EnvironmentKind.FallenCity) EnvFx.Smoke(dyn, at + Vector3.up * 0.2f, 0.8f);
                    else Rock(k, stat, at, false);
                    break;
                case EnvironmentKind.Temple:
                    if (r < 0.35) RuinPillar(k, stat, at, yaw);
                    else if (r < 0.55) Rock(k, stat, at, false);
                    else if (r < 0.7) BrokenStatue(k, stat, at, yaw);
                    else GrassTuft(k, dyn, at);
                    break;
                default: // DemonLand, Castle
                    if (r < 0.3) Bones(k, stat, at, yaw);
                    else if (r < 0.5) Spikes(k, stat, at, yaw);
                    else if (r < 0.65) Rubble(k, stat, at, yaw);
                    else if (r < 0.8) DeadTree(k, stat, at, yaw, false);
                    else Rock(k, stat, at, false);
                    break;
            }
        }

        static void Rock(PropKit k, Transform p, Vector3 at, bool snowy)
        {
            float s = R(0.4f, 1.3f);
            Prim(PrimitiveType.Sphere, p, at + Vector3.up * s * 0.3f, new Vector3(s * 1.3f, s * 0.8f, s), rng.NextDouble() < 0.5 ? k.rock : k.rockDark, new Vector3(R(-10f, 10f), R(0f, 360f), R(-10f, 10f)));
            if (rng.NextDouble() < 0.5) Prim(PrimitiveType.Sphere, p, at + new Vector3(s * 0.7f, s * 0.15f, R(-0.3f, 0.3f)), Vector3.one * s * 0.45f, k.rockDark, Vector3.zero);
            if (snowy) Prim(PrimitiveType.Sphere, p, at + Vector3.up * s * 0.62f, new Vector3(s * 1.1f, s * 0.25f, s * 0.85f), k.snow, Vector3.zero);
        }

        static void BrokenBranch(PropKit k, Transform p, Vector3 at, float yaw)
        {
            float len = R(1.2f, 2.4f);
            Prim(PrimitiveType.Cylinder, p, at + Vector3.up * 0.08f, new Vector3(0.1f, len * 0.5f, 0.1f), k.woodDark, new Vector3(88f, yaw, 0f));
            Prim(PrimitiveType.Cylinder, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.25f, 0.15f, len * 0.2f), new Vector3(0.05f, 0.3f, 0.05f), k.woodDark, new Vector3(60f, yaw + 40f, 0f));
        }

        static void Log(PropKit k, Transform p, Vector3 at, float yaw)
        {
            float len = R(1.6f, 3f);
            Prim(PrimitiveType.Cylinder, p, at + Vector3.up * 0.3f, new Vector3(0.6f, len * 0.5f, 0.6f), k.wood, new Vector3(90f, yaw, 0f));
            // Pale cut end and a patch of moss.
            var end = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward * len * 0.5f;
            Prim(PrimitiveType.Cylinder, p, at + end + Vector3.up * 0.3f, new Vector3(0.52f, 0.02f, 0.52f), MaterialFactory.Toon(new Color(0.8f, 0.68f, 0.5f)), new Vector3(90f, yaw, 0f));
            Prim(PrimitiveType.Sphere, p, at + Vector3.up * 0.55f, new Vector3(0.5f, 0.12f, 0.8f), k.grass, new Vector3(0f, yaw, 0f));
        }

        static void Mushrooms(PropKit k, Transform p, Vector3 at)
        {
            int c = 2 + rng.Next(3);
            for (int i = 0; i < c; i++)
            {
                Vector3 o = at + new Vector3(R(-0.4f, 0.4f), 0f, R(-0.4f, 0.4f));
                float h = R(0.12f, 0.3f);
                Prim(PrimitiveType.Cylinder, p, o + Vector3.up * h * 0.5f, new Vector3(0.05f, h * 0.5f, 0.05f), k.bone, Vector3.zero);
                Prim(PrimitiveType.Sphere, p, o + Vector3.up * h, new Vector3(0.22f, 0.1f, 0.22f), k.accent, Vector3.zero);
            }
        }

        static void GrassTuft(PropKit k, Transform dyn, Vector3 at)
        {
            var tuft = new GameObject("Grass");
            tuft.transform.SetParent(dyn, false);
            tuft.transform.position = at;
            for (int i = 0; i < 4; i++)
                MeshFactory.MeshObject(MeshFactory.Cone(), tuft.transform, new Vector3(R(-0.25f, 0.25f), 0f, R(-0.25f, 0.25f)), new Vector3(0.12f, R(0.35f, 0.6f), 0.12f), k.grass, false);
            var sw = tuft.AddComponent<Sway>();
            sw.Amount = 6f;
            sw.Speed = R(0.8f, 1.4f);
        }

        static void RuinPillar(PropKit k, Transform p, Vector3 at, float yaw)
        {
            float h = R(0.8f, 2.4f);
            Prim(PrimitiveType.Cube, p, at + Vector3.up * 0.1f, new Vector3(1.1f, 0.2f, 1.1f), k.rockDark, new Vector3(0f, yaw, 0f));
            Prim(PrimitiveType.Cylinder, p, at + Vector3.up * (0.2f + h * 0.5f), new Vector3(0.6f, h * 0.5f, 0.6f), k.rock, new Vector3(R(-6f, 6f), yaw, R(-6f, 6f)));
            // The broken top lies beside it.
            Prim(PrimitiveType.Cylinder, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1f, 0.3f, 0f), new Vector3(0.6f, 0.4f, 0.6f), k.rock, new Vector3(85f, yaw + 30f, 0f));
        }

        static void BrokenStatue(PropKit k, Transform p, Vector3 at, float yaw)
        {
            Prim(PrimitiveType.Cube, p, at + Vector3.up * 0.4f, new Vector3(0.9f, 0.8f, 0.9f), k.rockDark, new Vector3(0f, yaw, 0f));
            Prim(PrimitiveType.Capsule, p, at + Vector3.up * 1.2f, new Vector3(0.6f, 0.5f, 0.5f), k.rock, new Vector3(0f, yaw, 8f));
            Prim(PrimitiveType.Sphere, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.9f, 0.25f, 0.3f), Vector3.one * 0.5f, k.rock, Vector3.zero); // fallen head
        }

        static void DeadTree(PropKit k, Transform p, Vector3 at, float yaw, bool snowy)
        {
            float h = R(2f, 3.6f);
            Prim(PrimitiveType.Cylinder, p, at + Vector3.up * h * 0.5f, new Vector3(0.22f, h * 0.5f, 0.22f), k.woodDark, new Vector3(R(-5f, 5f), yaw, R(-5f, 5f)));
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Cylinder, p, at + Vector3.up * h * (0.55f + i * 0.14f), new Vector3(0.07f, 0.5f, 0.07f), k.woodDark, new Vector3(0f, yaw + i * 120f, 50f));
            if (snowy) Prim(PrimitiveType.Sphere, p, at + Vector3.up * 0.05f, new Vector3(1.2f, 0.15f, 1.2f), k.snow, Vector3.zero);
        }

        static void IceShards(Transform p, Vector3 at, float yaw)
        {
            var ice = MaterialFactory.Toon(new Color(0.7f, 0.88f, 1f), 0f, new Color(0.3f, 0.45f, 0.6f));
            for (int i = 0; i < 3; i++)
            {
                var go = MeshFactory.MeshObject(MeshFactory.Cone(), p, at + new Vector3(R(-0.4f, 0.4f), 0f, R(-0.4f, 0.4f)), new Vector3(0.25f, R(0.6f, 1.4f), 0.25f), ice, false);
                go.transform.rotation = Quaternion.Euler(R(-20f, 20f), yaw, R(-20f, 20f));
            }
        }

        static void Rubble(PropKit k, Transform p, Vector3 at, float yaw)
        {
            int c = 3 + rng.Next(4);
            for (int i = 0; i < c; i++)
            {
                float s = R(0.2f, 0.7f);
                Prim(PrimitiveType.Cube, p, at + new Vector3(R(-0.8f, 0.8f), s * 0.4f, R(-0.8f, 0.8f)), new Vector3(s, s * 0.7f, s * 1.2f), rng.NextDouble() < 0.5 ? k.rock : k.rockDark,
                    new Vector3(R(-25f, 25f), R(0f, 360f), R(-25f, 25f)));
            }
            if (rng.NextDouble() < 0.5)
                Prim(PrimitiveType.Cube, p, at + Vector3.up * 0.12f, new Vector3(1.6f, 0.08f, 0.14f), k.woodDark, new Vector3(0f, yaw, 12f)); // charred beam
        }

        static void BrokenWall(PropKit k, Transform p, Vector3 at, float yaw)
        {
            var rot = Quaternion.Euler(0f, yaw, 0f);
            float w = R(2f, 3.5f);
            // A jagged wall stub: blocks of falling height.
            for (int i = 0; i < 4; i++)
            {
                float h = R(0.6f, 2.4f) * (1f - i * 0.18f);
                Prim(PrimitiveType.Cube, p, at + rot * new Vector3((i - 1.5f) * w * 0.25f, h * 0.5f, 0f), new Vector3(w * 0.25f, h, 0.35f), k.rock, new Vector3(0f, yaw, 0f));
            }
            // A dark empty window.
            Prim(PrimitiveType.Cube, p, at + rot * new Vector3(-w * 0.2f, 1.2f, 0.01f), new Vector3(0.4f, 0.5f, 0.37f), MaterialFactory.Toon(new Color(0.04f, 0.04f, 0.05f), 0f), new Vector3(0f, yaw, 0f));
            Rubble(k, p, at + rot * new Vector3(0f, 0f, 0.8f), yaw);
        }

        static void Cart(PropKit k, Transform p, Vector3 at, float yaw, bool wrecked)
        {
            var rot = Quaternion.Euler(0f, yaw, wrecked ? 70f : 0f);
            Vector3 c = at + Vector3.up * (wrecked ? 0.5f : 0.7f);
            Prim(PrimitiveType.Cube, p, c, new Vector3(1.2f, 0.5f, 2f), wrecked ? k.woodDark : k.wood, rot.eulerAngles);
            for (int s = -1; s <= 1; s += 2)
            {
                if (wrecked && s > 0) continue;
                var wheel = MeshFactory.Primitive(PrimitiveType.Cylinder, p, c + rot * new Vector3(0.68f * s, -0.25f, 0f), new Vector3(0.8f, 0.06f, 0.8f), k.woodDark);
                wheel.transform.rotation = rot * Quaternion.Euler(0f, 0f, 90f);
            }
            if (wrecked)
            {
                // The lost wheel and spilled crates.
                var lost = MeshFactory.Primitive(PrimitiveType.Cylinder, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(1.6f, 0.04f, 0.5f), new Vector3(0.8f, 0.06f, 0.8f), k.woodDark);
                lost.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                Prim(PrimitiveType.Cube, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-1.2f, 0.25f, 0.9f), Vector3.one * 0.5f, k.wood, new Vector3(0f, yaw + 20f, 0f));
            }
            else Prim(PrimitiveType.Cube, p, c + Vector3.up * 0.45f, new Vector3(1f, 0.4f, 1.4f), k.cloth, rot.eulerAngles);
        }

        static void Sign(PropKit k, Transform p, Vector3 at, float yaw)
        {
            float tilt = R(-18f, 18f);
            Prim(PrimitiveType.Cylinder, p, at + Vector3.up * 0.9f, new Vector3(0.1f, 0.9f, 0.1f), k.woodDark, new Vector3(0f, yaw, tilt));
            Prim(PrimitiveType.Cube, p, at + Quaternion.Euler(0f, yaw, tilt) * Vector3.up * 1.7f, new Vector3(0.9f, 0.55f, 0.06f), k.wood, new Vector3(0f, yaw, tilt));
            Prim(PrimitiveType.Cube, p, at + Quaternion.Euler(0f, yaw, tilt) * new Vector3(0f, 1.7f, 0.035f), new Vector3(0.6f, 0.08f, 0.01f), k.accent, new Vector3(0f, yaw, tilt));
        }

        static void Bones(PropKit k, Transform p, Vector3 at, float yaw)
        {
            Prim(PrimitiveType.Sphere, p, at + Vector3.up * 0.2f, new Vector3(0.4f, 0.35f, 0.45f), k.bone, new Vector3(0f, yaw, 0f)); // skull
            Prim(PrimitiveType.Sphere, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0.08f, 0.24f, 0.2f), new Vector3(0.09f, 0.07f, 0.05f), MaterialFactory.Toon(Color.black, 0f), Vector3.zero);
            Prim(PrimitiveType.Sphere, p, at + Quaternion.Euler(0f, yaw, 0f) * new Vector3(-0.08f, 0.24f, 0.2f), new Vector3(0.09f, 0.07f, 0.05f), MaterialFactory.Toon(Color.black, 0f), Vector3.zero);
            for (int i = 0; i < 3; i++)
                Prim(PrimitiveType.Capsule, p, at + new Vector3(R(-0.9f, 0.9f), 0.05f, R(-0.9f, 0.9f)), new Vector3(0.08f, 0.35f, 0.08f), k.bone, new Vector3(90f, R(0f, 360f), 0f));
        }

        static void Spikes(PropKit k, Transform p, Vector3 at, float yaw)
        {
            var black = MaterialFactory.Toon(new Color(0.08f, 0.05f, 0.08f));
            for (int i = 0; i < 4; i++)
            {
                var go = MeshFactory.MeshObject(MeshFactory.Cone(), p, at + new Vector3(R(-0.6f, 0.6f), 0f, R(-0.6f, 0.6f)), new Vector3(0.3f, R(1f, 2.4f), 0.3f), black, true);
                go.transform.rotation = Quaternion.Euler(R(-25f, 25f), yaw, R(-25f, 25f));
            }
        }

        static void Crack(ArenaTheme theme, PropKit k, Transform p, Vector3 at)
        {
            bool hot = theme.kind == EnvironmentKind.DemonLand || theme.burning;
            var m = hot ? MaterialFactory.Toon(new Color(1f, 0.35f, 0.08f), 0f, new Color(1f, 0.3f, 0.05f)) : MaterialFactory.Toon(new Color(0.08f, 0.07f, 0.07f), 0f);
            float yaw = R(0f, 360f);
            Vector3 cur = at;
            for (int i = 0; i < 3; i++)
            {
                yaw += R(-45f, 45f);
                float len = R(0.6f, 1.4f);
                Vector3 dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                Prim(PrimitiveType.Cube, p, cur + dir * len * 0.5f + Vector3.up * 0.03f, new Vector3(0.08f, 0.01f, len), m, new Vector3(0f, yaw, 0f));
                cur += dir * len;
            }
        }

        /// <summary>Two rings of low hills and far mountains around the whole route, fading into the fog.</summary>
        static void BackgroundLayers(Journey j, ArenaTheme theme, Transform stat, Vector3 center, float extent)
        {
            Color near = Color.Lerp(theme.ground, theme.fog, 0.35f);
            Color far = Color.Lerp(theme.ground, theme.fog, 0.7f);
            var nearM = MaterialFactory.Toon(near, 0f);
            var farM = MaterialFactory.Toon(far, 0f);
            float r1 = Mathf.Max(55f, extent * 0.75f);
            for (int i = 0; i < 18; i++)
            {
                float a = i * Mathf.PI * 2f / 18f + R(-0.1f, 0.1f);
                Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r1 + R(-6f, 6f));
                if (j.DistanceToPath(p) < 30f) continue;
                float w = R(16f, 28f);
                Prim(PrimitiveType.Sphere, stat, p, new Vector3(w, R(6f, 12f), w * 0.8f), nearM, new Vector3(0f, R(0f, 360f), 0f));
            }
            float r2 = r1 + 30f;
            for (int i = 0; i < 12; i++)
            {
                float a = i * Mathf.PI * 2f / 12f + R(-0.15f, 0.15f);
                Vector3 p = center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r2 + R(-8f, 8f));
                float h = R(18f, 34f);
                MeshFactory.MeshObject(MeshFactory.Cone(), stat, p, new Vector3(R(22f, 34f), h, R(22f, 34f)), farM, false);
                if (theme.kind == EnvironmentKind.Mountain || theme.kind == EnvironmentKind.Forest)
                    MeshFactory.MeshObject(MeshFactory.Cone(), stat, p + Vector3.up * h * 0.68f, new Vector3(9f, h * 0.32f, 9f), MaterialFactory.Toon(Color.Lerp(Color.white, theme.fog, 0.3f), 0f), false);
            }
        }

        /// <summary>A few dark shapes with glowing eyes wander just beyond the road: the region is never empty.</summary>
        static void Prowlers(Journey j, ArenaTheme theme, Transform dyn)
        {
            if (theme.kind == EnvironmentKind.Kingdom && !theme.burning) return;
            int count = 3 + rng.Next(3);
            var body = MaterialFactory.Toon(new Color(0.03f, 0.02f, 0.04f), 0f);
            Color ec = theme.kind == EnvironmentKind.Mountain ? new Color(0.5f, 0.85f, 1f) : new Color(1f, 0.25f, 0.2f);
            var eye = MaterialFactory.Toon(ec, 0f, ec);
            for (int i = 0; i < count; i++)
            {
                float d = j.Length * (i + 0.5f) / count;
                Vector3 p = j.PointAt(d);
                Vector3 ahead = j.PointAt(d + 1f) - p;
                ahead.y = 0f;
                if (ahead.sqrMagnitude < 0.0001f) continue;
                Vector3 side = Vector3.Cross(Vector3.up, ahead.normalized) * (i % 2 == 0 ? 1f : -1f);
                Vector3 at = p + side * R(20f, 30f);
                var go = new GameObject("Prowler");
                go.transform.SetParent(dyn, false);
                go.transform.position = at;
                float s = R(1.2f, 2.2f);
                MeshFactory.Primitive(PrimitiveType.Capsule, go.transform, new Vector3(0f, s, 0f), new Vector3(0.9f, s, 0.8f) * 0.9f, body);
                MeshFactory.Primitive(PrimitiveType.Sphere, go.transform, new Vector3(0.18f, s * 1.7f, 0.38f), new Vector3(0.16f, 0.08f, 0.08f), eye);
                MeshFactory.Primitive(PrimitiveType.Sphere, go.transform, new Vector3(-0.18f, s * 1.7f, 0.38f), new Vector3(0.16f, 0.08f, 0.08f), eye);
                var w = go.AddComponent<Prowler>();
                w.Home = at;
                w.Range = R(5f, 9f);
            }
        }
    }

    /// <summary>A distant silhouette that slinks between points and sometimes stops to watch the player.</summary>
    public class Prowler : MonoBehaviour
    {
        public Vector3 Home;
        public float Range = 6f;
        Vector3 target;
        float wait;

        void Start() { target = Home; }

        void Update()
        {
            if (EnemyController.Frozen) return;
            float dt = Time.deltaTime;
            if (wait > 0f)
            {
                wait -= dt;
                var b = BattleController.Current;
                if (b != null && b.Team != null && b.Team.Active != null)
                {
                    Vector3 look = b.Team.Active.Position - transform.position;
                    look.y = 0f;
                    if (look.sqrMagnitude > 0.01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), dt * 2f);
                }
                return;
            }
            Vector3 to = target - transform.position;
            to.y = 0f;
            if (to.magnitude < 0.5f)
            {
                wait = Random.Range(2f, 5f);
                Vector2 r = Random.insideUnitCircle * Range;
                target = Home + new Vector3(r.x, 0f, r.y);
                return;
            }
            transform.position += to.normalized * 1.6f * dt;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(to), dt * 4f);
            // A low, loping bob.
            var p = transform.position;
            p.y = Mathf.Abs(Mathf.Sin(Time.time * 5f)) * 0.12f;
            transform.position = p;
        }
    }
}
