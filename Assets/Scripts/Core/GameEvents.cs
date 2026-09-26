namespace HashiraChronicles
{
    /// <summary>Global event hub so UI/audio/missions can react without hard references to gameplay objects.</summary>
    public static class GameEvents
    {
        public static event System.Action<DamageInfo, Combatant> DamageDealt;
        public static event System.Action<EnemyController> EnemyKilled;
        public static event System.Action<PlayerCharacter> PlayerMemberDown;
        public static event System.Action<PlayerCharacter, AbilityDefinition> SkillUsed;
        public static event System.Action<PlayerCharacter, AbilityDefinition> UltimateStarted;
        public static event System.Action<PlayerCharacter, float> UltimateFinished;
        public static event System.Action<string, string> Banner;
        public static event System.Action PerfectDodge;
        public static event System.Action<OwnedCharacter> CharacterUpgraded;
        /// <summary>Heavy impact (0..1 strength) – drives impact frames and speed lines in the HUD.</summary>
        public static event System.Action<float> Impact;
        /// <summary>Boss entrance: drives the cinematic name card.</summary>
        public static event System.Action<EnemyDefinition> BossIntro;
        /// <summary>In-battle dialogue line (speaker, text).</summary>
        public static event System.Action<string, string> Subtitle;
        /// <summary>The team reached a named place along the mission road (name, region).</summary>
        public static event System.Action<string, string> AreaEntered;

        public static void RaiseDamageDealt(DamageInfo info, Combatant target) { var h = DamageDealt; if (h != null) h(info, target); }
        public static void RaiseEnemyKilled(EnemyController e) { var h = EnemyKilled; if (h != null) h(e); }
        public static void RaisePlayerMemberDown(PlayerCharacter p) { var h = PlayerMemberDown; if (h != null) h(p); }
        public static void RaiseSkillUsed(PlayerCharacter p, AbilityDefinition a) { var h = SkillUsed; if (h != null) h(p, a); }
        public static void RaiseUltimateStarted(PlayerCharacter p, AbilityDefinition a) { var h = UltimateStarted; if (h != null) h(p, a); }
        public static void RaiseUltimateFinished(PlayerCharacter p, float total) { var h = UltimateFinished; if (h != null) h(p, total); }
        public static void RaiseBanner(string title, string subtitle) { var h = Banner; if (h != null) h(title, subtitle); }
        public static void RaisePerfectDodge() { var h = PerfectDodge; if (h != null) h(); }
        public static void RaiseImpact(float strength) { var h = Impact; if (h != null) h(strength); }
        public static void RaiseBossIntro(EnemyDefinition d) { var h = BossIntro; if (h != null) h(d); }
        public static void RaiseAreaEntered(string name, string sub) { var h = AreaEntered; if (h != null) h(name, sub); }
        public static void RaiseSubtitle(string speaker, string text) { var h = Subtitle; if (h != null) h(speaker, text); }
        public static void RaiseCharacterUpgraded(OwnedCharacter c) { var h = CharacterUpgraded; if (h != null) h(c); }
    }
}
