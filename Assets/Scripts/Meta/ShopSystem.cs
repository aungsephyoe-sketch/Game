using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>The Solmere market: materials for coins, equipment chests and summon crystals.</summary>
    public static class ShopSystem
    {
        public class Item
        {
            public string id, name, description;
            public int coinCost, crystalCost;
            public RewardBundle grant;
            /// <summary>Random equipment of at least this rarity (0 = none).</summary>
            public int chestRarity;
            public bool dailyFree;
        }

        public static readonly List<Item> Items = new List<Item>
        {
            new Item { id = "free_crystals", name = "Daily Gift", description = "20 crystals, free once a day.", grant = new RewardBundle { crystals = 20 }, dailyFree = true },
            new Item { id = "exp5", name = "XP Pack", description = "+5,000 XP for levelling slayers.", coinCost = 2000, grant = new RewardBundle { expScrolls = 5 } },
            new Item { id = "skill3", name = "Big XP Pack", description = "+15,000 XP for levels, skills and ascension.", coinCost = 5500, grant = new RewardBundle { expScrolls = 15 } },
            new Item { id = "chest4", name = "Forge Chest", description = "A random RARE+ weapon, haori or accessory.", coinCost = 12000, chestRarity = 3 },
            new Item { id = "chest5", name = "Pillar's Chest", description = "A random EPIC+ weapon, haori or accessory.", crystalCost = 150, chestRarity = 4 },
            new Item { id = "coins", name = "Gold Pouch", description = "25,000 gold.", crystalCost = 60, grant = new RewardBundle { coins = 25000 } },
        };

        /// <summary>Gold price of an accessory by rarity (Common .. Mythic).</summary>
        public static int AccessoryPrice(EquipmentDefinition e)
        {
            switch (Mathf.Clamp(e.rarity, 2, 6)) { case 2: return 3000; case 3: return 8000; case 4: return 20000; case 5: return 45000; default: return 90000; }
        }

        public static List<EquipmentDefinition> Accessories()
        {
            var list = GameDatabase.Equipment.FindAll(e => e.slot == EquipSlot.Accessory);
            list.Sort((a, b) => a.rarity.CompareTo(b.rarity));
            return list;
        }

        /// <summary>Buys an accessory for gold; equip it from the slayer's Gear tab.</summary>
        public static bool BuyAccessory(PlayerData d, EquipmentDefinition e)
        {
            int price = AccessoryPrice(e);
            if (d.coins < price) return false;
            d.coins -= price;
            InventorySystem.AddEquipment(d, e.id);
            return true;
        }

        public static bool FreeClaimedToday(PlayerData d)
        {
            return PlayerPrefs.GetString("shop_free_day", "") == System.DateTime.UtcNow.ToString("yyyyMMdd");
        }

        public static bool CanBuy(PlayerData d, Item item)
        {
            if (item.dailyFree) return !FreeClaimedToday(d);
            return d.coins >= item.coinCost && d.crystals >= item.crystalCost;
        }

        /// <summary>Returns a short description of what was received, or null on failure.</summary>
        public static string Buy(PlayerData d, Item item)
        {
            if (!CanBuy(d, item)) return null;
            d.coins -= item.coinCost;
            d.crystals -= item.crystalCost;
            if (item.dailyFree) PlayerPrefs.SetString("shop_free_day", System.DateTime.UtcNow.ToString("yyyyMMdd"));
            if (item.grant != null) InventorySystem.AddCurrencies(d, item.grant);
            if (item.chestRarity > 0)
            {
                var pool = GameDatabase.Equipment.FindAll(e => e.rarity >= item.chestRarity);
                var pick = pool[Random.Range(0, pool.Count)];
                InventorySystem.AddEquipment(d, pick.id);
                return pick.displayName + " " + CharacterSystem.Stars(pick.rarity);
            }
            return item.name;
        }
    }
}
