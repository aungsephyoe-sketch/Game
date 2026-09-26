using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The Summoning Shrine: a dark stone altar in a starry void. Every summon is staged as an event: the lights die,
    /// energy gathers, the circle ignites (and may "upgrade" its colour to tease a higher rarity), lightning strikes
    /// for Legendary+, a portal opens, a silhouette steps out, then the full reveal with a rarity-specific finale.
    /// The UI reads <see cref="Phase"/>/<see cref="Current"/> to draw name cards and the results grid.
    /// </summary>
    public class SummonStage : MonoBehaviour
    {
        public enum SummonPhase { Idle, Charging, Silhouette, Reveal, Summary }

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
            gameObject.SetActive(false);
        }

        public bool Busy { get { return routine != null; } }

        /// <summary>Tap: finish the current reveal and move to the next.</summary>
        public void Advance() { advance = true; }

        /// <summary>Skip straight to the results grid.</summary>
        public void SkipAll() { skipAll = true; advance = true; }

        /// <summary>Close the results grid and return to the banner.</summary>
        public void CloseSummary()
        {
            Phase = SummonPhase.Idle;
            ClearShown();
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
            for (CurrentIndex = 0; CurrentIndex < Results.Count && !skipAll; CurrentIndex++)
            {
                Current = Results[CurrentIndex];
                yield return RevealOne(Current, audio, Results.Count > 1);
            }
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

            // 1. Darkness and gathering energy.
            Phase = SummonPhase.Charging;
            PhaseStart = Time.unscaledTime;
            var cam = CameraController.Instance;
            if (cam != null) { cam.Cut(new Vector3(0f, 7f, -9f), AltarPos + Vector3.up * 0.5f); cam.Dolly(new Vector3(0f, 3.2f, -6.5f), AltarPos + Vector3.up * 1f, 1.6f); }
            ApplyLighting(0.15f);
            Color start = new Color(0.35f, 0.55f, 1f);
            SetCircle(start, 0.5f);
            if (audio != null) audio.Play("charge", 0.7f);
            float charge = multi ? 0.9f : 1.5f;
            float e = 0f;
            while (e < charge && !advance)
            {
                e += Time.unscaledDeltaTime;
                if (Random.value < 0.35f)
                {
                    Vector3 o = AltarPos + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(2f, 4f);
                    VFX.Breath(o, start, 3);
                }
                SetCircle(start, 0.5f + e / charge * 0.5f);
                yield return null;
            }

            // 2. Colour upgrades: the circle climbs through the tiers up to the pulled rarity.
            for (int tier = 4; tier <= rarity && !advance; tier++)
            {
                Color c = RarityInfo.Color(tier);
                SetCircle(c, 1.2f);
                VFX.Shockwave(AltarPos, 3.5f, c, 0.4f);
                if (audio != null) audio.Play(tier >= 6 ? "ultimate" : "skill", 0.55f);
                if (cam != null) cam.Shake(0.1f + 0.05f * (tier - 3));
                yield return Wait(multi ? 0.25f : 0.45f);
            }
            SetCircle(target, 1.4f);

            // 3. Lightning for Legendary and Mythic.
            if (rarity >= 6 && !advance)
            {
                for (int i = 0; i < (rarity == 7 ? 7 : 4); i++)
                {
                    Vector3 o = AltarPos + Quaternion.Euler(0f, Random.Range(0f, 360f), 0f) * Vector3.forward * Random.Range(1.5f, 5f);
                    VFX.Pillar(o, Color.Lerp(target, Color.white, 0.5f), 14f, 0.15f);
                    VFX.ImpactLight(o + Vector3.up * 3f, target, 12f, 0.15f);
                    if (audio != null) audio.Play("thud", 0.5f);
                    if (cam != null) cam.Shake(0.25f);
                    yield return Wait(0.12f);
                }
            }

            // 4. Portal and silhouette.
            Phase = SummonPhase.Silhouette;
            PhaseStart = Time.unscaledTime;
            portal.gameObject.SetActive(true);
            portalMat.color = new Color(target.r, target.g, target.b, 0.85f);
            e = 0f;
            float open = rarity >= 6 ? 0.8f : 0.45f;
            while (e < open && !advance)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, e / open);
                portal.localScale = new Vector3(2.4f * k, 3.4f * k, 1f);
                yield return null;
            }
            portal.localScale = new Vector3(2.4f, 3.4f, 1f);
            ShowHero(r.def, true);
            if (cam != null) cam.Dolly(new Vector3(0f, 1.8f, -4.6f), heroHolder.position + Vector3.up * 1.2f, rarity >= 6 ? 1.2f : 0.6f);
            yield return Wait(rarity >= 6 ? 1.1f : multi ? 0.35f : 0.6f);

            // 5. Reveal.
            Phase = SummonPhase.Reveal;
            PhaseStart = Time.unscaledTime;
            SetSilhouette(false);
            portal.gameObject.SetActive(false);
            ApplyLighting(rarity >= 6 ? 1.1f : 0.8f);
            VFX.Pillar(heroHolder.position, target, 9f, 0.8f);
            VFX.Shockwave(heroHolder.position, 5f, target, 0.6f);
            VFX.Breath(heroHolder.position, target, 40 + (rarity - 3) * 20);
            VFX.ImpactLight(heroHolder.position + Vector3.up * 1.5f, target, 10f, 0.8f);
            if (shown != null) { shown.Flash(Color.white, 1f); shown.Victory(); }
            if (audio != null) audio.Play(rarity >= 6 ? "ultimate" : "victory", 0.8f);
            GameEvents.RaiseImpact(rarity >= 6 ? 1f : 0.5f);
            if (rarity == 7)
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

            heroHolder = new GameObject("SummonHero").transform;
            heroHolder.SetParent(transform, false);
            heroHolder.position = AltarPos + Vector3.up * 0.3f;
            heroHolder.rotation = Quaternion.Euler(0f, 180f, 0f);
        }
    }
}
