using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Plays a <see cref="Cutscene"/> in-engine: builds the location set, spawns actors (heroes, NPCs, demons),
    /// drives the camera (cuts, dollies, orbits), dialogue with typewriter text, title cards, fades, effects,
    /// sky changes and music. Tap to advance dialogue; SKIP ends the scene. The UI reads its public state.
    /// </summary>
    public class CutscenePlayer : MonoBehaviour
    {
        public static CutscenePlayer Current { get; private set; }

        // ---- UI-facing state
        public string Speaker { get; private set; }
        public string FullText { get; private set; }
        public int VisibleChars { get; private set; }
        public string TitleText { get; private set; }
        public string TitleSub { get; private set; }
        public float TitleStart { get; private set; }
        public float TitleDuration { get; private set; }
        public float FadeAlpha { get; private set; }
        public bool Letterbox { get; private set; }

        Cutscene scene;
        System.Action onDone;
        GameObject set;
        Transform actorRoot;
        readonly Dictionary<string, Transform> actors = new Dictionary<string, Transform>();
        readonly Dictionary<string, CharacterVisual> visuals = new Dictionary<string, CharacterVisual>();
        bool advance;
        bool finished;
        float typeStart;

        public static CutscenePlayer Play(string id, System.Action done)
        {
            var sc = CutsceneDatabase.Get(id);
            if (sc == null)
            {
                if (done != null) done();
                return null;
            }
            if (Current != null) Current.Finish();
            var go = new GameObject("[Cutscene " + id + "]");
            var p = go.AddComponent<CutscenePlayer>();
            p.scene = sc;
            p.onDone = done;
            p.FadeAlpha = 1f;
            p.Letterbox = true;
            Current = p;
            p.StartCoroutine(p.Run());
            return p;
        }

        /// <summary>Tap / click / key: completes the typewriter or advances to the next line.</summary>
        public void Advance()
        {
            if (FullText != null && VisibleChars < FullText.Length) VisibleChars = FullText.Length;
            else advance = true;
        }

        public void Skip()
        {
            if (GameManager.Instance != null) GameManager.Instance.Audio.Play("click", 0.6f);
            Finish();
        }

        void Update()
        {
            if (FullText != null && VisibleChars < FullText.Length)
                VisibleChars = Mathf.Min(FullText.Length, Mathf.FloorToInt((Time.unscaledTime - typeStart) * 55f));
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.J)) Advance();
            if (Input.GetKeyDown(KeyCode.Escape)) Skip();
        }

        IEnumerator Run()
        {
            actorRoot = new GameObject("Actors").transform;
            actorRoot.SetParent(transform, false);
            foreach (var step in scene.steps)
            {
                if (finished) yield break;
                yield return Execute(step);
            }
            Speaker = null;
            FullText = null;
            // Hold on the last title card briefly, then end.
            yield return new WaitForSecondsRealtime(0.3f);
            Finish();
        }

        IEnumerator Execute(CutsceneStep s)
        {
            var cam = CameraController.Instance;
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            switch (s.kind)
            {
                case StepKind.Env:
                    ClearActors();
                    if (set != null) Destroy(set);
                    set = new GameObject("Set");
                    set.transform.SetParent(transform, false);
                    ArenaBuilder.Build(s.theme, set.transform, false, s.theme.GetHashCode());
                    break;

                case StepKind.Actor:
                    SpawnActor(s.a, s.b, s.v1, s.f1, s.f2 <= 0f ? 1f : s.f2);
                    break;

                case StepKind.Remove:
                    Transform t;
                    if (actors.TryGetValue(s.a, out t) && t != null) Destroy(t.gameObject);
                    actors.Remove(s.a);
                    visuals.Remove(s.a);
                    break;

                case StepKind.Cut:
                    if (cam != null) cam.Cut(s.v1, s.v2);
                    break;

                case StepKind.Dolly:
                    if (cam != null) cam.Dolly(s.v1, s.v2, s.f1);
                    if (s.wait) yield return new WaitForSecondsRealtime(s.f1);
                    break;

                case StepKind.Orbit:
                    if (s.wait) yield return OrbitRoutine(s);
                    else StartCoroutine(OrbitRoutine(s));
                    break;

                case StepKind.Say:
                    Speaker = s.a;
                    FullText = s.b;
                    VisibleChars = 0;
                    typeStart = Time.unscaledTime;
                    advance = false;
                    if (audio != null) audio.Play("click", 0.25f);
                    float auto = Mathf.Max(3f, s.b.Length * 0.06f + 1.8f);
                    float t0 = Time.unscaledTime;
                    while (!advance && Time.unscaledTime - t0 < auto && !finished) yield return null;
                    break;

                case StepKind.Move:
                    if (s.wait) yield return MoveRoutine(s.a, s.v1, s.f1);
                    else StartCoroutine(MoveRoutine(s.a, s.v1, s.f1));
                    break;

                case StepKind.Face:
                    Transform a, b;
                    if (actors.TryGetValue(s.a, out a) && actors.TryGetValue(s.b, out b))
                    {
                        Vector3 d = b.position - a.position;
                        d.y = 0f;
                        if (d.sqrMagnitude > 0.01f) a.rotation = Quaternion.LookRotation(d);
                    }
                    break;

                case StepKind.Anim:
                    PlayAnim(s.a, s.b);
                    break;

                case StepKind.Fx:
                    PlayFx(s.a, s.v1, s.color);
                    break;

                case StepKind.Title:
                    TitleText = s.a;
                    TitleSub = s.b;
                    TitleStart = Time.unscaledTime;
                    TitleDuration = s.f1;
                    Speaker = null;
                    FullText = null;
                    if (audio != null) audio.Play("switch", 0.5f);
                    yield return new WaitForSecondsRealtime(s.f1);
                    break;

                case StepKind.Fade:
                    yield return FadeRoutine(s.f1, s.f2);
                    break;

                case StepKind.Music:
                    if (audio != null) audio.SetMusicState((MusicState)System.Enum.Parse(typeof(MusicState), s.a));
                    break;

                case StepKind.Sfx:
                    if (audio != null) audio.Play(s.a, 0.9f);
                    break;

                case StepKind.Wait:
                    yield return new WaitForSecondsRealtime(s.f1);
                    break;

                case StepKind.Shake:
                    if (cam != null) cam.Shake(s.f1);
                    break;

                case StepKind.Sky:
                    StartCoroutine(SkyRoutine(s.color, new Color(s.v1.x, s.v1.y, s.v1.z), s.f1));
                    break;
            }
        }

        void SpawnActor(string key, string defId, Vector3 pos, float yaw, float scale)
        {
            Transform old;
            if (actors.TryGetValue(key, out old) && old != null) Destroy(old.gameObject);
            var go = new GameObject("Actor_" + key);
            go.transform.SetParent(actorRoot, false);
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = Vector3.one * scale;
            CharacterVisual v = null;
            var hero = GameDatabase.GetCharacter(defId);
            if (hero != null) v = CharacterVisual.BuildHero(hero, go.transform);
            else
            {
                var enemy = GameDatabase.GetEnemy(defId);
                if (enemy != null) v = CharacterVisual.BuildDemon(enemy, go.transform);
            }
            actors[key] = go.transform;
            if (v != null) visuals[key] = v;
        }

        void PlayAnim(string key, string anim)
        {
            CharacterVisual v;
            Transform t;
            if (!visuals.TryGetValue(key, out v) || !actors.TryGetValue(key, out t) || v == null) return;
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            switch (anim)
            {
                case "swing": v.Attack(Random.Range(0, 4), 0.14f); if (audio != null) audio.Play("slash", 0.5f); break;
                case "spin": v.Spin(0.3f); if (audio != null) audio.Play("slash", 0.5f); break;
                case "heavy": v.HeavyAttack(0.2f); if (audio != null) audio.Play("heavy", 0.6f); break;
                case "victory": v.Victory(); break;
                case "defeat": v.Defeat(); break;
                case "knockdown": v.Knockdown(); if (audio != null) audio.Play("thud", 0.6f); break;
                case "getup": v.GetUp(0.5f); break;
                case "hit": v.Hit(-t.forward); break;
                case "charge": StartCoroutine(GlowRoutine(v, new Color(0.5f, 0.8f, 1f), 1.5f)); break;
                case "glow": StartCoroutine(GlowRoutine(v, new Color(1f, 0.8f, 0.35f), 2f)); break;
                case "fade": StartCoroutine(DissolveRoutine(key, t)); break;
            }
        }

        void PlayFx(string kind, Vector3 pos, Color color)
        {
            var audio = GameManager.Instance != null ? GameManager.Instance.Audio : null;
            switch (kind)
            {
                case "fire":
                    if (set != null) EnvFx.Fire(set.transform, pos, 1.5f);
                    break;
                case "explosion":
                    VFX.BurstDisc(pos, 5f, color, 0.6f);
                    VFX.Shockwave(pos, 7f, color, 0.6f);
                    VFX.Smoke(pos, new Color(0.2f, 0.18f, 0.18f, 0.7f), 30);
                    VFX.ImpactLight(pos + Vector3.up, color, 10f, 0.5f);
                    if (audio != null) audio.Play("slam", 1f);
                    GameEvents.RaiseImpact(0.8f);
                    break;
                case "darkpulse":
                    VFX.Shockwave(pos, 6f, color, 0.8f);
                    VFX.Pillar(pos, color * 0.8f, 8f, 0.8f);
                    VFX.Breath(pos, color, 50);
                    VFX.ImpactLight(pos, color, 12f, 0.8f);
                    break;
                case "lightning":
                    for (int i = 0; i < 4; i++) VFX.Pillar(pos + Random.insideUnitSphere * 3f, new Color(1f, 0.95f, 0.5f), 12f, 0.2f);
                    VFX.ImpactLight(pos, Color.white, 14f, 0.25f);
                    if (audio != null) audio.Play("crit", 1f);
                    break;
                case "dawn":
                    VFX.Pillar(pos, color, 12f, 1.2f);
                    VFX.BurstDisc(pos, 6f, color, 1f);
                    VFX.Shockwave(pos, 9f, color, 1f);
                    VFX.Breath(pos, color, 90);
                    VFX.ImpactLight(pos + Vector3.up, color, 14f, 1.2f);
                    GameEvents.RaiseImpact(1f);
                    break;
                case "smoke":
                    VFX.Smoke(pos, new Color(0.25f, 0.22f, 0.22f, 0.7f), 25);
                    break;
                default:
                    VFX.Slash(pos, Vector3.forward, 2.5f, 160f, 0f, color, 0.3f);
                    break;
            }
        }

        IEnumerator GlowRoutine(CharacterVisual v, Color c, float seconds)
        {
            float t = 0f;
            while (t < seconds && v != null)
            {
                t += Time.unscaledDeltaTime;
                v.SetCharge(Mathf.Sin(Mathf.Clamp01(t / seconds) * Mathf.PI), c);
                yield return null;
            }
            if (v != null) v.SetCharge(0f, c);
        }

        IEnumerator DissolveRoutine(string key, Transform t)
        {
            VFX.Smoke(t.position, new Color(0.15f, 0.1f, 0.12f, 0.8f), 30);
            VFX.Breath(t.position, new Color(1f, 0.5f, 0.3f), 40);
            Vector3 s0 = t.localScale;
            float k = 0f;
            while (k < 1f && t != null)
            {
                k += Time.unscaledDeltaTime * 0.9f;
                t.localScale = s0 * (1f - k);
                yield return null;
            }
            if (t != null) Destroy(t.gameObject);
            actors.Remove(key);
            visuals.Remove(key);
        }

        IEnumerator MoveRoutine(string key, Vector3 to, float seconds)
        {
            Transform t;
            if (!actors.TryGetValue(key, out t) || t == null) yield break;
            CharacterVisual v;
            visuals.TryGetValue(key, out v);
            Vector3 from = t.position;
            Vector3 dir = to - from;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.01f) t.rotation = Quaternion.LookRotation(dir);
            float speed = dir.magnitude / Mathf.Max(0.01f, seconds);
            float e = 0f;
            while (e < seconds && t != null)
            {
                e += Time.unscaledDeltaTime;
                t.position = Vector3.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / seconds));
                if (v != null) v.SetMoving(Mathf.Clamp01(speed / 4f), speed > 5f);
                yield return null;
            }
            if (v != null) v.SetMoving(0f);
        }

        IEnumerator OrbitRoutine(CutsceneStep s)
        {
            Transform t;
            if (!actors.TryGetValue(s.a, out t) || CameraController.Instance == null) yield break;
            float from = s.v1.x, to = s.v1.y, seconds = Mathf.Max(0.1f, s.v1.z);
            float e = 0f;
            while (e < seconds && t != null && !finished)
            {
                e += Time.unscaledDeltaTime;
                float yaw = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, e / seconds));
                Vector3 look = t.position + Vector3.up * s.f2;
                Vector3 pos = look + Quaternion.Euler(8f, yaw, 0f) * Vector3.forward * s.f1;
                CameraController.Instance.Cut(pos, look);
                yield return null;
            }
        }

        IEnumerator FadeRoutine(float target, float seconds)
        {
            float start = FadeAlpha;
            if (seconds <= 0f) { FadeAlpha = target; yield break; }
            float e = 0f;
            while (e < seconds)
            {
                e += Time.unscaledDeltaTime;
                FadeAlpha = Mathf.Lerp(start, target, e / seconds);
                yield return null;
            }
            FadeAlpha = target;
        }

        IEnumerator SkyRoutine(Color sky, Color fog, float seconds)
        {
            var cam = Camera.main;
            Color s0 = cam != null ? cam.backgroundColor : sky;
            Color f0 = RenderSettings.fogColor;
            float e = 0f;
            while (e < seconds)
            {
                e += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(e / Mathf.Max(0.01f, seconds));
                if (cam != null) cam.backgroundColor = Color.Lerp(s0, sky, k);
                RenderSettings.fogColor = Color.Lerp(f0, fog, k);
                if (RenderSettings.sun != null) RenderSettings.sun.color = Color.Lerp(RenderSettings.sun.color, Color.Lerp(sky, Color.white, 0.5f), k * 0.1f);
                yield return null;
            }
        }

        void ClearActors()
        {
            foreach (var kv in actors) if (kv.Value != null) Destroy(kv.Value.gameObject);
            actors.Clear();
            visuals.Clear();
        }

        void Finish()
        {
            if (finished) return;
            finished = true;
            StopAllCoroutines();
            if (CameraController.Instance != null) CameraController.Instance.EndScripted();
            var done = onDone;
            onDone = null;
            if (Current == this) Current = null;
            Destroy(gameObject);
            if (done != null) done();
        }
    }
}
