using UnityEngine;

namespace HashiraChronicles
{
    public static class ExperienceSystem
    {
        public const int MaxLevel = 120;
        public const int ExpPerScroll = 1000;
        /// <summary>Every source of slayer EXP counts double.</summary>
        public const float ExpBoost = 2f;

        public static int ExpToNext(int level)
        {
            // Gentler curve: about half the EXP per level it used to take at high levels.
            return 60 + 25 * level + 3 * level * level;
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

        /// <summary>Awakening raises the cap by 30 per purple star.</summary>
        public const int LevelsPerAwaken = 30;
        public const int MaxAwaken = 6;

        /// <summary>This slayer's level cap: rarity cap + 30 per awakening.</summary>
        public static int Cap(OwnedCharacter c) { return LevelCap(c.stars) + LevelsPerAwaken * Mathf.Clamp(c.awaken, 0, MaxAwaken); }

        public static bool IsMaxed(OwnedCharacter c) { return c.level >= Cap(c); }

        /// <summary>GOD status: every star awakened (purple) and the level maxed out.</summary>
        public static bool IsGod(OwnedCharacter c) { return c.awaken >= MaxAwaken && IsMaxed(c); }

        /// <summary>XP (the currency) needed to train from this level to the next.</summary>
        public static int LevelUpXp(int level) { return ExpToNext(level) / 2; }

        /// <summary>Coins needed to train a slayer from this level to the next.</summary>
        public static int LevelUpCost(int level)
        {
            return 100 + level * 40 + level * level * 2;
        }

        /// <summary>Spends gold and XP to raise levels (up to count, never past the rarity cap). Returns levels gained.</summary>
        public static int BuyLevels(PlayerData d, OwnedCharacter c, int count)
        {
            int gained = 0;
            int cap = Cap(c);
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
            int cap = Cap(c);
            int start = c.level;
            if (c.level >= cap) { c.exp = 0; return 0; }
            c.exp += Mathf.Max(0, Mathf.RoundToInt(amount * ExpBoost));
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
