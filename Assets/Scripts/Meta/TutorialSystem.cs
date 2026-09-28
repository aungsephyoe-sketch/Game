using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// First-time guidance, each shown once and skippable at any time (and never again after that):
    ///   • Battle basics on the very first mission: move, attack and combo, dodge, skills, special, the goal
    ///     marker. Each step waits until the player has actually done it; enemies hold back until the basics
    ///     are learnt.
    ///   • A home-screen tour of the main buttons.
    ///   • A world-map guide: regions, travelling, story order, starting a mission.
    /// </summary>
    public static class TutorialSystem
    {
        public enum Kind { None, Battle, Home, Map }

        public class Step
        {
            public string title, text, target;
            /// <summary>0 = tap NEXT; otherwise completed by doing the action.</summary>
            public int need;
        }

        public static readonly Step[] BattleSteps =
        {
            new Step { title = "WELCOME, SLAYER!", text = "I'll show you the basics. You can skip any time.", target = "" },
            new Step { title = "MOVE", text = "Drag the <color=#5BE37D>JOYSTICK</color> (or use WASD) to run around.", target = "joystick", need = 1 },
            new Step { title = "ATTACK", text = "Tap <color=#FF8A5B>ATTACK</color> three times for a combo!", target = "attack", need = 3 },
            new Step { title = "DODGE", text = "Tap <color=#6BC8FF>DODGE</color> to roll through danger.", target = "dodge", need = 1 },
            new Step { title = "SKILLS", text = "Skills hit hard. Tap a <color=#FFD35B>SKILL</color> button!", target = "skill", need = 1 },
            new Step { title = "SPECIAL", text = "Hits fill your <color=#FF6BD6>SPECIAL</color>. When it glows, unleash it!", target = "ultimate" },
            new Step { title = "GO!", text = "Red zones on the ground mean an attack is coming. Follow the arrow and beat the demons!", target = "" },
        };

        public static readonly Step[] HomeSteps =
        {
            new Step { title = "YOUR HOME", text = "This is Kiriha, your home village. Let's look around!", target = "" },
            new Step { title = "PLAY", text = "<color=#FF8A5B>PLAY</color> continues the story on the world map.", target = "play" },
            new Step { title = "SUMMON", text = "<color=#C98BFF>SUMMON</color> new slayers at the moon shrine.", target = "summon" },
            new Step { title = "TEAM", text = "Pick three slayers for your <color=#5BE37D>TEAM</color> — you can switch between them in battle.", target = "team" },
            new Step { title = "FRIENDS", text = "Add <color=#6BC8FF>FRIENDS</color> with their gamer code, and tap your name to see your profile.", target = "friends" },
        };

        public static readonly Step[] MapSteps =
        {
            new Step { title = "WORLD MAP", text = "Each land is a region with its own story missions.", target = "" },
            new Step { title = "CHOOSE A LAND", text = "Tap a <color=#FFD35B>region</color> to see its missions. New lands unlock as the story goes on.", target = "" },
            new Step { title = "TRAVEL", text = "Your slayer walks there along the roads — sometimes you meet demons on the way!", target = "" },
            new Step { title = "PLAY A MISSION", text = "Pick a mission and tap <color=#FF8A5B>PLAY</color>. Clear them in order to move the story forward.", target = "" },
        };

        public static Kind Active { get; private set; }
        public static int StepIndex { get; private set; }
        public static int Progress { get; private set; }
        public static float StepStarted { get; private set; }
        static Vector3 lastPos;
        static float moved;

        public static Step[] Steps
        {
            get { return Active == Kind.Battle ? BattleSteps : Active == Kind.Home ? HomeSteps : Active == Kind.Map ? MapSteps : null; }
        }

        public static Step Current { get { var s = Steps; return s != null && StepIndex < s.Length ? s[StepIndex] : null; } }

        /// <summary>Old saves that already played never see the tutorials.</summary>
        public static void Migrate(PlayerData d)
        {
            if (d.missionsCleared > 0 || d.IsMissionCleared("1-1"))
            {
                d.tutorialDone = true;
                if (d.missionsCleared > 2) { d.homeTourDone = true; d.mapTutorialDone = true; }
            }
        }

        public static bool Begin(Kind k)
        {
            var gm = GameManager.Instance;
            if (gm == null || Active != Kind.None) return false;
            var d = gm.Data;
            if (k == Kind.Battle && d.tutorialDone) return false;
            if (k == Kind.Home && d.homeTourDone) return false;
            if (k == Kind.Map && d.mapTutorialDone) return false;
            Active = k;
            StepIndex = 0;
            Progress = 0;
            StepStarted = Time.unscaledTime;
            moved = 0f;
            lastPos = Vector3.zero;
            return true;
        }

        public static void Next()
        {
            if (Active == Kind.None) return;
            StepIndex++;
            Progress = 0;
            moved = 0f;
            StepStarted = Time.unscaledTime;
            var gm = GameManager.Instance;
            if (gm != null) gm.Audio.Play("perfect", 0.35f);
            if (StepIndex >= Steps.Length) Finish();
        }

        /// <summary>Skip or finish: this tutorial is never shown again.</summary>
        public static void Finish()
        {
            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (Active == Kind.Battle) gm.Data.tutorialDone = true;
                if (Active == Kind.Home) gm.Data.homeTourDone = true;
                if (Active == Kind.Map) gm.Data.mapTutorialDone = true;
                gm.Save();
            }
            Active = Kind.None;
        }

        /// <summary>Battle steps complete by doing the thing (called every frame during a battle).</summary>
        public static void Tick()
        {
            if (Active != Kind.Battle) return;
            var gm = GameManager.Instance;
            var b = BattleController.Current;
            if (gm == null || b == null || b.Team == null || b.Team.Active == null) { if (b == null) Active = Kind.None; return; }
            var step = Current;
            if (step == null) return;
            var input = gm.Controls != null ? gm.Controls.Current : InputState.Empty;
            var pc = b.Team.Active;
            switch (step.target)
            {
                case "joystick":
                    if (lastPos != Vector3.zero) moved += (pc.Position - lastPos).magnitude;
                    lastPos = pc.Position;
                    if (moved > 4f) Next();
                    break;
                case "attack":
                    if (input.attackDown && ++Progress >= step.need) Next();
                    break;
                case "dodge":
                    if (input.dodgeDown) Next();
                    break;
                case "skill":
                    if (input.skill1Down || input.skill2Down || input.skill3Down) Next();
                    break;
            }
        }

        /// <summary>True while the battle should hold back its demons (the first few steps).</summary>
        public static bool HoldEnemies { get { return Active == Kind.Battle && StepIndex < 3; } }
    }
}
