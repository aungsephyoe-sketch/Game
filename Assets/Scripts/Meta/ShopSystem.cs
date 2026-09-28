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
            new Item { id = "free_crystals", name = "Daily Gift", description = "20 diamonds, free once a day.", grant = new RewardBundle { crystals = 20 }, dailyFree = true },
            new Item { id = "exp5", name = "XP Pack", description = "+5,000 XP for levelling slayers.", coinCost = 2000, grant = new RewardBundle { expScrolls = 5 } },
            new Item { id = "skill3", name = "Big XP Pack", description = "+15,000 XP for levels, skills and ascension.", coinCost = 5500, grant = new RewardBundle { expScrolls = 15 } },
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

        /// <summary>A diamond pack on the store (price in USD, shown on the button).</summary>
        public class DiamondPack
        {
            public int diamonds;
            public string price;
            public string tag;
            public string badge;
        }

        public static readonly List<DiamondPack> DiamondPacks = new List<DiamondPack>
        {
            new DiamondPack { diamonds = 250, price = "$2.99", tag = "Starter Pack" },
            new DiamondPack { diamonds = 500, price = "$4.99", tag = "Popular" },
            new DiamondPack { diamonds = 1000, price = "$8.99", tag = "Best Value", badge = "★ BEST VALUE" },
            new DiamondPack { diamonds = 1500, price = "$12.99", tag = "" },
            new DiamondPack { diamonds = 2000, price = "$16.99", tag = "Best Seller", badge = "BEST SELLER" },
            new DiamondPack { diamonds = 3000, price = "$24.99", tag = "" },
            new DiamondPack { diamonds = 5000, price = "$39.99", tag = "" },
            new DiamondPack { diamonds = 10000, price = "$74.99", tag = "Mega Pack", badge = "◆ MEGA PACK" },
        };

        /// <summary>
        /// Real-money purchases need the platform store (App Store / Google Play billing), which isn't connected in
        /// this build — the button grants the diamonds directly so the flow can be tested.
        /// </summary>
        public static void BuyDiamondPack(PlayerData d, DiamondPack p) { d.crystals += p.diamonds; }

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
