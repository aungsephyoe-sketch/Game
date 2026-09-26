using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Location-specific set dressing. Every region gets its own silhouette, palette, props, weather and
    /// ambient life so the journey visibly changes as the story moves on. Density follows the graphics tier.
    /// Static pieces go under <c>root</c> (static-batched); moving pieces (NPCs, birds, breakables) under <c>dynamicRoot</c>.
    /// </summary>
    public static class ArenaDecor
    {
        static System.Random rng;
        static float density;
        const float R = BattleController.ArenaRadius;

        static float Rand(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }
        static bool Chance(double p) { return rng.NextDouble() < p; }
        static Vector3 Ring(float angleDeg, float radius) { return Quaternion.Euler(0f, angleDeg, 0f) * Vector3.forward * radius; }

        public static void Build(ArenaTheme theme, Transform root, Transform dynamicRoot, int seed, bool combatArena = true)
        {
            rng = new System.Random(seed);
            density = GameSettings.SceneryDensity;
            switch (theme.kind)
            {
                case EnvironmentKind.Village: Village(theme, root, dynamicRoot); break;
                case EnvironmentKind.Forest: Forest(theme, root, dynamicRoot); break;
                case EnvironmentKind.Mountain: Mountain(theme, root, dynamicRoot); break;
                case EnvironmentKind.Kingdom: Kingdom(theme, root, dynamicRoot, false); break;
                case EnvironmentKind.FallenCity: Kingdom(theme, root, dynamicRoot, true); break;
                case EnvironmentKind.Temple: Temple(theme, root, dynamicRoot); break;
                case EnvironmentKind.DemonLand: DemonLand(theme, root, dynamicRoot); break;
                default: Castle(theme, root, dynamicRoot); break;
            }
            Grass(theme, root);
            if (combatArena) Breakables(dynamicRoot, theme);
        }

        // ------------------------------------------------------------------ Shared props

        static void Tree(Transform root, Vector3 pos, float h, Color leaf, Transform dynamicRoot)
        {
            var trunk = MaterialFactory.Toon(new Color(0.22f, 0.14f, 0.1f));
            var leaves = MaterialFactory.Toon(leaf, 0.02f);
            var t = new GameObject("Tree").transform;
            t.SetParent(dynamicRoot, false);
            t.position = pos;
            MeshFactory.Primitive(PrimitiveType.Cylinder, t, Vector3.up * h * 0.5f, new Vector3(0.45f, h * 0.5f, 0.45f), trunk);
            for (int k = 0; k < 3; k++)
                MeshFactory.Primitive(PrimitiveType.Sphere, t, new Vector3(Rand(-0.8f, 0.8f), h + 0.3f * k, Rand(-0.8f, 0.8f)), Vector3.one * Rand(2.2f, 3.4f), leaves);
            var sway = t.gameObject.AddComponent<Sway>();
            sway.Amount = 1.5f;
            sway.Speed = Rand(0.5f, 0.9f);
        }

        static void Pine(Transform root, Vector3 pos, float h, Color leaf)
        {
            var trunk = MaterialFactory.Toon(new Color(0.2f, 0.13f, 0.1f));
            var needles = MaterialFactory.Toon(leaf, 0.02f);
            MeshFactory.Primitive(PrimitiveType.Cylinder, root, pos + Vector3.up * 0.6f, new Vector3(0.35f, 0.6f, 0.35f), trunk);
            for (int k = 0; k < 3; k++)
                MeshFactory.MeshObject(MeshFactory.Cone(), root, pos + Vector3.up * (1f + k * h * 0.22f), new Vector3(h * (0.55f - k * 0.13f), h * 0.4f, h * (0.55f - k * 0.13f)), needles);
        }

        static void House(Transform root, Vector3 pos, Color wallC, Color roofC, Color windowC, bool burning, float scale = 1f)
        {
            var wall = MaterialFactory.Toon(wallC);
            var timber = MaterialFactory.Toon(new Color(0.25f, 0.15f, 0.1f));
            var roof = MaterialFactory.Toon(roofC);
            var window = MaterialFactory.Toon(windowC, 0f, windowC * 0.9f);
            var house = new GameObject("House").transform;
            house.SetParent(root, false);
            house.position = pos;
            house.rotation = Quaternion.LookRotation(-new Vector3(pos.x, 0f, pos.z).normalized);
            float w = Rand(4f, 6f) * scale, h = Rand(2.6f, 3.4f) * scale, d = Rand(3.5f, 4.5f) * scale;
            MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
            MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h, 0f), new Vector3(w + 0.1f, 0.18f, d + 0.1f), timber);
            MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(-w * 0.5f, h * 0.5f, -d * 0.5f), new Vector3(0.2f, h, 0.2f), timber);
            MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(w * 0.5f, h * 0.5f, -d * 0.5f), new Vector3(0.2f, h, 0.2f), timber);
            var r1 = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h + 0.75f * scale, -d * 0.26f), new Vector3(w + 1f, 0.2f, d * 0.62f), roof);
            r1.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
            var r2 = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h + 0.75f * scale, d * 0.26f), new Vector3(w + 1f, 0.2f, d * 0.62f), roof);
            r2.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
            var win = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(-w * 0.22f, h * 0.55f, -d * 0.5f - 0.02f), new Vector3(w * 0.25f, h * 0.3f, 0.05f), window);
            win.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (burning && Chance(0.55))
            {
                EnvFx.Fire(house, new Vector3(Rand(-1f, 1f), h + 0.9f * scale, 0f), 1.4f, Chance(0.5));
                if (Chance(0.5)) EnvFx.Smoke(house, new Vector3(0f, h + 2.5f, 0f), 1.2f);
            }
        }

        static void Grass(ArenaTheme theme, Transform root)
        {
            if (theme.kind == EnvironmentKind.Castle || theme.kind == EnvironmentKind.DemonLand || theme.kind == EnvironmentKind.Kingdom) return;
            Color c = theme.kind == EnvironmentKind.Mountain ? new Color(0.9f, 0.93f, 1f) : Color.Lerp(new Color(0.2f, 0.35f, 0.18f), theme.ground, 0.35f);
            var grass = MaterialFactory.Toon(c, 0f);
            var flower = MaterialFactory.Toon(theme.foliage, 0f, theme.foliage * 0.25f);
            int count = Mathf.RoundToInt(60 * density);
            for (int i = 0; i < count; i++)
            {
                float a = Rand(0f, Mathf.PI * 2f);
                float r = Mathf.Sqrt(Rand(0.05f, 1f)) * (R + 3f);
                var pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                var tuft = MeshFactory.Primitive(PrimitiveType.Capsule, root, pos + Vector3.up * 0.1f,
                    new Vector3(Rand(0.25f, 0.45f), Rand(0.12f, 0.22f), Rand(0.25f, 0.45f)), Chance(0.15) ? flower : grass);
                tuft.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void Breakables(Transform dynamicRoot, ArenaTheme theme)
        {
            int n = theme.kind == EnvironmentKind.Castle || theme.kind == EnvironmentKind.Temple ? 4 : 6;
            for (int i = 0; i < n; i++)
                Breakable.Create(dynamicRoot, Ring(i * 360f / n + Rand(-15f, 15f), Rand(8f, 13f)), Chance(0.5));
        }

        static void Lanterns(Transform root, ArenaTheme theme, int count)
        {
            var stone = MaterialFactory.Toon(new Color(0.5f, 0.5f, 0.52f));
            var glow = MaterialFactory.Toon(theme.lantern, 0f, theme.lantern);
            int lights = Mathf.Min(4, GameSettings.MaxDynamicLights);
            for (int i = 0; i < count; i++)
            {
                var l = new GameObject("StoneLantern").transform;
                l.SetParent(root, false);
                l.position = Ring(i * (360f / count) + 22.5f, R + 0.9f);
                MeshFactory.Primitive(PrimitiveType.Cylinder, l, new Vector3(0f, 0.5f, 0f), new Vector3(0.3f, 0.5f, 0.3f), stone);
                MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.4f, 0.6f), glow);
                var cap = MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(0f, 1.5f, 0f), new Vector3(0.95f, 0.18f, 0.95f), stone);
                cap.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                if (i % 2 == 0 && lights-- > 0)
                {
                    var lg = new GameObject("Light");
                    lg.transform.SetParent(l, false);
                    lg.transform.localPosition = new Vector3(0f, 1.2f, 0f);
                    var light = lg.AddComponent<Light>();
                    light.type = LightType.Point;
                    light.color = theme.lantern;
                    light.range = 7f;
                    light.intensity = 1.2f;
                    light.shadows = LightShadows.None;
                    lg.AddComponent<LanternFlicker>();
                }
            }
        }

        // ------------------------------------------------------------------ Regions

        static void Village(ArenaTheme t, Transform root, Transform dyn)
        {
            int houses = Mathf.RoundToInt(12 * density);
            for (int i = 0; i < houses; i++)
            {
                float a = (i + 0.5f) / houses * 360f + Rand(-6f, 6f);
                House(root, Ring(a, Rand(21f, 25f)), new Color(0.78f, 0.7f, 0.58f), new Color(0.45f, 0.32f, 0.18f), t.lantern, t.burning);
            }
            // Farm plots and fences.
            var crop = MaterialFactory.Toon(new Color(0.35f, 0.55f, 0.2f), 0.01f);
            var fence = MaterialFactory.Toon(new Color(0.4f, 0.28f, 0.18f));
            for (int f = 0; f < 3; f++)
            {
                Vector3 c = Ring(120f * f + 60f, R + 5f);
                for (int row = 0; row < 5; row++)
                    for (int k = 0; k < 6; k++)
                        MeshFactory.Primitive(PrimitiveType.Capsule, root, c + new Vector3(k * 0.7f - 1.8f, 0.2f, row * 0.8f - 1.6f), new Vector3(0.3f, 0.25f, 0.3f), crop);
                for (int k = 0; k < 6; k++)
                    MeshFactory.Primitive(PrimitiveType.Cube, root, c + new Vector3(k * 0.9f - 2.3f, 0.45f, -2.4f), new Vector3(0.12f, 0.9f, 0.12f), fence);
                MeshFactory.Primitive(PrimitiveType.Cube, root, c + new Vector3(0f, 0.65f, -2.4f), new Vector3(4.8f, 0.1f, 0.08f), fence);
            }
            for (int i = 0; i < Mathf.RoundToInt(22 * density); i++) Tree(root, Ring(Rand(0f, 360f), Rand(28f, 36f)), Rand(3f, 5.5f), t.foliage, dyn);
            Lanterns(root, t, 8);
            // Mountains on the horizon.
            var mountain = MaterialFactory.Toon(Color.Lerp(t.fog, new Color(0.25f, 0.28f, 0.3f), 0.5f), 0f);
            for (int i = 0; i < 7; i++)
                MeshFactory.MeshObject(MeshFactory.Cone(), root, Ring(i * 52f + 10f, Rand(55f, 70f)), new Vector3(Rand(30f, 45f), Rand(18f, 30f), Rand(30f, 45f)), mountain, false);
            if (t.burning)
            {
                EnvFx.Weather(dyn, Vector3.zero, "embers");
                EnvFx.Weather(dyn, Vector3.zero, "ash");
            }
            else
            {
                EnvFx.Weather(dyn, Vector3.zero, "leaves");
                BirdFlock.Create(dyn, Vector3.zero, 5, new Color(0.15f, 0.12f, 0.12f));
            }
        }

        static void Forest(ArenaTheme t, Transform root, Transform dyn)
        {
            var bark = MaterialFactory.Toon(new Color(0.16f, 0.12f, 0.1f));
            var canopy = MaterialFactory.Toon(t.foliage, 0.02f);
            int giants = Mathf.RoundToInt(14 * density) + 4;
            for (int i = 0; i < giants; i++)
            {
                Vector3 p = Ring(i * 360f / giants + Rand(-8f, 8f), Rand(19f, 26f));
                float h = Rand(12f, 18f);
                var tr = new GameObject("GiantTree").transform;
                tr.SetParent(root, false);
                tr.position = p;
                MeshFactory.Primitive(PrimitiveType.Cylinder, tr, Vector3.up * h * 0.5f, new Vector3(Rand(1.6f, 2.4f), h * 0.5f, Rand(1.6f, 2.4f)), bark);
                for (int k = 0; k < 3; k++)
                {
                    var root2 = MeshFactory.Primitive(PrimitiveType.Capsule, tr, new Vector3(0f, 0.3f, 0f), new Vector3(0.5f, 1.6f, 0.5f), bark);
                    root2.transform.localRotation = Quaternion.Euler(70f, k * 120f + Rand(0f, 40f), 0f);
                    root2.transform.localPosition = Quaternion.Euler(0f, k * 120f, 0f) * Vector3.forward * 1.2f + Vector3.up * 0.3f;
                }
                for (int k = 0; k < 4; k++)
                    MeshFactory.Primitive(PrimitiveType.Sphere, tr, new Vector3(Rand(-3f, 3f), h + Rand(-1f, 2f), Rand(-3f, 3f)), Vector3.one * Rand(6f, 9f), canopy);
            }
            // Glowing plants and mushrooms.
            var glow = MaterialFactory.Toon(new Color(0.4f, 1f, 0.7f), 0f, new Color(0.3f, 0.9f, 0.6f));
            var shroom = MaterialFactory.Toon(new Color(0.8f, 0.35f, 0.6f), 0.01f, new Color(0.4f, 0.1f, 0.3f));
            var stem = MaterialFactory.Toon(new Color(0.85f, 0.8f, 0.7f), 0.01f);
            for (int i = 0; i < Mathf.RoundToInt(26 * density); i++)
            {
                Vector3 p = Ring(Rand(0f, 360f), Rand(10f, R + 3f));
                if (Chance(0.5))
                {
                    var g = MeshFactory.Primitive(PrimitiveType.Sphere, root, p + Vector3.up * 0.2f, Vector3.one * Rand(0.2f, 0.4f), glow);
                    g.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
                else
                {
                    float s = Rand(0.4f, 0.9f);
                    MeshFactory.Primitive(PrimitiveType.Cylinder, root, p + Vector3.up * 0.25f * s, new Vector3(0.12f, 0.25f, 0.12f) * s * 2f, stem);
                    MeshFactory.Primitive(PrimitiveType.Sphere, root, p + Vector3.up * 0.55f * s, new Vector3(0.7f, 0.35f, 0.7f) * s, shroom);
                }
            }
            EnvFx.Weather(dyn, Vector3.zero, "motes");
            EnvFx.Weather(dyn, Vector3.zero, "leaves");
            // Ground fog cards.
            var fogMat = MaterialFactory.Transparent(new Color(t.fog.r * 1.4f, t.fog.g * 1.4f, t.fog.b * 1.4f, 0.25f), true);
            for (int i = 0; i < 8; i++)
            {
                var f = MeshFactory.MeshObject(MeshFactory.Disc(), dyn, Ring(i * 45f, Rand(10f, 20f)) + Vector3.up * 0.4f, Vector3.one * Rand(6f, 10f), fogMat, false);
                var d = f.AddComponent<CloudDrift>();
                d.Speed = Rand(0.2f, 0.5f);
                d.WrapX = 20f;
            }
        }

        static void Mountain(ArenaTheme t, Transform root, Transform dyn)
        {
            var rock = MaterialFactory.Toon(new Color(0.45f, 0.47f, 0.52f));
            var snowCap = MaterialFactory.Toon(new Color(0.95f, 0.97f, 1f));
            // Cliff walls around the pass.
            for (int i = 0; i < 16; i++)
            {
                if (i == 0 || i == 8) continue; // the road in and out
                var cliff = MeshFactory.Primitive(PrimitiveType.Cube, root, Ring(i * 22.5f, R + 6f) + Vector3.up * 4f,
                    new Vector3(Rand(6f, 9f), Rand(8f, 14f), Rand(4f, 6f)), rock);
                cliff.transform.localRotation = Quaternion.Euler(Rand(-8f, 8f), i * 22.5f + Rand(-10f, 10f), Rand(-6f, 6f));
                MeshFactory.Primitive(PrimitiveType.Cube, cliff.transform, new Vector3(0f, 0.52f, 0f), new Vector3(1.05f, 0.08f, 1.05f), snowCap);
            }
            for (int i = 0; i < Mathf.RoundToInt(24 * density); i++)
                Pine(root, Ring(Rand(0f, 360f), Rand(26f, 38f)), Rand(4f, 7f), new Color(0.12f, 0.28f, 0.22f));
            var peaks = MaterialFactory.Toon(new Color(0.8f, 0.85f, 0.95f), 0f);
            for (int i = 0; i < 8; i++)
                MeshFactory.MeshObject(MeshFactory.Cone(), root, Ring(i * 45f + 20f, Rand(60f, 75f)), new Vector3(Rand(35f, 50f), Rand(30f, 45f), Rand(35f, 50f)), peaks, false);
            // Ice crystals.
            var ice = MaterialFactory.Toon(new Color(0.7f, 0.9f, 1f), 0.01f, new Color(0.2f, 0.4f, 0.5f));
            for (int i = 0; i < 10; i++)
            {
                var c = MeshFactory.MeshObject(MeshFactory.Cone(), root, Ring(Rand(0f, 360f), Rand(R + 0.5f, R + 3f)), new Vector3(0.8f, Rand(1.5f, 3f), 0.8f), ice);
                c.transform.localRotation = Quaternion.Euler(Rand(-15f, 15f), 0f, Rand(-15f, 15f));
            }
            EnvFx.Weather(dyn, Vector3.zero, "snow", 50f);
        }

        static void Kingdom(ArenaTheme t, Transform root, Transform dyn, bool fallen)
        {
            var wallC = fallen ? new Color(0.45f, 0.38f, 0.32f) : new Color(0.9f, 0.86f, 0.78f);
            var roofC = fallen ? new Color(0.2f, 0.16f, 0.15f) : new Color(0.25f, 0.35f, 0.6f);
            // Plaza tiles.
            var tile = MaterialFactory.Toon(Color.Lerp(t.ground, Color.white, 0.12f), 0f);
            for (int i = 0; i < 24; i++)
            {
                var slab = MeshFactory.Primitive(PrimitiveType.Cube, root, Ring(i * 15f, 8f) + Vector3.up * 0.02f, new Vector3(2.8f, 0.04f, 1.6f), tile);
                slab.transform.localRotation = Quaternion.Euler(0f, i * 15f, 0f);
                slab.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Tall townhouses.
            int houses = Mathf.RoundToInt(14 * density) + 4;
            for (int i = 0; i < houses; i++)
            {
                float a = (i + 0.5f) / houses * 360f;
                House(root, Ring(a, Rand(21f, 24f)), wallC, roofC, t.lantern, fallen, Rand(1.2f, 1.6f));
            }
            // Castle towers on the horizon.
            for (int i = 0; i < 5; i++)
            {
                Vector3 p = Ring(-40f + i * 20f, 50f);
                float h = Rand(18f, 28f);
                MeshFactory.Primitive(PrimitiveType.Cylinder, root, p + Vector3.up * h * 0.5f, new Vector3(5f, h * 0.5f, 5f), MaterialFactory.Toon(wallC));
                MeshFactory.MeshObject(MeshFactory.Cone(), root, p + Vector3.up * h, new Vector3(7f, 7f, 7f), MaterialFactory.Toon(roofC));
                if (fallen) EnvFx.Smoke(root, p + Vector3.up * (h + 2f), 3f);
            }
            if (!fallen)
            {
                // Market stalls with coloured awnings, banners, and townsfolk.
                Color[] awn = { new Color(0.85f, 0.25f, 0.2f), new Color(0.2f, 0.55f, 0.85f), new Color(0.95f, 0.75f, 0.2f), new Color(0.35f, 0.7f, 0.35f) };
                var wood = MaterialFactory.Toon(new Color(0.4f, 0.28f, 0.18f));
                for (int i = 0; i < 6; i++)
                {
                    var s = new GameObject("Stall").transform;
                    s.SetParent(root, false);
                    s.position = Ring(i * 60f + 30f, R + 2.5f);
                    s.rotation = Quaternion.LookRotation(-s.position.normalized);
                    MeshFactory.Primitive(PrimitiveType.Cube, s, new Vector3(0f, 0.5f, 0f), new Vector3(2.4f, 1f, 1.2f), wood);
                    var aw = MeshFactory.Primitive(PrimitiveType.Cube, s, new Vector3(0f, 2.1f, -0.2f), new Vector3(2.8f, 0.1f, 1.8f), MaterialFactory.Toon(awn[i % 4]));
                    aw.transform.localRotation = Quaternion.Euler(-12f, 0f, 0f);
                    MeshFactory.Primitive(PrimitiveType.Cylinder, s, new Vector3(-1.3f, 1f, 0.5f), new Vector3(0.08f, 1f, 0.08f), wood);
                    MeshFactory.Primitive(PrimitiveType.Cylinder, s, new Vector3(1.3f, 1f, 0.5f), new Vector3(0.08f, 1f, 0.08f), wood);
                }
                for (int i = 0; i < 6; i++)
                {
                    var pole = new GameObject("Banner").transform;
                    pole.SetParent(dyn, false);
                    pole.position = Ring(i * 60f, R + 1f);
                    MeshFactory.Primitive(PrimitiveType.Cylinder, pole, new Vector3(0f, 2.5f, 0f), new Vector3(0.1f, 2.5f, 0.1f), MaterialFactory.Toon(new Color(0.8f, 0.7f, 0.3f)));
                    var cloth = new GameObject("Cloth").transform;
                    cloth.SetParent(pole, false);
                    cloth.localPosition = new Vector3(0f, 4.5f, 0f);
                    MeshFactory.Primitive(PrimitiveType.Cube, cloth, new Vector3(0.5f, -0.9f, 0f), new Vector3(0.9f, 1.8f, 0.05f), MaterialFactory.Toon(new Color(0.2f, 0.3f, 0.7f)));
                    var sw = cloth.gameObject.AddComponent<Sway>();
                    sw.Amount = 8f;
                    sw.Speed = 1.6f;
                }
                string[] chatter =
                {
                    "Fresh bread! Fresh bread!", "Did you hear? The arena champion lost!", "They say the Chancellor never sleeps...",
                    "Demons? Inside the walls? Nonsense.", "My son joined the royal guard today.", "Crystals for summoning! Best prices!"
                };
                int npcs = Mathf.RoundToInt(8 * density);
                for (int i = 0; i < npcs; i++)
                    NpcWalker.Spawn(dyn, i % 3 == 0 ? "npc_soldier" : (i % 2 == 0 ? "npc_villager" : "npc_villager2"), Vector3.zero, R + 1.5f, R + 4f, chatter);
                BirdFlock.Create(dyn, Vector3.zero, 6, new Color(0.95f, 0.95f, 0.95f));
                EnvFx.Weather(dyn, Vector3.zero, "leaves");
            }
            else
            {
                // Rubble, broken walls, fires and falling ash.
                var rubble = MaterialFactory.Toon(new Color(0.4f, 0.36f, 0.32f));
                for (int i = 0; i < Mathf.RoundToInt(24 * density); i++)
                {
                    var r = MeshFactory.Primitive(PrimitiveType.Cube, root, Ring(Rand(0f, 360f), Rand(R + 0.5f, R + 6f)) + Vector3.up * 0.3f,
                        new Vector3(Rand(0.5f, 1.8f), Rand(0.4f, 1.2f), Rand(0.5f, 1.8f)), rubble);
                    r.transform.localRotation = Quaternion.Euler(Rand(-20f, 20f), Rand(0f, 360f), Rand(-20f, 20f));
                }
                for (int i = 0; i < 6; i++) EnvFx.Fire(dyn, Ring(i * 60f + 15f, R + 2f), 1.3f, i % 2 == 0);
                EnvFx.Weather(dyn, Vector3.zero, "embers");
                EnvFx.Weather(dyn, Vector3.zero, "ash");
            }
        }

        static void Temple(ArenaTheme t, Transform root, Transform dyn)
        {
            var stone = MaterialFactory.Toon(new Color(0.55f, 0.55f, 0.5f));
            var moss = MaterialFactory.Toon(new Color(0.3f, 0.45f, 0.3f));
            var rune = MaterialFactory.Toon(t.lantern, 0f, t.lantern);
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = Ring(i * 30f, R + 1.5f);
                float h = Chance(0.35) ? Rand(1.5f, 3f) : Rand(6f, 9f);
                var pillar = MeshFactory.Primitive(PrimitiveType.Cylinder, root, p + Vector3.up * h * 0.5f, new Vector3(1.1f, h * 0.5f, 1.1f), stone);
                pillar.transform.localRotation = Quaternion.Euler(Rand(-4f, 4f), 0f, Rand(-4f, 4f));
                MeshFactory.Primitive(PrimitiveType.Cube, root, p + Vector3.up * 0.2f, new Vector3(1.8f, 0.4f, 1.8f), moss);
                if (h > 5f) MeshFactory.Primitive(PrimitiveType.Cube, root, p + Vector3.up * (h + 0.2f), new Vector3(1.7f, 0.4f, 1.7f), stone);
            }
            // Guardian statues facing the arena.
            for (int i = 0; i < 4; i++)
            {
                var st = new GameObject("Statue").transform;
                st.SetParent(root, false);
                st.position = Ring(i * 90f + 45f, R + 6f);
                st.rotation = Quaternion.LookRotation(-st.position.normalized);
                MeshFactory.Primitive(PrimitiveType.Cube, st, new Vector3(0f, 1f, 0f), new Vector3(3f, 2f, 3f), stone);
                MeshFactory.Primitive(PrimitiveType.Capsule, st, new Vector3(0f, 4.2f, 0f), new Vector3(2f, 2.4f, 1.6f), stone);
                MeshFactory.Primitive(PrimitiveType.Sphere, st, new Vector3(0f, 7f, 0f), Vector3.one * 1.6f, stone);
                var eyes = MeshFactory.Primitive(PrimitiveType.Cube, st, new Vector3(0f, 7.1f, 0.75f), new Vector3(0.9f, 0.12f, 0.1f), rune);
                eyes.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var sword = MeshFactory.Primitive(PrimitiveType.Cube, st, new Vector3(1.4f, 4f, 0.6f), new Vector3(0.25f, 5f, 0.1f), stone);
                sword.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            }
            // Glowing rune circles on the floor.
            var runeGlow = MaterialFactory.Additive(new Color(t.lantern.r, t.lantern.g, t.lantern.b, 0.55f));
            for (int i = 0; i < 3; i++)
            {
                var ringGo = MeshFactory.MeshObject(MeshFactory.Ring(0.93f), dyn, Vector3.up * (0.03f + i * 0.005f), Vector3.one * (5f + i * 4.5f), runeGlow, false);
                var sp = ringGo.AddComponent<Spinner>();
                sp.DegreesPerSecond = new Vector3(0f, i % 2 == 0 ? 8f : -6f, 0f);
            }
            // Floating crystals.
            var crystal = MaterialFactory.Toon(t.lantern, 0.01f, t.lantern * 0.8f);
            for (int i = 0; i < 6; i++)
            {
                var c = MeshFactory.Primitive(PrimitiveType.Cube, dyn, Ring(i * 60f + 30f, R + 3f) + Vector3.up * Rand(3f, 5f), new Vector3(0.6f, 1.2f, 0.6f), crystal);
                c.transform.localRotation = Quaternion.Euler(45f, 0f, 45f);
                var sp = c.AddComponent<Spinner>();
                sp.DegreesPerSecond = new Vector3(0f, 50f, 0f);
                sp.BobHeight = 0.4f;
            }
            Lanterns(root, t, 8);
            EnvFx.Weather(dyn, Vector3.zero, "motes");
        }

        static void DemonLand(ArenaTheme t, Transform root, Transform dyn)
        {
            var spike = MaterialFactory.Toon(new Color(0.12f, 0.06f, 0.06f));
            var lava = MaterialFactory.Toon(new Color(1f, 0.4f, 0.05f), 0f, new Color(1f, 0.35f, 0.05f));
            for (int i = 0; i < Mathf.RoundToInt(30 * density); i++)
            {
                var s = MeshFactory.MeshObject(MeshFactory.Cone(), root, Ring(Rand(0f, 360f), Rand(R + 1f, R + 12f)), new Vector3(Rand(1f, 2.5f), Rand(3f, 9f), Rand(1f, 2.5f)), spike);
                s.transform.localRotation = Quaternion.Euler(Rand(-20f, 20f), 0f, Rand(-20f, 20f));
            }
            // Lava cracks across the ground.
            for (int i = 0; i < 14; i++)
            {
                var c = MeshFactory.Primitive(PrimitiveType.Cube, root, Ring(Rand(0f, 360f), Rand(4f, R + 4f)) + Vector3.up * 0.02f,
                    new Vector3(Rand(0.15f, 0.35f), 0.03f, Rand(2f, 6f)), lava);
                c.transform.localRotation = Quaternion.Euler(0f, Rand(0f, 360f), 0f);
                c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            // Lava pools and giant ribs.
            for (int i = 0; i < 4; i++)
            {
                MeshFactory.MeshObject(MeshFactory.Disc(), root, Ring(i * 90f + 45f, R + 7f) + Vector3.up * 0.03f, Vector3.one * Rand(4f, 7f), lava, false);
                EnvFx.Fire(dyn, Ring(i * 90f + 45f, R + 7f), 1.6f, i % 2 == 0);
            }
            var bone = MaterialFactory.Toon(new Color(0.8f, 0.75f, 0.65f));
            for (int i = 0; i < 6; i++)
            {
                var rib = MeshFactory.Primitive(PrimitiveType.Capsule, root, Ring(200f + i * 6f, 30f) + Vector3.up * 5f, new Vector3(1f, 7f, 1f), bone);
                rib.transform.localRotation = Quaternion.Euler(0f, 200f + i * 6f, 35f);
            }
            EnvFx.Weather(dyn, Vector3.zero, "embers");
            EnvFx.Weather(dyn, Vector3.zero, "ash");
        }

        static void Castle(ArenaTheme t, Transform root, Transform dyn)
        {
            var dark = MaterialFactory.Toon(new Color(0.16f, 0.14f, 0.2f));
            var trim = MaterialFactory.Toon(new Color(0.4f, 0.1f, 0.2f));
            var energy = MaterialFactory.Toon(t.lantern, 0f, t.lantern);
            // Gothic pillars with arches.
            for (int i = 0; i < 12; i++)
            {
                Vector3 p = Ring(i * 30f, R + 2f);
                MeshFactory.Primitive(PrimitiveType.Cube, root, p + Vector3.up * 7f, new Vector3(1.6f, 14f, 1.6f), dark);
                MeshFactory.MeshObject(MeshFactory.Cone(), root, p + Vector3.up * 14f, new Vector3(2.2f, 4f, 2.2f), trim);
                if (i % 2 == 0 && GameSettings.MaxDynamicLights > 1) EnvFx.Fire(dyn, p + Vector3.up * 3f + p.normalized * -1f, 0.6f, i % 4 == 0);
            }
            // Outer walls.
            for (int i = 0; i < 16; i++)
            {
                var w = MeshFactory.Primitive(PrimitiveType.Cube, root, Ring(i * 22.5f, R + 9f) + Vector3.up * 8f, new Vector3(9f, 16f, 2f), dark);
                w.transform.localRotation = Quaternion.Euler(0f, i * 22.5f, 0f);
            }
            // The throne and red banners.
            var throne = new GameObject("Throne").transform;
            throne.SetParent(root, false);
            throne.position = Ring(0f, R + 4f);
            throne.rotation = Quaternion.LookRotation(-throne.position.normalized);
            MeshFactory.Primitive(PrimitiveType.Cube, throne, new Vector3(0f, 1f, 0f), new Vector3(6f, 2f, 3f), dark);
            MeshFactory.Primitive(PrimitiveType.Cube, throne, new Vector3(0f, 5f, 1f), new Vector3(3f, 8f, 0.6f), trim);
            MeshFactory.Primitive(PrimitiveType.Sphere, throne, new Vector3(0f, 9.5f, 1f), Vector3.one * 1.4f, energy);
            var banner = MaterialFactory.Toon(new Color(0.55f, 0.05f, 0.1f));
            for (int i = 0; i < 6; i++)
            {
                var b = MeshFactory.Primitive(PrimitiveType.Cube, root, Ring(i * 60f + 15f, R + 8f) + Vector3.up * 9f, new Vector3(2f, 7f, 0.1f), banner);
                b.transform.localRotation = Quaternion.Euler(0f, i * 60f + 15f, 0f);
            }
            // Floating dark-energy orbs.
            for (int i = 0; i < 6; i++)
            {
                var o = MeshFactory.Primitive(PrimitiveType.Sphere, dyn, Ring(i * 60f, R + 4f) + Vector3.up * 6f, Vector3.one * 0.8f, energy);
                var sp = o.AddComponent<Spinner>();
                sp.BobHeight = 0.8f;
                sp.BobSpeed = 0.8f;
            }
            EnvFx.Weather(dyn, Vector3.zero, "embers");
        }
    }
}
