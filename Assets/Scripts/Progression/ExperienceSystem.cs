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

        /// <summary>Level cap grows with stars: ★3 40, ★4 55, ★5 70, ★6 85, ★7 100.</summary>
        public static int LevelCap(int stars)
        {
            return Mathf.Clamp(40 + (stars - 3) * 15, 40, MaxLevel);
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
