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
            GameEvents.RaiseBanner("MISSION " + Def.id, Def.name);
            yield return new WaitForSeconds(1.6f);
            started = true;

            for (WaveIndex = 0; WaveIndex < Def.waves.Count; WaveIndex++)
            {
                if (Finished) yield break;
                GameEvents.RaiseBanner("WAVE " + (WaveIndex + 1) + " / " + Def.waves.Count, "");
                yield return SpawnWave(Def.waves[WaveIndex]);
                while (alive.Count > 0 && !Finished) yield return null;
                yield return new WaitForSeconds(0.8f);
            }

            if (!string.IsNullOrEmpty(Def.bossId) && !Finished)
            {
                InBossStage = true;
                var bossDef = GameDatabase.GetEnemy(Def.bossId);
                GameEvents.RaiseBanner("WARNING", bossDef.displayName + " approaches");
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("roar", 1f);
                if (CameraController.Instance != null) CameraController.Instance.Shake(0.4f);
                yield return new WaitForSeconds(1.8f);
                Boss = SpawnEnemy(Def.bossId, new Vector3(0f, 0f, 6f), true) as BossController;
                while (Boss != null && Boss.IsAlive && !Finished) yield return null;
                // Clear leftover summons once the boss falls.
                foreach (var e in new List<EnemyController>(alive))
                    if (e != null && e.IsAlive) e.Health.TakeDamage(new DamageInfo { amount = 1e9f, unavoidable = true });
            }

            if (!Finished) End(true, "");
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
            else if (Elapsed >= Def.timeLimit) End(false, "Time's up");
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
            if (Victory)
            {
                TimeController.SlowMotion(0.25f, 1.2f);
                GameEvents.RaiseBanner("MISSION CLEAR", Def.name);
                if (gm != null) gm.Audio.Play("victory", 1f);
            }
            else
            {
                GameEvents.RaiseBanner("DEFEAT", reason);
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
