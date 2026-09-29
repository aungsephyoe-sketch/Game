using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// More signature specials, so no two slayers share one:
    ///   Ren (Dawn) — Sun Wheel: twelve dashes carve a star around the demons, then a sun falls as a pillar of light
    ///   Homura — Phoenix Dive: he becomes a firebird and crosses the field twice, leaving an X of fire
    ///   Kuroe — Eclipse: a blood moon is swallowed by darkness, a vortex drags demons in, crescents spiral out
    ///   Seren — Constellation: a star marks every demon, lines join them, and they all fall at once
    ///   Garou — Black Tornado: a storm of blades swallows the field and slams down
    /// Plus rarity: the rarer the slayer, the bigger and flashier every special and strong attack looks.
    /// </summary>
    public partial class PlayerCharacter
    {
        /// <summary>Effect scale by rarity: Common 0.8 → Mythic 1.4.</summary>
        public float RarityFx { get { return 0.8f + 0.15f * Mathf.Clamp((Owned != null ? Owned.stars : 2) - 2, 0, 4); } }

        /// <summary>True when this slayer's special is one of the set pieces (they don't need the generic extras).</summary>
        bool HasSignature
        {
            get
            {
                switch (Def.id)
                {
                    case "kiba_initiate": case "hana_healer": return false;
                    default: return SignatureIds.Contains(Def.id) || NewcomerIds.Contains(Def.id) || UsesComposedSpecial;
                }
            }
        }

        static readonly HashSet<string> SignatureIds = new HashSet<string>
        {
            "ren_initiate", "ren_sundance", "homura_pillar", "homura_lastflame", "sora_initiate", "raiga_pillar", "tetsu_guard",
            "mina_ember", "rokuro_hunter", "genji_ronin", "yui_tide", "kuroe_moon", "seren_starfall", "garou_onyx"
        };

        /// <summary>Drags a demon toward a point (a knockback aimed inward).</summary>
        void PullToward(Combatant f, Vector3 center, AttackTag tag, DamageTally tally)
        {
            if (f == null || !f.IsAlive) return;
            Vector3 away = f.Position - center;
            away.y = 0f;
            if (away.sqrMagnitude < 0.25f) { tally.Add(CombatSystem.ApplyHit(this, f, tag, f.Position + Vector3.forward)); return; }
            tally.Add(CombatSystem.ApplyHit(this, f, tag, f.Position + away.normalized * 3f));
        }

        static GameObject Glow(Transform parent, Vector3 pos, float size, Color c, float alpha)
        {
            var go = new GameObject("Glow");
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), go.transform, Vector3.zero, Vector3.one * size, MaterialFactory.Additive(new Color(c.r, c.g, c.b, alpha)), false);
            return go;
        }

        // ------------------------------------------------------------------ Sun Wheel (Ren, Dawn Mark)

        IEnumerator SunWheel(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var gold = new Color(1f, 0.82f, 0.3f);
            FaceTo(focus);
            WideShot(focus, 3f);
            // A sun rises behind him.
            var sun = new GameObject("RisingSun");
            sun.transform.position = Position - transform.forward * 2f + Vector3.up * 1f;
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), sun.transform, Vector3.zero, Vector3.one, MaterialFactory.Toon(new Color(1f, 0.95f, 0.7f), 0f, gold), false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), sun.transform, Vector3.zero, Vector3.one * 1.8f, MaterialFactory.Additive(new Color(1f, 0.7f, 0.2f, 0.4f)), false);
            var halo = MeshFactory.MeshObject(MeshFactory.Ring(0.8f), sun.transform, Vector3.zero, Vector3.one * 1.6f, MaterialFactory.Additive(new Color(1f, 0.85f, 0.4f, 0.8f)), false);
            halo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            halo.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 180f, 0f);
            float t = 0f;
            Vector3 sunFrom = sun.transform.position, sunTo = Position + Vector3.up * 7f;
            if (Audio != null) { Audio.Play("el_light", 1f); Audio.PlayPitched("charge", 0.6f, 1.2f); }
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.6f);
                sun.transform.position = Vector3.Lerp(sunFrom, sunTo, k);
                sun.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.4f * fx, k);
                yield return null;
            }
            // Twelve dashes around the demons, drawing a twelve-point star (each dash skips five points).
            float r = 4.2f * fx;
            var cut = tag;
            cut.multiplier *= 0.35f;
            cut.hitStop = 0.01f;
            int idx = 0;
            Vector3 cur = focus + Quaternion.Euler(0f, 0f, 0f) * Vector3.forward * r;
            MoveTo(cur);
            for (int i = 0; i < 12; i++)
            {
                idx = (idx + 5) % 12;
                Vector3 next = BattleController.ClampToArena(focus + Quaternion.Euler(0f, idx * 30f, 0f) * Vector3.forward * r);
                Vector3 d = next - cur;
                if (d.sqrMagnitude < 0.01f) d = transform.forward;
                FaceTo(next);
                Visual.DashAttack(0.05f);
                VFX.Flash(MeshFactory.Line(), cur + Vector3.up * 0.9f, Quaternion.LookRotation(d), new Vector3(0.5f * fx, 1f, d.magnitude), new Vector3(0.05f, 1f, d.magnitude), gold, 0.9f);
                MoveTo(next);
                tally.Add(CombatSystem.HitRadius(this, Vector3.Lerp(cur, next, 0.5f), 2.2f, cut));
                ElementFx.Slash(Vector3.Lerp(cur, next, 0.5f), d, 2f, 180f, Random.Range(-40f, 40f), Element.Light, 1.2f);
                if (Audio != null) Audio.PlayPitched("slash", 0.5f, 1.1f + i * 0.04f);
                cur = next;
                yield return new WaitForSeconds(0.05f);
            }
            // The sun falls onto the centre of the star as a pillar of light.
            t = 0f;
            Vector3 hi = sun.transform.position;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                sun.transform.position = Vector3.Lerp(hi, focus + Vector3.up * 0.5f, t / 0.25f);
                yield return null;
            }
            Destroy(sun);
            var blast = tag;
            blast.multiplier *= 2.4f;
            blast.heavy = true;
            blast.launch = true;
            tally.Add(CombatSystem.HitRadius(this, focus, r + 1f, blast));
            for (int i = 0; i < 3; i++) VFX.Shockwave(focus, (3f + i * 2.5f) * fx, i == 1 ? Color.white : gold, 0.4f + i * 0.15f);
            VFX.Pillar(focus, gold, 16f, 0.9f);
            VFX.BurstDisc(focus, r, new Color(1f, 0.95f, 0.7f), 0.5f);
            for (int i = 0; i < 12; i++) VFX.Pillar(focus + Quaternion.Euler(0f, i * 30f, 0f) * Vector3.forward * r, gold, 6f, 0.6f);
            VFX.ImpactLight(focus + Vector3.up * 3f, gold, 20f, 0.6f);
            if (Audio != null) { Audio.Play("bossSlam", 1f); Audio.Play("el_light", 1f); }
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.8f);
            FollowAgain();
        }

        // ------------------------------------------------------------------ Phoenix Dive (Homura)

        IEnumerator PhoenixDive(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var fire = new Color(1f, 0.45f, 0.1f);
            FaceTo(focus);
            WideShot(focus, 2.8f);
            // Wrap him in a firebird: a burning body, two great flapping wings and a long tail.
            var bird = new GameObject("Phoenix").transform;
            bird.SetParent(Visual.transform, false);
            bird.localPosition = Vector3.up * 1f;
            var flame = MaterialFactory.Additive(new Color(1f, 0.55f, 0.15f, 0.7f));
            var hot = MaterialFactory.Additive(new Color(1f, 0.9f, 0.4f, 0.6f));
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), bird, Vector3.zero, new Vector3(1.4f, 1.2f, 2.2f) * fx, flame, false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), bird, new Vector3(0f, 0.3f, 1.2f) * fx, Vector3.one * 0.8f * fx, hot, false);
            var wings = new List<Transform>();
            for (int s = -1; s <= 1; s += 2)
            {
                var pivot = new GameObject("Wing").transform;
                pivot.SetParent(bird, false);
                pivot.localPosition = new Vector3(0.4f * s, 0.2f, 0f) * fx;
                var w = MeshFactory.MeshObject(MeshFactory.Sector(80f, 0.1f), pivot, Vector3.zero, new Vector3(3.2f, 1f, 3.2f) * fx, flame, false);
                w.transform.localRotation = Quaternion.Euler(0f, 90f * s, 0f);
                wings.Add(pivot);
            }
            var tail = bird.gameObject.AddComponent<TrailRenderer>();
            tail.time = 0.5f;
            tail.startWidth = 1.6f * fx;
            tail.endWidth = 0f;
            tail.material = MaterialFactory.Additive(new Color(1f, 0.5f, 0.1f, 0.8f));
            if (Audio != null) { Audio.PlayPitched("roar", 0.8f, 1.6f); Audio.Play("el_flame", 1f); }

            var burn = tag;
            burn.multiplier *= 0.5f;
            burn.hitStop = 0.01f;
            burn.launch = true;
            Vector3 dir = focus - Position;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            // Two passes that cross over the target: an X of fire.
            Vector3[,] passes =
            {
                { focus - dir * 6f - side * 4f, focus + dir * 6f + side * 4f },
                { focus + dir * 6f - side * 4f, focus - dir * 6f + side * 4f }
            };
            for (int p = 0; p < 2; p++)
            {
                Vector3 a = BattleController.ClampToArena(passes[p, 0]), b = BattleController.ClampToArena(passes[p, 1]);
                // Rise, then dive along the line.
                MoveTo(a);
                FaceTo(b);
                float t = 0f, dur = 0.42f;
                var hitSet = new HashSet<Combatant>();
                Vector3 lastFire = a;
                while (t < dur)
                {
                    t += Time.deltaTime;
                    float k = t / dur;
                    MoveTo(Vector3.Lerp(a, b, k));
                    Visual.transform.localPosition = Vector3.up * (Mathf.Sin(k * Mathf.PI) * 2.2f + 0.3f);
                    foreach (var wg in wings) wg.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 30f) * 35f * (wg.localPosition.x > 0 ? 1f : -1f));
                    if ((Position - lastFire).sqrMagnitude > 1.2f)
                    {
                        lastFire = Position;
                        VFX.Pillar(Position, fire, 3.5f * fx, 0.6f);
                        VFX.Breath(Position, new Color(1f, 0.35f, 0.05f), 8);
                        foreach (var f in CombatSystem.Query(this, Position, dir, 2.4f * fx, 360f))
                            if (hitSet.Add(f)) tally.Add(CombatSystem.ApplyHit(this, f, burn, Position));
                    }
                    yield return null;
                }
                Visual.transform.localPosition = Vector3.zero;
                if (Audio != null) Audio.PlayPitched("whoosh", 1f, 0.7f);
                yield return new WaitForSeconds(0.08f);
            }
            Destroy(bird.gameObject);
            // The wings explode over the crossing.
            var blast = tag;
            blast.multiplier *= 2.2f;
            blast.heavy = true;
            blast.launch = true;
            tally.Add(CombatSystem.HitRadius(this, focus, 5f * fx, blast));
            ElementFx.Finisher(focus, dir, Element.Flame, 5.5f * fx);
            for (int i = 0; i < 10; i++)
            {
                Vector3 o = focus + Quaternion.Euler(0f, i * 36f, 0f) * Vector3.forward * 3f * fx;
                VFX.Pillar(o, fire, 5f, 0.7f);
            }
            VFX.Shockwave(focus, 7f * fx, fire, 0.6f);
            VFX.ImpactLight(focus + Vector3.up * 2f, fire, 18f, 0.5f);
            if (Audio != null) Audio.Play("bossSlam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.8f);
            FollowAgain();
        }

        // ------------------------------------------------------------------ Eclipse (Kuroe)

        IEnumerator Eclipse(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var crimson = new Color(1f, 0.15f, 0.3f);
            FaceTo(focus);
            WideShot(focus, 3.4f);
            SceneLighting.UltimateMood(new Color(0.6f, 0.05f, 0.15f), 3.5f);
            // A blood moon rises over the demons...
            var sky = new GameObject("Eclipse").transform;
            sky.position = focus + Vector3.up * 8f + Vector3.forward * 2f;
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), sky, Vector3.zero, Vector3.one * 3.2f * fx, MaterialFactory.Toon(new Color(1f, 0.3f, 0.3f), 0f, crimson), false);
            var corona = MeshFactory.MeshObject(MeshFactory.Ring(0.7f), sky, Vector3.zero, Vector3.one * 2.8f * fx, MaterialFactory.Additive(new Color(1f, 0.3f, 0.35f, 0.9f)), false);
            corona.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            corona.AddComponent<Pulse>().Speed = 6f;
            var shadow = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), sky, new Vector3(-4f, 0f, -0.4f) * fx, Vector3.one * 3.25f * fx, MaterialFactory.Toon(new Color(0.02f, 0f, 0.03f), 0f), false).transform;
            if (Audio != null) { Audio.Play("el_dark", 1f); Audio.PlayPitched("buildup", 0.6f, 0.7f); }
            // ...and darkness slides across it.
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.deltaTime;
                shadow.localPosition = Vector3.Lerp(new Vector3(-4f, 0f, -0.4f), new Vector3(0f, 0f, -0.4f), Mathf.SmoothStep(0f, 1f, t / 0.7f)) * fx;
                yield return null;
            }
            VFX.ImpactLight(sky.position, crimson, 30f, 0.4f);
            // A vortex opens under the demons and drags them in.
            var vortex = new GameObject("Vortex").transform;
            vortex.position = focus + Vector3.up * 0.06f;
            for (int i = 0; i < 3; i++)
            {
                var ring = MeshFactory.MeshObject(MeshFactory.Sector(250f, 0.75f), vortex, Vector3.up * (0.01f * i), Vector3.one * (2.5f + i * 2f) * fx, MaterialFactory.Additive(new Color(0.6f, 0.05f, 0.25f, 0.7f - i * 0.15f)), false);
                ring.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, (i % 2 == 0 ? 1f : -1f) * (360f - i * 80f), 0f);
            }
            var pull = tag;
            pull.multiplier *= 0.15f;
            pull.knockback = 5f;
            pull.hitStop = 0f;
            for (int i = 0; i < 6; i++)
            {
                foreach (var f in Foes(focus, 9f * fx)) PullToward(f, focus, pull, tally);
                VFX.Smoke(focus + Random.insideUnitSphere * 2f, new Color(0.25f, 0.02f, 0.12f, 0.7f), 6);
                yield return new WaitForSeconds(0.12f);
            }
            // Crescents spiral out from the centre, three turns.
            var crescent = tag;
            crescent.multiplier *= 0.4f;
            crescent.hitStop = 0.01f;
            for (int i = 0; i < 18; i++)
            {
                float a = i * 60f;
                float rr = (1f + i * 0.35f) * fx;
                Vector3 p = focus + Quaternion.Euler(0f, a, 0f) * Vector3.forward * rr;
                ElementFx.Slash(p, Quaternion.Euler(0f, a + 90f, 0f) * Vector3.forward, 2.2f * fx, 220f, 70f, Element.Dark, 1.4f);
                tally.Add(CombatSystem.HitRadius(this, p, 1.8f * fx, crescent));
                if (Audio != null && i % 2 == 0) Audio.PlayPitched("slash", 0.5f, 0.8f + i * 0.03f);
                yield return new WaitForSeconds(0.035f);
            }
            // The eclipse collapses: a black flash, then a crimson burst.
            Destroy(sky.gameObject);
            Destroy(vortex.gameObject);
            VFX.BurstDisc(focus, 6f * fx, new Color(0.05f, 0f, 0.05f), 0.25f);
            yield return new WaitForSeconds(0.12f);
            var burst = tag;
            burst.multiplier *= 2.6f;
            burst.heavy = true;
            burst.launch = true;
            tally.Add(CombatSystem.HitRadius(this, focus, 7f * fx, burst));
            ElementFx.Finisher(focus, transform.forward, Element.Dark, 6.5f * fx);
            VFX.Shockwave(focus, 8f * fx, crimson, 0.6f);
            VFX.Shockwave(focus, 5f * fx, Color.white, 0.3f);
            if (Audio != null) Audio.Play("bossSlam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.9f);
            FollowAgain();
        }

        // ------------------------------------------------------------------ Constellation (Seren)

        IEnumerator Constellation(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var star = new Color(1f, 0.95f, 0.7f);
            FaceTo(focus);
            WideShot(focus, 3f);
            Visual.Victory();
            // A star appears above every demon (and a few empty points to finish the shape).
            var foes = Foes(focus, 11f);
            if (foes.Count > 9) foes.RemoveRange(9, foes.Count - 9);
            var points = new List<Vector3>();
            foreach (var f in foes) if (f != null) points.Add(f.Position + Vector3.up * 5f);
            while (points.Count < 5)
                points.Add(focus + Quaternion.Euler(0f, points.Count * 72f, 0f) * Vector3.forward * 3.5f + Vector3.up * 5f);
            var stars = new List<GameObject>();
            for (int i = 0; i < points.Count; i++)
            {
                var s = Glow(null, points[i], 0.9f * fx, star, 0.8f);
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), s.transform, Vector3.zero, Vector3.one * 0.35f * fx, MaterialFactory.Toon(Color.white, 0f, star), false);
                s.AddComponent<Pulse>().Speed = 8f;
                stars.Add(s);
                VFX.HitSpark(points[i], star, 10);
                if (Audio != null) Audio.PlayPitched("coin", 0.35f, 0.8f + i * 0.12f);
                yield return new WaitForSeconds(0.08f);
            }
            // Lines of light join them into a constellation.
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 a = points[i], b = points[(i + 1) % points.Count];
                Vector3 d = b - a;
                if (d.sqrMagnitude < 0.01f) continue;
                VFX.Flash(MeshFactory.Line(), a, Quaternion.LookRotation(d), new Vector3(0.25f * fx, 1f, d.magnitude), new Vector3(0.08f, 1f, d.magnitude), star, 1.4f);
                yield return new WaitForSeconds(0.05f);
            }
            if (Audio != null) Audio.Play("el_light", 1f);
            yield return new WaitForSeconds(0.3f);
            // Every star falls at once as a beam.
            var fall = tag;
            fall.multiplier *= 1.6f;
            fall.heavy = true;
            fall.launch = true;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 ground = new Vector3(points[i].x, 0f, points[i].z);
                BoltFx.Strike(points[i] + Vector3.up * 8f, ground, star, 0.7f * fx, 0.5f, 0.05f);
                VFX.Pillar(ground, star, 10f, 0.6f);
                VFX.Shockwave(ground, 2.8f * fx, star, 0.4f);
                tally.Add(CombatSystem.HitRadius(this, ground, 2.6f * fx, fall));
                Destroy(stars[i]);
            }
            VFX.ImpactLight(focus + Vector3.up * 4f, star, 22f, 0.5f);
            if (Audio != null) Audio.Play("bossSlam", 0.9f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.6f);
            yield return new WaitForSeconds(0.2f);
            // Supernova in the middle.
            var nova = tag;
            nova.multiplier *= 1.4f;
            tally.Add(CombatSystem.HitRadius(this, focus, 6.5f * fx, nova));
            VFX.BurstDisc(focus, 6f * fx, Color.white, 0.4f);
            VFX.Shockwave(focus, 9f * fx, new Color(0.6f, 0.7f, 1f), 0.7f);
            ElementFx.Finisher(focus, transform.forward, Element.Light, 6f * fx);
            FollowAgain();
        }

        // ------------------------------------------------------------------ Black Tornado (Garou)

        IEnumerator BlackTornado(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var gale = new Color(0.35f, 0.9f, 0.7f);
            FaceTo(focus);
            WideShot(focus, 3.2f);
            // The storm forms: stacked spinning rings widening toward the sky, with debris orbiting inside.
            var storm = new GameObject("BlackTornado").transform;
            storm.position = Position + transform.forward * 2f;
            for (int i = 0; i < 9; i++)
            {
                float h = i * 0.9f;
                var ring = MeshFactory.MeshObject(MeshFactory.Sector(300f, 0.8f), storm, Vector3.up * h, Vector3.one * (1f + i * 0.45f) * fx,
                    MaterialFactory.Additive(i % 2 == 0 ? new Color(0.1f, 0.25f, 0.2f, 0.75f) : new Color(gale.r, gale.g, gale.b, 0.45f)), false);
                ring.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 540f + i * 40f, 0f);
            }
            var rock = MaterialFactory.Toon(new Color(0.25f, 0.24f, 0.22f), 0.02f);
            for (int i = 0; i < 10; i++)
            {
                var pivot = new GameObject("Debris").transform;
                pivot.SetParent(storm, false);
                pivot.localPosition = Vector3.up * Random.Range(0.5f, 6f);
                MeshFactory.Primitive(PrimitiveType.Cube, pivot, Vector3.forward * Random.Range(1f, 2.6f) * fx, Vector3.one * Random.Range(0.15f, 0.4f), rock);
                pivot.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, Random.Range(300f, 600f), 0f);
            }
            if (Audio != null) { Audio.Play("el_wind", 1f); Audio.PlayPitched("roar", 0.7f, 0.7f); }
            // It sweeps through the demons, dragging them in and throwing them around, while Garou spins inside.
            var churn = tag;
            churn.multiplier *= 0.3f;
            churn.knockback = 4f;
            churn.hitStop = 0f;
            Vector3 from = storm.position;
            var foes = Foes(focus, 10f);
            Vector3 to = foes.Count > 0 && foes[foes.Count / 2] != null ? foes[foes.Count / 2].Position : focus;
            float t = 0f, dur = 1.6f, tick = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                tick -= Time.deltaTime;
                float k = t / dur;
                Vector3 p = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, k)) + Vector3.Cross(Vector3.up, (to - from).normalized) * Mathf.Sin(k * Mathf.PI * 2f) * 2f;
                storm.position = BattleController.ClampToArena(p);
                MoveTo(storm.position);
                if (tick <= 0f)
                {
                    tick = 0.16f;
                    Visual.Spin(0.16f);
                    foreach (var f in Foes(storm.position, 5.5f * fx)) PullToward(f, storm.position, churn, tally);
                    ElementFx.Slash(storm.position, Random.insideUnitSphere, 2.8f * fx, 300f, Random.Range(-60f, 60f), Element.Beast, 1.2f);
                    VFX.Dust(storm.position, 6);
                    if (Audio != null) Audio.PlayPitched("slash", 0.45f, Random.Range(0.9f, 1.2f));
                }
                yield return null;
            }
            // He rides it up... and slams it down.
            t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                Visual.transform.localPosition = Vector3.up * Mathf.Lerp(0f, 5f, t / 0.25f);
                storm.localScale = Vector3.one * Mathf.Lerp(1f, 0.3f, t / 0.25f);
                yield return null;
            }
            Destroy(storm.gameObject);
            Visual.transform.localPosition = Vector3.zero;
            Visual.HeavyAttack(0.1f);
            var slam = tag;
            slam.multiplier *= 2.6f;
            slam.heavy = true;
            slam.launch = true;
            tally.Add(CombatSystem.HitRadius(this, Position, 6f * fx, slam));
            ElementFx.Finisher(Position, transform.forward, Element.Beast, 6f * fx);
            VFX.Shockwave(Position, 7.5f * fx, gale, 0.5f);
            VFX.Dust(Position, 30);
            for (int i = 0; i < 8; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                VFX.Flash(MeshFactory.Line(), Position + Vector3.up * 0.05f, Quaternion.LookRotation(d), new Vector3(0.4f, 1f, 0.5f), new Vector3(0.06f, 1f, 5f * fx), new Color(0.15f, 0.2f, 0.18f), 0.7f);
            }
            if (Audio != null) Audio.Play("bossSlam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.9f);
            FollowAgain();
        }
    }
}
