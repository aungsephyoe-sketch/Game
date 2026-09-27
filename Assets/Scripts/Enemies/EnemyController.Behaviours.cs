using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The behaviours that give each demon its own threat: ambushers hide as a shadow puddle with two
    /// glowing eyes, summoners call smaller demons to their side, chargers rush across the field,
    /// and every regular demon enrages when badly hurt (faster, shorter wind-ups, red glow).
    /// </summary>
    public partial class EnemyController
    {
        const int MaxAddsPerSummoner = 2;

        bool enraged;
        float enrageFxTimer;
        float summonTimer = 6f;
        float chargeTimer = 3f;
        readonly List<EnemyController> adds = new List<EnemyController>();

        public bool IsEnraged { get { return enraged; } }

        void UpdateBehaviours(float dt)
        {
            if (IsBoss || state == State.Spawning) return;
            summonTimer -= dt;
            chargeTimer -= dt;

            if (!enraged && Def.enrageAt > 0f && Health.Normalized > 0f && Health.Normalized < Def.enrageAt) Enrage();
            if (enraged)
            {
                enrageFxTimer -= dt;
                if (enrageFxTimer <= 0f)
                {
                    enrageFxTimer = 0.5f;
                    VFX.Breath(Position + Vector3.up * (1.2f * Def.scale), new Color(1f, 0.15f, 0.1f), 3);
                }
            }
        }

        /// <summary>Changes its attack pattern when damaged: a roar, a red flash, then faster and meaner.</summary>
        void Enrage()
        {
            enraged = true;
            speedMultiplier *= 1.3f;
            windupMultiplier *= 0.72f;
            visual.Flash(new Color(1f, 0.1f, 0.05f), 1f);
            VFX.Shockwave(Position, 2.5f * Def.scale, new Color(1f, 0.2f, 0.1f), 0.4f);
            VFX.Breath(Position + Vector3.up, new Color(1f, 0.2f, 0.1f), 20);
            DamageNumbers.SpawnText(Position + Vector3.up * (2.6f * Def.scale), "ENRAGED", new Color(1f, 0.25f, 0.2f), 44f);
            Voice(0.8f);
        }

        /// <summary>Summon and charge, tried before the normal approach. Returns true when an action began.</summary>
        bool TrySpecialBehaviour(PlayerCharacter player, Vector3 to, float dist)
        {
            if (!string.IsNullOrEmpty(Def.summonId) && summonTimer <= 0f && attackTimer <= 0f)
            {
                adds.RemoveAll(a => a == null || !a.IsAlive);
                summonTimer = Random.Range(9f, 13f);
                if (adds.Count < MaxAddsPerSummoner && TryTakeToken())
                {
                    BeginAttack(SummonRoutine());
                    return true;
                }
            }
            if (Def.charger && chargeTimer <= 0f && attackTimer <= 0f && dist > 5f && dist < 13f)
            {
                chargeTimer = Random.Range(6f, 9f);
                if (TryTakeToken())
                {
                    BeginAttack(ChargeRoutine(player));
                    return true;
                }
            }
            return false;
        }

        IEnumerator SummonRoutine()
        {
            var b = BattleController.Current;
            float wind = Windup(1f);
            Track(Telegraph.Circle(Position, 2.5f, wind));
            visual.Flash(Def.accentColor, 0.6f);
            visual.Swing(0f, 0f, wind, -30f);
            Voice(0.7f);
            VFX.Smoke(Position, new Color(0.15f, 0.05f, 0.2f, 0.8f), 20);
            yield return new WaitForSeconds(wind);
            ClearTelegraphs();
            if (b == null || b.Mission == null) yield break;
            for (int i = 0; i < 2; i++)
            {
                float a = (transform.eulerAngles.y + (i == 0 ? -70f : 70f)) * Mathf.Deg2Rad;
                Vector3 p = BattleController.ClampToArena(Position + new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * 2.4f);
                VFX.Pillar(p, Def.accentColor, 4f, 0.5f);
                var e = b.Mission.SpawnEnemy(Def.summonId, p, false);
                if (e != null) adds.Add(e);
                yield return new WaitForSeconds(0.2f);
            }
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("charge", 0.5f);
            yield return new WaitForSeconds(0.5f);
        }

        /// <summary>A long straight-line rush: telegraphed, dodgeable, and it stumbles at the end.</summary>
        IEnumerator ChargeRoutine(PlayerCharacter target)
        {
            if (target == null) yield break;
            FaceTowards(target.Position - Position, 1f);
            Vector3 fwd = target.Position - Position;
            fwd.y = 0f;
            float dist = Mathf.Min(fwd.magnitude + 3f, 14f);
            fwd.Normalize();
            float wind = Windup(0.95f);
            Track(Telegraph.Line(Position, fwd, 1.8f * Def.scale, dist, wind));
            visual.Flash(Def.accentColor, 0.6f);
            // Paws the ground, snorting dust.
            float t = 0f;
            while (t < wind)
            {
                t += Time.deltaTime;
                if (Random.value < 0.15f) VFX.Dust(Position - fwd * 0.6f, 2);
                yield return null;
            }
            ClearTelegraphs();
            Voice(0.9f);
            if (GameManager.Instance != null) GameManager.Instance.Audio.PlayVaried("whoosh", 0.7f, 0.1f);
            Vector3 start = Position;
            var hit = new HashSet<Combatant>();
            const float speed = 17f;
            float dur = dist / speed;
            t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                transform.position = BattleController.ClampToArena(start + fwd * dist * Mathf.Clamp01(t / dur));
                if (Random.value < 0.5f) VFX.Dust(Position, 2);
                CombatSystem.HitRadius(this, Position, Radius + 0.7f, EnemyTag(1.5f, 9f, 5f), hit);
                yield return null;
            }
            VFX.Smoke(Position, new Color(0.5f, 0.4f, 0.3f, 0.6f), 12);
            if (GameManager.Instance != null) GameManager.Instance.Audio.PlayPitched("slam", 0.6f, 0.8f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.15f);
            // The stumble: an opening to punish.
            visual.Hit(fwd);
            yield return new WaitForSeconds(1f);
        }

        /// <summary>Hidden in the ground as a dark puddle with two eyes until the player comes close.</summary>
        IEnumerator AmbushRoutine()
        {
            Health.PushInvulnerable();
            All.Remove(this);
            var renderers = GetComponentsInChildren<Renderer>();
            foreach (var r in renderers) r.enabled = false;
            var lurk = new GameObject("Lurk");
            lurk.transform.SetParent(transform, false);
            var puddle = MaterialFactory.Transparent(new Color(0.02f, 0f, 0.04f, 0.85f));
            MeshFactory.MeshObject(MeshFactory.PlanarDisc(), lurk.transform, Vector3.up * 0.03f, new Vector3(1.3f, 1f, 1f) * Def.scale, puddle, false);
            var eye = MaterialFactory.Additive(new Color(Def.accentColor.r, Def.accentColor.g, Def.accentColor.b, 1f));
            for (int i = -1; i <= 1; i += 2)
                MeshFactory.Primitive(PrimitiveType.Sphere, lurk.transform, new Vector3(i * 0.18f, 0.07f, 0.15f) * Def.scale, new Vector3(0.12f, 0.05f, 0.08f) * Def.scale, eye);

            float waited = 0f;
            while (waited < 14f)
            {
                var p = Player;
                if (p != null && (p.Position - Position).sqrMagnitude < 5.5f * 5.5f) break;
                waited += Time.deltaTime;
                // An occasional blink so the watchful player can spot it.
                lurk.SetActive(Mathf.Repeat(Time.time + zigzagPhase, 3f) > 0.12f);
                yield return null;
            }

            Destroy(lurk);
            foreach (var r in renderers) if (r != null) r.enabled = true;
            if (!All.Contains(this)) All.Add(this);
            Health.PopInvulnerable();
            VFX.Smoke(Position, new Color(0.05f, 0.02f, 0.08f, 0.9f), 26);
            VFX.Shockwave(Position, 2.2f, Def.accentColor, 0.3f);
            Voice(1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.12f);
            DamageNumbers.SpawnText(Position + Vector3.up * (2.6f * Def.scale), "AMBUSH!", new Color(1f, 0.35f, 0.3f), 50f);
            alerted = true;
            // Burst up out of the ground, then strike almost at once.
            Vector3 end = transform.position;
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                float k = t / 0.25f;
                transform.position = end + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.4f;
                yield return null;
            }
            transform.position = end;
            attackTimer = 0.15f;
            if (state == State.Spawning) state = State.Chase;
        }
    }
}
