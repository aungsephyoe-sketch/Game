using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Character progression rules and final stat computation (CharacterStats).
    /// Final = base × level growth × ascension, + skill tree %, + equipment flat bonuses.
    /// </summary>
    public static class CharacterSystem
    {
        public const int MaxSkillLevel = 10;
        public const int MaxStars = 6;

        /// <summary>Strong attacks (skill buttons) a slayer can use: Common 1, Rare 2, Epic and up 3.</summary>
        public static int SkillSlots(int stars) { return stars <= 2 ? 1 : stars == 3 ? 2 : 3; }

        /// <summary>The rarity a slot unlocks at, for the lock label.</summary>
        public static int SkillSlotRarity(int slot) { return slot <= 0 ? 2 : slot == 1 ? 3 : 4; }

        public static float LevelMultiplier(int level) { return 1f + 0.045f * (level - 1); }

        /// <summary>Each star above the character's base rarity adds 12% core stats.</summary>
        public static float AscensionMultiplier(CharacterDefinition def, int stars)
        {
            return 1f + 0.12f * Mathf.Max(0, stars - def.rarity);
        }

        public static StatBlock ComputeStats(PlayerData data, OwnedCharacter owned)
        {
            var def = GameDatabase.GetCharacter(owned.id);
            var s = def.baseStats.ScaleCore(LevelMultiplier(owned.level) * AscensionMultiplier(def, owned.stars));

            float atkPct, hpPct, crit, special;
            SkillTree.Accumulate(owned, out atkPct, out hpPct, out crit, out special);
            s.atk *= 1f + atkPct;
            s.hp *= 1f + hpPct;
            s.crit += crit;
            s.specialDmg += special;

            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                var item = data.GetItem(owned.GetEquipped(slot));
                if (item == null) continue;
                var eq = GameDatabase.GetEquipment(item.defId);
                if (eq != null) s = s + eq.BonusAt(item.level);
            }
            s.crit = Mathf.Clamp01(s.crit);
            return s;
        }

        public static int Power(PlayerData data, OwnedCharacter owned)
        {
            var s = ComputeStats(data, owned);
            int skillBonus = 0;
            foreach (var l in owned.skillLevels) skillBonus += (l - 1) * 120;
            return s.PowerRating + skillBonus;
        }

        public static int TeamPower(PlayerData data)
        {
            int p = 0;
            foreach (var id in data.team)
            {
                var c = data.GetCharacter(id);
                if (c != null) p += Power(data, c);
            }
            return p;
        }

        /// <summary>Damage scaling per skill level: +8% per level.</summary>
        public static float SkillLevelMultiplier(int level) { return 1f + 0.08f * (level - 1); }

        // --------------------------------------------------------------- Upgrades

        public static int SkillUpgradeCoinCost(int level) { return 1500 * level; }
        public static int SkillUpgradeXpCost(int level) { return 600 * level; }

        public static bool TryUpgradeSkill(PlayerData data, OwnedCharacter c, int index)
        {
            int lvl = c.skillLevels[index];
            if (lvl >= MaxSkillLevel) return false;
            int coins = SkillUpgradeCoinCost(lvl), xp = SkillUpgradeXpCost(lvl);
            if (data.coins < coins || data.xp < xp) return false;
            data.coins -= coins;
            data.xp -= xp;
            c.skillLevels[index]++;
            GameEvents.RaiseCharacterUpgraded(c);
            return true;
        }

        /// <summary>Feeds XP from the pool straight into a slayer's experience bar. Returns levels gained.</summary>
        public static int UseXp(PlayerData data, OwnedCharacter c, int amount)
        {
            amount = Mathf.Min(amount, data.xp);
            if (amount <= 0 || ExperienceSystem.IsMaxed(c)) return 0;
            data.xp -= amount;
            int gained = ExperienceSystem.AddExp(c, amount);
            GameEvents.RaiseCharacterUpgraded(c);
            return gained;
        }

        /// <summary>Kept for older callers: one "scroll" is 1000 XP from the pool.</summary>
        public static int UseExpScrolls(PlayerData data, OwnedCharacter c, int count) { return UseXp(data, c, count * ExperienceSystem.ExpPerScroll); }

        /// <summary>Feeds one duplicate copy (any slayer) to a slayer as EXP. Returns levels gained, or -1 if nothing was fed.</summary>
        public static int FeedCopy(PlayerData data, OwnedCharacter target, string copyId)
        {
            if (data.copies == null || ExperienceSystem.IsMaxed(target)) return -1;
            var stack = data.copies.Find(x => x.id == copyId);
            var def = GameDatabase.GetCharacter(copyId);
            if (stack == null || stack.count <= 0 || def == null) return -1;
            stack.count--;
            if (stack.count <= 0) data.copies.Remove(stack);
            int gained = ExperienceSystem.AddExp(target, ExperienceSystem.CopyXp(def.rarity));
            GameEvents.RaiseCharacterUpgraded(target);
            return gained;
        }

        /// <summary>Sells one duplicate copy for gold. Returns the gold earned.</summary>
        public static int SellCopy(PlayerData data, string copyId)
        {
            if (data.copies == null) return 0;
            var stack = data.copies.Find(x => x.id == copyId);
            var def = GameDatabase.GetCharacter(copyId);
            if (stack == null || stack.count <= 0 || def == null) return 0;
            stack.count--;
            if (stack.count <= 0) data.copies.Remove(stack);
            int gold = ExperienceSystem.CopyGold(def.rarity);
            data.coins += gold;
            return gold;
        }

        public static int AscendXpCost(int stars) { return 4000 * (stars - 1) * (stars - 1); }
        public static int AscendCoinCost(int stars) { return 8000 * (stars - 1); }
        public static int AscendOreCost(int stars) { return AscendXpCost(stars); }

        /// <summary>A slayer at their level cap can ascend one star, which raises the cap and all stats.</summary>
        public static bool CanAscend(PlayerData data, OwnedCharacter c, out string reason)
        {
            reason = "";
            if (c.stars >= MaxStars) { reason = "Max stars"; return false; }
            int cap = ExperienceSystem.LevelCap(c.stars);
            if (c.level < cap) { reason = "Reach Lv." + cap; return false; }
            if (data.xp < AscendXpCost(c.stars)) { reason = "Need " + AscendXpCost(c.stars).ToString("N0") + " XP"; return false; }
            if (data.coins < AscendCoinCost(c.stars)) { reason = "Need " + AscendCoinCost(c.stars).ToString("N0") + " gold"; return false; }
            return true;
        }

        public static bool TryAscend(PlayerData data, OwnedCharacter c)
        {
            string reason;
            if (!CanAscend(data, c, out reason)) return false;
            data.xp -= AscendXpCost(c.stars);
            data.coins -= AscendCoinCost(c.stars);
            c.stars++;
            GameEvents.RaiseCharacterUpgraded(c);
            return true;
        }

        public static string Stars(int n)
        {
            return new string('★', Mathf.Clamp(n, 0, 7));
        }
    }
}
