using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The tutorial coach: your leader's face beside a big speech box, a bouncing pointer at the button to use,
    /// NEXT for talk steps and SKIP TUTORIAL always available. Battle steps advance when you do the action.
    /// </summary>
    public partial class UIManager
    {
        bool TutorialOnScreen
        {
            get
            {
                var k = TutorialSystem.Active;
                if (k == TutorialSystem.Kind.Battle) return gm.CurrentScreen == GameScreen.Battle;
                if (k == TutorialSystem.Kind.Home) return gm.CurrentScreen == GameScreen.MainMenu;
                if (k == TutorialSystem.Kind.Map) return gm.CurrentScreen == GameScreen.WorldMap;
                return false;
            }
        }

        void DrawTutorial()
        {
            if (TutorialSystem.Active == TutorialSystem.Kind.None) return;
            if (!TutorialOnScreen)
            {
                // Left the screen mid-tour (home/map): the tour counts as seen.
                if (TutorialSystem.Active != TutorialSystem.Kind.Battle) TutorialSystem.Finish();
                return;
            }
            var step = TutorialSystem.Current;
            if (step == null) return;
            var d = gm.Data;
            bool battle = TutorialSystem.Active == TutorialSystem.Kind.Battle;
            float t = Time.unscaledTime - TutorialSystem.StepStarted;
            float k = Mathf.Clamp01(t / 0.3f);
            k = 1f - Mathf.Pow(1f - k, 3f);
            if (!battle) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.45f * k));

            // Pointer target.
            Rect? target = null;
            switch (step.target)
            {
                case "joystick": { var c = HudLayout.JoystickHome; target = new Rect(c.x - 90f, c.y - 90f, 180f, 180f); break; }
                case "attack": target = CircleRect(HudLayout.Attack.center, HudLayout.Attack.radius); break;
                case "dodge": target = CircleRect(HudLayout.Dodge.center, HudLayout.Dodge.radius); break;
                case "skill": target = CircleRect(HudLayout.Skill(0).center, HudLayout.Skill(0).radius); break;
                case "ultimate": target = CircleRect(HudLayout.Ultimate.center, HudLayout.Ultimate.radius); break;
                default:
                {
                    Rect r;
                    if (!string.IsNullOrEmpty(step.target) && tutTargets.TryGetValue(step.target, out r)) target = r;
                    break;
                }
            }
            if (target.HasValue)
            {
                var r = target.Value;
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 6f);
                RoundFrame(Grow(r, 8f + pulse * 6f), new Color(1f, 0.9f, 0.3f, 0.9f), 5f, 26f);
                // A bouncing pointer (a big arrow) aimed at the button.
                float bob = Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * 18f;
                bool above = r.y > H * 0.4f;
                var ar = new Rect(r.center.x - 45f, above ? r.y - 110f - bob : r.yMax + 20f + bob, 90f, 90f);
                UIStyles.Outlined(ar, above ? "▼" : "▲", UIStyles.Sized(UIStyles.Center, 80), new Color(1f, 0.9f, 0.3f), 4f);
            }

            // Coach box.
            float bw = Mathf.Min(1000f, W - 520f);
            var box = new Rect(W * 0.5f - bw * 0.5f, (battle ? safe.y + 110f : H * 0.3f) - (1f - k) * 60f, bw, 170f);
            Round(Offset(Grow(box, 4f), 0f, 8f), new Color(0f, 0f, 0f, 0.35f), 26f);
            Round(Grow(box, 5f), new Color(0.06f, 0.05f, 0.12f, 0.97f), 26f);
            Round(box, Color.white, 22f);
            string leaderId = d.team.Count > 0 ? d.team[0] : GameDatabase.Protagonist;
            var leader = GameDatabase.GetCharacter(leaderId);
            var fc = new Vector2(box.xMax + 80f, box.center.y);
            UIStyles.CircleTex(fc, 82f, new Color(0.06f, 0.05f, 0.12f));
            if (leader != null) FaceCircle(fc, 74f, leader, UIStyles.Gold);
            UIStyles.Rect(new Rect(box.xMax - 2f, box.center.y - 14f, 20f, 28f), Color.white);
            UIStyles.Outlined(new Rect(box.x + 28f, box.y + 12f, box.width - 56f, 44f), step.title, UIStyles.Sized(UIStyles.H2, 34), new Color(1f, 0.72f, 0.1f), 3f);
            GUI.Label(new Rect(box.x + 28f, box.y + 58f, box.width - 56f, 90f), "<color=#1B1830><b>" + step.text + "</b></color>", new GUIStyle(UIStyles.Sized(UIStyles.Body, 26)) { wordWrap = true, richText = true });
            var steps = TutorialSystem.Steps;
            for (int i = 0; i < steps.Length; i++)
                UIStyles.CircleTex(new Vector2(box.x + 36f + i * 26f, box.yMax - 16f), 7f, i <= TutorialSystem.StepIndex ? new Color(1f, 0.7f, 0.1f) : new Color(0.8f, 0.8f, 0.85f));

            // SKIP (always) and NEXT (talk steps).
            var skip = new Rect(box.x, box.yMax + 18f, 250f, 64f);
            var next = new Rect(box.xMax - 220f, box.yMax + 18f, 220f, 64f);
            if (battle && Event.current.type == EventType.Repaint)
            {
                MobileControls.UiBlockers.Add(skip);
                if (step.need == 0) MobileControls.UiBlockers.Add(next);
                MobileControls.UiBlockersFrame = Time.frameCount;
            }
            if (FlatBtn(skip, "SKIP TUTORIAL", new Color(0.35f, 0.38f, 0.5f), true, 20)) { TutorialSystem.Finish(); Toast("Tutorial skipped — it won't show again."); return; }
            if (step.need == 0)
            {
                bool last = TutorialSystem.StepIndex == steps.Length - 1;
                if (FlatBtn(next, last ? "GOT IT!" : "NEXT ▶", new Color(0.12f, 0.78f, 0.38f), t > 0.4f, 24)) TutorialSystem.Next();
            }
            else if (step.need > 1)
                GUI.Label(new Rect(next.x, next.y, next.width, next.height), "<color=#FFFFFF><b>" + TutorialSystem.Progress + " / " + step.need + "</b></color>", UIStyles.Sized(UIStyles.Center, 30));
        }

        static Rect CircleRect(Vector2 c, float r) { return new Rect(c.x - r, c.y - r, r * 2f, r * 2f); }
    }
}
