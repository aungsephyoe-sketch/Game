using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// First launch: the game asks the player's name (checked by the name filter), age and birthday. Also the
    /// birthday gift popup and the leader's "Hi, name!" greeting on the home screen.
    /// </summary>
    public partial class UIManager
    {
        string nameInput = "";
        int ageInput = 12, monthInput = 1, dayInput = 1;
        string nameError = "";
        static readonly string[] MonthNames = { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

        void ModalBackdrop(float k)
        {
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.72f * k));
        }

        bool Stepper(Rect r, string label, string value, out int delta)
        {
            delta = 0;
            GUI.Label(new Rect(r.x, r.y, r.width, 30f), "<color=#BBBBBB>" + label + "</color>", UIStyles.Sized(UIStyles.Body, 20));
            var box = new Rect(r.x, r.y + 34f, r.width, 64f);
            Round(box, new Color(1f, 1f, 1f, 0.07f), 14f);
            if (FlatBtn(new Rect(box.x + 6f, box.y + 6f, 52f, 52f), "‹", TileNavy, true, 32)) delta = -1;
            if (FlatBtn(new Rect(box.xMax - 58f, box.y + 6f, 52f, 52f), "›", TileNavy, true, 32)) delta = 1;
            GUI.Label(new Rect(box.x + 60f, box.y, box.width - 120f, box.height), "<b>" + value + "</b>", UIStyles.Sized(UIStyles.Center, 28));
            return delta != 0;
        }

        void DrawProfileSetup(PlayerData d)
        {
            ModalBackdrop(1f);
            var panel = new Rect(W * 0.5f - 520f, H * 0.5f - 400f, 1040f, 800f);
            Round(Offset(panel, 0f, 8f), new Color(0f, 0f, 0f, 0.5f), 26f);
            Round(panel, new Color(0.07f, 0.08f, 0.14f, 0.98f), 26f);
            RoundFrame(panel, new Color(1f, 0.8f, 0.3f, 0.9f), 3f, 26f);
            UIStyles.Outlined(new Rect(panel.x, panel.y + 26f, panel.width, 64f), "WELCOME, SLAYER!", UIStyles.Sized(UIStyles.H1, 52), UIStyles.Gold, 3f);
            GUI.Label(new Rect(panel.x + 60f, panel.y + 92f, panel.width - 120f, 40f), "<color=#CCCCCC>Tell us a little about yourself.</color>", UIStyles.Sized(UIStyles.Center, 24));

            float x = panel.x + 70f, w = panel.width - 140f, y = panel.y + 150f;
            GUI.Label(new Rect(x, y, w, 30f), "<color=#BBBBBB>What's your name?</color>", UIStyles.Sized(UIStyles.Body, 20));
            var field = new Rect(x, y + 34f, w, 70f);
            Round(field, new Color(1f, 1f, 1f, 0.1f), 14f);
            RoundFrame(field, string.IsNullOrEmpty(nameError) ? new Color(1f, 1f, 1f, 0.25f) : UIStyles.Bad, 2f, 14f);
            var fs = new GUIStyle(UIStyles.Sized(UIStyles.Body, 32)) { alignment = TextAnchor.MiddleLeft, padding = new RectOffset(20, 20, 0, 0) };
            fs.normal.background = null; fs.focused.background = null; fs.hover.background = null;
            fs.normal.textColor = fs.focused.textColor = fs.hover.textColor = Color.white;
            GUI.SetNextControlName("PlayerName");
            string before = nameInput;
            nameInput = GUI.TextField(field, nameInput, 14, fs);
            if (nameInput != before) nameError = "";
            if (string.IsNullOrEmpty(nameInput) && GUI.GetNameOfFocusedControl() != "PlayerName")
                GUI.Label(new Rect(field.x + 20f, field.y, field.width, field.height), "<color=#777777>Tap to type</color>", UIStyles.Sized(UIStyles.Body, 28));
            if (!string.IsNullOrEmpty(nameError))
                GUI.Label(new Rect(x, field.yMax + 4f, w, 30f), "<color=#FF7A6A>" + nameError + "</color>", UIStyles.Sized(UIStyles.Small, 19));
            y = field.yMax + 50f;

            int delta;
            float cw = (w - 40f) / 3f;
            if (Stepper(new Rect(x, y, cw, 100f), "How old are you?", ageInput.ToString(), out delta)) ageInput = Mathf.Clamp(ageInput + delta, 4, 100);
            if (Stepper(new Rect(x + cw + 20f, y, cw, 100f), "Birthday month", MonthNames[monthInput - 1], out delta))
            {
                monthInput = (monthInput - 1 + delta + 12) % 12 + 1;
                dayInput = Mathf.Min(dayInput, System.DateTime.DaysInMonth(2024, monthInput));
            }
            int days = System.DateTime.DaysInMonth(2024, monthInput);
            if (Stepper(new Rect(x + (cw + 20f) * 2f, y, cw, 100f), "Birthday day", dayInput.ToString(), out delta)) dayInput = (dayInput - 1 + delta + days) % days + 1;
            y += 130f;
            GUI.Label(new Rect(x, y, w, 60f), "<color=#FFD36B>On your birthday every year you'll get " + ProfileSystem.BirthdayDiamonds + " diamonds and " + ProfileSystem.BirthdayGold.ToString("N0") + " gold!</color>", UIStyles.Sized(UIStyles.Center, 21));
            y += 70f;
            GUI.Label(new Rect(x, y, w, 30f), "<color=#888888>Your name is shown in the game. Please keep it friendly — some names aren't allowed.</color>", UIStyles.Sized(UIStyles.CenterSmall, 17));

            if (FlatBtn(new Rect(panel.center.x - 220f, panel.yMax - 120f, 440f, 90f), "START", TileGreen, true, 36))
            {
                string reason;
                if (!ProfileSystem.CheckName(nameInput, out reason)) { nameError = reason; gm.Audio.Play("click", 0.4f); return; }
                ProfileSystem.Save(d, nameInput, ageInput, monthInput, dayInput);
                gm.Save();
                gm.Audio.Play("perfect", 0.8f);
                greetingAt = -1f;
                greetingDone = false;
            }
        }

        float birthdayShownAt = -1f;

        void DrawBirthday(PlayerData d)
        {
            if (birthdayShownAt < 0f) { birthdayShownAt = Time.unscaledTime; gm.Audio.Play("victory", 0.7f); }
            float t = Time.unscaledTime - birthdayShownAt;
            float k = Mathf.Clamp01(t / 0.35f);
            ModalBackdrop(k);
            // Confetti.
            var rng = new System.Random(3);
            for (int i = 0; i < 60; i++)
            {
                float cx = (float)rng.NextDouble() * W, speed = 80f + (float)rng.NextDouble() * 120f;
                float cy = Mathf.Repeat((float)rng.NextDouble() * H + t * speed, H + 40f) - 20f;
                Color cc = Color.HSVToRGB((float)rng.NextDouble(), 0.7f, 1f);
                UIStyles.Rect(new Rect(cx + Mathf.Sin(t * 3f + i) * 12f, cy, 10f, 16f), new Color(cc.r, cc.g, cc.b, 0.9f * k));
            }
            var panel = new Rect(W * 0.5f - 480f, H * 0.5f - 300f, 960f, 600f);
            Round(panel, new Color(0.08f, 0.07f, 0.14f, 0.97f), 26f);
            RoundFrame(panel, new Color(1f, 0.55f, 0.75f), 3f, 26f);
            if (k < 1f) return;
            UIStyles.Outlined(new Rect(panel.x, panel.y + 30f, panel.width, 70f), "HAPPY BIRTHDAY!", UIStyles.Sized(UIStyles.H1, 60), new Color(1f, 0.6f, 0.8f), 3f);
            GUI.Label(new Rect(panel.x, panel.y + 104f, panel.width, 44f), "<b>" + d.playerName + "</b>, the whole team wishes you a wonderful day!", UIStyles.Sized(UIStyles.Center, 26));
            Vector2 c1 = new Vector2(panel.center.x - 170f, panel.y + 290f), c2 = new Vector2(panel.center.x + 170f, panel.y + 290f);
            float bob = Mathf.Sin(Time.unscaledTime * 3f) * 5f;
            DiamondIcon(c1 + Vector2.up * bob, 120f);
            CoinIcon(c2 - Vector2.up * bob, 110f);
            UIStyles.Outlined(new Rect(c1.x - 150f, c1.y + 70f, 300f, 50f), "+" + ProfileSystem.BirthdayDiamonds, UIStyles.Sized(UIStyles.H1, 44), new Color(0.55f, 0.85f, 1f), 2f);
            UIStyles.Outlined(new Rect(c2.x - 150f, c2.y + 70f, 300f, 50f), "+" + ProfileSystem.BirthdayGold.ToString("N0"), UIStyles.Sized(UIStyles.H1, 44), new Color(1f, 0.82f, 0.3f), 2f);
            if (FlatBtn(new Rect(panel.center.x - 200f, panel.yMax - 110f, 400f, 84f), "THANK YOU!", new Color(0.85f, 0.35f, 0.6f), true, 32))
            {
                ProfileSystem.ClaimBirthday(d);
                gm.Save();
                gm.Audio.Play("gem", 0.8f);
                gm.Audio.Play("coinSpill", 0.7f);
                if (gm.Home != null) gm.Home.Celebrate(new Color(1f, 0.6f, 0.8f));
                birthdayShownAt = -1f;
            }
        }

        float greetingAt = -1f;
        bool greetingDone;
        string greetingText;

        /// <summary>Once per session on the home screen, the leader says hi to the player by name.</summary>
        void DrawGreeting(PlayerData d)
        {
            if (greetingDone || !d.profileDone) return;
            if (greetingAt < 0f)
            {
                greetingAt = Time.unscaledTime + 0.8f;
                greetingText = ProfileSystem.Greeting(d);
            }
            float t = Time.unscaledTime - greetingAt;
            if (t < 0f) return;
            if (t > 6.5f) { greetingDone = true; return; }
            if (t < 0.05f) gm.Audio.Play("perfect", 0.4f);
            float k = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((6.5f - t) / 0.4f);
            // Anchor the bubble over the leader standing in the village; fall back to the right side of the screen.
            Vector2 anchor = new Vector2(W - 520f, H * 0.42f);
            var cam = Camera.main;
            if (cam != null && gm.Home != null && gm.Home.LeaderVisible)
            {
                Vector3 sp = cam.WorldToScreenPoint(gm.Home.LeaderHead);
                if (sp.z > 0f) anchor = new Vector2(sp.x / HudLayout.Scale, (Screen.height - sp.y) / HudLayout.Scale);
            }
            anchor.x = Mathf.Clamp(anchor.x, safe.x + 1000f, safe.xMax - 260f);
            anchor.y = Mathf.Clamp(anchor.y, safe.y + 240f, H - 200f);
            float w = Mathf.Clamp(greetingText.Length * 13f, 360f, 560f);
            float pop = Mathf.Lerp(0.6f, 1f, Mathf.Clamp01(t / 0.25f));
            var r = new Rect(anchor.x - w * 0.5f * pop, anchor.y - 130f * pop, w * pop, 110f * pop);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, k);
            Round(Offset(r, 0f, 5f), new Color(0f, 0f, 0f, 0.25f), 20f);
            Round(r, new Color(1f, 0.98f, 0.93f, 0.97f), 20f);
            // Tail pointing at the leader.
            for (int i = 0; i < 4; i++) Round(new Rect(anchor.x - 12f + i * 3f, r.yMax - 2f + i * 5f, 24f - i * 6f, 8f), new Color(1f, 0.98f, 0.93f, 0.97f), 3f);
            var lead = d.team.Count > 0 ? GameDatabase.GetCharacter(d.team[0]) : null;
            GUI.Label(new Rect(r.x + 18f, r.y + 8f, r.width - 36f, 30f), "<color=#8A4B2A><b>" + (lead != null ? lead.displayName : "Ren") + "</b></color>", UIStyles.Sized(UIStyles.Small, 19));
            GUI.Label(new Rect(r.x + 18f, r.y + 36f, r.width - 36f, r.height - 40f), "<color=#222222>" + greetingText + "</color>", UIStyles.Sized(UIStyles.Body, 22));
            GUI.color = old;
        }
    }
}
