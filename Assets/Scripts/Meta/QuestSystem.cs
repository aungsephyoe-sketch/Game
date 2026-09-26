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
