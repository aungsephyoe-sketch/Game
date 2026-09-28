using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Summoning with published rates and pity. Every 10-pull guarantees at least one EPIC; 80 pulls without a
    /// featured MYTHIC guarantees one. Duplicates go to the duplicates pile (feed them as EXP or sell them).
    /// </summary>
    public static class SummonSystem
    {
        public const int SingleCost = 50;
        /// <summary>Step-up ×10 ladder: FREE, 250, 500, 500 (double Mythic rate), 400 — then it repeats.</summary>
        public static readonly int[] StepCosts = { 0, 250, 500, 500, 400 };
        public const int DoubleMythicStep = 3;
        /// <summary>Tokens for the Mythic exchange: one per paid ×10; 30 buys a Mythic.</summary>
        public const int ExchangeCost = 30;

        public static int Step(PlayerData d) { return ((d.summonStep % StepCosts.Length) + StepCosts.Length) % StepCosts.Length; }
        public static int MultiCostFor(PlayerData d) { return StepCosts[Step(d)]; }
        public static int MultiCost { get { return 450; } }
        public const int PityLimit = 80;
        public const string FeaturedId = "kuroe_moon";
        /// <summary>The three featured Mythics of the limited banner.</summary>
        public static readonly string[] FeaturedIds = { "seren_starfall", "kuroe_moon", "garou_onyx" };
        public const string BannerName = "CELESTIAL NIGHT";

        public static bool IsFeatured(string id) { return System.Array.IndexOf(FeaturedIds, id) >= 0; }

        /// <summary>Rates for Common, Rare, Epic, Legendary, Mythic.</summary>
        public static readonly float[] Rates = { 0f, 0f, 0.80f, 0.15f, 0.05f };

        public class Result
        {
            public CharacterDefinition def;
            public int rarity;
            public bool isNew;
        }

        public static bool CanAfford(PlayerData d, int count) { return d.crystals >= (count >= 10 ? MultiCostFor(d) : SingleCost * count); }

        public static List<Result> Summon(PlayerData d, int count)
        {
            var results = new List<Result>();
            if (!CanAfford(d, count)) return results;
            float mythicMul = 1f;
            if (count >= 10)
            {
                // Step-up ×10: pay this step's price, earn a token if it wasn't free, move up the ladder.
                int cost = MultiCostFor(d);
                d.crystals -= cost;
                if (cost > 0) d.summonTokens++;
                if (Step(d) == DoubleMythicStep) mythicMul = 2f;
                d.summonStep = (Step(d) + 1) % StepCosts.Length;
            }
            else d.crystals -= SingleCost * count;
            bool gotEpic = false;
            for (int i = 0; i < count; i++)
            {
                d.summonPity++;
                d.totalSummons++;
                int rarity = RollRarity(mythicMul);
                if (count >= 10 && i == count - 1 && !gotEpic) rarity = Mathf.Max(rarity, 4);
                CharacterDefinition def;
                if (d.summonPity >= PityLimit || rarity == 6)
                {
                    def = GameDatabase.GetCharacter(FeaturedIds[Random.Range(0, FeaturedIds.Length)]);
                    if (def == null) def = GameDatabase.GetCharacter(FeaturedId);
                    rarity = def.rarity;
                }
                else def = Pick(rarity);
                if (def == null) { def = Pick(3); rarity = def.rarity; }
                if (IsFeatured(def.id)) d.summonPity = 0;
                if (rarity >= 4) gotEpic = true;
                bool isNew = InventorySystem.AddCharacter(d, def.id);
                results.Add(new Result { def = def, rarity = rarity, isNew = isNew });
            }
            QuestSystem.Report("summon", count);
            return results;
        }

        /// <summary>Mythic chance at this step (the 4th step doubles it, taken from the Epic share).</summary>
        public static float MythicRate(PlayerData d) { return Rates[4] * (Step(d) == DoubleMythicStep ? 2f : 1f); }

        static int RollRarity(float mythicMul)
        {
            float mythic = Rates[4] * mythicMul;
            float r = Random.value;
            if (r < mythic) return 6;
            r -= mythic;
            if (r < Rates[3]) return 5;
            return 4;
        }

        /// <summary>Trade 30 tokens for a featured Mythic of your choice (a duplicate goes to the awakening pile).</summary>
        public static bool Exchange(PlayerData d, string id, out bool isNew)
        {
            isNew = false;
            if (d.summonTokens < ExchangeCost || !IsFeatured(id)) return false;
            d.summonTokens -= ExchangeCost;
            isNew = InventorySystem.AddCharacter(d, id);
            return true;
        }

        static CharacterDefinition Pick(int rarity)
        {
            var pool = GameDatabase.SummonPool(rarity);
            if (pool.Count == 0) return null;
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
