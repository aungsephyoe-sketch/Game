using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Summon shrine, missions board, shop, in-engine cutscene overlay and the credits roll.</summary>
    public partial class UIManager
    {
        // ------------------------------------------------------------------ Summon

        void DrawSummon()
        {
            var d = gm.Data;
            var st = gm.SummonHall;
            switch (st.Phase)
            {
                case SummonStage.SummonPhase.Idle: DrawSummonBanner(d, st); break;
                case SummonStage.SummonPhase.Summary: DrawSummonSummary(d, st); break;
                default: DrawSummonReveal(st); break;
            }
        }

        void DrawSummonBanner(PlayerData d, SummonStage st)
        {
            TopBar("SUMMON", GameScreen.MainMenu);
            var featured = GameDatabase.GetCharacter(SummonSystem.FeaturedId);
            float k = Enter(0f, 0.45f);
            var banner = new Rect(safe.x + 30f - (1f - k) * 300f, safe.y + 140f, 720f, H - safe.y - 170f);
            UIStyles.PanelBox(banner, RarityInfo.Color(7));
            GUI.Label(new Rect(banner.x + 30f, banner.y + 20f, 660f, 40f), "<color=#FF6B8A>LIMITED BANNER</color>", UIStyles.H2);
            UIStyles.Outlined(new Rect(banner.x + 30f, banner.y + 62f, 660f, 80f), "ECLIPSE OF THE MOON", UIStyles.Sized(UIStyles.H1, 50), Color.white, 3f);
            if (featured != null)
            {
                GUI.Label(new Rect(banner.x + 30f, banner.y + 150f, 660f, 44f), "Featured: <color=#FF6B8A>" + featured.FullName + "</color>  " + ElementTag(featured.element), UIStyles.Body);
                GUI.Label(new Rect(banner.x + 30f, banner.y + 196f, 660f, 90f), "<i>" + featured.description + "</i>", UIStyles.Small);
            }
            float y = banner.y + 300f;
            GUI.Label(new Rect(banner.x + 30f, y, 660f, 40f), "RATES", UIStyles.Sized(UIStyles.H2, 28));
            y += 42f;
            for (int i = 0; i < SummonSystem.Rates.Length; i++)
            {
                int rarity = 3 + i;
                UIStyles.Colored(new Rect(banner.x + 40f, y, 300f, 34f), RarityInfo.Name(rarity) + "  " + Stars(rarity), UIStyles.Sized(UIStyles.Body, 22), RarityInfo.Color(rarity));
                GUI.Label(new Rect(banner.x + 400f, y, 200f, 34f), (SummonSystem.Rates[i] * 100f).ToString("0.#") + "%", UIStyles.Sized(UIStyles.Body, 22));
                y += 34f;
            }
            y += 14f;
            GUI.Label(new Rect(banner.x + 30f, y, 660f, 70f), "Every ×10 summon guarantees an EPIC or better.\nPity: the featured MYTHIC is guaranteed within " + SummonSystem.PityLimit + " summons.", UIStyles.Sized(UIStyles.Small, 20));
            y += 80f;
            GUI.Label(new Rect(banner.x + 30f, y, 660f, 30f), "Pity  " + d.summonPity + " / " + SummonSystem.PityLimit, UIStyles.Sized(UIStyles.Body, 22));
            UIStyles.Bar(new Rect(banner.x + 30f, y + 34f, 660f, 16f), (float)d.summonPity / SummonSystem.PityLimit, RarityInfo.Color(7));
            GUI.Label(new Rect(banner.x + 30f, banner.yMax - 60f, 660f, 40f), "Duplicates become Ascension Ore.", UIStyles.Sized(UIStyles.Small, 20));

            float bw = 420f, bx = safe.xMax - bw - 40f, by = H - 330f;
            if (AnimBtn(new Rect(bx, by, bw, 120f), "SUMMON ×1\n<size=24>✦ " + SummonSystem.SingleCost + "</size>", UIStyles.Button, 0.2f, SummonSystem.CanAfford(d, 1), false, 60f))
                DoSummon(1);
            if (AnimBtn(new Rect(bx, by + 140f, bw, 140f), "SUMMON ×10\n<size=24>✦ " + SummonSystem.MultiCost + "</size>", UIStyles.ButtonBig, 0.3f, SummonSystem.CanAfford(d, 10), SummonSystem.CanAfford(d, 10), 60f))
                DoSummon(10);
            if (!SummonSystem.CanAfford(d, 1))
                GUI.Label(new Rect(bx - 200f, by - 60f, bw + 200f, 50f), "<color=#FF9C7A>Earn crystals from missions, objectives, daily missions and the shop.</color>", UIStyles.Sized(UIStyles.Right, 20));
        }

        void DoSummon(int count)
        {
            var results = SummonSystem.Summon(gm.Data, count);
            if (results.Count == 0) { Toast("Not enough crystals."); return; }
            gm.Save();
            gm.SummonHall.Play(results);
        }

        void DrawSummonReveal(SummonStage st)
        {
            var r = st.Current;
            if (Btn(new Rect(safe.xMax - 250f, safe.y + 24f, 220f, 80f), "SKIP ▶▶", UIStyles.ButtonSmall)) st.SkipAll();
            if (st.Results != null && st.Results.Count > 1)
                GUI.Label(new Rect(safe.x + 30f, safe.y + 30f, 300f, 60f), (st.CurrentIndex + 1) + " / " + st.Results.Count, UIStyles.H2);
            if (GUI.Button(new Rect(0f, 0f, W, H), GUIContent.none, GUIStyle.none)) st.Advance();
            if (r == null) return;

            if (st.Phase == SummonStage.SummonPhase.Charging || st.Phase == SummonStage.SummonPhase.Silhouette)
            {
                // Glow in the circle colour creeps in from the edges.
                var c = st.CircleColor;
                var old = GUI.color;
                GUI.color = new Color(c.r, c.g, c.b, 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f));
                GUI.DrawTexture(new Rect(0f, 0f, W, H), UIStyles.Vignette);
                GUI.color = old;
                if (st.Phase == SummonStage.SummonPhase.Silhouette)
                    GUI.Label(new Rect(0f, H - 200f, W, 60f), "<color=#DDDDDD>. . .</color>", UIStyles.Sized(UIStyles.Center, 48));
                return;
            }

            // Reveal card.
            float k = Mathf.Clamp01((Time.unscaledTime - st.PhaseStart) / 0.4f);
            Color rc = RarityInfo.Color(r.rarity);
            if (k < 1f) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(1f, 1f, 1f, (1f - k) * 0.8f));
            var card = new Rect(safe.xMax - 820f + (1f - k) * 400f, H * 0.5f - 200f, 760f, 400f);
            UIStyles.Rect(card, new Color(0f, 0f, 0f, 0.6f * k));
            var portrait = ArtLibrary.CharacterFull(r.def);
            if (portrait != null)
            {
                // The character's master art slides in beside the name card.
                var pr = new Rect(card.x - 330f - (1f - k) * 200f, H * 0.5f - 330f, 320f, 660f);
                ArtLibrary.DrawFit(pr, portrait, k);
            }
            UIStyles.Rect(new Rect(card.x, card.y, 12f, card.height), rc);
            UIStyles.Outlined(new Rect(card.x + 40f, card.y + 20f, 700f, 80f), RarityInfo.Name(r.rarity), UIStyles.Sized(UIStyles.Big, 64), rc, 3f);
            UIStyles.Colored(new Rect(card.x + 40f, card.y + 100f, 700f, 50f), Stars(r.rarity), UIStyles.Sized(UIStyles.H2, 40), UIStyles.Gold);
            UIStyles.Outlined(new Rect(card.x + 40f, card.y + 150f, 700f, 80f), r.def.displayName, UIStyles.Sized(UIStyles.H1, 60), Color.white, 3f);
            GUI.Label(new Rect(card.x + 40f, card.y + 228f, 700f, 44f), r.def.versionTitle + "   " + ElementTag(r.def.element) + "  " + r.def.role, UIStyles.Body);
            GUI.Label(new Rect(card.x + 40f, card.y + 276f, 700f, 80f), "<i>" + r.def.breathingStyle + "</i>", UIStyles.Small);
            if (r.isNew) UIStyles.Outlined(new Rect(card.xMax - 200f, card.y + 20f, 180f, 70f), "NEW!", UIStyles.Sized(UIStyles.Big, 54), UIStyles.Good, 3f);
            else GUI.Label(new Rect(card.xMax - 330f, card.y + 30f, 310f, 60f), "<color=#FF9C7A>DUPLICATE → Ore</color>", UIStyles.Sized(UIStyles.Right, 24));
            GUI.Label(new Rect(0f, H - 90f, W, 50f), "<color=#BBBBBB>Tap to continue</color>", UIStyles.Sized(UIStyles.Center, 26));
        }

        void DrawSummonSummary(PlayerData d, SummonStage st)
        {
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.35f));
            UIStyles.Outlined(new Rect(0f, safe.y + 30f, W, 90f), "SUMMON RESULTS", UIStyles.Sized(UIStyles.H1, 60), UIStyles.Gold, 3f);
            var res = st.Results;
            if (res == null) return;
            int cols = Mathf.Min(5, res.Count);
            float cw = 300f, ch = 170f, gap = 20f;
            float startX = W * 0.5f - (cols * cw + (cols - 1) * gap) * 0.5f;
            float startY = res.Count > 5 ? 200f : 380f;
            for (int i = 0; i < res.Count; i++)
            {
                var r = res[i];
                float k = Mathf.Clamp01((Time.unscaledTime - st.PhaseStart - i * 0.06f) / 0.25f);
                if (k <= 0f) continue;
                var rect = new Rect(startX + (i % cols) * (cw + gap), startY + (i / cols) * (ch + gap) + (1f - k) * 40f, cw, ch);
                Color rc = RarityInfo.Color(r.rarity);
                Round(rect, new Color(0.08f, 0.07f, 0.13f, 0.92f * k), 14f);
                RoundFrame(rect, rc, r.rarity >= 6 ? 4f : 2f, 14f);
                var thumb = ArtLibrary.Character(r.def);
                var tr = new Rect(rect.x + 10f, rect.y + 20f, 130f, 130f);
                Round(tr, Color.Lerp(new Color(0.1f, 0.1f, 0.16f), ElementChart.ColorOf(r.def.element), 0.3f), 12f);
                if (thumb != null) GUI.DrawTexture(tr, thumb, ScaleMode.ScaleAndCrop, true);
                float tx = rect.x + 150f, tw = cw - 158f;
                UIStyles.Colored(new Rect(tx, rect.y + 14f, tw, 30f), RarityInfo.Name(r.rarity), UIStyles.Sized(UIStyles.Body, 20), rc);
                GUI.Label(new Rect(tx, rect.y + 44f, tw, 40f), r.def.displayName, UIStyles.Sized(UIStyles.H2, 26));
                GUI.Label(new Rect(tx, rect.y + 84f, tw, 30f), "<color=#AAAAAA>" + r.def.versionTitle + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                GUI.Label(new Rect(tx, rect.y + 118f, tw, 40f), r.isNew ? "<color=#7CFF8A>NEW!</color>" : "<color=#FF9C7A>+Ore</color>", UIStyles.Sized(UIStyles.Body, 24));
            }
            float by = H - 150f;
            if (Btn(new Rect(W * 0.5f - 470f, by, 440f, 100f), "OK")) st.CloseSummary();
            if (Btn(new Rect(W * 0.5f + 30f, by, 440f, 100f), res.Count >= 10 ? "SUMMON ×10 AGAIN" : "SUMMON AGAIN", UIStyles.ButtonBig, SummonSystem.CanAfford(d, res.Count)))
                DoSummon(res.Count);
        }

        // ------------------------------------------------------------------ Missions board

        int boardTab;
        Vector2 boardScroll;

        void DrawMissionsBoard()
        {
            var d = gm.Data;
            TopBar("MISSIONS", GameScreen.MainMenu);
            QuestSystem.EnsureReset(d);
            string[] tabs = { "DAILY", "WEEKLY", "SIDE STORIES", "EVENTS & TRAINING" };
            float tx = safe.x + 30f, ty = safe.y + 130f;
            for (int t = 0; t < tabs.Length; t++)
                if (FlatBtn(new Rect(tx + t * 330f, ty, 315f, 70f), tabs[t], boardTab == t ? TileMaroon : new Color(0.2f, 0.2f, 0.28f), true, 26)) { boardTab = t; boardScroll = Vector2.zero; }
            var body = new Rect(safe.x + 30f, ty + 90f, safe.width - 60f, H - ty - 120f);
            Round(body, new Color(0.06f, 0.06f, 0.12f, 0.88f), 18f);

            if (boardTab < 2)
            {
                bool daily = boardTab == 0;
                GUI.Label(new Rect(body.x + 30f, body.y + 14f, body.width - 60f, 36f), "<color=#AAAAAA>" + (daily ? "Resets every day (UTC)." : "Resets every week.") + "</color>", UIStyles.Small);
                var quests = new List<QuestSystem.QuestDef>();
                foreach (var def in QuestSystem.Defs) if (def.daily == daily) quests.Add(def);
                var qview = new Rect(body.x + 20f, body.y + 56f, body.width - 40f, body.height - 70f);
                var qcontent = new Rect(0f, 0f, qview.width - 24f, quests.Count * 122f);
                boardScroll = GUI.BeginScrollView(qview, boardScroll, qcontent);
                for (int i = 0; i < quests.Count; i++)
                {
                    var def = quests[i];
                    var s = QuestSystem.State(d, def.id);
                    var row = new Rect(0f, i * 122f, qcontent.width, 110f);
                    Round(row, new Color(0.14f, 0.13f, 0.22f, 0.95f), 14f);
                    // Icon disc.
                    Color ic = QuestColor(def.kind);
                    UIStyles.CircleTex(new Vector2(row.x + 60f, row.center.y), 40f, ic);
                    GUI.Label(new Rect(row.x + 20f, row.y + 15f, 80f, 80f), QuestIcon(def.kind), UIStyles.Sized(UIStyles.Center, 40));
                    GUI.Label(new Rect(row.x + 120f, row.y + 12f, 560f, 44f), def.title, UIStyles.Sized(UIStyles.H2, 30));
                    ProgressBar(new Rect(row.x + 120f, row.y + 62f, 460f, 30f), (float)s.progress / def.target, Mathf.Min(s.progress, def.target) + " / " + def.target);
                    RewardIcons(new Rect(row.x + 620f, row.y + 25f, row.width - 920f, 60f), def.reward);
                    var cb = new Rect(row.xMax - 260f, row.y + 20f, 230f, 70f);
                    if (s.claimed) FlatBtn(cb, "CLAIMED", new Color(0.22f, 0.66f, 0.36f), true, 28);
                    else if (s.progress >= def.target)
                    {
                        if (FlatBtn(cb, "CLAIM", TileRed, true, 30))
                        {
                            QuestSystem.Claim(d, def);
                            gm.Save();
                            gm.Audio.Play("perfect", 0.6f);
                            Toast("Claimed: " + RewardText(def.reward));
                        }
                    }
                    else FlatBtn(cb, "CLAIM", new Color(0.4f, 0.4f, 0.45f), false, 28);
                }
                GUI.EndScrollView();
                return;
            }

            var list = new List<MissionDefinition>();
            foreach (var m in GameDatabase.AllMissions())
            {
                if (boardTab == 2 && (m.type == MissionType.Side || m.type == MissionType.Treasure)) list.Add(m);
                if (boardTab == 3 && (m.type == MissionType.Event || m.type == MissionType.Training)) list.Add(m);
            }
            var view = new Rect(body.x + 20f, body.y + 20f, body.width - 40f, body.height - 40f);
            var content = new Rect(0f, 0f, view.width - 24f, list.Count * 124f);
            boardScroll = GUI.BeginScrollView(view, boardScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                bool unlocked = d.IsMissionUnlocked(m);
                bool cleared = d.IsMissionCleared(m.id);
                var row = new Rect(0f, i * 124f, content.width, 114f);
                Round(row, new Color(0.14f, 0.13f, 0.22f, 0.95f), 14f);
                GUI.Label(new Rect(row.x + 20f, row.y + 8f, row.width - 320f, 44f), TypeTag(m.type) + "  " + m.id + "  " + m.name + (cleared ? "   <color=#7CFF8A>✔</color>" : ""), UIStyles.H2);
                string giver = string.IsNullOrEmpty(m.questGiver) ? "" : "<color=#7FD8FF>" + m.questGiver + ":</color> ";
                GUI.Label(new Rect(row.x + 20f, row.y + 54f, row.width - 320f, 56f), giver + (unlocked ? m.storyText : "Locked — clear " + m.requiresMissionId + " first.") +
                    "  <color=#AAAAAA>" + RegionName(m.regionId) + "</color>", UIStyles.Sized(UIStyles.Small, 21));
                if (FlatBtn(new Rect(row.xMax - 270f, row.y + 20f, 250f, 74f), unlocked ? "GO" : "LOCKED", TileRed, unlocked, 30))
                {
                    gm.SelectedMission = m;
                    gm.GoTo(GameScreen.MissionDetail);
                }
            }
            GUI.EndScrollView();
        }

        static string QuestIcon(string kind)
        {
            switch (kind)
            {
                case "kill": return "⚔";
                case "boss": return "☠";
                case "upgrade": return "▲";
                case "summon": return "✦";
                default: return "✔";
            }
        }

        static Color QuestColor(string kind)
        {
            switch (kind)
            {
                case "kill": return TileRed;
                case "boss": return TileMaroon;
                case "upgrade": return TileGreen;
                case "summon": return TilePurple;
                default: return TileBlue;
            }
        }

        /// <summary>Rewards as small icon + amount chips.</summary>
        void RewardIcons(Rect r, RewardBundle rw)
        {
            float x = r.x;
            float h = Mathf.Min(r.height, 52f);
            if (rw.crystals > 0) { Pill(new Rect(x, r.y, 150f, h), "✦", new Color(0.3f, 0.65f, 1f), rw.crystals.ToString()); x += 160f; }
            if (rw.coins > 0) { Pill(new Rect(x, r.y, 170f, h), "◆", new Color(0.95f, 0.72f, 0.2f), rw.coins.ToString("N0")); x += 180f; }
            if (rw.expScrolls > 0) { Pill(new Rect(x, r.y, 120f, h), "✎", new Color(0.6f, 0.4f, 0.9f), "×" + rw.expScrolls); x += 130f; }
            if (rw.skillScrolls > 0) { Pill(new Rect(x, r.y, 120f, h), "✧", new Color(0.35f, 0.8f, 0.45f), "×" + rw.skillScrolls); x += 130f; }
            if (rw.ascensionOre > 0) { Pill(new Rect(x, r.y, 120f, h), "▲", new Color(0.9f, 0.45f, 0.3f), "×" + rw.ascensionOre); x += 130f; }
            if (rw.exp > 0 && x < r.xMax - 150f) Pill(new Rect(x, r.y, 150f, h), "★", new Color(0.8f, 0.8f, 0.3f), rw.exp.ToString("N0"));
        }

        // ------------------------------------------------------------------ Shop

        void DrawShop()
        {
            var d = gm.Data;
            TopBar("SOLMERE MARKET", GameScreen.MainMenu);
            float cw = 520f, ch = 250f, gap = 24f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((safe.width - 60f + gap) / (cw + gap)));
            float top = safe.y + 140f;
            for (int i = 0; i < ShopSystem.Items.Count; i++)
            {
                var it = ShopSystem.Items[i];
                float k = Enter(i * 0.05f, 0.3f);
                var r = new Rect(safe.x + 30f + (i % cols) * (cw + gap), top + (i / cols) * (ch + gap) + (1f - k) * 40f, cw, ch);
                UIStyles.PanelBox(r, it.dailyFree ? UIStyles.Good : it.crystalCost > 0 ? new Color(0.5f, 0.85f, 1f) : UIStyles.Gold);
                GUI.Label(new Rect(r.x + 24f, r.y + 16f, cw - 48f, 44f), it.name, UIStyles.H2);
                GUI.Label(new Rect(r.x + 24f, r.y + 64f, cw - 48f, 70f), it.description, UIStyles.Small);
                string cost = it.dailyFree ? "FREE" : it.crystalCost > 0 ? "<color=#7FD8FF>✦ " + it.crystalCost + "</color>" : "<color=#FFD36B>◆ " + it.coinCost.ToString("N0") + "</color>";
                bool can = ShopSystem.CanBuy(d, it);
                string label = it.dailyFree && !can ? "COLLECTED" : "BUY  " + cost;
                if (Btn(new Rect(r.x + 24f, r.yMax - 100f, cw - 48f, 80f), label, UIStyles.Button, can))
                {
                    string got = ShopSystem.Buy(d, it);
                    if (got != null)
                    {
                        gm.Save();
                        gm.Audio.Play("perfect", 0.6f);
                        Toast("Received: " + got);
                    }
                }
            }
        }

        // ------------------------------------------------------------------ Cutscene overlay

        static Color SpeakerColor(string speaker)
        {
            if (string.IsNullOrEmpty(speaker)) return Color.white;
            if (speaker.StartsWith("Ren")) return new Color(1f, 0.8f, 0.35f);
            if (speaker.StartsWith("Master")) return new Color(0.7f, 0.8f, 0.9f);
            if (speaker.StartsWith("Sora")) return new Color(0.45f, 0.75f, 1f);
            if (speaker.StartsWith("Kiba")) return new Color(0.8f, 0.65f, 1f);
            if (speaker.StartsWith("Hana")) return new Color(0.5f, 1f, 0.6f);
            if (speaker.StartsWith("Homura")) return new Color(1f, 0.55f, 0.2f);
            if (speaker.StartsWith("Tetsu")) return new Color(0.75f, 0.75f, 0.7f);
            if (speaker.StartsWith("Veyrath") || speaker.StartsWith("Goken") || speaker.StartsWith("Chancellor") || speaker.StartsWith("General") || speaker.StartsWith("Gorvath") || speaker.StartsWith("Morgrath"))
                return new Color(1f, 0.3f, 0.35f);
            if (speaker.StartsWith("Akatsuki")) return new Color(1f, 0.95f, 0.7f);
            return new Color(0.9f, 0.9f, 0.9f);
        }

        void DrawCutscene()
        {
            var cp = CutscenePlayer.Current;
            if (cp == null) return;
            if (cp.Letterbox)
            {
                UIStyles.Rect(new Rect(0f, 0f, W, 100f), Color.black);
                UIStyles.Rect(new Rect(0f, H - 100f, W, 100f), Color.black);
            }

            if (Btn(new Rect(safe.xMax - 210f, 18f, 190f, 64f), "SKIP ▶▶", UIStyles.ButtonSmall)) { cp.Skip(); return; }
            if (GUI.Button(new Rect(0f, 100f, W, H - 100f), GUIContent.none, GUIStyle.none)) cp.Advance();

            // Dialogue.
            if (!string.IsNullOrEmpty(cp.FullText))
            {
                var box = new Rect(W * 0.5f - 900f, H - 330f, 1800f, 220f);
                UIStyles.Rect(box, new Color(0.02f, 0.01f, 0.05f, 0.82f));
                Color sc = SpeakerColor(cp.Speaker);
                UIStyles.Rect(new Rect(box.x, box.y, box.width, 3f), sc);
                if (!string.IsNullOrEmpty(cp.Speaker))
                {
                    var plate = new Rect(box.x + 40f, box.y - 44f, Mathf.Max(220f, cp.Speaker.Length * 22f + 60f), 56f);
                    UIStyles.Rect(plate, new Color(sc.r * 0.35f, sc.g * 0.35f, sc.b * 0.35f, 0.95f));
                    UIStyles.Frame(plate, sc, 2f);
                    UIStyles.Colored(plate, cp.Speaker, UIStyles.Sized(UIStyles.Center, 30), sc);
                }
                string shown = cp.FullText.Substring(0, Mathf.Clamp(cp.VisibleChars, 0, cp.FullText.Length));
                GUI.Label(new Rect(box.x + 50f, box.y + 30f, box.width - 100f, box.height - 50f), shown, UIStyles.Sized(UIStyles.Body, 36));
                if (cp.VisibleChars >= cp.FullText.Length && Mathf.Sin(Time.unscaledTime * 6f) > 0f)
                    GUI.Label(new Rect(box.xMax - 80f, box.yMax - 60f, 50f, 50f), "▼", UIStyles.Sized(UIStyles.Center, 30));
            }

            // Title cards ("THE JOURNEY BEGINS").
            if (!string.IsNullOrEmpty(cp.TitleText))
            {
                float t = Time.unscaledTime - cp.TitleStart;
                float a = Mathf.Clamp01(t / 0.6f) * Mathf.Clamp01((cp.TitleDuration - t) / 0.6f);
                if (a > 0f)
                {
                    float spread = Mathf.Lerp(1.08f, 1f, Mathf.Clamp01(t / 1.5f));
                    int size = Mathf.RoundToInt(96f * spread);
                    UIStyles.Rect(new Rect(0f, H * 0.5f - 110f, W, 220f), new Color(0f, 0f, 0f, 0.45f * a));
                    UIStyles.Outlined(new Rect(0f, H * 0.5f - 90f, W, 120f), cp.TitleText, UIStyles.Sized(UIStyles.Title, size), new Color(1f, 0.92f, 0.75f, a), 4f);
                    if (!string.IsNullOrEmpty(cp.TitleSub))
                        UIStyles.Colored(new Rect(0f, H * 0.5f + 30f, W, 60f), cp.TitleSub, UIStyles.Sized(UIStyles.Center, 34), new Color(0.85f, 0.85f, 0.9f, a));
                }
            }

            if (cp.FadeAlpha > 0.001f) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, cp.FadeAlpha));
            // SKIP stays usable even through fades to black.
            if (cp.FadeAlpha > 0.5f && Btn(new Rect(safe.xMax - 210f, 18f, 190f, 64f), "SKIP ▶▶", UIStyles.ButtonSmall)) cp.Skip();
        }

        // ------------------------------------------------------------------ Credits

        static readonly string[] CreditLines =
        {
            "<size=40>" + GameConfig.TitleLine1 + "</size>", "<size=70>" + GameConfig.TitleLine2 + "</size>", "", "",
            "<color=#FFD36B>STARRING</color>", "Ren Kagami — the Dawn Blade", "Sora Ikazuchi — the Frightened Blade", "Kiba Arashi — the Arena Rival", "Hana Shiraume — the Wisteria Healer",
            "Homura Enjoji — the Flame Pillar", "Tetsu Ganryu — the Iron Captain", "Master Tessai", "The echo of Akatsuki", "", "Veyrath — the Demon Lord of the Eclipse", "", "",
            "<color=#FFD36B>IN MEMORY OF</color>", "Everyone who held the gate.", "", "",
            "<color=#FFD36B>MUSIC & SOUND</color>", "Procedurally synthesised, original compositions", "", "",
            "<color=#FFD36B>BUILT WITH</color>", "Unity  ·  C#  ·  Claude", "", "", "",
            "<size=56>THANK YOU FOR PLAYING</size>", "", "The eclipse has passed.", "But demons still walk the Ashen Wastes...", "", "<color=#AAAAAA>Replay missions, clear every objective, and summon new allies.</color>"
        };

        void DrawCredits()
        {
            UIStyles.Rect(new Rect(0f, 0f, W, H), Color.black);
            float t = Time.unscaledTime - gm.ScreenEnteredAt;
            float lineH = 64f;
            float y = H - t * 70f;
            foreach (var line in CreditLines)
            {
                if (y > -80f && y < H + 10f)
                {
                    float a = Mathf.Clamp01(Mathf.Min(y, H - y) / 200f);
                    UIStyles.Colored(new Rect(0f, y, W, lineH), line, UIStyles.Sized(UIStyles.Center, 34), new Color(1f, 1f, 1f, a));
                }
                y += lineH;
            }
            bool done = y < H * 0.5f;
            if (Btn(new Rect(W - 330f - safe.x, H - 120f, 300f, 90f), done ? "CONTINUE" : "SKIP", done ? UIStyles.ButtonBig : UIStyles.ButtonSmall))
                gm.GoTo(gm.LastResult != null ? GameScreen.Results : GameScreen.MainMenu);
        }
    }
}
