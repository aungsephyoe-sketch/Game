using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Special attacks (the ultimate) are staged as big moments:
    ///   1. the world freezes and the slayer takes a stance,
    ///   2. the screen reacts (cut-in, flash, mood lighting),
    ///   3. the camera moves in close from a character-specific angle,
    ///   4. energy gathers with a rising build-up sound,
    ///   5. a release unique to the character's fighting style,
    ///   6. the demons are thrown back in slow motion,
    ///   7. a final impact flash and the total damage.
    /// </summary>
    public partial class PlayerCharacter
    {
        IEnumerator SpecialSequence()
        {
            var ab = Def.ultimate;
            var color = ElementColor;
            UltGauge = 0f;
            var target = AutoAim(12f);
            Health.PushInvulnerable();
            ultimateActive = true;
            EnemyController.Frozen = true;
            GameEvents.RaiseUltimateStarted(this, ab);
            var cam = CameraController.Instance;
            var audio = Audio;

            // 1-2. Stance, screen reaction.
            if (audio != null) audio.Play("buildup", 1f);
            Visual.SetCharge(1f, color);
            SceneLighting.UltimateMood(color, 3.2f);
            GameEvents.RaiseImpact(0.4f);
            VFX.Shockwave(Position, 2.5f, Color.white, 0.3f);

            // 3. Camera: in close, from an angle that suits the character.
            Vector3 fwd = transform.forward, right = transform.right;
            float side = Def.style == CombatStyle.Heavy ? -1f : 1f;
            float camH = Def.style == CombatStyle.Heavy ? 0.8f : Def.style == CombatStyle.Swift ? 1.6f : 1.3f;
            if (cam != null)
            {
                cam.Cut(Position + fwd * 3.2f + right * 1.4f * side + Vector3.up * camH, Position + Vector3.up * 1.1f);
                cam.Dolly(Position + fwd * 2.2f - right * 1.6f * side + Vector3.up * (camH + 0.4f), Position + Vector3.up * 1.2f, 1.3f);
            }

            // 4. Energy gathers.
            float build = 1.3f, e = 0f, spawn = 0f;
            while (e < build)
            {
                e += Time.unscaledDeltaTime;
                spawn -= Time.unscaledDeltaTime;
                if (spawn <= 0f)
                {
                    spawn = 0.07f;
                    Vector3 o = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2f, 3.5f);
                    VFX.Breath(Position + o, color, 4);
                    GatherFlourish(o, color, e / build);
                }
                Visual.transform.localPosition = Vector3.up * Mathf.Sin(e / build * Mathf.PI * 0.5f) * 0.25f;
                yield return null;
            }
            Visual.transform.localPosition = Vector3.zero;
            Visual.SetCharge(0f, color);

            // 5. Release — unique to the fighting style.
            EnemyController.Frozen = false;
            if (cam != null) { cam.Follow(transform, false); cam.PlayUltimateCinematic(transform, 1.4f); cam.Shake(0.6f); }
            if (audio != null) audio.Play("specialRelease", 1f);
            GameEvents.RaiseImpact(1f);
            var tally = new DamageTally();
            yield return StyleRelease(ab, color, target, tally);
            yield return AbilitySystem.Execute(this, ab, SkillLevelMult(3), true, tally);

            // 6. The demons react: thrown back in slow motion.
            var blow = AttackTag.Basic(0.3f, color);
            blow.isUltimate = true;
            blow.launch = true;
            blow.knockback = 12f;
            blow.stagger = 10f;
            tally.Add(CombatSystem.HitRadius(this, Position, 6.5f, blow));
            TimeController.SlowMotion(0.3f, 0.8f);

            // 7. Final impact.
            VFX.Shockwave(Position, 7f, color, 0.6f);
            VFX.BurstDisc(Position, 5f, Color.white, 0.25f);
            VFX.ImpactLight(Position + Vector3.up * 2f, color, 14f, 0.5f);
            if (cam != null) cam.Shake(0.7f);
            if (audio != null) audio.PlayPitched("impact", 1f, 0.75f);
            GameEvents.RaiseImpact(1f);
            yield return new WaitForSeconds(0.35f);
            FinishUltimateEffects();
            GameEvents.RaiseUltimateFinished(this, tally.total);
        }

        /// <summary>Element-flavoured detail while the energy gathers.</summary>
        void GatherFlourish(Vector3 offset, Color color, float k)
        {
            switch (Element)
            {
                case Element.Thunder:
                    if (Random.value < 0.35f) VFX.Pillar(Position + offset, new Color(1f, 0.95f, 0.5f), 5f, 0.1f);
                    break;
                case Element.Flame:
                    if (Random.value < 0.3f) VFX.Breath(Position + offset * 0.5f, new Color(1f, 0.4f, 0.05f), 6);
                    break;
                case Element.Water:
                    if (Random.value < 0.25f) VFX.Shockwave(Position, 1f + k * 3f, new Color(0.4f, 0.75f, 1f), 0.3f);
                    break;
                case Element.Dark:
                    if (Random.value < 0.25f) VFX.Smoke(Position + offset * 0.6f, new Color(0.25f, 0.05f, 0.3f, 0.7f), 6);
                    break;
                case Element.Light:
                    if (Random.value < 0.3f) VFX.Pillar(Position + offset * 0.4f, new Color(1f, 0.9f, 0.6f), 3f, 0.2f);
                    break;
                default:
                    if (Random.value < 0.3f) VFX.Dust(Position + offset * 0.5f, 4);
                    break;
            }
        }

        IEnumerator StyleRelease(AbilityDefinition ab, Color color, Combatant target, DamageTally tally)
        {
            var tag = AbilitySystem.MakeTag(this, ab, SkillLevelMult(3), true);
            tag.multiplier *= 0.35f;
            if (Def.weapon == WeaponKind.Moon)
            {
                // Crescent moons orbit the slayer, cutting everything they pass.
                for (int i = 0; i < 8; i++)
                {
                    float a = i * 45f;
                    Vector3 p = Position + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 3f;
                    VFX.Slash(p, Quaternion.Euler(0f, a + 90f, 0f) * Vector3.forward, 2.4f, 200f, 60f, color, 0.3f);
                    tally.Add(CombatSystem.HitRadius(this, p, 2f, tag));
                    if (Audio != null) Audio.PlayPitched("slash", 0.5f, 0.8f + i * 0.05f);
                    yield return new WaitForSeconds(0.06f);
                }
                yield break;
            }
            switch (Def.style)
            {
                case CombatStyle.Swift:
                {
                    // Chain of lightning-fast dashes through up to six demons.
                    var targets = new List<Combatant>(CombatSystem.Query(this, Position, Vector3.forward, 12f, 360f));
                    int n = Mathf.Min(6, Mathf.Max(3, targets.Count));
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 to = i < targets.Count && targets[i] != null ? targets[i].Position : Position + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 4f;
                        Vector3 from = Position;
                        Vector3 dir = to - from; dir.y = 0f;
                        if (dir.sqrMagnitude > 0.01f) Face(dir, 1f);
                        transform.position = BattleController.ClampToArena(to + dir.normalized * 1.2f);
                        VFX.Flash(MeshFactory.Line(), from + Vector3.up, Quaternion.LookRotation(dir.sqrMagnitude > 0.01f ? dir : Vector3.forward), new Vector3(1.2f, 1f, dir.magnitude), new Vector3(0.05f, 1f, dir.magnitude), color, 0.3f);
                        VFX.Pillar(to, color, 6f, 0.15f);
                        tally.Add(CombatSystem.HitRadius(this, to, 2f, tag));
                        if (Audio != null) Audio.PlayPitched("dash", 0.6f, 1.2f + i * 0.06f);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }
                case CombatStyle.Heavy:
                {
                    // Three earth-shattering slams, each wave bigger than the last.
                    for (int i = 0; i < 3; i++)
                    {
                        Visual.HeavyAttack(0.18f);
                        yield return new WaitForSeconds(0.15f);
                        float r = 3f + i * 1.6f;
                        VFX.Shockwave(Position, r, color, 0.45f);
                        VFX.Dust(Position, 16);
                        for (int k = 0; k < 6; k++) VFX.Pillar(Position + Quaternion.Euler(0f, k * 60f + i * 20f, 0f) * Vector3.forward * r * 0.8f, new Color(0.55f, 0.45f, 0.35f), 2.5f, 0.3f);
                        var slam = tag; slam.knockback = 9f; slam.heavy = true;
                        tally.Add(CombatSystem.HitRadius(this, Position, r, slam));
                        if (Audio != null) Audio.PlayPitched("bossSlam", 0.9f, 1.1f - i * 0.1f);
                        if (CameraController.Instance != null) CameraController.Instance.Shake(0.4f + i * 0.1f);
                    }
                    break;
                }
                case CombatStyle.Ranged:
                {
                    // A storm of shots rains down across the area.
                    Vector3 c = target != null ? target.Position : Position + transform.forward * 5f;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 p = c + new Vector3(Random.Range(-4f, 4f), 0f, Random.Range(-4f, 4f));
                        VFX.Pillar(p, color, 9f, 0.18f);
                        VFX.BurstDisc(p, 1.4f, color, 0.2f);
                        tally.Add(CombatSystem.HitRadius(this, p, 1.8f, tag));
                        if (Audio != null) Audio.PlayPitched("shoot", 0.5f, 0.9f + Random.value * 0.4f);
                        yield return new WaitForSeconds(0.05f);
                    }
                    if (Def.role == Role.Support)
                    {
                        var b = BattleController.Current;
                        if (b != null) foreach (var m in b.Team.Members) if (m != null && m.IsAlive) m.Health.Heal(m.Health.Max * 0.15f);
                        VFX.Pillar(Position, new Color(0.5f, 1f, 0.6f), 8f, 0.8f);
                    }
                    break;
                }
                case CombatStyle.Technical:
                {
                    // A spiral of cuts that rises around the slayer like a dragon.
                    for (int i = 0; i < 10; i++)
                    {
                        float a = i * 36f;
                        Vector3 dir = Quaternion.Euler(0f, a, 0f) * Vector3.forward;
                        VFX.Slash(Position + Vector3.up * (i * 0.15f), dir, 3.4f, 170f, i * 18f, color, 0.25f);
                        Visual.Swing(a - 90f, a + 90f, 0.06f, 20f);
                        tally.Add(CombatSystem.HitArc(this, Position, dir, 3.6f, 90f, tag));
                        if (Audio != null) Audio.PlayPitched("slash", 0.5f, 0.9f + i * 0.05f);
                        yield return new WaitForSeconds(0.05f);
                    }
                    VFX.Pillar(Position, color, 12f, 0.6f);
                    break;
                }
                default:
                {
                    // Balanced: a great elemental wave rolls forward.
                    for (int i = -1; i <= 1; i++)
                        WaveProjectile.Launch(this, Position, Quaternion.Euler(0f, i * 20f, 0f) * transform.forward, 18f, 14f, 2.4f, tag, tally);
                    for (int k = 1; k <= 4; k++) VFX.Pillar(Position + transform.forward * k * 2.5f, color, 5f + k, 0.4f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                }
            }
        }
    }
}
