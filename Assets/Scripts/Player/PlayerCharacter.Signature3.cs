using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Specials for the newer slayers, each its own set piece:
    ///   Taro — Boulder Toss            Nami — Bubble Barrage          Koji — Ember Spin            Yuna — Gale Arrow
    ///   Daigo — Landslide              Hotaru — Firefly Swarm         Kenta — Lightning Lance      Rin — Shadow Snare
    ///   Akane — Firework Finale        Tsukasa — Chain Storm Volley   Mizuki — Maelstrom Waltz
    /// </summary>
    public partial class PlayerCharacter
    {
        IEnumerator NewcomerSpecial(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            switch (Def.id)
            {
                case "taro_rock": return BoulderToss(tag, tally, focus);
                case "nami_bubble": return BubbleBarrage(tag, tally, focus);
                case "koji_ember": return EmberSpin(tag, tally);
                case "yuna_gale": return GaleArrow(tag, tally, focus);
                case "daigo_stone": return Landslide(tag, tally, focus);
                case "hotaru_light": return FireflySwarm(tag, tally, focus);
                case "kenta_spear": return LightningLance(tag, tally, focus);
                case "rin_shadow": return ShadowSnare(tag, tally, focus);
                case "akane_fire": return FireworkFinale(tag, tally, focus);
                case "tsukasa_storm": return ChainStorm(tag, tally, focus);
                case "kaito_gale": return CrosswindCyclone(tag, tally, focus);
                case "oboro_iron": return Sunbreaker(tag, tally, focus);
                case "shion_storm": return HeavenlyThunderArray(tag, tally, focus);
                default: return MaelstromWaltz(tag, tally);
            }
        }

        static readonly HashSet<string> NewcomerIds = new HashSet<string>
        {
            "taro_rock", "nami_bubble", "koji_ember", "yuna_gale", "daigo_stone", "hotaru_light", "kenta_spear", "rin_shadow", "akane_fire", "tsukasa_storm", "mizuki_tide",
            "kaito_gale", "oboro_iron", "shion_storm"
        };

        Vector3 DirTo(Vector3 focus)
        {
            Vector3 d = focus - Position;
            d.y = 0f;
            return d.sqrMagnitude > 0.01f ? d.normalized : transform.forward;
        }

        /// <summary>Moves an object from a to b along an arc of the given height.</summary>
        static IEnumerator Arc(Transform t, Vector3 a, Vector3 b, float height, float dur)
        {
            float e = 0f;
            while (e < dur && t != null)
            {
                e += Time.deltaTime;
                float k = Mathf.Clamp01(e / dur);
                t.position = Vector3.Lerp(a, b, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * height;
                yield return null;
            }
        }

        // ------------------------------------------------------------------ Common

        IEnumerator BoulderToss(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            FaceTo(focus);
            // Rip a boulder out of the ground and heave it overhead...
            VFX.Dust(Position + transform.forward, 16);
            var rock = new GameObject("Boulder").transform;
            MeshFactory.MeshObject(MeshFactory.Rock(3), rock, Vector3.zero, Vector3.one * 2.2f * fx, MaterialFactory.Toon(new Color(0.5f, 0.45f, 0.4f), 0.03f));
            Vector3 over = Position + Vector3.up * 3.2f;
            yield return Arc(rock, Position + transform.forward, over, 0.5f, 0.35f);
            Visual.HeavyAttack(0.15f);
            if (Audio != null) Audio.PlayPitched("whoosh", 1f, 0.6f);
            // ...and throw it.
            yield return Arc(rock, over, focus, 3f, 0.45f);
            Destroy(rock.gameObject);
            var hit = tag;
            hit.multiplier *= 2.6f;
            hit.heavy = true;
            hit.launch = true;
            tally.Add(CombatSystem.HitRadius(this, focus, 4.5f * fx, hit));
            VFX.Shockwave(focus, 5f * fx, new Color(0.7f, 0.6f, 0.45f), 0.5f);
            VFX.Dust(focus, 30);
            for (int i = 0; i < 6; i++) VFX.Flash(MeshFactory.Line(), focus + Vector3.up * 0.05f, Quaternion.Euler(0f, i * 60f, 0f), new Vector3(0.3f, 1f, 0.5f), new Vector3(0.05f, 1f, 3.5f * fx), new Color(0.2f, 0.15f, 0.1f), 0.8f);
            if (Audio != null) Audio.Play("bossSlam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.7f);
        }

        IEnumerator BubbleBarrage(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            Visual.Spin(0.4f, 2);
            var foes = Foes(focus, 9f);
            var pop = tag;
            pop.multiplier *= 0.5f;
            pop.hitStop = 0.01f;
            var mat = MaterialFactory.Additive(new Color(0.55f, 0.85f, 1f, 0.55f));
            int n = 14;
            var bubbles = new List<Transform>();
            var targets = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                var b = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), null, Position + Vector3.up * 1.2f, Vector3.one * Random.Range(0.6f, 1.1f) * fx, mat, false).transform;
                bubbles.Add(b);
                targets.Add(i < foes.Count && foes[i] != null ? foes[i].Position + Vector3.up * 0.8f : focus + new Vector3(Random.Range(-5f, 5f), 0.8f, Random.Range(-5f, 5f)));
            }
            if (Audio != null) Audio.Play("el_water", 1f);
            // Bubbles drift out on wobbly paths...
            float e = 0f;
            while (e < 0.9f)
            {
                e += Time.deltaTime;
                float k = e / 0.9f;
                for (int i = 0; i < n; i++)
                    bubbles[i].position = Vector3.Lerp(Position + Vector3.up * 1.2f, targets[i], k) + new Vector3(Mathf.Sin(e * 8f + i) * 0.4f, Mathf.Sin(k * Mathf.PI) * 1.5f, Mathf.Cos(e * 7f + i) * 0.4f);
                yield return null;
            }
            // ...and pop one after another.
            for (int i = 0; i < n; i++)
            {
                Vector3 p = bubbles[i].position;
                Destroy(bubbles[i].gameObject);
                VFX.Shockwave(new Vector3(p.x, 0f, p.z), 1.8f * fx, new Color(0.6f, 0.9f, 1f), 0.3f);
                VFX.HitSpark(p, new Color(0.7f, 0.95f, 1f), 10);
                tally.Add(CombatSystem.HitRadius(this, new Vector3(p.x, 0f, p.z), 1.8f * fx, pop));
                if (Audio != null) Audio.PlayPitched("coin", 0.3f, 1.4f + i * 0.04f);
                yield return new WaitForSeconds(0.05f);
            }
        }

        IEnumerator EmberSpin(AttackTag tag, DamageTally tally)
        {
            float fx = RarityFx;
            var fire = new Color(1f, 0.45f, 0.1f);
            var spin = tag;
            spin.multiplier *= 0.35f;
            spin.hitStop = 0.01f;
            if (Audio != null) Audio.Play("el_flame", 1f);
            for (int i = 0; i < 9; i++)
            {
                Visual.Spin(0.14f);
                float r = (1.5f + i * 0.35f) * fx;
                for (int k = 0; k < 6; k++) VFX.Pillar(Position + Quaternion.Euler(0f, k * 60f + i * 20f, 0f) * Vector3.forward * r, fire, 3f + i * 0.3f, 0.35f);
                VFX.Slash(Position, transform.forward, r, 340f, i * 15f, fire, 0.2f);
                tally.Add(CombatSystem.HitRadius(this, Position, r + 0.8f, spin));
                if (Audio != null) Audio.PlayPitched("slash", 0.45f, 1f + i * 0.05f);
                yield return new WaitForSeconds(0.13f);
            }
            var burst = tag;
            burst.multiplier *= 1.6f;
            burst.launch = true;
            tally.Add(CombatSystem.HitRadius(this, Position, 5f * fx, burst));
            ElementFx.Finisher(Position, transform.forward, Element.Flame, 5f * fx);
        }

        IEnumerator GaleArrow(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var wind = new Color(0.6f, 1f, 0.8f);
            Vector3 dir = DirTo(focus);
            Face(dir, 1f);
            // Draw — the wind gathers on the arrow.
            for (int i = 0; i < 6; i++) { VFX.Breath(Position + dir * 0.8f + Vector3.up * 1.2f, wind, 5); yield return new WaitForSeconds(0.06f); }
            if (Audio != null) { Audio.PlayPitched("shoot", 1f, 0.6f); Audio.Play("el_wind", 1f); }
            // Loose: one huge arrow tearing a tunnel of wind through the line.
            var arrow = MeshFactory.MeshObject(MeshFactory.Cone(), null, Position + Vector3.up * 1.2f, new Vector3(0.9f, 2.4f, 0.9f) * fx, MaterialFactory.Additive(new Color(0.7f, 1f, 0.85f, 0.8f)), false).transform;
            arrow.rotation = Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f);
            var hitSet = new HashSet<Combatant>();
            var hit = tag;
            hit.multiplier *= 2.4f;
            hit.knockback = 10f;
            float len = 18f, e = 0f;
            Vector3 start = Position + Vector3.up * 1.2f;
            while (e < 0.4f)
            {
                e += Time.deltaTime;
                Vector3 p = start + dir * len * (e / 0.4f);
                arrow.position = p;
                VFX.Flash(MeshFactory.Ring(0.8f), p, Quaternion.LookRotation(Vector3.up, dir), Vector3.one * 0.5f, Vector3.one * 2.2f * fx, wind, 0.3f);
                foreach (var f in CombatSystem.Query(this, new Vector3(p.x, 0f, p.z), dir, 2.5f * fx, 360f))
                    if (hitSet.Add(f)) tally.Add(CombatSystem.ApplyHit(this, f, hit, new Vector3(p.x, 0f, p.z) - dir));
                yield return null;
            }
            Destroy(arrow.gameObject);
            ElementFx.Finisher(Position + dir * len, dir, Element.Beast, 3.5f * fx);
        }

        // ------------------------------------------------------------------ Rare

        IEnumerator Landslide(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            Vector3 dir = DirTo(focus);
            Face(dir, 1f);
            Visual.HeavyAttack(0.15f);
            VFX.Shockwave(Position, 3f, new Color(0.6f, 0.5f, 0.4f), 0.4f);
            if (Audio != null) Audio.Play("bossSlam", 0.8f);
            yield return new WaitForSeconds(0.15f);
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            var rockMat = MaterialFactory.Toon(new Color(0.45f, 0.4f, 0.35f), 0.03f);
            var roll = tag;
            roll.multiplier *= 0.9f;
            roll.launch = true;
            roll.knockback = 8f;
            var rocks = new List<Transform>();
            for (int i = 0; i < 5; i++)
            {
                var r = MeshFactory.MeshObject(MeshFactory.Rock(i), null, Position + side * (i - 2) * 1.8f + Vector3.up * 1f, Vector3.one * 1.6f * fx, rockMat).transform;
                rocks.Add(r);
            }
            var hitSets = new List<HashSet<Combatant>>();
            for (int i = 0; i < rocks.Count; i++) hitSets.Add(new HashSet<Combatant>());
            float e = 0f;
            while (e < 1f)
            {
                e += Time.deltaTime;
                for (int i = 0; i < rocks.Count; i++)
                {
                    var r = rocks[i];
                    r.position += dir * 14f * Time.deltaTime;
                    r.Rotate(side, 400f * Time.deltaTime, Space.World);
                    Vector3 g = new Vector3(r.position.x, 0f, r.position.z);
                    if (Random.value < 0.2f) VFX.Dust(g, 3);
                    foreach (var f in CombatSystem.Query(this, g, dir, 1.6f * fx, 360f))
                        if (hitSets[i].Add(f)) tally.Add(CombatSystem.ApplyHit(this, f, roll, g - dir));
                }
                if (CameraController.Instance != null) CameraController.Instance.Shake(0.08f);
                yield return null;
            }
            foreach (var r in rocks) { VFX.Dust(r.position, 12); Destroy(r.gameObject); }
            if (Audio != null) Audio.Play("impact", 0.8f);
        }

        IEnumerator FireflySwarm(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var glow = new Color(1f, 0.95f, 0.5f);
            Visual.Victory();
            var mat = MaterialFactory.Additive(new Color(1f, 0.95f, 0.5f, 0.9f));
            int n = 30;
            var flies = new List<Transform>();
            for (int i = 0; i < n; i++) flies.Add(MeshFactory.MeshObject(MeshFactory.SmoothSphere(), null, Position, Vector3.one * 0.22f * fx, mat, false).transform);
            if (Audio != null) Audio.Play("el_light", 1f);
            // They swirl around her...
            float e = 0f;
            while (e < 0.9f)
            {
                e += Time.deltaTime;
                for (int i = 0; i < n; i++)
                    flies[i].position = Position + Quaternion.Euler(0f, i * 12f + e * 300f, 0f) * Vector3.forward * (1.2f + (i % 3) * 0.5f) + Vector3.up * (0.8f + Mathf.Sin(e * 6f + i) * 0.6f);
                yield return null;
            }
            // ...heal every friend...
            var battle = BattleController.Current;
            if (battle != null && battle.Team != null)
                foreach (var m in battle.Team.Members)
                    if (m != null && m.IsAlive) { m.Health.Heal(m.Health.Max * 0.3f); if (m.gameObject.activeInHierarchy) VFX.Breath(m.Position, new Color(0.6f, 1f, 0.6f), 20); }
            // ...then dive at the demons.
            var foes = Foes(focus, 10f);
            var sting = tag;
            sting.multiplier *= 0.35f;
            sting.hitStop = 0f;
            for (int i = 0; i < n; i++)
            {
                Vector3 to = foes.Count > 0 && foes[i % foes.Count] != null ? foes[i % foes.Count].Position + Vector3.up : focus + Random.insideUnitSphere * 3f;
                StartCoroutine(Arc(flies[i], flies[i].position, to, 1f, 0.35f));
            }
            yield return new WaitForSeconds(0.36f);
            for (int i = 0; i < n; i++)
            {
                Vector3 p = new Vector3(flies[i].position.x, 0f, flies[i].position.z);
                VFX.HitSpark(flies[i].position, glow, 6);
                if (i % 2 == 0) tally.Add(CombatSystem.HitRadius(this, p, 1.5f, sting));
                Destroy(flies[i].gameObject);
            }
            VFX.ImpactLight(focus + Vector3.up * 2f, glow, 14f, 0.4f);
        }

        IEnumerator LightningLance(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var bolt = new Color(1f, 0.95f, 0.45f);
            Vector3 dir = DirTo(focus);
            Face(dir, 1f);
            // Hop back and wind up the throw.
            Vector3 back = BattleController.ClampToArena(Position - dir * 2f);
            float e = 0f;
            Vector3 from = Position;
            while (e < 0.2f) { e += Time.deltaTime; transform.position = Vector3.Lerp(from, back, e / 0.2f); Visual.transform.localPosition = Vector3.up * Mathf.Sin(e / 0.2f * Mathf.PI) * 1f; yield return null; }
            Visual.transform.localPosition = Vector3.zero;
            for (int i = 0; i < 4; i++) BoltFx.Strike(Position + Vector3.up * 2.5f + Random.onUnitSphere, Position + Vector3.up * 1.6f, bolt, 0.12f, 0.15f);
            Visual.Attack(2, 0.08f);
            if (Audio != null) { Audio.PlayPitched("whoosh", 1f, 1.3f); Audio.Play("el_thunder", 1f); }
            // The spear flies as a bolt, piercing everything in the line.
            var spear = MeshFactory.Primitive(PrimitiveType.Cube, null, Position + Vector3.up * 1.4f, new Vector3(0.12f, 0.12f, 2.6f) * fx, MaterialFactory.Toon(Color.white, 0f, bolt)).transform;
            spear.rotation = Quaternion.LookRotation(dir);
            var hitSet = new HashSet<Combatant>();
            var pierce = tag;
            pierce.multiplier *= 1.6f;
            float len = 16f;
            Vector3 s0 = Position + Vector3.up * 1.4f, prev = s0;
            e = 0f;
            while (e < 0.3f)
            {
                e += Time.deltaTime;
                Vector3 p = s0 + dir * len * (e / 0.3f);
                spear.position = p;
                BoltFx.Strike(prev, p, bolt, 0.3f * fx, 0.2f, 0.3f);
                prev = p;
                foreach (var f in CombatSystem.Query(this, new Vector3(p.x, 0f, p.z), dir, 2f, 360f))
                    if (hitSet.Add(f)) tally.Add(CombatSystem.ApplyHit(this, f, pierce, new Vector3(p.x, 0f, p.z) - dir));
                yield return null;
            }
            Vector3 end = new Vector3(spear.position.x, 0f, spear.position.z);
            Destroy(spear.gameObject);
            // It lands and explodes into a star of lightning.
            var blast = tag;
            blast.multiplier *= 1.8f;
            blast.launch = true;
            tally.Add(CombatSystem.HitRadius(this, end, 4.5f * fx, blast));
            for (int i = 0; i < 8; i++) BoltFx.Strike(end + Vector3.up * 0.3f, end + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 5f * fx, bolt, 0.2f, 0.35f, 0.5f);
            BoltFx.Strike(end + Vector3.up * 15f, end, bolt, 0.6f, 0.4f, 0.3f);
            ElementFx.Finisher(end, dir, Element.Thunder, 4.5f * fx);
            if (Audio != null) Audio.Play("bossSlam", 0.9f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.6f);
        }

        IEnumerator ShadowSnare(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var shade = new Color(0.5f, 0.25f, 0.8f);
            Visual.Swing(40f, -40f, 0.3f, 10f);
            if (Audio != null) Audio.Play("el_dark", 1f);
            var foes = Foes(focus, 10f);
            if (foes.Count > 8) foes.RemoveRange(8, foes.Count - 8);
            var handMat = MaterialFactory.Toon(new Color(0.08f, 0.04f, 0.12f), 0.02f, new Color(0.25f, 0.08f, 0.35f));
            var hands = new List<Transform>();
            var pools = new List<GameObject>();
            var spots = new List<Vector3>();
            foreach (var f in foes) if (f != null) spots.Add(f.Position);
            if (spots.Count == 0) spots.Add(focus);
            // Shadow pools open, then hands claw up through them.
            foreach (var p in spots)
            {
                var pool = MeshFactory.MeshObject(MeshFactory.Disc(), null, p + Vector3.up * 0.04f, Vector3.one * 1.6f * fx, MaterialFactory.Transparent(new Color(0.05f, 0f, 0.1f, 0.8f)), false);
                pool.AddComponent<Pulse>().Speed = 6f;
                pools.Add(pool);
                var hand = new GameObject("ShadowHand").transform;
                hand.position = p - Vector3.up * 2f;
                MeshFactory.Primitive(PrimitiveType.Cylinder, hand, Vector3.up * 0.8f, new Vector3(0.5f, 0.8f, 0.5f) * fx, handMat);
                for (int k = 0; k < 4; k++)
                {
                    var finger = MeshFactory.MeshObject(MeshFactory.Cone(), hand, Quaternion.Euler(0f, k * 90f, 0f) * Vector3.forward * 0.35f * fx + Vector3.up * 1.6f * fx, new Vector3(0.18f, 0.9f, 0.18f) * fx, handMat);
                    finger.transform.localRotation = Quaternion.Euler(-25f, k * 90f, 0f);
                }
                hands.Add(hand);
            }
            float e = 0f;
            while (e < 0.35f) { e += Time.deltaTime; for (int i = 0; i < hands.Count; i++) hands[i].position = spots[i] - Vector3.up * 2f * (1f - e / 0.35f); yield return null; }
            var grab = tag;
            grab.multiplier *= 0.6f;
            grab.stagger = 10f;
            foreach (var p in spots) tally.Add(CombatSystem.HitRadius(this, p, 1.6f * fx, grab));
            yield return new WaitForSeconds(0.45f);
            // Crush.
            var crush = tag;
            crush.multiplier *= 1.8f;
            crush.heavy = true;
            for (int i = 0; i < hands.Count; i++)
            {
                hands[i].localScale = new Vector3(0.6f, 1.2f, 0.6f);
                VFX.Smoke(spots[i], new Color(0.2f, 0.05f, 0.3f, 0.8f), 14);
                VFX.Shockwave(spots[i], 2.2f * fx, shade, 0.35f);
                tally.Add(CombatSystem.HitRadius(this, spots[i], 1.8f * fx, crush));
            }
            if (Audio != null) Audio.Play("impact", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.5f);
            yield return new WaitForSeconds(0.25f);
            foreach (var h in hands) Destroy(h.gameObject);
            foreach (var pl in pools) Destroy(pl);
        }

        // ------------------------------------------------------------------ Legendary

        IEnumerator FireworkFinale(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            Color[] cols = { new Color(1f, 0.4f, 0.6f), new Color(1f, 0.85f, 0.3f), new Color(0.4f, 0.8f, 1f), new Color(0.6f, 1f, 0.5f), new Color(1f, 0.5f, 0.2f), new Color(0.8f, 0.5f, 1f) };
            WideShot(focus, 3f);
            Visual.Victory();
            var foes = Foes(focus, 10f);
            var spots = new List<Vector3>();
            for (int i = 0; i < 6; i++) spots.Add(i < foes.Count && foes[i] != null ? foes[i].Position : focus + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 4f);
            // Rockets launch one after another with trails...
            var rockets = new List<Transform>();
            for (int i = 0; i < spots.Count; i++)
            {
                var r = MeshFactory.MeshObject(MeshFactory.SmoothSphere(), null, Position + Vector3.up, Vector3.one * 0.35f, MaterialFactory.Additive(new Color(cols[i].r, cols[i].g, cols[i].b, 1f)), false).transform;
                var tr = r.gameObject.AddComponent<TrailRenderer>();
                tr.time = 0.35f; tr.startWidth = 0.35f; tr.endWidth = 0f;
                tr.material = MaterialFactory.Additive(new Color(cols[i].r, cols[i].g, cols[i].b, 0.8f));
                tr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                StartCoroutine(Arc(r, Position + Vector3.up, spots[i] + Vector3.up * 9f, 2f, 0.5f));
                rockets.Add(r);
                if (Audio != null) Audio.PlayPitched("whoosh", 0.6f, 1.6f);
                yield return new WaitForSeconds(0.08f);
            }
            yield return new WaitForSeconds(0.45f);
            // ...burst into rings of colour...
            for (int i = 0; i < rockets.Count; i++)
            {
                Vector3 p = rockets[i].position;
                Destroy(rockets[i].gameObject);
                VFX.Flash(MeshFactory.Ring(0.85f), p, Quaternion.LookRotation(Vector3.up, Vector3.forward) * Quaternion.Euler(90f, 0f, 0f), Vector3.one * 0.5f, Vector3.one * 5f * fx, cols[i], 0.6f);
                VFX.HitSpark(p, cols[i], 40);
                VFX.ImpactLight(p, cols[i], 16f, 0.3f);
                if (Audio != null) Audio.PlayPitched("impact", 0.6f, 1.4f + i * 0.05f);
                yield return new WaitForSeconds(0.06f);
            }
            // ...and the sparks rain down on the demons below.
            var rain = tag;
            rain.multiplier *= 1.1f;
            rain.launch = true;
            yield return new WaitForSeconds(0.2f);
            for (int i = 0; i < spots.Count; i++)
            {
                for (int k = 0; k < 5; k++) VFX.Pillar(spots[i] + new Vector3(Random.Range(-1.5f, 1.5f), 0f, Random.Range(-1.5f, 1.5f)), cols[i], 8f, 0.3f);
                tally.Add(CombatSystem.HitRadius(this, spots[i], 3f * fx, rain));
                VFX.Shockwave(spots[i], 3f * fx, cols[i], 0.4f);
            }
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.6f);
            FollowAgain();
        }

        IEnumerator ChainStorm(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var bolt = new Color(1f, 0.95f, 0.45f);
            var foes = Foes(Position, 14f);
            if (foes.Count > 9) foes.RemoveRange(9, foes.Count - 9);
            FaceTo(focus);
            Visual.Attack(2, 0.1f);
            // Arrows to every demon...
            var shot = tag;
            shot.multiplier *= 0.8f;
            foreach (var f in foes)
            {
                if (f == null) continue;
                Vector3 d = f.Position - Position;
                VFX.Flash(MeshFactory.Line(), Position + Vector3.up * 1.3f, Quaternion.LookRotation(d.sqrMagnitude > 0.01f ? d : transform.forward), new Vector3(0.4f, 1f, d.magnitude), new Vector3(0.05f, 1f, d.magnitude), bolt, 0.3f);
                tally.Add(CombatSystem.ApplyHit(this, f, shot, Position));
                if (Audio != null) Audio.PlayPitched("shoot", 0.5f, 1.3f);
                yield return new WaitForSeconds(0.05f);
            }
            yield return new WaitForSeconds(0.15f);
            // ...then lightning jumps from each to the next, twice around.
            var chain = tag;
            chain.multiplier *= 0.7f;
            chain.hitStop = 0.01f;
            for (int round = 0; round < 2; round++)
                for (int i = 0; i < foes.Count; i++)
                {
                    var a = foes[i];
                    var b = foes[(i + 1) % foes.Count];
                    if (a == null || b == null) continue;
                    BoltFx.Strike(a.Position + Vector3.up, b.Position + Vector3.up, bolt, 0.25f * fx, 0.25f, 0.5f);
                    if (b.IsAlive) tally.Add(CombatSystem.ApplyHit(this, b, chain, a.Position));
                    if (Audio != null && i % 2 == 0) Audio.Play("el_thunder", 0.5f);
                    yield return new WaitForSeconds(0.04f);
                }
            // A last bolt from the sky on each.
            var fin = tag;
            fin.multiplier *= 1.2f;
            fin.launch = true;
            foreach (var f in foes)
            {
                if (f == null || !f.IsAlive) continue;
                BoltFx.Strike(f.Position + Vector3.up * 16f, f.Position, bolt, 0.5f * fx, 0.4f, 0.4f);
                tally.Add(CombatSystem.ApplyHit(this, f, fin, f.Position + Vector3.up));
            }
            VFX.ImpactLight(Position + Vector3.up * 4f, bolt, 20f, 0.3f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.6f);
        }

        IEnumerator MaelstromWaltz(AttackTag tag, DamageTally tally)
        {
            float fx = RarityFx;
            var sea = new Color(0.35f, 0.75f, 1f);
            var ring = tag;
            ring.multiplier *= 1.1f;
            ring.launch = true;
            if (Audio != null) Audio.Play("el_water", 1f);
            // She dances; each turn raises a wider ring of geysers.
            for (int wave = 0; wave < 3; wave++)
            {
                Visual.Spin(0.3f, 1);
                float r = (3f + wave * 3f) * fx;
                int n = 8 + wave * 4;
                for (int i = 0; i < n; i++)
                {
                    Vector3 p = Position + Quaternion.Euler(0f, i * 360f / n + wave * 15f, 0f) * Vector3.forward * r;
                    VFX.Pillar(p, sea, 5f + wave, 0.5f);
                    ElementFx.Impact(p + Vector3.up * 0.5f, Element.Water, true);
                }
                VFX.Shockwave(Position, r + 1f, sea, 0.45f);
                // Everything in this ring's band gets thrown up.
                var inner = new HashSet<Combatant>(CombatSystem.Query(this, Position, Vector3.forward, Mathf.Max(0f, r - 2f), 360f));
                tally.Add(CombatSystem.HitRadius(this, Position, r + 1.5f, ring, inner));
                if (Audio != null) Audio.PlayPitched("wave", 0.8f, 1f + wave * 0.1f);
                if (CameraController.Instance != null) CameraController.Instance.Shake(0.3f + wave * 0.1f);
                yield return new WaitForSeconds(0.3f);
            }
            ElementFx.Finisher(Position, transform.forward, Element.Water, 6f * fx);
        }
    }
}
