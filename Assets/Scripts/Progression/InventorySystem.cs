namespace HashiraChronicles
{
    public static class InventorySystem
    {
        public static EquipmentItem AddEquipment(PlayerData data, string defId)
        {
            var item = new EquipmentItem { uid = "eq" + data.nextEquipmentUid++, defId = defId, level = 1 };
            data.equipment.Add(item);
            return item;
        }

        /// <summary>Adds a character, or keeps a duplicate as a copy (feed for XP or sell for gold).</summary>
        public static bool AddCharacter(PlayerData data, string id)
        {
            if (data.GetCharacter(id) != null)
            {
                if (data.copies == null) data.copies = new System.Collections.Generic.List<CopyStack>();
                var stack = data.copies.Find(x => x.id == id);
                if (stack == null) data.copies.Add(new CopyStack { id = id, count = 1 });
                else stack.count++;
                return false;
            }
            var def = GameDatabase.GetCharacter(id);
            if (def == null) return false;
            data.characters.Add(new OwnedCharacter { id = id, stars = def.rarity });
            return true;
        }

        public static void AddCurrencies(PlayerData data, RewardBundle r)
        {
            data.coins += r.coins;
            data.crystals += r.crystals;
            // Scrolls and ore from older reward tables all arrive as XP now.
            data.xp += r.expScrolls * ExperienceSystem.ExpPerScroll + r.skillScrolls * 500 + r.ascensionOre * 800;
        }
    }

    public static class EquipmentSystem
    {
        public static int UpgradeCost(EquipmentItem item) { return 800 * item.level; }

        public static bool TryUpgrade(PlayerData data, EquipmentItem item)
        {
            var def = GameDatabase.GetEquipment(item.defId);
            if (def == null || item.level >= def.maxLevel) return false;
            int cost = UpgradeCost(item);
            if (data.coins < cost) return false;
            data.coins -= cost;
            item.level++;
            return true;
        }

        /// <summary>Equips an item, removing it from whoever wore it before.</summary>
        public static void Equip(PlayerData data, OwnedCharacter c, EquipmentItem item)
        {
            var def = GameDatabase.GetEquipment(item.defId);
            var previousOwner = data.WhoEquipped(item.uid);
            if (previousOwner != null) previousOwner.SetEquipped(def.slot, "");
            c.SetEquipped(def.slot, item.uid);
        }

        public static void Unequip(OwnedCharacter c, EquipSlot slot)
        {
            c.SetEquipped(slot, "");
        }
    }
}
