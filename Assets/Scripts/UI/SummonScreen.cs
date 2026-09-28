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

            // ---- The banner: a night sky with a huge moon and the three featured Mythics.
            float bw = Mathf.Min(1260f, safe.width - 560f);
            var banner = new Rect(safe.x + 24f - (1f - k) * 200f, top, bw, H - top - 24f);
            Round(Offset(banner, 0f, 6f), new Color(0f, 0f, 0f, 0.45f), 22f);
            // Vertical gradient: deep navy to violet.
            for (int i = 0; i < 10; i++)
            {
                float f = i / 10f;
                Round(new Rect(banner.x, banner.y + banner.height * f, banner.width, banner.height * 0.1f + 22f),
                    Color.Lerp(new Color(0.05f, 0.05f, 0.16f), new Color(0.22f, 0.07f, 0.28f), f), i == 0 ? 22f : 0f);
            }
            Round(new Rect(banner.x, banner.yMax - 60f, banner.width, 60f), new Color(0.22f, 0.07f, 0.28f), 22f);
            // Twinkling stars.
            var rng = new System.Random(7);
            for (int i = 0; i < 70; i++)
            {
                float sx = banner.x + 20f + (float)rng.NextDouble() * (banner.width - 40f);
                float sy = banner.y + 20f + (float)rng.NextDouble() * (banner.height * 0.6f);
                float tw = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * (1.5f + (float)rng.NextDouble() * 3f) + i);
                float sz = 1.5f + (float)rng.NextDouble() * 2.5f;
                UIStyles.CircleTex(new Vector2(sx, sy), sz, new Color(1f, 1f, 1f, 0.25f + 0.6f * tw));
            }
            // The moon.
            Vector2 moon = new Vector2(banner.xMax - 190f, banner.y + 170f);
            for (int i = 4; i >= 1; i--) UIStyles.CircleTex(moon, 110f + i * 22f, new Color(1f, 0.55f, 0.65f, 0.05f));
            UIStyles.CircleTex(moon, 110f, new Color(1f, 0.86f, 0.88f, 0.9f));
            UIStyles.CircleTex(moon + new Vector2(-30f, -20f), 26f, new Color(0.95f, 0.72f, 0.78f, 0.6f));
            UIStyles.CircleTex(moon + new Vector2(34f, 30f), 18f, new Color(0.95f, 0.72f, 0.78f, 0.6f));

            // Title block.
            var tag = new Rect(banner.x + 30f, banner.y + 26f, 230f, 42f);
            Round(tag, new Color(0.95f, 0.25f, 0.45f), 21f);
            GUI.Label(tag, "<b>LIMITED BANNER</b>", UIStyles.Sized(UIStyles.Center, 20));
            UIStyles.Outlined(new Rect(banner.x + 30f, banner.y + 74f, bw - 60f, 80f), SummonSystem.BannerName, UIStyles.Sized(UIStyles.H1, 64), Color.white, 4f);
            GUI.Label(new Rect(banner.x + 34f, banner.y + 150f, bw - 60f, 36f), "<color=#FFB3C6>3 featured MYTHIC slayers · rate up</color>", UIStyles.Sized(UIStyles.Body, 24));

            // Three featured Mythics: the picked one stands in front, larger.
            int n = SummonSystem.FeaturedIds.Length;
            float slotW = (bw - 40f) / n;
            float baseY = banner.yMax - 150f;
            for (int pass = 0; pass < 2; pass++)
                for (int i = 0; i < n; i++)
                {
                    bool picked = i == bannerPick;
                    if ((pass == 0) == picked) continue; // draw the picked one last so it sits on top
                    var def = GameDatabase.GetCharacter(SummonSystem.FeaturedIds[i]);
                    if (def == null) continue;
                    float ek = Enter(0.15f + i * 0.12f, 0.5f);
                    if (ek <= 0f) continue;
                    Color ec = ElementChart.ColorOf(def.element);
                    float cx = banner.x + 20f + slotW * (i + 0.5f);
                    float h = picked ? 560f : 460f;
                    float bob = Mathf.Sin(Time.unscaledTime * 1.6f + i * 1.3f) * 6f;
                    // Aura and rays.
                    Vector2 auraC = new Vector2(cx, baseY - h * 0.45f);
                    float pulse = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f + i);
                    var saved = GUI.matrix;
                    for (int r = 0; r < 8; r++)
                    {
                        GUI.matrix = saved;
                        RotateGui(r * 45f + Time.unscaledTime * (picked ? 18f : 8f) * (i % 2 == 0 ? 1f : -1f), auraC);
                        UIStyles.Rect(new Rect(auraC.x, auraC.y - 10f, h * 0.55f * ek, 20f), new Color(ec.r, ec.g, ec.b, picked ? 0.1f : 0.05f));
                    }
                    GUI.matrix = saved;
                    for (int r = 4; r >= 1; r--)
                        UIStyles.CircleTex(auraC, h * (0.18f + r * 0.06f + pulse * 0.01f), new Color(ec.r, ec.g, ec.b, (picked ? 0.09f : 0.05f)));
                    // Full-body art.
                    var art = ArtLibrary.CharacterFull(def);
                    var ar = new Rect(cx - h * 0.3f, baseY - h + bob + (1f - ek) * 80f, h * 0.6f, h);
                    if (art != null)
                    {
                        var oc = GUI.color;
                        GUI.color = new Color(picked ? 1f : 0.7f, picked ? 1f : 0.7f, picked ? 1f : 0.78f, ek);
                        GUI.DrawTexture(ar, art, ScaleMode.ScaleToFit, true);
                        GUI.color = oc;
                    }
                    // Name plate.
                    var np = new Rect(cx - slotW * 0.46f, baseY + 6f, slotW * 0.92f, 128f);
                    Round(np, new Color(0.04f, 0.03f, 0.08f, picked ? 0.9f : 0.75f), 14f);
                    RoundFrame(np, picked ? RarityInfo.Color(6) : new Color(1f, 1f, 1f, 0.15f), picked ? 3f : 2f, 14f);
                    UIStyles.CircleTex(new Vector2(np.x + 28f, np.y + 30f), 18f, Color.Lerp(ec, Color.black, 0.2f));
                    GUI.DrawTexture(new Rect(np.x + 15f, np.y + 17f, 26f, 26f), IconFactory.Get(IconFactory.ForElement(def.element)), ScaleMode.ScaleToFit, true);
                    GUI.Label(new Rect(np.x + 54f, np.y + 8f, np.width - 60f, 40f), def.displayName, UIStyles.Sized(UIStyles.H2, picked ? 26 : 22));
                    GUI.Label(new Rect(np.x + 14f, np.y + 46f, np.width - 20f, 28f), "<color=#BBBBBB>" + def.versionTitle + "</color>", UIStyles.Sized(UIStyles.Small, 17));
                    UIStyles.Outlined(new Rect(np.x + 14f, np.y + 72f, np.width - 20f, 26f), "MYTHIC  " + Stars(6), UIStyles.Sized(UIStyles.Small, 17), RarityInfo.Color(6), 1.2f);
                    GUI.Label(new Rect(np.x + 14f, np.y + 98f, np.width - 20f, 26f), "<color=#FFD36B>✦ " + def.ultimate.name + "</color>", UIStyles.Sized(UIStyles.Small, 15));
                    // Tap to feature.
                    var hit = new Rect(cx - slotW * 0.46f, baseY - h, slotW * 0.92f, h + 134f);
                    if (!picked && GUI.Button(hit, GUIContent.none, GUIStyle.none)) { bannerPick = i; gm.Audio.Play("switch", 0.5f); }
                }
            var pd = GameDatabase.GetCharacter(SummonSystem.FeaturedIds[Mathf.Clamp(bannerPick, 0, n - 1)]);
            if (pd != null)
                GUI.Label(new Rect(banner.x + 34f, banner.y + 186f, bw * 0.62f, 60f), "<i><color=#DDDDEE>" + pd.description + "</color></i>", UIStyles.Sized(UIStyles.Small, 19));

            // ---- Right column: rates, pity and the summon buttons.
            float rx = banner.xMax + 24f, rw = safe.xMax - 24f - rx;
            float rk = Enter(0.2f, 0.4f);
            var rates = new Rect(rx + (1f - rk) * 200f, top, rw, 420f);
            Round(Offset(rates, 0f, 5f), new Color(0f, 0f, 0f, 0.4f), 18f);
            Round(rates, new Color(0.05f, 0.06f, 0.11f, 0.94f), 18f);
            GUI.Label(new Rect(rates.x + 24f, rates.y + 16f, rw - 48f, 40f), "RATES", UIStyles.Sized(UIStyles.H2, 28));
            float y = rates.y + 62f;
            for (int i = SummonSystem.Rates.Length - 1; i >= 0; i--)
            {
                int rarity = 2 + i;
                Color rc = RarityInfo.Color(rarity);
                Round(new Rect(rates.x + 18f, y, rw - 36f, 44f), new Color(rc.r, rc.g, rc.b, 0.1f), 10f);
                UIStyles.Colored(new Rect(rates.x + 32f, y, 200f, 44f), RarityInfo.Name(rarity), UIStyles.Sized(UIStyles.Body, 21), rc);
                UIStyles.Colored(new Rect(rates.x + 170f, y, 200f, 44f), Stars(rarity), UIStyles.Sized(UIStyles.Body, 18), Color.Lerp(rc, new Color(1f, 0.85f, 0.3f), 0.5f));
                GUI.Label(new Rect(rates.x + 24f, y, rw - 60f, 44f), (SummonSystem.Rates[i] * 100f).ToString("0.#") + "%", UIStyles.Sized(UIStyles.Right, 22));
                y += 50f;
            }
            GUI.Label(new Rect(rates.x + 24f, y + 6f, rw - 48f, 90f), "<color=#AAAAAA>Every ×10 guarantees EPIC or better.\nPity: a featured MYTHIC within " + SummonSystem.PityLimit + " summons.\nDuplicates can be fed as EXP or sold for gold.</color>", UIStyles.Sized(UIStyles.Small, 17));

            var pity = new Rect(rates.x, rates.yMax + 18f, rw, 96f);
            Round(pity, new Color(0.05f, 0.06f, 0.11f, 0.94f), 18f);
            GUI.Label(new Rect(pity.x + 24f, pity.y + 10f, rw - 48f, 34f), "Mythic pity  <b>" + d.summonPity + "</b> / " + SummonSystem.PityLimit, UIStyles.Sized(UIStyles.Body, 22));
            float pf = (float)d.summonPity / SummonSystem.PityLimit;
            Round(new Rect(pity.x + 24f, pity.y + 54f, rw - 48f, 22f), new Color(0f, 0f, 0f, 0.5f), 11f);
            if (pf > 0.01f) Round(new Rect(pity.x + 26f, pity.y + 56f, (rw - 52f) * pf, 18f), Color.Lerp(RarityInfo.Color(6), Color.white, 0.25f * Mathf.Sin(Time.unscaledTime * 4f)), 9f);

            // Summon buttons with diamond costs.
            float by = H - 290f;
            SummonButton(new Rect(rx, by, rw, 118f), "SUMMON ×1", SummonSystem.SingleCost, new Color(0.25f, 0.3f, 0.6f), SummonSystem.CanAfford(d, 1), 1, 0.3f);
            SummonButton(new Rect(rx, by + 134f, rw, 132f), "SUMMON ×10", SummonSystem.MultiCost, TileRed, SummonSystem.CanAfford(d, 10), 10, 0.4f);
            if (!SummonSystem.CanAfford(d, 1))
                GUI.Label(new Rect(rx, by - 64f, rw, 56f), "<color=#FF9C7A>Earn diamonds from mission stars, daily login and demons.</color>", UIStyles.Sized(UIStyles.CenterSmall, 18));
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
            string cs = cost.ToString();
            float cw = UIStyles.Sized(UIStyles.Body, 28).CalcSize(new GUIContent(cs)).x;
            float cx = r.center.x - (cw + 40f) * 0.5f;
            DiamondIcon(new Vector2(cx + 14f, r.y + r.height * 0.72f), 30f);
            GUI.Label(new Rect(cx + 36f, r.y + r.height * 0.52f, cw + 10f, r.height * 0.4f), cs, UIStyles.Sized(UIStyles.Body, 28));
        }

        void DoSummon(int count)
        {
            var results = SummonSystem.Summon(gm.Data, count);
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
            else GUI.Label(new Rect(card.xMax - 380f, card.y + 330f, 360f, 50f), "<color=#FF9C7A>DUPLICATE → feed as EXP or sell</color>", UIStyles.Sized(UIStyles.Right, 22));
            GUI.Label(new Rect(0f, H - 90f, W, 50f), "<color=#BBBBBB>Tap to continue</color>", UIStyles.Sized(UIStyles.Center, 26));
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
            if (FlatBtn(new Rect(W * 0.5f + 30f, by, 440f, 100f), res.Count >= 10 ? "SUMMON ×10 AGAIN" : "SUMMON AGAIN", TileRed, SummonSystem.CanAfford(d, res.Count), 32))
                DoSummon(res.Count);
        }
    }
}
