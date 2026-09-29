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
            public ChatPersona persona;
        }

        struct Queued
        {
            public SimPlayer who;
            public string text;
            public float at;
        }

        readonly List<Queued> queued = new List<Queued>();

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
        /// <summary>The gate asking "join a co-op game?" right now (-1 = none).</summary>
        public int PromptGate { get; private set; }
        /// <summary>The gate you said yes to: players are being gathered and the game starts on its own when all are in.</summary>
        public int QueuedGate { get; private set; }
        public float LaunchAt { get; private set; }
        int promptedGate = -1;
        float queuedAt, retryFindAt;
        public int ChatVersion { get; private set; }

        readonly List<Transform> portals = new List<Transform>();
        float nextChatter, nextJoinLeave;
        System.Random rng;

        static readonly string[] Names = GamerNames.All;

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
            PromptGate = -1;
            QueuedGate = -1;
            LaunchAt = -1f;
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
                p.persona = ChatPersona.From(p.name, rng.Next(), def.displayName);
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

            // Chatter from the other players, sometimes turning into a little conversation.
            if (now > nextChatter)
            {
                nextChatter = now + R(5f, 11f);
                var sp = RandomPlayer(true);
                if (sp != null)
                {
                    string topic;
                    string line = VillageChatter.Next(sp.persona, out topic);
                    Say(sp, line);
                    var other = RandomPlayer(true);
                    if (other != null && other != sp)
                    {
                        // Questions usually get an answer; other lines sometimes get a reaction.
                        if (topic != null && rng.NextDouble() < 0.8)
                        {
                            string ans = VillageChatter.AnswerFor(topic, other.persona);
                            if (ans != null) queued.Add(new Queued { who = other, text = ans, at = Time.time + R(2.5f, 5f) });
                        }
                        else if (rng.NextDouble() < 0.3) Answer(other, line, R(2f, 4.5f));
                    }
                }
            }
            for (int i = queued.Count - 1; i >= 0; i--)
            {
                if (now < queued[i].at) continue;
                var q = queued[i];
                queued.RemoveAt(i);
                Say(q.who, q.text);
            }
            // Players come and go.
            if (now > nextJoinLeave)
            {
                nextJoinLeave = now + R(25f, 45f);
                Notice(Names[rng.Next(Names.Length)] + (rng.Next(2) == 0 ? " arrived in the village." : " left the village."));
            }

            // Invitations answer after a moment.
            foreach (var p in Players)
            {
                if (p.answerAt < 0f || now < p.answerAt) continue;
                p.answerAt = -1f;
                if (Party.Count >= PartySize - 1) continue;
                bool yes = Searching || rng.NextDouble() < 0.8;
                if (yes) Join(p, a);
                else Say(p, StyleLine(p, rng.Next(2) == 0 ? "sorry, doing dailies right now!" : "maybe later, afk for a bit"));
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

            // Walking up to a gate asks once whether you want to join a game there.
            if (NearGate < 0) { promptedGate = -1; if (PromptGate >= 0) PromptGate = -1; }
            else if (NearGate != promptedGate && QueuedGate < 0) { promptedGate = NearGate; PromptGate = NearGate; }

            // Said yes: gather players, then start automatically once everyone is in.
            if (QueuedGate >= 0)
            {
                if (Party.Count >= PartySize - 1)
                {
                    if (LaunchAt < 0f)
                    {
                        LaunchAt = now + 2f;
                        Notice("All players gathered! Starting " + GateName(QueuedGate) + "...");
                        if (GameManager.Instance != null) GameManager.Instance.Audio.Play("perfect", 0.6f);
                    }
                    else if (now >= LaunchAt)
                    {
                        int g = QueuedGate;
                        QueuedGate = -1;
                        LaunchAt = -1f;
                        EnterGate(g);
                        return;
                    }
                }
                else if (now >= retryFindAt)
                {
                    // Nobody answered yet: ask more players.
                    retryFindAt = now + 6f;
                    Searching = false;
                    FindParty();
                }
            }
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
            Say(p, StyleLine(p, rng.Next(2) == 0 ? "let's go!" : "ready when you are"));
            if (Party.Count >= PartySize - 1 && GameManager.Instance != null) GameManager.Instance.Data.partiesFormed++;
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
        static string GateName(int g) { return g >= 0 && g < GameDatabase.CoopGateNames.Length ? GameDatabase.CoopGateNames[g] : "the gate"; }

        /// <summary>Yes to "join a co-op game?": find players for the gate; the game starts by itself when the party is full.</summary>
        public void AcceptJoin()
        {
            if (PromptGate < 0) return;
            QueuedGate = PromptGate;
            PromptGate = -1;
            LaunchAt = -1f;
            queuedAt = Time.time;
            retryFindAt = Time.time + 6f;
            Notice("Gathering players for " + GateName(QueuedGate) + "...");
            FindParty();
        }

        public void DeclineJoin() { PromptGate = -1; }

        public void CancelJoin()
        {
            QueuedGate = -1;
            LaunchAt = -1f;
            Searching = false;
            Notice("Stopped gathering players.");
        }

        public float GatherSeconds { get { return QueuedGate >= 0 ? Time.time - queuedAt : 0f; } }

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
            Add(new ChatLine { who = YourName, text = ProfileSystem.MaskChat(text), color = UIStyles.Gold });
            ChatBrain.Learn(text);
            if (GameManager.Instance != null) GameManager.Instance.Data.messagesSent++;
            // Whoever is named answers; otherwise one or two people (or nobody, like real chat).
            SimPlayer named = null;
            string lower = text.ToLowerInvariant();
            foreach (var p in Players) if (lower.Contains(p.name.ToLowerInvariant())) named = p;
            if (named != null) { Answer(named, text, R(1.5f, 3.5f)); return; }
            if (ChatBrain.Detect(text) == ChatBrain.Intent.Party && Party.Count < PartySize - 1)
            {
                var free = RandomPlayer(false);
                if (free != null)
                {
                    Say(free, StyleLine(free, "I'll come! invite me"));
                    free.answerAt = Time.time + R(2f, 3.5f);
                    return;
                }
            }
            int n = rng.NextDouble() < 0.15 ? 0 : rng.NextDouble() < 0.7 ? 1 : 2;
            float delay = R(1.5f, 3f);
            for (int i = 0; i < n; i++)
            {
                var p = RandomPlayer(true);
                if (p == null) break;
                Answer(p, text, delay);
                delay += R(1.5f, 3f);
            }
        }

        /// <summary>A reply (maybe two messages, maybe a typo correction) arriving after typing time.</summary>
        void Answer(SimPlayer p, string said, float delay)
        {
            foreach (var line in ChatBrain.Reply(p.persona, said))
            {
                queued.Add(new Queued { who = p, text = line, at = Time.time + delay });
                delay += 0.7f + line.Length * 0.05f;
            }
        }

        string StyleLine(SimPlayer p, string text)
        {
            string corr;
            return ProfileSystem.MaskChat(ChatBrain.Style(p.persona, text, out corr));
        }

        /// <summary>Befriend another player in the village.</summary>
        public void AddFriend(SimPlayer p)
        {
            var gm = GameManager.Instance;
            if (gm == null || p == null) return;
            var e = SocialSystem.ForName(p.name, p.charId, p.level);
            string reason;
            if (SocialSystem.AddFriend(gm.Data, e, out reason))
            {
                Notice("You and " + p.name + " are now friends!");
                gm.Save();
            }
            else Notice(reason);
        }

        public bool IsFriend(SimPlayer p)
        {
            var gm = GameManager.Instance;
            return gm != null && SocialSystem.IsFriend(gm.Data, SocialSystem.ForName(p.name, p.charId, p.level).code);
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
