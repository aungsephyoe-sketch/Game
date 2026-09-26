using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Summoning with published rates and pity. Every 10-pull guarantees at least one EPIC; 80 pulls without the
    /// featured MYTHIC guarantees it. Duplicates convert into ascension ore (and a few crystals back).
    /// </summary>
    public static class SummonSystem
    {
        public const int SingleCost = 50;
        public const int MultiCost = 450;
        public const int PityLimit = 80;
        public const string FeaturedId = "kuroe_moon";

        /// <summary>Rates for Common, Rare, Epic, Legendary, Mythic.</summary>
        public static readonly float[] Rates = { 0.45f, 0.35f, 0.14f, 0.05f, 0.01f };

        public class Result
        {
            public CharacterDefinition def;
            public int rarity;
            public bool isNew;
        }

        public static bool CanAfford(PlayerData d, int count) { return d.crystals >= (count >= 10 ? MultiCost : SingleCost * count); }

        public static List<Result> Summon(PlayerData d, int count)
        {
            var results = new List<Result>();
            if (!CanAfford(d, count)) return results;
            d.crystals -= count >= 10 ? MultiCost : SingleCost * count;
            bool gotEpic = false;
            for (int i = 0; i < count; i++)
            {
                d.summonPity++;
                d.totalSummons++;
                int rarity = RollRarity();
                if (count >= 10 && i == count - 1 && !gotEpic) rarity = Mathf.Max(rarity, 5);
                CharacterDefinition def;
                if (d.summonPity >= PityLimit || rarity == 7)
                {
                    def = GameDatabase.GetCharacter(FeaturedId);
                    rarity = def.rarity;
                }
                else def = Pick(rarity);
                if (def == null) { def = Pick(4); rarity = def.rarity; }
                if (def.id == FeaturedId) d.summonPity = 0;
                if (rarity >= 5) gotEpic = true;
                bool isNew = InventorySystem.AddCharacter(d, def.id);
                results.Add(new Result { def = def, rarity = rarity, isNew = isNew });
            }
            QuestSystem.Report("summon", count);
            return results;
        }

        static int RollRarity()
        {
            float r = Random.value, acc = 0f;
            for (int i = 0; i < Rates.Length; i++)
            {
                acc += Rates[i];
                if (r <= acc) return 3 + i;
            }
            return 3;
        }

        static CharacterDefinition Pick(int rarity)
        {
            var pool = GameDatabase.SummonPool(rarity);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
