using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// PvP: modes, ranked tiers, trophies and match making.
    ///   Modes — 3v3 Battle (first to 10 knockouts), Moon Crystal (hold the crystal zone to score), Boss Rush (both
    ///   teams race to deal the most damage to a boss while fighting each other).
    ///   Ranked — trophies move you through Bronze, Silver, Gold, Platinum, Diamond and Master (three divisions each
    ///   below Master); casual matches don't change trophies.
    /// NOT ONLINE YET: there is no game server, so matchmaking picks opponents of your rank from simulated
    /// players and the other five slayers are AI (<see cref="PartySlayer"/>). The match, modes, scoring and ranks
    /// are real; a server (Photon, Unity Netcode + Relay/Lobby, or similar) would replace the bots with people.
    /// </summary>
    public static class PvpSystem
    {
        public static readonly string[] ModeNames = { "3v3 Battle", "Moon Crystal", "Boss Rush" };
        public static readonly string[] ModeDesc =
        {
            "Knock out the other team. First to 10 knockouts — or the most when time runs out — wins.",
            "Hold the Moon Crystal zone in the centre. Your team scores while it stands alone in the zone.",
            "A boss appears between the teams. Deal the most damage to it (knockouts count too) to win.",
        };
        public const float MatchSeconds = 150f;
        public const int BattleTarget = 10;
        public const int CrystalTarget = 60;

        public struct Tier
        {
            public string name;
            public int min;
            public Color color;
        }

        public static readonly Tier[] Tiers =
        {
            new Tier { name = "Bronze", min = 0, color = new Color(0.85f, 0.55f, 0.3f) },
            new Tier { name = "Silver", min = 300, color = new Color(0.82f, 0.86f, 0.92f) },
            new Tier { name = "Gold", min = 700, color = new Color(1f, 0.8f, 0.3f) },
            new Tier { name = "Platinum", min = 1200, color = new Color(0.55f, 0.95f, 0.9f) },
            new Tier { name = "Diamond", min = 1800, color = new Color(0.5f, 0.75f, 1f) },
            new Tier { name = "Master", min = 2600, color = new Color(1f, 0.4f, 0.75f) },
        };

        public static int TierIndex(int trophies)
        {
            int t = 0;
            for (int i = 0; i < Tiers.Length; i++) if (trophies >= Tiers[i].min) t = i;
            return t;
        }

        /// <summary>"Gold II" style name.</summary>
        public static string RankName(int trophies)
        {
            int ti = TierIndex(trophies);
            if (ti == Tiers.Length - 1) return Tiers[ti].name;
            int span = Tiers[ti + 1].min - Tiers[ti].min;
            int div = 3 - Mathf.Clamp((trophies - Tiers[ti].min) * 3 / Mathf.Max(1, span), 0, 2);
            return Tiers[ti].name + " " + (div == 3 ? "III" : div == 2 ? "II" : "I");
        }

        /// <summary>0..1 progress toward the next tier.</summary>
        public static float TierProgress(int trophies)
        {
            int ti = TierIndex(trophies);
            if (ti == Tiers.Length - 1) return 1f;
            return Mathf.InverseLerp(Tiers[ti].min, Tiers[ti + 1].min, trophies);
        }

        static readonly string[] BotNames = GamerNames.All;

        /// <summary>Builds a match: your team (you + two), their team (three), an arena and the mode rules.</summary>
        public static MissionDefinition MakeMatch(PlayerData d, int mode, bool ranked)
        {
            mode = Mathf.Clamp(mode, 0, 2);
            int level = VillageHub.TeamLevel(d);
            var r = new System.Random(System.Environment.TickCount);
            var pool = GameDatabase.Characters.FindAll(c => !c.npc && !c.designTest);
            var names = new List<string>(BotNames);
            if (!string.IsNullOrEmpty(d.playerName)) names.Remove(d.playerName);
            var m = new MissionDefinition
            {
                id = "PVP-" + (mode + 1), name = (ranked ? "Ranked " : "") + ModeNames[mode], type = MissionType.Side, pvpMode = mode, pvpRanked = ranked,
                enemyLevel = level, timeLimit = 9999f, killObjective = 0, parTime = MatchSeconds, storyText = ModeDesc[mode]
            };
            // Arenas rotate through the new worlds.
            var kinds = new[] { EnvironmentKind.Forest, EnvironmentKind.Mountain, EnvironmentKind.DemonLand };
            var region = GameDatabase.Regions.Find(x => x.theme != null && x.theme.kind == kinds[(mode + System.DateTime.Now.Hour) % 3]);
            m.theme = region != null ? region.theme : new ArenaTheme();
            for (int i = 0; i < 2; i++) { m.coopAllyIds.Add(PickSlayer(pool, r, m)); m.coopAllyNames.Add(PickName(names, r)); }
            for (int i = 0; i < 3; i++) { m.pvpEnemyIds.Add(PickSlayer(pool, r, m)); m.pvpEnemyNames.Add(PickName(names, r)); }
            float scale = 1f + level * 0.12f;
            m.rewards = new RewardBundle { exp = Mathf.RoundToInt(600 * scale), coins = Mathf.RoundToInt(2000 * scale), expScrolls = 1 };
            m.firstClearRewards = new RewardBundle { crystals = 50 };
            return m;
        }

        static string PickSlayer(List<CharacterDefinition> pool, System.Random r, MissionDefinition m)
        {
            if (pool.Count == 0) return GameDatabase.Protagonist;
            for (int g = 0; g < 10; g++)
            {
                var id = pool[r.Next(pool.Count)].id;
                if (!m.coopAllyIds.Contains(id) && !m.pvpEnemyIds.Contains(id)) return id;
            }
            return pool[r.Next(pool.Count)].id;
        }

        static string PickName(List<string> names, System.Random r)
        {
            if (names.Count == 0) return "Slayer" + r.Next(100);
            int i = r.Next(names.Count);
            string n = names[i];
            names.RemoveAt(i);
            return n;
        }

        /// <summary>Trophies for a ranked result (bigger wins at low ranks, bigger losses near the top).</summary>
        public static int Record(PlayerData d, bool victory, bool ranked, bool draw)
        {
            if (victory) d.pvpWins++; else if (!draw) d.pvpLosses++;
            if (!ranked || draw) return 0;
            int ti = TierIndex(d.pvpTrophies);
            int delta = victory ? 14 - ti : -(5 + ti);
            d.pvpTrophies = Mathf.Max(0, d.pvpTrophies + delta);
            d.pvpBestTrophies = Mathf.Max(d.pvpBestTrophies, d.pvpTrophies);
            return delta;
        }
    }
}
