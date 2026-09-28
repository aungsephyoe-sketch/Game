using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Direct messages with friends, written to feel like texting a real friend:
    ///   • Replies take real time: anywhere from 3 minutes to 12 days (mostly minutes to hours, sometimes days), and
    ///     they're saved, so they arrive even after you quit and come back. A long wait gets a "sorry, just saw this".
    ///   • They read everything you sent since they last answered and reply to it together, in order, instead of
    ///     answering each line with a random one-liner.
    ///   • They keep the thread going: they ask things back and understand your answer to their question, explain
    ///     what they meant when you ask "wdym", and message you first now and then.
    ///   • They learn about you and remember it across sessions: who you main, what you like, what you pulled or
    ///     cleared, how you felt, what to call you — and bring it up later ("feeling better?", "hows Kuroe treating u").
    ///     The closer you get (the more you talk), the faster and warmer they reply.
    /// They're simulated players (no server yet) and say so honestly if you ask whether they're a bot.
    /// </summary>
    public static class FriendChat
    {
        const double MinDelayMinutes = 3.0, MaxDelayMinutes = 12.0 * 24.0 * 60.0;

        public static FriendMemory Memory(PlayerData d, FriendEntry f)
        {
            var m = d.friendMemory.Find(x => x.code == f.code);
            if (m == null)
            {
                m = new FriendMemory { code = f.code, answeredTo = System.DateTime.Now.Ticks };
                m.nextPing = System.DateTime.Now.AddHours(Random.Range(2f, 30f)).Ticks;
                d.friendMemory.Add(m);
            }
            return m;
        }

        /// <summary>How long until they reply: log-scaled between 3 minutes and 12 days, leaning short; closer friends reply sooner.</summary>
        static double DelayMinutes(FriendMemory m, FriendEntry f)
        {
            float u = Mathf.Pow(Random.value, 2.4f);
            double minutes = System.Math.Exp(System.Math.Log(MinDelayMinutes) + (System.Math.Log(MaxDelayMinutes) - System.Math.Log(MinDelayMinutes)) * u);
            minutes *= Mathf.Lerp(1f, 0.45f, Mathf.Clamp01(m.rapport / 60f));
            if (SocialSystem.IsOnline(f)) minutes *= 0.5;
            return System.Math.Max(MinDelayMinutes, System.Math.Min(MaxDelayMinutes, minutes));
        }

        /// <summary>You sent a message: they learn from it now and will answer later.</summary>
        public static void OnPlayerMessage(PlayerData d, FriendEntry f, string text)
        {
            var m = Memory(d, f);
            m.rapport = Mathf.Min(200, m.rapport + 1);
            LearnFacts(m, Norm(text), text);
            // Make sure this message counts as unanswered (a brand-new friend's memory starts "now").
            var t = SocialSystem.Thread(d, f.code);
            for (int i = t.lines.Count - 1; i >= 0; i--)
                if (t.lines[i].mine) { if (m.answeredTo >= t.lines[i].time) m.answeredTo = t.lines[i].time - 1; break; }
            if (m.replyDue == 0) m.replyDue = System.DateTime.Now.AddMinutes(DelayMinutes(m, f)).Ticks;
        }

        /// <summary>A friendly hint for the chat screen while you wait.</summary>
        public static string WaitingText(PlayerData d, FriendEntry f)
        {
            var m = d.friendMemory.Find(x => x.code == f.code);
            if (m == null || m.replyDue == 0) return null;
            return f.name + (SocialSystem.IsOnline(f) ? " is busy — they'll reply soon." : " is offline — they'll reply when they're back.");
        }

        /// <summary>Delivers replies that are due and first messages from friends. Returns true if anything arrived.</summary>
        public static bool Tick(PlayerData d, string openCode, out string typingCode)
        {
            typingCode = null;
            long now = System.DateTime.Now.Ticks;
            bool any = false;
            int pings = 0;
            foreach (var f in d.friends)
            {
                var m = Memory(d, f);
                if (m.replyDue > 0)
                {
                    long left = m.replyDue - now;
                    if (left > 0)
                    {
                        if (left < System.TimeSpan.TicksPerSecond * 4) typingCode = f.code;
                        continue;
                    }
                    var t = SocialSystem.Thread(d, f.code);
                    var unanswered = new List<string>();
                    foreach (var l in t.lines) if (l.mine && l.time > m.answeredTo) unanswered.Add(l.text);
                    double waitedHours = (now - m.answeredTo) / (double)System.TimeSpan.TicksPerHour;
                    var replies = Compose(d, f, m, unanswered, waitedHours);
                    long at = m.replyDue;
                    foreach (var r in replies)
                    {
                        t.lines.Add(new DmLine { mine = false, text = ProfileSystem.MaskChat(r), time = at });
                        at += System.TimeSpan.TicksPerSecond * (2 + r.Length / 6);
                    }
                    while (t.lines.Count > 80) t.lines.RemoveAt(0);
                    m.answeredTo = now;
                    m.replyDue = 0;
                    if (openCode != f.code) f.unread += replies.Count;
                    Notify(f, openCode);
                    any = true;
                }
                else if (m.nextPing > 0 && now > m.nextPing && pings < 3)
                {
                    // They message you first (at most a few at once when you come back after a while).
                    pings++;
                    m.nextPing = System.DateTime.Now.AddHours(Random.Range(8f, 96f)).Ticks;
                    var t = SocialSystem.Thread(d, f.code);
                    // Don't pile messages on someone who never answered the last one.
                    if (t.lines.Count > 0 && !t.lines[t.lines.Count - 1].mine && t.lines.Count > 2) continue;
                    foreach (var line in Opener(d, f, m))
                        t.lines.Add(new DmLine { mine = false, text = ProfileSystem.MaskChat(line), time = now });
                    while (t.lines.Count > 80) t.lines.RemoveAt(0);
                    if (openCode != f.code) f.unread++;
                    Notify(f, openCode);
                    any = true;
                }
            }
            return any;
        }

        static void Notify(FriendEntry f, string openCode)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.Audio.PlayPitched("click", 0.35f, 1.6f);
            if (openCode != f.code && gm.UI != null) gm.UI.Toast(f.name + " messaged you");
        }

        // ------------------------------------------------------------------ Understanding

        enum Kind
        {
            None, Rude, Bot, Clarify, WhatDoing, HowAreYou, Greet, Bye, Brb, Thanks, Sorry, Play, Name, CallMe, Main, Level,
            Pulled, Cleared, Stuck, Feeling, Likes, Laugh, Compliment, Yes, No, Question, Age, Where
        }

        static string Norm(string raw)
        {
            string s = " " + (raw ?? "").ToLowerInvariant() + " ";
            var sb = new StringBuilder();
            foreach (char c in s) sb.Append(char.IsLetterOrDigit(c) || c == ' ' || c == '?' || c == '\'' ? c : ' ');
            s = sb.ToString().Replace("'", "").Replace("?", " ? ");
            string[,] ex =
            {
                { " u ", " you " }, { " ya ", " you " }, { " ur ", " your " }, { " r ", " are " }, { " wyd ", " what are you doing " }, { " wdym ", " what do you mean " },
                { " wbu ", " what about you " }, { " hbu ", " how about you " }, { " hru ", " how are you " }, { " idk ", " i dont know " }, { " rn ", " right now " },
                { " im ", " i am " }, { " whats ", " what is " }, { " wanna ", " want to " }, { " gonna ", " going to " }, { " pls ", " please " }, { " plz ", " please " },
                { " ty ", " thanks " }, { " thx ", " thanks " }, { " sup ", " what is up " }, { " wassup ", " what is up " }, { " k ", " ok " }, { " kk ", " ok " },
                { " n ", " and " }, { " y ", " why " }, { " yea ", " yes " }, { " yeah ", " yes " }, { " yep ", " yes " }, { " ya ", " yes " }, { " nah ", " no " },
                { " nope ", " no " }, { " gn ", " good night " }, { " gm ", " good morning " }, { " cya ", " bye " }, { " ttyl ", " bye " }, { " gtg ", " bye " },
                { " lf ", " looking for " }, { " dont ", " do not " }, { " cant ", " can not " }, { " didnt ", " did not " }, { " tho ", " though " }
            };
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < ex.GetLength(0); i++) s = s.Replace(ex[i, 0], ex[i, 1]);
            while (s.Contains("  ")) s = s.Replace("  ", " ");
            return s;
        }

        static bool Has(string s, params string[] keys)
        {
            foreach (var k in keys) if (s.Contains(" " + k + " ")) return true;
            return false;
        }

        static Kind Detect(string raw, string s)
        {
            if (ProfileSystem.MaskChat(raw) != raw) return Kind.Rude;
            if (Has(s, "are you a bot", "are you real", "are you ai", "are you an ai", "you a bot", "is this a bot", "are you human", "bot")) return Kind.Bot;
            if (Has(s, "what do you mean", "what you mean", "huh", "what ?", "confused", "i do not get it", "what does that mean", "meaning") || s.Trim() == "?" || s.Trim() == "what") return Kind.Clarify;
            if (Has(s, "call me")) return Kind.CallMe;
            if (Has(s, "what are you doing", "what are you up to", "what you doing", "what you up to", "doing anything")) return Kind.WhatDoing;
            if (Has(s, "how are you", "how are you doing", "how is it going", "hows it going", "how you been", "what is up", "how is your day", "hows your day")) return Kind.HowAreYou;
            if (Has(s, "how old", "your age")) return Kind.Age;
            if (Has(s, "where are you from", "where do you live", "where you from")) return Kind.Where;
            if (Has(s, "play together", "want to play", "play with me", "join me", "team up", "party", "co op", "coop", "run a gate", "gates together", "carry me", "duo", "invite", "play later")) return Kind.Play;
            int wordCount = s.Trim().Split(' ').Length;
            if (Has(s, "bye", "good night", "see you", "talk later", "going to sleep", "going to bed") || (wordCount <= 2 && Has(s, "later"))) return Kind.Bye;
            if (Has(s, "brb", "afk", "be right back", "dinner", "lunch", "eating")) return Kind.Brb;
            if (Has(s, "sorry", "my bad", "apologies")) return Kind.Sorry;
            if (Has(s, "thanks", "thank you", "thank")) return Kind.Thanks;
            if (Has(s, "your name", "who are you")) return Kind.Name;
            if (Has(s, "your main", "you main", "who do you use", "who do you play", "best character", "favorite character", "favourite character", "fav character")) return Kind.Main;
            if (Has(s, "what level", "your level", "what lvl", "your lvl")) return Kind.Level;
            if (Has(s, "i pulled", "i got", "i summoned", "just pulled", "just got", "i finally got")) return Kind.Pulled;
            if (Has(s, "i beat", "i cleared", "i finished", "just beat", "just cleared", "i won", "we won")) return Kind.Cleared;
            if (Has(s, "stuck", "can not beat", "too hard", "keep dying", "i lost", "help me")) return Kind.Stuck;
            if (Sentiment(s) != 0 && (Has(s, "i am", "i feel", "feeling", "today", "day", "was", "so") || wordCount <= 4)) return Kind.Feeling;
            if (Has(s, "i like", "i love", "my favorite", "my favourite", "my fav", "i main", "i use")) return Kind.Likes;
            if (Has(s, "lol", "lmao", "haha", "hahaha", "xd", "rofl", "lmfao", "hehe")) return Kind.Laugh;
            if (Has(s, "hi", "hello", "hey", "heyy", "yo", "hiya", "good morning", "morning", "evening", "hii")) return Kind.Greet;
            if (Has(s, "nice", "gg", "good job", "cool", "awesome", "you are good", "you are cool", "amazing", "you are the best")) return Kind.Compliment;
            if (Has(s, "yes", "sure", "ok", "okay", "definitely", "of course", "true", "fr", "same")) return Kind.Yes;
            if (Has(s, "no", "not really", "never")) return Kind.No;
            if (s.Contains("?") || Has(s, "how", "what", "where", "when", "why", "who", "which", "should", "can you", "do you")) return Kind.Question;
            return Kind.None;
        }

        /// <summary>-1 negative, +1 positive, 0 neutral.</summary>
        static int Sentiment(string s)
        {
            if (Has(s, "sad", "tired", "bored", "mad", "angry", "upset", "stressed", "sick", "lonely", "rough", "awful", "terrible", "bad", "worst", "annoyed", "exhausted", "not good", "crying")) return -1;
            if (Has(s, "happy", "excited", "great", "amazing", "awesome", "fun", "good", "best", "love", "hyped", "glad")) return 1;
            return 0;
        }

        // ------------------------------------------------------------------ Memory

        static string Fact(FriendMemory m, string key)
        {
            foreach (var f in m.facts) if (f.StartsWith(key + "=")) return f.Substring(key.Length + 1);
            return null;
        }

        static void SetFact(FriendMemory m, string key, string value)
        {
            m.facts.RemoveAll(f => f.StartsWith(key + "="));
            if (!string.IsNullOrEmpty(value)) m.facts.Add(key + "=" + value);
            while (m.facts.Count > 24) m.facts.RemoveAt(0);
        }

        static string Character(string s) { return ChatBrain.MentionedCharacter(s); }

        /// <summary>Picks up facts about you from what you wrote.</summary>
        static void LearnFacts(FriendMemory m, string s, string raw)
        {
            string c = Character(s);
            if (Has(s, "i main", "i use", "my main") && c != null) SetFact(m, "main", c);
            if (Has(s, "i like", "i love", "my favorite", "my favourite", "my fav"))
            {
                if (c != null) SetFact(m, "likes", c);
                else
                {
                    string after = After(s, "i like", "i love", "my favorite is", "my favourite is", "my fav is");
                    if (after != null) SetFact(m, "likes", after);
                }
            }
            if (Has(s, "i pulled", "i got", "i summoned", "just pulled", "just got", "i finally got") && c != null) SetFact(m, "got", c);
            if (Has(s, "i beat", "i cleared", "i finished", "just beat", "just cleared"))
            {
                string after = After(s, "i beat", "i cleared", "i finished", "just beat", "just cleared");
                if (after != null) SetFact(m, "did", after);
            }
            int senti = Sentiment(s);
            if (senti < 0) SetFact(m, "mood", "down");
            else if (senti > 0 && Fact(m, "mood") == "down") SetFact(m, "mood", "up");
            if (Has(s, "call me"))
            {
                string nick = After(s, "call me");
                if (nick != null && nick.Length <= 16) SetFact(m, "nick", nick);
            }
        }

        /// <summary>The (up to three) words after a phrase.</summary>
        static string After(string s, params string[] keys)
        {
            foreach (var k in keys)
            {
                int i = s.IndexOf(" " + k + " ");
                if (i < 0) continue;
                var words = s.Substring(i + k.Length + 2).Trim().Replace("?", "").Split(' ');
                var sb = new StringBuilder();
                for (int w = 0; w < words.Length && w < 3; w++)
                {
                    if (words[w] == "and" || words[w] == "but" || words[w] == "because" || words[w].Length == 0) break;
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(words[w]);
                }
                if (sb.Length > 1) return sb.ToString();
            }
            return null;
        }

        static string You(PlayerData d, FriendMemory m)
        {
            string nick = Fact(m, "nick");
            if (nick != null) return nick;
            return !string.IsNullOrEmpty(d.playerName) ? d.playerName : "slayer";
        }

        // ------------------------------------------------------------------ Replies

        static string Pick(System.Random r, params string[] o) { return o[r.Next(o.Length)]; }

        /// <summary>Their answer to everything you sent since they last replied.</summary>
        static List<string> Compose(PlayerData d, FriendEntry f, FriendMemory m, List<string> said, double waitedHours)
        {
            var p = SocialSystem.PersonaFor(f);
            var r = p.rng;
            var outLines = new List<string>();
            string you = You(d, m);
            string activity = SocialSystem.Status(f).ToLowerInvariant();

            // A long wait gets an apology, sized to the wait.
            if (waitedHours > 72) outLines.Add(Pick(r, "omg sorry i was away for a few days", "hey sorry!! took a little break from the game", "sorry for the late reply, life got busy lol"));
            else if (waitedHours > 8) outLines.Add(Pick(r, "sorry just saw this!", "hey sorry, was out all day", "oops sorry for the slow reply"));
            else if (waitedHours > 1.5 && r.NextDouble() < 0.5) outLines.Add(Pick(r, "sorry was in a gate", "back! sorry was doing dailies"));

            bool greeted = false, comforted = false, said_bye = false;
            string question = null, asked = m.asked;
            m.asked = "";
            int answered = 0;
            // Answer the last few things you said, in order (older ones get folded in).
            int start = Mathf.Max(0, said.Count - 3);
            for (int i = start; i < said.Count && answered < 3; i++)
            {
                string raw = said[i], s = Norm(raw);
                var kind = Detect(raw, s);
                string line = null, explain = null;
                string c = Character(s);

                // Your answer to what they asked last time.
                if (!string.IsNullOrEmpty(asked) && (kind == Kind.None || kind == Kind.Yes || kind == Kind.No || kind == Kind.Likes || kind == Kind.Feeling || kind == Kind.Laugh))
                {
                    line = AnswerTo(asked, s, c, kind, m, r, out explain);
                    asked = "";
                    if (line != null && kind == Kind.Feeling) comforted = true;
                }
                // Something sad in the batch: one caring reply, not a reply per line.
                if (line == null && comforted && (kind == Kind.None || kind == Kind.Feeling)) continue;
                if (line == null)
                    switch (kind)
                    {
                        case Kind.Rude: line = Pick(r, "hey that's not cool :(", "woah, let's keep it nice ok?", "not a fan of that tbh"); explain = "i just didn't like how that sounded"; break;
                        case Kind.Bot:
                            line = Pick(r, "haha kinda! i'm one of the game's simulated players until real online is ready", "lol yeah, i'm an AI player for now. still here to chat and run gates tho");
                            explain = "the online part isn't live yet, so friends like me are AI players";
                            break;
                        case Kind.Clarify:
                            line = !string.IsNullOrEmpty(m.explain) ? Pick(r, "oh i mean ", "sorry lol, i meant ", "like, ") + m.explain : Pick(r, "nvm lol ignore me", "haha i was half asleep, ignore that");
                            break;
                        case Kind.WhatDoing:
                            line = Pick(r, "just " + DoingPhrase(activity, r), "not much, " + DoingPhrase(activity, r), DoingPhrase(activity, r) + " lol");
                            question = Pick(r, "wbu?", "what about you?", "u?");
                            m.asked = "doing";
                            explain = "i was " + DoingPhrase(activity, r);
                            break;
                        case Kind.HowAreYou:
                            line = Pick(r, "good!! kinda tired but good", "pretty good, just pulled a new slayer", "im good! been grinding gates", "honestly great today");
                            question = Pick(r, "how about you?", "hbu?", "how r u?");
                            m.asked = "howareyou";
                            break;
                        case Kind.Greet:
                            if (!greeted) { line = Pick(r, "hey " + you + "!", "heyy!", "hii " + you, "yo!"); greeted = true; }
                            if (question == null && r.NextDouble() < 0.5) { question = Pick(r, "whats up?", "how's it going?", "what are you up to?"); m.asked = "howareyou"; }
                            break;
                        case Kind.Bye: line = Pick(r, "night! talk later :)", "byee, see u in the village", "later " + you + "!"); said_bye = true; break;
                        case Kind.Brb: line = Pick(r, "np!", "ok enjoy!", "take ur time"); break;
                        case Kind.Thanks: line = Pick(r, "anytime!", "ofc!", "np :)"); break;
                        case Kind.Sorry: line = Pick(r, "no worries at all", "all good!!", "dw about it"); break;
                        case Kind.Play:
                            line = activity.Contains("gate") ? "yes!! i'm in a gate rn but after for sure" : Pick(r, "yes!! let's run a co-op gate next time we're both on", "i'm down! the frost gate?", "sure! find me in the village plaza");
                            explain = "we could team up in a co-op gate in the village";
                            break;
                        case Kind.Name: line = Pick(r, "it's " + f.name + " lol", f.name + "! you already added me silly"); break;
                        case Kind.CallMe:
                            string nick = Fact(m, "nick");
                            line = nick != null ? Pick(r, "ok " + nick + " it is!", "got it, " + nick) : "haha ok!";
                            break;
                        case Kind.Main:
                            line = p.main != null ? Pick(r, "i main " + p.main + "!", p.main + " all the way", "mostly " + p.main + " rn") : "i switch a lot tbh";
                            if (Fact(m, "main") == null) { question = Pick(r, "who do u main?", "wbu, who's ur main?"); m.asked = "main"; }
                            else question = "still maining " + Fact(m, "main") + "?";
                            break;
                        case Kind.Level: line = Pick(r, "lvl " + f.level + " rn", "i'm level " + f.level + ", slowly getting there"); break;
                        case Kind.Pulled:
                            line = c != null ? Pick(r, "no way you got " + c + "?? congrats!!", c + "!! so jealous", "omg " + c + " is so good, gz!") : Pick(r, "ooh nice pull!", "congrats!!", "lucky!!");
                            break;
                        case Kind.Cleared: line = Pick(r, "lets goooo gg!", "nice!! that one's tough", "gg!! proud of u"); break;
                        case Kind.Stuck:
                            string tip;
                            ChatBrain.TopicReply(s, r, out tip);
                            line = Pick(r, "try parrying the big red attacks, it staggers them", "bring a slayer with the right element, it helps a lot", "save your ultimate for the second phase") + (r.NextDouble() < 0.5 ? " — or we can duo it!" : "");
                            explain = "parry right before the hit lands, and match elements";
                            break;
                        case Kind.Feeling:
                            if (Sentiment(s) < 0)
                            {
                                line = Pick(r, "aw i'm sorry :( want to talk about it?", "sending hugs, take it easy today", "that sucks, i hope tomorrow's better");
                                comforted = true;
                                explain = "i'm here if you want to talk";
                            }
                            else line = Pick(r, "yay love that!", "nice!! good vibes", "that's awesome");
                            break;
                        case Kind.Likes:
                            string likes = Fact(m, "likes") ?? Fact(m, "main");
                            line = c != null ? Pick(r, c + " is so good", "ooh " + c + " is a great pick", "same, " + c + " goes hard") : likes != null ? Pick(r, "ooh " + likes + ", nice", "i can see that lol") : "nice!";
                            break;
                        case Kind.Laugh: line = r.NextDouble() < 0.5 ? Pick(r, "lol", "haha", "ikr") : null; break;
                        case Kind.Compliment: line = Pick(r, "aw thanks!!", "haha ty, you too", "stop it u"); break;
                        case Kind.Yes: line = Pick(r, "nice", "yay", "ok cool"); break;
                        case Kind.No: line = Pick(r, "aw ok", "fair", "ah no worries"); break;
                        case Kind.Age: line = Pick(r, "haha not telling, internet rules", "old enough to grind gates all night lol"); break;
                        case Kind.Where: line = Pick(r, "kiriha village obviously lol", "somewhere near the frost gate, it's cold here"); break;
                        case Kind.Question:
                            string topic;
                            string tr = ChatBrain.TopicReply(s, r, out topic);
                            if (tr != null) { line = tr; m.lastTopic = topic; explain = tr.ToLowerInvariant(); }
                            else line = c != null ? Pick(r, c + "? i like them a lot", "hmm " + c + " is solid") : Pick(r, "hmm good question, not sure", "i think so? not 100%", "honestly no idea lol");
                            break;
                        default:
                            line = Engage(s, r, m, out explain);
                            break;
                    }
                if (line != null)
                {
                    outLines.Add(line);
                    if (explain != null) m.explain = explain;
                    answered++;
                }
            }
            if (outLines.Count == 0) outLines.Add(Pick(r, "haha", "ok ok", "hehe"));

            // Keep the conversation going — sometimes with something they remember about you (never the same thing twice in a row).
            if (question == null && !said_bye && !comforted && r.NextDouble() < 0.45)
            {
                question = Remembered(m, r);
                if (question != null && question == m.lastTopic) question = null;
                if (question != null) m.lastTopic = question;
            }
            if (said_bye && m.asked != "") m.asked = "";
            if (question != null && !said_bye) outLines.Add(question);

            // Style each line in their voice (lowercase, slang, typos, faces).
            var styled = new List<string>();
            foreach (var l in outLines)
            {
                string corr;
                string st = ChatBrain.Style(p, l, out corr);
                // No laughing faces on a caring message.
                if (comforted || l.Contains("sorry") || l.Contains("sucks") || l.Contains("rough") || l.Contains("not cool")) st = Serious(st);
                styled.Add(st);
                if (corr != null) styled.Add(corr);
            }
            m.rapport = Mathf.Min(200, m.rapport + 2);
            return styled;
        }

        static string Serious(string t)
        {
            string[] faces = { " xD", " XD", " :D", " :P", " :p", " ^^", " ^_^", " lol", " lmao", " haha", " hehe", " :3", " owo", " uwu", " >w<" };
            bool again = true;
            while (again)
            {
                again = false;
                foreach (var f in faces)
                    if (t.EndsWith(f)) { t = t.Substring(0, t.Length - f.Length); again = true; }
            }
            return t;
        }

        static string DoingPhrase(string status, System.Random r)
        {
            if (status.Contains("gate")) return "running a co-op gate";
            if (status.Contains("summon")) return "saving up for the next banner";
            if (status.Contains("story")) return "doing story missions";
            if (status.Contains("forest")) return "exploring the forest";
            if (status.Contains("village")) return "hanging out in the village";
            return Pick(r, "leveling my team", "doing dailies", "chilling in the plaza");
        }

        /// <summary>Understands your reply to the question they asked.</summary>
        static string AnswerTo(string asked, string s, string c, Kind kind, FriendMemory m, System.Random r, out string explain)
        {
            explain = null;
            switch (asked)
            {
                case "main":
                    if (c != null) { SetFact(m, "main", c); return Pick(r, "ooh " + c + "! good taste", c + " is cracked", "nice, " + c + " is so fun"); }
                    return null;
                case "doing":
                    if (Has(s, "nothing", "not much", "bored", "chilling")) return Pick(r, "same lol", "wanna run a gate then?");
                    if (Has(s, "gate", "mission", "story", "summon", "event", "boss")) return Pick(r, "ooh good luck!!", "nice, gl!");
                    if (Has(s, "school", "homework", "work", "study")) return Pick(r, "ugh good luck with that", "you got this!");
                    return kind == Kind.None ? Pick(r, "oh nice", "sounds fun") : null;
                case "howareyou":
                    if (kind == Kind.No || Sentiment(s) < 0 || Has(s, "meh", "ok i guess")) { SetFact(m, "mood", "down"); return Pick(r, "aw :( what happened?", "that's rough, i'm here if u wanna talk"); }
                    if (Sentiment(s) > 0 || Has(s, "fine", "ok")) { SetFact(m, "mood", "up"); return Pick(r, "yay good to hear!", "nice!!"); }
                    return null;
                case "pulled":
                    if (kind == Kind.Yes || c != null) { if (c != null) SetFact(m, "got", c); return c != null ? "no way, " + c + "!! congrats" : "ooh who'd u get?"; }
                    return Pick(r, "rip, next time!", "the pity will get u eventually");
                case "better":
                    if (kind == Kind.Yes || Has(s, "better", "fine", "i am good", "i am ok")) { SetFact(m, "mood", "up"); return "yay i'm glad!!"; }
                    if (kind == Kind.No || Sentiment(s) < 0) return "aw, take care of urself ok? i'm here";
                    return null;
                case "playlater":
                    if (kind == Kind.Yes) return Pick(r, "yay! ping me when you're on", "cool, i'll be in the plaza");
                    if (kind == Kind.No) return "no worries, another time!";
                    return null;
                default:
                    return null;
            }
        }

        /// <summary>A response to something that isn't a question: react, then show interest.</summary>
        static string Engage(string s, System.Random r, FriendMemory m, out string explain)
        {
            explain = null;
            string c = Character(s);
            if (c != null) return Pick(r, c + " is great", "ooh " + c, "haha " + c + " is so fun");
            int words = s.Trim().Split(' ').Length;
            int senti = Sentiment(s);
            if (senti > 0 && words >= 4) { explain = "it sounded really fun"; return Pick(r, "that sounds so fun!!", "aww that's awesome", "omg i love that") + (r.NextDouble() < 0.5 ? " what did u do?" : ""); }
            if (senti < 0 && words >= 3) { explain = "that sounded rough"; return Pick(r, "oh no, that sounds rough", "aw that sucks :(", "ugh sorry"); }
            if (words >= 6)
            {
                explain = "i wanted to hear more about it";
                return Pick(r, "wait really? what happened?", "oh wow, tell me more", "no way lol", "that's actually so cool");
            }
            return Pick(r, "haha fair", "true", "ooh ok", "nice", "mhm");
        }

        /// <summary>A follow-up that uses what they remember about you.</summary>
        static string Remembered(FriendMemory m, System.Random r)
        {
            string mood = Fact(m, "mood"), got = Fact(m, "got"), main = Fact(m, "main"), did = Fact(m, "did");
            if (mood == "down" && r.NextDouble() < 0.6) { m.asked = "better"; return Pick(r, "are u feeling any better btw?", "hope ur doing better today"); }
            if (got != null && r.NextDouble() < 0.4) return Pick(r, "how's " + got + " treating u?", "have u leveled " + got + " yet?");
            if (main != null && r.NextDouble() < 0.4) return Pick(r, "still maining " + main + "?", "how's ur " + main + " build going?");
            if (did != null && r.NextDouble() < 0.3) return "did u do the next one after " + did + "?";
            if (r.NextDouble() < 0.4) { m.asked = "playlater"; return Pick(r, "wanna run a gate later?", "co-op later?"); }
            return null;
        }

        /// <summary>What they say when they message you first.</summary>
        static List<string> Opener(PlayerData d, FriendEntry f, FriendMemory m)
        {
            var p = SocialSystem.PersonaFor(f);
            var r = p.rng;
            var lines = new List<string>();
            string you = You(d, m);
            int hour = System.DateTime.Now.Hour;
            string hello = hour < 11 ? Pick(r, "morning " + you + "!", "good morning!") : hour > 21 ? Pick(r, "heyy u still up?", "hey night owl") : Pick(r, "hey " + you + "!", "heyy", "yo " + you);
            lines.Add(hello);
            string mood = Fact(m, "mood"), got = Fact(m, "got"), main = Fact(m, "main");
            string body;
            double roll = r.NextDouble();
            if (mood == "down" && roll < 0.5) { body = "just checking in, u feeling better?"; m.asked = "better"; }
            else if (got != null && roll < 0.4) { body = "how's " + got + " doing? did u level them up?"; m.asked = "pulled"; }
            else if (main != null && roll < 0.35) body = "saw a crazy " + main + " play in the plaza today and thought of u lol";
            else if (roll < 0.55) { body = Pick(r, "wanna run a co-op gate later?", "i need one more for the frost gate, u free later?"); m.asked = "playlater"; }
            else if (roll < 0.75) { body = Pick(r, "i just did a 10 pull and got nothing lmao", "finally beat the frost gate boss!!", "the new event is kinda fun ngl"); }
            else { body = Pick(r, "did u pull on the new banner?", "u try the arena yet?"); m.asked = "pulled"; }
            lines.Add(body);
            m.explain = body.ToLowerInvariant();
            var styled = new List<string>();
            foreach (var l in lines)
            {
                string corr;
                styled.Add(ChatBrain.Style(p, l, out corr));
            }
            return styled;
        }
    }
}
