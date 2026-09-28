using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Procedural bodies for every monster form, following the Higgsfield bestiary concepts so the demon in the
    /// mission preview reads as the demon in battle. Used whenever no imported model exists for the enemy.
    /// Each builder works in a ~2 m tall local space (the model is already scaled by EnemyDefinition.scale).
    /// </summary>
    public partial class CharacterVisual
    {
        /// <summary>Hover height for floating forms (0 = grounded).</summary>
        public float Hover;

        Material mBody, mAccent, mGlow, mDark, mBone;
        Transform mRoot;

        GameObject P(PrimitiveType t, Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            var go = MeshFactory.Primitive(t, mRoot, pos, scale, m);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Add(go);
            return go;
        }

        GameObject Cone(Vector3 pos, Vector3 scale, Material m, Vector3? euler = null)
        {
            var go = MeshFactory.MeshObject(MeshFactory.Cone(), mRoot, pos, scale, m);
            if (euler.HasValue) go.transform.localRotation = Quaternion.Euler(euler.Value);
            Add(go);
            return go;
        }

        void Eyes(float y, float z, float spread, float size, Color c, bool single = false)
        {
            var m = MaterialFactory.Toon(c, 0f, c);
            // A soft halo around every eye so they burn out of the dark.
            var halo = MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.45f));
            if (single)
            {
                P(PrimitiveType.Sphere, new Vector3(0f, y, z), Vector3.one * size, m);
                P(PrimitiveType.Sphere, new Vector3(0f, y, z), Vector3.one * size * 2.4f, halo);
                return;
            }
            // Angry slant: the eyes tilt down toward the middle.
            P(PrimitiveType.Sphere, new Vector3(spread, y, z), new Vector3(size * 1.2f, size * 0.55f, size * 0.5f), m, new Vector3(0f, 0f, 18f));
            P(PrimitiveType.Sphere, new Vector3(-spread, y, z), new Vector3(size * 1.2f, size * 0.55f, size * 0.5f), m, new Vector3(0f, 0f, -18f));
            P(PrimitiveType.Sphere, new Vector3(spread, y, z), new Vector3(size, size * 0.7f, size * 0.4f) * 2.6f, halo).AddComponent<Pulse>().Speed = 7f;
            P(PrimitiveType.Sphere, new Vector3(-spread, y, z), new Vector3(size, size * 0.7f, size * 0.4f) * 2.6f, halo).AddComponent<Pulse>().Speed = 7f;
            if (!mNoTeeth)
            {
                // A jagged grin under the eyes.
                float ty = y - size * 2.2f;
                for (int i = 0; i < 5; i++)
                {
                    float x = (i - 2) * spread * 0.55f;
                    Cone(new Vector3(x, ty, z + 0.01f), new Vector3(size * 0.35f, size * (i % 2 == 0 ? 0.9f : 0.6f), size * 0.3f), mBone, new Vector3(180f, 0f, 0f));
                }
            }
        }

        bool mNoTeeth;

        /// <summary>Shared menace for every demon: a creeping dark aura and a glowing ground stain.</summary>
        void AddMenace(EnemyDefinition def)
        {
            if (def.form == "dummy") return;
            // (The old dark smoke aura drifted off in world space and smudged dark blobs all over the
            // battlefield, hiding the attacks — the glowing stain and rising motes carry the menace now.)
            var stain = MaterialFactory.Additive(new Color(def.accentColor.r, def.accentColor.g, def.accentColor.b, 0.22f));
            var disc = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), mRoot, Vector3.up * 0.04f, new Vector3(0.9f, 1f, 0.9f), stain, false);
            Add(disc);
            var pulse = disc.AddComponent<Pulse>();
            pulse.Speed = 2.2f + Random.value;
        }

        /// <summary>
        /// Extra menace on every demon (PG — spooky, not gory): horns, a ridge of back spikes, glowing cracks in the
        /// skin, a pulsing ember heart, flickering eye glow and rising motes. Bosses and elites get a crown of horns,
        /// shoulder spikes and a slowly turning sigil under their feet.
        /// </summary>
        void Scarify(EnemyDefinition def)
        {
            if (def.form == "dummy") return;
            // Measure the body in model space.
            bool any = false;
            Vector3 mn = Vector3.zero, mx = Vector3.zero;
            foreach (var r in mRoot.GetComponentsInChildren<Renderer>())
            {
                if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                Vector3 a = mRoot.InverseTransformPoint(r.bounds.min), b = mRoot.InverseTransformPoint(r.bounds.max);
                Vector3 lo = Vector3.Min(a, b), hi = Vector3.Max(a, b);
                if (!any) { mn = lo; mx = hi; any = true; } else { mn = Vector3.Min(mn, lo); mx = Vector3.Max(mx, hi); }
            }
            if (!any) return;
            float h = mx.y - mn.y, w = mx.x - mn.x;
            bool big = def.archetype == EnemyArchetype.Boss || def.archetype == EnemyArchetype.Elite;
            Color ac = def.accentColor;
            var hornMat = MaterialFactory.Toon(Color.Lerp(new Color(0.12f, 0.08f, 0.1f), ac, 0.15f), 0.02f);
            var crackMat = MaterialFactory.Toon(ac, 0f, ac * 1.2f);
            float top = mx.y, frontZ = mx.z * 0.6f, backZ = mn.z;

            // Horns sweeping back from the top of the head.
            int pairs = big ? 2 : 1;
            for (int p = 0; p < pairs; p++)
                for (int s = -1; s <= 1; s += 2)
                {
                    float x = s * w * (0.14f + p * 0.12f);
                    var horn = Cone(new Vector3(x, top - h * 0.06f - p * h * 0.05f, frontZ * 0.3f), new Vector3(h * 0.06f, h * (0.2f - p * 0.05f), h * 0.06f), hornMat, new Vector3(-35f - p * 15f, 0f, s * (-20f - p * 18f)));
                    horn.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                }

            // A ridge of spikes down the back.
            int spikes = big ? 6 : 4;
            for (int i = 0; i < spikes; i++)
            {
                float t = (i + 0.5f) / spikes;
                Cone(new Vector3(0f, mn.y + h * (0.82f - t * 0.45f), backZ + 0.02f), new Vector3(h * 0.05f, h * (0.12f - t * 0.04f), h * 0.05f), hornMat, new Vector3(-120f, 0f, 0f));
            }

            // Glowing cracks across the chest and a pulsing ember heart.
            for (int i = 0; i < 5; i++)
            {
                var c = P(PrimitiveType.Cube, new Vector3(Random.Range(-w * 0.18f, w * 0.18f), mn.y + h * Random.Range(0.4f, 0.7f), frontZ * 0.85f),
                    new Vector3(h * 0.012f, h * Random.Range(0.08f, 0.16f), h * 0.012f), crackMat, new Vector3(0f, 0f, Random.Range(-40f, 40f)));
                c.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            }
            var heart = P(PrimitiveType.Sphere, new Vector3(0f, mn.y + h * 0.55f, frontZ * 0.8f), Vector3.one * h * 0.08f, MaterialFactory.Additive(new Color(ac.r, ac.g, ac.b, 0.8f)));
            heart.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            var hp = heart.AddComponent<Pulse>();
            hp.Speed = 5f;
            hp.Amount = 0.35f;

            // Rising motes in the accent colour.
            var motes = new GameObject("Motes");
            motes.transform.SetParent(mRoot, false);
            motes.transform.localPosition = new Vector3(0f, mn.y + h * 0.3f, 0f);
            var ps = motes.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = ps.main;
            main.loop = true;
            main.startLifetime = 1.4f;
            main.startSpeed = 0.4f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.12f);
            main.startColor = new ParticleSystem.MinMaxGradient(ac, Color.Lerp(ac, Color.white, 0.4f));
            main.gravityModifier = -0.15f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;
            var em = ps.emission;
            em.rateOverTime = (big ? 14f : 6f) * GameSettings.ParticleScale;
            var sh = ps.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = w * 0.4f;
            motes.GetComponent<ParticleSystemRenderer>().material = MaterialFactory.Additive(Color.white, true);
            ps.Play();

            if (big)
            {
                // Shoulder spikes and a turning sigil on the ground.
                for (int s = -1; s <= 1; s += 2)
                    for (int k = 0; k < 2; k++)
                        Cone(new Vector3(s * w * 0.42f, mn.y + h * (0.72f - k * 0.06f), -k * 0.1f), new Vector3(h * 0.05f, h * 0.14f, h * 0.05f), hornMat, new Vector3(0f, 0f, s * -55f));
                var sigil = MeshFactory.MeshObject(MeshFactory.Ring(0.85f), mRoot, Vector3.up * 0.05f, new Vector3(1.4f, 1f, 1.4f), MaterialFactory.Additive(new Color(ac.r, ac.g, ac.b, 0.55f)), false);
                Add(sigil);
                sigil.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 25f, 0f);
                for (int i = 0; i < 6; i++)
                {
                    var rune = MeshFactory.MeshObject(MeshFactory.Sector(12f, 0.7f), sigil.transform, Vector3.zero, Vector3.one * 1.1f, MaterialFactory.Additive(new Color(ac.r, ac.g, ac.b, 0.7f)), false);
                    rune.transform.localRotation = Quaternion.Euler(0f, i * 60f, 0f);
                }
            }
        }

        /// <summary>Returns false for unknown forms so the legacy generic demon is built instead.</summary>
        bool BuildMonster(EnemyDefinition def, Transform model)
        {
            mRoot = model;
            mNoTeeth = true; // PG: spooky eyes, no fangs
            mBody = MaterialFactory.Toon(def.bodyColor);
            mAccent = MaterialFactory.Toon(def.accentColor, 0.02f);
            mGlow = MaterialFactory.Toon(def.accentColor, 0f, def.accentColor);
            mDark = MaterialFactory.Toon(new Color(0.05f, 0.04f, 0.06f), 0.01f);
            mBone = MaterialFactory.Toon(new Color(0.85f, 0.82f, 0.72f));
            switch (def.form)
            {
                case "ghoul": Ghoul(def); break;
                case "stalker": Stalker(def); break;
                case "shadow": Shadow(def); break;
                case "beast": Beast(def); break;
                case "bone": BoneWarrior(def); break;
                case "forest": ForestDemon(def); break;
                case "lion": FlameBeast(def); break;
                case "void": VoidDemon(def, false); break;
                case "wraith": VoidDemon(def, true); break;
                case "knight": Knight(def); break;
                case "hunter": Hunter(def); break;
                case "imp": Imp(def); break;
                case "oni": Oni(def); break;
                case "sentinel": Sentinel(def); break;
                case "dummy": Dummy(); break;
                case "brute": Brute(def); break;
                case "general": General(def); break;
                case "guardian": Guardian(def); break;
                case "lord": Lord(def); break;
                default: return false;
            }
            Scarify(def);
            AddMenace(def);
            if (Trail != null && def.archetype != EnemyArchetype.Boss && def.archetype != EnemyArchetype.Elite) Trail.widthMultiplier = 0.25f;
            return true;
        }

        // ------------------------------------------------------------------ Common demons

        void Ghoul(EnemyDefinition d)
        {
            // Hunched scavenger with a crude blade.
            P(PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0.1f), new Vector3(0.75f, 0.7f, 0.6f), mBody, new Vector3(28f, 0f, 0f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.45f, 0.45f), new Vector3(0.45f, 0.4f, 0.5f), mBody);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.36f, 0.66f), new Vector3(0.3f, 0.12f, 0.12f), mDark); // jaw
            Eyes(1.52f, 0.66f, 0.1f, 0.09f, new Color(1f, 0.85f, 0.2f));
            for (int i = 0; i < 3; i++) Cone(new Vector3(0f, 1.25f - i * 0.22f, -0.25f - i * 0.05f), new Vector3(0.12f, 0.25f, 0.12f), mAccent, new Vector3(-60f, 0f, 0f));
            P(PrimitiveType.Capsule, new Vector3(0.45f, 0.95f, 0.25f), new Vector3(0.18f, 0.5f, 0.18f), mBody, new Vector3(20f, 0f, 15f));
            P(PrimitiveType.Capsule, new Vector3(-0.45f, 0.95f, 0.25f), new Vector3(0.18f, 0.5f, 0.18f), mBody, new Vector3(20f, 0f, -15f));
            P(PrimitiveType.Capsule, new Vector3(0.22f, 0.35f, 0f), new Vector3(0.22f, 0.38f, 0.22f), mBody);
            P(PrimitiveType.Capsule, new Vector3(-0.22f, 0.35f, 0f), new Vector3(0.22f, 0.38f, 0.22f), mBody);
            BuildSword(new Color(0.55f, 0.5f, 0.45f), 0.8f, new Vector3(0.5f, 0.6f, 0.4f));
        }

        void Stalker(EnemyDefinition d)
        {
            // Lanky, long-legged sprinter with a blade-like head crest.
            P(PrimitiveType.Capsule, new Vector3(0f, 1.3f, 0f), new Vector3(0.4f, 0.55f, 0.35f), mBody, new Vector3(15f, 0f, 0f));
            Cone(new Vector3(0f, 1.9f, 0.1f), new Vector3(0.3f, 0.7f, 0.3f), mBody, new Vector3(70f, 0f, 0f));
            Eyes(1.88f, 0.35f, 0.08f, 0.07f, d.accentColor);
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, new Vector3(0.18f * s, 0.75f, 0.12f), new Vector3(0.14f, 0.45f, 0.14f), mBody, new Vector3(-20f, 0f, 0f));
                P(PrimitiveType.Capsule, new Vector3(0.18f * s, 0.3f, -0.08f), new Vector3(0.1f, 0.35f, 0.1f), mAccent, new Vector3(25f, 0f, 0f));
                P(PrimitiveType.Capsule, new Vector3(0.35f * s, 1.25f, 0.2f), new Vector3(0.1f, 0.5f, 0.1f), mBody, new Vector3(40f, 0f, 20f * s));
            }
            BuildSword(new Color(1f, 0.9f, 0.5f), 0.6f, new Vector3(0.4f, 1f, 0.4f));
        }

        void Shadow(EnemyDefinition d)
        {
            // Gaunt smoke-bodied hunter, porcelain mask, very long blade claws.
            var smoke = MaterialFactory.Toon(new Color(0.05f, 0.04f, 0.08f), 0.02f);
            P(PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0.05f), new Vector3(0.5f, 0.65f, 0.4f), smoke, new Vector3(25f, 0f, 0f));
            var mask = MaterialFactory.Toon(new Color(0.92f, 0.9f, 0.88f), 0.02f);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.62f, 0.35f), new Vector3(0.36f, 0.42f, 0.3f), mask);
            P(PrimitiveType.Cube, new Vector3(0.06f, 1.7f, 0.5f), new Vector3(0.015f, 0.2f, 0.02f), mDark, new Vector3(0f, 0f, 20f)); // crack
            Eyes(1.65f, 0.49f, 0.08f, 0.08f, new Color(0.75f, 0.35f, 1f));
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, new Vector3(0.38f * s, 1.05f, 0.25f), new Vector3(0.12f, 0.6f, 0.12f), smoke, new Vector3(35f, 0f, 12f * s));
                // Blade claws hang past the knees.
                for (int c = 0; c < 3; c++)
                    Cone(new Vector3(0.45f * s + c * 0.04f * s, 0.45f, 0.45f + c * 0.04f), new Vector3(0.05f, 0.55f, 0.05f), mBone, new Vector3(160f, 0f, 0f));
                P(PrimitiveType.Capsule, new Vector3(0.2f * s, 0.4f, -0.05f), new Vector3(0.14f, 0.42f, 0.14f), smoke, new Vector3(-15f, 0f, 0f));
            }
            var wisps = EnvFx.Smoke(mRoot, new Vector3(0f, 1f, 0f), 0.35f);
            if (wisps != null) { var m = wisps.main; m.startColor = new Color(0.25f, 0.1f, 0.4f, 0.5f); }
            BuildSword(new Color(0.75f, 0.4f, 1f), 1f, new Vector3(0.45f, 0.9f, 0.3f));
        }

        void Beast(EnemyDefinition d)
        {
            // Bull-sized quadruped charger: ramming skull, spined back, glowing veins.
            P(PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(1.1f, 1.1f, 0.95f), mBody, new Vector3(90f, 0f, 0f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.25f, 0.55f), new Vector3(1.2f, 1f, 0.9f), mBody); // shoulders
            P(PrimitiveType.Sphere, new Vector3(0f, 0.95f, 1.25f), new Vector3(0.75f, 0.6f, 0.75f), mBone); // skull
            for (int s = -1; s <= 1; s += 2)
            {
                Cone(new Vector3(0.3f * s, 1.1f, 1.45f), new Vector3(0.18f, 0.7f, 0.18f), mBone, new Vector3(80f, 0f, -25f * s));
                P(PrimitiveType.Capsule, new Vector3(0.45f * s, 0.4f, 0.65f), new Vector3(0.3f, 0.45f, 0.3f), mBody);
                P(PrimitiveType.Capsule, new Vector3(0.4f * s, 0.4f, -0.65f), new Vector3(0.26f, 0.42f, 0.26f), mBody);
                P(PrimitiveType.Cube, new Vector3(0.52f * s, 1.05f, 0.1f), new Vector3(0.03f, 0.08f, 1.1f), mGlow, new Vector3(0f, 0f, 10f * s)); // veins
            }
            Eyes(1.0f, 1.58f, 0.18f, 0.07f, new Color(1f, 0.9f, 0.2f));
            for (int i = 0; i < 5; i++) Cone(new Vector3(0f, 1.55f - i * 0.05f, 0.6f - i * 0.35f), new Vector3(0.14f, 0.4f, 0.14f), mAccent, new Vector3(-20f, 0f, 0f));
            BuildSword(new Color(0.9f, 0.85f, 0.75f), 0.5f, new Vector3(0f, 1f, 1.5f));
        }

        void BoneWarrior(EnemyDefinition d)
        {
            // Armoured skeleton with a huge cleaver, green spirit fire in the eyes.
            var rust = MaterialFactory.Toon(new Color(0.35f, 0.22f, 0.15f), 0.02f);
            var spirit = new Color(0.4f, 1f, 0.5f);
            for (int i = 0; i < 4; i++) P(PrimitiveType.Cylinder, new Vector3(0f, 1.05f + i * 0.12f, 0.02f), new Vector3(0.6f - i * 0.05f, 0.02f, 0.4f), mBone); // ribs
            P(PrimitiveType.Cylinder, new Vector3(0f, 1.1f, -0.12f), new Vector3(0.08f, 0.35f, 0.08f), mBone); // spine
            P(PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0.05f), new Vector3(0.38f, 0.42f, 0.4f), mBone);
            P(PrimitiveType.Cylinder, new Vector3(0f, 1.9f, 0.02f), new Vector3(0.5f, 0.08f, 0.5f), rust); // kabuto brim
            P(PrimitiveType.Sphere, new Vector3(0f, 1.9f, 0f), new Vector3(0.42f, 0.3f, 0.42f), rust);
            Cone(new Vector3(0.22f, 2.1f, 0f), new Vector3(0.1f, 0.4f, 0.1f), rust, new Vector3(0f, 0f, -30f));
            Cone(new Vector3(-0.22f, 2.1f, 0f), new Vector3(0.1f, 0.4f, 0.1f), rust, new Vector3(0f, 0f, 30f));
            Eyes(1.76f, 0.22f, 0.09f, 0.1f, spirit);
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Cube, new Vector3(0.42f * s, 1.5f, 0f), new Vector3(0.4f, 0.12f, 0.45f), rust, new Vector3(0f, 0f, -20f * s)); // pauldron
                P(PrimitiveType.Cylinder, new Vector3(0.4f * s, 1.15f, 0.05f), new Vector3(0.07f, 0.3f, 0.07f), mBone);
                P(PrimitiveType.Cylinder, new Vector3(0.16f * s, 0.45f, 0f), new Vector3(0.08f, 0.42f, 0.08f), mBone);
                P(PrimitiveType.Cube, new Vector3(0.16f * s, 0.75f, 0.12f), new Vector3(0.28f, 0.35f, 0.05f), rust); // tassets
            }
            var fire = EnvFx.Fire(mRoot, new Vector3(0f, 1.2f, 0f), 0.3f, false);
            if (fire != null) { var m = fire.main; m.startColor = new Color(0.4f, 1f, 0.5f, 0.6f); }
            BuildSword(new Color(0.45f, 0.4f, 0.38f), 1.7f, new Vector3(0.5f, 1.2f, 0.15f));
            // Widen the blade into a cleaver.
            Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0.18f, 1.1f), new Vector3(0.06f, 0.4f, 1.1f), MaterialFactory.Toon(new Color(0.4f, 0.36f, 0.33f), 0.01f)));
        }

        void ForestDemon(EnemyDefinition d)
        {
            // Walking corrupted tree: bark trunk, antler branches, skull face, root arms, glowing sap and mushrooms.
            var bark = MaterialFactory.Toon(new Color(0.28f, 0.2f, 0.13f), 0.02f);
            var moss = MaterialFactory.Toon(new Color(0.2f, 0.4f, 0.18f), 0.01f);
            var sap = MaterialFactory.Toon(new Color(0.55f, 1f, 0.35f), 0f, new Color(0.45f, 1f, 0.3f));
            P(PrimitiveType.Cylinder, new Vector3(0f, 1.05f, 0f), new Vector3(0.85f, 0.7f, 0.75f), bark, new Vector3(8f, 0f, 0f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.6f, 0.1f), new Vector3(1f, 0.6f, 0.85f), moss);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0.4f), new Vector3(0.42f, 0.45f, 0.42f), mBone);
            Eyes(1.97f, 0.6f, 0.1f, 0.08f, new Color(0.6f, 1f, 0.3f));
            for (int s = -1; s <= 1; s += 2)
            {
                for (int k = 0; k < 3; k++)
                    P(PrimitiveType.Cylinder, new Vector3(0.18f * s + 0.12f * k * s, 2.25f + k * 0.18f, 0.3f), new Vector3(0.05f, 0.25f, 0.05f), bark, new Vector3(0f, 0f, -35f * s - k * 10f * s));
                // Long root arms dragging on the ground.
                P(PrimitiveType.Capsule, new Vector3(0.65f * s, 1.05f, 0.25f), new Vector3(0.22f, 0.75f, 0.22f), bark, new Vector3(25f, 0f, 18f * s));
                for (int f = 0; f < 3; f++) Cone(new Vector3(0.85f * s + (f - 1) * 0.08f, 0.2f, 0.55f), new Vector3(0.06f, 0.35f, 0.06f), bark, new Vector3(150f, 0f, 0f));
                P(PrimitiveType.Capsule, new Vector3(0.28f * s, 0.35f, 0f), new Vector3(0.3f, 0.38f, 0.3f), bark);
                P(PrimitiveType.Cube, new Vector3(0.3f * s, 1.05f, 0.42f), new Vector3(0.04f, 0.5f, 0.02f), sap, new Vector3(0f, 0f, 12f * s));
            }
            for (int i = 0; i < 4; i++)
                Cone(new Vector3(-0.3f + i * 0.2f, 1.75f, -0.4f), new Vector3(0.18f, 0.18f, 0.18f), MaterialFactory.Toon(new Color(0.3f, 0.9f, 1f), 0f, new Color(0.3f, 0.9f, 1f)));
            BuildSword(new Color(0.5f, 1f, 0.4f), 0.9f, new Vector3(0.85f, 0.9f, 0.5f));
        }

        void FlameBeast(EnemyDefinition d)
        {
            // Lion of cooled lava with a burning mane and a fire-whip tail.
            var rock = MaterialFactory.Toon(new Color(0.1f, 0.08f, 0.08f), 0.02f);
            var lava = MaterialFactory.Toon(new Color(1f, 0.45f, 0.05f), 0f, new Color(1f, 0.4f, 0.05f));
            P(PrimitiveType.Capsule, new Vector3(0f, 0.95f, 0f), new Vector3(0.9f, 1f, 0.8f), rock, new Vector3(90f, 0f, 0f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.3f, 0.95f), new Vector3(0.95f, 0.95f, 0.8f), lava); // mane core
            P(PrimitiveType.Sphere, new Vector3(0f, 1.2f, 1.3f), new Vector3(0.55f, 0.5f, 0.6f), rock);
            P(PrimitiveType.Cube, new Vector3(0f, 1.05f, 1.55f), new Vector3(0.3f, 0.12f, 0.2f), lava); // burning jaws
            Eyes(1.32f, 1.55f, 0.14f, 0.07f, new Color(1f, 0.8f, 0.2f));
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, new Vector3(0.38f * s, 0.4f, 0.55f), new Vector3(0.26f, 0.42f, 0.26f), rock);
                P(PrimitiveType.Capsule, new Vector3(0.35f * s, 0.4f, -0.55f), new Vector3(0.24f, 0.4f, 0.24f), rock);
                P(PrimitiveType.Cube, new Vector3(0.46f * s, 0.95f, 0f), new Vector3(0.02f, 0.4f, 0.9f), lava, new Vector3(20f, 0f, 0f));
            }
            P(PrimitiveType.Capsule, new Vector3(0f, 1.1f, -1.2f), new Vector3(0.1f, 0.5f, 0.1f), rock, new Vector3(-50f, 0f, 0f));
            EnvFx.Fire(mRoot, new Vector3(0f, 1.6f, 0.95f), 0.7f, true);
            EnvFx.Fire(mRoot, new Vector3(0f, 1.5f, -1.55f), 0.3f, false);
            BuildSword(new Color(1f, 0.5f, 0.1f), 0.6f, new Vector3(0f, 1.1f, 1.5f));
        }

        void VoidDemon(EnemyDefinition d, bool icy)
        {
            // Legless floating caster: tattered hooded shroud, one great eye, four thin arms, orbiting rune rings.
            Hover = 0.55f;
            var cloth = MaterialFactory.Toon(icy ? new Color(0.55f, 0.7f, 0.85f) : new Color(0.12f, 0.08f, 0.25f), 0.02f);
            var eye = icy ? new Color(0.6f, 0.95f, 1f) : new Color(1f, 0.25f, 0.8f);
            Cone(new Vector3(0f, 0.2f, 0f), new Vector3(0.9f, 1.4f, 0.9f), cloth, new Vector3(180f, 0f, 0f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.35f, 0f), new Vector3(0.7f, 0.6f, 0.6f), cloth);
            Cone(new Vector3(0f, 1.6f, -0.05f), new Vector3(0.55f, 0.6f, 0.55f), cloth, new Vector3(-15f, 0f, 0f)); // hood
            P(PrimitiveType.Sphere, new Vector3(0f, 1.5f, 0.2f), new Vector3(0.36f, 0.34f, 0.2f), mDark);
            Eyes(1.5f, 0.3f, 0f, 0.16f, eye, true);
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < 2; k++)
                    P(PrimitiveType.Capsule, new Vector3(0.42f * s, 1.25f - k * 0.3f, 0.2f), new Vector3(0.07f, 0.4f, 0.07f), cloth, new Vector3(50f, 0f, (30f + k * 25f) * s));
            if (!icy)
            {
                for (int r = 0; r < 2; r++)
                {
                    var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.9f), mRoot, new Vector3(0f, 1.1f + r * 0.35f, 0f), Vector3.one * (1.3f + r * 0.35f), mGlow, false);
                    ring.transform.localRotation = Quaternion.Euler(r == 0 ? 20f : -25f, 0f, 0f);
                    var sp = ring.AddComponent<Spinner>();
                    sp.DegreesPerSecond = new Vector3(0f, r == 0 ? 60f : -45f, 0f);
                    Add(ring);
                }
            }
            var orb = P(PrimitiveType.Sphere, new Vector3(0.55f, 0.95f, 0.45f), Vector3.one * 0.22f, MaterialFactory.Toon(eye, 0f, eye));
            orb.AddComponent<Spinner>().BobHeight = 0.08f;
            var wisps = EnvFx.Smoke(mRoot, new Vector3(0f, 0.2f, 0f), 0.3f);
            if (wisps != null) { var m = wisps.main; m.startColor = icy ? new Color(0.8f, 0.9f, 1f, 0.4f) : new Color(0.2f, 0.05f, 0.3f, 0.5f); }
            BuildSword(eye, 0.5f, new Vector3(0.5f, 1f, 0.4f));
        }

        void Knight(EnemyDefinition d)
        {
            // Black spiked plate, swept-horn helm with a burning visor, demon-face kite shield and a barbed halberd.
            var plate = MaterialFactory.Toon(new Color(0.08f, 0.08f, 0.1f), 0.02f);
            var tabard = MaterialFactory.Toon(new Color(0.3f, 0.1f, 0.4f), 0.02f);
            P(PrimitiveType.Cube, new Vector3(0f, 1.25f, 0f), new Vector3(0.75f, 0.7f, 0.45f), plate);
            P(PrimitiveType.Cube, new Vector3(0f, 0.8f, 0.2f), new Vector3(0.5f, 0.6f, 0.04f), tabard);
            P(PrimitiveType.Cylinder, new Vector3(0f, 1.8f, 0f), new Vector3(0.36f, 0.25f, 0.38f), plate);
            P(PrimitiveType.Cube, new Vector3(0f, 1.8f, 0.19f), new Vector3(0.25f, 0.04f, 0.02f), mGlow);
            for (int s = -1; s <= 1; s += 2)
            {
                Cone(new Vector3(0.18f * s, 1.98f, -0.15f), new Vector3(0.09f, 0.55f, 0.09f), plate, new Vector3(-55f, 0f, -15f * s));
                P(PrimitiveType.Sphere, new Vector3(0.48f * s, 1.5f, 0f), new Vector3(0.42f, 0.32f, 0.42f), plate);
                Cone(new Vector3(0.55f * s, 1.68f, 0f), new Vector3(0.08f, 0.22f, 0.08f), plate);
                P(PrimitiveType.Cube, new Vector3(0.2f * s, 0.4f, 0f), new Vector3(0.24f, 0.8f, 0.28f), plate);
                P(PrimitiveType.Cube, new Vector3(0.38f * s, 1.25f, 0.02f), new Vector3(0.02f, 0.5f, 0.02f), mGlow);
            }
            var shield = P(PrimitiveType.Cube, new Vector3(-0.58f, 1.05f, 0.3f), new Vector3(0.08f, 0.95f, 0.6f), plate, new Vector3(0f, -15f, 0f));
            MeshFactory.Primitive(PrimitiveType.Sphere, shield.transform, new Vector3(0.6f, 0.1f, 0f), new Vector3(0.4f, 0.3f, 0.5f), mGlow);
            BuildSword(new Color(0.7f, 0.3f, 1f), 2.1f, new Vector3(0.5f, 1.05f, 0.1f));
            Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0.12f, 0f, 1.95f), new Vector3(0.05f, 0.35f, 0.35f), plate)); // axe head
        }

        void Hunter(EnemyDefinition d)
        {
            var cloak = MaterialFactory.Toon(new Color(0.12f, 0.05f, 0.15f), 0.02f);
            P(PrimitiveType.Capsule, new Vector3(0f, 1f, 0f), new Vector3(0.55f, 0.75f, 0.45f), mBody);
            P(PrimitiveType.Cube, new Vector3(0f, 1.05f, -0.25f), new Vector3(0.75f, 1.2f, 0.05f), cloak, new Vector3(8f, 0f, 0f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.8f, 0.05f), Vector3.one * 0.42f, mBody);
            Cone(new Vector3(0f, 1.95f, -0.05f), new Vector3(0.5f, 0.5f, 0.5f), cloak);
            Eyes(1.82f, 0.24f, 0.09f, 0.08f, d.accentColor);
            // Crescent blade.
            BuildSword(d.accentColor, 1.2f, new Vector3(0.45f, 1.1f, 0.2f));
            var arc = MeshFactory.MeshObject(MeshFactory.Sector(140f, 0.8f), SwordPivot, new Vector3(0f, 0f, 0.9f), new Vector3(0.9f, 1f, 0.9f), mGlow, false);
            arc.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Add(arc);
        }

        void Imp(EnemyDefinition d)
        {
            var lava = MaterialFactory.Toon(d.accentColor, 0f, d.accentColor);
            P(PrimitiveType.Sphere, new Vector3(0f, 0.75f, 0f), new Vector3(0.7f, 0.75f, 0.6f), mBody);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.25f, 0.15f), new Vector3(0.5f, 0.45f, 0.45f), mBody);
            Cone(new Vector3(0.15f, 1.5f, 0.1f), new Vector3(0.08f, 0.3f, 0.08f), lava, new Vector3(0f, 0f, -25f));
            Cone(new Vector3(-0.15f, 1.5f, 0.1f), new Vector3(0.08f, 0.3f, 0.08f), lava, new Vector3(0f, 0f, 25f));
            Eyes(1.28f, 0.36f, 0.1f, 0.09f, new Color(1f, 0.9f, 0.3f));
            P(PrimitiveType.Cube, new Vector3(0f, 0.8f, 0.3f), new Vector3(0.35f, 0.04f, 0.02f), lava);
            EnvFx.Fire(mRoot, new Vector3(0f, 1.5f, 0f), 0.2f, false);
            BuildSword(new Color(1f, 0.6f, 0.2f), 0.45f, new Vector3(0.35f, 0.8f, 0.3f));
        }

        void Oni(EnemyDefinition d)
        {
            var ice = MaterialFactory.Toon(new Color(0.8f, 0.92f, 1f), 0.01f, new Color(0.4f, 0.6f, 0.8f));
            P(PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0f), new Vector3(1f, 0.85f, 0.8f), mBody);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0.1f), new Vector3(0.55f, 0.5f, 0.5f), mBody);
            Cone(new Vector3(0.18f, 2.25f, 0f), new Vector3(0.12f, 0.45f, 0.12f), ice, new Vector3(0f, 0f, -15f));
            Cone(new Vector3(-0.18f, 2.25f, 0f), new Vector3(0.12f, 0.45f, 0.12f), ice, new Vector3(0f, 0f, 15f));
            Eyes(2f, 0.35f, 0.12f, 0.1f, new Color(0.6f, 0.95f, 1f));
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Sphere, new Vector3(0.62f * s, 1.55f, 0f), Vector3.one * 0.55f, ice);
                P(PrimitiveType.Capsule, new Vector3(0.7f * s, 1.05f, 0.1f), new Vector3(0.3f, 0.55f, 0.3f), mBody);
                P(PrimitiveType.Capsule, new Vector3(0.3f * s, 0.4f, 0f), new Vector3(0.35f, 0.42f, 0.35f), mBody);
            }
            BuildSword(new Color(0.75f, 0.9f, 1f), 1.5f, new Vector3(0.75f, 1f, 0.3f));
            Add(MeshFactory.Primitive(PrimitiveType.Cylinder, SwordPivot, new Vector3(0f, 0f, 1.1f), new Vector3(0.22f, 0.5f, 0.22f), ice));
        }

        void Sentinel(EnemyDefinition d)
        {
            var plate = MaterialFactory.Toon(new Color(0.12f, 0.1f, 0.14f), 0.02f);
            P(PrimitiveType.Cube, new Vector3(0f, 1.3f, 0f), new Vector3(1.1f, 0.9f, 0.7f), plate);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.3f, 0.36f), Vector3.one * 0.3f, mGlow); // core
            P(PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), new Vector3(0.45f, 0.4f, 0.45f), plate);
            P(PrimitiveType.Cube, new Vector3(0f, 1.95f, 0.23f), new Vector3(0.3f, 0.05f, 0.02f), mGlow);
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Cube, new Vector3(0.75f * s, 1.65f, 0f), new Vector3(0.5f, 0.4f, 0.6f), plate);
                P(PrimitiveType.Cube, new Vector3(0.75f * s, 1f, 0.05f), new Vector3(0.35f, 0.9f, 0.35f), plate);
                P(PrimitiveType.Cube, new Vector3(0.3f * s, 0.4f, 0f), new Vector3(0.4f, 0.8f, 0.45f), plate);
            }
            BuildSword(d.accentColor, 1.8f, new Vector3(0.8f, 1f, 0.3f));
        }

        void Dummy()
        {
            var straw = MaterialFactory.Toon(new Color(0.75f, 0.62f, 0.35f), 0.02f);
            var wood = MaterialFactory.Toon(new Color(0.4f, 0.28f, 0.16f), 0.02f);
            P(PrimitiveType.Cylinder, new Vector3(0f, 0.8f, 0f), new Vector3(0.12f, 0.8f, 0.12f), wood);
            P(PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(0.55f, 0.45f, 0.45f), straw);
            P(PrimitiveType.Cylinder, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 0.6f, 0.08f), wood, new Vector3(0f, 0f, 90f));
            P(PrimitiveType.Sphere, new Vector3(0f, 1.75f, 0f), Vector3.one * 0.38f, straw);
            BuildSword(new Color(0.5f, 0.4f, 0.3f), 0.1f, new Vector3(0f, 1f, 0f));
        }

        // ------------------------------------------------------------------ Bosses

        void Brute(EnemyDefinition d)
        {
            // Gorvath: hulking horned butcher with a meat cleaver.
            P(PrimitiveType.Capsule, new Vector3(0f, 1.15f, 0f), new Vector3(1f, 0.85f, 0.8f), mBody);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.55f, 0.15f), new Vector3(1.2f, 0.6f, 0.8f), mBody);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0.35f), new Vector3(0.45f, 0.42f, 0.45f), mBody);
            Cone(new Vector3(0.25f, 2.25f, 0.2f), new Vector3(0.14f, 0.6f, 0.14f), mBone, new Vector3(-20f, 0f, -40f));
            Cone(new Vector3(-0.25f, 2.25f, 0.2f), new Vector3(0.14f, 0.6f, 0.14f), mBone, new Vector3(-20f, 0f, 40f));
            Eyes(1.98f, 0.57f, 0.12f, 0.08f, d.accentColor);
            P(PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.38f), new Vector3(0.9f, 0.5f, 0.05f), MaterialFactory.Toon(new Color(0.4f, 0.3f, 0.25f), 0.02f)); // apron
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Capsule, new Vector3(0.75f * s, 1.15f, 0.1f), new Vector3(0.35f, 0.6f, 0.35f), mBody);
                P(PrimitiveType.Capsule, new Vector3(0.3f * s, 0.4f, 0f), new Vector3(0.38f, 0.42f, 0.38f), mBody);
            }
            BuildSword(new Color(0.6f, 0.55f, 0.5f), 1.3f, new Vector3(0.8f, 1f, 0.3f));
            Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0.2f, 0.9f), new Vector3(0.06f, 0.5f, 0.9f), MaterialFactory.Toon(new Color(0.5f, 0.45f, 0.42f), 0.01f)));
        }

        void General(EnemyDefinition d)
        {
            // Demon general: ash-grey warlord, horns, fur-collared war cloak, back banners, twin blades.
            var ash = MaterialFactory.Toon(new Color(0.55f, 0.55f, 0.58f), 0.02f);
            var armor = MaterialFactory.Toon(Color.Lerp(d.bodyColor, new Color(0.12f, 0.1f, 0.1f), 0.5f), 0.02f);
            var lacquer = MaterialFactory.Toon(d.accentColor * 0.8f, 0.02f);
            var fur = MaterialFactory.Toon(new Color(0.08f, 0.07f, 0.07f), 0.02f);
            P(PrimitiveType.Capsule, new Vector3(0f, 1.1f, 0f), new Vector3(0.7f, 0.75f, 0.5f), armor);
            P(PrimitiveType.Cube, new Vector3(0f, 1.3f, 0.2f), new Vector3(0.6f, 0.4f, 0.08f), lacquer);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.8f, 0.05f), new Vector3(0.38f, 0.42f, 0.38f), ash);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.9f, -0.04f), new Vector3(0.4f, 0.3f, 0.4f), fur); // slicked hair
            Cone(new Vector3(0.14f, 2.02f, 0.05f), new Vector3(0.07f, 0.25f, 0.07f), mBone, new Vector3(-30f, 0f, -20f));
            Cone(new Vector3(-0.14f, 2.02f, 0.05f), new Vector3(0.07f, 0.25f, 0.07f), mBone, new Vector3(-30f, 0f, 20f));
            Eyes(1.82f, 0.22f, 0.08f, 0.07f, d.accentColor);
            P(PrimitiveType.Capsule, new Vector3(0f, 1.52f, -0.05f), new Vector3(0.95f, 0.18f, 0.6f), fur, new Vector3(0f, 0f, 90f)); // fur collar
            P(PrimitiveType.Cube, new Vector3(0f, 0.95f, -0.3f), new Vector3(0.9f, 1.4f, 0.05f), fur, new Vector3(6f, 0f, 0f)); // cloak
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Cylinder, new Vector3(0.25f * s, 2.1f, -0.35f), new Vector3(0.03f, 0.9f, 0.03f), mDark);
                P(PrimitiveType.Cube, new Vector3(0.25f * s + 0.12f * s, 2.45f, -0.35f), new Vector3(0.25f, 0.5f, 0.02f), lacquer); // war banners
                P(PrimitiveType.Sphere, new Vector3(0.48f * s, 1.5f, 0f), new Vector3(0.36f, 0.26f, 0.36f), armor);
                P(PrimitiveType.Capsule, new Vector3(0.2f * s, 0.4f, 0f), new Vector3(0.26f, 0.42f, 0.26f), armor);
            }
            BuildSword(d.accentColor, 1.3f, new Vector3(0.5f, 1.05f, 0.2f));
            // Second blade held low in the off hand.
            var off = P(PrimitiveType.Cube, new Vector3(-0.5f, 0.75f, 0.55f), new Vector3(0.05f, 0.08f, 1.2f), MaterialFactory.Toon(d.accentColor, 0.01f, d.accentColor * 0.5f), new Vector3(35f, 10f, 0f));
            off.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        void Guardian(EnemyDefinition d)
        {
            // Ancient Guardian: moss-covered four-armed stone colossus, serene mask, halo of floating stones, ring-blade.
            var stone = MaterialFactory.Toon(new Color(0.75f, 0.74f, 0.68f), 0.02f);
            var moss = MaterialFactory.Toon(new Color(0.3f, 0.45f, 0.25f), 0.01f);
            var gold = MaterialFactory.Toon(new Color(0.85f, 0.7f, 0.3f), 0.01f);
            var rune = MaterialFactory.Toon(new Color(0.35f, 0.95f, 1f), 0f, new Color(0.3f, 0.9f, 1f));
            P(PrimitiveType.Cube, new Vector3(0f, 1.2f, 0f), new Vector3(1.1f, 1.1f, 0.7f), stone);
            P(PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.36f), new Vector3(0.7f, 0.05f, 0.02f), rune);
            P(PrimitiveType.Cube, new Vector3(0f, 1.35f, 0.36f), new Vector3(0.05f, 0.6f, 0.02f), rune);
            P(PrimitiveType.Sphere, new Vector3(0.3f, 1.75f, 0.1f), new Vector3(0.5f, 0.25f, 0.45f), moss);
            P(PrimitiveType.Cube, new Vector3(0f, 2.05f, 0.05f), new Vector3(0.5f, 0.55f, 0.45f), stone);
            P(PrimitiveType.Cube, new Vector3(0f, 2.05f, 0.29f), new Vector3(0.4f, 0.45f, 0.03f), gold); // serene mask
            Eyes(2.1f, 0.31f, 0.1f, 0.07f, new Color(0.35f, 0.95f, 1f));
            for (int s = -1; s <= 1; s += 2)
            {
                P(PrimitiveType.Cube, new Vector3(0.75f * s, 1.55f, 0f), new Vector3(0.45f, 0.35f, 0.45f), stone);
                P(PrimitiveType.Cube, new Vector3(0.85f * s, 1.15f, 0.2f), new Vector3(0.25f, 0.7f, 0.25f), stone, new Vector3(20f, 0f, 10f * s));
                P(PrimitiveType.Cube, new Vector3(0.7f * s, 0.9f, -0.05f), new Vector3(0.22f, 0.6f, 0.22f), stone, new Vector3(-15f, 0f, 25f * s)); // lower arms
                P(PrimitiveType.Cube, new Vector3(0.3f * s, 0.35f, 0f), new Vector3(0.4f, 0.7f, 0.45f), stone);
            }
            var shield = P(PrimitiveType.Cylinder, new Vector3(-1.05f, 0.75f, 0.1f), new Vector3(0.7f, 0.05f, 0.7f), stone, new Vector3(0f, 0f, 90f));
            MeshFactory.Primitive(PrimitiveType.Cube, shield.transform, new Vector3(0f, -1.2f, 0f), new Vector3(0.1f, 0.2f, 0.9f), mDark); // crack
            // Halo of floating stones.
            var halo = new GameObject("Halo").transform;
            halo.SetParent(mRoot, false);
            halo.localPosition = new Vector3(0f, 2.15f, -0.45f);
            halo.localRotation = Quaternion.Euler(90f, 0f, 0f);
            for (int i = 0; i < 10; i++)
            {
                float a = i * 36f * Mathf.Deg2Rad;
                Add(MeshFactory.Primitive(PrimitiveType.Cube, halo, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 0.75f, Vector3.one * 0.14f, stone));
            }
            halo.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 25f, 0f);
            BuildSword(new Color(0.35f, 0.95f, 1f), 1.2f, new Vector3(0.9f, 1.1f, 0.4f));
            var ringBlade = MeshFactory.MeshObject(MeshFactory.Ring(0.75f), SwordPivot, new Vector3(0f, 0f, 1.1f), Vector3.one * 0.8f, stone, false);
            ringBlade.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            Add(ringBlade);
        }

        void Lord(EnemyDefinition d)
        {
            // Veyrath, first form: tall regal demon king. Silver hair, horn crown, black armour, crimson cape, chest void.
            var armor = MaterialFactory.Toon(new Color(0.06f, 0.05f, 0.07f), 0.02f);
            var crimson = MaterialFactory.Toon(new Color(0.55f, 0.05f, 0.12f), 0.02f);
            var gold = MaterialFactory.Toon(new Color(0.9f, 0.72f, 0.3f), 0.01f);
            var skin = MaterialFactory.Toon(new Color(0.78f, 0.78f, 0.8f), 0.02f);
            var silver = MaterialFactory.Toon(new Color(0.92f, 0.92f, 0.95f), 0.02f);
            var voidMat = MaterialFactory.Toon(new Color(0.8f, 0.1f, 0.5f), 0f, new Color(0.8f, 0.1f, 0.45f));
            P(PrimitiveType.Capsule, new Vector3(0f, 1.2f, 0f), new Vector3(0.6f, 0.8f, 0.42f), armor);
            P(PrimitiveType.Cube, new Vector3(0f, 1.35f, 0.2f), new Vector3(0.45f, 0.45f, 0.06f), armor);
            P(PrimitiveType.Sphere, new Vector3(0.12f, 1.42f, 0.21f), Vector3.one * 0.08f, gold); // eclipse emblem
            P(PrimitiveType.Sphere, new Vector3(-0.14f, 1.45f, 0.2f), new Vector3(0.18f, 0.2f, 0.08f), voidMat); // missing half of the heart
            P(PrimitiveType.Cube, new Vector3(0f, 0.95f, 0.22f), new Vector3(0.5f, 0.07f, 0.04f), gold);
            P(PrimitiveType.Sphere, new Vector3(0f, 1.9f, 0.03f), new Vector3(0.34f, 0.4f, 0.34f), skin);
            P(PrimitiveType.Capsule, new Vector3(0f, 1.45f, -0.2f), new Vector3(0.42f, 0.65f, 0.12f), silver); // long hair
            P(PrimitiveType.Sphere, new Vector3(0f, 2f, -0.03f), new Vector3(0.38f, 0.3f, 0.38f), silver);
            for (int s = -1; s <= 1; s += 2)
            {
                Cone(new Vector3(0.1f * s, 2.15f, 0f), new Vector3(0.05f, 0.32f, 0.05f), armor, new Vector3(-25f, 0f, -20f * s));
                Cone(new Vector3(0.44f * s, 1.68f, -0.02f), new Vector3(0.09f, 0.3f, 0.09f), armor, new Vector3(0f, 0f, -35f * s)); // spiked collar
                P(PrimitiveType.Sphere, new Vector3(0.4f * s, 1.55f, 0f), new Vector3(0.3f, 0.22f, 0.32f), armor);
                P(PrimitiveType.Capsule, new Vector3(0.18f * s, 0.42f, 0f), new Vector3(0.22f, 0.45f, 0.22f), armor);
            }
            Eyes(1.92f, 0.3f, 0.07f, 0.06f, new Color(1f, 0.15f, 0.2f));
            var cape = P(PrimitiveType.Cube, new Vector3(0f, 1f, -0.3f), new Vector3(0.95f, 1.8f, 0.04f), crimson, new Vector3(8f, 0f, 0f));
            cape.AddComponent<Sway>().Amount = 3f;
            BuildSword(new Color(1f, 0.1f, 0.2f), 1.9f, new Vector3(0.45f, 1.05f, 0.2f));
            Add(MeshFactory.Primitive(PrimitiveType.Cube, SwordPivot, new Vector3(0f, 0f, 1.05f), new Vector3(0.14f, 0.2f, 1.9f), armor)); // greatsword body
        }

        /// <summary>Veyrath's second form: six wings and the eclipse ring unfold behind him.</summary>
        public void UnfoldEclipseForm(Color accent)
        {
            if (Model == null) return;
            mRoot = Model;
            var wing = MaterialFactory.Toon(new Color(0.05f, 0.03f, 0.05f), 0.02f);
            var edge = MaterialFactory.Toon(accent, 0f, accent);
            for (int s = -1; s <= 1; s += 2)
                for (int k = 0; k < 3; k++)
                {
                    var w = P(PrimitiveType.Cube, new Vector3(0.75f * s, 1.55f + k * 0.35f - 0.35f, -0.35f), new Vector3(1.4f, 0.06f, 0.55f), wing,
                        new Vector3(0f, 25f * s, (20f - k * 22f) * s));
                    MeshFactory.Primitive(PrimitiveType.Cube, w.transform, new Vector3(0f, -0.6f, 0.45f), new Vector3(1f, 0.4f, 0.1f), edge);
                    w.AddComponent<Sway>().Amount = 6f;
                }
            var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.85f), Model, new Vector3(0f, 2.1f, -0.6f), Vector3.one * 1.3f, edge, false);
            ring.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            ring.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 40f, 0f);
            Add(ring);
        }
    }
}
