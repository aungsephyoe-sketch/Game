using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// The home screen, laid out like a premium anime action RPG lobby over the living night village:
    ///   top-left      player profile (portrait, level, name, EXP) and a compact shortcut column
    ///                 (Event, Notice, Ranking, Friends)
    ///   upper-left    the BLADE LEGENDS logo, then a large event banner carousel with indicator dots
    ///   top-right     gold, diamonds and XP counters, mail, gift and settings
    ///   centre-right  the featured slayer standing in the village (HomeStage) — the focal point
    ///   right         TEAM POWER panel with the three team portraits and a member list
    ///   bottom        navigation: a large PLAY button, then SUMMON, CHARACTERS, TEAM, MISSIONS, SHOP, INVENTORY
    /// Priority is kept clear: character first, then PLAY, logo, banner, team power, navigation, utilities.
    /// </summary>
    public partial class UIManager
    {
        int bannerIndex;
        float bannerSince = -1f, bannerFrom = -1f;
        int bannerPrev = -1;

        void DrawMainMenu()
        {
            var d = gm.Data;
            ShadeForReadability();
            HomeProfile(d);
            HomeShortcuts(d);
            HomeLogo();
            HomeBanner(d);
            HomeResources(d);
            HomeTeamPower(d);
            HomeHeroDrag();
            HomeNav(d);
            DrawNpcBubbles();
        }

        /// <summary>Soft gradients at the left, top and bottom so the UI reads over the scene, leaving the hero clear.</summary>
        void ShadeForReadability()
        {
            for (int i = 0; i < 12; i++)
            {
                float f = i / 12f;
                UIStyles.Rect(new Rect(0f, 0f, safe.x + 940f * (1f - f), H), new Color(0.02f, 0.02f, 0.07f, 0.035f));
                UIStyles.Rect(new Rect(0f, 0f, W, 150f * (1f - f)), new Color(0.02f, 0.02f, 0.07f, 0.03f));
                UIStyles.Rect(new Rect(0f, H - 300f * (1f - f), W, 300f * (1f - f)), new Color(0.02f, 0.02f, 0.07f, 0.045f));
            }
        }

        /// <summary>Account level from overall progress (missions, bosses, demons, summons).</summary>
        static int AccountLevel(PlayerData d, out float frac)
        {
            float exp = d.missionsCleared * 120f + d.bossesDefeated * 400f + d.totalKills * 2f + d.totalSummons * 10f;
            float lv = Mathf.Sqrt(exp / 60f);
            frac = lv - Mathf.Floor(lv);
            return 1 + Mathf.FloorToInt(lv);
        }

        void HomeProfile(PlayerData d)
        {
            float k = Enter(0f, 0.4f);
            float x0 = safe.x + 24f, y0 = safe.y + 14f - (1f - k) * 40f;
            string leaderId = d.team.Count > 0 ? d.team[0] : GameDatabase.Protagonist;
            var leader = GameDatabase.GetCharacter(leaderId);
            var c = new Vector2(x0 + 52f, y0 + 52f);
            UIStyles.CircleTex(c, 54f, new Color(0f, 0f, 0f, 0.35f));
            if (leader != null) FaceCircle(c, 46f, leader, new Color(1f, 0.85f, 0.45f));
            float frac;
            int lv = AccountLevel(d, out frac);
            string name = string.IsNullOrEmpty(d.playerName) ? "Slayer" : d.playerName;
            UIStyles.Outlined(new Rect(x0 + 114f, y0 + 6f, 260f, 38f), "Lv. " + lv, UIStyles.Sized(UIStyles.H2, 30), Color.white, 2f);
            UIStyles.Outlined(new Rect(x0 + 114f, y0 + 40f, 260f, 34f), name, UIStyles.Sized(UIStyles.Body, 24), new Color(0.92f, 0.94f, 1f), 2f);
            var bar = new Rect(x0 + 114f, y0 + 80f, 220f, 11f);
            Round(bar, new Color(0f, 0f, 0f, 0.55f), 6f);
            if (frac > 0.01f) Round(new Rect(bar.x, bar.y, Mathf.Max(11f, bar.width * frac), bar.height), new Color(0.35f, 0.75f, 1f), 6f);
            Round(new Rect(bar.x, bar.y, Mathf.Max(11f, bar.width * frac), bar.height * 0.45f), new Color(1f, 1f, 1f, 0.3f), 6f);
            // Tap the profile to open it (name, gamer code, medals).
            var hit = new Rect(x0, y0, 380f, 104f);
            if (hit.Contains(Event.current.mousePosition)) RoundFrame(hit, new Color(1f, 1f, 1f, 0.15f), 2f, 16f);
            if (GUI.Button(hit, GUIContent.none, GUIStyle.none)) OpenSocial(SocialPanel.Profile);
        }

        void HomeShortcuts(PlayerData d)
        {
            float x = safe.x + 34f, y = safe.y + 150f;
            string[] labels = { "EVENT", "NOTICE", "RANKING", "FRIENDS" };
            string[] icons = { "sun", "scroll", "up", "people" };
            for (int i = 0; i < labels.Length; i++)
            {
                float k = Enter(0.08f + i * 0.05f, 0.35f);
                if (k <= 0f) continue;
                var c = new Vector2(x + 38f - (1f - k) * 60f, y + i * 104f + 36f);
                var hit = new Rect(c.x - 40f, c.y - 40f, 80f, 100f);
                if (i == 3) tutTargets["friends"] = hit;
                bool hover = hit.Contains(Event.current.mousePosition);
                UIStyles.CircleTex(c, 37f, new Color(0f, 0f, 0f, 0.3f));
                UIStyles.CircleTex(c, 34f, hover ? new Color(0.2f, 0.22f, 0.34f, 0.92f) : new Color(0.08f, 0.09f, 0.16f, 0.82f));
                if (i == 0) UIStyles.CircleTex(c, 34f, new Color(1f, 0.55f, 0.2f, 0.18f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f)));
                GUI.DrawTexture(new Rect(c.x - 20f, c.y - 20f, 40f, 40f), IconFactory.Get(icons[i]), ScaleMode.ScaleToFit, true);
                UIStyles.Outlined(new Rect(c.x - 60f, c.y + 36f, 120f, 24f), labels[i], UIStyles.Sized(UIStyles.Center, 16), Color.white, 1.5f);
                if (i == 3)
                {
                    int unread = SocialSystem.Unread(d);
                    if (unread > 0) Badge(new Vector2(c.x + 26f, c.y - 26f), unread > 9 ? "9+" : unread.ToString(), UIStyles.Crimson, 0.8f);
                }
                if (i == 0)
                {
                    int badge = EventBadge(d);
                    if (badge > 0) Badge(new Vector2(c.x + 26f, c.y - 26f), badge > 9 ? "!" : badge.ToString(), UIStyles.Crimson, 0.8f);
                }
                if (GUI.Button(hit, GUIContent.none, GUIStyle.none))
                {
                    gm.Audio.Play("click", 0.5f);
                    switch (i)
                    {
                        case 0: gm.GoTo(GameScreen.Events); break;
                        case 1: gm.GoTo(GameScreen.Story); break;
                        case 2: Toast("Rankings open with the next season."); break;
                        default: OpenSocial(SocialPanel.Friends); break;
                    }
                }
            }
        }

        /// <summary>The title treatment: a red brush stroke, BLADE large, LEGENDS in red, a spaced subtitle.</summary>
        void HomeLogo()
        {
            float k = Enter(0.05f, 0.6f);
            float x = safe.x + 150f, y = safe.y + 108f - (1f - k) * 50f;
            var old = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, k);
            var saved = GUI.matrix;
            // Brush stroke behind CHRONICLES.
            RotateGui(-3f, new Vector2(x + 300f, y + 150f));
            Round(new Rect(x + 40f, y + 118f, 520f, 58f), new Color(0.55f, 0.04f, 0.06f, 0.55f), 28f);
            Round(new Rect(x + 90f, y + 130f, 470f, 36f), new Color(0.75f, 0.08f, 0.1f, 0.5f), 18f);
            GUI.matrix = saved;
            RotateGui(-4f, new Vector2(x, y + 60f));
            UIStyles.Outlined(new Rect(x + 3f, y + 4f, 900f, 120f), "BLADE", UIStyles.Sized(UIStyles.Title, 110), new Color(0f, 0f, 0f, 0.6f), 0f);
            UIStyles.Outlined(new Rect(x, y, 900f, 120f), "BLADE", UIStyles.Sized(UIStyles.Title, 110), Color.white, 5f);
            UIStyles.Outlined(new Rect(x + 60f, y + 96f, 900f, 96f), "LEGENDS", UIStyles.Sized(UIStyles.Title, 80), new Color(0.95f, 0.16f, 0.16f), 5f);
            GUI.matrix = saved;
            Round(new Rect(x + 60f, y + 206f, 70f, 2f), new Color(1f, 1f, 1f, 0.7f), 1f);
            Round(new Rect(x + 430f, y + 206f, 70f, 2f), new Color(1f, 1f, 1f, 0.7f), 1f);
            UIStyles.Outlined(new Rect(x + 130f, y + 192f, 300f, 30f), "S L A Y E R   R P G", UIStyles.Sized(UIStyles.Center, 18), new Color(0.95f, 0.92f, 0.85f), 1.5f);
            GUI.color = old;
        }

        struct BannerSlide
        {
            public string tag, title, sub;
            public Color accent;
            public Texture art;
            public System.Action<Rect> picture;
            public System.Action open;
        }

        List<BannerSlide> BannerSlides(PlayerData d)
        {
            var list = new List<BannerSlide>();
            var events = GameDatabase.Events;
            if (events.Count > 0)
            {
                var ev = events[0];
                var enemy = string.IsNullOrEmpty(ev.bannerEnemy) ? null : GameDatabase.GetEnemy(ev.bannerEnemy);
                list.Add(new BannerSlide { tag = "LIMITED EVENT", title = ev.title, sub = ev.subtitle, accent = ev.accent,
                    art = enemy != null ? PortraitStudio.Enemy(enemy) : null, open = () => gm.GoTo(GameScreen.Events) });
            }
            var ids = SummonSystem.FeaturedIds;
            if (ids.Length > 0)
            {
                var f = GameDatabase.GetCharacter(ids[0]);
                if (f != null)
                    list.Add(new BannerSlide { tag = "LIMITED SUMMON", title = f.displayName + " — " + f.versionTitle, sub = "Featured Mythic · rate up", accent = new Color(0.75f, 0.4f, 1f),
                        art = ArtLibrary.CharacterFull(f), open = () => gm.GoTo(GameScreen.Summon) });
            }
            list.Add(new BannerSlide { tag = "VILLAGE HUB", title = "Kiriha Village", sub = "Chat, team up in threes and brave the co-op gates (online preview)", accent = new Color(0.35f, 0.85f, 0.55f),
                art = TileArt.Village, open = () => gm.BeginMission(GameDatabase.OpenWorld) });
            list.Add(new BannerSlide { tag = "FORGE", title = "Upgrade Your Gear", sub = "Swords, haori and charms for every slayer", accent = new Color(1f, 0.7f, 0.25f),
                picture = PicUpgrade, open = () => OpenMenu("EQUIPMENT") });
            return list;
        }

        void HomeBanner(PlayerData d)
        {
            var slides = BannerSlides(d);
            if (slides.Count == 0) return;
            float k = Enter(0.15f, 0.5f);
            var r = new Rect(safe.x + 150f - (1f - k) * 80f, safe.y + 352f, 640f, 210f);
            if (bannerSince < 0f) bannerSince = Time.unscaledTime;
            if (Time.unscaledTime - bannerSince > 5.5f) SetBanner((bannerIndex + 1) % slides.Count);
            bannerIndex = Mathf.Clamp(bannerIndex, 0, slides.Count - 1);

            // Swipe: drag the banner left or right to change events; a tap opens the one showing.
            var ev = Event.current;
            if (GUI.enabled && ev.type == EventType.MouseDown && r.Contains(ev.mousePosition)) { bannerDragging = true; bannerDrag = 0f; bannerDragLast = ev.mousePosition.x; ev.Use(); }
            else if (bannerDragging && ev.type == EventType.MouseDrag) { bannerDrag += ev.mousePosition.x - bannerDragLast; bannerDragLast = ev.mousePosition.x; bannerSince = Time.unscaledTime; ev.Use(); }
            else if (bannerDragging && ev.type == EventType.MouseUp)
            {
                bannerDragging = false;
                ev.Use();
                if (bannerDrag < -70f) SetBanner((bannerIndex + 1) % slides.Count, 1);
                else if (bannerDrag > 70f) SetBanner((bannerIndex - 1 + slides.Count) % slides.Count, -1);
                else if (Mathf.Abs(bannerDrag) < 10f && slides[bannerIndex].open != null) { bannerDrag = 0f; gm.Audio.Play("click", 0.5f); slides[bannerIndex].open(); return; }
                bannerDrag = 0f;
            }

            Round(Offset(r, 0f, 6f), new Color(0f, 0f, 0f, 0.4f), 14f);
            GUI.BeginGroup(r);
            var local = new Rect(0f, 0f, r.width, r.height);
            // Slide transition: the new slide eases in from the side it was swiped from; while dragging it follows the finger.
            float tk = bannerFrom < 0f ? 1f : Mathf.Clamp01((Time.unscaledTime - bannerFrom) / 0.4f);
            tk = tk * tk * (3f - 2f * tk);
            float drag = bannerDragging ? Mathf.Clamp(bannerDrag, -r.width, r.width) : 0f;
            if (tk < 1f && bannerPrev >= 0 && bannerPrev < slides.Count) DrawSlide(Offset(local, -tk * r.width * bannerDir, 0f), slides[bannerPrev]);
            DrawSlide(Offset(local, (1f - tk) * r.width * bannerDir + drag, 0f), slides[bannerIndex]);
            if (Mathf.Abs(drag) > 1f)
            {
                int nb = drag < 0f ? (bannerIndex + 1) % slides.Count : (bannerIndex - 1 + slides.Count) % slides.Count;
                DrawSlide(Offset(local, drag + (drag < 0f ? r.width : -r.width), 0f), slides[nb]);
            }
            GUI.EndGroup();
            RoundFrame(r, new Color(1f, 1f, 1f, 0.35f), 2f, 14f);
            // Indicator dots (tap to jump).
            float dw = slides.Count * 26f;
            for (int i = 0; i < slides.Count; i++)
            {
                bool on = i == bannerIndex;
                var dot = new Rect(r.center.x - dw * 0.5f + i * 26f + (on ? 0f : 4f), r.yMax + 12f, on ? 20f : 12f, 12f);
                Round(dot, on ? Color.white : new Color(1f, 1f, 1f, 0.35f), 6f);
                if (GUI.Button(Grow(dot, 6f), GUIContent.none, GUIStyle.none)) SetBanner(i);
            }
        }

        bool bannerDragging;
        float bannerDrag, bannerDragLast;
        int bannerDir = 1;

        void SetBanner(int i, int dir = 1)
        {
            if (i == bannerIndex) return;
            bannerDir = dir;
            bannerPrev = bannerIndex;
            bannerIndex = i;
            bannerFrom = Time.unscaledTime;
            bannerSince = Time.unscaledTime;
        }

        void DrawSlide(Rect r, BannerSlide s)
        {
            Color a = s.accent;
            Round(r, Color.Lerp(new Color(0.06f, 0.05f, 0.1f), a, 0.28f), 14f);
            for (int i = 0; i < 8; i++)
                Round(new Rect(r.x + r.width * (0.42f + i * 0.07f), r.y, r.width * 0.07f + 2f, r.height), new Color(a.r, a.g, a.b, 0.05f + i * 0.02f), 0f);
            var artR = new Rect(r.x + r.width * 0.5f, r.y, r.width * 0.5f, r.height);
            if (s.art != null) GUI.DrawTexture(artR, s.art, ScaleMode.ScaleAndCrop, true);
            else if (s.picture != null) s.picture(new Rect(artR.x + 20f, artR.y + 20f, artR.width - 40f, artR.height - 40f));
            // Fade the art into the text side.
            for (int i = 0; i < 10; i++)
                UIStyles.Rect(new Rect(artR.x + i * 12f, r.y, 12f, r.height), new Color(0.05f, 0.04f, 0.09f, 0.55f * (1f - i / 10f)));
            var tagR = new Rect(r.x + 22f, r.y + 20f, 180f, 30f);
            Round(tagR, a, 8f);
            UIStyles.Outlined(tagR, s.tag, UIStyles.Sized(UIStyles.Center, 16), Color.white, 1.5f);
            UIStyles.Outlined(new Rect(r.x + 22f, r.y + 58f, r.width * 0.56f, 90f), s.title, new GUIStyle(UIStyles.Sized(UIStyles.H2, 34)) { wordWrap = true }, Color.white, 2.5f);
            GUI.Label(new Rect(r.x + 24f, r.y + r.height - 58f, r.width * 0.52f, 50f), "<color=#E6E0FF>" + s.sub + "</color>", new GUIStyle(UIStyles.Sized(UIStyles.Small, 16)) { wordWrap = true });
        }

        void HomeResources(PlayerData d)
        {
            float k = Enter(0.1f, 0.4f);
            float y = safe.y + 20f - (1f - k) * 40f;
            float px = safe.xMax - 24f;
            // Utility icons, far right: settings, gift, mail.
            if (IconButton(new Rect(px - 60f, y, 60f, 56f), IconFactory.Get("gear"))) OpenMenu("SETTINGS");
            var gift = new Rect(px - 128f, y, 60f, 56f);
            if (IconButton(gift, IconFactory.Get("heart"))) OpenMenu("SHOP");
            if (MenuBadge("SHOP") > 0) Badge(new Vector2(gift.xMax - 6f, gift.y + 6f), "!", UIStyles.Crimson, 0.7f);
            var mail = new Rect(px - 196f, y, 60f, 56f);
            if (IconButton(mail, IconFactory.Get("mail"))) OpenMenu("MISSIONS");
            int mb = MenuBadge("MISSIONS");
            if (mb > 0) Badge(new Vector2(mail.xMax - 6f, mail.y + 6f), mb > 9 ? "!" : mb.ToString(), UIStyles.Crimson, 0.7f);
            // Currency counters.
            PlusPill(new Rect(px - 430f, y, 220f, 56f), 2, d.xp.ToString("N0"), () => OpenMenu("SHOP"));
            PlusPill(new Rect(px - 664f, y, 220f, 56f), 1, d.crystals.ToString("N0"), () => OpenMenu("SHOP"));
            PlusPill(new Rect(px - 898f, y, 220f, 56f), 0, d.coins.ToString("N0"), () => OpenMenu("SHOP"));
        }

        void HomeTeamPower(PlayerData d)
        {
            float k = Enter(0.35f, 0.5f);
            int n = Mathf.Min(3, d.team.Count);
            var panel = new Rect(safe.xMax - 440f + (1f - k) * 300f, safe.y + 250f, 416f, 150f + n * 44f + 10f);
            Round(Offset(panel, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
            Round(panel, new Color(0.04f, 0.05f, 0.1f, 0.8f), 16f);
            RoundFrame(panel, new Color(1f, 1f, 1f, 0.14f), 2f, 16f);
            GUI.DrawTexture(new Rect(panel.x + 18f, panel.y + 14f, 34f, 34f), IconFactory.Get("swords"), ScaleMode.ScaleToFit, true);
            UIStyles.Outlined(new Rect(panel.x + 60f, panel.y + 12f, 220f, 40f), "TEAM POWER", UIStyles.Sized(UIStyles.Body, 24), Color.white, 1.5f);
            UIStyles.Outlined(new Rect(panel.x + 200f, panel.y + 6f, 198f, 48f), CharacterSystem.TeamPower(d).ToString("N0"), UIStyles.Sized(UIStyles.Right, 38), new Color(1f, 0.82f, 0.3f), 2f);
            // Portraits.
            for (int i = 0; i < n; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                var pr = new Rect(panel.x + 18f + i * 130f, panel.y + 58f, 118f, 76f);
                Color ec = ElementChart.ColorOf(def.element);
                Round(pr, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), ec, 0.25f), 10f);
                var tex = ArtLibrary.Character(def);
                if (tex != null) GUI.DrawTexture(new Rect(pr.x + 3f, pr.y + 3f, pr.width - 6f, pr.height - 6f), tex, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, 8f);
                RoundFrame(pr, ec, 2f, 10f);
                Round(new Rect(pr.x, pr.yMax - 22f, pr.width, 22f), new Color(0f, 0f, 0f, 0.55f), 8f);
                UIStyles.Outlined(new Rect(pr.x, pr.yMax - 24f, pr.width, 24f), "Lv. " + c.level, UIStyles.Sized(UIStyles.Center, 15), Color.white, 1.5f);
            }
            // Member list.
            UIStyles.Rect(new Rect(panel.x + 18f, panel.y + 142f, panel.width - 36f, 1f), new Color(1f, 1f, 1f, 0.12f));
            for (int i = 0; i < n; i++)
            {
                var c = d.GetCharacter(d.team[i]);
                var def = GameDatabase.GetCharacter(d.team[i]);
                if (c == null || def == null) continue;
                float ry = panel.y + 150f + i * 44f;
                Color ec = ElementChart.ColorOf(def.element);
                var o2 = GUI.color;
                GUI.color = ec;
                GUI.DrawTexture(new Rect(panel.x + 20f, ry + 8f, 26f, 26f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
                GUI.color = o2;
                GUI.Label(new Rect(panel.x + 58f, ry, 170f, 42f), def.displayName, UIStyles.Sized(UIStyles.Body, 22));
                GUI.Label(new Rect(panel.x + 210f, ry, 90f, 42f), "Lv. " + c.level, UIStyles.Sized(UIStyles.Body, 20));
                UIStyles.Colored(new Rect(panel.x + 290f, ry, 108f, 42f), new string('★', Mathf.Clamp(c.stars, 1, 7)), UIStyles.Sized(UIStyles.Right, 18), new Color(1f, 0.85f, 0.35f));
            }
            if (GUI.Button(panel, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("click", 0.5f); teamReturn = GameScreen.MainMenu; gm.GoTo(GameScreen.Team); }
        }

        /// <summary>Screen rects of buttons the tutorials point at (filled while drawing).</summary>
        readonly System.Collections.Generic.Dictionary<string, Rect> tutTargets = new System.Collections.Generic.Dictionary<string, Rect>();

        void HomeNav(PlayerData d)
        {
            float bottom = H - 22f;
            // PLAY: huge, bright and bouncy — the first thing you see.
            float pw = Mathf.Min(560f, W * 0.3f);
            var play = new Rect(safe.x + 22f, bottom - 206f, pw, 206f);
            float kp = Enter(0.2f, 0.45f);
            play = Offset(play, 0f, (1f - kp) * 140f);
            float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.6f);
            // Idle "breathing" so it always invites a tap.
            float breathe = 1f + 0.018f * Mathf.Sin(Time.unscaledTime * 3.2f);
            var pb = new Rect(play.center.x - play.width * 0.5f * breathe, play.center.y - play.height * 0.5f * breathe, play.width * breathe, play.height * breathe);
            Round(Grow(pb, 10f + pulse * 6f), new Color(1f, 0.75f, 0.1f, 0.14f + 0.12f * pulse), 34f);
            bool hp;
            var face = ChunkyBody(pb, new Color(1f, 0.74f, 0.08f), true, 26f, out hp);
            Gradient(new Rect(face.x + 4f, face.y + 4f, face.width - 8f, face.height - 8f), new Color(1f, 0.86f, 0.25f), new Color(1f, 0.55f, 0.05f), 22f);
            Round(new Rect(face.x + 12f, face.y + 9f, face.width - 24f, face.height * 0.3f), new Color(1f, 1f, 1f, 0.3f), 16f);
            GUI.DrawTexture(new Rect(face.x + 26f, face.center.y - 60f, 120f, 120f), IconFactory.Get("swords"), ScaleMode.ScaleToFit, true);
            UIStyles.Outlined(new Rect(face.x + 150f, face.center.y - 66f, face.width - 160f, 100f), "PLAY", UIStyles.Sized(UIStyles.Title, 96), Color.white, 5f);
            UIStyles.Outlined(new Rect(face.x + 156f, face.center.y + 34f, face.width - 160f, 34f), "M A I N   S T O R Y", UIStyles.Sized(UIStyles.Body, 19), new Color(1f, 0.97f, 0.85f), 2f);
            tutTargets["play"] = pb;
            if (GUI.Button(pb, GUIContent.none, GUIStyle.none) && kp > 0.6f) { Bounce(play); gm.Audio.Play("click", 0.6f); gm.GoTo(GameScreen.WorldMap); return; }

            // Secondary buttons: chunky, each its own bright colour, icon on top.
            string[] labels = { "SUMMON", "CHARACTERS", "TEAM", "MISSIONS", "SHOP", "INVENTORY" };
            string[] icons = { "flame", "people", "group", "scroll", "cart", "bag" };
            Color[] cols =
            {
                new Color(0.62f, 0.3f, 1f), new Color(0.16f, 0.52f, 1f), new Color(0.12f, 0.78f, 0.38f),
                new Color(0.95f, 0.2f, 0.26f), new Color(1f, 0.62f, 0.1f), new Color(0.42f, 0.36f, 0.95f)
            };
            float gap = 16f;
            float x0 = play.xMax + 26f;
            float avail = safe.xMax - 22f - x0;
            float bw = Mathf.Clamp((avail - gap * 5f) / 6f, 110f, 186f);
            float bh = 156f;
            for (int i = 0; i < labels.Length; i++)
            {
                float k = Enter(0.28f + i * 0.04f, 0.4f);
                if (k <= 0f) continue;
                var r = new Rect(x0 + i * (bw + gap), bottom - bh + (1f - k) * 120f, bw, bh);
                bool hover;
                var f = ChunkyBody(r, cols[i], true, 20f, out hover);
                float ic = Mathf.Min(78f, f.width * 0.55f);
                float bob = hover ? Mathf.Sin(Time.unscaledTime * 10f) * 3f : 0f;
                GUI.DrawTexture(new Rect(f.center.x - ic * 0.5f, f.y + 14f + bob, ic, ic), IconFactory.Get(icons[i]), ScaleMode.ScaleToFit, true);
                UIStyles.Outlined(new Rect(f.x, f.yMax - 46f, f.width, 36f), labels[i], UIStyles.Sized(UIStyles.Center, bw < 150f ? 18 : 22), Color.white, 3f);
                int badge = MenuBadge(labels[i]);
                if (badge > 0) Badge(new Vector2(f.xMax - 8f, f.y + 8f), badge > 9 ? "!" : badge.ToString(), UIStyles.Crimson, 0.85f);
                tutTargets[labels[i].ToLowerInvariant()] = r;
                if (GUI.Button(r, GUIContent.none, GUIStyle.none) && k > 0.6f)
                {
                    Bounce(r);
                    gm.Audio.Play("click", 0.5f);
                    if (labels[i] == "INVENTORY") gm.GoTo(GameScreen.Inventory);
                    else OpenMenu(labels[i]);
                    return;
                }
            }
        }

        bool heroDragging;
        float heroDragLast;

        /// <summary>Drag the featured slayer to turn them around (they ease back to facing you after a while).</summary>
        void HomeHeroDrag()
        {
            var cam = Camera.main;
            if (!GUI.enabled) { heroDragging = false; return; }
            if (cam == null || gm.Home == null || !gm.Home.LeaderVisible) return;
            float s = HudLayout.Scale;
            Vector3 feet = cam.WorldToScreenPoint(gm.Home.LeaderFeet);
            Vector3 top = cam.WorldToScreenPoint(gm.Home.LeaderHead);
            if (feet.z <= 0f) return;
            var fp = new Vector2(feet.x / s, (Screen.height - feet.y) / s);
            var tp = new Vector2(top.x / s, (Screen.height - top.y) / s);
            var area = new Rect(fp.x - 150f, tp.y, 300f, fp.y - tp.y + 20f);
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && area.Contains(ev.mousePosition)) { heroDragging = true; heroDragLast = ev.mousePosition.x; ev.Use(); }
            else if (heroDragging && ev.type == EventType.MouseDrag)
            {
                gm.Home.RotateLeader(-(ev.mousePosition.x - heroDragLast) * 0.6f);
                heroDragLast = ev.mousePosition.x;
                ev.Use();
            }
            else if (heroDragging && ev.type == EventType.MouseUp) { heroDragging = false; ev.Use(); }
        }

        /// <summary>A rounded vertical gradient (top colour to bottom colour).</summary>
        static void Gradient(Rect r, Color top, Color bottom, float radius)
        {
            Round(r, bottom, radius);
            const int steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float f = i / (float)steps;
                var band = new Rect(r.x, r.y, r.width, r.height * (1f - f));
                var c = Color.Lerp(bottom, top, 1f - f);
                c.a = 0.35f;
                Round(band, c, radius);
            }
        }
    }
}
