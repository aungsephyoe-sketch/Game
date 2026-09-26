using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Pooled, code-built particle systems plus one-shot mesh flashes (slashes, shockwaves, bursts).
    /// All effects are procedural so there is no dependency on art assets yet.
    /// </summary>
    public static class VFX
    {
        static Transform root;
        static ParticleSystem sparks, embers, smoke;

        static void Ensure()
        {
            if (root != null) return;
            var go = new GameObject("[VFX]");
            Object.DontDestroyOnLoad(go);
            root = go.transform;
            sparks = CreateSystem("Sparks", true, 0.18f, 0.4f, 5f, 12f, 0.06f, 0.16f, 0.6f);
            embers = CreateSystem("Embers", false, 0.5f, 1.1f, 0.6f, 3f, 0.12f, 0.35f, -0.15f);
            smoke = CreateSystem("Smoke", false, 0.6f, 1.2f, 0.5f, 1.8f, 0.6f, 1.4f, -0.05f, true);
        }

        static ParticleSystem CreateSystem(string name, bool stretched, float lifeMin, float lifeMax, float speedMin, float speedMax,
            float sizeMin, float sizeMax, float gravity, bool alphaBlended = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.maxParticles = 1500;

            var emission = ps.emission;
            emission.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0f));

            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.8f, 0.5f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = alphaBlended ? MaterialFactory.Transparent(Color.white, true) : MaterialFactory.Additive(Color.white, true);
            if (stretched)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.04f;
                r.lengthScale = 1.5f;
            }
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Color color, int count, float sizeMul = 1f)
        {
            Ensure();
            var p = new ParticleSystem.EmitParams
            {
                position = pos,
                applyShapeToPosition = true,
                startColor = color
            };
            if (sizeMul != 1f)
            {
                var main = ps.main;
                p.startSize = Random.Range(main.startSize.constantMin, main.startSize.constantMax) * sizeMul;
            }
            ps.Emit(p, count);
        }

        public static void HitSpark(Vector3 pos, Color color, int count = 12)
        {
            Emit(sparks, pos, Color.Lerp(color, Color.white, 0.35f), count);
        }

        /// <summary>Breathing-style motes around a character when they use a form.</summary>
        public static void Breath(Vector3 pos, Color color, int count = 30)
        {
            Emit(embers, pos + Vector3.up, color, count);
        }

        public static void Smoke(Vector3 pos, Color color, int count = 16)
        {
            Emit(smoke, pos + Vector3.up * 0.4f, color, count);
        }

        // ------------------------------------------------------------ Mesh flashes

        /// <summary>Spawns a mesh that scales from→to and fades out over duration.</summary>
        public static FlashFx Flash(Mesh mesh, Vector3 pos, Quaternion rot, Vector3 fromScale, Vector3 toScale, Color color, float duration)
        {
            Ensure();
            var go = new GameObject("Flash");
            go.transform.SetPositionAndRotation(pos, rot);
            go.transform.localScale = fromScale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = go.AddComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            mr.material = MaterialFactory.Additive(color);
            var fx = go.AddComponent<FlashFx>();
            fx.Setup(mr.material, color, fromScale, toScale, duration);
            return fx;
        }

        /// <summary>A crescent sword arc in front of the attacker. roll tilts the slash plane.</summary>
        public static void Slash(Vector3 center, Vector3 forward, float radius, float arcDegrees, float roll, Color color, float duration = 0.18f)
        {
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            var rot = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, roll);
            Flash(MeshFactory.Sector(arcDegrees, 0.72f), center + Vector3.up, rot,
                new Vector3(radius * 0.8f, 1f, radius * 0.8f), new Vector3(radius * 1.1f, 1f, radius * 1.1f), color, duration);
        }

        public static void Shockwave(Vector3 center, float radius, Color color, float duration = 0.35f)
        {
            Flash(MeshFactory.Ring(0.85f), center + Vector3.up * 0.08f, Quaternion.identity,
                new Vector3(0.2f, 1f, 0.2f), new Vector3(radius, 1f, radius), color, duration);
        }

        public static void BurstDisc(Vector3 center, float radius, Color color, float duration = 0.4f)
        {
            Flash(MeshFactory.Disc(), center + Vector3.up * 0.06f, Quaternion.identity,
                new Vector3(radius * 0.3f, 1f, radius * 0.3f), new Vector3(radius, 1f, radius), color * 0.8f, duration);
        }

        public static void Pillar(Vector3 pos, Color color, float height = 6f, float duration = 0.5f)
        {
            Ensure();
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var c = go.GetComponent<Collider>();
            if (c != null) Object.Destroy(c);
            go.transform.position = pos + Vector3.up * height * 0.5f;
            var mr = go.GetComponent<MeshRenderer>();
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.material = MaterialFactory.Additive(color);
            var fx = go.AddComponent<FlashFx>();
            fx.Setup(mr.material, color, new Vector3(1.4f, height * 0.5f, 1.4f), new Vector3(0.05f, height * 0.5f, 0.05f), duration);
        }
    }
}
