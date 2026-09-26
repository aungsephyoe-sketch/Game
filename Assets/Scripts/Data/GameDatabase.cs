using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// All placeholder content for the vertical slice: original characters, demons, missions and equipment.
    /// Swap this out (or back it with ScriptableObjects) when real/licensed content is ready.
    /// </summary>
    public static class GameDatabase
    {
        public static readonly List<CharacterDefinition> Characters = new List<CharacterDefinition>();
        public static readonly List<EnemyDefinition> Enemies = new List<EnemyDefinition>();
        public static readonly List<ChapterDefinition> Chapters = new List<ChapterDefinition>();
        public static readonly List<EquipmentDefinition> Equipment = new List<EquipmentDefinition>();

        public static readonly string[] StarterTeam = { "ren_initiate", "sora_initiate", "kiba_initiate" };
        public static readonly string[] StarterEquipment = { "sword_steel", "haori_cotton", "acc_charm" };

        static bool built;

        public static void EnsureBuilt()
        {
            if (built) return;
            built = true;
            BuildCharacters();
            BuildEnemies();
            BuildEquipment();
            BuildMissions();
        }

        public static CharacterDefinition GetCharacter(string id)
        {
            EnsureBuilt();
            return Characters.Find(c => c.id == id);
        }

        public static EnemyDefinition GetEnemy(string id)
        {
            EnsureBuilt();
            return Enemies.Find(e => e.id == id);
        }

        public static EquipmentDefinition GetEquipment(string id)
        {
            EnsureBuilt();
            return Equipment.Find(e => e.id == id);
        }

        public static MissionDefinition GetMission(string id)
        {
            EnsureBuilt();
            foreach (var c in Chapters)
                foreach (var m in c.missions)
                    if (m.id == id) return m;
            return null;
        }

        // ------------------------------------------------------------------ Characters

        static AbilityDefinition Ab(string name, AbilityShape shape, float cd, float mult, int hits, float range, float radius, string desc,
            float stagger = 2f, float knockback = 3f)
        {
            return new AbilityDefinition
            {
                name = name, shape = shape, cooldown = cd, damageMultiplier = mult, hits = hits,
                range = range, radius = radius, description = desc, stagger = stagger, knockback = knockback
            };
        }

        static StatBlock BaseStats(int rarity, Role role)
        {
            float r = rarity >= 5 ? 1.2f : 1f;
            var s = new StatBlock(3000f * r, 500f * r, 280f * r, 0.1f, 1.5f, 6.6f, 1f);
            switch (role)
            {
                case Role.Burst: s.atk *= 1.08f; s.hp *= 0.92f; s.crit += 0.06f; s.speed += 0.5f; break;
                case Role.Tank: s.hp *= 1.2f; s.def *= 1.25f; s.atk *= 0.9f; break;
                case Role.Support: s.hp *= 1.08f; s.specialDmg += 0.1f; break;
            }
            return s;
        }

        static void BuildCharacters()
        {
            Characters.Add(new CharacterDefinition
            {
                id = "ren_initiate", baseId = "ren", displayName = "Ren Kagami", versionTitle = "Initiate",
                breathingStyle = "Tide Breathing", rarity = 4, element = Element.Water, role = Role.DPS,
                description = "A kind-hearted swordsman who reads the flow of battle like water.",
                baseStats = BaseStats(4, Role.DPS),
                skills = new[]
                {
                    Ab("First Form: Tide Wheel", AbilityShape.Spin, 5f, 1.25f, 2, 0f, 3.3f, "A vertical wheel of water that strikes all around."),
                    Ab("Second Form: Surface Cleave", AbilityShape.Wave, 7f, 2.3f, 1, 8f, 1.6f, "Sends a cutting wave straight ahead."),
                    Ab("Third Form: Whirlpool Dance", AbilityShape.Dash, 9f, 0.9f, 4, 6.5f, 1.8f, "Flows through enemies in a dancing dash.")
                },
                ultimate = Ab("Eleventh Form: Dead Calm", AbilityShape.Burst, 0f, 1.7f, 6, 0f, 8f, "Perfect stillness. Every blade that enters is cut down.", 20f, 6f),
                bodyColor = new Color(0.12f, 0.12f, 0.14f), haoriColor = new Color(0.12f, 0.5f, 0.38f),
                hairColor = new Color(0.4f, 0.1f, 0.08f), bladeColor = new Color(0.15f, 0.35f, 0.95f)
            });

            Characters.Add(new CharacterDefinition
            {
                id = "sora_initiate", baseId = "sora", displayName = "Sora Ikazuchi", versionTitle = "Initiate",
                breathingStyle = "Storm Breathing", rarity = 4, element = Element.Thunder, role = Role.Burst,
                description = "Terrified of everything — until the first strike, which lands faster than sight.",
                baseStats = BaseStats(4, Role.Burst), attackSpeed = 1.1f,
                skills = new[]
                {
                    Ab("First Form: Flash Step", AbilityShape.Dash, 6f, 3.1f, 1, 8.5f, 1.8f, "A single lightning-fast draw cut."),
                    Ab("First Form: Sixfold", AbilityShape.MultiSlash, 9f, 0.72f, 6, 0f, 4.2f, "Six flashes in the blink of an eye."),
                    Ab("Second Form: Rolling Thunder", AbilityShape.Spin, 8f, 0.95f, 3, 0f, 3.8f, "Spinning slashes crackling with lightning.")
                },
                ultimate = Ab("Seventh Form: Heavenstrike", AbilityShape.Burst, 0f, 11f, 1, 0f, 9f, "One strike. One thunderclap. Nothing remains.", 25f, 8f),
                bodyColor = new Color(0.1f, 0.1f, 0.12f), haoriColor = new Color(0.95f, 0.72f, 0.1f),
                hairColor = new Color(1f, 0.85f, 0.25f), bladeColor = new Color(1f, 0.9f, 0.3f)
            });

            Characters.Add(new CharacterDefinition
            {
                id = "kiba_initiate", baseId = "kiba", displayName = "Kiba Arashi", versionTitle = "Initiate",
                breathingStyle = "Fang Breathing", rarity = 4, element = Element.Beast, role = Role.DPS,
                description = "Raised in the mountains. Fights with two jagged blades and zero restraint.",
                baseStats = BaseStats(4, Role.DPS), attackSpeed = 1.15f,
                comboMultipliers = new[] { 0.55f, 0.55f, 0.7f, 0.7f, 1.6f },
                skills = new[]
                {
                    Ab("First Fang: Pierce", AbilityShape.Dash, 5f, 1.35f, 2, 5.5f, 1.6f, "Charges forward with both blades thrust out."),
                    Ab("Second Fang: Rend", AbilityShape.MultiSlash, 7f, 0.9f, 4, 0f, 3.6f, "Wild criss-cross slashes."),
                    Ab("Spatial Awareness", AbilityShape.Burst, 10f, 2.5f, 1, 0f, 6.5f, "Senses every foe and lashes out at all of them.")
                },
                ultimate = Ab("Crazed Cutting", AbilityShape.MultiSlash, 0f, 1.15f, 9, 0f, 7.5f, "A frenzy of blades that shreds everything nearby.", 20f, 5f),
                bodyColor = new Color(0.55f, 0.42f, 0.32f), haoriColor = new Color(0.3f, 0.32f, 0.42f),
                hairColor = new Color(0.12f, 0.15f, 0.3f), bladeColor = new Color(0.55f, 0.55f, 0.6f)
            });

            Characters.Add(new CharacterDefinition
            {
                id = "homura_pillar", baseId = "homura", displayName = "Homura Enjoji", versionTitle = "Flame Pillar",
                breathingStyle = "Blaze Breathing", rarity = 5, element = Element.Flame, role = Role.DPS,
                description = "One of the nine Pillars. His blade burns as brightly as his heart.",
                baseStats = BaseStats(5, Role.DPS),
                skills = new[]
                {
                    Ab("First Form: Unknowing Blaze", AbilityShape.Dash, 6f, 3.3f, 1, 7.5f, 1.9f, "A blazing lunge that closes any distance."),
                    Ab("Second Form: Rising Sunfire", AbilityShape.Spin, 7f, 1.55f, 2, 0f, 3.6f, "An upward arc of flame."),
                    Ab("Fifth Form: Blazing Tiger", AbilityShape.Wave, 10f, 3.6f, 1, 10f, 2.2f, "A roaring tiger of fire tears across the field.")
                },
                ultimate = Ab("Ninth Form: Purgatory", AbilityShape.Burst, 0f, 2.3f, 5, 0f, 9f, "Everything before him is consumed in flame.", 25f, 7f),
                bodyColor = new Color(0.12f, 0.1f, 0.1f), haoriColor = new Color(0.95f, 0.95f, 0.9f),
                hairColor = new Color(1f, 0.55f, 0.1f), bladeColor = new Color(1f, 0.35f, 0.05f)
            });

            Characters.Add(new CharacterDefinition
            {
                id = "ren_sundance", baseId = "ren", displayName = "Ren Kagami", versionTitle = "Dawn Dance",
                breathingStyle = "Dawn Kagura", rarity = 5, element = Element.Light, role = Role.Burst,
                description = "Ren awakens the ancestral dance of dawn. A different fighter entirely.",
                baseStats = BaseStats(5, Role.Burst),
                skills = new[]
                {
                    Ab("Kagura: Circle Dance", AbilityShape.Spin, 5f, 1.4f, 3, 0f, 3.8f, "A flowing circular dance of burning light."),
                    Ab("Kagura: Clear Sky", AbilityShape.Wave, 7f, 2.9f, 1, 9f, 2f, "A wide arc of dawn light."),
                    Ab("Kagura: Burning Bones", AbilityShape.Dash, 8f, 1.5f, 3, 7f, 2f, "Rushes through the enemy line leaving embers.")
                },
                ultimate = Ab("Thirteenth Form: Unbroken Dawn", AbilityShape.MultiSlash, 0f, 1.3f, 12, 0f, 9f, "All twelve forms, repeated until sunrise.", 30f, 6f),
                bodyColor = new Color(0.12f, 0.12f, 0.14f), haoriColor = new Color(0.95f, 0.5f, 0.15f),
                hairColor = new Color(0.4f, 0.1f, 0.08f), bladeColor = new Color(1f, 0.75f, 0.3f)
            });
        }

        // ------------------------------------------------------------------ Enemies

        static void BuildEnemies()
        {
            Enemies.Add(new EnemyDefinition
            {
                id = "grunt", displayName = "Lesser Demon", archetype = EnemyArchetype.Normal, element = Element.Beast,
                baseHp = 2200, baseAtk = 160, baseDef = 100, moveSpeed = 3.2f, attackRange = 1.9f, attackCooldown = 2.2f, windup = 0.6f
            });
            Enemies.Add(new EnemyDefinition
            {
                id = "runner", displayName = "Swift Demon", archetype = EnemyArchetype.Fast, element = Element.Thunder,
                baseHp = 1500, baseAtk = 135, baseDef = 80, moveSpeed = 5.8f, attackRange = 3.2f, attackCooldown = 1.8f, windup = 0.4f,
                scale = 0.85f, bodyColor = new Color(0.3f, 0.25f, 0.08f), accentColor = new Color(1f, 0.85f, 0.2f)
            });
            Enemies.Add(new EnemyDefinition
            {
                id = "brute", displayName = "Armored Demon", archetype = EnemyArchetype.Tank, element = Element.Flame,
                baseHp = 6500, baseAtk = 250, baseDef = 400, moveSpeed = 2.1f, attackRange = 2.6f, attackCooldown = 3f, windup = 0.95f,
                poise = 6f, scale = 1.5f, radius = 0.9f, bodyColor = new Color(0.35f, 0.15f, 0.05f), accentColor = new Color(1f, 0.45f, 0.1f)
            });
            Enemies.Add(new EnemyDefinition
            {
                id = "spitter", displayName = "Blood-Spitter", archetype = EnemyArchetype.Ranged, element = Element.Water,
                baseHp = 1800, baseAtk = 170, baseDef = 90, moveSpeed = 2.8f, attackRange = 10f, attackCooldown = 2.8f, windup = 0.65f,
                bodyColor = new Color(0.08f, 0.18f, 0.3f), accentColor = new Color(0.3f, 0.7f, 1f)
            });
            Enemies.Add(new EnemyDefinition
            {
                id = "elite", displayName = "Crescent Hunter", archetype = EnemyArchetype.Elite, element = Element.Dark,
                baseHp = 9000, baseAtk = 290, baseDef = 300, moveSpeed = 3.8f, attackRange = 2.6f, attackCooldown = 2.4f, windup = 0.7f,
                poise = 10f, scale = 1.25f, radius = 0.75f, bodyColor = new Color(0.2f, 0.05f, 0.25f), accentColor = new Color(0.85f, 0.2f, 1f)
            });
            Enemies.Add(new EnemyDefinition
            {
                id = "boss_thousandarm", displayName = "The Thousand-Arm Demon", archetype = EnemyArchetype.Boss, element = Element.Beast,
                baseHp = 30000, baseAtk = 260, baseDef = 280, moveSpeed = 2.6f, attackRange = 4f, attackCooldown = 1.6f, windup = 0.8f,
                poise = 60f, scale = 2.1f, radius = 1.3f, bossStyle = "thousandarm", phaseThresholds = new[] { 0.5f },
                bodyColor = new Color(0.25f, 0.35f, 0.18f), accentColor = new Color(0.8f, 1f, 0.4f)
            });
            Enemies.Add(new EnemyDefinition
            {
                id = "boss_goken", displayName = "Goken, the Crimson Fist", archetype = EnemyArchetype.Boss, element = Element.Flame,
                baseHp = 60000, baseAtk = 360, baseDef = 360, moveSpeed = 4.2f, attackRange = 2.8f, attackCooldown = 1.2f, windup = 0.6f,
                poise = 80f, scale = 1.45f, radius = 0.9f, bossStyle = "goken", phaseThresholds = new[] { 0.7f, 0.3f },
                bodyColor = new Color(0.85f, 0.55f, 0.5f), accentColor = new Color(0.35f, 0.6f, 1f)
            });
        }

        // ------------------------------------------------------------------ Equipment

        static void BuildEquipment()
        {
            Equipment.Add(new EquipmentDefinition
            {
                id = "sword_steel", displayName = "Steel Nichirin Blade", slot = EquipSlot.Sword, rarity = 3,
                baseBonus = new StatBlock(0, 180, 0, 0.02f), perLevel = new StatBlock(0, 22, 0), description = "Standard-issue slayer blade."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "sword_crimson", displayName = "Crimson-Forged Blade", slot = EquipSlot.Sword, rarity = 4,
                baseBonus = new StatBlock(0, 450, 0, 0.05f), perLevel = new StatBlock(0, 40, 0, 0.002f), description = "ATK +450, CRIT +5%."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "sword_dawn", displayName = "Dawnsteel Blade", slot = EquipSlot.Sword, rarity = 5,
                baseBonus = new StatBlock(0, 620, 0, 0.06f, 0.2f), perLevel = new StatBlock(0, 55, 0, 0.002f, 0.01f), description = "Glows faintly at sunrise."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "haori_cotton", displayName = "Cotton Haori", slot = EquipSlot.Haori, rarity = 3,
                baseBonus = new StatBlock(400, 0, 90), perLevel = new StatBlock(60, 0, 12), description = "Simple but sturdy."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "haori_wisteria", displayName = "Wisteria Haori", slot = EquipSlot.Haori, rarity = 4,
                baseBonus = new StatBlock(900, 0, 200), perLevel = new StatBlock(110, 0, 22), description = "Scented with wisteria. Demons hate it."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "acc_charm", displayName = "Protective Charm", slot = EquipSlot.Accessory, rarity = 3,
                baseBonus = new StatBlock(200, 40, 40), perLevel = new StatBlock(25, 5, 5), description = "A charm from home."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "acc_mask", displayName = "Fox Mask", slot = EquipSlot.Accessory, rarity = 4,
                baseBonus = new StatBlock(0, 80, 0, 0.05f, 0.15f, 0.3f), perLevel = new StatBlock(0, 10, 0, 0.002f, 0.01f),
                description = "CRIT +5%, CRIT DMG +15%, SPD +0.3."
            });
            Equipment.Add(new EquipmentDefinition
            {
                id = "acc_earrings", displayName = "Hanafuda Earrings", slot = EquipSlot.Accessory, rarity = 5,
                baseBonus = new StatBlock(300, 120, 60, 0.03f, 0f, 0f, 0.15f), perLevel = new StatBlock(30, 12, 6, 0f, 0f, 0f, 0.01f),
                description = "Special (skill/ultimate) damage +15%."
            });
        }

        // ------------------------------------------------------------------ Missions

        static WaveDefinition Wave(params object[] pairs)
        {
            var w = new WaveDefinition();
            for (int i = 0; i + 1 < pairs.Length; i += 2) w.spawns.Add(new SpawnEntry((string)pairs[i], (int)pairs[i + 1]));
            return w;
        }

        static void BuildMissions()
        {
            var wisteria = new ArenaTheme();
            var lantern = new ArenaTheme
            {
                ground = new Color(0.2f, 0.12f, 0.1f), groundAccent = new Color(0.4f, 0.14f, 0.1f),
                sky = new Color(0.08f, 0.02f, 0.05f), fog = new Color(0.2f, 0.06f, 0.08f),
                lantern = new Color(1f, 0.35f, 0.2f), foliage = new Color(0.8f, 0.2f, 0.25f), petals = false
            };

            var ch1 = new ChapterDefinition { number = 1, title = "Trial of Wisteria Hill", available = true };
            ch1.missions.Add(new MissionDefinition
            {
                id = "1-1", chapter = 1, name = "Night of Trials", enemyLevel = 1, recommendedPower = 2500,
                description = "Survive the first night of the Final Trial.",
                waves = { Wave("grunt", 4), Wave("grunt", 4, "runner", 2), Wave("grunt", 3, "runner", 3) },
                killObjective = 15, parTime = 150f, theme = wisteria,
                rewards = new RewardBundle { exp = 600, coins = 3000, expScrolls = 2, skillScrolls = 1 },
                firstClearRewards = new RewardBundle { crystals = 50, coins = 2000 },
                dropTable = { "sword_steel", "haori_cotton", "acc_charm" }
            });
            ch1.missions.Add(new MissionDefinition
            {
                id = "1-2", chapter = 1, name = "Howling Pines", enemyLevel = 3, recommendedPower = 3200,
                description = "Something fast moves between the trees.", requiresMissionId = "1-1",
                waves = { Wave("runner", 4, "grunt", 3), Wave("spitter", 3, "grunt", 4), Wave("brute", 1, "runner", 3, "spitter", 2) },
                killObjective = 20, parTime = 180f, theme = wisteria,
                rewards = new RewardBundle { exp = 1100, coins = 4500, expScrolls = 3, skillScrolls = 2, ascensionOre = 1 },
                firstClearRewards = new RewardBundle { crystals = 50, equipmentIds = { "haori_wisteria" } },
                dropTable = { "sword_steel", "haori_cotton", "acc_charm", "acc_mask" }
            });
            ch1.missions.Add(new MissionDefinition
            {
                id = "1-3", chapter = 1, name = "The Thousand-Arm Demon", enemyLevel = 5, recommendedPower = 4000,
                description = "The demon that has haunted the Trial for decades. BOSS.", requiresMissionId = "1-2",
                waves = { Wave("grunt", 5), Wave("runner", 3, "spitter", 2) }, bossId = "boss_thousandarm",
                killObjective = 10, parTime = 210f, theme = wisteria,
                rewards = new RewardBundle { exp = 1800, coins = 8000, crystals = 5, expScrolls = 4, skillScrolls = 3, ascensionOre = 2 },
                firstClearRewards = new RewardBundle { crystals = 100, characterId = "homura_pillar", equipmentIds = { "sword_crimson" } },
                dropTable = { "sword_crimson", "haori_wisteria", "acc_mask" }
            });

            var ch2 = new ChapterDefinition { number = 2, title = "The Lantern Quarter", available = true };
            ch2.missions.Add(new MissionDefinition
            {
                id = "2-1", chapter = 2, name = "Lanterns in the Rain", enemyLevel = 8, recommendedPower = 6000,
                description = "Demons hide among the crowds of the pleasure district.", requiresMissionId = "1-3",
                waves = { Wave("grunt", 5, "runner", 3), Wave("brute", 2, "spitter", 3), Wave("elite", 1, "grunt", 4) },
                killObjective = 22, parTime = 190f, theme = lantern,
                rewards = new RewardBundle { exp = 2600, coins = 9000, expScrolls = 4, skillScrolls = 3, ascensionOre = 2 },
                firstClearRewards = new RewardBundle { crystals = 60 },
                dropTable = { "sword_crimson", "haori_wisteria", "acc_mask" }
            });
            ch2.missions.Add(new MissionDefinition
            {
                id = "2-2", chapter = 2, name = "Crimson Alley", enemyLevel = 11, recommendedPower = 8000,
                description = "Two Crescent Hunters stalk the alleys.", requiresMissionId = "2-1",
                waves = { Wave("runner", 6, "spitter", 2), Wave("elite", 1, "brute", 1, "grunt", 3), Wave("elite", 2, "spitter", 3) },
                killObjective = 25, parTime = 200f, theme = lantern,
                rewards = new RewardBundle { exp = 3600, coins = 12000, expScrolls = 5, skillScrolls = 4, ascensionOre = 3 },
                firstClearRewards = new RewardBundle { crystals = 60, equipmentIds = { "acc_earrings" } },
                dropTable = { "sword_crimson", "haori_wisteria", "acc_mask", "acc_earrings" }
            });
            ch2.missions.Add(new MissionDefinition
            {
                id = "2-3", chapter = 2, name = "The Crimson Fist", enemyLevel = 14, recommendedPower = 11000,
                description = "A demon of the Upper Crescents. Learn his patterns — or fall. BOSS.", requiresMissionId = "2-2",
                waves = { Wave("elite", 1, "runner", 4) }, bossId = "boss_goken",
                killObjective = 5, parTime = 240f, timeLimit = 360f, theme = lantern,
                rewards = new RewardBundle { exp = 5000, coins = 20000, crystals = 10, expScrolls = 6, skillScrolls = 5, ascensionOre = 5 },
                firstClearRewards = new RewardBundle { crystals = 150, characterId = "ren_sundance", equipmentIds = { "sword_dawn" } },
                dropTable = { "sword_dawn", "acc_earrings", "haori_wisteria" }
            });

            Chapters.Add(ch1);
            Chapters.Add(ch2);
            Chapters.Add(new ChapterDefinition { number = 3, title = "Swordsmith Hollow", available = false });
            Chapters.Add(new ChapterDefinition { number = 4, title = "Pillar Training", available = false });
            Chapters.Add(new ChapterDefinition { number = 5, title = "The Endless Keep", available = false });
        }
    }
}
