using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The player's profile: a name that passes the name filter, their age and birthday. Every year once the
    /// birthday has passed the game gives 500 diamonds and 10,000 gold (and the age goes up by one).
    /// </summary>
    public static class ProfileSystem
    {
        public const int BirthdayDiamonds = 500, BirthdayGold = 10000;

        // Words a name may not contain anywhere (after normalising look-alike characters).
        static readonly string[] Banned =
        {
            "fuck", "fuk", "fck", "shit", "bitch", "cunt", "cock", "pussy", "penis", "vagina", "porn", "nude", "boob", "slut", "whore",
            "nigg", "nigger", "fagg", "retard", "rape", "nazi", "hitler", "dildo", "asshole", "bastard", "twat", "wank", "jizz", "horny",
            "sexy", "hentai", "milf", "anal", "orgasm", "erotic", "stripper", "killyourself", "suicide", "molest", "pedo", "incest"
        };
        // Words blocked only when they stand alone (so "Cassie" or "Titan" are fine).
        static readonly string[] BannedWords = { "ass", "sex", "cum", "tit", "tits", "dick", "fag", "kys", "piss", "hoe", "xxx", "nsfw", "suck", "damn", "hell", "crap", "butt", "poop", "kill", "die", "dead" };

        static string Normalise(string s)
        {
            var sb = new System.Text.StringBuilder();
            foreach (char ch0 in s.ToLowerInvariant())
            {
                char ch = ch0;
                switch (ch) { case '0': ch = 'o'; break; case '1': case '!': case '|': ch = 'i'; break; case '3': ch = 'e'; break; case '4': case '@': ch = 'a'; break; case '5': case '$': ch = 's'; break; case '7': ch = 't'; break; case '8': ch = 'b'; break; }
                sb.Append(ch);
            }
            return sb.ToString();
        }

        /// <summary>True when the name is allowed; otherwise reason says why.</summary>
        public static bool CheckName(string name, out string reason)
        {
            reason = "";
            name = (name ?? "").Trim();
            if (name.Length < 2) { reason = "Your name needs at least 2 letters."; return false; }
            if (name.Length > 14) { reason = "Your name can be at most 14 characters."; return false; }
            bool letter = false;
            foreach (char ch in name)
            {
                if (char.IsLetter(ch)) letter = true;
                else if (!char.IsDigit(ch) && ch != ' ' && ch != '-' && ch != '_' && ch != '.') { reason = "Use letters, numbers, spaces, - or _ only."; return false; }
            }
            if (!letter) { reason = "Your name needs some letters."; return false; }
            string n = Normalise(name);
            string squashed = n.Replace(" ", "").Replace("-", "").Replace("_", "").Replace(".", "");
            // Also catch spaced-out or doubled letters ("f u c k", "fuuuck").
            var dedup = new System.Text.StringBuilder();
            foreach (char ch in squashed) if (dedup.Length == 0 || dedup[dedup.Length - 1] != ch) dedup.Append(ch);
            foreach (var b in Banned)
                if (squashed.Contains(b) || dedup.ToString().Contains(b)) { reason = "That name isn't allowed. Please choose another."; return false; }
            foreach (var word in n.Split(new[] { ' ', '-', '_', '.' }, System.StringSplitOptions.RemoveEmptyEntries))
                foreach (var b in BannedWords)
                    if (word == b) { reason = "That name isn't allowed. Please choose another."; return false; }
            return true;
        }

        public static void Save(PlayerData d, string name, int age, int month, int day)
        {
            d.playerName = name.Trim();
            d.playerAge = Mathf.Clamp(age, 1, 120);
            d.birthMonth = Mathf.Clamp(month, 1, 12);
            d.birthDay = Mathf.Clamp(day, 1, System.DateTime.DaysInMonth(2024, d.birthMonth));
            d.profileDone = true;
            d.profileYear = System.DateTime.Now.Year;
            // A birthday that already passed this year is celebrated next year; one that is today counts now.
            var now = System.DateTime.Now;
            d.birthdayRewardYear = HasPassed(d, now) && !IsToday(d, now) ? now.Year : now.Year - 1;
        }

        static System.DateTime ThisYears(PlayerData d, System.DateTime now)
        {
            int day = Mathf.Min(d.birthDay, System.DateTime.DaysInMonth(now.Year, d.birthMonth));
            return new System.DateTime(now.Year, d.birthMonth, day);
        }

        static bool HasPassed(PlayerData d, System.DateTime now) { return now.Date >= ThisYears(d, now); }
        static bool IsToday(PlayerData d, System.DateTime now) { return now.Date == ThisYears(d, now); }

        /// <summary>True when this year's birthday has arrived and its gift hasn't been given yet.</summary>
        public static bool BirthdayDue(PlayerData d)
        {
            if (d == null || !d.profileDone) return false;
            var now = System.DateTime.Now;
            return HasPassed(d, now) && d.birthdayRewardYear < now.Year;
        }

        public static void ClaimBirthday(PlayerData d)
        {
            if (!BirthdayDue(d)) return;
            var now = System.DateTime.Now;
            d.crystals += BirthdayDiamonds;
            d.coins += BirthdayGold;
            if (now.Year > d.profileYear) d.playerAge++;
            d.birthdayRewardYear = now.Year;
        }

        static readonly string[] Welcomes =
        {
            "Welcome back! The demons won't know what hit them.",
            "Great to see you! Ready for today's adventure?",
            "You're here! I saved you a dumpling from the festival.",
            "Welcome back, slayer. The village feels safer already.",
            "Good to see you! Let's make today legendary.",
            "Hey! I've been practising my new move — want to see?",
            "You came back! The team's been waiting for you.",
        };

        public static string Greeting(PlayerData d)
        {
            string name = string.IsNullOrEmpty(d.playerName) ? "friend" : d.playerName;
            return "Hi " + name + "! " + Welcomes[Random.Range(0, Welcomes.Length)];
        }
    }
}
