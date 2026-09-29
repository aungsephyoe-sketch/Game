using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Specials for every slayer without a hand-made set piece (all monster-folk and the design-test six). Each is
    /// composed from an opener, a main act and a finale, assigned by roster order so no two are the same, coloured
    /// by element and flavoured by species (bats, bones, bandages, foxfire, claw rakes...).
    ///   Openers : Sky Leap, Vanishing Step, Terror Roar, Summoning Circle, Rampage Charge.
    ///   Main    : Element Rain, Pillar Rings, Spiral Storm, Sweeping Beam, Maelstrom Pull, Chain Hunt, Earth Spikes, Phantom Legion.
    ///   Finale  : Meteor Fall, Giant X, Implosion, Sky Launch, Triple Ring.
    /// </summary>
    public partial class PlayerCharacter
    {
        static readonly HashSet<string> NoComposed = new HashSet<string> { "kiba_initiate", "hana_healer" };

        bool UsesComposedSpecial
        {
            get { return !NoComposed.Contains(Def.id) && !SignatureIds.Contains(Def.id) && !NewcomerIds.Contains(Def.id); }
        }

        static int ComposedIndex(CharacterDefinition def)
        {
            int k = 0;
            foreach (var c in GameDatabase.Characters)
            {
                if (c == def) return k;
                if (!NoComposed.Contains(c.id) && !SignatureIds.Contains(c.id) && !NewcomerIds.Contains(c.id)) k++;
            }
            return k;
        }

        IEnumerator ComposedSpecial(AttackTag tag, DamageTally tally, Vector3 focus, Color c)
        {
            int k = ComposedIndex(Def);
            int opener = k % 5, main = k % 8, finale = (k / 8 + k) % 5;
            float fx = RarityFx;
            focus = BattleController.ClampToArena(focus);
            FaceTo(focus);
            yield return CsOpener(opener, tag, tally, focus, c, fx);
            yield return CsMain(main, Share(tag, 0.6f), tally, focus, c, fx);
            yield return CsFinale(finale, Share(tag, 0.5f), tally, focus, c, fx);
            Visual.transform.localPosition = Vector3.zero;
        }

        static AttackTag Share(AttackTag t, float k) { t.multiplier *= k; return t; }

        void Flavor(Vector3 at) { if (Visual != null) Visual.SpeciesBurstAt(at); }

        // ------------------------------------------------------------------ Openers

        IEnumerator CsOpener(int o, AttackTag tag, DamageTally tally, Vector3 focus, Color c, float fx)
        {
            var cam = CameraController.Instance;
            switch (o)
            {
                case 0: // Sky Leap
                {
                    Visual.Spin(0.4f, 2);
                    float t = 0f;
                    while (t < 0.35f) { t += Time.deltaTime; Visual.transform.localPosition = Vector3.up * Mathf.Sin(Mathf.Clamp01(t / 0.35f) * Mathf.PI * 0.5f) * 3.2f; yield return null; }
                    VFX.Breath(Position + Vector3.up * 3f, c, 20);
                    break;
                }
                case 1: // Vanishing Step
                    VFX.Flash(MeshFactory.SmoothSphere(), Position + Vector3.up, Quaternion.identity, Vector3.one * 1.6f, Vector3.one * 0.1f, new Color(c.r, c.g, c.b, 0.7f), 0.2f);
                    yield return new WaitForSeconds(0.12f);
                    MoveTo(BattleController.ClampToArena(focus - transform.forward * 2f));
                    VFX.Flash(MeshFactory.SmoothSphere(), Position + Vector3.up, Quaternion.identity, Vector3.one * 0.1f, Vector3.one * 2f, new Color(c.r, c.g, c.b, 0.7f), 0.25f);
                    if (Audio != null) Audio.PlayPitched("sp_whoosh", 0.8f, 1.3f);
                    break;
                case 2: // Terror Roar
                    for (int i = 0; i < 3; i++)
                    {
                        VFX.Shockwave(Position, (3f + i * 2.5f) * fx, i == 1 ? Color.white : c, 0.35f);
                        if (cam != null) cam.Shake(0.25f);
                        yield return new WaitForSeconds(0.1f);
                    }
                    if (Audio != null) Audio.PlayPitched("smash", 0.8f, 0.6f);
                    tally.Add(CombatSystem.HitRadius(this, Position, 4f * fx, Share(tag, 0.1f)));
                    break;
                case 3: // Summoning Circle
                    VFX.BurstDisc(focus, 6f * fx, c, 0.6f);
                    VFX.Shockwave(focus, 6.5f * fx, Color.Lerp(c, Color.white, 0.5f), 0.6f);
                    VFX.Pillar(focus, c, 6f, 0.5f);
                    if (Audio != null) Audio.Play("sp_activate", 0.7f);
                    yield return new WaitForSeconds(0.3f);
                    break;
                default: // Rampage Charge
                {
                    Vector3 a = Position, b = BattleController.ClampToArena(focus - (focus - a).normalized * 1.5f);
                    var hit = new HashSet<Combatant>();
                    Visual.DashAttack(0.2f);
                    float t = 0f;
                    while (t < 0.2f)
                    {
                        t += Time.deltaTime;
                        MoveTo(Vector3.Lerp(a, b, t / 0.2f));
                        tally.Add(CombatSystem.HitArc(this, Position, transform.forward, 1.8f, 360f, Share(tag, 0.15f), hit));
                        yield return null;
                    }
                    VFX.Dust(Position, 12);
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ Main acts

        IEnumerator CsMain(int m, AttackTag tag, DamageTally tally, Vector3 focus, Color c, float fx)
        {
            switch (m)
            {
                case 0: // Element Rain
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 p = BattleController.ClampToArena(focus + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(0f, 6f * fx));
                        ElementFx.Finisher(p, Vector3.down, Def.element, 1.6f * fx);
                        tally.Add(CombatSystem.HitRadius(this, p, 1.8f, Share(tag, 0.1f)));
                        if (i % 3 == 0) Flavor(p);
                        yield return new WaitForSeconds(0.06f);
                    }
                    break;
                case 1: // Pillar Rings
                    for (int ring = 1; ring <= 3; ring++)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Vector3 p = focus + Quaternion.Euler(0f, i * 60f + ring * 20f, 0f) * Vector3.forward * ring * 2f * fx;
                            VFX.Pillar(p, Color.Lerp(c, Color.white, 0.3f), 5f, 0.4f);
                        }
                        tally.Add(CombatSystem.HitRadius(this, focus, (ring * 2f + 1f) * fx, Share(tag, 0.3f)));
                        Flavor(focus + Vector3.up);
                        if (Audio != null) Audio.PlayPitched("impact", 0.6f, 0.9f + ring * 0.1f);
                        yield return new WaitForSeconds(0.16f);
                    }
                    break;
                case 2: // Spiral Storm
                    Visual.Spin(0.8f, 3);
                    for (int i = 0; i < 8; i++)
                    {
                        ElementFx.Slash(focus + Vector3.up * (0.3f + i * 0.15f), Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward, (5f - i * 0.3f) * fx, 200f, i * 12f, Def.element, 1.6f * fx);
                        tally.Add(CombatSystem.HitRadius(this, focus, 5f * fx, Share(tag, 0.12f)));
                        if (i % 2 == 0) Flavor(focus + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * 2f + Vector3.up);
                        yield return new WaitForSeconds(0.07f);
                    }
                    break;
                case 3: // Sweeping Beam
                {
                    Vector3 f0 = focus - Position;
                    f0.y = 0f;
                    if (f0.sqrMagnitude < 0.01f) f0 = transform.forward;
                    f0.Normalize();
                    for (int i = 0; i <= 8; i++)
                    {
                        Vector3 d = Quaternion.Euler(0f, -60f + i * 15f, 0f) * f0;
                        VFX.Flash(MeshFactory.Line(), Position + Vector3.up * 1.1f, Quaternion.LookRotation(d), new Vector3(1.2f, 1f, 12f), new Vector3(0.2f, 1f, 12f), Color.Lerp(c, Color.white, 0.4f), 0.18f);
                        tally.Add(CombatSystem.HitArc(this, Position, d, 12f, 16f, Share(tag, 0.12f)));
                        if (i % 3 == 0) Flavor(Position + d * 6f + Vector3.up);
                        yield return new WaitForSeconds(0.05f);
                    }
                    break;
                }
                case 4: // Maelstrom Pull
                {
                    var pull = tag;
                    pull.multiplier *= 0.12f;
                    pull.knockback = 0f;
                    for (int i = 0; i < 7; i++)
                    {
                        foreach (var f in Foes(focus, 8f * fx)) PullToward(f, focus, pull, tally);
                        ElementFx.Slash(focus + Vector3.up * 0.4f, Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward, (4.5f - i * 0.4f) * fx, 220f, 10f, Def.element, 1.3f * fx);
                        if (i % 2 == 0) Flavor(focus + Vector3.up);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }
                case 5: // Chain Hunt
                {
                    var foes = Foes(focus, 12f);
                    Vector3 from = Position + Vector3.up;
                    for (int i = 0; i < Mathf.Min(6, foes.Count); i++)
                    {
                        var f = foes[i];
                        if (f == null || !f.IsAlive) continue;
                        BoltFx.Strike(from, f.Position + Vector3.up, Color.Lerp(c, Color.white, 0.4f), 0.3f, 0.2f, 0.3f);
                        tally.Add(CombatSystem.ApplyHit(this, f, Share(tag, 0.22f), from));
                        Flavor(f.Position + Vector3.up);
                        from = f.Position + Vector3.up;
                        if (Audio != null) Audio.PlayPitched(HitSound(), 0.6f, 1f + i * 0.08f);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }
                case 6: // Earth Spikes
                    for (int step = 1; step <= 4; step++)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            Vector3 p = focus + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * step * 1.4f * fx;
                            ElementFx.Finisher(p, Vector3.up, Def.element, 1.2f * fx);
                        }
                        tally.Add(CombatSystem.HitRadius(this, focus, (step * 1.4f + 0.8f) * fx, Share(tag, 0.22f)));
                        Flavor(focus + Vector3.up);
                        yield return new WaitForSeconds(0.09f);
                    }
                    break;
                default: // Phantom Legion
                {
                    var foes = Foes(focus, 10f);
                    int n = Mathf.Max(3, Mathf.Min(6, foes.Count));
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 p = i < foes.Count && foes[i] != null ? foes[i].Position : focus + Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward * 2.5f;
                        Vector3 d = Quaternion.Euler(0f, i * 67f, 0f) * Vector3.forward;
                        VFX.Flash(MeshFactory.SmoothCapsule(), p - d * 1.2f + Vector3.up, Quaternion.identity, new Vector3(0.6f, 1f, 0.6f), new Vector3(0.1f, 1.2f, 0.1f), new Color(c.r, c.g, c.b, 0.6f), 0.25f);
                        ElementFx.Slash(p, d, 2.4f * fx, 160f, i * 30f, Def.element, 1.4f * fx);
                        if (i < foes.Count && foes[i] != null) tally.Add(CombatSystem.ApplyHit(this, foes[i], Share(tag, 0.25f), p - d));
                        else tally.Add(CombatSystem.HitRadius(this, p, 1.6f, Share(tag, 0.15f)));
                        Flavor(p + Vector3.up);
                        Visual.Attack(i % 4, 0.05f);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
                }
            }
        }

        // ------------------------------------------------------------------ Finales

        IEnumerator CsFinale(int f, AttackTag tag, DamageTally tally, Vector3 focus, Color c, float fx)
        {
            var cam = CameraController.Instance;
            var blow = tag;
            blow.launch = true;
            blow.knockback = 10f;
            blow.stagger = 10f;
            switch (f)
            {
                case 0: // Meteor Fall
                    VFX.Flash(MeshFactory.SmoothSphere(), focus + Vector3.up * 8f, Quaternion.identity, Vector3.one * 2f, Vector3.one * 3f, new Color(c.r, c.g, c.b, 0.8f), 0.25f);
                    yield return new WaitForSeconds(0.22f);
                    ElementFx.Finisher(focus, Vector3.down, Def.element, 6f * fx);
                    VFX.Shockwave(focus, 7f * fx, c, 0.5f);
                    VFX.Dust(focus, 20);
                    break;
                case 1: // Giant X
                    ElementFx.Slash(focus, transform.forward, 7f * fx, 60f, 45f, Def.element, 3f * fx);
                    yield return new WaitForSeconds(0.1f);
                    ElementFx.Slash(focus, transform.forward, 7f * fx, 60f, -45f, Def.element, 3f * fx);
                    VFX.HitStar(focus + Vector3.up, c, 4f * fx, 0.25f);
                    break;
                case 2: // Implosion
                    VFX.Flash(MeshFactory.SmoothSphere(), focus + Vector3.up, Quaternion.identity, Vector3.one * 9f * fx, Vector3.one * 0.3f, new Color(c.r, c.g, c.b, 0.5f), 0.3f);
                    yield return new WaitForSeconds(0.28f);
                    VFX.BurstDisc(focus, 6f * fx, Color.white, 0.3f);
                    VFX.HitStar(focus + Vector3.up, c, 3.6f * fx, 0.25f);
                    break;
                case 3: // Sky Launch
                    VFX.Pillar(focus, Color.Lerp(c, Color.white, 0.4f), 16f, 0.8f);
                    blow.knockback = 4f;
                    break;
                default: // Triple Ring
                    for (int i = 0; i < 3; i++)
                    {
                        VFX.Shockwave(focus, (3f + i * 2.5f) * fx, i == 1 ? Color.white : c, 0.4f);
                        yield return new WaitForSeconds(0.08f);
                    }
                    break;
            }
            Flavor(focus + Vector3.up);
            tally.Add(CombatSystem.HitRadius(this, focus, 6f * fx, blow));
            VFX.ImpactLight(focus + Vector3.up * 2f, c, 14f * fx, 0.4f);
            if (cam != null) cam.Shake(0.5f);
            if (Audio != null) Audio.Play("sp_finish", 0.9f);
            yield return new WaitForSeconds(0.2f);
        }
    }
}
