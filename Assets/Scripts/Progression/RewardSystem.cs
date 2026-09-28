using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    public class LevelUpInfo
    {
        public string name;
        public int from;
        public int to;
    }

    /// <summary>One slayer's progress this mission, for the animated EXP bars on the results screen.</summary>
    public class MemberGain
    {
        public string id;
        public int fromLevel, toLevel;
        public float fromFill, toFill;
        public int expGained;
        public bool maxed;
    }

    public class BattleResult
    {
        public List<MemberGain> members = new List<MemberGain>();
        /// <summary>Stars (objectives) earned this run, and diamonds paid out for improving the mission's best.</summary>
        public int stars;
        public int starDiamonds;
        /// <summary>Gold picked up from defeated demons during the mission.</summary>
        public int goldCollected;
        /// <summary>Diamonds picked up from defeated demons.</summary>
        public int diamondsCollected;
        public MissionDefinition mission;
        public bool victory;
        public string failReason = "";
        public float time;
        public int kills;
        public int maxCombo;
        public float totalDamage;
        public bool[] objectives = new bool[3];
        public string[] objectiveLabels = new string[3];
        public bool firstClear;
        public RewardBundle granted = new RewardBundle();
        public List<LevelUpInfo> levelUps = new List<LevelUpInfo>();
        public string unlockedCharacterName;
        public List<string> droppedEquipmentNames = new List<string>();
    }

    public static class RewardSystem
    {
        /// <summary>Kept for the mission page: the diamond value of a first star.</summary>
        public const int CrystalsPerNewObjective = 1;

        /// <summary>Diamonds for a mission's best star count: 1★ 1, 2★ 2, 3★ 5.</summary>
        public static int DiamondsForStars(int stars) { return stars >= 3 ? 5 : Mathf.Max(0, stars); }

        static int Bits(int mask) { int n = 0; for (int i = 0; i < 3; i++) if ((mask & (1 << i)) != 0) n++; return n; }

        /// <summary>Applies mission rewards to the save and fills in the result's reward summary.</summary>
        public static void Grant(PlayerData data, BattleResult result)
        {
            var m = result.mission;
            var total = new RewardBundle();

            if (result.victory)
            {
                total.Add(m.rewards);

                var progress = data.GetMission(m.id, true);
                result.firstClear = !progress.cleared;
                if (result.firstClear)
                {
                    total.Add(m.firstClearRewards);
                    data.missionsCleared++;
                }
                progress.cleared = true;
                progress.clears++;
                if (progress.bestTime <= 0f || result.time < progress.bestTime) progress.bestTime = result.time;

                // Stars pay diamonds: 1★ = 1, 2★ = 2, 3★ = 5, counted on the mission's best result.
                int before = Bits(progress.objectivesMask);
                int earned = 0;
                for (int i = 0; i < 3; i++)
                {
                    if (!result.objectives[i]) continue;
                    earned++;
                    progress.objectivesMask |= 1 << i;
                }
                result.stars = earned;
                result.starDiamonds = Mathf.Max(0, DiamondsForStars(Bits(progress.objectivesMask)) - DiamondsForStars(before));
                total.crystals += result.starDiamonds;

                if (m.dropTable.Count > 0 && Random.value < 0.3f)
                    total.equipmentIds.Add(m.dropTable[Random.Range(0, m.dropTable.Count)]);
            }
            else
            {
                // Consolation: a fraction of EXP/coins so a failed attempt still moves the player forward.
                total.exp = Mathf.RoundToInt(m.rewards.exp * 0.3f);
                total.coins = Mathf.RoundToInt(m.rewards.coins * 0.3f);
            }

            total.coins += result.goldCollected;
            total.crystals += result.diamondsCollected;
            // Half the mission EXP also goes into the XP pool for training.
            data.xp += total.exp / 2;
            InventorySystem.AddCurrencies(data, total);

            foreach (var eqId in total.equipmentIds)
            {
                InventorySystem.AddEquipment(data, eqId);
                var def = GameDatabase.GetEquipment(eqId);
                if (def != null) result.droppedEquipmentNames.Add(def.displayName);
            }

            if (!string.IsNullOrEmpty(total.characterId))
            {
                var def = GameDatabase.GetCharacter(total.characterId);
                if (InventorySystem.AddCharacter(data, total.characterId) && def != null)
                {
                    result.unlockedCharacterName = def.FullName;
                    // Story allies join the party straight away while there's room.
                    // A new version of someone already in the team (e.g. an awakened form) takes their place.
                    int same = data.team.FindIndex(t => { var o = GameDatabase.GetCharacter(t); return o != null && o.baseId == def.baseId; });
                    if (same >= 0) data.team[same] = def.id;
                    else if (data.team.Count < 3) data.team.Add(def.id);
                }
            }

            foreach (var id in data.team)
            {
                var c = data.GetCharacter(id);
                if (c == null) continue;
                int lvBefore = c.level;
                float fillBefore = ExperienceSystem.IsMaxed(c) ? 1f : Mathf.Clamp01(c.exp / (float)ExperienceSystem.ExpToNext(c.level));
                ExperienceSystem.AddExp(c, total.exp);
                result.members.Add(new MemberGain
                {
                    id = id, fromLevel = lvBefore, toLevel = c.level, fromFill = fillBefore, expGained = total.exp,
                    toFill = ExperienceSystem.IsMaxed(c) ? 1f : Mathf.Clamp01(c.exp / (float)ExperienceSystem.ExpToNext(c.level)),
                    maxed = ExperienceSystem.IsMaxed(c)
                });
                if (c.level > lvBefore)
                    result.levelUps.Add(new LevelUpInfo { name = GameDatabase.GetCharacter(id).displayName, from = lvBefore, to = c.level });
            }

            data.totalKills += result.kills;
            result.granted = total;
        }
    }
}
