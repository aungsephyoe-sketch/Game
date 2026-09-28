using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Runs a 3v3 PvP match in the arena: your team (you + two allies) at the blue base, the opponents at the red
    /// base, respawns after knockouts, the match timer, scoring for the mode and the result.
    ///   Battle — each knockout scores; first to 10 or the most at the buzzer.
    ///   Moon Crystal — a team standing alone in the crystal zone scores a point a second; first to 60.
    ///   Boss Rush — a boss both teams can hit; damage (and knockouts) score; the boss falling or the buzzer ends it.
    /// </summary>
    public class PvpMatch : MonoBehaviour
    {
        public static PvpMatch Current { get; private set; }

        public int Mode { get; private set; }
        public float BlueScore { get; private set; }
        public float RedScore { get; private set; }
        public int BlueKOs { get; private set; }
        public int RedKOs { get; private set; }
        public float TimeLeft { get; private set; }
        public float RespawnIn { get; private set; }
        /// <summary>-1 red holds the crystal, 1 blue, 0 contested/empty.</summary>
        public int CrystalOwner { get; private set; }
        public EnemyController Boss { get; private set; }
        public readonly List<string> Feed = new List<string>();
        public float FeedAt;

        public static readonly Vector3 BlueBase = new Vector3(0f, 0f, -11f), RedBase = new Vector3(0f, 0f, 11f);
        const float ZoneRadius = 3.6f;
        const float RespawnSeconds = 5f;

        BattleController battle;
        bool over, started;
        float startAt;
        Transform crystal, zoneRing;
        Material zoneMat;
        readonly List<PartySlayer> allies = new List<PartySlayer>(), enemies = new List<PartySlayer>();
        readonly Dictionary<PlayerCharacter, float> respawns = new Dictionary<PlayerCharacter, float>();

        void Awake() { Current = this; }

        void OnDestroy()
        {
            if (Current == this) Current = null;
            PartySlayer.Downed -= OnBotDown;
            GameEvents.PlayerMemberDown -= OnPlayerDown;
            CombatSystem.Damaged -= OnDamaged;
        }

        void Start()
        {
            battle = GetComponent<BattleController>();
            if (battle == null || battle.Def == null) { enabled = false; return; }
            var d = battle.Def;
            Mode = d.pvpMode;
            TimeLeft = PvpSystem.MatchSeconds;
            startAt = Time.time + 3f;
            PartySlayer.Downed += OnBotDown;
            GameEvents.PlayerMemberDown += OnPlayerDown;
            CombatSystem.Damaged += OnDamaged;
            Vector3? obj = Mode == 0 ? (Vector3?)null : Vector3.zero;
            PartySlayer.Style? first = null;
            for (int i = 0; i < d.coopAllyIds.Count; i++)
            {
                var a = PartySlayer.Spawn(transform, BlueBase + new Vector3(i == 0 ? -2.6f : 2.6f, 0f, -1f), d.enemyLevel, d.coopAllyIds[i],
                    i < d.coopAllyNames.Count ? d.coopAllyNames[i] : "Ally", i, first, CombatTeam.Player);
                if (a == null) continue;
                if (!first.HasValue) first = a.PlayStyle;
                a.RespawnDelay = RespawnSeconds;
                a.Objective = obj;
                allies.Add(a);
            }
            first = null;
            for (int i = 0; i < d.pvpEnemyIds.Count; i++)
            {
                var e = PartySlayer.Spawn(transform, RedBase + new Vector3((i - 1) * 2.6f, 0f, 1f), d.enemyLevel, d.pvpEnemyIds[i],
                    i < d.pvpEnemyNames.Count ? d.pvpEnemyNames[i] : "Rival", i, first, CombatTeam.Enemy);
                if (e == null) continue;
                if (!first.HasValue) first = e.PlayStyle;
                e.RespawnDelay = RespawnSeconds;
                // In Battle the opponents push toward your side; otherwise they go for the objective.
                e.Objective = Mode == 0 ? (Vector3?)(BlueBase * 0.4f) : Vector3.zero;
                e.transform.rotation = Quaternion.LookRotation(Vector3.back);
                enemies.Add(e);
            }
            BuildBases();
            if (Mode == 1) BuildCrystal();
            GameEvents.RaiseBanner(PvpSystem.ModeNames[Mode].ToUpperInvariant(), PvpSystem.ModeDesc[Mode]);
        }

        void BuildBases()
        {
            for (int s = 0; s < 2; s++)
            {
                Vector3 p = s == 0 ? BlueBase : RedBase;
                Color c = s == 0 ? new Color(0.25f, 0.55f, 1f) : new Color(1f, 0.28f, 0.3f);
                var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.85f), transform, p + Vector3.up * 0.06f, Vector3.one * 3.2f, MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.55f)), false);
                ring.AddComponent<Pulse>().Speed = 1.5f;
                var pad = MeshFactory.MeshObject(MeshFactory.PlanarDisc(), transform, p + Vector3.up * 0.04f, Vector3.one * 3f, MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.18f), true), false);
                pad.name = "BasePad";
            }
        }

        void BuildCrystal()
        {
            crystal = new GameObject("MoonCrystal").transform;
            crystal.SetParent(transform, false);
            crystal.position = Vector3.up * 1.8f;
            var mat = MaterialFactory.Toon(new Color(0.7f, 0.85f, 1f), 0.02f, new Color(0.35f, 0.5f, 0.9f));
            var top = MeshFactory.MeshObject(MeshFactory.FacetCone(6), crystal, Vector3.zero, new Vector3(0.9f, 1.1f, 0.9f), mat);
            var bot = MeshFactory.MeshObject(MeshFactory.FacetCone(6), crystal, Vector3.zero, new Vector3(0.9f, 0.8f, 0.9f), mat);
            bot.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
            crystal.gameObject.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 60f, 0f);
            crystal.GetComponent<Spinner>().BobHeight = 0.25f;
            zoneMat = MaterialFactory.Additive(new Color(1f, 1f, 1f, 0.35f));
            var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.9f), transform, Vector3.up * 0.06f, Vector3.one * ZoneRadius, zoneMat, false);
            zoneRing = ring.transform;
            ring.AddComponent<Pulse>().Speed = 2f;
            if (GameSettings.MaxDynamicLights > 0)
            {
                var l = new GameObject("CrystalLight").AddComponent<Light>();
                l.transform.SetParent(crystal, false);
                l.type = LightType.Point;
                l.color = new Color(0.6f, 0.8f, 1f);
                l.range = 8f;
                l.intensity = 1.5f;
            }
        }

        void SpawnBoss()
        {
            var bosses = GameDatabase.Enemies.FindAll(e => e.archetype == EnemyArchetype.Boss);
            if (bosses.Count == 0 || battle.Mission == null) return;
            var def = bosses[Random.Range(0, bosses.Count)];
            Boss = battle.Mission.SpawnEnemy(def.id, Vector3.zero, false);
            if (Boss == null) return;
            // Both teams can hit it.
            Boss.Neutral = true;
            GameEvents.RaiseBanner("BOSS INCOMING", def.displayName + " — deal the most damage!");
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.4f);
        }

        void Update()
        {
            if (battle == null || over) return;
            if (!started)
            {
                if (Time.time < startAt) return;
                started = true;
                GameEvents.RaiseBanner("FIGHT!", "");
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("switch", 0.8f);
                if (Mode == 2) SpawnBoss();
            }
            float dt = Time.deltaTime;
            TimeLeft = Mathf.Max(0f, TimeLeft - dt);

            // Respawns for your slayers.
            RespawnIn = 0f;
            var keys = new List<PlayerCharacter>(respawns.Keys);
            foreach (var pc in keys)
            {
                float left = respawns[pc] - Time.time;
                if (left > 0f) { RespawnIn = Mathf.Max(RespawnIn, left); continue; }
                respawns.Remove(pc);
                if (battle.Team != null) battle.Team.Respawn(pc, BlueBase + new Vector3(Random.Range(-1.5f, 1.5f), 0f, 0f));
            }
            if (battle.Team != null && battle.Team.Active != null && battle.Team.Active.IsAlive) RespawnIn = 0f;

            if (Mode == 1) CrystalTick(dt);
            if (Mode == 2) { BlueScore = blueDamage / 10f + BlueKOs * 50f; RedScore = redDamage / 10f + RedKOs * 50f; }
            if (Mode == 0) { BlueScore = BlueKOs; RedScore = RedKOs; }

            // Result.
            bool timeUp = TimeLeft <= 0f;
            if (Mode == 0 && (BlueKOs >= PvpSystem.BattleTarget || RedKOs >= PvpSystem.BattleTarget)) Finish();
            else if (Mode == 1 && (BlueScore >= PvpSystem.CrystalTarget || RedScore >= PvpSystem.CrystalTarget)) Finish();
            else if (Mode == 2 && Boss != null && !Boss.IsAlive) Finish();
            else if (timeUp) Finish();
        }

        float blueDamage, redDamage;

        void CrystalTick(float dt)
        {
            bool blue = false, red = false;
            var b = battle.Team != null ? battle.Team.Active : null;
            if (b != null && b.IsAlive && Flat(b.Position).magnitude < ZoneRadius) blue = true;
            foreach (var a in allies) if (a != null && !a.Down && Flat(a.Position).magnitude < ZoneRadius) blue = true;
            foreach (var e in enemies) if (e != null && !e.Down && Flat(e.Position).magnitude < ZoneRadius) red = true;
            CrystalOwner = blue && !red ? 1 : red && !blue ? -1 : 0;
            if (CrystalOwner == 1) BlueScore += dt;
            else if (CrystalOwner == -1) RedScore += dt;
            if (zoneMat != null)
            {
                Color c = CrystalOwner == 1 ? new Color(0.3f, 0.6f, 1f, 0.6f) : CrystalOwner == -1 ? new Color(1f, 0.3f, 0.3f, 0.6f) : new Color(1f, 1f, 1f, 0.35f);
                zoneMat.color = Color.Lerp(zoneMat.color, c, dt * 6f);
            }
        }

        static Vector3 Flat(Vector3 v) { return new Vector3(v.x, 0f, v.z); }

        void OnBotDown(PartySlayer p)
        {
            if (over || p == null) return;
            if (p.Team == CombatTeam.Enemy) { BlueKOs++; AddFeed("<color=#6EA8FF>Your team</color> knocked out <color=#FF6E6E>" + p.DisplayName + "</color>"); }
            else { RedKOs++; AddFeed("<color=#6EA8FF>" + p.DisplayName + "</color> was knocked out"); }
        }

        void OnPlayerDown(PlayerCharacter pc)
        {
            if (over || pc == null) return;
            RedKOs++;
            AddFeed("<color=#6EA8FF>" + (pc.Def != null ? pc.Def.displayName : "You") + "</color> was knocked out");
            respawns[pc] = Time.time + RespawnSeconds;
        }

        void OnDamaged(Combatant attacker, Combatant target, float amount)
        {
            if (Mode != 2 || over || target == null || target != Boss || attacker == null) return;
            if (attacker.Team == CombatTeam.Player) blueDamage += amount; else redDamage += amount;
        }

        void AddFeed(string line)
        {
            Feed.Add(line);
            if (Feed.Count > 4) Feed.RemoveAt(0);
            FeedAt = Time.time;
        }

        void Finish()
        {
            if (over) return;
            over = true;
            bool draw = Mathf.Approximately(Mathf.Floor(BlueScore), Mathf.Floor(RedScore));
            bool win = !draw && BlueScore > RedScore;
            string score = Mathf.FloorToInt(BlueScore) + " – " + Mathf.FloorToInt(RedScore);
            if (battle.Mission != null) battle.Mission.EndMatch(win, draw ? "Draw" : win ? "Victory " + score : "Defeat " + score);
        }
    }
}
