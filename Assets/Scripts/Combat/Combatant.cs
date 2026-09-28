using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Anything that can hit or be hit. Keeps a registry so hit detection needs no physics layers.</summary>
    [RequireComponent(typeof(HealthSystem))]
    public abstract class Combatant : MonoBehaviour
    {
        public static readonly List<Combatant> All = new List<Combatant>();

        public CombatTeam Team;
        /// <summary>Anyone can hit it, whatever their team (the PvP boss both teams race to damage).</summary>
        public bool Neutral;
        public Element Element;
        public StatBlock Stats;
        public float Radius = 0.5f;
        /// <summary>Incoming damage multiplier (e.g. 1.5 while a boss is exhausted / "BREAK").</summary>
        public float DamageTakenMultiplier = 1f;

        HealthSystem health;
        public HealthSystem Health
        {
            get
            {
                if (health == null) health = GetComponent<HealthSystem>();
                return health;
            }
        }

        public bool IsAlive { get { return Health != null && !Health.IsDead; } }
        public Vector3 Position { get { return transform.position; } }

        protected virtual void OnEnable() { if (!All.Contains(this)) All.Add(this); }
        protected virtual void OnDisable() { All.Remove(this); }

        /// <summary>Called after damage was applied to this combatant.</summary>
        public virtual void OnHitReceived(DamageInfo info) { }

        /// <summary>Called on the attacker after it damaged a target.</summary>
        public virtual void OnDealtDamage(DamageInfo info, Combatant target) { }

        public static Combatant Nearest(CombatTeam team, Vector3 from, float maxDistance)
        {
            Combatant best = null;
            float bestSq = maxDistance * maxDistance;
            for (int i = 0; i < All.Count; i++)
            {
                var c = All[i];
                if (c.Team != team || !c.IsAlive) continue;
                float d = (c.Position - from).sqrMagnitude;
                if (d < bestSq) { bestSq = d; best = c; }
            }
            return best;
        }
    }
}
