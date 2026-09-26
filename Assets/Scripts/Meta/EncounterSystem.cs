using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Something that happens on the road between two locations on the world map.</summary>
    public class Encounter
    {
        public string kind;
        public string title;
        public string text;
        public string accept;
        public string decline;
        public bool battle;
        /// <summary>Region whose demons (and scenery) a battle encounter uses.</summary>
        public string regionId;
        public string enemyId;
        public int enemyCount;
        public int level;
    }

    /// <summary>
    /// Random events while travelling the world map: ambushes, rare monsters, wandering bosses, merchants,
    /// lost travellers, buried treasure, hidden caves and a mysterious stranger. Gives the road a reason to exist.
    /// </summary>
    public static class EncounterSystem
    {
        public const float Chance = 0.45f;

        public static Encounter Roll(PlayerData d, string fromRegion, string toRegion)
        {
            if (Random.value > Chance) return null;
            string region = !string.IsNullOrEmpty(fromRegion) ? fromRegion : toRegion;
            int level = RegionLevel(region);
            float r = Random.value;
            if (r < 0.22f) return Ambush(region, level);
            if (r < 0.32f) return Rare(region, level);
            if (r < 0.38f)
            {
                var wb = WanderingBoss(d, region, level);
                if (wb != null) return wb;
            }
            var peaceful = new[] { "merchant", "traveler", "chest", "cave", "stranger", "cart" };
            string k = peaceful[Random.Range(0, peaceful.Length)];
            switch (k)
            {
                case "merchant":
                    return new Encounter { kind = k, title = "A Travelling Merchant", text = "\"Scrolls! Fresh from the capital's archives. Three EXP scrolls — 1,500 coins, for you.\"", accept = "BUY (1,500)", decline = "No thanks" };
                case "traveler":
                    return new Encounter { kind = k, title = "A Lost Traveller", text = "An old woman has lost the road in the dusk. \"Could you walk me to the next waystone, young swordsman?\"", accept = "GUIDE HER", decline = "Point the way" };
                case "chest":
                    return new Encounter { kind = k, title = "Buried Treasure", text = "A lacquered chest lies half-buried beside the road, its lock long rusted away.", accept = "OPEN IT", decline = "Leave it" };
                case "cave":
                    return new Encounter { kind = k, title = "A Hidden Cave", text = "Behind a curtain of vines, a narrow cave glitters with veins of ore.", accept = "EXPLORE", decline = "Keep moving" };
                case "cart":
                    return new Encounter { kind = k, title = "Someone Needs Help", text = "A farmer's cart has overturned on the road. His daughter is trying to lift it alone.", accept = "HELP", decline = "Pass by" };
                default:
                    return new Encounter { kind = "stranger", title = "A Mysterious Stranger", text = "A hooded figure watches you from beneath a dead tree. \"Dawn and eclipse... two halves of one heart. Do you know whose half you carry, Ren?\"", accept = "LISTEN", decline = "Walk away" };
            }
        }

        static Encounter Ambush(string region, int level)
        {
            string id = RegionEnemy(region, false);
            var e = GameDatabase.GetEnemy(id);
            return new Encounter
            {
                kind = "ambush", battle = true, regionId = region, enemyId = id, enemyCount = 4, level = level,
                title = "Demon Ambush!", text = (e != null ? e.displayName + "s" : "Demons") + " burst from the roadside, blocking the way forward.",
                accept = "FIGHT", decline = "Try to slip past"
            };
        }

        static Encounter Rare(string region, int level)
        {
            string id = RegionEnemy(region, true);
            var e = GameDatabase.GetEnemy(id);
            return new Encounter
            {
                kind = "rare", battle = true, regionId = region, enemyId = id, enemyCount = 1, level = level + 3,
                title = "A Rare Monster", text = "A lone " + (e != null ? e.displayName : "demon") + " — larger than any you've seen — prowls near the road. Its hide glitters with crystal.",
                accept = "HUNT IT", decline = "Let it be"
            };
        }

        static Encounter WanderingBoss(PlayerData d, string region, int level)
        {
            foreach (var m in GameDatabase.MissionsInRegion(region))
                if (m.type == MissionType.Boss && d.IsMissionCleared(m.id) && !string.IsNullOrEmpty(m.bossId))
                {
                    var b = GameDatabase.GetEnemy(m.bossId);
                    return new Encounter
                    {
                        kind = "boss", battle = true, regionId = region, enemyId = m.bossId, enemyCount = 1, level = level + 5,
                        title = "A Wandering Boss", text = "The ground trembles. " + (b != null ? b.displayName : "A great demon") + " has returned — stronger, and hungry for revenge.",
                        accept = "FACE IT", decline = "Retreat quietly"
                    };
                }
            return null;
        }

        static int RegionLevel(string region)
        {
            int lvl = 1;
            foreach (var m in GameDatabase.MissionsInRegion(region)) lvl = Mathf.Max(lvl, m.enemyLevel);
            return lvl;
        }

        static string RegionEnemy(string region, bool strong)
        {
            var pool = new List<string>();
            foreach (var m in GameDatabase.MissionsInRegion(region))
                foreach (var w in m.waves)
                    foreach (var s in w.spawns)
                    {
                        var e = GameDatabase.GetEnemy(s.enemyId);
                        if (e == null) continue;
                        bool isStrong = e.archetype == EnemyArchetype.Elite || e.archetype == EnemyArchetype.Tank;
                        if (strong == isStrong && !pool.Contains(s.enemyId)) pool.Add(s.enemyId);
                    }
            if (pool.Count == 0) return strong ? "brute" : "grunt";
            return pool[Random.Range(0, pool.Count)];
        }

        /// <summary>Applies a peaceful encounter's outcome. Returns the message to show.</summary>
        public static string Resolve(PlayerData d, Encounter e, bool accept)
        {
            if (!accept) return e.kind == "traveler" ? "\"Thank you anyway, dear.\"" : "You continue on your way.";
            switch (e.kind)
            {
                case "merchant":
                    if (d.coins < 1500) return "\"Come back with more coin, friend.\"";
                    d.coins -= 1500; d.expScrolls += 3;
                    return "Bought 3 EXP Scrolls.";
                case "traveler":
                    d.crystals += 20;
                    return "She presses a small crystal charm into your hand. +20 crystals";
                case "chest":
                {
                    int coins = Random.Range(2000, 6000);
                    d.coins += coins;
                    var pool = GameDatabase.Equipment.FindAll(x => x.rarity <= 5);
                    string eq = "";
                    if (pool.Count > 0 && Random.value < 0.6f)
                    {
                        var pick = pool[Random.Range(0, pool.Count)];
                        InventorySystem.AddEquipment(d, pick.id);
                        eq = " and " + pick.displayName;
                    }
                    return "Found " + coins.ToString("N0") + " coins" + eq + "!";
                }
                case "cave":
                    d.ascensionOre += 1; d.skillScrolls += 1;
                    return "Mined Ascension Ore ×1 and found a Skill Scroll.";
                case "cart":
                    d.coins += 1500; d.expScrolls += 1;
                    return "The farmer insists you take something. +1,500 coins, +1 EXP Scroll";
                default:
                    d.crystals += 10;
                    return "\"...We will meet again, at the end of the eclipse.\" The stranger vanishes. +10 crystals";
            }
        }

        /// <summary>A short journey-battle for a combat encounter, set in the region's scenery.</summary>
        public static MissionDefinition BuildBattle(Encounter e)
        {
            var r = GameDatabase.GetRegion(e.regionId);
            var m = new MissionDefinition
            {
                id = "ENC", chapter = 0, type = MissionType.Encounter, regionId = e.regionId, name = e.title,
                description = e.text, storyText = e.text, enemyLevel = e.level, recommendedLevel = e.level + 2,
                theme = r != null ? r.theme : new ArenaTheme(), parTime = 120f, timeLimit = 400f
            };
            if (e.kind == "boss") m.bossId = e.enemyId;
            else m.waves.Add(new WaveDefinition(new SpawnEntry(e.enemyId, e.enemyCount)));
            if (m.waves.Count == 0) m.waves.Add(new WaveDefinition(new SpawnEntry(RegionEnemy(e.regionId, false), 3)));
            int total = 0;
            foreach (var w in m.waves) total += w.TotalCount;
            m.killObjective = Mathf.Max(1, total);
            float scale = 1f + e.level * 0.12f;
            m.rewards = new RewardBundle { exp = Mathf.RoundToInt(400 * scale), coins = Mathf.RoundToInt(2000 * scale), crystals = e.kind == "ambush" ? 5 : e.kind == "rare" ? 30 : 50 };
            if (e.kind == "rare") m.rewards.ascensionOre = 1;
            if (e.kind == "boss") m.rewards.skillScrolls = 2;
            m.route.Add("Roadside");
            return m;
        }
    }
}
