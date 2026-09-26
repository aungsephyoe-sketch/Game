using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Multi-touch battle controls: floating virtual joystick on the left, attack / skills / ultimate / dodge
    /// on the right, tap portraits to switch. Mouse and keyboard work too for editor and desktop testing:
    ///   WASD/Arrows move · J attack (hold = charge) · Space/K dodge · 1 2 3 or U I O skills · L/R ultimate
    ///   Q/E or Tab switch · Esc/P pause.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class MobileControls : MonoBehaviour
    {
        struct Pointer
        {
            public int id;
            public TouchPhase phase;
            public Vector2 pos;
        }

        const int None = int.MinValue;

        public InputState Current { get; private set; }
        public bool JoystickActive { get { return joyFinger != None; } }
        public Vector2 JoystickCenter { get; private set; }
        public Vector2 JoystickKnob { get; private set; }
        /// <summary>Visual press feedback per control: 0 attack, 1 dodge, 2-4 skills, 5 ultimate.</summary>
        public readonly float[] Pressed = new float[6];

        int joyFinger = None;
        int attackFinger = None;
        bool prevAttackHeld;
        readonly List<Pointer> pointers = new List<Pointer>();

        void Update()
        {
            var s = InputState.Empty;
            var gm = GameManager.Instance;
            for (int i = 0; i < Pressed.Length; i++) Pressed[i] = Mathf.MoveTowards(Pressed[i], 0f, Time.unscaledDeltaTime * 5f);

            if (gm == null || gm.CurrentScreen != GameScreen.Battle)
            {
                joyFinger = attackFinger = None;
                prevAttackHeld = false;
                Current = s;
                return;
            }

            CollectPointers();
            foreach (var p in pointers)
            {
                switch (p.phase)
                {
                    case TouchPhase.Began: OnBegan(p, ref s); break;
                    case TouchPhase.Moved:
                    case TouchPhase.Stationary:
                        if (p.id == joyFinger) JoystickKnob = ClampKnob(p.pos);
                        break;
                    default:
                        if (p.id == joyFinger) joyFinger = None;
                        if (p.id == attackFinger) attackFinger = None;
                        break;
                }
            }

            if (joyFinger != None)
            {
                Vector2 d = (JoystickKnob - JoystickCenter) / HudLayout.JoystickRadius;
                if (d.magnitude > 0.12f) s.move = new Vector2(d.x, -d.y);
            }
            else
            {
                JoystickCenter = HudLayout.JoystickHome;
                JoystickKnob = JoystickCenter;
            }

            bool held = attackFinger != None;
            ReadKeyboard(ref s, ref held);
            s.attackHeld = held;
            s.attackUp = prevAttackHeld && !held;
            prevAttackHeld = held;
            if (s.move.sqrMagnitude > 1f) s.move.Normalize();
            Current = s;
        }

        void CollectPointers()
        {
            pointers.Clear();
            if (Input.touchCount > 0)
            {
                for (int i = 0; i < Input.touchCount; i++)
                {
                    var t = Input.GetTouch(i);
                    pointers.Add(new Pointer { id = t.fingerId, phase = t.phase, pos = HudLayout.ScreenToGui(t.position) });
                }
            }
            else if (!Application.isMobilePlatform)
            {
                var pos = HudLayout.ScreenToGui(Input.mousePosition);
                if (Input.GetMouseButtonDown(0)) pointers.Add(new Pointer { id = -1, phase = TouchPhase.Began, pos = pos });
                else if (Input.GetMouseButtonUp(0)) pointers.Add(new Pointer { id = -1, phase = TouchPhase.Ended, pos = pos });
                else if (Input.GetMouseButton(0)) pointers.Add(new Pointer { id = -1, phase = TouchPhase.Moved, pos = pos });
            }
        }

        void OnBegan(Pointer p, ref InputState s)
        {
            if (HudLayout.Pause.Contains(p.pos)) { s.pauseDown = true; return; }
            if (TimeController.Paused) return;

            for (int i = 0; i < 3; i++)
                if (HudLayout.Portrait(i).Contains(p.pos)) { s.switchTo = i; return; }

            if (HudLayout.Attack.Contains(p.pos)) { attackFinger = p.id; s.attackDown = true; Pressed[0] = 1f; return; }
            if (HudLayout.Dodge.Contains(p.pos)) { s.dodgeDown = true; Pressed[1] = 1f; return; }
            for (int i = 0; i < 3; i++)
            {
                if (!HudLayout.Skill(i).Contains(p.pos)) continue;
                if (i == 0) s.skill1Down = true;
                else if (i == 1) s.skill2Down = true;
                else s.skill3Down = true;
                Pressed[2 + i] = 1f;
                return;
            }
            if (HudLayout.Ultimate.Contains(p.pos)) { s.ultimateDown = true; Pressed[5] = 1f; return; }

            if (joyFinger == None && HudLayout.InJoystickZone(p.pos))
            {
                joyFinger = p.id;
                JoystickCenter = p.pos;
                JoystickKnob = p.pos;
            }
        }

        Vector2 ClampKnob(Vector2 pos)
        {
            Vector2 d = pos - JoystickCenter;
            if (d.magnitude > HudLayout.JoystickRadius)
            {
                // Drag the base along so the stick never "sticks" at the edge.
                JoystickCenter = pos - d.normalized * HudLayout.JoystickRadius;
                return pos;
            }
            return pos;
        }

        void ReadKeyboard(ref InputState s, ref bool attackHeld)
        {
            var k = Vector2.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) k.y += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) k.y -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) k.x += 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) k.x -= 1f;
            if (k != Vector2.zero) s.move = k.normalized;

            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P)) s.pauseDown = true;
            if (TimeController.Paused) return;

            if (Input.GetKeyDown(KeyCode.J)) { s.attackDown = true; Pressed[0] = 1f; }
            if (Input.GetKey(KeyCode.J)) attackHeld = true;
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.K)) { s.dodgeDown = true; Pressed[1] = 1f; }
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.U)) { s.skill1Down = true; Pressed[2] = 1f; }
            if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.I)) { s.skill2Down = true; Pressed[3] = 1f; }
            if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.O)) { s.skill3Down = true; Pressed[4] = 1f; }
            if (Input.GetKeyDown(KeyCode.L) || Input.GetKeyDown(KeyCode.R)) { s.ultimateDown = true; Pressed[5] = 1f; }

            var b = BattleController.Current;
            if (b != null && b.Team != null && b.Team.Members.Count > 0)
            {
                int n = b.Team.Members.Count;
                if (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Tab)) s.switchTo = NextAlive(b.Team, 1);
                if (Input.GetKeyDown(KeyCode.Q)) s.switchTo = NextAlive(b.Team, n - 1);
            }
        }

        static int NextAlive(TeamSystem team, int step)
        {
            int n = team.Members.Count;
            for (int i = 1; i < n; i++)
            {
                int idx = (team.ActiveIndex + step * i) % n;
                if (team.Members[idx].IsAlive) return idx;
            }
            return -1;
        }
    }
}
