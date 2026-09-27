using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Home menu, world map, story journal, mission page and the end-of-mission screen.</summary>
    public partial class UIManager
    {
        // ------------------------------------------------------------------ Home

        float resetArmed = -10f;

        void DrawMainMenu()
        {
            var d = gm.Data;
            // A soft shade on the left so the tiles read over the living village.
            for (int i = 0; i < 6; i++)
                UIStyles.Rect(new Rect(0f, 0f, 170f * (6 - i) + safe.x, H), new Color(0.02f, 0.02f, 0.06f, 0.05f));

            float x = safe.x + 60f;
            float k = Enter(0f, 0.6f);
            float ty = safe.y + 24f - (1f - k) * 80f;
            UIStyles.Outlined(new Rect(x, ty, 900f, 60f), GameConfig.TitleLine1, UIStyles.Sized(UIStyles.Title, 46), new Color(0.9f, 0.12f, 0.12f), 3f);
            string[] words = GameConfig.TitleLine2.Split(' ');
            UIStyles.Outlined(new Rect(x - 4f, ty + 52f, 1000f, 110f), words[0], UIStyles.Sized(UIStyles.Title, 96), Color.white, 4f);
            if (words.Length > 1) UIStyles.Outlined(new Rect(x - 4f, ty + 144f, 1000f, 110f), words[1], UIStyles.Sized(UIStyles.Title, 96), Color.white, 4f);

            var next = gm.NextStoryMission();
            string leaderId = d.team.Count > 0 ? d.team[0] : null;
            var leader = leaderId != null ? GameDatabase.GetCharacter(leaderId) : null;

            // Row 1: PLAY and STORY.
            float top = safe.y + 300f;
            if (HomeTile(new Rect(x, top, 490f, 272f), "PLAY", null, new Color(1f, 0.25f, 0.2f), 0.1f, TileArt.Play, leader != null ? ArtLibrary.CharacterFull(leader) : null, 0, true))
                gm.GoTo(GameScreen.WorldMap);
            if (HomeTile(new Rect(x + 506f, top, 420f, 272f), "STORY", null, new Color(0.9f, 0.9f, 0.95f), 0.16f, TileArt.Story))
            {
                // Story → world map, opened on the chapter and stop of the next story mission.
                if (next != null)
                {
                    mapSelected = next.regionId;
                    mapOverview = false;
                    areaRegion = next.regionId;
                    areaPick = GameDatabase.MissionsInRegion(next.regionId).IndexOf(next);
                    gm.GoTo(GameScreen.WorldMap);
                }
                else gm.GoTo(GameScreen.Story);
            }
            // Row 2: SUMMON, CHARACTERS, TEAM.
            float r2 = top + 290f, h2 = 190f;
            if (HomeTile(new Rect(x, r2, 262f, h2), "SUMMON", IconFactory.Get("flame"), new Color(0.52f, 0.26f, 0.85f), 0.22f, null, null, MenuBadge("SUMMON"))) OpenMenu("SUMMON");
            if (HomeTile(new Rect(x + 278f, r2, 272f, h2), "CHARACTERS", IconFactory.Get("people"), new Color(0.2f, 0.42f, 0.85f), 0.26f)) OpenMenu("CHARACTERS");
            if (HomeTile(new Rect(x + 566f, r2, 360f, h2), "TEAM", IconFactory.Get("group"), new Color(0.2f, 0.62f, 0.4f), 0.3f)) OpenMenu("TEAM");
            // Row 3: EQUIPMENT, MISSIONS, SHOP, SETTINGS.
            float r3 = r2 + h2 + 16f;
            if (HomeTile(new Rect(x, r3, 262f, h2), "EQUIPMENT", IconFactory.Get("bag"), new Color(0.72f, 0.5f, 0.18f), 0.34f)) OpenMenu("EQUIPMENT");
            if (HomeTile(new Rect(x + 278f, r3, 212f, h2), "MISSIONS", IconFactory.Get("scroll"), new Color(0.62f, 0.14f, 0.18f), 0.38f, null, null, MenuBadge("MISSIONS"))) OpenMenu("MISSIONS");
            if (HomeTile(new Rect(x + 506f, r3, 196f, h2), "SHOP", IconFactory.Get("cart"), new Color(0.12f, 0.58f, 0.62f), 0.42f, null, null, MenuBadge("SHOP"))) OpenMenu("SHOP");
            if (HomeTile(new Rect(x + 718f, r3, 208f, h2), "SETTINGS", IconFactory.Get("gear"), new Color(0.42f, 0.44f, 0.5f), 0.46f)) OpenMenu("SETTINGS");

            // Top right: coins and crystals with +, mail (missions board) and menu (journal).
            float px = safe.xMax - 40f;
            if (IconButton(new Rect(px - 72f, safe.y + 24f, 72f, 64f), IconFactory.Get("menu"))) OpenMenu("JOURNAL");
            if (IconButton(new Rect(px - 154f, safe.y + 24f, 72f, 64f), IconFactory.Get("mail"))) OpenMenu("MISSIONS");
            PlusPill(new Rect(px - 430f, safe.y + 24f, 260f, 64f), false, d.crystals.ToString("N0"), () => OpenMenu("SHOP"));
            PlusPill(new Rect(px - 740f, safe.y + 24f, 294f, 64f), true, d.coins.ToString("N0"), () => OpenMenu("SHOP"));

            // Team power panel, bottom right.
            float tk = Enter(0.5f, 0.5f);
            int rows = Mathf.Min(4, d.team.Count);
            float ph = 96f + rows * 46f;
            var panel = new Rect(safe.xMax - 590f + (1f - tk) * 300f, H - ph - 40f, 550f, ph);
            Round(Offset(panel, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(panel, new Color(0.04f, 0.05f, 0.08f, 0.86f), 16f);
            RoundFrame(panel, new Color(1f, 1f, 1f, 0.1f), 2f, 16f);
            GUI.DrawTexture(new Rect(panel.x + 22f, panel.y + 18f, 48f, 48f), IconFactory.Get("swords"), ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(panel.x + 84f, panel.y + 16f, 260f, 52f), "TEAM POWER", UIStyles.Sized(UIStyles.H2, 30));
            UIStyles.Colored(new Rect(panel.x + 280f, panel.y + 10f, 250f, 60f), CharacterSystem.TeamPower(d).ToString("N0"), UIStyles.Sized(UIStyles.Right, 46), new Color(1f, 0.82f, 0.25f));
            UIStyles.Rect(new Rect(panel.x + 22f, panel.y + 80f, panel.width - 44f, 1f), new Color(1f, 1f, 1f, 0.12f));
            for (int i = 0; i < rows; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                float ry = panel.y + 92f + i * 46f;
                Color ec = ElementChart.ColorOf(def.element);
                var o2 = GUI.color;
                GUI.color = ec;
                GUI.DrawTexture(new Rect(panel.x + 26f, ry + 6f, 30f, 30f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
                GUI.color = o2;
                UIStyles.Colored(new Rect(panel.x + 66f, ry, 130f, 42f), ElementName(def.element), UIStyles.Sized(UIStyles.Body, 22), ec);
                GUI.Label(new Rect(panel.x + 196f, ry, 200f, 42f), def.displayName, UIStyles.Sized(UIStyles.Body, 24));
                GUI.Label(new Rect(panel.x + 360f, ry, 70f, 42f), "Lv. " + c.level, UIStyles.Sized(UIStyles.Body, 22));
                UIStyles.Colored(new Rect(panel.x + 420f, ry, 110f, 42f), new string('★', Mathf.Clamp(c.stars, 1, 7)), UIStyles.Sized(UIStyles.Right, 20), Color.white);
                if (GUI.Button(new Rect(panel.x, ry, panel.width, 44f), GUIContent.none, GUIStyle.none)) { teamReturn = GameScreen.MainMenu; gm.GoTo(GameScreen.Team); }
            }

            DrawNpcBubbles();
        }

        static string ElementName(Element e)
        {
            // Beast is the green wind element in the menus.
            return e == Element.Beast ? "WIND" : e.ToString().ToUpper();
        }

        void OpenMenu(string label)
        {
            switch (label)
            {
                case "SUMMON": gm.GoTo(GameScreen.Summon); break;
                case "CHARACTERS": gm.GoTo(GameScreen.Characters); break;
                case "TEAM": teamReturn = GameScreen.MainMenu; gm.GoTo(GameScreen.Team); break;
                case "EQUIPMENT": gm.GoTo(GameScreen.Equipment); break;
                case "MISSIONS": gm.GoTo(GameScreen.MissionsBoard); break;
                case "SHOP": gm.GoTo(GameScreen.Shop); break;
                case "JOURNAL": gm.GoTo(GameScreen.Story); break;
                default: gm.OpenSettings(); break;
            }
        }

        int MenuBadge(string label)
        {
            var d = gm.Data;
            switch (label)
            {
                case "MISSIONS": return QuestSystem.Claimable(d);
                case "SHOP": return ShopSystem.FreeClaimedToday(d) ? 0 : 1;
                case "SUMMON": return d.crystals >= SummonSystem.MultiCost ? 10 : 0;
                default: return 0;
            }
        }

        /// <summary>Villagers' chatter floats above their heads.</summary>
        void DrawNpcBubbles()
        {
            var cam = Camera.main;
            if (cam == null) return;
            float s = HudLayout.Scale;
            foreach (var n in NpcWalker.All)
            {
                if (n == null || !n.gameObject.activeInHierarchy || string.IsNullOrEmpty(n.CurrentLine) || Time.time > n.LineUntil) continue;
                Vector3 sp = cam.WorldToScreenPoint(n.transform.position + Vector3.up * 2.3f);
                if (sp.z < 0f) continue;
                var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                if (p.x < 640f + safe.x) continue; // keep the menu column clear
                float w = Mathf.Clamp(n.CurrentLine.Length * 13f, 220f, 520f);
                var r = new Rect(p.x - w * 0.5f, p.y - 86f, w, 76f);
                UIStyles.Rect(r, new Color(1f, 0.98f, 0.92f, 0.92f));
                UIStyles.Rect(new Rect(p.x - 8f, r.yMax, 16f, 10f), new Color(1f, 0.98f, 0.92f, 0.92f));
                GUI.Label(new Rect(r.x + 10f, r.y + 4f, r.width - 20f, 26f), "<color=#8A4B2A><b>" + n.SpeakerName + "</b></color>", UIStyles.Sized(UIStyles.Small, 18));
                GUI.Label(new Rect(r.x + 10f, r.y + 28f, r.width - 20f, 46f), "<color=#222222>" + n.CurrentLine + "</color>", UIStyles.Sized(UIStyles.Small, 19));
            }
        }

        static string RegionName(string id)
        {
            var r = GameDatabase.GetRegion(id);
            return r != null ? r.name : "";
        }

        // ------------------------------------------------------------------ World map

        string mapSelected;
        Vector2 mapScroll;

        void DrawWorldMap()
        {
            var d = gm.Data;
            if (gm.Map.Traveling) Currencies(new Rect(safe.xMax - 980f, safe.y + 28f, 960f, 60f));
            else TopBar("WORLD MAP", GameScreen.MainMenu);
            if (string.IsNullOrEmpty(mapSelected)) mapSelected = d.currentRegion;
            var cam = Camera.main;
            float s = HudLayout.Scale;

            // Location labels floating over the 3D markers (world overview only).
            if (cam != null && !gm.Map.AreaMode)
            {
                foreach (var id in MapStage.RouteOrder)
                {
                    var region = GameDatabase.GetRegion(id);
                    Vector3 sp = cam.WorldToScreenPoint(gm.Map.NodeWorld(id) + Vector3.up * 5f);
                    if (sp.z < 0f) continue;
                    var p = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                    if (p.y < safe.y + 120f || p.x > W - 700f) continue;
                    bool unlocked = d.IsRegionUnlocked(region);
                    bool here = id == d.currentRegion;
                    var r = new Rect(p.x - 150f, p.y - 40f, 300f, 80f);
                    Color accent = !unlocked ? Color.gray : here ? UIStyles.Gold : RegionHasOpenBoss(id) ? UIStyles.Crimson : new Color(0.45f, 0.8f, 1f);
                    UIStyles.Rect(r, new Color(0f, 0f, 0f, id == mapSelected ? 0.75f : 0.5f));
                    UIStyles.Frame(r, accent, id == mapSelected ? 3f : 1.5f);
                    string name = unlocked ? region.name : "???";
                    GUI.Label(new Rect(r.x, r.y + 4f, r.width, 36f), name, UIStyles.Sized(UIStyles.Center, 24));
                    GUI.Label(new Rect(r.x, r.y + 38f, r.width, 34f), unlocked ? RegionTags(id) : "<color=#999999>LOCKED</color>", UIStyles.Sized(UIStyles.Center, 18));
                    if (here) GUI.Label(new Rect(r.x, r.y - 34f, r.width, 30f), "<color=#FFD36B>▼ YOU ARE HERE</color>", UIStyles.Sized(UIStyles.Center, 20));
                    if (!gm.Map.Traveling && GUI.Button(r, GUIContent.none, GUIStyle.none))
                    {
                        gm.Audio.Play("click", 0.6f);
                        if (unlocked) { mapSelected = id; if (id == d.currentRegion) mapOverview = false; }
                        else Toast("Clear " + region.unlockAfterMission + " to open the road to this land.");
                    }
                }
            }

            if (gm.CurrentEncounter != null)
            {
                DrawEncounter(gm.CurrentEncounter);
                return;
            }
            if (gm.Map.Traveling)
            {
                var r = new Rect(W * 0.5f - 420f, H - 190f, 840f, 100f);
                UIStyles.PanelBox(r, UIStyles.Gold);
                int dots = Mathf.FloorToInt(Time.unscaledTime * 3f) % 4;
                GUI.Label(r, "Travelling to " + RegionName(gm.TravelDestination) + new string('.', dots), UIStyles.Sized(UIStyles.Center, 40));
                return;
            }

            // Region panel.
            var region2 = GameDatabase.GetRegion(mapSelected);
            if (region2 == null) return;
            var list = GameDatabase.MissionsInRegion(mapSelected);
            bool inArea = mapSelected == d.currentRegion && !mapOverview && list.Count > 0;
            int current = list.Count - 1;
            for (int i = 0; i < list.Count; i++)
                if (d.IsMissionUnlocked(list[i]) && !d.IsMissionCleared(list[i].id)) { current = i; break; }
            gm.Map.ClearFocus();
            if (inArea) gm.Map.EnterArea(mapSelected, list.Count, current);
            else gm.Map.ExitArea();
            if (areaPick < 0 || areaPick >= list.Count || areaRegion != mapSelected) { areaPick = current; areaRegion = mapSelected; }
            if (inArea) DrawAreaNodes(list, current);

            float pk = Enter(0.1f, 0.4f);
            var panel = new Rect(safe.xMax - 610f + (1f - pk) * 300f, safe.y + 124f, 590f, H - safe.y - 144f);
            Round(Offset(panel, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(panel, new Color(0.07f, 0.08f, 0.13f, 0.94f), 16f);
            float px = panel.x + 26f, pw = panel.width - 52f;
            float y = panel.y + 18f;
            UIStyles.Outlined(new Rect(px, y, pw, 56f), region2.name, UIStyles.Sized(UIStyles.H1, 42), new Color(1f, 0.74f, 0.22f), 1.5f);
            y += 56f;
            int chapterNo = 0;
            foreach (var m in list) if (m.chapter > 0) { chapterNo = m.chapter; break; }
            if (chapterNo > 0) { GUI.Label(new Rect(px, y, pw, 32f), "<color=#B8B8C8>Chapter " + chapterNo + "</color>", UIStyles.Sized(UIStyles.Body, 24)); y += 36f; }
            GUI.Label(new Rect(px, y, pw, 60f), region2.subtitle, UIStyles.Sized(UIStyles.Body, 21));
            y += 66f;
            if (mapSelected != d.currentRegion && d.IsRegionUnlocked(region2))
            {
                if (FlatBtn(new Rect(px, y, pw, 60f), "TRAVEL HERE", TileBlue, true, 26)) { mapOverview = false; gm.TravelTo(mapSelected); }
                y += 72f;
            }

            var view = new Rect(panel.x + 14f, y, panel.width - 28f, panel.yMax - y - 116f);
            float rowH = 64f;
            var content = new Rect(0f, 0f, view.width - (list.Count * (rowH + 4f) > view.height ? 22f : 0f), list.Count * (rowH + 4f));
            mapScroll = GUI.BeginScrollView(view, mapScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                bool unlocked = d.IsMissionUnlocked(m);
                bool boss = m.type == MissionType.Boss;
                var r = new Rect(0f, i * (rowH + 4f), content.width, rowH);
                bool picked = i == areaPick;
                if (picked) Round(r, new Color(0.2f, 0.22f, 0.32f), 8f);
                else if (r.Contains(Event.current.mousePosition)) Round(r, new Color(1f, 1f, 1f, 0.05f), 8f);
                if (picked) Round(new Rect(r.x, r.y + 4f, 6f, r.height - 8f), new Color(1f, 0.78f, 0.25f), 3f);
                else if (boss) Round(new Rect(r.x, r.y + 14f, 6f, r.height - 28f), UIStyles.Crimson, 3f);
                float tx = r.x + 20f;
                if (boss)
                {
                    UIStyles.Colored(new Rect(tx, r.y, 80f, r.height), "BOSS", UIStyles.Sized(UIStyles.Body, 22), new Color(1f, 0.3f, 0.3f));
                    tx += 70f;
                }
                if (picked && !boss)
                {
                    Round(new Rect(tx - 4f, r.y + 12f, 64f, r.height - 24f), new Color(0.1f, 0.1f, 0.16f), 6f);
                }
                UIStyles.Colored(new Rect(tx, r.y, 70f, r.height), "<b>" + m.MissionLabel + "</b>", UIStyles.Sized(UIStyles.Body, boss ? 22 : 26), unlocked ? Color.white : new Color(1f, 0.75f, 0.35f));
                GUI.Label(new Rect(tx + 76f, r.y, r.width - tx - 190f, r.height), m.name, UIStyles.Sized(UIStyles.Body, 22));
                if (!unlocked) LockIcon(new Vector2(r.xMax - 34f, r.center.y), 30f, new Color(0.85f, 0.85f, 0.9f));
                else StarsIcons(new Vector2(r.xMax - 118f, r.center.y), 28f, d, m);
                if (GUI.Button(r, GUIContent.none, GUIStyle.none))
                {
                    gm.Audio.Play("click", 0.5f);
                    if (!unlocked) Toast("Locked — clear " + m.requiresMissionId + " first.");
                    else if (picked) { gm.SelectedMission = m; gm.GoTo(GameScreen.MissionDetail); }
                    else areaPick = i;
                }
            }
            GUI.EndScrollView();

            var chosen = areaPick >= 0 && areaPick < list.Count ? list[areaPick] : null;
            var cb = new Rect(px - 4f, panel.yMax - 100f, pw + 8f, 78f);
            if (chosen != null && d.IsMissionUnlocked(chosen))
            {
                if (FlatBtn(cb, "CONTINUE " + chosen.MissionLabel + "   ›", TileRed, true, 34))
                {
                    gm.SelectedMission = chosen;
                    gm.GoTo(GameScreen.MissionDetail);
                }
            }
            else
            {
                var next = gm.NextStoryMission();
                if (next != null && FlatBtn(cb, "CONTINUE " + next.MissionLabel + "   ›", TileRed, true, 34))
                {
                    gm.SelectedMission = next;
                    mapSelected = next.regionId;
                    gm.GoTo(GameScreen.MissionDetail);
                }
            }

            // Small toggle between this area and the whole world, under the top bar.
            if (mapSelected == d.currentRegion)
            {
                var tr = new Rect(safe.x + 22f, safe.y + 124f, 230f, 50f);
                if (FlatBtn(tr, mapOverview ? "‹ AREA MAP" : "ALL REGIONS", new Color(0.15f, 0.16f, 0.26f, 0.9f), true, 20)) mapOverview = !mapOverview;
            }
        }

        /// <summary>Three star glyphs: gold for objectives done, dim for the rest.</summary>
        void StarsIcons(Vector2 left, float size, PlayerData d, MissionDefinition m)
        {
            var p = d.GetMission(m.id);
            int mask = p != null ? p.objectivesMask : 0;
            for (int i = 0; i < 3; i++)
            {
                bool on = (mask & (1 << i)) != 0;
                UIStyles.Colored(new Rect(left.x + i * size * 1.05f, left.y - size * 0.6f, size * 1.1f, size * 1.2f), "★", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(size)),
                    on ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 1f, 1f, 0.18f));
            }
        }

        bool mapOverview;
        int areaPick = -1;
        string areaRegion;

        /// <summary>★ per objective ever completed, ☆ for the rest.</summary>
        static string StarRow(PlayerData d, MissionDefinition m)
        {
            var p = d.GetMission(m.id);
            int mask = p != null ? p.objectivesMask : 0;
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < 3; i++) sb.Append((mask & (1 << i)) != 0 ? "★" : "☆");
            return sb.ToString();
        }

        /// <summary>Locks and number plates over the area's stone pedestals (the trail itself is 3D).</summary>
        void DrawAreaNodes(List<MissionDefinition> list, int current)
        {
            var cam = Camera.main;
            var pts = gm.Map.AreaPoints;
            if (cam == null || pts.Count != list.Count) return;
            float s = HudLayout.Scale;
            var d = gm.Data;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 top = pts[i] + Vector3.up * 0.4f;
                Vector3 sp = cam.WorldToScreenPoint(top);
                if (sp.z <= 0f) continue;
                var c = new Vector2(sp.x / s, (Screen.height - sp.y) / s);
                // Size the icons with the pedestal on screen.
                Vector3 edge = cam.WorldToScreenPoint(top + cam.transform.right * 0.7f);
                float px = Mathf.Clamp(Mathf.Abs(edge.x - sp.x) / s, 18f, 60f);
                var m = list[i];
                bool unlocked = d.IsMissionUnlocked(m);
                bool cleared = d.IsMissionCleared(m.id);
                bool boss = m.type == MissionType.Boss;
                if (i != current)
                {
                    if (!unlocked) LockIcon(c + new Vector2(0f, -px * 0.55f), px * 0.95f, Color.white);
                    else if (cleared) UIStyles.Outlined(new Rect(c.x - px, c.y - px * 1.4f, px * 2f, px * 1.4f), "★", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(px * 1.1f)), new Color(1f, 0.82f, 0.25f), 2f);
                    else UIStyles.Outlined(new Rect(c.x - px, c.y - px * 1.4f, px * 2f, px * 1.4f), "!", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(px * 1.1f)), new Color(1f, 0.9f, 0.4f), 2f);
                    if (boss) UIStyles.Outlined(new Rect(c.x - px * 1.5f, c.y - px * 2.4f, px * 3f, px), "BOSS", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(px * 0.55f)), new Color(1f, 0.35f, 0.3f), 2f);
                }
                // Number plate under the pedestal.
                string label = m.MissionLabel;
                var st = UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(Mathf.Clamp(px * 0.62f, 18f, 30f)));
                float lw = Mathf.Max(px * 1.9f, st.CalcSize(new GUIContent(label)).x + 22f), lh = px * 0.95f;
                var plate = new Rect(c.x - lw * 0.5f, c.y + px * 0.55f, lw, lh);
                Round(Offset(plate, 0f, 3f), new Color(0f, 0f, 0f, 0.35f), lh * 0.3f);
                Round(plate, i == areaPick ? new Color(0.2f, 0.18f, 0.08f, 0.95f) : new Color(0.1f, 0.11f, 0.16f, 0.92f), lh * 0.3f);
                RoundFrame(plate, i == areaPick ? new Color(1f, 0.8f, 0.3f) : new Color(1f, 1f, 1f, 0.15f), 2f, lh * 0.3f);
                GUI.Label(plate, "<b>" + label + "</b>", st);
                var hit = new Rect(c.x - px * 1.1f, c.y - px * 1.6f, px * 2.2f, px * 3.2f);
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
                {
                    gm.Audio.Play("click", 0.5f);
                    if (!unlocked) Toast("Locked — clear " + m.requiresMissionId + " first.");
                    else if (areaPick == i) { gm.SelectedMission = m; gm.GoTo(GameScreen.MissionDetail); }
                    else areaPick = i;
                }
            }
        }

        void DrawEncounter(Encounter e)
        {
            float k = Mathf.Clamp01(Enter(0f, 0.3f));
            var r = new Rect(W * 0.5f - 560f, H - 470f + (1f - k) * 80f, 1120f, 400f);
            UIStyles.PanelBox(r, e.battle ? UIStyles.Crimson : UIStyles.Gold);
            UIStyles.Outlined(new Rect(r.x + 40f, r.y + 24f, r.width - 80f, 70f), e.title, UIStyles.Sized(UIStyles.H1, 48), e.battle ? UIStyles.Bad : UIStyles.Gold, 2f);
            GUI.Label(new Rect(r.x + 40f, r.y + 104f, r.width - 80f, 150f), e.text, UIStyles.Sized(UIStyles.Body, 28));
            if (e.battle)
                GUI.Label(new Rect(r.x + 40f, r.y + 236f, r.width - 80f, 40f), "<color=#AAAAAA>Enemy Lv." + e.level + " · win for crystals and materials, then continue your journey</color>", UIStyles.Small);
            if (Btn(new Rect(r.x + 40f, r.yMax - 120f, 500f, 96f), e.accept, UIStyles.ButtonBig))
            {
                string msg = gm.ResolveEncounter(true);
                if (!string.IsNullOrEmpty(msg)) Toast(msg);
            }
            if (Btn(new Rect(r.xMax - 540f, r.yMax - 120f, 500f, 96f), e.decline, UIStyles.Button))
            {
                string msg = gm.ResolveEncounter(false);
                if (!string.IsNullOrEmpty(msg)) Toast(msg);
            }
        }

        bool RegionHasOpenBoss(string regionId)
        {
            foreach (var m in GameDatabase.MissionsInRegion(regionId))
                if (m.type == MissionType.Boss && gm.Data.IsMissionUnlocked(m) && !gm.Data.IsMissionCleared(m.id)) return true;
            return false;
        }

        string RegionTags(string regionId)
        {
            var d = gm.Data;
            int total = 0, done = 0;
            bool boss = false, side = false, treasure = false;
            foreach (var m in GameDatabase.MissionsInRegion(regionId))
            {
                total++;
                bool cleared = d.IsMissionCleared(m.id);
                if (cleared) done++;
                else if (d.IsMissionUnlocked(m))
                {
                    if (m.type == MissionType.Boss) boss = true;
                    if (m.type == MissionType.Side) side = true;
                    if (m.type == MissionType.Treasure) treasure = true;
                }
            }
            string s = done == total && total > 0 ? "<color=#7CFF8A>✔ COMPLETE</color>" : done + "/" + total;
            if (boss) s += "  <color=#FF6060>☠ BOSS</color>";
            if (side) s += "  <color=#7FD8FF>! SIDE</color>";
            if (treasure) s += "  <color=#FFD36B>◆ TREASURE</color>";
            return s;
        }

        static string TypeTag(MissionType t)
        {
            switch (t)
            {
                case MissionType.Boss: return "<color=#FF6060>☠ BOSS</color>";
                case MissionType.Side: return "<color=#7FD8FF>! SIDE</color>";
                case MissionType.Treasure: return "<color=#FFD36B>◆ TREASURE</color>";
                case MissionType.Event: return "<color=#FF9CFF>✦ EVENT</color>";
                case MissionType.Training: return "<color=#9CF29C>TRAINING</color>";
                case MissionType.Encounter: return "<color=#FF9C7A>ENCOUNTER</color>";
                default: return "<color=#FFFFFF>STORY</color>";
            }
        }

        // ------------------------------------------------------------------ Story journal

        int selectedChapter = 1;

        void DrawStory()
        {
            var d = gm.Data;
            TopBar("JOURNAL", GameScreen.MainMenu);
            float top = safe.y + 140f;
            float x = safe.x + 30f;
            foreach (var ch in GameDatabase.Chapters)
            {
                bool open = ch.missions.Count > 0 && d.IsMissionUnlocked(ch.missions[0]);
                var r = new Rect(x, top, 440f, 96f);
                bool sel = ch.number == selectedChapter;
                string title = ch.number == 7 ? "Final Chapter" : "Chapter " + ch.number;
                string label = title + "\n<size=22>" + (open ? ch.title : "???") + "</size>";
                if (Btn(r, label, sel ? UIStyles.ButtonBig : UIStyles.Button, open)) selectedChapter = ch.number;
                top += 106f;
            }

            var chapter = GameDatabase.Chapters.Find(c => c.number == selectedChapter);
            if (chapter == null) return;
            float mx = x + 480f, mw = safe.xMax - mx - 30f;
            var head = new Rect(mx, safe.y + 140f, mw, 130f);
            UIStyles.PanelBox(head, UIStyles.Gold);
            var chArt = ArtLibrary.Chapter(chapter);
            if (chArt != null)
            {
                ArtLibrary.DrawCover(new Rect(head.x + 3f, head.y + 3f, head.width - 6f, head.height - 6f), chArt, 0.9f);
                UIStyles.Rect(new Rect(head.x + 3f, head.y + 3f, head.width - 6f, head.height - 6f), new Color(0f, 0f, 0f, 0.5f));
            }
            GUI.Label(new Rect(head.x + 24f, head.y + 12f, mw - 48f, 50f), chapter.title, UIStyles.H1);
            GUI.Label(new Rect(head.x + 24f, head.y + 70f, mw - 48f, 56f), "<i>" + chapter.synopsis + "</i>", UIStyles.Small);
            float my = head.yMax + 16f;
            foreach (var m in chapter.missions)
            {
                var r = new Rect(mx, my, mw, 104f);
                bool unlocked = d.IsMissionUnlocked(m);
                var prog = d.GetMission(m.id);
                UIStyles.PanelBox(r, unlocked ? UIStyles.Gold : Color.gray);
                GUI.Label(new Rect(r.x + 20f, r.y + 10f, mw - 560f, 44f), TypeTag(m.type) + "  " + m.id + "  " + (unlocked ? m.name : "???"), UIStyles.Sized(UIStyles.H2, 30));
                GUI.Label(new Rect(r.x + 20f, r.y + 56f, mw - 560f, 44f), unlocked ? m.storyText : "Locked", UIStyles.Sized(UIStyles.Small, 20));
                string stars = "";
                for (int i = 0; i < 3; i++) stars += prog != null && (prog.objectivesMask & (1 << i)) != 0 ? "★" : "☆";
                UIStyles.Colored(new Rect(r.xMax - 540f, r.y + 20f, 140f, 60f), stars, UIStyles.Sized(UIStyles.Center, 38), UIStyles.Gold);
                // Replay story scenes already seen.
                float bx = r.xMax - 390f;
                foreach (var sc in new[] { m.cutsceneBefore, m.cutsceneAfter })
                {
                    if (string.IsNullOrEmpty(sc) || !d.HasSeen(sc)) continue;
                    string id = sc;
                    if (Btn(new Rect(bx, r.y + 22f, 120f, 60f), "▶ SCENE", UIStyles.ButtonSmall)) gm.PlayCutscene(id, () => gm.GoTo(GameScreen.Story));
                    bx += 128f;
                }
                if (unlocked && Btn(new Rect(r.xMax - 130f, r.y + 22f, 110f, 60f), "GO", UIStyles.ButtonSmall))
                {
                    gm.SelectedMission = m;
                    gm.GoTo(GameScreen.MissionDetail);
                }
                my += 112f;
            }
        }

        // ------------------------------------------------------------------ Mission page

        /// <summary>
        /// Mission preparation, laid out like the reference: story panel on the left (breadcrumb, title, hook,
        /// levels, enemies, objectives, rewards), the leader standing on the trail in the middle, and the team
        /// panel on the right with CHANGE TEAM and TRAVEL &amp; PLAY.
        /// </summary>
        void DrawMissionDetail()
        {
            var m = gm.SelectedMission;
            if (m == null) { gm.GoTo(GameScreen.WorldMap); return; }
            var d = gm.Data;
            var prog = d.GetMission(m.id);
            bool unlocked = d.IsMissionUnlocked(m);

            // The 3D preview: this mission's region, the leader on its stop, looking up the trail.
            var regionList = GameDatabase.MissionsInRegion(m.regionId);
            int stop = regionList.IndexOf(m);
            if (stop >= 0 && System.Array.IndexOf(MapStage.RouteOrder, m.regionId) >= 0)
            {
                gm.Map.EnterArea(m.regionId, regionList.Count, stop);
                gm.Map.FocusStop(stop);
            }

            MissionTopBar(m);
            float k = Enter(0f, 0.4f);

            // ---- Left: the story panel.
            var left = new Rect(safe.x + 32f - (1f - k) * 200f, safe.y + 138f, 710f, H - safe.y - 232f);
            Round(Offset(left, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(left, new Color(0.05f, 0.06f, 0.1f, 0.88f), 16f);
            RoundFrame(left, new Color(1f, 1f, 1f, 0.08f), 2f, 16f);
            float x = left.x + 30f, w = left.width - 60f, y = left.y + 22f;
            string crumb = "<b>" + (m.type == MissionType.Story ? "STORY" : m.type.ToString().ToUpper()) + "</b>    <color=#AAAAAA>" + RegionName(m.regionId) +
                           (m.chapter > 0 ? "  ›  </color>Chapter " + m.chapter : "</color>");
            GUI.Label(new Rect(x, y, w, 34f), crumb, UIStyles.Sized(UIStyles.Body, 22));
            y += 38f;
            UIStyles.Outlined(new Rect(x, y, w, 66f), m.name, UIStyles.Sized(UIStyles.H1, 50), Color.white, 1.5f);
            y += 66f;
            GUI.Label(new Rect(x, y, w, 64f), "<i><color=#C8C8D0>" + m.storyText + "</color></i>", UIStyles.Sized(UIStyles.Body, 22));
            y += 66f;
            UIStyles.Rect(new Rect(x, y, w, 1f), new Color(1f, 1f, 1f, 0.15f));
            y += 16f;

            int avgLevel = 0;
            foreach (var id in d.team) { var c = d.GetCharacter(id); if (c != null) avgLevel += c.level; }
            avgLevel = d.team.Count > 0 ? avgLevel / d.team.Count : 1;
            string lc = avgLevel >= m.recommendedLevel ? "#7CFF8A" : "#FF6060";
            GUI.Label(new Rect(x, y, w, 36f), "<color=#BBBBBB>Recommended</color> <b>Lv. " + m.recommendedLevel + "</b>   <color=#555555>|</color>   <color=#BBBBBB>Your team</color> <b><color=" + lc + ">Lv. " + avgLevel +
                "</color></b>   <color=#555555>|</color>   <color=#BBBBBB>Enemy</color> <b>Lv. " + m.enemyLevel + "</b>", UIStyles.Sized(UIStyles.Body, 23));
            y += 46f;

            // Enemies with portraits.
            GUI.Label(new Rect(x, y, w, 36f), "<b>ENEMIES</b>", UIStyles.Sized(UIStyles.Body, 26));
            y += 40f;
            var foes = new List<string>();
            foreach (var wv in m.waves) foreach (var sp in wv.spawns) if (!foes.Contains(sp.enemyId)) foes.Add(sp.enemyId);
            foreach (var pb in m.preBosses) if (!foes.Contains(pb)) foes.Add(pb);
            if (!string.IsNullOrEmpty(m.bossId) && !foes.Contains(m.bossId)) foes.Add(m.bossId);
            int shownFoes = Mathf.Min(foes.Count, 4);
            string tip = null;
            for (int i = 0; i < shownFoes; i++)
            {
                var e = GameDatabase.GetEnemy(foes[i]);
                if (e == null) continue;
                var er = new Rect(x + (i % 2) * (w * 0.5f), y + (i / 2) * 90f, w * 0.5f - 12f, 82f);
                var pr = new Rect(er.x, er.y, 78f, 78f);
                bool boss = e.archetype == EnemyArchetype.Boss;
                Round(pr, boss ? new Color(0.35f, 0.08f, 0.1f) : new Color(0.14f, 0.15f, 0.2f), 10f);
                var art = ArtLibrary.Monster(e);
                if (art != null) GUI.DrawTexture(new Rect(pr.x + 3f, pr.y + 3f, pr.width - 6f, pr.height - 6f), art, ScaleMode.ScaleAndCrop, true);
                RoundFrame(pr, boss ? UIStyles.Crimson : new Color(1f, 1f, 1f, 0.2f), 2f, 10f);
                GUI.Label(new Rect(pr.xMax + 14f, er.y + 6f, er.width - 96f, 34f), e.displayName + " <color=#AAAAAA><size=18>(" + ArchetypeName(e.archetype) + ")</size></color>", UIStyles.Sized(UIStyles.Body, 22));
                UIStyles.Colored(new Rect(pr.xMax + 14f, er.y + 40f, er.width - 96f, 30f), ElementName(e.element), UIStyles.Sized(UIStyles.Body, 20), ElementChart.ColorOf(e.element));
                if (er.Contains(Event.current.mousePosition)) tip = e.weakness;
            }
            y += Mathf.CeilToInt(shownFoes / 2f) * 90f + 6f;
            if (tip != null) GUI.Label(new Rect(x, y - 8f, w, 28f), "<color=#FFD36B>Tip: " + tip + "</color>", UIStyles.Sized(UIStyles.Small, 17));
            y += 14f;

            // Objectives.
            if (!m.training)
            {
                GUI.Label(new Rect(x, y, w, 36f), "<b>OBJECTIVES</b>", UIStyles.Sized(UIStyles.Body, 26));
                y += 40f;
                string[] labels = { "Defeat " + m.killObjective + " demons", "No player falls", "Clear within " + Mathf.RoundToInt(m.parTime) + "s" };
                for (int i = 0; i < 3; i++)
                {
                    bool done = prog != null && (prog.objectivesMask & (1 << i)) != 0;
                    UIStyles.Colored(new Rect(x + 4f, y, 36f, 36f), "★", UIStyles.Sized(UIStyles.Center, 28), done ? new Color(1f, 0.8f, 0.2f) : new Color(0.55f, 0.55f, 0.6f));
                    GUI.Label(new Rect(x + 50f, y, 300f, 36f), labels[i], UIStyles.Sized(UIStyles.Body, 22));
                    if (!done)
                    {
                        GUI.Label(new Rect(x + 320f, y, 50f, 36f), "+" + RewardSystem.CrystalsPerNewObjective, UIStyles.Sized(UIStyles.Body, 20));
                        DiamondIcon(new Vector2(x + 372f, y + 18f), 24f);
                    }
                    y += 38f;
                }
                y += 10f;
                GUI.Label(new Rect(x, y, 140f, 36f), "<b>REWARDS</b>", UIStyles.Sized(UIStyles.Body, 24));
                GUI.Label(new Rect(x + 140f, y, w - 140f, 36f), RewardLine(m.rewards), UIStyles.Sized(UIStyles.Body, 20));
                y += 42f;
                if (prog == null || !prog.cleared)
                {
                    UIStyles.Colored(new Rect(x, y, 160f, 36f), "<b>FIRST CLEAR</b>", UIStyles.Sized(UIStyles.Body, 22), new Color(1f, 0.4f, 0.35f));
                    GUI.Label(new Rect(x + 170f, y, w - 170f, 36f), RewardLine(m.firstClearRewards), UIStyles.Sized(UIStyles.Body, 20));
                }
            }

            // ---- Right: your team.
            var right = new Rect(safe.xMax - 560f + (1f - k) * 200f, safe.y + 138f, 528f, H - safe.y - 232f);
            Round(Offset(right, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(right, new Color(0.05f, 0.06f, 0.1f, 0.88f), 16f);
            RoundFrame(right, new Color(1f, 1f, 1f, 0.08f), 2f, 16f);
            float rx = right.x + 26f, rw = right.width - 52f, ry = right.y + 20f;
            GUI.Label(new Rect(rx, ry, 300f, 56f), "<b>YOUR TEAM</b>", UIStyles.Sized(UIStyles.H1, 40));
            UIStyles.Colored(new Rect(rx + 250f, ry, rw - 250f, 56f), "<b>" + CharacterSystem.TeamPower(d).ToString("N0") + "</b>", UIStyles.Sized(UIStyles.Right, 44), new Color(1f, 0.82f, 0.25f));
            ry += 62f;
            UIStyles.Rect(new Rect(rx, ry, rw, 1f), new Color(1f, 1f, 1f, 0.15f));
            ry += 10f;
            string[] roles = { "LEADER", "VANGUARD", "STRIKER", "SUPPORT" };
            for (int i = 0; i < d.team.Count && i < 4; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                string ec = UIStyles.Hex(ElementChart.ColorOf(def.element));
                GUI.Label(new Rect(rx, ry, rw, 40f), "<color=#AAAAAA><size=17>" + roles[i] + "</size></color>  <color=#" + ec + ">" + ElementName(def.element) + "</color> " + def.displayName +
                    "  <color=#BBBBBB>Lv. " + c.level + "</color>  " + new string('★', Mathf.Clamp(c.stars, 1, 7)), UIStyles.Sized(UIStyles.Body, 21));
                ry += 40f;
            }
            ry += 6f;
            GUI.Label(new Rect(rx, ry, rw, 54f), "<color=#AAAAAA><size=17>Type advantage deals ×1.5. Water › Flame › Beast › Thunder › Water, Light ↔ Dark.</size></color>", UIStyles.Small);
            ry += 58f;
            float pw = (rw - 3f * 10f) / 4f, ph = pw * 1.32f;
            for (int i = 0; i < 4; i++)
            {
                var pr = new Rect(rx + i * (pw + 10f), ry, pw, ph);
                if (i < d.team.Count)
                {
                    var c = d.GetCharacter(d.team[i]);
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    if (c == null || def == null) continue;
                    SlayerCard(pr, def, c, false, -1);
                }
                else
                {
                    Round(pr, new Color(1f, 1f, 1f, 0.05f), 12f);
                    GUI.DrawTexture(new Rect(pr.center.x - 24f, pr.center.y - 24f, 48f, 48f), IconFactory.Get("plus"), ScaleMode.ScaleToFit, true);
                    if (GUI.Button(pr, GUIContent.none, GUIStyle.none)) { teamReturn = GameScreen.MissionDetail; gm.GoTo(GameScreen.Team); }
                }
            }
            // CHANGE TEAM (outlined purple) and TRAVEL & PLAY (big red).
            var ct = new Rect(rx, right.yMax - 214f, rw, 68f);
            bool hov = ct.Contains(Event.current.mousePosition);
            Round(ct, hov ? new Color(0.24f, 0.15f, 0.42f) : new Color(0.16f, 0.1f, 0.3f), 12f);
            RoundFrame(ct, new Color(0.62f, 0.45f, 1f), 2f, 12f);
            GUI.Label(ct, "<b>CHANGE TEAM</b>", UIStyles.Sized(UIStyles.Center, 26));
            if (GUI.Button(ct, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("click", 0.5f); teamReturn = GameScreen.MissionDetail; gm.GoTo(GameScreen.Team); }

            var pb2 = new Rect(rx, right.yMax - 130f, rw, 108f);
            if (!unlocked)
            {
                Round(pb2, new Color(0.2f, 0.2f, 0.25f), 14f);
                GUI.Label(pb2, "<color=#FF8080>Clear " + m.requiresMissionId + " first</color>", UIStyles.Sized(UIStyles.Center, 30));
            }
            else
            {
                bool travel = m.regionId != d.currentRegion && System.Array.IndexOf(MapStage.RouteOrder, m.regionId) >= 0 && m.type != MissionType.Training && m.type != MissionType.Event;
                bool hp = pb2.Contains(Event.current.mousePosition);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                RoundFrame(Grow(pb2, 3f + pulse * 2f), new Color(1f, 0.35f, 0.3f, 0.35f + 0.3f * pulse), 3f, 16f);
                Round(Offset(pb2, 0f, 5f), new Color(0f, 0f, 0f, 0.4f), 14f);
                Round(pb2, hp ? new Color(0.95f, 0.2f, 0.18f) : new Color(0.85f, 0.12f, 0.12f), 14f);
                Round(new Rect(pb2.x, pb2.y, pb2.width, pb2.height * 0.45f), new Color(1f, 1f, 1f, 0.1f), 14f);
                GUI.DrawTexture(new Rect(pb2.x + 40f, pb2.y + 22f, 64f, 64f), IconFactory.Get("swords"), ScaleMode.ScaleToFit, true);
                UIStyles.Outlined(new Rect(pb2.x + 110f, pb2.y, pb2.width - 130f, pb2.height), travel ? "TRAVEL & PLAY" : "PLAY", UIStyles.Sized(UIStyles.Center, 42), Color.white, 2f);
                if (GUI.Button(pb2, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("perfect", 0.5f); gm.BeginMission(m); }
            }
        }

        /// <summary>Mission page header: BACK, the small logo, the mission number, resources.</summary>
        void MissionTopBar(MissionDefinition m)
        {
            UIStyles.Rect(new Rect(0f, 0f, W, 118f + safe.y), new Color(0.04f, 0.05f, 0.09f, 0.9f));
            var br = new Rect(safe.x + 24f, safe.y + 14f, 104f, 92f);
            bool hover = br.Contains(Event.current.mousePosition);
            Round(br, hover ? new Color(0.42f, 0.28f, 0.72f) : new Color(0.32f, 0.2f, 0.6f), 12f);
            UIStyles.Outlined(new Rect(br.x, br.y + 4f, br.width, 44f), "←", UIStyles.Sized(UIStyles.Center, 40), Color.white, 1f);
            GUI.Label(new Rect(br.x, br.y + 50f, br.width, 34f), "<b>BACK</b>", UIStyles.Sized(UIStyles.Center, 22));
            if (GUI.Button(br, GUIContent.none, GUIStyle.none) || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                gm.Audio.Play("click", 0.5f);
                gm.GoTo(GameScreen.WorldMap);
                if (Event.current.type == EventType.KeyDown) Event.current.Use();
            }
            UIStyles.Outlined(new Rect(br.xMax + 30f, safe.y + 18f, 400f, 52f), GameConfig.TitleLine1, UIStyles.Sized(UIStyles.Title, 38), new Color(0.9f, 0.12f, 0.12f), 2f);
            GUI.Label(new Rect(br.xMax + 32f, safe.y + 68f, 420f, 34f), "<b>H A S H I R A   C H R O N I C L E S</b>", UIStyles.Sized(UIStyles.Body, 18));
            string title = m.type == MissionType.Training ? "TRAINING" : "MISSION " + m.MissionLabel;
            UIStyles.Outlined(new Rect(br.xMax + 480f, safe.y + 20f, 560f, 76f), title, UIStyles.Sized(UIStyles.H1, 56), Color.white, 2f);
            Currencies(new Rect(safe.xMax - 1000f, safe.y + 32f, 980f, 56f));
        }

        static string ArchetypeName(EnemyArchetype a)
        {
            switch (a)
            {
                case EnemyArchetype.Normal: return "Basic";
                case EnemyArchetype.Fast: return "Fast";
                case EnemyArchetype.Tank: return "Tank";
                case EnemyArchetype.Ranged: return "Ranged";
                case EnemyArchetype.Elite: return "Elite";
                default: return "Boss";
            }
        }

        /// <summary>One coloured line of rewards: EXP, coins, scrolls, ore, crystals.</summary>
        static string RewardLine(RewardBundle r)
        {
            var parts = new List<string>();
            if (r.exp > 0) parts.Add("<color=#C9A7FF>EXP +" + r.exp.ToString("N0") + "</color>");
            if (r.coins > 0) parts.Add("<color=#FFD36B>Coins +" + r.coins.ToString("N0") + "</color>");
            if (r.crystals > 0) parts.Add("<color=#7FD8FF>Crystals +" + r.crystals + "</color>");
            if (r.expScrolls > 0) parts.Add("EXP Scroll ×" + r.expScrolls);
            if (r.skillScrolls > 0) parts.Add("Skill Scroll ×" + r.skillScrolls);
            if (r.ascensionOre > 0) parts.Add("Ore ×" + r.ascensionOre);
            foreach (var e in r.equipmentIds)
            {
                var def = GameDatabase.GetEquipment(e);
                if (def != null) parts.Add("<color=#C9A7FF>" + def.displayName + "</color>");
            }
            if (!string.IsNullOrEmpty(r.characterId))
            {
                var c = GameDatabase.GetCharacter(r.characterId);
                if (c != null) parts.Add("<color=#FF9C7A>New: " + c.displayName + "</color>");
            }
            return parts.Count > 0 ? string.Join("   ", parts.ToArray()) : "—";
        }

        static readonly Dictionary<string, string> routeCache = new Dictionary<string, string>();

        static string RouteFor(MissionDefinition m)
        {
            string r;
            if (!routeCache.TryGetValue(m.id, out r))
            {
                r = Journey.Build(m).RouteText();
                if (!string.IsNullOrEmpty(m.bossId)) r += "  <color=#FF6060>☠</color>";
                routeCache[m.id] = r;
            }
            return r;
        }

        /// <summary>One card per enemy type: concept art (when available), name, role and the trick to beating it.</summary>
        float DrawBestiary(MissionDefinition m, Rect row)
        {
            var seen = new List<string>();
            foreach (var wv in m.waves)
                foreach (var sp in wv.spawns)
                    if (!seen.Contains(sp.enemyId)) seen.Add(sp.enemyId);
            float cw = Mathf.Min(300f, (row.width - (seen.Count - 1) * 8f) / Mathf.Max(1, seen.Count));
            string tip = null;
            for (int i = 0; i < seen.Count; i++)
            {
                var e = GameDatabase.GetEnemy(seen[i]);
                if (e == null) continue;
                var r = new Rect(row.x + i * (cw + 8f), row.y, cw, row.height);
                Round(r, new Color(1f, 1f, 1f, 0.06f), 10f);
                Round(new Rect(r.x, r.yMax - 5f, r.width, 5f), ElementChart.ColorOf(e.element), 2f);
                var art = ArtLibrary.Monster(e);
                float tx = r.x + 8f;
                if (art != null) { ArtLibrary.DrawCover(new Rect(r.x + 3f, r.y + 3f, 74f, 74f), art); tx = r.x + 84f; }
                GUI.Label(new Rect(tx, r.y + 6f, r.xMax - tx - 4f, 50f), e.displayName, UIStyles.Sized(UIStyles.Body, 21));
                GUI.Label(new Rect(tx, r.y + 50f, r.xMax - tx - 4f, 28f), ElementTag(e.element) + " <color=#999999>" + e.archetype + "</color>", UIStyles.Sized(UIStyles.Small, 17));
                if (r.Contains(Event.current.mousePosition)) tip = e.displayName + ": " + e.weakness;
            }
            float y = row.yMax;
            if (tip != null)
            {
                GUI.Label(new Rect(row.x, y + 2f, row.width, 30f), "<color=#FFD36B>" + tip + "</color>", UIStyles.Sized(UIStyles.Small, 18));
                y += 28f;
            }
            return y;
        }

        static string EnemySummary(MissionDefinition m)
        {
            var seen = new List<string>();
            var sb = new System.Text.StringBuilder();
            foreach (var w in m.waves)
                foreach (var s in w.spawns)
                {
                    if (seen.Contains(s.enemyId)) continue;
                    seen.Add(s.enemyId);
                    var e = GameDatabase.GetEnemy(s.enemyId);
                    if (e == null) continue;
                    if (sb.Length > 0) sb.Append("    ");
                    sb.Append(ElementTag(e.element) + " " + e.displayName + " <color=#999999>(" + e.archetype + ")</color>");
                }
            return sb.Length > 0 ? sb.ToString() : "—";
        }

        // ------------------------------------------------------------------ Results

        static string Rating(BattleResult r)
        {
            if (!r.victory) return "—";
            int n = 0;
            foreach (var o in r.objectives) if (o) n++;
            return n == 3 ? "S" : n == 2 ? "A" : n == 1 ? "B" : "C";
        }

        void DrawResults()
        {
            var r = gm.LastResult;
            if (r == null) { gm.GoTo(GameScreen.MainMenu); return; }
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.6f));
            var panel = new Rect(W * 0.5f - 840f, 50f, 1680f, H - 100f);
            Round(Offset(panel, 0f, 6f), new Color(0f, 0f, 0f, 0.4f), 18f);
            Round(panel, new Color(0.05f, 0.06f, 0.1f, 0.94f), 18f);
            Round(new Rect(panel.x, panel.y, panel.width, 8f), r.victory ? UIStyles.Gold : UIStyles.Crimson, 4f);

            // Stamp-in title.
            float k = Enter(0.05f, 0.35f);
            int size = Mathf.RoundToInt(Mathf.Lerp(150f, 84f, k));
            var tc = r.victory ? UIStyles.Gold : UIStyles.Bad;
            UIStyles.Outlined(new Rect(panel.x, panel.y + 18f, panel.width, 120f), r.victory ? "MISSION COMPLETE" : "MISSION FAILED", UIStyles.Sized(UIStyles.Big, size), new Color(tc.r, tc.g, tc.b, k), 4f);
            GUI.Label(new Rect(panel.x, panel.y + 130f, panel.width, 44f), "Mission " + r.mission.id + " — " + r.mission.name + (r.victory ? "" : "   (" + r.failReason + ")"), UIStyles.Center);

            // Rating.
            float rk = Enter(0.9f, 0.3f);
            if (rk > 0f && r.victory)
            {
                string rating = Rating(r);
                Color rc = rating == "S" ? UIStyles.Gold : rating == "A" ? new Color(0.6f, 0.85f, 1f) : Color.white;
                int rs = Mathf.RoundToInt(Mathf.Lerp(260f, 150f, rk));
                UIStyles.Outlined(new Rect(panel.xMax - 330f, panel.y + 160f, 280f, 220f), rating, UIStyles.Sized(UIStyles.Big, rs), new Color(rc.r, rc.g, rc.b, rk), 5f);
                GUI.Label(new Rect(panel.xMax - 330f, panel.y + 370f, 280f, 40f), "RATING", UIStyles.Sized(UIStyles.Center, 26));
            }

            float x = panel.x + 60f, y = panel.y + 200f;
            int mins = Mathf.FloorToInt(r.time / 60f), secs = Mathf.FloorToInt(r.time % 60f);
            string[] stats = { "Time  " + mins + ":" + secs.ToString("00"), "Demons slain  " + r.kills, "Max combo  " + r.maxCombo, "Total damage  " + Mathf.RoundToInt(r.totalDamage).ToString("N0") };
            for (int i = 0; i < stats.Length; i++)
            {
                float sk = Enter(0.3f + i * 0.1f, 0.25f);
                if (sk <= 0f) continue;
                GUI.Label(new Rect(x - (1f - sk) * 60f, y, 700f, 44f), stats[i], UIStyles.H2);
                y += 50f;
            }
            y += 16f;
            for (int i = 0; i < 3; i++)
            {
                if (Enter(0.7f + i * 0.12f) <= 0f) continue;
                GUI.Label(new Rect(x, y, 760f, 42f), (r.objectives[i] ? "<color=#FFD36B>★</color> " : "<color=#888888>☆</color> ") + r.objectiveLabels[i], UIStyles.Body);
                y += 44f;
            }

            // Rewards count up.
            float rx = panel.x + 830f, ry = panel.y + 200f;
            float ck = Enter(1f, 1.2f);
            GUI.Label(new Rect(rx, ry, 600f, 50f), "REWARDS" + (r.firstClear ? "  <color=#FF9C7A>FIRST CLEAR</color>" : ""), UIStyles.H2);
            ry += 54f;
            var g = r.granted;
            GUI.Label(new Rect(rx, ry, 560f, 40f), "Slayer EXP  <color=#C9A7FF>+" + Mathf.RoundToInt(g.exp * ck).ToString("N0") + "</color>", UIStyles.Body); ry += 42f;
            GUI.Label(new Rect(rx, ry, 560f, 40f), "Gold  <color=#FFD36B>+" + Mathf.RoundToInt(g.coins * ck).ToString("N0") + "</color>", UIStyles.Body); ry += 42f;
            if (g.crystals > 0) { GUI.Label(new Rect(rx, ry, 560f, 40f), "Crystals  <color=#7FD8FF>+" + Mathf.RoundToInt(g.crystals * ck) + "</color>", UIStyles.Body); ry += 42f; }
            string items = "";
            if (g.expScrolls > 0) items += "EXP Scroll ×" + g.expScrolls + "   ";
            if (g.skillScrolls > 0) items += "Skill Scroll ×" + g.skillScrolls + "   ";
            if (g.ascensionOre > 0) items += "Ore ×" + g.ascensionOre;
            if (items.Length > 0 && Enter(1.6f) > 0f) { GUI.Label(new Rect(rx, ry, 600f, 40f), "Items  " + items, UIStyles.Small); ry += 40f; }
            if (Enter(1.8f) > 0f)
                foreach (var e in r.droppedEquipmentNames)
                {
                    GUI.Label(new Rect(rx, ry, 600f, 40f), "<color=#C9A7FF>EQUIPMENT</color>  " + e, UIStyles.Body);
                    ry += 40f;
                }
            if (Enter(2f) > 0f)
                foreach (var lu in r.levelUps)
                {
                    float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
                    UIStyles.Outlined(new Rect(rx, ry, 600f, 44f), "LEVEL UP!  " + lu.name + "  Lv." + lu.from + " → " + lu.to, UIStyles.Sized(UIStyles.Body, Mathf.RoundToInt(28 * pulse)), UIStyles.Good, 2f);
                    ry += 44f;
                }
            if (!string.IsNullOrEmpty(r.unlockedCharacterName) && Enter(2.2f) > 0f)
            {
                ry += 6f;
                UIStyles.Outlined(new Rect(rx, ry, 800f, 50f), "NEW ALLY JOINED: " + r.unlockedCharacterName, UIStyles.Sized(UIStyles.H2, 32), UIStyles.Gold);
            }

            // Next steps.
            float by = panel.yMax - 124f, bw = 370f, gap = 22f;
            float bx = panel.x + (panel.width - (bw * 4f + gap * 3f)) * 0.5f;
            var next = NextMission(r.mission);
            if (r.mission.type == MissionType.Encounter)
            {
                // An encounter interrupted a journey: pick the road back up.
                string cont = gm.PendingMission != null ? "CONTINUE JOURNEY ▶" : "CONTINUE ▶";
                if ((Enter(1.2f) > 0f && FlatBtn(new Rect(bx, by, bw * 2f + gap, 100f), cont, TileRed, true, 30))) gm.ContinueJourney();
                if ((Enter(1.3f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 2f, by, bw, 100f), "RETURN TO MAP", new Color(0.14f, 0.17f, 0.28f), true, 30))) { gm.PendingMission = null; gm.GoTo(GameScreen.WorldMap); }
                if ((Enter(1.4f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 3f, by, bw, 100f), "CHARACTERS", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.GoTo(GameScreen.Characters);
                return;
            }
            if (r.victory)
            {
                if (next != null && (Enter(1.2f) > 0f && FlatBtn(new Rect(bx, by, bw, 100f), "NEXT MISSION ▶", TileRed, true, 30)))
                {
                    gm.SelectedMission = next;
                    gm.GoTo(GameScreen.MissionDetail);
                }
                if ((Enter(1.3f) > 0f && FlatBtn(new Rect(bx + (bw + gap), by, bw, 100f), "REPLAY", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.BeginMission(r.mission);
            }
            else
            {
                if ((Enter(1.2f) > 0f && FlatBtn(new Rect(bx, by, bw, 100f), "RETRY", TileRed, true, 30))) gm.BeginMission(r.mission);
                if ((Enter(1.3f) > 0f && FlatBtn(new Rect(bx + (bw + gap), by, bw, 100f), "UPGRADE SLAYERS", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.GoTo(GameScreen.Characters);
            }
            if ((Enter(1.4f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 2f, by, bw, 100f), "RETURN TO MAP", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.GoTo(GameScreen.WorldMap);
            if ((Enter(1.5f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 3f, by, bw, 100f), "CHARACTERS", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.GoTo(GameScreen.Characters);
        }

        /// <summary>The mission this one unlocks on the main path (falls back to the next open story mission).</summary>
        MissionDefinition NextMission(MissionDefinition m)
        {
            foreach (var x in GameDatabase.AllMissions())
                if (x.requiresMissionId == m.id && (x.type == MissionType.Story || x.type == MissionType.Boss)) return x;
            return gm.NextStoryMission();
        }
    }
}
