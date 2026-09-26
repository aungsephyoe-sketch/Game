using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Set dressing so arenas feel like places: a ring of houses, a torii gate, stone lanterns with flickering
    /// light, grass and flowers, rocks, fireflies, ground paths and breakable crates/barrels.
    /// Density and light count follow the graphics tier.
    /// </summary>
    public static class ArenaDecor
    {
        public static void Build(ArenaTheme theme, Transform root, int seed)
        {
            var rng = new System.Random(seed);
            float density = GameSettings.SceneryDensity;

            BuildPath(root, theme);
            BuildHouses(root, theme, rng, density);
            BuildTorii(root, new Vector3(0f, 0f, BattleController.ArenaRadius + 4.5f), 0f);
            BuildStoneLanterns(root, theme);
            BuildGrass(root, theme, rng, density);
            BuildRocks(root, rng, density);
            BuildBreakables(root.parent != null ? root.parent : root, rng);
            BuildFireflies(root, theme);
        }

        static float Rand(System.Random r, float a, float b) { return a + (float)r.NextDouble() * (b - a); }

        static void BuildPath(Transform root, ArenaTheme theme)
        {
            var stone = MaterialFactory.Toon(Color.Lerp(theme.ground, new Color(0.45f, 0.43f, 0.42f), 0.55f), 0f);
            for (int i = -7; i <= 7; i++)
            {
                var slab = MeshFactory.Primitive(PrimitiveType.Cube, root, new Vector3((i % 2) * 0.25f, 0.02f, i * 2.3f),
                    new Vector3(1.9f, 0.04f, 1.6f), stone);
                slab.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                slab.transform.localRotation = Quaternion.Euler(0f, (i * 37) % 11 - 5f, 0f);
            }
        }

        static void BuildHouses(Transform root, ArenaTheme theme, System.Random rng, float density)
        {
            var wall = MaterialFactory.Toon(new Color(0.78f, 0.72f, 0.62f));
            var timber = MaterialFactory.Toon(new Color(0.25f, 0.15f, 0.1f));
            var roof = MaterialFactory.Toon(new Color(0.18f, 0.2f, 0.26f));
            var window = MaterialFactory.Toon(theme.lantern, 0f, theme.lantern * 0.9f);
            int count = Mathf.RoundToInt(14 * density);
            for (int i = 0; i < count; i++)
            {
                float a = (i + 0.5f) / count * 360f + Rand(rng, -6f, 6f);
                if (Mathf.Abs(Mathf.DeltaAngle(a, 0f)) < 16f) continue; // keep the torii gate (at +Z) clear
                float r = Rand(rng, 22f, 26f);
                var pos = Quaternion.Euler(0f, a, 0f) * Vector3.forward * r;
                var house = new GameObject("House").transform;
                house.SetParent(root, false);
                house.position = pos;
                house.rotation = Quaternion.LookRotation(-pos.normalized);
                float w = Rand(rng, 4f, 6f), h = Rand(rng, 2.6f, 3.4f), d = Rand(rng, 3.5f, 4.5f);
                MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), wall);
                // Timber frame.
                MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h, 0f), new Vector3(w + 0.1f, 0.18f, d + 0.1f), timber);
                MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(-w * 0.5f, h * 0.5f, -d * 0.5f), new Vector3(0.2f, h, 0.2f), timber);
                MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(w * 0.5f, h * 0.5f, -d * 0.5f), new Vector3(0.2f, h, 0.2f), timber);
                // Gabled roof from two tilted slabs.
                var r1 = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h + 0.75f, -d * 0.26f), new Vector3(w + 1f, 0.2f, d * 0.62f), roof);
                r1.transform.localRotation = Quaternion.Euler(-28f, 0f, 0f);
                var r2 = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(0f, h + 0.75f, d * 0.26f), new Vector3(w + 1f, 0.2f, d * 0.62f), roof);
                r2.transform.localRotation = Quaternion.Euler(28f, 0f, 0f);
                // Warm windows facing the arena.
                var win = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(-w * 0.22f, h * 0.55f, -d * 0.5f - 0.02f), new Vector3(w * 0.25f, h * 0.3f, 0.05f), window);
                win.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                if (rng.NextDouble() < 0.6)
                {
                    var door = MeshFactory.Primitive(PrimitiveType.Cube, house, new Vector3(w * 0.2f, h * 0.35f, -d * 0.5f - 0.02f), new Vector3(w * 0.22f, h * 0.7f, 0.05f), timber);
                    door.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }
            }
        }

        static void BuildTorii(Transform root, Vector3 pos, float yaw)
        {
            var red = MaterialFactory.Toon(new Color(0.8f, 0.12f, 0.08f));
            var black = MaterialFactory.Toon(new Color(0.08f, 0.07f, 0.07f));
            var gate = new GameObject("Torii").transform;
            gate.SetParent(root, false);
            gate.position = pos;
            gate.rotation = Quaternion.Euler(0f, yaw, 0f);
            MeshFactory.Primitive(PrimitiveType.Cylinder, gate, new Vector3(-2.2f, 2.4f, 0f), new Vector3(0.45f, 2.4f, 0.45f), red);
            MeshFactory.Primitive(PrimitiveType.Cylinder, gate, new Vector3(2.2f, 2.4f, 0f), new Vector3(0.45f, 2.4f, 0.45f), red);
            MeshFactory.Primitive(PrimitiveType.Cube, gate, new Vector3(0f, 3.9f, 0f), new Vector3(5.6f, 0.3f, 0.35f), red);
            MeshFactory.Primitive(PrimitiveType.Cube, gate, new Vector3(0f, 4.85f, 0f), new Vector3(6.6f, 0.35f, 0.5f), black);
            MeshFactory.Primitive(PrimitiveType.Cube, gate, new Vector3(0f, 4.55f, 0f), new Vector3(6.0f, 0.28f, 0.42f), red);
        }

        static void BuildStoneLanterns(Transform root, ArenaTheme theme)
        {
            var stone = MaterialFactory.Toon(new Color(0.5f, 0.5f, 0.52f));
            var glow = MaterialFactory.Toon(theme.lantern, 0f, theme.lantern);
            int lights = Mathf.Min(6, GameSettings.MaxDynamicLights);
            const int count = 8;
            for (int i = 0; i < count; i++)
            {
                float a = i * (360f / count) + 22.5f;
                var pos = Quaternion.Euler(0f, a, 0f) * Vector3.forward * (BattleController.ArenaRadius + 0.9f);
                var l = new GameObject("StoneLantern").transform;
                l.SetParent(root, false);
                l.position = pos;
                MeshFactory.Primitive(PrimitiveType.Cylinder, l, new Vector3(0f, 0.5f, 0f), new Vector3(0.3f, 0.5f, 0.3f), stone);
                MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(0f, 1.15f, 0f), new Vector3(0.6f, 0.4f, 0.6f), glow);
                var cap = MeshFactory.Primitive(PrimitiveType.Cube, l, new Vector3(0f, 1.5f, 0f), new Vector3(0.95f, 0.18f, 0.95f), stone);
                cap.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                if (i % 2 == 0 && lights > 0)
                {
                    lights--;
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

        static void BuildGrass(Transform root, ArenaTheme theme, System.Random rng, float density)
        {
            var grass = MaterialFactory.Toon(Color.Lerp(new Color(0.2f, 0.35f, 0.18f), theme.ground, 0.35f), 0f);
            var flower = MaterialFactory.Toon(theme.foliage, 0f, theme.foliage * 0.25f);
            int count = Mathf.RoundToInt(70 * density);
            for (int i = 0; i < count; i++)
            {
                float a = Rand(rng, 0f, Mathf.PI * 2f);
                float r = Mathf.Sqrt(Rand(rng, 0.05f, 1f)) * (BattleController.ArenaRadius + 3f);
                var pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                if (Mathf.Abs(pos.x) < 1.3f) continue; // keep the stone path clean
                var tuft = MeshFactory.Primitive(PrimitiveType.Capsule, root, pos + Vector3.up * 0.1f,
                    new Vector3(Rand(rng, 0.25f, 0.45f), Rand(rng, 0.12f, 0.22f), Rand(rng, 0.25f, 0.45f)), rng.NextDouble() < 0.15 ? flower : grass);
                tuft.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
        }

        static void BuildRocks(Transform root, System.Random rng, float density)
        {
            var rock = MaterialFactory.Toon(new Color(0.32f, 0.31f, 0.34f));
            int count = Mathf.RoundToInt(18 * density);
            for (int i = 0; i < count; i++)
            {
                float a = Rand(rng, 0f, 360f);
                var pos = Quaternion.Euler(0f, a, 0f) * Vector3.forward * Rand(rng, BattleController.ArenaRadius + 1.5f, BattleController.ArenaRadius + 4f);
                var go = MeshFactory.Primitive(PrimitiveType.Sphere, root, pos + Vector3.up * 0.2f,
                    new Vector3(Rand(rng, 0.6f, 1.6f), Rand(rng, 0.4f, 0.9f), Rand(rng, 0.6f, 1.4f)), rock);
                go.transform.localRotation = Quaternion.Euler(0f, a, Rand(rng, -10f, 10f));
            }
        }

        static void BuildBreakables(Transform root, System.Random rng)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = i * 60f + Rand(rng, -15f, 15f);
                var pos = Quaternion.Euler(0f, a, 0f) * Vector3.forward * Rand(rng, 8f, 13f);
                Breakable.Create(root, pos, rng.NextDouble() < 0.5);
            }
        }

        static void BuildFireflies(Transform root, ArenaTheme theme)
        {
            var go = new GameObject("Fireflies");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f);
            main.startSpeed = 0.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(Color.Lerp(theme.lantern, Color.white, 0.3f), new Color(0.8f, 1f, 0.5f));
            main.maxParticles = 200;
            var em = ps.emission;
            em.rateOverTime = 12f * GameSettings.ParticleScale;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(40f, 3f, 40f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.5f;
            noise.frequency = 0.4f;
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.3f), new GradientAlphaKey(0.2f, 0.6f), new GradientAlphaKey(1f, 0.8f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
            go.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.Additive(Color.white, true);
            ps.Play();
        }
    }
}
