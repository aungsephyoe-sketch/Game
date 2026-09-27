using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Battle HUD geometry in virtual coordinates (1080 px tall, width follows aspect ratio), respecting the
    /// device safe area. Shared by the touch handler and the renderer so hit areas always match visuals.
    /// </summary>
    public static class HudLayout
    {
        public const float RefHeight = 1080f;

        public struct Circle
        {
            public Vector2 center;
            public float radius;

            public Circle(float x, float y, float r)
            {
                center = new Vector2(x, y);
                radius = r;
            }

            public bool Contains(Vector2 p, float slack = 1.18f)
            {
                return (p - center).sqrMagnitude <= radius * radius * slack * slack;
            }
        }

        public static float Scale { get { return Mathf.Max(0.01f, Screen.height / RefHeight); } }
        public static float Width { get { return Screen.width / Scale; } }
        public static float Height { get { return RefHeight; } }

        public static Rect Safe
        {
            get
            {
                var sa = Screen.safeArea;
                float s = Scale;
                return new Rect(sa.x / s, (Screen.height - sa.yMax) / s, sa.width / s, sa.height / s);
            }
        }

        public static Vector2 ScreenToGui(Vector2 screen)
        {
            float s = Scale;
            return new Vector2(screen.x / s, (Screen.height - screen.y) / s);
        }

        public static Circle Attack { get { var s = Safe; return new Circle(s.xMax - 215f, s.yMax - 215f, 112f); } }
        public static Circle Dodge { get { var s = Safe; return new Circle(s.xMax - 480f, s.yMax - 110f, 68f); } }
        public static Circle Guard { get { var s = Safe; return new Circle(s.xMax - 640f, s.yMax - 120f, 62f); } }
        public static Circle Lock { get { var s = Safe; return new Circle(s.xMax - 200f, s.y + 67f, 46f); } }
        public static Circle Ultimate { get { var s = Safe; return new Circle(s.xMax - 150f, s.yMax - 690f, 88f); } }

        public static Circle Skill(int i)
        {
            var s = Safe;
            switch (i)
            {
                case 0: return new Circle(s.xMax - 455f, s.yMax - 300f, 76f);
                case 1: return new Circle(s.xMax - 350f, s.yMax - 455f, 76f);
                default: return new Circle(s.xMax - 180f, s.yMax - 495f, 76f);
            }
        }

        public static Rect Pause { get { var s = Safe; return new Rect(s.xMax - 115f, s.y + 20f, 95f, 95f); } }

        public static Rect Portrait(int i)
        {
            var s = Safe;
            return new Rect(s.x + 24f, s.y + 250f + i * 118f, 300f, 106f);
        }

        public static Vector2 JoystickHome { get { var s = Safe; return new Vector2(s.x + 270f, s.yMax - 250f); } }
        public const float JoystickRadius = 140f;

        /// <summary>Touches starting here (and not on another control) grab the floating joystick.</summary>
        public static bool InJoystickZone(Vector2 p)
        {
            return p.x < Width * 0.45f && p.y > Height * 0.35f;
        }
    }
}
