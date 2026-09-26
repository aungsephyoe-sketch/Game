using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Immediate-mode UI for every screen (menus, battle HUD, results). Drawn in a 1080-tall virtual space.
    /// IMGUI keeps the prototype dependency-free; screens are isolated methods so each can be ported to
    /// uGUI/UI Toolkit with final art later without touching game systems.
    /// </summary>
    public partial class UIManager : MonoBehaviour
    {
        GameManager gm;
        float W, H;
        Rect safe;
        Vector2 scroll;
        string toast = "";
        float toastUntil;

        // Battle presentation state (fed by GameEvents).
        string bannerTitle = "", bannerSub = "";
        float bannerTime = -10f;
        string callout = "";
        Color calloutColor = Color.white;
        float calloutTime = -10f;
        string ultCharacter = "", ultName = "";
        Color ultColor = Color.white;
        float ultTime = -10f;
        float ultTotal;
        float ultTotalTime = -10f;

        void OnEnable()
        {
            GameEvents.Banner += OnBanner;
            GameEvents.SkillUsed += OnSkillUsed;
            GameEvents.UltimateStarted += OnUltimateStarted;
            GameEvents.UltimateFinished += OnUltimateFinished;
            GameEvents.Impact += OnImpact;
        }

        void OnDisable()
        {
            GameEvents.Banner -= OnBanner;
            GameEvents.SkillUsed -= OnSkillUsed;
            GameEvents.UltimateStarted -= OnUltimateStarted;
            GameEvents.UltimateFinished -= OnUltimateFinished;
            GameEvents.Impact -= OnImpact;
        }

        float impactTime = -10f;
        float impactStrength;

        void OnImpact(float strength)
        {
            impactTime = Time.unscaledTime;
            impactStrength = Mathf.Clamp01(strength);
        }

        void OnBanner(string title, string sub)
        {
            bannerTitle = title;
            bannerSub = sub;
            bannerTime = Time.unscaledTime;
        }

        void OnSkillUsed(PlayerCharacter pc, AbilityDefinition ab)
        {
            callout = pc.Def.breathingStyle + " — " + ab.name;
            calloutColor = ElementChart.ColorOf(pc.Element);
            calloutTime = Time.unscaledTime;
        }

        void OnUltimateStarted(PlayerCharacter pc, AbilityDefinition ab)
        {
            ultCharacter = pc.Def.displayName;
            ultName = ab.name;
            ultColor = ElementChart.ColorOf(pc.Element);
            ultTime = Time.unscaledTime;
        }

        void OnUltimateFinished(PlayerCharacter pc, float total)
        {
            ultTotal = total;
            ultTotalTime = Time.unscaledTime;
        }

        public void Toast(string message)
        {
            toast = message;
            toastUntil = Time.unscaledTime + 2.2f;
        }

        bool Btn(Rect r, string label, GUIStyle style = null, bool enabled = true)
        {
            var old = GUI.enabled;
            GUI.enabled = enabled;
            bool clicked = GUI.Button(r, label, style ?? UIStyles.Button);
            GUI.enabled = old;
            if (clicked && gm != null) gm.Audio.Play("click", 0.6f);
            return clicked;
        }

        void OnGUI()
        {
            gm = GameManager.Instance;
            if (gm == null) return;
            UIStyles.Ensure();
            float s = HudLayout.Scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
            W = HudLayout.Width;
            H = HudLayout.Height;
            safe = HudLayout.Safe;

            switch (gm.CurrentScreen)
            {
                case GameScreen.MainMenu: DrawMainMenu(); break;
                case GameScreen.Story: DrawStory(); break;
                case GameScreen.MissionDetail: DrawMissionDetail(); break;
                case GameScreen.Team: DrawTeam(); break;
                case GameScreen.Characters: DrawCharacters(); break;
                case GameScreen.CharacterDetail: DrawCharacterDetail(); break;
                case GameScreen.Equipment: DrawEquipment(); break;
                case GameScreen.Battle: DrawBattleHUD(); break;
                case GameScreen.Results: DrawResults(); break;
                case GameScreen.ComingSoon: DrawComingSoon(); break;
                case GameScreen.Settings: DrawSettings(); break;
            }

            if (Time.unscaledTime < toastUntil)
            {
                var r = new Rect(W * 0.5f - 450f, H - 170f, 900f, 80f);
                UIStyles.PanelBox(r);
                GUI.Label(r, toast, UIStyles.Center);
            }
        }

        // ------------------------------------------------------------------ Shared chrome

        void TopBar(string title, GameScreen back)
        {
            UIStyles.Rect(new Rect(0f, 0f, W, 110f + safe.y), UIStyles.Panel);
            UIStyles.Rect(new Rect(0f, 108f + safe.y, W, 3f), UIStyles.Gold * 0.7f);
            if (Btn(new Rect(safe.x + 20f, safe.y + 18f, 150f, 74f), "◀ BACK")) gm.GoTo(back);
            GUI.Label(new Rect(safe.x + 200f, safe.y + 22f, 700f, 70f), title, UIStyles.H1);
            Currencies(new Rect(safe.xMax - 980f, safe.y + 28f, 960f, 60f));
        }

        void Currencies(Rect r)
        {
            var d = gm.Data;
            string text = "<color=#FFD36B>◆ " + d.coins.ToString("N0") + "</color>    <color=#7FD8FF>✦ " + d.crystals.ToString("N0") +
                          "</color>    <color=#C9A7FF>EXP ×" + d.expScrolls + "</color>    <color=#9CF29C>SKILL ×" + d.skillScrolls +
                          "</color>    <color=#FF9C7A>ORE ×" + d.ascensionOre + "</color>";
            GUI.Label(r, text, UIStyles.Sized(UIStyles.Right, 26));
        }

        static string Stars(int n) { return CharacterSystem.Stars(n); }

        static string ElementTag(Element e)
        {
            return "<color=#" + UIStyles.Hex(ElementChart.ColorOf(e)) + ">" + ElementChart.Icon(e) + "</color>";
        }

        // ------------------------------------------------------------------ Main menu

        void DrawMainMenu()
        {
            var d = gm.Data;
            UIStyles.Rect(new Rect(0f, 0f, 820f + safe.x, H), new Color(0.03f, 0.02f, 0.06f, 0.55f));
            float x = safe.x + 70f;
            UIStyles.Outlined(new Rect(x, 80f, 900f, 90f), GameConfig.TitleLine1, UIStyles.Sized(UIStyles.Title, 60), UIStyles.Crimson, 3f);
            UIStyles.Outlined(new Rect(x, 150f, 1000f, 130f), GameConfig.TitleLine2, UIStyles.Sized(UIStyles.Title, 104), Color.white, 4f);
            GUI.Label(new Rect(x + 6f, 280f, 700f, 40f), GameConfig.Subtitle, UIStyles.Small);

            GUI.Label(new Rect(x, 350f, 700f, 50f), "TEAM POWER  <color=#FFD36B>" + CharacterSystem.TeamPower(d).ToString("N0") + "</color>", UIStyles.H2);
            float y = 410f;
            foreach (var id in d.team)
            {
                var c = d.GetCharacter(id);
                var def = GameDatabase.GetCharacter(id);
                if (c == null || def == null) continue;
                GUI.Label(new Rect(x, y, 760f, 40f), ElementTag(def.element) + "  " + def.displayName + "  <color=#AAAAAA>Lv." + c.level + "  " + Stars(c.stars) + "</color>", UIStyles.Body);
                y += 44f;
            }

            if (Btn(new Rect(x, 580f, 520f, 130f), "PLAY", UIStyles.ButtonBig)) OpenStoryAtNext();

            float bx = x, by = 740f, bw = 250f, bh = 84f, gap = 16f;
            if (Btn(new Rect(bx, by, bw, bh), "Story")) gm.GoTo(GameScreen.Story);
            if (Btn(new Rect(bx + bw + gap, by, bw, bh), "Events")) gm.ShowComingSoon("Events (Mugen-style limited missions & event shop) — Phase 3");
            by += bh + gap;
            if (Btn(new Rect(bx, by, bw, bh), "Characters")) gm.GoTo(GameScreen.Characters);
            if (Btn(new Rect(bx + bw + gap, by, bw, bh), "Summon")) gm.ShowComingSoon("Summoning banners with pity — Phase 3");
            by += bh + gap;
            if (Btn(new Rect(bx, by, bw, bh), "Equipment")) gm.GoTo(GameScreen.Equipment);
            if (Btn(new Rect(bx + bw + gap, by, bw, bh), "Team")) { teamReturn = GameScreen.MainMenu; gm.GoTo(GameScreen.Team); }

            Currencies(new Rect(safe.xMax - 980f, safe.y + 28f, 960f, 60f));
            if (Btn(new Rect(safe.xMax - 300f, H - 110f, 280f, 70f), "Daily Missions", UIStyles.ButtonSmall))
                gm.ShowComingSoon("Daily missions — Phase 3");
            if (Btn(new Rect(safe.xMax - 300f, H - 270f, 280f, 70f), "Settings", UIStyles.ButtonSmall)) gm.OpenSettings();
            if (Btn(new Rect(safe.xMax - 300f, H - 190f, 280f, 70f), "Reset Save", UIStyles.ButtonSmall))
            {
                if (Time.unscaledTime - resetArmed < 3f) { gm.ResetSave(); Toast("Progress reset."); }
                else { resetArmed = Time.unscaledTime; Toast("Tap Reset Save again to confirm."); }
            }
        }

        float resetArmed = -10f;

        void OpenStoryAtNext()
        {
            foreach (var ch in GameDatabase.Chapters)
                foreach (var m in ch.missions)
                    if (!gm.Data.IsMissionCleared(m.id) && gm.Data.IsMissionUnlocked(m))
                    {
                        selectedChapter = ch.number;
                        gm.SelectedMission = m;
                        gm.GoTo(GameScreen.MissionDetail);
                        return;
                    }
            gm.GoTo(GameScreen.Story);
        }

        // ------------------------------------------------------------------ Story

        int selectedChapter = 1;

        void DrawStory()
        {
            TopBar("STORY", GameScreen.MainMenu);
            float top = safe.y + 140f;
            float x = safe.x + 30f;
            foreach (var ch in GameDatabase.Chapters)
            {
                var r = new Rect(x, top, 440f, 100f);
                bool sel = ch.number == selectedChapter;
                string label = "Chapter " + ch.number + "\n<size=22>" + ch.title + (ch.available ? "" : "  (coming soon)") + "</size>";
                if (Btn(r, label, sel ? UIStyles.ButtonBig : UIStyles.Button, ch.available)) selectedChapter = ch.number;
                top += 112f;
            }

            var chapter = GameDatabase.Chapters.Find(c => c.number == selectedChapter);
            if (chapter == null) return;
            float mx = x + 480f, mw = safe.xMax - mx - 30f;
            float my = safe.y + 140f;
            foreach (var m in chapter.missions)
            {
                var r = new Rect(mx, my, mw, 150f);
                bool unlocked = gm.Data.IsMissionUnlocked(m);
                var prog = gm.Data.GetMission(m.id);
                UIStyles.PanelBox(r, unlocked ? UIStyles.Gold : Color.gray);
                GUI.Label(new Rect(r.x + 24f, r.y + 16f, mw - 400f, 50f), "Mission " + m.id + "  —  " + m.name + (string.IsNullOrEmpty(m.bossId) ? "" : "  <color=#FF6060>BOSS</color>"), UIStyles.H2);
                GUI.Label(new Rect(r.x + 24f, r.y + 66f, mw - 420f, 70f), m.description + "\n<size=22>Enemy Lv." + m.enemyLevel + "   Recommended power " + m.recommendedPower.ToString("N0") + "</size>", UIStyles.Small);
                string stars = "";
                for (int i = 0; i < 3; i++) stars += prog != null && (prog.objectivesMask & (1 << i)) != 0 ? "★" : "☆";
                UIStyles.Colored(new Rect(r.xMax - 390f, r.y + 20f, 160f, 60f), stars, UIStyles.Sized(UIStyles.Center, 44), UIStyles.Gold);
                if (unlocked)
                {
                    if (Btn(new Rect(r.xMax - 210f, r.y + 35f, 180f, 80f), prog != null && prog.cleared ? "REPLAY" : "GO"))
                    {
                        gm.SelectedMission = m;
                        gm.GoTo(GameScreen.MissionDetail);
                    }
                }
                else GUI.Label(new Rect(r.xMax - 230f, r.y + 45f, 200f, 60f), "LOCKED", UIStyles.Center);
                my += 166f;
            }
        }

        // ------------------------------------------------------------------ Mission detail

        void DrawMissionDetail()
        {
            var m = gm.SelectedMission;
            if (m == null) { gm.GoTo(GameScreen.Story); return; }
            TopBar("MISSION " + m.id, GameScreen.Story);
            var d = gm.Data;
            var prog = d.GetMission(m.id);

            var left = new Rect(safe.x + 30f, safe.y + 140f, W * 0.5f - safe.x - 45f, H - safe.y - 170f);
            UIStyles.PanelBox(left);
            float y = left.y + 24f;
            GUI.Label(new Rect(left.x + 30f, y, left.width - 60f, 60f), m.name, UIStyles.H1);
            y += 70f;
            GUI.Label(new Rect(left.x + 30f, y, left.width - 60f, 80f), m.description, UIStyles.Body);
            y += 90f;
            int power = CharacterSystem.TeamPower(d);
            string pc = power >= m.recommendedPower ? "#7CFF8A" : "#FF7070";
            GUI.Label(new Rect(left.x + 30f, y, left.width - 60f, 40f), "Enemy Lv." + m.enemyLevel + "     Recommended " + m.recommendedPower.ToString("N0") +
                "     Your team <color=" + pc + ">" + power.ToString("N0") + "</color>", UIStyles.Small);
            y += 60f;
            GUI.Label(new Rect(left.x + 30f, y, 400f, 40f), "OBJECTIVES", UIStyles.H2);
            y += 50f;
            string[] labels = { "Defeat " + m.killObjective + " demons", "No slayer falls", "Clear within " + Mathf.RoundToInt(m.parTime) + " seconds" };
            for (int i = 0; i < 3; i++)
            {
                bool done = prog != null && (prog.objectivesMask & (1 << i)) != 0;
                GUI.Label(new Rect(left.x + 40f, y, left.width - 80f, 40f), (done ? "<color=#7CFF8A>☑</color> " : "☐ ") + labels[i] +
                    (done ? "" : "  <color=#7FD8FF>+" + RewardSystem.CrystalsPerNewObjective + " ✦</color>"), UIStyles.Body);
                y += 44f;
            }
            y += 20f;
            GUI.Label(new Rect(left.x + 30f, y, 400f, 40f), "REWARDS", UIStyles.H2);
            y += 50f;
            GUI.Label(new Rect(left.x + 40f, y, left.width - 80f, 120f), RewardText(m.rewards), UIStyles.Body);
            y += 90f;
            if (prog == null || !prog.cleared)
            {
                GUI.Label(new Rect(left.x + 30f, y, left.width - 60f, 40f), "FIRST CLEAR", UIStyles.Sized(UIStyles.H2, 30));
                y += 42f;
                GUI.Label(new Rect(left.x + 40f, y, left.width - 80f, 120f), RewardText(m.firstClearRewards), UIStyles.Body);
            }

            var right = new Rect(W * 0.5f + 15f, safe.y + 140f, safe.xMax - W * 0.5f - 45f, H - safe.y - 170f);
            UIStyles.PanelBox(right, UIStyles.Crimson);
            float ry = right.y + 24f;
            GUI.Label(new Rect(right.x + 30f, ry, right.width - 60f, 50f), "YOUR TEAM", UIStyles.H2);
            ry += 60f;
            for (int i = 0; i < d.team.Count; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                GUI.Label(new Rect(right.x + 30f, ry, right.width - 60f, 44f), (i == 0 ? "LEAD  " : "          ") + ElementTag(def.element) + "  " + def.FullName, UIStyles.Body);
                GUI.Label(new Rect(right.x + 30f, ry + 38f, right.width - 60f, 36f), "          Lv." + c.level + "  " + Stars(c.stars) + "   Power " + CharacterSystem.Power(d, c).ToString("N0"), UIStyles.Small);
                ry += 90f;
            }
            GUI.Label(new Rect(right.x + 30f, ry + 10f, right.width - 60f, 100f),
                "Tip: element advantage deals ×1.5 damage.\nWater ▶ Flame ▶ Beast ▶ Thunder ▶ Water.  Light ◀▶ Dark.", UIStyles.Small);
            if (Btn(new Rect(right.x + 30f, right.yMax - 250f, right.width - 60f, 90f), "CHANGE TEAM")) { teamReturn = GameScreen.MissionDetail; gm.GoTo(GameScreen.Team); }
            if (Btn(new Rect(right.x + 30f, right.yMax - 140f, right.width - 60f, 110f), "START", UIStyles.ButtonBig)) gm.StartMission(m);
        }

        static string RewardText(RewardBundle r)
        {
            var sb = new System.Text.StringBuilder();
            if (r.exp > 0) sb.Append("EXP +" + r.exp.ToString("N0") + "    ");
            if (r.coins > 0) sb.Append("<color=#FFD36B>Coins +" + r.coins.ToString("N0") + "</color>    ");
            if (r.crystals > 0) sb.Append("<color=#7FD8FF>Crystals +" + r.crystals + "</color>    ");
            if (r.expScrolls > 0) sb.Append("EXP Scroll ×" + r.expScrolls + "    ");
            if (r.skillScrolls > 0) sb.Append("Skill Scroll ×" + r.skillScrolls + "    ");
            if (r.ascensionOre > 0) sb.Append("Ore ×" + r.ascensionOre + "    ");
            foreach (var e in r.equipmentIds)
            {
                var def = GameDatabase.GetEquipment(e);
                if (def != null) sb.Append("<color=#C9A7FF>" + def.displayName + "</color>    ");
            }
            if (!string.IsNullOrEmpty(r.characterId))
            {
                var c = GameDatabase.GetCharacter(r.characterId);
                if (c != null) sb.Append("<color=#FF9C7A>NEW SLAYER: " + c.FullName + "</color>");
            }
            return sb.ToString();
        }

        // ------------------------------------------------------------------ Team

        int teamSlot;
        GameScreen teamReturn = GameScreen.MainMenu;

        void DrawTeam()
        {
            TopBar("TEAM", teamReturn);
            var d = gm.Data;
            float top = safe.y + 140f;
            GUI.Label(new Rect(safe.x + 30f, top, 1400f, 40f), "Tap a slot, then a slayer to place them. The first slot leads the battle.", UIStyles.Small);
            top += 50f;
            float sw = (safe.width - 60f - 40f) / 3f;
            for (int i = 0; i < 3; i++)
            {
                var r = new Rect(safe.x + 30f + i * (sw + 20f), top, sw, 140f);
                string label = "SLOT " + (i + 1) + (i == 0 ? " (LEAD)" : "");
                if (i < d.team.Count)
                {
                    var def = GameDatabase.GetCharacter(d.team[i]);
                    var c = d.GetCharacter(d.team[i]);
                    label += "\n" + def.FullName + "\n<size=22>" + ElementChart.Icon(def.element) + "  Lv." + c.level + "  " + Stars(c.stars) + "</size>";
                }
                else label += "\n(empty)";
                if (Btn(r, label, teamSlot == i ? UIStyles.ButtonBig : UIStyles.Button)) teamSlot = i;
            }

            top += 170f;
            GUI.Label(new Rect(safe.x + 30f, top, 600f, 50f), "ROSTER", UIStyles.H2);
            top += 55f;
            var view = new Rect(safe.x + 30f, top, safe.width - 60f, H - top - 30f);
            float rowH = 96f;
            var content = new Rect(0f, 0f, view.width - 30f, d.characters.Count * (rowH + 10f));
            scroll = GUI.BeginScrollView(view, scroll, content);
            for (int i = 0; i < d.characters.Count; i++)
            {
                var c = d.characters[i];
                var def = GameDatabase.GetCharacter(c.id);
                var r = new Rect(0f, i * (rowH + 10f), content.width, rowH);
                int inTeam = d.team.IndexOf(c.id);
                string label = ElementTag(def.element) + "   " + def.FullName + "   <size=24>" + Stars(c.stars) + "  Lv." + c.level + "   Power " +
                               CharacterSystem.Power(d, c).ToString("N0") + (inTeam >= 0 ? "   <color=#FFD36B>[SLOT " + (inTeam + 1) + "]</color>" : "") + "</size>";
                if (Btn(r, label)) AssignToSlot(c.id);
            }
            GUI.EndScrollView();
        }

        void AssignToSlot(string id)
        {
            var team = gm.Data.team;
            int existing = team.IndexOf(id);
            if (teamSlot >= team.Count)
            {
                if (existing < 0) team.Add(id);
            }
            else if (existing >= 0)
            {
                // Swap positions within the team.
                string other = team[teamSlot];
                team[teamSlot] = id;
                team[existing] = other;
            }
            else team[teamSlot] = id;
            gm.Save();
            gm.Stage.Show(gm.Data);
            teamSlot = (teamSlot + 1) % 3;
        }

        // ------------------------------------------------------------------ Results

        void DrawResults()
        {
            var r = gm.LastResult;
            if (r == null) { gm.GoTo(GameScreen.MainMenu); return; }
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.55f));
            var panel = new Rect(W * 0.5f - 820f, 60f, 1640f, H - 120f);
            UIStyles.PanelBox(panel, r.victory ? UIStyles.Gold : UIStyles.Crimson);
            UIStyles.Outlined(new Rect(panel.x, panel.y + 20f, panel.width, 110f), r.victory ? "MISSION CLEAR" : "MISSION FAILED",
                UIStyles.Sized(UIStyles.Big, 86), r.victory ? UIStyles.Gold : UIStyles.Bad, 4f);
            GUI.Label(new Rect(panel.x, panel.y + 125f, panel.width, 44f), "Mission " + r.mission.id + " — " + r.mission.name +
                (r.victory ? "" : "   (" + r.failReason + ")"), UIStyles.Center);

            float x = panel.x + 60f, y = panel.y + 200f;
            int mins = Mathf.FloorToInt(r.time / 60f), secs = Mathf.FloorToInt(r.time % 60f);
            GUI.Label(new Rect(x, y, 700f, 44f), "Time  " + mins + ":" + secs.ToString("00"), UIStyles.H2); y += 52f;
            GUI.Label(new Rect(x, y, 700f, 44f), "Demons slain  " + r.kills, UIStyles.H2); y += 52f;
            GUI.Label(new Rect(x, y, 700f, 44f), "Max combo  " + r.maxCombo, UIStyles.H2); y += 52f;
            GUI.Label(new Rect(x, y, 700f, 44f), "Total damage  " + Mathf.RoundToInt(r.totalDamage).ToString("N0"), UIStyles.H2); y += 70f;
            for (int i = 0; i < 3; i++)
            {
                GUI.Label(new Rect(x, y, 760f, 44f), (r.objectives[i] ? "<color=#7CFF8A>☑</color> " : "<color=#888888>☐</color> ") + r.objectiveLabels[i], UIStyles.Body);
                y += 46f;
            }

            float rx = panel.x + 860f, ry = panel.y + 200f;
            GUI.Label(new Rect(rx, ry, 700f, 50f), "REWARDS" + (r.firstClear ? "  <color=#FF9C7A>(FIRST CLEAR)</color>" : ""), UIStyles.H2);
            ry += 56f;
            GUI.Label(new Rect(rx, ry, 720f, 200f), RewardText(r.granted), UIStyles.Body);
            ry += 150f;
            foreach (var lu in r.levelUps)
            {
                GUI.Label(new Rect(rx, ry, 720f, 40f), "<color=#7CFF8A>LEVEL UP</color>  " + lu.name + "  Lv." + lu.from + " → Lv." + lu.to, UIStyles.Body);
                ry += 42f;
            }
            if (!string.IsNullOrEmpty(r.unlockedCharacterName))
            {
                ry += 10f;
                UIStyles.Outlined(new Rect(rx, ry, 720f, 50f), "NEW SLAYER JOINED: " + r.unlockedCharacterName, UIStyles.Sized(UIStyles.H2, 34), UIStyles.Gold);
                ry += 56f;
            }

            float by = panel.yMax - 130f;
            if (Btn(new Rect(panel.x + 60f, by, 420f, 100f), "MENU")) gm.GoTo(GameScreen.MainMenu);
            if (Btn(new Rect(panel.x + 610f, by, 420f, 100f), "RETRY")) gm.StartMission(r.mission);
            var next = NextMission(r.mission);
            if (r.victory && next != null && Btn(new Rect(panel.xMax - 480f, by, 420f, 100f), "NEXT: " + next.id, UIStyles.ButtonBig))
            {
                gm.SelectedMission = next;
                gm.GoTo(GameScreen.MissionDetail);
            }
            else if (!r.victory && Btn(new Rect(panel.xMax - 480f, by, 420f, 100f), "UPGRADE SLAYERS", UIStyles.ButtonBig))
                gm.GoTo(GameScreen.Characters);
        }

        static MissionDefinition NextMission(MissionDefinition m)
        {
            bool found = false;
            foreach (var ch in GameDatabase.Chapters)
                foreach (var x in ch.missions)
                {
                    if (found) return x;
                    if (x == m) found = true;
                }
            return null;
        }

        // ------------------------------------------------------------------ Coming soon

        void DrawComingSoon()
        {
            TopBar("COMING SOON", GameScreen.MainMenu);
            var r = new Rect(W * 0.5f - 650f, H * 0.5f - 180f, 1300f, 360f);
            UIStyles.PanelBox(r);
            GUI.Label(new Rect(r.x + 40f, r.y + 50f, r.width - 80f, 80f), gm.ComingSoonFeature, UIStyles.Sized(UIStyles.Center, 40));
            GUI.Label(new Rect(r.x + 40f, r.y + 150f, r.width - 80f, 160f),
                "This vertical slice focuses on combat, progression and missions (Phases 1–2).\nSummons, events, daily missions and the shop arrive in Phase 3; co-op and PvP in Phase 4.",
                UIStyles.CenterSmall);
        }
    }
}
