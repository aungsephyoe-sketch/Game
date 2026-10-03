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
            bool guiWas = GUI.enabled;
            if (sheetDef != null) GUI.enabled = false; // the sheet on top takes the clicks
            switch (st.Phase)
            {
                case SummonStage.SummonPhase.Idle: DrawSummonBanner(d, st); break;
                case SummonStage.SummonPhase.Summary: DrawSummonSummary(d, st); break;
                default: DrawSummonReveal(st); break;
            }
            GUI.enabled = guiWas;
            if (st.Phase != SummonStage.SummonPhase.Idle && st.Phase != SummonStage.SummonPhase.Summary) sheetDef = null;
            DrawSlayerSheet();
        }

        // ------------------------------------------------------------------ Missions board

        int boardTab;
        Vector2 boardScroll;

        void DrawMissionsBoard()
        {
            var d = gm.Data;
            TopBar("MISSIONS", GameScreen.MainMenu);
            QuestSystem.EnsureReset(d);
            string[] tabs = { "DAILY", "WEEKLY", "SIDE STORIES", "EVENTS &\nTRAINING" };
            string[] tabIcons = { "scroll", "scroll", "scroll", "claw" };
            float tx = safe.x + 60f, ty = safe.y + 124f;
            float tabW = Mathf.Min(330f, (safe.width - 120f - 3f * 16f) / 4f);
            for (int t = 0; t < tabs.Length; t++)
            {
                var r = new Rect(tx + t * (tabW + 16f), ty, tabW, 88f);
                bool on = boardTab == t;
                Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.35f), 10f);
                Round(r, on ? TileRed : new Color(0.08f, 0.09f, 0.13f, 0.92f), 10f);
                RoundFrame(r, on ? new Color(1f, 0.55f, 0.45f) : new Color(1f, 1f, 1f, 0.1f), 2f, 10f);
                GUI.DrawTexture(new Rect(r.x + 30f, r.y + 22f, 44f, 44f), IconFactory.Get(tabIcons[t]), ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(r.x + 84f, r.y, r.width - 90f, r.height), tabs[t], UIStyles.Sized(UIStyles.Center, 26));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { gm.Audio.Play("click", 0.5f); boardTab = t; boardScroll = Vector2.zero; }
            }
            var body = new Rect(safe.x + 60f, ty + 108f, safe.width - 120f, H - ty - 132f);
            Round(body, new Color(0.04f, 0.05f, 0.08f, 0.9f), 16f);

            if (boardTab < 2)
            {
                bool daily = boardTab == 0;
                var quests = new List<QuestSystem.QuestDef>();
                foreach (var def in QuestSystem.Defs) if (def.daily == daily) quests.Add(def);
                var qview = new Rect(body.x + 16f, body.y + 16f, body.width - 32f, body.height - 32f);
                float rowH = 124f;
                var qcontent = new Rect(0f, 0f, qview.width - (quests.Count * (rowH + 10f) > qview.height ? 22f : 0f), quests.Count * (rowH + 10f));
                boardScroll = GUI.BeginScrollView(qview, boardScroll, qcontent);
                for (int i = 0; i < quests.Count; i++)
                {
                    var def = quests[i];
                    var s = QuestSystem.State(d, def.id);
                    var row = new Rect(0f, i * (rowH + 10f), qcontent.width, rowH);
                    Round(row, new Color(0.1f, 0.11f, 0.16f, 0.95f), 12f);
                    // Icon tile.
                    var it = new Rect(row.x + 14f, row.y + 12f, 100f, 100f);
                    Round(it, new Color(0.16f, 0.17f, 0.24f), 10f);
                    RoundFrame(it, new Color(1f, 1f, 1f, 0.08f), 2f, 10f);
                    var o = GUI.color;
                    GUI.color = QuestColor(def.kind);
                    GUI.DrawTexture(new Rect(it.x + 18f, it.y + 18f, 64f, 64f), IconFactory.Get(QuestIconKey(def.kind)), ScaleMode.ScaleToFit, true);
                    GUI.color = o;
                    GUI.Label(new Rect(row.x + 138f, row.y + 14f, 560f, 50f), def.title, UIStyles.Sized(UIStyles.H2, 34));
                    GUI.Label(new Rect(row.x + 138f, row.y + 66f, 560f, 40f), "<color=#AAAAAA>" + RewardSummary(def.reward) + "</color>", UIStyles.Sized(UIStyles.Body, 22));
                    float bx = row.x + 700f, bw = Mathf.Min(360f, row.width - 1320f + 360f);
                    GUI.Label(new Rect(bx, row.y + 20f, bw, 40f), Mathf.Min(s.progress, def.target) + " / " + def.target, UIStyles.Sized(UIStyles.Center, 26));
                    Round(new Rect(bx, row.y + 68f, bw, 18f), new Color(0.2f, 0.21f, 0.26f), 6f);
                    float f = Mathf.Clamp01((float)s.progress / def.target);
                    if (f > 0.01f) Round(new Rect(bx, row.y + 68f, Mathf.Max(18f, bw * f), 18f), ProgressYellow, 6f);
                    RewardIconsRef(new Vector2(bx + bw + 40f, row.center.y), def.reward);
                    var cb = new Rect(row.xMax - 230f, row.y + 26f, 206f, 72f);
                    if (s.claimed)
                    {
                        Round(cb, new Color(0.08f, 0.22f, 0.14f), 10f);
                        RoundFrame(cb, new Color(0.3f, 0.85f, 0.45f), 2f, 10f);
                        UIStyles.Colored(cb, "✔ CLAIMED", UIStyles.Sized(UIStyles.Center, 28), new Color(0.4f, 0.95f, 0.5f));
                    }
                    else if (s.progress >= def.target)
                    {
                        if (FlatBtn(cb, "CLAIM", TileRed, true, 32))
                        {
                            QuestSystem.Claim(d, def);
                            gm.Save();
                            gm.Audio.Play("perfect", 0.6f);
                            Toast("Claimed: " + RewardText(def.reward));
                        }
                    }
                    else
                    {
                        Round(cb, new Color(0.14f, 0.15f, 0.2f), 10f);
                        RoundFrame(cb, new Color(1f, 1f, 1f, 0.08f), 2f, 10f);
                        UIStyles.Colored(cb, "CLAIM", UIStyles.Sized(UIStyles.Center, 30), new Color(1f, 1f, 1f, 0.35f));
                    }
                }
                GUI.EndScrollView();
                return;
            }

            var list = new List<MissionDefinition>();
            foreach (var m in GameDatabase.AllMissions())
            {
                if (boardTab == 2 && (m.type == MissionType.Side || m.type == MissionType.Treasure)) list.Add(m);
                if (boardTab == 3 && string.IsNullOrEmpty(m.eventId) && (m.type == MissionType.Event || m.type == MissionType.Training)) list.Add(m);
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

        static string QuestIconKey(string kind)
        {
            switch (kind)
            {
                case "kill": return "claw";
                case "boss": return "swords";
                case "upgrade": return "person";
                case "summon": return "spin";
                case "coop": return "group";
                case "pvp": case "pvpwin": return "swords";
                case "ultimate": return "burst";
                case "skill": return "slashes";
                case "dodge": return "dodge";
                case "stars3": return "up";
                case "chat": return "mail";
                case "event": return "sun";
                default: return "scroll";
            }
        }

        static string RewardSummary(RewardBundle r)
        {
            var parts = new List<string>();
            if (r.coins > 0) parts.Add("Coins +" + r.coins.ToString("N0"));
            if (r.crystals > 0) parts.Add("Diamonds +" + r.crystals);
            if (r.XpValue > 0) parts.Add("XP +" + r.XpValue.ToString("N0"));
            return string.Join("    ", parts.ToArray());
        }

        /// <summary>Reward icons with amounts, left to right from a centre-left point.</summary>
        void RewardIconsRef(Vector2 at, RewardBundle rw)
        {
            float x = at.x;
            var st = UIStyles.Sized(UIStyles.Body, 28);
            if (rw.coins > 0) { CoinIcon(new Vector2(x + 20f, at.y), 40f); GUI.Label(new Rect(x + 48f, at.y - 22f, 120f, 44f), rw.coins.ToString("N0"), st); x += 170f; }
            if (rw.crystals > 0) { DiamondIcon(new Vector2(x + 20f, at.y), 42f); GUI.Label(new Rect(x + 48f, at.y - 22f, 80f, 44f), rw.crystals.ToString(), st); x += 120f; }
            if (rw.XpValue > 0) { XpIcon(new Vector2(x + 20f, at.y), 40f); GUI.Label(new Rect(x + 48f, at.y - 22f, 120f, 44f), rw.XpValue.ToString("N0"), st); }
        }

        static void ScrollIcon(Vector2 c, Color col)
        {
            var o = GUI.color;
            GUI.color = col;
            GUI.DrawTexture(new Rect(c.x - 20f, c.y - 20f, 40f, 40f), IconFactory.Get("scroll"), ScaleMode.ScaleToFit, true);
            GUI.color = o;
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
                case "summon": case "ultimate": return TilePurple;
                case "coop": case "event": case "stars3": return TileGreen;
                case "pvp": case "pvpwin": return TileMaroon;
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
            if (rw.XpValue > 0) { Pill(new Rect(x, r.y, 160f, h), "XP", new Color(0.6f, 0.4f, 0.9f), rw.XpValue.ToString("N0")); x += 170f; }
            if (rw.exp > 0 && x < r.xMax - 150f) Pill(new Rect(x, r.y, 150f, h), "★", new Color(0.8f, 0.8f, 0.3f), rw.exp.ToString("N0"));
        }

        // ------------------------------------------------------------------ Shop

        void DrawShop()
        {
            var d = gm.Data;
            TopBar("SOLMERE MARKET", GameScreen.MainMenu);
            // Tabs: supplies (XP packs, chests, diamonds) and accessories for gold.
            string[] tabs = { "DIAMONDS", "SUPPLIES", "ACCESSORIES", "MYTHIC EXCHANGE" };
            if (shopTab != 2) gearInfo = null;
            bool guiWas = GUI.enabled;
            GUI.enabled = guiWas && gearInfo == null;
            for (int t = 0; t < 4; t++)
                if (FlatBtn(new Rect(safe.x + 30f + t * 260f, safe.y + 120f, 240f, 60f), tabs[t], shopTab == t ? TileRed : TileNavy, true, t == 3 ? 18 : 22)) shopTab = t;
            GUI.enabled = guiWas;
            if (shopTab == 0) { DrawDiamondPacks(d); return; }
            if (shopTab == 2) { DrawAccessoryShop(d); return; }
            if (shopTab == 3) { DrawMythicExchange(d); return; }
            float cw = 520f, ch = 230f, gap = 22f;
            int cols = Mathf.Max(1, Mathf.FloorToInt((safe.width - 60f + gap) / (cw + gap)));
            float top = safe.y + 200f;
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

        int shopTab;

        /// <summary>Diamond packs: a grid of cards with the amount, a gem pile, a ribbon for the featured ones and the price.</summary>
        void DrawDiamondPacks(PlayerData d)
        {
            var packs = ShopSystem.DiamondPacks;
            int cols = 4;
            float gap = 22f, top = safe.y + 200f;
            float cw = (safe.width - 60f - (cols - 1) * gap) / cols, ch = (H - top - 40f - gap) / 2f;
            for (int i = 0; i < packs.Count; i++)
            {
                var p = packs[i];
                float k = Enter(i * 0.05f, 0.3f);
                var r = new Rect(safe.x + 30f + (i % cols) * (cw + gap), top + (i / cols) * (ch + gap) + (1f - k) * 40f, cw, ch);
                bool featured = !string.IsNullOrEmpty(p.badge);
                Color accent = p.diamonds >= 10000 ? new Color(0.75f, 0.45f, 1f) : p.diamonds == 2000 ? new Color(1f, 0.45f, 0.25f) : featured ? new Color(1f, 0.8f, 0.25f) : new Color(0.35f, 0.65f, 1f);
                Round(Offset(r, 0f, 6f), new Color(0f, 0f, 0f, 0.4f), 18f);
                Round(r, Color.Lerp(new Color(0.06f, 0.07f, 0.13f), accent, featured ? 0.22f : 0.1f), 18f);
                RoundFrame(r, featured ? accent : new Color(1f, 1f, 1f, 0.1f), featured ? 3f : 2f, 18f);
                if (featured)
                {
                    var rib = new Rect(r.x + 14f, r.y - 14f, Mathf.Min(r.width - 28f, 220f), 34f);
                    Round(rib, accent, 17f);
                    GUI.Label(rib, "<b>" + p.badge + "</b>", UIStyles.Sized(UIStyles.Center, 17));
                }
                // A pile of gems that grows with the pack.
                int gems = Mathf.Clamp(1 + Mathf.RoundToInt(Mathf.Log(p.diamonds / 250f, 2f) * 1.5f), 1, 9);
                Vector2 gc = new Vector2(r.center.x, r.y + ch * 0.36f);
                float glow = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f + i);
                UIStyles.CircleTex(gc, ch * 0.22f + glow * 4f, new Color(accent.r, accent.g, accent.b, 0.15f));
                for (int g = 0; g < gems; g++)
                {
                    float a = g * 2.4f;
                    Vector2 o = g == 0 ? Vector2.zero : new Vector2(Mathf.Cos(a), Mathf.Sin(a) * 0.5f) * (18f + g * 5f);
                    DiamondIcon(gc + o, g == 0 ? 70f : 44f);
                }
                UIStyles.Outlined(new Rect(r.x, r.y + ch * 0.55f, r.width, 50f), p.diamonds.ToString("N0"), UIStyles.Sized(UIStyles.H1, 42), Color.white, 2f);
                GUI.Label(new Rect(r.x, r.y + ch * 0.55f + 46f, r.width, 28f), "<color=#9FD8FF>DIAMONDS</color>" + (string.IsNullOrEmpty(p.tag) ? "" : "  <color=#BBBBBB>· " + p.tag + "</color>"), UIStyles.Sized(UIStyles.Center, 17));
                if (FlatBtn(new Rect(r.x + 18f, r.yMax - 76f, r.width - 36f, 60f), p.price, TileGreen, true, 28))
                {
                    ShopSystem.BuyDiamondPack(d, p);
                    gm.Save();
                    gm.Audio.Play("gem", 0.8f);
                    Toast("+" + p.diamonds.ToString("N0") + " diamonds (test purchase — store billing isn't connected yet)");
                }
            }
        }

        /// <summary>Tokens from paid step-up ×10s buy a featured Mythic: 30 tokens each.</summary>
        void DrawMythicExchange(PlayerData d)
        {
            float top = safe.y + 200f;
            var head = new Rect(safe.x + 30f, top, safe.width - 60f, 70f);
            Round(head, new Color(0.08f, 0.06f, 0.14f, 0.92f), 16f);
            GUI.Label(new Rect(head.x + 24f, head.y, head.width - 48f, head.height), "Every paid step-up ×10 summon gives <b>1 token</b>. Trade <b>" + SummonSystem.ExchangeCost + " tokens</b> for the Mythic of your choice.", UIStyles.Sized(UIStyles.Body, 22));
            GUI.Label(new Rect(head.x + 24f, head.y, head.width - 48f, head.height), "<color=#FFD36B><b>TOKENS  " + d.summonTokens + "</b></color>", UIStyles.Sized(UIStyles.Right, 30));
            int n = SummonSystem.FeaturedIds.Length;
            float cw = Mathf.Min(520f, (safe.width - 60f - (n - 1) * 24f) / n), ch = H - top - 110f;
            float x0 = safe.x + 30f + (safe.width - 60f - (n * cw + (n - 1) * 24f)) * 0.5f;
            for (int i = 0; i < n; i++)
            {
                var def = GameDatabase.GetCharacter(SummonSystem.FeaturedIds[i]);
                if (def == null) continue;
                var r = new Rect(x0 + i * (cw + 24f), top + 90f, cw, ch);
                Color ec = ElementChart.ColorOf(def.element), rc = RarityInfo.Color(6);
                Round(Offset(r, 0f, 6f), new Color(0f, 0f, 0f, 0.4f), 18f);
                Round(r, Color.Lerp(new Color(0.07f, 0.06f, 0.12f), rc, 0.15f), 18f);
                RoundFrame(r, rc, 3f, 18f);
                var ar = new Rect(r.x + 20f, r.y + 16f, r.width - 40f, r.height - 250f);
                Aura(ar.center, ar.height * 0.35f, ec, false);
                var art = ArtLibrary.CharacterFull(def);
                if (art != null) GUI.DrawTexture(ar, art, ScaleMode.ScaleToFit, true);
                var owned = d.GetCharacter(def.id);
                GUI.Label(new Rect(r.x + 20f, r.yMax - 230f, r.width - 40f, 40f), def.displayName, UIStyles.Sized(UIStyles.H2, 30));
                GUI.Label(new Rect(r.x + 20f, r.yMax - 190f, r.width - 40f, 30f), "<color=#BBBBBB>" + def.versionTitle + "</color>  " + ElementTag(def.element), UIStyles.Sized(UIStyles.Body, 20));
                UIStyles.Outlined(new Rect(r.x + 20f, r.yMax - 160f, r.width - 40f, 28f), "MYTHIC  " + Stars(6), UIStyles.Sized(UIStyles.Small, 18), rc, 1.2f);
                GUI.Label(new Rect(r.x + 20f, r.yMax - 130f, r.width - 40f, 30f), owned != null ? "<color=#C77DFF>Owned — another copy awakens them</color>" : "<color=#7CFF8A>Not owned yet</color>", UIStyles.Sized(UIStyles.Small, 17));
                bool can = d.summonTokens >= SummonSystem.ExchangeCost;
                if (FlatBtn(new Rect(r.x + 20f, r.yMax - 92f, r.width - 40f, 74f), "EXCHANGE  <size=22>" + SummonSystem.ExchangeCost + " tokens</size>", TileRed, can, 28))
                {
                    bool isNew;
                    if (SummonSystem.Exchange(d, def.id, out isNew))
                    {
                        gm.Save();
                        gm.Audio.Play("ultimate", 0.7f);
                        Toast(isNew ? def.displayName + " joined you!" : "Got a copy of " + def.displayName + " — use it to awaken them");
                        if (gm.Home != null) gm.Home.Celebrate(rc);
                    }
                }
            }
        }
        Vector2 accScroll;

        /// <summary>Accessories bought with gold. Each shows its stats; equip it from a slayer's Gear tab.</summary>
        void DrawAccessoryShop(PlayerData d)
        {
            var list = ShopSystem.Accessories();
            bool guiWas = GUI.enabled;
            if (gearInfo != null) GUI.enabled = false; // the popup on top takes the taps
            float cw = 400f, ch = 300f, gap = 22f;
            var view = new Rect(safe.x + 30f, safe.y + 200f, safe.width - 60f, H - safe.y - 230f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((view.width - 20f + gap) / (cw + gap)));
            var content = new Rect(0f, 0f, view.width - 20f, Mathf.CeilToInt(list.Count / (float)cols) * (ch + gap));
            accScroll = GUI.BeginScrollView(view, accScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                var r = new Rect((i % cols) * (cw + gap), (i / cols) * (ch + gap), cw, ch);
                Color rc = RarityInfo.Color(e.rarity);
                Round(Offset(r, 0f, 5f), new Color(0f, 0f, 0f, 0.35f), 16f);
                Round(r, Color.Lerp(new Color(0.07f, 0.08f, 0.13f), rc, 0.2f), 16f);
                Round(new Rect(r.x, r.y, r.width, 8f), rc, 4f);
                var ic = new Rect(r.x + 20f, r.y + 26f, 90f, 90f);
                UIStyles.CircleTex(ic.center, 56f, new Color(rc.r, rc.g, rc.b, 0.25f + 0.1f * Mathf.Sin(Time.unscaledTime * 3f + i)));
                var art = GearArt.Get(e);
                if (art != null) GUI.DrawTexture(Grow(ic, 6f), art, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(r.x + 124f, r.y + 22f, cw - 140f, 40f), e.displayName, UIStyles.Sized(UIStyles.H2, 24));
                UIStyles.Outlined(new Rect(r.x + 124f, r.y + 60f, cw - 140f, 24f), RarityInfo.Name(e.rarity), UIStyles.Sized(UIStyles.Small, 16), rc, 1.2f);
                int owned = d.equipment.FindAll(x => x.defId == e.id).Count;
                GUI.Label(new Rect(r.x + 124f, r.y + 86f, cw - 140f, 26f), "<color=#AAAAAA>Owned: " + owned + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                var b = e.baseBonus;
                string st = "";
                if (b.hp > 0) st += "HP +" + Mathf.RoundToInt(b.hp) + "   ";
                if (b.atk > 0) st += "ATK +" + Mathf.RoundToInt(b.atk) + "   ";
                if (b.def > 0) st += "DEF +" + Mathf.RoundToInt(b.def);
                GUI.Label(new Rect(r.x + 20f, r.y + 128f, cw - 40f, 30f), "<color=#8CFF9E>" + st + "</color>", UIStyles.Sized(UIStyles.Body, 18));
                GUI.Label(new Rect(r.x + 20f, r.y + 158f, cw - 40f, 50f), (string.IsNullOrEmpty(e.effect) ? "<color=#BBBBBB>" : "<color=#FFD36B>✦ ") + e.description + "</color>", new GUIStyle(UIStyles.Sized(UIStyles.Small, 16)) { wordWrap = true });
                int price = ShopSystem.AccessoryPrice(e);
                bool can = d.coins >= price;
                var br = new Rect(r.x + 20f, r.yMax - 80f, cw - 40f, 62f);
                if (FlatBtn(br, "", TileGreen, can, 22))
                {
                    if (ShopSystem.BuyAccessory(d, e))
                    {
                        gm.Save();
                        gm.Audio.Play("coin", 0.8f);
                        Toast("Bought " + e.displayName + " · equip it from a slayer's Gear tab");
                    }
                }
                CoinIcon(new Vector2(br.x + 90f, br.center.y), 32f);
                GUI.Label(new Rect(br.x + 112f, br.y, br.width - 120f, br.height), "BUY  " + price.ToString("N0"), UIStyles.Sized(UIStyles.Body, 24));
                GUI.Label(new Rect(r.xMax - 150f, r.y + 90f, 134f, 24f), "<color=#8FA0C8>ⓘ tap for details</color>", UIStyles.Sized(UIStyles.Right, 14));
                // Tap anywhere on the card except the buy button: show what it does.
                if (GUI.Button(new Rect(r.x, r.y, r.width, r.height - 90f), GUIContent.none, GUIStyle.none))
                {
                    gearInfo = e;
                    gearInfoAt = Time.unscaledTime;
                    gm.Audio.Play("click", 0.5f);
                }
            }
            GUI.EndScrollView();
            GUI.enabled = guiWas;
            DrawGearInfo(d);
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

            // Dialogue: a chunky cartoon box, the speaker's name plate and their portrait (reacting to the line).
            if (!string.IsNullOrEmpty(cp.FullText))
            {
                float boxW = Mathf.Min(1700f, W - 200f);
                bool hasFace = cp.SpeakerDef != null;
                var box = new Rect(W * 0.5f - boxW * 0.5f + (hasFace ? 110f : 0f), H - 320f, boxW - (hasFace ? 110f : 0f), 200f);
                Color sc = SpeakerColor(cp.Speaker);
                Round(Offset(Grow(box, 5f), 0f, 8f), new Color(0f, 0f, 0f, 0.4f), 26f);
                Round(Grow(box, 5f), new Color(0.05f, 0.04f, 0.1f, 0.97f), 26f);
                Round(box, new Color(0.1f, 0.09f, 0.18f, 0.96f), 22f);
                Round(new Rect(box.x + 8f, box.y + 6f, box.width - 16f, 8f), new Color(sc.r, sc.g, sc.b, 0.8f), 4f);
                if (hasFace)
                {
                    // The portrait pops a little with each new line and shakes when the line is angry or shocked.
                    float since = Time.unscaledTime - cp.LineStarted;
                    float pop = 1f + 0.12f * Mathf.Exp(-since * 8f);
                    Vector2 fc = new Vector2(box.x - 90f, box.center.y - 10f);
                    if (cp.SpeakerMood == CharacterExpression.Mood.Angry || cp.SpeakerMood == CharacterExpression.Mood.Surprised)
                        fc += new Vector2(Mathf.Sin(Time.unscaledTime * 60f), Mathf.Cos(Time.unscaledTime * 53f)) * 3f * Mathf.Exp(-since * 3f);
                    UIStyles.CircleTex(fc, 100f * pop, new Color(0.05f, 0.04f, 0.1f));
                    FaceCircle(fc, 92f * pop, cp.SpeakerDef, sc);
                }
                if (!string.IsNullOrEmpty(cp.Speaker))
                {
                    var plate = new Rect(box.x + 30f, box.y - 40f, Mathf.Max(220f, cp.Speaker.Length * 20f + 70f), 60f);
                    FlatBtnLook(plate, sc);
                    UIStyles.Outlined(new Rect(plate.x, plate.y - 3f, plate.width, plate.height), cp.Speaker, UIStyles.Sized(UIStyles.Center, 30), Color.white, 3f);
                }
                string shown = cp.FullText.Substring(0, Mathf.Clamp(cp.VisibleChars, 0, cp.FullText.Length));
                GUI.Label(new Rect(box.x + 44f, box.y + 36f, box.width - 90f, box.height - 50f), "<b>" + shown + "</b>", new GUIStyle(UIStyles.Sized(UIStyles.Body, 34)) { wordWrap = true, richText = true });
                if (cp.VisibleChars >= cp.FullText.Length)
                    GUI.Label(new Rect(box.xMax - 80f, box.yMax - 64f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 5f)) * -8f, 50f, 50f), "▼", UIStyles.Sized(UIStyles.Center, 30));
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
            "<size=40>SLAYER RPG</size>", "<size=70>" + GameConfig.TitleLine2 + "</size>", "", "",
            "<color=#FFD36B>STARRING</color>", "Ren — the Dawn Blade", "Sora — the Frightened Blade", "Kiba — the Arena Rival", "Hana — the Wisteria Healer",
            "Homura — the Flame Pillar", "Tetsu — the Iron Captain", "Master Tessai", "The echo of Akatsuki", "", "Veyrath — the Demon Lord of the Eclipse", "", "",
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
