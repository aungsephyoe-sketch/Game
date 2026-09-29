using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// A slayer's full sheet, opened from the summon banner or a pulled card: action art, rarity, element, role and
    /// weapon, stats at level 1 next to fully starred, awakened max level, every ability with what it does, and the
    /// element matchup.
    /// </summary>
    public partial class UIManager
    {
        CharacterDefinition sheetDef;
        float sheetOpenedAt;
        Vector2 sheetScroll;

        void OpenSheet(CharacterDefinition def)
        {
            sheetDef = def;
            sheetOpenedAt = Time.unscaledTime;
            sheetScroll = Vector2.zero;
            if (gm != null) gm.Audio.Play("click", 0.5f);
        }

        /// <summary>Draws the sheet if one is open; true while it's showing (the screen below should ignore input).</summary>
        bool DrawSlayerSheet()
        {
            var def = sheetDef;
            if (def == null) return false;
            float k = 1f - Mathf.Pow(1f - Mathf.Clamp01((Time.unscaledTime - sheetOpenedAt) / 0.25f), 3f);
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.75f * k));
            var panel = new Rect(W * 0.5f - 820f, H * 0.5f - 430f + (1f - k) * 60f, 1640f, 860f);
            Color ec = ElementChart.ColorOf(def.element), rc = RarityInfo.Color(def.rarity);
            Round(Offset(panel, 0f, 8f), new Color(0f, 0f, 0f, 0.5f), 26f);
            Round(panel, new Color(0.07f, 0.07f, 0.13f, 0.98f), 26f);
            RoundFrame(panel, rc, 3f, 26f);

            // Left: art on its element backdrop.
            var artR = new Rect(panel.x + 20f, panel.y + 20f, 520f, panel.height - 40f);
            GUI.BeginGroup(artR);
            promoOrigin = artR.position;
            PromoBackdrop(new Rect(0f, 0f, artR.width, artR.height), ThemeFor(def.element), new Vector2(artR.width * 0.5f, artR.height * 0.45f), def.rarity * 1.3f, artR.height * 0.8f);
            var art = ActionArt(def);
            if (art != null) GUI.DrawTexture(new Rect(10f, 30f, artR.width - 20f, artR.height - 80f), art, ScaleMode.ScaleToFit, true);
            GUI.EndGroup();
            UIStyles.Outlined(new Rect(artR.x, artR.yMax - 60f, artR.width, 50f), RarityInfo.Name(def.rarity) + "  " + Stars(Mathf.Min(def.rarity, 7)), UIStyles.Sized(UIStyles.Center, 30), rc, 3f);

            // Right: name, facts, stats, abilities.
            float x = artR.xMax + 30f, w = panel.xMax - x - 30f, y = panel.y + 24f;
            UIStyles.Outlined(new Rect(x, y, w - 80f, 64f), def.FullName, UIStyles.Sized(UIStyles.H1, 50), Color.white, 3f);
            if (FlatBtn(new Rect(panel.xMax - 90f, panel.y + 20f, 68f, 64f), "✕", new Color(0.35f, 0.2f, 0.3f), true, 30)) { sheetDef = null; return true; }
            y += 64f;
            GUI.Label(new Rect(x, y, w, 34f), "<color=#" + UIStyles.Hex(ec) + "><b>" + ElementChart.Name(def.element).ToUpperInvariant() + "</b></color>   ·   " + def.role + "   ·   " + def.weapon + "   ·   <i>" + def.breathingStyle + "</i>", UIStyles.Sized(UIStyles.Body, 22));
            y += 36f;
            string hint = ElementChart.Neutral(def.element) ? "Neutral element: no advantage or weakness."
                : "Strong vs " + ElementChart.Name(ElementChart.StrongAgainst(def.element)) + " (+15%)   ·   weak vs " + ElementChart.Name(ElementChart.WeakTo(def.element)) + " (−15%)";
            GUI.Label(new Rect(x, y, w, 30f), "<color=#AAB0C8>" + hint + "</color>", UIStyles.Sized(UIStyles.Small, 19));
            y += 36f;
            if (!string.IsNullOrEmpty(def.description))
            {
                var ds = new GUIStyle(UIStyles.Sized(UIStyles.Small, 19)) { wordWrap = true };
                GUI.Label(new Rect(x, y, w, 56f), "<i><color=#D8D8E6>" + def.description + "</color></i>", ds);
                y += 60f;
            }

            // Stats: level 1 → max.
            var lv1 = new OwnedCharacter { id = def.id, level = 1, stars = Mathf.Min(def.rarity, CharacterSystem.MaxStars) };
            var mx = new OwnedCharacter { id = def.id, stars = CharacterSystem.MaxStars, awaken = ExperienceSystem.MaxAwaken, skillLevels = new[] { 10, 10, 10, 10 } };
            mx.level = ExperienceSystem.Cap(mx);
            var empty = new PlayerData();
            var s1 = CharacterSystem.ComputeStats(empty, lv1);
            var s2 = CharacterSystem.ComputeStats(empty, mx);
            var st = new Rect(x, y, w * 0.46f, 330f);
            Round(st, new Color(1f, 1f, 1f, 0.05f), 14f);
            GUI.Label(new Rect(st.x + 16f, st.y + 8f, st.width - 32f, 32f), "<b>STATS</b>", UIStyles.Sized(UIStyles.Body, 22));
            GUI.Label(new Rect(st.x + 16f, st.y + 8f, st.width - 32f, 32f), "<color=#AAAAAA>Lv.1  →  Lv." + mx.level + "</color>", UIStyles.Sized(UIStyles.Right, 18));
            string[] names = { "HP", "ATK", "DEF", "CRIT", "CRIT DMG", "SPEED", "SPECIAL" };
            string[] a1 = { N(s1.hp), N(s1.atk), N(s1.def), Pct(s1.crit), Pct(s1.critDmg), s1.speed.ToString("0.0"), Pct(s1.specialDmg) };
            string[] a2 = { N(s2.hp), N(s2.atk), N(s2.def), Pct(s2.crit), Pct(s2.critDmg), s2.speed.ToString("0.0"), Pct(s2.specialDmg) };
            for (int i = 0; i < names.Length; i++)
            {
                float ry = st.y + 46f + i * 40f;
                if (i % 2 == 0) Round(new Rect(st.x + 8f, ry - 2f, st.width - 16f, 38f), new Color(1f, 1f, 1f, 0.03f), 8f);
                GUI.Label(new Rect(st.x + 18f, ry, 150f, 34f), "<color=#BBBBCC>" + names[i] + "</color>", UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(st.x + 150f, ry, 150f, 34f), a1[i], UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(st.x + 290f, ry, st.width - 300f, 34f), "→  <color=#7CFF8A><b>" + a2[i] + "</b></color>", UIStyles.Sized(UIStyles.Body, 20));
            }
            if (CharacterSystem.IsMythic(def))
                GUI.Label(new Rect(st.x + 16f, st.yMax - 36f, st.width - 32f, 30f), "<color=#FF9CF0>MYTHIC: +20% HP/ATK, +12% crit, cinematic strong attacks & special encore</color>", UIStyles.Sized(UIStyles.Small, 15));

            // Abilities.
            var ab = new Rect(st.xMax + 16f, y, x + w - st.xMax - 16f, panel.yMax - y - 24f);
            Round(ab, new Color(1f, 1f, 1f, 0.05f), 14f);
            GUI.Label(new Rect(ab.x + 16f, ab.y + 8f, ab.width - 32f, 32f), "<b>ABILITIES</b>", UIStyles.Sized(UIStyles.Body, 22));
            var wrap = new GUIStyle(UIStyles.Sized(UIStyles.Small, 18)) { wordWrap = true };
            float ay = ab.y + 46f;
            for (int i = 0; i < 4; i++)
            {
                var a = i < 3 ? (def.skills != null && i < def.skills.Length ? def.skills[i] : null) : def.ultimate;
                if (a == null) continue;
                bool ult = i == 3;
                var row = new Rect(ab.x + 10f, ay, ab.width - 20f, 108f);
                Round(row, ult ? new Color(rc.r, rc.g, rc.b, 0.14f) : new Color(1f, 1f, 1f, 0.04f), 10f);
                UIStyles.CircleTex(new Vector2(row.x + 34f, row.y + 34f), 24f, ult ? rc : ec);
                GUI.DrawTexture(new Rect(row.x + 18f, row.y + 18f, 32f, 32f), IconFactory.Get(IconFactory.ForSkill(a.shape)), ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(row.x + 68f, row.y + 6f, row.width - 80f, 30f), (ult ? "<color=#FFD36B>SPECIAL · </color>" : "<color=#AAAAAA>SKILL " + (i + 1) + " · </color>") + "<b>" + a.name + "</b>", UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(row.x + 68f, row.y + 34f, row.width - 80f, 48f), "<color=#D0D0DC>" + (string.IsNullOrEmpty(a.description) ? "A " + a.shape.ToString().ToLower() + " attack." : a.description) + "</color>", wrap);
                GUI.Label(new Rect(row.x + 68f, row.y + 80f, row.width - 80f, 26f), "<color=#8890A8>" + Mathf.RoundToInt(a.damageMultiplier * 100f) + "% ATK" + (a.hits > 1 ? " × " + a.hits + " hits" : "") + "   ·   " + (ult ? "charges with energy" : a.cooldown.ToString("0.#") + "s cooldown") + "</color>", UIStyles.Sized(UIStyles.Small, 15));
                ay += 116f;
            }
            // Clicks don't fall through to the screen below.
            GUI.Button(new Rect(0f, 0f, W, H), GUIContent.none, GUIStyle.none);
            return true;
        }

        static string N(float v) { return Mathf.RoundToInt(v).ToString("N0"); }
        static string Pct(float v) { return Mathf.RoundToInt(v * 100f) + "%"; }
    }
}
