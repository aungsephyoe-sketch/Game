using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// One slayer on the field. Built around responsiveness:
    ///  • Input buffer (0.28 s): a press made slightly early is never dropped – it fires as soon as it's legal.
    ///  • Cancel windows: combos cancel into skills, dodge, guard and movement after their active frames.
    ///  • 5-hit combo, hold-to-charge heavy, dash attack (attack out of a dodge), sprint after sustained movement.
    ///  • Guard (hold) with parry (tap just before a hit), dodge with i-frames and perfect-dodge slow motion.
    ///  • Hit stun and knockdown with quick-rise (dodge while down), super armour during skills, invulnerable ultimates.
    /// </summary>
    public class PlayerCharacter : Combatant
    {
        public enum ActionKind { None, Attack, Charged, DashAttack, Skill, Dodge, Ultimate, SwitchIn, HitStun, Knockdown, Jump }

        enum Buffered { None, Attack, Dodge, Skill0, Skill1, Skill2, Ultimate }

        public const float UltMax = 100f;
        const float ChargeStart = 0.3f;
        const float ChargeFull = 0.8f;
        const float BufferWindow = 0.28f;
        const float ParryWindow = 0.18f;
        const float DashAttackWindow = 0.3f;
        const float SprintDelay = 0.45f;

        public CharacterDefinition Def { get; private set; }
        public OwnedCharacter Owned { get; private set; }
        public CharacterVisual Visual { get; private set; }
        public readonly float[] Cooldowns = new float[3];
        public float UltGauge;
        public ActionKind Action { get; private set; }
        public bool UltReady { get { return UltGauge >= UltMax; } }
        public float ChargeAmount { get; private set; }
        public int ComboStep { get { return comboIndex; } }
        public bool Guarding { get; private set; }
        public bool Sprinting { get; private set; }
        public bool CanSwitchOut { get { return Action != ActionKind.Ultimate && Action != ActionKind.Knockdown && IsAlive; } }

        Coroutine actionRoutine;
        int comboIndex;
        float comboResetTimer;
        bool attackQueued;
        bool canMoveCancel;
        float attackHeldTime;
        bool attackHeldLast;
        float dodgeCooldown;
        float lastDodgeEnd = -10f;
        float lastPerfectDodge = -10f;
        float guardPressedAt = -10f;
        float lastParry = -10f;
        float knockdownAt;
        float moveTime;
        float stepTimer;
        Vector3 knockVelocity;
        Vector3 moveInput;
        Buffered buffered;
        float bufferedAt;
        bool ultimateActive;

        Vector3 velocity;
        bool wasSprinting;

        /// <summary>The demon the camera and attacks are locked onto (shared by the whole team).</summary>
        public static Combatant LockTarget;

        Color ElementColor { get { return ElementChart.ColorOf(Def.element); } }
        AudioManager Audio { get { return GameManager.Instance != null ? GameManager.Instance.Audio : null; } }

        public void Init(CharacterDefinition def, OwnedCharacter owned, StatBlock stats)
        {
            Def = def;
            Owned = owned;
            Team = CombatTeam.Player;
            Element = def.element;
            Stats = stats;
            Radius = 0.45f;
            Health.Init(stats.hp);
            Health.Evaded += OnEvaded;
            Health.Died += OnDied;
            Health.Filter = GuardFilter;
            Visual = CharacterVisual.BuildHero(def, transform);
        }

        float SkillLevelMult(int index) { return CharacterSystem.SkillLevelMultiplier(Owned.skillLevels[index]); }

        void Play(string id, float vol)
        {
            var a = Audio;
            if (a != null) a.Play(id, vol);
        }

        public void TickCooldowns(float dt)
        {
            for (int i = 0; i < Cooldowns.Length; i++) Cooldowns[i] = Mathf.Max(0f, Cooldowns[i] - dt);
            dodgeCooldown = Mathf.Max(0f, dodgeCooldown - dt);
        }

        // ------------------------------------------------------------------ Input

        public void HandleInput(InputState input, float dt)
        {
            if (!IsAlive) return;
            moveInput = new Vector3(input.move.x, 0f, input.move.y);
            if (moveInput.sqrMagnitude > 1f) moveInput.Normalize();

            if (Action != ActionKind.None) comboResetTimer = 0.7f;
            else if (comboResetTimer > 0f)
            {
                comboResetTimer -= dt;
                if (comboResetTimer <= 0f) comboIndex = 0;
            }

            if (input.lockDown) CycleLock();
            if (LockTarget != null && !LockTarget.IsAlive) LockTarget = null;
            if (input.jumpDown && (Action == ActionKind.None || ((Action == ActionKind.Attack || Action == ActionKind.DashAttack) && canMoveCancel)))
                StartAction(JumpRoutine(), ActionKind.Jump);

            // Record the newest press; priority order resolves same-frame presses.
            if (input.dodgeDown) Buffer(Buffered.Dodge);
            else if (input.ultimateDown) Buffer(Buffered.Ultimate);
            else if (input.skill1Down) Buffer(Buffered.Skill0);
            else if (input.skill2Down) Buffer(Buffered.Skill1);
            else if (input.skill3Down) Buffer(Buffered.Skill2);
            else if (input.attackDown)
            {
                attackHeldTime = 0f;
                Buffer(Buffered.Attack);
            }
            if (input.guardDown) guardPressedAt = Time.time;

            ConsumeBuffer();

            // Guard is a held stance available whenever the slayer is free.
            bool wantGuard = input.guardHeld && Action == ActionKind.None;
            if (wantGuard != Guarding)
            {
                Guarding = wantGuard;
                Visual.Guard(Guarding);
                if (Guarding) Play("guard", 0.35f);
            }

            // Hold attack = charge (only while otherwise idle).
            bool held = input.attackHeld;
            if (held) attackHeldTime += dt;
            ChargeAmount = held && Action == ActionKind.None && !Guarding
                ? Mathf.Clamp01((attackHeldTime - ChargeStart) / (ChargeFull - ChargeStart)) : 0f;
            if (!held && attackHeldLast && Action == ActionKind.None && attackHeldTime >= ChargeFull)
            {
                attackHeldTime = 0f;
                StartAction(ChargedRoutine(), ActionKind.Charged);
            }
            attackHeldLast = held;
            Visual.SetCharge(ChargeAmount, ElementColor);

            // Movement cancels the tail of normal attacks so the character never feels stuck.
            // (Never when the next hit is already queued – players often hold the stick while tapping attack.)
            if (canMoveCancel && !attackQueued && buffered == Buffered.None && !input.attackHeld && moveInput.sqrMagnitude > 0.2f &&
                (Action == ActionKind.Attack || Action == ActionKind.DashAttack))
                StopAction();

            if (Action == ActionKind.None) Move(dt);
            else if (Action != ActionKind.Jump)
            {
                moveTime = 0f;
                Sprinting = false;
                velocity = Vector3.zero;
                Visual.SetMoving(0f);
            }
        }

        // ------------------------------------------------------------------ Lock-on

        void CycleLock()
        {
            // Lock the boss first, then the nearest demon; pressing again moves to the next one, then releases.
            var candidates = new List<Combatant>();
            foreach (var c in All)
                if (c.Team != Team && c.IsAlive && Vector3.Distance(c.Position, Position) < 18f) candidates.Add(c);
            if (candidates.Count == 0) { LockTarget = null; return; }
            candidates.Sort((a, b) =>
            {
                bool ab = a is BossController, bb = b is BossController;
                if (ab != bb) return ab ? -1 : 1;
                return Vector3.Distance(a.Position, Position).CompareTo(Vector3.Distance(b.Position, Position));
            });
            int idx = LockTarget != null ? candidates.IndexOf(LockTarget) : -1;
            LockTarget = idx + 1 < candidates.Count ? candidates[idx + 1] : null;
            if (LockTarget == null && idx < 0) LockTarget = candidates[0];
            Play("click", 0.4f);
        }

        // ------------------------------------------------------------------ Jump / plunge

        IEnumerator JumpRoutine()
        {
            const float up = 0.42f, height = 2.3f;
            Play("dodge", 0.5f);
            VFX.Dust(Position, 6);
            Health.GrantInvulnerability(0.35f);
            var model = Visual.transform;
            float t = 0f;
            bool plunge = false;
            while (t < up * 2f)
            {
                t += Time.deltaTime;
                float k = t / (up * 2f);
                model.localPosition = new Vector3(0f, Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI) * height, 0f);
                // Air control.
                transform.position = BattleController.ClampToArena(transform.position + moveInput * Stats.speed * 0.8f * Time.deltaTime);
                if (moveInput.sqrMagnitude > 0.01f) Face(moveInput, 12f * Time.deltaTime);
                if (buffered == Buffered.Attack && t > 0.12f) { buffered = Buffered.None; plunge = true; break; }
                yield return null;
            }
            if (plunge)
            {
                // Plunging strike: hang for a beat, then slam down.
                AutoAim(6f);
                Visual.HeavyAttack(0.12f);
                float y0 = model.localPosition.y;
                yield return new WaitForSeconds(0.06f);
                float e = 0f;
                while (e < 0.1f)
                {
                    e += Time.deltaTime;
                    model.localPosition = new Vector3(0f, Mathf.Lerp(y0, 0f, e / 0.1f), 0f);
                    yield return null;
                }
                model.localPosition = Vector3.zero;
                var tag = AttackTag.Basic(Def.chargedMultiplier * 0.9f, ElementColor);
                tag.knockback = 6f; tag.stagger = 6f; tag.hitStop = 0.09f; tag.shake = 0.4f; tag.heavy = true;
                CombatSystem.HitRadius(this, Position, 3.2f, tag);
                VFX.Shockwave(Position, 3.4f, ElementColor, 0.4f);
                VFX.BurstDisc(Position, 2.5f, ElementColor, 0.3f);
                VFX.Dust(Position, 14);
                Play("heavy", 0.9f);
                UltGauge = Mathf.Min(UltMax, UltGauge + 3f);
                yield return new WaitForSeconds(0.25f);
            }
            else
            {
                model.localPosition = Vector3.zero;
                VFX.Dust(Position, 5);
                Play("step", 0.4f);
                yield return new WaitForSeconds(0.06f);
            }
        }

        void Move(float dt)
        {
            bool moving = moveInput.sqrMagnitude > 0.01f;
            moveTime = moving ? moveTime + dt : 0f;
            Sprinting = moving && !Guarding && ChargeAmount <= 0f && moveTime > SprintDelay && moveInput.magnitude > 0.7f;

            float speed = Stats.speed;
            if (Guarding) speed *= 0.35f;
            else if (ChargeAmount > 0f) speed *= 0.4f;
            else if (Sprinting) speed *= 1.35f;

            // Accelerate into motion and slide to a stop instead of snapping.
            Vector3 targetVel = moveInput * speed;
            float accel = moving ? (Vector3.Dot(velocity, targetVel) < 0f ? 70f : 45f) : 32f;
            velocity = Vector3.MoveTowards(velocity, targetVel, accel * dt);
            if (!moving && wasSprinting && velocity.sqrMagnitude > 4f)
            {
                VFX.Dust(Position, 5);
                Play("step", 0.3f);
            }
            wasSprinting = Sprinting || (wasSprinting && moving);
            if (velocity.sqrMagnitude > 0.0004f)
                transform.position = BattleController.ClampToArena(transform.position + velocity * dt);
            if (moving)
            {
                if (!Guarding) Face(LockTarget != null && !Sprinting ? Vector3.Lerp(moveInput, LockTarget.Position - Position, 0.35f) : moveInput, 16f * dt);
                stepTimer -= dt;
                if (stepTimer <= 0f)
                {
                    stepTimer = Sprinting ? 0.24f : 0.32f;
                    Play("step", Sprinting ? 0.3f : 0.18f);
                    if (Sprinting) VFX.Dust(Position, 3);
                }
            }
            if (Guarding)
            {
                var near = LockTarget != null ? LockTarget : Nearest(CombatTeam.Enemy, Position, 8f);
                if (near != null) Face(near.Position - Position, 14f * dt);
            }
            Visual.SetMoving(Mathf.Clamp01(velocity.magnitude / Mathf.Max(0.1f, Stats.speed)), Sprinting);
        }

        void Buffer(Buffered b)
        {
            buffered = b;
            bufferedAt = Time.time;
        }

        void ConsumeBuffer()
        {
            if (buffered == Buffered.None) return;
            if (Time.time - bufferedAt > BufferWindow) { buffered = Buffered.None; return; }

            switch (buffered)
            {
                case Buffered.Dodge:
                    if (CanDodge()) { buffered = Buffered.None; StartAction(DodgeRoutine(), ActionKind.Dodge); }
                    break;
                case Buffered.Ultimate:
                    if (UltReady && CanUseAbility(true)) { buffered = Buffered.None; StartAction(UltimateRoutine(), ActionKind.Ultimate); }
                    else if (!UltReady) buffered = Buffered.None;
                    break;
                case Buffered.Skill0:
                case Buffered.Skill1:
                case Buffered.Skill2:
                    int i = buffered - Buffered.Skill0;
                    if (Cooldowns[i] > 0f) { buffered = Buffered.None; break; }
                    if (CanUseAbility(false)) { buffered = Buffered.None; StartAction(SkillRoutine(i), ActionKind.Skill); }
                    break;
                case Buffered.Attack:
                    if (Action == ActionKind.Attack || Action == ActionKind.DashAttack)
                    {
                        attackQueued = true;
                        buffered = Buffered.None;
                    }
                    else if (Action == ActionKind.None)
                    {
                        buffered = Buffered.None;
                        if (Time.time - lastDodgeEnd < DashAttackWindow) StartAction(DashAttackRoutine(), ActionKind.DashAttack);
                        else StartAction(ComboRoutine(), ActionKind.Attack);
                    }
                    // During a dodge the press stays buffered and becomes a dash attack when the dodge ends.
                    break;
            }
        }

        bool CanDodge()
        {
            if (dodgeCooldown > 0f) return false;
            switch (Action)
            {
                case ActionKind.Ultimate:
                case ActionKind.Dodge:
                    return false;
                case ActionKind.Knockdown:
                    return Time.time - knockdownAt > 0.35f; // quick rise
                default:
                    return true;
            }
        }

        bool CanUseAbility(bool ultimate)
        {
            switch (Action)
            {
                case ActionKind.None:
                case ActionKind.Attack:
                case ActionKind.DashAttack:
                case ActionKind.SwitchIn:
                    return true;
                case ActionKind.Charged:
                case ActionKind.Skill:
                    return ultimate;
                default:
                    return false;
            }
        }

        void Update()
        {
            if (knockVelocity.sqrMagnitude > 0.01f)
            {
                transform.position = BattleController.ClampToArena(transform.position + knockVelocity * Time.deltaTime);
                knockVelocity = Vector3.Lerp(knockVelocity, Vector3.zero, 1f - Mathf.Exp(-Time.deltaTime * 10f));
            }
        }

        void Face(Vector3 dir, float t)
        {
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f) return;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), Mathf.Clamp01(t));
        }

        /// <summary>Soft aim assist: faces the best demon, preferring the stick direction.</summary>
        Combatant AutoAim(float range)
        {
            if (LockTarget != null && LockTarget.IsAlive && Vector3.Distance(LockTarget.Position, Position) <= range + 3f)
            {
                Face(LockTarget.Position - Position, 1f);
                return LockTarget;
            }
            Combatant best = null;
            float bestScore = float.MaxValue;
            Vector3 stick = moveInput.sqrMagnitude > 0.05f ? moveInput.normalized : Vector3.zero;
            foreach (var c in All)
            {
                if (c.Team == Team || !c.IsAlive) continue;
                Vector3 to = c.Position - Position;
                to.y = 0f;
                float d = to.magnitude;
                if (d > range) continue;
                float score = d;
                if (stick != Vector3.zero && d > 0.01f) score *= 1.6f - Vector3.Dot(stick, to / d) * 0.6f;
                if (score < bestScore) { bestScore = score; best = c; }
            }
            if (best != null) Face(best.Position - Position, 1f);
            else if (stick != Vector3.zero) Face(stick, 1f);
            return best;
        }

        void StartAction(IEnumerator routine, ActionKind kind)
        {
            StopAction();
            if (Guarding)
            {
                Guarding = false;
                Visual.Guard(false);
            }
            Action = kind;
            actionRoutine = StartCoroutine(Wrap(routine));
        }

        IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            Action = ActionKind.None;
            actionRoutine = null;
            canMoveCancel = false;
        }

        void StopAction()
        {
            if (actionRoutine != null) StopCoroutine(actionRoutine);
            actionRoutine = null;
            if (Action == ActionKind.Ultimate) FinishUltimateEffects();
            if (Action == ActionKind.Knockdown) Visual.GetUp(0.15f);
            if (Action == ActionKind.Jump) Visual.transform.localPosition = Vector3.zero;
            Action = ActionKind.None;
            attackQueued = false;
            canMoveCancel = false;
        }

        // ------------------------------------------------------------------ Normal attacks

        static readonly float[] SlashRoll = { 10f, -15f, 70f, -5f, 0f };

        IEnumerator ComboRoutine()
        {
            while (true)
            {
                attackQueued = false;
                canMoveCancel = false;
                int step = comboIndex;
                bool finisher = step >= Def.comboMultipliers.Length - 1;
                float spd = Def.attackSpeed;
                var target = AutoAim(5.5f);

                float lunge = 0.35f;
                if (target != null)
                {
                    float d = Vector3.Distance(target.Position, Position) - target.Radius - 1.2f;
                    lunge = Mathf.Clamp(d, 0f, 1.8f);
                }
                Visual.Attack(step, 0.12f / spd);
                float windup = 0.07f / spd;
                float t = 0f;
                Vector3 start = Position;
                while (t < windup)
                {
                    t += Time.deltaTime;
                    transform.position = BattleController.ClampToArena(start + transform.forward * lunge * Mathf.Clamp01(t / windup));
                    yield return null;
                }

                var tag = AttackTag.Basic(Def.comboMultipliers[step], ElementColor);
                float range = 2.7f, arc = 170f;
                if (finisher)
                {
                    tag.knockback = 6f;
                    tag.stagger = 5f;
                    tag.hitStop = 0.08f;
                    tag.shake = 0.35f;
                    tag.heavy = true;
                    range = 3.3f;
                    arc = 360f;
                }
                else if (step == 2)
                {
                    tag.stagger = 1.6f;
                    tag.launch = true; // third hit pops light demons into the air for juggles
                }
                CombatSystem.HitArc(this, Position, transform.forward, range, arc, tag);
                VFX.Slash(Position, transform.forward, range, finisher ? 330f : 160f, SlashRoll[step % 5], ElementColor, 0.2f);
                if (finisher) VFX.Shockwave(Position, range, ElementColor, 0.3f);
                Play(finisher ? "heavy" : "slash", finisher ? 0.7f : 0.5f);

                comboIndex = finisher ? 0 : step + 1;

                float recovery = (finisher ? 0.4f : 0.2f) / spd;
                float cancelAt = (finisher ? 0.22f : 0.07f) / spd;
                float r = 0f;
                while (r < recovery)
                {
                    r += Time.deltaTime;
                    if (r >= cancelAt) canMoveCancel = true;
                    if (attackQueued && r >= cancelAt) break;
                    yield return null;
                }
                if (!attackQueued) yield break;
            }
        }

        IEnumerator DashAttackRoutine()
        {
            canMoveCancel = false;
            AutoAim(7f);
            Visual.DashAttack(0.14f);
            Play("dash", 0.6f);
            var tag = AttackTag.Basic(1.3f, ElementColor);
            tag.stagger = 2.5f;
            tag.knockback = 3f;
            var hit = new System.Collections.Generic.HashSet<Combatant>();
            Vector3 start = Position;
            Vector3 dir = transform.forward;
            float t = 0f;
            while (t < 0.14f)
            {
                t += Time.deltaTime;
                transform.position = BattleController.ClampToArena(start + dir * 4.2f * Mathf.Clamp01(t / 0.14f));
                CombatSystem.HitRadius(this, Position, 1.6f, tag, hit);
                yield return null;
            }
            VFX.Flash(MeshFactory.Line(), start + Vector3.up, Quaternion.LookRotation(dir), new Vector3(1.2f, 1f, 4.2f), new Vector3(0.05f, 1f, 4.2f), ElementColor, 0.25f);
            comboIndex = 1; // flows straight into the 2nd combo hit
            float r = 0f;
            while (r < 0.2f)
            {
                r += Time.deltaTime;
                if (r > 0.06f) canMoveCancel = true;
                if (attackQueued) break;
                yield return null;
            }
            if (attackQueued)
            {
                Action = ActionKind.Attack;
                yield return ComboRoutine();
            }
        }

        IEnumerator ChargedRoutine()
        {
            AutoAim(6f);
            Visual.SetCharge(0f, ElementColor);
            Visual.HeavyAttack(0.16f);
            yield return new WaitForSeconds(0.06f);
            var tag = AttackTag.Basic(Def.chargedMultiplier, ElementColor);
            tag.knockback = 8f;
            tag.stagger = 7f;
            tag.hitStop = 0.1f;
            tag.shake = 0.45f;
            tag.heavy = true;
            CombatSystem.HitArc(this, Position, transform.forward, 4.4f, 210f, tag);
            VFX.Slash(Position, transform.forward, 4.4f, 210f, 0f, ElementColor, 0.3f);
            VFX.Breath(Position + transform.forward * 2f, ElementColor, 30);
            VFX.Dust(Position + transform.forward * 1.5f, 10);
            Play("heavy", 0.9f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 3f);
            yield return new WaitForSeconds(0.32f);
            comboIndex = 0;
        }

        // ------------------------------------------------------------------ Dodge / guard / parry

        IEnumerator DodgeRoutine()
        {
            Vector3 dir = moveInput.sqrMagnitude > 0.05f ? moveInput.normalized : -transform.forward;
            dodgeCooldown = 0.4f;
            const float duration = 0.2f;
            Health.GrantInvulnerability(0.3f);
            Visual.Dodge(dir, duration);
            VFX.Dust(Position, 6);
            Play("dodge", 0.6f);
            Vector3 start = Position;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f);
                transform.position = BattleController.ClampToArena(start + dir * 4.5f * k);
                yield return null;
            }
            if (moveInput.sqrMagnitude > 0.05f) Face(moveInput, 1f);
            lastDodgeEnd = Time.time;
            yield return new WaitForSeconds(0.04f);
        }

        void OnEvaded(DamageInfo info)
        {
            if (Action != ActionKind.Dodge || info.source == null || info.source.Team == Team) return;
            if (Time.unscaledTime - lastPerfectDodge < 1.2f) return;
            lastPerfectDodge = Time.unscaledTime;
            UltGauge = Mathf.Min(UltMax, UltGauge + 12f);
            TimeController.SlowMotion(0.25f, 0.6f);
            Health.GrantInvulnerability(0.4f);
            DamageNumbers.SpawnText(Position + Vector3.up * 2.4f, "PERFECT DODGE", new Color(0.6f, 0.9f, 1f), 48f);
            Play("perfect", 0.8f);
            GameEvents.RaisePerfectDodge();
        }

        /// <summary>Guard blocks frontal hits (20% chip damage); pressing guard just before a hit parries it.</summary>
        bool GuardFilter(ref DamageInfo info)
        {
            if (!Guarding || info.source == null || info.source.Team == Team) return true;
            Vector3 to = info.source.Position - Position;
            to.y = 0f;
            if (to.sqrMagnitude > 0.01f && Vector3.Dot(transform.forward, to.normalized) < 0.1f) return true;

            if (Time.time - guardPressedAt <= ParryWindow && Time.unscaledTime - lastParry > 0.3f)
            {
                Parry(info.source);
                return false;
            }
            info.amount = Mathf.Max(1f, Mathf.Round(info.amount * 0.2f));
            info.blocked = true;
            info.staggerPower = 0f;
            info.knockback *= 0.35f;
            VFX.HitSpark(Position + Vector3.up * 1.1f + transform.forward * 0.5f, new Color(1f, 0.9f, 0.6f), 10);
            Play("block", 0.6f);
            return true;
        }

        void Parry(Combatant attacker)
        {
            lastParry = Time.unscaledTime;
            Health.GrantInvulnerability(0.35f);
            var e = attacker as EnemyController;
            if (e != null) e.OnParried(this);
            UltGauge = Mathf.Min(UltMax, UltGauge + 15f);
            TimeController.SlowMotion(0.2f, 0.45f);
            VFX.HitSpark(Position + Vector3.up * 1.2f + transform.forward * 0.6f, Color.white, 30);
            VFX.Shockwave(Position, 2.5f, Color.white, 0.25f);
            VFX.ImpactLight(Position + Vector3.up, Color.white, 6f, 0.2f);
            DamageNumbers.SpawnText(Position + Vector3.up * 2.4f, "PARRY!", new Color(1f, 0.95f, 0.5f), 56f);
            if (CameraController.Instance != null) CameraController.Instance.Punch(0.9f, 0.25f);
            GameEvents.RaiseImpact(0.6f);
            Play("parry", 1f);
        }

        // ------------------------------------------------------------------ Skills & ultimate

        IEnumerator SkillRoutine(int index)
        {
            var ab = Def.skills[index];
            Cooldowns[index] = ab.cooldown;
            AutoAim(ab.shape == AbilityShape.Wave || ab.shape == AbilityShape.Dash ? ab.range + 2f : 7f);
            Visual.Skill(index, 0.4f);
            GameEvents.RaiseSkillUsed(this, ab);
            Play("skill", 0.7f);
            yield return AbilitySystem.Execute(this, ab, SkillLevelMult(index), false, new DamageTally());
        }

        IEnumerator UltimateRoutine()
        {
            var ab = Def.ultimate;
            UltGauge = 0f;
            AutoAim(8f);
            Health.PushInvulnerable();
            ultimateActive = true;
            GameEvents.RaiseUltimateStarted(this, ab);
            Play("ultimate", 1f);
            Visual.Ultimate();
            if (CameraController.Instance != null) CameraController.Instance.PlayUltimateCinematic(transform, 0.95f);
            SceneLighting.UltimateMood(ElementColor, 1.8f);
            TimeController.SlowMotion(0.12f, 0.95f);
            VFX.Breath(Position, ElementColor, 80);
            VFX.ImpactLight(Position + Vector3.up * 1.5f, ElementColor, 8f, 1f);
            Visual.SetCharge(1f, ElementColor);
            yield return new WaitForSecondsRealtime(0.95f);

            Visual.SetCharge(0f, ElementColor);
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetZoom(1.15f, 3f);
                CameraController.Instance.Shake(0.6f);
            }
            GameEvents.RaiseImpact(1f);
            var tally = new DamageTally();
            yield return AbilitySystem.Execute(this, ab, SkillLevelMult(3), true, tally);
            yield return new WaitForSeconds(0.2f);
            FinishUltimateEffects();
            GameEvents.RaiseUltimateFinished(this, tally.total);
        }

        void FinishUltimateEffects()
        {
            if (!ultimateActive) return;
            ultimateActive = false;
            Health.PopInvulnerable();
            Visual.SetCharge(0f, ElementColor);
            if (CameraController.Instance != null)
            {
                CameraController.Instance.EndCinematic();
                CameraController.Instance.SetZoom(1f, 3f);
            }
        }

        // ------------------------------------------------------------------ Switching

        /// <summary>Tag-in attack when switched onto the field.</summary>
        public void OnSwitchIn()
        {
            Health.GrantInvulnerability(0.5f);
            StartAction(SwitchInRoutine(), ActionKind.SwitchIn);
        }

        IEnumerator SwitchInRoutine()
        {
            Visual.Spin(0.22f);
            VFX.Breath(Position, ElementColor, 40);
            VFX.Shockwave(Position, 3.2f, ElementColor, 0.35f);
            var tag = AttackTag.Basic(1.2f, ElementColor);
            tag.special = true;
            tag.knockback = 4f;
            tag.stagger = 3f;
            CombatSystem.HitRadius(this, Position, 3.2f, tag);
            Play("switch", 0.7f);
            yield return new WaitForSeconds(0.2f);
        }

        public void OnSwitchOut()
        {
            StopAction();
            ChargeAmount = 0f;
            attackHeldLast = false;
            comboIndex = 0;
            buffered = Buffered.None;
            Guarding = false;
            Sprinting = false;
            knockVelocity = Vector3.zero;
            Visual.SetCharge(0f, ElementColor);
            Visual.ResetPose();
        }

        // ------------------------------------------------------------------ End of battle

        public void PlayVictory()
        {
            StopAction();
            Guarding = false;
            Visual.SetMoving(0f);
            Visual.Victory();
        }

        public void PlayDefeat()
        {
            StopAction();
            Visual.Defeat();
        }

        // ------------------------------------------------------------------ Damage reactions

        public override void OnHitReceived(DamageInfo info)
        {
            if (info.blocked)
            {
                Visual.Flash(Color.white, 0.4f);
                knockVelocity += info.knockback;
                return;
            }
            Visual.Flash(new Color(1f, 0.2f, 0.2f), 1f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 4f);
            knockVelocity += info.knockback * 1.5f;
            if (Action == ActionKind.Ultimate || Action == ActionKind.Skill || Action == ActionKind.Knockdown) return; // super armour

            Vector3 from = info.source != null ? info.source.Position - Position : -transform.forward;
            bool heavy = info.staggerPower >= 4f && info.amount >= Health.Max * 0.08f;
            if (heavy) StartAction(KnockdownRoutine(from), ActionKind.Knockdown);
            else if (info.staggerPower >= 2.5f && Action != ActionKind.Dodge) StartAction(HitStunRoutine(from, 0.26f), ActionKind.HitStun);
            else Visual.Hit(from);
        }

        IEnumerator HitStunRoutine(Vector3 from, float duration)
        {
            Visual.Hit(from);
            yield return new WaitForSeconds(duration);
        }

        IEnumerator KnockdownRoutine(Vector3 from)
        {
            knockdownAt = Time.time;
            Health.GrantInvulnerability(1.15f); // never juggle the player
            Visual.Knockdown();
            Play("thud", 0.7f);
            VFX.Dust(Position, 8);
            yield return new WaitForSeconds(0.75f);
            Visual.GetUp(0.35f);
            yield return new WaitForSeconds(0.35f);
        }

        public override void OnDealtDamage(DamageInfo info, Combatant target)
        {
            if (info.isUltimate) return;
            UltGauge = Mathf.Min(UltMax, UltGauge + (info.special ? 0.8f : 1.8f));
            if (BattleController.Current != null) BattleController.Current.RegisterHit(info.amount);
        }

        void OnDied()
        {
            StopAction();
            Visual.PlayDeath();
            GameEvents.RaisePlayerMemberDown(this);
        }
    }
}
