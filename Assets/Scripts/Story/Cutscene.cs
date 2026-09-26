using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    public enum StepKind { Env, Actor, Remove, Cut, Dolly, Orbit, Say, Move, Face, Anim, Fx, Title, Fade, Music, Sfx, Wait, Shake, Sky }

    /// <summary>One instruction in a cutscene script.</summary>
    public class CutsceneStep
    {
        public StepKind kind;
        public string a, b, c;          // actor keys, ids, speaker/text, fx kind...
        public Vector3 v1, v2;           // positions / look targets
        public float f1, f2, f3;         // durations, yaw, radius...
        public bool wait = true;         // block until finished
        public ArenaTheme theme;
        public Color color;
    }

    /// <summary>
    /// A scripted in-engine scene. Built with a small fluent API so story beats read like a screenplay:
    /// <c>.Cut(...).Say("Ren", "...").Anim("ren", "swing")</c>.
    /// </summary>
    public class Cutscene
    {
        public string id;
        public readonly List<CutsceneStep> steps = new List<CutsceneStep>();

        public Cutscene(string id) { this.id = id; }

        CutsceneStep Add(StepKind k)
        {
            var s = new CutsceneStep { kind = k };
            steps.Add(s);
            return s;
        }

        /// <summary>Build (or rebuild) the set from a location theme.</summary>
        public Cutscene Env(ArenaTheme theme) { Add(StepKind.Env).theme = theme; return this; }

        /// <summary>Place a character (hero/NPC id) or demon (enemy id) at a position, facing a yaw.</summary>
        public Cutscene Actor(string key, string defId, Vector3 pos, float yaw = 180f, float scale = 1f)
        {
            var s = Add(StepKind.Actor);
            s.a = key; s.b = defId; s.v1 = pos; s.f1 = yaw; s.f2 = scale;
            return this;
        }

        public Cutscene Remove(string key) { Add(StepKind.Remove).a = key; return this; }
        public Cutscene Cut(Vector3 pos, Vector3 look) { var s = Add(StepKind.Cut); s.v1 = pos; s.v2 = look; return this; }

        public Cutscene Dolly(Vector3 pos, Vector3 look, float seconds, bool wait = true)
        {
            var s = Add(StepKind.Dolly);
            s.v1 = pos; s.v2 = look; s.f1 = seconds; s.wait = wait;
            return this;
        }

        /// <summary>Orbit the camera around an actor from one yaw to another.</summary>
        public Cutscene Orbit(string key, float radius, float height, float fromYaw, float toYaw, float seconds, bool wait = true)
        {
            var s = Add(StepKind.Orbit);
            s.a = key; s.f1 = radius; s.f2 = height; s.v1 = new Vector3(fromYaw, toYaw, seconds); s.wait = wait;
            return this;
        }

        public Cutscene Say(string speaker, string text) { var s = Add(StepKind.Say); s.a = speaker; s.b = text; return this; }

        public Cutscene Move(string key, Vector3 to, float seconds, bool wait = false)
        {
            var s = Add(StepKind.Move);
            s.a = key; s.v1 = to; s.f1 = seconds; s.wait = wait;
            return this;
        }

        public Cutscene Face(string key, string targetKey) { var s = Add(StepKind.Face); s.a = key; s.b = targetKey; return this; }

        /// <summary>swing, spin, heavy, victory, defeat, knockdown, getup, hit, charge, glow, fade</summary>
        public Cutscene Anim(string key, string anim) { var s = Add(StepKind.Anim); s.a = key; s.b = anim; return this; }

        /// <summary>fire, explosion, darkpulse, lightning, dawn, smoke, slash</summary>
        public Cutscene Fx(string kind, Vector3 pos, Color color) { var s = Add(StepKind.Fx); s.a = kind; s.v1 = pos; s.color = color; return this; }

        public Cutscene Title(string title, string subtitle, float seconds = 3f)
        {
            var s = Add(StepKind.Title);
            s.a = title; s.b = subtitle; s.f1 = seconds;
            return this;
        }

        /// <summary>Fade to alpha (1 = black) over seconds.</summary>
        public Cutscene Fade(float alpha, float seconds) { var s = Add(StepKind.Fade); s.f1 = alpha; s.f2 = seconds; return this; }
        public Cutscene Music(MusicState m) { var s = Add(StepKind.Music); s.a = m.ToString(); return this; }
        public Cutscene Sfx(string id) { Add(StepKind.Sfx).a = id; return this; }
        public Cutscene Wait(float seconds) { Add(StepKind.Wait).f1 = seconds; return this; }
        public Cutscene Shake(float amount) { Add(StepKind.Shake).f1 = amount; return this; }

        /// <summary>Tint the sky/fog (e.g. the moment the eclipse appears).</summary>
        public Cutscene Sky(Color sky, Color fog, float seconds) { var s = Add(StepKind.Sky); s.color = sky; s.v1 = new Vector3(fog.r, fog.g, fog.b); s.f1 = seconds; return this; }
    }
}
