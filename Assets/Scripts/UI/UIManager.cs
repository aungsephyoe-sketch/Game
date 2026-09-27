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
            GameEvents.BossIntro += OnBossIntro;
            GameEvents.Subtitle += OnSubtitle;
            GameEvents.AreaEntered += OnAreaEntered;
        }

        void OnDisable()
        {
            GameEvents.Banner -= OnBanner;
            GameEvents.SkillUsed -= OnSkillUsed;
            GameEvents.UltimateStarted -= OnUltimateStarted;
            GameEvents.UltimateFinished -= OnUltimateFinished;
            GameEvents.Impact -= OnImpact;
            GameEvents.BossIntro -= OnBossIntro;
            GameEvents.Subtitle -= OnSubtitle;
            GameEvents.AreaEntered -= OnAreaEntered;
        }

        string areaName = "", areaSub = "";
        float areaTime = -10f;
        void OnAreaEntered(string name, string sub) { areaName = name; areaSub = sub; areaTime = Time.unscaledTime; }

        EnemyDefinition bossIntro;
        float bossIntroTime = -10f;
        string subSpeaker = "", subText = "";
        float subTime = -10f;

        void OnBossIntro(EnemyDefinition d) { bossIntro = d; bossIntroTime = Time.unscaledTime; }
        void OnSubtitle(string speaker, string text) { subSpeaker = speaker; subText = text; subTime = Time.unscaledTime; }

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

            try { DrawScreen(); }
            catch (System.Exception ex)
            {
                if (Time.unscaledTime - lastUiError > 5f) Debug.LogError("[UI] " + gm.CurrentScreen + " failed: " + ex);
                lastUiError = Time.unscaledTime;
                if (ex is ExitGUIException) throw;
            }
            DrawOverlays();
        }

        float lastUiError = -10f;

        void DrawScreen()
        {
            switch (gm.CurrentScreen)
            {
                case GameScreen.MainMenu: DrawMainMenu(); break;
                case GameScreen.WorldMap: DrawWorldMap(); break;
                case GameScreen.Summon: DrawSummon(); break;
                case GameScreen.MissionsBoard: DrawMissionsBoard(); break;
                case GameScreen.Shop: DrawShop(); break;
                case GameScreen.Cutscene: DrawCutscene(); break;
                case GameScreen.Credits: DrawCredits(); break;
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

        }

        void DrawOverlays()
        {
            // If a screen keeps failing, always offer a way out instead of a frozen page.
            if (Time.unscaledTime - lastUiError < 1f && gm.CurrentScreen != GameScreen.Battle)
            {
                var er = new Rect(W * 0.5f - 400f, H - 140f, 800f, 100f);
                UIStyles.PanelBox(er, UIStyles.Crimson);
                if (GUI.Button(new Rect(er.x + 20f, er.y + 15f, er.width - 40f, 70f), "Something went wrong on this screen — RETURN HOME", UIStyles.Button))
                {
                    lastUiError = -10f;
                    gm.GoTo(GameScreen.MainMenu);
                }
            }
            if (Time.unscaledTime < toastUntil)
            {
                var r = new Rect(W * 0.5f - 450f, H - 170f, 900f, 80f);
                UIStyles.PanelBox(r);
                GUI.Label(r, toast, UIStyles.Center);
            }

            // Screen transitions: a quick fade through black whenever the 3D stage changes.
            if (gm.TransitionAlpha > 0.001f) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, gm.TransitionAlpha));
        }

        // ------------------------------------------------------------------ Animation helpers

        /// <summary>0→1 eased progress of an entrance animation that starts <paramref name="delay"/> seconds after the screen opened.</summary>
        float Enter(float delay, float duration = 0.35f)
        {
            float t = Mathf.Clamp01((Time.unscaledTime - gm.ScreenEnteredAt - delay) / duration);
            return 1f - Mathf.Pow(1f - t, 3f);
        }

        static Rect Offset(Rect r, float dx, float dy) { return new Rect(r.x + dx, r.y + dy, r.width, r.height); }

        static Rect Grow(Rect r, float amount) { return new Rect(r.x - amount, r.y - amount, r.width + amount * 2f, r.height + amount * 2f); }

        /// <summary>A button that slides/pops in, swells on hover and pulses when highlighted.</summary>
        bool AnimBtn(Rect r, string label, GUIStyle style, float delay, bool enabled = true, bool pulse = false, float slideX = -60f)
        {
            float k = Enter(delay);
            if (k <= 0f) return false;
            var rr = Offset(r, (1f - k) * slideX, 0f);
            bool hover = rr.Contains(Event.current.mousePosition);
            float grow = (hover && enabled ? 6f : 0f) + (pulse ? (Mathf.Sin(Time.unscaledTime * 4f) * 0.5f + 0.5f) * 5f : 0f);
            rr = Grow(rr, grow);
            if (pulse) UIStyles.Frame(Grow(rr, 4f), new Color(1f, 0.8f, 0.35f, 0.4f + 0.4f * Mathf.Sin(Time.unscaledTime * 4f)), 3f);
            var old = GUI.color;
            GUI.color = new Color(old.r, old.g, old.b, old.a * k);
            bool clicked = Btn(rr, label, style, enabled);
            GUI.color = old;
            return clicked && k > 0.6f;
        }

        void Badge(Vector2 at, string text, Color c)
        {
            var r = new Rect(at.x - 22f, at.y - 22f, 44f, 44f);
            UIStyles.CircleTex(r.center, 22f, c);
            GUI.Label(r, text, UIStyles.Sized(UIStyles.Center, 24));
        }

        // ------------------------------------------------------------------ Shared chrome

        void TopBar(string title, GameScreen back)
        {
            UIStyles.Rect(new Rect(0f, 0f, W, 110f + safe.y), UIStyles.Panel);
            UIStyles.Rect(new Rect(0f, 108f + safe.y, W, 3f), UIStyles.Gold * 0.7f);
            if (Btn(new Rect(safe.x + 20f, safe.y + 18f, 150f, 74f), "◀ BACK") || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                gm.GoTo(back);
                if (Event.current.type == EventType.KeyDown) Event.current.Use();
            }
            GUI.Label(new Rect(safe.x + 200f, safe.y + 22f, 700f, 70f), title, UIStyles.H1);
            Currencies(new Rect(safe.xMax - 980f, safe.y + 28f, 960f, 60f));
        }

        void Currencies(Rect r)
        {
            var d = gm.Data;
            float h = Mathf.Min(52f, r.height);
            float w = 210f, gap = 12f;
            float x = r.xMax - w;
            Pill(new Rect(x, r.y, w, h), "✦", new Color(0.3f, 0.65f, 1f), d.crystals.ToString("N0")); x -= w + gap;
            Pill(new Rect(x, r.y, w, h), "◆", new Color(0.95f, 0.72f, 0.2f), d.coins.ToString("N0")); x -= w + gap;
            Pill(new Rect(x, r.y, 150f, h), "✎", new Color(0.6f, 0.4f, 0.9f), "×" + d.expScrolls); x -= 150f + gap;
            Pill(new Rect(x, r.y, 150f, h), "▲", new Color(0.9f, 0.45f, 0.3f), "×" + d.ascensionOre);
        }

        static string Stars(int n) { return CharacterSystem.Stars(n); }

        static string ElementTag(Element e)
        {
            return "<color=#" + UIStyles.Hex(ElementChart.ColorOf(e)) + ">" + ElementChart.Icon(e) + "</color>";
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

        const int TeamSize = 4;
        static readonly Role[] SlotRoles = { Role.DPS, Role.Tank, Role.DPS, Role.Support };

        void AssignToSlot(string id, int inTeam)
        {
            var team = gm.Data.team;
            int existing = team.IndexOf(id);
            if (inTeam >= 0 && inTeam == teamSlot && team.Count > 1)
            {
                // Tapping the slotted slayer again removes them (the team always keeps one member).
                team.RemoveAt(inTeam);
                SaveTeam(gm.Data);
                return;
            }
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
            SaveTeam(gm.Data);
            teamSlot = Mathf.Min((teamSlot + 1) % TeamSize, team.Count);
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
