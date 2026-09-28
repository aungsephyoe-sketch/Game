using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// INVENTORY: every piece of gear you own (with its picture, rarity, level and who wears it) and the GEAR SHOP
    /// — three featured Mythic pieces up top, then everything else from Common to Legendary.
    /// </summary>
    public partial class UIManager
    {
        int invTab, invFilter = -1;
        string invPick;
        Vector2 invScroll, gearShopScroll;

        public static int GearGoldPrice(EquipmentDefinition e)
        {
            switch (Mathf.Clamp(e.rarity, 2, 6)) { case 2: return 2000; case 3: return 6000; case 4: return 15000; default: return 0; }
        }

        public static int GearDiamondPrice(EquipmentDefinition e)
        {
            switch (Mathf.Clamp(e.rarity, 2, 6)) { case 5: return 400; case 6: return 1200; default: return 0; }
        }

        static string StatText(StatBlock b)
        {
            var parts = new List<string>();
            if (b.hp > 0) parts.Add("HP +" + Mathf.RoundToInt(b.hp));
            if (b.atk > 0) parts.Add("ATK +" + Mathf.RoundToInt(b.atk));
            if (b.def > 0) parts.Add("DEF +" + Mathf.RoundToInt(b.def));
            if (b.crit > 0) parts.Add("CRIT +" + Mathf.RoundToInt(b.crit * 100f) + "%");
            if (b.critDmg > 0) parts.Add("CRIT DMG +" + Mathf.RoundToInt(b.critDmg * 100f) + "%");
            if (b.speed > 0) parts.Add("SPD +" + b.speed.ToString("0.0"));
            if (b.specialDmg > 0) parts.Add("SPECIAL +" + Mathf.RoundToInt(b.specialDmg * 100f) + "%");
            return string.Join("   ", parts.ToArray());
        }

        static string SlotName(EquipSlot s) { return s == EquipSlot.Sword ? "WEAPON" : s == EquipSlot.Haori ? "HAORI" : "ACCESSORY"; }

        void DrawInventory()
        {
            TopBar("INVENTORY", GameScreen.MainMenu);
            var d = gm.Data;
            string[] tabs = { "MY GEAR", "GEAR SHOP" };
            for (int t = 0; t < 2; t++)
                if (FlatBtn(new Rect(safe.x + 30f + t * 260f, safe.y + 120f, 240f, 60f), tabs[t], invTab == t ? TileRed : TileNavy, true, 22)) invTab = t;
            if (invTab == 1) DrawGearShop(d);
            else DrawMyGear(d);
        }

        /// <summary>A gear card: picture, rarity frame, name, stars and level.</summary>
        void GearCard(Rect r, EquipmentDefinition e, EquipmentItem item, bool selected, string footer)
        {
            Color rc = RarityInfo.Color(e.rarity);
            if (e.rarity >= 5)
            {
                float g = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 3f + r.x * 0.01f);
                for (int i = 2; i >= 1; i--) Round(Grow(r, i * 4f), new Color(rc.r, rc.g, rc.b, 0.06f + 0.05f * g), 14f + i * 4f);
            }
            Round(Offset(r, 0f, 4f), new Color(0f, 0f, 0f, 0.4f), 14f);
            Round(r, Color.Lerp(new Color(0.07f, 0.07f, 0.12f), rc, 0.16f), 14f);
            var img = new Rect(r.x + 10f, r.y + 10f, r.width - 20f, r.width - 20f);
            var tex = GearArt.Get(e);
            if (tex != null) GUI.DrawTexture(img, tex, ScaleMode.ScaleToFit, true);
            GUI.Label(new Rect(r.x + 8f, img.yMax + 2f, r.width - 16f, 26f), e.displayName, UIStyles.Sized(UIStyles.CenterSmall, e.displayName.Length > 16 ? 13 : 15));
            UIStyles.Outlined(new Rect(r.x + 8f, img.yMax + 26f, r.width - 16f, 22f), RarityInfo.Name(e.rarity) + (item != null ? "  Lv." + item.level : ""), UIStyles.Sized(UIStyles.CenterSmall, 13), rc, 1.2f);
            if (!string.IsNullOrEmpty(footer)) GUI.Label(new Rect(r.x + 8f, r.yMax - 26f, r.width - 16f, 22f), footer, UIStyles.Sized(UIStyles.CenterSmall, 13));
            RoundFrame(r, selected ? Color.white : Color.Lerp(rc, Color.black, 0.2f), selected ? 3f : 2f, 14f);
        }

        void DrawMyGear(PlayerData d)
        {
            float top = safe.y + 200f;
            // Slot filters.
            string[] fl = { "ALL", "WEAPON", "HAORI", "ACCESSORY" };
            for (int i = 0; i < 4; i++)
                if (FlatBtn(new Rect(safe.x + 30f + i * 190f, top, 176f, 50f), fl[i], invFilter == i - 1 ? new Color(0.3f, 0.36f, 0.6f) : new Color(0.12f, 0.13f, 0.2f), true, 18)) invFilter = i - 1;
            top += 64f;
            var items = new List<EquipmentItem>();
            foreach (var it in d.equipment)
            {
                var def = GameDatabase.GetEquipment(it.defId);
                if (def == null) continue;
                if (invFilter >= 0 && (int)def.slot != invFilter) continue;
                items.Add(it);
            }
            items.Sort((a, b) => GameDatabase.GetEquipment(b.defId).rarity.CompareTo(GameDatabase.GetEquipment(a.defId).rarity));

            var detail = new Rect(safe.xMax - 500f, safe.y + 200f, 470f, H - safe.y - 230f);
            var area = new Rect(safe.x + 30f, top, detail.x - safe.x - 60f, H - top - 30f);
            Round(area, new Color(0.05f, 0.06f, 0.1f, 0.85f), 16f);
            if (items.Count == 0)
            {
                GUI.Label(area, "<color=#AAAAAA>No gear yet — visit the GEAR SHOP, clear missions and events.</color>", UIStyles.CenterSmall);
            }
            float cw = 170f, ch = 250f, gap = 14f;
            var view = new Rect(area.x + 14f, area.y + 14f, area.width - 28f, area.height - 28f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((view.width - 20f + gap) / (cw + gap)));
            var content = new Rect(0f, 0f, view.width - 20f, Mathf.CeilToInt(items.Count / (float)cols) * (ch + gap));
            invScroll = GUI.BeginScrollView(view, invScroll, content);
            for (int i = 0; i < items.Count; i++)
            {
                var it = items[i];
                var def = GameDatabase.GetEquipment(it.defId);
                var r = new Rect((i % cols) * (cw + gap), (i / cols) * (ch + gap), cw, ch);
                var who = d.WhoEquipped(it.uid);
                var whoDef = who != null ? GameDatabase.GetCharacter(who.id) : null;
                GearCard(r, def, it, invPick == it.uid, whoDef != null ? "<color=#7CFF8A>on " + whoDef.displayName + "</color>" : "<color=#777777>unequipped</color>");
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) { invPick = it.uid; gm.Audio.Play("click", 0.4f); }
            }
            GUI.EndScrollView();

            // Detail and actions.
            Round(detail, new Color(0.05f, 0.06f, 0.1f, 0.94f), 18f);
            var sel = d.GetItem(invPick);
            var sd = sel != null ? GameDatabase.GetEquipment(sel.defId) : null;
            if (sd == null)
            {
                GUI.Label(detail, "<color=#AAAAAA>Pick a piece of gear.</color>", UIStyles.CenterSmall);
                return;
            }
            float x = detail.x + 24f, w = detail.width - 48f, y = detail.y + 20f;
            var big = new Rect(detail.center.x - 110f, y, 220f, 220f);
            var bt = GearArt.Get(sd);
            if (bt != null) GUI.DrawTexture(big, bt, ScaleMode.ScaleToFit, true);
            y += 230f;
            GUI.Label(new Rect(x, y, w, 40f), sd.displayName, UIStyles.Sized(UIStyles.H2, 28));
            y += 38f;
            UIStyles.Outlined(new Rect(x, y, w, 28f), RarityInfo.Name(sd.rarity) + "  ·  " + SlotName(sd.slot) + "  ·  Lv." + sel.level + "/" + sd.maxLevel, UIStyles.Sized(UIStyles.Body, 19), RarityInfo.Color(sd.rarity), 1.2f);
            y += 34f;
            GUI.Label(new Rect(x, y, w, 60f), "<color=#8CFF9E>" + StatText(sd.BonusAt(sel.level)) + "</color>", UIStyles.Sized(UIStyles.Small, 18));
            y += 62f;
            GUI.Label(new Rect(x, y, w, 50f), "<i><color=#BBBBBB>" + sd.description + "</color></i>", UIStyles.Sized(UIStyles.Small, 17));
            y += 56f;
            int cost = EquipmentSystem.UpgradeCost(sel);
            bool canUp = sel.level < sd.maxLevel && d.coins >= cost;
            if (FlatBtn(new Rect(x, y, w, 60f), sel.level >= sd.maxLevel ? "MAX LEVEL" : "UPGRADE  <size=18>" + cost.ToString("N0") + " gold</size>", TileGreen, canUp, 24))
            {
                if (EquipmentSystem.TryUpgrade(d, sel)) { gm.Save(); gm.Audio.Play("perfect", 0.6f); Toast(sd.displayName + " → Lv." + sel.level); }
            }
            y += 72f;
            GUI.Label(new Rect(x, y, w, 28f), "<color=#BBBBBB>EQUIP ON</color>", UIStyles.Sized(UIStyles.Body, 18));
            y += 30f;
            float fw = (w - 20f) / 3f;
            for (int i = 0; i < 3 && i < d.team.Count; i++)
            {
                var oc = d.GetCharacter(d.team[i]);
                var od = GameDatabase.GetCharacter(d.team[i]);
                if (oc == null || od == null) continue;
                var fr = new Rect(x + i * (fw + 10f), y, fw, fw + 26f);
                bool wearing = oc.GetEquipped(sd.slot) == sel.uid;
                Round(fr, wearing ? new Color(0.2f, 0.45f, 0.25f) : new Color(1f, 1f, 1f, 0.06f), 12f);
                var ft = ArtLibrary.Character(od);
                if (ft != null) GUI.DrawTexture(new Rect(fr.x + 6f, fr.y + 6f, fw - 12f, fw - 12f), ft, ScaleMode.ScaleAndCrop, true);
                GUI.Label(new Rect(fr.x, fr.yMax - 26f, fr.width, 24f), wearing ? "<color=#7CFF8A>WEARING</color>" : od.displayName, UIStyles.Sized(UIStyles.CenterSmall, 14));
                if (GUI.Button(fr, GUIContent.none, GUIStyle.none))
                {
                    if (wearing) EquipmentSystem.Unequip(oc, sd.slot);
                    else EquipmentSystem.Equip(d, oc, sel);
                    gm.Save();
                    gm.Audio.Play("switch", 0.5f);
                    Toast(wearing ? "Removed from " + od.displayName : od.displayName + " equipped " + sd.displayName);
                }
            }
        }

        void DrawGearShop(PlayerData d)
        {
            float top = safe.y + 200f;
            // Three featured Mythic pieces, in the same layout as the Mythic banners.
            var mythics = GameDatabase.Equipment.FindAll(e => e.rarity >= 6 && (e.id == "sword_moonfall" || e.id == "haori_starweave" || e.id == "acc_phoenixheart"));
            float mw = (safe.width - 60f - 2f * 20f) / 3f, mh = 330f;
            for (int i = 0; i < mythics.Count && i < 3; i++)
            {
                var e = mythics[i];
                Color rc = RarityInfo.Color(6);
                float k = Enter(0.05f + i * 0.08f, 0.35f);
                var r = new Rect(safe.x + 30f + i * (mw + 20f), top + (1f - k) * 40f, mw, mh);
                for (int j = 0; j < 8; j++)
                {
                    float f = j / 8f;
                    Round(new Rect(r.x, r.y + r.height * f, r.width, r.height / 8f + 18f), Color.Lerp(new Color(0.06f, 0.04f, 0.12f), Color.Lerp(rc, Color.black, 0.5f), f), j == 0 ? 18f : 0f);
                }
                Round(new Rect(r.x, r.yMax - 40f, r.width, 40f), Color.Lerp(rc, Color.black, 0.5f), 18f);
                float g = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.5f + i);
                RoundFrame(r, Color.Lerp(rc, Color.white, 0.3f * g), 3f, 18f);
                var tag = new Rect(r.x + 16f, r.y + 14f, 170f, 34f);
                Round(tag, rc, 17f);
                GUI.Label(tag, "<b>MYTHIC GEAR</b>", UIStyles.Sized(UIStyles.Center, 16));
                var img = new Rect(r.xMax - 190f, r.y + 20f, 170f, 170f);
                UIStyles.CircleTex(img.center, 95f + 6f * g, new Color(rc.r, rc.g, rc.b, 0.15f));
                var tex = GearArt.Get(e);
                if (tex != null) GUI.DrawTexture(new Rect(img.x, img.y + Mathf.Sin(Time.unscaledTime * 2f + i) * 5f, img.width, img.height), tex, ScaleMode.ScaleToFit, true);
                GUI.Label(new Rect(r.x + 16f, r.y + 56f, r.width - 220f, 70f), "<b>" + e.displayName + "</b>", UIStyles.Sized(UIStyles.H2, 26));
                GUI.Label(new Rect(r.x + 16f, r.y + 124f, r.width - 220f, 26f), "<color=#CCCCCC>" + SlotName(e.slot) + "</color>", UIStyles.Sized(UIStyles.Small, 16));
                GUI.Label(new Rect(r.x + 16f, r.y + 196f, r.width - 32f, 50f), "<color=#8CFF9E>" + StatText(e.baseBonus) + "</color>", UIStyles.Sized(UIStyles.Small, 15));
                int price = GearDiamondPrice(e);
                var br = new Rect(r.x + 16f, r.yMax - 70f, r.width - 32f, 56f);
                if (FlatBtn(br, "", Color.Lerp(rc, Color.black, 0.2f), d.crystals >= price, 22)) BuyGear(d, e);
                DiamondIcon(new Vector2(br.center.x - 50f, br.center.y), 30f);
                GUI.Label(new Rect(br.center.x - 30f, br.y, 200f, br.height), "<b>BUY  " + price.ToString("N0") + "</b>", UIStyles.Sized(UIStyles.Body, 22));
            }
            top += mh + 24f;

            // Everything else, rarest first.
            var list = GameDatabase.Equipment.FindAll(e => mythics.IndexOf(e) < 0);
            list.Sort((a, b) => b.rarity != a.rarity ? b.rarity.CompareTo(a.rarity) : a.slot.CompareTo(b.slot));
            var area = new Rect(safe.x + 30f, top, safe.width - 60f, H - top - 30f);
            Round(area, new Color(0.05f, 0.06f, 0.1f, 0.85f), 16f);
            float cw = 190f, ch = 300f, gap = 16f;
            var view = new Rect(area.x + 14f, area.y + 14f, area.width - 28f, area.height - 28f);
            int cols = Mathf.Max(1, Mathf.FloorToInt((view.width - 20f + gap) / (cw + gap)));
            var content = new Rect(0f, 0f, view.width - 20f, Mathf.CeilToInt(list.Count / (float)cols) * (ch + gap));
            gearShopScroll = GUI.BeginScrollView(view, gearShopScroll, content);
            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                var r = new Rect((i % cols) * (cw + gap), (i / cols) * (ch + gap), cw, ch - 56f);
                int owned = d.equipment.FindAll(x => x.defId == e.id).Count;
                GearCard(r, e, null, false, owned > 0 ? "<color=#AAAAAA>owned ×" + owned + "</color>" : "");
                int gold = GearGoldPrice(e), dia = GearDiamondPrice(e);
                bool can = gold > 0 ? d.coins >= gold : d.crystals >= dia;
                var br = new Rect(r.x, r.yMax + 6f, cw, 48f);
                if (FlatBtn(br, "", TileGreen, can, 20)) BuyGear(d, e);
                if (gold > 0) CoinIcon(new Vector2(br.x + 34f, br.center.y), 26f); else DiamondIcon(new Vector2(br.x + 34f, br.center.y), 26f);
                GUI.Label(new Rect(br.x + 54f, br.y, br.width - 60f, br.height), "<b>" + (gold > 0 ? gold : dia).ToString("N0") + "</b>", UIStyles.Sized(UIStyles.Body, 20));
            }
            GUI.EndScrollView();
        }

        void BuyGear(PlayerData d, EquipmentDefinition e)
        {
            int gold = GearGoldPrice(e), dia = GearDiamondPrice(e);
            if (gold > 0) { if (d.coins < gold) return; d.coins -= gold; }
            else { if (d.crystals < dia) return; d.crystals -= dia; }
            var it = InventorySystem.AddEquipment(d, e.id);
            gm.Save();
            gm.Audio.Play(e.rarity >= 6 ? "ultimate" : "coin", 0.8f);
            Toast("Bought " + e.displayName + "! Find it in MY GEAR.");
            if (it != null) invPick = it.uid;
        }
    }
}
