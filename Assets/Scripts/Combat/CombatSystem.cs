using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>How an attack feels and what it does on contact.</summary>
    public struct AttackTag
    {
        public float multiplier;
        public bool special;
        public bool isUltimate;
        public float knockback;
        public float stagger;
        public float hitStop;
        public float shake;
        public Color color;
        /// <summary>Big hit: impact frame, camera punch, impact light, knocks light demons down.</summary>
        public bool heavy;
        /// <summary>Pops light demons into the air (juggle).</summary>
        public bool launch;

        public static AttackTag Basic(float mult, Color color)
        {
            return new AttackTag { multiplier = mult, knockback = 1.2f, stagger = 1f, hitStop = 0.035f, shake = 0.12f, color = color };
        }
    }

    public static class DamageCalculator
    {
        const float DefenseConstant = 600f;

        public static DamageInfo Calculate(Combatant attacker, Combatant target, AttackTag tag)
        {
            var s = attacker.Stats;
            float raw = s.atk * tag.multiplier;
            if (tag.special) raw *= Mathf.Max(0.1f, s.specialDmg);
            float mitigation = DefenseConstant / (DefenseConstant + Mathf.Max(0f, target.Stats.def));
            float elem = ElementChart.Multiplier(attacker.Element, target.Element);
            bool crit = Random.value < s.crit;
            float dmg = raw * mitigation * elem * target.DamageTakenMultiplier * (crit ? Mathf.Max(1f, s.critDmg) : 1f) * Random.Range(0.95f, 1.05f);
            return new DamageInfo
            {
                amount = Mathf.Max(1f, Mathf.Round(dmg)),
                crit = crit,
                elementMultiplier = elem,
                source = attacker,
                staggerPower = tag.stagger,
                special = tag.special,
                isUltimate = tag.isUltimate
            };
        }
    }

    /// <summary>
    /// Hit detection (distance + angle against the Combatant registry) and hit feedback
    /// (damage numbers, sparks, hit-stop, camera shake, sound).
    /// </summary>
    public static class CombatSystem
    {
        static readonly List<Combatant> scratch = new List<Combatant>();

        /// <summary>Collects living opponents inside an arc. arcDegrees 360 = full circle.</summary>
        public static List<Combatant> Query(Combatant attacker, Vector3 origin, Vector3 forward, float range, float arcDegrees)
        {
            scratch.Clear();
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();
            float halfCos = Mathf.Cos(arcDegrees * 0.5f * Mathf.Deg2Rad);
            for (int i = 0; i < Combatant.All.Count; i++)
            {
                var t = Combatant.All[i];
                if (t == attacker || t.Team == attacker.Team || !t.IsAlive) continue;
                Vector3 to = t.Position - origin;
                to.y = 0f;
                float dist = to.magnitude;
                if (dist > range + t.Radius) continue;
                if (arcDegrees < 359f && dist > t.Radius + 0.3f)
                {
                    if (Vector3.Dot(forward, to / dist) < halfCos) continue;
                }
                scratch.Add(t);
            }
            return scratch;
        }

        /// <summary>Hits everything in the arc once. Returns total damage dealt.</summary>
        public static float HitArc(Combatant attacker, Vector3 origin, Vector3 forward, float range, float arcDegrees, AttackTag tag,
            HashSet<Combatant> exclude = null)
        {
            if (attacker is PlayerCharacter)
            {
                Breakable.HitInArc(origin, forward, range, arcDegrees);
                SealStone.HitInArc(origin, forward, range, arcDegrees);
            }
            var targets = Query(attacker, origin, forward, range, arcDegrees);
            float total = 0f;
            // Copy because applying damage can kill/disable targets and mutate the registry.
            var list = new List<Combatant>(targets);
            foreach (var t in list)
            {
                if (exclude != null)
                {
                    if (exclude.Contains(t)) continue;
                    exclude.Add(t);
                }
                total += ApplyHit(attacker, t, tag, origin);
            }
            return total;
        }

        public static float HitRadius(Combatant attacker, Vector3 origin, float radius, AttackTag tag, HashSet<Combatant> exclude = null)
        {
            return HitArc(attacker, origin, Vector3.forward, radius, 360f, tag, exclude);
        }

        public static float ApplyHit(Combatant attacker, Combatant target, AttackTag tag, Vector3 from)
        {
            if (attacker == null || target == null || !target.IsAlive) return 0f;
            var info = DamageCalculator.Calculate(attacker, target, tag);
            Vector3 dir = target.Position - from;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.001f) dir = attacker.transform.forward;
            info.knockback = dir.normalized * tag.knockback;

            if (!target.Health.TakeDamage(ref info)) return 0f;

            info.launch = tag.launch;
            info.heavy = tag.heavy;
            target.OnHitReceived(info);
            attacker.OnDealtDamage(info, target);

            Vector3 hitPos = target.Position + Vector3.up * (1f * target.transform.localScale.y);
            bool playerHit = target is PlayerCharacter;
            VFX.HitSpark(hitPos, playerHit ? new Color(1f, 0.25f, 0.2f) : tag.color, info.crit ? 22 : 12);
            DamageNumbers.Spawn(hitPos, info.amount, info.crit, info.elementMultiplier, playerHit);
            if (GameManager.Instance != null && GameManager.Instance.Audio != null)
                GameManager.Instance.Audio.Play(info.crit ? "crit" : "hit", playerHit ? 0.8f : 0.55f);
            TimeController.HitStop(info.crit ? tag.hitStop * 1.6f : tag.hitStop);
            if (CameraController.Instance != null)
                CameraController.Instance.Shake(info.blocked ? 0.08f : playerHit ? 0.35f : (info.crit ? tag.shake * 1.5f : tag.shake));

            // Heavy impacts get the full treatment: impact frame, zoom punch, light flash, dust.
            if (!playerHit && (tag.heavy || (info.crit && tag.isUltimate)))
            {
                GameEvents.RaiseImpact(tag.isUltimate ? 0.35f : 0.55f);
                if (CameraController.Instance != null) CameraController.Instance.Punch(0.6f, 0.18f);
                VFX.ImpactLight(hitPos, tag.color, 5f, 0.18f);
                VFX.Dust(target.Position, 6);
            }

            GameEvents.RaiseDamageDealt(info, target);
            return info.amount;
        }
    }
}
