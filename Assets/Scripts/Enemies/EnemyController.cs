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
    public partial class EnemyController : Combatant
    {
        protected enum State { Spawning, Chase, Attacking, Staggered, Airborne, Down, Dead }

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
        float zigzagPhase;
        float slotAngle;
        float dodgeCooldown;
        float height;
        float verticalVelocity;
        float downTimer;
        bool alerted;

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

        /// <summary>The demon's own voice: shrieks for the quick and eerie ones, growls for the big ones.</summary>
        protected void Voice(float volume)
        {
            var gm = GameManager.Instance;
            if (gm == null || Def == null) return;
            bool shrill = Def.form == "shadow" || Def.form == "stalker" || Def.form == "void" || Def.form == "wraith" || Def.form == "imp" || Def.form == "hunter";
            float pitch = Mathf.Clamp(1.25f - Def.scale * 0.25f, 0.55f, 1.25f) * Random.Range(0.92f, 1.08f);
            gm.Audio.PlayPitched(shrill ? "screech" : "growl", volume, pitch);
        }

        public virtual void Init(EnemyDefinition def, int level)
        {
            Def = def;
            Level = level;
            Team = CombatTeam.Enemy;
            Element = def.element;
            float hpMul = 1f + 0.12f * (level - 1);
            float atkMul = 1f + 0.09f * (level - 1);
            float defMul = 1f + 0.06f * (level - 1);
            if (def.archetype == EnemyArchetype.Boss)
            {
                // Bosses grow with the story: each chapter's boss is noticeably tougher than the last.
                var battle = BattleController.Current;
                int ch = battle != null && battle.Def != null ? Mathf.Max(1, battle.Def.chapter) : 1;
                // Main bosses were still too tanky: 18% of their table HP, growing gently with each chapter.
                hpMul *= 0.18f * (1f + 0.08f * (ch - 1));
                atkMul *= 1f + 0.15f * (ch - 1);
                defMul *= 1f + 0.08f * (ch - 1);
            }
            else
            {
                // Regular demons go down faster.
                hpMul *= 0.55f;
            }
            Stats = new StatBlock(def.baseHp * hpMul, def.baseAtk * atkMul, def.baseDef * defMul, 0.05f, 1.5f, def.moveSpeed, 1f);
            Radius = def.radius;
            Health.Init(Stats.hp);
            Health.Died += OnDied;
            visual = CharacterVisual.BuildDemon(def, transform);
            attackTimer = Random.Range(0.6f, 1.6f);
            strafeSign = Random.value < 0.5f ? -1f : 1f;
            zigzagPhase = Random.Range(0f, 100f);
            slotAngle = Random.Range(0f, 360f);
            dodgeCooldown = Random.Range(2f, 5f);
            StartCoroutine(SpawnRoutine());
        }

        IEnumerator SpawnRoutine()
        {
            state = State.Spawning;
            if (Def.ambush && !IsBoss) { yield return AmbushRoutine(); yield break; }
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
            // Detection beat: a "!" pop so the player reads that this demon has spotted them.
            if (!alerted && !IsBoss)
            {
                alerted = true;
                DamageNumbers.SpawnText(Position + Vector3.up * (2.6f * Def.scale), "!", new Color(1f, 0.35f, 0.3f), 60f);
            }
        }

        /// <summary>While a special attack plays, every demon holds still.</summary>
        public static bool Frozen;

        protected virtual void Update()
        {
            if (state == State.Dead) return;
            if (Frozen) { if (visual != null) visual.SetMoving(0f); return; }
            float dt = Time.deltaTime;

            if (knockVelocity.sqrMagnitude > 0.01f)
            {
                transform.position += knockVelocity * dt;
                knockVelocity = Vector3.Lerp(knockVelocity, Vector3.zero, 1f - Mathf.Exp(-dt * 8f));
            }

            poiseResetTimer -= dt;
            if (poiseResetTimer <= 0f) poiseDamage = 0f;
            dodgeCooldown -= dt;
            UpdateBehaviours(dt);

            switch (state)
            {
                case State.Staggered:
                    staggerTimer -= dt;
                    if (staggerTimer <= 0f) state = State.Chase;
                    break;
                case State.Chase:
                    Think(dt);
                    break;
                case State.Airborne:
                    verticalVelocity -= 22f * dt;
                    height += verticalVelocity * dt;
                    if (height <= 0f)
                    {
                        height = 0f;
                        verticalVelocity = 0f;
                        state = State.Down;
                        downTimer = 0.55f;
                        visual.Knockdown();
                        VFX.Dust(Position, 6);
                        if (GameManager.Instance != null) GameManager.Instance.Audio.Play("thud", 0.4f);
                    }
                    break;
                case State.Down:
                    downTimer -= dt;
                    if (downTimer <= 0f)
                    {
                        visual.GetUp(0.35f);
                        state = State.Staggered;
                        staggerTimer = 0.35f;
                    }
                    break;
            }

            if (state != State.Airborne) Separate();
            if (state != State.Spawning)
            {
                var p = BattleController.ClampToArena(transform.position);
                p.y = height;
                transform.position = p;
            }
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

            if (TryEvade(player, to, dist)) return;
            if (TrySpecialBehaviour(player, to, dist)) return;

            float desired = Def.archetype == EnemyArchetype.Ranged ? 7f : Def.attackRange * 0.8f;
            Vector3 move = Vector3.zero;
            if (Def.archetype == EnemyArchetype.Ranged)
            {
                if (dist > desired + 1.5f) move = to.normalized;
                else if (dist < 3.5f) move = -to.normalized * 1.6f; // flee when a slayer closes in
                else if (dist < desired - 2f) move = -to.normalized;
                strafeTimer -= dt;
                if (strafeTimer <= 0f) { strafeTimer = Random.Range(1.5f, 3f); strafeSign = -strafeSign; }
                move += Vector3.Cross(Vector3.up, to.normalized) * strafeSign * 0.6f;
            }
            else if (dist > desired)
            {
                move = to.normalized;
                if (Def.archetype == EnemyArchetype.Fast) move += Vector3.Cross(Vector3.up, move) * Mathf.Sin(Time.time * 4f + zigzagPhase) * 0.7f;
            }
            else if (!CanAttackNow())
            {
                // Waiting for a token: hold a surround slot so demons flank instead of stacking up.
                slotAngle += strafeSign * dt * 25f;
                Vector3 slot = player.Position + Quaternion.Euler(0f, slotAngle, 0f) * Vector3.forward * (Def.attackRange + 1.4f);
                Vector3 toSlot = slot - Position;
                toSlot.y = 0f;
                move = toSlot.magnitude > 0.3f ? toSlot.normalized * 0.7f : Vector3.zero;
            }
            else if (dist > desired * 0.6f)
            {
                // Closing in for an attack: approach from the assigned flank.
                move = to.normalized;
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

        /// <summary>Fast and elite demons read the player's swing and hop back out of range.</summary>
        bool TryEvade(PlayerCharacter player, Vector3 to, float dist)
        {
            if (dodgeCooldown > 0f || dist > 3.2f) return false;
            if (Def.archetype != EnemyArchetype.Fast && Def.archetype != EnemyArchetype.Elite) return false;
            bool threatened = player.Action == PlayerCharacter.ActionKind.Attack || player.Action == PlayerCharacter.ActionKind.Charged;
            if (!threatened) return false;
            dodgeCooldown = Random.Range(3f, 6f);
            if (Random.value > 0.35f) return false;
            BeginAttack(EvadeRoutine(-to.normalized + Vector3.Cross(Vector3.up, to.normalized) * strafeSign * 0.6f));
            return true;
        }

        IEnumerator EvadeRoutine(Vector3 dir)
        {
            dir.y = 0f;
            dir.Normalize();
            Health.GrantInvulnerability(0.25f);
            visual.Dodge(dir, 0.2f);
            VFX.Dust(Position, 4);
            Vector3 start = Position;
            float t = 0f;
            while (t < 0.2f)
            {
                t += Time.deltaTime;
                transform.position = start + dir * 3.2f * Mathf.Clamp01(t / 0.2f);
                yield return null;
            }
            yield return new WaitForSeconds(0.15f);
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
                if (GameManager.Instance != null) { GameManager.Instance.Audio.PlayVaried("enemyAttack", 0.5f, 0.1f); Voice(0.35f); }
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
            if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("slam", 0.8f, Mathf.Clamp(1.15f - Def.scale * 0.15f, 0.7f, 1.1f));
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
            if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("slam", 0.8f, Mathf.Clamp(1.15f - Def.scale * 0.15f, 0.7f, 1.1f));
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.25f);
            CombatSystem.HitRadius(this, landing, r, EnemyTag(1.6f, 6f, 4f));
            yield return new WaitForSeconds(0.7f);
        }

        // ------------------------------------------------------------------ Damage & death

        bool IsLight { get { return Def.archetype == EnemyArchetype.Normal || Def.archetype == EnemyArchetype.Fast || Def.archetype == EnemyArchetype.Ranged; } }

        public override void OnHitReceived(DamageInfo info)
        {
            visual.Flash(Color.white, 1f);
            float weight = IsBoss ? 0.1f : Mathf.Max(0.3f, 1f / Def.scale);
            if (state == State.Down) weight *= 0.3f;
            knockVelocity += info.knockback * weight * 2f;
            // Elemental weakness breaks guard faster.
            poiseDamage += info.staggerPower * (info.elementMultiplier > 1.01f ? 1.5f : 1f);
            poiseResetTimer = 3f;
            if (state == State.Spawning || state == State.Dead) return;

            if (state == State.Airborne)
            {
                // Juggle: every hit keeps a launched demon up a little longer.
                verticalVelocity = Mathf.Max(verticalVelocity, 4.5f);
                return;
            }
            if (!IsBoss && info.launch && IsLight && state != State.Down)
            {
                Launch(7.5f);
                return;
            }
            if (!IsBoss && info.heavy && (IsLight || poiseDamage > Def.poise) && CanBeStaggered())
            {
                Launch(IsLight ? 5f : 3f);
                return;
            }
            if (poiseDamage > Def.poise && CanBeStaggered())
            {
                poiseDamage = 0f;
                Stagger(IsBoss ? 1.2f : 0.45f);
                if (info.source != null) visual.Hit(info.source.Position - Position);
            }
            else if (info.source != null && state != State.Down) visual.Hit(info.source.Position - Position);
        }

        void Launch(float upSpeed)
        {
            CancelAttack();
            state = State.Airborne;
            verticalVelocity = upSpeed;
            visual.SetMoving(0f);
            visual.Hit(-transform.forward);
        }

        /// <summary>The player parried this demon's attack: long stagger, attack cancelled.</summary>
        public virtual void OnParried(PlayerCharacter by)
        {
            if (state == State.Dead) return;
            poiseDamage = 0f;
            visual.Flash(Color.white, 1f);
            Stagger(IsBoss ? 1.4f : 1.3f);
            knockVelocity += (Position - by.Position).normalized * (IsBoss ? 1f : 4f);
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
            height = 0f;
            var gp = transform.position;
            gp.y = 0f;
            transform.position = gp;
            CancelAttack();
            StopAllCoroutines();
            ClearTelegraphs();
            visual.PlayDeath();
            VFX.Smoke(Position, new Color(0.15f, 0.1f, 0.12f, 0.7f), IsBoss ? 60 : 20);
            VFX.Breath(Position, new Color(1f, 0.5f, 0.3f), IsBoss ? 80 : 15);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("enemyDeath", 0.6f);
            // Demons drop gold that flies into the active slayer.
            if (Def.form != "dummy")
            {
                int each = 4 + Level * 2;
                bool big = Def.archetype == EnemyArchetype.Elite || Def.archetype == EnemyArchetype.Tank;
                GoldCoin.Burst(Position, IsBoss ? 24 : (big ? 6 : 3), IsBoss ? each * 5 : each);
                // Sometimes a demon drops diamonds too: bosses always, elites often, others now and then.
                if (IsBoss) GoldCoin.Diamonds(Position, 3, 5);
                else if (Random.value < (big ? 0.3f : 0.07f)) GoldCoin.Diamonds(Position, 1, big ? 3 : 1);
            }
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
