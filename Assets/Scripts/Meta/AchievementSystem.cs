using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    public enum Medal { Bronze, Silver, Gold, Platinum }

    /// <summary>
    /// Achievements: goals across the whole game (story, fighting, summoning, the village, friends, co-op). Each
    /// earns a medal and a reward the moment it's reached; a congratulation pops up and the medal shows on the
    /// profile. Progress is read from the save, so nothing is lost if an achievement is added later.
    /// </summary>
    public static class AchievementSystem
    {
        public class Def
        {
            public string id, title, desc;
            public Medal medal;
            public int coins, crystals;
            public System.Func<PlayerData, int> progress;
            public int target;
        }

        public static readonly List<Def> All = new List<Def>();
        /// <summary>Unlocked since the last popup was shown (the UI shows them one by one).</summary>
        public static readonly Queue<Def> Popups = new Queue<Def>();
        static float nextCheck;

        static void A(string id, string title, string desc, Medal m, int target, System.Func<PlayerData, int> progress)
        {
            int coins = m == Medal.Bronze ? 2000 : m == Medal.Silver ? 6000 : m == Medal.Gold ? 15000 : 40000;
            int crystals = m == Medal.Bronze ? 30 : m == Medal.Silver ? 80 : m == Medal.Gold ? 200 : 500;
            All.Add(new Def { id = id, title = title, desc = desc, medal = m, target = target, progress = progress, coins = coins, crystals = crystals });
        }

        static AchievementSystem()
        {
            A("name", "Hello, Slayer", "Create your profile", Medal.Bronze, 1, d => d.profileDone ? 1 : 0);
            A("clear1", "First Steps", "Clear your first mission", Medal.Bronze, 1, d => d.missionsCleared);
            A("clear10", "On the Road", "Clear 10 missions", Medal.Silver, 10, d => d.missionsCleared);
            A("clear50", "Seasoned Slayer", "Clear 50 missions", Medal.Gold, 50, d => d.missionsCleared);
            A("clear150", "Living Legend", "Clear 150 missions", Medal.Platinum, 150, d => d.missionsCleared);
            A("kill100", "Demon Hunter", "Defeat 100 demons", Medal.Bronze, 100, d => d.totalKills);
            A("kill1000", "Blade Storm", "Defeat 1,000 demons", Medal.Silver, 1000, d => d.totalKills);
            A("kill5000", "Night's End", "Defeat 5,000 demons", Medal.Gold, 5000, d => d.totalKills);
            A("boss1", "Giant Slayer", "Defeat a boss", Medal.Bronze, 1, d => d.bossesDefeated);
            A("boss10", "Boss Breaker", "Defeat 10 bosses", Medal.Silver, 10, d => d.bossesDefeated);
            A("boss50", "Tyrant's Bane", "Defeat 50 bosses", Medal.Gold, 50, d => d.bossesDefeated);
            A("sum1", "First Summon", "Summon at the shrine", Medal.Bronze, 1, d => d.totalSummons);
            A("sum100", "Shrine Regular", "Summon 100 times", Medal.Silver, 100, d => d.totalSummons);
            A("sum500", "Moonlit Devotion", "Summon 500 times", Medal.Gold, 500, d => d.totalSummons);
            A("roster10", "Growing Team", "Own 10 slayers", Medal.Bronze, 10, d => d.characters.Count);
            A("roster25", "Full Dojo", "Own 25 slayers", Medal.Silver, 25, d => d.characters.Count);
            A("mythic", "Touched by Myth", "Own a Mythic slayer", Medal.Gold, 1, d => CountRarity(d, 6));
            A("lv30", "Honed Edge", "Raise a slayer to Lv 30", Medal.Bronze, 30, MaxLevel);
            A("lv80", "Master of Breath", "Raise a slayer to Lv 80", Medal.Gold, 80, MaxLevel);
            A("chest1", "Treasure Seeker", "Find a hidden chest in Kiriha", Medal.Bronze, 1, d => d.chestsFound);
            A("chest6", "Nothing Left Hidden", "Find 6 hidden chests", Medal.Silver, 6, d => d.chestsFound);
            A("party1", "Better Together", "Form a party of three", Medal.Bronze, 1, d => d.partiesFormed);
            A("coop1", "Through the Gate", "Clear a co-op gate", Medal.Silver, 1, d => d.coopClears);
            A("coop10", "Gatekeeper", "Clear 10 co-op gates", Medal.Gold, 10, d => d.coopClears);
            A("abyss", "Abyss Walker", "Clear the Gate of the Abyss", Medal.Platinum, 1, d => d.IsMissionCleared("COOP-3") ? 1 : 0);
            A("friend1", "New Friend", "Add a friend", Medal.Bronze, 1, d => d.friends.Count);
            A("friend10", "Popular", "Have 10 friends", Medal.Silver, 10, d => d.friends.Count);
            A("chat20", "Chatterbox", "Send 20 messages", Medal.Bronze, 20, d => d.messagesSent);
            A("login7", "Regular", "Log in on 7 days", Medal.Silver, 7, d => d.loginDays);
            A("login30", "Devoted", "Log in on 30 days", Medal.Gold, 30, d => d.loginDays);
        }

        static int MaxLevel(PlayerData d)
        {
            int m = 0;
            foreach (var c in d.characters) m = Mathf.Max(m, c.level);
            return m;
        }

        static int CountRarity(PlayerData d, int rarity)
        {
            int n = 0;
            foreach (var c in d.characters)
            {
                var def = GameDatabase.GetCharacter(c.id);
                if (def != null && def.rarity >= rarity) n++;
            }
            return n;
        }

        public static AchievementState State(PlayerData d, string id)
        {
            var s = d.achievements.Find(a => a.id == id);
            if (s == null) { s = new AchievementState { id = id }; d.achievements.Add(s); }
            return s;
        }

        public static int Progress(PlayerData d, Def a) { return Mathf.Min(a.target, a.progress(d)); }

        /// <summary>Unlocks everything newly reached: rewards go straight in, the popup queue gets the rest.</summary>
        public static bool Check(PlayerData d, bool force = false)
        {
            if (d == null) return false;
            if (!force && Time.unscaledTime < nextCheck) return false;
            nextCheck = Time.unscaledTime + 1f;
            bool any = false;
            foreach (var a in All)
            {
                var s = State(d, a.id);
                if (s.unlocked || a.progress(d) < a.target) continue;
                s.unlocked = true;
                s.claimed = true;
                s.when = System.DateTime.Now.Ticks;
                d.coins += a.coins;
                d.crystals += a.crystals;
                Popups.Enqueue(a);
                any = true;
            }
            return any;
        }

        public static int Count(PlayerData d, Medal? medal = null)
        {
            int n = 0;
            foreach (var a in All)
            {
                if (medal.HasValue && a.medal != medal.Value) continue;
                var s = d.achievements.Find(x => x.id == a.id);
                if (s != null && s.unlocked) n++;
            }
            return n;
        }

        public static Color MedalColor(Medal m)
        {
            switch (m)
            {
                case Medal.Bronze: return new Color(0.85f, 0.55f, 0.3f);
                case Medal.Silver: return new Color(0.82f, 0.86f, 0.92f);
                case Medal.Gold: return new Color(1f, 0.8f, 0.3f);
                default: return new Color(0.7f, 0.95f, 1f);
            }
        }
    }
}
