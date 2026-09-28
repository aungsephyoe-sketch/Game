using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Profile identity and friends:
    ///   • a gamer code (BL-XXXX-XXXX) made once per save, shown on the profile and used to add friends;
    ///   • the player name can be changed once every 7 days (same name filter as the first time);
    ///   • a friends list: add by code, accept requests, remove; each friend has a status and a chat thread.
    /// There is no server yet, so the other players are simulated: any valid code resolves to a (made-up but
    /// stable) player, their online status follows a daily rhythm, and their messages come from
    /// <see cref="ChatBrain"/>. Every message both ways goes through the chat filter.
    /// </summary>
    public static class SocialSystem
    {
        public const int MaxFriends = 50;
        const string CodeChars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

        static readonly string[] SimNames =
        {
            "Kaede", "Haruto", "MoonlitRiver", "Sumi", "TsubakiBlade", "Riku_07", "Hotaru", "Asagi", "Kuro", "YuzuTea", "Botan", "Shin",
            "Akari", "Nagisa", "Ryo", "Hinata", "Komorebi", "Sora_Kaze", "Minato", "Tomoe", "Ember_Fox", "Mochi", "LanternLight", "Takumi",
            "Ichigo", "Rin_Rin", "StormPetal", "Kazu", "NightOwl", "Yuki22", "PeachBlossom", "Daichi", "Mei", "Suzu", "Kenta", "Aoi"
        };

        struct Pending
        {
            public string code;
            public string text;
            public float at;
        }

        static readonly List<Pending> pending = new List<Pending>();
        static readonly Dictionary<string, ChatPersona> personas = new Dictionary<string, ChatPersona>();
        static float nextPing = -1f;
        /// <summary>The friend currently typing (for the "typing…" line) and until when.</summary>
        public static string TypingCode { get; private set; }
        public static float TypingUntil { get; private set; }

        // ------------------------------------------------------------------ Identity

        public static string EnsureCode(PlayerData d)
        {
            if (!string.IsNullOrEmpty(d.gamerCode)) return d.gamerCode;
            var r = new System.Random(System.Environment.TickCount ^ System.Guid.NewGuid().GetHashCode());
            d.gamerCode = MakeCode(r);
            return d.gamerCode;
        }

        static string MakeCode(System.Random r)
        {
            var sb = new System.Text.StringBuilder("BL-");
            for (int i = 0; i < 8; i++)
            {
                if (i == 4) sb.Append('-');
                sb.Append(CodeChars[r.Next(CodeChars.Length)]);
            }
            return sb.ToString();
        }

        /// <summary>Tidies what someone typed into BL-XXXX-XXXX form; null when it can't be a code.</summary>
        public static string NormaliseCode(string input)
        {
            if (string.IsNullOrEmpty(input)) return null;
            var sb = new System.Text.StringBuilder();
            foreach (char c in input.ToUpperInvariant()) if (char.IsLetterOrDigit(c)) sb.Append(c);
            string s = sb.ToString();
            if (s.StartsWith("BL")) s = s.Substring(2);
            if (s.Length != 8) return null;
            foreach (char c in s) if (CodeChars.IndexOf(c) < 0) return null;
            return "BL-" + s.Substring(0, 4) + "-" + s.Substring(4);
        }

        public static System.TimeSpan NameChangeWait(PlayerData d)
        {
            if (d.nameChangedTicks <= 0) return System.TimeSpan.Zero;
            var next = new System.DateTime(d.nameChangedTicks).AddDays(7);
            var left = next - System.DateTime.Now;
            return left.Ticks > 0 ? left : System.TimeSpan.Zero;
        }

        public static bool CanChangeName(PlayerData d) { return NameChangeWait(d) == System.TimeSpan.Zero; }

        public static bool ChangeName(PlayerData d, string name, out string reason)
        {
            reason = "";
            if (!CanChangeName(d)) { reason = "You can change your name again in " + WaitText(NameChangeWait(d)) + "."; return false; }
            if (!ProfileSystem.CheckName(name, out reason)) return false;
            if (name.Trim() == d.playerName) { reason = "That's already your name."; return false; }
            d.playerName = name.Trim();
            d.nameChangedTicks = System.DateTime.Now.Ticks;
            return true;
        }

        public static string WaitText(System.TimeSpan t)
        {
            if (t.TotalDays >= 1) return Mathf.CeilToInt((float)t.TotalDays) + " days";
            if (t.TotalHours >= 1) return Mathf.CeilToInt((float)t.TotalHours) + " hours";
            return Mathf.Max(1, Mathf.CeilToInt((float)t.TotalMinutes)) + " minutes";
        }

        // ------------------------------------------------------------------ Players

        static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s) h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        /// <summary>The (simulated) player behind a code.</summary>
        public static FriendEntry Lookup(string code)
        {
            int h = Hash(code);
            var r = new System.Random(h);
            var pool = GameDatabase.Characters.FindAll(c => !c.npc && !c.designTest);
            string main = pool.Count > 0 ? pool[r.Next(pool.Count)].id : GameDatabase.Protagonist;
            return new FriendEntry { code = code, name = SimNames[r.Next(SimNames.Length)] + (r.Next(3) == 0 ? (r.Next(90) + 10).ToString() : ""), charId = main, level = 5 + r.Next(70), seed = h };
        }

        /// <summary>A player met in the village (their code is stable for their name).</summary>
        public static FriendEntry ForName(string name, string charId, int level)
        {
            var r = new System.Random(Hash(name) ^ 0x5bd1e995);
            string code = MakeCode(r);
            return new FriendEntry { code = code, name = name, charId = charId, level = level, seed = Hash(code) };
        }

        public static bool IsFriend(PlayerData d, string code) { return d.friends.Exists(f => f.code == code); }

        public static bool AddFriend(PlayerData d, FriendEntry e, out string reason)
        {
            reason = "";
            if (e == null) { reason = "No player found."; return false; }
            if (e.code == EnsureCode(d)) { reason = "That's your own code!"; return false; }
            if (IsFriend(d, e.code)) { reason = e.name + " is already your friend."; return false; }
            if (d.friends.Count >= MaxFriends) { reason = "Your friends list is full (" + MaxFriends + ")."; return false; }
            e.since = System.DateTime.Now.Ticks;
            d.friends.Add(e);
            d.friendRequests.RemoveAll(q => q.code == e.code);
            // They say hi shortly after.
            Queue(e.code, "hey thanks for the add!", Random.Range(3f, 7f));
            return true;
        }

        public static void RemoveFriend(PlayerData d, string code)
        {
            d.friends.RemoveAll(f => f.code == code);
            d.dms.RemoveAll(t => t.code == code);
        }

        /// <summary>Online now? Each friend keeps their own daily hours.</summary>
        public static bool IsOnline(FriendEntry f)
        {
            var now = System.DateTime.Now;
            int slot = now.DayOfYear * 24 + now.Hour;
            var r = new System.Random(f.seed ^ slot);
            return r.NextDouble() < 0.55;
        }

        public static string Status(FriendEntry f)
        {
            if (IsOnline(f))
            {
                var r = new System.Random(f.seed + System.DateTime.Now.Hour);
                string[] doing = { "In the village", "In a co-op gate", "Summoning", "On a story mission", "In the forest", "Online" };
                return doing[r.Next(doing.Length)];
            }
            int hours = 1 + new System.Random(f.seed + System.DateTime.Now.DayOfYear).Next(20);
            return "Last online " + hours + "h ago";
        }

        // ------------------------------------------------------------------ Messages

        public static DmThread Thread(PlayerData d, string code)
        {
            var t = d.dms.Find(x => x.code == code);
            if (t == null) { t = new DmThread { code = code }; d.dms.Add(t); }
            return t;
        }

        static ChatPersona PersonaFor(FriendEntry f)
        {
            ChatPersona p;
            if (!personas.TryGetValue(f.code, out p))
            {
                var def = GameDatabase.GetCharacter(f.charId);
                p = ChatPersona.From(f.name, f.seed, def != null ? def.displayName : null);
                personas[f.code] = p;
            }
            return p;
        }

        public static void Send(PlayerData d, FriendEntry f, string text)
        {
            if (string.IsNullOrEmpty(text) || f == null) return;
            text = text.Trim();
            if (text.Length == 0) return;
            if (text.Length > 160) text = text.Substring(0, 160);
            var t = Thread(d, f.code);
            t.lines.Add(new DmLine { mine = true, text = ProfileSystem.MaskChat(text), time = System.DateTime.Now.Ticks });
            Trim(t);
            ChatBrain.Learn(text);
            d.messagesSent++;
            // Offline friends answer later; online ones after "typing".
            float delay = IsOnline(f) ? Random.Range(1.5f, 4f) : Random.Range(25f, 60f);
            var replies = ChatBrain.Reply(PersonaFor(f), text);
            foreach (var r in replies)
            {
                Queue(f.code, r, delay);
                delay += 0.8f + r.Length * 0.06f;
            }
        }

        static void Queue(string code, string text, float delay)
        {
            pending.Add(new Pending { code = code, text = text, at = Time.unscaledTime + delay });
        }

        static void Trim(DmThread t) { while (t.lines.Count > 80) t.lines.RemoveAt(0); }

        /// <summary>Delivers replies whose time has come and, now and then, a friend messages you first.</summary>
        public static void Tick(PlayerData d)
        {
            if (d == null) return;
            float now = Time.unscaledTime;
            TypingCode = null;
            for (int i = pending.Count - 1; i >= 0; i--)
            {
                var p = pending[i];
                if (now < p.at)
                {
                    if (p.at - now < 2.5f) { TypingCode = p.code; TypingUntil = p.at; }
                    continue;
                }
                pending.RemoveAt(i);
                var f = d.friends.Find(x => x.code == p.code);
                if (f == null) continue;
                var t = Thread(d, p.code);
                t.lines.Add(new DmLine { mine = false, text = ProfileSystem.MaskChat(p.text), time = System.DateTime.Now.Ticks });
                Trim(t);
                f.unread++;
                if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("click", 0.35f, 1.6f);
            }
            if (nextPing < 0f) nextPing = now + Random.Range(60f, 120f);
            if (now > nextPing && d.friends.Count > 0)
            {
                nextPing = now + Random.Range(90f, 240f);
                var online = d.friends.FindAll(IsOnline);
                if (online.Count > 0)
                {
                    var f = online[Random.Range(0, online.Count)];
                    Queue(f.code, ChatBrain.Chatter(PersonaFor(f)), 0.5f);
                }
            }
        }

        public static int Unread(PlayerData d)
        {
            int n = 0;
            foreach (var f in d.friends) n += f.unread;
            return n + d.friendRequests.Count;
        }

        /// <summary>Someone you partied with may send you a friend request.</summary>
        public static void MaybeRequest(PlayerData d, string name, string charId, int level)
        {
            if (d == null || Random.value > 0.5f) return;
            var e = ForName(name, charId, level);
            if (IsFriend(d, e.code) || d.friendRequests.Exists(q => q.code == e.code)) return;
            d.friendRequests.Add(e);
            if (d.friendRequests.Count > 10) d.friendRequests.RemoveAt(0);
        }

        /// <summary>Players you could add (from the village, stable per day).</summary>
        public static List<FriendEntry> Suggestions(PlayerData d, int count)
        {
            var list = new List<FriendEntry>();
            var r = new System.Random(System.DateTime.Now.DayOfYear * 97 + (d.gamerCode ?? "").GetHashCode());
            for (int i = 0; i < 40 && list.Count < count; i++)
            {
                var e = Lookup(MakeCode(r));
                if (!IsFriend(d, e.code)) list.Add(e);
            }
            return list;
        }
    }
}
