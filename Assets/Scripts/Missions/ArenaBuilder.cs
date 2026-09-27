using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Builds a location (battle arena, cutscene set or home backdrop): ground, sky, fog, sun and moon,
    /// then the region-specific dressing from <see cref="ArenaDecor"/>.
    /// </summary>
    public static class ArenaBuilder
    {
        public static GameObject Build(ArenaTheme theme, Transform parent, bool combatArena = true, int seed = 7)
        {
            var root = new GameObject("Arena");
            root.transform.SetParent(parent, false);
            var dyn = new GameObject("ArenaDynamic");
            dyn.transform.SetParent(parent, false);

            var ground = new GameObject("Ground");
            ground.transform.SetParent(root.transform, false);
            ground.transform.localScale = new Vector3(90f, 1f, 90f);
            ground.AddComponent<MeshFilter>().sharedMesh = MeshFactory.PlanarDisc();
            var gmr = ground.AddComponent<MeshRenderer>();
            gmr.sharedMaterial = MaterialFactory.Painted(ArtLibrary.GroundKey(theme), MaterialFactory.TextureTint(theme.ground), 90f / 5f);
            gmr.receiveShadows = true;

            if (combatArena)
            {
                // Subtle rings mark the playable area.
                // The fighting ground: a worn clearing of the region's road surface.
                var clearing = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), root.transform, Vector3.up * 0.008f, new Vector3(BattleController.ArenaRadius + 0.6f, 1f, BattleController.ArenaRadius + 0.6f),
                    MaterialFactory.Painted(ArtLibrary.RoadKey(theme), MaterialFactory.TextureTint(JourneyBuilder.RoadColor(theme), 0.35f), (BattleController.ArenaRadius + 0.6f) / 3f), false);
                clearing.GetComponent<Renderer>().receiveShadows = true;
                AddFlat(root.transform, MeshFactory.Ring(0.96f), BattleController.ArenaRadius + 0.6f, 0.01f, MaterialFactory.Toon(theme.groundAccent, 0f));
                AddFlat(root.transform, MeshFactory.Ring(0.97f), 11f, 0.012f, MaterialFactory.Toon(Color.Lerp(theme.ground, theme.groundAccent, 0.5f), 0f));
            }

            if (theme.night)
            {
                Color moonC = theme.kind == EnvironmentKind.DemonLand || theme.burning || theme.kind == EnvironmentKind.Castle
                    ? new Color(1f, 0.35f, 0.25f) : new Color(1f, 0.95f, 0.8f);
                var moon = MeshFactory.Primitive(PrimitiveType.Sphere, root.transform, new Vector3(-35f, 38f, 90f), Vector3.one * 14f, MaterialFactory.Toon(moonC, 0f, moonC));
                moon.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            else
            {
                CloudDrift.CreateLayer(dyn.transform, new Vector3(0f, 0f, 40f), 6, 26f, new Color(1f, 1f, 1f, 0.55f), 60f);
            }

            ArenaDecor.Build(theme, root.transform, dyn.transform, seed, combatArena);
            if (theme.petals) BuildPetals(dyn.transform, theme.foliage);

            ApplyLighting(theme);
            // Merge the hundreds of static set pieces into a few draw calls (big win on mobile).
            StaticBatchingUtility.Combine(root);
            return root;
        }

        /// <summary>Sky, fog, ambient and key-light colour for a location.</summary>
        public static void ApplyLighting(ArenaTheme theme)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = theme.fog;
            RenderSettings.fogStartDistance = theme.fogStart;
            RenderSettings.fogEndDistance = theme.fogEnd;
            // Softer light overall so the characters read clearly (no washed-out faces).
            RenderSettings.ambientLight = Color.Lerp(theme.sky, Color.white, theme.night ? 0.2f : 0.3f) * 0.8f;
            if (Camera.main != null) Camera.main.backgroundColor = theme.sky;
            var sun = RenderSettings.sun;
            if (sun != null)
            {
                sun.color = theme.sun;
                sun.intensity = theme.sunIntensity * 0.8f;
                sun.transform.rotation = theme.night ? Quaternion.Euler(55f, -35f, 0f) : Quaternion.Euler(50f, 30f, 0f);
            }
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
            em.rateOverTime = 25f * GameSettings.ParticleScale;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(36f, 1f, 36f);
            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.3f;
            go.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.Transparent(Color.white, true);
            ps.Play();
        }
    }
}
