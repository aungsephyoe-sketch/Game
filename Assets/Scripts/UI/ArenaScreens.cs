using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The ARENA overlay: rank badge, trophies and tier ladder, mode cards, Ranked/Casual, FIND MATCH → a
    /// "SEARCHING FOR OPPONENT..." matchmaking screen → YOUR TEAM vs OPPONENT TEAM portraits → the match.
    /// Opponents are simulated (AI) until a game server is added — the panel says so.
    /// </summary>
    public partial class UIManager
    {
        int arenaMode;
        bool arenaRanked = true;
        int arenaStage; // 0 pick, 1 searching, 2 versus
        float arenaStageAt, arenaSearchFor;
        MissionDefinition arenaMatch;

        void OpenArena()
        {
            OpenSocial(SocialPanel.Arena);
            arenaStage = 0;
            arenaMatch = null;
        }

        void DrawArenaPanel(PlayerData d, float k)
        {
            if (arenaStage == 1) { DrawMatchmaking(d); return; }
            if (arenaStage == 2) { DrawVersus(d); return; }

            var panel = new Rect(W * 0.5f - 700f, H * 0.5f - 420f + (1f - k) * 60f, 1400f, 840f);
            Round(Offset(panel, 0f, 8f), new Color(0f, 0f, 0f, 0.5f), 26f);
            Round(panel, new Color(0.07f, 0.08f, 0.14f, 0.98f), 26f);
            RoundFrame(panel, new Color(1f, 0.8f, 0.3f, 0.8f), 3f, 26f);
            CloseButton(panel);
            UIStyles.Outlined(new Rect(panel.x + 40f, panel.y + 22f, 600f, 60f), "ARENA  <size=26>3v3</size>", UIStyles.Sized(UIStyles.H1, 48), UIStyles.Gold, 3f);
            GUI.Label(new Rect(panel.x + 40f, panel.y + 82f, 1100f, 30f), "<color=#9AA0B8>Practice matches against AI slayers of your rank — live online play arrives with game servers.</color>", UIStyles.Sized(UIStyles.Body, 20));

            // Rank card.
            int tr = d.pvpTrophies, ti = PvpSystem.TierIndex(tr);
            var tier = PvpSystem.Tiers[ti];
            var rc = new Rect(panel.x + 40f, panel.y + 132f, 420f, 560f);
            Round(rc, new Color(1f, 1f, 1f, 0.05f), 20f);
            var bc = new Vector2(rc.center.x, rc.y + 140f);
            float glow = 0.18f + 0.08f * Mathf.Sin(Time.unscaledTime * 2.5f);
            UIStyles.CircleTex(bc, 118f, new Color(tier.color.r, tier.color.g, tier.color.b, glow));
            UIStyles.CircleTex(bc, 96f, new Color(0.06f, 0.05f, 0.12f));
            UIStyles.CircleTex(bc, 88f, Color.Lerp(tier.color, Color.black, 0.35f));
            UIStyles.CircleTex(bc, 72f, tier.color);
            GUI.DrawTexture(new Rect(bc.x - 40f, bc.y - 40f, 80f, 80f), IconFactory.Get("swords"), ScaleMode.ScaleToFit, true);
            UIStyles.Outlined(new Rect(rc.x, rc.y + 262f, rc.width, 50f), PvpSystem.RankName(tr).ToUpperInvariant(), UIStyles.Sized(UIStyles.Center, 38), tier.color, 3f);
            UIStyles.Outlined(new Rect(rc.x, rc.y + 312f, rc.width, 34f), tr + " trophies", UIStyles.Sized(UIStyles.Center, 26), Color.white, 2f);
            UIStyles.Bar(new Rect(rc.x + 40f, rc.y + 356f, rc.width - 80f, 14f), PvpSystem.TierProgress(tr), tier.color);
            string next = ti < PvpSystem.Tiers.Length - 1 ? (PvpSystem.Tiers[ti + 1].min - tr) + " to " + PvpSystem.Tiers[ti + 1].name : "Top tier reached";
            GUI.Label(new Rect(rc.x, rc.y + 374f, rc.width, 28f), "<color=#AAB0C8>" + next + "</color>", UIStyles.Sized(UIStyles.Center, 18));
            // Tier ladder.
            for (int i = 0; i < PvpSystem.Tiers.Length; i++)
            {
                var c = new Vector2(rc.x + 50f + i * 64f, rc.y + 440f);
                var t = PvpSystem.Tiers[i];
                bool reached = i <= ti;
                UIStyles.CircleTex(c, i == ti ? 24f : 18f, reached ? t.color : new Color(0.25f, 0.26f, 0.32f));
                GUI.Label(new Rect(c.x - 40f, c.y + 22f, 80f, 24f), (reached ? "" : "<color=#777777>") + t.name + (reached ? "" : "</color>"), UIStyles.Sized(UIStyles.Center, 13));
            }
            GUI.Label(new Rect(rc.x, rc.y + 500f, rc.width, 30f), "<color=#DDDDDD>Wins " + d.pvpWins + "   ·   Losses " + d.pvpLosses + "   ·   Best " + d.pvpBestTrophies + "</color>", UIStyles.Sized(UIStyles.Center, 19));

            // Mode cards.
            string[] icons = { "swords", "orb", "claw" };
            Color[] cols = { new Color(0.95f, 0.35f, 0.3f), new Color(0.35f, 0.6f, 1f), new Color(0.7f, 0.35f, 0.95f) };
            for (int i = 0; i < 3; i++)
            {
                var mr = new Rect(panel.x + 500f, panel.y + 132f + i * 188f, 860f, 170f);
                bool sel = arenaMode == i;
                if (sel) Round(Grow(mr, 5f), UIStyles.Gold, 22f);
                Round(mr, sel ? Color.Lerp(cols[i], new Color(0.07f, 0.08f, 0.14f), 0.55f) : new Color(1f, 1f, 1f, 0.06f), 20f);
                var ic = new Vector2(mr.x + 90f, mr.center.y);
                UIStyles.CircleTex(ic, 60f, cols[i]);
                GUI.DrawTexture(new Rect(ic.x - 34f, ic.y - 34f, 68f, 68f), IconFactory.Get(icons[i]), ScaleMode.ScaleToFit, true);
                UIStyles.Outlined(new Rect(mr.x + 180f, mr.y + 18f, 600f, 50f), PvpSystem.ModeNames[i].ToUpperInvariant(), UIStyles.Sized(UIStyles.H2, 36), Color.white, 3f);
                var descStyle = new GUIStyle(UIStyles.Sized(UIStyles.Body, 21)) { wordWrap = true };
                GUI.Label(new Rect(mr.x + 180f, mr.y + 70f, mr.width - 210f, 90f), "<color=#DDDDEE>" + PvpSystem.ModeDesc[i] + "</color>", descStyle);
                if (GUI.Button(mr, GUIContent.none, GUIStyle.none)) { arenaMode = i; Bounce(mr); gm.Audio.Play("click", 0.5f); }
            }

            // Ranked toggle + FIND MATCH.
            var tg = new Rect(panel.x + 500f, panel.yMax - 120f, 380f, 84f);
            if (FlatBtn(new Rect(tg.x, tg.y, 186f, tg.height), "RANKED", arenaRanked ? new Color(0.95f, 0.6f, 0.15f) : TileNavy, true, 26)) arenaRanked = true;
            if (FlatBtn(new Rect(tg.x + 194f, tg.y, 186f, tg.height), "CASUAL", !arenaRanked ? new Color(0.25f, 0.6f, 0.95f) : TileNavy, true, 26)) arenaRanked = false;
            var fm = new Rect(panel.xMax - 470f, panel.yMax - 130f, 430f, 104f);
            if (FlatBtn(fm, "FIND MATCH", new Color(1f, 0.78f, 0.15f), true, 40))
            {
                arenaStage = 1;
                arenaStageAt = Time.unscaledTime;
                arenaSearchFor = Random.Range(2.2f, 4f);
                gm.Audio.Play("switch", 0.6f);
            }
            GUI.Label(new Rect(panel.x + 40f, panel.yMax - 60f, 440f, 28f), "<color=#8890A8>" + (arenaRanked ? "Ranked: win trophies, climb the tiers." : "Casual: no trophies at stake.") + "</color>", UIStyles.Sized(UIStyles.Body, 18));
        }

        void DrawMatchmaking(PlayerData d)
        {
            float t = Time.unscaledTime - arenaStageAt;
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0.03f, 0.03f, 0.08f, 0.94f));
            var c = new Vector2(W * 0.5f, H * 0.42f);
            // Spinning dots around a pulsing core.
            for (int i = 0; i < 12; i++)
            {
                float a = i / 12f * Mathf.PI * 2f + t * 3f;
                float s = 8f + 8f * Mathf.Clamp01(Mathf.Sin(a * 0.5f - t * 3f));
                UIStyles.CircleTex(c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * 120f, s, new Color(1f, 0.8f, 0.3f, 0.35f + 0.6f * (i / 12f)));
            }
            UIStyles.CircleTex(c, 70f + 6f * Mathf.Sin(t * 5f), new Color(1f, 0.8f, 0.3f, 0.25f));
            GUI.DrawTexture(new Rect(c.x - 44f, c.y - 44f, 88f, 88f), IconFactory.Get("swords"), ScaleMode.ScaleToFit, true);
            string dots = new string('.', 1 + Mathf.FloorToInt(t * 2.5f) % 3);
            UIStyles.Outlined(new Rect(0f, c.y + 170f, W, 80f), "SEARCHING FOR OPPONENT" + dots, UIStyles.Sized(UIStyles.Center, 56), Color.white, 4f);
            int found = Mathf.Clamp(1 + Mathf.FloorToInt(t / arenaSearchFor * 6f), 1, 6);
            UIStyles.Outlined(new Rect(0f, c.y + 250f, W, 40f), PvpSystem.ModeNames[arenaMode] + "  ·  " + (arenaRanked ? PvpSystem.RankName(d.pvpTrophies) : "Casual") + "  ·  Slayers found " + found + " / 6",
                UIStyles.Sized(UIStyles.Center, 26), UIStyles.Gold, 2f);
            GUI.Label(new Rect(0f, c.y + 292f, W, 30f), "<color=#8890A8>" + Mathf.FloorToInt(t) + "s</color>", UIStyles.Sized(UIStyles.Center, 20));
            if (FlatBtn(new Rect(W * 0.5f - 150f, c.y + 350f, 300f, 84f), "CANCEL", new Color(0.8f, 0.25f, 0.3f), true, 30)) { arenaStage = 0; return; }
            if (t >= arenaSearchFor)
            {
                arenaMatch = PvpSystem.MakeMatch(d, arenaMode, arenaRanked);
                arenaStage = 2;
                arenaStageAt = Time.unscaledTime;
                gm.Audio.Play("perfect", 0.8f);
            }
        }

        void DrawVersus(PlayerData d)
        {
            var m = arenaMatch;
            if (m == null) { arenaStage = 0; return; }
            float t = Time.unscaledTime - arenaStageAt;
            float slide = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / 0.45f), 3f);
            // Blue and red halves split by a slanted seam.
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0.02f, 0.02f, 0.06f, 1f));
            UIStyles.Rect(new Rect(-W * 0.5f * (1f - slide), 0f, W * 0.5f, H), new Color(0.1f, 0.25f, 0.62f));
            UIStyles.Rect(new Rect(W * 0.5f + W * 0.5f * (1f - slide), 0f, W * 0.5f, H), new Color(0.6f, 0.12f, 0.16f));
            var old = GUI.matrix;
            GUIUtility.RotateAroundPivot(12f, new Vector2(W * 0.5f, H * 0.5f));
            UIStyles.Rect(new Rect(W * 0.5f - 14f, -H, 28f, H * 3f), new Color(1f, 0.85f, 0.35f));
            GUI.matrix = old;

            UIStyles.Outlined(new Rect(0f, 40f, W * 0.5f, 60f), "YOUR TEAM", UIStyles.Sized(UIStyles.Center, 48), new Color(0.75f, 0.88f, 1f), 4f);
            UIStyles.Outlined(new Rect(W * 0.5f, 40f, W * 0.5f, 60f), "OPPONENT TEAM", UIStyles.Sized(UIStyles.Center, 48), new Color(1f, 0.8f, 0.8f), 4f);
            string lead = d.team.Count > 0 ? d.team[0] : GameDatabase.Protagonist;
            string me = string.IsNullOrEmpty(d.playerName) ? "You" : d.playerName;
            for (int i = 0; i < 3; i++)
            {
                string bid = i == 0 ? lead : i - 1 < m.coopAllyIds.Count ? m.coopAllyIds[i - 1] : lead;
                string bn = i == 0 ? me : i - 1 < m.coopAllyNames.Count ? m.coopAllyNames[i - 1] : "Ally";
                string rid = i < m.pvpEnemyIds.Count ? m.pvpEnemyIds[i] : lead;
                string rn = i < m.pvpEnemyNames.Count ? m.pvpEnemyNames[i] : "Rival";
                float pk = 1f - Mathf.Pow(1f - Mathf.Clamp01((t - 0.3f - i * 0.15f) / 0.35f), 3f);
                float y = H * 0.26f + i * 220f;
                VersusCard(new Vector2(W * 0.25f - (1f - pk) * 600f, y), bid, bn, new Color(0.35f, 0.65f, 1f), i == 0);
                VersusCard(new Vector2(W * 0.75f + (1f - pk) * 600f, y), rid, rn, new Color(1f, 0.4f, 0.4f), false);
            }
            float vk = Mathf.Clamp01((t - 0.8f) / 0.3f);
            if (vk > 0f)
            {
                int vs = Mathf.RoundToInt(Mathf.Lerp(260f, 150f, vk));
                UIStyles.Outlined(new Rect(W * 0.5f - 300f, H * 0.5f - 130f, 600f, 240f), "VS", UIStyles.Sized(UIStyles.Big, vs), new Color(1f, 0.85f, 0.3f, vk), 6f);
            }
            UIStyles.Outlined(new Rect(0f, H - 150f, W, 50f), (m.pvpRanked ? "RANKED · " : "CASUAL · ") + PvpSystem.ModeNames[m.pvpMode].ToUpperInvariant(), UIStyles.Sized(UIStyles.Center, 36), Color.white, 3f);
            int left = Mathf.CeilToInt(3.4f - t);
            GUI.Label(new Rect(0f, H - 100f, W, 40f), "<color=#DDDDDD>Match starts in " + Mathf.Max(1, left) + "</color>", UIStyles.Sized(UIStyles.Center, 26));
            if (t >= 3.4f)
            {
                arenaStage = 0;
                social = SocialPanel.None;
                arenaMatch = null;
                gm.StartMission(m);
            }
        }

        void VersusCard(Vector2 c, string charId, string name, Color ring, bool you)
        {
            var def = GameDatabase.GetCharacter(charId);
            var plate = new Rect(c.x - 300f, c.y - 70f, 600f, 150f);
            Round(plate, new Color(0f, 0f, 0f, 0.35f), 24f);
            if (def != null) FaceCircle(new Vector2(plate.x + 90f, c.y + 5f), 62f, def, you ? UIStyles.Gold : ring);
            UIStyles.Outlined(new Rect(plate.x + 180f, c.y - 44f, 400f, 50f), name.Replace("<", "‹"), UIStyles.Sized(UIStyles.H2, 36), you ? UIStyles.Gold : Color.white, 3f);
            if (def != null) GUI.Label(new Rect(plate.x + 180f, c.y + 8f, 400f, 36f), "<color=#DDDDDD>" + def.displayName + "</color>", UIStyles.Sized(UIStyles.Body, 24));
        }
    }
}
