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
    public class SummonStage : MonoBehaviour
    {
        public enum SummonPhase { Idle, Gather, Charging, Seal, Silhouette, Reveal, Summary }

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
            if (Results.Count > 1) yield return Gather(audio);
            for (CurrentIndex = 0; CurrentIndex < Results.Count && !skipAll; CurrentIndex++)
            {
                Current = Results[CurrentIndex];
                if (CurrentIndex < ringOrbs.Count && ringOrbs[CurrentIndex] != null) ringOrbs[CurrentIndex].gameObject.SetActive(false);
                yield return RevealOne(Current, audio, Results.Count > 1);
            }
            ClearRing();
            HideOrb();
            Phase = SummonPhase.Summary;
            PhaseStart = Time.unscaledTime;
            ClearShown();
            // Line up the best pull for the summary backdrop.
            SummonSystem.Result best = Results[0];
            foreach (var r in Results) if (r.rarity > best.rarity) best = r;
            ShowHero(best.def, false);
            SetCircle(RarityInfo.Color(best.rarity), 1f);
            ApplyLighting(0.8f);
            if (CameraController.Instance != null) CameraController.Instance.Cut(new Vector3(-1.2f, 1.9f, -5.5f), new Vector3(-1.2f, 1.3f, 0f));
            routine = null;
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
            SealNeeded = multi && rarity < 4 ? 0 : 3;
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
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 45f;
            RenderSettings.ambientLight = new Color(0.2f, 0.18f, 0.3f) * brightness;
            if (Camera.main != null) Camera.main.backgroundColor = new Color(0.02f, 0.01f, 0.05f);
            if (RenderSettings.sun != null)
            {
                RenderSettings.sun.color = new Color(0.7f, 0.7f, 1f);
                RenderSettings.sun.intensity = 0.9f * brightness;
                RenderSettings.sun.transform.rotation = Quaternion.Euler(60f, -20f, 0f);
            }
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
