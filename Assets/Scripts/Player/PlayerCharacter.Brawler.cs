using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Two more ways to fight:
    ///   Brawler — hand-to-hand. Rapid jab-cross strings that step in with every punch, a rising uppercut
    ///             that launches, a charged ground-pound, and a hundred-fist barrage as the special.
    ///   Healer  — ranged casting that mends the team with every volley, a charged sanctuary that heals and
    ///             burns, and a special that restores everyone while light rains on the demons.
    /// </summary>
    public partial class PlayerCharacter
    {
        Vector3 FistPoint { get { return Position + Vector3.up * 1.05f + transform.forward * 0.9f; } }

        IEnumerator BrawlerCombo()
        {
            while (true)
            {
                attackQueued = false;
                canMoveCancel = false;
                int step = comboIndex;
                bool finisher = step >= Def.comboMultipliers.Length - 1;
                float spd = Def.attackSpeed * StyleSpeed;
                var target = AutoAim(5f);
                // Step in with every punch.
                float lunge = 0.55f;
                if (target != null) lunge = Mathf.Clamp(Vector3.Distance(target.Position, Position) - target.Radius - 0.9f, 0.2f, 2.2f);
                if (finisher) Visual.HeavyAttack(0.14f / spd);
                else Visual.Attack(step, 0.08f / spd);
                Visual.Punch(1.08f);
                float windup = 0.06f / spd, t = 0f;
                Vector3 start = Position;
                while (t < windup)
                {
                    t += Time.deltaTime;
                    transform.position = BattleController.ClampToArena(start + transform.forward * lunge * Mathf.Clamp01(t / windup));
                    yield return null;
                }

                var tag = AttackTag.Basic(Def.comboMultipliers[step] * StyleDamage, ElementColor);
                tag.hitStop = 0.045f;
                tag.shake = 0.18f;
                if (finisher)
                {
                    // Rising uppercut: launches everything in front, then a shock ring.
                    tag.multiplier *= 1.3f;
                    tag.launch = true;
                    tag.heavy = true;
                    tag.knockback = 3f;
                    tag.stagger = 6f;
                    tag.hitStop = 0.1f;
                    tag.shake = 0.4f;
                    CombatSystem.HitArc(this, Position, transform.forward, 2.6f, 140f, tag);
                    ElementFx.Slash(Position, transform.forward, 2.2f, 120f, 90f, Def.element, 1.8f);
                    ElementFx.Finisher(Position + transform.forward * 1.2f, transform.forward, Def.element, 2.8f);
                    VFX.Pillar(Position + transform.forward * 1.2f, ElementColor, 5f, 0.3f);
                    if (Audio != null) { Audio.PlayPitched("impact", 1f, 1.1f); Audio.PlayVaried("whoosh", 0.6f, 0.1f); }
                    if (CameraController.Instance != null) CameraController.Instance.Punch(0.5f, 0.15f);
                }
                else
                {
                    // Jab / cross: two quick hits per press, alternating fists.
                    for (int k = 0; k < 2; k++)
                    {
                        var jab = tag;
                        jab.multiplier *= 0.6f;
                        jab.knockback = 1.2f;
                        CombatSystem.HitArc(this, Position, transform.forward, 2.2f, 100f, jab);
                        Vector3 fist = FistPoint + transform.right * (k == 0 ? 0.25f : -0.25f);
                        VFX.Flash(MeshFactory.Ring(0.7f), fist, Quaternion.LookRotation(Vector3.up, transform.forward), new Vector3(0.2f, 1f, 0.2f), new Vector3(0.7f, 1f, 0.7f), ElementColor, 0.12f);
                        ElementFx.Impact(fist, Def.element, false);
                        if (Audio != null) Audio.PlayPitched("hit", 0.45f, Random.Range(1.3f, 1.5f));
                        Visual.Swing(k == 0 ? -30f : 30f, 0f, 0.05f, 10f);
                        if (k == 0) yield return new WaitForSeconds(0.06f / spd);
                    }
                }

                comboIndex = finisher ? 0 : step + 1;
                float recovery = (finisher ? 0.35f : 0.14f) / spd;
                float cancelAt = (finisher ? 0.18f : 0.05f) / spd;
                float r = 0f;
                while (r < recovery)
                {
                    r += Time.deltaTime;
                    if (r >= cancelAt) canMoveCancel = true;
                    if (attackQueued && r >= cancelAt) break;
                    yield return null;
                }
                if (!attackQueued) yield break;
            }
        }

        /// <summary>Charged: leap and pound the ground with both fists.</summary>
        IEnumerator BrawlerCharged()
        {
            var target = AutoAim(7f);
            Visual.SetCharge(0f, ElementColor);
            Visual.HeavyAttack(0.2f);
            Vector3 start = Position;
            Vector3 end = target != null ? BattleController.ClampToArena(target.Position - (target.Position - Position).normalized * (target.Radius + 0.6f)) : BattleController.ClampToArena(Position + transform.forward * 2.5f);
            float t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                float k = t / 0.22f;
                transform.position = Vector3.Lerp(start, end, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f;
                yield return null;
            }
            transform.position = end;
            var tag = AttackTag.Basic(Def.chargedMultiplier * 1.1f, ElementColor);
            tag.heavy = true;
            tag.launch = true;
            tag.knockback = 7f;
            tag.stagger = 7f;
            tag.hitStop = 0.12f;
            tag.shake = 0.5f;
            CombatSystem.HitRadius(this, Position, 3.6f, tag);
            ElementFx.Finisher(Position, transform.forward, Def.element, 4f);
            VFX.Dust(Position, 20);
            GameEvents.RaiseImpact(0.6f);
            if (Audio != null) Audio.PlayPitched("bossSlam", 0.9f, 1.2f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 3f);
            yield return new WaitForSeconds(0.3f);
            comboIndex = 0;
        }

        /// <summary>Special: a barrage of a hundred fists, then one colossal straight punch.</summary>
        IEnumerator BrawlerSpecial(AttackTag tag, Color color, Combatant target, DamageTally tally)
        {
            if (target != null)
            {
                Vector3 to = target.Position - Position;
                to.y = 0f;
                if (to.sqrMagnitude > 0.01f) Face(to, 1f);
                transform.position = BattleController.ClampToArena(target.Position - to.normalized * (target.Radius + 1f));
            }
            var hit = tag;
            hit.multiplier *= 0.35f;
            hit.hitStop = 0.01f;
            hit.knockback = 0.5f;
            for (int i = 0; i < 24; i++)
            {
                Vector3 fist = FistPoint + transform.right * Random.Range(-0.6f, 0.6f) + Vector3.up * Random.Range(-0.4f, 0.5f);
                VFX.Flash(MeshFactory.Ring(0.6f), fist, Quaternion.LookRotation(Vector3.up, transform.forward), new Vector3(0.15f, 1f, 0.15f), new Vector3(0.8f, 1f, 0.8f), Color.Lerp(color, Color.white, 0.4f), 0.1f);
                if (i % 3 == 0) ElementFx.Impact(fist + transform.forward * 0.4f, Def.element, false);
                Visual.Swing(i % 2 == 0 ? -25f : 25f, 0f, 0.03f, 8f);
                if (i % 2 == 0) tally.Add(CombatSystem.HitArc(this, Position, transform.forward, 2.6f, 90f, hit));
                if (Audio != null && i % 2 == 0) Audio.PlayPitched("hit", 0.35f, 1.3f + Random.value * 0.4f);
                yield return new WaitForSeconds(0.035f);
            }
            Visual.HeavyAttack(0.15f);
            yield return new WaitForSeconds(0.12f);
            var big = tag;
            big.multiplier *= 1.4f;
            big.knockback = 14f;
            big.heavy = true;
            tally.Add(CombatSystem.HitArc(this, Position, transform.forward, 4f, 120f, big));
            ElementFx.Finisher(Position + transform.forward * 2f, transform.forward, Def.element, 4.5f);
            VFX.Flash(MeshFactory.Line(), Position + Vector3.up, transform.rotation, new Vector3(2f, 1f, 7f), new Vector3(0.1f, 1f, 9f), color, 0.35f);
            if (Audio != null) Audio.Play("impact", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.6f);
        }

        // ------------------------------------------------------------------ Healer

        void HealTeam(float fraction)
        {
            var b = BattleController.Current;
            if (b == null) return;
            foreach (var m in b.Team.Members)
                if (m != null && m.IsAlive)
                {
                    m.Health.Heal(m.Health.Max * fraction);
                    if (m.gameObject.activeInHierarchy) VFX.Breath(m.Position, new Color(0.5f, 1f, 0.6f), 10);
                }
        }

        /// <summary>Charged: a sanctuary circle that heals the team and scorches demons inside it.</summary>
        IEnumerator HealerCharged()
        {
            Visual.SetCharge(0f, ElementColor);
            Visual.Victory();
            Vector3 at = Position;
            var green = new Color(0.5f, 1f, 0.6f);
            VFX.BurstDisc(at, 4f, green, 0.8f);
            VFX.Pillar(at, green, 7f, 0.6f);
            if (Audio != null) Audio.Play("skill", 0.8f);
            var tag = AttackTag.Basic(Def.chargedMultiplier * 0.35f, ElementColor);
            tag.stagger = 2f;
            for (int i = 0; i < 4; i++)
            {
                HealTeam(0.035f);
                CombatSystem.HitRadius(this, at, 4f, tag);
                VFX.Shockwave(at, 4f, green, 0.4f);
                ElementFx.Impact(at + Vector3.up, Def.element, false);
                yield return new WaitForSeconds(0.18f);
            }
            UltGauge = Mathf.Min(UltMax, UltGauge + 3f);
            comboIndex = 0;
        }

        /// <summary>Special: the whole team is restored while light rains on every demon nearby.</summary>
        IEnumerator HealerSpecial(AttackTag tag, Color color, Combatant target, DamageTally tally)
        {
            var green = new Color(0.55f, 1f, 0.65f);
            VFX.Pillar(Position, green, 12f, 1f);
            VFX.BurstDisc(Position, 6f, green, 1f);
            HealTeam(0.35f);
            var rain = tag;
            rain.multiplier *= 0.5f;
            var foes = new List<Combatant>(CombatSystem.Query(this, Position, Vector3.forward, 14f, 360f));
            for (int wave = 0; wave < 3; wave++)
            {
                foreach (var f in foes)
                {
                    if (f == null || !f.IsAlive) continue;
                    VFX.Pillar(f.Position, Color.Lerp(color, Color.white, 0.5f), 10f, 0.25f);
                    ElementFx.Impact(f.Position + Vector3.up, Def.element, true);
                    tally.Add(CombatSystem.ApplyHit(this, f, rain, f.Position + Vector3.up));
                }
                if (Audio != null) Audio.PlayPitched("perfect", 0.6f, 0.9f + wave * 0.15f);
                yield return new WaitForSeconds(0.25f);
            }
            HealTeam(0.15f);
        }
    }
}
