using System.Collections;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// One Demon Slayer on the field: movement, 5-hit combo, charged attack, dodge with i-frames
    /// (and perfect-dodge slow motion), three breathing forms and a cinematic ultimate.
    /// </summary>
    public class PlayerCharacter : Combatant
    {
        public enum ActionKind { None, Attack, Charged, Skill, Dodge, Ultimate, SwitchIn }

        public const float UltMax = 100f;
        const float ChargeStart = 0.3f;
        const float ChargeFull = 0.8f;

        public CharacterDefinition Def { get; private set; }
        public OwnedCharacter Owned { get; private set; }
        public CharacterVisual Visual { get; private set; }
        public readonly float[] Cooldowns = new float[3];
        public float UltGauge;
        public ActionKind Action { get; private set; }
        public bool UltReady { get { return UltGauge >= UltMax; } }
        public float ChargeAmount { get; private set; }
        public int ComboStep { get { return comboIndex; } }
        public bool CanSwitchOut { get { return Action != ActionKind.Ultimate && IsAlive; } }

        Coroutine actionRoutine;
        int comboIndex;
        float comboResetTimer;
        bool attackQueued;
        float attackHeldTime;
        bool attackHeldLast;
        float dodgeCooldown;
        float lastPerfectDodge = -10f;
        Vector3 knockVelocity;
        Vector3 moveInput;

        Color ElementColor { get { return ElementChart.ColorOf(Def.element); } }

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
            Visual = CharacterVisual.BuildHero(def, transform);
        }

        float SkillLevelMult(int index) { return CharacterSystem.SkillLevelMultiplier(Owned.skillLevels[index]); }

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

            if (Action != ActionKind.None)
            {
                comboResetTimer = 0.7f;
            }
            else if (comboResetTimer > 0f)
            {
                comboResetTimer -= dt;
                if (comboResetTimer <= 0f) comboIndex = 0;
            }

            // Dodge cancels everything except the ultimate – responsiveness first.
            if (input.dodgeDown && dodgeCooldown <= 0f && Action != ActionKind.Ultimate && Action != ActionKind.Dodge)
            {
                StartAction(DodgeRoutine(), ActionKind.Dodge);
                return;
            }

            if (input.ultimateDown && UltReady && Action != ActionKind.Ultimate && Action != ActionKind.Dodge)
            {
                StartAction(UltimateRoutine(), ActionKind.Ultimate);
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                if (!input.SkillDown(i) || Cooldowns[i] > 0f) continue;
                if (Action == ActionKind.None || Action == ActionKind.Attack || Action == ActionKind.SwitchIn)
                {
                    StartAction(SkillRoutine(i), ActionKind.Skill);
                    return;
                }
            }

            // Tap = combo, hold = charged attack.
            if (input.attackDown)
            {
                attackHeldTime = 0f;
                if (Action == ActionKind.None) StartAction(ComboRoutine(), ActionKind.Attack);
                else if (Action == ActionKind.Attack) attackQueued = true;
            }
            bool held = input.attackHeld;
            if (held) attackHeldTime += dt;
            ChargeAmount = held && Action == ActionKind.None ? Mathf.Clamp01((attackHeldTime - ChargeStart) / (ChargeFull - ChargeStart)) : 0f;
            if (!held && attackHeldLast && Action == ActionKind.None && attackHeldTime >= ChargeFull)
            {
                attackHeldTime = 0f;
                StartAction(ChargedRoutine(), ActionKind.Charged);
            }
            attackHeldLast = held;
            Visual.SetCharge(ChargeAmount, ElementColor);

            if (Action == ActionKind.None)
            {
                float speed = Stats.speed * (ChargeAmount > 0f ? 0.4f : 1f);
                if (moveInput.sqrMagnitude > 0.01f)
                {
                    transform.position = BattleController.ClampToArena(transform.position + moveInput * speed * dt);
                    Face(moveInput, 18f * dt);
                }
                Visual.SetMoving(moveInput.magnitude);
            }
            else
            {
                Visual.SetMoving(0f);
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

        /// <summary>Soft aim assist: faces the nearest demon, preferring the stick direction.</summary>
        Combatant AutoAim(float range)
        {
            Combatant best = null;
            float bestScore = float.MaxValue;
            Vector3 stick = moveInput.sqrMagnitude > 0.05f ? moveInput.normalized : Vector3.zero;
            foreach (var c in Combatant.All)
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
            Action = kind;
            actionRoutine = StartCoroutine(Wrap(routine));
        }

        IEnumerator Wrap(IEnumerator routine)
        {
            yield return routine;
            Action = ActionKind.None;
            actionRoutine = null;
        }

        void StopAction()
        {
            if (actionRoutine != null) StopCoroutine(actionRoutine);
            actionRoutine = null;
            if (Action == ActionKind.Ultimate) FinishUltimateEffects();
            Action = ActionKind.None;
            attackQueued = false;
        }

        // ------------------------------------------------------------------ Normal attacks

        static readonly float[] SwingFrom = { -110f, 100f, -40f, 120f, -150f };
        static readonly float[] SwingTo = { 100f, -110f, 60f, -120f, 150f };
        static readonly float[] SwingPitch = { 20f, 10f, 70f, 5f, 15f };
        static readonly float[] SlashRoll = { 10f, -15f, 70f, -5f, 0f };

        IEnumerator ComboRoutine()
        {
            while (true)
            {
                attackQueued = false;
                int step = comboIndex;
                bool finisher = step >= Def.comboMultipliers.Length - 1;
                float spd = Def.attackSpeed;
                var target = AutoAim(5.5f);

                // Lunge toward the target so combos connect.
                float lunge = 0.35f;
                if (target != null)
                {
                    float d = Vector3.Distance(target.Position, Position) - target.Radius - 1.2f;
                    lunge = Mathf.Clamp(d, 0f, 1.6f);
                }
                Visual.Swing(SwingFrom[step % 5], SwingTo[step % 5], 0.12f / spd, SwingPitch[step % 5]);
                if (finisher) Visual.Spin(0.18f);
                float t = 0f;
                Vector3 start = Position;
                while (t < 0.08f / spd)
                {
                    t += Time.deltaTime;
                    transform.position = BattleController.ClampToArena(start + transform.forward * lunge * Mathf.Clamp01(t / (0.08f / spd)));
                    yield return null;
                }

                var tag = AttackTag.Basic(Def.comboMultipliers[step], ElementColor);
                float range = 2.6f, arc = 160f;
                if (finisher)
                {
                    tag.knockback = 5f;
                    tag.stagger = 3f;
                    tag.hitStop = 0.07f;
                    tag.shake = 0.3f;
                    range = 3.2f;
                    arc = 360f;
                }
                CombatSystem.HitArc(this, Position, transform.forward, range, arc, tag);
                VFX.Slash(Position, transform.forward, range, finisher ? 330f : 150f, SlashRoll[step % 5], ElementColor, 0.2f);
                if (finisher) VFX.Shockwave(Position, range, ElementColor, 0.3f);
                if (GameManager.Instance != null) GameManager.Instance.Audio.Play("slash", 0.5f);

                comboIndex = finisher ? 0 : step + 1;

                float recovery = (finisher ? 0.42f : 0.22f) / spd;
                float r = 0f;
                while (r < recovery)
                {
                    r += Time.deltaTime;
                    // Queued input chains immediately once the swing is committed – snappy combos.
                    if (attackQueued && r > 0.08f / spd && !finisher) break;
                    yield return null;
                }
                if (!attackQueued) yield break;
            }
        }

        IEnumerator ChargedRoutine()
        {
            AutoAim(6f);
            Visual.SetCharge(0f, ElementColor);
            Visual.Swing(-160f, 160f, 0.16f, 25f);
            yield return new WaitForSeconds(0.06f);
            var tag = AttackTag.Basic(Def.chargedMultiplier, ElementColor);
            tag.knockback = 7f;
            tag.stagger = 6f;
            tag.hitStop = 0.09f;
            tag.shake = 0.4f;
            CombatSystem.HitArc(this, Position, transform.forward, 4.2f, 200f, tag);
            VFX.Slash(Position, transform.forward, 4.2f, 200f, 0f, ElementColor, 0.3f);
            VFX.Breath(Position + transform.forward * 2f, ElementColor, 30);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("heavy", 0.8f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 3f);
            yield return new WaitForSeconds(0.35f);
            comboIndex = 0;
        }

        // ------------------------------------------------------------------ Dodge

        IEnumerator DodgeRoutine()
        {
            Vector3 dir = moveInput.sqrMagnitude > 0.05f ? moveInput.normalized : -transform.forward;
            dodgeCooldown = 0.45f;
            const float duration = 0.2f;
            Health.GrantInvulnerability(0.32f);
            VFX.Smoke(Position, new Color(0.9f, 0.9f, 1f, 0.5f), 8);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("dodge", 0.6f);
            Vector3 start = Position;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f);
                transform.position = BattleController.ClampToArena(start + dir * 4.2f * k);
                yield return null;
            }
            if (moveInput.sqrMagnitude > 0.05f) Face(moveInput, 1f);
            yield return new WaitForSeconds(0.05f);
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
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("perfect", 0.8f);
            GameEvents.RaisePerfectDodge();
        }

        // ------------------------------------------------------------------ Skills & ultimate

        IEnumerator SkillRoutine(int index)
        {
            var ab = Def.skills[index];
            Cooldowns[index] = ab.cooldown;
            AutoAim(ab.shape == AbilityShape.Wave || ab.shape == AbilityShape.Dash ? ab.range + 2f : 7f);
            GameEvents.RaiseSkillUsed(this, ab);
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("skill", 0.7f);
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
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("ultimate", 1f);
            if (CameraController.Instance != null) CameraController.Instance.SetZoom(0.45f, 8f);
            TimeController.SlowMotion(0.12f, 0.9f);
            VFX.Breath(Position, ElementColor, 80);
            Visual.SetCharge(1f, ElementColor);
            yield return new WaitForSecondsRealtime(0.9f);

            Visual.SetCharge(0f, ElementColor);
            if (CameraController.Instance != null)
            {
                CameraController.Instance.SetZoom(1.15f, 3f);
                CameraController.Instance.Shake(0.6f);
            }
            var tally = new DamageTally();
            yield return AbilitySystem.Execute(this, ab, SkillLevelMult(3), true, tally);
            yield return new WaitForSeconds(0.2f);
            FinishUltimateEffects();
            GameEvents.RaiseUltimateFinished(this, tally.total);
        }

        bool ultimateActive;

        void FinishUltimateEffects()
        {
            if (!ultimateActive) return;
            ultimateActive = false;
            Health.PopInvulnerable();
            Visual.SetCharge(0f, ElementColor);
            if (CameraController.Instance != null) CameraController.Instance.SetZoom(1f, 3f);
        }

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
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("switch", 0.7f);
            yield return new WaitForSeconds(0.25f);
        }

        public void OnSwitchOut()
        {
            StopAction();
            ChargeAmount = 0f;
            attackHeldLast = false;
            comboIndex = 0;
            knockVelocity = Vector3.zero;
            Visual.SetCharge(0f, ElementColor);
            Visual.ResetPose();
        }

        // ------------------------------------------------------------------ Damage

        public override void OnHitReceived(DamageInfo info)
        {
            Visual.Flash(new Color(1f, 0.2f, 0.2f), 1f);
            UltGauge = Mathf.Min(UltMax, UltGauge + 4f);
            knockVelocity += info.knockback * 1.5f;
            // Heavy hits interrupt normal attacks and charging (never dodges/skills/ultimates).
            if (info.staggerPower >= 3f && (Action == ActionKind.Attack || Action == ActionKind.None))
            {
                StopAction();
                attackHeldTime = 0f;
            }
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
