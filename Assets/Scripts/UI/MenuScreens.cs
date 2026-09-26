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
            // Soft gradient on the left so the menu reads over the living village.
            for (int i = 0; i < 8; i++)
                UIStyles.Rect(new Rect(0f, 0f, 120f * (8 - i) + safe.x, H), new Color(0.02f, 0.01f, 0.05f, 0.07f));

            float x = safe.x + 70f;
            float k = Enter(0f, 0.6f);
            UIStyles.Outlined(new Rect(x, 60f - (1f - k) * 80f, 900f, 90f), GameConfig.TitleLine1, UIStyles.Sized(UIStyles.Title, 60), UIStyles.Crimson, 3f);
            UIStyles.Outlined(new Rect(x, 130f - (1f - k) * 80f, 1000f, 130f), GameConfig.TitleLine2, UIStyles.Sized(UIStyles.Title, 104), Color.white, 4f);
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, Enter(0.3f, 0.6f));
            GUI.Label(new Rect(x + 6f, 258f, 800f, 40f), GameConfig.Subtitle, UIStyles.Small);

            // Where the story stands.
            var next = gm.NextStoryMission();
            var chapter = next != null ? GameDatabase.ChapterOf(next) : null;
            string where = chapter != null ? "CHAPTER " + chapter.number + " — " + chapter.title.ToUpper() : "THE STORY IS COMPLETE";
            GUI.Label(new Rect(x + 6f, 310f, 900f, 44f), "<color=#FFD36B>" + where + "</color>", UIStyles.H2);
            if (next != null)
                GUI.Label(new Rect(x + 6f, 356f, 900f, 40f), "Next: " + next.id + "  " + next.name + "   <color=#AAAAAA>" + RegionName(next.regionId) + "</color>", UIStyles.Small);
            GUI.color = old;

            // Primary actions.
            if (AnimBtn(new Rect(x, 420f, 560f, 130f), "PLAY", UIStyles.ButtonBig, 0.15f, true, true)) gm.GoTo(GameScreen.WorldMap);
            string storyLabel = next != null ? "STORY  <size=24>▶ " + next.id + "</size>" : "STORY";
            if (AnimBtn(new Rect(x, 566f, 560f, 96f), storyLabel, UIStyles.Button, 0.22f))
            {
                if (next != null) { gm.SelectedMission = next; gm.GoTo(GameScreen.MissionDetail); }
                else gm.GoTo(GameScreen.Story);
            }

            // Secondary menu.
            string[] labels = { "SUMMON", "CHARACTERS", "TEAM", "EQUIPMENT", "MISSIONS", "SHOP", "JOURNAL", "SETTINGS" };
            float bx = x, by = 684f, bw = 272f, bh = 78f, gap = 16f;
            for (int i = 0; i < labels.Length; i++)
            {
                var r = new Rect(bx + (i % 2) * (bw + gap), by + (i / 2) * (bh + 12f), bw, bh);
                if (AnimBtn(r, labels[i], UIStyles.Button, 0.3f + i * 0.05f)) OpenMenu(labels[i]);
                int badge = MenuBadge(labels[i]);
                if (badge > 0) Badge(new Vector2(r.xMax - 12f, r.y + 8f), badge > 9 ? "!" : badge.ToString(), UIStyles.Crimson);
            }

            // Team strip.
            float tk = Enter(0.5f, 0.5f);
            var strip = new Rect(safe.xMax - 620f + (1f - tk) * 300f, H - 250f, 600f, 220f);
            UIStyles.PanelBox(strip, UIStyles.Gold);
            GUI.Label(new Rect(strip.x + 24f, strip.y + 14f, 560f, 44f), "TEAM POWER  <color=#FFD36B>" + CharacterSystem.TeamPower(d).ToString("N0") + "</color>", UIStyles.H2);
            float ty = strip.y + 64f;
            for (int i = 0; i < d.team.Count; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                GUI.Label(new Rect(strip.x + 24f, ty, 560f, 36f), ElementTag(def.element) + "  " + def.displayName + "  <color=#AAAAAA>Lv." + c.level + "  " + Stars(c.stars) + "</color>", UIStyles.Sized(UIStyles.Body, 24));
                ty += 36f;
            }
            Currencies(new Rect(safe.xMax - 980f, safe.y + 28f, 960f, 60f));

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

            // Location labels floating over the 3D markers.
            if (cam != null)
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
                        if (unlocked) mapSelected = id;
                        else Toast("Clear " + region.unlockAfterMission + " to open the road to this land.");
                    }
                }
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
            float pk = Enter(0.1f, 0.4f);
            var panel = new Rect(safe.xMax - 660f + (1f - pk) * 300f, safe.y + 130f, 640f, H - safe.y - 150f);
            UIStyles.PanelBox(panel, region2.mapColor);
            GUI.Label(new Rect(panel.x + 26f, panel.y + 16f, panel.width - 52f, 56f), region2.name, UIStyles.H1);
            GUI.Label(new Rect(panel.x + 26f, panel.y + 74f, panel.width - 52f, 40f), region2.subtitle, UIStyles.Small);
            float y = panel.y + 124f;
            if (mapSelected != d.currentRegion && d.IsRegionUnlocked(region2))
            {
                if (Btn(new Rect(panel.x + 26f, y, panel.width - 52f, 70f), "TRAVEL HERE")) gm.TravelTo(mapSelected);
                y += 84f;
            }
            var list = GameDatabase.MissionsInRegion(mapSelected);
            var view = new Rect(panel.x + 16f, y, panel.width - 32f, panel.yMax - y - 120f);
            var content = new Rect(0f, 0f, view.width - 24f, list.Count * 104f);
            mapScroll = GUI.BeginScrollView(view, mapScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                bool unlocked = d.IsMissionUnlocked(m);
                bool cleared = d.IsMissionCleared(m.id);
                var r = new Rect(0f, i * 104f, content.width, 94f);
                string tag = TypeTag(m.type);
                string label = "<size=26>" + tag + "  " + m.id + "  " + m.name + "</size>\n<size=20><color=#AAAAAA>" +
                               (unlocked ? "Rec. Lv." + m.recommendedLevel : "Locked — clear " + m.requiresMissionId) + (cleared ? "   <color=#7CFF8A>✔ CLEARED</color>" : "") + "</color></size>";
                if (Btn(r, label, UIStyles.Button, unlocked))
                {
                    gm.SelectedMission = m;
                    gm.GoTo(GameScreen.MissionDetail);
                }
            }
            GUI.EndScrollView();

            var next = gm.NextStoryMission();
            if (next != null && Btn(new Rect(panel.x + 26f, panel.yMax - 100f, panel.width - 52f, 80f), "CONTINUE STORY ▶ " + next.id, UIStyles.ButtonBig))
            {
                gm.SelectedMission = next;
                mapSelected = next.regionId;
                gm.GoTo(GameScreen.MissionDetail);
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
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.35f));
            TopBar(m.type == MissionType.Training ? "TRAINING" : "MISSION " + m.MissionLabel, GameScreen.WorldMap);
            bool unlocked = d.IsMissionUnlocked(m);

            float k = Enter(0f, 0.4f);
            var left = new Rect(safe.x + 30f - (1f - k) * 200f, safe.y + 130f, W * 0.58f - safe.x - 45f, H - safe.y - 150f);
            UIStyles.PanelBox(left, m.type == MissionType.Boss ? UIStyles.Crimson : UIStyles.Gold);
            float x = left.x + 30f, w = left.width - 60f, y = left.y + 18f;
            GUI.Label(new Rect(x, y, w, 40f), TypeTag(m.type) + "   <color=#AAAAAA>" + RegionName(m.regionId) + (m.chapter > 0 ? "  ·  Chapter " + m.chapter : "") + "</color>", UIStyles.Small);
            y += 40f;
            UIStyles.Outlined(new Rect(x, y, w, 70f), m.name, UIStyles.Sized(UIStyles.H1, 52), Color.white, 2f);
            y += 74f;
            if (!string.IsNullOrEmpty(m.questGiver))
            {
                GUI.Label(new Rect(x, y, w, 34f), "<color=#7FD8FF>Requested by " + m.questGiver + "</color>", UIStyles.Small);
                y += 36f;
            }
            GUI.Label(new Rect(x, y, w, 80f), "<i>" + m.storyText + "</i>", UIStyles.Body);
            y += 84f;

            int avgLevel = 0;
            foreach (var id in d.team) { var c = d.GetCharacter(id); if (c != null) avgLevel += c.level; }
            avgLevel = d.team.Count > 0 ? avgLevel / d.team.Count : 1;
            string lc = avgLevel >= m.recommendedLevel ? "#7CFF8A" : "#FF7070";
            GUI.Label(new Rect(x, y, w, 36f), "Recommended Lv. <b>" + m.recommendedLevel + "</b>   Your team <color=" + lc + ">Lv." + avgLevel + "</color>   ·   Enemy Lv." + m.enemyLevel +
                (m.allies > 0 ? "   ·   <color=#7FD8FF>" + m.allies + " allied soldiers</color>" : "") + (m.sealPuzzle ? "   ·   <color=#7FD8FF>Seal puzzle</color>" : ""), UIStyles.Small);
            y += 46f;

            // Enemies.
            GUI.Label(new Rect(x, y, w, 36f), "ENEMIES", UIStyles.Sized(UIStyles.H2, 28));
            y += 36f;
            GUI.Label(new Rect(x, y, w, 64f), EnemySummary(m), UIStyles.Sized(UIStyles.Small, 21));
            y += 64f;

            // Boss info.
            var bosses = new List<string>(m.preBosses);
            if (!string.IsNullOrEmpty(m.bossId)) bosses.Add(m.bossId);
            foreach (var bid in bosses)
            {
                var b = GameDatabase.GetEnemy(bid);
                if (b == null) continue;
                var br = new Rect(x, y, w, 76f);
                UIStyles.Rect(br, new Color(0.35f, 0.04f, 0.06f, 0.55f));
                GUI.Label(new Rect(br.x + 14f, br.y + 4f, w - 28f, 36f), "<color=#FF6060>☠ BOSS</color>  " + b.displayName + " — <i>" + b.bossTitle + "</i>  " + ElementTag(b.element), UIStyles.Sized(UIStyles.Body, 24));
                GUI.Label(new Rect(br.x + 14f, br.y + 38f, w - 28f, 36f), "<color=#CCCCCC>" + b.description + "</color>", UIStyles.Sized(UIStyles.Small, 19));
                y += 84f;
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
                GUI.Label(new Rect(x, y, w, 60f), "REWARDS  <size=22>" + RewardText(m.rewards) + "</size>", UIStyles.Sized(UIStyles.Body, 24));
                y += 44f;
                if (prog == null || !prog.cleared)
                    GUI.Label(new Rect(x, y, w, 60f), "<color=#FF9C7A>FIRST CLEAR</color>  <size=22>" + RewardText(m.firstClearRewards) + "</size>", UIStyles.Sized(UIStyles.Body, 24));
            }

            // Team + PLAY.
            var right = new Rect(W * 0.58f + 5f + (1f - k) * 200f, safe.y + 130f, safe.xMax - W * 0.58f - 35f, H - safe.y - 150f);
            UIStyles.PanelBox(right, UIStyles.Crimson);
            float ry = right.y + 20f;
            GUI.Label(new Rect(right.x + 26f, ry, right.width - 52f, 44f), "YOUR TEAM   <color=#FFD36B>" + CharacterSystem.TeamPower(d).ToString("N0") + "</color>", UIStyles.H2);
            ry += 54f;
            for (int i = 0; i < d.team.Count; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                GUI.Label(new Rect(right.x + 26f, ry, right.width - 52f, 40f), "<color=#AAAAAA><size=18>" + SlotNames[i].Split(' ')[0] + "</size></color>  " + ElementTag(def.element) + " " + def.displayName +
                    "  <color=#AAAAAA>Lv." + c.level + " " + Stars(c.stars) + "</color>", UIStyles.Sized(UIStyles.Body, 24));
                ry += 42f;
            }
            ry += 10f;
            GUI.Label(new Rect(right.x + 26f, ry, right.width - 52f, 70f), "<size=20><color=#AAAAAA>Tip: element advantage deals ×1.5.  " +
                "Water ▶ Flame ▶ Beast ▶ Thunder ▶ Water;  Light ◀▶ Dark.</color></size>", UIStyles.Small);
            if (Btn(new Rect(right.x + 26f, right.yMax - 250f, right.width - 52f, 80f), "CHANGE TEAM")) { teamReturn = GameScreen.MissionDetail; gm.GoTo(GameScreen.Team); }
            if (!unlocked)
                GUI.Label(new Rect(right.x + 26f, right.yMax - 160f, right.width - 52f, 130f), "<color=#FF7070>Clear " + m.requiresMissionId + " first.</color>", UIStyles.Sized(UIStyles.Center, 32));
            else if (AnimBtn(new Rect(right.x + 26f, right.yMax - 160f, right.width - 52f, 136f),
                         m.regionId != d.currentRegion && System.Array.IndexOf(MapStage.RouteOrder, m.regionId) >= 0 && m.type != MissionType.Training && m.type != MissionType.Event
                             ? "TRAVEL & PLAY" : "PLAY", UIStyles.ButtonBig, 0.35f, true, true, 60f))
                gm.BeginMission(m);
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
