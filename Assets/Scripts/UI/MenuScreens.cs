using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Home menu, world map, story journal, mission page and the end-of-mission screen.</summary>
    public partial class UIManager
    {
        // ------------------------------------------------------------------ Home

        float resetArmed = -10f;

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
                case "SUMMON": return d.crystals >= SummonSystem.MultiCostFor(d) ? 10 : 0;
                default: return 0;
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
            // Every visit to the map opens on the detailed area map of where you are (or the chosen story stop).
            if (Time.unscaledTime - gm.ScreenEnteredAt < 0.05f && !gm.Map.Traveling)
            {
                mapOverview = false;
                if (!d.IsRegionUnlocked(GameDatabase.GetRegion(mapSelected))) mapSelected = d.currentRegion;
            }
            var cam = Camera.main;
            float s = HudLayout.Scale;

            // Arrived after tapping a land on the map: open its area map.
            if (!gm.Map.Traveling && travelArea != null && gm.CurrentEncounter == null)
            {
                mapSelected = travelArea;
                travelArea = null;
                mapOverview = false;
                areaPick = -1;
            }
            // Drag to scroll the map, mouse wheel to zoom (the mission panel on the right keeps its own scroll).
            if (!gm.Map.Traveling && gm.CurrentEncounter == null) HandleMapDrag();

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
                    if (!gm.Map.Traveling && GUI.Button(r, GUIContent.none, GUIStyle.none) && !mapDragged)
                    {
                        gm.Audio.Play("click", 0.6f);
                        if (!unlocked) Toast("Clear " + region.unlockAfterMission + " to open the road to this land.");
                        else if (id == d.currentRegion) { mapSelected = id; mapOverview = false; areaPick = -1; }
                        else WalkTo(id);
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
            // Any unlocked region can be browsed as its area map (go back to earlier chapters any time).
            bool inArea = d.IsRegionUnlocked(region2) && !mapOverview && list.Count > 0;
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
            UIStyles.Outlined(new Rect(px, y, pw - 120f, 56f), region2.name, UIStyles.Sized(UIStyles.H1, region2.name.Length > 18 ? 34 : 42), new Color(1f, 0.74f, 0.22f), 1.5f);
            // Previous / next region arrows.
            int ri = System.Array.IndexOf(MapStage.RouteOrder, mapSelected);
            string prevId = null, nextId = null;
            for (int i = ri - 1; i >= 0; i--) if (d.IsRegionUnlocked(GameDatabase.GetRegion(MapStage.RouteOrder[i]))) { prevId = MapStage.RouteOrder[i]; break; }
            for (int i = ri + 1; i < MapStage.RouteOrder.Length; i++) if (d.IsRegionUnlocked(GameDatabase.GetRegion(MapStage.RouteOrder[i]))) { nextId = MapStage.RouteOrder[i]; break; }
            if (FlatBtn(new Rect(px + pw - 112f, y + 4f, 52f, 48f), "‹", new Color(0.15f, 0.17f, 0.28f), prevId != null, 34)) { mapScroll = Vector2.zero; WalkTo(prevId); }
            if (FlatBtn(new Rect(px + pw - 54f, y + 4f, 52f, 48f), "›", new Color(0.15f, 0.17f, 0.28f), nextId != null, 34)) { mapScroll = Vector2.zero; WalkTo(nextId); }
            y += 56f;
            int chapterNo = 0;
            foreach (var m in list) if (m.chapter > 0) { chapterNo = m.chapter; break; }
            if (chapterNo > 0) { GUI.Label(new Rect(px, y, pw, 32f), "<color=#B8B8C8>Chapter " + chapterNo + "</color>", UIStyles.Sized(UIStyles.Body, 24)); y += 36f; }
            GUI.Label(new Rect(px, y, pw, 60f), region2.subtitle, UIStyles.Sized(UIStyles.Body, 21));
            y += 66f;
            if (mapSelected != d.currentRegion && d.IsRegionUnlocked(region2))
            {
                if (FlatBtn(new Rect(px, y, pw, 60f), "TRAVEL HERE  (you are in " + RegionName(d.currentRegion) + ")", TileBlue, true, 22)) WalkTo(mapSelected);
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
            {
                var tr = new Rect(safe.x + 22f, safe.y + 124f, 230f, 50f);
                if (FlatBtn(tr, mapOverview ? "‹ AREA MAP" : "ALL REGIONS", new Color(0.15f, 0.16f, 0.26f, 0.9f), true, 20)) { mapOverview = !mapOverview; gm.Map.ResetPan(); }
                var hint = new Rect(safe.x + 22f, H - 70f, 760f, 44f);
                Round(hint, new Color(0f, 0f, 0f, 0.45f), 22f);
                GUI.Label(hint, mapOverview ? "Drag to explore  ·  scroll to zoom  ·  tap a land to walk there" : "Drag to look around  ·  ‹ › walks to the next land", UIStyles.Sized(UIStyles.Center, 20));
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
        /// <summary>The land the leader is walking to (its area map opens on arrival).</summary>
        string travelArea;
        bool mapDragged;
        float mapDragDistance;

        /// <summary>Leader walks across the world map to this land; its area map opens when they arrive.</summary>
        void WalkTo(string regionId)
        {
            if (string.IsNullOrEmpty(regionId)) return;
            var d = gm.Data;
            if (regionId == d.currentRegion) { mapSelected = regionId; mapOverview = false; areaPick = -1; return; }
            mapSelected = regionId;
            travelArea = regionId;
            mapOverview = true;
            areaPick = -1;
            gm.Map.ExitArea();
            gm.TravelTo(regionId);
        }

        /// <summary>Drag anywhere left of the panel to scroll the map; the wheel zooms the overview.</summary>
        void HandleMapDrag()
        {
            var ev = Event.current;
            bool overPanel = ev.mousePosition.x > safe.xMax - 630f || ev.mousePosition.y < safe.y + 110f;
            switch (ev.type)
            {
                case EventType.MouseDown:
                    mapDragDistance = 0f;
                    mapDragged = false;
                    break;
                case EventType.MouseDrag:
                    if (overPanel) break;
                    mapDragDistance += ev.delta.magnitude;
                    if (mapDragDistance > 14f) mapDragged = true;
                    gm.Map.Pan(ev.delta);
                    break;
                case EventType.ScrollWheel:
                    if (overPanel || gm.Map.AreaMode) break;
                    gm.Map.Zoom(ev.delta.y * 0.04f);
                    ev.Use();
                    break;
            }
        }

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
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none) && !mapDragged)
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
                GUI.Label(new Rect(r.x + 40f, r.y + 236f, r.width - 80f, 40f), "<color=#AAAAAA>Enemy Lv." + e.level + " · win for diamonds and XP, then continue your journey</color>", UIStyles.Small);
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

            // ---- Left: the story panel, as stacked cards.
            var left = new Rect(safe.x + 32f - (1f - k) * 200f, safe.y + 138f, 720f, H - safe.y - 232f);
            DrawMissionStory(left, m, prog, d);

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
            string[] roles = { "LEADER", "VANGUARD", "SUPPORT" };
            for (int i = 0; i < d.team.Count && i < 3; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                Color ecol = ElementChart.ColorOf(def.element);
                GUI.Label(new Rect(rx, ry, 110f, 40f), "<color=#AAAAAA><size=16>" + roles[i] + "</size></color>", UIStyles.Sized(UIStyles.Body, 16));
                UIStyles.CircleTex(new Vector2(rx + 124f, ry + 20f), 13f, Color.Lerp(ecol, Color.black, 0.2f));
                GUI.DrawTexture(new Rect(rx + 114f, ry + 10f, 20f, 20f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(rx + 146f, ry, rw - 250f, 40f), def.displayName, UIStyles.Sized(UIStyles.Body, 21));
                GUI.Label(new Rect(rx, ry, rw, 40f), "<color=#BBBBBB>Lv. " + c.level + "</color>", UIStyles.Sized(UIStyles.Right, 20));
                ry += 40f;
            }
            ry += 6f;
            GUI.Label(new Rect(rx, ry, rw, 54f), "<color=#AAAAAA><size=17>Type advantage deals ×1.5. Water › Flame › Beast › Thunder › Water, Light ↔ Dark.</size></color>", UIStyles.Small);
            ry += 58f;
            float pw = (rw - 2f * 12f) / 3f, ph = pw * 1.25f;
            for (int i = 0; i < 3; i++)
            {
                var pr = new Rect(rx + i * (pw + 12f), ry, pw, ph);
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

        /// <summary>
        /// The mission's story panel: a header card with the main foe's portrait, three level tiles, enemy cards,
        /// the star objectives (with the diamonds they pay) and reward chips.
        /// </summary>
        void DrawMissionStory(Rect left, MissionDefinition m, MissionProgress prog, PlayerData d)
        {
            Round(Offset(left, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 18f);
            Round(left, new Color(0.05f, 0.06f, 0.1f, 0.9f), 18f);
            RoundFrame(left, new Color(1f, 1f, 1f, 0.08f), 2f, 18f);
            float x = left.x + 22f, w = left.width - 44f, y = left.y + 20f;
            bool bossMission = !string.IsNullOrEmpty(m.bossId);

            // Enemy list (bosses last).
            var foes = new List<string>();
            foreach (var wv in m.waves) foreach (var sp in wv.spawns) if (!foes.Contains(sp.enemyId)) foes.Add(sp.enemyId);
            foreach (var pb in m.preBosses) if (!foes.Contains(pb)) foes.Add(pb);
            if (bossMission && !foes.Contains(m.bossId)) foes.Add(m.bossId);
            var headFoe = GameDatabase.GetEnemy(bossMission ? m.bossId : (foes.Count > 0 ? foes[foes.Count - 1] : null));

            // Header card: portrait with a glow, type chip, breadcrumb, name, story line.
            var head = new Rect(x, y, w, 176f);
            Color accent = bossMission ? new Color(0.9f, 0.2f, 0.22f) : m.type == MissionType.Story ? new Color(0.3f, 0.55f, 1f) : new Color(0.3f, 0.8f, 0.55f);
            Round(head, Color.Lerp(new Color(0.08f, 0.09f, 0.14f), accent, 0.18f), 14f);
            var pr = new Rect(head.x + 14f, head.y + 14f, 148f, 148f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.5f);
            for (int i = 3; i >= 1; i--) UIStyles.CircleTex(pr.center, 70f + i * 7f + pulse * 3f, new Color(accent.r, accent.g, accent.b, 0.08f));
            Round(pr, new Color(0.1f, 0.1f, 0.15f), 74f);
            if (headFoe != null)
            {
                var art = ArtLibrary.Monster(headFoe);
                if (art != null) GUI.DrawTexture(pr, art, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, 74f);
            }
            RoundFrame(pr, accent, 3f, 74f);
            float tx = pr.xMax + 18f, tw = head.xMax - tx - 14f;
            string typeName = bossMission ? "BOSS" : m.type == MissionType.Story ? "STORY" : m.type.ToString().ToUpper();
            var chip = new Rect(tx, head.y + 16f, 92f, 30f);
            Round(chip, accent, 15f);
            GUI.Label(chip, "<b>" + typeName + "</b>", UIStyles.Sized(UIStyles.Center, 16));
            GUI.Label(new Rect(chip.xMax + 10f, head.y + 14f, tw - 104f, 34f), "<color=#AAAAAA>" + RegionName(m.regionId) + (m.chapter > 0 ? "  ›  Chapter " + m.chapter : "") + "</color>", UIStyles.Sized(UIStyles.Body, 19));
            UIStyles.Outlined(new Rect(tx, head.y + 50f, tw, 56f), m.name, UIStyles.Sized(UIStyles.H1, m.name.Length > 18 ? 36 : 44), Color.white, 1.5f);
            GUI.Label(new Rect(tx, head.y + 106f, tw, 64f), "<i><color=#C8C8D0>" + m.storyText + "</color></i>", UIStyles.Sized(UIStyles.Body, 19));
            y = head.yMax + 14f;

            // Level tiles.
            int avgLevel = 0;
            foreach (var id in d.team) { var c = d.GetCharacter(id); if (c != null) avgLevel += c.level; }
            avgLevel = d.team.Count > 0 ? avgLevel / d.team.Count : 1;
            bool ready = avgLevel >= m.recommendedLevel;
            string[] lvLabels = { "RECOMMENDED", "YOUR TEAM", "ENEMY" };
            int[] lvVals = { m.recommendedLevel, avgLevel, m.enemyLevel };
            Color[] lvCols = { Color.white, ready ? new Color(0.5f, 1f, 0.55f) : new Color(1f, 0.45f, 0.4f), new Color(1f, 0.75f, 0.45f) };
            float lw = (w - 20f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var t = new Rect(x + i * (lw + 10f), y, lw, 78f);
                Round(t, new Color(1f, 1f, 1f, 0.05f), 12f);
                if (i == 1) Round(new Rect(t.x, t.yMax - 5f, t.width, 5f), lvCols[1], 3f);
                GUI.Label(new Rect(t.x, t.y + 6f, t.width, 24f), "<color=#999999>" + lvLabels[i] + "</color>", UIStyles.Sized(UIStyles.Center, 15));
                UIStyles.Outlined(new Rect(t.x, t.y + 28f, t.width, 44f), "Lv. " + lvVals[i], UIStyles.Sized(UIStyles.Center, 30), lvCols[i], 1.5f);
            }
            y += 78f + 6f;
            GUI.Label(new Rect(x, y, w, 26f), ready ? "<color=#7CFF8A>Your team is ready.</color>" : "<color=#FF9C7A>Tough fight — level up or upgrade your slayers first.</color>", UIStyles.Sized(UIStyles.Small, 17));
            y += 30f;

            // Enemy cards.
            GUI.Label(new Rect(x, y, w, 32f), "<b>ENEMIES</b>", UIStyles.Sized(UIStyles.Body, 24));
            y += 34f;
            int shownFoes = Mathf.Min(foes.Count, 4);
            float ew = shownFoes > 0 ? (w - (shownFoes - 1) * 10f) / shownFoes : w;
            string tip = null;
            for (int i = 0; i < shownFoes; i++)
            {
                var e = GameDatabase.GetEnemy(foes[i]);
                if (e == null) continue;
                bool boss = e.archetype == EnemyArchetype.Boss;
                Color ec = ElementChart.ColorOf(e.element);
                var er = new Rect(x + i * (ew + 10f), y, ew, 128f);
                if (boss) for (int g = 2; g >= 1; g--) Round(Grow(er, g * 3f), new Color(1f, 0.2f, 0.2f, 0.08f + 0.05f * pulse), 12f + g * 3f);
                Round(er, boss ? new Color(0.22f, 0.06f, 0.08f) : new Color(0.1f, 0.11f, 0.16f), 12f);
                float ps = Mathf.Min(72f, ew - 20f);
                var ep = new Rect(er.center.x - ps * 0.5f, er.y + 8f, ps, ps);
                Round(ep, Color.Lerp(new Color(0.12f, 0.12f, 0.18f), ec, 0.25f), 10f);
                var art = ArtLibrary.Monster(e);
                if (art != null) GUI.DrawTexture(ep, art, ScaleMode.ScaleAndCrop, true);
                RoundFrame(ep, boss ? UIStyles.Crimson : ec, 2f, 10f);
                if (boss) UIStyles.Outlined(new Rect(ep.xMax - 26f, ep.y - 12f, 36f, 30f), "♛", UIStyles.Sized(UIStyles.Center, 24), new Color(1f, 0.8f, 0.2f), 1.5f);
                GUI.Label(new Rect(er.x + 4f, ep.yMax + 2f, er.width - 8f, 26f), e.displayName, UIStyles.Sized(UIStyles.CenterSmall, e.displayName.Length > 14 ? 14 : 17));
                UIStyles.Colored(new Rect(er.x + 4f, ep.yMax + 24f, er.width - 8f, 22f), ElementName(e.element) + " · " + ArchetypeName(e.archetype), UIStyles.Sized(UIStyles.CenterSmall, 14), boss ? new Color(1f, 0.5f, 0.45f) : ec);
                if (er.Contains(Event.current.mousePosition)) tip = e.weakness;
            }
            y += 132f;
            if (tip != null) GUI.Label(new Rect(x, y - 2f, w, 24f), "<color=#FFD36B>Tip: " + tip + "</color>", UIStyles.Sized(UIStyles.Small, 16));
            y += 22f;
            if (m.training) return;

            // Objectives: each is a star; stars pay diamonds (1★ 1, 2★ 2, 3★ 5).
            int have = 0;
            for (int i = 0; i < 3; i++) if (prog != null && (prog.objectivesMask & (1 << i)) != 0) have++;
            GUI.Label(new Rect(x, y, 200f, 32f), "<b>OBJECTIVES</b>", UIStyles.Sized(UIStyles.Body, 24));
            GUI.Label(new Rect(x + 200f, y + 2f, w - 240f, 30f), "<color=#9FD8FF>1★ = 1   2★ = 2   3★ = 5</color>", UIStyles.Sized(UIStyles.Right, 17));
            DiamondIcon(new Vector2(x + w - 16f, y + 17f), 24f);
            y += 34f;
            string[] labels = { "Defeat " + m.killObjective + " demons", "No slayer falls", "Clear within " + Mathf.RoundToInt(m.parTime) + "s" };
            for (int i = 0; i < 3; i++)
            {
                bool done = prog != null && (prog.objectivesMask & (1 << i)) != 0;
                var row = new Rect(x, y, w, 38f);
                Round(row, done ? new Color(1f, 0.8f, 0.2f, 0.1f) : new Color(1f, 1f, 1f, 0.04f), 10f);
                UIStyles.Outlined(new Rect(x + 6f, y, 36f, 38f), "★", UIStyles.Sized(UIStyles.Center, 26), done ? new Color(1f, 0.8f, 0.2f) : new Color(0.45f, 0.45f, 0.5f), 1.2f);
                GUI.Label(new Rect(x + 48f, y, w - 160f, 38f), labels[i], UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(x, y, w - 14f, 38f), done ? "<color=#7CFF8A>DONE ✓</color>" : "<color=#777777>—</color>", UIStyles.Sized(UIStyles.Right, 18));
                y += 42f;
            }
            if (have < 3) GUI.Label(new Rect(x, y, w, 24f), "<color=#9FD8FF>Next star pays +" + (RewardSystem.DiamondsForStars(have + 1) - RewardSystem.DiamondsForStars(have)) + " diamonds</color>", UIStyles.Sized(UIStyles.Small, 16));
            y += 28f;

            // Rewards as chips.
            GUI.Label(new Rect(x, y, 140f, 36f), "<b>REWARDS</b>", UIStyles.Sized(UIStyles.Body, 22));
            RewardChips(new Rect(x + 130f, y, w - 130f, 36f), m.rewards, false);
            y += 44f;
            if (prog == null || !prog.cleared)
            {
                var fc = new Rect(x, y, 122f, 36f);
                Round(fc, new Color(0.9f, 0.3f, 0.25f), 18f);
                GUI.Label(fc, "<b>1ST CLEAR</b>", UIStyles.Sized(UIStyles.Center, 16));
                RewardChips(new Rect(x + 130f, y, w - 130f, 36f), m.firstClearRewards, true);
            }
        }

        /// <summary>Icon + amount chips for a reward bundle (gold, XP, diamonds, items, new slayer).</summary>
        void RewardChips(Rect r, RewardBundle rw, bool first)
        {
            float x = r.x, h = r.height;
            var st = UIStyles.Sized(UIStyles.Body, 18);
            System.Action<int, string, Color> chip = (kind, text, col) =>
            {
                float tw = st.CalcSize(new GUIContent(text)).x;
                float cw = tw + h + 16f;
                if (x + cw > r.xMax) return;
                var c = new Rect(x, r.y, cw, h);
                Round(c, new Color(col.r, col.g, col.b, first ? 0.22f : 0.14f), h * 0.5f);
                Vector2 ic = new Vector2(c.x + h * 0.5f + 2f, c.center.y);
                if (kind == 0) CoinIcon(ic, h * 0.72f);
                else if (kind == 1) XpIcon(ic, h * 0.75f);
                else if (kind == 2) DiamondIcon(ic, h * 0.8f);
                else UIStyles.CircleTex(ic, h * 0.32f, col);
                GUI.Label(new Rect(c.x + h + 4f, c.y, tw + 8f, h), text, st);
                x += cw + 8f;
            };
            if (rw.coins > 0) chip(0, rw.coins.ToString("N0"), new Color(1f, 0.8f, 0.2f));
            int xp = rw.exp / 2 + rw.XpValue;
            if (xp > 0) chip(1, xp.ToString("N0"), new Color(0.65f, 0.45f, 1f));
            if (rw.crystals > 0) chip(2, rw.crystals.ToString(), new Color(0.35f, 0.7f, 1f));
            foreach (var e in rw.equipmentIds)
            {
                var def = GameDatabase.GetEquipment(e);
                if (def != null) chip(3, def.displayName, RarityInfo.Color(def.rarity));
            }
            if (!string.IsNullOrEmpty(rw.characterId))
            {
                var c = GameDatabase.GetCharacter(rw.characterId);
                if (c != null) chip(3, "New: " + c.displayName, new Color(1f, 0.6f, 0.45f));
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
                gm.GoTo(!string.IsNullOrEmpty(m.eventId) ? GameScreen.Events : GameScreen.WorldMap);
                if (Event.current.type == EventType.KeyDown) Event.current.Use();
            }
            UIStyles.Outlined(new Rect(br.xMax + 30f, safe.y + 18f, 400f, 52f), GameConfig.TitleLine1, UIStyles.Sized(UIStyles.Title, 38), new Color(0.9f, 0.12f, 0.12f), 2f);
            GUI.Label(new Rect(br.xMax + 32f, safe.y + 68f, 420f, 34f), "<b>S L A Y E R   R P G</b>", UIStyles.Sized(UIStyles.Body, 18));
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
            if (r.coins > 0) parts.Add("<color=#FFD36B>Gold +" + r.coins.ToString("N0") + "</color>");
            if (r.crystals > 0) parts.Add("<color=#7FD8FF>Diamonds +" + r.crystals + "</color>");
            if (r.XpValue > 0) parts.Add("<color=#C9A7FF>XP +" + r.XpValue.ToString("N0") + "</color>");
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
            UIStyles.Outlined(new Rect(panel.x, panel.y + 18f, panel.width, 120f), r.mission.pvpMode >= 0 ? (r.victory ? "VICTORY!" : r.failReason == "Draw" ? "DRAW" : "DEFEAT") : r.victory ? "MISSION COMPLETE" : "MISSION FAILED", UIStyles.Sized(UIStyles.Big, size), new Color(tc.r, tc.g, tc.b, k), 4f);
            if (r.mission.pvpMode >= 0)
            {
                string tl = r.failReason + (r.mission.pvpRanked ? "   ·   <color=#FFD36B>" + (gm.LastTrophyDelta >= 0 ? "+" : "") + gm.LastTrophyDelta + " trophies</color>  " + PvpSystem.RankName(gm.Data.pvpTrophies) : "   ·   Casual");
                if (r.victory) tl = PvpSystem.ModeNames[r.mission.pvpMode] + "   ·   " + tl;
                GUI.Label(new Rect(panel.x, panel.y + 130f, panel.width, 44f), tl, UIStyles.Center);
            }
            else
            GUI.Label(new Rect(panel.x, panel.y + 130f, panel.width, 44f), "Mission " + r.mission.id + " — " + r.mission.name + (r.victory ? "" : "   (" + r.failReason + ")"), UIStyles.Center);

            // Rating.
            float rk = Enter(0.9f, 0.3f);
            if (rk > 0f && r.victory)
            {
                string rating = Rating(r);
                Color rc = rating == "S" ? UIStyles.Gold : rating == "A" ? new Color(0.6f, 0.85f, 1f) : Color.white;
                int rs = Mathf.RoundToInt(Mathf.Lerp(200f, 110f, rk));
                UIStyles.Outlined(new Rect(panel.xMax - 200f, panel.y + 20f, 170f, 140f), rating, UIStyles.Sized(UIStyles.Big, rs), new Color(rc.r, rc.g, rc.b, rk), 5f);
                GUI.Label(new Rect(panel.xMax - 200f, panel.y + 150f, 170f, 30f), "RATING", UIStyles.Sized(UIStyles.Center, 20));
            }

            // Stars earned this run pop in one by one, with the diamonds they paid.
            if (r.victory)
            {
                float sc = panel.center.x;
                for (int i = 0; i < 3; i++)
                {
                    float sk = Enter(0.45f + i * 0.22f, 0.3f);
                    bool on = i < r.stars;
                    float sz = (i == 1 ? 104f : 84f) * (on ? Mathf.Lerp(1.8f, 1f, sk) : 1f);
                    var sr = new Rect(sc + (i - 1) * 118f - sz * 0.6f, panel.y + 176f + (i == 1 ? 0f : 16f) - sz * 0.1f, sz * 1.2f, sz * 1.2f);
                    Color scol = on ? new Color(1f, 0.82f, 0.22f, sk) : new Color(0.3f, 0.3f, 0.36f, Mathf.Max(0.5f, sk));
                    if (on && sk > 0.99f) UIStyles.CircleTex(sr.center, sz * 0.62f, new Color(1f, 0.8f, 0.2f, 0.12f + 0.06f * Mathf.Sin(Time.unscaledTime * 4f + i)));
                    UIStyles.Outlined(sr, "★", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(sz)), scol, 3f);
                }
                if (Enter(1.2f) > 0f)
                {
                    string dl = r.starDiamonds > 0 ? "+" + r.starDiamonds + (r.starDiamonds == 1 ? " DIAMOND" : " DIAMONDS") : r.stars > 0 ? "Best stars already claimed" : "Clear objectives for diamonds";
                    var dr = new Rect(sc - 200f, panel.y + 296f, 400f, 40f);
                    if (r.starDiamonds > 0) DiamondIcon(new Vector2(dr.x + 70f, dr.center.y), 34f);
                    UIStyles.Outlined(dr, dl, UIStyles.Sized(UIStyles.Center, r.starDiamonds > 0 ? 26 : 20), r.starDiamonds > 0 ? new Color(0.55f, 0.85f, 1f) : new Color(0.7f, 0.7f, 0.75f), 2f);
                }
            }

            float x = panel.x + 60f, y = panel.y + 180f;
            int mins = Mathf.FloorToInt(r.time / 60f), secs = Mathf.FloorToInt(r.time % 60f);
            string[] stats = { "Time  " + mins + ":" + secs.ToString("00"), "Demons slain  " + r.kills, "Max combo  " + r.maxCombo, "Total damage  " + Mathf.RoundToInt(r.totalDamage).ToString("N0") };
            for (int i = 0; i < stats.Length; i++)
            {
                float sk = Enter(0.3f + i * 0.1f, 0.25f);
                if (sk <= 0f) continue;
                GUI.Label(new Rect(x - (1f - sk) * 60f, y, 560f, 40f), stats[i], UIStyles.Sized(UIStyles.H2, 28));
                y += 42f;
            }
            y += 10f;
            for (int i = 0; i < 3; i++)
            {
                if (Enter(0.7f + i * 0.12f) <= 0f) continue;
                GUI.Label(new Rect(x, y, 600f, 38f), (r.objectives[i] ? "<color=#FFD36B>★</color> " : "<color=#888888>☆</color> ") + r.objectiveLabels[i], UIStyles.Sized(UIStyles.Body, 22));
                y += 38f;
            }

            // Rewards count up (right column, under the rating).
            float rx = panel.xMax - 560f, ry = panel.y + 180f;
            float ck = Enter(1f, 1.2f);
            GUI.Label(new Rect(rx, ry, 520f, 44f), "REWARDS" + (r.firstClear ? "  <color=#FF9C7A><size=20>FIRST CLEAR</size></color>" : ""), UIStyles.Sized(UIStyles.H2, 30));
            ry += 50f;
            var g = r.granted;
            CoinIcon(new Vector2(rx + 18f, ry + 20f), 32f);
            GUI.Label(new Rect(rx + 44f, ry, 480f, 40f), "Gold  <color=#FFD36B>+" + Mathf.RoundToInt(g.coins * ck).ToString("N0") + "</color>" + (r.goldCollected > 0 ? "  <color=#AAAAAA><size=18>(" + r.goldCollected.ToString("N0") + " picked up)</size></color>" : ""), UIStyles.Body); ry += 44f;
            DiamondIcon(new Vector2(rx + 18f, ry + 20f), 32f);
            GUI.Label(new Rect(rx + 44f, ry, 480f, 40f), "Diamonds  <color=#7FD8FF>+" + Mathf.RoundToInt(g.crystals * ck) + "</color>" + (r.diamondsCollected > 0 ? "  <color=#AAAAAA><size=18>(" + r.diamondsCollected + " picked up)</size></color>" : ""), UIStyles.Body); ry += 44f;
            XpIcon(new Vector2(rx + 18f, ry + 20f), 32f);
            GUI.Label(new Rect(rx + 44f, ry, 480f, 40f), "XP  <color=#C9A7FF>+" + Mathf.RoundToInt((g.exp / 2 + g.XpValue) * ck).ToString("N0") + "</color>", UIStyles.Body); ry += 44f;
            if (Enter(1.8f) > 0f)
                foreach (var e in r.droppedEquipmentNames)
                {
                    GUI.Label(new Rect(rx, ry, 520f, 36f), "<color=#C9A7FF>ITEM</color>  " + e, UIStyles.Sized(UIStyles.Body, 22));
                    ry += 36f;
                }
            if (!string.IsNullOrEmpty(r.unlockedCharacterName) && Enter(2.2f) > 0f)
                UIStyles.Outlined(new Rect(rx, ry + 4f, 540f, 40f), "NEW ALLY: " + r.unlockedCharacterName, UIStyles.Sized(UIStyles.H2, 24), UIStyles.Gold);

            // The team: each slayer's face with their EXP bar filling up, rolling over on every level gained.
            int n = r.members.Count;
            if (n > 0)
            {
                float cw = 500f, cg = 24f, ch = 200f;
                float cx0 = panel.center.x - (n * cw + (n - 1) * cg) * 0.5f, cy = panel.yMax - 124f - ch - 28f;
                for (int i = 0; i < n; i++)
                    ResultMember(new Rect(cx0 + i * (cw + cg), cy, cw, ch), r.members[i], 1.3f + i * 0.15f);
            }

            // Next steps.
            float by = panel.yMax - 124f, bw = 370f, gap = 22f;
            float bx = panel.x + (panel.width - (bw * 4f + gap * 3f)) * 0.5f;
            var next = NextMission(r.mission);
            if (r.mission.pvpMode >= 0)
            {
                // Arena: queue again straight away, or head home.
                if (Enter(1.2f) > 0f && FlatBtn(new Rect(bx, by, bw * 2f + gap, 100f), "PLAY AGAIN ▶", TileRed, true, 30))
                {
                    int mode = r.mission.pvpMode;
                    bool ranked = r.mission.pvpRanked;
                    gm.GoTo(GameScreen.MainMenu);
                    OpenArena();
                    arenaMode = mode;
                    arenaRanked = ranked;
                    arenaStage = 1;
                    arenaStageAt = Time.unscaledTime;
                    arenaSearchFor = Random.Range(2.2f, 4f);
                }
                if (Enter(1.3f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 2f, by, bw, 100f), "ARENA", new Color(0.14f, 0.17f, 0.28f), true, 30)) { gm.GoTo(GameScreen.MainMenu); OpenArena(); }
                if (Enter(1.4f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 3f, by, bw, 100f), "HOME", new Color(0.14f, 0.17f, 0.28f), true, 30)) gm.GoTo(GameScreen.MainMenu);
                return;
            }
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
            bool evMission = !string.IsNullOrEmpty(r.mission.eventId);
            if ((Enter(1.4f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 2f, by, bw, 100f), evMission ? "BACK TO EVENT" : "RETURN TO MAP", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.GoTo(evMission ? GameScreen.Events : GameScreen.WorldMap);
            if ((Enter(1.5f) > 0f && FlatBtn(new Rect(bx + (bw + gap) * 3f, by, bw, 100f), "CHARACTERS", new Color(0.14f, 0.17f, 0.28f), true, 30))) gm.GoTo(GameScreen.Characters);
        }

        /// <summary>One team member on the results screen: face with aura, level, and an EXP bar that animates from
        /// where they started to where they ended, rolling over (with a LEVEL UP! flash) on each level gained.</summary>
        void ResultMember(Rect r, MemberGain m, float delay)
        {
            var def = GameDatabase.GetCharacter(m.id);
            var c = gm.Data.GetCharacter(m.id);
            if (def == null || c == null) return;
            float ek = Enter(delay, 0.3f);
            if (ek <= 0f) return;
            r.y += (1f - ek) * 40f;
            Color ec = ElementChart.ColorOf(def.element);
            Color rc = RarityInfo.Color(c.stars);
            Round(Offset(r, 0f, 5f), new Color(0f, 0f, 0f, 0.4f), 16f);
            Round(r, Color.Lerp(new Color(0.08f, 0.09f, 0.14f), rc, 0.18f), 16f);
            if (m.maxed) RoundFrame(r, new Color(1f, 0.2f, 0.15f, 0.6f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f)), 3f, 16f);

            // Progress through the gained levels over ~1.6 s.
            float t = Enter(delay + 0.4f, 1.6f);
            float span = (m.toLevel - m.fromLevel) + m.toFill - m.fromFill;
            float pos = m.fromFill + span * t;
            int lv = m.fromLevel + Mathf.FloorToInt(pos);
            float fill = pos - Mathf.Floor(pos);
            if (lv >= m.toLevel) { lv = m.toLevel; fill = Mathf.Clamp01(pos - (m.toLevel - m.fromLevel)); }
            bool leveled = lv > m.fromLevel;
            bool atMax = m.maxed && t >= 1f;

            var face = new Rect(r.x + 14f, r.y + 14f, 150f, 150f);
            Aura(face.center, 78f, ec, atMax);
            Round(face, new Color(0f, 0f, 0f, 0.25f), 75f);
            var tex = ArtLibrary.Character(def);
            if (tex != null) GUI.DrawTexture(face, tex, ScaleMode.ScaleAndCrop, true);

            float tx = r.x + 178f, tw = r.width - 192f;
            GUI.Label(new Rect(tx, r.y + 14f, tw, 36f), def.displayName, UIStyles.Sized(UIStyles.H2, 26));
            UIStyles.Outlined(new Rect(tx, r.y + 48f, tw, 22f), RarityInfo.Name(c.stars), UIStyles.Sized(UIStyles.Small, 15), rc, 1.2f);
            string lvText = "Lv. " + lv + (atMax ? "  MAX" : "");
            Color lvCol = atMax ? new Color(1f, 0.28f + 0.15f * Mathf.Sin(Time.unscaledTime * 5f), 0.2f) : Color.white;
            UIStyles.Outlined(new Rect(tx, r.y + 74f, tw, 40f), lvText, UIStyles.Sized(UIStyles.Body, 30), lvCol, 2f);
            if (leveled)
            {
                float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f) * 0.06f;
                UIStyles.Outlined(new Rect(tx + 120f, r.y + 76f, tw - 120f, 36f), "LEVEL UP!", UIStyles.Sized(UIStyles.Right, Mathf.RoundToInt(24 * pulse)), UIStyles.Good, 2f);
            }
            var bar = new Rect(tx, r.y + 124f, tw, 20f);
            Round(bar, new Color(0f, 0f, 0f, 0.5f), 10f);
            if (fill > 0.01f) Round(new Rect(bar.x + 2f, bar.y + 2f, (bar.width - 4f) * Mathf.Clamp01(fill), bar.height - 4f), atMax ? new Color(0.95f, 0.22f, 0.18f) : new Color(0.62f, 0.42f, 1f), 8f);
            GUI.Label(new Rect(tx, r.y + 150f, tw, 30f), atMax ? "<color=#FF7A6A>Max level — ascend to go higher</color>" : "<color=#C9A7FF>+" + Mathf.RoundToInt(m.expGained * t).ToString("N0") + " EXP</color>", UIStyles.Sized(UIStyles.Small, 18));
        }

        /// <summary>The mission this one unlocks on the main path (falls back to the next open story mission).</summary>
        MissionDefinition NextMission(MissionDefinition m)
        {
            if (!string.IsNullOrEmpty(m.eventId))
            {
                var ev = GameDatabase.GetEvent(m.eventId);
                int i = ev != null ? ev.quests.IndexOf(m) : -1;
                return ev != null && i >= 0 && i + 1 < ev.quests.Count ? ev.quests[i + 1] : null;
            }
            foreach (var x in GameDatabase.AllMissions())
                if (x.requiresMissionId == m.id && (x.type == MissionType.Story || x.type == MissionType.Boss)) return x;
            return gm.NextStoryMission();
        }
    }
}
