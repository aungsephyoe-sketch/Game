using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Persistent, looping environment particles: fire, smoke, snow, embers, leaves, glowing motes.</summary>
    public static class EnvFx
    {
        static ParticleSystem Make(Transform parent, string name, Vector3 localPos, bool additive)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = additive ? MaterialFactory.Additive(Color.white, true) : MaterialFactory.Transparent(Color.white, true);
            // No particle may fill more than a sliver of the screen, even right in front of the camera.
            r.maxParticleSize = 0.035f;
            var main = ps.main;
            main.loop = true;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            return ps;
        }

        static void Fade(ParticleSystem ps)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0.8f, 0.7f), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        public static ParticleSystem Fire(Transform parent, Vector3 localPos, float size = 1f, bool light = true)
        {
            var ps = Make(parent, "Fire", localPos, true);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f * size, 2f * size);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f * size, 0.8f * size);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.55f, 0.1f), new Color(1f, 0.25f, 0.05f));
            main.maxParticles = 120;
            var em = ps.emission;
            em.rateOverTime = 28f * GameSettings.ParticleScale * size;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.35f * size;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.1f));
            Fade(ps);
            ps.Play();
            if (light && GameSettings.MaxDynamicLights > 0)
            {
                var lg = new GameObject("FireLight");
                lg.transform.SetParent(ps.transform, false);
                lg.transform.localPosition = Vector3.up * 0.8f * size;
                var l = lg.AddComponent<Light>();
                l.type = LightType.Point;
                l.color = new Color(1f, 0.55f, 0.2f);
                l.range = 6f * size;
                l.intensity = 1.4f;
                l.shadows = LightShadows.None;
                lg.AddComponent<LanternFlicker>();
            }
            return ps;
        }

        public static ParticleSystem Smoke(Transform parent, Vector3 localPos, float size = 1f)
        {
            var ps = Make(parent, "Smoke", localPos, false);
            var main = ps.main;
            main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2f);
            main.startSize = new ParticleSystem.MinMaxCurve(1.2f * size, 2.5f * size);
            main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.15f, 0.13f, 0.13f, 0.5f), new Color(0.3f, 0.28f, 0.28f, 0.4f));
            main.maxParticles = 60;
            var em = ps.emission;
            em.rateOverTime = 5f * GameSettings.ParticleScale;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f * size;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.6f));
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(0.4f, 0.9f);
            vel.y = new ParticleSystem.MinMaxCurve(0f, 0f);
            vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
            Fade(ps);
            ps.Play();
            return ps;
        }

        /// <summary>Area weather: kind = snow, embers, leaves, motes, ash.</summary>
        public static ParticleSystem Weather(Transform parent, Vector3 center, string kind, float area = 40f)
        {
            bool additive = kind == "embers" || kind == "motes";
            // Emitted low (the battle camera sits ~11 m up) so flakes never blur right in front of the lens.
            var ps = Make(parent, "Weather_" + kind, center + Vector3.up * 6f, additive);
            var main = ps.main;
            main.maxParticles = 400;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(area, 1f, area);
            var noise = ps.noise;
            noise.enabled = true;
            var em = ps.emission;
            float scale = GameSettings.ParticleScale;
            switch (kind)
            {
                case "snow":
                    main.startLifetime = 8f; main.startSpeed = 0.2f; main.gravityModifier = 0.05f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.11f); main.startColor = new Color(1f, 1f, 1f, 0.55f);
                    em.rateOverTime = 16f * scale; noise.strength = 0.4f; noise.frequency = 0.3f;
                    break;
                case "embers":
                    ps.transform.localPosition = center + Vector3.up * 0.2f;
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); main.startSpeed = 0.6f; main.gravityModifier = -0.04f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.14f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.5f, 0.1f), new Color(1f, 0.2f, 0.05f));
                    em.rateOverTime = 25f * scale; noise.strength = 0.8f; noise.frequency = 0.5f;
                    break;
                case "leaves":
                    main.startLifetime = 9f; main.startSpeed = 0.3f; main.gravityModifier = 0.03f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.1f, 0.22f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.85f, 0.45f, 0.15f), new Color(0.55f, 0.65f, 0.2f));
                    main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                    em.rateOverTime = 8f * scale; noise.strength = 0.7f; noise.frequency = 0.3f;
                    break;
                case "ash":
                    main.startLifetime = 9f; main.startSpeed = 0.2f; main.gravityModifier = 0.02f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.1f); main.startColor = new Color(0.45f, 0.42f, 0.42f, 0.45f);
                    em.rateOverTime = 12f * scale; noise.strength = 0.5f; noise.frequency = 0.3f;
                    break;
                default: // glowing motes
                    ps.transform.localPosition = center + Vector3.up * 1.5f;
                    shape.scale = new Vector3(area, 3f, area);
                    main.startLifetime = new ParticleSystem.MinMaxCurve(3f, 6f); main.startSpeed = 0.15f;
                    main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
                    main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.5f, 1f, 0.8f), new Color(0.6f, 0.9f, 1f));
                    em.rateOverTime = 14f * scale; noise.strength = 0.5f; noise.frequency = 0.4f;
                    break;
            }
            Fade(ps);
            ps.Play();
            return ps;
        }
    }
}
