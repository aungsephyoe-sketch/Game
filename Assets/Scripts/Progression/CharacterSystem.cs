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
        public const int MaxStars = 7;

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
        public static int SkillUpgradeScrollCost(int level) { return 1 + level / 3; }

        public static bool TryUpgradeSkill(PlayerData data, OwnedCharacter c, int index)
        {
            int lvl = c.skillLevels[index];
            if (lvl >= MaxSkillLevel) return false;
            int coins = SkillUpgradeCoinCost(lvl), scrolls = SkillUpgradeScrollCost(lvl);
            if (data.coins < coins || data.skillScrolls < scrolls) return false;
            data.coins -= coins;
            data.skillScrolls -= scrolls;
            c.skillLevels[index]++;
            GameEvents.RaiseCharacterUpgraded(c);
            return true;
        }

        /// <summary>Consumes EXP scrolls. Returns levels gained, or -1 if nothing happened.</summary>
        public static int UseExpScrolls(PlayerData data, OwnedCharacter c, int count)
        {
            if (c.level >= ExperienceSystem.LevelCap(c.stars)) return -1;
            count = Mathf.Min(count, data.expScrolls);
            if (count <= 0) return -1;
            int coinCost = count * 200;
            if (data.coins < coinCost) return -1;
            data.expScrolls -= count;
            data.coins -= coinCost;
            int gained = ExperienceSystem.AddExp(c, count * ExperienceSystem.ExpPerScroll);
            GameEvents.RaiseCharacterUpgraded(c);
            return gained;
        }

        public static int AscendOreCost(int stars) { return 2 + (stars - 3) * 3; }
        public static int AscendCoinCost(int stars) { return 10000 * (stars - 2); }

        public static bool CanAscend(PlayerData data, OwnedCharacter c, out string reason)
        {
            reason = "";
            if (c.stars >= MaxStars) { reason = "Max stars"; return false; }
            if (c.level < ExperienceSystem.LevelCap(c.stars)) { reason = "Reach Lv." + ExperienceSystem.LevelCap(c.stars); return false; }
            if (data.ascensionOre < AscendOreCost(c.stars)) { reason = "Need " + AscendOreCost(c.stars) + " ore"; return false; }
            if (data.coins < AscendCoinCost(c.stars)) { reason = "Need " + AscendCoinCost(c.stars) + " coins"; return false; }
            return true;
        }

        public static bool TryAscend(PlayerData data, OwnedCharacter c)
        {
            string reason;
            if (!CanAscend(data, c, out reason)) return false;
            data.ascensionOre -= AscendOreCost(c.stars);
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
