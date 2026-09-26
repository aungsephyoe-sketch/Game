using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Builds a stylised night arena (ground, lantern ring, trees, moon, fog, petals) from primitives.</summary>
    public static class ArenaBuilder
    {
        public static GameObject Build(ArenaTheme theme, Transform parent)
        {
            var root = new GameObject("Arena");
            root.transform.SetParent(parent, false);

            var groundMat = MaterialFactory.Toon(theme.ground, 0f);
            var ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = new Vector3(40f, 1f, 40f);
            ground.AddComponent<MeshFilter>().sharedMesh = MeshFactory.Disc();
            var gmr = ground.AddComponent<MeshRenderer>();
            gmr.sharedMaterial = groundMat;
            gmr.receiveShadows = true;

            // Decorative rings mark the playable area.
            AddFlat(root.transform, MeshFactory.Ring(0.96f), BattleController.ArenaRadius + 0.6f, 0.01f, MaterialFactory.Toon(theme.groundAccent, 0f));
            AddFlat(root.transform, MeshFactory.Ring(0.9f), 6f, 0.012f, MaterialFactory.Toon(Color.Lerp(theme.ground, theme.groundAccent, 0.5f), 0f));
            AddFlat(root.transform, MeshFactory.Ring(0.97f), 11f, 0.012f, MaterialFactory.Toon(Color.Lerp(theme.ground, theme.groundAccent, 0.5f), 0f));

            var postMat = MaterialFactory.Toon(new Color(0.18f, 0.1f, 0.08f));
            var lanternMat = MaterialFactory.Toon(theme.lantern, 0.01f, theme.lantern);
            for (int i = 0; i < 20; i++)
            {
                float a = i * 18f * Mathf.Deg2Rad;
                var pos = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (BattleController.ArenaRadius + 2f);
                var post = MeshFactory.Primitive(PrimitiveType.Cylinder, root.transform, pos + Vector3.up * 1.2f, new Vector3(0.25f, 1.2f, 0.25f), postMat);
                MeshFactory.Primitive(PrimitiveType.Cube, post.transform, new Vector3(0f, 1.1f, 0f), new Vector3(3.2f, 0.12f, 3.2f), postMat);
                var lamp = MeshFactory.Primitive(PrimitiveType.Sphere, root.transform, pos + Vector3.up * 2.1f, new Vector3(0.6f, 0.75f, 0.6f), lanternMat);
                lamp.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }

            var trunkMat = MaterialFactory.Toon(new Color(0.16f, 0.1f, 0.1f));
            var leafMat = MaterialFactory.Toon(theme.foliage, 0.02f, theme.foliage * 0.15f);
            var rng = new System.Random(7);
            int trees = Mathf.RoundToInt(26 * GameSettings.SceneryDensity);
            for (int i = 0; i < trees; i++)
            {
                float a = (float)(rng.NextDouble() * Mathf.PI * 2f);
                float r = 28f + (float)rng.NextDouble() * 8f;
                var pos = new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                float h = 3f + (float)rng.NextDouble() * 3f;
                MeshFactory.Primitive(PrimitiveType.Cylinder, root.transform, pos + Vector3.up * h * 0.5f, new Vector3(0.5f, h * 0.5f, 0.5f), trunkMat);
                for (int k = 0; k < 3; k++)
                {
                    var off = new Vector3((float)rng.NextDouble() - 0.5f, 0f, (float)rng.NextDouble() - 0.5f) * 2f;
                    MeshFactory.Primitive(PrimitiveType.Sphere, root.transform, pos + off + Vector3.up * (h + 0.3f * k),
                        Vector3.one * (2.2f + (float)rng.NextDouble() * 1.2f), leafMat);
                }
            }

            var moon = MeshFactory.Primitive(PrimitiveType.Sphere, root.transform, new Vector3(-35f, 38f, 90f), Vector3.one * 14f,
                MaterialFactory.Toon(new Color(1f, 0.95f, 0.8f), 0f, new Color(1f, 0.95f, 0.8f)));
            moon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

            ArenaDecor.Build(theme, root.transform, theme.GetHashCode());
            if (theme.petals) BuildPetals(root.transform, theme.foliage);

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.fog;
            RenderSettings.fogStartDistance = 28f;
            RenderSettings.fogEndDistance = 75f;
            RenderSettings.ambientLight = Color.Lerp(theme.sky, Color.white, 0.25f);
            if (Camera.main != null) Camera.main.backgroundColor = theme.sky;
            // Merge the hundreds of static set pieces into a few draw calls (big win on mobile).
            StaticBatchingUtility.Combine(root);
            return root;
        }

        static void AddFlat(Transform parent, Mesh mesh, float radius, float y, Material mat)
        {
            var go = new GameObject("Ring");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, y, 0f);
            go.transform.localScale = new Vector3(radius, 1f, radius);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static void BuildPetals(Transform parent, Color color)
        {
            var go = new GameObject("Petals");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(0f, 9f, 0f);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 9f;
            main.startSpeed = 0.3f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.5f));
            main.gravityModifier = 0.02f;
            main.maxParticles = 300;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            var em = ps.emission;
            em.rateOverTime = 25f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 1f, 36f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = MaterialFactory.Transparent(Color.white, true);
            ps.Play();
        }
    }
}
