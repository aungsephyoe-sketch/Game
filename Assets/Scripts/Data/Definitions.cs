using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    // Static (design-time) data. Everything here is plain C# so it can later be moved into
    // ScriptableObjects / remote config and so placeholder characters can be swapped for licensed ones
    // without touching gameplay code.

    [System.Serializable]
    public class AbilityDefinition
    {
        public string name;
        public string description;
        public AbilityShape shape;
        public float cooldown = 6f;
        /// <summary>Damage multiplier per hit (scaled by ATK and specialDmg).</summary>
        public float damageMultiplier = 1.5f;
        public int hits = 1;
        /// <summary>Forward reach for Wave/Dash, attack radius for Spin/Burst/MultiSlash.</summary>
        public float range = 3f;
        public float radius = 3f;
        public float stagger = 2f;
        public float knockback = 3f;
    }

    public class CharacterDefinition
    {
        public string id;
        /// <summary>Characters sharing a baseId are different versions of the same person.</summary>
        public string baseId;
        public string displayName;
        public string versionTitle;
        public string breathingStyle;
        public string description;
        public int rarity = 4;
        public Element element;
        public Role role;
        public StatBlock baseStats;
        public float attackSpeed = 1f;
        public float[] comboMultipliers = { 0.62f, 0.66f, 0.74f, 0.82f, 1.45f };
        public float chargedMultiplier = 2.6f;
        public AbilityDefinition[] skills = new AbilityDefinition[3];
        public AbilityDefinition ultimate;

        public Color bodyColor = Color.black;
        public Color haoriColor = Color.green;
        public Color hairColor = Color.black;
        public Color bladeColor = Color.cyan;

        /// <summary>Lore shown on the collection screen.</summary>
        public string story = "";
        /// <summary>Only obtainable through the story (never from summons).</summary>
        public bool storyOnly;
        /// <summary>Roster design test (the six Design Bible reference characters): playable, never in summons.</summary>
        public bool designTest;
        /// <summary>Not a playable character (villagers, mentors, soldiers in cutscenes).</summary>
        public bool npc;
        public float scale = 1f;

        // ---- Chibi look & fighting identity
        public HairStyle hair = HairStyle.Messy;
        public WeaponKind weapon = WeaponKind.Katana;
        public CombatStyle style = CombatStyle.Balanced;
        public MotionStyle motion = MotionStyle.Steady;
        /// <summary>Body proportions (1 = standard chibi).</summary>
        public float bodyHeight = 1f, bodyWidth = 1f;
        public bool scarf, cape, pelt, armor, bell;
        public Color accentColor = new Color(0.9f, 0.9f, 0.9f);
        /// <summary>Face and hands.</summary>
        public Color skinTone = new Color(0.97f, 0.82f, 0.7f);
        /// <summary>Human, or one of the monster-folk (their body, face and weapon change to suit).</summary>
        public Species species = Species.Human;

        public string FullName { get { return displayName + " — " + versionTitle; } }
        public Rarity RarityTier { get { return (Rarity)Mathf.Clamp(rarity, 3, 7); } }
    }

    public static class RarityInfo
    {
        /// <summary>2★ Common, 3★ Rare, 4★ Epic, 5★ Legendary, 6★ Mythic.</summary>
        public static string Name(int rarity)
        {
            switch (Mathf.Clamp(rarity, 2, 6))
            {
                case 2: return "COMMON";
                case 3: return "RARE";
                case 4: return "EPIC";
                case 5: return "LEGENDARY";
                default: return "MYTHIC";
            }
        }

        public static Color Color(int rarity)
        {
            switch (Mathf.Clamp(rarity, 2, 6))
            {
                case 2: return new Color(0.75f, 0.78f, 0.8f);
                case 3: return new Color(0.35f, 0.65f, 1f);
                case 4: return new Color(0.75f, 0.4f, 1f);
                case 5: return new Color(1f, 0.78f, 0.25f);
                default: return new Color(1f, 0.3f, 0.45f);
            }
        }
    }

    public class EnemyDefinition
    {
        public string id;
        public string displayName;
        public EnemyArchetype archetype;
        public Element element = Element.Dark;
        public float baseHp = 2000f;
        public float baseAtk = 150f;
        public float baseDef = 100f;
        public float moveSpeed = 3f;
        public float attackRange = 1.8f;
        public float attackCooldown = 2.2f;
        public float windup = 0.55f;
        /// <summary>Accumulated stagger power required to interrupt this enemy (0 = always flinches).</summary>
        public float poise = 0f;
        public float scale = 1f;
        public float radius = 0.55f;
        public Color bodyColor = new Color(0.35f, 0.08f, 0.12f);
        public Color accentColor = new Color(0.9f, 0.2f, 0.25f);
        /// <summary>Selects the pattern set used by BossAI.</summary>
        public string bossStyle;
        /// <summary>Body plan used by the procedural model (see MonsterBuilder): shadow, beast, bone, forest, lion,
        /// void, knight, guardian, general, lord, ghoul, stalker, hunter, imp, oni, wraith, sentinel, dummy.</summary>
        public string form = "ghoul";
        /// <summary>Bestiary art in Resources/Art/Monsters (Higgsfield concept), shown in mission previews.</summary>
        public string artKey;
        /// <summary>What to exploit (shown in the bestiary on mission pages).</summary>
        public string weakness = "";
        /// <summary>Boss epithet for the intro card.</summary>
        public string bossTitle = "";
        public string description = "";
        /// <summary>HP fractions at which a boss enters its next phase (descending).</summary>
        public float[] phaseThresholds;
        /// <summary>Hides as a shadow puddle until the player comes close, then bursts out.</summary>
        public bool ambush;
        /// <summary>Periodically calls these smaller demons to its side (empty = never).</summary>
        public string summonId;
        /// <summary>Rushes across the field in a long, telegraphed straight-line charge.</summary>
        public bool charger;
        /// <summary>HP fraction under which a regular demon enrages: faster, shorter wind-ups (0 = never).</summary>
        public float enrageAt = 0.4f;
    }

    [System.Serializable]
    public class SpawnEntry
    {
        public string enemyId;
        public int count;
        public SpawnEntry(string enemyId, int count) { this.enemyId = enemyId; this.count = count; }
    }

    public class WaveDefinition
    {
        public List<SpawnEntry> spawns = new List<SpawnEntry>();

        public WaveDefinition(params SpawnEntry[] entries) { spawns.AddRange(entries); }

        public int TotalCount
        {
            get
            {
                int n = 0;
                foreach (var s in spawns) n += s.count;
                return n;
            }
        }
    }

    [System.Serializable]
    public class RewardBundle
    {
        public int exp;
        public int coins;
        public int crystals;
        public int expScrolls;
        public int skillScrolls;
        public int ascensionOre;
        public List<string> equipmentIds = new List<string>();
        public string characterId;

        /// <summary>Scrolls and ore from older reward tables, counted as XP.</summary>
        public int XpValue { get { return expScrolls * 1000 + skillScrolls * 500 + ascensionOre * 800; } }

        public bool IsEmpty
        {
            get
            {
                return exp == 0 && coins == 0 && crystals == 0 && expScrolls == 0 && skillScrolls == 0 &&
                       ascensionOre == 0 && equipmentIds.Count == 0 && string.IsNullOrEmpty(characterId);
            }
        }

        public void Add(RewardBundle other)
        {
            if (other == null) return;
            exp += other.exp;
            coins += other.coins;
            crystals += other.crystals;
            expScrolls += other.expScrolls;
            skillScrolls += other.skillScrolls;
            ascensionOre += other.ascensionOre;
            equipmentIds.AddRange(other.equipmentIds);
            if (!string.IsNullOrEmpty(other.characterId)) characterId = other.characterId;
        }
    }

    public class ArenaTheme
    {
        public Color ground = new Color(0.16f, 0.14f, 0.2f);
        public Color groundAccent = new Color(0.28f, 0.2f, 0.36f);
        public Color sky = new Color(0.04f, 0.04f, 0.1f);
        public Color fog = new Color(0.1f, 0.07f, 0.16f);
        public Color lantern = new Color(1f, 0.6f, 0.3f);
        public Color foliage = new Color(0.55f, 0.35f, 0.75f);
        public bool petals = true;
        public EnvironmentKind kind = EnvironmentKind.Village;
        public bool night = true;
        public Color sun = new Color(0.85f, 0.85f, 1f);
        public float sunIntensity = 1.1f;
        /// <summary>Linear fog start/end.</summary>
        public float fogStart = 28f;
        public float fogEnd = 75f;
        /// <summary>Village under attack: burning roofs, smoke, embers.</summary>
        public bool burning;
    }

    /// <summary>A location on the world map.</summary>
    public class RegionDefinition
    {
        public string id;
        public string name;
        public string subtitle;
        public Vector2 mapPosition;
        public ArenaTheme theme = new ArenaTheme();
        public Color mapColor = Color.gray;
        /// <summary>Mission whose clear unlocks travel here (null = always open).</summary>
        public string unlockAfterMission;
    }

    public class MissionDefinition
    {
        public string id;
        public int chapter;
        public string name;
        public string description;
        public int enemyLevel = 1;
        public int recommendedPower = 1000;
        public List<WaveDefinition> waves = new List<WaveDefinition>();
        public string bossId;
        public int killObjective = 15;
        public float parTime = 180f;
        public float timeLimit = 300f;
        public RewardBundle rewards = new RewardBundle();
        public RewardBundle firstClearRewards = new RewardBundle();
        /// <summary>Possible equipment drops (30% chance of one on victory).</summary>
        public List<string> dropTable = new List<string>();
        public string requiresMissionId;
        public ArenaTheme theme = new ArenaTheme();

        public MissionType type = MissionType.Story;
        public string regionId;
        public int recommendedLevel = 1;
        /// <summary>Short story hook shown on the mission page.</summary>
        public string storyText = "";
        public string cutsceneBefore;
        public string cutsceneAfter;
        /// <summary>Who asked for this side mission (shown on the page).</summary>
        public string questGiver;
        /// <summary>Allied soldiers fighting alongside the team (large battles).</summary>
        public int allies;
        /// <summary>Temple seal puzzle before the waves.</summary>
        public bool sealPuzzle;
        /// <summary>Training: endless weak dummies, no rewards.</summary>
        public bool training;
        /// <summary>Extra bosses fought before the final one (generals).</summary>
        public List<string> preBosses = new List<string>();
        /// <summary>Optional names for the places along the mission's route (otherwise chosen from the region).</summary>
        public List<string> route = new List<string>();
        /// <summary>A place along the route to investigate (story beat + ambush), e.g. "the abandoned village".</summary>
        public string investigate;
        /// <summary>What Ren says when investigating.</summary>
        public string investigateLine;
        /// <summary>Summon-banner trial: this slayer alone at max power for 60 seconds.</summary>
        public string trialCharacterId;
        /// <summary>Environment quality-test world ("forest", "snow", "volcano"); null for the normal builder.</summary>
        public string prototypeEnv;
        /// <summary>The collision test yard (tree, pole, rock, wall, fence, narrow and wide gaps).</summary>
        public bool collisionTest;
        /// <summary>Custom name for the destination arena (otherwise chosen from the boss).</summary>
        public string arenaName;
        /// <summary>Free-roam village (no enemies, talk to villagers, find chests).</summary>
        public bool openWorld;
        /// <summary>The event this quest belongs to (null for story missions).</summary>
        public string eventId;
        /// <summary>Event quests: 0 Easy, 1 Medium, 2 Hard.</summary>
        public int difficulty;
        /// <summary>Co-op gate missions: how much stronger the demons are (1 = normal), the gate tier, and the
        /// party members who fight beside you (AI slayers standing in for the other players).</summary>
        public float coopPower = 1f;
        public int coopTier = -1;
        public List<string> coopAllyIds = new List<string>();
        public List<string> coopAllyNames = new List<string>();
        /// <summary>PvP: -1 not PvP, 0 3v3 Battle, 1 Moon Crystal, 2 Boss Rush; ranked or casual; the opposing team.</summary>
        public int pvpMode = -1;
        public bool pvpRanked;
        public List<string> pvpEnemyIds = new List<string>();
        public List<string> pvpEnemyNames = new List<string>();
        public string MissionLabel { get { return id.ToUpper(); } }
    }

    /// <summary>A limited event: its own mini story told across five quests from Easy to Hard.</summary>
    public class EventDefinition
    {
        public string id;
        public string title;
        public string subtitle;
        /// <summary>The mini story shown on the event page.</summary>
        public string story;
        public string regionId;
        public Color accent = Color.white;
        /// <summary>Enemy whose portrait heads the event banner.</summary>
        public string bannerEnemy;
        /// <summary>Shown as the event's prize (granted by the final quest's first clear).</summary>
        public string prizeText;
        public List<MissionDefinition> quests = new List<MissionDefinition>();
    }

    public class ChapterDefinition
    {
        public int number;
        public string title;
        public string regionId;
        public string synopsis = "";
        public bool available;
        public List<MissionDefinition> missions = new List<MissionDefinition>();
    }

    public class EquipmentDefinition
    {
        public string id;
        public string displayName;
        public EquipSlot slot;
        public int rarity = 3;
        public StatBlock baseBonus;
        public StatBlock perLevel;
        public int maxLevel = 20;
        public string description;

        public StatBlock BonusAt(int level)
        {
            return baseBonus + perLevel * Mathf.Max(0, level - 1);
        }
    }
}
