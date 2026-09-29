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
    public partial class AudioManager : MonoBehaviour
    {
        const int Rate = 32000;
        const float CrossfadeSeconds = 1.6f;

        readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
        readonly Dictionary<string, float> lastPlayed = new Dictionary<string, float>();
        readonly Dictionary<MusicState, AudioClip> tracks = new Dictionary<MusicState, AudioClip>();
        AudioSource sfx;
        AudioSource musicA, musicB, heartbeat, ambience;
        readonly AudioSource[] voices = new AudioSource[10];
        int nextVoice;
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
            ambience = MakeMusicSource();
            for (int i = 0; i < voices.Length; i++)
            {
                voices[i] = gameObject.AddComponent<AudioSource>();
                voices[i].playOnAwake = false;
            }
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

        /// <summary>Plays a sound at a given pitch on its own voice (so pitches never bleed into each other).</summary>
        public void PlayPitched(string id, float volume, float pitch)
        {
            AudioClip clip;
            if (!clips.TryGetValue(id, out clip)) return;
            float last;
            string key = id + ((int)(pitch * 10f));
            if (lastPlayed.TryGetValue(key, out last) && Time.unscaledTime - last < 0.03f) return;
            lastPlayed[key] = Time.unscaledTime;
            var v = voices[nextVoice];
            nextVoice = (nextVoice + 1) % voices.Length;
            v.pitch = pitch;
            v.PlayOneShot(clip, volume * SfxVolume);
        }

        /// <summary>A randomly varied version of a sound so repeated hits never sound identical.</summary>
        public void PlayVaried(string id, float volume, float spread = 0.08f)
        {
            PlayPitched(id, volume, 1f + Random.Range(-spread, spread));
        }

        /// <summary>Looping environmental bed for the current location (forest, wind, fire, dark, village, none).</summary>
        public void SetAmbience(string kind)
        {
            AudioClip clip = null;
            if (!string.IsNullOrEmpty(kind)) clips.TryGetValue("amb_" + kind, out clip);
            if (ambience.clip == clip) return;
            ambience.clip = clip;
            if (clip != null) { ambience.volume = 0f; ambience.Play(); }
            else ambience.Stop();
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
            if (ambience != null && ambience.clip != null)
                ambience.volume = Mathf.MoveTowards(ambience.volume, GameSettings.SfxVolume * 0.35f, dt * 0.3f);
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
            // Every one-shot effect gets the same finishing pass; loops (ambience, heartbeat) stay as they are.
            if (!id.StartsWith("amb_") && id != "heartbeat") data = Polish(data, id == "click" || id == "step" ? 0.05f : 0.14f);
            float peak = 0.0001f;
            foreach (var s in data) peak = Mathf.Max(peak, Mathf.Abs(s));
            float norm = gain / peak;
            for (int i = 0; i < data.Length; i++) data[i] *= norm;
            var clip = AudioClip.Create(id, data.Length, 1, Rate, false);
            clip.SetData(data, 0);
            clips[id] = clip;
            return clip;
        }

        /// <summary>
        /// The finishing pass that makes the synthesised effects sound produced rather than raw: DC removed, the
        /// harsh digital top smoothed, a soft tape-like saturation that rounds peaks and adds weight, a small warm
        /// room (Schroeder reverb) so hits and chimes have space, and click-free fades at both ends.
        /// </summary>
        static float[] Polish(float[] src, float wet)
        {
            int tail = Mathf.RoundToInt(0.22f * Rate * Mathf.Clamp01(wet * 8f));
            var d = new float[src.Length + tail];
            // DC blocker + gentle low-pass.
            float prevX = 0f, prevY = 0f, lp = 0f, peak = 0.0001f;
            for (int i = 0; i < src.Length; i++)
            {
                float x = src[i];
                float y = x - prevX + 0.995f * prevY;
                prevX = x; prevY = y;
                lp += (y - lp) * 0.72f;
                d[i] = lp;
                peak = Mathf.Max(peak, Mathf.Abs(lp));
            }
            // Soft saturation on the normalised signal.
            const float drive = 1.7f;
            float norm = 1f / (float)System.Math.Tanh(drive);
            for (int i = 0; i < src.Length; i++) d[i] = (float)System.Math.Tanh(d[i] / peak * drive) * norm * peak;
            if (wet > 0f)
            {
                int[] combs = { 797, 863, 941, 1013 };
                var outR = new float[d.Length];
                foreach (int len in combs)
                {
                    var buf = new float[len];
                    int idx = 0;
                    float store = 0f;
                    for (int i = 0; i < d.Length; i++)
                    {
                        float o = buf[idx];
                        store = o * 0.7f + store * 0.3f;
                        buf[idx] = d[i] + store * 0.74f;
                        idx = (idx + 1) % len;
                        outR[i] += o * 0.25f;
                    }
                }
                int[] allpass = { 225, 341 };
                foreach (int len in allpass)
                {
                    var buf = new float[len];
                    int idx = 0;
                    for (int i = 0; i < outR.Length; i++)
                    {
                        float bo = buf[idx];
                        float o = -outR[i] + bo;
                        buf[idx] = outR[i] + bo * 0.5f;
                        idx = (idx + 1) % len;
                        outR[i] = o;
                    }
                }
                for (int i = 0; i < d.Length; i++) d[i] += outR[i] * wet;
            }
            int fin = Mathf.Min(d.Length, 32), fout = Mathf.Min(d.Length, Rate / 100);
            for (int i = 0; i < fin; i++) d[i] *= i / (float)fin;
            for (int i = 0; i < fout; i++) d[d.Length - 1 - i] *= i / (float)fout;
            return d;
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
            // Reward chime: three struck bells (inharmonic partials) rising, with a sparkle.
            b = Buffer(1.3f);
            float[] bells = { 1046.5f, 1318.5f, 1568f };
            for (int k = 0; k < bells.Length; k++)
            {
                float f = bells[k], t0 = k * 0.07f;
                AddTone(b, t0, 1.1f, f, f, 0.45f, 3.2f, 0.002f); AddTone(b, t0, 0.8f, f * 2.76f, f * 2.76f, 0.14f, 5f, 0.002f);
                AddTone(b, t0, 0.5f, f * 5.4f, f * 5.4f, 0.05f, 8f, 0.002f); AddTone(b, t0, 1.1f, f * 0.5f, f * 0.5f, 0.12f, 3f, 0.004f);
            }
            AddNoise(b, 0.14f, 0.35f, 0.05f, 9f, 1f, 0.02f);
            Make("perfect", b, 0.6f);
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
            // UI tap: a soft wooden "tok" instead of a beep.
            b = Buffer(0.09f); AddTone(b, 0, 0.08f, 1150f, 720f, 0.55f, 55f, 0.001f); AddTone(b, 0, 0.04f, 2400f, 1900f, 0.12f, 80f, 0.001f); AddNoise(b, 0, 0.01f, 0.25f, 300f, 0.9f, 0.0005f); Make("click", b, 0.4f);
            // Coin pickup: two bright pings.
            // Coin pickup: the classic bright "ka-ching" (two bell tones with metallic overtones and a tiny shimmer).
            b = Buffer(0.5f);
            AddTone(b, 0, 0.1f, 1976f, 1976f, 0.45f, 28f); AddTone(b, 0, 0.1f, 3952f, 3952f, 0.12f, 34f); AddTone(b, 0, 0.1f, 5588f, 5588f, 0.05f, 40f);
            AddTone(b, 0.07f, 0.42f, 2637f, 2637f, 0.5f, 8f); AddTone(b, 0.07f, 0.42f, 5274f, 5274f, 0.12f, 11f); AddTone(b, 0.07f, 0.42f, 7458f, 7458f, 0.04f, 14f);
            AddNoise(b, 0.07f, 0.05f, 0.08f, 60f, 1f);
            Make("coin", b, 0.6f);
            // Coins spilling from a defeated demon: a quick shower of little clinks.
            b = Buffer(0.7f);
            for (int k = 0; k < 9; k++) { float t0 = k * 0.05f + Random.Range(0f, 0.03f), f = Random.Range(2400f, 3600f); AddTone(b, t0, 0.14f, f, f, 0.22f, 26f); AddTone(b, t0, 0.14f, f * 2.4f, f * 2.4f, 0.06f, 30f); }
            AddNoise(b, 0, 0.12f, 0.12f, 25f, 0.9f);
            Make("coinSpill", b, 0.55f);
            // Diamond: a sparkling rising arpeggio.
            b = Buffer(0.9f);
            float[] gem = { 1568f, 2093f, 2637f, 3136f, 4186f };
            for (int k = 0; k < gem.Length; k++) { AddTone(b, k * 0.06f, 0.6f, gem[k], gem[k], 0.3f, 6f); AddTone(b, k * 0.06f, 0.4f, gem[k] * 2f, gem[k] * 2f, 0.07f, 9f); }
            AddNoise(b, 0, 0.5f, 0.06f, 5f, 1f, 0.05f);
            Make("gem", b, 0.6f);
            // Element swing layers (played on top of the blade sound).
            b = Buffer(0.45f); AddNoise(b, 0, 0.45f, 0.9f, 7f, 0.35f, 0.03f); for (int k = 0; k < 10; k++) AddNoise(b, Random.Range(0f, 0.35f), 0.02f, 0.5f, 90f, 1f); Make("el_flame", b, 0.7f);          // roar + crackle
            b = Buffer(0.45f); AddNoise(b, 0, 0.06f, 1.2f, 50f, 1f); AddNoise(b, 0.04f, 0.4f, 0.6f, 9f, 0.25f); AddTone(b, 0, 0.3f, 3200f, 900f, 0.2f, 14f, 0.001f, true); Make("el_thunder", b, 0.8f); // crack + rumble
            b = Buffer(0.4f); AddNoise(b, 0, 0.35f, 0.7f, 10f, 0.45f, 0.05f); for (int k = 0; k < 6; k++) AddTone(b, Random.Range(0f, 0.25f), 0.08f, Random.Range(500f, 900f), Random.Range(900f, 1500f), 0.25f, 30f); Make("el_water", b, 0.7f); // splash + bubbles
            b = Buffer(0.5f); AddNoise(b, 0, 0.5f, 0.8f, 5f, 0.55f, 0.12f); AddTone(b, 0, 0.5f, 300f, 700f, 0.12f, 5f, 0.1f); Make("el_wind", b, 0.6f);          // rising gust
            b = Buffer(0.6f); AddTone(b, 0, 0.6f, 1318f, 1318f, 0.35f, 6f); AddTone(b, 0.03f, 0.55f, 1976f, 1976f, 0.25f, 7f); AddTone(b, 0.06f, 0.5f, 2637f, 2637f, 0.18f, 8f); AddNoise(b, 0, 0.1f, 0.3f, 30f, 1f); Make("el_light", b, 0.6f); // chime
            b = Buffer(0.5f); AddTone(b, 0, 0.5f, 110f, 70f, 0.6f, 5f, 0.04f); AddTone(b, 0, 0.5f, 116f, 72f, 0.5f, 5f, 0.04f); AddNoise(b, 0, 0.4f, 0.4f, 7f, 0.2f, 0.06f); Make("el_dark", b, 0.7f); // low hum
            // Strong attack: a thunderous smash.
            b = Buffer(0.8f); AddTaiko(b, 0f, 1f); AddNoise(b, 0, 0.5f, 0.9f, 6f, 0.5f); AddTone(b, 0.02f, 0.6f, 90f, 30f, 0.9f, 5f); Make("smash", b, 0.95f);

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

            GenerateCombatSounds();
            GenerateSoundDesign();
            GenerateCoolPass();
            tracks[MusicState.Menu] = BuildHomeTheme();
            tracks[MusicState.Explore] = BuildExploreMusic(scale);
            tracks[MusicState.Combat] = BuildBattleMusic(scale);
            tracks[MusicState.Boss] = BuildBossMusic(scale);
            tracks[MusicState.Story] = BuildStoryMusic(scale);
            tracks[MusicState.Map] = BuildMapMusic(scale);
            tracks[MusicState.Summon] = BuildSummonMusic();
            tracks[MusicState.Victory] = clips["victory"];
            tracks[MusicState.Defeat] = clips["defeat"];
        }

        /// <summary>
        /// Weightier combat sounds: heavy swings, big impacts, special-attack build-ups and releases, monster voices,
        /// boss slams, plus looping ambience beds for each kind of location.
        /// </summary>
        void GenerateCombatSounds()
        {
            float[] b;
            b = Buffer(0.3f); AddNoise(b, 0, 0.28f, 1f, 12f, 0.55f, 0.02f); AddTone(b, 0, 0.2f, 500f, 120f, 0.35f, 12f); Make("slashHeavy", b, 0.8f);
            b = Buffer(0.9f); AddTone(b, 0, 0.9f, 75f, 28f, 1f, 4.5f); AddNoise(b, 0, 0.6f, 0.9f, 7f, 0.35f); AddNoise(b, 0, 0.06f, 0.8f, 60f, 1f); AddTaiko(b, 0f, 0.7f); Make("impact", b, 1f);
            // Special build-up: a rising shimmer over a swelling drone (long, so the moment breathes).
            b = Buffer(1.6f);
            AddTone(b, 0, 1.6f, 110f, 220f, 0.6f, 0.2f, 0.4f);
            AddTone(b, 0, 1.6f, 440f, 1760f, 0.25f, 0.3f, 0.6f);
            AddNoise(b, 0.2f, 1.4f, 0.5f, 0.5f, 0.9f, 1f);
            for (int i = 0; i < 8; i++) AddTone(b, 0.2f + i * 0.17f, 0.15f, 900f + i * 180f, 1100f + i * 180f, 0.15f, 20f);
            Make("buildup", b, 0.8f);
            // Special release: a huge layered boom with a long tail.
            b = Buffer(2.2f);
            AddTone(b, 0, 2.2f, 60f, 25f, 1f, 1.8f);
            AddNoise(b, 0, 1.8f, 1f, 2.2f, 0.45f, 0.005f);
            AddNoise(b, 0, 0.12f, 1f, 30f, 1f);
            AddTone(b, 0, 1.4f, 880f, 220f, 0.25f, 2f);
            AddTaiko(b, 0f, 1f); AddTaiko(b, 0.18f, 0.7f);
            Make("specialRelease", b, 1f);
            // Monster voices.
            b = Buffer(0.6f);
            for (int i = 0; i < b.Length; i++)
            {
                float t = (float)i / Rate;
                float f = 70f + Mathf.Sin(t * 45f) * 12f;
                b[i] = (((t * f) % 1f) * 2f - 1f) * Mathf.Min(1f, t * 12f) * Mathf.Exp(-t * 3.5f);
            }
            AddNoise(b, 0, 0.6f, 0.6f, 4f, 0.25f, 0.05f);
            Make("growl", b, 0.7f);
            b = Buffer(0.5f); AddTone(b, 0, 0.5f, 1800f, 900f, 0.6f, 5f, 0.02f, true); AddNoise(b, 0, 0.5f, 0.7f, 6f, 0.95f, 0.02f); Make("screech", b, 0.6f);
            b = Buffer(1.1f); AddTone(b, 0, 1.1f, 55f, 22f, 1f, 3f); AddNoise(b, 0, 0.8f, 1f, 4f, 0.2f); AddTaiko(b, 0f, 1f); Make("bossSlam", b, 1f);
            b = Buffer(0.25f); AddNoise(b, 0, 0.25f, 1f, 9f, 0.95f, 0.06f); AddTone(b, 0, 0.2f, 300f, 700f, 0.15f, 8f); Make("whoosh", b, 0.55f);
            b = Buffer(0.3f); AddTone(b, 0, 0.3f, 2100f, 2000f, 0.5f, 10f); AddTone(b, 0, 0.25f, 3150f, 3000f, 0.3f, 12f); AddNoise(b, 0, 0.05f, 0.8f, 60f, 1f); AddTone(b, 0, 0.12f, 150f, 70f, 0.8f, 18f); Make("clang", b, 0.8f);

            // Ambience beds (loopable).
            b = Buffer(8f); AddNoise(b, 0, 8f, 0.25f, 0f, 0.15f, 2f);
            for (int i = 0; i < 9; i++) { float t0 = 0.4f + i * 0.85f; AddTone(b, t0, 0.08f, 3200f, 3800f, 0.12f, 30f); AddTone(b, t0 + 0.1f, 0.08f, 3600f, 3000f, 0.1f, 30f); }
            MakeLoopable(b); Make("amb_forest", b, 0.35f);
            b = Buffer(8f); AddNoise(b, 0, 8f, 0.6f, 0f, 0.3f, 3f);
            for (int i = 0; i < b.Length; i++) b[i] *= 0.6f + 0.4f * Mathf.Sin((float)i / Rate * 0.8f);
            MakeLoopable(b); Make("amb_wind", b, 0.35f);
            b = Buffer(6f); AddNoise(b, 0, 6f, 0.2f, 0f, 0.2f, 1f);
            for (int i = 0; i < 40; i++) AddNoise(b, (float)rng.NextDouble() * 5.8f, 0.03f, 0.5f, 90f, 1f);
            MakeLoopable(b); Make("amb_fire", b, 0.35f);
            b = Buffer(8f); AddTone(b, 0, 8f, 55f, 55f, 0.5f, 0f, 2f); AddTone(b, 0, 8f, 82.4f, 80f, 0.3f, 0f, 2f); AddNoise(b, 0, 8f, 0.2f, 0f, 0.1f, 2f);
            MakeLoopable(b); Make("amb_dark", b, 0.35f);
            b = Buffer(8f); AddNoise(b, 0, 8f, 0.18f, 0f, 0.2f, 2f);
            for (int i = 0; i < 5; i++) AddTone(b, 1f + i * 1.4f, 0.1f, 2500f, 2900f, 0.1f, 25f);
            MakeLoopable(b); Make("amb_village", b, 0.3f);
        }

        /// <summary>
        /// Driving battle theme: war taiko on eighths with fills, a pulsing bass ostinato, a dark string drone, shaker
        /// sixteenths, and the koto melody doubled an octave up in the second half for lift.
        /// </summary>
        AudioClip BuildBattleMusic(float[] scale)
        {
            const float bpm = 140f;
            float beat = 60f / bpm;
            int bars = 8;
            var b = Buffer(bars * 4 * beat);
            int[] melody = { 0, 2, 3, 2, 4, 3, 2, 1, 0, 2, 3, 5, 4, 3, 2, 3, 5, 4, 3, 2, 3, 2, 1, 0, 0, 1, 2, 3, 4, 5, 4, 3 };
            int[] roots = { 0, 0, 3, 2, 0, 0, 3, 4 };
            for (int bar = 0; bar < bars; bar++)
            {
                float t0 = bar * 4 * beat;
                bool fill = bar % 4 == 3;
                // Drums.
                AddTaiko(b, t0, 1.1f);
                AddTone(b, t0, 0.5f, 60f, 32f, 0.6f, 6f);
                AddTaiko(b, t0 + beat * 0.75f, 0.45f);
                AddTaiko(b, t0 + beat * 1.5f, 0.7f);
                AddTaiko(b, t0 + beat * 2f, 1f);
                AddTaiko(b, t0 + beat * 2.75f, 0.45f);
                if (fill) for (int k = 0; k < 4; k++) AddTaiko(b, t0 + beat * (3f + k * 0.25f), 0.55f + k * 0.15f);
                else { AddTaiko(b, t0 + beat * 3f, 0.7f); AddTaiko(b, t0 + beat * 3.5f, 0.55f); }
                if (bar % 4 == 0) AddNoise(b, t0, 1.2f, 0.28f, 3.5f, 0.95f, 0.002f); // crash
                for (int k = 0; k < 16; k++) AddNoise(b, t0 + k * beat * 0.25f, 0.03f, k % 4 == 2 ? 0.14f : 0.07f, 70f, 1f);
                // Bass ostinato on eighths.
                float root = scale[roots[bar]] * 0.25f;
                for (int k = 0; k < 8; k++) AddPluck(b, t0 + k * beat * 0.5f, k % 4 == 3 ? root * 1.5f : root, 0.32f, beat * 0.45f, 0.993f);
                // Dark drone.
                AddPad(b, t0, 4 * beat + 0.2f, scale[roots[bar]] * 0.5f, 0.12f);
                AddPad(b, t0, 4 * beat + 0.2f, scale[roots[bar]] * 0.75f, 0.07f);
                // Melody (doubled an octave up in the second half).
                for (int n = 0; n < 4; n++)
                {
                    int note = melody[(bar * 4 + n) % melody.Length];
                    AddPluck(b, t0 + n * beat, scale[note], 0.36f, beat * 1.2f, 0.994f);
                    if (bar >= 4) AddPluck(b, t0 + n * beat + 0.01f, scale[note] * 2f, 0.16f, beat, 0.992f);
                }
            }
            // Glue the mix: bring it to a known level, then a gentle tape-style squash.
            Normalize(b, 0.9f);
            Saturate(b, 1.3f);
            MakeLoopable(b);
            var clip = AudioClip.Create("battle_music", b.Length, 1, Rate, false);
            Normalize(b, 0.62f);
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
