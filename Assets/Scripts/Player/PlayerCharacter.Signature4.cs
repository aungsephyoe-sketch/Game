using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Specials for the premium design test, each matched to how the fighter moves:
    ///   Kaito — Crosswind Cyclone: leaps up, a green cyclone drags demons in, then he drops through it with an X-cut.
    ///   Oboro — Sunbreaker: raises the club, leaps and slams; the ground splits into rivers of light that erupt outward.
    ///   Shion — Heavenly Thunder Array: charms fly to six points, lightning strikes each, then the sky strikes the centre.
    /// </summary>
    public partial class PlayerCharacter
    {
        IEnumerator CrosswindCyclone(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var wind = new Color(0.55f, 1f, 0.6f);
            FaceTo(focus);
            Vector3 center = BattleController.ClampToArena(focus);
            if (Audio != null) Audio.Play("el_wind", 1f);
            // Hop up while a cyclone forms over the target.
            float t = 0f;
            while (t < 0.25f)
            {
                t += Time.deltaTime;
                Visual.transform.localPosition = Vector3.up * Mathf.Sin(Mathf.Clamp01(t / 0.25f) * Mathf.PI * 0.5f) * 3f;
                yield return null;
            }
            Visual.Spin(0.5f, 2);
            var pull = tag;
            pull.multiplier *= 0.35f;
            pull.knockback = 0f;
            pull.hitStop = 0.01f;
            for (int wave = 0; wave < 6; wave++)
            {
                float r = (4.5f - wave * 0.5f) * fx;
                for (int i = 0; i < 3; i++)
                    VFX.Slash(center + Vector3.up * (0.4f + wave * 0.45f), Quaternion.Euler(0f, wave * 50f + i * 120f, 0f) * Vector3.forward, r, 200f, 8f + wave * 4f, wind, 0.22f);
                foreach (var f in Foes(center, 6.5f * fx)) PullToward(f, center, pull, tally);
                if (Audio != null) Audio.PlayPitched("whoosh", 0.6f, 1f + wave * 0.08f);
                yield return new WaitForSeconds(0.08f);
            }
            // Drop through the eye of the storm with a crossing cut.
            transform.position = center - transform.forward * 0.6f;
            t = 0f;
            while (t < 0.12f)
            {
                t += Time.deltaTime;
                Visual.transform.localPosition = Vector3.up * Mathf.Lerp(3f, 0f, t / 0.12f);
                yield return null;
            }
            Visual.transform.localPosition = Vector3.zero;
            Visual.Attack(4, 0.1f);
            var x = tag;
            x.multiplier *= 2.2f;
            x.heavy = true;
            x.launch = true;
            tally.Add(CombatSystem.HitRadius(this, center, 4.5f * fx, x));
            for (int s = -1; s <= 1; s += 2)
                VFX.Slash(center + Vector3.up, transform.forward, 4.5f * fx, 150f, 45f * s, Color.Lerp(wind, Color.white, 0.4f), 0.3f);
            VFX.Shockwave(center, 6f * fx, wind, 0.45f);
            ElementFx.Finisher(center, transform.forward, Element.Beast, 6f * fx);
            if (Audio != null) Audio.Play("impact", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.5f);
        }

        IEnumerator Sunbreaker(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var ember = new Color(1f, 0.5f, 0.12f);
            FaceTo(focus);
            // Wind up: the club rises overhead and the ground smoulders.
            Visual.SetCharge(1f, ember);
            VFX.Pillar(Position, ember, 3f, 0.5f);
            if (Audio != null) Audio.PlayPitched("charge", 0.8f, 0.8f);
            yield return new WaitForSeconds(0.35f);
            Visual.SetCharge(0f, ember);
            Vector3 land = BattleController.ClampToArena(Position + DirTo(focus) * Mathf.Min(5f, Vector3.Distance(Position, focus)));
            Vector3 start = Position;
            float t = 0f;
            while (t < 0.3f)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / 0.3f);
                transform.position = Vector3.Lerp(start, land, k);
                Visual.transform.localPosition = Vector3.up * Mathf.Sin(k * Mathf.PI) * 2.5f;
                yield return null;
            }
            Visual.transform.localPosition = Vector3.zero;
            Visual.HeavyAttack(0.12f);
            var slam = tag;
            slam.multiplier *= 1.6f;
            slam.heavy = true;
            slam.launch = true;
            tally.Add(CombatSystem.HitRadius(this, land, 4f * fx, slam));
            VFX.Shockwave(land, 5f * fx, ember, 0.4f);
            VFX.Dust(land, 30);
            if (Audio != null) Audio.Play("bossSlam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.8f);
            // Six fissures race outward, erupting along their length.
            var erupt = tag;
            erupt.multiplier *= 0.6f;
            erupt.launch = true;
            var hitOnce = new HashSet<Combatant>();
            for (int step = 1; step <= 4; step++)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3 dir = Quaternion.Euler(0f, i * 60f + 15f, 0f) * transform.forward;
                    Vector3 p = land + dir * step * 1.8f * fx;
                    VFX.Flash(MeshFactory.Line(), land + Vector3.up * 0.05f, Quaternion.LookRotation(dir), new Vector3(0.4f, 1f, step * 1.8f * fx), new Vector3(0.1f, 1f, step * 1.8f * fx), ember, 0.6f);
                    VFX.Pillar(p, ember, 2.5f + step * 0.6f, 0.35f);
                    foreach (var f in Foes(p, 1.8f))
                        if (f != null && f.IsAlive && hitOnce.Add(f)) tally.Add(CombatSystem.ApplyHit(this, f, erupt, land));
                }
                if (Audio != null) Audio.PlayPitched("slam", 0.7f, 0.9f + step * 0.08f);
                yield return new WaitForSeconds(0.1f);
            }
            ElementFx.Finisher(land, transform.forward, Element.Flame, 7f * fx);
        }

        IEnumerator HeavenlyThunderArray(AttackTag tag, DamageTally tally, Vector3 focus)
        {
            float fx = RarityFx;
            var bolt = new Color(1f, 0.9f, 0.45f);
            var violet = new Color(0.7f, 0.45f, 1f);
            FaceTo(focus);
            Vector3 center = BattleController.ClampToArena(focus);
            if (Audio != null) Audio.Play("el_thunder", 1f);
            // Six charms fly out to the points of the seal.
            var charms = new List<Transform>();
            var paper = MaterialFactory.Toon(new Color(0.97f, 0.94f, 0.82f), 0.006f, new Color(0.3f, 0.25f, 0.4f));
            var pts = new Vector3[6];
            for (int i = 0; i < 6; i++)
            {
                pts[i] = center + Quaternion.Euler(0f, i * 60f, 0f) * Vector3.forward * 4.5f * fx;
                var c = new GameObject("Charm").transform;
                MeshFactory.Primitive(PrimitiveType.Cube, c, Vector3.zero, new Vector3(0.35f, 0.9f, 0.03f), paper);
                c.position = Position + Vector3.up * 1.5f;
                charms.Add(c);
            }
            float t = 0f;
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 0.4f);
                for (int i = 0; i < 6; i++)
                {
                    charms[i].position = Vector3.Lerp(Position + Vector3.up * 1.5f, pts[i] + Vector3.up * 1.2f, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.5f;
                    charms[i].rotation = Quaternion.Euler(0f, t * 720f + i * 60f, 0f);
                }
                yield return null;
            }
            // The seal lights up: lines between the points, then lightning on each point.
            for (int i = 0; i < 6; i++)
            {
                Vector3 a = pts[i], b = pts[(i + 2) % 6];
                VFX.Flash(MeshFactory.Line(), a + Vector3.up * 0.05f, Quaternion.LookRotation(b - a), new Vector3(0.3f, 1f, (b - a).magnitude), new Vector3(0.12f, 1f, (b - a).magnitude), violet, 1.2f);
            }
            VFX.Shockwave(center, 5f * fx, violet, 0.6f);
            var strike = tag;
            strike.multiplier *= 0.55f;
            for (int i = 0; i < 6; i++)
            {
                BoltFx.Strike(pts[i] + Vector3.up * 14f, pts[i], bolt, 0.3f, 0.3f, 0.45f);
                VFX.Pillar(pts[i], bolt, 5f, 0.3f);
                tally.Add(CombatSystem.HitRadius(this, pts[i], 2.4f * fx, strike));
                Destroy(charms[i].gameObject);
                if (Audio != null) Audio.PlayPitched("impact", 0.6f, 1.2f + i * 0.05f);
                yield return new WaitForSeconds(0.07f);
            }
            // Then the heavens strike the centre.
            yield return new WaitForSeconds(0.15f);
            for (int i = 0; i < 4; i++) BoltFx.Strike(center + Vector3.up * 18f + Random.insideUnitSphere * 1.5f, center, Color.Lerp(bolt, Color.white, 0.4f), 0.7f, 0.4f, 0.5f);
            var big = tag;
            big.multiplier *= 2.1f;
            big.heavy = true;
            big.launch = true;
            tally.Add(CombatSystem.HitRadius(this, center, 5.5f * fx, big));
            VFX.ImpactLight(center + Vector3.up * 4f, bolt, 20f, 0.35f);
            ElementFx.Finisher(center, transform.forward, Element.Thunder, 7f * fx);
            if (Audio != null) Audio.Play("bossSlam", 1f);
            if (CameraController.Instance != null) CameraController.Instance.Shake(0.7f);
        }
    }
}
