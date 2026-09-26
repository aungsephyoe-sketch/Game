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

    public class BattleResult
    {
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
        public const int CrystalsPerNewObjective = 5;

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

                for (int i = 0; i < 3; i++)
                {
                    int bit = 1 << i;
                    if (result.objectives[i] && (progress.objectivesMask & bit) == 0)
                    {
                        progress.objectivesMask |= bit;
                        total.crystals += CrystalsPerNewObjective;
                    }
                }

                if (m.dropTable.Count > 0 && Random.value < 0.3f)
                    total.equipmentIds.Add(m.dropTable[Random.Range(0, m.dropTable.Count)]);
            }
            else
            {
                // Consolation: a fraction of EXP/coins so a failed attempt still moves the player forward.
                total.exp = Mathf.RoundToInt(m.rewards.exp * 0.3f);
                total.coins = Mathf.RoundToInt(m.rewards.coins * 0.3f);
            }

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
                    else if (data.team.Count < 4) data.team.Add(def.id);
                }
            }

            foreach (var id in data.team)
            {
                var c = data.GetCharacter(id);
                if (c == null) continue;
                int before = c.level;
                ExperienceSystem.AddExp(c, total.exp);
                if (c.level > before)
                    result.levelUps.Add(new LevelUpInfo { name = GameDatabase.GetCharacter(id).displayName, from = before, to = c.level });
            }

            data.totalKills += result.kills;
            result.granted = total;
        }
    }
}
