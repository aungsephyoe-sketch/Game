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
                case GameScreen.MainMenu: DrawHome(); break;
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
                case GameScreen.Events: DrawEvents(); break;
                case GameScreen.Inventory: DrawInventory(); break;
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

        void Badge(Vector2 at, string text, Color c, float scale = 1f)
        {
            var r = new Rect(at.x - 22f, at.y - 22f, 44f, 44f);
            UIStyles.CircleTex(r.center, 22f * scale, c);
            GUI.Label(r, text, UIStyles.Sized(UIStyles.Center, 24));
        }

        // ------------------------------------------------------------------ Shared chrome

        void TopBar(string title, GameScreen back, System.Action onBack = null)
        {
            // Dark strip, small rounded BACK, gold title, resources on the right (reference layout).
            UIStyles.Rect(new Rect(0f, 0f, W, 104f + safe.y), new Color(0.05f, 0.06f, 0.1f, 0.92f));
            UIStyles.Rect(new Rect(0f, 104f + safe.y, W, 2f), new Color(1f, 1f, 1f, 0.06f));
            var br = new Rect(safe.x + 22f, safe.y + 24f, 150f, 58f);
            bool hover = br.Contains(Event.current.mousePosition);
            Round(br, hover ? new Color(0.28f, 0.26f, 0.45f) : new Color(0.18f, 0.17f, 0.3f), 10f);
            RoundFrame(br, new Color(1f, 1f, 1f, 0.12f), 2f, 10f);
            GUI.Label(br, "‹  BACK", UIStyles.Sized(UIStyles.Center, 26));
            if (GUI.Button(br, GUIContent.none, GUIStyle.none) || (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape))
            {
                gm.Audio.Play("click", 0.5f);
                if (onBack != null) onBack();
                else gm.GoTo(back);
                if (Event.current.type == EventType.KeyDown) Event.current.Use();
            }
            UIStyles.Outlined(new Rect(safe.x + 196f, safe.y + 18f, 800f, 70f), title, UIStyles.Sized(UIStyles.H1, 50), new Color(1f, 0.8f, 0.2f), 2f);
            Currencies(new Rect(safe.xMax - 1000f, safe.y + 26f, 980f, 56f));
        }

        void Currencies(Rect r)
        {
            var d = gm.Data;
            float h = r.height;
            float x = r.xMax;
            var st = UIStyles.Sized(UIStyles.Body, 30);
            // Right to left: ORE, SKILL, EXP, crystals, coins.
            // Three currencies: XP, diamonds, gold.
            string xpS = d.xp.ToString("N0");
            float xw = st.CalcSize(new GUIContent(xpS)).x;
            x -= xw + 12f;
            GUI.Label(new Rect(x, r.y, xw + 4f, h), xpS, st);
            XpIcon(new Vector2(x - 26f, r.y + h * 0.5f), 38f);
            x -= 80f;
            string cr = d.crystals.ToString("N0");
            float cw = st.CalcSize(new GUIContent(cr)).x;
            x -= cw + 12f;
            GUI.Label(new Rect(x, r.y, cw + 4f, h), cr, st);
            DiamondIcon(new Vector2(x - 24f, r.y + h * 0.5f), 34f);
            x -= 70f;
            string co = d.coins.ToString("N0");
            float ow = st.CalcSize(new GUIContent(co)).x;
            x -= ow;
            GUI.Label(new Rect(x, r.y, ow + 4f, h), co, st);
            CoinIcon(new Vector2(x - 24f, r.y + h * 0.5f), 34f);
        }

        float TextRes(float right, float y, float h, string label, string value, Color c, GUIStyle st)
        {
            string text = label + " <size=26>" + value + "</size>";
            float w = st.CalcSize(new GUIContent(label + " " + value)).x + 6f;
            float x = right - w;
            UIStyles.Colored(new Rect(x, y, w + 10f, h), text, st, c);
            return x - 34f;
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
            if (r.coins > 0) sb.Append("<color=#FFD36B>Gold +" + r.coins.ToString("N0") + "</color>    ");
            if (r.crystals > 0) sb.Append("<color=#7FD8FF>Diamonds +" + r.crystals + "</color>    ");
            if (r.XpValue > 0) sb.Append("XP +" + r.XpValue.ToString("N0") + "    ");
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

        /// <summary>Teams are three slayers: a leader, a vanguard and a support.</summary>
        const int TeamSize = 3;
        static readonly Role[] SlotRoles = { Role.DPS, Role.Tank, Role.Support };

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
