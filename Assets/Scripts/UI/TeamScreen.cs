using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The TEAM screen from the reference sheets.
    ///   Line-up: TEAM 1-4 tabs down the left, the team standing in 3D in the village with element, level,
    ///            stars, name and power under each, a + slot for empty places, team power, AUTO SET and EDIT TEAM.
    ///   Edit:    element filters down the left, a grid of slayer cards, and a detail card with HP / ATK / DEF
    ///            and SELECT to place the slayer in the chosen slot.
    /// </summary>
    public partial class UIManager
    {
        int rosterFilter = -1; // -1 = all, otherwise (int)Element
        string rosterPick;
        bool teamEditing;
        Vector2 rosterScroll;

        static readonly int[] TeamUnlockClears = { 0, 5, 15, 30 };

        static bool TeamUnlocked(PlayerData d, int t) { return t == 0 || d.missionsCleared >= TeamUnlockClears[t] || d.activeTeam == t; }

        static readonly Element[] FilterOrder = { Element.Water, Element.Flame, Element.Thunder, Element.Beast, Element.Dark, Element.Light };

        int teamDragSlot = -1;
        float teamDragLast, teamDragDistance;

        void HandleTeamDrag(PlayerData d)
        {
            if (teamDragSlot < 0) return;
            var ev = Event.current;
            if (ev.type == EventType.MouseDrag)
            {
                float dx = ev.mousePosition.x - teamDragLast;
                teamDragLast = ev.mousePosition.x;
                teamDragDistance += Mathf.Abs(dx);
                gm.Home.RotateLineupSlot(teamDragSlot, -dx * 0.6f);
                ev.Use();
            }
            else if (ev.type == EventType.MouseUp)
            {
                int i = teamDragSlot;
                teamDragSlot = -1;
                ev.Use();
                if (teamDragDistance < 10f && i < d.team.Count)
                {
                    gm.Audio.Play("click", 0.5f);
                    teamSlot = i;
                    rosterPick = d.team[i];
                    gm.Home.LineupCheer(i);
                }
            }
        }

        void DrawTeam()
        {
            var d = gm.Data;
            EnsurePresets(d);
            gm.Home.ShowLineup(d.team);
            if (teamEditing)
            {
                // BACK in the edit view returns to the line-up.
                TopBar("TEAM", GameScreen.Team, () => { teamEditing = false; });
                DrawTeamEdit(d);
                return;
            }
            TopBar("TEAM", teamReturn);
            float x0 = safe.x + 30f, top = safe.y + 136f;

            // Team tabs down the left.
            for (int t = 0; t < 4; t++)
            {
                var r = new Rect(x0, top + t * 92f, 300f, 78f);
                bool on = d.activeTeam == t;
                bool open = TeamUnlocked(d, t);
                Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.35f), 12f);
                Round(r, on ? TileRed : new Color(0.08f, 0.09f, 0.14f, 0.88f), 12f);
                if (on) RoundFrame(Grow(r, 2f), new Color(1f, 0.5f, 0.45f, 0.8f), 2f, 13f);
                var o = GUI.color;
                GUI.color = open ? Color.white : new Color(1f, 1f, 1f, 0.45f);
                GUI.DrawTexture(new Rect(r.x + 26f, r.y + 20f, 38f, 38f), IconFactory.Get(!open ? "lock" : "group"), ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(r.x + 84f, r.y, 200f, r.height), "Team " + (t + 1), UIStyles.Sized(UIStyles.Body, 28));
                GUI.color = o;
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    gm.Audio.Play("click", 0.5f);
                    if (open) SwitchPreset(d, t);
                    else Toast("Team " + (t + 1) + " unlocks after clearing " + TeamUnlockClears[t] + " missions.");
                }
            }
            var pw = new Rect(x0, top + 4f * 92f + 20f, 300f, 90f);
            Round(pw, new Color(0.06f, 0.07f, 0.11f, 0.9f), 12f);
            GUI.Label(new Rect(pw.x + 22f, pw.y, 150f, pw.height), "Team Power", UIStyles.Sized(UIStyles.Body, 24));
            UIStyles.Colored(new Rect(pw.x + 150f, pw.y, 130f, pw.height), CharacterSystem.TeamPower(d).ToString("N0"), UIStyles.Sized(UIStyles.Right, 34), new Color(1f, 0.82f, 0.25f));

            // The slayers standing in the village: info under each, LEADER above the first.
            var cam = Camera.main;
            float s = HudLayout.Scale;
            for (int i = 0; i < TeamSize && cam != null; i++)
            {
                Vector3 feet = gm.Home.LineupSlot(i);
                Vector3 sp = cam.WorldToScreenPoint(feet);
                Vector3 hp = cam.WorldToScreenPoint(feet + Vector3.up * 2.35f);
                if (sp.z <= 0f) continue;
                var fp = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                var headP = new Vector2(hp.x / s, (Screen.height - hp.y) / s);
                var hit = new Rect(fp.x - 95f, headP.y, 190f, fp.y - headP.y + 10f);
                if (i < d.team.Count)
                {
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    var c = d.GetCharacter(d.team[i]);
                    if (def == null || c == null) continue;
                    if (i == 0)
                    {
                        var lr = new Rect(headP.x - 70f, headP.y - 44f, 140f, 38f);
                        Round(lr, new Color(0.12f, 0.2f, 0.45f, 0.95f), 6f);
                        RoundFrame(lr, new Color(0.5f, 0.7f, 1f, 0.6f), 2f, 6f);
                        GUI.Label(lr, "LEADER", UIStyles.Sized(UIStyles.Center, 22));
                    }
                    if (teamSlot == i) RoundFrame(Grow(hit, 4f), new Color(1f, 0.85f, 0.35f, 0.5f + 0.3f * Mathf.Sin(Time.unscaledTime * 4f)), 3f, 14f);
                    // Info block under the feet.
                    float iy = fp.y + 8f;
                    Color ec = ElementChart.ColorOf(def.element);
                    UIStyles.CircleTex(new Vector2(fp.x - 72f, iy + 20f), 20f, Color.Lerp(ec, Color.black, 0.25f));
                    GUI.DrawTexture(new Rect(fp.x - 86f, iy + 6f, 28f, 28f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
                    UIStyles.Outlined(new Rect(fp.x - 44f, iy, 140f, 40f), "Lv. " + c.level, UIStyles.Sized(UIStyles.Body, 26), Color.white, 2f);
                    StarStrip(new Vector2(fp.x - 44f, iy + 40f), c.stars, 22f, c.awaken);
                    UIStyles.Outlined(new Rect(fp.x - 110f, iy + 66f, 220f, 32f), def.displayName, UIStyles.Sized(UIStyles.Center, 22), Color.white, 2f);
                    UIStyles.Outlined(new Rect(fp.x - 110f, iy + 94f, 220f, 30f), "Power " + CharacterSystem.Power(d, c).ToString("N0"), UIStyles.Sized(UIStyles.Center, 20), new Color(0.9f, 0.9f, 0.9f), 2f);
                    // Drag to turn the slayer around; a quick tap selects them (and they cheer).
                    var ev = Event.current;
                    if (ev.type == EventType.MouseDown && hit.Contains(ev.mousePosition))
                    {
                        teamDragSlot = i;
                        teamDragLast = ev.mousePosition.x;
                        teamDragDistance = 0f;
                        ev.Use();
                    }
                }
                else
                {
                    // Empty slot: a dark card with + and the role it wants.
                    var card = new Rect(fp.x - 95f, headP.y + 10f, 190f, fp.y - headP.y + 40f);
                    Round(card, new Color(0.08f, 0.09f, 0.13f, 0.85f), 14f);
                    RoundFrame(card, teamSlot == i ? new Color(1f, 0.85f, 0.35f) : new Color(1f, 1f, 1f, 0.12f), 2f, 14f);
                    var box = new Rect(card.center.x - 60f, card.center.y - 90f, 120f, 120f);
                    Round(box, new Color(0.16f, 0.17f, 0.22f, 0.95f), 12f);
                    GUI.DrawTexture(new Rect(box.x + 26f, box.y + 26f, 68f, 68f), IconFactory.Get("plus"), ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(card.x, card.center.y + 30f, card.width, 40f), SlotRoles[i] == Role.Support ? "SUPPORT" : SlotRoles[i] == Role.Tank ? "VANGUARD" : "STRIKER", UIStyles.Sized(UIStyles.Center, 24));
                    if (GUI.Button(card, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("click", 0.5f); teamSlot = i; teamEditing = true; }
                }
            }

            HandleTeamDrag(d);
            if (d.team.Count > 0)
                GUI.Label(new Rect(W * 0.5f - 300f, H - 40f, 600f, 32f), "<color=#BBBBBB>Drag a slayer to turn them around</color>", UIStyles.Sized(UIStyles.Center, 18));

            float by = H - 130f;
            float bx = safe.xMax - 30f;
            if (FlatBtn(new Rect(bx - 380f, by, 380f, 96f), "EDIT TEAM", TileRed, true, 34)) { teamEditing = true; if (string.IsNullOrEmpty(rosterPick) && d.team.Count > 0) rosterPick = d.team[0]; }
            if (FlatBtn(new Rect(bx - 380f - 290f, by + 8f, 270f, 80f), "AUTO SET", new Color(0.12f, 0.16f, 0.28f), true, 28)) AutoSet(d);
            // Choose who leads: pick a slayer, then SET LEADER.
            bool canLead = teamSlot > 0 && teamSlot < d.team.Count;
            string leadName = canLead ? GameDatabase.GetCharacter(d.team[teamSlot]).displayName.Split(' ')[0] : "";
            if (FlatBtn(new Rect(bx - 380f - 290f - 310f, by + 8f, 290f, 80f), canLead ? "SET " + leadName.ToUpper() + " AS LEADER" : "TAP A SLAYER", new Color(0.12f, 0.3f, 0.62f), canLead, canLead ? 20 : 22))
                SetLeader(d, teamSlot);
        }

        /// <summary>Rarity stars: 2★ Common up to 6★ Mythic, coloured by rarity.</summary>
        void StarStrip(Vector2 at, int stars, float size, int awaken = 0)
        {
            Color col = RarityInfo.Color(stars);
            Color purple = new Color(0.78f, 0.35f, 1f);
            float glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f);
            for (int i = 0; i < CharacterSystem.MaxStars; i++)
            {
                bool on = i < stars;
                bool awakened = i < awaken;
                var sr = new Rect(at.x + i * size, at.y, size + 4f, size + 4f);
                if (awakened) UIStyles.CircleTex(sr.center, size * (0.5f + 0.12f * glow), new Color(purple.r, purple.g, purple.b, 0.35f));
                Color sc = awakened ? Color.Lerp(purple, Color.white, 0.25f * glow) : on ? Color.Lerp(col, new Color(1f, 0.85f, 0.3f), 0.5f) : new Color(0.35f, 0.35f, 0.4f);
                UIStyles.Outlined(sr, "★", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(size)), sc, 1.5f);
            }
        }

        /// <summary>GOD status: every star purple and the level maxed.</summary>
        static readonly Color GodColor = new Color(0.8f, 0.4f, 1f);

        void DrawTeamEdit(PlayerData d, bool manage = false)
        {
            float x0 = safe.x + 24f, top = safe.y + 130f;
            // Element filters down the left.
            var fr = new Rect(x0, top, 196f, 70f);
            if (FilterButton(fr, "ALL", IconFactory.Get("all"), Color.white, rosterFilter < 0)) rosterFilter = -1;
            for (int i = 0; i < FilterOrder.Length; i++)
            {
                var e = FilterOrder[i];
                var r = new Rect(x0, top + (i + 1) * 80f, 196f, 70f);
                if (FilterButton(r, ElementName(e), IconFactory.Get(IconFactory.ForElement(e)), ElementChart.ColorOf(e), rosterFilter == (int)e)) rosterFilter = rosterFilter == (int)e ? -1 : (int)e;
            }

            // Detail card on the right.
            var detail = new Rect(safe.xMax - 470f, top, 446f, H - top - 30f);
            DrawRosterDetail(detail, d, manage);

            // Grid of slayer cards.
            var list = new List<OwnedCharacter>();
            foreach (var c in d.characters)
            {
                var def = GameDatabase.GetCharacter(c.id);
                if (def == null) continue;
                if (rosterFilter >= 0 && (int)def.element != rosterFilter) continue;
                list.Add(c);
            }
            // Every slayer in the game is shown: the ones you don't have yet are greyed out with a lock.
            var locked = new List<CharacterDefinition>();
            foreach (var def in GameDatabase.Characters)
            {
                if (def.npc || d.GetCharacter(def.id) != null) continue;
                if (rosterFilter >= 0 && (int)def.element != rosterFilter) continue;
                locked.Add(def);
            }
            locked.Sort((a, b) => b.rarity.CompareTo(a.rarity));
            var area = new Rect(x0 + 216f, top, detail.x - x0 - 236f, H - top - 30f);
            Round(area, new Color(0.05f, 0.06f, 0.1f, 0.82f), 16f);
            float gw = 168f, gh = 222f, gg = 16f;
            var view = new Rect(area.x + 16f, area.y + 16f, area.width - 32f, area.height - 32f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((view.width - 20f + gg) / (gw + gg)));
            int shown = list.Count + locked.Count;
            var content = new Rect(0f, 0f, view.width - 20f, Mathf.CeilToInt(shown / (float)cols) * (gh + gg));
            rosterScroll = GUI.BeginScrollView(view, rosterScroll, content);
            for (int i = 0; i < shown; i++)
            {
                var r = new Rect((i % cols) * (gw + gg), (i / cols) * (gh + gg), gw, gh);
                if (i >= list.Count)
                {
                    LockedCard(r, locked[i - list.Count]);
                    continue;
                }
                var c = list[i];
                var def = GameDatabase.GetCharacter(c.id);
                SlayerCard(r, def, c, rosterPick == c.id, d.team.IndexOf(c.id));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { rosterPick = c.id; gm.Audio.Play("click", 0.4f); }
            }
            GUI.EndScrollView();
        }

        /// <summary>A slayer you haven't unlocked: grey portrait, name, rarity and a big grey lock.</summary>
        void LockedCard(Rect r, CharacterDefinition def)
        {
            Color rc = RarityInfo.Color(def.rarity);
            Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.4f), 12f);
            Round(r, new Color(0.1f, 0.1f, 0.13f, 0.95f), 12f);
            var face = new Rect(r.x + 8f, r.y + 8f, r.width - 16f, r.width - 16f);
            var tex = ArtLibrary.Character(def);
            var o = GUI.color;
            if (tex != null)
            {
                GUI.color = new Color(0.32f, 0.32f, 0.36f, 1f);
                GUI.DrawTexture(face, tex, ScaleMode.ScaleAndCrop, true);
            }
            GUI.color = o;
            Round(face, new Color(0.15f, 0.15f, 0.2f, 0.45f), 10f);
            LockIcon(new Vector2(face.center.x, face.center.y + 6f), 56f, new Color(0.62f, 0.62f, 0.66f));
            Round(new Rect(r.x + 2f, r.yMax - 64f, r.width - 4f, 62f), new Color(0f, 0f, 0f, 0.55f), 10f);
            GUI.Label(new Rect(r.x + 10f, r.yMax - 64f, r.width - 20f, 30f), "<color=#9A9AA2>" + def.displayName + "</color>", UIStyles.Sized(UIStyles.Small, 17));
            UIStyles.Colored(new Rect(r.x + 10f, r.yMax - 34f, r.width - 20f, 26f), RarityInfo.Name(def.rarity), UIStyles.Sized(UIStyles.Small, 15), Color.Lerp(rc, Color.gray, 0.5f));
            RoundFrame(r, new Color(1f, 1f, 1f, 0.08f), 2f, 12f);
            if (GUI.Button(r, GUIContent.none, GUIStyle.none))
            {
                gm.Audio.Play("click", 0.4f);
                Toast(def.displayName + " is locked — find them in Summon" + (def.storyOnly ? ", the story" : "") + " or Events.");
            }
        }

        bool FilterButton(Rect r, string label, Texture2D icon, Color ic, bool on)
        {
            Round(r, on ? TileRed : new Color(0.06f, 0.07f, 0.11f, 0.9f), 10f);
            if (on) RoundFrame(Grow(r, 2f), new Color(1f, 0.5f, 0.45f, 0.8f), 2f, 11f);
            var o = GUI.color;
            GUI.color = on ? Color.white : ic;
            GUI.DrawTexture(new Rect(r.x + 18f, r.y + 18f, 34f, 34f), icon, ScaleMode.ScaleToFit, true);
            GUI.color = o;
            GUI.Label(new Rect(r.x + 64f, r.y, r.width - 70f, r.height), label, UIStyles.Sized(UIStyles.Body, 22));
            bool c = GUI.Button(r, GUIContent.none, GUIStyle.none);
            if (c) gm.Audio.Play("click", 0.4f);
            return c;
        }

        /// <summary>A soft pulsing glow behind a portrait: the element's colour, or red once the slayer is maxed.</summary>
        void Aura(Vector2 c, float radius, Color col, bool maxed)
        {
            float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (maxed ? 5f : 2.5f) + c.x * 0.01f);
            if (maxed) col = new Color(1f, 0.15f, 0.12f);
            for (int i = 4; i >= 1; i--)
                UIStyles.CircleTex(c, radius * (0.55f + i * 0.14f + p * 0.05f), new Color(col.r, col.g, col.b, (maxed ? 0.1f : 0.07f) + 0.02f * p));
        }

        /// <summary>Roster card: the slayer's face over an aura, level (red and MAX at the cap), rarity stars, element badge.</summary>
        void SlayerCard(Rect r, CharacterDefinition def, OwnedCharacter c, bool selected, int teamIndex)
        {
            Color ec = ElementChart.ColorOf(def.element);
            Color rc = RarityInfo.Color(c.stars);
            bool maxed = ExperienceSystem.IsMaxed(c);
            bool god = ExperienceSystem.IsGod(c);
            if (god) rc = GodColor;
            Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.4f), 12f);
            Round(r, Color.Lerp(new Color(0.08f, 0.08f, 0.13f), rc, 0.22f), 12f);
            Round(new Rect(r.x, r.y, r.width, r.height * 0.55f), new Color(1f, 1f, 1f, 0.06f), 12f);
            // Face with aura.
            var face = new Rect(r.x + 8f, r.y + 8f, r.width - 16f, r.width - 16f);
            Aura(face.center, face.width * 0.5f, god ? GodColor : ec, maxed && !god);
            if (god) Aura(face.center, face.width * 0.62f, new Color(1f, 0.85f, 0.3f), false);
            var tex = ArtLibrary.Character(def);
            if (tex != null) GUI.DrawTexture(face, tex, ScaleMode.ScaleAndCrop, true);
            Round(new Rect(r.x + 2f, r.yMax - 64f, r.width - 4f, 62f), new Color(0f, 0f, 0f, 0.55f), 10f);
            UIStyles.CircleTex(new Vector2(r.x + 22f, r.yMax - 82f), 17f, Color.Lerp(ec, Color.black, 0.2f));
            GUI.DrawTexture(new Rect(r.x + 10f, r.yMax - 94f, 24f, 24f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
            if (god)
            {
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                UIStyles.Outlined(new Rect(r.x + 12f, r.yMax - 64f, r.width - 20f, 32f), "Lv. " + c.level + "  <size=17>GOD</size>", UIStyles.Sized(UIStyles.Body, 22), Color.Lerp(GodColor, new Color(1f, 0.85f, 0.3f), p), 2f);
            }
            else if (maxed)
            {
                float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f);
                UIStyles.Outlined(new Rect(r.x + 12f, r.yMax - 64f, r.width - 20f, 32f), "Lv. " + c.level + "  <size=16>MAX</size>", UIStyles.Sized(UIStyles.Body, 22), new Color(1f, 0.25f + 0.15f * p, 0.2f), 2f);
            }
            else GUI.Label(new Rect(r.x + 12f, r.yMax - 64f, r.width - 20f, 32f), "Lv. " + c.level, UIStyles.Sized(UIStyles.Body, 22));
            StarStrip(new Vector2(r.x + 8f, r.yMax - 32f), c.stars, Mathf.Min(20f, (r.width - 16f) / 6.2f), c.awaken);
            RoundFrame(r, god ? Color.Lerp(GodColor, new Color(1f, 0.85f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f)) : maxed ? new Color(1f, 0.2f, 0.15f) : selected ? new Color(0.45f, 0.85f, 1f) : Color.Lerp(rc, Color.black, 0.2f), selected || maxed ? 4f : 2f, 12f);
            if (teamIndex >= 0)
            {
                UIStyles.CircleTex(new Vector2(r.xMax - 20f, r.y + 20f), 16f, TileGreen);
                GUI.Label(new Rect(r.xMax - 36f, r.y + 4f, 32f, 32f), (teamIndex + 1).ToString(), UIStyles.Sized(UIStyles.Center, 20));
            }
            // Rarity name.
            UIStyles.Outlined(new Rect(r.x + 36f, r.yMax - 98f, r.width - 44f, 26f), god ? "GOD" : RarityInfo.Name(c.stars), UIStyles.Sized(UIStyles.Right, 14), rc, 1.5f);
        }

        void DrawRosterDetail(Rect card, PlayerData d, bool manage = false)
        {
            Round(Offset(card, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(card, new Color(0.05f, 0.06f, 0.1f, 0.92f), 16f);
            var c = string.IsNullOrEmpty(rosterPick) ? null : d.GetCharacter(rosterPick);
            var def = c != null ? GameDatabase.GetCharacter(c.id) : null;
            if (c == null || def == null)
            {
                GUI.Label(card, "<color=#AAAAAA>Choose a slayer.</color>", UIStyles.CenterSmall);
                return;
            }
            float x = card.x + 24f, y = card.y + 24f;
            var pr = new Rect(x, y, 130f, 130f);
            Round(pr, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), ElementChart.ColorOf(def.element), 0.3f), 12f);
            var head = ArtLibrary.Character(def);
            if (head != null) GUI.DrawTexture(pr, head, ScaleMode.ScaleAndCrop, true);
            float tx = x + 150f;
            GUI.Label(new Rect(tx, y, card.xMax - tx - 16f, 40f), def.displayName, UIStyles.Sized(UIStyles.H2, 30));
            GUI.DrawTexture(new Rect(tx, y + 48f, 28f, 28f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(tx + 36f, y + 42f, 220f, 40f), "Lv. " + c.level + " / " + ExperienceSystem.Cap(c), UIStyles.Sized(UIStyles.Body, 24));
            StarStrip(new Vector2(tx, y + 84f), c.stars, 24f, c.awaken);
            if (ExperienceSystem.IsGod(c)) UIStyles.Outlined(new Rect(tx + 160f, y + 78f, 120f, 36f), "GOD", UIStyles.Sized(UIStyles.H2, 30), Color.Lerp(GodColor, new Color(1f, 0.85f, 0.3f), 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f)), 2f);
            GUI.Label(new Rect(x, y + 146f, card.width - 48f, 40f), "Power  <color=#FFD36B>" + CharacterSystem.Power(d, c).ToString("N0") + "</color>", UIStyles.Sized(UIStyles.Body, 26));
            GUI.Label(new Rect(x, y + 184f, card.width - 48f, 34f), "<color=#AAAAAA>" + def.role + " · " + def.style + "</color>", UIStyles.Sized(UIStyles.Small, 20));
            UIStyles.Rect(new Rect(x, y + 224f, card.width - 48f, 1f), new Color(1f, 1f, 1f, 0.12f));

            var st = CharacterSystem.ComputeStats(d, c);
            string[] names = { "HP", "ATK", "DEF" };
            string[] icons = { "heart", "swords", "shield" };
            float[] vals = { st.hp, st.atk, st.def };
            // Upgrade page: preview what the next level adds, so every upgrade visibly makes the slayer stronger.
            bool hasNext = false;
            float[] nvals = null;
            if (manage && upgradeMode && !ExperienceSystem.IsMaxed(c))
            {
                c.level++;
                var next = CharacterSystem.ComputeStats(d, c);
                c.level--;
                hasNext = true;
                nvals = new float[] { next.hp, next.atk, next.def };
            }
            for (int i = 0; i < 3; i++)
            {
                float ry = y + 240f + i * 58f;
                var o = GUI.color;
                GUI.color = new Color(0.8f, 0.8f, 0.85f);
                GUI.DrawTexture(new Rect(x + 4f, ry + 10f, 32f, 32f), IconFactory.Get(icons[i]), ScaleMode.ScaleToFit, true);
                GUI.color = o;
                GUI.Label(new Rect(x + 50f, ry, 150f, 52f), names[i], UIStyles.Sized(UIStyles.Body, 24));
                string val = Mathf.RoundToInt(vals[i]).ToString("N0");
                if (hasNext) val += "  <color=#6BFF8A>+" + Mathf.Max(0, Mathf.RoundToInt(nvals[i] - vals[i])).ToString("N0") + "</color>";
                GUI.Label(new Rect(x + 150f, ry, card.width - 198f, 52f), val, UIStyles.Sized(UIStyles.Right, 26));
            }

            int inTeam = d.team.IndexOf(c.id);
            if (manage && upgradeMode)
            {
                DrawUpgradeButtons(card, x, d, c, def);
                return;
            }
            if (manage)
            {
                // Characters screen: SELECT adds to the team; the other three open the right upgrade page.
                bool inT = inTeam >= 0;
                if (FlatBtn(new Rect(x, card.yMax - 250f, card.width - 48f, 80f), inT ? "IN TEAM" : "SELECT", TileRed, !inT, 32))
                {
                    if (d.team.Count < TeamSize) { d.team.Add(c.id); SaveTeam(d); Toast(def.displayName + " joined the team"); }
                    else { teamSlot = 0; teamReturn = GameScreen.Characters; rosterPick = c.id; teamEditing = true; gm.GoTo(GameScreen.Team); }
                }
                float bw3 = (card.width - 48f - 20f) / 3f;
                string[] labels = { "UPGRADE", "DETAILS", "SKILLS" };
                for (int i = 0; i < 3; i++)
                    if (FlatBtn(new Rect(x + i * (bw3 + 10f), card.yMax - 156f, bw3, 64f), labels[i], i == 0 ? TileGreen : i == 1 ? TileOrange : TileBlue, true, 20))
                    {
                        gm.SelectedCharacterId = c.id;
                        rosterPick = c.id;
                        pickingSlot = null;
                        if (i == 0) { gm.GoTo(GameScreen.Equipment); continue; }
                        detailTab = i == 1 ? DetailTab.Stats : DetailTab.Skills;
                        gm.GoTo(GameScreen.CharacterDetail);
                    }
                GUI.Label(new Rect(x, card.yMax - 80f, card.width - 48f, 60f), "<color=#AAAAAA><size=18>Weapon: " + def.weapon + "\nSpecial: " + def.ultimate.name + "</size></color>", UIStyles.Small);
                return;
            }
            if (inTeam > 0 && FlatBtn(new Rect(x, card.yMax - 262f, card.width - 48f, 50f), "SET AS LEADER", new Color(0.12f, 0.3f, 0.62f), true, 20)) SetLeader(d, inTeam);
            bool same = inTeam >= 0 && inTeam == teamSlot;
            GUI.Label(new Rect(x, card.yMax - 210f, card.width - 48f, 36f), "<color=#AAAAAA>Slot " + (teamSlot + 1) + " · " + (teamSlot == 0 ? "Leader" : SlotRoles[teamSlot].ToString()) + "</color>", UIStyles.Sized(UIStyles.Small, 20));
            if (FlatBtn(new Rect(x, card.yMax - 170f, card.width - 48f, 84f), same ? "IN TEAM" : "SELECT", TileRed, !same, 34)) AssignToSlot(c.id, -1);
            if (FlatBtn(new Rect(x, card.yMax - 76f, card.width - 48f, 56f), "DETAILS", new Color(0.14f, 0.18f, 0.3f), true, 22))
            {
                gm.SelectedCharacterId = c.id;
                gm.GoTo(GameScreen.CharacterDetail);
            }
        }

        /// <summary>Moves the slayer in this slot to the front: the leader starts every battle.</summary>
        void SetLeader(PlayerData d, int index)
        {
            if (index <= 0 || index >= d.team.Count) return;
            string id = d.team[index];
            d.team[index] = d.team[0];
            d.team[0] = id;
            teamSlot = 0;
            rosterPick = id;
            SaveTeam(d);
            gm.Audio.Play("switch", 0.6f);
            var def = GameDatabase.GetCharacter(id);
            Toast((def != null ? def.displayName : "New leader") + " now leads the team");
        }

        bool upgradeMode;

        /// <summary>UPGRADE screen: roster on the left, the chosen slayer's levels and training on the right.</summary>
        void DrawUpgrade()
        {
            TopBar("UPGRADE", GameScreen.MainMenu);
            var d = gm.Data;
            if (string.IsNullOrEmpty(rosterPick) || d.GetCharacter(rosterPick) == null) rosterPick = d.team.Count > 0 ? d.team[0] : (d.characters.Count > 0 ? d.characters[0].id : null);
            upgradeMode = true;
            DrawTeamEdit(d, true);
            upgradeMode = false;
        }

        int dupIndex;
        string awakenFocus;

        /// <summary>
        /// Level up with gold + XP (1 or 10 levels), feed XP from the pool, feed or sell duplicate slayers, and
        /// ascend at the rarity cap. The stat rows above show the next level's gains in green.
        /// </summary>
        void DrawUpgradeButtons(Rect card, float x, PlayerData d, OwnedCharacter c, CharacterDefinition def)
        {
            float w = card.width - 48f;
            float y = card.yMax - 340f;
            int cap = ExperienceSystem.Cap(c);
            bool maxed = c.level >= cap;
            float hw = (w - 10f) / 2f;

            // Level bar to this rarity's cap, with the EXP fill toward the next level.
            GUI.Label(new Rect(x, y, hw, 30f), "<color=#AAAAAA>Level</color>  <b>" + (maxed ? "<color=#FF5A4A>" + c.level + " MAX</color>" : c.level.ToString()) + "</b> / " + cap, UIStyles.Sized(UIStyles.Body, 22));
            GUI.Label(new Rect(x + hw, y, hw + 10f, 30f), "<color=#C9A6FF>" + d.xp.ToString("N0") + " XP</color>   <color=#FFD36B>" + d.coins.ToString("N0") + "</color>", UIStyles.Sized(UIStyles.Right, 18));
            float fill = maxed ? 1f : (c.level - 1 + c.exp / (float)Mathf.Max(1, ExperienceSystem.ExpToNext(c.level))) / Mathf.Max(1f, cap - 1f);
            UIStyles.Bar(new Rect(x, y + 32f, w, 14f), fill, maxed ? new Color(0.95f, 0.25f, 0.2f) : new Color(0.35f, 0.85f, 0.45f));
            y += 56f;

            int gold1 = ExperienceSystem.LevelUpCost(c.level), xp1 = ExperienceSystem.LevelUpXp(c.level);
            int gold10 = 0, xp10 = 0;
            for (int i = 0; i < 10 && c.level + i < cap; i++) { gold10 += ExperienceSystem.LevelUpCost(c.level + i); xp10 += ExperienceSystem.LevelUpXp(c.level + i); }
            bool can1 = !maxed && d.coins >= gold1 && d.xp >= xp1;
            if (FlatBtn(new Rect(x, y, hw, 66f), maxed ? "MAX LEVEL" : "LEVEL UP\n<size=15>" + gold1.ToString("N0") + " gold · " + xp1.ToString("N0") + " XP</size>", TileGreen, can1, 21))
                AfterLevel(c, ExperienceSystem.BuyLevels(d, c, 1));
            if (FlatBtn(new Rect(x + hw + 10f, y, hw, 66f), maxed ? "MAX LEVEL" : "LEVEL UP ×10\n<size=15>" + gold10.ToString("N0") + " · " + xp10.ToString("N0") + " XP</size>", new Color(0.16f, 0.52f, 0.3f), can1, 21))
                AfterLevel(c, ExperienceSystem.BuyLevels(d, c, 10));
            y += 74f;

            int feed = Mathf.Min(d.xp, 5000);
            if (FlatBtn(new Rect(x, y, hw, 60f), maxed ? "FEED XP" : "FEED XP\n<size=15>+" + feed.ToString("N0") + " EXP</size>", new Color(0.45f, 0.3f, 0.75f), !maxed && feed > 0, 20))
                AfterLevel(c, CharacterSystem.UseXp(d, c, feed));
            string reason;
            bool canAscend = CharacterSystem.CanAscend(d, c, out reason);
            string ascText = canAscend
                ? "ASCEND ★\n<size=15>" + CharacterSystem.AscendXpCost(c.stars).ToString("N0") + " XP · " + CharacterSystem.AscendCoinCost(c.stars).ToString("N0") + " gold</size>"
                : "ASCEND\n<size=15>" + reason + "</size>";
            if (FlatBtn(new Rect(x + hw + 10f, y, hw, 60f), ascText, new Color(0.8f, 0.55f, 0.15f), canAscend, 20))
            {
                if (CharacterSystem.TryAscend(d, c))
                {
                    gm.Save();
                    gm.Audio.Play("perfect", 0.8f);
                    Toast(def.displayName + " is now " + RarityInfo.Name(c.stars) + " · max Lv." + ExperienceSystem.Cap(c));
                    if (gm.Home != null) gm.Home.Celebrate(RarityInfo.Color(c.stars));
                }
            }
            y += 70f;

            // Duplicates: feed one to this slayer as EXP, or sell it for gold.
            var r = new Rect(x, y, w, 118f);
            Round(r, new Color(1f, 1f, 1f, 0.05f), 12f);
            int dupCount = d.copies != null ? d.copies.Count : 0;
            if (dupCount == 0)
            {
                GUI.Label(r, "<color=#888888><size=18>No duplicates yet.\nExtra summons of a slayer you own land here.</size></color>", UIStyles.CenterSmall);
                return;
            }
            dupIndex = ((dupIndex % dupCount) + dupCount) % dupCount;
            // Jump to this slayer's own duplicate first: it's the one that awakens them.
            if (awakenFocus != c.id)
            {
                awakenFocus = c.id;
                int own = d.copies.FindIndex(cs => cs.id == c.id);
                if (own >= 0) dupIndex = own;
            }
            var stack = d.copies[dupIndex];
            var ddef = GameDatabase.GetCharacter(stack.id);
            if (ddef == null) { d.copies.RemoveAt(dupIndex); return; }
            var face = new Rect(r.x + 44f, r.y + 10f, 60f, 60f);
            Round(face, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), RarityInfo.Color(ddef.rarity), 0.45f), 10f);
            var tex = ArtLibrary.Character(ddef);
            if (tex != null) GUI.DrawTexture(face, tex, ScaleMode.ScaleAndCrop, true);
            if (dupCount > 1)
            {
                if (FlatBtn(new Rect(r.x + 6f, r.y + 22f, 32f, 36f), "<", new Color(0.2f, 0.22f, 0.3f), true, 20)) dupIndex--;
                if (FlatBtn(new Rect(r.x + 110f, r.y + 22f, 32f, 36f), ">", new Color(0.2f, 0.22f, 0.3f), true, 20)) dupIndex++;
            }
            GUI.Label(new Rect(r.x + 150f, r.y + 8f, w - 160f, 30f), "DUPLICATE  " + (dupIndex + 1) + "/" + dupCount, UIStyles.Sized(UIStyles.Small, 15));
            GUI.Label(new Rect(r.x + 150f, r.y + 32f, w - 160f, 36f), ddef.displayName + "  <color=#FFD36B>×" + stack.count + "</color>", UIStyles.Sized(UIStyles.Body, 20));
            UIStyles.Outlined(new Rect(r.x + 8f, r.y + 72f, 136f, 20f), RarityInfo.Name(ddef.rarity), UIStyles.Sized(UIStyles.CenterSmall, 13), RarityInfo.Color(ddef.rarity), 1.2f);
            bool sameChar = stack.id == c.id;
            if (sameChar && CharacterSystem.CanAwakenRarity(c))
            {
                // A copy of this very slayer: AWAKEN (purple star, +30 max level and levels, +20% stats).
                bool canAw = c.awaken < ExperienceSystem.MaxAwaken;
                if (FlatBtn(new Rect(r.x + 150f, r.y + 68f, w - 160f, 42f), canAw ? "AWAKEN ★  <size=14>+30 Lv · +20% stats</size>" : "FULLY AWAKENED", new Color(0.55f, 0.2f, 0.85f), canAw, 18))
                {
                    if (CharacterSystem.TryAwaken(d, c))
                    {
                        gm.Save();
                        gm.Audio.Play("ultimate", 0.7f);
                        Toast(ExperienceSystem.IsGod(c) ? def.displayName + " has become a GOD!" : def.displayName + " awakened! " + c.awaken + "/6 purple stars · Lv." + c.level);
                        if (gm.Home != null) gm.Home.Celebrate(GodColor);
                    }
                }
                return;
            }
            float bw = (w - 160f - 10f) / 2f;
            int fx = ExperienceSystem.CopyXp(ddef.rarity) * (sameChar ? 2 : 1), fg = ExperienceSystem.CopyGold(ddef.rarity);
            if (FlatBtn(new Rect(r.x + 150f, r.y + 68f, bw, 42f), "FEED <size=14>+" + fx.ToString("N0") + " EXP</size>", new Color(0.45f, 0.3f, 0.75f), !maxed, 17))
            {
                int gained = CharacterSystem.FeedCopy(d, c, stack.id);
                if (gained > 0) AfterLevel(c, gained);
                else if (gained == 0) { gm.Save(); gm.Audio.Play("perfect", 0.4f); Toast(def.displayName + " absorbed " + ddef.displayName); }
            }
            if (FlatBtn(new Rect(r.x + 160f + bw, r.y + 68f, bw, 42f), "SELL <size=14>+" + fg.ToString("N0") + " gold</size>", new Color(0.75f, 0.55f, 0.12f), true, 17))
            {
                int gold = CharacterSystem.SellCopy(d, stack.id);
                if (gold > 0) { gm.Save(); gm.Audio.Play("coin", 0.8f); Toast("Sold " + ddef.displayName + " for " + gold.ToString("N0") + " gold"); }
            }
        }

        void AfterLevel(OwnedCharacter c, int gained)
        {
            if (gained <= 0) return;
            GameEvents.RaiseCharacterUpgraded(c);
            gm.Save();
            gm.Audio.Play("perfect", 0.6f);
            var def = GameDatabase.GetCharacter(c.id);
            Toast((def != null ? def.displayName : "Slayer") + " reached Lv." + c.level);
            if (gm.Home != null) gm.Home.Celebrate(def != null ? ElementChart.ColorOf(def.element) : Color.white);
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
            // An empty preset starts with the leader so there is always someone to fight.
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
