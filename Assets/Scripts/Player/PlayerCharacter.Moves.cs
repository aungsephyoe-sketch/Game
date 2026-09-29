using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Strong-attack library: every slayer's strong attack is their own. Each gets one of twelve movement patterns
    /// (how they get there and hit) and one of six finishers (how the blow lands), assigned by roster order so no
    /// two slayers share the same combination (72 combinations). Coloured by their element, sized by rarity, and
    /// wrapped in the mythic prelude and aftershock for Mythics.
    ///   Patterns : Leap Slam, Rising Fang, Cyclone, Piercing Line, Cross Cut, Earth Stride, Crescent Wave,
    ///              Meteor Dive, Hundred Strikes, Backflip Volley, Vortex, Phantom Step.
    ///   Finishers: Twin Rings, Sky Pillar, Star Fracture, Spiral Blades, Shard Rain, Implosion.
    /// </summary>
    public partial class PlayerCharacter
    {
        public static readonly string[] StrongPatternNames =
        {
            "Leap Slam", "Rising Fang", "Cyclone", "Piercing Line", "Cross Cut", "Earth Stride",
            "Crescent Wave", "Meteor Dive", "Hundred Strikes", "Backflip Volley", "Vortex", "Phantom Step"
        };
        public static readonly string[] StrongFinisherNames = { "Twin Rings", "Sky Pillar", "Star Fracture", "Spiral Blades", "Shard Rain", "Implosion" };

        /// <summary>This slayer's strong attack: pattern (0–11) and finisher (0–5), unique across the roster.</summary>
        public static void StrongSignature(CharacterDefinition def, out int pattern, out int finisher)
        {
            int i = GameDatabase.Characters.IndexOf(def);
            if (i < 0)
            {
                int h = 17;
                foreach (char ch in def.id) h = h * 31 + ch;
                i = (h & 0x7fffffff) % 72;
            }
            pattern = i % 12;
            finisher = (i / 12) % 6;
        }

        public static string StrongName(CharacterDefinition def)
        {
            int p, f;
            StrongSignature(def, out p, out f);
            return StrongPatternNames[p] + " · " + StrongFinisherNames[f];
        }

        Vector3 moveImpact;

        AttackTag StrongTag(float share)
        {
            var tag = AttackTag.Basic(StrongMultiplier * (1f + 0.05f * Mathf.Max(0, Owned.stars - 2)) * share, ElementColor);
            tag.knockback = 9f;
            tag.stagger = 8f;
            tag.hitStop = 0.1f;
            tag.shake = 0.45f;
            tag.heavy = true;
            tag.launch = true;
            return tag;
        }

        IEnumerator UniqueStrong()
        {
            int p, f;
            StrongSignature(Def, out p, out f);
            var target = AutoAim(9f);
            Color c = ElementColor;
            float fx = RarityFx;
            var tally = new DamageTally();
            // Anticipation: a crouch while the element gathers (the same short beat for everyone).
            Visual.Punch(0.8f);
            Visual.SetCharge(0.6f, c);
            for (int i = 0; i < 6; i++) VFX.Breath(Position + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 1.5f, c, 3);
            PlayElementSound(0.6f);
            if (Audio != null) Audio.PlayPitched("charge", 0.35f, 1.6f + p * 0.05f);
            yield return new WaitForSeconds(0.1f);
            Visual.SetCharge(0f, c);
            moveImpact = Position + transform.forward * 2f;
            switch (p)
            {
                case 0: yield return MvLeapSlam(target, c, fx, tally); break;
                case 1: yield return MvRisingFang(target, c, fx, tally); break;
                case 2: yield return MvCyclone(c, fx, tally); break;
                case 3: yield return MvPiercingLine(target, c, fx, tally); break;
                case 4: yield return MvCrossCut(c, fx, tally); break;
                case 5: yield return MvEarthStride(c, fx, tally); break;
                case 6: yield return MvCrescentWave(c, fx, tally); break;
                case 7: yield return MvMeteorDive(target, c, fx, tally); break;
                case 8: yield return MvHundredStrikes(c, fx, tally); break;
                case 9: yield return MvBackflipVolley(c, fx, tally); break;
                case 10: yield return MvVortex(c, fx, tally); break;
                default: yield return MvPhantomStep(c, fx, tally); break;
            }
            Visual.transform.localPosition = Vector3.zero;
            yield return MvFinisher(f, moveImpact, c, fx, tally);
            if (Def.style == CombatStyle.Healer) HealTeam(0.08f);
            if (tally.total > 0f)
            {
                TimeController.SlowMotion(0.35f, 0.22f);
                DamageNumbers.SpawnText(moveImpact + Vector3.up * 2.6f, StrongPatternNames[p].ToUpperInvariant() + "!", Color.Lerp(c, Color.white, 0.4f), 54f);
            }
            if (Audio != null) Audio.PlayPitched("smash", 1f, 0.9f + f * 0.04f);
            GameEvents.RaiseImpact(0.7f);
            if (CameraController.Instance != null) CameraController.Instance.Punch(0.6f, 0.2f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 4f);
            yield return new WaitForSeconds(0.28f);
            comboIndex = 0;
        }

        Vector3 DirToward(Combatant target)
        {
            if (target == null) return transform.forward;
            Vector3 to = target.Position - Position;
            to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return transform.forward;
            Face(to.normalized, 1f);
            return to.normalized;
        }

        IEnumerator Hop(Vector3 from, Vector3 to, float height, float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                MoveTo(Vector3.Lerp(from, to, k));
                Visual.transform.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * height;
                yield return null;
            }
            Visual.transform.localPosition = Vector3.zero;
        }

        // ------------------------------------------------------------------ Patterns

        IEnumerator MvLeapSlam(Combatant target, Color c, float fx, DamageTally tally)
        {
            Vector3 dir = DirToward(target);
            float dist = target != null ? Mathf.Clamp((target.Position - Position).magnitude - target.Radius - 0.8f, 0.5f, 4f) : 2.6f;
            Visual.Spin(0.22f, 1);
            yield return Hop(Position, Position + dir * dist, 1.2f, 0.22f);
            Visual.HeavyAttack(0.12f);
            tally.Add(CombatSystem.HitArc(this, Position, dir, 4.6f, 220f, StrongTag(1f)));
            ElementFx.Slash(Position, dir, 4.6f * fx, 220f, 0f, Def.element, 2.6f * fx);
            moveImpact = Position + dir * 2f;
        }

        IEnumerator MvRisingFang(Combatant target, Color c, float fx, DamageTally tally)
        {
            Vector3 dir = DirToward(target);
            Vector3 a = Position;
            Visual.Swing(40f, -40f, 0.14f, 70f);
            float t = 0f;
            while (t < 0.14f) { t += Time.deltaTime; MoveTo(Vector3.Lerp(a, a + dir * 2.4f, t / 0.14f)); yield return null; }
            var tag = StrongTag(1f);
            tag.knockback = 3f;
            tally.Add(CombatSystem.HitArc(this, Position, dir, 3.6f, 160f, tag));
            ElementFx.Slash(Position + Vector3.up * 0.6f, dir, 3.6f * fx, 160f, 80f, Def.element, 2.2f * fx);
            // Rise with the blow, then float down.
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = t / 0.35f;
                Visual.transform.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.4f;
                yield return null;
            }
            moveImpact = Position + dir * 1.5f;
        }

        IEnumerator MvCyclone(Color c, float fx, DamageTally tally)
        {
            Visual.Spin(0.6f, 3);
            var tag = StrongTag(0.4f);
            tag.knockback = 0f;
            for (int i = 0; i < 3; i++)
            {
                foreach (var foe in Foes(Position, 4.2f * fx)) PullToward(foe, Position, tag, tally);
                ElementFx.Slash(Position + Vector3.up * (0.3f + i * 0.35f), Quaternion.Euler(0f, i * 120f, 0f) * transform.forward, 3.8f * fx, 360f, 10f + i * 10f, Def.element, 1.6f * fx);
                if (Audio != null) Audio.PlayPitched(SwingSound(), 0.6f, 1f + i * 0.1f);
                yield return new WaitForSeconds(0.18f);
            }
            moveImpact = Position;
        }

        IEnumerator MvPiercingLine(Combatant target, Color c, float fx, DamageTally tally)
        {
            Vector3 dir = DirToward(target);
            Vector3 a = Position, b = a + dir * 7f;
            var hit = new HashSet<Combatant>();
            var tag = StrongTag(1f);
            float t = 0f;
            Visual.DashAttack(0.18f);
            while (t < 0.18f)
            {
                t += Time.deltaTime;
                MoveTo(Vector3.Lerp(a, b, t / 0.18f));
                tally.Add(CombatSystem.HitArc(this, Position, dir, 1.8f, 360f, tag, hit));
                VFX.Flash(MeshFactory.SmoothCapsule(), Position + Vector3.up, Quaternion.LookRotation(dir) * Quaternion.Euler(90f, 0f, 0f), new Vector3(0.5f, 0.6f, 0.5f), new Vector3(0.1f, 1.2f, 0.1f), new Color(c.r, c.g, c.b, 0.5f), 0.3f);
                yield return null;
            }
            VFX.Flash(MeshFactory.Line(), a + Vector3.up, Quaternion.LookRotation(dir), new Vector3(0.6f, 1f, 7f), new Vector3(0.05f, 1f, 7f), Color.Lerp(c, Color.white, 0.5f), 0.35f);
            moveImpact = Position;
        }

        IEnumerator MvCrossCut(Color c, float fx, DamageTally tally)
        {
            Vector3 dir = transform.forward;
            for (int k = 0; k < 2; k++)
            {
                float roll = k == 0 ? 45f : -45f;
                Visual.Swing(k == 0 ? -120f : 120f, k == 0 ? 100f : -100f, 0.1f, 20f, roll);
                tally.Add(CombatSystem.HitArc(this, Position, dir, 4f, 170f, StrongTag(0.55f)));
                ElementFx.Slash(Position + Vector3.up * 0.2f, dir, 4f * fx, 170f, roll, Def.element, 2.2f * fx);
                yield return new WaitForSeconds(0.13f);
            }
            moveImpact = Position + dir * 2.2f;
            VFX.HitStar(moveImpact + Vector3.up, c, 2.2f * fx, 0.2f);
        }

        IEnumerator MvEarthStride(Color c, float fx, DamageTally tally)
        {
            Vector3 dir = transform.forward;
            var hit = new HashSet<Combatant>();
            for (int k = 1; k <= 3; k++)
            {
                Visual.HeavyAttack(0.08f);
                Vector3 p = Position + dir * (1.6f * k);
                tally.Add(CombatSystem.HitRadius(this, p, 1.9f * fx, StrongTag(0.45f), hit));
                VFX.Shockwave(p, 2.2f * fx, c, 0.3f);
                VFX.Dust(p, 10);
                if (Audio != null) Audio.PlayPitched("thud", 0.7f, 0.8f + k * 0.1f);
                if (CameraController.Instance != null) CameraController.Instance.Shake(0.15f + k * 0.05f);
                moveImpact = p;
                yield return new WaitForSeconds(0.12f);
            }
        }

        IEnumerator MvCrescentWave(Color c, float fx, DamageTally tally)
        {
            Vector3 dir = transform.forward;
            Visual.Swing(-140f, 140f, 0.14f, 15f);
            yield return new WaitForSeconds(0.08f);
            var tag = StrongTag(1f);
            WaveProjectile.Launch(this, Position + Vector3.up * 0.2f + dir * 0.6f, dir, 22f, 13f, 2.6f * fx, tag, tally);
            ElementFx.Slash(Position + dir, dir, 3f * fx, 200f, 0f, Def.element, 2.4f * fx);
            yield return new WaitForSeconds(0.25f);
            moveImpact = Position + dir * 6f;
        }

        IEnumerator MvMeteorDive(Combatant target, Color c, float fx, DamageTally tally)
        {
            Vector3 dir = DirToward(target);
            Vector3 aim = target != null ? target.Position : Position + dir * 4f;
            Vector3 a = Position, back = a - dir * 1.2f;
            // Up and back...
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                float k = t / 0.2f;
                MoveTo(Vector3.Lerp(a, back, k));
                Visual.transform.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI * 0.5f) * 3.5f;
                yield return null;
            }
            Visual.Spin(0.15f, 1);
            yield return new WaitForSeconds(0.06f);
            // ...and straight down onto the target.
            Vector3 land = BattleController.ClampToArena(aim - dir * 0.8f);
            t = 0f;
            while (t < 0.14f)
            {
                t += Time.deltaTime;
                float k = t / 0.14f;
                MoveTo(Vector3.Lerp(back, land, k));
                Visual.transform.localPosition = Vector3.up * 3.5f * (1f - k * k);
                yield return null;
            }
            Visual.HeavyAttack(0.1f);
            tally.Add(CombatSystem.HitRadius(this, Position, 4f * fx, StrongTag(1.1f)));
            VFX.Dust(Position, 20);
            moveImpact = Position;
        }

        IEnumerator MvHundredStrikes(Color c, float fx, DamageTally tally)
        {
            Vector3 dir = transform.forward;
            var tag = StrongTag(0.16f);
            tag.knockback = 0.5f;
            tag.launch = false;
            tag.hitStop = 0.02f;
            for (int k = 0; k < 8; k++)
            {
                Visual.Attack(k % 4, 0.05f);
                tally.Add(CombatSystem.HitArc(this, Position, dir, 3.2f, 110f, tag));
                VFX.HitSpark(Position + dir * 1.6f + Vector3.up * (0.6f + (k % 3) * 0.3f) + transform.right * ((k % 2) * 0.6f - 0.3f), c, 6);
                if (Audio != null && k % 2 == 0) Audio.PlayPitched(HitSound(), 0.5f, 1f + k * 0.04f);
                yield return new WaitForSeconds(0.045f);
            }
            var shove = StrongTag(0.3f);
            tally.Add(CombatSystem.HitArc(this, Position, dir, 3.4f, 120f, shove));
            moveImpact = Position + dir * 2f;
        }

        IEnumerator MvBackflipVolley(Color c, float fx, DamageTally tally)
        {
            Vector3 dir = transform.forward;
            Visual.Dodge(-dir, 0.28f);
            Vector3 a = Position;
            var tag = StrongTag(0.35f);
            float t = 0f;
            int fired = 0;
            while (t < 0.28f)
            {
                t += Time.deltaTime;
                MoveTo(Vector3.Lerp(a, a - dir * 2.6f, t / 0.28f));
                while (fired < 5 && t > fired * 0.05f)
                {
                    Vector3 d = Quaternion.Euler(0f, -30f + fired * 15f, 0f) * dir;
                    WaveProjectile.Launch(this, Position + Vector3.up * 0.4f, d, 20f, 11f, 1.4f * fx, tag, tally);
                    fired++;
                }
                yield return null;
            }
            moveImpact = Position + dir * 5f;
        }

        IEnumerator MvVortex(Color c, float fx, DamageTally tally)
        {
            Vector3 center = Position + transform.forward * 2.5f;
            Visual.HeavyAttack(0.1f);
            var pull = StrongTag(0.12f);
            pull.knockback = 0f;
            pull.launch = false;
            pull.hitStop = 0.01f;
            for (int k = 0; k < 5; k++)
            {
                foreach (var foe in Foes(center, 7f * fx)) PullToward(foe, center, pull, tally);
                for (int i = 0; i < 3; i++)
                    ElementFx.Slash(center + Vector3.up * 0.4f, Quaternion.Euler(0f, k * 70f + i * 120f, 0f) * Vector3.forward, (3.5f - k * 0.5f) * fx, 160f, 12f, Def.element, 1.2f * fx);
                yield return new WaitForSeconds(0.09f);
            }
            tally.Add(CombatSystem.HitRadius(this, center, 3.6f * fx, StrongTag(0.8f)));
            moveImpact = center;
        }

        IEnumerator MvPhantomStep(Color c, float fx, DamageTally tally)
        {
            // Blinks behind up to three demons in turn, striking each.
            var foes = Foes(Position, 9f);
            int n = Mathf.Min(3, foes.Count);
            var tag = StrongTag(0.6f);
            if (n == 0)
            {
                tally.Add(CombatSystem.HitArc(this, Position, transform.forward, 3.6f, 180f, tag));
                moveImpact = Position + transform.forward * 2f;
                yield break;
            }
            for (int k = 0; k < n; k++)
            {
                var foe = foes[k];
                if (foe == null || !foe.IsAlive) continue;
                VFX.Flash(MeshFactory.SmoothSphere(), Position + Vector3.up, Quaternion.identity, Vector3.one * 1.2f, Vector3.one * 0.1f, new Color(c.r, c.g, c.b, 0.6f), 0.2f);
                Vector3 to = foe.Position - Position;
                to.y = 0f;
                Vector3 behind = foe.Position + (to.sqrMagnitude > 0.01f ? to.normalized : transform.forward) * (foe.Radius + 0.9f);
                MoveTo(BattleController.ClampToArena(behind));
                Face(foe.Position - Position, 1f);
                Visual.Attack(k, 0.08f);
                tally.Add(CombatSystem.ApplyHit(this, foe, tag, Position));
                ElementFx.Slash(Position, transform.forward, 2.6f * fx, 150f, k * 30f - 30f, Def.element, 1.6f * fx);
                if (Audio != null) Audio.PlayPitched("sp_whoosh", 0.5f, 1.2f + k * 0.1f);
                moveImpact = foe.Position;
                yield return new WaitForSeconds(0.1f);
            }
        }

        // ------------------------------------------------------------------ Finishers

        IEnumerator MvFinisher(int f, Vector3 at, Color c, float fx, DamageTally tally)
        {
            var tag = StrongTag(0.25f);
            switch (f)
            {
                case 0:
                    VFX.Shockwave(at, 3f * fx, Color.white, 0.25f);
                    VFX.Shockwave(at, 5.5f * fx, c, 0.5f);
                    ElementFx.Finisher(at, transform.forward, Def.element, 4f * fx);
                    break;
                case 1:
                    VFX.Pillar(at, Color.Lerp(c, Color.white, 0.4f), 9f * fx, 0.5f);
                    VFX.ImpactLight(at + Vector3.up * 2f, c, 12f * fx, 0.4f);
                    break;
                case 2:
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 d = Quaternion.Euler(0f, i * 60f, 0f) * transform.forward;
                        VFX.Flash(MeshFactory.Line(), at + Vector3.up * 0.05f, Quaternion.LookRotation(d), new Vector3(0.3f, 1f, 0.5f), new Vector3(0.05f, 1f, 3.2f * fx), Color.Lerp(c, Color.black, 0.3f), 0.55f);
                    }
                    VFX.Dust(at, 16);
                    break;
                case 3:
                    for (int i = 0; i < 4; i++)
                    {
                        ElementFx.Slash(at + Vector3.up * (0.3f + i * 0.3f), Quaternion.Euler(0f, i * 90f, 0f) * transform.forward, (2f + i * 0.5f) * fx, 200f, i * 25f, Def.element, 1.4f * fx);
                        yield return new WaitForSeconds(0.04f);
                    }
                    break;
                case 4:
                    for (int i = 0; i < 6; i++)
                    {
                        Vector3 o = at + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(0.5f, 3f * fx);
                        ElementFx.Finisher(o, Vector3.down, Def.element, 1.4f * fx);
                        yield return new WaitForSeconds(0.035f);
                    }
                    break;
                default:
                    VFX.Flash(MeshFactory.SmoothSphere(), at + Vector3.up, Quaternion.identity, Vector3.one * 6f * fx, Vector3.one * 0.2f, new Color(c.r, c.g, c.b, 0.5f), 0.22f);
                    yield return new WaitForSeconds(0.2f);
                    VFX.HitStar(at + Vector3.up, c, 3f * fx, 0.22f);
                    VFX.BurstDisc(at, 4f * fx, Color.white, 0.3f);
                    break;
            }
            tally.Add(CombatSystem.HitRadius(this, at, 2.6f * fx, tag));
        }
    }
}
