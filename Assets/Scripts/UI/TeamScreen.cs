using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The TEAM screen from the reference: TEAM 1-4 tabs, a line-up of four full-body cards (element, level,
    /// stars, name, power), AUTO SET / EDIT TEAM, and on the right a roster grid with element filters and a
    /// detail card (HP / ATK / DEF) with SELECT to put the slayer into the chosen slot.
    /// </summary>
    public partial class UIManager
    {
        int rosterFilter = -1; // -1 = all, otherwise (int)Element
        string rosterPick;
        bool teamEditing;
        Vector2 rosterScroll;

        void DrawTeam()
        {
            TopBar("TEAM", teamReturn);
            var d = gm.Data;
            EnsurePresets(d);
            float x0 = safe.x + 30f, top = safe.y + 130f;

            // TEAM 1-4 tabs.
            for (int t = 0; t < 4; t++)
                if (FlatBtn(new Rect(x0 + t * 170f, top, 158f, 58f), "TEAM " + (t + 1), d.activeTeam == t ? TileGreen : new Color(0.2f, 0.2f, 0.28f), true, 24))
                    SwitchPreset(d, t);
            GUI.Label(new Rect(x0 + 690f, top + 6f, 280f, 50f), "POWER <color=#FFD36B>" + CharacterSystem.TeamPower(d).ToString("N0") + "</color>", UIStyles.Sized(UIStyles.Right, 28));

            // Line-up.
            float cw = 228f, ch = 300f, gap = 14f;
            float ly = top + 76f;
            for (int i = 0; i < TeamSize; i++)
            {
                var r = new Rect(x0 + i * (cw + gap), ly, cw, ch);
                bool sel = teamSlot == i;
                string roleName = i == 0 ? "LEADER" : SlotRoles[i].ToString().ToUpper();
                if (i < d.team.Count)
                {
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    var c = d.GetCharacter(d.team[i]);
                    if (def == null || c == null) continue;
                    PortraitCard(r, def, true, roleName, sel);
                    var info = new Rect(r.x, r.yMax + 6f, cw, 84f);
                    Round(info, new Color(0.08f, 0.08f, 0.14f, 0.9f), 10f);
                    GUI.Label(new Rect(info.x + 10f, info.y + 4f, cw - 20f, 30f), "Lv." + c.level + "  <color=#FFD36B>" + Stars(c.stars) + "</color>", UIStyles.Sized(UIStyles.Body, 20));
                    GUI.Label(new Rect(info.x + 10f, info.y + 28f, cw - 20f, 30f), def.displayName, UIStyles.Sized(UIStyles.H2, 24));
                    GUI.Label(new Rect(info.x + 10f, info.y + 54f, cw - 20f, 28f), "<color=#AAAAAA>Power</color> " + CharacterSystem.Power(d, c).ToString("N0"), UIStyles.Sized(UIStyles.Small, 20));
                    if (teamEditing && d.team.Count > 1)
                    {
                        UIStyles.CircleTex(new Vector2(r.xMax - 18f, r.y + 18f), 16f, UIStyles.Crimson);
                        GUI.Label(new Rect(r.xMax - 34f, r.y + 2f, 32f, 32f), "✕", UIStyles.Sized(UIStyles.Center, 20));
                    }
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                    {
                        if (teamEditing && d.team.Count > 1) { d.team.RemoveAt(i); SaveTeam(d); }
                        else { teamSlot = i; rosterPick = d.team[i]; }
                    }
                }
                else
                {
                    Round(r, new Color(1f, 1f, 1f, sel ? 0.14f : 0.06f), 12f);
                    RoundFrame(r, sel ? UIStyles.Gold : new Color(1f, 1f, 1f, 0.2f), sel ? 5f : 2f, 12f);
                    GUI.Label(r, "+\n<size=20>" + roleName + "</size>", UIStyles.Sized(UIStyles.Center, 60));
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none)) teamSlot = i;
                }
            }

            float by = ly + ch + 104f;
            if (FlatBtn(new Rect(x0, by, 300f, 70f), "AUTO SET", TileGreen)) AutoSet(d);
            if (FlatBtn(new Rect(x0 + 316f, by, 300f, 70f), teamEditing ? "DONE" : "EDIT TEAM", TilePurple)) teamEditing = !teamEditing;
            GUI.Label(new Rect(x0 + 632f, by + 4f, 330f, 64f), teamEditing ? "<color=#AAAAAA>Tap ✕ to remove a slayer.</color>" : "<color=#AAAAAA>Pick a slot, then a slayer.</color>", UIStyles.Sized(UIStyles.Small, 20));

            // Detail card for the highlighted slayer.
            if (string.IsNullOrEmpty(rosterPick) && d.team.Count > 0) rosterPick = d.team[Mathf.Clamp(teamSlot, 0, d.team.Count - 1)];
            var card = new Rect(x0, by + 90f, 4f * cw + 3f * gap, H - (by + 90f) - 24f);
            DrawRosterDetail(card, d);

            // Roster grid with element filters.
            float rx = x0 + 4f * (cw + gap) + 16f;
            var rp = new Rect(rx, top, safe.xMax - 30f - rx, H - top - 24f);
            Round(rp, new Color(0.06f, 0.06f, 0.12f, 0.88f), 18f);
            GUI.Label(new Rect(rp.x + 20f, rp.y + 10f, 300f, 44f), "ROSTER", UIStyles.Sized(UIStyles.H2, 30));
            float fx = rp.x + 20f, fy = rp.y + 60f;
            if (FlatBtn(new Rect(fx, fy, 90f, 52f), "ALL", rosterFilter < 0 ? TileRed : new Color(0.22f, 0.22f, 0.3f), true, 22)) rosterFilter = -1;
            for (int e = 0; e < 6; e++)
            {
                var el = (Element)e;
                var fr = new Rect(fx + 100f + e * 62f, fy, 52f, 52f);
                Round(fr, rosterFilter == e ? ElementChart.ColorOf(el) : new Color(0.22f, 0.22f, 0.3f), 26f);
                GUI.Label(fr, ElementChart.Icon(el), UIStyles.Sized(UIStyles.Center, 26));
                if (GUI.Button(fr, GUIContent.none, GUIStyle.none)) rosterFilter = rosterFilter == e ? -1 : e;
            }

            var list = new List<OwnedCharacter>();
            foreach (var c in d.characters)
            {
                var def = GameDatabase.GetCharacter(c.id);
                if (def == null) continue;
                if (rosterFilter >= 0 && (int)def.element != rosterFilter) continue;
                list.Add(c);
            }
            float gw = 150f, gh = 190f, gg = 14f;
            var view = new Rect(rp.x + 16f, fy + 70f, rp.width - 32f, rp.yMax - fy - 86f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((view.width - 20f + gg) / (gw + gg)));
            var content = new Rect(0f, 0f, view.width - 20f, Mathf.CeilToInt(list.Count / (float)cols) * (gh + gg));
            rosterScroll = GUI.BeginScrollView(view, rosterScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var c = list[i];
                var def = GameDatabase.GetCharacter(c.id);
                var r = new Rect((i % cols) * (gw + gg), (i / cols) * (gh + gg), gw, gw);
                int inTeam = d.team.IndexOf(c.id);
                PortraitCard(r, def, false, "Lv." + c.level, rosterPick == c.id);
                if (inTeam >= 0)
                {
                    UIStyles.CircleTex(new Vector2(r.xMax - 18f, r.yMax - 18f), 16f, TileGreen);
                    GUI.Label(new Rect(r.xMax - 34f, r.yMax - 34f, 32f, 32f), (inTeam + 1).ToString(), UIStyles.Sized(UIStyles.Center, 20));
                }
                GUI.Label(new Rect(r.x, r.yMax + 2f, gw, 34f), def.displayName, UIStyles.Sized(UIStyles.Center, 20));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { rosterPick = c.id; gm.Audio.Play("click", 0.4f); }
            }
            GUI.EndScrollView();
        }

        void DrawRosterDetail(Rect card, PlayerData d)
        {
            Round(card, new Color(0.08f, 0.08f, 0.15f, 0.9f), 16f);
            var c = string.IsNullOrEmpty(rosterPick) ? null : d.GetCharacter(rosterPick);
            var def = c != null ? GameDatabase.GetCharacter(c.id) : null;
            if (c == null || def == null)
            {
                GUI.Label(card, "<color=#AAAAAA>Choose a slayer from the roster.</color>", UIStyles.CenterSmall);
                return;
            }
            float ph = Mathf.Min(card.height - 24f, 200f);
            PortraitCard(new Rect(card.x + 14f, card.y + 12f, ph, ph), def, false);
            float tx = card.x + ph + 34f;
            GUI.Label(new Rect(tx, card.y + 10f, 520f, 40f), def.displayName + "  <size=22><color=#AAAAAA>" + def.versionTitle + "</color></size>", UIStyles.Sized(UIStyles.H2, 30));
            GUI.Label(new Rect(tx, card.y + 48f, 520f, 30f), ElementTag(def.element) + " " + def.element + " · " + def.role + " · " + def.style + "   Lv." + c.level + "  <color=#FFD36B>" + Stars(c.stars) + "</color>", UIStyles.Sized(UIStyles.Body, 20));
            var st = CharacterSystem.ComputeStats(d, c);
            string[] names = { "HP", "ATK", "DEF" };
            float[] vals = { st.hp, st.atk, st.def };
            Color[] cols = { TileGreen, TileRed, TileBlue };
            for (int i = 0; i < 3; i++)
            {
                var sr = new Rect(tx + i * 170f, card.y + 88f, 158f, 60f);
                Round(sr, new Color(1f, 1f, 1f, 0.06f), 10f);
                Round(new Rect(sr.x, sr.y, 6f, sr.height), cols[i], 3f);
                GUI.Label(new Rect(sr.x + 14f, sr.y + 2f, 140f, 26f), "<color=#AAAAAA>" + names[i] + "</color>", UIStyles.Sized(UIStyles.Small, 18));
                GUI.Label(new Rect(sr.x + 14f, sr.y + 24f, 140f, 34f), Mathf.RoundToInt(vals[i]).ToString("N0"), UIStyles.Sized(UIStyles.Body, 26));
            }
            float bx = card.xMax - 250f;
            int inTeam = d.team.IndexOf(c.id);
            bool same = inTeam >= 0 && inTeam == teamSlot;
            if (FlatBtn(new Rect(bx, card.y + 14f, 230f, 64f), same ? "IN SLOT" : "SELECT", TileRed, !same)) AssignToSlot(c.id, -1);
            if (FlatBtn(new Rect(bx, card.y + 90f, 230f, 58f), "DETAILS", TileBlue, true, 24))
            {
                gm.SelectedCharacterId = c.id;
                gm.GoTo(GameScreen.CharacterDetail);
            }
        }

        void EnsurePresets(PlayerData d)
        {
            if (d.teamPresets == null) d.teamPresets = new List<TeamPreset>();
            while (d.teamPresets.Count < 4) d.teamPresets.Add(new TeamPreset());
            d.activeTeam = Mathf.Clamp(d.activeTeam, 0, 3);
        }

        void SwitchPreset(PlayerData d, int n)
        {
            if (n == d.activeTeam) return;
            d.teamPresets[d.activeTeam].ids = new List<string>(d.team);
            d.activeTeam = n;
            var next = d.teamPresets[n].ids;
            next.RemoveAll(id => d.GetCharacter(id) == null);
            // An empty preset starts as a copy of the leader so there is always someone to fight.
            if (next.Count == 0 && d.team.Count > 0) next.Add(d.team[0]);
            d.team = new List<string>(next);
            teamSlot = 0;
            rosterPick = d.team.Count > 0 ? d.team[0] : null;
            SaveTeam(d);
        }

        /// <summary>Best power first, trying to fill Tank and Support slots with the right roles.</summary>
        void AutoSet(PlayerData d)
        {
            var pool = new List<OwnedCharacter>(d.characters);
            pool.RemoveAll(c => GameDatabase.GetCharacter(c.id) == null);
            pool.Sort((a, b) => CharacterSystem.Power(d, b).CompareTo(CharacterSystem.Power(d, a)));
            var team = new List<string>();
            for (int slot = 0; slot < TeamSize && pool.Count > 0; slot++)
            {
                int pick = 0;
                if (slot > 0)
                    for (int i = 0; i < pool.Count; i++)
                        if (GameDatabase.GetCharacter(pool[i].id).role == SlotRoles[slot]) { pick = i; break; }
                team.Add(pool[pick].id);
                pool.RemoveAt(pick);
            }
            if (team.Count == 0) return;
            d.team = team;
            teamSlot = 0;
            SaveTeam(d);
            gm.Audio.Play("switch", 0.6f);
            Toast("Strongest team set");
        }

        void SaveTeam(PlayerData d)
        {
            EnsurePresets(d);
            d.teamPresets[d.activeTeam].ids = new List<string>(d.team);
            gm.Save();
            gm.RefreshStage();
        }
    }
}
