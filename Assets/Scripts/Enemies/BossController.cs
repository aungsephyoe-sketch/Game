using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Multi-phase bosses built from learnable, telegraphed patterns rather than raw HP.
    ///
    /// Goken, the Crimson Fist (3 phases):
    ///   P1 100–70%  Flurry, Shockwave (dodge/run), Needle Rush (line dashes)
    ///   P2  70–30%  + Double Shockwave (inner circle then outer ring – step in after the first)
    ///       at 50%  DESTRUCTIVE MODE: faster, shorter windups, + Scatter Blossoms (radial projectiles)
    ///   P3  30– 0%  + Annihilation: arena-wide blast – perfect-dodge it or reach the edge – then BREAK (+50% dmg taken)
    ///
    /// The Thousand-Arm Demon (2 phases): Grasp, Ground Hands, Summon; enraged at 50% adds Sweep.
    /// </summary>
    public class BossController : EnemyController
    {
        public int Phase { get; private set; }
        public int PhaseCount { get { return Def.phaseThresholds != null ? Def.phaseThresholds.Length + 1 : 1; } }
        public bool Exhausted { get; private set; }
        public bool DestructiveMode { get; private set; }

        int pendingPhase;
        bool superArmor;
        float summonTimer = 8f;
        int lastPattern = -1;
        ParticleSystem aura;

        public override void Init(EnemyDefinition def, int level)
        {
            base.Init(def, level);
            attackTimer = 1.5f;
            BuildAura(def.accentColor);
        }

        void BuildAura(Color c)
        {
            var go = new GameObject("Aura");
            go.transform.SetParent(transform, false);
            aura = go.AddComponent<ParticleSystem>();
            aura.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = aura.main;
            main.startLifetime = 0.9f;
            main.startSpeed = 1.2f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            main.startColor = new Color(c.r, c.g, c.b, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.gravityModifier = -0.3f;
            var em = aura.emission;
            em.rateOverTime = 25f;
            var shape = aura.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = Def.radius;
            shape.rotation = new Vector3(90f, 0f, 0f);
            var r = go.GetComponent<ParticleSystemRenderer>();
            r.material = MaterialFactory.Additive(Color.white, true);
            aura.Play();
        }

        void SetAuraColor(Color c)
        {
            if (aura == null) return;
            var main = aura.main;
            main.startColor = new Color(c.r, c.g, c.b, 0.7f);
            var em = aura.emission;
            em.rateOverTime = 60f;
        }

        int ComputePhase()
        {
            if (Def.phaseThresholds == null) return 0;
            float hp = Health.Normalized;
            int p = 0;
            foreach (var t in Def.phaseThresholds) if (hp <= t) p++;
            return p;
        }

        public override void OnParried(PlayerCharacter by)
        {
            // Parries still reward the player, but can't cancel phase changes or the break window.
            if (superArmor || Exhausted)
            {
                visual.Flash(Color.white, 1f);
                return;
            }
            base.OnParried(by);
        }

        protected override bool CanBeStaggered() { return !superArmor && !Exhausted; }

        protected override void Think(float dt)
        {
            int target = ComputePhase();
            if (target > Phase)
            {
                pendingPhase = target;
                BeginAttack(PhaseTransition());
                return;
            }
            if (!DestructiveMode && Def.bossStyle == "goken" && Health.Normalized <= 0.5f)
            {
                BeginAttack(EnterDestructiveMode());
                return;
            }

            var player = Player;
            if (player == null) { visual.SetMoving(0f); return; }
            Vector3 to = player.Position - Position;
            to.y = 0f;
            float dist = to.magnitude;
            float keep = Def.attackRange + player.Radius;
            if (dist > keep)
            {
                transform.position += to.normalized * Stats.speed * speedMultiplier * dt;
                visual.SetMoving(1f);
            }
            else visual.SetMoving(0f);
            FaceTowards(to, dt * 6f);

            attackTimer -= dt;
            summonTimer -= dt;
            if (attackTimer <= 0f) BeginAttack(ChoosePattern(player, dist));
        }

        IEnumerator ChoosePattern(PlayerCharacter player, float dist)
        {
            var options = new List<int>();
            if (Def.bossStyle == "goken")
            {
                // 0 Flurry, 1 Shockwave, 2 Needle Rush, 3 Double Shockwave, 4 Scatter Blossoms, 5 Annihilation
                if (dist < 4f) options.Add(0);
                options.Add(1);
                options.Add(2);
                if (Phase >= 1) options.Add(3);
                if (DestructiveMode) options.Add(4);
                if (Phase >= 2 && lastPattern != 5 && Random.value < 0.45f) { options.Clear(); options.Add(5); }
            }
            else
            {
                // 10 Grasp, 11 Ground Hands, 12 Summon, 13 Sweep
                if (dist < 5f) options.Add(10);
                options.Add(11);
                if (summonTimer <= 0f) { options.Clear(); options.Add(12); }
                if (Phase >= 1 && dist < 5f) options.Add(13);
            }
            if (options.Count > 1) options.Remove(lastPattern);
            int pick = options[Random.Range(0, options.Count)];
            lastPattern = pick;

            switch (pick)
            {
                case 0: return ClawCombo(player, 3, 1.1f);
                case 1: return Shockwave(5.5f, 1.0f, 1.8f);
                case 2: return NeedleRush(player, DestructiveMode ? 3 : 2);
                case 3: return DoubleShockwave();
                case 4: return ScatterBlossoms();
                case 5: return Annihilation();
                case 10: return Grasp(player);
                case 11: return GroundHands(player, Phase >= 1 ? 5 : 3);
                case 12: return Summon();
                default: return Shockwave(4.5f, 1.0f, 1.5f);
            }
        }

        // ------------------------------------------------------------------ Phase changes

        IEnumerator PhaseTransition()
        {
            Phase = pendingPhase;
            superArmor = true;
            Health.GrantInvulnerability(1.4f);
            string title = Def.bossStyle == "goken" ? (Phase == 1 ? "PHASE 2" : "PHASE 3 — FINAL") : "ENRAGED";
            GameEvents.RaiseBanner(title, Def.displayName + " grows stronger");
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("roar", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.5f);
            VFX.Shockwave(Position, 7f, Def.accentColor, 0.6f);
            // Roar shockwave pushes the player away without damage.
            var p = Player;
            if (p != null && Vector3.Distance(p.Position, Position) < 5f)
                p.OnHitReceived(new DamageInfo { knockback = (p.Position - Position).normalized * 6f, staggerPower = 3f });
            if (Def.bossStyle != "goken")
            {
                speedMultiplier = 1.25f;
                windupMultiplier = 0.85f;
                SetAuraColor(new Color(1f, 0.3f, 0.2f));
            }
            yield return new WaitForSeconds(1.3f);
            superArmor = false;
        }

        IEnumerator EnterDestructiveMode()
        {
            DestructiveMode = true;
            superArmor = true;
            Health.GrantInvulnerability(1.4f);
            GameEvents.RaiseBanner("DESTRUCTIVE MODE", "His attacks are faster — watch the ground!");
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("roar", 1f);
            SetAuraColor(new Color(0.3f, 0.6f, 1f));
            VFX.Shockwave(Position, 8f, new Color(0.3f, 0.6f, 1f), 0.7f);
            speedMultiplier = 1.3f;
            windupMultiplier = 0.8f;
            yield return new WaitForSeconds(1.3f);
            superArmor = false;
        }

        // ------------------------------------------------------------------ Shared patterns

        IEnumerator Shockwave(float radius, float windup, float mult)
        {
            Track(Telegraph.Circle(Position, radius, Windup(windup)));
            visual.Flash(Def.accentColor, 0.6f);
            yield return new WaitForSeconds(Windup(windup));
            ClearTelegraphs();
            Blast(Position, radius, mult);
            yield return new WaitForSeconds(0.6f);
        }

        void Blast(Vector3 center, float radius, float mult)
        {
            visual.Swing(0f, 0f, 0.1f, 90f);
            VFX.Shockwave(center, radius, Def.accentColor, 0.4f);
            VFX.BurstDisc(center, radius, Def.accentColor * 0.6f, 0.3f);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.3f);
            CombatSystem.HitRadius(this, center, radius, EnemyTag(mult, 7f, 4f));
        }

        // ------------------------------------------------------------------ Goken

        IEnumerator NeedleRush(PlayerCharacter player, int count)
        {
            for (int i = 0; i < count; i++)
            {
                var p = Player;
                if (p == null) yield break;
                Vector3 dir = p.Position - Position;
                dir.y = 0f;
                dir.Normalize();
                FaceTowards(dir, 1f);
                const float length = 12f;
                Track(Telegraph.Line(Position, dir, 1.8f, length, Windup(0.75f)));
                yield return new WaitForSeconds(Windup(0.75f));
                ClearTelegraphs();
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("dash", 0.9f);
                visual.Swing(-90f, 90f, 0.15f, 10f);
                Vector3 start = Position;
                var hit = new HashSet<Combatant>();
                float t = 0f;
                while (t < 0.22f)
                {
                    t += Time.deltaTime;
                    transform.position = BattleController.ClampToArena(start + dir * length * Mathf.Clamp01(t / 0.22f));
                    CombatSystem.HitRadius(this, Position, 1.2f, EnemyTag(1.5f, 5f, 3f), hit);
                    yield return null;
                }
                VFX.Flash(MeshFactory.Line(), start + Vector3.up, Quaternion.LookRotation(dir),
                    new Vector3(1.4f, 1f, length), new Vector3(0.1f, 1f, length), Def.accentColor, 0.3f);
                yield return new WaitForSeconds(0.3f);
            }
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator DoubleShockwave()
        {
            const float inner = 4f, outer = 9.5f;
            Track(Telegraph.Circle(Position, inner, Windup(0.9f)));
            yield return new WaitForSeconds(Windup(0.9f));
            ClearTelegraphs();
            Blast(Position, inner, 1.6f);
            // The ring follows immediately: the safe spot is now right next to the boss.
            Track(Telegraph.Ring(Position, inner, outer, Windup(0.8f)));
            yield return new WaitForSeconds(Windup(0.8f));
            ClearTelegraphs();
            VFX.Shockwave(Position, outer, Def.accentColor, 0.45f);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slam", 1f);
            var targets = new List<Combatant>(CombatSystem.Query(this, Position, Vector3.forward, outer, 360f));
            foreach (var t in targets)
            {
                float d = Vector3.Distance(new Vector3(t.Position.x, 0f, t.Position.z), new Vector3(Position.x, 0f, Position.z));
                if (d + t.Radius >= inner) CombatSystem.ApplyHit(this, t, EnemyTag(1.8f, 6f, 4f), Position);
            }
            yield return new WaitForSeconds(0.7f);
        }

        IEnumerator ScatterBlossoms()
        {
            for (int v = 0; v < 2; v++)
            {
                visual.Flash(new Color(0.3f, 0.6f, 1f), 0.8f);
                VFX.Breath(Position, new Color(0.3f, 0.6f, 1f), 30);
                yield return new WaitForSeconds(Windup(0.5f));
                float offset = v * 15f;
                for (int i = 0; i < 12; i++)
                {
                    var dir = Quaternion.Euler(0f, offset + i * 30f, 0f) * Vector3.forward;
                    EnemyProjectile.Fire(this, Position + dir, dir, 9f, 16f, EnemyTag(1.1f, 3f, 2f), new Color(0.4f, 0.7f, 1f), 0.55f);
                }
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("shoot", 0.9f);
                yield return new WaitForSeconds(0.6f);
            }
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator Annihilation()
        {
            superArmor = true;
            GameEvents.RaiseBanner("ANNIHILATION", "Reach the edge — or dodge at the last moment!");
            // Leap to the centre of the arena.
            Vector3 start = Position;
            Vector3 center = Vector3.zero;
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float k = t / 0.6f;
                transform.position = Vector3.Lerp(start, center, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 5f;
                yield return null;
            }
            transform.position = center;
            const float radius = 12.5f;
            float windup = 2.4f;
            Track(Telegraph.Circle(center, radius, windup));
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("charge", 1f);
            float c = 0f;
            while (c < windup)
            {
                c += Time.deltaTime;
                visual.SetCharge(c / windup, Def.accentColor);
                if (Random.value < 0.3f) VFX.Breath(Position, Def.accentColor, 3);
                yield return null;
            }
            ClearTelegraphs();
            visual.SetCharge(0f, Def.accentColor);
            Blast(center, radius, 4f);
            VFX.Pillar(center, Def.accentColor, 10f, 0.8f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.8f);
            superArmor = false;
            yield return ExhaustedWindow(3.2f);
        }

        IEnumerator ExhaustedWindow(float seconds)
        {
            Exhausted = true;
            DamageTakenMultiplier = 1.5f;
            DamageNumbers.SpawnText(Position + Vector3.up * 3.5f, "BREAK!", new Color(0.4f, 1f, 1f), 64f);
            GameEvents.RaiseBanner("BREAK!", "He's exhausted — go all out!");
            float t = 0f;
            while (t < seconds)
            {
                t += Time.deltaTime;
                visual.SetCharge(0.4f, new Color(0.4f, 1f, 1f));
                yield return null;
            }
            visual.SetCharge(0f, Color.white);
            DamageTakenMultiplier = 1f;
            Exhausted = false;
        }

        // ------------------------------------------------------------------ Thousand-Arm Demon

        IEnumerator Grasp(PlayerCharacter player)
        {
            FaceTowards(player.Position - Position, 1f);
            Vector3 fwd = transform.forward;
            Track(Telegraph.Sector(Position, fwd, 5f, 80f, Windup(0.8f)));
            yield return new WaitForSeconds(Windup(0.8f));
            ClearTelegraphs();
            visual.Swing(-60f, 60f, 0.12f, 30f);
            VFX.Slash(Position, fwd, 5f, 80f, 0f, Def.accentColor, 0.2f);
            CombatSystem.HitArc(this, Position, fwd, 5f, 80f, EnemyTag(1.4f, 5f, 3f));
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("enemyAttack", 0.8f);
            yield return new WaitForSeconds(0.6f);
        }

        IEnumerator GroundHands(PlayerCharacter player, int count)
        {
            var pending = new List<Vector3>();
            for (int i = 0; i < count; i++)
            {
                var p = Player;
                if (p == null) break;
                Vector3 spot = BattleController.ClampToArena(p.Position);
                pending.Add(spot);
                Track(Telegraph.Circle(spot, 1.9f, Windup(0.8f)));
                yield return new WaitForSeconds(Windup(0.35f));
                if (pending.Count >= 2) yield return HandErupt(pending[pending.Count - 2]);
            }
            yield return new WaitForSeconds(Windup(0.45f));
            if (pending.Count > 0) yield return HandErupt(pending[pending.Count - 1]);
            ClearTelegraphs();
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator HandErupt(Vector3 spot)
        {
            VFX.Pillar(spot, Def.accentColor, 3f, 0.35f);
            VFX.Smoke(spot, new Color(0.3f, 0.35f, 0.2f, 0.7f), 10);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slam", 0.6f);
            CombatSystem.HitRadius(this, spot, 1.9f, EnemyTag(1.2f, 4f, 3f));
            yield return null;
        }

        IEnumerator Summon()
        {
            summonTimer = 20f;
            GameEvents.RaiseBanner("SUMMON", "Lesser demons crawl from the earth");
            visual.Flash(Def.accentColor, 1f);
            yield return new WaitForSeconds(0.6f);
            var b = BattleController.Current;
            if (b != null && b.Mission != null)
            {
                for (int i = 0; i < 3; i++)
                {
                    Vector3 pos = Position + Quaternion.Euler(0f, i * 120f, 0f) * Vector3.forward * 3.5f;
                    b.Mission.SpawnEnemy("grunt", BattleController.ClampToArena(pos), false);
                }
            }
            yield return new WaitForSeconds(0.8f);
        }
    }
}
