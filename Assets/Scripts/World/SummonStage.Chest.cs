using System.Collections;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The summon chest. It replaces the old sword-draw intro and shrine rite:
    ///   1. A chunky treasure chest slams down onto the altar (dust, shockwave, squash-and-bounce).
    ///   2. It lifts and spins faster and faster; as it spins it can upgrade — each rarity step flashes and
    ///      repaints the chest in that rarity's colour, up to the best slayer in this pull.
    ///   3. It slows to face the camera and settles, rattling: TAP TO OPEN (1 tap, 2 for Epic+, 3 for Legendary+).
    ///      Each tap jolts it and cracks the lid open a little more, leaking light.
    ///   4. The lid bursts open: a pillar of light, a shockwave, lightning for the big ones, and the cards fly out.
    /// </summary>
    public partial class SummonStage
    {
        Transform chest, chestLid;
        Material chestBodyMat, chestGlowMat;
        Light chestLight;

        /// <summary>The chest is waiting for taps (the UI shows TAP TO OPEN).</summary>
        public bool ChestWaiting { get; private set; }
        public int ChestTaps { get; private set; }
        public int ChestTapsNeeded { get; private set; }
        /// <summary>Colour the chest currently shows (the UI tints its glow to match).</summary>
        public Color ChestColor { get; private set; }

        void BuildChest()
        {
            HideChest();
            chest = new GameObject("SummonChest").transform;
            chest.SetParent(world.transform, false);
            chest.position = AltarPos + Vector3.up * 8f;
            var body = new GameObject("Body").transform;
            body.SetParent(chest, false);
            chestBodyMat = MaterialFactory.Toon(new Color(0.3f, 0.5f, 0.9f), 0.03f);
            var gold = MaterialFactory.Toon(new Color(1f, 0.8f, 0.3f), 0.03f, new Color(0.25f, 0.18f, 0.05f));
            var dark = MaterialFactory.Toon(new Color(0.16f, 0.12f, 0.18f), 0.03f);
            var inner = MaterialFactory.Toon(new Color(0.08f, 0.06f, 0.1f), 0f);
            // Base: a rounded box with a gold rim, corner caps and two straps; the front faces the camera (−Z).
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), body, new Vector3(0f, 0.45f, 0f), new Vector3(1.7f, 0.9f, 1.15f), chestBodyMat);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), body, new Vector3(0f, 0.9f, 0f), new Vector3(1.76f, 0.1f, 1.21f), gold);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), body, new Vector3(0f, 0.04f, 0f), new Vector3(1.78f, 0.1f, 1.23f), dark);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), body, new Vector3(0f, 0.88f, 0f), new Vector3(1.55f, 0.04f, 1.0f), inner);
            for (int s = -1; s <= 1; s += 2)
            {
                MeshFactory.MeshObject(MeshFactory.RoundedCube(), body, new Vector3(s * 0.5f, 0.45f, 0f), new Vector3(0.16f, 0.94f, 1.2f), gold);
                for (int z = -1; z <= 1; z += 2)
                    MeshFactory.MeshObject(MeshFactory.RoundedCube(), body, new Vector3(s * 0.8f, 0.45f, z * 0.52f), new Vector3(0.2f, 0.98f, 0.2f), dark);
            }
            // Lid: hinged at the back, a rounded dome with straps and a big lock plate at the front.
            chestLid = new GameObject("Lid").transform;
            chestLid.SetParent(chest, false);
            chestLid.localPosition = new Vector3(0f, 0.95f, 0.58f);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), chestLid, new Vector3(0f, 0.1f, -0.58f), new Vector3(1.74f, 0.22f, 1.19f), chestBodyMat);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), chestLid, new Vector3(0f, 0.2f, -0.58f), new Vector3(1.72f, 0.8f, 1.17f), chestBodyMat);
            for (int s = -1; s <= 1; s += 2)
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), chestLid, new Vector3(s * 0.5f, 0.2f, -0.58f), new Vector3(0.18f, 0.86f, 1.22f), gold);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), chestLid, new Vector3(0f, -0.02f, -1.2f), new Vector3(0.36f, 0.42f, 0.1f), gold);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), chestLid, new Vector3(0f, -0.04f, -1.26f), new Vector3(0.1f, 0.14f, 0.04f), dark);
            // The light inside (shown as the lid opens).
            chestGlowMat = MaterialFactory.Additive(new Color(1f, 1f, 1f, 0f));
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), chest, new Vector3(0f, 0.95f, 0f), new Vector3(1.5f, 0.6f, 1f), chestGlowMat, false);
            var lg = new GameObject("ChestLight");
            lg.transform.SetParent(chest, false);
            lg.transform.localPosition = new Vector3(0f, 1.3f, -0.4f);
            chestLight = lg.AddComponent<Light>();
            chestLight.type = LightType.Point;
            chestLight.range = 7f;
            chestLight.intensity = 0f;
        }

        void HideChest()
        {
            if (chest != null) Destroy(chest.gameObject);
            chest = null;
            chestLid = null;
            ChestWaiting = false;
        }

        void PaintChest(Color c)
        {
            ChestColor = c;
            if (chestBodyMat != null) chestBodyMat.color = Color.Lerp(c, new Color(0.15f, 0.1f, 0.2f), 0.2f);
            if (chestLight != null) chestLight.color = c;
        }

        void ChestGlow(float k)
        {
            if (chestGlowMat != null) chestGlowMat.color = new Color(ChestColor.r, ChestColor.g, ChestColor.b, Mathf.Clamp01(k) * 0.8f);
            if (chestLight != null) chestLight.intensity = k * 6f;
        }

        IEnumerator ChestRite(AudioManager audio, Color best, bool big)
        {
            Phase = SummonPhase.Chest;
            PhaseStart = Time.unscaledTime;
            advance = false;
            tapped = false;
            ChestTaps = 0;
            // The chest never tells: random taps, random colours, the same burst for every pull.
            ChestTapsNeeded = Random.Range(1, 4);
            Color neutral = new Color(0.98f, 0.8f, 0.35f);
            PrepareShrine();
            ApplyLighting(0.5f);
            SetShrineGlow(0.5f);
            var cam = CameraController.Instance;
            if (cam != null) cam.Cut(new Vector3(0f, 3f, -6.6f), AltarPos + Vector3.up * 1.1f);
            BuildChest();
            int step = 3;
            PaintChest(RarityInfo.Color(step));
            ChestGlow(0f);
            Vector3 rest = AltarPos + Vector3.up * 0.3f;

            // 1. Slam down onto the altar.
            float e = 0f;
            if (audio != null) audio.Play("sp_whoosh", 0.6f);
            while (e < 0.42f && !advance)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / 0.42f);
                chest.position = Vector3.Lerp(AltarPos + Vector3.up * 8f, rest, k * k);
                chest.localScale = new Vector3(0.92f, 1.12f, 0.92f);
                yield return null;
            }
            chest.position = rest;
            VFX.Dust(rest, 14);
            VFX.Shockwave(rest, 4f, new Color(1f, 0.9f, 0.7f), 0.35f);
            if (audio != null) { audio.Play("thud", 0.8f); audio.PlayPitched("slam", 0.5f, 0.8f); }
            if (cam != null) cam.Shake(0.25f);
            e = 0f;
            while (e < 0.3f)
            {
                e += Time.unscaledDeltaTime;
                float k = e / 0.3f;
                float sq = Mathf.Sin(k * Mathf.PI * 2.5f) * (1f - k) * 0.22f;
                chest.localScale = new Vector3(1f + sq, 1f - sq, 1f + sq);
                yield return null;
            }
            chest.localScale = Vector3.one;

            // 2. Spin up, flickering through random colours (a roulette, not a hint).
            if (audio != null) audio.Play("charge", 0.6f);
            float spinDur = 1.9f, angle = 0f, speed = 0f;
            float nextFlip = 0.35f;
            e = 0f;
            while (e < spinDur && !advance)
            {
                float dt = Time.unscaledDeltaTime;
                e += dt;
                float k = e / spinDur;
                speed = Mathf.Lerp(90f, 1300f, k * k);
                angle += speed * dt;
                chest.rotation = Quaternion.Euler(0f, angle, Mathf.Sin(e * 30f) * 3f * k);
                chest.position = rest + Vector3.up * (Mathf.SmoothStep(0f, 1.1f, k) + Mathf.Sin(e * 9f) * 0.05f);
                ChestGlow(0.15f + 0.35f * k);
                if (e >= nextFlip)
                {
                    nextFlip = e + Random.Range(0.18f, 0.45f);
                    step = Random.Range(2, 7);
                    PaintChest(RarityInfo.Color(step));
                    VFX.BurstDisc(chest.position + Vector3.up * 0.5f, 3f + step * 0.4f, ChestColor, 0.35f);
                    VFX.ImpactLight(chest.position + Vector3.up, ChestColor, 10f, 0.4f);
                    VFX.HitSpark(chest.position + Vector3.up * 0.6f, ChestColor, 24);
                    if (audio != null) audio.PlayPitched("coin", 0.8f, 0.8f + step * 0.12f);
                    if (cam != null) cam.Shake(0.12f + step * 0.03f);
                }
                if (Random.value < 0.5f) VFX.Breath(chest.position + Random.insideUnitSphere * 1.2f, ChestColor, 1);
                yield return null;
            }
            PaintChest(neutral);

            // 3. Slow to face the camera and drop back onto the altar.
            float startA = angle, endA = Mathf.Ceil((angle + 200f) / 360f) * 360f;
            e = 0f;
            while (e < 0.6f)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / 0.6f);
                float ek = 1f - (1f - k) * (1f - k) * (1f - k);
                chest.rotation = Quaternion.Euler(0f, Mathf.Lerp(startA, endA, ek), 0f);
                chest.position = rest + Vector3.up * 1.1f * (1f - k * k);
                yield return null;
            }
            chest.rotation = Quaternion.identity;
            chest.position = rest;
            VFX.Dust(rest, 8);
            if (audio != null) audio.Play("thud", 0.6f);

            // 4. Tap to open: it rattles, each tap jolts it and cracks the lid.
            ChestWaiting = true;
            tapped = false;
            advance = false;
            float idle = 0f;
            while (ChestTaps < ChestTapsNeeded && !skipAll)
            {
                float dt = Time.unscaledDeltaTime;
                idle += dt;
                float rattle = Mathf.Sin(Time.unscaledTime * 26f) * (2f + ChestTaps * 1.5f) * (Mathf.Repeat(Time.unscaledTime, 1.1f) < 0.35f ? 1f : 0.15f);
                chest.rotation = Quaternion.Euler(0f, 0f, rattle);
                ChestGlow(0.5f + 0.15f * ChestTaps + 0.1f * Mathf.Sin(Time.unscaledTime * 6f));
                if (tapped || advance || idle > 8f)
                {
                    tapped = false;
                    advance = false;
                    idle = 0f;
                    ChestTaps++;
                    VFX.HitSpark(chest.position + new Vector3(0f, 1f, -0.6f), ChestColor, 18);
                    if (audio != null) audio.PlayPitched("parry", 0.7f, 0.9f + ChestTaps * 0.12f);
                    if (cam != null) cam.Shake(0.1f + ChestTaps * 0.05f);
                    float j = 0f;
                    while (j < 0.18f)
                    {
                        j += Time.unscaledDeltaTime;
                        float jk = Mathf.Sin(Mathf.Clamp01(j / 0.18f) * Mathf.PI);
                        chest.localScale = new Vector3(1f + 0.14f * jk, 1f - 0.14f * jk, 1f + 0.14f * jk);
                        chest.position = rest + Vector3.up * 0.25f * jk;
                        yield return null;
                    }
                    chest.localScale = Vector3.one;
                    chest.position = rest;
                    if (ChestTaps < ChestTapsNeeded) chestLid.localRotation = Quaternion.Euler(10f * ChestTaps, 0f, 0f);
                }
                yield return null;
            }
            ChestWaiting = false;

            // 5. Burst open.
            if (audio != null) { audio.Play("specialRelease", 0.8f); audio.Play(big ? "ultimate" : "skill", 0.7f); }
            e = 0f;
            float from = chestLid.localRotation.eulerAngles.x;
            while (e < 0.28f)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / 0.28f);
                const float c1 = 1.70158f, c3 = c1 + 1f;
                float x = k - 1f;
                float back = 1f + c3 * x * x * x + c1 * x * x;
                chestLid.localRotation = Quaternion.Euler(Mathf.LerpUnclamped(from, 112f, back), 0f, 0f);
                ChestGlow(0.8f + 0.2f * k);
                chest.localScale = Vector3.one * (1f + 0.1f * Mathf.Sin(k * Mathf.PI));
                yield return null;
            }
            if (circle != null) circle.localScale = circleBase;
            if (runes != null) runes.localScale = runesBase;
            // The same golden burst whatever is inside; the colours come at each slayer's reveal.
            best = neutral;
            big = Random.value < 0.5f;
            SetCircle(best, 1.6f);
            SetShrineGlow(1f);
            ApplyLighting(0.7f);
            VFX.Pillar(rest + Vector3.up, best, big ? 18f : 12f, 1f);
            VFX.Shockwave(rest, big ? 9f : 6f, best, 0.6f);
            VFX.BurstDisc(rest, 6f, best, 0.6f);
            VFX.Breath(rest + Vector3.up, best, big ? 90 : 50);
            VFX.ImpactLight(rest + Vector3.up * 2f, best, big ? 22f : 14f, 0.6f);
            if (cam != null) cam.Shake(big ? 0.6f : 0.3f);
            GameEvents.RaiseImpact(big ? 1f : 0.5f);
            if (big)
                for (int i = 0; i < 5; i++)
                {
                    Vector3 o = AltarPos + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.5f, 5f);
                    BoltFx.Strike(o + Vector3.up * 18f, o, Color.Lerp(best, Color.white, 0.5f), 0.4f, 0.25f, 0.5f);
                    if (audio != null && i % 2 == 0) audio.Play("el_thunder", 0.6f);
                    yield return Wait(0.07f);
                }
            SetBraziers(best, 1.4f);
            yield return Wait(0.35f);
        }
    }
}
