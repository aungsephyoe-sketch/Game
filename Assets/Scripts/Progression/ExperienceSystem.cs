using UnityEngine;

namespace HashiraChronicles
{
    public static class ExperienceSystem
    {
        public const int MaxLevel = 100;
        public const int ExpPerScroll = 1000;

        public static int ExpToNext(int level)
        {
            return 80 + 40 * level + 6 * level * level;
        }

        /// <summary>Every slayer can reach level 100 (stars still raise their stats through ascension).</summary>
        public static int LevelCap(int stars)
        {
            return MaxLevel;
        }

        /// <summary>Coins needed to train a slayer from this level to the next.</summary>
        public static int LevelUpCost(int level)
        {
            return 150 + level * 60 + level * level * 4;
        }

        /// <summary>Spends coins to raise levels (up to count). Returns levels gained.</summary>
        public static int BuyLevels(PlayerData d, OwnedCharacter c, int count)
        {
            int gained = 0;
            while (gained < count && c.level < MaxLevel && d.coins >= LevelUpCost(c.level))
            {
                d.coins -= LevelUpCost(c.level);
                c.level++;
                c.exp = 0;
                gained++;
            }
            return gained;
        }

        /// <summary>Adds EXP, returns levels gained. EXP stops accumulating at the level cap.</summary>
        public static int AddExp(OwnedCharacter c, int amount)
        {
            int cap = LevelCap(c.stars);
            int start = c.level;
            if (c.level >= cap) { c.exp = 0; return 0; }
            c.exp += Mathf.Max(0, amount);
            while (c.level < cap && c.exp >= ExpToNext(c.level))
            {
                c.exp -= ExpToNext(c.level);
                c.level++;
            }
            if (c.level >= cap) c.exp = 0;
            return c.level - start;
        }
    }
}
