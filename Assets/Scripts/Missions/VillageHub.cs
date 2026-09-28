using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Kiriha Village as the players' hub: other slayers walking around the plaza, a chat, how many players are
    /// online, parties of three, and the three co-op gates whose demons are several times stronger.
    ///
    /// THERE IS NO GAME SERVER YET. Everything "online" here is a local simulation behind <see cref="VillageOnline"/>
    /// (the other players are AI, their chat is canned, the online count is made up) so the whole flow can be
    /// played and tuned now; the UI labels it as a preview. A real backend (for example Photon, or Unity Netcode
    /// with a relay/lobby service) would replace VillageOnline and leave the hub and its UI as they are.
    /// </summary>
    public class VillageHub : MonoBehaviour
    {
        public static VillageHub Instance { get; private set; }

        public class SimPlayer
        {
            public string name;
            public string charId;
            public int level;
            public Color color;
            public NpcWalker walker;
            public bool inParty;
            public float answerAt = -1f;
        }

        public struct ChatLine
        {
            public string who;
            public string text;
            public Color color;
            public bool system;
        }

        public const int PartySize = 3;
        public readonly List<SimPlayer> Players = new List<SimPlayer>();
        public readonly List<SimPlayer> Party = new List<SimPlayer>();
        public readonly List<ChatLine> Chat = new List<ChatLine>();

        /// <summary>The co-op gate the active slayer is standing at (-1 = none).</summary>
        public int NearGate { get; private set; }
        public bool Searching { get; private set; }
        public int ChatVersion { get; private set; }

        readonly List<Transform> portals = new List<Transform>();
        float nextChatter, nextJoinLeave;
        string replyTo;
        float replyAt = -1f;
        System.Random rng;

        static readonly string[] Names =
        {
            "Kaede", "Haruto", "MoonlitRiver", "Sumi", "TsubakiBlade", "Riku_07", "Hotaru", "Asagi", "Kuro", "YuzuTea",
            "Botan", "Shin", "Akari", "Nagisa", "Ryo", "Hinata", "Komorebi", "Sora_Kaze", "Minato", "Tomoe"
        };

        static readonly string[] Chatter =
        {
            "anyone for the Gate of Frost?", "LF2M Gate of Embers, all welcome!", "just pulled a new slayer at the shrine :D",
            "the cherry tree looks so pretty tonight", "gg everyone, that abyss run was wild", "need one more for the Abyss gate",
            "how do you parry the frost oni?", "tip: dodge through the red zone right before it hits", "brb, dinner",
            "anyone found the chest by the mill?", "go in as three or the gate demons flatten you lol", "o/ hi all",
            "the gates hit SO hard, bring healers", "who wants to run embers a few times?", "love the lanterns in the market"
        };

        static readonly string[] Greetings = { "Hey there!", "Want to team up?", "Going to the gates?", "Nice blade!", "Hi! Party of three?", "The Abyss gate is no joke." };

        void Awake()
        {
            Instance = this;
            rng = new System.Random(System.Environment.TickCount);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            MobileControls.KeyboardBlocked = false;
        }

        float R(float a, float b) { return a + (float)rng.NextDouble() * (b - a); }

        void Start()
        {
            NearGate = -1;
            VillageOnline.Refresh();
            var pool = GameDatabase.Characters.FindAll(c => !c.npc && !c.designTest);
            int want = Mathf.RoundToInt(9 * Mathf.Clamp(GameSettings.SceneryDensity, 0.5f, 1.5f));
            var names = new List<string>(Names);
            Vector3 plaza = PrototypeWorld.VillagePlaza;
            for (int i = 0; i < want && pool.Count > 0 && names.Count > 0; i++)
            {
                int ni = rng.Next(names.Count);
                var def = pool[rng.Next(pool.Count)];
                var p = new SimPlayer { name = names[ni], charId = def.id, level = 8 + rng.Next(40), color = ElementChart.ColorOf(def.element) };
                names.RemoveAt(ni);
                p.walker = NpcWalker.Spawn(transform, def.id, plaza, 4f, 12f, Greetings);
                if (p.walker == null) continue;
                p.walker.SpeakerName = p.name;
                p.walker.Speed = R(1.4f, 2.2f);
                Players.Add(p);
            }
            BuildPortals();
            Notice("Welcome to Kiriha Village! Team up with two other slayers and step through a gate in the plaza.");
            Notice("Online play is a simulated preview — the other players are AI until the game has a server.");
            nextChatter = Time.time + 2f;
            nextJoinLeave = Time.time + 20f;
        }

        void BuildPortals()
        {
            for (int i = 0; i < PrototypeWorld.CoopGateSpots.Count; i++)
            {
                Vector3 gp = PrototypeWorld.CoopGateSpots[i], o = PrototypeWorld.CoopGateDirs[i];
                Color c = GateColor(i);
                var root = new GameObject("CoopPortal" + i).transform;
                root.SetParent(transform, false);
                root.position = gp + o * 0.3f + Vector3.up * 1.85f;
                root.rotation = Quaternion.LookRotation(o) * Quaternion.Euler(90f, 0f, 0f);
                var glow = MeshFactory.MeshObject(MeshFactory.Disc(), root, Vector3.zero, new Vector3(1.55f, 1f, 1.75f), MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.55f), true), false);
                glow.AddComponent<Pulse>().Speed = 1.6f;
                var swirl = MeshFactory.MeshObject(MeshFactory.Ring(0.55f), root, Vector3.up * 0.02f, new Vector3(1.35f, 1f, 1.5f), MaterialFactory.Additive(new Color(1f, 1f, 1f, 0.35f)), false);
                swirl.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 70f, 0f);
                var swirl2 = MeshFactory.MeshObject(MeshFactory.Ring(0.8f), root, Vector3.down * 0.02f, new Vector3(1.6f, 1f, 1.8f), MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.6f)), false);
                swirl2.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, -45f, 0f);
                var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.86f), transform, gp + Vector3.up * 0.2f, Vector3.one * 2.2f, MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.45f)), false);
                ring.AddComponent<Pulse>().Speed = 2f;
                if (i < GameSettings.MaxDynamicLights)
                {
                    var lg = new GameObject("PortalLight").AddComponent<Light>();
                    lg.transform.SetParent(root, false);
                    lg.type = LightType.Point;
                    lg.color = c;
                    lg.range = 6f;
                    lg.intensity = 1.4f;
                    lg.shadows = LightShadows.None;
                }
                portals.Add(root);
            }
        }

        public static Color GateColor(int i)
        {
            switch (i)
            {
                case 0: return new Color(1f, 0.45f, 0.2f);
                case 1: return new Color(0.45f, 0.75f, 1f);
                default: return new Color(0.75f, 0.35f, 1f);
            }
        }

        void Update()
        {
            float now = Time.time;
            VillageOnline.Tick();
            var b = BattleController.Current;
            var a = b != null && b.Team != null ? b.Team.Active : null;

            // Chatter from the other players.
            if (now > nextChatter)
            {
                nextChatter = now + R(4f, 9f);
                var sp = RandomPlayer(false);
                if (sp != null) Say(sp, Chatter[rng.Next(Chatter.Length)]);
            }
            // Players come and go.
            if (now > nextJoinLeave)
            {
                nextJoinLeave = now + R(25f, 45f);
                Notice(Names[rng.Next(Names.Length)] + (rng.Next(2) == 0 ? " arrived in the village." : " left the village."));
            }
            if (replyAt > 0f && now > replyAt)
            {
                replyAt = -1f;
                AnswerChat(replyTo);
            }

            // Invitations answer after a moment.
            foreach (var p in Players)
            {
                if (p.answerAt < 0f || now < p.answerAt) continue;
                p.answerAt = -1f;
                if (Party.Count >= PartySize - 1) continue;
                bool yes = Searching || rng.NextDouble() < 0.8;
                if (yes) Join(p, a);
                else Say(p, rng.Next(2) == 0 ? "sorry, doing dailies right now!" : "maybe later, afk for a bit");
            }
            if (Searching && Party.Count >= PartySize - 1)
            {
                Searching = false;
                Notice("Party complete! Head to a gate.");
            }
            // Party members keep following whichever slayer you're playing.
            for (int i = 0; i < Party.Count; i++)
            {
                var w = Party[i].walker;
                if (w == null || a == null) continue;
                w.FollowTarget = a.transform;
                w.FollowOffset = new Vector3(i == 0 ? -1.4f : 1.4f, 0f, -1.8f);
            }

            NearGate = -1;
            if (a != null)
                for (int i = 0; i < PrototypeWorld.CoopGateSpots.Count; i++)
                    if ((Journey.Flat(PrototypeWorld.CoopGateSpots[i]) - Journey.Flat(a.Position)).magnitude < 3.4f) NearGate = i;
        }

        SimPlayer RandomPlayer(bool mayBeInParty)
        {
            var list = mayBeInParty ? Players : Players.FindAll(p => !p.inParty);
            return list.Count > 0 ? list[rng.Next(list.Count)] : null;
        }

        void Join(SimPlayer p, PlayerCharacter a)
        {
            if (p.inParty || Party.Count >= PartySize - 1) return;
            p.inParty = true;
            Party.Add(p);
            if (p.walker != null && a != null)
            {
                p.walker.transform.position = OpenWorldBuilder.Clamp(p.walker.transform.position);
                p.walker.FollowTarget = a.transform;
            }
            Notice(p.name + " joined your party (" + (Party.Count + 1) + "/" + PartySize + ").");
            Say(p, rng.Next(2) == 0 ? "let's go!" : "ready when you are");
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("click", 0.6f);
        }

        // ------------------------------------------------------------------ Actions from the UI

        public bool CanInvite(SimPlayer p) { return p != null && !p.inParty && p.answerAt < 0f && Party.Count < PartySize - 1; }

        public void Invite(SimPlayer p)
        {
            if (!CanInvite(p)) return;
            p.answerAt = Time.time + R(1f, 2.5f);
            Notice("Invited " + p.name + "...");
        }

        /// <summary>Matchmaking: fills the free party slots with players looking for a group.</summary>
        public void FindParty()
        {
            if (Party.Count >= PartySize - 1) return;
            Searching = true;
            Notice("Looking for party members...");
            int need = PartySize - 1 - Party.Count;
            var free = Players.FindAll(p => !p.inParty && p.answerAt < 0f);
            for (int i = 0; i < need && free.Count > 0; i++)
            {
                int k = rng.Next(free.Count);
                free[k].answerAt = Time.time + R(1.5f, 3.5f) + i * 1.2f;
                free.RemoveAt(k);
            }
        }

        public void LeaveParty()
        {
            foreach (var p in Party)
            {
                p.inParty = false;
                if (p.walker != null) p.walker.StopFollowing();
            }
            Party.Clear();
            Searching = false;
            Notice("You left the party.");
        }

        public void Send(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            text = text.Trim();
            if (text.Length == 0) return;
            if (text.Length > 120) text = text.Substring(0, 120);
            Add(new ChatLine { who = YourName, text = text, color = UIStyles.Gold });
            replyTo = text.ToLowerInvariant();
            replyAt = Time.time + R(1.2f, 2.8f);
        }

        void AnswerChat(string said)
        {
            if (said == null) return;
            var p = RandomPlayer(true);
            if (p == null) return;
            if (said.Contains("party") || said.Contains("team") || said.Contains("join") || said.Contains("group") || said.Contains("lfg") || said.Contains("gate"))
            {
                var free = RandomPlayer(false);
                if (free != null && Party.Count < PartySize - 1)
                {
                    Say(free, "I'll come! invite me");
                    free.answerAt = Time.time + R(1.5f, 2.5f);
                }
                else Say(p, "your party's full already :)");
            }
            else if (said.Contains("hi") || said.Contains("hello") || said.Contains("hey") || said.Contains("yo"))
                Say(p, "hey " + YourName + "!");
            else if (said.Contains("gg") || said.Contains("thanks") || said.Contains("ty"))
                Say(p, "gg!");
            else if (rng.NextDouble() < 0.5)
            {
                string[] generic = { "lol", "nice", "same here", "good luck out there!", "haha true", "see you at the gates" };
                Say(p, generic[rng.Next(generic.Length)]);
            }
        }

        /// <summary>Steps through a gate with the party (a co-op mission with the party as AI slayers).</summary>
        public void EnterGate(int gate)
        {
            var gm = GameManager.Instance;
            if (gm == null || Party.Count < PartySize - 1) return;
            var m = GameDatabase.CoopMission(gate, TeamLevel(gm.Data));
            foreach (var p in Party)
            {
                m.coopAllyIds.Add(p.charId);
                m.coopAllyNames.Add(p.name);
            }
            MobileControls.KeyboardBlocked = false;
            gm.StartMission(m);
        }

        public static int TeamLevel(PlayerData d)
        {
            if (d == null || d.team.Count == 0) return 5;
            int sum = 0, n = 0;
            foreach (var id in d.team)
            {
                var oc = d.GetCharacter(id);
                if (oc == null) continue;
                sum += oc.level;
                n++;
            }
            return n > 0 ? Mathf.Max(3, sum / n) : 5;
        }

        public static string YourName
        {
            get
            {
                var gm = GameManager.Instance;
                return gm != null && gm.Data != null && !string.IsNullOrEmpty(gm.Data.playerName) ? gm.Data.playerName : "You";
            }
        }

        // ------------------------------------------------------------------ Chat feed

        void Say(SimPlayer p, string text)
        {
            Add(new ChatLine { who = p.name, text = text, color = Color.Lerp(p.color, Color.white, 0.35f) });
        }

        void Notice(string text)
        {
            Add(new ChatLine { who = "", text = text, color = new Color(0.6f, 0.85f, 1f), system = true });
        }

        void Add(ChatLine l)
        {
            Chat.Add(l);
            if (Chat.Count > 60) Chat.RemoveAt(0);
            ChatVersion++;
        }
    }

    /// <summary>
    /// Stand-in for an online service (SIMULATED — no network): the number of players online. Swap this for a
    /// real presence/lobby API when the game gets a backend.
    /// </summary>
    public static class VillageOnline
    {
        public const bool Simulated = true;
        public static int OnlineNow { get; private set; }
        static float nextTick;

        public static void Refresh()
        {
            // A plausible evening crowd that follows the time of day.
            float hour = System.DateTime.Now.Hour + System.DateTime.Now.Minute / 60f;
            float wave = 0.65f + 0.35f * Mathf.Sin((hour - 14f) / 24f * Mathf.PI * 2f);
            OnlineNow = Mathf.RoundToInt(900f + 1400f * wave) + Random.Range(-40, 40);
            nextTick = Time.time + 5f;
        }

        public static void Tick()
        {
            if (OnlineNow == 0) Refresh();
            if (Time.time < nextTick) return;
            nextTick = Time.time + Random.Range(4f, 8f);
            OnlineNow = Mathf.Max(200, OnlineNow + Random.Range(-18, 22));
        }
    }
}
