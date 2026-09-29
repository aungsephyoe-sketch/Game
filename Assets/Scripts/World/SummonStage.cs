using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The Summoning Shrine: a dark stone altar in a starry void. Every summon is staged as an event:
    ///   ×10 — ten star orbs rise in a ring, each already glowing its rarity colour, a tease of what's coming;
    ///   each pull — the lights die, energy spirals into a sealed orb over the altar and the player TAPS to crack
    ///   the seal (each crack can "upgrade" the colour), the orb shatters, the braziers ignite in the rarity colour,
    ///   lightning strikes for Legendary+, a portal opens, a silhouette steps out, then the full reveal.
    /// The UI reads <see cref="Phase"/>/<see cref="Current"/> to draw prompts, name cards and the results grid.
    /// </summary>
    public partial class SummonStage : MonoBehaviour
    {
        public enum SummonPhase { Idle, Gather, Charging, Seal, Silhouette, Reveal, Summary, Intro, Shrine, Cards, Chest }

        // The 2D intro drawn by the summon screen, in seconds from the start of the Intro phase:
        // fade to black, the sheathed sword appears, a hand grips it, CLANG — drawn, the screen is sliced open.
        public const float IntroFade = 0.4f, IntroSword = 1.3f, IntroGrip = 1.85f, IntroDraw = 2.15f, IntroSlice = 2.8f;

        /// <summary>Best rarity in this summon (colours the slash, the seal and the effects).</summary>
        public int BestRarity { get; private set; }

        /// <summary>Taps needed to break the current seal, and taps landed so far.</summary>
        public int SealNeeded { get; private set; }
        public int SealCracks { get; private set; }
        bool tapped;
        Transform orb;
        Material orbCore, orbGlow;
        readonly List<Transform> cracks = new List<Transform>();
        readonly List<Transform> ringOrbs = new List<Transform>();
        readonly List<Light> brazierLights = new List<Light>();

        public SummonPhase Phase { get; private set; }
        public SummonSystem.Result Current { get; private set; }
        public int CurrentIndex { get; private set; }
        public List<SummonSystem.Result> Results { get; private set; }
        public float PhaseStart { get; private set; }
        /// <summary>Colour the circle currently shows (the UI tints its glow to match).</summary>
        public Color CircleColor { get; private set; }

        GameObject world;
        Transform heroHolder;
        Material circleMat, runeMat, portalMat, glowMat;
        Transform circle, runes, portal;
        Light altarLight;
        CharacterVisual shown;
        Coroutine routine;
        bool advance, skipAll;
        readonly List<Transform> pillars = new List<Transform>();

        static readonly Vector3 AltarPos = Vector3.zero;

        public void Show()
        {
            EnsureWorld();
            gameObject.SetActive(true);
            ApplyLighting(0.55f);
            if (routine == null)
            {
                Phase = SummonPhase.Idle;
                ClearShown();
                SetCircle(new Color(0.35f, 0.55f, 1f), 0.35f);
            }
        }

        public void Hide()
        {
            if (routine != null) { StopCoroutine(routine); routine = null; }
            Phase = SummonPhase.Idle;
            ClearShown();
            ClearRing();
            HideOrb();
            gameObject.SetActive(false);
        }

        public bool Busy { get { return routine != null; } }

        /// <summary>Tap: finish the current reveal and move to the next.</summary>
        public void Advance() { advance = true; }

        /// <summary>Tap on the seal: cracks it a little more.</summary>
        public void Tap() { tapped = true; }

        /// <summary>Skip straight to the results grid.</summary>
        public void SkipAll() { skipAll = true; advance = true; tapped = true; }

        /// <summary>Close the results grid and return to the banner.</summary>
        public void CloseSummary()
        {
            Phase = SummonPhase.Idle;
            ClearShown();
            ClearRing();
            HideOrb();
            SetBraziers(new Color(1f, 0.6f, 0.3f), 0.8f);
            SetCircle(new Color(0.35f, 0.55f, 1f), 0.35f);
            ApplyLighting(0.55f);
        }

        public void Play(List<SummonSystem.Result> results)
        {
            if (results == null || results.Count == 0) return;
            Results = results;
            skipAll = false;
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Sequence());
        }

        // ------------------------------------------------------------------ Sequence

        IEnumerator Sequence()
        {
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            BestRarity = 2;
            foreach (var r in Results) BestRarity = Mathf.Max(BestRarity, r.rarity);
            bool big = BestRarity >= 5;
            Color best = RarityInfo.Color(BestRarity);
            ClearShown();
            ClearCards();
            // Nothing from the previous pull may show while this one plays.
            Current = null;
            CurrentIndex = 0;

            // 1–9. The summon chest: it slams down, spins and upgrades, waits for taps and bursts open.
            advance = false;
            if (!skipAll) yield return ChestRite(audio, best, big);
            // 10. Cards fly out and land around the altar.
            if (!skipAll) yield return FlyCards(audio);
            HideChest();
            // 11–12. Each slayer lands with an impact; rarity sets the colour and the effects.
            for (CurrentIndex = 0; CurrentIndex < Results.Count && !skipAll; CurrentIndex++)
            {
                Current = Results[CurrentIndex];
                yield return LandOne(Current, audio, Results.Count > 1);
            }
            ClearCards();
            HideChest();
            HideRite();
            heroHolder.position = AltarPos + Vector3.up * 0.3f;
            Phase = SummonPhase.Summary;
            PhaseStart = Time.unscaledTime;
            ClearShown();
            SummonSystem.Result top = Results[0];
            foreach (var r in Results) if (r.rarity > top.rarity) top = r;
            ShowHero(top.def, false);
            SetCircle(RarityInfo.Color(top.rarity), 1f);
            ApplyLighting(0.8f);
            if (CameraController.Instance != null) CameraController.Instance.Cut(new Vector3(-1.2f, 1.9f, -5.5f), new Vector3(-1.2f, 1.3f, 0f), true);
            routine = null;
        }

        // ------------------------------------------------------------------ The rite

        Transform moon, swirl;
        Light shrineLight;
        Material shrinePaper;
        readonly List<Transform> cards = new List<Transform>();
        Vector3 circleBase, runesBase;

        /// <summary>Dark shrine, no moon, no seal: ready for the slice to open onto.</summary>
        void PrepareShrine()
        {
            ApplyLighting(0.25f);
            SetBraziers(new Color(0.4f, 0.5f, 1f), 0.15f);
            SetShrineGlow(0.15f);
            if (moon != null) { moon.gameObject.SetActive(false); }
            if (swirl != null) swirl.gameObject.SetActive(false);
            if (circle != null) circle.localScale = Vector3.zero;
            if (runes != null) runes.localScale = Vector3.zero;
            SetCircle(new Color(0.35f, 0.55f, 1f), 0.4f);
        }

        void HideRite()
        {
            if (moon != null) moon.gameObject.SetActive(false);
            if (swirl != null) swirl.gameObject.SetActive(false);
            if (circle != null) circle.localScale = circleBase;
            if (runes != null) runes.localScale = runesBase;
            SetShrineGlow(0.6f);
        }

        void SetShrineGlow(float k)
        {
            if (shrinePaper != null)
            {
                Color em = new Color(1f, 0.62f, 0.3f) * Mathf.Clamp01(k);
                if (shrinePaper.HasProperty("_Emission")) shrinePaper.SetColor("_Emission", em);
            }
            if (shrineLight != null) shrineLight.intensity = 3.2f * k;
        }

        IEnumerator ShrineRite(AudioManager audio, Color best, bool big)
        {
            Phase = SummonPhase.Shrine;
            PhaseStart = Time.unscaledTime;
            advance = false;
            var cam = CameraController.Instance;
            // 5. The shrine lights up behind the slash.
            float e = 0f;
            while (e < 0.6f && !advance)
            {
                e += Time.unscaledDeltaTime;
                SetShrineGlow(Mathf.SmoothStep(0.15f, 1f, e / 0.6f));
                yield return null;
            }
            SetShrineGlow(1f);
            // 6. A massive moon rises overhead; the camera tilts up to it.
            moon.gameObject.SetActive(true);
            if (cam != null) cam.Dolly(new Vector3(0f, 3f, -9f), new Vector3(0f, 10f, 22f), 1.2f);
            if (audio != null) audio.Play("charge", 0.5f);
            e = 0f;
            while (e < 1.3f && !advance)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, e / 1.3f);
                moon.position = new Vector3(0f, Mathf.Lerp(10f, 26f, k), 48f);
                moon.localScale = Vector3.one * Mathf.Lerp(4f, 22f, k);
                ApplyLighting(Mathf.Lerp(0.25f, 0.55f, k));
                yield return null;
            }
            moon.position = new Vector3(0f, 26f, 48f);
            moon.localScale = Vector3.one * 22f;
            // 7. Energy swirls around the shrine and down to the altar.
            swirl.gameObject.SetActive(true);
            SetSwirl(best, 0f);
            if (cam != null) cam.Dolly(new Vector3(0f, 4.2f, -9.5f), new Vector3(0f, 2f, 4f), 1.2f);
            if (audio != null) audio.Play("buildup", 0.6f);
            e = 0f;
            while (e < 1.3f && !advance)
            {
                e += Time.unscaledDeltaTime;
                float k = e / 1.3f;
                SetSwirl(best, k);
                for (int i = 0; i < 2; i++)
                {
                    float a = (Time.unscaledTime * 3f + i * Mathf.PI) % (Mathf.PI * 2f);
                    float rad = Mathf.Lerp(7f, 2f, k);
                    Vector3 p = AltarPos + new Vector3(Mathf.Cos(a) * rad, 0.5f + (1f - k) * 3f, Mathf.Sin(a) * rad + 3f * (1f - k));
                    VFX.Breath(p, Color.Lerp(new Color(0.5f, 0.7f, 1f), best, k), 2);
                }
                yield return null;
            }
            // 8. The summon seal draws itself on the ground.
            if (cam != null) cam.Dolly(new Vector3(0f, 6.2f, -8f), AltarPos + Vector3.up * 0.4f, 0.9f);
            if (audio != null) audio.Play("sp_activate", 0.7f);
            e = 0f;
            while (e < 0.9f && !advance)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, e / 0.9f);
                circle.localScale = circleBase * k;
                runes.localScale = runesBase * Mathf.Clamp01(k * 1.3f - 0.3f);
                SetCircle(Color.Lerp(new Color(0.35f, 0.55f, 1f), best, k), 0.6f + k);
                SetSwirl(best, 1f);
                yield return null;
            }
            circle.localScale = circleBase;
            runes.localScale = runesBase;
            // 9. The seal erupts upward.
            SetCircle(best, 1.6f);
            VFX.Pillar(AltarPos, best, big ? 18f : 12f, 1f);
            VFX.Shockwave(AltarPos, big ? 9f : 6f, best, 0.6f);
            VFX.BurstDisc(AltarPos, 6f, best, 0.6f);
            VFX.Breath(AltarPos + Vector3.up, best, big ? 90 : 50);
            VFX.ImpactLight(AltarPos + Vector3.up * 2f, best, big ? 22f : 14f, 0.6f);
            if (audio != null) { audio.Play("specialRelease", 0.8f); audio.Play(big ? "ultimate" : "skill", 0.7f); }
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
            swirl.gameObject.SetActive(false);
            yield return Wait(0.3f);
        }

        void SetSwirl(Color c, float k)
        {
            if (swirl == null) return;
            for (int i = 0; i < swirl.childCount; i++)
            {
                var ring = swirl.GetChild(i);
                float spin = Time.unscaledTime * (120f + i * 60f) * (i % 2 == 0 ? 1f : -1f);
                ring.localRotation = Quaternion.Euler(70f + i * 8f, spin, 0f);
                float rad = Mathf.Lerp(6f - i, 2.2f + i * 0.4f, k);
                ring.localScale = Vector3.one * rad;
                ring.localPosition = new Vector3(0f, Mathf.Lerp(3f + i, 0.6f + i * 0.5f, k), 0f);
                var rend = ring.GetComponent<Renderer>();
                if (rend != null) rend.sharedMaterial.color = new Color(c.r, c.g, c.b, 0.25f + 0.35f * k);
            }
        }

        /// <summary>Cards burst out of the pillar and land in an arc around the altar, each glowing its rarity.</summary>
        IEnumerator FlyCards(AudioManager audio)
        {
            Phase = SummonPhase.Cards;
            PhaseStart = Time.unscaledTime;
            advance = false;
            // The rite is over: the moon and the swirl leave the sky so the reveals stay clean.
            if (moon != null) moon.gameObject.SetActive(false);
            if (swirl != null) swirl.gameObject.SetActive(false);
            var cam = CameraController.Instance;
            if (cam != null) cam.Dolly(new Vector3(0f, 4.4f, -9.5f), AltarPos + Vector3.up * 1f, 0.8f);
            int n = Results.Count;
            var from = new List<Vector3>();
            var to = new List<Vector3>();
            for (int i = 0; i < n; i++)
            {
                float a = n == 1 ? 0f : Mathf.Lerp(-62f, 62f, i / (n - 1f));
                to.Add(AltarPos + new Vector3(Mathf.Sin(a * Mathf.Deg2Rad) * 3.4f, 0.8f, -Mathf.Cos(a * Mathf.Deg2Rad) * 3.4f));
                from.Add(AltarPos + Vector3.up * 2.6f);
                cards.Add(MakeCard(Results[i].rarity));
            }
            float dur = 0.9f, stagger = Mathf.Min(0.08f, 0.6f / n);
            float e = 0f;
            var landed = new bool[n];
            while (e < dur + stagger * n)
            {
                e += Time.unscaledDeltaTime;
                for (int i = 0; i < n; i++)
                {
                    float k = Mathf.Clamp01((e - i * stagger) / dur);
                    var c = cards[i];
                    if (c == null) continue;
                    c.gameObject.SetActive(k > 0f);
                    float ek = 1f - (1f - k) * (1f - k);
                    c.position = Vector3.Lerp(from[i], to[i], ek) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 3.2f;
                    c.rotation = Quaternion.Euler(0f, 180f + (1f - k) * 900f, (1f - k) * 40f);
                    if (k >= 1f && !landed[i])
                    {
                        landed[i] = true;
                        Color rc = RarityInfo.Color(Results[i].rarity);
                        VFX.Dust(to[i] - Vector3.up * 0.75f, 6);
                        VFX.Shockwave(to[i] - Vector3.up * 0.75f, 1f + Results[i].rarity * 0.2f, rc, 0.3f);
                        if (audio != null) audio.PlayPitched("coin", 0.35f, 0.8f + Results[i].rarity * 0.1f);
                    }
                }
                yield return null;
            }
            yield return Wait(0.35f);
        }

        Transform MakeCard(int rarity)
        {
            Color rc = RarityInfo.Color(rarity);
            var t = new GameObject("SummonCard").transform;
            t.SetParent(world.transform, false);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), t, Vector3.zero, new Vector3(0.86f, 1.2f, 0.04f), MaterialFactory.Toon(Color.Lerp(rc, Color.white, 0.2f), 0.01f, rc * 0.8f), false);
            MeshFactory.MeshObject(MeshFactory.RoundedCube(), t, new Vector3(0f, 0f, -0.012f), new Vector3(0.74f, 1.06f, 0.04f), MaterialFactory.Toon(new Color(0.06f, 0.05f, 0.1f), 0f), false);
            // A slayer's silhouette on the face of the card.
            var ink = MaterialFactory.Toon(Color.Lerp(rc, Color.black, 0.6f), 0f, rc * 0.25f);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), t, new Vector3(0f, 0.22f, -0.04f), new Vector3(0.26f, 0.26f, 0.02f), ink, false);
            MeshFactory.MeshObject(MeshFactory.SmoothCapsule(), t, new Vector3(0f, -0.14f, -0.04f), new Vector3(0.26f, 0.22f, 0.02f), ink, false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), t, Vector3.zero, new Vector3(1.4f, 1.8f, 0.3f), MaterialFactory.Additive(new Color(rc.r, rc.g, rc.b, rarity >= 5 ? 0.45f : 0.25f)), false);
            t.gameObject.SetActive(false);
            return t;
        }

        void ClearCards()
        {
            foreach (var c in cards) if (c != null) Destroy(c.gameObject);
            cards.Clear();
        }

        /// <summary>One slayer: their card flies to the altar and bursts, the slayer drops from above and lands.</summary>
        IEnumerator LandOne(SummonSystem.Result r, AudioManager audio, bool multi)
        {
            int rarity = r.rarity;
            Color rc = RarityInfo.Color(rarity);
            bool big = rarity >= 5;
            advance = false;
            ClearShown();
            var cam = CameraController.Instance;
            Phase = SummonPhase.Silhouette;
            PhaseStart = Time.unscaledTime;
            SetCircle(rc, 1.1f);
            // The card rises to the altar and bursts.
            var card = CurrentIndex < cards.Count ? cards[CurrentIndex] : null;
            if (card != null)
            {
                Vector3 a = card.position, b = AltarPos + Vector3.up * 2.4f;
                float e = 0f, d = multi ? 0.25f : 0.4f;
                while (e < d && !advance)
                {
                    e += Time.unscaledDeltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, e / d);
                    card.position = Vector3.Lerp(a, b, k);
                    card.rotation = Quaternion.Euler(0f, 180f + k * 360f, 0f);
                    yield return null;
                }
                VFX.HitSpark(b, Color.Lerp(rc, Color.white, 0.4f), big ? 40 : 20);
                VFX.Shockwave(b, big ? 4f : 2.5f, rc, 0.3f);
                VFX.ImpactLight(b, rc, big ? 14f : 8f, 0.25f);
                Destroy(card.gameObject);
                cards[CurrentIndex] = null;
                if (audio != null) audio.PlayPitched("impact", 0.6f, 1.2f);
            }
            // The slayer drops out of the light and lands with an impact.
            ShowHero(r.def, true);
            Vector3 land = AltarPos + Vector3.up * 0.3f, top = land + Vector3.up * 5f;
            if (cam != null) cam.Dolly(new Vector3(0f, 2.1f, -5.4f), land + Vector3.up * 1.2f, multi ? 0.35f : 0.5f);
            float f = 0f, fall = multi ? 0.28f : 0.4f;
            while (f < fall)
            {
                f += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(f / fall);
                heroHolder.position = Vector3.Lerp(top, land, k * k);
                yield return null;
            }
            heroHolder.position = land;
            VFX.Dust(land, big ? 16 : 10);
            VFX.Shockwave(land, big ? 5f : 3f, rc, 0.4f);
            VFX.BurstDisc(land, big ? 4f : 2.5f, rc, 0.4f);
            if (audio != null) audio.Play(big ? "impact" : "thud", 0.7f);
            if (cam != null) cam.Shake(big ? 0.5f : 0.15f);
            GameEvents.RaiseImpact(big ? 0.8f : 0.3f);
            if (big)
                for (int i = 0; i < (rarity >= 6 ? 6 : 3); i++)
                {
                    Vector3 o = land + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.2f, 3.5f);
                    BoltFx.Strike(o + Vector3.up * 14f, o, Color.Lerp(rc, Color.white, 0.5f), 0.35f, 0.22f, 0.45f);
                    if (audio != null && i == 0) audio.Play("el_thunder", 0.6f);
                    yield return Wait(0.06f);
                }
            yield return Wait(multi ? 0.15f : 0.3f);

            // The reveal: colour floods in (the UI shows the card).
            Phase = SummonPhase.Reveal;
            PhaseStart = Time.unscaledTime;
            SetSilhouette(false);
            ApplyLighting(big ? 1.1f : 0.8f);
            VFX.Pillar(land, rc, big ? 10f : 6f, 0.7f);
            VFX.Breath(land, rc, 30 + (rarity - 2) * 20);
            VFX.ImpactLight(land + Vector3.up * 1.5f, rc, 10f, 0.8f);
            if (shown != null) { shown.Flash(Color.white, 1f); shown.Victory(); }
            if (audio != null) audio.Play(big ? "ultimate" : "victory", 0.8f);
            if (rarity >= 6) foreach (var p in pillars) VFX.Pillar(p.position, rc, 12f, 1f);
            // Frame the slayer on the left third, clear of the name card on the right.
            if (cam != null) cam.Dolly(new Vector3(1.3f, 1.6f, -4.3f), land + new Vector3(1.15f, 1.15f, 0f), 0.9f);
            advance = false;
            float auto = multi ? 2.2f : 4.5f;
            float t0 = Time.unscaledTime;
            while (!advance && Time.unscaledTime - t0 < auto) yield return null;
            advance = false;
        }

        IEnumerator RevealOne(SummonSystem.Result r, AudioManager audio, bool multi)
        {
            int rarity = r.rarity;
            Color target = RarityInfo.Color(rarity);
            advance = false;
            ClearShown();

            // 1. Darkness; energy spirals into the sealed orb above the altar.
            Phase = SummonPhase.Charging;
            PhaseStart = Time.unscaledTime;
            var cam = CameraController.Instance;
            if (cam != null) { cam.Cut(new Vector3(0f, 7f, -9f), AltarPos + Vector3.up * 0.5f); cam.Dolly(new Vector3(0f, 2.8f, -6.2f), OrbPos, 1.4f); }
            ApplyLighting(0.15f);
            SetBraziers(new Color(0.35f, 0.55f, 1f), 0.2f);
            Color start = new Color(0.35f, 0.55f, 1f);
            SetCircle(start, 0.5f);
            ShowOrb(start);
            if (audio != null) audio.Play("charge", 0.7f);
            float charge = multi ? 0.6f : 1.1f;
            float e = 0f;
            while (e < charge && !advance)
            {
                e += Time.unscaledDeltaTime;
                // Motes stream inward from the pillars.
                if (Random.value < 0.5f)
                {
                    Vector3 o = AltarPos + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2.5f, 5f);
                    VFX.Breath(Vector3.Lerp(o, OrbPos - Vector3.up, Random.value), start, 3);
                }
                orb.localScale = Vector3.one * (0.6f + 0.4f * e / charge);
                SetCircle(start, 0.5f + e / charge * 0.5f);
                yield return null;
            }

            // 2. The seal: the player taps to crack it. Each crack may climb one rarity colour toward the result.
            //    Low pulls inside a ×10 break on their own so the run keeps moving.
            SealNeeded = multi && rarity < 5 ? 0 : 3;
            SealCracks = 0;
            Color seen = start;
            int tierShown = 2;
            if (SealNeeded > 0 && !skipAll)
            {
                Phase = SummonPhase.Seal;
                PhaseStart = Time.unscaledTime;
                tapped = false;
                float idle = 0f;
                while (SealCracks < SealNeeded && !skipAll)
                {
                    idle += Time.unscaledDeltaTime;
                    if (tapped || idle > 6f)
                    {
                        tapped = false;
                        idle = 0f;
                        SealCracks++;
                        // Tease: climb toward the real rarity on the last cracks.
                        int climb = Mathf.Min(rarity, tierShown + (SealCracks == SealNeeded ? 99 : (rarity > 3 && Random.value < 0.6f ? 1 : 0)));
                        if (climb > tierShown) { tierShown = climb; seen = RarityInfo.Color(tierShown); }
                        AddCrack(seen);
                        SetOrb(seen, 1f + SealCracks * 0.4f);
                        SetCircle(seen, 1f + SealCracks * 0.2f);
                        VFX.Shockwave(OrbPos, 1.4f + SealCracks * 0.6f, seen, 0.3f);
                        VFX.HitSpark(OrbPos, Color.Lerp(seen, Color.white, 0.4f), 16);
                        VFX.ImpactLight(OrbPos, seen, 8f, 0.2f);
                        if (audio != null) audio.PlayPitched(SealCracks == SealNeeded ? "impact" : "parry", 0.7f, 0.9f + SealCracks * 0.12f);
                        if (cam != null) cam.Shake(0.12f + SealCracks * 0.08f);
                        GameEvents.RaiseImpact(0.2f + SealCracks * 0.1f);
                    }
                    orb.localPosition = OrbPos + Random.insideUnitSphere * 0.03f * SealCracks;
                    yield return null;
                }
            }
            else
            {
                for (int tier = 3; tier <= rarity && !advance; tier++)
                {
                    seen = RarityInfo.Color(tier);
                    SetOrb(seen, 1.3f);
                    SetCircle(seen, 1.2f);
                    VFX.Shockwave(AltarPos, 3.5f, seen, 0.4f);
                    if (audio != null) audio.Play("skill", 0.4f);
                    yield return Wait(0.2f);
                }
            }
            Phase = SummonPhase.Charging;

            // 3. Shatter: shards fly, the braziers ignite in the rarity colour one by one.
            ShatterOrb(target, rarity);
            SetCircle(target, 1.4f);
            if (audio != null) audio.Play(rarity >= 5 ? "ultimate" : "skill", 0.7f);
            if (cam != null) cam.Shake(0.2f + 0.08f * rarity);
            for (int i = 0; i < brazierLights.Count && !advance; i++)
            {
                SetBrazier(i, target, 1f + 0.4f * (rarity - 2));
                VFX.Pillar(pillars[i].position + Vector3.up * 2.5f, target, 3f, 0.25f);
                if (audio != null && i % 2 == 0) audio.PlayPitched("thud", 0.3f, 1f + i * 0.05f);
                yield return Wait(multi ? 0.03f : 0.06f);
            }

            // Lightning for Legendary and Mythic; the Mythic sky turns into a ring of every rarity colour.
            if (rarity >= 5 && !advance)
            {
                for (int i = 0; i < (rarity >= 6 ? 8 : 4); i++)
                {
                    Vector3 o = AltarPos + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.5f, 5f);
                    BoltFx.Strike(o + Vector3.up * 16f, o, Color.Lerp(target, Color.white, 0.5f), 0.4f, 0.25f, 0.5f);
                    VFX.ImpactLight(o + Vector3.up * 3f, target, 12f, 0.15f);
                    if (audio != null) audio.Play("el_thunder", 0.6f);
                    if (cam != null) cam.Shake(0.25f);
                    yield return Wait(0.1f);
                }
                if (rarity >= 6)
                    for (int tier = 2; tier <= 6; tier++)
                    {
                        VFX.Shockwave(AltarPos, 3f + tier * 1.4f, RarityInfo.Color(tier), 0.6f);
                        yield return Wait(0.07f);
                    }
            }

            // 4. Portal and silhouette.
            Phase = SummonPhase.Silhouette;
            PhaseStart = Time.unscaledTime;
            portal.gameObject.SetActive(true);
            portalMat.color = new Color(target.r, target.g, target.b, 0.85f);
            e = 0f;
            float open = rarity >= 5 ? 0.8f : 0.45f;
            while (e < open && !advance)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, e / open);
                portal.localScale = new Vector3(2.4f * k, 3.4f * k, 1f);
                yield return null;
            }
            portal.localScale = new Vector3(2.4f, 3.4f, 1f);
            ShowHero(r.def, true);
            if (cam != null) cam.Dolly(new Vector3(0f, 1.8f, -4.6f), heroHolder.position + Vector3.up * 1.2f, rarity >= 5 ? 1.2f : 0.6f);
            yield return Wait(rarity >= 5 ? 1.1f : multi ? 0.35f : 0.6f);

            // 5. Reveal.
            Phase = SummonPhase.Reveal;
            PhaseStart = Time.unscaledTime;
            SetSilhouette(false);
            portal.gameObject.SetActive(false);
            ApplyLighting(rarity >= 5 ? 1.1f : 0.8f);
            VFX.Pillar(heroHolder.position, target, 9f, 0.8f);
            VFX.Shockwave(heroHolder.position, 5f, target, 0.6f);
            VFX.Breath(heroHolder.position, target, 40 + (rarity - 2) * 20);
            VFX.ImpactLight(heroHolder.position + Vector3.up * 1.5f, target, 10f, 0.8f);
            if (shown != null) { shown.Flash(Color.white, 1f); shown.Victory(); }
            if (audio != null) audio.Play(rarity >= 5 ? "ultimate" : "victory", 0.8f);
            GameEvents.RaiseImpact(rarity >= 5 ? 1f : 0.5f);
            if (rarity >= 6)
            {
                // Mythic: the whole shrine answers.
                foreach (var p in pillars) VFX.Pillar(p.position, target, 12f, 1f);
                if (cam != null) cam.Shake(0.5f);
            }
            if (cam != null) cam.Dolly(new Vector3(-1.1f, 1.7f, -3.9f), heroHolder.position + new Vector3(-0.9f, 1.25f, 0f), 1.2f);

            advance = false;
            float auto = multi ? 2.2f : 4.5f;
            float t0 = Time.unscaledTime;
            while (!advance && Time.unscaledTime - t0 < auto) yield return null;
            advance = false;
        }

        static readonly Vector3 OrbPos = new Vector3(0f, 1.9f, 0f);

        /// <summary>×10: ten orbs rise in a ring, each already glowing its pull's rarity colour.</summary>
        IEnumerator Gather(AudioManager audio)
        {
            Phase = SummonPhase.Gather;
            PhaseStart = Time.unscaledTime;
            advance = false;
            ClearRing();
            ApplyLighting(0.25f);
            var cam = CameraController.Instance;
            if (cam != null) { cam.Cut(new Vector3(0f, 9f, -10f), AltarPos); cam.Dolly(new Vector3(0f, 5.5f, -9f), AltarPos + Vector3.up * 1.5f, 2.2f); }
            if (audio != null) audio.Play("charge", 0.6f);
            for (int i = 0; i < Results.Count; i++)
            {
                Color c = RarityInfo.Color(Results[i].rarity);
                var t = new GameObject("RingOrb").transform;
                t.SetParent(world.transform, false);
                float a = i * 360f / Results.Count;
                t.position = AltarPos + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 3.4f;
                float size = Results[i].rarity >= 5 ? 0.62f : 0.45f;
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), t, Vector3.zero, Vector3.one * size, MaterialFactory.Toon(Color.Lerp(c, Color.white, 0.35f), 0f, c), false);
                MeshFactory.MeshObject(MeshFactory.SmoothSphere(), t, Vector3.zero, Vector3.one * size * 2.2f, MaterialFactory.Additive(new Color(c.r, c.g, c.b, 0.35f)), false);
                ringOrbs.Add(t);
                VFX.Pillar(t.position, c, 3f, 0.3f);
                if (audio != null) audio.PlayPitched("coin", 0.25f, 0.8f + Results[i].rarity * 0.1f);
                yield return Wait(0.1f);
            }
            float e = 0f;
            while (e < 1.4f && !advance)
            {
                e += Time.unscaledDeltaTime;
                for (int i = 0; i < ringOrbs.Count; i++)
                {
                    float a = i * 360f / ringOrbs.Count + e * 60f;
                    ringOrbs[i].position = AltarPos + Quaternion.Euler(0f, a, 0f) * Vector3.forward * 3.4f + Vector3.up * (0.6f + e * 1.2f + Mathf.Sin(e * 5f + i) * 0.15f);
                }
                yield return null;
            }
            advance = false;
        }

        void ClearRing()
        {
            foreach (var t in ringOrbs) if (t != null) Destroy(t.gameObject);
            ringOrbs.Clear();
        }

        void ShowOrb(Color c)
        {
            foreach (var cr in cracks) if (cr != null) Destroy(cr.gameObject);
            cracks.Clear();
            orb.gameObject.SetActive(true);
            orb.localPosition = OrbPos;
            orb.localScale = Vector3.one * 0.6f;
            SetOrb(c, 0.8f);
        }

        void HideOrb()
        {
            if (orb != null) orb.gameObject.SetActive(false);
        }

        void SetOrb(Color c, float intensity)
        {
            orbCore.color = Color.Lerp(c, Color.white, 0.3f);
            Color em = c * Mathf.Min(1.5f, 0.5f + intensity * 0.3f);
            if (orbCore.HasProperty("_Emission")) orbCore.SetColor("_Emission", em);
            if (orbCore.HasProperty("_EmissionColor")) orbCore.SetColor("_EmissionColor", em);
            orbGlow.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(0.25f + 0.1f * intensity));
            if (altarLight != null) { altarLight.color = c; altarLight.intensity = 1.5f + intensity * 1.5f; }
        }

        /// <summary>A bright crack line across the seal.</summary>
        void AddCrack(Color c)
        {
            var mat = MaterialFactory.Toon(Color.white, 0f, Color.Lerp(c, Color.white, 0.6f));
            for (int i = 0; i < 2; i++)
            {
                var cr = MeshFactory.Primitive(PrimitiveType.Cube, orb, Vector3.zero, new Vector3(0.04f, 1.05f, 0.04f), mat).transform;
                cr.localRotation = Random.rotation;
                cracks.Add(cr);
            }
        }

        void ShatterOrb(Color c, int rarity)
        {
            if (orb == null || !orb.gameObject.activeSelf) return;
            Vector3 p = orb.position;
            HideOrb();
            var mat = MaterialFactory.Toon(Color.Lerp(c, Color.white, 0.3f), 0f, c);
            int n = 10 + rarity * 3;
            for (int i = 0; i < n; i++)
            {
                var go = MeshFactory.Primitive(PrimitiveType.Cube, world.transform, p, Vector3.one * Random.Range(0.08f, 0.2f), mat);
                var sh = go.AddComponent<SummonShard>();
                sh.velocity = Random.onUnitSphere * Random.Range(4f, 9f) + Vector3.up * 2f;
            }
            VFX.Shockwave(p, 4f + rarity, c, 0.5f);
            VFX.BurstDisc(AltarPos, 5f, c, 0.6f);
            VFX.HitSpark(p, Color.white, 40);
            VFX.Breath(p, c, 60);
            VFX.ImpactLight(p, c, 16f, 0.5f);
            GameEvents.RaiseImpact(0.4f + rarity * 0.1f);
        }

        void SetBraziers(Color c, float intensity)
        {
            for (int i = 0; i < brazierLights.Count; i++) SetBrazier(i, c, intensity);
        }

        void SetBrazier(int i, Color c, float intensity)
        {
            if (i < 0 || i >= brazierLights.Count || brazierLights[i] == null) return;
            brazierLights[i].color = c;
            brazierLights[i].intensity = intensity;
        }

        IEnumerator Wait(float seconds)
        {
            float e = 0f;
            while (e < seconds && !advance)
            {
                e += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        void Update()
        {
            if (world == null) return;
            float t = Time.unscaledTime;
            if (runes != null) runes.localRotation = Quaternion.Euler(0f, t * (Phase == SummonPhase.Charging ? 160f : 25f), 0f);
            if (circle != null) circle.localRotation = Quaternion.Euler(0f, -t * 12f, 0f);
            if (portal != null && portal.gameObject.activeSelf) portal.Rotate(0f, 0f, 90f * Time.unscaledDeltaTime, Space.Self);
            if (orb != null && orb.gameObject.activeSelf) orb.Rotate(20f * Time.unscaledDeltaTime, 70f * Time.unscaledDeltaTime, 0f, Space.Self);
            if (routine == null && Phase == SummonPhase.Idle && CameraController.Instance != null)
                CameraController.Instance.SetFixed(new Vector3(Mathf.Sin(t * 0.15f) * 0.8f, 3.6f, -8f), AltarPos + Vector3.up * 1.4f);
            if (Phase == SummonPhase.Summary && heroHolder != null)
                heroHolder.rotation = Quaternion.Euler(0f, 180f + Mathf.Sin(t * 0.4f) * 20f, 0f);
        }

        // ------------------------------------------------------------------ Helpers

        void ShowHero(CharacterDefinition def, bool silhouette)
        {
            ClearShown();
            if (def == null) return;
            shown = CharacterVisual.BuildHero(def, heroHolder);
            heroHolder.rotation = Quaternion.Euler(0f, 180f, 0f);
            SetSilhouette(silhouette);
        }

        readonly List<Renderer> silhouetteRenderers = new List<Renderer>();
        readonly List<Material[]> savedMaterials = new List<Material[]>();
        Material blackMat;

        void SetSilhouette(bool on)
        {
            if (shown == null) return;
            if (on)
            {
                if (blackMat == null) blackMat = MaterialFactory.Toon(Color.black, 0.04f);
                silhouetteRenderers.Clear();
                savedMaterials.Clear();
                foreach (var r in shown.GetComponentsInChildren<Renderer>())
                {
                    if (r is ParticleSystemRenderer || r is TrailRenderer) continue;
                    silhouetteRenderers.Add(r);
                    savedMaterials.Add(r.sharedMaterials);
                    var mats = new Material[r.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = blackMat;
                    r.sharedMaterials = mats;
                }
            }
            else
            {
                for (int i = 0; i < silhouetteRenderers.Count; i++)
                    if (silhouetteRenderers[i] != null) silhouetteRenderers[i].sharedMaterials = savedMaterials[i];
                silhouetteRenderers.Clear();
                savedMaterials.Clear();
            }
        }

        void ClearShown()
        {
            silhouetteRenderers.Clear();
            savedMaterials.Clear();
            if (shown != null) Destroy(shown.gameObject);
            shown = null;
            if (portal != null) portal.gameObject.SetActive(false);
        }

        void SetCircle(Color c, float intensity)
        {
            CircleColor = c;
            if (circleMat == null) return;
            circleMat.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(0.45f * intensity));
            runeMat.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(0.6f * intensity));
            glowMat.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(0.25f * intensity));
            if (altarLight != null) { altarLight.color = c; altarLight.intensity = 1.5f + intensity * 2f; }
        }

        void ApplyLighting(float brightness)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.03f, 0.02f, 0.07f);
            RenderSettings.fogStartDistance = 22f;
            RenderSettings.fogEndDistance = 110f;
            RenderSettings.ambientLight = new Color(0.2f, 0.18f, 0.3f) * brightness;
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.color = new Color(0.7f, 0.7f, 1f);
                RenderSettings.sun.intensity = 0.9f * brightness;
                RenderSettings.sun.transform.rotation = Quaternion.Euler(60f, -20f, 0f);
            }
        }

        /// <summary>The glowing shrine behind the altar, a torii before it, the moon and the energy swirl.</summary>
        void BuildShrine(Transform root)
        {
            var s = new GameObject("Shrine").transform;
            s.SetParent(root, false);
            s.position = new Vector3(0f, 0f, 15f);

            var stone = MaterialFactory.Toon(new Color(0.3f, 0.29f, 0.33f), 0.02f);
            var red = MaterialFactory.Toon(new Color(0.72f, 0.12f, 0.12f), 0.02f);
            var wood = MaterialFactory.Toon(new Color(0.3f, 0.2f, 0.14f), 0.02f);
            var roof = MaterialFactory.Toon(new Color(0.16f, 0.15f, 0.22f), 0.02f);
            var gold = MaterialFactory.Toon(new Color(0.95f, 0.78f, 0.35f), 0.01f, new Color(0.3f, 0.2f, 0.05f));
            shrinePaper = MaterialFactory.Toon(new Color(1f, 0.9f, 0.7f), 0.01f, new Color(1f, 0.62f, 0.3f));
            var cube = MeshFactory.RoundedCube();
            var cyl = MeshFactory.SmoothCylinder();
            SP(s, cube, new Vector3(0f, 0.45f, 0f), new Vector3(11f, 0.9f, 7.5f), stone, Vector3.zero);
            for (int i = 0; i < 3; i++) SP(s, cube, new Vector3(0f, 0.15f + i * 0.2f, -4.4f + i * 0.45f), new Vector3(4.4f, 0.3f, 0.6f), stone, Vector3.zero);
            // Pillars, glowing paper walls, back wall.
            for (int i = 0; i < 4; i++)
            {
                float x = -4.2f + i * 2.8f;
                SP(s, cyl, new Vector3(x, 2.4f, -2.9f), new Vector3(0.42f, 1.5f, 0.42f), red, Vector3.zero);
                SP(s, cyl, new Vector3(x, 2.4f, 2.9f), new Vector3(0.42f, 1.5f, 0.42f), red, Vector3.zero);
                if (i < 3) SP(s, cube, new Vector3(x + 1.4f, 2.4f, -2.75f), new Vector3(2.3f, 2.6f, 0.12f), shrinePaper, Vector3.zero);
            }
            SP(s, cube, new Vector3(0f, 2.4f, 2.9f), new Vector3(8.6f, 3f, 0.25f), wood, Vector3.zero);
            for (int sd = -1; sd <= 1; sd += 2) SP(s, cube, new Vector3(sd * 4.3f, 2.4f, 0f), new Vector3(0.25f, 3f, 5.8f), wood, Vector3.zero);
            SP(s, cube, new Vector3(0f, 3.95f, -2.9f), new Vector3(9.4f, 0.3f, 0.35f), red, Vector3.zero);
            // A deep two-tier roof with upturned eaves and a gold ridge.
            for (int tier = 0; tier < 2; tier++)
            {
                float y = 4.7f + tier * 1.4f, w = 12.4f - tier * 3.2f, d = 4.6f - tier * 1.2f;
                for (int sd = -1; sd <= 1; sd += 2)
                    SP(s, cube, new Vector3(0f, y, sd * d * 0.42f), new Vector3(w, 0.3f, d), roof, new Vector3(sd * 26f, 0f, 0f));
                SP(s, cube, new Vector3(0f, y + d * 0.2f + 0.2f, 0f), new Vector3(w * 0.96f, 0.3f, 0.45f), gold, Vector3.zero);
                for (int sx = -1; sx <= 1; sx += 2)
                    for (int sz = -1; sz <= 1; sz += 2)
                        SP(s, cube, new Vector3(sx * w * 0.5f, y - 0.45f, sz * d * 0.82f), new Vector3(0.9f, 0.22f, 0.5f), roof, new Vector3(0f, sx * sz * 35f, sx * 18f));
            }
            // Hanging lanterns and the shrine's warm light.
            for (int sx = -1; sx <= 1; sx += 2)
                SP(s, MeshFactory.SmoothSphere(), new Vector3(sx * 2.8f, 3.3f, -3.3f), new Vector3(0.6f, 0.75f, 0.6f), shrinePaper, Vector3.zero);
            var lg = new GameObject("ShrineLight");
            lg.transform.SetParent(s, false);
            lg.transform.localPosition = new Vector3(0f, 2.6f, -4.5f);
            shrineLight = lg.AddComponent<Light>();
            shrineLight.type = LightType.Point;
            shrineLight.color = new Color(1f, 0.66f, 0.35f);
            shrineLight.range = 14f;
            // A torii between the altar and the shrine.
            float tz = -5f;
            for (int sx = -1; sx <= 1; sx += 2) SP(s, cyl, new Vector3(sx * 2.8f, 2.6f, tz), new Vector3(0.45f, 2.6f, 0.45f), red, Vector3.zero);
            SP(s, cube, new Vector3(0f, 5.35f, tz), new Vector3(7.6f, 0.42f, 0.7f), red, Vector3.zero);
            SP(s, cube, new Vector3(0f, 5.62f, tz), new Vector3(8.2f, 0.18f, 0.8f), roof, Vector3.zero);
            SP(s, cube, new Vector3(0f, 4.4f, tz), new Vector3(6.4f, 0.3f, 0.35f), red, Vector3.zero);
            SP(s, cube, new Vector3(0f, 4.85f, tz - 0.05f), new Vector3(0.9f, 0.7f, 0.12f), gold, Vector3.zero);

            // The moon (appears during the rite) and the energy swirl rings.
            moon = new GameObject("Moon").transform;
            moon.SetParent(root, false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), moon, Vector3.zero, Vector3.one, MaterialFactory.Toon(new Color(1f, 0.97f, 0.88f), 0f, new Color(0.95f, 0.9f, 0.78f)), false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), moon, Vector3.zero, Vector3.one * 1.35f, MaterialFactory.Additive(new Color(0.8f, 0.85f, 1f, 0.22f)), false);
            moon.gameObject.SetActive(false);
            swirl = new GameObject("Swirl").transform;
            swirl.SetParent(root, false);
            swirl.position = AltarPos;
            for (int i = 0; i < 3; i++)
                MeshFactory.MeshObject(MeshFactory.Ring(0.93f - i * 0.02f), swirl, Vector3.zero, Vector3.one, MaterialFactory.Additive(new Color(0.5f, 0.7f, 1f, 0.4f)), false);
            swirl.gameObject.SetActive(false);
        }

        static Transform SP(Transform parent, Mesh m, Vector3 p, Vector3 sc, Material mat, Vector3 e)
        {
            var go = MeshFactory.MeshObject(m, parent, p, sc, mat);
            go.transform.localRotation = Quaternion.Euler(e);
            return go.transform;
        }

        void EnsureWorld()
        {
            if (world != null) return;
            world = new GameObject("SummonWorld");
            world.transform.SetParent(transform, false);
            var root = world.transform;

            var stone = MaterialFactory.Toon(new Color(0.18f, 0.17f, 0.22f), 0.02f);
            var stoneDark = MaterialFactory.Toon(new Color(0.1f, 0.09f, 0.13f), 0.02f);
            MeshFactory.MeshObject(MeshFactory.Disc(), root, new Vector3(0f, -0.02f, 0f), new Vector3(60f, 1f, 60f), stoneDark, false);
            var plinth = MeshFactory.Primitive(PrimitiveType.Cylinder, root, AltarPos + Vector3.up * 0.1f, new Vector3(7f, 0.1f, 7f), stone);
            plinth.GetComponent<Renderer>().receiveShadows = true;
            MeshFactory.Primitive(PrimitiveType.Cylinder, root, AltarPos + Vector3.up * 0.22f, new Vector3(5f, 0.05f, 5f), stoneDark);

            circleMat = MaterialFactory.Additive(Color.white);
            runeMat = MaterialFactory.Additive(Color.white);
            glowMat = MaterialFactory.Additive(Color.white, true);
            circle = MeshFactory.MeshObject(MeshFactory.Ring(0.9f), root, AltarPos + Vector3.up * 0.3f, new Vector3(4.6f, 1f, 4.6f), circleMat, false).transform;
            runes = new GameObject("Runes").transform;
            runes.SetParent(root, false);
            runes.position = AltarPos + Vector3.up * 0.31f;
            for (int i = 0; i < 12; i++)
            {
                var rune = MeshFactory.MeshObject(MeshFactory.Sector(14f, 0.7f), runes, Vector3.zero, new Vector3(3.6f, 1f, 3.6f), runeMat, false);
                rune.transform.localRotation = Quaternion.Euler(0f, i * 30f, 0f);
            }
            MeshFactory.MeshObject(MeshFactory.Ring(0.55f), root, AltarPos + Vector3.up * 0.305f, new Vector3(2.2f, 1f, 2.2f), runeMat, false);
            var glow = MeshFactory.MeshObject(MeshFactory.Disc(), root, AltarPos + Vector3.up * 0.28f, new Vector3(7f, 1f, 7f), glowMat, false);
            glow.GetComponent<Renderer>().receiveShadows = false;

            // Ring of torii-like pillars with braziers.
            var red = MaterialFactory.Toon(new Color(0.55f, 0.1f, 0.12f), 0.02f);
            for (int i = 0; i < 8; i++)
            {
                Vector3 p = Quaternion.Euler(0f, i * 45f + 22.5f, 0f) * Vector3.forward * 9f;
                var pl = MeshFactory.Primitive(PrimitiveType.Cylinder, root, p + Vector3.up * 2.5f, new Vector3(0.6f, 2.5f, 0.6f), red);
                pillars.Add(pl.transform);
                MeshFactory.Primitive(PrimitiveType.Cube, root, p + Vector3.up * 5.1f, new Vector3(1.4f, 0.3f, 1.4f), stoneDark);
                EnvFx.Fire(root, p + Vector3.up * 5.3f, 0.5f, i % 2 == 0);
                var bl = new GameObject("BrazierLight");
                bl.transform.SetParent(root, false);
                bl.transform.position = p + Vector3.up * 5.8f;
                var l = bl.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 7f;
                l.color = new Color(1f, 0.6f, 0.3f);
                l.intensity = 0.8f;
                brazierLights.Add(l);
            }
            CloudDrift.CreateLayer(root, Vector3.zero, 6, 9f, new Color(0.4f, 0.3f, 0.6f, 0.35f), 30f);
            var stars = EnvFx.Weather(root, Vector3.zero, "embers", 30f);
            if (stars != null) stars.transform.position = new Vector3(0f, 6f, 0f);

            var lg = new GameObject("AltarLight");
            lg.transform.SetParent(root, false);
            lg.transform.position = AltarPos + Vector3.up * 2.5f;
            altarLight = lg.AddComponent<Light>();
            altarLight.type = LightType.Point;
            altarLight.range = 12f;

            portalMat = MaterialFactory.Additive(Color.white, true);
            portal = MeshFactory.MeshObject(MeshFactory.Ring(0.6f), root, AltarPos + new Vector3(0f, 1.7f, 0.6f), Vector3.one, portalMat, false).transform;
            portal.localRotation = Quaternion.Euler(-90f, 0f, 0f);
            portal.gameObject.SetActive(false);

            // The sealed orb that the player cracks open.
            orb = new GameObject("SealOrb").transform;
            orb.SetParent(root, false);
            orbCore = MaterialFactory.Toon(new Color(0.6f, 0.75f, 1f), 0f, new Color(0.35f, 0.55f, 1f));
            orbGlow = MaterialFactory.Additive(new Color(0.35f, 0.55f, 1f, 0.3f));
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), orb, Vector3.zero, Vector3.one * 0.95f, orbCore, false);
            MeshFactory.MeshObject(MeshFactory.SmoothSphere(), orb, Vector3.zero, Vector3.one * 1.8f, orbGlow, false);
            var band = MeshFactory.MeshObject(MeshFactory.Ring(0.85f), orb, Vector3.zero, Vector3.one * 1.5f, orbGlow, false);
            band.transform.localRotation = Quaternion.Euler(70f, 0f, 0f);
            orb.gameObject.SetActive(false);

            circleBase = circle.localScale;
            runesBase = runes.localScale;
            BuildShrine(root);

            heroHolder = new GameObject("SummonHero").transform;
            heroHolder.SetParent(transform, false);
            heroHolder.position = AltarPos + Vector3.up * 0.3f;
            heroHolder.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }

    /// <summary>A shard of a broken summon seal: flies out, tumbles, falls and fades.</summary>
    public class SummonShard : MonoBehaviour
    {
        public Vector3 velocity;
        Vector3 spin;
        float age;
        Vector3 baseScale;

        void Start() { spin = Random.onUnitSphere * 540f; baseScale = transform.localScale; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            age += dt;
            velocity += Vector3.down * 12f * dt;
            transform.position += velocity * dt;
            if (transform.position.y < 0.35f) { var p = transform.position; p.y = 0.35f; transform.position = p; velocity = new Vector3(velocity.x * 0.5f, -velocity.y * 0.3f, velocity.z * 0.5f); }
            transform.Rotate(spin * dt, Space.World);
            if (age > 1.2f) transform.localScale = baseScale * Mathf.Clamp01(1f - (age - 1.2f) / 0.5f);
            if (age > 1.7f) Destroy(gameObject);
        }
    }
}
