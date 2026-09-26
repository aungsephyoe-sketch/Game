using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The full stat sheet shared by player characters, equipment bonuses and demons.
    /// crit is a 0..1 chance, critDmg a multiplier, specialDmg multiplies skill/ultimate damage.
    /// </summary>
    [System.Serializable]
    public struct StatBlock
    {
        public float hp;
        public float atk;
        public float def;
        public float crit;
        public float critDmg;
        public float speed;
        public float specialDmg;

        public StatBlock(float hp, float atk, float def, float crit = 0f, float critDmg = 0f, float speed = 0f, float specialDmg = 0f)
        {
            this.hp = hp;
            this.atk = atk;
            this.def = def;
            this.crit = crit;
            this.critDmg = critDmg;
            this.speed = speed;
            this.specialDmg = specialDmg;
        }

        public static StatBlock operator +(StatBlock a, StatBlock b)
        {
            return new StatBlock(a.hp + b.hp, a.atk + b.atk, a.def + b.def, a.crit + b.crit,
                a.critDmg + b.critDmg, a.speed + b.speed, a.specialDmg + b.specialDmg);
        }

        public static StatBlock operator *(StatBlock a, float m)
        {
            return new StatBlock(a.hp * m, a.atk * m, a.def * m, a.crit * m, a.critDmg * m, a.speed * m, a.specialDmg * m);
        }

        /// <summary>Scales only the "growth" stats (HP/ATK/DEF) – crit and speed do not grow with level.</summary>
        public StatBlock ScaleCore(float m)
        {
            var s = this;
            s.hp *= m;
            s.atk *= m;
            s.def *= m;
            return s;
        }

        /// <summary>A single number summarising strength, used for team power and matchmaking later.</summary>
        public int PowerRating
        {
            get
            {
                float offense = atk * (1f + Mathf.Clamp01(crit) * Mathf.Max(0f, critDmg - 1f)) * Mathf.Max(1f, specialDmg);
                return Mathf.RoundToInt(hp * 0.12f + offense * 2.2f + def * 1.1f);
            }
        }
    }
}
