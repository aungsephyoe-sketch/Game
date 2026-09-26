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
            new Item { id = "exp5", name = "EXP Scroll ×5", description = "Level up slayers faster.", coinCost = 2000, grant = new RewardBundle { expScrolls = 5 } },
            new Item { id = "skill3", name = "Skill Scroll ×3", description = "Upgrade breathing forms and the ability tree.", coinCost = 3500, grant = new RewardBundle { skillScrolls = 3 } },
            new Item { id = "ore2", name = "Ascension Ore ×2", description = "Raise a slayer's star rank.", coinCost = 6000, grant = new RewardBundle { ascensionOre = 2 } },
            new Item { id = "chest4", name = "Forge Chest", description = "A random RARE+ weapon, haori or accessory.", coinCost = 12000, chestRarity = 4 },
            new Item { id = "chest5", name = "Pillar's Chest", description = "A random EPIC+ weapon, haori or accessory.", crystalCost = 150, chestRarity = 5 },
            new Item { id = "coins", name = "Coin Pouch", description = "25,000 coins.", crystalCost = 60, grant = new RewardBundle { coins = 25000 } },
        };

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
