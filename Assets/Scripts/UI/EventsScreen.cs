using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// EVENTS: five limited events on the left, each with its own mini story; the chosen event's story, prize
    /// and its five quests (Easy → Medium → Hard) on the right.
    /// </summary>
    public partial class UIManager
    {
        int eventPick;

        /// <summary>Home tile badge: quests that are open and not yet cleared.</summary>
        int EventBadge(PlayerData d)
        {
            int n = 0;
            foreach (var ev in GameDatabase.Events)
                foreach (var q in ev.quests)
                    if (d.IsMissionUnlocked(q) && !d.IsMissionCleared(q.id)) { n++; break; }
            return n;
        }

        static string DifficultyName(int d) { return d <= 0 ? "EASY" : d == 1 ? "MEDIUM" : "HARD"; }
        static Color DifficultyColor(int d) { return d <= 0 ? new Color(0.3f, 0.8f, 0.4f) : d == 1 ? new Color(1f, 0.6f, 0.2f) : new Color(0.95f, 0.25f, 0.25f); }

        void DrawEvents()
        {
            TopBar("EVENTS", GameScreen.MainMenu);
            var d = gm.Data;
            var events = GameDatabase.Events;
            if (events.Count == 0) return;
            eventPick = Mathf.Clamp(eventPick, 0, events.Count - 1);
            float top = safe.y + 124f;
            float k = Enter(0f, 0.4f);

            // ---- Event banners down the left.
            float lw = 560f, lh = (H - top - 24f - (events.Count - 1) * 12f) / events.Count;
            for (int i = 0; i < events.Count; i++)
            {
                var ev = events[i];
                float ek = Enter(0.05f + i * 0.06f, 0.35f);
                var r = new Rect(safe.x + 24f - (1f - ek) * 200f, top + i * (lh + 12f), lw, lh);
                bool picked = i == eventPick;
                int done = 0;
                foreach (var q in ev.quests) if (d.IsMissionCleared(q.id)) done++;
                Color a = ev.accent;
                Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.4f), 16f);
                for (int g = 0; g < 4; g++)
                    Round(new Rect(r.x, r.y, r.width * (1f - g * 0.2f), r.height), new Color(a.r * 0.5f, a.g * 0.5f, a.b * 0.5f, 0.25f + (picked ? 0.1f : 0f)), 16f);
                Round(r, new Color(0.05f, 0.05f, 0.09f, picked ? 0.5f : 0.7f), 16f);
                if (picked) RoundFrame(r, a, 3f, 16f);
                // Banner boss portrait on the right.
                var boss = GameDatabase.GetEnemy(ev.bannerEnemy);
                var pr = new Rect(r.xMax - lh + 10f, r.y + 10f, lh - 20f, lh - 20f);
                UIStyles.CircleTex(pr.center, pr.width * 0.55f, new Color(a.r, a.g, a.b, 0.2f));
                if (boss != null)
                {
                    var art = ArtLibrary.Monster(boss);
                    if (art != null) GUI.DrawTexture(pr, art, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, pr.width * 0.5f);
                }
                UIStyles.Outlined(new Rect(r.x + 22f, r.y + 12f, lw - lh - 30f, 40f), ev.title, UIStyles.Sized(UIStyles.H2, 28), Color.white, 1.5f);
                GUI.Label(new Rect(r.x + 22f, r.y + 50f, lw - lh - 30f, 30f), "<color=#CCCCCC>" + ev.subtitle + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                // Progress pips.
                for (int p = 0; p < ev.quests.Count; p++)
                {
                    bool clr = d.IsMissionCleared(ev.quests[p].id);
                    UIStyles.CircleTex(new Vector2(r.x + 32f + p * 26f, r.yMax - 26f), 9f, clr ? a : new Color(1f, 1f, 1f, 0.2f));
                }
                GUI.Label(new Rect(r.x + 32f + ev.quests.Count * 26f, r.yMax - 42f, 200f, 32f), done == ev.quests.Count ? "<color=#7CFF8A>COMPLETE ✓</color>" : "<color=#BBBBBB>" + done + "/" + ev.quests.Count + "</color>", UIStyles.Sized(UIStyles.Body, 18));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && !picked) { eventPick = i; gm.Audio.Play("switch", 0.5f); eventScroll = Vector2.zero; }
            }

            // ---- The chosen event.
            var e = events[eventPick];
            var panel = new Rect(safe.x + 24f + lw + 20f + (1f - k) * 200f, top, safe.xMax - 24f - (safe.x + 24f + lw + 20f), H - top - 24f);
            Round(Offset(panel, 0f, 6f), new Color(0f, 0f, 0f, 0.4f), 18f);
            Round(panel, new Color(0.05f, 0.06f, 0.1f, 0.94f), 18f);
            Round(new Rect(panel.x, panel.y, panel.width, 8f), e.accent, 4f);
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f;
            UIStyles.Outlined(new Rect(x, y, w, 60f), e.title, UIStyles.Sized(UIStyles.H1, 46), e.accent, 2f);
            y += 62f;
            GUI.Label(new Rect(x, y, w, 120f), "<i><color=#DADAE6>" + e.story + "</color></i>", UIStyles.Sized(UIStyles.Body, 20));
            y += 124f;
            var prize = new Rect(x, y, w, 50f);
            Round(prize, new Color(e.accent.r, e.accent.g, e.accent.b, 0.15f), 12f);
            GUI.Label(new Rect(prize.x + 16f, prize.y, prize.width - 32f, prize.height), "<b>EVENT PRIZE</b>   <color=#FFD36B>" + e.prizeText + "</color>   <color=#999999>(clear the Hard quest)</color>", UIStyles.Sized(UIStyles.Body, 20));
            y += 62f;

            // Quests: difficulty chip, number, name + its story beat, level, stars, PLAY.
            var view = new Rect(panel.x + 14f, y, panel.width - 28f, panel.yMax - y - 16f);
            float rowH = 118f;
            var content = new Rect(0f, 0f, view.width - (e.quests.Count * (rowH + 10f) > view.height ? 22f : 0f), e.quests.Count * (rowH + 10f));
            eventScroll = GUI.BeginScrollView(view, eventScroll, content);
            for (int i = 0; i < e.quests.Count; i++)
            {
                var q = e.quests[i];
                bool unlocked = d.IsMissionUnlocked(q), cleared = d.IsMissionCleared(q.id);
                var r = new Rect(14f, i * (rowH + 10f), content.width - 28f, rowH);
                Color dc = DifficultyColor(q.difficulty);
                Round(r, unlocked ? new Color(1f, 1f, 1f, 0.05f) : new Color(0f, 0f, 0f, 0.3f), 14f);
                Round(new Rect(r.x, r.y + 10f, 6f, r.height - 20f), dc, 3f);
                var chip = new Rect(r.x + 22f, r.y + 14f, 96f, 30f);
                Round(chip, dc, 15f);
                GUI.Label(chip, "<b>" + DifficultyName(q.difficulty) + "</b>", UIStyles.Sized(UIStyles.Center, 15));
                GUI.Label(new Rect(chip.xMax + 12f, r.y + 10f, 300f, 36f), "<color=#AAAAAA>QUEST " + (i + 1) + "</color>   <color=#BBBBBB>Lv. " + q.enemyLevel + "</color>", UIStyles.Sized(UIStyles.Body, 18));
                GUI.Label(new Rect(r.x + 22f, r.y + 46f, r.width - 330f, 34f), "<b>" + q.name + "</b>", UIStyles.Sized(UIStyles.Body, 24));
                GUI.Label(new Rect(r.x + 22f, r.y + 78f, r.width - 330f, 36f), "<color=#AAAAB8>" + q.storyText + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                if (!string.IsNullOrEmpty(q.bossId)) UIStyles.Outlined(new Rect(r.xMax - 300f, r.y + 10f, 80f, 30f), "BOSS", UIStyles.Sized(UIStyles.Center, 18), new Color(1f, 0.35f, 0.3f), 1.5f);
                if (unlocked) StarsIcons(new Vector2(r.xMax - 300f, r.y + 60f), 26f, d, q);
                var pb = new Rect(r.xMax - 190f, r.y + 24f, 170f, 70f);
                if (!unlocked)
                {
                    LockIcon(new Vector2(pb.center.x, pb.center.y - 6f), 34f, new Color(1f, 1f, 1f, 0.6f));
                    GUI.Label(new Rect(pb.x, pb.yMax - 18f, pb.width, 24f), "<color=#999999>Clear quest " + i + "</color>", UIStyles.Sized(UIStyles.Center, 15));
                }
                else if (FlatBtn(pb, cleared ? "REPLAY" : "PLAY", cleared ? new Color(0.2f, 0.24f, 0.4f) : TileRed, true, 26))
                {
                    gm.SelectedMission = q;
                    gm.GoTo(GameScreen.MissionDetail);
                }
            }
            GUI.EndScrollView();
        }

        Vector2 eventScroll;
    }
}
