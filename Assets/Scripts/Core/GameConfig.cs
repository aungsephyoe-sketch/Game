namespace HashiraChronicles
{
    /// <summary>
    /// Branding in one place. The working title and all characters are original placeholders –
    /// using any licensed IP (names, art, story) requires a license before a commercial release.
    /// </summary>
    public static class GameConfig
    {
        public const string TitleLine1 = "BLADE LEGENDS";
        public const string TitleLine2 = "BLADE LEGENDS";
        public const string GameName = "Blade Legends";
        /// <summary>Stylised cartoon look: chunky proportions, hard toon shading, bold outlines, saturated colour.</summary>
        public const bool CartoonStyle = true;
        public const string Subtitle = "A story of dawn and eclipse · original characters";
        /// <summary>Every mission uses the new-standard worlds (Forest, Snow Mountain, Volcano).</summary>
        public const bool NewWorlds = true;
        public const string Version = "0.34.0";
        /// <summary>Build every slayer on the premium jointed rig (false = the original chibi builder).</summary>
        public const bool PremiumRoster = true;
        /// <summary>The game's art direction is the simple chibi style built in code; imported models are off.</summary>
        public static readonly bool UseImportedModels = false;
    }
}
