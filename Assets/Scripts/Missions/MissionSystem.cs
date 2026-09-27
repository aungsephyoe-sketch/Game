using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Runs a mission: waves → optional boss → result. Tracks the three objectives
    /// (defeat N demons / no slayer falls / clear within par time) and hands off to RewardSystem.
    /// </summary>
    public class MissionSystem : MonoBehaviour
    {
        public MissionDefinition Def { get; private set; }
        public int WaveIndex { get; private set; }
        public int WaveCount { get { return Def.waves.Count; } }
        public int Kills { get; private set; }
        public float Elapsed { get; private set; }
        public bool Finished { get; private set; }
        public bool Victory { get; private set; }
        public BossController Boss { get; private set; }
        public bool InBossStage { get; private set; }

        readonly List<EnemyController> alive = new List<EnemyController>();
        BattleController battle;
        bool started;

        public void Begin(MissionDefinition def, BattleController owner)
        {
            Def = def;
            battle = owner;
            GameEvents.EnemyKilled += OnEnemyKilled;
            StartCoroutine(SafeCoroutine.Run(Run(), "Mission " + def.id));
        }

        void OnDestroy()
        {
            GameEvents.EnemyKilled -= OnEnemyKilled;
            SealStone.Struck -= OnSealStruck;
        }

        // ------------------------------------------------------------------ Objectives

        public string ObjectiveLabel(int i)
        {
            switch (i)
            {
                case 0: return "Defeat " + Def.killObjective + " demons";
                case 1: return "No slayer falls";
                default: return "Clear within " + Mathf.RoundToInt(Def.parTime) + "s";
            }
        }

        public string ObjectiveProgress(int i)
        {
            switch (i)
            {
                case 0: return Mathf.Min(Kills, Def.killObjective) + "/" + Def.killObjective;
                case 1: return battle.Team.AnyMemberFell ? "FAILED" : "OK";
                default: return Mathf.Max(0, Mathf.CeilToInt(Def.parTime - Elapsed)) + "s";
            }
        }

        public bool ObjectiveMet(int i)
        {
            switch (i)
            {
                case 0: return Kills >= Def.killObjective;
                case 1: return !battle.Team.AnyMemberFell;
                default: return Elapsed <= Def.parTime;
            }
        }

        // ------------------------------------------------------------------ Flow

        IEnumerator Run()
        {
            yield return new WaitForSeconds(0.4f);
            var gm = GameManager.Instance;
            if (Def.training)
            {
                GameEvents.RaiseBanner("TRAINING GROUNDS", "Try every skill · PAUSE → Retreat to leave");
                started = true;
                yield return Training();
                yield break;
            }
            GameEvents.RaiseBanner(Def.type == MissionType.Event ? "EVENT" : "MISSION " + Def.id, Def.name);
            yield return new WaitForSeconds(1.6f);
            // First-time tutorial prompts on the very first mission.
            if (Def.id == "1-1" && gm != null && !gm.Data.IsMissionCleared("1-1"))
            {
                GameEvents.RaiseBanner("TAP ATTACK TO COMBO", "Hold to charge · attack right after a dodge for a dash strike");
                yield return new WaitForSeconds(2.4f);
                GameEvents.RaiseBanner("RED ZONES = INCOMING ATTACK", "Dodge through at the last moment, or tap GUARD just before the hit to parry");
                yield return new WaitForSeconds(2.4f);
                GameEvents.RaiseBanner("BUILD ENERGY · UNLEASH YOUR SPECIAL", "Hits fill the gauge — press SPECIAL when it glows · LOCK keeps your blade on one demon");
                yield return new WaitForSeconds(2.2f);
            }
            started = true;

            if (Def.allies > 0) SpawnAllies(Def.allies);
            var j = battle.Journey;
            if (j == null) { yield return ClassicRun(); yield break; }
            GameEvents.RaiseAreaEntered(j.places[0].name, RegionName());
            if (Def.id == "1-1" && gm != null && !gm.Data.IsMissionCleared("1-1"))
                GameEvents.RaiseBanner("FOLLOW THE ROAD", "The objective marker always shows where to go next");

            for (StageIndex = 0; StageIndex < j.stages.Count && !Finished; StageIndex++)
            {
                var st = j.stages[StageIndex];
                var pl = j.places[st.place];
                ObjectiveTitle = st.objective;
                switch (st.kind)
                {
                    case StageKind.Travel: yield return Travel(pl, false); break;
                    case StageKind.BossApproach: yield return Travel(pl, true); break;
                    case StageKind.Fight: yield return FightAt(pl, st.wave); break;
                    case StageKind.Investigate: yield return Investigate(pl); break;
                    case StageKind.Seal:
                        battle.Lock(pl.pos, pl.radius, new Color(0.4f, 0.9f, 1f));
                        yield return SealPuzzle(pl.pos);
                        battle.Unlock();
                        break;
                    case StageKind.Boss: yield return BossStage(pl); break;
                }
            }

            if (!Finished) End(true, "");
        }

        /// <summary>Fallback when the journey could not be built: waves then bosses in one arena.</summary>
        IEnumerator ClassicRun()
        {
            if (Def.sealPuzzle) yield return SealPuzzle(Vector3.zero);
            for (int w = 0; w < Def.waves.Count && !Finished; w++)
            {
                WaveIndex = w;
                StageTotal = Def.waves[w].TotalCount;
                stageKillBase = Kills;
                ObjectiveTitle = "Defeat the demons (" + (w + 1) + "/" + Def.waves.Count + ")";
                if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(MusicState.Combat);
                yield return SpawnWave(Def.waves[w]);
                while (alive.Count > 0 && !Finished) yield return null;
                yield return new WaitForSeconds(0.6f);
            }
            if (!string.IsNullOrEmpty(Def.bossId) && !Finished)
                yield return BossStage(new JourneyPlace { name = "Arena", pos = Vector3.zero, radius = BattleController.ArenaRadius, isBossArena = true });
            if (!Finished) End(true, "");
        }

        // ------------------------------------------------------------------ Journey stages (UI-facing state)

        public int StageIndex { get; private set; }
        public string ObjectiveTitle { get; private set; }
        /// <summary>Where the objective marker points (travel stages), else null.</summary>
        public Vector3? ObjectiveTarget { get; private set; }
        public int StageTotal { get; private set; }
        int stageKillBase;
        public int StageKills { get { return Mathf.Clamp(Kills - stageKillBase, 0, StageTotal); } }
        public bool InAmbush { get; private set; }

        string RegionName()
        {
            var r = GameDatabase.GetRegion(Def.regionId);
            return r != null ? r.name : "";
        }

        /// <summary>Walk the road to the next place. Ambushes wait along longer stretches; the boss road grows darker.</summary>
        IEnumerator Travel(JourneyPlace pl, bool bossApproach)
        {
            var j = battle.Journey;
            var gm = GameManager.Instance;
            if (gm != null) gm.Audio.SetMusicState(bossApproach ? MusicState.None : MusicState.Explore);
            ObjectiveTarget = pl.pos;
            StageTotal = 0;
            float startProg = battle.Team.Active != null ? j.Progress(battle.Team.Active.Position) : 0f;
            float endProg = j.Progress(pl.pos) - pl.radius;
            float segment = endProg - startProg;
            bool ambushDone = segment < 24f || !(bossApproach || StageIndex % 2 == 1 || Def.waves.Count == 1);
            if (bossApproach)
            {
                StartCoroutine(BossApproachAtmosphere(startProg, endProg));
                GameEvents.RaiseSubtitle("Ren", BossApproachLine());
            }
            float arrive = pl.radius * 0.75f;
            while (!Finished)
            {
                var a = battle.Team.Active;
                if (a != null)
                {
                    float prog = j.Progress(a.Position);
                    if (!ambushDone && prog > startProg + segment * 0.45f)
                    {
                        ambushDone = true;
                        yield return Ambush(bossApproach);
                        ObjectiveTitle = Journey.ReachText(pl.name);
                        ObjectiveTarget = pl.pos;
                    }
                    if ((Journey.Flat(a.Position) - Journey.Flat(pl.pos)).magnitude < arrive) break;
                }
                yield return null;
            }
            ObjectiveTarget = null;
            GameEvents.RaiseAreaEntered(pl.name, bossApproach ? "Something is waiting here." : RegionName());
        }

        string BossApproachLine()
        {
            switch (Def.theme.kind)
            {
                case EnvironmentKind.Forest: return "The birds stopped singing. Whatever rules this forest is close.";
                case EnvironmentKind.Mountain: return "The wind's getting worse... and that's not thunder.";
                case EnvironmentKind.Temple: return "The runes are waking up. It knows we're here.";
                case EnvironmentKind.Castle: return "I can feel him. The other half of this heart.";
                case EnvironmentKind.DemonLand: return "The air itself is burning. Stay sharp.";
                default: return "It's too quiet. Something big is ahead.";
            }
        }

        /// <summary>Ambush on the road: a few demons (elites on the boss road) burst out of cover. No barrier — keep moving.</summary>
        IEnumerator Ambush(bool bossApproach)
        {
            InAmbush = true;
            ObjectiveTitle = "Survive the ambush";
            ObjectiveTarget = null;
            GameEvents.RaiseBanner("AMBUSH!", bossApproach ? "The boss's guards block the road" : "Demons leap from cover");
            if (GameManager.Instance != null) { GameManager.Instance.Audio.SetMusicState(MusicState.Combat); GameManager.Instance.Audio.Play("roar", 0.6f); }
            string id = AmbushEnemy(bossApproach);
            int count = bossApproach ? 2 : 3;
            StageTotal = count;
            stageKillBase = Kills;
            var a = battle.Team.Active;
            var j = battle.Journey;
            float prog = a != null ? j.Progress(a.Position) : 0f;
            for (int i = 0; i < count; i++)
            {
                Vector3 p = j.PointAt(prog + (i % 2 == 0 ? 9f : -6f) + Random.Range(-2f, 2f));
                Vector3 dir = j.PointAt(prog + 1f) - j.PointAt(prog);
                p += Vector3.Cross(Vector3.up, dir.normalized) * Random.Range(-j.halfWidth + 1f, j.halfWidth - 1f);
                SpawnEnemy(id, p, true);
                yield return new WaitForSeconds(0.25f);
            }
            while (alive.Count > 0 && !Finished) yield return null;
            InAmbush = false;
            StageTotal = 0;
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(bossApproach ? MusicState.None : MusicState.Explore);
        }

        string AmbushEnemy(bool strong)
        {
            if (strong)
            {
                foreach (var w in Def.waves)
                    foreach (var sp in w.spawns)
                    {
                        var e = GameDatabase.GetEnemy(sp.enemyId);
                        if (e != null && (e.archetype == EnemyArchetype.Elite || e.archetype == EnemyArchetype.Tank)) return sp.enemyId;
                    }
            }
            int wi = Mathf.Clamp(StageIndex / 2, 0, Mathf.Max(0, Def.waves.Count - 1));
            return Def.waves.Count > 0 && Def.waves[wi].spawns.Count > 0 ? Def.waves[wi].spawns[0].enemyId : "grunt";
        }

        /// <summary>The last stretch before a boss: light fails, fog closes in, the weather thickens, the music stops.</summary>
        IEnumerator BossApproachAtmosphere(float from, float to)
        {
            var j = battle.Journey;
            Color fog0 = RenderSettings.fogColor, amb0 = RenderSettings.ambientLight;
            float start0 = RenderSettings.fogStartDistance, end0 = RenderSettings.fogEndDistance;
            float sun0 = RenderSettings.sun != null ? RenderSettings.sun.intensity : 1f;
            Color sky0 = Camera.main != null ? Camera.main.backgroundColor : Def.theme.sky;
            Color darkFog = Color.Lerp(fog0, new Color(0.08f, 0.02f, 0.06f), 0.55f);
            var weather = battle.World != null ? battle.World.weather : null;
            float rate0 = weather != null ? weather.emission.rateOverTimeMultiplier : 0f;
            while (!Finished && !InBossStage)
            {
                var a = battle.Team.Active;
                if (a != null)
                {
                    float k = Mathf.Clamp01((j.Progress(a.Position) - from) / Mathf.Max(1f, to - from));
                    RenderSettings.fogColor = Color.Lerp(fog0, darkFog, k);
                    RenderSettings.ambientLight = Color.Lerp(amb0, amb0 * 0.55f, k);
                    RenderSettings.fogStartDistance = Mathf.Lerp(start0, start0 * 0.55f, k);
                    RenderSettings.fogEndDistance = Mathf.Lerp(end0, end0 * 0.7f, k);
                    if (RenderSettings.sun != null) RenderSettings.sun.intensity = Mathf.Lerp(sun0, sun0 * 0.6f, k);
                    if (Camera.main != null) Camera.main.backgroundColor = Color.Lerp(sky0, darkFog, k);
                    if (weather != null) { var em = weather.emission; em.rateOverTimeMultiplier = rate0 * (1f + k * 2.5f); }
                }
                yield return null;
            }
        }

        IEnumerator FightAt(JourneyPlace pl, int wave)
        {
            WaveIndex = wave;
            ObjectiveTarget = null;
            battle.Lock(pl.pos, pl.radius, new Color(1f, 0.3f, 0.3f));
            var w = Def.waves[wave];
            StageTotal = w.TotalCount;
            stageKillBase = Kills;
            ObjectiveTitle = "Defeat the demons";
            GameEvents.RaiseBanner("DEMONS APPEAR", pl.name);
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(MusicState.Combat);
            yield return SpawnWave(w);
            while (alive.Count > 0 && !Finished) yield return null;
            StageTotal = 0;
            yield return new WaitForSeconds(0.5f);
            battle.Unlock();
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(MusicState.Explore);
        }

        /// <summary>Walk up to the clue, hear what Ren makes of it — then the trap springs.</summary>
        IEnumerator Investigate(JourneyPlace pl)
        {
            var markerRoot = new GameObject("Clue").transform;
            markerRoot.SetParent(battle.transform, false);
            Vector3 spot = pl.pos + Vector3.forward * 2f;
            markerRoot.position = spot;
            var glow = new Color(1f, 0.85f, 0.4f);
            var ring = MeshFactory.MeshObject(MeshFactory.Ring(0.8f), markerRoot, Vector3.up * 0.05f, Vector3.one * 1.4f, MaterialFactory.Additive(new Color(glow.r, glow.g, glow.b, 0.7f)), false);
            ring.AddComponent<Spinner>().DegreesPerSecond = new Vector3(0f, 90f, 0f);
            var beam = MeshFactory.Primitive(PrimitiveType.Cylinder, markerRoot, Vector3.up * 2f, new Vector3(0.25f, 2f, 0.25f), MaterialFactory.Additive(new Color(glow.r, glow.g, glow.b, 0.35f)));
            beam.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            ObjectiveTarget = spot;
            while (!Finished)
            {
                var a = battle.Team.Active;
                if (a != null && (Journey.Flat(a.Position) - Journey.Flat(spot)).magnitude < 2.2f) break;
                yield return null;
            }
            ObjectiveTarget = null;
            Destroy(markerRoot.gameObject);
            if (Finished) yield break;
            battle.CinematicLock = true;
            GameEvents.RaiseSubtitle("Ren", string.IsNullOrEmpty(Def.investigateLine) ? "These tracks are fresh... and they lead straight into a trap." : Def.investigateLine);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("perfect", 0.5f);
            yield return new WaitForSeconds(2.6f);
            battle.CinematicLock = false;
            battle.Lock(pl.pos, pl.radius, new Color(1f, 0.3f, 0.3f));
            ObjectiveTitle = "It's a trap — fight your way out";
            GameEvents.RaiseBanner("AMBUSH!", "It was waiting for you");
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(MusicState.Combat);
            string id = AmbushEnemy(false);
            StageTotal = 3;
            stageKillBase = Kills;
            for (int i = 0; i < 3; i++) { SpawnEnemy(id, RandomSpawnPoint(), true); yield return new WaitForSeconds(0.25f); }
            while (alive.Count > 0 && !Finished) yield return null;
            StageTotal = 0;
            battle.Unlock();
            if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(MusicState.Explore);
        }

        IEnumerator BossStage(JourneyPlace pl)
        {
            InBossStage = true;
            ObjectiveTarget = null;
            battle.Lock(pl.pos, pl.radius, new Color(0.9f, 0.1f, 0.3f));
            // Generals first, then the boss.
            var bossIds = new List<string>(Def.preBosses);
            if (!string.IsNullOrEmpty(Def.bossId)) bossIds.Add(Def.bossId);
            for (int i = 0; i < bossIds.Count && !Finished; i++)
            {
                var bd = GameDatabase.GetEnemy(bossIds[i]);
                ObjectiveTitle = "Defeat " + (bd != null ? bd.displayName : "the boss");
                yield return BossEntrance(bossIds[i], i, pl);
                while (Boss != null && Boss.IsAlive && !Finished) yield return null;
                // Clear leftover summons once a boss falls.
                foreach (var e in new List<EnemyController>(alive))
                    if (e != null && e.IsAlive) e.Health.TakeDamage(new DamageInfo { amount = 1e9f, unavoidable = true });
                if (i < bossIds.Count - 1 && !Finished)
                {
                    GameEvents.RaiseBanner("DEFEATED", Boss != null ? Boss.Def.displayName : "");
                    yield return new WaitForSeconds(2f);
                }
            }
            battle.Unlock();
        }

        /// <summary>
        /// Boss reveal: the camera pushes slowly across the quiet arena, the ground shakes, the boss rises in the
        /// distance, the camera closes on its face, the title card lands, it roars — and the fight begins.
        /// </summary>
        IEnumerator BossEntrance(string bossId, int index, JourneyPlace pl)
        {
            var bossDef = GameDatabase.GetEnemy(bossId);
            var gm = GameManager.Instance;
            var cam = CameraController.Instance;
            var player = battle.Team.Active;
            Vector3 c = pl.pos;
            Vector3 from = player != null ? player.Position : c - Vector3.forward * 10f;
            Vector3 dirIn = Journey.Flat(c - from);
            if (dirIn.sqrMagnitude < 0.01f) dirIn = Vector3.forward;
            dirIn.Normalize();
            Vector3 side = Vector3.Cross(Vector3.up, dirIn);
            Vector3 bossPos = c + dirIn * (pl.radius * 0.45f);
            battle.CinematicLock = true;
            if (gm != null) gm.Audio.SetMusicState(MusicState.None);

            if (index == 0 && cam != null)
            {
                // 1. Wide establishing shot, a slow push across the silent arena.
                cam.Cut(from - dirIn * 7f + side * 5f + Vector3.up * 9f, c + Vector3.up * 2f);
                cam.Dolly(from + dirIn * 3f + side * 3f + Vector3.up * 6f, bossPos + Vector3.up * 2f, 2.4f);
                yield return new WaitForSeconds(1.6f);
            }
            // 2. The ground shakes.
            for (int k = 0; k < 3; k++)
            {
                if (cam != null) cam.Shake(0.18f + k * 0.08f);
                VFX.Dust(bossPos + new Vector3(Random.Range(-3f, 3f), 0f, Random.Range(-3f, 3f)), 14);
                if (gm != null) gm.Audio.Play("thud", 0.7f);
                yield return new WaitForSeconds(0.4f);
            }
            // 3. The boss appears in the distance and turns to face the team.
            Boss = SpawnEnemy(bossId, bossPos, true) as BossController;
            if (Boss == null) { battle.CinematicLock = false; yield break; }
            Boss.HoldForIntro(4.5f);
            if (player != null)
            {
                Vector3 look = Journey.Flat(player.Position - bossPos);
                if (look.sqrMagnitude > 0.01f) Boss.transform.rotation = Quaternion.LookRotation(look.normalized);
            }
            VFX.Pillar(bossPos, bossDef.accentColor, 12f, 0.9f);
            VFX.Smoke(bossPos, new Color(0.1f, 0.05f, 0.1f, 0.8f), 40);
            VFX.Shockwave(bossPos, 7f, bossDef.accentColor, 0.6f);
            float h = 2f * Mathf.Max(1f, bossDef.scale);
            if (cam != null)
            {
                // 4. Push in toward its face.
                cam.Cut(bossPos - dirIn * (9f + h * 2f) + side * 2f + Vector3.up * (h * 0.6f), bossPos + Vector3.up * h * 0.7f);
                cam.Dolly(bossPos - dirIn * (2.5f + h * 0.9f) + side * 0.8f + Vector3.up * (h * 0.85f), bossPos + Vector3.up * h * 0.9f, 1.6f);
            }
            yield return new WaitForSeconds(1.5f);
            // 5. Title card, the roar, a surge of power.
            GameEvents.RaiseBossIntro(bossDef);
            string line = BossLine(bossId, true);
            if (line != null) GameEvents.RaiseSubtitle(bossDef.displayName, line);
            if (gm != null) { gm.Audio.Play("roar", 1f); gm.Audio.SetMusicState(MusicState.Boss); }
            if (cam != null) cam.Shake(0.55f);
            VFX.Breath(bossPos, bossDef.accentColor, 80);
            VFX.ImpactLight(bossPos + Vector3.up * h, bossDef.accentColor, 14f, 0.6f);
            GameEvents.RaiseImpact(0.9f);
            yield return new WaitForSeconds(2.2f);
            if (cam != null && battle.Team.Active != null) cam.Follow(battle.Team.Active.transform, false);
            battle.CinematicLock = false;
        }

        /// <summary>What each boss says as it enters (and, for some, when it enrages).</summary>
        public static string BossLine(string bossId, bool entrance)
        {
            switch (bossId)
            {
                case "boss_gorvath": return entrance ? "The old man's student? Good. He screamed your name, you know." : "You'll burn like your village!";
                case "boss_thousandarm": return entrance ? "...Leave... this forest..." : "...THE ECLIPSE... CALLS...";
                case "boss_hyoga": return entrance ? "No one crosses my pass. Not for a hundred years." : "The mountain itself will bury you!";
                case "boss_chancellor": return entrance ? "Twenty years I served that fool of a king. Tonight, I serve my true master." : "Enough games. See what I really am!";
                case "boss_goken": return entrance ? "So you're the one with his eyes. Show me, boy. Show me the dawn!" : "YES! THIS is what I was waiting for!";
                case "boss_seal_guardian": return entrance ? "SEEKER. PROVE THAT YOU CAN BEAR THE TRUTH." : "THE SEAL WEAKENS. RESOLVE UNCERTAIN.";
                case "boss_morgrath": return entrance ? "I burned this city in a single night. You are one more ember." : "Impossible... I have never bled!";
                case "boss_vex": return entrance ? "Chains for the prisoner. Chains for the guests." : "Break my chains? Then break yourself!";
                case "boss_nyx": return entrance ? "..." : "......!";
                case "boss_veyrath": return entrance ? "Five hundred years, and my other half finally comes home." : "Then let the eclipse be total!";
                default: return null;
            }
        }

        void SpawnAllies(int count)
        {
            for (int i = 0; i < count; i++)
            {
                float a = Mathf.Lerp(200f, 340f, count > 1 ? (float)i / (count - 1) : 0.5f) * Mathf.Deg2Rad;
                Vector3 origin = battle.Team.Active != null ? battle.Team.Active.Position : Vector3.zero;
                AllySoldier.Spawn(battle.transform, origin + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 3f, Def.enemyLevel);
            }
            GameEvents.RaiseBanner("THE ROYAL GUARD FIGHTS WITH YOU", count + " soldiers join the battle");
        }

        // ------------------------------------------------------------------ Temple seal puzzle

        int sealNext;
        bool sealWrong;

        IEnumerator SealPuzzle(Vector3 center)
        {
            var root = new GameObject("Seals").transform;
            root.SetParent(battle.transform, false);
            var order = new List<int> { 1, 2, 3, 4 };
            // Shuffle positions so the order has to be read from the runes.
            for (int i = 0; i < order.Count; i++) { int j = Random.Range(i, order.Count); int t = order[i]; order[i] = order[j]; order[j] = t; }
            var stones = new List<SealStone>();
            for (int i = 0; i < 4; i++)
            {
                float a = (45f + i * 90f) * Mathf.Deg2Rad;
                stones.Add(SealStone.Create(root, center + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7.5f, order[i]));
            }
            var glow = new Color(0.4f, 0.9f, 1f);
            GameEvents.RaiseBanner("THE ANCIENT SEALS", "Strike the stones in order: I → II → III → IIII");
            ObjectiveTitle = "Strike the seals in order (I → IIII)";
            GameEvents.RaiseSubtitle("Akatsuki's Echo", "Count the marks, seeker. One, then two, then three, then four.");
            sealNext = 1;
            SealStone.Struck += OnSealStruck;
            while (sealNext <= 4 && !Finished)
            {
                if (sealWrong)
                {
                    sealWrong = false;
                    sealNext = 1;
                    foreach (var s in stones) s.SetLit(false, glow);
                    GameEvents.RaiseBanner("WRONG SEAL", "The temple's shadows awaken!");
                    if (CameraController.Instance != null) CameraController.Instance.Shake(0.3f);
                    for (int i = 0; i < 2; i++) SpawnEnemy("shadow_demon", RandomSpawnPoint(), true);
                }
                yield return null;
            }
            SealStone.Struck -= OnSealStruck;
            if (Finished) yield break;
            foreach (var s in stones) s.Pulse(glow);
            GameEvents.RaiseBanner("THE SEAL IS BROKEN", "The temple's guardians stir...");
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("perfect", 1f);
            yield return new WaitForSeconds(1.2f);
            // Leftover shadows from wrong guesses must still be dealt with.
            while (alive.Count > 0 && !Finished) yield return null;
            yield return new WaitForSeconds(0.6f);
        }

        void OnSealStruck(SealStone s)
        {
            var glow = new Color(0.4f, 0.9f, 1f);
            if (s.Lit) return;
            if (s.Order == sealNext)
            {
                s.SetLit(true, glow);
                s.Pulse(glow);
                sealNext++;
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("skill", 0.5f);
            }
            else sealWrong = true;
        }

        // ------------------------------------------------------------------ Training

        IEnumerator Training()
        {
            var dummies = new List<EnemyController>();
            while (!Finished)
            {
                dummies.RemoveAll(e => e == null || !e.IsAlive);
                while (dummies.Count < 3)
                {
                    float a = Random.Range(0f, Mathf.PI * 2f);
                    var e = SpawnEnemy("dummy", new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(4f, 8f), true);
                    if (e == null) yield break;
                    dummies.Add(e);
                    yield return new WaitForSeconds(0.3f);
                }
                // Keep the ultimate topped up so it can be practised freely.
                var pc = battle.Team.Active;
                if (pc != null) pc.UltGauge = Mathf.Min(PlayerCharacter.UltMax, pc.UltGauge + Time.deltaTime * 8f);
                yield return null;
            }
        }

        IEnumerator SpawnWave(WaveDefinition wave)
        {
            foreach (var entry in wave.spawns)
            {
                for (int i = 0; i < entry.count; i++)
                {
                    SpawnEnemy(entry.enemyId, RandomSpawnPoint(), true);
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }

        Vector3 RandomSpawnPoint()
        {
            Vector3 player = battle.Team.Active != null ? battle.Team.Active.Position : Vector3.zero;
            Vector3 c = battle.Locked ? battle.LockCenter : (battle.Journey != null ? player : Vector3.zero);
            float r = battle.Locked ? battle.LockRadius : BattleController.ArenaRadius;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(r * 0.45f, r * 0.85f);
                p = BattleController.ClampToArena(p);
                if (Vector3.Distance(p, player) > 6f) return p;
            }
            return BattleController.ClampToArena(c + (c - player).normalized * r * 0.7f);
        }

        public EnemyController SpawnEnemy(string enemyId, Vector3 pos, bool tracked)
        {
            var def = GameDatabase.GetEnemy(enemyId);
            if (def == null)
            {
                Debug.LogWarning("[MissionSystem] Unknown enemy " + enemyId);
                return null;
            }
            var go = new GameObject(def.displayName);
            go.transform.SetParent(battle.transform, false);
            go.transform.position = pos;
            go.AddComponent<HealthSystem>();
            EnemyController e = def.archetype == EnemyArchetype.Boss ? go.AddComponent<BossController>() : go.AddComponent<EnemyController>();
            e.Init(def, Def.enemyLevel);
            if (battle.Team.Active != null)
            {
                Vector3 look = battle.Team.Active.Position - pos;
                look.y = 0f;
                if (look.sqrMagnitude > 0.01f) go.transform.rotation = Quaternion.LookRotation(look);
            }
            // Summoned adds also count toward clearing the stage so they can't be ignored forever.
            alive.Add(e);
            return e;
        }

        void OnEnemyKilled(EnemyController e)
        {
            if (!alive.Remove(e)) return;
            Kills++;
        }

        void Update()
        {
            if (Finished || !started) return;
            Elapsed += Time.deltaTime;
            if (battle.Team.AllDefeated) End(false, "All slayers have fallen");
            else if (Elapsed >= Def.timeLimit && !Def.training) End(false, "Time's up");
        }

        public void Retreat()
        {
            if (!Finished) End(false, "Retreated");
        }

        void End(bool victory, string reason)
        {
            if (Finished) return;
            Finished = true;
            Victory = victory;
            StartCoroutine(SafeCoroutine.Run(EndRoutine(reason), "Mission end"));
        }

        IEnumerator EndRoutine(string reason)
        {
            var gm = GameManager.Instance;
            var active = battle.Team.Active;
            if (Def.training)
            {
                if (gm != null) gm.EndBattle(new BattleResult { mission = Def, failReason = reason });
                yield break;
            }
            if (Victory && active != null && active.IsAlive)
            {
                active.PlayVictory();
                if (CameraController.Instance != null) CameraController.Instance.PlayUltimateCinematic(active.transform, 2.4f);
            }
            if (Victory)
            {
                if (gm != null) gm.Audio.SetMusicState(MusicState.Victory);
                TimeController.SlowMotion(0.25f, 1.2f);
                GameEvents.RaiseBanner("MISSION CLEAR", Def.name);
                if (gm != null) gm.Audio.Play("victory", 1f);
            }
            else
            {
                GameEvents.RaiseBanner("DEFEAT", reason);
                if (gm != null) gm.Audio.SetMusicState(MusicState.Defeat);
                if (active != null && active.IsAlive) active.PlayDefeat();
                if (gm != null) gm.Audio.Play("defeat", 1f);
            }
            if (reason != "Retreated") yield return new WaitForSecondsRealtime(2.6f);

            var result = new BattleResult
            {
                mission = Def,
                victory = Victory,
                failReason = reason,
                time = Elapsed,
                kills = Kills,
                maxCombo = battle.MaxCombo,
                totalDamage = battle.TotalDamage
            };
            for (int i = 0; i < 3; i++)
            {
                result.objectives[i] = Victory && ObjectiveMet(i);
                result.objectiveLabels[i] = ObjectiveLabel(i);
            }
            if (gm != null) gm.EndBattle(result);
        }
    }
}
