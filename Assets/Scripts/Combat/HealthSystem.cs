using UnityEngine;

namespace HashiraChronicles
{
    public struct DamageInfo
    {
        public float amount;
        public bool crit;
        public float elementMultiplier;
        public Combatant source;
        public Vector3 knockback;
        public float staggerPower;
        public bool special;
        public bool isUltimate;
        /// <summary>Ignores invulnerability (e.g. falling hazards). Nothing uses it for dodgeable attacks.</summary>
        public bool unavoidable;
        /// <summary>Set by a guard filter when the hit was blocked (reduced damage, no flinch).</summary>
        public bool blocked;
        public bool heavy;
        public bool launch;
    }

    /// <summary>Lets a combatant modify or cancel incoming damage (guard / parry). Return false to cancel.</summary>
    public delegate bool DamageFilter(ref DamageInfo info);

    /// <summary>HP pool with invulnerability windows (dodge i-frames, ultimates) and damage/death events.</summary>
    public class HealthSystem : MonoBehaviour
    {
        public float Max { get; private set; }
        public float Current { get; private set; }
        public bool IsDead { get; private set; }
        public float Normalized { get { return Max > 0f ? Current / Max : 0f; } }

        float invulnerableUntil;
        int invulnerableLocks;

        public bool Invulnerable { get { return invulnerableLocks > 0 || Time.time < invulnerableUntil; } }

        public event System.Action<DamageInfo> Damaged;
        /// <summary>Fired when an attack connects during invulnerability – used for perfect dodges.</summary>
        public event System.Action<DamageInfo> Evaded;
        public event System.Action Died;
        public DamageFilter Filter;

        public void Init(float max, float current = -1f)
        {
            Max = Mathf.Max(1f, max);
            Current = current < 0f ? Max : Mathf.Clamp(current, 0f, Max);
            IsDead = Current <= 0f;
        }

        public void GrantInvulnerability(float seconds)
        {
            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + seconds);
        }

        public void PushInvulnerable() { invulnerableLocks++; }
        public void PopInvulnerable() { invulnerableLocks = Mathf.Max(0, invulnerableLocks - 1); }

        public bool TakeDamage(DamageInfo info) { return TakeDamage(ref info); }

        /// <summary>Returns true if the damage was applied. The filter may reduce info.amount.</summary>
        public bool TakeDamage(ref DamageInfo info)
        {
            if (IsDead) return false;
            if (Invulnerable && !info.unavoidable)
            {
                var ev = Evaded;
                if (ev != null) ev(info);
                return false;
            }
            if (Filter != null && !info.unavoidable && !Filter(ref info)) return false;
            Current = Mathf.Max(0f, Current - info.amount);
            var dmg = Damaged;
            if (dmg != null) dmg(info);
            if (Current <= 0f)
            {
                IsDead = true;
                var died = Died;
                if (died != null) died();
            }
            return true;
        }

        public void Heal(float amount)
        {
            if (IsDead) return;
            Current = Mathf.Min(Max, Current + amount);
        }

        public void Revive(float fraction)
        {
            IsDead = false;
            Current = Mathf.Max(1f, Max * fraction);
        }
    }
}
