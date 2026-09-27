using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Fighting styles. Every slayer shares the same controls, but each style plays differently:
    ///   Swift     — very fast double-hit swings, long dash attacks, quick dodges.
    ///   Heavy     — slow, huge damage and knockback, armour through swings, ground-slam finishers.
    ///   Ranged    — attacks are projectiles; the finisher is a fan of shots; the charged attack is an area blast.
    ///   Technical — wider parry window; every parry becomes an instant counter; after a parry or perfect dodge
    ///               the combo finisher unleashes a crescent wave.
    ///   Balanced  — the classic 5-hit combo with an elemental burst finisher.
    /// </summary>
    public partial class PlayerCharacter
    {
        float techReadyUntil = -10f;

        float StyleSpeed
        {
            get
            {
                switch (Def.style)
                {
                    case CombatStyle.Swift: return 1.5f;
                    case CombatStyle.Brawler: return 1.35f;
                    case CombatStyle.Heavy: return 0.72f;
                    default: return 1f;
                }
            }
        }

        float StyleDamage
        {
            get
            {
                switch (Def.style)
                {
                    case CombatStyle.Heavy: return 1.45f;
                    case CombatStyle.Ranged: return 0.9f;
                    case CombatStyle.Healer: return 0.75f;
                    case CombatStyle.Brawler: return 0.9f;
                    default: return 1f;
                }
            }
        }

        void PlayStyleSwing(bool finisher)
        {
            var a = Audio;
            if (a == null) return;
            switch (Def.style)
            {
                case CombatStyle.Swift: a.PlayPitched(finisher ? "heavy" : "slash", finisher ? 0.6f : 0.45f, Random.Range(1.25f, 1.45f)); break;
                case CombatStyle.Heavy: a.PlayPitched(finisher ? "impact" : "slashHeavy", finisher ? 1f : 0.8f, Random.Range(0.8f, 0.95f)); break;
                case CombatStyle.Technical: a.PlayPitched(finisher ? "heavy" : "slash", finisher ? 0.7f : 0.5f, Random.Range(1.05f, 1.15f)); break;
                default: a.PlayPitched(finisher ? "heavy" : "slash", finisher ? 0.7f : 0.5f, Random.Range(0.95f, 1.05f)); break;
            }
        }

        /// <summary>Style-specific extra beat after each melee swing.</summary>
        IEnumerator StyleFollowUp(int step, bool finisher, float range, float arc, AttackTag tag)
        {
            switch (Def.style)
            {
                case CombatStyle.Swift:
                    if (!finisher)
                    {
                        // The second cut of every swing.
                        yield return new WaitForSeconds(0.05f);
                        CombatSystem.HitArc(this, Position, transform.forward, range, arc, tag);
                        ElementFx.Slash(Position, transform.forward, range * 0.9f, 150f, -SlashRoll[step % 5], Def.element, 0.7f);
                        if (Audio != null) Audio.PlayPitched("slash", 0.35f, 1.5f);
                    }
                    else
                    {
                        // Finisher: a flurry of five rapid cuts.
                        var flurry = tag;
                        flurry.multiplier *= 0.35f;
                        flurry.hitStop = 0.02f;
                        for (int i = 0; i < 5; i++)
                        {
                            yield return new WaitForSeconds(0.045f);
                            CombatSystem.HitRadius(this, Position + transform.forward * 1.2f, 2.4f, flurry);
                            ElementFx.Slash(Position + transform.forward * 1.2f, Random.insideUnitSphere, 1.8f, 160f, Random.Range(-80f, 80f), Def.element, 0.6f);
                            if (Audio != null) Audio.PlayPitched("slash", 0.3f, 1.4f + i * 0.08f);
                        }
                    }
                    break;
                case CombatStyle.Heavy:
                    if (finisher)
                    {
                        // Ground slam: shockwave, dust, and everything nearby is thrown back.
                        var slam = tag;
                        slam.multiplier *= 0.8f;
                        slam.knockback = 11f;
                        slam.heavy = true;
                        CombatSystem.HitRadius(this, Position + transform.forward * 1.5f, 4.2f, slam);
                        VFX.Shockwave(Position + transform.forward * 1.5f, 4.6f, ElementColor, 0.45f);
                        VFX.Dust(Position + transform.forward * 1.5f, 18);
                        VFX.ImpactLight(Position + transform.forward * 1.5f, ElementColor, 7f, 0.3f);
                        if (CameraController.Instance != null) CameraController.Instance.Shake(0.45f);
                        GameEvents.RaiseImpact(0.7f);
                        if (Audio != null) Audio.PlayPitched("impact", 1f, 0.8f);
                    }
                    else VFX.Dust(Position + transform.forward, 4);
                    break;
                case CombatStyle.Technical:
                    if (finisher && Time.time < techReadyUntil)
                    {
                        techReadyUntil = -10f;
                        // Crescent wave: the reward for reading the enemy.
                        var wave = tag;
                        wave.multiplier *= 1.4f;
                        wave.special = true;
                        WaveProjectile.Launch(this, Position + Vector3.up * 0.1f, transform.forward, 20f, 12f, 1.6f, wave, new DamageTally());
                        DamageNumbers.SpawnText(Position + Vector3.up * 2.6f, "CRESCENT!", ElementColor, 44f);
                        if (Audio != null) Audio.PlayPitched("wave", 0.8f, 1.1f);
                    }
                    break;
                case CombatStyle.Balanced:
                    if (finisher)
                    {
                        var burst = tag;
                        burst.multiplier *= 0.6f;
                        CombatSystem.HitRadius(this, Position + transform.forward * 1.8f, 2.8f, burst);
                        VFX.BurstDisc(Position + transform.forward * 1.8f, 2.8f, ElementColor, 0.35f);
                        VFX.Breath(Position + transform.forward * 1.8f, ElementColor, 25);
                    }
                    break;
            }
        }

        // ------------------------------------------------------------------ Ranged

        IEnumerator RangedCombo()
        {
            while (true)
            {
                attackQueued = false;
                canMoveCancel = false;
                int step = comboIndex;
                bool finisher = step >= Def.comboMultipliers.Length - 1;
                float spd = Def.attackSpeed;
                var target = AutoAim(16f);
                Visual.Attack(step, 0.1f / spd);
                Visual.Punch(1.05f);
                yield return new WaitForSeconds(0.07f / spd);

                Vector3 dir = transform.forward;
                if (target != null)
                {
                    dir = target.Position - Position;
                    dir.y = 0f;
                    dir.Normalize();
                }
                var tag = AttackTag.Basic(Def.comboMultipliers[step] * StyleDamage, ElementColor);
                tag.hitStop = 0.02f;
                Vector3 muzzle = Position + dir * 0.7f;
                if (finisher)
                {
                    tag.knockback = 4f;
                    tag.stagger = 3f;
                    for (int i = -2; i <= 2; i++)
                        EnemyProjectile.Fire(this, muzzle, Quaternion.Euler(0f, i * 12f, 0f) * dir, 24f, 16f, tag, ElementColor, 0.42f);
                    VFX.BurstDisc(muzzle, 1.2f, ElementColor, 0.25f);
                    if (Def.role == Role.Support || Def.style == CombatStyle.Healer)
                    {
                        // Healers mend the team a little with every volley.
                        var b = BattleController.Current;
                        if (b != null)
                            foreach (var m in b.Team.Members)
                                if (m != null && m.IsAlive) m.Health.Heal(m.Health.Max * 0.02f);
                        VFX.Breath(Position, new Color(0.5f, 1f, 0.6f), 12);
                    }
                    if (Audio != null) Audio.PlayPitched("shoot", 0.8f, 0.85f);
                }
                else
                {
                    int shots = step % 2 == 1 ? 2 : 1;
                    for (int i = 0; i < shots; i++)
                        EnemyProjectile.Fire(this, muzzle, Quaternion.Euler(0f, shots > 1 ? (i == 0 ? -5f : 5f) : 0f, 0f) * dir, 26f, 16f, tag, ElementColor, 0.34f);
                    if (Audio != null) Audio.PlayPitched("shoot", 0.55f, Random.Range(1.2f, 1.4f));
                }
                VFX.HitSpark(muzzle + Vector3.up, ElementColor, 6);
                comboIndex = finisher ? 0 : step + 1;

                float recovery = (finisher ? 0.38f : 0.2f) / spd;
                float cancelAt = 0.06f / spd;
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

        /// <summary>Ranged charged attack: an area blast where the target stands.</summary>
        IEnumerator RangedCharged()
        {
            var target = AutoAim(16f);
            Vector3 at = target != null ? target.Position : Position + transform.forward * 6f;
            Visual.SetCharge(0f, ElementColor);
            Visual.HeavyAttack(0.16f);
            var t = Telegraph.Circle(at, 3.4f, 0.25f);
            yield return new WaitForSeconds(0.25f);
            if (t != null) Destroy(t.gameObject);
            var tag = AttackTag.Basic(Def.chargedMultiplier * 0.9f, ElementColor);
            tag.knockback = 6f;
            tag.stagger = 6f;
            tag.heavy = true;
            tag.launch = true;
            CombatSystem.HitRadius(this, at, 3.4f, tag);
            VFX.Pillar(at, ElementColor, 7f, 0.5f);
            VFX.Shockwave(at, 3.6f, ElementColor, 0.4f);
            VFX.Breath(at, ElementColor, 30);
            if (Audio != null) Audio.PlayPitched("impact", 0.9f, 1.1f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 3f);
            yield return new WaitForSeconds(0.3f);
            comboIndex = 0;
        }

        // ------------------------------------------------------------------ Technical

        /// <summary>Instant counter after a parry: step in and cut through the attacker.</summary>
        IEnumerator CounterRoutine(Combatant attacker)
        {
            Vector3 to = attacker.Position - Position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f) Face(to, 1f);
            Visual.DashAttack(0.1f);
            Vector3 start = Position;
            Vector3 end = BattleController.ClampToArena(attacker.Position - to.normalized * (attacker.Radius + 0.9f));
            float t = 0f;
            while (t < 0.1f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, t / 0.1f);
                yield return null;
            }
            var tag = AttackTag.Basic(3f, ElementColor);
            tag.heavy = true;
            tag.knockback = 7f;
            tag.stagger = 8f;
            tag.hitStop = 0.1f;
            tag.shake = 0.4f;
            CombatSystem.HitArc(this, Position, transform.forward, 3.2f, 200f, tag);
            VFX.Slash(Position, transform.forward, 3.2f, 220f, 0f, Color.white, 0.25f);
            VFX.Slash(Position, transform.forward, 3f, 200f, 30f, ElementColor, 0.3f);
            DamageNumbers.SpawnText(Position + Vector3.up * 2.6f, "COUNTER!", new Color(1f, 0.95f, 0.6f), 58f);
            if (Audio != null) Audio.PlayPitched("heavy", 0.9f, 1.2f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 8f);
            yield return new WaitForSeconds(0.25f);
        }
    }
}
