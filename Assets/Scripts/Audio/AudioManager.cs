using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    public enum MusicState { None, Menu, Explore, Combat, Boss, Victory, Defeat, Story, Map, Summon }

    /// <summary>
    /// Sound effects and adaptive music, all synthesised at startup (noise bursts, sweeps, Karplus-Strong
    /// plucks, taiko drums) so the game ships without audio files. Music crossfades between states
    /// (menu → explore → combat → boss → victory/defeat), a heartbeat layer fades in at low HP, and music
    /// ducks under ultimates. Swap Generate() for licensed/commissioned AudioClips later – callers only use
    /// Play("id") and SetMusicState().
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int Rate = 22050;
        const float CrossfadeSeconds = 1.6f;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        readonly Dictionary<MusicState, AudioClip> tracks = new Dictionary<MusicState, AudioClip>();
        AudioSource sfx;
        AudioSource musicA, musicB, heartbeat;
        AudioSource currentMusic;
        MusicState state = MusicState.None;
        float duckUntil;
        float fade = 1f;
        System.Random rng = new System.Random(1234);

        public float SfxVolume { get { return GameSettings.SfxVolume; } }
        public float MusicVolume { get { return GameSettings.MusicVolume * 0.5f; } }
        public MusicState State { get { return state; } }

        void Awake()
        {
            sfx = gameObject.AddComponent<AudioSource>();
            sfx.playOnAwake = false;
            musicA = MakeMusicSource();
            musicB = MakeMusicSource();
            heartbeat = MakeMusicSource();
            Generate();
            heartbeat.clip = clips["heartbeat"];
            heartbeat.volume = 0f;
            heartbeat.Play();
            GameEvents.UltimateStarted += OnUltimate;
            SetMusicState(MusicState.Menu);
        }

        void OnDestroy()
        {
            GameEvents.UltimateStarted -= OnUltimate;
        }

        AudioSource MakeMusicSource()
        {
            var a = gameObject.AddComponent<AudioSource>();
            a.loop = true;
            a.playOnAwake = false;
            a.volume = 0f;
            a.ignoreListenerPause = true;
            return a;
        }

        void OnUltimate(PlayerCharacter pc, AbilityDefinition ab)
        {
            duckUntil = Time.unscaledTime + 1.6f;
        }

        public void Play(string id, float volume = 1f)
        {
            AudioClip clip;
            if (!clips.TryGetValue(id, out clip)) return;
            // Throttle identical sounds so 10 simultaneous hits don't clip.
            float last;
            if (lastPlayed.TryGetValue(id, out last) && Time.unscaledTime - last < 0.035f) return;
            lastPlayed[id] = Time.unscaledTime;
            sfx.pitch = 1f;
            sfx.PlayOneShot(clip, volume * SfxVolume);
        }

        /// <summary>Kept for older callers: true = combat, false = menu.</summary>
        public void PlayMusic(bool battle)
        {
            SetMusicState(battle ? MusicState.Explore : MusicState.Menu);
        }

        public void SetMusicState(MusicState next)
        {
            if (next == state) return;
            state = next;
            AudioClip clip;
            if (!tracks.TryGetValue(next, out clip))
            {
                // Silence (emotional beats): let everything fade out.
                currentMusic = null;
                return;
            }
            var target = currentMusic == musicA ? musicB : musicA;
            target.clip = clip;
            target.loop = next != MusicState.Victory && next != MusicState.Defeat;
            target.volume = 0f;
            target.Play();
            currentMusic = target;
            fade = 0f;
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            fade = Mathf.MoveTowards(fade, 1f, dt / CrossfadeSeconds);
            float duck = Time.unscaledTime < duckUntil ? 0.35f : 1f;
            float vol = MusicVolume * duck;
            if (currentMusic != null) currentMusic.volume = Mathf.Lerp(currentMusic.volume, vol * fade, 1f - Mathf.Exp(-dt * 8f));
            foreach (var other in new[] { musicA, musicB })
            {
                if (other == currentMusic) continue;
                other.volume = Mathf.MoveTowards(other.volume, 0f, dt * Mathf.Max(0.05f, MusicVolume) / CrossfadeSeconds * 1.5f);
                if (other.volume <= 0f && other.isPlaying) other.Stop();
            }

            // Low-health layer: heartbeat fades in under 30% HP of the active slayer.
            float danger = 0f;
            var b = BattleController.Current;
            if (b != null && b.Team != null && b.Team.Active != null && b.Team.Active.IsAlive && !b.Finished)
                danger = Mathf.Clamp01((0.3f - b.Team.Active.Health.Normalized) / 0.3f);
            heartbeat.volume = Mathf.MoveTowards(heartbeat.volume, danger * GameSettings.SfxVolume * 0.8f, dt * 0.8f);
            if (currentMusic != null && danger > 0f) currentMusic.pitch = Mathf.Lerp(1f, 0.97f, danger);
            else if (currentMusic != null) currentMusic.pitch = 1f;
        }

        // ------------------------------------------------------------------ Synthesis

        float Noise() { return (float)(rng.NextDouble() * 2.0 - 1.0); }

        static float[] Buffer(float seconds) { return new float[Mathf.Max(1, Mathf.RoundToInt(seconds * Rate))]; }

        AudioClip Make(string id, float[] data, float gain = 1f)
        {
            float peak = 0.0001f;
            foreach (var s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            float norm = gain / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= norm;
            var clip = AudioClip.Create(id, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clips[id] = clip;
            return clip;
        }

        /// <summary>Filtered noise with exponential decay. brightness 0..1 (1 = hiss, 0 = rumble).</summary>
        void AddNoise(float[] d, float start, float length, float amp, float decay, float brightness, float attack = 0.005f)
        {
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            float lp = 0f;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay);
                float x = Noise();
                lp += (x - lp) * Mathf.Lerp(0.05f, 0.9f, brightness);
                d[s0 + i] += lp * env * amp;
            }
        }

        /// <summary>Sine sweep from f0 to f1 with exponential decay.</summary>
        void AddTone(float[] d, float start, float length, float f0, float f1, float amp, float decay, float attack = 0.003f, bool square = false)
        {
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            float phase = 0f;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                float t = (float)i / Rate;
                float k = (float)i / n;
                float f = Mathf.Lerp(f0, f1, k);
                phase += 2f * Mathf.PI * f / Rate;
                float env = Mathf.Min(1f, t / attack) * Mathf.Exp(-t * decay);
                float v = Mathf.Sin(phase);
                if (square) v = Mathf.Sign(v) * 0.6f;
                d[s0 + i] += v * env * amp;
            }
        }

        /// <summary>Karplus-Strong plucked string (shamisen/koto-like).</summary>
        void AddPluck(float[] d, float start, float freq, float amp, float length = 0.9f, float damping = 0.996f)
        {
            int period = Mathf.Max(2, Mathf.RoundToInt(Rate / freq));
            var ring = new float[period];
            for (int i = 0; i < period; i++) ring[i] = Noise();
            int s0 = (int)(start * Rate), n = (int)(length * Rate);
            int idx = 0;
            for (int i = 0; i < n && s0 + i < d.Length; i++)
            {
                int next = (idx + 1) % period;
                float v = ring[idx];
                ring[idx] = (ring[idx] + ring[next]) * 0.5f * damping;
                idx = next;
                d[s0 + i] += v * amp;
            }
        }

        void AddTaiko(float[] d, float start, float amp)
        {
            AddTone(d, start, 0.45f, 110f, 45f, amp, 9f);
            AddNoise(d, start, 0.08f, amp * 0.35f, 40f, 0.3f);
        }

        void Generate()
        {
            float[] b;

            b = Buffer(0.16f); AddNoise(b, 0, 0.16f, 1f, 28f, 0.95f, 0.01f); AddTone(b, 0, 0.1f, 1400f, 500f, 0.25f, 30f); Make("slash", b, 0.7f);
            b = Buffer(0.14f); AddTone(b, 0, 0.14f, 160f, 55f, 1f, 22f); AddNoise(b, 0, 0.05f, 0.6f, 60f, 0.7f); Make("hit", b, 0.8f);
            b = Buffer(0.3f); AddTone(b, 0, 0.14f, 180f, 60f, 1f, 20f); AddTone(b, 0.01f, 0.3f, 1760f, 1760f, 0.35f, 12f); AddNoise(b, 0, 0.06f, 0.6f, 50f, 0.9f); Make("crit", b, 0.85f);
            b = Buffer(0.35f); AddNoise(b, 0, 0.3f, 1f, 12f, 0.8f, 0.02f); AddTone(b, 0.02f, 0.3f, 120f, 40f, 0.9f, 10f); Make("heavy", b, 0.9f);
            b = Buffer(0.22f); AddNoise(b, 0, 0.22f, 1f, 10f, 0.6f, 0.08f); Make("dodge", b, 0.5f);
            b = Buffer(0.7f); AddTone(b, 0, 0.7f, 880f, 880f, 0.6f, 5f); AddTone(b, 0.05f, 0.65f, 1320f, 1320f, 0.4f, 5f); AddTone(b, 0.1f, 0.6f, 1760f, 1760f, 0.3f, 6f); Make("perfect", b, 0.6f);
            b = Buffer(0.4f); AddTone(b, 0, 0.3f, 300f, 1200f, 0.5f, 6f, 0.02f); AddNoise(b, 0.05f, 0.35f, 0.7f, 8f, 0.9f, 0.05f); Make("skill", b, 0.6f);
            b = Buffer(1.6f); AddTone(b, 0, 1.6f, 55f, 55f, 0.8f, 1.5f, 0.05f); AddTone(b, 0, 0.9f, 200f, 1600f, 0.4f, 2f, 0.3f); AddNoise(b, 0.8f, 0.8f, 1f, 4f, 0.8f, 0.01f); AddTaiko(b, 0.85f, 1f); Make("ultimate", b, 0.9f);
            b = Buffer(0.35f); AddNoise(b, 0, 0.35f, 1f, 7f, 0.75f, 0.05f); AddTone(b, 0, 0.3f, 900f, 300f, 0.2f, 8f); Make("wave", b, 0.6f);
            b = Buffer(0.25f); AddTone(b, 0, 0.1f, 660f, 660f, 0.6f, 20f); AddTone(b, 0.08f, 0.15f, 990f, 990f, 0.6f, 18f); AddNoise(b, 0, 0.2f, 0.4f, 12f, 0.8f, 0.03f); Make("switch", b, 0.6f);
            b = Buffer(0.2f); AddNoise(b, 0, 0.2f, 1f, 14f, 0.35f, 0.02f); AddTone(b, 0, 0.18f, 140f, 90f, 0.5f, 10f, 0.01f, true); Make("enemyAttack", b, 0.6f);
            b = Buffer(0.5f); AddTone(b, 0, 0.5f, 90f, 35f, 1f, 7f); AddNoise(b, 0, 0.4f, 0.8f, 9f, 0.25f); Make("slam", b, 0.9f);
            b = Buffer(0.18f); AddTone(b, 0, 0.18f, 700f, 180f, 0.8f, 12f); Make("shoot", b, 0.5f);
            b = Buffer(0.3f); AddNoise(b, 0, 0.3f, 1f, 6f, 0.7f, 0.1f); Make("dash", b, 0.6f);
            b = Buffer(2.4f); AddTone(b, 0, 2.4f, 80f, 400f, 0.8f, 0.2f, 0.2f, true); AddNoise(b, 0, 2.4f, 0.3f, 0.3f, 0.2f, 0.5f); Make("charge", b, 0.6f);
            b = Buffer(1.3f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 85f + Mathf.Sin(t * 30f) * 8f;
                float saw = ((t * f) % 1f) * 2f - 1f;
                b[i] = saw * Mathf.Min(1f, t * 8f) * Mathf.Exp(-t * 2f);
            }
            AddNoise(b, 0, 1.3f, 0.5f, 2.5f, 0.3f, 0.1f);
            Make("roar", b, 0.8f);
            b = Buffer(0.4f); AddTone(b, 0, 0.4f, 420f, 70f, 0.7f, 6f); AddNoise(b, 0, 0.35f, 0.6f, 8f, 0.5f); Make("enemyDeath", b, 0.55f);
            b = Buffer(0.05f); AddTone(b, 0, 0.05f, 1300f, 1300f, 0.6f, 60f); Make("click", b, 0.4f);

            // Pentatonic "in" scale on D: D Eb G A Bb.
            float[] scale = { 293.66f, 311.13f, 392f, 440f, 466.16f, 587.33f, 622.25f, 783.99f };
            b = Buffer(1.8f);
            AddPluck(b, 0f, scale[0], 0.8f, 1.5f); AddPluck(b, 0.15f, scale[2], 0.8f, 1.5f); AddPluck(b, 0.3f, scale[3], 0.8f, 1.5f);
            AddPluck(b, 0.45f, scale[5], 0.9f, 1.3f); AddTaiko(b, 0.45f, 0.8f);
            Make("victory", b, 0.8f);
            b = Buffer(1.8f);
            AddPluck(b, 0f, scale[4], 0.8f, 1.5f); AddPluck(b, 0.3f, scale[1], 0.8f, 1.5f); AddPluck(b, 0.6f, scale[0] * 0.5f, 0.9f, 1.2f);
            Make("defeat", b, 0.7f);

            // New feedback sounds.
            b = Buffer(0.12f); AddNoise(b, 0, 0.08f, 0.6f, 40f, 0.2f); AddTone(b, 0, 0.1f, 90f, 60f, 0.5f, 30f); Make("step", b, 0.5f);
            b = Buffer(0.2f); AddTone(b, 0, 0.2f, 520f, 480f, 0.4f, 18f); AddNoise(b, 0, 0.05f, 0.3f, 50f, 0.9f); Make("guard", b, 0.4f);
            b = Buffer(0.35f); AddTone(b, 0, 0.35f, 1240f, 1180f, 0.6f, 10f); AddTone(b, 0, 0.3f, 1860f, 1800f, 0.35f, 12f); AddNoise(b, 0, 0.06f, 0.7f, 45f, 1f); Make("block", b, 0.7f);
            b = Buffer(0.9f); AddTone(b, 0, 0.9f, 1568f, 1568f, 0.6f, 4f); AddTone(b, 0, 0.8f, 2349f, 2349f, 0.4f, 5f); AddTone(b, 0, 0.7f, 3136f, 3136f, 0.25f, 6f); AddNoise(b, 0, 0.08f, 1f, 35f, 1f); AddTaiko(b, 0f, 0.6f); Make("parry", b, 0.9f);
            b = Buffer(0.4f); AddTone(b, 0, 0.4f, 70f, 35f, 1f, 9f); AddNoise(b, 0, 0.25f, 0.7f, 14f, 0.2f); Make("thud", b, 0.8f);
            b = Buffer(1.0f); AddTone(b, 0f, 0.18f, 60f, 40f, 1f, 18f); AddTone(b, 0.22f, 0.18f, 55f, 38f, 0.7f, 18f); Make("heartbeat", b, 0.9f);

            tracks[MusicState.Menu] = BuildMenuMusic(scale);
            tracks[MusicState.Explore] = BuildExploreMusic(scale);
            tracks[MusicState.Combat] = BuildBattleMusic(scale);
            tracks[MusicState.Boss] = BuildBossMusic(scale);
            tracks[MusicState.Story] = BuildStoryMusic(scale);
            tracks[MusicState.Map] = BuildMapMusic(scale);
            tracks[MusicState.Summon] = BuildSummonMusic();
            tracks[MusicState.Victory] = clips["victory"];
            tracks[MusicState.Defeat] = clips["defeat"];
        }

        AudioClip BuildBattleMusic(float[] scale)
        {
            const float bpm = 132f;
            float beat = 60f / bpm;
            int bars = 8;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 0, 2, 3, 2, 4, 3, 2, 1, 0, 2, 3, 5, 4, 3, 2, 3, 5, 4, 3, 2, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 4, 3 };
            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                AddTaiko(b, t0, 1f);
                AddTaiko(b, t0 + beat * 1.5f, 0.6f);
                AddTaiko(b, t0 + beat * 2f, 0.9f);
                AddTaiko(b, t0 + beat * 3f, 0.6f);
                AddTaiko(b, t0 + beat * 3.5f, 0.5f);
                for (int k = 0; k < 8; k++) AddNoise(b, t0 + k * beat * 0.5f, 0.04f, 0.12f, 60f, 1f);
                for (int n = 0; n < 4; n++)
                {
                    int note = melody[(bar * 4 + n) % melody.Length];
                    AddPluck(b, t0 + n * beat, scale[note], 0.35f, beat * 1.2f, 0.994f);
                }
                AddPluck(b, t0, scale[0] * 0.5f, 0.3f, beat * 3.5f, 0.998f);
            }
            MakeLoopable(b);
            var clip = AudioClip.Create("battle_music", b.Length, 1, Rate, false);
            Normalize(b, 0.6f);
            clip.SetData(b, 0);
            return clip;
        }

        AudioClip BuildMenuMusic(float[] scale)
        {
            const float bpm = 70f;
            float beat = 60f / bpm;
            int bars = 6;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 5, 3, 2, 0, 2, 3, 4, 3, 7, 5, 4, 3, 2, 3, 2, 0, 1, 2, 3, 2, 0, 1, 0, 0 };
            for (int i = 0; i < bars * 4; i++)
            {
                float t = i * beat;
                AddPluck(b, t, scale[melody[i % melody.Length]], 0.4f, beat * 2f, 0.997f);
                if (i % 4 == 0) AddPluck(b, t, scale[0] * 0.5f, 0.35f, beat * 4f, 0.999f);
                if (i % 8 == 0) AddTaiko(b, t, 0.5f);
            }
            MakeLoopable(b);
            Normalize(b, 0.5f);
            var clip = AudioClip.Create("menu_music", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        /// <summary>Calm, sparse koto over a drone – between waves.</summary>
        AudioClip BuildExploreMusic(float[] scale)
        {
            const float bpm = 84f;
            float beat = 60f / bpm;
            int bars = 6;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 0, 2, 3, 4, 3, 2, 5, 4, 3, 2, 0, 1 };
            for (int i = 0; i < bars * 4; i++)
            {
                float t = i * beat;
                if (i % 2 == 0) AddPluck(b, t, scale[melody[(i / 2) % melody.Length]], 0.35f, beat * 3f, 0.997f);
                if (i % 8 == 0) AddPluck(b, t, scale[0] * 0.5f, 0.3f, beat * 6f, 0.999f);
                if (i % 4 == 2) AddNoise(b, t, 0.05f, 0.06f, 50f, 1f);
            }
            AddTone(b, 0f, b.Length / (float)Rate, 73.4f, 73.4f, 0.12f, 0f, 1.5f);
            MakeLoopable(b);
            Normalize(b, 0.45f);
            var clip = AudioClip.Create("explore_music", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        /// <summary>Driving 150 bpm taiko + low saw drone + urgent shamisen – boss fights.</summary>
        AudioClip BuildBossMusic(float[] scale)
        {
            const float bpm = 150f;
            float beat = 60f / bpm;
            int bars = 8;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 0, 1, 0, 4, 3, 1, 0, 1, 5, 4, 3, 1, 0, 1, 2, 1 };
            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                for (int k = 0; k < 4; k++) AddTaiko(b, t0 + k * beat, k % 2 == 0 ? 1f : 0.7f);
                AddTaiko(b, t0 + beat * 3.5f, 0.6f);
                for (int k = 0; k < 8; k++) AddNoise(b, t0 + k * beat * 0.5f, 0.04f, 0.15f, 70f, 1f);
                for (int n = 0; n < 8; n++)
                {
                    int note = melody[(bar * 8 + n) % melody.Length];
                    AddPluck(b, t0 + n * beat * 0.5f, scale[note], 0.3f, beat, 0.99f);
                }
            }
            // Menacing detuned saw drone.
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float saw = ((t * 55f) % 1f) * 2f - 1f + (((t * 55.6f) % 1f) * 2f - 1f);
                b[i] += saw * 0.05f;
            }
            MakeLoopable(b);
            Normalize(b, 0.6f);
            var clip = AudioClip.Create("boss_music", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        /// <summary>Slow, sparse and sad – emotional story beats.</summary>
        AudioClip BuildStoryMusic(float[] scale)
        {
            const float bpm = 56f;
            float beat = 60f / bpm;
            int bars = 4;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 4, 3, 1, 0, 1, 3, 2, 1, 0, 1, 0, 0, 4, 3, 5, 4 };
            for (int i = 0; i < bars * 4; i++)
            {
                AddPluck(b, i * beat, scale[melody[i % melody.Length]] * 0.5f, 0.45f, beat * 3f, 0.998f);
                if (i % 4 == 0) AddTone(b, i * beat, beat * 4f, scale[0] * 0.25f, scale[0] * 0.25f, 0.18f, 0.4f, 0.8f);
            }
            MakeLoopable(b);
            Normalize(b, 0.4f);
            var clip = AudioClip.Create("story_music", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        /// <summary>Hopeful travelling theme for the world map.</summary>
        AudioClip BuildMapMusic(float[] scale)
        {
            const float bpm = 100f;
            float beat = 60f / bpm;
            int bars = 8;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 0, 2, 3, 5, 4, 3, 2, 3, 5, 7, 5, 4, 3, 2, 0, 2 };
            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                AddTaiko(b, t0, 0.5f);
                AddTaiko(b, t0 + beat * 2f, 0.35f);
                for (int n = 0; n < 4; n++)
                {
                    AddPluck(b, t0 + n * beat, scale[melody[(bar * 4 + n) % melody.Length]], 0.35f, beat * 1.4f, 0.995f);
                    AddNoise(b, t0 + n * beat + beat * 0.5f, 0.03f, 0.06f, 70f, 1f);
                }
                AddPluck(b, t0, scale[0] * 0.5f, 0.3f, beat * 3.5f, 0.998f);
            }
            MakeLoopable(b);
            Normalize(b, 0.5f);
            var clip = AudioClip.Create("map_music", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        /// <summary>Mystic rising drone with chimes for the summon altar.</summary>
        AudioClip BuildSummonMusic()
        {
            var b = Buffer(8f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                b[i] = Mathf.Sin(2f * Mathf.PI * 110f * t) * 0.2f + Mathf.Sin(2f * Mathf.PI * 164.8f * t) * 0.12f
                       + Mathf.Sin(2f * Mathf.PI * 220.5f * t) * 0.08f * (0.5f + 0.5f * Mathf.Sin(t * 1.3f));
            }
            float[] chimes = { 880f, 1174.7f, 1318.5f, 1760f };
            for (int k = 0; k < 8; k++) AddTone(b, k * 1f, 1.2f, chimes[k % 4], chimes[k % 4], 0.25f, 3f);
            MakeLoopable(b);
            Normalize(b, 0.45f);
            var clip = AudioClip.Create("summon_music", b.Length, 1, Rate, false);
            clip.SetData(b, 0);
            return clip;
        }

        static void Normalize(float[] b, float gain)
        {
            float peak = 0.0001f;
            foreach (var s in b) peak = Mathf.Max(peak, Mathf.Abs(s));
            for (int i = 0; i < b.Length; i++) b[i] *= gain / peak;
        }

        static void MakeLoopable(float[] b)
        {
            int fade = Rate / 50;
            for (int i = 0; i < fade && i < b.Length; i++)
            {
                float k = (float)i / fade;
                b[i] *= k;
                b[b.Length - 1 - i] *= k;
            }
        }
    }
}
