using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>PvP battle HUD: blue vs red scoreboard with the match clock, the mode's goal, rosters, kill feed and respawn countdown.</summary>
    public partial class UIManager
    {
        static readonly Color PvpBlue = new Color(0.22f, 0.52f, 1f), PvpRed = new Color(1f, 0.27f, 0.3f);

        void DrawPvpHud(BattleController b)
        {
            var pm = PvpMatch.Current;
            if (pm == null) return;
            int mode = pm.Mode;

            // Scoreboard (top centre).
            float cx = W * 0.5f, y = safe.y + 18f;
            var blue = new Rect(cx - 330f, y, 230f, 86f);
            var red = new Rect(cx + 100f, y, 230f, 86f);
            var clock = new Rect(cx - 92f, y - 4f, 184f, 94f);
            FlatBtnLook(blue, PvpBlue);
            FlatBtnLook(red, PvpRed);
            FlatBtnLook(clock, new Color(0.12f, 0.12f, 0.2f));
            string bs = Mathf.FloorToInt(pm.BlueScore).ToString(), rs = Mathf.FloorToInt(pm.RedScore).ToString();
            UIStyles.Outlined(new Rect(blue.x, blue.y + 2f, blue.width, 50f), bs, UIStyles.Sized(UIStyles.Center, 46), Color.white, 3f);
            UIStyles.Outlined(new Rect(blue.x, blue.y + 48f, blue.width, 28f), "YOUR TEAM", UIStyles.Sized(UIStyles.Center, 18), new Color(0.85f, 0.92f, 1f), 2f);
            UIStyles.Outlined(new Rect(red.x, red.y + 2f, red.width, 50f), rs, UIStyles.Sized(UIStyles.Center, 46), Color.white, 3f);
            UIStyles.Outlined(new Rect(red.x, red.y + 48f, red.width, 28f), "OPPONENTS", UIStyles.Sized(UIStyles.Center, 18), new Color(1f, 0.88f, 0.88f), 2f);
            int t = Mathf.CeilToInt(pm.TimeLeft);
            bool hurry = t <= 20;
            float pulse = hurry ? 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 10f) : 1f;
            int ts = Mathf.RoundToInt(40 * pulse);
            UIStyles.Outlined(new Rect(clock.x, clock.y + 6f, clock.width, 50f), t / 60 + ":" + (t % 60).ToString("00"), UIStyles.Sized(UIStyles.Center, ts), hurry ? new Color(1f, 0.45f, 0.4f) : Color.white, 3f);
            string goal = mode == 0 ? "FIRST TO " + PvpSystem.BattleTarget : mode == 1 ? "HOLD · " + PvpSystem.CrystalTarget : "BOSS DAMAGE";
            UIStyles.Outlined(new Rect(clock.x, clock.y + 54f, clock.width, 26f), goal, UIStyles.Sized(UIStyles.Center, 16), UIStyles.Gold, 2f);
            // Progress bars toward the target.
            if (mode != 2)
            {
                float target = mode == 0 ? PvpSystem.BattleTarget : PvpSystem.CrystalTarget;
                UIStyles.Bar(new Rect(blue.x + 14f, blue.yMax + 10f, blue.width - 28f, 8f), pm.BlueScore / target, PvpBlue);
                UIStyles.Bar(new Rect(red.x + 14f, red.yMax + 10f, red.width - 28f, 8f), pm.RedScore / target, PvpRed);
            }
            string modeLine = (b.Def.pvpRanked ? "RANKED · " : "") + PvpSystem.ModeNames[mode].ToUpperInvariant() + "   <color=#9AA0B8><size=14>AI PRACTICE MATCH</size></color>";
            UIStyles.Outlined(new Rect(cx - 400f, y + 104f, 800f, 30f), modeLine, UIStyles.Sized(UIStyles.Center, 20), Color.white, 2f);
            if (mode == 1)
            {
                string own = pm.CrystalOwner == 1 ? "<color=#7FB2FF>YOUR TEAM HOLDS THE CRYSTAL</color>" : pm.CrystalOwner == -1 ? "<color=#FF8080>OPPONENTS HOLD THE CRYSTAL</color>" : "<color=#DDDDDD>CRYSTAL CONTESTED</color>";
                UIStyles.Outlined(new Rect(cx - 400f, y + 134f, 800f, 30f), own, UIStyles.Sized(UIStyles.Center, 20), Color.white, 2f);
            }
            if (mode == 2 && pm.Boss != null && pm.Boss.IsAlive)
            {
                var br = new Rect(cx - 260f, y + 140f, 520f, 20f);
                UIStyles.Outlined(new Rect(br.x, br.yMax + 2f, br.width, 26f), pm.Boss.Def.displayName, UIStyles.Sized(UIStyles.Center, 18), Color.white, 2f);
                UIStyles.Bar(br, pm.Boss.Health.Normalized, new Color(0.85f, 0.2f, 0.9f));
            }

            // Rosters (left).
            var roster = new Rect(safe.x + 24f, safe.y + 20f, 360f, 250f);
            Round(roster, new Color(0.04f, 0.05f, 0.08f, 0.62f), 14f);
            float ry = roster.y + 10f;
            var me = b.Team.Active;
            RosterRow(ref ry, roster.x, "You", me != null && me.IsAlive, me != null ? me.Health.Normalized : 0f, PvpBlue);
            foreach (var p in PartySlayer.Party) if (p != null && p.Team == CombatTeam.Player) RosterRow(ref ry, roster.x, p.DisplayName, !p.Down, p.Health.Normalized, PvpBlue);
            ry += 8f;
            foreach (var p in PartySlayer.Party) if (p != null && p.Team == CombatTeam.Enemy) RosterRow(ref ry, roster.x, p.DisplayName, !p.Down, p.Health.Normalized, PvpRed);

            // Kill feed (right, under the pause button).
            float fy = HudLayout.Pause.yMax + 16f;
            float fade = Mathf.Clamp01(1f - (Time.time - pm.FeedAt - 6f) / 1.5f);
            for (int i = 0; i < pm.Feed.Count; i++)
            {
                var fr = new Rect(W - safe.x - 540f, fy + i * 40f, 520f, 36f);
                Round(fr, new Color(0f, 0f, 0f, 0.5f * fade), 10f);
                GUI.color = new Color(1f, 1f, 1f, fade);
                GUI.Label(new Rect(fr.x + 12f, fr.y + 4f, fr.width - 24f, 30f), pm.Feed[i], UIStyles.Sized(UIStyles.Body, 19));
                GUI.color = Color.white;
            }

            // Respawn countdown.
            if (pm.RespawnIn > 0f)
            {
                var rr = new Rect(cx - 260f, H * 0.36f, 520f, 120f);
                Round(rr, new Color(0f, 0f, 0f, 0.55f), 20f);
                UIStyles.Outlined(new Rect(rr.x, rr.y + 8f, rr.width, 40f), "KNOCKED OUT", UIStyles.Sized(UIStyles.Center, 30), new Color(1f, 0.5f, 0.45f), 3f);
                UIStyles.Outlined(new Rect(rr.x, rr.y + 50f, rr.width, 60f), "Respawn in " + Mathf.CeilToInt(pm.RespawnIn), UIStyles.Sized(UIStyles.Center, 44), Color.white, 3f);
            }
        }

        void RosterRow(ref float y, float x, string name, bool alive, float hp, Color team)
        {
            UIStyles.CircleTex(new Vector2(x + 26f, y + 17f), 9f, alive ? team : new Color(0.35f, 0.35f, 0.4f));
            GUI.Label(new Rect(x + 44f, y, 170f, 34f), (alive ? "" : "<color=#888888>") + name.Replace("<", "‹") + (alive ? "" : "</color>"), UIStyles.Sized(UIStyles.Body, 20));
            UIStyles.Bar(new Rect(x + 214f, y + 12f, 130f, 10f), alive ? hp : 0f, alive ? team : new Color(0.3f, 0.3f, 0.35f));
            y += 36f;
        }
    }
}
