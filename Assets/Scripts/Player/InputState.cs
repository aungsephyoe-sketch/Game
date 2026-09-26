using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>One frame of player intent, produced by MobileControls (touch) or the keyboard.</summary>
    public struct InputState
    {
        public Vector2 move;
        public bool attackDown;
        public bool attackHeld;
        public bool attackUp;
        public bool dodgeDown;
        public bool skill1Down;
        public bool skill2Down;
        public bool skill3Down;
        public bool ultimateDown;
        /// <summary>Team slot to switch to, or -1.</summary>
        public int switchTo;
        public bool pauseDown;

        public bool SkillDown(int i)
        {
            return i == 0 ? skill1Down : i == 1 ? skill2Down : skill3Down;
        }

        public static InputState Empty { get { return new InputState { switchTo = -1 }; } }
    }
}
