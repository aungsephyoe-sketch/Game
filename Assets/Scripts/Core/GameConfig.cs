namespace HashiraChronicles
{
    /// <summary>
    /// Branding in one place. The working title and all characters are original placeholders –
    /// using any licensed IP (names, art, story) requires a license before a commercial release.
    /// </summary>
    public static class GameConfig
    {
        public const string TitleLine1 = "BLADES OF DAWN";
        public const string TitleLine2 = "HASHIRA CHRONICLES";
        public const string Subtitle = "A story of dawn and eclipse · original characters";
        public const string Version = "0.17.0";
        /// <summary>Build every slayer on the premium jointed rig (false = the original chibi builder).</summary>
        public const bool PremiumRoster = true;
        /// <summary>The game's art direction is the simple chibi style built in code; imported models are off.</summary>
        public static readonly bool UseImportedModels = false;
    }
}
