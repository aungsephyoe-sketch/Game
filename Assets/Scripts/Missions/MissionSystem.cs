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
            StartCoroutine(Run());
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
                GameEvents.RaiseBanner("JUMP · LOCK-ON", "JUMP then ATTACK for a plunging strike · LOCK keeps your blade on one demon");
                yield return new WaitForSeconds(2.2f);
            }
            started = true;

            if (Def.allies > 0) SpawnAllies(Def.allies);
            if (Def.sealPuzzle) yield return SealPuzzle();

            for (WaveIndex = 0; WaveIndex < Def.waves.Count; WaveIndex++)
            {
                if (Finished) yield break;
                GameEvents.RaiseBanner("WAVE " + (WaveIndex + 1) + " / " + Def.waves.Count, "");
                if (GameManager.Instance != null) GameManager.Instance.Audio.SetMusicState(MusicState.Combat);
                yield return SpawnWave(Def.waves[WaveIndex]);
                while (alive.Count > 0 && !Finished) yield return null;
                if (GameManager.Instance != null && WaveIndex < Def.waves.Count - 1) GameManager.Instance.Audio.SetMusicState(MusicState.Explore);
                yield return new WaitForSeconds(0.8f);
            }

            // Generals first, then the boss.
            var bossIds = new List<string>(Def.preBosses);
            if (!string.IsNullOrEmpty(Def.bossId)) bossIds.Add(Def.bossId);
            for (int i = 0; i < bossIds.Count && !Finished; i++)
            {
                InBossStage = true;
                yield return BossEntrance(bossIds[i], i, bossIds.Count);
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

            if (!Finished) End(true, "");
        }

        /// <summary>The boss arrives: warning, roar, a cinematic sweep onto the boss with its name card and a line of dialogue.</summary>
        IEnumerator BossEntrance(string bossId, int index, int count)
        {
            var bossDef = GameDatabase.GetEnemy(bossId);
            var gm = GameManager.Instance;
            GameEvents.RaiseBanner(count > 1 && index < count - 1 ? "GENERAL APPROACHES" : "WARNING", bossDef.displayName + " approaches");
            if (gm != null) { gm.Audio.SetMusicState(MusicState.Boss); gm.Audio.Play("roar", 1f); }
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.4f);
            yield return new WaitForSeconds(1.4f);
            Boss = SpawnEnemy(bossId, new Vector3(0f, 0f, 7f), true) as BossController;
            if (Boss == null) yield break;
            Boss.HoldForIntro(2.6f);
            VFX.Shockwave(Boss.Position, 6f, bossDef.accentColor, 0.6f);
            VFX.Pillar(Boss.Position, bossDef.accentColor, 10f, 0.8f);
            VFX.Smoke(Boss.Position, new Color(0.1f, 0.05f, 0.1f, 0.8f), 30);
            if (CameraController.Instance != null) CameraController.Instance.PlayUltimateCinematic(Boss.transform, 2.4f);
            GameEvents.RaiseBossIntro(bossDef);
            string line = BossLine(bossId, true);
            if (line != null) GameEvents.RaiseSubtitle(bossDef.displayName, line);
            yield return new WaitForSeconds(2.6f);
            if (CameraController.Instance != null) CameraController.Instance.EndCinematic();
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
                AllySoldier.Spawn(battle.transform, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7f, Def.enemyLevel);
            }
            GameEvents.RaiseBanner("THE ROYAL GUARD FIGHTS WITH YOU", count + " soldiers join the battle");
        }

        // ------------------------------------------------------------------ Temple seal puzzle

        int sealNext;
        bool sealWrong;

        IEnumerator SealPuzzle()
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
                stones.Add(SealStone.Create(root, new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * 7.5f, order[i]));
            }
            var glow = new Color(0.4f, 0.9f, 1f);
            GameEvents.RaiseBanner("THE ANCIENT SEALS", "Strike the stones in order: I → II → III → IIII");
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
            for (int attempt = 0; attempt < 10; attempt++)
            {
                float a = Random.Range(0f, Mathf.PI * 2f);
                var p = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * Random.Range(8f, 13f);
                if (Vector3.Distance(p, player) > 6f) return p;
            }
            return -player.normalized * 10f;
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
            StartCoroutine(EndRoutine(reason));
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
