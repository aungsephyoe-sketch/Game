using UnityEngine;

namespace HashiraChronicles
{
    public static class ExperienceSystem
    {
        public const int MaxLevel = 120;
        public const int ExpPerScroll = 1000;

        public static int ExpToNext(int level)
        {
            return 80 + 40 * level + 6 * level * level;
        }

        /// <summary>Max level by rarity: Common 30, Rare 60, Epic 80, Legendary 100, Mythic 120.</summary>
        public static int LevelCap(int stars)
        {
            switch (Mathf.Clamp(stars, 2, 6))
            {
                case 2: return 30;
                case 3: return 60;
                case 4: return 80;
                case 5: return 100;
                default: return 120;
            }
        }

        public static bool IsMaxed(OwnedCharacter c) { return c.level >= LevelCap(c.stars); }

        /// <summary>XP (the currency) needed to train from this level to the next.</summary>
        public static int LevelUpXp(int level) { return ExpToNext(level) / 2; }

        /// <summary>Coins needed to train a slayer from this level to the next.</summary>
        public static int LevelUpCost(int level)
        {
            return 150 + level * 60 + level * level * 4;
        }

        /// <summary>Spends gold and XP to raise levels (up to count, never past the rarity cap). Returns levels gained.</summary>
        public static int BuyLevels(PlayerData d, OwnedCharacter c, int count)
        {
            int gained = 0;
            int cap = LevelCap(c.stars);
            while (gained < count && c.level < cap && d.coins >= LevelUpCost(c.level) && d.xp >= LevelUpXp(c.level))
            {
                d.coins -= LevelUpCost(c.level);
                d.xp -= LevelUpXp(c.level);
                c.level++;
                c.exp = 0;
                gained++;
            }
            return gained;
        }

        /// <summary>XP a duplicate copy gives when fed to a slayer, by its rarity.</summary>
        public static int CopyXp(int rarity)
        {
            switch (Mathf.Clamp(rarity, 2, 6)) { case 2: return 1500; case 3: return 4000; case 4: return 10000; case 5: return 25000; default: return 60000; }
        }

        /// <summary>Gold a duplicate copy sells for, by its rarity.</summary>
        public static int CopyGold(int rarity)
        {
            switch (Mathf.Clamp(rarity, 2, 6)) { case 2: return 500; case 3: return 1500; case 4: return 4000; case 5: return 10000; default: return 25000; }
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
