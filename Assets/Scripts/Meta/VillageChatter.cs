using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The village chat's variation engine. Instead of a short list of lines, it writes messages from templates
    /// in many categories (greetings, village life, food, shops, weather, training, rumours, travel, missions,
    /// bosses, team building, events, banners, questions, reactions, jokes, complaints, celebrations, advice),
    /// fills their slots from the live game (the current event and its boss, the featured summon, regions,
    /// demons, slayers, food, shops, the time of day, and what YOU just did — like the mission you cleared), and
    /// then runs them through each player's typing style (<see cref="ChatBrain.Style"/>).
    /// Anti-spam: weighted categories that shift with context, a cooldown per category, no category twice in a
    /// row, and no template reused until many others have been.
    /// Some lines are questions another player can answer, which turns chatter into little conversations.
    /// </summary>
    public static class VillageChatter
    {
        public enum Cat { Greeting, Village, Food, Shop, Weather, Training, Rumor, Travel, Mission, Boss, Team, Event, Banner, Question, Reaction, Joke, Complaint, Celebrate, Advice, AboutYou }

        class Template
        {
            public Cat cat;
            public string text;
            public string answerTopic; // a question that others can answer
        }

        static readonly List<Template> all = new List<Template>();
        static readonly Dictionary<Cat, float> cooldownUntil = new Dictionary<Cat, float>();
        static readonly Queue<string> recent = new Queue<string>();
        static Cat lastCat = Cat.Greeting;

        // ------------------------------------------------------------------ Context the chat reacts to

        /// <summary>The last mission the player cleared (set when a battle is won) and when.</summary>
        public static string LastClearName, LastClearBoss;
        public static float LastClearAt = -999f;
        public static bool LastClearHard;

        public static void OnMissionCleared(MissionDefinition m)
        {
            if (m == null || m.openWorld || m.training) return;
            LastClearName = m.name;
            var boss = string.IsNullOrEmpty(m.bossId) ? null : GameDatabase.GetEnemy(m.bossId);
            LastClearBoss = boss != null ? boss.displayName : null;
            LastClearHard = boss != null || m.coopPower > 1f || m.difficulty >= 2;
            LastClearAt = Time.realtimeSinceStartup;
        }

        static bool RecentClear { get { return Time.realtimeSinceStartup - LastClearAt < 240f && !string.IsNullOrEmpty(LastClearName); } }

        // ------------------------------------------------------------------ Templates

        static void T(Cat c, params string[] lines) { foreach (var l in lines) all.Add(new Template { cat = c, text = l }); }
        static void Q(Cat c, string topic, params string[] lines) { foreach (var l in lines) all.Add(new Template { cat = c, text = l, answerTopic = topic }); }

        static VillageChatter()
        {
            T(Cat.Greeting, "good {time}!", "good {time} everyone", "hey all", "o/ evening crew", "hi hi", "yo what's up", "back again lol", "hello from the plaza",
                "anyone heading toward {region}?", "did you see the new mission?", "who's online rn", "morning... or evening? lost track of time", "hey {name}!", "sup {name}",
                "just logged in", "finally home from work, time to grind", "wave if you're in the plaza", "hiii", "greetings fellow slayers");
            T(Cat.Village, "the lanterns look so nice tonight", "love hanging out under the big cherry tree", "the mill wheel sound is so relaxing", "someone put fresh flowers by the well",
                "the shrine steps are pretty at dusk", "the market is packed today", "why is the notice board always full lol", "the old guardian statues at the shrine creep me out",
                "just noticed you can hear the river from the plaza", "kiriha is the comfiest place in the game", "the village feels so alive tonight", "I always park my slayer by the well",
                "the sakura petals never stop falling here", "who else afk's at the tea stop", "the gate walls look so cool at night", "someone should fix that lantern by the bridge");
            T(Cat.Food, "anyone tried the {food} at the market?", "the {food} stall is the best", "I'd kill for some {food} rn", "just ate {food} irl lol", "{food} gives no buffs but it's worth it",
                "the dumpling guy remembered my name", "i want {food} so bad", "{food} or {food2}? go", "the tea stop needs more {food}", "grandma's rice cakes > everything");
            T(Cat.Shop, "the forge upgrade costs are wild", "saving gold for the forge", "gear shop restocked btw", "which sword should I forge first?", "bought a new charm, feels good",
                "the shop bundle today is decent", "spent all my gold again oops", "haori upgrades are underrated", "anyone know when the shop resets?", "forged my first gold sword!!");
            T(Cat.Weather, "the sky looks unreal tonight", "that sunset over the hills tho", "it's so foggy by the river", "petal storm in the plaza lol", "the sky turned purple, is something happening?",
                "love this lighting", "the moon's coming up behind the shrine", "chilly evening in kiriha", "fireflies are out by the river", "perfect weather for demon hunting");
            T(Cat.Training, "going to practise combos at the training grounds", "finally nailed the parry timing", "dodge into dash attack is so satisfying", "practising my {slayer} rotations",
                "charge attacks are underrated", "training grounds dummy doesn't hit back which is nice", "working on perfect dodges", "my thumbs hurt from combo practice lol",
                "switching slayers mid-combo is so good", "lock-on changed my life");
            T(Cat.Rumor, "heard there's a hidden chest near the {spot}", "someone said a {enemy} was seen near {region}", "rumour is there's a secret boss at night", "they say the shrine hall has a secret",
                "heard the co-op gates get harder after midnight lol", "people keep talking about a {enemy} in {region}", "a merchant said the {region} road isn't safe", "apparently the old mill is haunted",
                "somebody saw lights in the forest last night", "I heard {slayer} is getting buffed");
            T(Cat.Travel, "heading to {region}, anyone want to come?", "just got back from {region}", "the road to {region} is so long", "met demons on the way to {region} again",
                "{region} is beautiful but deadly", "traveling to {region} for the story", "why do encounters always happen when I travel lol", "finally unlocked {region}!!", "back from {region}, need a nap");
            T(Cat.Mission, "that {region} mission took me forever", "stuck on the {region} missions", "the escort ones are the worst", "cleared the whole {region} chapter!", "the seal puzzle got me again",
                "how many stars did you all get on the last mission", "three stars finally", "the timed objective is so tight", "the {enemy} waves in {region} are brutal", "the story's getting good ngl");
            T(Cat.Boss, "that {boss} fight is insane", "has anyone beaten {boss} yet?", "{boss} nearly destroyed my team", "{boss} second phase is no joke", "tip for {boss}: dodge the red zones, then punish",
                "{boss} one shot me lol", "finally beat {boss}!!", "{boss} took me like 10 tries", "who's the best slayer vs {boss}?", "{boss} is my nightmare");
            T(Cat.Team, "running {slayer}, {slayer2} and {slayer3} rn", "is {slayer} good in co-op?", "my team is all {element} lol", "need a healer for my team", "{slayer} and {slayer2} combo is sick",
                "tank + dps + healer is the way", "who's your main?", "{slayer} carries me every time", "thinking of ascending {slayer}", "{slayer} ultimate looks so good");
            T(Cat.Event, "{event} is so fun", "that {event} boss is insane", "has anyone beaten the {event} boss yet?", "how far are you in {event}?", "{event} rewards are worth it",
                "doing {event} quests all night", "the {event} story is actually cute", "only two quests left in {event}", "{event} hard mode is HARD", "{event} ends soon, hurry");
            T(Cat.Banner, "I'm saving my summons for {banner}", "{banner} banner looks so good", "pulled {banner} on my first 10x!!!", "no {banner} after 80 pulls T_T", "is {banner} worth it?",
                "{banner} special is gorgeous", "should I pull for {banner} or save?", "pity is close for {banner}", "{banner} + {slayer} team would be crazy", "the {banner} animation made me scream");
            Q(Cat.Question, "parry", "how do you parry reliably?", "what's the parry timing?");
            Q(Cat.Question, "gate", "which gate is easiest?", "is the abyss gate worth it?", "anyone doing gates? what level do I need?");
            Q(Cat.Question, "summon", "is it better to single pull or 10x?", "when does pity reset?");
            Q(Cat.Question, "level", "fastest way to level up?", "where do you farm xp?");
            Q(Cat.Question, "gear", "what should I forge first?", "are charms worth upgrading?");
            Q(Cat.Question, "team", "what's a good team for beginners?", "do elements matter a lot?");
            Q(Cat.Question, "chest", "where are the hidden chests?", "found all the chests yet?");
            T(Cat.Reaction, "lol", "lmao", "no way", "same", "fr", "true", "ikr", "that's wild", "big if true", "respect", "wait what", "hahaha", "oof", "nice!", "W", "rip", "real");
            T(Cat.Joke, "my slayer runs faster than my wifi", "the demons saw my gear and laughed", "I dodge perfectly... into walls", "my combo is just mashing and hoping",
                "the training dummy has more wins than me", "I summon like I'm allergic to legendaries", "whoever named the demons deserves a raise", "I parried once and now I'm unstoppable (I'm not)");
            T(Cat.Complaint, "why is the {boss} fight so long", "my luck on the banner is terrible", "I keep dying to the red zones", "not enough gold for anything", "the {enemy}s are so annoying",
                "lag spike right at the boss ugh", "forgot to claim my daily AGAIN", "stamina of a potato today");
            T(Cat.Celebrate, "FINALLY cleared it!!", "got my first mythic!!", "3 stars on everything in {region}!", "ascended {slayer}!!", "maxed my first slayer!", "beat the abyss gate!!", "first try on {boss} lets gooo",
                "10 day login streak", "new personal best combo: {combo} hits");
            T(Cat.Advice, "tip: attack right after a dodge for a dash strike", "save your special for the boss's second phase", "switch slayers to reset combos", "water beats flame btw",
                "use lock-on for bosses", "do dailies first, they add up", "don't forget the chests in the village", "forge the sword before the haori", "co-op gates give way more xp",
                "the red zones fill up before the hit, move when it's almost full");
            T(Cat.AboutYou, "gg {you}, saw you clear {clear}!", "{you} how was {clear}?", "that {clear} took me forever too", "finally cleared {clear} huh {you}? congrats", "{clearBoss} nearly destroyed my team there",
                "did {clearBoss} give you trouble {you}?", "{you} you're getting strong");
        }

        // ------------------------------------------------------------------ Answers (little conversations)

        static readonly Dictionary<string, string[]> Answers = new Dictionary<string, string[]>
        {
            { "parry", new[] { "tap guard just before the hit, like right as the red zone fills", "it's all timing, practise on the dummy", "watch the weapon flash then tap" } },
            { "gate", new[] { "embers is the easiest", "abyss is worth it if you have a full party", "frost is fine around lvl 20" } },
            { "summon", new[] { "10x is better, guaranteed epic", "pity carries over I think", "save for the banner you want" } },
            { "level", new[] { "co-op gates for sure", "dailies + feeding duplicates", "replay boss missions" } },
            { "gear", new[] { "sword first always", "charms later, sword and haori first", "forge whatever your main uses" } },
            { "team", new[] { "one tank, one damage, one healer", "yes elements matter a lot vs bosses", "just use who you like tbh" } },
            { "chest", new[] { "one by the mill, one behind the market, one near the gate walls", "check the shrine too", "I found 5, missing one" } },
        };

        /// <summary>An answer to a question asked in chat (or null).</summary>
        public static string AnswerFor(string topic, ChatPersona p)
        {
            string[] opts;
            if (topic == null || !Answers.TryGetValue(topic, out opts)) return null;
            string corr;
            return ProfileSystem.MaskChat(ChatBrain.Style(p, opts[p.rng.Next(opts.Length)], out corr));
        }

        // ------------------------------------------------------------------ Generating

        /// <summary>A fresh message for this persona, and the question topic if it's a question others can answer.</summary>
        public static string Next(ChatPersona p, out string questionTopic)
        {
            questionTopic = null;
            var r = p.rng;
            var weights = Weights();
            // Choose a category (weighted, not on cooldown, not the same as last time).
            Cat cat = Cat.Reaction;
            for (int tries = 0; tries < 8; tries++)
            {
                cat = Roll(weights, r);
                float until;
                if (cat == lastCat && cat != Cat.Reaction) continue;
                if (cooldownUntil.TryGetValue(cat, out until) && Time.realtimeSinceStartup < until) continue;
                break;
            }
            lastCat = cat;
            cooldownUntil[cat] = Time.realtimeSinceStartup + CooldownFor(cat);
            // Pick a template from that category that hasn't been used recently.
            var pool = all.FindAll(t => t.cat == cat && !recent.Contains(t.text));
            if (pool.Count == 0) pool = all.FindAll(t => t.cat == cat);
            var tpl = pool[r.Next(pool.Count)];
            recent.Enqueue(tpl.text);
            while (recent.Count > 60) recent.Dequeue();
            string line = Fill(tpl.text, r);
            if (line == null) return Next(p, out questionTopic);
            questionTopic = tpl.answerTopic;
            string corr;
            return ProfileSystem.MaskChat(ChatBrain.Style(p, line, out corr));
        }

        static float CooldownFor(Cat c)
        {
            switch (c)
            {
                case Cat.Greeting: return 25f;
                case Cat.Event: case Cat.Banner: case Cat.Boss: return 45f;
                case Cat.AboutYou: return 90f;
                case Cat.Celebrate: case Cat.Joke: return 40f;
                case Cat.Reaction: return 6f;
                default: return 20f;
            }
        }

        static Dictionary<Cat, float> Weights()
        {
            var w = new Dictionary<Cat, float>
            {
                { Cat.Greeting, 1f }, { Cat.Village, 1.2f }, { Cat.Food, 0.7f }, { Cat.Shop, 0.6f }, { Cat.Weather, 0.6f }, { Cat.Training, 0.7f },
                { Cat.Rumor, 0.6f }, { Cat.Travel, 0.8f }, { Cat.Mission, 1f }, { Cat.Boss, 0.8f }, { Cat.Team, 0.9f }, { Cat.Event, 0f }, { Cat.Banner, 0.9f },
                { Cat.Question, 1f }, { Cat.Reaction, 0.9f }, { Cat.Joke, 0.4f }, { Cat.Complaint, 0.5f }, { Cat.Celebrate, 0.5f }, { Cat.Advice, 0.7f }, { Cat.AboutYou, 0f },
            };
            // Context: a live event gets talked about; so does whatever you just cleared.
            if (GameDatabase.Events.Count > 0) w[Cat.Event] = 1.3f;
            if (RecentClear) { w[Cat.AboutYou] = 1.4f; w[Cat.Mission] += 0.5f; w[Cat.Celebrate] += 0.3f; if (LastClearHard) w[Cat.Boss] += 0.5f; }
            int h = System.DateTime.Now.Hour;
            if (h < 11) w[Cat.Greeting] += 0.6f;
            if (h >= 18 || h < 5) { w[Cat.Weather] += 0.3f; w[Cat.Village] += 0.3f; }
            return w;
        }

        static Cat Roll(Dictionary<Cat, float> w, System.Random r)
        {
            float total = 0f;
            foreach (var kv in w) total += kv.Value;
            float x = (float)r.NextDouble() * total;
            foreach (var kv in w) { x -= kv.Value; if (x <= 0f) return kv.Key; }
            return Cat.Reaction;
        }

        // ------------------------------------------------------------------ Slots

        static readonly string[] Foods = { "dumplings", "rice cakes", "grilled fish", "sweet bean buns", "ramen", "matcha", "skewers", "melon bread", "onigiri", "taiyaki", "miso soup", "tea" };
        static readonly string[] Spots = { "mill", "shrine steps", "gate walls", "market", "bridge", "tea stop", "well" };
        static readonly string[] Elements = { "fire", "water", "thunder", "wind", "light", "dark" };

        static string Fill(string text, System.Random r)
        {
            if (text.Contains("{clear}") || text.Contains("{clearBoss}"))
            {
                if (!RecentClear) return null;
                if (text.Contains("{clearBoss}") && string.IsNullOrEmpty(LastClearBoss)) return null;
                text = text.Replace("{clear}", LastClearName).Replace("{clearBoss}", LastClearBoss ?? "the boss");
            }
            var gm = GameManager.Instance;
            string you = gm != null && gm.Data != null && !string.IsNullOrEmpty(gm.Data.playerName) ? gm.Data.playerName : "slayer";
            text = text.Replace("{you}", you);
            if (text.Contains("{event}"))
            {
                if (GameDatabase.Events.Count == 0) return null;
                text = text.Replace("{event}", GameDatabase.Events[r.Next(GameDatabase.Events.Count)].title);
            }
            if (text.Contains("{banner}"))
            {
                var ids = SummonSystem.FeaturedIds;
                var def = ids.Length > 0 ? GameDatabase.GetCharacter(ids[r.Next(ids.Length)]) : null;
                if (def == null) return null;
                text = text.Replace("{banner}", def.displayName);
            }
            text = ReplaceEach(text, "{region}", () => RandomRegion(r));
            text = ReplaceEach(text, "{boss}", () => RandomBoss(r));
            text = ReplaceEach(text, "{enemy}", () => RandomEnemy(r));
            // Different slayers in one line (never "A, B and A").
            var used = new List<string>();
            foreach (var key in new[] { "{slayer}", "{slayer2}", "{slayer3}" })
                while (text.Contains(key))
                {
                    string n = RandomSlayer(r);
                    for (int g = 0; g < 6 && used.Contains(n); g++) n = RandomSlayer(r);
                    used.Add(n);
                    int i = text.IndexOf(key, System.StringComparison.Ordinal);
                    text = text.Substring(0, i) + n + text.Substring(i + key.Length);
                }
            text = text.Replace("{food}", Foods[r.Next(Foods.Length)]).Replace("{food2}", Foods[r.Next(Foods.Length)]);
            text = text.Replace("{spot}", Spots[r.Next(Spots.Length)]).Replace("{element}", Elements[r.Next(Elements.Length)]);
            text = text.Replace("{combo}", (40 + r.Next(160)).ToString());
            int hour = System.DateTime.Now.Hour;
            text = text.Replace("{time}", hour < 12 ? "morning" : hour < 18 ? "afternoon" : "evening");
            text = text.Replace("{name}", "all");
            return text;
        }

        static string ReplaceEach(string text, string key, System.Func<string> value)
        {
            while (text.Contains(key))
            {
                int i = text.IndexOf(key, System.StringComparison.Ordinal);
                text = text.Substring(0, i) + value() + text.Substring(i + key.Length);
            }
            return text;
        }

        static string RandomRegion(System.Random r)
        {
            var list = GameDatabase.Regions;
            return list.Count > 0 ? list[r.Next(list.Count)].name : "the forest";
        }

        static string RandomBoss(System.Random r)
        {
            var list = GameDatabase.Enemies.FindAll(e => e.archetype == EnemyArchetype.Boss);
            return list.Count > 0 ? list[r.Next(list.Count)].displayName : "the boss";
        }

        static string RandomEnemy(System.Random r)
        {
            var list = GameDatabase.Enemies.FindAll(e => e.archetype != EnemyArchetype.Boss);
            return list.Count > 0 ? list[r.Next(list.Count)].displayName : "demon";
        }

        static string RandomSlayer(System.Random r)
        {
            var list = GameDatabase.Characters.FindAll(c => !c.npc && !c.designTest);
            return list.Count > 0 ? list[r.Next(list.Count)].displayName : "Ren";
        }

        /// <summary>How many distinct messages the templates can produce (for the README / sanity).</summary>
        public static int TemplateCount { get { return all.Count; } }
    }
}
