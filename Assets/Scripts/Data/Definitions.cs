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

        public string FullName { get { return displayName + " — " + versionTitle; } }
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
        /// <summary>HP fractions at which a boss enters its next phase (descending).</summary>
        public float[] phaseThresholds;
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
    }

    public class ChapterDefinition
    {
        public int number;
        public string title;
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
