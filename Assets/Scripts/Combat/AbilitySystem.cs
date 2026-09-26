using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Running total of damage for one ability use (shown as "TOTAL DAMAGE" after ultimates).</summary>
    public class DamageTally
    {
        public float total;
        public int hits;

        public void Add(float dmg)
        {
            if (dmg <= 0f) return;
            total += dmg;
            hits++;
        }
    }

    /// <summary>
    /// Executes data-driven abilities (AbilityDefinition) for player characters. New breathing forms
    /// are added as data; only genuinely new movement/hit patterns need a new AbilityShape here.
    /// </summary>
    public static class AbilitySystem
    {
        public static AttackTag MakeTag(PlayerCharacter pc, AbilityDefinition ab, float levelMult, bool ultimate)
        {
            return new AttackTag
            {
                multiplier = ab.damageMultiplier * levelMult,
                special = true,
                isUltimate = ultimate,
                knockback = ab.knockback,
                stagger = ab.stagger,
                hitStop = ultimate ? 0.02f : 0.045f,
                shake = ultimate ? 0.3f : 0.2f,
                color = ElementChart.ColorOf(pc.Element),
                heavy = !ultimate && ab.stagger >= 5f,
                launch = !ultimate && ab.shape == AbilityShape.Spin
            };
        }

        public static IEnumerator Execute(PlayerCharacter pc, AbilityDefinition ab, float levelMult, bool ultimate, DamageTally tally)
        {
            var tag = MakeTag(pc, ab, levelMult, ultimate);
            Color color = tag.color;
            float scale = ultimate ? 1.35f : 1f;
            VFX.Breath(pc.Position, color, ultimate ? 70 : 30);
            ElementFlourish(pc, color, ultimate);

            switch (ab.shape)
            {
                case AbilityShape.Dash: yield return Dash(pc, ab, tag, tally, scale); break;
                case AbilityShape.Spin: yield return Spin(pc, ab, tag, tally, scale); break;
                case AbilityShape.Wave: yield return Wave(pc, ab, tag, tally, scale); break;
                case AbilityShape.Burst: yield return Burst(pc, ab, tag, tally, scale); break;
                case AbilityShape.MultiSlash: yield return MultiSlash(pc, ab, tag, tally, scale); break;
                case AbilityShape.Heal: yield return Heal(pc, ab, tag, tally); break;
            }
        }

        /// <summary>Support form: restores a share of every slayer's max HP (benched members too); ultimates also blast nearby demons.</summary>
        static IEnumerator Heal(PlayerCharacter pc, AbilityDefinition ab, AttackTag tag, DamageTally tally)
        {
            var green = new Color(0.5f, 1f, 0.65f);
            pc.Visual.Victory();
            VFX.BurstDisc(pc.Position, ab.radius, green, 0.7f);
            VFX.Pillar(pc.Position, green, 6f, 0.7f);
            VFX.Breath(pc.Position, green, 60);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("skill", 0.6f);
            var battle = BattleController.Current;
            if (battle != null && battle.Team != null)
            {
                foreach (var m in battle.Team.Members)
                {
                    if (m == null || !m.IsAlive) continue;
                    m.Health.Heal(m.Health.Max * ab.damageMultiplier);
                    if (m.gameObject.activeInHierarchy) VFX.Breath(m.Position, green, 20);
                }
            }
            if (tag.isUltimate)
            {
                yield return new WaitForSeconds(0.25f);
                var blast = tag;
                blast.multiplier = 3f;
                VFX.Shockwave(pc.Position, ab.radius, green, 0.5f);
                tally.Add(CombatSystem.HitRadius(pc, pc.Position, ab.radius, blast));
            }
            yield return new WaitForSeconds(0.35f);
            pc.Visual.ResetPose();
        }

        /// <summary>Each breathing style gets its own signature flourish on top of the shape's effects.</summary>
        static void ElementFlourish(PlayerCharacter pc, Color color, bool ultimate)
        {
            Vector3 p = pc.Position;
            float scale = ultimate ? 1.6f : 1f;
            switch (pc.Element)
            {
                case Element.Water:
                    // Rolling tide rings.
                    VFX.Shockwave(p, 2.2f * scale, new Color(0.5f, 0.8f, 1f), 0.45f);
                    VFX.Shockwave(p, 3.4f * scale, new Color(0.3f, 0.6f, 1f), 0.6f);
                    break;
                case Element.Flame:
                    VFX.Pillar(p, new Color(1f, 0.5f, 0.1f), 3.5f * scale, 0.4f);
                    VFX.Breath(p, new Color(1f, 0.35f, 0.05f), ultimate ? 60 : 25);
                    break;
                case Element.Thunder:
                    // Lightning strikes around the slayer.
                    for (int i = 0; i < (ultimate ? 6 : 3); i++)
                    {
                        Vector3 o = p + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1f, 3f * scale);
                        VFX.Pillar(o, new Color(1f, 0.95f, 0.5f), 8f, 0.15f);
                    }
                    VFX.ImpactLight(p + Vector3.up * 2f, new Color(1f, 0.95f, 0.6f), 8f, 0.2f);
                    break;
                case Element.Beast:
                    // Claw marks.
                    for (int i = 0; i < 3; i++)
                        VFX.Slash(p + pc.transform.forward * 1.2f, pc.transform.forward, 2f * scale, 60f, -30f + i * 30f, new Color(0.8f, 0.7f, 1f), 0.25f);
                    break;
                case Element.Light:
                    VFX.BurstDisc(p, 3f * scale, new Color(1f, 0.9f, 0.6f), 0.5f);
                    VFX.Pillar(p, new Color(1f, 0.95f, 0.8f), 6f * scale, 0.5f);
                    break;
                default:
                    VFX.Smoke(p, new Color(0.3f, 0.1f, 0.4f, 0.7f), 16);
                    break;
            }
        }

        static IEnumerator Dash(PlayerCharacter pc, AbilityDefinition ab, AttackTag tag, DamageTally tally, float scale)
        {
            Vector3 dir = pc.transform.forward;
            Vector3 start = pc.Position;
            float duration = 0.18f;
            pc.Health.GrantInvulnerability(duration + 0.1f);
            pc.Visual.Swing(-130f, 110f, duration, 10f);
            var hitSet = new HashSet<Combatant>();
            var hitList = new List<Combatant>();
            float t = 0f;
            Vector3 last = start;
            while (t < duration)
            {
                t += Time.deltaTime;
                Vector3 target = BattleController.ClampToArena(start + dir * ab.range * Mathf.Clamp01(t / duration));
                pc.transform.position = target;
                // Sweep the segment travelled this frame so fast dashes never skip enemies.
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector3.Distance(last, target) / 0.5f));
                for (int s = 1; s <= steps; s++)
                {
                    Vector3 p = Vector3.Lerp(last, target, (float)s / steps);
                    foreach (var c in new List<Combatant>(CombatSystem.Query(pc, p, dir, ab.radius, 360f)))
                    {
                        if (!hitSet.Add(c)) continue;
                        hitList.Add(c);
                        tally.Add(CombatSystem.ApplyHit(pc, c, tag, p));
                    }
                }
                last = target;
                yield return null;
            }
            Vector3 end = pc.Position;
            Vector3 mid = (start + end) * 0.5f + Vector3.up;
            float len = Vector3.Distance(start, end);
            if (len > 0.1f)
                VFX.Flash(MeshFactory.Line(), start + Vector3.up, Quaternion.LookRotation(dir),
                    new Vector3(ab.radius * 0.8f * scale, 1f, len), new Vector3(0.05f, 1f, len), tag.color, 0.3f);
            VFX.Breath(mid, tag.color, 20);

            for (int h = 1; h < ab.hits; h++)
            {
                yield return new WaitForSeconds(0.07f);
                foreach (var c in hitList)
                {
                    if (c == null || !c.IsAlive) continue;
                    VFX.Slash(c.Position, Random.insideUnitSphere, 1.6f, 140f, Random.Range(-60f, 60f), tag.color, 0.15f);
                    tally.Add(CombatSystem.ApplyHit(pc, c, tag, c.Position - dir));
                }
            }
            yield return new WaitForSeconds(0.12f);
        }

        static IEnumerator Spin(PlayerCharacter pc, AbilityDefinition ab, AttackTag tag, DamageTally tally, float scale)
        {
            for (int h = 0; h < ab.hits; h++)
            {
                pc.Visual.Spin(0.2f);
                VFX.Shockwave(pc.Position, ab.radius * scale, tag.color, 0.3f);
                VFX.Slash(pc.Position, pc.transform.forward, ab.radius * scale, 330f, h % 2 == 0 ? 15f : -15f, tag.color, 0.22f);
                tally.Add(CombatSystem.HitRadius(pc, pc.Position, ab.radius, tag));
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slash", 0.6f);
                yield return new WaitForSeconds(0.16f);
            }
            yield return new WaitForSeconds(0.08f);
        }

        static IEnumerator Wave(PlayerCharacter pc, AbilityDefinition ab, AttackTag tag, DamageTally tally, float scale)
        {
            pc.Visual.Swing(-140f, 140f, 0.16f, 0f);
            yield return new WaitForSeconds(0.08f);
            for (int h = 0; h < ab.hits; h++)
            {
                WaveProjectile.Launch(pc, pc.Position + pc.transform.forward * 0.6f, pc.transform.forward, 22f, ab.range,
                    ab.radius * scale, tag, tally);
                if (h < ab.hits - 1) yield return new WaitForSeconds(0.1f);
            }
            yield return new WaitForSeconds(0.22f);
        }

        static IEnumerator Burst(PlayerCharacter pc, AbilityDefinition ab, AttackTag tag, DamageTally tally, float scale)
        {
            pc.Visual.Spin(0.3f);
            VFX.BurstDisc(pc.Position, ab.radius, tag.color, 0.5f);
            for (int h = 0; h < ab.hits; h++)
            {
                VFX.Shockwave(pc.Position, ab.radius * (0.6f + 0.4f * (h + 1) / ab.hits), tag.color, 0.35f);
                foreach (var c in CombatSystem.Query(pc, pc.Position, Vector3.forward, ab.radius, 360f))
                    VFX.Slash(c.Position, Random.insideUnitSphere, 1.8f * scale, 160f, Random.Range(-70f, 70f), tag.color, 0.18f);
                tally.Add(CombatSystem.HitRadius(pc, pc.Position, ab.radius, tag));
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slash", 0.5f);
                yield return new WaitForSeconds(ab.hits > 1 ? 0.12f : 0.05f);
            }
            if (tag.isUltimate)
            {
                foreach (var c in CombatSystem.Query(pc, pc.Position, Vector3.forward, ab.radius, 360f))
                    VFX.Pillar(c.Position, tag.color, 7f, 0.6f);
            }
            yield return new WaitForSeconds(0.15f);
        }

        static IEnumerator MultiSlash(PlayerCharacter pc, AbilityDefinition ab, AttackTag tag, DamageTally tally, float scale)
        {
            for (int h = 0; h < ab.hits; h++)
            {
                var targets = CombatSystem.Query(pc, pc.Position, Vector3.forward, ab.radius, 360f);
                pc.Visual.Swing(h % 2 == 0 ? -120f : 120f, h % 2 == 0 ? 120f : -120f, 0.07f, Random.Range(-20f, 50f));
                if (targets.Count > 0)
                {
                    var t = targets[Random.Range(0, targets.Count)];
                    VFX.Slash(t.Position, Random.insideUnitSphere, 2f * scale, 170f, Random.Range(-80f, 80f), tag.color, 0.14f);
                    tally.Add(CombatSystem.ApplyHit(pc, t, tag, pc.Position));
                }
                else
                {
                    VFX.Slash(pc.Position + pc.transform.forward, pc.transform.forward, 2f, 160f, Random.Range(-60f, 60f), tag.color, 0.14f);
                }
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slash", 0.45f);
                yield return new WaitForSeconds(0.075f);
            }
            if (tag.isUltimate)
            {
                // Final sweep that catches everything left standing.
                VFX.Shockwave(pc.Position, ab.radius, tag.color, 0.4f);
                tally.Add(CombatSystem.HitRadius(pc, pc.Position, ab.radius, tag));
            }
            yield return new WaitForSeconds(0.12f);
        }
    }
}
