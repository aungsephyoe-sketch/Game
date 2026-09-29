namespace HashiraChronicles
{
    /// <summary>
    /// Names for the simulated players (village hub, co-op parties, arena opponents, friends): the kind people really
    /// pick — real first names with a gamer twist, and silly, cute, sassy and powerful handles. All kid-friendly.
    /// </summary>
    public static class GamerNames
    {
        public static readonly string[] RealWorld =
        {
            "JakeTheSnake", "EmilyPlays", "Marcus_07", "SofiaStrikes", "LiamGG", "ChloeCrits", "NoahTheBrave", "Ava_xo", "Diego_Dash",
            "PriyaPlays", "Tyler2Fast", "MiaMoonlight", "Oliver_TTV", "ZaraZoom", "EthanEpic", "Lily.Loot", "Carlos_Crit", "HannahHeals",
            "Kenji_Main", "SamTheSlayer", "Isabella_B", "MaxOut_Max", "Aiden99", "Leah_LOL", "Omar_Onyx", "RubyRush", "Jonah_J", "Nina_Ninja"
        };
        public static readonly string[] Silly =
        {
            "NoodleNinja", "SirSnacksALot", "PotatoSamurai", "WaffleWarrior", "SneakyPickle", "CaptainNapTime", "BananaBlade", "TacoTornado",
            "OopsAllCrits", "DuckWithASword", "SpicyMeatball", "LagLord3000", "Mr_Wobbles", "ToastedToad"
        };
        public static readonly string[] Cute =
        {
            "MochiMuffin", "BunnyBlade", "SakuraPuff", "KittyKatana", "BobaBlossom", "PeachyPaws", "HoneyBun", "StarlightPip",
            "Cupcake_Kun", "TinyTanuki", "SnuggleSlash", "MarshmallowMoon"
        };
        public static readonly string[] Sassy =
        {
            "QueenOfCrits", "NotYourHealer", "ImBetterThanU", "SassyShogun", "DramaLlama99", "TryHarderLOL", "Main_Character",
            "ObviouslyMVP", "GGEasyPeasy", "NoPityNeeded", "Carry_Me_Not", "SlayAllDay"
        };
        public static readonly string[] Powerful =
        {
            "StormbreakerX", "DragonfireKai", "ThunderKingRyu", "ShadowMonarch", "IronFistJin", "BladeOfDawn", "VoidReaper",
            "Titan_Takeda", "PhoenixRising", "CrimsonShogun", "Frostfang", "EclipseKnight"
        };

        public static readonly string[] All = Join(RealWorld, Silly, Cute, Sassy, Powerful);

        static string[] Join(params string[][] lists)
        {
            int n = 0;
            foreach (var l in lists) n += l.Length;
            var all = new string[n];
            int i = 0;
            foreach (var l in lists) foreach (var s in l) all[i++] = s;
            return all;
        }
    }
}
