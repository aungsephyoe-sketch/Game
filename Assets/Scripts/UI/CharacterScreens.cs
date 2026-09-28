using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>Character collection, character detail (stats / skills / tree / gear) and equipment screens.</summary>
    public partial class UIManager
    {
        enum DetailTab { Stats, Skills, Tree, Gear }

        DetailTab detailTab = DetailTab.Stats;
        EquipSlot? pickingSlot;
        Vector2 pickerScroll, equipScroll;

        // ------------------------------------------------------------------ Collection

        void DrawCharacters()
        {
            TopBar("CHARACTERS", GameScreen.MainMenu);
            var d = gm.Data;
            if (string.IsNullOrEmpty(rosterPick) || d.GetCharacter(rosterPick) == null) rosterPick = d.team.Count > 0 ? d.team[0] : (d.characters.Count > 0 ? d.characters[0].id : null);
            DrawTeamEdit(d, true);
        }

        // ------------------------------------------------------------------ Detail

        void DrawCharacterDetail()
        {
            var d = gm.Data;
            var c = d.GetCharacter(gm.SelectedCharacterId);
            if (c == null) { gm.GoTo(GameScreen.Characters); return; }
            var def = GameDatabase.GetCharacter(c.id);
            TopBar(def.displayName.ToUpper(), GameScreen.Characters);
            Color rc = RarityInfo.Color(c.stars);

            // Left: the live 3D model (drag to rotate), rarity, lore and skill previews.
            var viewArea = new Rect(0f, safe.y + 120f, W * 0.44f, H - safe.y - 120f);
            var ev = Event.current;
            if (ev.type == EventType.MouseDrag && viewArea.Contains(ev.mousePosition)) { gm.Home.RotateViewer(-ev.delta.x * 0.6f); ev.Use(); }
            float lx = safe.x + 30f;
            UIStyles.Outlined(new Rect(lx, safe.y + 130f, 700f, 60f), RarityInfo.Name(c.stars), UIStyles.Sized(UIStyles.H1, 44), rc, 2f);
            UIStyles.Colored(new Rect(lx, safe.y + 186f, 700f, 44f), Stars(c.stars), UIStyles.Sized(UIStyles.H2, 34), UIStyles.Gold);
            GUI.Label(new Rect(lx, safe.y + 232f, 700f, 40f), def.versionTitle + "   " + ElementTag(def.element) + "  " + def.role, UIStyles.Small);
            GUI.Label(new Rect(lx, H - 330f, W * 0.4f, 44f), "<color=#AAAAAA>◀ drag to rotate ▶</color>", UIStyles.Sized(UIStyles.Small, 20));

            // Animation sheet: every move this slayer has, front and back.
            GUI.Label(new Rect(lx, safe.y + 282f, 300f, 34f), "ANIMATIONS", UIStyles.Sized(UIStyles.H2, 22));
            string[] anims = { "idle", "walk", "run", "attack", "heavy", "special", "hit", "victory", "defeat" };
            for (int i = 0; i < anims.Length; i++)
            {
                var ar = new Rect(lx + (i % 2) * 138f, safe.y + 320f + (i / 2) * 56f, 128f, 48f);
                if (FlatBtn(ar, anims[i].ToUpper(), i == 5 ? TileRed : new Color(0.22f, 0.22f, 0.32f), true, 18)) gm.Home.PreviewAnim(anims[i]);
            }
            float fy = safe.y + 320f + 5f * 56f;
            if (FlatBtn(new Rect(lx, fy, 128f, 48f), "FRONT", TileBlue, true, 18)) gm.Home.FaceViewer(false);
            if (FlatBtn(new Rect(lx + 138f, fy, 128f, 48f), "BACK", TileBlue, true, 18)) gm.Home.FaceViewer(true);
            string lore = string.IsNullOrEmpty(def.story) ? def.description : def.story;
            GUI.Label(new Rect(lx, H - 290f, W * 0.4f, 90f), "<i>" + lore + "</i>", UIStyles.Sized(UIStyles.Small, 20));
            GUI.Label(new Rect(lx, H - 196f, 400f, 36f), "TEST SKILLS", UIStyles.Sized(UIStyles.H2, 26));
            string[] tests = { "1", "2", "3", "ULT" };
            for (int i = 0; i < 4; i++)
                if (Btn(new Rect(lx + i * 118f, H - 156f, 108f, 76f), tests[i], i == 3 ? UIStyles.ButtonBig : UIStyles.ButtonSmall)) gm.Home.PreviewSkill(i);
            if (Btn(new Rect(lx + 490f, H - 156f, 300f, 76f), "TRAINING ▶", UIStyles.ButtonSmall))
            {
                // Practise with this slayer in the lead.
                d.team.Remove(c.id);
                d.team.Insert(0, c.id);
                if (d.team.Count > TeamSize) d.team.RemoveAt(d.team.Count - 1);
                gm.Save();
                gm.BeginMission(GameDatabase.GetMission("TR"));
                return;
            }

            // Right: power, tabs and the upgrade panels.
            float rx = W * 0.45f, rw = safe.xMax - rx - 30f;
            var header = new Rect(rx, safe.y + 130f, rw, 110f);
            UIStyles.PanelBox(header, ElementChart.ColorOf(def.element));
            GUI.Label(new Rect(header.x + 24f, header.y + 12f, rw - 380f, 50f), def.FullName, UIStyles.H2);
            GUI.Label(new Rect(header.x + 24f, header.y + 60f, rw - 380f, 44f), def.breathingStyle + "   <color=#AAAAAA>Lv." + c.level + "</color>", UIStyles.Small);
            GUI.Label(new Rect(header.xMax - 360f, header.y + 12f, 340f, 90f), "POWER\n<size=44>" + CharacterSystem.Power(d, c).ToString("N0") + "</size>", UIStyles.Sized(UIStyles.Right, 26));

            float tabY = header.yMax + 12f;
            string[] tabs = { "STATS", "SKILLS", "TREE", "GEAR" };
            float tw = (rw - 36f) / 4f;
            for (int t = 0; t < tabs.Length; t++)
            {
                if (Btn(new Rect(rx + t * (tw + 12f), tabY, tw, 70f), tabs[t], (int)detailTab == t ? UIStyles.ButtonBig : UIStyles.Button))
                {
                    detailTab = (DetailTab)t;
                    pickingSlot = null;
                }
            }

            var body = new Rect(rx, tabY + 84f, rw, H - tabY - 104f);
            UIStyles.PanelBox(body);
            switch (detailTab)
            {
                case DetailTab.Stats: DrawStatsTab(body, c, def); break;
                case DetailTab.Skills: DrawSkillsTab(body, c, def); break;
                case DetailTab.Tree: DrawTreeTab(body, c); break;
                case DetailTab.Gear: DrawGearTab(body, c); break;
            }
        }

        void DrawStatsTab(Rect body, OwnedCharacter c, CharacterDefinition def)
        {
            var d = gm.Data;
            var s = CharacterSystem.ComputeStats(d, c);
            float x = body.x + 40f, y = body.y + 30f;
            int cap = ExperienceSystem.Cap(c);
            GUI.Label(new Rect(x, y, 440f, 50f), "Level " + c.level + " / " + cap, UIStyles.H2);
            y += 56f;
            float need = ExperienceSystem.ExpToNext(c.level);
            UIStyles.Bar(new Rect(x, y, 300f, 26f), c.level >= cap ? 1f : c.exp / need, new Color(0.5f, 0.8f, 1f));
            GUI.Label(new Rect(x + 315f, y - 6f, 200f, 40f), c.level >= cap ? "MAX" : c.exp + " / " + need, UIStyles.Small);
            y += 60f;

            string[] names = { "HP", "ATK", "DEF", "CRIT", "CRIT DMG", "SPEED", "SPECIAL DMG", "ELEMENT" };
            string[] values =
            {
                Mathf.RoundToInt(s.hp).ToString("N0"), Mathf.RoundToInt(s.atk).ToString("N0"), Mathf.RoundToInt(s.def).ToString("N0"),
                Mathf.RoundToInt(s.crit * 100f) + "%", Mathf.RoundToInt(s.critDmg * 100f) + "%", s.speed.ToString("0.0"),
                Mathf.RoundToInt(s.specialDmg * 100f) + "%", ElementTag(def.element)
            };
            for (int i = 0; i < names.Length; i++)
            {
                GUI.Label(new Rect(x, y, 220f, 44f), names[i], UIStyles.Small);
                GUI.Label(new Rect(x + 220f, y - 4f, 240f, 44f), values[i], UIStyles.H2);
                y += 50f;
            }

            float rx = body.x + body.width * 0.5f, ry = body.y + 30f, rw = body.width * 0.47f;
            GUI.Label(new Rect(rx, ry, rw, 50f), "LEVEL UP", UIStyles.H2);
            ry += 56f;
            GUI.Label(new Rect(rx, ry, rw, 80f), "Feed XP from your pool.  You have " + d.xp.ToString("N0") + " XP", UIStyles.Small);
            ry += 60f;
            bool canLevel = c.level < cap && d.xp > 0;
            if (Btn(new Rect(rx, ry, rw * 0.48f, 90f), "+1,000 XP", UIStyles.Button, canLevel)) UseScrolls(c, 1);
            if (Btn(new Rect(rx + rw * 0.52f, ry, rw * 0.48f, 90f), "+10,000 XP", UIStyles.Button, canLevel)) UseScrolls(c, 10);
            ry += 140f;

            GUI.Label(new Rect(rx, ry, rw, 50f), "ASCENSION  " + Stars(c.stars) + (c.stars < CharacterSystem.MaxStars ? " → " + Stars(c.stars + 1) : ""), UIStyles.H2);
            ry += 56f;
            string reason;
            bool canAscend = CharacterSystem.CanAscend(d, c, out reason);
            if (c.stars < CharacterSystem.MaxStars)
            {
                GUI.Label(new Rect(rx, ry, rw, 100f), "Next rarity: +12% HP/ATK/DEF and a higher level cap.  Cost: " + CharacterSystem.AscendXpCost(c.stars).ToString("N0") + " XP, " +
                    CharacterSystem.AscendCoinCost(c.stars).ToString("N0") + " gold." + (canAscend ? "" : "  <color=#FF7070>" + reason + "</color>"), UIStyles.Small);
                ry += 100f;
                if (Btn(new Rect(rx, ry, rw, 90f), "ASCEND", UIStyles.ButtonBig, canAscend))
                {
                    CharacterSystem.TryAscend(d, c);
                    gm.Save();
                    gm.Home.Celebrate(UIStyles.Gold);
                    QuestSystem.Report("upgrade", 1);
                    Toast(def.displayName + " ascended to " + Stars(c.stars) + "!");
                }
            }
            else GUI.Label(new Rect(rx, ry, 700f, 40f), "Fully ascended.", UIStyles.Small);
        }

        void UseScrolls(OwnedCharacter c, int count)
        {
            int before = c.level;
            int gained = CharacterSystem.UseExpScrolls(gm.Data, c, count);
            if (gained < 0) { Toast("Not enough coins or scrolls."); return; }
            gm.Save();
            if (c.level > before) { gm.Home.Celebrate(new Color(0.5f, 0.85f, 1f)); QuestSystem.Report("upgrade", 1); }
            gm.Audio.Play("perfect", 0.5f);
            Toast(gained > 0 ? "Level up! Lv." + before + " → Lv." + c.level : "EXP gained.");
        }

        void DrawSkillsTab(Rect body, OwnedCharacter c, CharacterDefinition def)
        {
            var d = gm.Data;
            float y = body.y + 24f;
            float rowH = (body.height - 48f) / 4f;
            for (int i = 0; i < 4; i++)
            {
                var ab = i < 3 ? def.skills[i] : def.ultimate;
                int lvl = c.skillLevels[i];
                var row = new Rect(body.x + 30f, y, body.width - 60f, rowH - 12f);
                UIStyles.Rect(row, UIStyles.PanelLight);
                string kind = i < 3 ? "STRONG " + (i + 1) : "<color=#FFD36B>SPECIAL</color>";
                if (i < 3 && i >= CharacterSystem.SkillSlots(c.stars))
                {
                    // Locked by rarity: Common has 1 strong attack, Rare 2, Epic and up 3.
                    int need = CharacterSystem.SkillSlotRarity(i);
                    Round(row, new Color(0f, 0f, 0f, 0.45f), 8f);
                    LockIcon(new Vector2(row.x + 50f, row.center.y), 44f, new Color(1f, 1f, 1f, 0.7f));
                    GUI.Label(new Rect(row.x + 90f, row.y + 8f, row.width - 120f, 44f), kind + "   " + ab.name, UIStyles.Sized(UIStyles.H2, 26));
                    GUI.Label(new Rect(row.x + 90f, row.y + 50f, row.width - 120f, 40f), "<color=#FF9C7A>Unlocks at " + RarityInfo.Name(need) + " — ascend this slayer to use it.</color>", UIStyles.Sized(UIStyles.Small, 20));
                    y += rowH;
                    continue;
                }
                GUI.Label(new Rect(row.x + 20f, row.y + 8f, row.width - 330f, 44f), kind + "   " + ab.name + "   <color=#AAAAAA>Lv." + lvl + "/" + CharacterSystem.MaxSkillLevel + "</color>", UIStyles.Sized(UIStyles.H2, 26));
                string detail = ab.description + "   <color=#AAAAAA>" + ab.hits + " hit" + (ab.hits > 1 ? "s" : "") + " × " +
                                Mathf.RoundToInt(ab.damageMultiplier * CharacterSystem.SkillLevelMultiplier(lvl) * 100f) + "% ATK" +
                                (i < 3 ? "   CD " + ab.cooldown + "s" : "") + "</color>";
                GUI.Label(new Rect(row.x + 20f, row.y + 50f, row.width - 330f, row.height - 54f), detail, UIStyles.Sized(UIStyles.Small, 19));
                if (lvl < CharacterSystem.MaxSkillLevel)
                {
                    int coins = CharacterSystem.SkillUpgradeCoinCost(lvl), xpCost = CharacterSystem.SkillUpgradeXpCost(lvl);
                    bool can = d.coins >= coins && d.xp >= xpCost;
                    if (Btn(new Rect(row.xMax - 300f, row.y + (row.height - 100f) * 0.5f, 285f, 100f),
                        "UPGRADE\n<size=20>" + coins.ToString("N0") + " gold · " + xpCost.ToString("N0") + " XP</size>", UIStyles.Button, can))
                    {
                        CharacterSystem.TryUpgradeSkill(d, c, i);
                        gm.Save();
                        gm.Home.PreviewSkill(i);
                        QuestSystem.Report("upgrade", 1);
                        Toast(ab.name + " → Lv." + c.skillLevels[i]);
                    }
                }
                else GUI.Label(new Rect(row.xMax - 300f, row.y + 20f, 280f, 60f), "MAX", UIStyles.Center);
                if (Btn(new Rect(row.xMax - 410f, row.y + (row.height - 70f) * 0.5f, 96f, 70f), "▶", UIStyles.ButtonSmall)) gm.Home.PreviewSkill(i);
                y += rowH;
            }
        }

        void DrawTreeTab(Rect body, OwnedCharacter c)
        {
            var d = gm.Data;
            float cx = body.x + body.width * 0.36f;
            float nodeW = 300f, nodeH = 100f;
            // Layout mirrors the design doc: a spine that branches and rejoins.
            Vector2[] pos =
            {
                new Vector2(cx, body.y + 40f), new Vector2(cx, body.y + 170f), new Vector2(cx, body.y + 300f),
                new Vector2(cx - 165f, body.y + 430f), new Vector2(cx + 165f, body.y + 430f), new Vector2(cx, body.y + 560f)
            };
            DrawLink(pos[0], pos[1], nodeW, nodeH); DrawLink(pos[1], pos[2], nodeW, nodeH);
            DrawLink(pos[2], pos[3], nodeW, nodeH); DrawLink(pos[2], pos[4], nodeW, nodeH);
            DrawLink(pos[3], pos[5], nodeW, nodeH); DrawLink(pos[4], pos[5], nodeW, nodeH);
            for (int i = 0; i < SkillTree.Nodes.Length; i++)
            {
                var n = SkillTree.Nodes[i];
                var r = new Rect(pos[i].x - nodeW * 0.5f, pos[i].y, nodeW, nodeH);
                bool unlocked = SkillTree.IsUnlocked(c, i);
                bool available = SkillTree.CanUnlock(c, i);
                string label = n.label + "\n<size=20>" + (unlocked ? "UNLOCKED" : n.scrollCost + " scrolls · " + n.coinCost.ToString("N0") + " coins") + "</size>";
                var style = unlocked ? UIStyles.ButtonBig : UIStyles.Button;
                if (Btn(r, label, style, available || unlocked) && available)
                {
                    if (SkillTree.TryUnlock(d, c, i)) { gm.Save(); QuestSystem.Report("upgrade", 1); gm.Home.Celebrate(new Color(0.6f, 1f, 0.7f)); Toast("Unlocked " + n.label); }
                    else Toast("Not enough skill scrolls or coins.");
                }
            }
            GUI.Label(new Rect(body.x + body.width * 0.7f, body.y + 40f, body.width * 0.28f, 400f),
                "Ability tree nodes are permanent bonuses for this slayer (each scroll of cost = 500 XP).\n\nXP: " + d.xp.ToString("N0") + "\nGold: " + d.coins.ToString("N0"), UIStyles.Body);
        }

        void DrawLink(Vector2 a, Vector2 b, float w, float h)
        {
            Vector2 from = new Vector2(a.x, a.y + h), to = new Vector2(b.x, b.y);
            float midY = (from.y + to.y) * 0.5f;
            UIStyles.Rect(new Rect(from.x - 2f, from.y, 4f, midY - from.y), UIStyles.Gold * 0.6f);
            UIStyles.Rect(new Rect(Mathf.Min(from.x, to.x) - 2f, midY - 2f, Mathf.Abs(to.x - from.x) + 4f, 4f), UIStyles.Gold * 0.6f);
            UIStyles.Rect(new Rect(to.x - 2f, midY, 4f, to.y - midY), UIStyles.Gold * 0.6f);
        }

        void DrawGearTab(Rect body, OwnedCharacter c)
        {
            var d = gm.Data;
            float x = body.x + 30f, y = body.y + 30f;
            foreach (EquipSlot slot in System.Enum.GetValues(typeof(EquipSlot)))
            {
                var item = d.GetItem(c.GetEquipped(slot));
                var eq = item != null ? GameDatabase.GetEquipment(item.defId) : null;
                var r = new Rect(x, y, body.width * 0.45f, 130f);
                string label = "<size=24>" + slot.ToString().ToUpper() + "</size>\n" + (eq != null ? eq.displayName + "  +" + item.level + "\n<size=22>" + BonusText(eq.BonusAt(item.level)) + "</size>" : "(empty — tap to equip)");
                if (Btn(r, label, pickingSlot == slot ? UIStyles.ButtonBig : UIStyles.Button)) { pickingSlot = slot; pickerScroll = Vector2.zero; }
                y += 146f;
            }

            if (pickingSlot == null)
            {
                GUI.Label(new Rect(body.x + body.width * 0.5f, body.y + 30f, body.width * 0.45f, 200f),
                    "Tap a slot to choose equipment.\nSword → Attack · Haori → Defense · Accessory → special effects.", UIStyles.Body);
                return;
            }

            var slotSel = pickingSlot.Value;
            var list = d.equipment.FindAll(e => { var def = GameDatabase.GetEquipment(e.defId); return def != null && def.slot == slotSel; });
            var view = new Rect(body.x + body.width * 0.5f, body.y + 30f, body.width * 0.48f, body.height - 60f);
            var content = new Rect(0f, 0f, view.width - 30f, (list.Count + 1) * 116f);
            pickerScroll = GUI.BeginScrollView(view, pickerScroll, content);
            if (Btn(new Rect(0f, 0f, content.width, 100f), "Unequip"))
            {
                EquipmentSystem.Unequip(c, slotSel);
                gm.Save();
            }
            for (int i = 0; i < list.Count; i++)
            {
                var item = list[i];
                var def = GameDatabase.GetEquipment(item.defId);
                var owner = d.WhoEquipped(item.uid);
                string ownerText = owner == null ? "" : "  <color=#FFD36B>[" + GameDatabase.GetCharacter(owner.id).displayName + "]</color>";
                string label = def.displayName + " +" + item.level + "  " + Stars(def.rarity) + ownerText + "\n<size=22>" + BonusText(def.BonusAt(item.level)) + "</size>";
                if (Btn(new Rect(0f, (i + 1) * 116f, content.width, 100f), label))
                {
                    EquipmentSystem.Equip(d, c, item);
                    gm.Save();
                    Toast("Equipped " + def.displayName);
                }
            }
            GUI.EndScrollView();
        }

        static string BonusText(StatBlock b)
        {
            var sb = new System.Text.StringBuilder();
            if (b.hp > 0) sb.Append("HP +" + Mathf.RoundToInt(b.hp) + "  ");
            if (b.atk > 0) sb.Append("ATK +" + Mathf.RoundToInt(b.atk) + "  ");
            if (b.def > 0) sb.Append("DEF +" + Mathf.RoundToInt(b.def) + "  ");
            if (b.crit > 0) sb.Append("CRIT +" + (b.crit * 100f).ToString("0.#") + "%  ");
            if (b.critDmg > 0) sb.Append("CRIT DMG +" + Mathf.RoundToInt(b.critDmg * 100f) + "%  ");
            if (b.speed > 0) sb.Append("SPD +" + b.speed.ToString("0.0") + "  ");
            if (b.specialDmg > 0) sb.Append("SPECIAL +" + Mathf.RoundToInt(b.specialDmg * 100f) + "%");
            return sb.ToString();
        }

        // ------------------------------------------------------------------ Equipment inventory

        // The home tile and Characters screen now lead to character upgrades instead of gear.
        void DrawEquipment() { DrawUpgrade(); }

        void DrawGearList()
        {
            TopBar("EQUIPMENT", GameScreen.MainMenu);
            var d = gm.Data;
            var view = new Rect(safe.x + 30f, safe.y + 140f, safe.width - 60f, H - safe.y - 170f);
            float rowH = 110f;
            var content = new Rect(0f, 0f, view.width - 30f, d.equipment.Count * (rowH + 12f));
            equipScroll = GUI.BeginScrollView(view, equipScroll, content);
            for (int i = 0; i < d.equipment.Count; i++)
            {
                var item = d.equipment[i];
                var def = GameDatabase.GetEquipment(item.defId);
                var r = new Rect(0f, i * (rowH + 12f), content.width, rowH);
                UIStyles.PanelBox(r);
                var owner = d.WhoEquipped(item.uid);
                GUI.Label(new Rect(r.x + 24f, r.y + 12f, r.width - 560f, 44f), def.displayName + "  +" + item.level + "/" + def.maxLevel +
                    "   <color=#FFD36B>" + Stars(def.rarity) + "</color>   <size=24><color=#AAAAAA>" + def.slot + (owner != null ? " · worn by " + GameDatabase.GetCharacter(owner.id).displayName : "") + "</color></size>", UIStyles.Body);
                GUI.Label(new Rect(r.x + 24f, r.y + 58f, r.width - 560f, 44f), BonusText(def.BonusAt(item.level)) + "   <color=#888888>" + def.description + "</color>", UIStyles.Small);
                if (item.level < def.maxLevel)
                {
                    int cost = EquipmentSystem.UpgradeCost(item);
                    if (Btn(new Rect(r.xMax - 500f, r.y + 15f, 480f, 80f), "UPGRADE  <size=22>(" + cost.ToString("N0") + " coins)</size>", UIStyles.Button, d.coins >= cost))
                    {
                        EquipmentSystem.TryUpgrade(d, item);
                        gm.Save();
                    }
                }
                else GUI.Label(new Rect(r.xMax - 300f, r.y + 25f, 280f, 60f), "MAX", UIStyles.Center);
            }
            GUI.EndScrollView();
        }
    }
}
