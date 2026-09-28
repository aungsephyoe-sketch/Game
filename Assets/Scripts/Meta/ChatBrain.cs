using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>How one (simulated) player types: how sloppy, how excitable, which faces they use.</summary>
    public class ChatPersona
    {
        public string name;
        public float typo, lowercase, emote, slang, excite, split;
        public int faceSet;
        public string main;
        public System.Random rng;

        public static ChatPersona From(string name, int seed, string mainCharacter = null)
        {
            var r = new System.Random(seed);
            return new ChatPersona
            {
                name = name, rng = new System.Random(seed * 7 + System.Environment.TickCount), main = mainCharacter,
                typo = (float)r.NextDouble() * 0.12f, lowercase = 0.4f + (float)r.NextDouble() * 0.6f, emote = 0.15f + (float)r.NextDouble() * 0.45f,
                slang = 0.2f + (float)r.NextDouble() * 0.7f, excite = (float)r.NextDouble(), split = (float)r.NextDouble() * 0.35f,
                faceSet = r.Next(ChatBrain.FaceSets.Length)
            };
        }
    }

    /// <summary>
    /// The chat voice of the other players (village chat, party members, friends' messages). It runs locally —
    /// there is no server and no AI model behind it — but it tries hard to sound like people typing:
    ///   • each persona has its own habits: all lowercase or not, slang (u, ur, rn, ngl, tbh), typos that are
    ///     sometimes corrected in a second message (*their), stretched words (sooo), and text faces (:D ^_^ T_T);
    ///   • it recognises what you say (greetings, questions about the game, party invites, compliments, jokes,
    ///     how you feel, goodbyes) and answers, asks things back and remembers what you told it this session;
    ///   • it learns from you: the words you use (and your own slang) are saved with your profile, and the others
    ///     start using your slang and sometimes remix your phrases, the way a friend group picks up each other's talk.
    /// Anything anyone says passes the chat filter, which blurs bad words.
    /// </summary>
    public static class ChatBrain
    {
        public static readonly string[][] FaceSets =
        {
            new[] { ":)", ":D", "xD", "^^" },
            new[] { "^_^", "^-^", ":3", "owo" },
            new[] { ":P", ";)", "B)", ":]" },
            new[] { "T_T", ";-;", "o_o", ":/" },
            new[] { "\\o/", "<3", "o/", ">_<" },
            new[] { "¯\\_(ツ)_/¯", "(>_<)", "(^o^)", "(-_-)" },
        };

        static readonly string[,] Abbrev =
        {
            { "you", "u" }, { "your", "ur" }, { "you're", "ur" }, { "are", "r" }, { "right now", "rn" }, { "to be honest", "tbh" },
            { "going to", "gonna" }, { "want to", "wanna" }, { "because", "cuz" }, { "okay", "ok" }, { "please", "pls" },
            { "thanks", "ty" }, { "thank you", "ty" }, { "i don't know", "idk" }, { "not gonna lie", "ngl" }, { "oh my god", "omg" },
            { "for real", "fr" }, { "be right back", "brb" }, { "got to go", "gtg" }, { "though", "tho" }, { "people", "ppl" },
            { "probably", "prob" }, { "really", "rly" }, { "something", "smth" }, { "don't", "dont" }, { "i'm", "im" },
            { "can't", "cant" }, { "that's", "thats" }, { "what's", "whats" }, { "it's", "its" },
        };

        static readonly string Keyboard = "qwertyuiopasdfghjklzxcvbnm";

        // ------------------------------------------------------------------ Typing like a person

        /// <summary>Casual form of a line (party members' shouts): lowercase-ish, the odd typo.</summary>
        public static string Casual(string text, float skill)
        {
            var p = ChatPersona.From("x", Random.Range(0, 100000));
            p.typo = (1f - skill) * 0.08f;
            string ignore;
            return Style(p, text, out ignore);
        }

        /// <summary>Rewrites a line in the persona's typing style. correction is a "*word" follow-up for a typo, or null.</summary>
        public static string Style(ChatPersona p, string text, out string correction)
        {
            correction = null;
            var r = p.rng;
            string t = text;
            if (r.NextDouble() < p.slang)
                for (int i = 0; i < Abbrev.GetLength(0); i++)
                    t = ReplaceWord(t, Abbrev[i, 0], Abbrev[i, 1]);
            if (r.NextDouble() < p.lowercase)
            {
                t = t.ToLowerInvariant();
                if (t.EndsWith(".")) t = t.Substring(0, t.Length - 1);
            }
            var words = new List<string>(t.Split(' '));
            for (int i = 0; i < words.Count; i++)
            {
                string w = words[i];
                if (w.Length > 3 && IsWord(w) && r.NextDouble() < p.typo)
                {
                    string typo = Typo(w, r);
                    if (typo != w)
                    {
                        if (correction == null && r.NextDouble() < 0.4) correction = "*" + w.ToLowerInvariant();
                        words[i] = typo;
                    }
                }
                else if (r.NextDouble() < p.excite * 0.25 && (w == "so" || w == "no" || w == "yes" || w == "omg" || w == "lol" || w == "nice" || w == "yay" || w == "hi"))
                    words[i] = Stretch(w, r);
            }
            t = string.Join(" ", words.ToArray());
            // Learned slang from the player.
            var d = Data;
            if (d != null && d.chatSlang.Count > 0 && r.NextDouble() < 0.12 * p.slang) t += " " + d.chatSlang[r.Next(d.chatSlang.Count)];
            if (r.NextDouble() < p.emote)
            {
                var set = FaceSets[p.faceSet];
                t += " " + set[r.Next(set.Length)];
            }
            else if (p.excite > 0.7f && r.NextDouble() < 0.3) t += "!!";
            return t;
        }

        static bool IsWord(string w)
        {
            foreach (char c in w) if (!char.IsLetter(c)) return false;
            return true;
        }

        static string Typo(string w, System.Random r)
        {
            var sb = new StringBuilder(w);
            int i = 1 + r.Next(w.Length - 2);
            switch (r.Next(4))
            {
                case 0: { char c = sb[i]; sb[i] = sb[i + 1]; sb[i + 1] = c; break; }
                case 1: sb.Remove(i, 1); break;
                case 2: sb.Insert(i, sb[i]); break;
                default:
                {
                    int k = Keyboard.IndexOf(char.ToLowerInvariant(sb[i]));
                    if (k >= 0) sb[i] = Keyboard[Mathf.Clamp(k + (r.Next(2) == 0 ? -1 : 1), 0, Keyboard.Length - 1)];
                    break;
                }
            }
            return sb.ToString();
        }

        static string Stretch(string w, System.Random r)
        {
            char last = w[w.Length - 1];
            return w + new string(last, 1 + r.Next(3));
        }

        static string ReplaceWord(string text, string from, string to)
        {
            string lower = text.ToLowerInvariant();
            int idx = 0;
            var sb = new StringBuilder();
            while (true)
            {
                int at = lower.IndexOf(from, idx, System.StringComparison.Ordinal);
                if (at < 0) break;
                bool startOk = at == 0 || !char.IsLetter(lower[at - 1]);
                int end = at + from.Length;
                bool endOk = end >= lower.Length || !char.IsLetter(lower[end]);
                sb.Append(text, idx, at - idx);
                sb.Append(startOk && endOk ? to : text.Substring(at, from.Length));
                idx = end;
            }
            sb.Append(text.Substring(idx));
            return sb.ToString();
        }

        // ------------------------------------------------------------------ Understanding

        public enum Intent { Brb, None, Greet, HowAreYou, Party, Thanks, Bye, Laugh, Compliment, Rude, Question, Feeling, Main, Bot, Agree, Name, Code }

        /// <summary>What the chat remembers about you this session (per persona).</summary>
        class Memory
        {
            public string lastTopic;
            public bool askedBack;
            public int talked;
        }

        static readonly Dictionary<string, Memory> memories = new Dictionary<string, Memory>();
        static string youLike;

        static PlayerData Data { get { return GameManager.Instance != null ? GameManager.Instance.Data : null; } }

        static string Norm(string s)
        {
            s = " " + s.ToLowerInvariant() + " ";
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == ' ' || c == '?' ? c : ' ');
            s = sb.ToString();
            string[,] expand = { { " u ", " you " }, { " ur ", " your " }, { " r ", " are " }, { " pls ", " please " }, { " plz ", " please " }, { " ty ", " thanks " }, { " thx ", " thanks " }, { " wanna ", " want to " }, { " gonna ", " going to " }, { " im ", " i am " }, { " whats ", " what is " }, { " wbu ", " what about you " }, { " hru ", " how are you " }, { " lf ", " looking for " }, { " lfg ", " looking for party " } };
            for (int i = 0; i < expand.GetLength(0); i++) s = s.Replace(expand[i, 0], expand[i, 1]);
            return s;
        }

        static bool Has(string s, params string[] keys)
        {
            foreach (var k in keys) if (s.Contains(" " + k + " ") || (k.Length > 4 && s.Contains(k))) return true;
            return false;
        }

        public static Intent Detect(string raw)
        {
            string s = Norm(raw);
            if (ProfileSystem.MaskChat(raw) != raw) return Intent.Rude;
            if (Has(s, "are you a bot", "are you real", "are you ai", "you a bot", "bot")) return Intent.Bot;
            if (Has(s, "how are you", "how is it going", "whats up", "sup", "how you doing", "hows it going")) return Intent.HowAreYou;
            if (Has(s, "hi", "hello", "hey", "yo", "hiya", "howdy", "heya", "morning", "evening")) return Intent.Greet;
            if (Has(s, "bye", "gtg", "cya", "see you", "goodnight", "gn", "later")) return Intent.Bye;
            if (Has(s, "brb", "afk", "dinner", "lunch", "going to eat", "be right back")) return Intent.Brb;
            if (Has(s, "thanks", "thank")) return Intent.Thanks;
            if (Has(s, "party", "team up", "join", "group", "looking for", "invite", "co op", "coop", "carry")) return Intent.Party;
            if (Has(s, "your name", "who are you")) return Intent.Name;
            if (Has(s, "friend code", "your code", "gamer code", "add me", "add you")) return Intent.Code;
            if (Has(s, "your main", "you main", "who do you use", "who do you play", "who you main", "best character", "favorite character", "favourite")) return Intent.Main;
            if (Has(s, "lol", "lmao", "haha", "xd", "rofl", "lmfao", "hehe", "funny")) return Intent.Laugh;
            if (Has(s, "sad", "tired", "bored", "happy", "excited", "angry", "mad", "sleepy", "great day", "bad day")) return Intent.Feeling;
            if (Has(s, "nice", "gg", "good job", "cool", "awesome", "pro", "amazing", "love", "good", "great")) return Intent.Compliment;
            if (s.Contains("?") || Has(s, "how", "what", "where", "when", "why", "who", "which", "can", "should")) return Intent.Question;
            if (Has(s, "yes", "yeah", "yep", "sure", "ok", "okay", "true", "same", "agree", "fr")) return Intent.Agree;
            return Intent.None;
        }

        /// <summary>Game topics the others can talk about.</summary>
        static readonly string[][] Topics =
        {
            new[] { "gate", "Stick together in the gates, the demons in there hit like 5x harder. Frost is the easiest after Embers imo", "gates", "The Abyss gate is insane, bring a healer" },
            new[] { "summon", "Summon at the shrine, pity kicks in eventually so dont give up", "summons", "Ten pulls at once is the best value" },
            new[] { "parry", "Tap GUARD right before the hit lands, the timing is tight but so worth it", "parry", "Parry the big red attacks, it staggers them" },
            new[] { "dodge", "Dodge through the red zone right as it fills up", "dodging", "Attack right after a dodge for a dash strike" },
            new[] { "boss", "Save your ultimate for the boss's second phase", "bosses", "Boss goes nuts under half HP, watch the ground" },
            new[] { "level", "Feed your duplicates and do the dailies for XP", "leveling", "Co-op gates give way more XP" },
            new[] { "gear", "Forge your sword first, it matters the most", "gear", "Upgrade gear at the forge in the menu" },
            new[] { "chest", "One chest is by the mill and one is behind the market I think", "chests", "Theres a chest near the gate walls" },
            new[] { "element", "Flame beats wind, water beats flame... check the element chart", "elements", "Bring a water slayer vs the fire demons" },
            new[] { "shrine", "The shrine up the hill is pretty at night", "shrine", "The shrine hall is at the very top of the village" },
            new[] { "team", "Mix a tank, a damage dealer and a healer", "teams", "Switch slayers to keep combos going" },
        };

        /// <summary>
        /// A reply to what you said, as one or two messages (people often split a thought). Returns an empty list
        /// when the persona would just not answer.
        /// </summary>
        public static List<string> Reply(ChatPersona p, string said)
        {
            var outList = new List<string>();
            var r = p.rng;
            Memory m;
            if (!memories.TryGetValue(p.name, out m)) { m = new Memory(); memories[p.name] = m; }
            m.talked++;
            string you = YourName;
            string s = Norm(said);
            Remember(s);
            string text = null, follow = null;
            var intent = Detect(said);
            // Answering our own question ("wbu?") — unless they asked something new.
            if (m.askedBack && (intent == Intent.None || intent == Intent.Agree || intent == Intent.Feeling || intent == Intent.Compliment))
                text = Pick(r, "nice", "ahh same", "oh cool", "haha fair", "love that", "respect");
            m.askedBack = false;
            // Talking about a slayer they like beats a generic reply.
            var liked = MentionedCharacter(s);
            if (liked != null && text == null && (intent == Intent.None || intent == Intent.Compliment || intent == Intent.Agree))
            {
                youLike = liked;
                text = Pick(r, liked + " is so good", "ooh " + liked + " is cool", liked + " goes hard", "same i love " + liked);
                liked = null;
            }
            string topicReply = TopicReply(s, r, out m.lastTopic);
            if (text == null)
                switch (intent)
                {
                    case Intent.Rude: text = Pick(r, "hey keep it nice pls", "woah chill", "not cool :(", "be nice lol"); break;
                    case Intent.Bot: text = Pick(r, "beep boop... jk", "lol what", "bold question", "im just here for the gates"); break;
                    case Intent.HowAreYou:
                        text = Pick(r, "good! just grinding", "pretty good, u?", "tired but vibing", "doing great, wbu", "not bad, just pulled a new slayer");
                        m.askedBack = text.Contains("u?") || text.Contains("wbu");
                        break;
                    case Intent.Greet: text = Pick(r, "hey " + you + "!", "hii", "yo", "hello!", "heyy " + you, "o/"); if (r.NextDouble() < 0.4) follow = Pick(r, "hows it going", "u doing gates?", "whats up"); break;
                    case Intent.Bye: text = Pick(r, "cya!", "bye " + you, "later!", "gn!", "see u around"); break;
                    case Intent.Brb: text = Pick(r, "kk", "take ur time", "enjoy ur food!", "ok cya soon", "np"); break;
                    case Intent.Thanks: text = Pick(r, "np!", "anytime", "ofc", "no problem"); break;
                    case Intent.Party: text = Pick(r, "im down", "sure send me an invite", "ya lets run a gate", "i can join after this", "need a healer?"); break;
                    case Intent.Name: text = Pick(r, "im " + p.name, "its " + p.name + " lol", p.name + "! nice to meet u"); break;
                    case Intent.Code: text = Pick(r, "add me from the friends list, my code's on my profile", "sure add me", "tap FRIENDS and type my code"); break;
                    case Intent.Main:
                        text = p.main != null ? Pick(r, "i main " + p.main, p.main + " all the way", "mostly " + p.main + " rn") : Pick(r, "i switch a lot tbh");
                        follow = Pick(r, "who do u main?", "wbu");
                        m.askedBack = true;
                        break;
                    case Intent.Laugh: text = Pick(r, "lmao", "haha", "xD", "LOL", "ikr"); break;
                    case Intent.Feeling:

                        text = Has(s, "sad", "tired", "bad day", "angry", "mad", "bored")
                            ? Pick(r, "aw hope ur ok", "same honestly", "take a break, the demons can wait", "sending good vibes") : Pick(r, "yay!", "love that", "nice!!", "same energy");
                        break;
                    case Intent.Compliment: text = Pick(r, "thanks!!", "aw ty", "gg!", "ur good too", "haha appreciate it"); break;
                    case Intent.Question: text = topicReply ?? Pick(r, "idk tbh", "hmm not sure", "good question", "ask in the plaza lol", "i think so?"); break;
                    case Intent.Agree: text = Pick(r, "fr", "true", "yep", "exactly", "same"); break;
                    default:
                        if (topicReply != null) text = topicReply;
                        else if (r.NextDouble() < 0.25) text = Remix(r);
                        if (text == null) text = Pick(r, "true", "haha", "fair", "ok ok", "i see", "nice", "mhm");
                        break;
                }
            // Remember what you like: a roster name you mention.
            if (liked != null) { youLike = liked; follow = Pick(r, liked + " is so good", "ooh " + liked + " is cool", liked + " goes hard"); }
            else if (follow == null && m.talked > 3 && youLike != null && r.NextDouble() < 0.15) follow = "still using " + youLike + "?";
            string corr;
            outList.Add(ProfileSystem.MaskChat(Style(p, text, out corr)));
            if (corr != null) outList.Add(corr);
            if (follow != null && r.NextDouble() < 0.8) outList.Add(ProfileSystem.MaskChat(Style(p, follow, out corr)));
            return outList;
        }

        static string TopicReply(string s, System.Random r, out string topic)
        {
            topic = null;
            foreach (var t in Topics)
                if (Has(s, t[0], t[2]))
                {
                    topic = t[0];
                    return r.NextDouble() < 0.5 ? t[1] : t[3];
                }
            return null;
        }

        static string MentionedCharacter(string s)
        {
            foreach (var c in GameDatabase.Characters)
            {
                if (c.npc || string.IsNullOrEmpty(c.displayName)) continue;
                string n = c.displayName.ToLowerInvariant();
                if (n.Length >= 3 && s.Contains(" " + n + " ")) return c.displayName;
            }
            return null;
        }

        static string YourName
        {
            get
            {
                var d = Data;
                return d != null && !string.IsNullOrEmpty(d.playerName) ? d.playerName : "slayer";
            }
        }

        static string Pick(System.Random r, params string[] options) { return options[r.Next(options.Length)]; }

        /// <summary>Something one of the others says unprompted in the village chat.</summary>
        static readonly List<string> recentChatter = new List<string>();

        public static string Chatter(ChatPersona p)
        {
            // Nobody repeats what was just said.
            string line = null;
            for (int tries = 0; tries < 6; tries++)
            {
                line = ChatterOnce(p);
                if (!recentChatter.Contains(line.ToLowerInvariant())) break;
            }
            recentChatter.Add(line.ToLowerInvariant());
            if (recentChatter.Count > 10) recentChatter.RemoveAt(0);
            return line;
        }

        static string ChatterOnce(ChatPersona p)
        {
            var r = p.rng;
            string line;
            double roll = r.NextDouble();
            if (roll < 0.04 && (line = Remix(r)) != null) { }
            else if (roll < 0.35) line = Pick(r, "anyone for the Gate of Frost?", "LF2M embers gate", "need 1 more for abyss, lvl 20+", "lf party, any gate", "who wants to run embers a few times?", "carry me through abyss pls");
            else if (roll < 0.55) line = Pick(r, "just pulled a legendary!!", "10 pull and all commons again", "pity finally hit lets gooo", "should i save diamonds or pull now", "the new banner looks sick");
            else if (roll < 0.7) line = Pick(r, "the cherry tree looks so pretty tonight", "love the lanterns in the market", "this village music slaps", "who else just vibing in the plaza", "the moon is huge tonight");
            else if (roll < 0.85) line = Pick(r, "how do you parry the frost oni?", "tip: dodge through the red zone right before it hits", "fire demons are weak to water btw", "the abyss boss one shot me", "gear matters more than level ngl");
            else line = Pick(r, "brb dinner", "gg everyone", "back", "o/ hi all", "anyone found the chest by the mill?", "afk 5 min");
            string corr;
            return ProfileSystem.MaskChat(Style(p, line, out corr));
        }

        // ------------------------------------------------------------------ Learning

        static readonly HashSet<string> Common = new HashSet<string>(new[]
        {
            "the", "a", "an", "and", "or", "but", "i", "you", "he", "she", "it", "we", "they", "me", "my", "your", "is", "are", "was", "were", "be",
            "to", "of", "in", "on", "at", "for", "with", "this", "that", "what", "how", "why", "who", "where", "when", "do", "does", "did", "not",
            "no", "yes", "can", "will", "just", "so", "go", "get", "got", "have", "has", "had", "up", "out", "all", "any", "one", "like", "want",
            "good", "nice", "hi", "hey", "hello", "lol", "ok", "okay", "gate", "gates", "party", "please", "thanks", "am", "im", "u", "ur", "r"
        });

        /// <summary>Learns from what the player typed: the word pairs they use and their own slang.</summary>
        public static void Learn(string text)
        {
            var d = Data;
            if (d == null || string.IsNullOrEmpty(text)) return;
            if (ProfileSystem.MaskChat(text) != text) return;
            var words = new List<string>();
            foreach (var w in Norm(text).Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries))
                if (w.Length <= 14) words.Add(w.Replace("?", ""));
            for (int i = 0; i < words.Count; i++)
            {
                string w = words[i];
                if (w.Length == 0) continue;
                int count = Bump(d, w);
                if (count >= 2 && !Common.Contains(w) && w.Length >= 2 && w.Length <= 8 && !d.chatSlang.Contains(w) && MentionedCharacter(" " + w + " ") == null)
                {
                    d.chatSlang.Add(w);
                    if (d.chatSlang.Count > 30) d.chatSlang.RemoveAt(0);
                }
                if (i + 1 < words.Count && words[i + 1].Length > 0)
                {
                    string pair = w + " " + words[i + 1];
                    if (!d.chatPairs.Contains(pair)) d.chatPairs.Add(pair);
                    if (d.chatPairs.Count > 400) d.chatPairs.RemoveAt(0);
                }
            }
        }

        static int Bump(PlayerData d, string w)
        {
            for (int i = 0; i < d.chatWordCounts.Count; i++)
            {
                string e = d.chatWordCounts[i];
                int colon = e.LastIndexOf(':');
                if (colon <= 0 || e.Substring(0, colon) != w) continue;
                int n;
                int.TryParse(e.Substring(colon + 1), out n);
                n++;
                d.chatWordCounts[i] = w + ":" + n;
                return n;
            }
            d.chatWordCounts.Add(w + ":1");
            if (d.chatWordCounts.Count > 300) d.chatWordCounts.RemoveAt(0);
            return 1;
        }

        static void Remember(string normalised)
        {
            if (normalised.Contains(" i like ") || normalised.Contains(" i love "))
            {
                var c = MentionedCharacter(normalised);
                if (c != null) youLike = c;
            }
        }

        /// <summary>A short sentence stitched from word pairs the player has used (their phrases, remixed).</summary>
        static string Remix(System.Random r)
        {
            var d = Data;
            if (d == null || d.chatPairs.Count < 40) return null;
            string first = d.chatPairs[r.Next(d.chatPairs.Count)];
            var words = new List<string>(first.Split(' '));
            for (int guard = 0; guard < 8 && words.Count < 9; guard++)
            {
                string last = words[words.Count - 1];
                var next = new List<string>();
                foreach (var pr in d.chatPairs)
                    if (pr.StartsWith(last + " ")) next.Add(pr.Substring(last.Length + 1));
                if (next.Count == 0) break;
                words.Add(next[r.Next(next.Count)]);
            }
            if (words.Count < 4) return null;
            return string.Join(" ", words.ToArray());
        }
    }
}
