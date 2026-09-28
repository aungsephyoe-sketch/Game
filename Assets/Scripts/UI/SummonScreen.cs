using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Summon screens: the limited banner with its three featured Mythics, the staged reveal (tap to break the
    /// seal, then the slayer steps out) and the results grid.
    /// </summary>
    public partial class UIManager
    {
        int bannerPick = 1;

        void DrawSummonBanner(PlayerData d, SummonStage st)
        {
            TopBar("SUMMON", GameScreen.MainMenu);
            float top = safe.y + 124f;
            float k = Enter(0f, 0.45f);

            // ---- Three Mythic banners, one Mythic each: tabs across the top, the chosen banner below.
            int n = SummonSystem.FeaturedIds.Length;
            bannerPick = Mathf.Clamp(bannerPick, 0, n - 1);
            float bw = Mathf.Min(1260f, safe.width - 560f);
            float tabW = (bw - (n - 1) * 12f) / n;
            for (int i = 0; i < n; i++)
            {
                var td = GameDatabase.GetCharacter(SummonSystem.FeaturedIds[i]);
                if (td == null) continue;
                Color tc = BannerColor(i);
                var tr = new Rect(safe.x + 24f + i * (tabW + 12f) - (1f - k) * 200f, top, tabW, 78f);
                bool on = i == bannerPick;
                Round(tr, on ? Color.Lerp(tc, Color.black, 0.35f) : new Color(0.07f, 0.07f, 0.12f, 0.9f), 14f);
                if (on) RoundFrame(tr, tc, 3f, 14f);
                var face = new Rect(tr.x + 8f, tr.y + 6f, 66f, 66f);
                var ft = ArtLibrary.Character(td);
                if (ft != null) GUI.DrawTexture(face, ft, ScaleMode.ScaleAndCrop, true, 0f, Color.white, 0f, 33f);
                GUI.Label(new Rect(face.xMax + 10f, tr.y + 8f, tr.width - 90f, 30f), "<b>" + SummonSystem.BannerNames[i] + "</b>", UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(face.xMax + 10f, tr.y + 40f, tr.width - 90f, 28f), "<color=#BBBBBB>" + td.displayName + "</color>", UIStyles.Sized(UIStyles.Small, 17));
                if (!on && GUI.Button(tr, GUIContent.none, GUIStyle.none)) { bannerPick = i; gm.Audio.Play("switch", 0.5f); }
            }
            float btop = top + 92f;
            var def0 = GameDatabase.GetCharacter(SummonSystem.FeaturedIds[bannerPick]);
            Color bc = BannerColor(bannerPick);
            var banner = new Rect(safe.x + 24f - (1f - k) * 200f, btop, bw, H - btop - 24f);
            Round(Offset(banner, 0f, 6f), new Color(0f, 0f, 0f, 0.45f), 22f);
            for (int i = 0; i < 10; i++)
            {
                float f = i / 10f;
                Round(new Rect(banner.x, banner.y + banner.height * f, banner.width, banner.height * 0.1f + 22f),
                    Color.Lerp(new Color(0.04f, 0.04f, 0.12f), Color.Lerp(bc, Color.black, 0.55f), f), i == 0 ? 22f : 0f);
            }
            Round(new Rect(banner.x, banner.yMax - 60f, banner.width, 60f), Color.Lerp(bc, Color.black, 0.55f), 22f);
            var rng = new System.Random(7 + bannerPick);
            for (int i = 0; i < 70; i++)
            {
                float sx = banner.x + 20f + (float)rng.NextDouble() * (banner.width - 40f);
                float sy = banner.y + 20f + (float)rng.NextDouble() * (banner.height * 0.6f);
                float tw = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (1.5f + (float)rng.NextDouble() * 3f) + i);
                UIStyles.CircleTex(new Vector2(sx, sy), 1.5f + (float)rng.NextDouble() * 2.5f, new Color(1f, 1f, 1f, 0.25f + 0.6f * tw));
            }
            if (def0 != null)
            {
                Color ec = ElementChart.ColorOf(def0.element);
                // The Mythic on the right: rays, aura, full art.
                float h = banner.height - 40f;
                Vector2 auraC = new Vector2(banner.xMax - bw * 0.27f, banner.y + h * 0.52f);
                float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f);
                var saved = GUI.matrix;
                for (int r = 0; r < 10; r++)
                {
                    GUI.matrix = saved;
                    RotateGui(r * 36f + Time.unscaledTime * 14f, auraC);
                    UIStyles.Rect(new Rect(auraC.x, auraC.y - 12f, h * 0.55f, 24f), new Color(bc.r, bc.g, bc.b, 0.09f));
                }
                GUI.matrix = saved;
                for (int r = 4; r >= 1; r--) UIStyles.CircleTex(auraC, h * (0.16f + r * 0.06f + pulse * 0.01f), new Color(ec.r, ec.g, ec.b, 0.08f));
                var art = ArtLibrary.CharacterFull(def0);
                float bob = Mathf.Sin(Time.unscaledTime * 1.6f) * 6f;
                if (art != null)
                {
                    var ar = new Rect(auraC.x - h * 0.31f, banner.y + 20f + bob, h * 0.62f, h);
                    GUI.DrawTexture(ar, art, ScaleMode.ScaleToFit, true);
                }
                // Text on the left.
                float tx = banner.x + 32f, tw2 = bw * 0.5f;
                var tag = new Rect(tx, banner.y + 26f, 250f, 42f);
                Round(tag, bc, 21f);
                GUI.Label(tag, "<b>MYTHIC BANNER</b>", UIStyles.Sized(UIStyles.Center, 20));
                UIStyles.Outlined(new Rect(tx, banner.y + 76f, tw2 + 80f, 74f), SummonSystem.BannerNames[bannerPick], UIStyles.Sized(UIStyles.H1, 54), Color.white, 4f);
                GUI.Label(new Rect(tx, banner.y + 150f, tw2, 40f), def0.FullName + "  " + ElementTag(def0.element), UIStyles.Sized(UIStyles.Body, 26));
                UIStyles.Outlined(new Rect(tx, banner.y + 190f, tw2, 30f), "MYTHIC  " + Stars(6), UIStyles.Sized(UIStyles.Body, 22), RarityInfo.Color(6), 1.5f);
                GUI.Label(new Rect(tx, banner.y + 226f, tw2, 90f), "<i><color=#DDDDEE>" + def0.description + "</color></i>", UIStyles.Sized(UIStyles.Small, 20));
                GUI.Label(new Rect(tx, banner.y + 312f, tw2, 34f), "<color=#FFD36B>✦ Special: " + def0.ultimate.name + "</color>", UIStyles.Sized(UIStyles.Body, 22));
                GUI.Label(new Rect(tx, banner.y + 350f, tw2, 60f), "<color=#FFB3C6>Every MYTHIC pulled on this banner is " + def0.displayName + ".</color>", UIStyles.Sized(UIStyles.Small, 19));
                // 60-second trial at full power.
                var trial = new Rect(tx, banner.yMax - 130f, 360f, 96f);
                float g = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                RoundFrame(Grow(trial, 4f + 2f * g), new Color(bc.r, bc.g, bc.b, 0.5f + 0.3f * g), 3f, 18f);
                if (FlatBtn(trial, "TRY IT! <size=20>60s at max power</size>", Color.Lerp(bc, Color.black, 0.15f), true, 30))
                    gm.BeginTrial(def0.id);
            }

            // ---- Right column: rates, pity and the summon buttons.
            float rx = banner.xMax + 24f, rw = safe.xMax - 24f - rx;
            float rk = Enter(0.2f, 0.4f);
            var rates = new Rect(rx + (1f - rk) * 200f, top, rw, 340f);
            Round(Offset(rates, 0f, 5f), new Color(0f, 0f, 0f, 0.4f), 18f);
            Round(rates, new Color(0.05f, 0.06f, 0.11f, 0.94f), 18f);
            GUI.Label(new Rect(rates.x + 24f, rates.y + 16f, rw - 48f, 40f), "RATES", UIStyles.Sized(UIStyles.H2, 28));
            float y = rates.y + 62f;
            for (int i = SummonSystem.Rates.Length - 1; i >= 0; i--)
            {
                int rarity = 2 + i;
                Color rc = RarityInfo.Color(rarity);
                Round(new Rect(rates.x + 18f, y, rw - 36f, 38f), new Color(rc.r, rc.g, rc.b, 0.1f), 10f);
                UIStyles.Colored(new Rect(rates.x + 32f, y, 200f, 38f), RarityInfo.Name(rarity), UIStyles.Sized(UIStyles.Body, 20), rc);
                UIStyles.Colored(new Rect(rates.x + 170f, y, 200f, 38f), Stars(rarity), UIStyles.Sized(UIStyles.Body, 16), Color.Lerp(rc, new Color(1f, 0.85f, 0.3f), 0.5f));
                float rate = SummonSystem.RateFor(rarity, d);
                GUI.Label(new Rect(rates.x + 24f, y, rw - 60f, 38f), (rate * 100f).ToString("0.#") + "%" + (rarity == 6 && SummonSystem.Step(d) == SummonSystem.DoubleMythicStep ? " <color=#FF7AD9>×2!</color>" : ""), UIStyles.Sized(UIStyles.Right, 20));
                y += 42f;
            }
            GUI.Label(new Rect(rates.x + 24f, y + 2f, rw - 48f, 60f), "<color=#AAAAAA>Every ×10 guarantees EPIC or better. Pity: this banner's MYTHIC within " + SummonSystem.PityLimit + ".</color>", UIStyles.Sized(UIStyles.Small, 16));

            var pity = new Rect(rates.x, rates.yMax + 18f, rw, 96f);
            Round(pity, new Color(0.05f, 0.06f, 0.11f, 0.94f), 18f);
            GUI.Label(new Rect(pity.x + 24f, pity.y + 10f, rw - 48f, 34f), "Mythic pity  <b>" + d.summonPity + "</b> / " + SummonSystem.PityLimit, UIStyles.Sized(UIStyles.Body, 22));
            float pf = (float)d.summonPity / SummonSystem.PityLimit;
            Round(new Rect(pity.x + 24f, pity.y + 54f, rw - 48f, 22f), new Color(0f, 0f, 0f, 0.5f), 11f);
            if (pf > 0.01f) Round(new Rect(pity.x + 26f, pity.y + 56f, (rw - 52f) * pf, 18f), Color.Lerp(RarityInfo.Color(6), Color.white, 0.25f * Mathf.Sin(Time.unscaledTime * 4f)), 9f);

            // Step-up ladder: FREE → 250 → 500 → 500 (×2 Mythic) → 400, then repeat. Paid ×10s earn a token.
            var ladder = new Rect(rates.x, pity.yMax + 14f, rw, 112f);
            Round(ladder, new Color(0.05f, 0.06f, 0.11f, 0.94f), 18f);
            GUI.Label(new Rect(ladder.x + 20f, ladder.y + 6f, rw - 40f, 30f), "STEP-UP ×10", UIStyles.Sized(UIStyles.Body, 20));
            GUI.Label(new Rect(ladder.x + 20f, ladder.y + 6f, rw - 40f, 30f), "<color=#FFD36B>Tokens " + d.summonTokens + "</color>  <color=#888888>(" + SummonSystem.ExchangeCost + " = 1 Mythic in Shop)</color>", UIStyles.Sized(UIStyles.Right, 16));
            int n2 = SummonSystem.StepCosts.Length, step = SummonSystem.Step(d);
            float sw = (rw - 40f - (n2 - 1) * 8f) / n2;
            for (int i = 0; i < n2; i++)
            {
                var sr = new Rect(ladder.x + 20f + i * (sw + 8f), ladder.y + 42f, sw, 58f);
                bool cur = i == step, done = i < step;
                Color sc = i == SummonSystem.DoubleMythicStep ? RarityInfo.Color(6) : new Color(0.3f, 0.45f, 0.9f);
                Round(sr, cur ? sc : done ? new Color(0.2f, 0.2f, 0.25f) : new Color(sc.r, sc.g, sc.b, 0.25f), 10f);
                if (cur) RoundFrame(Grow(sr, 3f), new Color(1f, 1f, 1f, 0.6f + 0.3f * Mathf.Sin(Time.unscaledTime * 5f)), 2f, 12f);
                int c = SummonSystem.StepCosts[i];
                GUI.Label(new Rect(sr.x, sr.y + 2f, sr.width, 26f), "<color=#DDDDDD>" + (i + 1) + "</color>", UIStyles.Sized(UIStyles.Center, 14));
                GUI.Label(new Rect(sr.x, sr.y + 20f, sr.width, 32f), done ? "<color=#888888>✓</color>" : c == 0 ? "<b>FREE</b>" : "<b>" + c + "</b>", UIStyles.Sized(UIStyles.Center, 18));
                if (i == SummonSystem.DoubleMythicStep) UIStyles.Outlined(new Rect(sr.x - 6f, sr.y - 14f, sr.width + 12f, 22f), "×2 MYTHIC", UIStyles.Sized(UIStyles.Center, 13), new Color(1f, 0.6f, 0.9f), 1.2f);
            }

            // Summon buttons with diamond costs.
            float by = H - 290f;
            SummonButton(new Rect(rx, by, rw, 118f), "SUMMON ×1", SummonSystem.SingleCost, new Color(0.25f, 0.3f, 0.6f), SummonSystem.CanAfford(d, 1), 1, 0.3f);
            SummonButton(new Rect(rx, by + 134f, rw, 132f), "SUMMON ×10", SummonSystem.MultiCostFor(d), TileRed, SummonSystem.CanAfford(d, 10), 10, 0.4f);
            if (!SummonSystem.CanAfford(d, 1))
                GUI.Label(new Rect(rx, by - 64f, rw, 56f), "<color=#FF9C7A>Earn diamonds from mission stars, daily login and demons.</color>", UIStyles.Sized(UIStyles.CenterSmall, 18));
        }

        static Color BannerColor(int i)
        {
            switch (i) { case 0: return new Color(0.95f, 0.75f, 0.3f); case 1: return new Color(0.95f, 0.22f, 0.4f); default: return new Color(0.3f, 0.85f, 0.6f); }
        }

        void SummonButton(Rect r, string label, int cost, Color c, bool can, int count, float delay)
        {
            float k = Enter(delay, 0.35f);
            if (k <= 0f) return;
            r.x += (1f - k) * 200f;
            if (can && count >= 10)
            {
                float g = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f);
                for (int i = 3; i >= 1; i--) Round(Grow(r, i * 5f), new Color(c.r, c.g, c.b, 0.06f + 0.05f * g), 16f + i * 5f);
            }
            if (FlatBtn(r, "", c, can, 30)) DoSummon(count);
            UIStyles.Outlined(new Rect(r.x, r.y + 12f, r.width, r.height * 0.5f), label, UIStyles.Sized(UIStyles.Center, count >= 10 ? 40 : 34), can ? Color.white : new Color(1f, 1f, 1f, 0.6f), 2f);
            string cs = cost > 0 ? cost.ToString() : "FREE";
            float cw = UIStyles.Sized(UIStyles.Body, 28).CalcSize(new GUIContent(cs)).x;
            float cx = r.center.x - (cw + 40f) * 0.5f;
            if (cost > 0) DiamondIcon(new Vector2(cx + 14f, r.y + r.height * 0.72f), 30f);
            GUI.Label(new Rect(cx + 36f, r.y + r.height * 0.52f, cw + 10f, r.height * 0.4f), cs, UIStyles.Sized(UIStyles.Body, 28));
        }

        void DoSummon(int count)
        {
            var results = SummonSystem.Summon(gm.Data, count, bannerPick);
            if (results.Count == 0) { Toast("Not enough diamonds."); return; }
            gm.Save();
            gm.SummonHall.Play(results);
        }

        // ------------------------------------------------------------------ Reveal

        void DrawSummonReveal(SummonStage st)
        {
            var r = st.Current;
            if (Btn(new Rect(safe.xMax - 250f, safe.y + 24f, 220f, 80f), "SKIP ▶▶", UIStyles.ButtonSmall)) st.SkipAll();
            if (st.Results != null && st.Results.Count > 1 && st.Phase != SummonStage.SummonPhase.Gather)
                GUI.Label(new Rect(safe.x + 30f, safe.y + 30f, 300f, 60f), (st.CurrentIndex + 1) + " / " + st.Results.Count, UIStyles.H2);

            if (st.Phase == SummonStage.SummonPhase.Intro)
            {
                DrawSummonIntro(st);
                // Drawn again so it shows over the black (the one above already takes the tap).
                if (Btn(new Rect(safe.xMax - 250f, safe.y + 24f, 220f, 80f), "SKIP ▶▶", UIStyles.ButtonSmall)) st.SkipAll();
                if (GUI.Button(new Rect(0f, 0f, W, H), GUIContent.none, GUIStyle.none)) st.Advance();
                return;
            }
            if (st.Phase == SummonStage.SummonPhase.Gather)
            {
                UIStyles.Outlined(new Rect(0f, H - 210f, W, 70f), "The stars are gathering...", UIStyles.Sized(UIStyles.Center, 40), Color.white, 3f);
                if (GUI.Button(new Rect(0f, 0f, W, H), GUIContent.none, GUIStyle.none)) st.Advance();
                return;
            }
            if (st.Phase == SummonStage.SummonPhase.Seal)
            {
                // The seal waits for taps: each tap cracks it (and may reveal a better colour).
                var c = st.CircleColor;
                var old = GUI.color;
                GUI.color = new Color(c.r, c.g, c.b, 0.3f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f));
                GUI.DrawTexture(new Rect(0f, 0f, W, H), UIStyles.Vignette);
                GUI.color = old;
                float p = 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 9f);
                UIStyles.Outlined(new Rect(0f, H - 230f, W, 80f), "TAP TO BREAK THE SEAL!", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(52 * p)), Color.white, 4f);
                for (int i = 0; i < st.SealNeeded; i++)
                {
                    Vector2 pc = new Vector2(W * 0.5f + (i - (st.SealNeeded - 1) * 0.5f) * 56f, H - 120f);
                    UIStyles.CircleTex(pc, 18f, i < st.SealCracks ? c : new Color(1f, 1f, 1f, 0.25f));
                }
                if (GUI.Button(new Rect(0f, 0f, W, H), GUIContent.none, GUIStyle.none)) st.Tap();
                return;
            }
            if (GUI.Button(new Rect(0f, 0f, W, H), GUIContent.none, GUIStyle.none)) st.Advance();
            if (r == null) return;

            if (st.Phase == SummonStage.SummonPhase.Charging || st.Phase == SummonStage.SummonPhase.Silhouette)
            {
                var c = st.CircleColor;
                var old = GUI.color;
                GUI.color = new Color(c.r, c.g, c.b, 0.35f + 0.15f * Mathf.Sin(Time.unscaledTime * 8f));
                GUI.DrawTexture(new Rect(0f, 0f, W, H), UIStyles.Vignette);
                GUI.color = old;
                if (st.Phase == SummonStage.SummonPhase.Silhouette)
                    GUI.Label(new Rect(0f, H - 200f, W, 60f), "<color=#DDDDDD>. . .</color>", UIStyles.Sized(UIStyles.Center, 48));
                return;
            }

            // Reveal: a flash, a diagonal band in the rarity colour, the name slams in and the stars pop one by one.
            float t = Time.unscaledTime - st.PhaseStart;
            float k = Mathf.Clamp01(t / 0.4f);
            Color rc = RarityInfo.Color(r.rarity);
            if (k < 1f) UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(1f, 1f, 1f, (1f - k) * 0.85f));
            bool mythic = r.rarity >= 6, legend = r.rarity >= 5;

            // Band behind the card.
            var saved = GUI.matrix;
            RotateGui(-8f, new Vector2(W * 0.62f, H * 0.5f));
            UIStyles.Rect(new Rect(W * 0.3f - (1f - k) * W, H * 0.5f - 220f, W * 0.9f, 440f), new Color(rc.r * 0.35f, rc.g * 0.35f, rc.b * 0.35f, 0.85f));
            UIStyles.Rect(new Rect(W * 0.3f - (1f - k) * W, H * 0.5f - 230f, W * 0.9f, 8f), rc);
            UIStyles.Rect(new Rect(W * 0.3f - (1f - k) * W, H * 0.5f + 222f, W * 0.9f, 8f), rc);
            if (legend)
                for (int i = 0; i < 6; i++)
                {
                    float sx = Mathf.Repeat(Time.unscaledTime * 500f + i * 330f, W * 1.2f);
                    UIStyles.Rect(new Rect(sx, H * 0.5f - 200f + i * 70f, 160f, 3f), new Color(1f, 1f, 1f, 0.25f));
                }
            GUI.matrix = saved;

            var card = new Rect(safe.xMax - 860f + (1f - k) * 400f, H * 0.5f - 200f, 800f, 400f);
            var portrait = ArtLibrary.CharacterFull(r.def);
            if (portrait != null)
            {
                var pr = new Rect(card.x - 360f - (1f - k) * 200f, H * 0.5f - 350f, 340f, 700f);
                if (legend) for (int i = 3; i >= 1; i--) UIStyles.CircleTex(pr.center, 150f + i * 40f, new Color(rc.r, rc.g, rc.b, 0.08f));
                ArtLibrary.DrawFit(pr, portrait, k);
            }
            float slam = Mathf.Clamp01((t - 0.15f) / 0.25f);
            int nameSize = Mathf.RoundToInt(Mathf.Lerp(120f, mythic ? 80f : 70f, slam));
            UIStyles.Outlined(new Rect(card.x + 40f, card.y + 10f, 740f, 90f), RarityInfo.Name(r.rarity), UIStyles.Sized(UIStyles.Big, nameSize), new Color(rc.r, rc.g, rc.b, slam), 4f);
            for (int i = 0; i < r.rarity; i++)
            {
                float sk = Mathf.Clamp01((t - 0.35f - i * 0.1f) / 0.18f);
                if (sk <= 0f) continue;
                float ss = Mathf.Lerp(90f, 46f, sk);
                UIStyles.Outlined(new Rect(card.x + 40f + i * 54f - (ss - 46f) * 0.5f, card.y + 104f - (ss - 46f) * 0.5f, ss + 10f, ss + 10f), "★", UIStyles.Sized(UIStyles.Center, Mathf.RoundToInt(ss)), Color.Lerp(rc, new Color(1f, 0.85f, 0.3f), 0.6f), 2f);
            }
            UIStyles.Outlined(new Rect(card.x + 40f, card.y + 160f, 740f, 80f), r.def.displayName, UIStyles.Sized(UIStyles.H1, 62), Color.white, 3f);
            GUI.Label(new Rect(card.x + 40f, card.y + 238f, 740f, 44f), r.def.versionTitle + "   " + ElementTag(r.def.element) + "  " + r.def.role, UIStyles.Body);
            GUI.Label(new Rect(card.x + 40f, card.y + 284f, 740f, 40f), "<color=#FFD36B>✦ Special: " + r.def.ultimate.name + "</color>", UIStyles.Sized(UIStyles.Small, 22));
            if (r.isNew) UIStyles.Outlined(new Rect(card.xMax - 210f, card.y + 20f, 190f, 70f), "NEW!", UIStyles.Sized(UIStyles.Big, 58), UIStyles.Good, 3f);
            else GUI.Label(new Rect(card.xMax - 380f, card.y + 330f, 360f, 50f), (r.def.rarity >= 5 ? "<color=#C77DFF>DUPLICATE → awaken: +30 Lv, purple star</color>" : "<color=#FF9C7A>DUPLICATE → feed for double EXP</color>"), UIStyles.Sized(UIStyles.Right, 22));
            GUI.Label(new Rect(0f, H - 90f, W, 50f), "<color=#BBBBBB>Tap to continue</color>", UIStyles.Sized(UIStyles.Center, 26));
        }

        /// <summary>
        /// The opening of every summon, drawn over the 3D shrine: black; a sheathed sword fades in across the screen;
        /// a hand closes on the hilt; CLANG — the blade is drawn in a flash; a diagonal slash splits the black and
        /// the two halves fall apart onto the shrine. High rarities get a bigger, coloured slash, lightning and shake.
        /// </summary>
        void DrawSummonIntro(SummonStage st)
        {
            float t = Time.unscaledTime - st.PhaseStart;
            int rar = st.BestRarity;
            bool big = rar >= 5;
            Color rc = rar >= 4 ? RarityInfo.Color(rar) : new Color(0.85f, 0.95f, 1f);
            var saved = GUI.matrix;
            // Shake after the draw (bigger for high rarities).
            if (t > SummonStage.IntroGrip && t < SummonStage.IntroSlice)
            {
                float amp = (big ? 16f : 5f) * Mathf.Clamp01(1f - (t - SummonStage.IntroGrip) / 0.9f);
                GUI.matrix = saved * Matrix4x4.Translate(new Vector3(Random.Range(-amp, amp), Random.Range(-amp, amp), 0f));
            }
            float cy = H * 0.5f;
            float mouth = W * 0.66f;           // where the scabbard ends and the hilt begins
            float hiltLen = W * 0.14f, bladeLen = W * 0.56f;

            if (t < SummonStage.IntroDraw + 0.05f)
            {
                // 1. Black.
                UIStyles.Rect(new Rect(-40f, -40f, W + 80f, H + 80f), new Color(0f, 0f, 0f, Mathf.Clamp01(t / SummonStage.IntroFade)));
                // 2. The sword: scabbard and hilt fade in, a glint runs along it.
                float a = Mathf.Clamp01((t - SummonStage.IntroFade) / (SummonStage.IntroSword - SummonStage.IntroFade));
                a = a * a * (3f - 2f * a);
                float draw = Mathf.Clamp01((t - SummonStage.IntroGrip) / (SummonStage.IntroDraw - SummonStage.IntroGrip));
                float slide = draw * draw * W * 0.9f;   // hilt and blade leave to the right
                var old = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, a);
                // Scabbard: black lacquer with gold fittings (stays put).
                var sc = new Rect(mouth - bladeLen, cy - 16f, bladeLen, 32f);
                Round(sc, new Color(0.08f, 0.06f, 0.08f), 16f);
                Round(new Rect(sc.x + 10f, sc.y + 4f, sc.width - 20f, 6f), new Color(1f, 1f, 1f, 0.12f), 3f);
                Round(new Rect(sc.x - 6f, sc.y - 2f, 40f, 36f), new Color(0.85f, 0.66f, 0.28f), 12f);
                Round(new Rect(sc.xMax - 30f, sc.y - 3f, 30f, 38f), new Color(0.85f, 0.66f, 0.28f), 8f);
                Round(new Rect(sc.x + sc.width * 0.3f, sc.y - 2f, 18f, 36f), new Color(0.75f, 0.12f, 0.12f), 6f);
                // The drawn blade, visible between the scabbard mouth and the hilt as it slides out.
                if (slide > 1f)
                {
                    var bl = new Rect(mouth, cy - 12f, Mathf.Min(slide, bladeLen), 24f);
                    Round(bl, new Color(0.78f, 0.81f, 0.88f), 6f);
                    Round(new Rect(bl.x, bl.y, bl.width, 5f), new Color(1f, 1f, 1f, 0.9f), 3f);
                    Round(new Rect(bl.x, bl.y + 12f, bl.width, 3f), new Color(rc.r, rc.g, rc.b, 0.7f), 2f);
                }
                // Hilt: gold guard, wrapped handle, pommel.
                float hx = mouth + slide;
                Round(new Rect(hx - 8f, cy - 44f, 22f, 88f), new Color(0.9f, 0.72f, 0.3f), 10f);
                var grip = new Rect(hx + 14f, cy - 17f, hiltLen, 34f);
                Round(grip, new Color(0.12f, 0.1f, 0.14f), 12f);
                for (int i = 0; i < 7; i++) Round(new Rect(grip.x + 10f + i * (hiltLen - 24f) / 6f, grip.y + 6f, 14f, 22f), new Color(0.55f, 0.1f, 0.14f), 7f);
                Round(new Rect(grip.xMax - 6f, cy - 20f, 26f, 40f), new Color(0.85f, 0.66f, 0.28f), 10f);
                // Glint.
                if (a > 0.5f && draw <= 0f)
                {
                    float gx = Mathf.Lerp(sc.x, grip.xMax, Mathf.Repeat((t - SummonStage.IntroFade) * 0.8f, 1f));
                    Round(new Rect(gx - 30f, cy - 18f, 60f, 4f), new Color(1f, 1f, 1f, 0.7f), 2f);
                }
                // 3. The hand closes on the grip.
                float g = Mathf.Clamp01((t - SummonStage.IntroSword) / (SummonStage.IntroGrip - SummonStage.IntroSword - 0.1f));
                if (g > 0f)
                {
                    g = 1f - (1f - g) * (1f - g) * (1f - g);
                    float handX = Mathf.Lerp(W + 60f, grip.x + hiltLen * 0.35f, g) + slide;
                    var sleeve = new Rect(handX + 70f, cy - 58f, W, 116f);
                    Round(sleeve, new Color(0.12f, 0.14f, 0.24f), 40f);
                    Round(new Rect(sleeve.x, sleeve.y, 26f, sleeve.height), new Color(0.85f, 0.66f, 0.28f), 12f);
                    var fist = new Rect(handX - 10f, cy - 46f, 92f, 92f);
                    Round(fist, new Color(0.96f, 0.82f, 0.7f), 36f);
                    for (int i = 0; i < 4; i++) Round(new Rect(fist.x - 8f + i * 22f, fist.y + 50f, 24f, 36f), new Color(0.93f, 0.78f, 0.66f), 11f);
                    Round(new Rect(fist.x + 6f, fist.y + 4f, 48f, 26f), new Color(0.9f, 0.74f, 0.62f), 12f);
                }
                GUI.color = old;
                // CLANG: the flash and sparks at the scabbard mouth.
                float c = t - SummonStage.IntroGrip - 0.05f;
                if (c > 0f && c < 0.35f)
                {
                    float f = 1f - c / 0.35f;
                    UIStyles.Rect(new Rect(-40f, -40f, W + 80f, H + 80f), new Color(1f, 1f, 1f, f * 0.55f));
                    for (int i = 0; i < 10; i++)
                    {
                        var sv = GUI.matrix;
                        RotateGui(i * 36f + c * 200f, new Vector2(mouth, cy));
                        Round(new Rect(mouth, cy - 2f, (80f + i * 12f) * (1f - f * 0.5f), 4f), new Color(1f, 0.9f, 0.6f, f), 2f);
                        GUI.matrix = sv;
                    }
                    UIStyles.Outlined(new Rect(mouth - 300f, cy - 190f, 600f, 120f), "CLANG!", UIStyles.Sized(UIStyles.Big, Mathf.RoundToInt(Mathf.Lerp(120f, 96f, f))), new Color(1f, 1f, 1f, f), 4f);
                }
            }
            else
            {
                // 4. The screen is sliced diagonally; the black falls apart onto the shrine behind it.
                float k = Mathf.Clamp01((t - SummonStage.IntroDraw) / (SummonStage.IntroSlice - SummonStage.IntroDraw));
                float sep = k * k * H * 1.3f;
                var pivot = new Vector2(W * 0.5f, H * 0.5f);
                var sv = GUI.matrix;
                RotateGui(-24f, pivot);
                UIStyles.Rect(new Rect(-W, pivot.y - H * 2f - sep, W * 3f, H * 2f), Color.black);
                UIStyles.Rect(new Rect(-W, pivot.y + sep, W * 3f, H * 2f), Color.black);
                // The slash itself: a hot line with a coloured glow (much bigger for high rarities).
                float thick = (big ? 26f : 10f) * (1f - k * 0.6f);
                float glowA = 1f - k;
                UIStyles.Rect(new Rect(-W, pivot.y - thick * 2.5f, W * 3f, thick * 5f), new Color(rc.r, rc.g, rc.b, 0.35f * glowA));
                UIStyles.Rect(new Rect(-W, pivot.y - thick * 0.5f, W * 3f, thick), new Color(1f, 1f, 1f, glowA));
                if (big)
                {
                    // Lightning crackling along the cut.
                    var rng = new System.Random(Mathf.FloorToInt(t * 20f));
                    for (int b = 0; b < 3; b++)
                    {
                        float x = -W * 0.2f;
                        float y = pivot.y;
                        while (x < W * 1.2f)
                        {
                            float nx = x + 40f + (float)rng.NextDouble() * 60f;
                            float ny = pivot.y + ((float)rng.NextDouble() - 0.5f) * 70f;
                            float dx = nx - x, dy = ny - y;
                            float len = Mathf.Sqrt(dx * dx + dy * dy);
                            var s2 = GUI.matrix;
                            RotateGui(Mathf.Atan2(dy, dx) * Mathf.Rad2Deg, new Vector2(x, y));
                            UIStyles.Rect(new Rect(x, y - 2f, len, 4f), new Color(Mathf.Lerp(rc.r, 1f, 0.5f), Mathf.Lerp(rc.g, 1f, 0.5f), 1f, glowA));
                            GUI.matrix = s2;
                            x = nx; y = ny;
                        }
                    }
                }
                GUI.matrix = sv;
                if (k < 0.2f) UIStyles.Rect(new Rect(-40f, -40f, W + 80f, H + 80f), new Color(rc.r, rc.g, rc.b, (0.2f - k) * (big ? 3f : 1.5f)));
            }
            GUI.matrix = saved;
        }

        void DrawSummonSummary(PlayerData d, SummonStage st)
        {
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.45f));
            UIStyles.Outlined(new Rect(0f, safe.y + 30f, W, 90f), "SUMMON RESULTS", UIStyles.Sized(UIStyles.H1, 60), UIStyles.Gold, 3f);
            var res = st.Results;
            if (res == null) return;
            int cols = Mathf.Min(5, res.Count);
            float cw = 320f, ch = 190f, gap = 20f;
            float startX = W * 0.5f - (cols * cw + (cols - 1) * gap) * 0.5f;
            float startY = res.Count > 5 ? 190f : 380f;
            for (int i = 0; i < res.Count; i++)
            {
                var r = res[i];
                float k = Mathf.Clamp01((Time.unscaledTime - st.PhaseStart - i * 0.07f) / 0.25f);
                if (k <= 0f) continue;
                float pop = k < 1f ? Mathf.Lerp(0.6f, 1.08f, k) : 1f;
                var rect0 = new Rect(startX + (i % cols) * (cw + gap), startY + (i / cols) * (ch + gap), cw, ch);
                var rect = new Rect(rect0.center.x - cw * 0.5f * pop, rect0.center.y - ch * 0.5f * pop, cw * pop, ch * pop);
                Color rc = RarityInfo.Color(r.rarity);
                if (r.rarity >= 5)
                {
                    float g = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4f + i);
                    for (int j = 3; j >= 1; j--) Round(Grow(rect, j * 5f), new Color(rc.r, rc.g, rc.b, 0.07f + 0.05f * g), 14f + j * 5f);
                }
                Round(rect, Color.Lerp(new Color(0.08f, 0.07f, 0.13f, 0.95f), rc, 0.18f), 14f);
                RoundFrame(rect, rc, r.rarity >= 5 ? 4f : 2f, 14f);
                var thumb = ArtLibrary.Character(r.def);
                var tr = new Rect(rect.x + 10f, rect.y + 24f, 140f, 140f);
                Aura(tr.center, 70f, ElementChart.ColorOf(r.def.element), false);
                if (thumb != null) GUI.DrawTexture(tr, thumb, ScaleMode.ScaleAndCrop, true);
                float tx = rect.x + 158f, tw = rect.width - 166f;
                UIStyles.Outlined(new Rect(tx, rect.y + 14f, tw, 30f), RarityInfo.Name(r.rarity), UIStyles.Sized(UIStyles.Body, 20), rc, 1.5f);
                GUI.Label(new Rect(tx, rect.y + 44f, tw, 30f), "<color=#FFD36B>" + Stars(r.rarity) + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                GUI.Label(new Rect(tx, rect.y + 72f, tw, 40f), r.def.displayName, UIStyles.Sized(UIStyles.H2, 24));
                GUI.Label(new Rect(tx, rect.y + 108f, tw, 30f), "<color=#AAAAAA>" + r.def.versionTitle + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                GUI.Label(new Rect(tx, rect.y + 140f, tw, 40f), r.isNew ? "<color=#7CFF8A>NEW!</color>" : "<color=#FF9C7A>DUPLICATE</color>", UIStyles.Sized(UIStyles.Body, 22));
            }
            float by = H - 150f;
            if (FlatBtn(new Rect(W * 0.5f - 470f, by, 440f, 100f), "OK", new Color(0.2f, 0.24f, 0.4f), true, 36)) st.CloseSummary();
            string again = res.Count >= 10 ? "SUMMON ×10  <size=22>(" + (SummonSystem.MultiCostFor(d) == 0 ? "FREE" : SummonSystem.MultiCostFor(d) + " ◆") + ")</size>" : "SUMMON AGAIN";
            if (FlatBtn(new Rect(W * 0.5f + 30f, by, 440f, 100f), again, TileRed, SummonSystem.CanAfford(d, res.Count), 32))
                DoSummon(res.Count);
        }
    }
}
