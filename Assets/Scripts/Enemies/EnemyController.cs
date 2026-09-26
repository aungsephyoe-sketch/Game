using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Demon AI for the Normal / Fast / Tank / Ranged / Elite archetypes. Every attack is telegraphed on
    /// the ground and resolved against the zone when it lands, so a well-timed dodge always works.
    /// An attack-token limit stops crowds from attacking all at once.
    /// </summary>
    public class EnemyController : Combatant
    {
        protected enum State { Spawning, Chase, Attacking, Staggered, Dead }

        const int MaxSimultaneousAttackers = 3;
        static int attackTokensInUse;

        public EnemyDefinition Def { get; private set; }
        public int Level { get; private set; }
        public bool IsBoss { get { return Def.archetype == EnemyArchetype.Boss; } }
        public event System.Action<EnemyController> Killed;

        protected CharacterVisual visual;
        protected State state = State.Spawning;
        protected float attackTimer;
        protected float speedMultiplier = 1f;
        protected float windupMultiplier = 1f;
        protected readonly List<Telegraph> telegraphs = new List<Telegraph>();

        Coroutine attackRoutine;
        bool holdsToken;
        float staggerTimer;
        float poiseDamage;
        float poiseResetTimer;
        Vector3 knockVelocity;
        float strafeSign = 1f;
        float strafeTimer;

        protected PlayerCharacter Player
        {
            get
            {
                var b = BattleController.Current;
                if (b == null || b.Team == null) return null;
                var p = b.Team.Active;
                return p != null && p.IsAlive && p.gameObject.activeInHierarchy ? p : null;
            }
        }

        public static void ResetTokens() { attackTokensInUse = 0; }

        public virtual void Init(EnemyDefinition def, int level)
        {
            Def = def;
            Level = level;
            Team = CombatTeam.Enemy;
            Element = def.element;
            float hpMul = 1f + 0.12f * (level - 1);
            float atkMul = 1f + 0.09f * (level - 1);
            float defMul = 1f + 0.06f * (level - 1);
            Stats = new StatBlock(def.baseHp * hpMul, def.baseAtk * atkMul, def.baseDef * defMul, 0.05f, 1.5f, def.moveSpeed, 1f);
            Radius = def.radius;
            Health.Init(Stats.hp);
            Health.Died += OnDied;
            visual = CharacterVisual.BuildDemon(def, transform);
            attackTimer = Random.Range(0.6f, 1.6f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            StartCoroutine(SpawnRoutine());
        }

        IEnumerator SpawnRoutine()
        {
            state = State.Spawning;
            VFX.Smoke(Position, new Color(0.3f, 0.1f, 0.25f, 0.8f), 18);
            Vector3 end = transform.position;
            Vector3 start = end - Vector3.up * 2f;
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                transform.position = Vector3.Lerp(start, end, Mathf.SmoothStep(0f, 1f, t / 0.6f));
                yield return null;
            }
            transform.position = end;
            if (state == State.Spawning) state = State.Chase;
        }

        protected virtual void Update()
        {
            if (state == State.Dead) return;
            float dt = Time.deltaTime;

            if (knockVelocity.sqrMagnitude > 0.01f)
            {
                transform.position += knockVelocity * dt;
                knockVelocity = Vector3.Lerp(knockVelocity, Vector3.zero, 1f - Mathf.Exp(-dt * 8f));
            }

            poiseResetTimer -= dt;
            if (poiseResetTimer <= 0f) poiseDamage = 0f;

            switch (state)
            {
                case State.Staggered:
                    staggerTimer -= dt;
                    if (staggerTimer <= 0f) state = State.Chase;
                    break;
                case State.Chase:
                    Think(dt);
                    break;
            }

            Separate();
            var p = transform.position;
            p.y = 0f;
            if (state != State.Spawning) transform.position = BattleController.ClampToArena(p);
        }

        /// <summary>Default brain: approach, keep spacing, attack when in range and a token is free.</summary>
        protected virtual void Think(float dt)
        {
            var player = Player;
            if (player == null) { visual.SetMoving(0f); return; }
            Vector3 to = player.Position - Position;
            to.y = 0f;
            float dist = to.magnitude;
            attackTimer -= dt;

            float desired = Def.archetype == EnemyArchetype.Ranged ? 7f : Def.attackRange * 0.8f;
            Vector3 move = Vector3.zero;
            if (Def.archetype == EnemyArchetype.Ranged)
            {
                if (dist > desired + 1.5f) move = to.normalized;
                else if (dist < desired - 2f) move = -to.normalized;
                strafeTimer -= dt;
                if (strafeTimer <= 0f) { strafeTimer = Random.Range(1.5f, 3f); strafeSign = -strafeSign; }
                move += Vector3.Cross(Vector3.up, to.normalized) * strafeSign * 0.6f;
            }
            else if (dist > desired)
            {
                move = to.normalized;
                if (Def.archetype == EnemyArchetype.Fast) move += Vector3.Cross(Vector3.up, move) * Mathf.Sin(Time.time * 4f + GetInstanceID()) * 0.7f;
            }
            else if (!CanAttackNow())
            {
                // Waiting for a token: circle the player instead of queueing into them.
                move = Vector3.Cross(Vector3.up, to.normalized) * strafeSign * 0.5f;
            }

            if (move.sqrMagnitude > 0.001f)
            {
                transform.position += move.normalized * Stats.speed * speedMultiplier * dt * Mathf.Min(1f, move.magnitude);
                visual.SetMoving(1f);
            }
            else visual.SetMoving(0f);
            FaceTowards(to, dt * 8f);

            float range = Def.archetype == EnemyArchetype.Ranged ? Def.attackRange : Def.attackRange + player.Radius;
            if (dist <= range && attackTimer <= 0f && TryTakeToken())
                BeginAttack(ArchetypeAttack(player));
        }

        protected bool CanAttackNow()
        {
            return attackTimer <= 0f && (IsBoss || attackTokensInUse < MaxSimultaneousAttackers);
        }

        bool TryTakeToken()
        {
            if (IsBoss) return true;
            if (attackTokensInUse >= MaxSimultaneousAttackers) return false;
            attackTokensInUse++;
            holdsToken = true;
            return true;
        }

        void ReleaseToken()
        {
            if (!holdsToken) return;
            holdsToken = false;
            attackTokensInUse = Mathf.Max(0, attackTokensInUse - 1);
        }

        protected void FaceTowards(Vector3 dir, float t)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(t));
        }

        protected void BeginAttack(IEnumerator routine)
        {
            state = State.Attacking;
            visual.SetMoving(0f);
            attackRoutine = StartCoroutine(AttackWrapper(routine));
        }

        IEnumerator AttackWrapper(IEnumerator routine)
        {
            yield return routine;
            ClearTelegraphs();
            ReleaseToken();
            attackTimer = Def.attackCooldown * Random.Range(0.8f, 1.2f) / Mathf.Max(0.5f, speedMultiplier);
            attackRoutine = null;
            if (state == State.Attacking) state = State.Chase;
        }

        protected void CancelAttack()
        {
            if (attackRoutine != null) StopCoroutine(attackRoutine);
            attackRoutine = null;
            ClearTelegraphs();
            ReleaseToken();
            attackTimer = Def.attackCooldown * 0.6f;
        }

        protected Telegraph Track(Telegraph t)
        {
            telegraphs.Add(t);
            return t;
        }

        protected void ClearTelegraphs()
        {
            foreach (var t in telegraphs) if (t != null) t.Finish();
            telegraphs.Clear();
        }

        protected float Windup(float baseSeconds) { return baseSeconds * windupMultiplier; }

        protected AttackTag EnemyTag(float mult, float knockback = 3f, float stagger = 1f)
        {
            return new AttackTag { multiplier = mult, knockback = knockback, stagger = stagger, hitStop = 0.05f, shake = 0.3f, color = Def.accentColor };
        }

        // ------------------------------------------------------------------ Archetype attacks

        IEnumerator ArchetypeAttack(PlayerCharacter target)
        {
            switch (Def.archetype)
            {
                case EnemyArchetype.Fast: return LungeAttack(target);
                case EnemyArchetype.Tank: return SlamAttack();
                case EnemyArchetype.Ranged: return ShootAttack(target);
                case EnemyArchetype.Elite: return Random.value < 0.4f ? LeapAttack(target) : ClawCombo(target, 2);
                default: return ClawCombo(target, 1);
            }
        }

        /// <summary>Cone swipe(s) in front.</summary>
        protected IEnumerator ClawCombo(PlayerCharacter target, int swipes, float multiplier = 1f)
        {
            for (int i = 0; i < swipes; i++)
            {
                if (target != null) FaceTowards(target.Position - Position, 1f);
                Vector3 fwd = transform.forward;
                float range = Def.attackRange + 0.6f;
                Track(Telegraph.Sector(Position, fwd, range, 110f, Windup(Def.windup)));
                visual.Flash(Def.accentColor, 0.4f);
                yield return new WaitForSeconds(Windup(Def.windup));
                ClearTelegraphs();
                visual.Swing(-100f, 100f, 0.12f, 20f);
                VFX.Slash(Position, fwd, range, 110f, 0f, Def.accentColor, 0.18f);
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("enemyAttack", 0.5f);
                CombatSystem.HitArc(this, Position, fwd, range, 110f, EnemyTag(multiplier));
                yield return new WaitForSeconds(0.35f);
            }
        }

        IEnumerator LungeAttack(PlayerCharacter target)
        {
            FaceTowards(target.Position - Position, 1f);
            Vector3 fwd = transform.forward;
            float dist = 4f;
            Track(Telegraph.Line(Position, fwd, 1.2f, dist, Windup(Def.windup)));
            yield return new WaitForSeconds(Windup(Def.windup));
            ClearTelegraphs();
            visual.Swing(-90f, 90f, 0.12f, 15f);
            Vector3 start = Position;
            var hit = new HashSet<Combatant>();
            float t = 0f;
            while (t < 0.16f)
            {
                t += Time.deltaTime;
                transform.position = start + fwd * dist * Mathf.Clamp01(t / 0.16f);
                CombatSystem.HitRadius(this, Position, 0.9f, EnemyTag(1f, 2f), hit);
                yield return null;
            }
            yield return new WaitForSeconds(0.45f);
        }

        IEnumerator SlamAttack()
        {
            float r = Def.attackRange + 0.8f;
            Track(Telegraph.Circle(Position, r, Windup(Def.windup)));
            visual.Flash(Def.accentColor, 0.5f);
            yield return new WaitForSeconds(Windup(Def.windup));
            ClearTelegraphs();
            visual.Swing(0f, 0f, 0.1f, 90f);
            VFX.Shockwave(Position, r, Def.accentColor, 0.35f);
            VFX.Smoke(Position, new Color(0.5f, 0.4f, 0.3f, 0.6f), 14);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slam", 0.8f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.2f);
            CombatSystem.HitRadius(this, Position, r, EnemyTag(1.4f, 6f, 4f));
            yield return new WaitForSeconds(0.8f);
        }

        IEnumerator ShootAttack(PlayerCharacter target)
        {
            FaceTowards(target.Position - Position, 1f);
            Vector3 fwd = (target.Position - Position);
            fwd.y = 0f;
            fwd.Normalize();
            Track(Telegraph.Line(Position, fwd, 0.8f, 12f, Windup(Def.windup)));
            yield return new WaitForSeconds(Windup(Def.windup));
            ClearTelegraphs();
            EnemyProjectile.Fire(this, Position + fwd * 0.6f, fwd, 13f, 14f, EnemyTag(1f, 2f), Def.accentColor);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("shoot", 0.5f);
            yield return new WaitForSeconds(0.4f);
        }

        IEnumerator LeapAttack(PlayerCharacter target)
        {
            Vector3 landing = BattleController.ClampToArena(target.Position);
            const float r = 2.8f;
            Track(Telegraph.Circle(landing, r, Windup(1f)));
            Vector3 start = Position;
            float t = 0f;
            float dur = Windup(1f);
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                transform.position = Vector3.Lerp(start, landing, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 4f;
                yield return null;
            }
            transform.position = landing;
            ClearTelegraphs();
            VFX.Shockwave(landing, r, Def.accentColor, 0.35f);
            VFX.Smoke(landing, new Color(0.4f, 0.2f, 0.4f, 0.6f), 16);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slam", 0.8f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.25f);
            CombatSystem.HitRadius(this, landing, r, EnemyTag(1.6f, 6f, 4f));
            yield return new WaitForSeconds(0.7f);
        }

        // ------------------------------------------------------------------ Damage & death

        public override void OnHitReceived(DamageInfo info)
        {
            visual.Flash(Color.white, 1f);
            float weight = IsBoss ? 0.1f : Mathf.Max(0.3f, 1f / Def.scale);
            knockVelocity += info.knockback * weight * 2f;
            poiseDamage += info.staggerPower;
            poiseResetTimer = 3f;
            if (state != State.Spawning && poiseDamage > Def.poise && CanBeStaggered())
            {
                poiseDamage = 0f;
                Stagger(IsBoss ? 1.2f : 0.45f);
            }
        }

        protected virtual bool CanBeStaggered() { return true; }

        protected virtual void Stagger(float duration)
        {
            if (state == State.Dead) return;
            CancelAttack();
            state = State.Staggered;
            staggerTimer = duration;
            visual.SetMoving(0f);
        }

        protected virtual void OnDied()
        {
            state = State.Dead;
            CancelAttack();
            StopAllCoroutines();
            ClearTelegraphs();
            visual.PlayDeath();
            VFX.Smoke(Position, new Color(0.15f, 0.1f, 0.12f, 0.7f), IsBoss ? 60 : 20);
            VFX.Breath(Position, new Color(1f, 0.5f, 0.3f), IsBoss ? 80 : 15);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("enemyDeath", 0.6f);
            if (Killed != null) Killed(this);
            GameEvents.RaiseEnemyKilled(this);
            // Leave the registry immediately so nothing targets the corpse.
            All.Remove(this);
            Destroy(gameObject, 1.2f);
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            ReleaseToken();
            ClearTelegraphs();
        }

        void Separate()
        {
            for (int i = 0; i < All.Count; i++)
            {
                var o = All[i];
                if (o == this || !o.IsAlive) continue;
                Vector3 d = Position - o.Position;
                d.y = 0f;
                float min = Radius + o.Radius;
                float m = d.magnitude;
                if (m < min && m > 0.0001f)
                {
                    // Enemies push each other; the player is never pushed by demons (they step aside instead).
                    float push = (min - m) * (o.Team == CombatTeam.Player ? 1f : 0.5f);
                    transform.position += d / m * push;
                }
            }
        }
    }
}
