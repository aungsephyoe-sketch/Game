namespace HashiraChronicles
{
    /// <summary>Breathing-style affinity. Water > Flame > Beast > Thunder > Water; Light and Dark counter each other.</summary>
    public enum Element { Water, Flame, Beast, Thunder, Light, Dark }

    public enum Role { DPS, Burst, Support, Tank }

    /// <summary>Common(3) Rare(4) Epic(5) Legendary(6) Mythic(7) – stored as the character's star rarity.</summary>
    public enum Rarity { Common = 3, Rare = 4, Epic = 5, Legendary = 6, Mythic = 7 }

    public enum MissionType { Story, Side, Boss, Treasure, Event, Training, Encounter }

    /// <summary>Visual identity of a location: drives arena dressing, lighting, fog and particles.</summary>
    public enum EnvironmentKind { Village, Forest, Mountain, Kingdom, Temple, DemonLand, Castle, FallenCity }

    /// <summary>How an ability is executed by the AbilitySystem.</summary>
    public enum AbilityShape { Dash, Spin, Wave, Burst, MultiSlash, Heal }

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
        Credits
    }
}
