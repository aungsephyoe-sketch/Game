using System.Collections.Generic;

namespace HashiraChronicles
{
    /// <summary>Daily and weekly missions with automatic resets and claimable rewards.</summary>
    public static class QuestSystem
    {
        public class QuestDef
        {
            public string id, title, kind;
            public int target;
            public bool daily;
            public RewardBundle reward;
        }

        public static readonly List<QuestDef> Defs = new List<QuestDef>
        {
            new QuestDef { id = "d_clear", title = "Complete 3 missions", kind = "clear", target = 3, daily = true, reward = new RewardBundle { crystals = 20, coins = 3000 } },
            new QuestDef { id = "d_kill", title = "Defeat 100 demons", kind = "kill", target = 100, daily = true, reward = new RewardBundle { crystals = 20, expScrolls = 3 } },
            new QuestDef { id = "d_upgrade", title = "Upgrade a slayer", kind = "upgrade", target = 1, daily = true, reward = new RewardBundle { coins = 2000, skillScrolls = 1 } },
            new QuestDef { id = "d_boss", title = "Defeat a boss", kind = "boss", target = 1, daily = true, reward = new RewardBundle { crystals = 15, ascensionOre = 1 } },
            new QuestDef { id = "d_summon", title = "Perform a summon", kind = "summon", target = 1, daily = true, reward = new RewardBundle { coins = 2500 } },
            new QuestDef { id = "w_clear", title = "Complete 15 missions", kind = "clear", target = 15, daily = false, reward = new RewardBundle { crystals = 100, coins = 15000 } },
            new QuestDef { id = "w_kill", title = "Defeat 500 demons", kind = "kill", target = 500, daily = false, reward = new RewardBundle { crystals = 80, skillScrolls = 5 } },
            new QuestDef { id = "w_boss", title = "Defeat 5 bosses", kind = "boss", target = 5, daily = false, reward = new RewardBundle { crystals = 80, ascensionOre = 4 } },
            new QuestDef { id = "w_summon", title = "Summon 10 times", kind = "summon", target = 10, daily = false, reward = new RewardBundle { crystals = 50 } },
            // More dailies.
            new QuestDef { id = "d_coop", title = "Clear a co-op gate", kind = "coop", target = 1, daily = true, reward = new RewardBundle { crystals = 25, coins = 4000 } },
            new QuestDef { id = "d_pvp", title = "Play 2 arena matches", kind = "pvp", target = 2, daily = true, reward = new RewardBundle { crystals = 20, coins = 3000 } },
            new QuestDef { id = "d_ult", title = "Unleash 3 ultimates", kind = "ultimate", target = 3, daily = true, reward = new RewardBundle { crystals = 15, expScrolls = 2 } },
            new QuestDef { id = "d_skill", title = "Use skills 20 times", kind = "skill", target = 20, daily = true, reward = new RewardBundle { coins = 3000, skillScrolls = 1 } },
            new QuestDef { id = "d_dodge", title = "Land 3 perfect dodges", kind = "dodge", target = 3, daily = true, reward = new RewardBundle { crystals = 15 } },
            new QuestDef { id = "d_stars", title = "Clear a mission with 3 stars", kind = "stars3", target = 1, daily = true, reward = new RewardBundle { crystals = 15, coins = 2500 } },
            new QuestDef { id = "d_chat", title = "Message a friend", kind = "chat", target = 1, daily = true, reward = new RewardBundle { coins = 1500 } },
            // More weeklies.
            new QuestDef { id = "w_coop", title = "Clear 7 co-op gates", kind = "coop", target = 7, daily = false, reward = new RewardBundle { crystals = 150, ascensionOre = 3 } },
            new QuestDef { id = "w_pvpwin", title = "Win 10 arena matches", kind = "pvpwin", target = 10, daily = false, reward = new RewardBundle { crystals = 150, coins = 20000 } },
            new QuestDef { id = "w_event", title = "Clear 5 event quests", kind = "event", target = 5, daily = false, reward = new RewardBundle { crystals = 100, expScrolls = 6 } },
            new QuestDef { id = "w_ult", title = "Unleash 20 ultimates", kind = "ultimate", target = 20, daily = false, reward = new RewardBundle { crystals = 80, skillScrolls = 4 } },
            new QuestDef { id = "w_stars", title = "Clear 10 missions with 3 stars", kind = "stars3", target = 10, daily = false, reward = new RewardBundle { crystals = 120 } },
            new QuestDef { id = "w_upgrade", title = "Upgrade slayers 10 times", kind = "upgrade", target = 10, daily = false, reward = new RewardBundle { coins = 25000, ascensionOre = 2 } },
        };

        static string DailyKey() { return System.DateTime.UtcNow.ToString("yyyyMMdd"); }
        static string WeeklyKey() { var n = System.DateTime.UtcNow; return n.Year + "w" + ((n.DayOfYear + 6) / 7); }

        public static void EnsureReset(PlayerData d)
        {
            string dk = DailyKey(), wk = WeeklyKey();
            if (d.dailyKey != dk)
            {
                d.dailyKey = dk;
                d.quests.RemoveAll(q => q.id.StartsWith("d_"));
            }
            if (d.weeklyKey != wk)
            {
                d.weeklyKey = wk;
                d.quests.RemoveAll(q => q.id.StartsWith("w_"));
            }
            foreach (var def in Defs)
                if (d.quests.Find(q => q.id == def.id) == null) d.quests.Add(new QuestState { id = def.id });
        }

        public static QuestState State(PlayerData d, string id)
        {
            EnsureReset(d);
            return d.quests.Find(q => q.id == id);
        }

        static bool hooked;

        /// <summary>Listens to battle events (ultimates, skills, perfect dodges) once at startup.</summary>
        public static void Hook()
        {
            if (hooked) return;
            hooked = true;
            GameEvents.UltimateStarted += (p, a) => Report("ultimate", 1);
            GameEvents.SkillUsed += (p, a) => Report("skill", 1);
            GameEvents.PerfectDodge += () => Report("dodge", 1);
        }

        public static void Report(string kind, int amount)
        {
            var gm = GameManager.Instance;
            if (gm == null || gm.Data == null) return;
            var d = gm.Data;
            EnsureReset(d);
            foreach (var def in Defs)
            {
                if (def.kind != kind) continue;
                var s = d.quests.Find(q => q.id == def.id);
                if (s != null && !s.claimed) s.progress = System.Math.Min(def.target, s.progress + amount);
            }
        }

        public static int Claimable(PlayerData d)
        {
            EnsureReset(d);
            int n = 0;
            foreach (var def in Defs)
            {
                var s = d.quests.Find(q => q.id == def.id);
                if (s != null && !s.claimed && s.progress >= def.target) n++;
            }
            return n;
        }

        public static bool Claim(PlayerData d, QuestDef def)
        {
            var s = State(d, def.id);
            if (s == null || s.claimed || s.progress < def.target) return false;
            s.claimed = true;
            InventorySystem.AddCurrencies(d, def.reward);
            return true;
        }
    }
}
