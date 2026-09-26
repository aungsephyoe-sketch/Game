using System.Collections.Generic;
using UnityEngine;

namespace HashiraChronicles
{
    /// <summary>
    /// All game content: playable slayers (by rarity), story NPCs, demons and bosses, world regions,
    /// chapters/missions and equipment. Everything is original. Swap this for data assets or remote
    /// config later without touching gameplay code.
    ///
    /// Story: Ren Kagami, a young swordsman from Kiriha village, watches the Demon Lord Veyrath's
    /// eclipse swallow his home. A mark of dawn-light awakens in him. Five centuries ago the hero Akatsuki
    /// split the Demon Lord's heart in two – the Dark half sealed away, the Dawn half lost. Ren carries it.
    /// </summary>
    public static class GameDatabase
    {
        public static readonly List<CharacterDefinition> Characters = new List<CharacterDefinition>();
        public static readonly List<CharacterDefinition> Npcs = new List<CharacterDefinition>();
        public static readonly List<EnemyDefinition> Enemies = new List<EnemyDefinition>();
        public static readonly List<RegionDefinition> Regions = new List<RegionDefinition>();
        public static readonly List<ChapterDefinition> Chapters = new List<ChapterDefinition>();
        public static readonly List<MissionDefinition> ExtraMissions = new List<MissionDefinition>();
        public static readonly List<EquipmentDefinition> Equipment = new List<EquipmentDefinition>();

        public const string Protagonist = "ren_initiate";
        public static readonly string[] StarterTeam = { Protagonist };
        public static readonly string[] StarterEquipment = { "sword_steel", "haori_cotton", "acc_charm" };

        static bool built;

        public static void EnsureBuilt()
        {
            if (built) return;
            built = true;
            BuildCharacters();
            BuildNpcs();
            BuildEnemies();
            BuildEquipment();
            BuildRegions();
            BuildMissions();
        }

        public static CharacterDefinition GetCharacter(string id)
        {
            EnsureBuilt();
            var c = Characters.Find(x => x.id == id);
            return c ?? Npcs.Find(x => x.id == id);
        }

        public static EnemyDefinition GetEnemy(string id) { EnsureBuilt(); return Enemies.Find(e => e.id == id); }
        public static EquipmentDefinition GetEquipment(string id) { EnsureBuilt(); return Equipment.Find(e => e.id == id); }
        public static RegionDefinition GetRegion(string id) { EnsureBuilt(); return Regions.Find(r => r.id == id); }

        public static MissionDefinition GetMission(string id)
        {
            EnsureBuilt();
            foreach (var c in Chapters)
                foreach (var m in c.missions)
                    if (m.id == id) return m;
            return ExtraMissions.Find(m => m.id == id);
        }

        public static IEnumerable<MissionDefinition> AllMissions()
        {
            EnsureBuilt();
            foreach (var c in Chapters)
                foreach (var m in c.missions)
                    yield return m;
            foreach (var m in ExtraMissions) yield return m;
        }

        public static List<MissionDefinition> MissionsInRegion(string regionId)
        {
            var list = new List<MissionDefinition>();
            foreach (var m in AllMissions())
                if (m.regionId == regionId && m.type != MissionType.Training && m.type != MissionType.Event) list.Add(m);
            return list;
        }

        public static ChapterDefinition ChapterOf(MissionDefinition m)
        {
            return Chapters.Find(c => c.missions.Contains(m));
        }

        /// <summary>Characters that can appear from summons.</summary>
        public static List<CharacterDefinition> SummonPool(int rarity)
        {
            EnsureBuilt();
            return Characters.FindAll(c => !c.storyOnly && !c.npc && c.rarity == rarity);
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
            float r = 0.85f + 0.18f * (Mathf.Clamp(rarity, 3, 7) - 3);
            var s = new StatBlock(3000f * r, 500f * r, 280f * r, 0.1f, 1.5f, 6.6f, 1f);
            switch (role)
            {
                case Role.Burst: s.atk *= 1.08f; s.hp *= 0.92f; s.crit += 0.06f; s.speed += 0.5f; break;
                case Role.Tank: s.hp *= 1.35f; s.def *= 1.4f; s.atk *= 0.85f; s.speed -= 0.4f; break;
                case Role.Support: s.hp *= 1.1f; s.specialDmg += 0.15f; s.atk *= 0.9f; break;
            }
            return s;
        }

        static CharacterDefinition Hero(string id, string baseId, string name, string title, string style, int rarity, Element el, Role role,
            string desc, string story, Color body, Color haori, Color hair, Color blade)
        {
            return new CharacterDefinition
            {
                id = id, baseId = baseId, displayName = name, versionTitle = title, breathingStyle = style, rarity = rarity,
                element = el, role = role, description = desc, story = story, baseStats = BaseStats(rarity, role),
                bodyColor = body, haoriColor = haori, hairColor = hair, bladeColor = blade
            };
        }

        static void BuildCharacters()
        {
            var ren = Hero("ren_initiate", "ren", "Ren Kagami", "Kiriha Swordsman", "Tide Breathing", 4, Element.Water, Role.DPS,
                "The last swordsman of Kiriha. Kind, stubborn, and carrying a light he doesn't understand.",
                "Raised by Master Tessai after his parents vanished on the night of a crimson moon. Ren trained every day for a battle he hoped would never come. It came.",
                new Color(0.12f, 0.12f, 0.14f), new Color(0.12f, 0.5f, 0.38f), new Color(0.4f, 0.1f, 0.08f), new Color(0.15f, 0.35f, 0.95f));
            ren.storyOnly = true;
            ren.skills = new[]
            {
                Ab("First Form: Tide Wheel", AbilityShape.Spin, 5f, 1.25f, 2, 0f, 3.3f, "A vertical wheel of water that strikes all around."),
                Ab("Second Form: Surface Cleave", AbilityShape.Wave, 7f, 2.3f, 1, 8f, 1.6f, "Sends a cutting wave straight ahead."),
                Ab("Third Form: Whirlpool Dance", AbilityShape.Dash, 9f, 0.9f, 4, 6.5f, 1.8f, "Flows through enemies in a dancing dash.")
            };
            ren.ultimate = Ab("Eleventh Form: Dead Calm", AbilityShape.Burst, 0f, 1.7f, 6, 0f, 8f, "Perfect stillness. Every blade that enters is cut down.", 20f, 6f);
            Characters.Add(ren);

            var sora = Hero("sora_initiate", "sora", "Sora Ikazuchi", "Frightened Blade", "Storm Breathing", 4, Element.Thunder, Role.Burst,
                "Terrified of everything — until the first strike, which lands faster than sight.",
                "Found hiding in the Forest of Shadows after his squad was wiped out. He follows Ren because Ren is the first person who didn't call him a coward.",
                new Color(0.1f, 0.1f, 0.12f), new Color(0.95f, 0.72f, 0.1f), new Color(1f, 0.85f, 0.25f), new Color(1f, 0.9f, 0.3f));
            sora.storyOnly = true;
            sora.attackSpeed = 1.1f;
            sora.skills = new[]
            {
                Ab("First Form: Flash Step", AbilityShape.Dash, 6f, 3.1f, 1, 8.5f, 1.8f, "A single lightning-fast draw cut."),
                Ab("First Form: Sixfold", AbilityShape.MultiSlash, 9f, 0.72f, 6, 0f, 4.2f, "Six flashes in the blink of an eye."),
                Ab("Second Form: Rolling Thunder", AbilityShape.Spin, 8f, 0.95f, 3, 0f, 3.8f, "Spinning slashes crackling with lightning.")
            };
            sora.ultimate = Ab("Seventh Form: Heavenstrike", AbilityShape.Burst, 0f, 11f, 1, 0f, 9f, "One strike. One thunderclap. Nothing remains.", 25f, 8f);
            Characters.Add(sora);

            var kiba = Hero("kiba_initiate", "kiba", "Kiba Arashi", "Arena Rival", "Fang Breathing", 4, Element.Beast, Role.DPS,
                "Raised in the mountains. Fights with two jagged blades and zero restraint.",
                "Champion of the Solmere arena. He thinks Ren is soft. He also can't stop following him.",
                new Color(0.55f, 0.42f, 0.32f), new Color(0.3f, 0.32f, 0.42f), new Color(0.12f, 0.15f, 0.3f), new Color(0.55f, 0.55f, 0.6f));
            kiba.storyOnly = true;
            kiba.attackSpeed = 1.15f;
            kiba.comboMultipliers = new[] { 0.55f, 0.55f, 0.7f, 0.7f, 1.6f };
            kiba.skills = new[]
            {
                Ab("First Fang: Pierce", AbilityShape.Dash, 5f, 1.35f, 2, 5.5f, 1.6f, "Charges forward with both blades thrust out."),
                Ab("Second Fang: Rend", AbilityShape.MultiSlash, 7f, 0.9f, 4, 0f, 3.6f, "Wild criss-cross slashes."),
                Ab("Spatial Awareness", AbilityShape.Burst, 10f, 2.5f, 1, 0f, 6.5f, "Senses every foe and lashes out at all of them.")
            };
            kiba.ultimate = Ab("Crazed Cutting", AbilityShape.MultiSlash, 0f, 1.15f, 9, 0f, 7.5f, "A frenzy of blades that shreds everything nearby.", 20f, 5f);
            Characters.Add(kiba);

            var hana = Hero("hana_healer", "hana", "Hana Shiraume", "Wisteria Healer", "Blossom Breathing", 5, Element.Water, Role.Support,
                "A royal healer who treats demons' victims — and quietly studies the demons themselves.",
                "Hana lost her village to the eclipse too. She believes the demons were human once. She is right.",
                new Color(0.95f, 0.92f, 0.95f), new Color(0.75f, 0.55f, 0.9f), new Color(0.2f, 0.12f, 0.25f), new Color(0.9f, 0.7f, 1f));
            hana.storyOnly = true;
            hana.skills = new[]
            {
                Ab("Healing Mist", AbilityShape.Heal, 12f, 0.22f, 1, 0f, 6f, "Heals the whole team for 22% of max HP."),
                Ab("Petal Storm", AbilityShape.MultiSlash, 8f, 0.8f, 5, 0f, 4f, "A storm of razor-sharp petals."),
                Ab("Moonlit Ward", AbilityShape.Burst, 10f, 1.6f, 2, 0f, 5f, "A ring of moonlight that repels demons.")
            };
            hana.ultimate = Ab("Garden of Rebirth", AbilityShape.Heal, 0f, 0.5f, 1, 0f, 9f, "Restores 50% HP to every slayer and blasts nearby demons.", 15f, 6f);
            Characters.Add(hana);

            var tetsu = Hero("tetsu_guard", "tetsu", "Tetsu Ganryu", "Iron Captain", "Stone Breathing", 5, Element.Beast, Role.Tank,
                "Captain of the Solmere guard. Wields a greatsword like a wall.",
                "Tetsu served the Chancellor for twenty years. Learning what the Chancellor really was broke something in him — and hardened the rest.",
                new Color(0.3f, 0.3f, 0.35f), new Color(0.25f, 0.35f, 0.6f), new Color(0.15f, 0.12f, 0.1f), new Color(0.7f, 0.65f, 0.55f));
            tetsu.storyOnly = true;
            tetsu.attackSpeed = 0.85f;
            tetsu.comboMultipliers = new[] { 0.8f, 0.85f, 0.95f, 1.0f, 1.9f };
            tetsu.skills = new[]
            {
                Ab("Iron Wall", AbilityShape.Burst, 9f, 1.2f, 1, 0f, 4.5f, "Slams the ground, staggering everything nearby.", 8f, 7f),
                Ab("Shield Rush", AbilityShape.Dash, 7f, 1.8f, 1, 6f, 2.2f, "Barrels through the enemy line.", 6f, 8f),
                Ab("Earthsplitter", AbilityShape.Wave, 10f, 2.6f, 1, 9f, 2.4f, "Splits the earth in a straight line.", 6f, 5f)
            };
            tetsu.ultimate = Ab("Fortress Breaker", AbilityShape.Burst, 0f, 3.2f, 3, 0f, 8f, "Three earth-shattering blows.", 30f, 9f);
            Characters.Add(tetsu);

            var homura = Hero("homura_pillar", "homura", "Homura Enjoji", "Flame Pillar", "Blaze Breathing", 6, Element.Flame, Role.DPS,
                "One of the nine Pillars. His blade burns as brightly as his heart.",
                "The strongest swordsman alive, and the first to believe in Ren. He stays behind so the others can live.",
                new Color(0.12f, 0.1f, 0.1f), new Color(0.95f, 0.95f, 0.9f), new Color(1f, 0.55f, 0.1f), new Color(1f, 0.35f, 0.05f));
            homura.storyOnly = true;
            homura.skills = new[]
            {
                Ab("First Form: Unknowing Blaze", AbilityShape.Dash, 6f, 3.3f, 1, 7.5f, 1.9f, "A blazing lunge that closes any distance."),
                Ab("Second Form: Rising Sunfire", AbilityShape.Spin, 7f, 1.55f, 2, 0f, 3.6f, "An upward arc of flame."),
                Ab("Fifth Form: Blazing Tiger", AbilityShape.Wave, 10f, 3.6f, 1, 10f, 2.2f, "A roaring tiger of fire tears across the field.")
            };
            homura.ultimate = Ab("Ninth Form: Purgatory", AbilityShape.Burst, 0f, 2.3f, 5, 0f, 9f, "Everything before him is consumed in flame.", 25f, 7f);
            Characters.Add(homura);

            var homura2 = Hero("homura_lastflame", "homura", "Homura Enjoji", "Last Flame", "Blaze Breathing: Final", 7, Element.Flame, Role.Burst,
                "He came back from the gate. Scarred, one-eyed, and burning hotter than ever.",
                "No one saw how he survived the fall of Solmere. He only says: 'A flame this stubborn doesn't go out.'",
                new Color(0.1f, 0.08f, 0.08f), new Color(0.85f, 0.2f, 0.1f), new Color(1f, 0.45f, 0.05f), new Color(1f, 0.6f, 0.1f));
            homura2.storyOnly = true;
            homura2.skills = new[]
            {
                Ab("Ember Rush", AbilityShape.Dash, 5f, 1.4f, 3, 8f, 2.2f, "Three blazing lunges in a row."),
                Ab("Phoenix Wheel", AbilityShape.Spin, 6f, 1.3f, 4, 0f, 4.2f, "A spinning wheel of phoenix fire."),
                Ab("Sun Tiger", AbilityShape.Wave, 9f, 2.4f, 2, 11f, 2.6f, "Twin tigers of fire.")
            };
            homura2.ultimate = Ab("Final Form: Rekka", AbilityShape.Burst, 0f, 3.4f, 6, 0f, 10f, "His last flame, given everything.", 30f, 8f);
            Characters.Add(homura2);

            var dawn = Hero("ren_sundance", "ren", "Ren Kagami", "Dawn Mark", "Dawn Kagura", 6, Element.Light, Role.Burst,
                "The Dawn half of the Demon Lord's heart, awakened and finally under Ren's control.",
                "In the Forgotten Temple, Ren learned the truth: the light inside him was never his. He chose to make it his anyway.",
                new Color(0.12f, 0.12f, 0.14f), new Color(0.95f, 0.5f, 0.15f), new Color(0.4f, 0.1f, 0.08f), new Color(1f, 0.75f, 0.3f));
            dawn.storyOnly = true;
            dawn.skills = new[]
            {
                Ab("Kagura: Circle Dance", AbilityShape.Spin, 5f, 1.4f, 3, 0f, 3.8f, "A flowing circular dance of burning light."),
                Ab("Kagura: Clear Sky", AbilityShape.Wave, 7f, 2.9f, 1, 9f, 2f, "A wide arc of dawn light."),
                Ab("Kagura: Burning Bones", AbilityShape.Dash, 8f, 1.5f, 3, 7f, 2f, "Rushes through the enemy line leaving embers.")
            };
            dawn.ultimate = Ab("Thirteenth Form: Unbroken Dawn", AbilityShape.MultiSlash, 0f, 1.3f, 12, 0f, 9f, "All twelve forms, repeated until sunrise.", 30f, 6f);
            Characters.Add(dawn);

            // ---- Summonable heroes (all rarities) ----
            var mina = Hero("mina_ember", "mina", "Mina Kaede", "Ember Blade", "Flame Breathing", 3, Element.Flame, Role.DPS,
                "A cheerful blacksmith's daughter with a very sharp sword.", "Mina forged her own blade. It's slightly too big. She doesn't care.",
                new Color(0.2f, 0.15f, 0.12f), new Color(0.85f, 0.35f, 0.2f), new Color(0.35f, 0.15f, 0.1f), new Color(1f, 0.45f, 0.2f));
            mina.skills = new[]
            {
                Ab("Spark Slash", AbilityShape.Dash, 6f, 2.2f, 1, 6f, 1.6f, "A quick fiery dash."),
                Ab("Cinder Spin", AbilityShape.Spin, 7f, 1.1f, 2, 0f, 3.2f, "A spinning slash trailing embers."),
                Ab("Forge Burst", AbilityShape.Burst, 10f, 2f, 1, 0f, 4.5f, "Releases the heat of the forge.")
            };
            mina.ultimate = Ab("Hammerfall", AbilityShape.Burst, 0f, 1.6f, 4, 0f, 7f, "A rain of molten strikes.", 18f, 6f);
            Characters.Add(mina);

            var rokuro = Hero("rokuro_hunter", "rokuro", "Rokuro", "Mountain Hunter", "Beast Tracking", 3, Element.Beast, Role.Tank,
                "A quiet hunter who has tracked demons in the snow for thirty years.", "He never talks about the thing he's still hunting.",
                new Color(0.35f, 0.3f, 0.25f), new Color(0.4f, 0.45f, 0.3f), new Color(0.25f, 0.25f, 0.25f), new Color(0.6f, 0.6f, 0.6f));
            rokuro.attackSpeed = 0.9f;
            rokuro.skills = new[]
            {
                Ab("Trap Strike", AbilityShape.Burst, 8f, 1.4f, 1, 0f, 4f, "Stuns everything nearby.", 7f, 4f),
                Ab("Tusk Charge", AbilityShape.Dash, 7f, 1.7f, 1, 6f, 2f, "A heavy shoulder charge.", 5f, 7f),
                Ab("Howl", AbilityShape.Spin, 9f, 1f, 2, 0f, 4f, "A spinning sweep.")
            };
            rokuro.ultimate = Ab("Hunter's Moon", AbilityShape.Burst, 0f, 2.4f, 3, 0f, 7.5f, "The hunt ends here.", 25f, 8f);
            Characters.Add(rokuro);

            var genji = Hero("genji_ronin", "genji", "Genji Hayabusa", "Storm Ronin", "Wind Breathing", 4, Element.Thunder, Role.DPS,
                "A wandering ronin who sells his sword to anyone fighting demons.", "Genji's master was the first person the eclipse took.",
                new Color(0.15f, 0.15f, 0.2f), new Color(0.35f, 0.4f, 0.55f), new Color(0.1f, 0.1f, 0.12f), new Color(0.7f, 0.85f, 1f));
            genji.attackSpeed = 1.1f;
            genji.skills = new[]
            {
                Ab("Gale Cut", AbilityShape.Wave, 6f, 2.2f, 2, 8f, 1.6f, "Twin wind blades."),
                Ab("Hayabusa", AbilityShape.Dash, 7f, 2.6f, 1, 9f, 1.8f, "A falcon-fast dash strike."),
                Ab("Tempest", AbilityShape.Spin, 9f, 0.9f, 4, 0f, 4f, "A spinning storm.")
            };
            genji.ultimate = Ab("Thousand Winds", AbilityShape.MultiSlash, 0f, 1.1f, 10, 0f, 8f, "A thousand cuts carried on the wind.", 20f, 5f);
            Characters.Add(genji);

            var yui = Hero("yui_tide", "yui", "Yui Aozora", "Tide Dancer", "Tide Breathing", 5, Element.Water, Role.Burst,
                "Ren's senior from the same school of swordsmanship — graceful and lethal.", "She left Kiriha years ago. She's been looking for Ren ever since the red sky.",
                new Color(0.1f, 0.12f, 0.18f), new Color(0.2f, 0.55f, 0.85f), new Color(0.08f, 0.1f, 0.2f), new Color(0.4f, 0.8f, 1f));
            yui.skills = new[]
            {
                Ab("Ripple", AbilityShape.Spin, 5f, 1.2f, 3, 0f, 3.5f, "Rings of water."),
                Ab("Waterfall Basin", AbilityShape.Burst, 8f, 2.6f, 1, 0f, 5f, "A crushing downward torrent."),
                Ab("Flowing Current", AbilityShape.Dash, 7f, 1f, 4, 7f, 2f, "Flows through every foe.")
            };
            yui.ultimate = Ab("Great Deluge", AbilityShape.Burst, 0f, 2f, 6, 0f, 9f, "The sea itself answers.", 22f, 6f);
            Characters.Add(yui);

            var raiga = Hero("raiga_pillar", "raiga", "Raiga Tenrai", "Thunder Pillar", "Thunder God Breathing", 6, Element.Thunder, Role.Burst,
                "The Thunder Pillar. Arrogant, flashy, and absolutely worth it.", "Raiga has never lost a duel. He considers the Demon Lord a scheduling problem.",
                new Color(0.1f, 0.1f, 0.1f), new Color(0.95f, 0.85f, 0.2f), new Color(0.95f, 0.95f, 0.95f), new Color(1f, 0.95f, 0.4f));
            raiga.attackSpeed = 1.2f;
            raiga.skills = new[]
            {
                Ab("Thunderclap", AbilityShape.Dash, 5f, 3.4f, 1, 10f, 2f, "Crosses the arena in a flash."),
                Ab("Raijin Drums", AbilityShape.Burst, 8f, 1.4f, 4, 0f, 5.5f, "Four bolts of lightning."),
                Ab("Storm Crown", AbilityShape.Spin, 9f, 1.3f, 4, 0f, 4.5f, "A crown of spinning lightning.")
            };
            raiga.ultimate = Ab("Heaven's Judgement", AbilityShape.Burst, 0f, 14f, 1, 0f, 10f, "The sky splits open.", 30f, 9f);
            Characters.Add(raiga);

            var kuroe = Hero("kuroe_moon", "kuroe", "Kuroe", "Crimson Moon", "Moon Breathing", 7, Element.Dark, Role.Burst,
                "A demon who remembers being human, and hunts her own kind.", "Kuroe knew Veyrath before he was the Demon Lord. She won't say how.",
                new Color(0.12f, 0.05f, 0.1f), new Color(0.55f, 0.1f, 0.25f), new Color(0.05f, 0.05f, 0.08f), new Color(1f, 0.2f, 0.4f));
            kuroe.skills = new[]
            {
                Ab("Crescent Moon", AbilityShape.Wave, 5f, 1.8f, 3, 9f, 2f, "Three crescent blades."),
                Ab("Moonfall", AbilityShape.Burst, 8f, 2f, 3, 0f, 6f, "Crescents rain from above."),
                Ab("Blood Eclipse", AbilityShape.MultiSlash, 9f, 0.9f, 8, 0f, 5f, "Eight slashes in darkness.")
            };
            kuroe.ultimate = Ab("Moon Breathing: Final Night", AbilityShape.MultiSlash, 0f, 1.6f, 14, 0f, 10f, "Night falls. Nothing survives it.", 30f, 7f);
            Characters.Add(kuroe);
        }

        static void BuildNpcs()
        {
            Npcs.Add(new CharacterDefinition { id = "npc_tessai", displayName = "Master Tessai", versionTitle = "Ren's teacher", npc = true,
                bodyColor = new Color(0.3f, 0.3f, 0.32f), haoriColor = new Color(0.55f, 0.55f, 0.6f), hairColor = new Color(0.9f, 0.9f, 0.92f), bladeColor = new Color(0.8f, 0.8f, 0.85f) });
            Npcs.Add(new CharacterDefinition { id = "npc_villager", displayName = "Villager", npc = true,
                bodyColor = new Color(0.45f, 0.35f, 0.25f), haoriColor = new Color(0.6f, 0.5f, 0.35f), hairColor = new Color(0.15f, 0.1f, 0.08f), bladeColor = new Color(0.4f, 0.3f, 0.2f) });
            Npcs.Add(new CharacterDefinition { id = "npc_villager2", displayName = "Villager", npc = true,
                bodyColor = new Color(0.3f, 0.35f, 0.45f), haoriColor = new Color(0.7f, 0.4f, 0.4f), hairColor = new Color(0.1f, 0.08f, 0.06f), bladeColor = new Color(0.4f, 0.3f, 0.2f) });
            Npcs.Add(new CharacterDefinition { id = "npc_soldier", displayName = "Royal Guard", npc = true, element = Element.Water,
                bodyColor = new Color(0.25f, 0.28f, 0.35f), haoriColor = new Color(0.2f, 0.3f, 0.65f), hairColor = new Color(0.12f, 0.1f, 0.08f), bladeColor = new Color(0.8f, 0.8f, 0.85f) });
            Npcs.Add(new CharacterDefinition { id = "npc_chancellor", displayName = "Chancellor Mikado", npc = true,
                bodyColor = new Color(0.15f, 0.1f, 0.2f), haoriColor = new Color(0.45f, 0.2f, 0.55f), hairColor = new Color(0.9f, 0.9f, 0.9f), bladeColor = new Color(0.5f, 0.2f, 0.6f) });
            Npcs.Add(new CharacterDefinition { id = "npc_merchant", displayName = "Merchant Oda", npc = true,
                bodyColor = new Color(0.4f, 0.3f, 0.2f), haoriColor = new Color(0.8f, 0.6f, 0.2f), hairColor = new Color(0.2f, 0.15f, 0.1f), bladeColor = new Color(0.5f, 0.4f, 0.3f) });
        }

        // ------------------------------------------------------------------ Enemies

        static void BuildEnemies()
        {
            Enemies.Add(new EnemyDefinition { id = "grunt", displayName = "Lesser Demon", archetype = EnemyArchetype.Normal, element = Element.Beast,
                baseHp = 2200, baseAtk = 160, baseDef = 100, moveSpeed = 3.2f, attackRange = 1.9f, attackCooldown = 2.2f, windup = 0.6f,
                description = "The eclipse's foot soldiers. Weak alone, dangerous in packs." });
            Enemies.Add(new EnemyDefinition { id = "runner", displayName = "Swift Demon", archetype = EnemyArchetype.Fast, element = Element.Thunder,
                baseHp = 1500, baseAtk = 135, baseDef = 80, moveSpeed = 5.8f, attackRange = 3.2f, attackCooldown = 1.8f, windup = 0.4f,
                scale = 0.85f, bodyColor = new Color(0.3f, 0.25f, 0.08f), accentColor = new Color(1f, 0.85f, 0.2f), description = "Lunges fast and dodges your swings." });
            Enemies.Add(new EnemyDefinition { id = "brute", displayName = "Armored Demon", archetype = EnemyArchetype.Tank, element = Element.Flame,
                baseHp = 6500, baseAtk = 250, baseDef = 400, moveSpeed = 2.1f, attackRange = 2.6f, attackCooldown = 3f, windup = 0.95f,
                poise = 6f, scale = 1.5f, radius = 0.9f, bodyColor = new Color(0.35f, 0.15f, 0.05f), accentColor = new Color(1f, 0.45f, 0.1f), description = "Slow, armoured, hits like a landslide." });
            Enemies.Add(new EnemyDefinition { id = "spitter", displayName = "Blood-Spitter", archetype = EnemyArchetype.Ranged, element = Element.Water,
                baseHp = 1800, baseAtk = 170, baseDef = 90, moveSpeed = 2.8f, attackRange = 10f, attackCooldown = 2.8f, windup = 0.65f,
                bodyColor = new Color(0.08f, 0.18f, 0.3f), accentColor = new Color(0.3f, 0.7f, 1f), description = "Keeps its distance and spits blood orbs." });
            Enemies.Add(new EnemyDefinition { id = "elite", displayName = "Crescent Hunter", archetype = EnemyArchetype.Elite, element = Element.Dark,
                baseHp = 9000, baseAtk = 290, baseDef = 300, moveSpeed = 3.8f, attackRange = 2.6f, attackCooldown = 2.4f, windup = 0.7f,
                poise = 10f, scale = 1.25f, radius = 0.75f, bodyColor = new Color(0.2f, 0.05f, 0.25f), accentColor = new Color(0.85f, 0.2f, 1f), description = "Elite hunter. Leaps across the field." });
            Enemies.Add(new EnemyDefinition { id = "shadow_demon", displayName = "Shadow Demon", archetype = EnemyArchetype.Normal, element = Element.Dark,
                baseHp = 2600, baseAtk = 175, baseDef = 110, moveSpeed = 3.6f, attackRange = 2f, attackCooldown = 2f, windup = 0.55f,
                bodyColor = new Color(0.08f, 0.06f, 0.12f), accentColor = new Color(0.5f, 0.3f, 0.9f), description = "Born from the forest's shadows." });
            Enemies.Add(new EnemyDefinition { id = "forest_beast", displayName = "Forest Beast", archetype = EnemyArchetype.Tank, element = Element.Beast,
                baseHp = 7000, baseAtk = 240, baseDef = 330, moveSpeed = 2.6f, attackRange = 2.6f, attackCooldown = 2.8f, windup = 0.85f,
                poise = 6f, scale = 1.6f, radius = 0.95f, bodyColor = new Color(0.22f, 0.28f, 0.12f), accentColor = new Color(0.6f, 0.9f, 0.3f), description = "A corrupted guardian of the woods." });
            Enemies.Add(new EnemyDefinition { id = "demon_warrior", displayName = "Demon Warrior", archetype = EnemyArchetype.Elite, element = Element.Flame,
                baseHp = 8000, baseAtk = 280, baseDef = 280, moveSpeed = 3.6f, attackRange = 2.6f, attackCooldown = 2.3f, windup = 0.65f,
                poise = 8f, scale = 1.2f, radius = 0.7f, bodyColor = new Color(0.35f, 0.08f, 0.06f), accentColor = new Color(1f, 0.3f, 0.15f), description = "A demon trained in human swordplay." });
            Enemies.Add(new EnemyDefinition { id = "frost_oni", displayName = "Frost Oni", archetype = EnemyArchetype.Tank, element = Element.Water,
                baseHp = 7500, baseAtk = 260, baseDef = 380, moveSpeed = 2.3f, attackRange = 2.7f, attackCooldown = 2.8f, windup = 0.9f,
                poise = 7f, scale = 1.55f, radius = 0.9f, bodyColor = new Color(0.55f, 0.7f, 0.85f), accentColor = new Color(0.8f, 0.95f, 1f), description = "Its breath freezes blood." });
            Enemies.Add(new EnemyDefinition { id = "ice_wraith", displayName = "Ice Wraith", archetype = EnemyArchetype.Ranged, element = Element.Water,
                baseHp = 2000, baseAtk = 190, baseDef = 90, moveSpeed = 3f, attackRange = 10f, attackCooldown = 2.5f, windup = 0.6f,
                bodyColor = new Color(0.6f, 0.75f, 0.9f), accentColor = new Color(0.7f, 0.9f, 1f), description = "Hurls shards of ice from afar." });
            Enemies.Add(new EnemyDefinition { id = "lava_imp", displayName = "Lava Imp", archetype = EnemyArchetype.Fast, element = Element.Flame,
                baseHp = 1900, baseAtk = 170, baseDef = 90, moveSpeed = 6f, attackRange = 3.2f, attackCooldown = 1.7f, windup = 0.38f,
                scale = 0.8f, bodyColor = new Color(0.3f, 0.08f, 0.02f), accentColor = new Color(1f, 0.5f, 0.1f), description = "Fast, burning, and always in groups." });
            Enemies.Add(new EnemyDefinition { id = "corrupted_knight", displayName = "Corrupted Knight", archetype = EnemyArchetype.Elite, element = Element.Dark,
                baseHp = 11000, baseAtk = 320, baseDef = 380, moveSpeed = 3.4f, attackRange = 2.8f, attackCooldown = 2.4f, windup = 0.7f,
                poise = 12f, scale = 1.35f, radius = 0.8f, bodyColor = new Color(0.12f, 0.12f, 0.16f), accentColor = new Color(0.7f, 0.2f, 0.9f), description = "A royal knight swallowed by the eclipse." });
            Enemies.Add(new EnemyDefinition { id = "castle_sentinel", displayName = "Eclipse Sentinel", archetype = EnemyArchetype.Tank, element = Element.Dark,
                baseHp = 12000, baseAtk = 300, baseDef = 480, moveSpeed = 2.2f, attackRange = 3f, attackCooldown = 3f, windup = 0.9f,
                poise = 12f, scale = 1.8f, radius = 1f, bodyColor = new Color(0.1f, 0.08f, 0.12f), accentColor = new Color(0.9f, 0.1f, 0.3f), description = "Guards the Demon Lord's halls." });
            Enemies.Add(new EnemyDefinition { id = "dummy", displayName = "Training Dummy", archetype = EnemyArchetype.Normal, element = Element.Beast,
                baseHp = 30000, baseAtk = 1, baseDef = 50, moveSpeed = 0.8f, attackRange = 1.5f, attackCooldown = 99f, windup = 1f,
                bodyColor = new Color(0.6f, 0.5f, 0.35f), accentColor = new Color(0.8f, 0.7f, 0.5f), description = "Hit it. It doesn't mind." });

            // ---- Bosses ----
            Boss("boss_gorvath", "Gorvath", "The Horned Butcher of Kiriha", "thousandarm", Element.Beast, 22000, 230, 240, 2.4f, 2f,
                new Color(0.35f, 0.18f, 0.12f), new Color(1f, 0.5f, 0.2f), new[] { 0.5f }, "The first of the Demon Lord's servants to reach Kiriha.");
            Boss("boss_thousandarm", "The Ancient Guardian", "Warden of the Shadow Forest", "thousandarm", Element.Beast, 34000, 260, 280, 2.1f, 2.2f,
                new Color(0.25f, 0.35f, 0.18f), new Color(0.8f, 1f, 0.4f), new[] { 0.5f }, "Roots and arms without number. Once it protected the forest.");
            Boss("boss_hyoga", "Hyoga", "The Frost Oni", "goken", Element.Water, 36000, 300, 330, 1.35f, 1.3f,
                new Color(0.6f, 0.75f, 0.9f), new Color(0.8f, 0.95f, 1f), new[] { 0.5f }, "It has guarded the mountain pass for a century.");
            Boss("boss_chancellor", "Chancellor Mikado", "The Masked Servant of the Eclipse", "thousandarm", Element.Dark, 45000, 300, 320, 1.6f, 1.6f,
                new Color(0.2f, 0.08f, 0.28f), new Color(0.85f, 0.3f, 1f), new[] { 0.5f }, "Advisor to the King for twenty years. Demon for much longer.");
            Boss("boss_goken", "Goken", "The Crimson Fist", "goken", Element.Flame, 60000, 360, 360, 1.45f, 1.2f,
                new Color(0.85f, 0.55f, 0.5f), new Color(0.35f, 0.6f, 1f), new[] { 0.7f, 0.3f }, "An Upper Crescent. He fights for the joy of it — and knows Ren's name.");
            Boss("boss_seal_guardian", "The Seal Guardian", "Keeper of the Forgotten Temple", "goken", Element.Light, 62000, 350, 400, 1.7f, 1.3f,
                new Color(0.8f, 0.75f, 0.55f), new Color(0.4f, 0.9f, 1f), new[] { 0.7f, 0.3f }, "A construct left by the hero Akatsuki to guard the truth.");
            Boss("boss_morgrath", "General Morgrath", "Blade of the Eclipse", "goken", Element.Dark, 80000, 400, 420, 1.6f, 1.2f,
                new Color(0.15f, 0.1f, 0.14f), new Color(1f, 0.15f, 0.3f), new[] { 0.7f, 0.3f }, "Led the fall of Solmere. Has never been wounded.");
            Boss("boss_vex", "General Vex", "The Thousand Chains", "thousandarm", Element.Thunder, 60000, 380, 380, 1.9f, 1.4f,
                new Color(0.25f, 0.22f, 0.1f), new Color(1f, 0.9f, 0.3f), new[] { 0.5f }, "The Demon Lord's jailer.");
            Boss("boss_nyx", "General Nyx", "The Silent Eclipse", "goken", Element.Water, 70000, 400, 400, 1.5f, 1.15f,
                new Color(0.1f, 0.15f, 0.3f), new Color(0.3f, 0.6f, 1f), new[] { 0.7f, 0.3f }, "Nobody has heard her speak. Nobody who survived.");
            Boss("boss_veyrath", "Veyrath", "The Demon Lord of the Eclipse", "demonlord", Element.Dark, 90000, 440, 450, 1.8f, 1.1f,
                new Color(0.08f, 0.04f, 0.1f), new Color(1f, 0.1f, 0.25f), new[] { 0.5f }, "Five hundred years ago he was a man. Half of his heart still is.");
        }

        static void Boss(string id, string name, string title, string style, Element el, float hp, float atk, float def, float scale, float cooldown,
            Color body, Color accent, float[] phases, string desc)
        {
            Enemies.Add(new EnemyDefinition
            {
                id = id, displayName = name, bossTitle = title, archetype = EnemyArchetype.Boss, element = el, baseHp = hp, baseAtk = atk, baseDef = def,
                moveSpeed = style == "goken" ? 4.2f : 2.8f, attackRange = style == "goken" ? 2.8f : 4f, attackCooldown = cooldown,
                windup = style == "goken" ? 0.6f : 0.8f, poise = 70f, scale = scale, radius = 0.6f * scale + 0.1f, bossStyle = style,
                phaseThresholds = phases, bodyColor = body, accentColor = accent, description = desc
            });
        }

        // ------------------------------------------------------------------ Equipment

        static void Eq(string id, string name, EquipSlot slot, int rarity, StatBlock b, StatBlock per, string desc)
        {
            Equipment.Add(new EquipmentDefinition { id = id, displayName = name, slot = slot, rarity = rarity, baseBonus = b, perLevel = per, description = desc });
        }

        static void BuildEquipment()
        {
            Eq("sword_steel", "Steel Blade", EquipSlot.Sword, 3, new StatBlock(0, 180, 0, 0.02f), new StatBlock(0, 22, 0), "Standard-issue slayer blade.");
            Eq("sword_crimson", "Crimson-Forged Blade", EquipSlot.Sword, 4, new StatBlock(0, 450, 0, 0.05f), new StatBlock(0, 40, 0, 0.002f), "ATK +450, CRIT +5%.");
            Eq("sword_tessai", "Tessai's Broken Blade", EquipSlot.Sword, 4, new StatBlock(200, 380, 60, 0.04f), new StatBlock(20, 36, 6, 0.002f), "Your master's sword. It remembers him.");
            Eq("sword_dawn", "Dawnsteel Blade", EquipSlot.Sword, 5, new StatBlock(0, 620, 0, 0.06f, 0.2f), new StatBlock(0, 55, 0, 0.002f, 0.01f), "Glows faintly at sunrise.");
            Eq("sword_akatsuki", "Akatsuki, the First Dawn", EquipSlot.Sword, 6, new StatBlock(400, 900, 100, 0.08f, 0.3f, 0f, 0.1f), new StatBlock(40, 70, 10, 0.002f, 0.01f), "The hero's blade from five hundred years ago.");
            Eq("haori_cotton", "Cotton Haori", EquipSlot.Haori, 3, new StatBlock(400, 0, 90), new StatBlock(60, 0, 12), "Simple but sturdy.");
            Eq("haori_wisteria", "Wisteria Haori", EquipSlot.Haori, 4, new StatBlock(900, 0, 200), new StatBlock(110, 0, 22), "Scented with wisteria. Demons hate it.");
            Eq("haori_royal", "Royal Guard Mantle", EquipSlot.Haori, 5, new StatBlock(1500, 60, 320), new StatBlock(160, 6, 30), "Issued to Solmere's elite.");
            Eq("acc_charm", "Protective Charm", EquipSlot.Accessory, 3, new StatBlock(200, 40, 40), new StatBlock(25, 5, 5), "A charm from home.");
            Eq("acc_mask", "Fox Mask", EquipSlot.Accessory, 4, new StatBlock(0, 80, 0, 0.05f, 0.15f, 0.3f), new StatBlock(0, 10, 0, 0.002f, 0.01f), "CRIT +5%, CRIT DMG +15%, SPD +0.3.");
            Eq("acc_earrings", "Hanafuda Earrings", EquipSlot.Accessory, 5, new StatBlock(300, 120, 60, 0.03f, 0f, 0f, 0.15f), new StatBlock(30, 12, 6, 0f, 0f, 0f, 0.01f), "Special damage +15%.");
        }

        // ------------------------------------------------------------------ World regions

        static ArenaTheme Theme(EnvironmentKind kind)
        {
            var t = new ArenaTheme { kind = kind };
            switch (kind)
            {
                case EnvironmentKind.Village:
                    t.ground = new Color(0.22f, 0.2f, 0.16f); t.groundAccent = new Color(0.4f, 0.3f, 0.22f);
                    t.sky = new Color(0.16f, 0.05f, 0.08f); t.fog = new Color(0.3f, 0.1f, 0.1f);
                    t.lantern = new Color(1f, 0.55f, 0.25f); t.foliage = new Color(0.35f, 0.5f, 0.25f); t.petals = false;
                    t.sun = new Color(1f, 0.55f, 0.45f); t.sunIntensity = 0.9f; t.burning = true;
                    break;
                case EnvironmentKind.Forest:
                    t.ground = new Color(0.1f, 0.14f, 0.1f); t.groundAccent = new Color(0.18f, 0.25f, 0.15f);
                    t.sky = new Color(0.03f, 0.06f, 0.05f); t.fog = new Color(0.08f, 0.15f, 0.12f); t.fogStart = 12f; t.fogEnd = 45f;
                    t.lantern = new Color(0.5f, 1f, 0.7f); t.foliage = new Color(0.12f, 0.3f, 0.18f); t.petals = false;
                    t.sun = new Color(0.55f, 0.75f, 0.7f); t.sunIntensity = 0.7f;
                    break;
                case EnvironmentKind.Mountain:
                    t.ground = new Color(0.85f, 0.88f, 0.95f); t.groundAccent = new Color(0.65f, 0.7f, 0.8f);
                    t.sky = new Color(0.55f, 0.65f, 0.8f); t.fog = new Color(0.75f, 0.8f, 0.9f); t.fogStart = 20f; t.fogEnd = 70f;
                    t.lantern = new Color(1f, 0.8f, 0.5f); t.foliage = new Color(0.15f, 0.3f, 0.25f); t.petals = false; t.night = false;
                    t.sun = new Color(1f, 0.97f, 0.9f); t.sunIntensity = 1.25f;
                    break;
                case EnvironmentKind.Kingdom:
                    t.ground = new Color(0.55f, 0.52f, 0.48f); t.groundAccent = new Color(0.7f, 0.62f, 0.5f);
                    t.sky = new Color(0.45f, 0.6f, 0.85f); t.fog = new Color(0.7f, 0.75f, 0.85f); t.fogStart = 35f; t.fogEnd = 100f;
                    t.lantern = new Color(1f, 0.8f, 0.5f); t.foliage = new Color(0.3f, 0.55f, 0.3f); t.petals = true; t.night = false;
                    t.sun = new Color(1f, 0.95f, 0.85f); t.sunIntensity = 1.3f;
                    break;
                case EnvironmentKind.Temple:
                    t.ground = new Color(0.3f, 0.32f, 0.35f); t.groundAccent = new Color(0.25f, 0.5f, 0.6f);
                    t.sky = new Color(0.05f, 0.1f, 0.18f); t.fog = new Color(0.08f, 0.16f, 0.25f); t.fogStart = 18f; t.fogEnd = 60f;
                    t.lantern = new Color(0.4f, 0.9f, 1f); t.foliage = new Color(0.2f, 0.4f, 0.35f); t.petals = false;
                    t.sun = new Color(0.6f, 0.8f, 1f); t.sunIntensity = 0.85f;
                    break;
                case EnvironmentKind.DemonLand:
                    t.ground = new Color(0.14f, 0.07f, 0.06f); t.groundAccent = new Color(0.45f, 0.1f, 0.05f);
                    t.sky = new Color(0.2f, 0.03f, 0.02f); t.fog = new Color(0.3f, 0.07f, 0.04f); t.fogStart = 15f; t.fogEnd = 55f;
                    t.lantern = new Color(1f, 0.35f, 0.1f); t.foliage = new Color(0.2f, 0.05f, 0.05f); t.petals = false;
                    t.sun = new Color(1f, 0.45f, 0.3f); t.sunIntensity = 0.9f;
                    break;
                case EnvironmentKind.FallenCity:
                    t.ground = new Color(0.25f, 0.2f, 0.18f); t.groundAccent = new Color(0.45f, 0.25f, 0.15f);
                    t.sky = new Color(0.3f, 0.12f, 0.05f); t.fog = new Color(0.35f, 0.18f, 0.1f); t.fogStart = 18f; t.fogEnd = 60f;
                    t.lantern = new Color(1f, 0.5f, 0.15f); t.foliage = new Color(0.25f, 0.2f, 0.15f); t.petals = false;
                    t.sun = new Color(1f, 0.6f, 0.35f); t.sunIntensity = 1f;
                    break;
                default:
                    t.ground = new Color(0.1f, 0.08f, 0.12f); t.groundAccent = new Color(0.35f, 0.08f, 0.2f);
                    t.sky = new Color(0.06f, 0.02f, 0.08f); t.fog = new Color(0.12f, 0.04f, 0.14f); t.fogStart = 16f; t.fogEnd = 55f;
                    t.lantern = new Color(0.8f, 0.2f, 1f); t.foliage = new Color(0.2f, 0.05f, 0.2f); t.petals = false;
                    t.sun = new Color(0.8f, 0.5f, 1f); t.sunIntensity = 0.8f;
                    break;
            }
            return t;
        }

        static void Region(string id, string name, string sub, float x, float y, EnvironmentKind kind, Color mapColor, string unlockAfter)
        {
            Regions.Add(new RegionDefinition { id = id, name = name, subtitle = sub, mapPosition = new Vector2(x, y), theme = Theme(kind), mapColor = mapColor, unlockAfterMission = unlockAfter });
        }

        static void BuildRegions()
        {
            Region("village", "Kiriha Village", "Home. Mountains, forests, and a red sky.", -42f, -16f, EnvironmentKind.Village, new Color(0.45f, 0.6f, 0.3f), null);
            Region("forest", "Forest of Shadows", "Fog, giant trees, and things that watch.", -27f, -2f, EnvironmentKind.Forest, new Color(0.12f, 0.35f, 0.2f), "1-3");
            Region("mountain", "Hakuro Pass", "Snow, cliffs, and an old oni.", -12f, 14f, EnvironmentKind.Mountain, new Color(0.85f, 0.9f, 0.95f), "2-3");
            Region("kingdom", "Royal Capital Solmere", "Markets, guards, and an arena.", 5f, 5f, EnvironmentKind.Kingdom, new Color(0.8f, 0.7f, 0.45f), "3-2");
            Region("demonland", "The Ashen Wastes", "Demon territory. The land itself burns.", 21f, -12f, EnvironmentKind.DemonLand, new Color(0.55f, 0.12f, 0.08f), "3-5");
            Region("temple", "Forgotten Temple", "Where the hero Akatsuki left the truth.", 36f, 10f, EnvironmentKind.Temple, new Color(0.3f, 0.6f, 0.7f), "4-3");
            Region("fallen", "Fallen Solmere", "The capital burns.", 11f, -6f, EnvironmentKind.FallenCity, new Color(0.7f, 0.35f, 0.15f), "5-3");
            Region("castle", "Castle of the Eclipse", "The Demon Lord waits.", 42f, -24f, EnvironmentKind.Castle, new Color(0.3f, 0.08f, 0.35f), "6-3");
        }

        // ------------------------------------------------------------------ Missions

        static WaveDefinition Wave(params object[] pairs)
        {
            var w = new WaveDefinition();
            for (int i = 0; i + 1 < pairs.Length; i += 2) w.spawns.Add(new SpawnEntry((string)pairs[i], (int)pairs[i + 1]));
            return w;
        }

        static MissionDefinition M(ChapterDefinition ch, string id, MissionType type, string region, string name, int level, string story, params WaveDefinition[] waves)
        {
            var r = GetRegion(region);
            var m = new MissionDefinition
            {
                id = id, chapter = ch != null ? ch.number : 0, type = type, regionId = region, name = name, enemyLevel = level,
                recommendedLevel = level + 2, recommendedPower = 2000 + level * 700, storyText = story, description = story,
                theme = r != null ? r.theme : new ArenaTheme(), killObjective = 0, parTime = 150f + level * 2f, timeLimit = 360f
            };
            m.waves.AddRange(waves);
            int total = 0;
            foreach (var w in waves) total += w.TotalCount;
            m.killObjective = Mathf.Max(3, Mathf.RoundToInt(total * 0.8f));
            float scale = 1f + level * 0.12f;
            m.rewards = new RewardBundle
            {
                exp = Mathf.RoundToInt(500 * scale), coins = Mathf.RoundToInt(2500 * scale), expScrolls = 1 + level / 6,
                skillScrolls = 1 + level / 8, ascensionOre = level / 8
            };
            m.firstClearRewards = new RewardBundle { crystals = type == MissionType.Boss ? 100 : type == MissionType.Side ? 40 : 30 };
            m.dropTable.AddRange(level < 10 ? new[] { "sword_steel", "haori_cotton", "acc_charm" } :
                level < 25 ? new[] { "sword_crimson", "haori_wisteria", "acc_mask" } : new[] { "sword_dawn", "haori_royal", "acc_earrings" });
            if (ch != null) ch.missions.Add(m);
            return m;
        }

        static void Chain(ChapterDefinition ch)
        {
            // Story/boss missions unlock sequentially; side missions unlock with the mission before them.
            MissionDefinition prevMain = null;
            foreach (var m in ch.missions)
            {
                if (m.requiresMissionId != null) { if (m.type == MissionType.Story || m.type == MissionType.Boss) prevMain = m; continue; }
                if (prevMain != null) m.requiresMissionId = prevMain.id;
                if (m.type == MissionType.Story || m.type == MissionType.Boss) prevMain = m;
            }
        }

        static void BuildMissions()
        {
            // ---------------- Chapter 1
            var ch1 = new ChapterDefinition { number = 1, title = "The Fallen Village", regionId = "village", available = true,
                synopsis = "The night the sky turned red, Kiriha burned. Ren's training was over. His war had begun." };
            var m = M(ch1, "1-1", MissionType.Story, "village", "Night of the Red Sky", 1, "Demons pour out of the forest. Protect the village gate.",
                Wave("grunt", 3), Wave("grunt", 4));
            m.parTime = 120f;
            M(ch1, "1-2", MissionType.Story, "village", "Burning Streets", 2, "The market square is on fire. Find Master Tessai.",
                Wave("grunt", 4, "runner", 2), Wave("grunt", 3, "runner", 3));
            m = M(ch1, "1-S", MissionType.Side, "village", "Rescue the Villagers", 3, "Families are trapped near the shrine. Clear a path out.",
                Wave("runner", 4), Wave("runner", 3, "grunt", 3));
            m.questGiver = "Old Farmer Joji";
            m.allies = 2;
            m.firstClearRewards.equipmentIds.Add("acc_mask");
            m = M(ch1, "1-3", MissionType.Boss, "village", "Gorvath the Horned", 4, "The demon that led the attack waits at the shrine. So does your master.",
                Wave("grunt", 4)); m.bossId = "boss_gorvath"; m.cutsceneBefore = "c1_boss"; m.cutsceneAfter = "c1_end";
            m.firstClearRewards.equipmentIds.Add("sword_tessai");
            Chain(ch1);

            // ---------------- Chapter 2
            var ch2 = new ChapterDefinition { number = 2, title = "The Forest of Shadows", regionId = "forest", available = true,
                synopsis = "Something in the forest remembers the last time the sky turned red." };
            m = M(ch2, "2-1", MissionType.Story, "forest", "Into the Shadows", 6, "The only road to the capital runs through the Forest of Shadows.",
                Wave("shadow_demon", 4), Wave("shadow_demon", 3, "spitter", 2));
            m.cutsceneBefore = "c2_forest";
            m = M(ch2, "2-2", MissionType.Story, "forest", "The Frightened Swordsman", 7, "Someone is screaming deeper in the woods.",
                Wave("shadow_demon", 4, "runner", 2), Wave("forest_beast", 1, "shadow_demon", 3));
            m.cutsceneAfter = "c2_sora"; m.firstClearRewards.characterId = "sora_initiate";
            m = M(ch2, "2-S", MissionType.Side, "forest", "Hunt: The Great Forest Beast", 8, "A hunter's guild bounty on a beast the size of a house.",
                Wave("forest_beast", 2), Wave("forest_beast", 2, "shadow_demon", 3));
            m.questGiver = "Hunter Rokuro";
            m = M(ch2, "2-T", MissionType.Treasure, "forest", "The Lost Blade", 8, "Legends say a slayer's blade lies in the hollow tree.",
                Wave("shadow_demon", 5), Wave("spitter", 3, "runner", 3));
            m.firstClearRewards.equipmentIds.Add("sword_crimson"); m.firstClearRewards.crystals += 30;
            m = M(ch2, "2-3", MissionType.Boss, "forest", "The Ancient Guardian", 10, "Something ancient has awakened beneath the forest.",
                Wave("shadow_demon", 4, "forest_beast", 1)); m.bossId = "boss_thousandarm"; m.cutsceneAfter = "c2_end";
            Chain(ch2);

            // ---------------- Chapter 3
            var ch3 = new ChapterDefinition { number = 3, title = "The Kingdom", regionId = "kingdom", available = true,
                synopsis = "Solmere is the last great city. It has walls, an army, and a king. It also has a traitor." };
            m = M(ch3, "3-1", MissionType.Story, "mountain", "The Frozen Pass", 12, "The only way to Solmere is over Hakuro Pass.",
                Wave("frost_oni", 1, "ice_wraith", 3), Wave("frost_oni", 2, "ice_wraith", 2));
            m.cutsceneBefore = "c3_mountain";
            m = M(ch3, "3-2", MissionType.Boss, "mountain", "Hyoga the Frost Oni", 13, "The oni of the pass does not let anyone through.",
                Wave("ice_wraith", 4)); m.bossId = "boss_hyoga";
            m = M(ch3, "3-3", MissionType.Story, "kingdom", "The Arena of Solmere", 14, "No one enters the capital's guard without winning in the arena.",
                Wave("grunt", 6), Wave("demon_warrior", 1, "runner", 4), Wave("brute", 2, "spitter", 2));
            m.cutsceneBefore = "c3_kingdom"; m.cutsceneAfter = "c3_kiba"; m.firstClearRewards.characterId = "kiba_initiate";
            m = M(ch3, "3-S", MissionType.Side, "kingdom", "Protect the Merchant", 14, "A merchant's caravan is being hunted on the outer road.",
                Wave("runner", 5), Wave("spitter", 3, "grunt", 3)); m.questGiver = "Merchant Oda"; m.allies = 2;
            m.firstClearRewards.equipmentIds.Add("haori_wisteria");
            m = M(ch3, "3-4", MissionType.Story, "kingdom", "Shadows in the Capital", 15, "Demons are appearing inside the walls. Someone is letting them in.",
                Wave("shadow_demon", 5, "spitter", 2), Wave("demon_warrior", 2, "shadow_demon", 3));
            m.cutsceneAfter = "c3_hana"; m.firstClearRewards.characterId = "hana_healer";
            m = M(ch3, "3-5", MissionType.Boss, "kingdom", "The Masked Chancellor", 16, "The King's own advisor. The mask comes off.",
                Wave("demon_warrior", 2, "shadow_demon", 3)); m.bossId = "boss_chancellor"; m.cutsceneBefore = "c3_betrayal"; m.cutsceneAfter = "c3_end";
            m.firstClearRewards.characterId = "tetsu_guard";
            Chain(ch3);

            // ---------------- Chapter 4
            var ch4 = new ChapterDefinition { number = 4, title = "The Demon Territory", regionId = "demonland", available = true,
                synopsis = "To find the Demon Lord's weakness, Ren must walk where no human has returned from." };
            m = M(ch4, "4-1", MissionType.Story, "demonland", "The Ashen Wastes", 19, "The land burns. A Pillar waits at the border.",
                Wave("lava_imp", 5), Wave("lava_imp", 3, "demon_warrior", 2));
            m.cutsceneBefore = "c4_wastes"; m.firstClearRewards.characterId = "homura_pillar";
            M(ch4, "4-2", MissionType.Story, "demonland", "Elite Hunters", 21, "The Demon Lord's hunters have your scent.",
                Wave("corrupted_knight", 1, "lava_imp", 4), Wave("elite", 2, "demon_warrior", 1));
            m = M(ch4, "4-S", MissionType.Side, "demonland", "Hunt: The Crescent Pack", 22, "Three Crescent Hunters. A hunter's guild bounty that no one has survived.",
                Wave("elite", 3)); m.questGiver = "Hunter Rokuro"; m.firstClearRewards.equipmentIds.Add("acc_earrings");
            m = M(ch4, "4-3", MissionType.Boss, "demonland", "Goken, the Crimson Fist", 24, "An Upper Crescent blocks the road. He's been waiting for you specifically.",
                Wave("elite", 1, "lava_imp", 4)); m.bossId = "boss_goken"; m.cutsceneAfter = "c4_end";
            Chain(ch4);

            // ---------------- Chapter 5
            var ch5 = new ChapterDefinition { number = 5, title = "The Forgotten Temple", regionId = "temple", available = true,
                synopsis = "Why does the Demon Lord know Ren's name? The hero Akatsuki left the answer behind." };
            m = M(ch5, "5-1", MissionType.Story, "temple", "Echoes of the Past", 26, "Ancient seals guard the temple doors. Strike them in the right order.",
                Wave("shadow_demon", 4, "spitter", 3));
            m.sealPuzzle = true; m.cutsceneBefore = "c5_temple";
            M(ch5, "5-2", MissionType.Story, "temple", "The Guardian's Trial", 27, "The temple tests everyone who seeks the truth.",
                Wave("corrupted_knight", 1, "shadow_demon", 4), Wave("castle_sentinel", 1, "spitter", 3));
            m = M(ch5, "5-T", MissionType.Treasure, "temple", "The Hero's Reliquary", 27, "Akatsuki's personal vault lies somewhere below.",
                Wave("shadow_demon", 6), Wave("corrupted_knight", 2)); m.sealPuzzle = true; m.firstClearRewards.equipmentIds.Add("sword_dawn");
            m = M(ch5, "5-3", MissionType.Boss, "temple", "The Seal Guardian", 28, "The last guardian of the truth.",
                Wave("corrupted_knight", 1, "shadow_demon", 3)); m.bossId = "boss_seal_guardian"; m.cutsceneAfter = "c5_truth";
            m.firstClearRewards.characterId = "ren_sundance";
            Chain(ch5);

            // ---------------- Chapter 6
            var ch6 = new ChapterDefinition { number = 6, title = "The Fallen Kingdom", regionId = "fallen", available = true,
                synopsis = "While Ren sought the truth, the Demon Lord's army marched on Solmere." };
            m = M(ch6, "6-1", MissionType.Story, "fallen", "The Siege of Solmere", 31, "The outer walls have fallen. Hold the main street with the royal guard.",
                Wave("lava_imp", 6, "grunt", 4), Wave("demon_warrior", 3, "spitter", 3), Wave("corrupted_knight", 2, "lava_imp", 4));
            m.allies = 6; m.cutsceneBefore = "c6_siege";
            m = M(ch6, "6-2", MissionType.Story, "fallen", "Hold the Gate", 32, "The civilians need time to escape. Someone has to hold the gate.",
                Wave("castle_sentinel", 1, "demon_warrior", 3), Wave("elite", 2, "lava_imp", 5));
            m.allies = 4; m.cutsceneAfter = "c6_sacrifice";
            m = M(ch6, "6-S", MissionType.Side, "fallen", "Rescue the Survivors", 32, "Families are trapped in the burning market.",
                Wave("lava_imp", 6), Wave("shadow_demon", 5)); m.questGiver = "Captain Tetsu"; m.allies = 3;
            m = M(ch6, "6-3", MissionType.Boss, "fallen", "General Morgrath", 34, "The general who burned Solmere stands in the throne room.",
                Wave("corrupted_knight", 2)); m.bossId = "boss_morgrath"; m.cutsceneAfter = "c6_end";
            m.firstClearRewards.equipmentIds.Add("haori_royal");
            Chain(ch6);

            // ---------------- Final chapter
            var ch7 = new ChapterDefinition { number = 7, title = "The Demon Lord", regionId = "castle", available = true,
                synopsis = "Everything ends where it began: with an eclipse, and two halves of one heart." };
            m = M(ch7, "7-1", MissionType.Story, "castle", "Gates of the Eclipse", 36, "The castle gates open for you. That's not a good sign.",
                Wave("castle_sentinel", 1, "shadow_demon", 5), Wave("corrupted_knight", 2, "lava_imp", 4));
            m.cutsceneBefore = "c7_castle";
            M(ch7, "7-2", MissionType.Story, "castle", "Hall of Elites", 37, "The Demon Lord's finest wait in the great hall.",
                Wave("elite", 2, "corrupted_knight", 1), Wave("castle_sentinel", 2, "elite", 1), Wave("demon_warrior", 4, "corrupted_knight", 2));
            m = M(ch7, "7-3", MissionType.Boss, "castle", "The Twin Generals", 38, "General Vex and General Nyx guard the final stair.",
                Wave("corrupted_knight", 2)); m.preBosses.Add("boss_vex"); m.bossId = "boss_nyx"; m.cutsceneAfter = "c7_truth";
            m = M(ch7, "7-4", MissionType.Boss, "castle", "Veyrath, the Demon Lord", 40, "The Demon Lord of the Eclipse. The other half of your heart.",
                Wave("castle_sentinel", 1, "elite", 1)); m.bossId = "boss_veyrath"; m.cutsceneBefore = "c7_final"; m.cutsceneAfter = "ending";
            m.timeLimit = 480f; m.parTime = 300f; m.firstClearRewards.characterId = "homura_lastflame"; m.firstClearRewards.equipmentIds.Add("sword_akatsuki");
            m.firstClearRewards.crystals = 500;
            Chain(ch7);

            Chapters.Add(ch1); Chapters.Add(ch2); Chapters.Add(ch3); Chapters.Add(ch4); Chapters.Add(ch5); Chapters.Add(ch6); Chapters.Add(ch7);
            // Each chapter opens when the previous chapter's final boss falls.
            for (int i = 1; i < Chapters.Count; i++)
            {
                var prev = Chapters[i - 1].missions;
                var first = Chapters[i].missions[0];
                if (first.requiresMissionId == null) first.requiresMissionId = prev[prev.Count - 1].id;
            }

            // ---------------- Event & training
            m = M(null, "EV-1", MissionType.Event, "kingdom", "Crimson Moon Boss Rush", 30, "EVENT: Face the Demon Lord's servants back to back.",
                Wave("elite", 2));
            m.preBosses.Add("boss_gorvath"); m.preBosses.Add("boss_chancellor"); m.bossId = "boss_goken"; m.timeLimit = 600f;
            m.firstClearRewards.crystals = 300; m.rewards.crystals = 20; m.requiresMissionId = "3-5";
            ExtraMissions.Add(m);
            m = M(null, "TR", MissionType.Training, "kingdom", "Training Grounds", 1, "Test every skill on training dummies. No rewards, no pressure.",
                Wave("dummy", 3)); m.training = true; m.timeLimit = 9999f; m.rewards = new RewardBundle(); m.firstClearRewards = new RewardBundle();
            ExtraMissions.Add(m);
        }
    }
}
