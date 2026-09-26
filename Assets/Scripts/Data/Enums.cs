namespace HashiraChronicles
{
    /// <summary>Breathing-style affinity. Water > Flame > Beast > Thunder > Water; Light and Dark counter each other.</summary>
    public enum Element { Water, Flame, Beast, Thunder, Light, Dark }

    public enum Role { DPS, Burst, Support, Tank }

    /// <summary>How an ability is executed by the AbilitySystem.</summary>
    public enum AbilityShape { Dash, Spin, Wave, Burst, MultiSlash }

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
        Settings
    }
}
