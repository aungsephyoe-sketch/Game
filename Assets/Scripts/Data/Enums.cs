namespace HashiraChronicles
{
    /// <summary>Breathing-style affinity. Water > Flame > Beast > Thunder > Water; Light and Dark counter each other.</summary>
    /// <summary>Beast is shown as WIND in the menus. Earth was added last so saved games keep their values.</summary>
    public enum Element { Water, Flame, Beast, Thunder, Light, Dark, Earth }

    public enum Role { DPS, Burst, Support, Tank }

    /// <summary>Most slayers are human; a few are monster-folk with their own bodies, faces and way of fighting.</summary>
    public enum Species
    {
        Human, Goblin, Skeleton, Demon, Cyclops, Werewolf, Mummy,
        Vampire, Zombie, Ghost, Lizardfolk, Minotaur, Orc, Troll, Harpy, Kitsune, Gargoyle, Kappa, Tengu, Yeti, Reaper, Golem, PumpkinKnight
    }

    /// <summary>Common(3) Rare(4) Epic(5) Legendary(6) Mythic(7) – stored as the character's star rarity.</summary>
    public enum Rarity { Common = 3, Rare = 4, Epic = 5, Legendary = 6, Mythic = 7 }

    public enum MissionType { Story, Side, Boss, Treasure, Event, Training, Encounter }

    /// <summary>Visual identity of a location: drives arena dressing, lighting, fog and particles.</summary>
    public enum EnvironmentKind { Village, Forest, Mountain, Kingdom, Temple, DemonLand, Castle, FallenCity }

    /// <summary>How an ability is executed by the AbilitySystem.</summary>
    public enum AbilityShape { Dash, Spin, Wave, Burst, MultiSlash, Heal }

    /// <summary>Hair / headwear silhouette of a chibi character.</summary>
    public enum HairStyle { Messy, Spiky, Long, Ponytail, Braid, Short, Bun, Hood, Cap, StrawHat, Wild, Crest, Bob, Twintails, Curly }

    public enum WeaponKind { Katana, TwinBlades, Greatsword, SwordShield, Spear, Staff, Bow, Fans, Cleavers, Cane, Moon, Fists }

    /// <summary>How a character fights: each style has its own combo timing, reach and signature mechanic.</summary>
    public enum CombatStyle { Balanced, Swift, Heavy, Ranged, Technical, Brawler, Healer }

    /// <summary>Movement personality used by idle, walk and run animation.</summary>
    public enum MotionStyle { Steady, Nervous, Aggressive, Graceful, Stoic, Confident, Sly, Light }

    public enum EnemyArchetype { Normal, Fast, Tank, Ranged, Elite, Boss }

    public enum EquipSlot { Sword, Haori, Accessory }

    public enum CombatTeam { Player, Enemy }

    public enum GameScreen
    {
        MainMenu,
        Story,
        MissionDetail,
        Team,
        Characters,
        CharacterDetail,
        Equipment,
        Battle,
        Results,
        ComingSoon,
        Settings,
        WorldMap,
        Summon,
        MissionsBoard,
        Shop,
        Cutscene,
        Credits,
        Events,
        Inventory,
        Designs
    }
}
