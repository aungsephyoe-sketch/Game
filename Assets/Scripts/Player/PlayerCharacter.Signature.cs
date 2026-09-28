using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Signature specials: every slayer's special has its own set piece, not just a bigger swing.
    ///   Ren — a water dragon coils around him and breathes a torrent      Ren (Dawn) — Sun Wheel (see Signature2)
    ///   Homura — Phoenix Dive (see Signature2)                              Homura (Last Flame) — a meteor storm
    ///   Sora — a zig-zag lightning dash through every demon                 Raiga — leaps into the sky and falls as a thunder spear
    ///   Tetsu — rings of stone spikes burst from the ground                 Mina — four shadow clones strike from every side
    ///   Rokuro — a rain of glowing arrows                                   Genji — time stops, one flash, every demon is cut at once
    ///   Yui — a tidal wave rolls across the field                           Kuroe — Eclipse (see Signature2)
    /// Kiba (hundred fists) and Hana (restoring light) use their fighting-style specials.
    /// </summary>
    public partial class PlayerCharacter
    {
        IEnumerator SignatureRelease(AbilityDefinition ab, Color color, Combatant target, DamageTally tally)
        {
            var tag = AbilitySystem.MakeTag(this, ab, SkillLevelMult(3), true);
            // Specials hit much harder than regular skills (they no longer add the generic burst on top), and rarer
            // slayers hit harder still.
            tag.multiplier *= 1.3f * (1f + 0.08f * Mathf.Max(0, Owned.stars - 2));
            Vector3 focus = target != null ? target.Position : Position + transform.forward * 5f;
            if (NewcomerIds.Contains(Def.id)) return NewcomerSpecial(tag, tally, focus);
            switch (Def.id)
            {
                case "ren_initiate": return DragonSpecial(tag, tally, focus, 1f);
                case "ren_sundance": return SunWheel(tag, tally, focus);
                case "homura_pillar": return PhoenixDive(tag, tally, focus);
                case "seren_starfall": return Constellation(tag, tally, focus);
                case "garou_onyx": return BlackTornado(tag, tally, focus);
                case "homura_lastflame": return MeteorStorm(tag, tally, focus, color, false);
                case "sora_initiate": return ZigzagThunder(tag, tally, focus);
                case "raiga_pillar": return ThunderSpear(tag, tally, focus);
                case "tetsu_guard": return EarthFortress(tag, tally);
                case "mina_ember": return ShadowClones(tag, tally, focus, color);
                case "rokuro_hunter": return ArrowRain(tag, tally, focus, color);
                case "genji_ronin": return IaidoFlash(tag, tally);
                case "yui_tide": return TidalSpecial(tag, tally, focus);
                case "kuroe_moon": return Eclipse(tag, tally, focus);
                default: return StyleRelease(ab, color, target, tally);
            }
        }

        List<Combatant> Foes(Vector3 at, float radius)
        {
            var list = new List<Combatant>(CombatSystem.Query(this, at, Vector3.forward, radius, 360f));
            list.Sort((a, b) => (a.Position - at).sqrMagnitude.CompareTo((b.Position - at).sqrMagnitude));
            return list;
        }

        /// <summary>A wide shot so a big summon reads, then back to following the slayer.</summary>
        void WideShot(Vector3 focus, float duration)
        {
            var cam = CameraController.Instance;
            if (cam == null) return;
            Vector3 fwd = focus - Position;
            fwd.y = 0f;
            if (fwd.sqrMagnitude < 0.01f) fwd = transform.forward;
            fwd.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, fwd);
            cam.Cut(Position - fwd * 8f + side * 4f + Vector3.up * 5f, (Position + focus) * 0.5f + Vector3.up * 2.5f, true);
            cam.Dolly(Position - fwd * 10f - side * 3f + Vector3.up * 7f, focus + Vector3.up * 2f, duration);
        }

        void FollowAgain()
        {
            if (CameraController.Instance != null) CameraController.Instance.Follow(transform, false);
        }

        void FaceTo(Vector3 p)
        {
            Vector3 d = p - Position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.01f) Face(d, 1f);
        }

        // ------------------------------------------------------------------ Dragons

        IEnumerator DragonSpecial(AttackTag tag, DamageTally tally, Vector3 focus, float size)
        {
            FaceTo(focus);
            Vector3 P = Position, fwd = transform.forward, right = transform.right;
            WideShot(focus, 3.2f);
            Visual.Victory();
            // The dragon bursts out of the ground behind the slayer...
            Vector3 start = P - fwd * 2.5f - Vector3.up * 1f;
            VFX.Shockwave(P - fwd * 2.5f, 3f, ElementChart.ColorOf(Def.element), 0.5f);
            ElementFx.Finisher(P - fwd * 2.5f, fwd, Def.element, 2.5f);
            var dragon = SpiritDragon.Summon(start, Def.element, size);
            if (Audio != null) { Audio.PlayPitched("roar", 0.9f, 1.25f); Audio.Play("charge", 0.6f); }
            // ...coils around them, rises, and swings out over the demons.
            yield return dragon.Fly(new[]
            {
                start, P + right * 3.2f + Vector3.up * 1.5f, P + fwd * 3f + Vector3.up * 2.8f, P - right * 3.2f + Vector3.up * 3.8f,
                P - fwd * 2.5f + Vector3.up * 5f, P + (focus - P) * 0.5f + Vector3.up * 6.5f, focus - fwd * 3.5f + Vector3.up * 4.5f
            }, 1.5f);
            if (Audio != null) Audio.PlayPitched("specialRelease", 0.9f, 0.8f);
            var breath = tag;
            breath.multiplier *= 0.45f;
            breath.hitStop = 0.01f;
            yield return dragon.Breathe(focus, 1.4f, at =>
            {
                tally.Add(CombatSystem.HitRadius(this, at, 3.4f, breath));
                ElementFx.Impact(at + Vector3.up * 0.5f + Random.insideUnitSphere, Def.element, true);
                if (CameraController.Instance != null) CameraController.Instance.Shake(0.12f);
            });
            ElementFx.Finisher(focus, fwd, Def.element, 4.5f);
            tally.Add(CombatSystem.HitRadius(this, focus, 4.5f, tag));
            StartCoroutine(dragon.Leave());
            FollowAgain();
        }

        // ------------------------------------------------------------------ Meteors

        IEnumerator MeteorStorm(AttackTag tag, DamageTally tally, Vector3 focus, Color color, bool moon)
        {
            FaceTo(focus);
            WideShot(focus, 2.8f);
            Visual.Victory();
            if (moon)
            {
                // Crescents orbit the slayer first...
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f;
                    Vector3 p = Position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 3f;
                    ElementFx.Slash(p, Quaternion.Euler(0f, a + 90f, 0f) * Vector3.forward, 2.4f, 200f, 60f, Def.element, 1.4f);
                    tally.Add(CombatSystem.HitRadius(this, p, 2f, tag));
                    yield return new WaitForSeconds(0.05f);
                }
            }
            int count = moon ? 1 : 10;
            int landed = 0;
            var foes = Foes(focus, 8f);
            for (int i = 0; i < count; i++)
            {
                Vector3 at = moon ? focus : (i < foes.Count && foes[i] != null ? foes[i].Position : focus + new Vector3(Random.Range(-5f, 5f), 0f, Random.Range(-5f, 5f)));
                at = BattleController.ClampToArena(at);
                at.y = 0f;
                float size = moon ? 2.6f : Random.Range(0.8f, 1.3f);
                var t = Telegraph.Circle(at, moon ? 5f : 2.6f, moon ? 0.9f : 0.55f);
                FallingMeteor.Drop(at, color, size, moon ? 0.9f : 0.55f, false, p =>
                {
                    landed++;
                    var hit = tag;
                    hit.multiplier *= moon ? 3f : 0.9f;
                    hit.heavy = true;
                    hit.launch = true;
                    tally.Add(CombatSystem.HitRadius(this, p, moon ? 5f : 2.6f, hit));
                    ElementFx.Finisher(p, Vector3.forward, Def.element, moon ? 6f : 2.8f);
                    VFX.Dust(p, 12);
                    VFX.Smoke(p, new Color(0.3f, 0.2f, 0.2f, 0.6f), 10);
                    if (Audio != null) Audio.PlayPitched("bossSlam", moon ? 1f : 0.6f, Random.Range(0.8f, 1.1f));
                    if (CameraController.Instance != null) CameraController.Instance.Shake(moon ? 0.7f : 0.25f);
                    if (t != null) t.Finish();
                });
                yield return new WaitForSeconds(0.13f);
            }
            float wait = 0f;
            while (landed < count && wait < 2f) { wait += Time.deltaTime; yield return null; }
            yield return new WaitForSeconds(0.2f);
            FollowAgain();
        }

        // ------------------------------------------------------------------ Thunder

        IEnumerator ZigzagThunder(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            var foes = Foes(Position, 12f);
            Vector3 cur = Position;
            Vector3 dir = focus - cur;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
            dir.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dir);
            var bolt = new Color(1f, 0.95f, 0.45f);
            var zig = tag;
            zig.multiplier *= 0.6f;
            zig.hitStop = 0.02f;
            for (int i = 0; i < 10; i++)
            {
                // Zig-zag: alternate sides, homing in on demons when there are any.
                Vector3 next;
                if (i < foes.Count && foes[i] != null && foes[i].IsAlive) next = foes[i].Position + side * (i % 2 == 0 ? 1.2f : -1.2f);
                else next = cur + dir * 2.2f + side * (i % 2 == 0 ? 2.6f : -2.6f);
                next = BattleController.ClampToArena(next);
                FaceTo(next);
                Visual.DashAttack(0.06f);
                BoltFx.Strike(cur + Vector3.up, next + Vector3.up, bolt, 0.25f, 0.35f, 0.45f);
                VFX.Flash(MeshFactory.Line(), cur + Vector3.up, Quaternion.LookRotation((next - cur).sqrMagnitude > 0.01f ? next - cur : dir), new Vector3(1.2f, 1f, (next - cur).magnitude), new Vector3(0.05f, 1f, (next - cur).magnitude), bolt, 0.25f);
                MoveTo(next);
                tally.Add(CombatSystem.HitRadius(this, next, 2.2f, zig));
                ElementFx.Impact(next + Vector3.up, Element.Thunder, true);
                if (Audio != null) Audio.PlayPitched("dash", 0.6f, 1.3f + i * 0.05f);
                cur = next;
                yield return new WaitForSeconds(0.07f);
            }
            // Then the sky answers: lightning on everything that was struck.
            yield return new WaitForSeconds(0.2f);
            foreach (var f in Foes(Position, 12f))
            {
                if (f == null || !f.IsAlive) continue;
                BoltFx.Strike(f.Position + Vector3.up * 14f, f.Position, bolt, 0.4f, 0.35f, 0.5f);
                tally.Add(CombatSystem.ApplyHit(this, f, tag, f.Position + Vector3.up));
            }
            VFX.ImpactLight(Position + Vector3.up * 4f, bolt, 18f, 0.3f);
            if (Audio != null) Audio.Play("impact", 1f);
        }

        IEnumerator ThunderSpear(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            FaceTo(focus);
            var bolt = new Color(1f, 0.95f, 0.45f);
            var cam = CameraController.Instance;
            // Leap high into the sky, gathering lightning on the spear...
            float t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                Visual.transform.localPosition = Vector3.up * Mathf.SmoothStep(0f, 8f, t / 0.35f);
                yield return null;
            }
            if (cam != null) { cam.Cut(focus - transform.forward * 7f + Vector3.up * 3f, focus + Vector3.up * 5f, true); }
            for (int i = 0; i < 6; i++)
            {
                Vector3 top = Position + Vector3.up * 8.5f;
                BoltFx.Strike(top + Random.onUnitSphere * 3f, top, bolt, 0.18f, 0.2f);
                yield return new WaitForSeconds(0.06f);
            }
            // ...then fall on the target like a thunderbolt.
            MoveTo(focus - transform.forward * 0.8f);
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                Visual.transform.localPosition = Vector3.up * Mathf.Lerp(8f, 0f, t / 0.12f);
                yield return null;
            }
            Visual.transform.localPosition = Vector3.zero;
            Visual.HeavyAttack(0.12f);
            BoltFx.Strike(Position + Vector3.up * 16f, Position, bolt, 0.8f, 0.45f, 0.3f);
            for (int i = 0; i < 8; i++)
            {
                Vector3 d = Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward;
                BoltFx.Strike(Position + Vector3.up * 0.3f, Position + d * 5.5f, bolt, 0.2f, 0.4f, 0.5f);
            }
            var slam = tag;
            slam.multiplier *= 3f;
            slam.heavy = true;
            slam.launch = true;
            tally.Add(CombatSystem.HitRadius(this, Position, 5.5f, slam));
            ElementFx.Finisher(Position, transform.forward, Element.Thunder, 5.5f);
            if (cam != null) { cam.Shake(0.8f); cam.Follow(transform, false); }
            if (Audio != null) Audio.Play("bossSlam", 1f);
            yield return new WaitForSeconds(0.3f);
        }

        // ------------------------------------------------------------------ Earth, shadows, arrows, time, water

        IEnumerator EarthFortress(AttackTag tag, DamageTally tally)
        {
            var stone = new Color(0.55f, 0.48f, 0.4f);
            for (int ring = 0; ring < 3; ring++)
            {
                Visual.HeavyAttack(0.15f);
                yield return new WaitForSeconds(0.12f);
                float r = 2.5f + ring * 2f;
                int n = 8 + ring * 4;
                for (int i = 0; i < n; i++)
                {
                    float a = i * 360f / n + ring * 15f;
                    RisingSpike.Burst(Position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * r, stone, 1.4f + ring * 0.5f, 0.7f);
                }
                var hit = tag;
                hit.launch = true;
                hit.knockback = 6f;
                tally.Add(CombatSystem.HitRadius(this, Position, r + 0.8f, hit));
                VFX.Shockwave(Position, r + 1f, stone, 0.4f);
                if (Audio != null) Audio.PlayPitched("bossSlam", 0.8f, 1.2f - ring * 0.12f);
                if (CameraController.Instance != null) CameraController.Instance.Shake(0.35f + ring * 0.1f);
                yield return new WaitForSeconds(0.2f);
            }
        }

        IEnumerator ShadowClones(AttackTag tag, DamageTally tally, Vector3 focus, Color color)
        {
            var clones = new List<CharacterVisual>();
            var holders = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                var h = new GameObject("ShadowClone").transform;
                h.position = BattleController.ClampToArena(focus + Quaternion.Euler(0f, i * 90f + 45f, 0f) * Vector3.forward * 3.2f);
                Vector3 look = focus - h.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) h.rotation = Quaternion.LookRotation(look);
                var v = CharacterVisual.BuildHero(Def, h);
                v.FidgetsEnabled = false;
                v.Flash(color, 0.8f);
                VFX.Smoke(h.position, new Color(0.2f, 0.05f, 0.3f, 0.8f), 12);
                clones.Add(v);
                holders.Add(h);
                yield return new WaitForSeconds(0.08f);
            }
            if (Audio != null) Audio.PlayPitched("whoosh", 0.8f, 0.8f);
            var cut = tag;
            cut.multiplier *= 0.8f;
            // Each clone dashes through the target to the far side, one after another, then all at once.
            for (int round = 0; round < 2; round++)
                for (int i = 0; i < clones.Count; i++)
                {
                    var h = holders[i];
                    Vector3 from = h.position, to = BattleController.ClampToArena(focus + (focus - from).normalized * 3f);
                    clones[i].DashAttack(0.08f);
                    clones[i].Flash(color, 0.8f);
                    VFX.Flash(MeshFactory.Line(), from + Vector3.up, Quaternion.LookRotation((to - from).sqrMagnitude > 0.01f ? to - from : Vector3.forward), new Vector3(1f, 1f, (to - from).magnitude), new Vector3(0.05f, 1f, (to - from).magnitude), color, 0.3f);
                    h.position = to;
                    h.rotation = Quaternion.LookRotation(focus - to);
                    ElementFx.Slash(focus, to - from, 2.6f, 200f, Random.Range(-60f, 60f), Def.element, 1.5f);
                    tally.Add(CombatSystem.HitRadius(this, focus, 2.6f, cut));
                    if (Audio != null) Audio.PlayPitched("slash", 0.6f, 1.2f + i * 0.08f);
                    yield return new WaitForSeconds(round == 0 ? 0.12f : 0.04f);
                }
            yield return new WaitForSeconds(0.2f);
            foreach (var h in holders) { VFX.Smoke(h.position, new Color(0.15f, 0.03f, 0.2f, 0.8f), 14); Destroy(h.gameObject); }
            ElementFx.Finisher(focus, transform.forward, Def.element, 4f);
        }

        IEnumerator ArrowRain(AttackTag tag, DamageTally tally, Vector3 focus, Color color)
        {
            FaceTo(focus);
            WideShot(focus, 2.4f);
            Visual.Attack(2, 0.1f);
            // One arrow up into the sky...
            VFX.Flash(MeshFactory.Line(), Position + Vector3.up * 1.5f, Quaternion.LookRotation(Vector3.up + transform.forward * 0.3f), new Vector3(0.6f, 1f, 12f), new Vector3(0.05f, 1f, 14f), color, 0.35f);
            if (Audio != null) Audio.PlayPitched("shoot", 1f, 0.7f);
            yield return new WaitForSeconds(0.45f);
            // ...and a storm of them comes down.
            var hit = tag;
            hit.multiplier *= 0.45f;
            hit.hitStop = 0.01f;
            for (int i = 0; i < 28; i++)
            {
                Vector2 r = Random.insideUnitCircle * 5f;
                Vector3 at = BattleController.ClampToArena(focus + new Vector3(r.x, 0f, r.y));
                at.y = 0f;
                FallingMeteor.Drop(at, color, 1f, 0.35f, true, p =>
                {
                    tally.Add(CombatSystem.HitRadius(this, p, 1.5f, hit));
                    ElementFx.Impact(p + Vector3.up * 0.3f, Def.element, false);
                    VFX.Dust(p, 3);
                });
                if (Audio != null && i % 3 == 0) Audio.PlayPitched("shoot", 0.35f, Random.Range(1.3f, 1.6f));
                yield return new WaitForSeconds(0.04f);
            }
            yield return new WaitForSeconds(0.4f);
            FollowAgain();
        }

        IEnumerator IaidoFlash(AttackTag tag, DamageTally tally)
        {
            var foes = Foes(Position, 11f);
            if (foes.Count > 7) foes.RemoveRange(7, foes.Count - 7);
            EnemyController.Frozen = true;
            // A blur from demon to demon...
            Vector3 cur = Position;
            foreach (var f in foes)
            {
                if (f == null) continue;
                Vector3 to = BattleController.ClampToArena(f.Position + (f.Position - cur).normalized * 1.4f);
                VFX.Flash(MeshFactory.Line(), cur + Vector3.up, Quaternion.LookRotation((to - cur).sqrMagnitude > 0.01f ? to - cur : transform.forward), new Vector3(0.6f, 1f, (to - cur).magnitude), new Vector3(0.02f, 1f, (to - cur).magnitude), Color.white, 0.5f);
                MoveTo(to);
                FaceTo(f.Position + (f.Position - cur));
                cur = to;
                if (Audio != null) Audio.PlayPitched("whoosh", 0.5f, 1.6f);
                yield return new WaitForSeconds(0.06f);
            }
            // ...stillness while the blade returns to its sheath...
            Visual.Swing(40f, -40f, 0.5f, 10f);
            yield return new WaitForSeconds(0.55f);
            if (Audio != null) Audio.PlayPitched("clang", 0.9f, 1.4f);
            yield return new WaitForSeconds(0.15f);
            // ...then every cut lands at once.
            EnemyController.Frozen = false;
            var cut = tag;
            cut.multiplier *= 2.2f;
            cut.heavy = true;
            foreach (var f in foes)
            {
                if (f == null || !f.IsAlive) continue;
                ElementFx.Slash(f.Position, Vector3.forward, 1.8f, 220f, 45f, Def.element, 1.8f);
                ElementFx.Slash(f.Position, Vector3.right, 1.8f, 220f, -45f, Def.element, 1.8f);
                tally.Add(CombatSystem.ApplyHit(this, f, cut, f.Position));
            }
            VFX.ImpactLight(Position + Vector3.up * 2f, Color.white, 16f, 0.2f);
            if (Audio != null) Audio.Play("impact", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.5f);
        }

        IEnumerator TidalSpecial(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            FaceTo(focus);
            WideShot(focus, 2f);
            Visual.Spin(0.5f, 2);
            ElementFx.Finisher(Position, transform.forward, Element.Water, 3f);
            yield return new WaitForSeconds(0.3f);
            var wave = tag;
            wave.multiplier *= 0.35f;
            wave.knockback = 2f;
            TidalWave.Launch(this, Position - transform.forward * 0.5f, transform.forward, 9f, 17f, wave, tally);
            if (Audio != null) { Audio.PlayPitched("wave", 1f, 0.6f); Audio.PlayPitched("specialRelease", 0.7f, 0.9f); }
            yield return new WaitForSeconds(1.25f);
            FollowAgain();
        }
    }
}
