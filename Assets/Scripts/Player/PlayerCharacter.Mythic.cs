using System.Collections;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Mythic slayers fight like the headliners they are: their strong attack becomes a short cinematic (the camera
    /// swings in, time slows while the element gathers, the blow lands, then three rolling aftershocks tear outward)
    /// and their special gets an encore (a volley of element strikes on every demon nearby before the finale fades).
    /// </summary>
    public partial class PlayerCharacter
    {
        IEnumerator MythicPrelude()
        {
            Color c = ElementColor;
            var cam = CameraController.Instance;
            Health.GrantInvulnerability(0.9f);
            if (cam != null) { cam.PlayUltimateCinematic(transform, 1.6f, 4.5f); cam.SetZoom(0.82f, 6f); }
            TimeController.SlowMotion(0.45f, 0.55f);
            SceneLighting.UltimateMood(c, 1.8f);
            if (Audio != null) { Audio.Play("myth_heart", 0.9f); Audio.Play("myth_rise", 0.8f); Audio.PlayPitched("sp_activate", 0.5f, 1.15f); }
            // A rune circle under the slayer and the rarity colours rippling out.
            VFX.BurstDisc(Position, 3.5f, Color.Lerp(c, Color.white, 0.3f), 0.6f);
            for (int tier = 2; tier <= 6; tier++) VFX.Shockwave(Position, 1.5f + tier * 0.6f, RarityInfo.Color(tier), 0.4f + tier * 0.05f);
            float t = 0f;
            const float dur = 0.55f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = t / dur;
                Visual.SetCharge(k, c);
                // Element spiralling in.
                float a = t * 900f;
                Vector3 o = Quaternion.Euler(0f, a, 0f) * Vector3.forward * Mathf.Lerp(3f, 0.6f, k);
                VFX.Breath(Position + o, c, 3);
                if (Random.value < 0.25f) VFX.HitStar(Position + Vector3.up * Random.Range(0.6f, 2.2f) + Random.insideUnitSphere * 0.8f, c, 0.4f, 0.1f);
                yield return null;
            }
            // Lightning converges on the slayer as the power locks in.
            for (int i = 0; i < 5; i++)
            {
                Vector3 o = Position + Quaternion.Euler(0f, i * 72f, 0f) * Vector3.forward * 3.5f;
                BoltFx.Strike(o + Vector3.up * 9f, Position + Vector3.up * 1.2f, Color.Lerp(c, Color.white, 0.5f), 0.25f, 0.2f, 0.4f);
            }
            if (Audio != null) Audio.Play("myth_whoosh", 0.9f);
            VFX.Pillar(Position, Color.Lerp(c, Color.white, 0.4f), 9f, 0.4f);
            VFX.HitStar(Position + Vector3.up * 1.4f, c, 2.2f, 0.18f);
            Visual.SetCharge(0f, c);
        }

        IEnumerator MythicAftershock()
        {
            Color c = ElementColor;
            var cam = CameraController.Instance;
            Vector3 fwd = transform.forward;
            // The blow lands like a headliner: a held beat of slow motion, the boom, sky bolts ringing the impact.
            Vector3 hitAt = moveImpact;
            TimeController.SlowMotion(0.25f, 0.4f);
            if (Audio != null) Audio.Play("myth_boom", 1f);
            if (cam != null) cam.Punch(1f, 0.3f);
            VFX.HitStar(hitAt + Vector3.up * 1.4f, c, 3.4f, 0.25f);
            for (int i = 0; i < 6; i++)
            {
                Vector3 o = hitAt + Quaternion.Euler(0f, i * 60f + 15f, 0f) * Vector3.forward * 3f;
                BoltFx.Strike(o + Vector3.up * 14f, o, Color.Lerp(c, Color.white, 0.5f), 0.35f, 0.22f, 0.45f);
            }
            for (int tier = 2; tier <= 6; tier++) VFX.Shockwave(hitAt, 2f + tier * 1.3f, RarityInfo.Color(tier), 0.45f + tier * 0.06f);
            DamageNumbers.SpawnText(hitAt + Vector3.up * 3.4f, "MYTHIC", Color.Lerp(RarityInfo.Color(6), Color.white, 0.3f), 64f);
            yield return new WaitForSeconds(0.18f);
            for (int wave = 0; wave < 3; wave++)
            {
                float r = 3.5f + wave * 2f;
                Vector3 at = Position + fwd * (1.2f + wave * 0.8f);
                var tag = AttackTag.Basic(1.1f + wave * 0.3f, c);
                tag.heavy = true;
                tag.knockback = 4f + wave * 2f;
                tag.stagger = 4f;
                CombatSystem.HitRadius(this, at, r, tag);
                VFX.Shockwave(at, r, c, 0.45f);
                VFX.Shockwave(at, r * 0.7f, Color.Lerp(c, Color.white, 0.5f), 0.35f);
                ElementFx.Finisher(at, fwd, Def.element, r * 0.8f);
                VFX.Dust(at, 10);
                if (cam != null) cam.Shake(0.35f + wave * 0.1f);
                if (Audio != null) Audio.PlayPitched("impact", 0.7f, 1.1f - wave * 0.12f);
                GameEvents.RaiseImpact(0.35f + wave * 0.15f);
                yield return new WaitForSeconds(0.22f);
            }
            VFX.HitStar(Position + fwd * 2f + Vector3.up * 1.4f, c, 2.6f, 0.2f);
            yield return new WaitForSeconds(0.2f);
            if (cam != null) { cam.EndCinematic(); cam.SetZoom(1f, 3f); }
        }

        /// <summary>The Mythic special's encore: element strikes rain on every demon nearby, then a closing burst.</summary>
        IEnumerator MythicEncore(Color c, DamageTally tally)
        {
            var cam = CameraController.Instance;
            if (Audio != null) Audio.PlayPitched("sp_whoosh", 0.8f, 0.85f);
            for (int i = 0; i < 6; i++)
            {
                // The nearest demons get hit first; empty ground around the slayer if there are none left.
                Vector3 at = Position + Quaternion.Euler(0f, i * 60f + Random.Range(-20f, 20f), 0f) * Vector3.forward * Random.Range(2.5f, 6f);
                float best = 11f;
                foreach (var e in Combatant.All)
                {
                    if (e == null || !e.IsAlive || e.Team == Team) continue;
                    float d = (e.Position - Position).magnitude + Random.Range(0f, 3f);
                    if (d < best) { best = d; at = e.Position; }
                }
                at = new Vector3(at.x, 0f, at.z);
                VFX.Pillar(at, Color.Lerp(c, Color.white, 0.3f), 9f, 0.3f);
                ElementFx.Impact(at + Vector3.up, Def.element, true);
                VFX.HitStar(at + Vector3.up * 1.2f, c, 1.3f, 0.12f);
                var tag = AttackTag.Basic(1.6f, c);
                tag.special = true;
                tag.isUltimate = true;
                tag.heavy = true;
                tag.stagger = 5f;
                tally.Add(CombatSystem.HitRadius(this, at, 2.6f, tag));
                if (cam != null) cam.Shake(0.3f);
                if (Audio != null) Audio.PlayPitched("hit_heavy", 0.55f, Random.Range(0.9f, 1.15f));
                yield return new WaitForSeconds(0.16f);
            }
            // Closing burst.
            VFX.Shockwave(Position, 11f * RarityFx, Color.Lerp(c, Color.white, 0.5f), 0.7f);
            VFX.HitStar(Position + Vector3.up * 1.8f, c, 4f, 0.25f);
            VFX.ImpactLight(Position + Vector3.up * 2f, c, 16f, 0.5f);
            if (cam != null) cam.Shake(0.8f);
            if (Audio != null) Audio.Play("sp_finish", 0.9f);
            GameEvents.RaiseImpact(1f);
            yield return new WaitForSeconds(0.3f);
        }
    }
}
