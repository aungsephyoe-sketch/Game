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
                UIStyles.Rect(new Rect(0f, 0f, 160f * (6 - i) + safe.x, H), new Color(0.02f, 0.02f, 0.06f, 0.06f));

            float x = safe.x + 56f;
            float k = Enter(0f, 0.6f);
            UIStyles.Outlined(new Rect(x, 36f - (1f - k) * 80f, 900f, 60f), GameConfig.TitleLine1, UIStyles.Sized(UIStyles.Title, 40), UIStyles.Crimson, 3f);
            UIStyles.Outlined(new Rect(x, 82f - (1f - k) * 80f, 1000f, 100f), GameConfig.TitleLine2, UIStyles.Sized(UIStyles.Title, 80), Color.white, 4f);

            var next = gm.NextStoryMission();
            var chapter = next != null ? GameDatabase.ChapterOf(next) : null;
            string leaderId = d.team.Count > 0 ? d.team[0] : null;
            var leader = leaderId != null ? GameDatabase.GetCharacter(leaderId) : null;

            // PLAY and STORY on the left, the coloured tiles beside them.
            float top = 212f;
            if (Tile(new Rect(x, top, 440f, 300f), "PLAY", "▶", TileRed, 0.1f, leader != null ? ArtLibrary.CharacterFull(leader) : null,
                chapter != null ? "Chapter " + chapter.number + " · " + chapter.title : "Free play", 0, true))
                gm.GoTo(GameScreen.WorldMap);
            string storySub = next != null ? "Next: " + next.id + "  " + next.name : "The story is complete";
            if (Tile(new Rect(x, top + 316f, 440f, 140f), "STORY", "☾", TileNavy, 0.18f, null, storySub))
            {
                if (next != null) { gm.SelectedMission = next; gm.GoTo(GameScreen.MissionDetail); }
                else gm.GoTo(GameScreen.Story);
            }
            if (Tile(new Rect(x, top + 472f, 440f, 110f), "JOURNAL", "✎", new Color(0.3f, 0.25f, 0.2f), 0.24f)) OpenMenu("JOURNAL");

            string[] labels = { "SUMMON", "CHARACTERS", "TEAM", "EQUIPMENT", "MISSIONS", "SHOP", "SETTINGS" };
            string[] icons = { "✦", "☺", "⚑", "⚔", "✔", "♦", "⚙" };
            Color[] colors = { TilePurple, TileBlue, TileGreen, TileOrange, TileMaroon, TileTeal, TileGrey };
            float gx = x + 456f, tw = 210f, th = 134f, gap = 12f;
            for (int i = 0; i < labels.Length; i++)
            {
                var r = new Rect(gx + (i % 2) * (tw + gap), top + (i / 2) * (th + gap), tw, th);
                if (i == labels.Length - 1) r.width = tw * 2f + gap;
                Texture art = labels[i] == "CHARACTERS" && leader != null ? ArtLibrary.Character(leader) : null;
                if (Tile(r, labels[i], icons[i], colors[i], 0.28f + i * 0.05f, art, null, MenuBadge(labels[i]))) OpenMenu(labels[i]);
            }

            // Team power panel, bottom right: the four slayers' faces and the total.
            float tk = Enter(0.5f, 0.5f);
            var panel = new Rect(safe.xMax - 560f + (1f - tk) * 300f, H - 210f, 530f, 180f);
            Round(Offset(panel, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(panel, new Color(0.06f, 0.06f, 0.12f, 0.85f), 16f);
            GUI.Label(new Rect(panel.x + 22f, panel.y + 10f, 300f, 40f), "TEAM POWER", UIStyles.Sized(UIStyles.H2, 26));
            GUI.Label(new Rect(panel.x + 200f, panel.y + 8f, 310f, 44f), "<color=#FFD36B>" + CharacterSystem.TeamPower(d).ToString("N0") + "</color>", UIStyles.Sized(UIStyles.Right, 34));
            for (int i = 0; i < 4; i++)
            {
                var r = new Rect(panel.x + 22f + i * 124f, panel.y + 58f, 110f, 110f);
                if (i < d.team.Count)
                {
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    var c = d.GetCharacter(d.team[i]);
                    if (def == null || c == null) continue;
                    PortraitCard(r, def, false, "Lv." + c.level);
                    if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { teamReturn = GameScreen.MainMenu; gm.GoTo(GameScreen.Team); }
                }
                else
                {
                    Round(r, new Color(1f, 1f, 1f, 0.06f), 12f);
                    GUI.Label(r, "+", UIStyles.Sized(UIStyles.Center, 44));
                }
            }
            Currencies(new Rect(safe.xMax - 980f, safe.y + 28f, 960f, 52f));

            DrawNpcBubbles();
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
            if (inArea) gm.Map.EnterArea(mapSelected, list.Count, current);
            else gm.Map.ExitArea();
            if (areaPick < 0 || areaPick >= list.Count || areaRegion != mapSelected) { areaPick = current; areaRegion = mapSelected; }
            if (inArea) DrawAreaNodes(list, current);

            float pk = Enter(0.1f, 0.4f);
            var panel = new Rect(safe.xMax - 640f + (1f - pk) * 300f, safe.y + 130f, 620f, H - safe.y - 150f);
            Round(Offset(panel, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 18f);
            Round(panel, new Color(0.06f, 0.06f, 0.12f, 0.9f), 18f);
            Round(new Rect(panel.x, panel.y, panel.width, 8f), region2.mapColor, 4f);
            GUI.Label(new Rect(panel.x + 26f, panel.y + 16f, panel.width - 52f, 56f), region2.name, UIStyles.H1);
            GUI.Label(new Rect(panel.x + 26f, panel.y + 72f, panel.width - 52f, 36f), region2.subtitle, UIStyles.Small);
            float y = panel.y + 116f;
            if (mapSelected != d.currentRegion && d.IsRegionUnlocked(region2))
            {
                if (FlatBtn(new Rect(panel.x + 26f, y, panel.width - 52f, 66f), "TRAVEL HERE", TileBlue)) { mapOverview = false; gm.TravelTo(mapSelected); }
                y += 80f;
            }
            else if (mapSelected == d.currentRegion)
            {
                if (FlatBtn(new Rect(panel.x + 26f, y, panel.width - 52f, 56f), mapOverview ? "ENTER AREA" : "◀ WORLD MAP", new Color(0.25f, 0.25f, 0.34f), true, 24)) mapOverview = !mapOverview;
                y += 70f;
            }

            var view = new Rect(panel.x + 16f, y, panel.width - 32f, panel.yMax - y - 110f);
            float rowH = 82f;
            var content = new Rect(0f, 0f, view.width - 24f, list.Count * (rowH + 8f));
            mapScroll = GUI.BeginScrollView(view, mapScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                bool unlocked = d.IsMissionUnlocked(m);
                bool cleared = d.IsMissionCleared(m.id);
                bool boss = m.type == MissionType.Boss;
                var r = new Rect(0f, i * (rowH + 8f), content.width, rowH);
                bool picked = i == areaPick;
                Round(r, boss ? new Color(0.35f, 0.08f, 0.12f, 0.95f) : new Color(0.14f, 0.13f, 0.22f, 0.95f), 12f);
                if (picked) RoundFrame(r, UIStyles.Gold, 3f, 12f);
                Color nc = !unlocked ? new Color(0.35f, 0.35f, 0.4f) : cleared ? new Color(0.95f, 0.72f, 0.2f) : TileRed;
                UIStyles.CircleTex(new Vector2(r.x + 40f, r.center.y), 26f, nc);
                GUI.Label(new Rect(r.x + 14f, r.y + 14f, 52f, 52f), boss ? "☠" : (i + 1).ToString(), UIStyles.Sized(UIStyles.Center, 26));
                string title = (boss ? "<color=#FF7A7A>BOSS</color>  " : "") + m.name;
                GUI.Label(new Rect(r.x + 80f, r.y + 8f, r.width - 250f, 38f), title, UIStyles.Sized(UIStyles.H2, 24));
                GUI.Label(new Rect(r.x + 80f, r.y + 44f, r.width - 250f, 30f), "<color=#AAAAAA>" + m.id + " · Rec. Lv." + m.recommendedLevel + "</color>", UIStyles.Sized(UIStyles.Small, 18));
                if (unlocked) GUI.Label(new Rect(r.xMax - 170f, r.y, 160f, rowH), "<color=#FFD36B>" + StarRow(d, m) + "</color>", UIStyles.Sized(UIStyles.Right, 28));
                else GUI.Label(new Rect(r.xMax - 170f, r.y, 160f, rowH), "<color=#999999>LOCKED</color>", UIStyles.Sized(UIStyles.Right, 20));
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
            if (chosen != null && d.IsMissionUnlocked(chosen))
            {
                if (FlatBtn(new Rect(panel.x + 26f, panel.yMax - 94f, panel.width - 52f, 76f), "CONTINUE ▶  " + chosen.id, TileRed, true, 32))
                {
                    gm.SelectedMission = chosen;
                    gm.GoTo(GameScreen.MissionDetail);
                }
            }
            else
            {
                var next = gm.NextStoryMission();
                if (next != null && FlatBtn(new Rect(panel.x + 26f, panel.yMax - 94f, panel.width - 52f, 76f), "CONTINUE STORY ▶ " + next.id, TileRed, true, 30))
                {
                    gm.SelectedMission = next;
                    mapSelected = next.regionId;
                    gm.GoTo(GameScreen.MissionDetail);
                }
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

        /// <summary>Numbered stops and the dotted trail between them, drawn over the area's 3D pads.</summary>
        void DrawAreaNodes(List<MissionDefinition> list, int current)
        {
            var cam = Camera.main;
            var pts = gm.Map.AreaPoints;
            if (cam == null || pts.Count != list.Count) return;
            float s = HudLayout.Scale;
            var screen = new List<Vector2>();
            foreach (var p in pts)
            {
                Vector3 sp = cam.WorldToScreenPoint(p);
                screen.Add(sp.z > 0f ? new Vector2(sp.x / s, (Screen.height - sp.y) / s) : new Vector2(-9999f, -9999f));
            }
            var d = gm.Data;
            // Dotted trail.
            for (int i = 0; i < screen.Count - 1; i++)
            {
                bool open = d.IsMissionUnlocked(list[i + 1]);
                float len = Vector2.Distance(screen[i], screen[i + 1]);
                int dots = Mathf.Max(2, Mathf.FloorToInt(len / 22f));
                for (int k = 1; k < dots; k++)
                    UIStyles.CircleTex(Vector2.Lerp(screen[i], screen[i + 1], (float)k / dots), 5f, open ? new Color(1f, 1f, 1f, 0.9f) : new Color(0.3f, 0.3f, 0.35f, 0.7f));
            }
            for (int i = 0; i < screen.Count; i++)
            {
                var m = list[i];
                bool unlocked = d.IsMissionUnlocked(m);
                bool cleared = d.IsMissionCleared(m.id);
                bool boss = m.type == MissionType.Boss;
                float rad = boss ? 40f : 30f;
                // The leader stands on the current stop, so its marker sits just in front of their feet.
                if (i == current) rad = rad * 0.75f + Mathf.Sin(Time.unscaledTime * 4f) * 3f;
                Vector2 c = screen[i] + new Vector2(0f, i == current ? rad + 12f : 0f);
                Color col = !unlocked ? new Color(0.35f, 0.35f, 0.4f) : boss ? UIStyles.Crimson : cleared ? new Color(0.95f, 0.72f, 0.2f) : TileRed;
                UIStyles.CircleTex(c + new Vector2(0f, 4f), rad + 4f, new Color(0f, 0f, 0f, 0.4f));
                UIStyles.CircleTex(c, rad + 4f, i == areaPick ? UIStyles.Gold : Color.white);
                UIStyles.CircleTex(c, rad, col);
                string label = !unlocked ? "✕" : boss ? "☠" : (i + 1).ToString();
                GUI.Label(new Rect(c.x - rad, c.y - rad, rad * 2f, rad * 2f), label, UIStyles.Sized(UIStyles.Center, boss ? 34 : 28));
                if (unlocked) UIStyles.Outlined(new Rect(c.x - 60f, c.y + rad + 2f, 120f, 30f), StarRow(d, m), UIStyles.Sized(UIStyles.Center, 20), new Color(1f, 0.83f, 0.3f), 2f);
                if (boss) UIStyles.Outlined(new Rect(c.x - 60f, c.y - rad - 34f, 120f, 30f), "BOSS", UIStyles.Sized(UIStyles.Center, 22), UIStyles.Bad, 2f);
                var hit = new Rect(c.x - rad, c.y - rad, rad * 2f, rad * 2f);
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
                {
                    gm.Audio.Play("click", 0.5f);
                    if (unlocked) areaPick = i;
                    else Toast("Locked — clear " + m.requiresMissionId + " first.");
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

        void DrawMissionDetail()
        {
            var m = gm.SelectedMission;
            if (m == null) { gm.GoTo(GameScreen.WorldMap); return; }
            var d = gm.Data;
            var prog = d.GetMission(m.id);
            // The region itself stays visible in the middle of the page (the 3D area map behind the UI).
            TopBar(m.type == MissionType.Training ? "TRAINING" : "MISSION " + m.MissionLabel, GameScreen.WorldMap);
            bool unlocked = d.IsMissionUnlocked(m);

            float k = Enter(0f, 0.4f);
            var left = new Rect(safe.x + 24f - (1f - k) * 200f, safe.y + 126f, 760f, H - safe.y - 146f);
            Round(Offset(left, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 18f);
            Round(left, new Color(0.06f, 0.06f, 0.12f, 0.9f), 18f);
            Round(new Rect(left.x, left.y, left.width, 8f), m.type == MissionType.Boss ? UIStyles.Crimson : UIStyles.Gold, 4f);
            float x = left.x + 28f, w = left.width - 56f, y = left.y + 18f;
            // Breadcrumb.
            GUI.Label(new Rect(x, y, w, 40f), "<color=#AAAAAA>WORLD MAP  ›  " + RegionName(m.regionId).ToUpper() + (m.chapter > 0 ? "  ›  CHAPTER " + m.chapter : "") + "  ›  </color>" + TypeTag(m.type), UIStyles.Sized(UIStyles.Small, 19));
            y += 40f;
            UIStyles.Outlined(new Rect(x, y, w, 64f), m.name, UIStyles.Sized(UIStyles.H1, 44), Color.white, 2f);
            y += 64f;
            if (!string.IsNullOrEmpty(m.questGiver))
            {
                GUI.Label(new Rect(x, y, w, 34f), "<color=#7FD8FF>Requested by " + m.questGiver + "</color>", UIStyles.Small);
                y += 36f;
            }
            GUI.Label(new Rect(x, y, w, 76f), "<i>" + m.storyText + "</i>", UIStyles.Sized(UIStyles.Body, 22));
            y += 78f;

            int avgLevel = 0;
            foreach (var id in d.team) { var c = d.GetCharacter(id); if (c != null) avgLevel += c.level; }
            avgLevel = d.team.Count > 0 ? avgLevel / d.team.Count : 1;
            // Three level chips: recommended, yours, enemy.
            string[] lvNames = { "RECOMMENDED", "YOUR LEVEL", "ENEMY LEVEL" };
            int[] lvVals = { m.recommendedLevel, avgLevel, m.enemyLevel };
            Color[] lvCols = { TileBlue, avgLevel >= m.recommendedLevel ? TileGreen : TileRed, TileMaroon };
            for (int i = 0; i < 3; i++)
            {
                var lr = new Rect(x + i * (w / 3f), y, w / 3f - 10f, 58f);
                Round(lr, new Color(1f, 1f, 1f, 0.06f), 10f);
                Round(new Rect(lr.x, lr.y, 6f, lr.height), lvCols[i], 3f);
                GUI.Label(new Rect(lr.x + 14f, lr.y + 2f, lr.width - 16f, 24f), "<color=#AAAAAA>" + lvNames[i] + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                GUI.Label(new Rect(lr.x + 14f, lr.y + 22f, lr.width - 16f, 34f), "Lv. " + lvVals[i], UIStyles.Sized(UIStyles.Body, 26));
            }
            y += 66f;
            if (m.allies > 0 || m.sealPuzzle)
            {
                GUI.Label(new Rect(x, y, w, 30f), (m.allies > 0 ? "<color=#7FD8FF>" + m.allies + " allied soldiers</color>   " : "") + (m.sealPuzzle ? "<color=#7FD8FF>Seal puzzle</color>" : ""), UIStyles.Sized(UIStyles.Small, 19));
                y += 30f;
            }

            // The road ahead.
            if (!m.training)
            {
                GUI.Label(new Rect(x, y, w, 44f), "<color=#FFD36B>ROUTE</color>  <size=19>" + RouteFor(m) + "</size>", UIStyles.Sized(UIStyles.Small, 20));
                y += 46f;
            }

            // Bestiary: the demons on this road (concept art + how to beat them).
            GUI.Label(new Rect(x, y, w, 36f), "ENEMIES", UIStyles.Sized(UIStyles.H2, 28));
            y += 38f;
            y = DrawBestiary(m, new Rect(x, y, w, 84f)) + 8f;

            // Boss info.
            var bosses = new List<string>(m.preBosses);
            if (!string.IsNullOrEmpty(m.bossId)) bosses.Add(m.bossId);
            foreach (var bid in bosses)
            {
                var b = GameDatabase.GetEnemy(bid);
                if (b == null) continue;
                var br = new Rect(x, y, w, 72f);
                Round(br, new Color(0.35f, 0.04f, 0.06f, 0.75f), 10f);
                var art = ArtLibrary.Monster(b);
                float tx = br.x + 12f;
                if (art != null) { ArtLibrary.DrawCover(new Rect(br.x + 4f, br.y + 4f, 64f, 64f), art); tx = br.x + 80f; }
                GUI.Label(new Rect(tx, br.y + 2f, br.xMax - tx - 8f, 34f), "<color=#FF6060>☠ BOSS</color>  " + b.displayName + " — <i>" + b.bossTitle + "</i>  " + ElementTag(b.element), UIStyles.Sized(UIStyles.Body, 23));
                GUI.Label(new Rect(tx, br.y + 36f, br.xMax - tx - 8f, 34f), "<color=#FFB0A0>Weakness:</color> <color=#CCCCCC>" + b.weakness + "</color>", UIStyles.Sized(UIStyles.Small, 18));
                y += 78f;
            }

            // Objectives + rewards.
            if (!m.training)
            {
                GUI.Label(new Rect(x, y, w, 36f), "OBJECTIVES", UIStyles.Sized(UIStyles.H2, 28));
                y += 36f;
                string[] labels = { "Defeat " + m.killObjective + " demons", "No slayer falls", "Clear within " + Mathf.RoundToInt(m.parTime) + "s" };
                for (int i = 0; i < 3; i++)
                {
                    bool done = prog != null && (prog.objectivesMask & (1 << i)) != 0;
                    GUI.Label(new Rect(x + 10f, y, w, 32f), (done ? "<color=#7CFF8A>★</color> " : "☆ ") + labels[i] + (done ? "" : "  <color=#7FD8FF>+" + RewardSystem.CrystalsPerNewObjective + " ✦</color>"), UIStyles.Sized(UIStyles.Body, 23));
                    y += 32f;
                }
                y += 8f;
                GUI.Label(new Rect(x, y, 170f, 44f), "REWARDS", UIStyles.Sized(UIStyles.H2, 22));
                RewardIcons(new Rect(x + 170f, y, w - 170f, 44f), m.rewards);
                y += 52f;
                if (prog == null || !prog.cleared)
                {
                    GUI.Label(new Rect(x, y, 170f, 44f), "<color=#FF9C7A>FIRST CLEAR</color>", UIStyles.Sized(UIStyles.H2, 22));
                    RewardIcons(new Rect(x + 170f, y, w - 170f, 44f), m.firstClearRewards);
                }
            }

            // Team + PLAY.
            var right = new Rect(safe.xMax - 560f + (1f - k) * 200f, safe.y + 126f, 536f, H - safe.y - 146f);
            Round(Offset(right, 0f, 6f), new Color(0f, 0f, 0f, 0.35f), 18f);
            Round(right, new Color(0.06f, 0.06f, 0.12f, 0.9f), 18f);
            float ry = right.y + 16f;
            GUI.Label(new Rect(right.x + 24f, ry, 260f, 44f), "YOUR TEAM", UIStyles.Sized(UIStyles.H2, 30));
            GUI.Label(new Rect(right.x + 250f, ry, right.width - 274f, 44f), "<color=#FFD36B>" + CharacterSystem.TeamPower(d).ToString("N0") + "</color>", UIStyles.Sized(UIStyles.Right, 30));
            ry += 56f;
            float pw = (right.width - 48f - 3f * 10f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                var pr = new Rect(right.x + 24f + i * (pw + 10f), ry, pw, pw * 1.5f);
                if (i < d.team.Count)
                {
                    var c = d.GetCharacter(d.team[i]);
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    if (c == null || def == null) continue;
                    PortraitCard(pr, def, true, null, i == 0);
                    Round(new Rect(pr.x, pr.yMax - 30f, pr.width, 30f), new Color(0f, 0f, 0f, 0.6f), 8f);
                    GUI.Label(new Rect(pr.x, pr.yMax - 30f, pr.width, 30f), "Lv." + c.level, UIStyles.Sized(UIStyles.Center, 18));
                    GUI.Label(new Rect(pr.x - 4f, pr.yMax + 2f, pr.width + 8f, 28f), def.displayName, UIStyles.Sized(UIStyles.Center, 18));
                }
                else
                {
                    Round(pr, new Color(1f, 1f, 1f, 0.06f), 12f);
                    GUI.Label(pr, "+", UIStyles.Sized(UIStyles.Center, 40));
                }
            }
            ry += pw * 1.5f + 40f;
            GUI.Label(new Rect(right.x + 24f, ry, right.width - 48f, 70f), "<size=19><color=#AAAAAA>Element advantage deals ×1.5.\n" +
                "Water ▶ Flame ▶ Beast ▶ Thunder ▶ Water;  Light ◀▶ Dark.</color></size>", UIStyles.Small);
            if (FlatBtn(new Rect(right.x + 24f, right.yMax - 236f, right.width - 48f, 76f), "CHANGE TEAM", TilePurple, true, 30)) { teamReturn = GameScreen.MissionDetail; gm.GoTo(GameScreen.Team); }
            if (!unlocked)
                GUI.Label(new Rect(right.x + 24f, right.yMax - 150f, right.width - 48f, 130f), "<color=#FF7070>Clear " + m.requiresMissionId + " first.</color>", UIStyles.Sized(UIStyles.Center, 32));
            else
            {
                bool travel = m.regionId != d.currentRegion && System.Array.IndexOf(MapStage.RouteOrder, m.regionId) >= 0 && m.type != MissionType.Training && m.type != MissionType.Event;
                var pr = new Rect(right.x + 24f, right.yMax - 146f, right.width - 48f, 124f);
                float pulse = (Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f);
                RoundFrame(Grow(pr, 4f + pulse * 3f), new Color(1f, 0.85f, 0.4f, 0.4f + 0.4f * pulse), 3f, 16f);
                if (FlatBtn(pr, travel ? "TRAVEL & PLAY" : "PLAY", TileRed, true, 44)) gm.BeginMission(m);
            }
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
            UIStyles.PanelBox(panel, r.victory ? UIStyles.Gold : UIStyles.Crimson);

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
                if (AnimBtn(new Rect(bx, by, bw * 2f + gap, 100f), cont, UIStyles.ButtonBig, 1.2f, true, true, 0f)) gm.ContinueJourney();
                if (AnimBtn(new Rect(bx + (bw + gap) * 2f, by, bw, 100f), "RETURN TO MAP", UIStyles.Button, 1.3f, true, false, 0f)) { gm.PendingMission = null; gm.GoTo(GameScreen.WorldMap); }
                if (AnimBtn(new Rect(bx + (bw + gap) * 3f, by, bw, 100f), "CHARACTERS", UIStyles.Button, 1.4f, true, false, 0f)) gm.GoTo(GameScreen.Characters);
                return;
            }
            if (r.victory)
            {
                if (next != null && AnimBtn(new Rect(bx, by, bw, 100f), "NEXT MISSION ▶", UIStyles.ButtonBig, 1.2f, true, true, 0f))
                {
                    gm.SelectedMission = next;
                    gm.GoTo(GameScreen.MissionDetail);
                }
                if (AnimBtn(new Rect(bx + (bw + gap), by, bw, 100f), "REPLAY", UIStyles.Button, 1.3f, true, false, 0f)) gm.BeginMission(r.mission);
            }
            else
            {
                if (AnimBtn(new Rect(bx, by, bw, 100f), "RETRY", UIStyles.ButtonBig, 1.2f, true, true, 0f)) gm.BeginMission(r.mission);
                if (AnimBtn(new Rect(bx + (bw + gap), by, bw, 100f), "UPGRADE SLAYERS", UIStyles.Button, 1.3f, true, false, 0f)) gm.GoTo(GameScreen.Characters);
            }
            if (AnimBtn(new Rect(bx + (bw + gap) * 2f, by, bw, 100f), "RETURN TO MAP", UIStyles.Button, 1.4f, true, false, 0f)) gm.GoTo(GameScreen.WorldMap);
            if (AnimBtn(new Rect(bx + (bw + gap) * 3f, by, bw, 100f), "CHARACTERS", UIStyles.Button, 1.5f, true, false, 0f)) gm.GoTo(GameScreen.Characters);
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
