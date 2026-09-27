using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Elemental attack effects, layered on top of the plain crescent slash so every element reads at a
    /// glance: Water throws blue arcs and spray, Flame leaves burning arcs and rising fire, Thunder cracks
    /// with forked bolts and sparks, Beast (wind) whips green gust lines and leaves, Light glitters gold and
    /// Dark bleeds violet smoke. Slash for swings, Impact for hits, Finisher for big blows.
    /// </summary>
    public static class ElementFx
    {
        static Transform root;
        static ParticleSystem droplets, flames, leaves, sparkles, wisps;

        static void Ensure()
        {
            if (root != null) return;
            var go = new GameObject("[ElementFx]");
            Object.DontDestroyOnLoad(go);
            root = go.transform;
            droplets = Make("Droplets", true, false, 0.35f, 0.7f, 0.07f, 0.16f, 1.6f);
            flames = Make("Flames", false, false, 0.35f, 0.7f, 0.35f, 0.75f, -0.35f);
            leaves = Make("Leaves", false, true, 0.6f, 1.1f, 0.1f, 0.2f, 0.15f);
            sparkles = Make("Sparkles", false, false, 0.3f, 0.7f, 0.08f, 0.22f, -0.05f);
            wisps = Make("Wisps", false, true, 0.6f, 1.2f, 0.5f, 1.1f, -0.1f);
            var fl = flames.velocityOverLifetime;
            fl.enabled = false;
            var noise = flames.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 2f;
            var rot = leaves.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
        }

        static ParticleSystem Make(string name, bool stretched, bool alpha, float lifeMin, float lifeMax, float sizeMin, float sizeMax, float gravity)
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
            main.startSpeed = 0f;
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.maxParticles = 1200;
            var em = ps.emission;
            em.enabled = false;
            var shape = ps.shape;
            shape.enabled = false;
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f));
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var grad = new Gradient();
            grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0.85f, 0.4f), new GradientAlphaKey(0f, 1f) });
            col.color = grad;
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = alpha ? MaterialFactory.Transparent(Color.white, true) : MaterialFactory.Additive(Color.white, true);
            if (stretched)
            {
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.velocityScale = 0.06f;
                r.lengthScale = 2f;
            }
            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Vector3 vel, Color c, float size = -1f)
        {
            var p = new ParticleSystem.EmitParams { position = pos, velocity = vel, startColor = c, applyShapeToPosition = false };
            if (size > 0f) p.startSize = size;
            ps.Emit(p, 1);
        }

        static int Count(int n) { return Mathf.Max(1, Mathf.RoundToInt(n * GameSettings.ParticleScale)); }

        /// <summary>Points along a slash arc (same framing as VFX.Slash).</summary>
        static Vector3 ArcPoint(Vector3 center, Quaternion rot, float radius, float arc, float t)
        {
            float a = Mathf.Lerp(-arc * 0.5f, arc * 0.5f, t) * Mathf.Deg2Rad;
            return center + Vector3.up + rot * new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * radius;
        }

        // ------------------------------------------------------------------ Swings

        /// <summary>A swing: layered crescents plus the element's signature along the arc. power 1 = normal hit, 2 = finisher.</summary>
        public static void Slash(Vector3 center, Vector3 forward, float radius, float arc, float roll, Element e, float power = 1f)
        {
            Ensure();
            Color c = ElementChart.ColorOf(e);
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
            forward.Normalize();
            var rot = Quaternion.LookRotation(forward) * Quaternion.Euler(0f, 0f, roll);
            float dur = 0.2f + 0.06f * power;
            // Glow, colour and white-hot core.
            VFX.Slash(center, forward, radius * 1.12f, arc, roll, new Color(c.r, c.g, c.b, 0.45f), dur * 1.3f);
            VFX.Slash(center, forward, radius, arc, roll, c, dur);
            VFX.Slash(center, forward, radius * 0.9f, arc * 0.92f, roll, Color.Lerp(c, Color.white, 0.75f), dur * 0.6f);

            int n = Count(Mathf.RoundToInt(10 * power));
            switch (e)
            {
                case Element.Water:
                    // A trailing second wave and a fan of spray.
                    VFX.Slash(center, forward, radius * 0.78f, arc * 0.85f, roll + 14f, new Color(0.55f, 0.9f, 1f, 0.8f), dur * 1.4f);
                    for (int i = 0; i < n * 2; i++)
                    {
                        float t = Random.value;
                        Vector3 p = ArcPoint(center, rot, radius, arc, t);
                        Vector3 outDir = (p - center - Vector3.up).normalized;
                        Emit(droplets, p, outDir * Random.Range(2f, 5f) + Vector3.up * Random.Range(1f, 3f),
                            Random.value < 0.4f ? new Color(0.9f, 0.97f, 1f) : new Color(0.3f, 0.65f, 1f));
                    }
                    break;
                case Element.Flame:
                    VFX.Slash(center, forward, radius * 1.05f, arc, roll - 8f, new Color(1f, 0.3f, 0.05f, 0.7f), dur * 1.6f);
                    for (int i = 0; i < n * 2; i++)
                    {
                        Vector3 p = ArcPoint(center, rot, radius * Random.Range(0.85f, 1.05f), arc, Random.value);
                        Color fc = Random.value < 0.3f ? new Color(1f, 0.9f, 0.4f) : Random.value < 0.6f ? new Color(1f, 0.5f, 0.1f) : new Color(0.95f, 0.2f, 0.05f);
                        Emit(flames, p, Vector3.up * Random.Range(1.2f, 2.6f) + Random.insideUnitSphere * 0.5f, fc);
                    }
                    VFX.HitSpark(center + Vector3.up + forward * radius * 0.7f, new Color(1f, 0.6f, 0.15f), Count(6));
                    break;
                case Element.Thunder:
                    int bolts = power > 1.5f ? 4 : 2;
                    for (int i = 0; i < bolts; i++)
                    {
                        float t0 = Random.Range(0f, 0.7f);
                        BoltFx.Strike(ArcPoint(center, rot, radius, arc, t0), ArcPoint(center, rot, radius * Random.Range(0.8f, 1.2f), arc, t0 + Random.Range(0.2f, 0.35f)),
                            new Color(1f, 0.92f, 0.35f), 0.12f, 0.18f + 0.05f * power);
                    }
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p = ArcPoint(center, rot, radius, arc, Random.value);
                        Emit(droplets, p, Random.onUnitSphere * Random.Range(4f, 8f), new Color(1f, 0.95f, 0.5f), 0.05f);
                    }
                    VFX.ImpactLight(center + Vector3.up + forward * radius * 0.6f, new Color(1f, 0.9f, 0.4f), 5f, 0.08f);
                    break;
                case Element.Beast:
                    VFX.Slash(center, forward, radius * 1.2f, arc * 0.7f, roll + 20f, new Color(0.6f, 1f, 0.6f, 0.5f), dur * 1.2f);
                    VFX.Slash(center, forward, radius * 0.7f, arc * 0.7f, roll - 20f, new Color(0.6f, 1f, 0.6f, 0.5f), dur * 1.2f);
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p = ArcPoint(center, rot, radius, arc, Random.value);
                        Emit(leaves, p, (forward + Random.insideUnitSphere * 0.6f) * Random.Range(2f, 4f), Random.value < 0.5f ? new Color(0.35f, 0.8f, 0.3f) : new Color(0.7f, 0.9f, 0.3f));
                    }
                    break;
                case Element.Light:
                    for (int i = 0; i < n * 2; i++)
                    {
                        Vector3 p = ArcPoint(center, rot, radius * Random.Range(0.8f, 1.1f), arc, Random.value);
                        Emit(sparkles, p, Random.insideUnitSphere * 1.2f + Vector3.up * 0.8f, Random.value < 0.5f ? Color.white : new Color(1f, 0.9f, 0.5f));
                    }
                    break;
                default: // Dark
                    VFX.Slash(center, forward, radius * 1.08f, arc, roll + 6f, new Color(0.35f, 0.05f, 0.5f, 0.7f), dur * 1.8f);
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p = ArcPoint(center, rot, radius, arc, Random.value);
                        Emit(wisps, p, Random.insideUnitSphere * 0.6f + Vector3.up * 0.4f, new Color(0.2f, 0.05f, 0.3f, 0.6f));
                        Emit(sparkles, p, Random.insideUnitSphere * 1.5f, new Color(0.75f, 0.35f, 1f));
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ Hits

        /// <summary>The element bursting on a target when a blow lands.</summary>
        public static void Impact(Vector3 pos, Element e, bool heavy)
        {
            Ensure();
            Color c = ElementChart.ColorOf(e);
            int n = Count(heavy ? 18 : 9);
            switch (e)
            {
                case Element.Water:
                    for (int i = 0; i < n; i++)
                        Emit(droplets, pos, Random.onUnitSphere * Random.Range(2f, 5f) + Vector3.up * 2.5f, Random.value < 0.4f ? Color.white : new Color(0.35f, 0.7f, 1f));
                    if (heavy) VFX.Shockwave(pos - Vector3.up * (pos.y - 0.05f), 2f, new Color(0.5f, 0.85f, 1f), 0.35f);
                    break;
                case Element.Flame:
                    for (int i = 0; i < n; i++)
                        Emit(flames, pos + Random.insideUnitSphere * 0.3f, Random.onUnitSphere * 1.5f + Vector3.up * 1.5f, Random.value < 0.4f ? new Color(1f, 0.85f, 0.3f) : new Color(1f, 0.35f, 0.05f));
                    break;
                case Element.Thunder:
                    int bolts = heavy ? 4 : 2;
                    for (int i = 0; i < bolts; i++) BoltFx.Strike(pos, pos + Random.onUnitSphere * Random.Range(0.8f, 1.6f), new Color(1f, 0.92f, 0.4f), 0.07f, 0.14f);
                    for (int i = 0; i < n; i++) Emit(droplets, pos, Random.onUnitSphere * Random.Range(5f, 9f), new Color(1f, 0.95f, 0.55f), 0.05f);
                    break;
                case Element.Beast:
                    for (int i = 0; i < n; i++) Emit(leaves, pos, Random.onUnitSphere * Random.Range(1.5f, 3.5f), new Color(0.45f, 0.85f, 0.35f));
                    break;
                case Element.Light:
                    for (int i = 0; i < n; i++) Emit(sparkles, pos, Random.onUnitSphere * Random.Range(1f, 3f), Random.value < 0.5f ? Color.white : new Color(1f, 0.9f, 0.5f));
                    break;
                default:
                    for (int i = 0; i < n / 2 + 1; i++) Emit(wisps, pos + Random.insideUnitSphere * 0.3f, Random.insideUnitSphere + Vector3.up * 0.5f, new Color(0.2f, 0.05f, 0.3f, 0.7f));
                    for (int i = 0; i < n; i++) Emit(sparkles, pos, Random.onUnitSphere * 2.5f, new Color(0.75f, 0.35f, 1f));
                    break;
            }
            if (heavy) VFX.ImpactLight(pos, c, 6f, 0.15f);
        }

        // ------------------------------------------------------------------ Finishers

        /// <summary>The element erupting at the end of a combo or a heavy blow.</summary>
        public static void Finisher(Vector3 pos, Vector3 forward, Element e, float radius)
        {
            Ensure();
            Color c = ElementChart.ColorOf(e);
            Vector3 ground = new Vector3(pos.x, 0.05f, pos.z);
            VFX.Shockwave(ground, radius, c, 0.4f);
            VFX.ImpactLight(pos + Vector3.up, c, 9f, 0.25f);
            int n = Count(30);
            switch (e)
            {
                case Element.Water:
                    // A crashing wave: rings of spray and a rising column.
                    VFX.Shockwave(ground, radius * 0.7f, new Color(0.7f, 0.95f, 1f), 0.55f);
                    VFX.Pillar(ground, new Color(0.4f, 0.75f, 1f), 4f, 0.45f);
                    for (int i = 0; i < n * 2; i++)
                    {
                        float a = Random.value * Mathf.PI * 2f;
                        Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Emit(droplets, ground + d * radius * 0.4f + Vector3.up * 0.3f, d * Random.Range(3f, 6f) + Vector3.up * Random.Range(3f, 7f), Random.value < 0.4f ? Color.white : new Color(0.3f, 0.65f, 1f));
                    }
                    break;
                case Element.Flame:
                    VFX.Pillar(ground, new Color(1f, 0.45f, 0.08f), 5f, 0.5f);
                    for (int i = 0; i < n * 2; i++)
                    {
                        Vector2 r = Random.insideUnitCircle * radius * 0.8f;
                        Emit(flames, ground + new Vector3(r.x, 0.2f, r.y), Vector3.up * Random.Range(2f, 4.5f), Random.value < 0.3f ? new Color(1f, 0.9f, 0.4f) : new Color(1f, 0.4f, 0.06f), Random.Range(0.5f, 1f));
                    }
                    break;
                case Element.Thunder:
                    // Lightning falls from the sky onto the blow.
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 r = Random.insideUnitCircle * radius * 0.5f;
                        Vector3 hit = ground + new Vector3(r.x, 0f, r.y);
                        BoltFx.Strike(hit + Vector3.up * 12f + Random.insideUnitSphere * 2f, hit, new Color(1f, 0.95f, 0.45f), 0.3f, 0.3f, 0.5f);
                    }
                    for (int i = 0; i < n; i++) Emit(droplets, ground + Vector3.up * 0.3f, Random.onUnitSphere * Random.Range(6f, 11f) + Vector3.up * 2f, new Color(1f, 0.95f, 0.5f), 0.06f);
                    VFX.ImpactLight(ground + Vector3.up * 3f, new Color(1f, 0.95f, 0.6f), 16f, 0.2f);
                    break;
                case Element.Beast:
                    // A green whirlwind.
                    for (int k = 0; k < 3; k++) VFX.Slash(ground, Quaternion.Euler(0f, k * 120f, 0f) * forward, radius * 0.8f, 200f, 70f, new Color(0.6f, 1f, 0.6f, 0.7f), 0.35f);
                    for (int i = 0; i < n; i++)
                    {
                        float a = Random.value * Mathf.PI * 2f;
                        Vector3 d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                        Emit(leaves, ground + d * radius * 0.5f + Vector3.up * Random.Range(0.2f, 2f), Vector3.Cross(Vector3.up, d) * 5f + Vector3.up * 2f, new Color(0.45f, 0.85f, 0.35f));
                    }
                    break;
                case Element.Light:
                    VFX.Pillar(ground, new Color(1f, 0.95f, 0.7f), 9f, 0.6f);
                    for (int i = 0; i < n * 2; i++) Emit(sparkles, ground + Random.insideUnitSphere * radius * 0.5f + Vector3.up, Vector3.up * Random.Range(1f, 4f), Random.value < 0.5f ? Color.white : new Color(1f, 0.9f, 0.5f));
                    break;
                default:
                    VFX.Pillar(ground, new Color(0.45f, 0.1f, 0.6f), 5f, 0.6f);
                    for (int i = 0; i < n; i++)
                    {
                        Vector2 r = Random.insideUnitCircle * radius * 0.6f;
                        Emit(wisps, ground + new Vector3(r.x, 0.3f, r.y), Vector3.up * Random.Range(0.5f, 1.5f), new Color(0.15f, 0.03f, 0.22f, 0.8f), Random.Range(0.8f, 1.5f));
                    }
                    break;
            }
        }
    }
}
