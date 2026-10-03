using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// Gear details popup (tap an item in the shop): the item's painted art, rarity and slot, every stat at Lv 1 and
    /// at max level, what its special effect does in plain words, where it drops, how many you own, and BUY.
    /// Tap outside or ✕ to close.
    /// </summary>
    public partial class UIManager
    {
        EquipmentDefinition gearInfo;
        float gearInfoAt;

        /// <summary>A plain explanation of an accessory effect: what it does and when it is worth it.</summary>
        static string EffectHelp(string fx)
        {
            switch (fx)
            {
                case "bloom": return "Every demon you defeat heals you for 3% of your max HP. Great in long fights against many small demons.";
                case "fortune": return "While anyone in your team wears it, every battle pays 30% more gold. Stacks with Gold Rush.";
                case "wisdom": return "While anyone in your team wears it, every slayer in the team earns 30% more EXP from battles.";
                case "burn": return "Each hit has a 15% chance to burst into flame, dealing extra fire damage on top of the hit.";
                case "frost": return "Each hit has a 12% chance to freeze the demon — a big stagger that stops it in its tracks.";
                case "guard": return "All damage you take is reduced by 12%, on top of your DEF. Good on whoever leads the team.";
                case "swift": return "Your dodge comes back 35% sooner, and you move a little faster. Dodge, dodge, dodge.";
                case "thorns": return "Whenever a demon hits you, it takes 25% of that damage back.";
                case "lifesteal": return "You heal for 6% of all the damage you deal — the harder you hit, the more you heal.";
                case "thunder": return "Every 5th hit you land calls a lightning bolt down on the demon, hitting everything near it.";
                case "fury": return "While you are below 40% HP, everything you do deals 30% more damage.";
                case "ultcharge": return "Your special gauge fills 35% faster from your hits, so you can use your special more often.";
                case "serpent": return "Every critical hit heals you for 1.5% of your max HP, and your crits hit 20% harder.";
                case "ambush": return "After you dodge, your next hit within 1.5 seconds deals double damage. Dodge in, strike hard.";
                case "revive": return "The first time you would fall in a battle, you rise again with 35% HP and a moment of invincibility.";
                case "eclipse": return "+15% damage on every hit, you heal for 5% of the damage you deal, and every 4th hit calls lightning.";
                default: return "";
            }
        }

        static List<string> StatLines(StatBlock b)
        {
            var l = new List<string>();
            if (b.hp > 0f) l.Add("HP +" + Mathf.RoundToInt(b.hp).ToString("N0"));
            if (b.atk > 0f) l.Add("ATK +" + Mathf.RoundToInt(b.atk).ToString("N0"));
            if (b.def > 0f) l.Add("DEF +" + Mathf.RoundToInt(b.def).ToString("N0"));
            if (b.crit > 0f) l.Add("CRIT +" + (b.crit * 100f).ToString("0.#") + "%");
            if (b.critDmg > 0f) l.Add("CRIT DMG +" + (b.critDmg * 100f).ToString("0.#") + "%");
            if (b.speed > 0f) l.Add("SPD +" + b.speed.ToString("0.##"));
            if (b.specialDmg > 0f) l.Add("SPECIAL DMG +" + (b.specialDmg * 100f).ToString("0.#") + "%");
            return l;
        }

        static string DropSources(EquipmentDefinition e)
        {
            var names = new List<string>();
            foreach (var ev in GameDatabase.Events)
                foreach (var q in ev.quests)
                    if (q.dropTable.Contains(e.id) && !names.Contains(ev.title)) names.Add(ev.title);
            return names.Count == 0 ? "" : string.Join(", ", names.ToArray());
        }

        void DrawGearInfo(PlayerData d)
        {
            var e = gearInfo;
            if (e == null) return;
            float t = Time.unscaledTime - gearInfoAt;
            float pop = AdBack(t / 0.28f);
            Color rc = RarityInfo.Color(e.rarity);
            // Dim the shop; tapping outside the panel closes.
            UIStyles.Rect(new Rect(0f, 0f, W, H), new Color(0f, 0f, 0f, 0.65f * Mathf.Clamp01(t / 0.15f)));
            float pw = Mathf.Min(1100f, W - 120f), ph = Mathf.Min(720f, H - 120f);
            var full = new Rect((W - pw) * 0.5f, (H - ph) * 0.5f, pw, ph);
            var panel = new Rect(full.center.x - pw * 0.5f * pop, full.center.y - ph * 0.5f * pop, pw * pop, ph * pop);
            var ev = Event.current;
            if (ev.type == EventType.MouseDown && !full.Contains(ev.mousePosition)) { gearInfo = null; ev.Use(); return; }
            if (pop < 0.6f) { Round(panel, new Color(0.06f, 0.07f, 0.12f, 0.97f), 22f); return; }

            Round(Offset(full, 0f, 8f), new Color(0f, 0f, 0f, 0.45f), 24f);
            Round(full, new Color(0.06f, 0.07f, 0.12f, 0.97f), 22f);
            Round(new Rect(full.x, full.y, full.width, 10f), rc, 5f);
            RoundFrame(full, new Color(rc.r, rc.g, rc.b, 0.6f), 3f, 22f);
            if (FlatBtn(new Rect(full.xMax - 84f, full.y + 22f, 64f, 60f), "✕", new Color(0.35f, 0.2f, 0.3f), true, 28)) { gearInfo = null; return; }

            // Left: the art, floating in a glow of its rarity colour.
            var art = new Rect(full.x + 40f, full.y + 70f, 340f, 340f);
            float bob = Mathf.Sin(Time.unscaledTime * 1.8f) * 6f;
            float breathe = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.4f);
            for (int i = 4; i >= 1; i--) UIStyles.CircleTex(art.center, art.width * (0.32f + i * 0.09f + breathe * 0.02f), new Color(rc.r, rc.g, rc.b, 0.07f));
            var tex = GearArt.Get(e);
            if (tex != null) GUI.DrawTexture(Offset(art, 0f, bob), tex, ScaleMode.ScaleToFit, true);
            AdSparkBurst(art.center, t, 0.15f, Color.Lerp(rc, Color.white, 0.4f), 200f);
            UIStyles.Outlined(new Rect(art.x, art.yMax + 10f, art.width, 34f), RarityInfo.Name(e.rarity) + "  ·  " + SlotName(e.slot), UIStyles.Sized(UIStyles.Center, 22), rc, 1.5f);
            int owned = d.equipment.FindAll(it => it.defId == e.id).Count;
            GUI.Label(new Rect(art.x, art.yMax + 48f, art.width, 30f), "<color=#AAB0C8>You own " + owned + "</color>", UIStyles.Sized(UIStyles.Center, 19));

            // Right: name, effect, stats, sources.
            float x = art.xMax + 40f, w = full.xMax - 40f - x, y = full.y + 34f;
            GUI.Label(new Rect(x, y, w - 70f, 52f), "<b>" + e.displayName + "</b>", UIStyles.Sized(UIStyles.H1, 38));
            y += 62f;
            string help = EffectHelp(e.effect);
            var box = new Rect(x, y, w, string.IsNullOrEmpty(help) ? 70f : 150f);
            Round(box, new Color(rc.r * 0.25f, rc.g * 0.25f, rc.b * 0.25f, 0.9f), 14f);
            GUI.BeginGroup(box);
            promoOrigin = box.position;
            AdSweep(new Rect(0f, 0f, box.width, box.height), 3f, 0.5f, 0.8f);
            GUI.EndGroup();
            var ws = new GUIStyle(UIStyles.Sized(UIStyles.Body, 21)) { wordWrap = true };
            if (string.IsNullOrEmpty(help))
                GUI.Label(new Rect(box.x + 18f, box.y + 8f, box.width - 36f, box.height - 16f), "<b>STAT BOOST</b>   <color=#DADAE6>" + e.description + "</color>", ws);
            else
            {
                GUI.Label(new Rect(box.x + 18f, box.y + 10f, box.width - 36f, 30f), "<color=#FFD36B><b>✦ SPECIAL EFFECT</b></color>", UIStyles.Sized(UIStyles.Body, 20));
                GUI.Label(new Rect(box.x + 18f, box.y + 42f, box.width - 36f, box.height - 50f), "<color=#F0F0FA>" + help + "</color>", ws);
            }
            y = box.yMax + 20f;
            // Stats now and at max level, side by side.
            var lv1 = StatLines(e.BonusAt(1));
            var lvMax = StatLines(e.BonusAt(e.maxLevel));
            GUI.Label(new Rect(x, y, w * 0.5f, 30f), "<color=#AAB0C8><b>AT LV 1</b></color>", UIStyles.Sized(UIStyles.Body, 19));
            GUI.Label(new Rect(x + w * 0.5f, y, w * 0.5f, 30f), "<color=#AAB0C8><b>AT LV " + e.maxLevel + " (MAX)</b></color>", UIStyles.Sized(UIStyles.Body, 19));
            y += 32f;
            int rows = Mathf.Max(lv1.Count, lvMax.Count);
            for (int i = 0; i < rows; i++)
            {
                float rk = AdEase((t - 0.2f - i * 0.05f) / 0.25f);
                var row = new Rect(x + (1f - rk) * 60f, y + i * 34f, w, 32f);
                if (i % 2 == 0) Round(new Rect(x, row.y, w, 32f), new Color(1f, 1f, 1f, 0.04f), 8f);
                if (i < lv1.Count) GUI.Label(new Rect(row.x + 10f, row.y, w * 0.5f - 10f, 32f), "<color=#8CFF9E>" + lv1[i] + "</color>", UIStyles.Sized(UIStyles.Body, 20));
                if (i < lvMax.Count) GUI.Label(new Rect(row.x + w * 0.5f, row.y, w * 0.5f, 32f), "<color=#FFD36B>" + lvMax[i] + "</color>", UIStyles.Sized(UIStyles.Body, 20));
            }
            y += rows * 34f + 12f;
            string src = DropSources(e);
            GUI.Label(new Rect(x, y, w, 30f), "<color=#AAB0C8>Get it: gear shop" + (src.Length > 0 ? "  ·  drops in " + src : "") + "  ·  equip from a slayer's Gear tab</color>", new GUIStyle(UIStyles.Sized(UIStyles.Small, 17)) { wordWrap = true });

            // Buy.
            int price = ShopSystem.AccessoryPrice(e);
            bool can = d.coins >= price;
            var br = new Rect(x, full.yMax - 96f, w, 72f);
            if (FlatBtn(br, "", TileGreen, can, 22))
            {
                if (ShopSystem.BuyAccessory(d, e))
                {
                    gm.Save();
                    gm.Audio.Play("coin", 0.8f);
                    Toast("Bought " + e.displayName + " · equip it from a slayer's Gear tab");
                    gearInfoAt = Time.unscaledTime - 0.14f; // a little celebratory sparkle again
                }
            }
            CoinIcon(new Vector2(br.center.x - 110f, br.center.y), 36f);
            GUI.Label(new Rect(br.center.x - 84f, br.y, 300f, br.height), "BUY  " + price.ToString("N0"), UIStyles.Sized(UIStyles.Body, 28));
            if (!can) GUI.Label(new Rect(br.x, br.y - 28f, br.width, 26f), "<color=#FF9C7A>Not enough gold</color>", UIStyles.Sized(UIStyles.Center, 17));
        }
    }
}
