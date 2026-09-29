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
            NpcLooks();
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
            // Every slayer can be summoned (story allies too — a story copy you already own becomes a duplicate).
            return Characters.FindAll(c => !c.npc && !c.designTest && c.rarity == rarity);
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
                // Rarity tiers are 2★ Common .. 6★ Mythic (the data table lists them one step higher).
                // Slayers go by their first name only.
                id = id, baseId = baseId, displayName = name.Split(' ')[0], versionTitle = title, breathingStyle = style, rarity = rarity - 1,
                element = el, role = role, description = desc, story = story, baseStats = BaseStats(rarity, role),
                bodyColor = body, haoriColor = haori, hairColor = hair, bladeColor = blade
            };
        }

        static void BuildCharacters()
        {
            var ren = Hero("ren_initiate", "ren", "Ren Kagami", "Kiriha Swordsman", "Tide Breathing", 4, Element.Water, Role.DPS,
                "The last swordsman of Kiriha. Kind, stubborn, and carrying a light he doesn't understand.",
                "Raised by Master Tessai after his parents vanished on the night of a crimson moon. Ren trained every day for a battle he hoped would never come. It came.",
                new Color(0.1f, 0.1f, 0.12f), new Color(0.08f, 0.36f, 0.4f), new Color(0.07f, 0.05f, 0.06f), new Color(0.2f, 0.4f, 0.95f));
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
                new Color(0.1f, 0.1f, 0.13f), new Color(0.45f, 0.47f, 0.52f), new Color(0.58f, 0.6f, 0.64f), new Color(1f, 0.85f, 0.3f));
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
                new Color(0.32f, 0.22f, 0.14f), new Color(0.55f, 0.55f, 0.55f), new Color(0.1f, 0.12f, 0.3f), new Color(0.78f, 0.56f, 0.3f));
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
                new Color(0.96f, 0.94f, 0.98f), new Color(0.76f, 0.66f, 0.9f), new Color(0.82f, 0.78f, 0.92f), new Color(0.9f, 0.72f, 1f));
            hana.storyOnly = true;
            hana.skills = new[]
            {
                Ab("Healing Mist", AbilityShape.Heal, 12f, 0.22f, 1, 0f, 6f, "Heals the whole team for 22% of max HP."),
                Ab("Petal Storm", AbilityShape.MultiSlash, 8f, 0.8f, 5, 0f, 4f, "A storm of razor-sharp petals."),
                Ab("Moonlit Ward", AbilityShape.Burst, 10f, 1.6f, 2, 0f, 5f, "A ring of moonlight that repels demons.")
            };
            hana.ultimate = Ab("Garden of Rebirth", AbilityShape.Heal, 0f, 0.5f, 1, 0f, 9f, "Restores 50% HP to every slayer and blasts nearby demons.", 15f, 6f);
            Characters.Add(hana);

            var tetsu = Hero("tetsu_guard", "tetsu", "Tetsu Ganryu", "Iron Captain", "Stone Breathing", 5, Element.Earth, Role.Tank,
                "Captain of the Solmere guard. Wields a greatsword like a wall.",
                "Tetsu served the Chancellor for twenty years. Learning what the Chancellor really was broke something in him — and hardened the rest.",
                new Color(0.35f, 0.42f, 0.55f), new Color(0.12f, 0.16f, 0.36f), new Color(0.26f, 0.17f, 0.1f), new Color(0.78f, 0.78f, 0.82f));
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
                new Color(0.08f, 0.07f, 0.07f), new Color(0.55f, 0.07f, 0.1f), new Color(0.78f, 0.12f, 0.1f), new Color(1f, 0.45f, 0.1f));
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
                new Color(0.08f, 0.06f, 0.06f), new Color(0.7f, 0.12f, 0.08f), new Color(0.85f, 0.2f, 0.08f), new Color(1f, 0.6f, 0.1f));
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
                new Color(0.1f, 0.1f, 0.12f), new Color(0.95f, 0.55f, 0.18f), new Color(0.07f, 0.05f, 0.06f), new Color(1f, 0.78f, 0.3f));
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
                Ab("Crimson Eclipse", AbilityShape.MultiSlash, 9f, 0.9f, 8, 0f, 5f, "Eight slashes in darkness.")
            };
            kuroe.ultimate = Ab("Moon Breathing: Final Night", AbilityShape.MultiSlash, 0f, 1.6f, 14, 0f, 10f, "Night falls. Nothing survives it.", 30f, 7f);
            Characters.Add(kuroe);

            // Limited-banner Mythics.
            var seren = Hero("seren_starfall", "seren", "Seren Hoshimiya", "Starfall Oracle", "Star Breathing", 7, Element.Light, Role.Burst,
                "A shrine oracle who reads the stars — and can pull them down.", "Seren saw the Demon Lord's return in the night sky years ago. Nobody believed her. She has been getting ready ever since.",
                new Color(0.95f, 0.93f, 1f), new Color(0.3f, 0.3f, 0.75f), new Color(0.85f, 0.88f, 1f), new Color(1f, 0.92f, 0.55f));
            seren.skills = new[]
            {
                Ab("Star Needles", AbilityShape.Wave, 5f, 1.5f, 3, 10f, 1.6f, "Three needles of starlight."),
                Ab("Orbiting Stars", AbilityShape.Spin, 7f, 1.2f, 4, 0f, 4.2f, "Stars circle her, cutting all around."),
                Ab("Comet Lance", AbilityShape.Dash, 9f, 3f, 1, 10f, 2.2f, "She becomes a comet for a heartbeat.")
            };
            seren.ultimate = Ab("Constellation: Final Verdict", AbilityShape.Burst, 0f, 2.4f, 6, 0f, 10f, "Stars mark every demon, then fall together.", 28f, 7f);
            Characters.Add(seren);

            var garou = Hero("garou_onyx", "garou", "Garou Kurogane", "Onyx Tempest", "Gale Fang Breathing", 7, Element.Beast, Role.DPS,
                "A hooded wanderer with twin cleavers and a storm at his back.", "Garou walked out of the northern wastes alone. The storm followed him, and the demons that chased him did not come back.",
                new Color(0.12f, 0.12f, 0.12f), new Color(0.15f, 0.35f, 0.3f), new Color(0.2f, 0.22f, 0.25f), new Color(0.55f, 1f, 0.8f));
            garou.comboMultipliers = new[] { 0.75f, 0.8f, 0.9f, 1f, 1.8f };
            garou.skills = new[]
            {
                Ab("Rending Gale", AbilityShape.MultiSlash, 5f, 0.9f, 6, 0f, 4f, "Six cleaver cuts on the wind."),
                Ab("Cyclone Maw", AbilityShape.Spin, 7f, 1.4f, 3, 0f, 4.4f, "A spinning storm of blades."),
                Ab("Onyx Charge", AbilityShape.Dash, 9f, 3.2f, 1, 9f, 2.4f, "Tears straight through the line.", 7f, 8f)
            };
            garou.ultimate = Ab("Black Tornado", AbilityShape.Burst, 0f, 2.6f, 6, 0f, 10f, "A tornado that swallows the battlefield.", 30f, 8f);
            Characters.Add(garou);
            BuildNewcomers();
            ApplyLooks();
            BuildMonsterfolk();
            BuildMonsterfolk2();
        }

        static void Monster(string id, string name, string title, string breathing, int rarity, Element el, Role role, Species sp, string desc, string story,
            AbilityDefinition[] skills, AbilityDefinition ult, HairStyle hair, WeaponKind weapon, CombatStyle style, MotionStyle motion,
            Color body, Color outfit, Color hairC, Color blade, Color skin, float h = 1f, float w = 1f, bool cape = false, bool pelt = false, bool armor = false, Color? accent = null, float atkSpeed = 1f)
        {
            var C = new Color(0.1f, 0.1f, 0.12f);
            var d = Hero(id, id.Split('_')[0], name, title, breathing, rarity, el, role, desc, story, C, C, C, C);
            d.species = sp;
            d.skills = skills;
            d.ultimate = ult;
            d.attackSpeed = atkSpeed;
            Characters.Add(d);
            Look(id, hair, weapon, style, motion, body, outfit, hairC, blade, h, w, false, cape, pelt, armor, false, accent);
            d.skinTone = skin;
        }

        /// <summary>More monster-folk: sixteen kinds, from a shambling zombie to a mythic soul reaper.</summary>
        static void BuildMonsterfolk2()
        {
            // The first six get a meaner, darker palette.
            Skin("snik_goblin", new Color(0.36f, 0.56f, 0.24f));
            Skin("brimm_demon", new Color(0.72f, 0.18f, 0.18f));
            Skin("howl_werewolf", new Color(0.38f, 0.34f, 0.36f));

            // ---- Common
            Monster("grub_zombie", "Grub", "Shambling Brawler", "Graveyard Breathing", 3, Element.Dark, Role.Tank, Species.Zombie,
                "A slow, stubborn zombie who just keeps getting back up.", "Grub doesn't remember much. He remembers that demons are bad, and that he is very, very hard to stop.",
                new[] { Ab("Grave Grab", AbilityShape.Dash, 6f, 1.6f, 2, 5f, 1.8f, "Lurches forward and grabs."), Ab("Rot Slam", AbilityShape.Burst, 8f, 1.5f, 1, 0f, 3.8f, "Slams both fists down.", 6f, 5f), Ab("Get Back Up", AbilityShape.Heal, 12f, 0.16f, 1, 0f, 5f, "Pulls himself together (heals the team a little).") },
                Ab("Horde Night", AbilityShape.Burst, 0f, 2f, 3, 0f, 7f, "Hands claw up from the ground around every demon nearby.", 18f, 5f),
                HairStyle.Messy, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Nervous,
                new Color(0.3f, 0.3f, 0.28f), new Color(0.36f, 0.3f, 0.4f), new Color(0.22f, 0.24f, 0.18f), new Color(0.6f, 0.9f, 0.3f), new Color(0.58f, 0.7f, 0.5f), 1.05f, 1.1f, false, false, false, null, 0.9f);
            Monster("kappi_kappa", "Kappi", "River Imp", "Riverbed Breathing", 3, Element.Water, Role.Support, Species.Kappa,
                "A mischievous river kappa who fights with webbed fists and splashes.", "Kappi challenges everyone he meets to a wrestling match. He usually wins. The demons always lose.",
                new[] { Ab("Splash Jab", AbilityShape.MultiSlash, 4f, 0.7f, 4, 0f, 3.2f, "Four slippery jabs."), Ab("Belly Slide", AbilityShape.Dash, 6f, 1.6f, 2, 8f, 1.8f, "Slides through on his shell."), Ab("Dish Refill", AbilityShape.Heal, 11f, 0.2f, 1, 0f, 5.5f, "Splashes cool river water over the team.") },
                Ab("River Rush", AbilityShape.Wave, 0f, 2.6f, 1, 16f, 3f, "Calls the river; a wave of water carries every demon away.", 18f, 8f),
                HairStyle.Short, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Light,
                new Color(0.3f, 0.35f, 0.25f), new Color(0.25f, 0.45f, 0.55f), new Color(0.22f, 0.3f, 0.2f), new Color(0.5f, 0.85f, 1f), new Color(0.46f, 0.68f, 0.44f), 0.9f, 1f);

            // ---- Rare
            Monster("brakka_orc", "Brakka", "Tusk Raider", "Warcry Breathing", 4, Element.Flame, Role.DPS, Species.Orc,
                "A tusked orc raider with two heavy cleavers and a battle cry that shakes trees.", "Brakka's clan was scattered by the eclipse. She fights for a new clan now: the slayers.",
                new[] { Ab("Cleave Frenzy", AbilityShape.MultiSlash, 5f, 0.9f, 5, 0f, 3.8f, "Five brutal cleaver chops."), Ab("War Charge", AbilityShape.Dash, 7f, 2.1f, 1, 8f, 2.2f, "Charges, cleavers first.", 6f, 7f), Ab("Warcry", AbilityShape.Burst, 9f, 1.4f, 1, 0f, 5f, "A roar that knocks demons back.", 8f, 8f) },
                Ab("Blood Moon Rampage", AbilityShape.MultiSlash, 0f, 1.1f, 10, 0f, 7.5f, "Goes berserk: a storm of cleaver blows around her.", 22f, 6f),
                HairStyle.Wild, WeaponKind.Cleavers, CombatStyle.Heavy, MotionStyle.Aggressive,
                new Color(0.3f, 0.22f, 0.16f), new Color(0.45f, 0.18f, 0.12f), new Color(0.12f, 0.1f, 0.08f), new Color(1f, 0.5f, 0.2f), new Color(0.42f, 0.58f, 0.3f), 1.1f, 1.2f, false, true);
            Monster("ssira_lizard", "Ssira", "Marsh Lancer", "Swamp Breathing", 4, Element.Water, Role.DPS, Species.Lizardfolk,
                "A lizardfolk lancer who strikes from the reeds like a snake.", "Ssira guards the marshes where the demons dump their cursed waters. She is not a forgiving guard.",
                new[] { Ab("Tail Sweep", AbilityShape.Spin, 5f, 1.1f, 3, 0f, 3.6f, "Spins, spear and tail sweeping."), Ab("Reed Strike", AbilityShape.Dash, 6f, 2.2f, 1, 9f, 1.6f, "Darts out of nowhere, spear first."), Ab("Swamp Mire", AbilityShape.Burst, 9f, 1.3f, 3, 0f, 4.5f, "Turns the ground to biting mud.") },
                Ab("Coil of the Deep", AbilityShape.Burst, 0f, 2.4f, 1, 0f, 8f, "A giant water serpent rises and coils around the demons.", 20f, 6f),
                HairStyle.Crest, WeaponKind.Spear, CombatStyle.Swift, MotionStyle.Sly,
                new Color(0.2f, 0.25f, 0.18f), new Color(0.2f, 0.38f, 0.3f), new Color(0.25f, 0.5f, 0.3f), new Color(0.4f, 0.95f, 0.7f), new Color(0.36f, 0.62f, 0.34f), 1.05f, 0.95f, false, false, false, null, 1.1f);
            Monster("kree_harpy", "Kree", "Storm Harpy", "Gale Wing Breathing", 4, Element.Thunder, Role.Burst, Species.Harpy,
                "A shrieking harpy who throws razor feathers from the sky.", "Kree nests on the highest peak in the land. She came down because the demons were blocking her view.",
                new[] { Ab("Feather Volley", AbilityShape.Wave, 4f, 1.3f, 3, 11f, 1.5f, "Three razor feathers."), Ab("Talon Dive", AbilityShape.Dash, 7f, 2.3f, 1, 9f, 2f, "Dives with talons out."), Ab("Shriek", AbilityShape.Burst, 9f, 1.3f, 2, 0f, 5f, "A shriek that crackles with lightning.") },
                Ab("Thunderwing Tempest", AbilityShape.Burst, 0f, 2.5f, 1, 0f, 9f, "Flies up and beats her wings; lightning feathers rain on everything.", 22f, 6f),
                HairStyle.Wild, WeaponKind.Fans, CombatStyle.Ranged, MotionStyle.Light,
                new Color(0.3f, 0.26f, 0.35f), new Color(0.4f, 0.34f, 0.6f), new Color(0.55f, 0.4f, 0.75f), new Color(1f, 0.9f, 0.4f), new Color(0.9f, 0.78f, 0.7f), 0.95f, 0.9f);
            Monster("grimstone_gargoyle", "Grimstone", "Cathedral Watcher", "Granite Breathing", 4, Element.Earth, Role.Tank, Species.Gargoyle,
                "A stone gargoyle who wakes at dusk and claws demons to gravel.", "Grimstone watched over the old cathedral for eight hundred years without moving. Then the demons knocked on the door.",
                new[] { Ab("Stone Claw", AbilityShape.MultiSlash, 5f, 0.95f, 4, 0f, 3.5f, "Four stone claw swipes."), Ab("Plummet", AbilityShape.Burst, 7f, 2f, 1, 0f, 4f, "Drops like a statue from above.", 8f, 6f), Ab("Petrify", AbilityShape.Burst, 10f, 1.2f, 1, 0f, 5f, "A stone gaze that staggers demons.", 12f, 2f) },
                Ab("Cathedral Fall", AbilityShape.Burst, 0f, 2.4f, 1, 0f, 8f, "Stone pillars crash down across the battlefield.", 25f, 7f),
                HairStyle.Short, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Stoic,
                new Color(0.3f, 0.3f, 0.32f), new Color(0.35f, 0.3f, 0.4f), new Color(0.3f, 0.3f, 0.32f), new Color(0.6f, 0.9f, 0.9f), new Color(0.58f, 0.6f, 0.62f), 1.1f, 1.15f, false, false, false, null, 0.9f);

            // ---- Epic
            Monster("taurok_minotaur", "Taurok", "Labyrinth Breaker", "Stampede Breathing", 5, Element.Earth, Role.Tank, Species.Minotaur,
                "A minotaur with a double axe who charges through walls, and demons.", "Taurok escaped a labyrinth by walking through it in a straight line. He has been walking in straight lines ever since.",
                new[] { Ab("Gore Charge", AbilityShape.Dash, 6f, 2.4f, 1, 9f, 2.4f, "A horns-down charge.", 8f, 9f), Ab("Axe Whirl", AbilityShape.Spin, 8f, 1.3f, 3, 0f, 4.4f, "Whirls the double axe."), Ab("Hoof Stomp", AbilityShape.Burst, 9f, 1.8f, 1, 0f, 4.5f, "Stomps; the ground cracks.", 8f, 6f) },
                Ab("Maze Breaker", AbilityShape.Dash, 0f, 3f, 3, 16f, 3.5f, "Charges back and forth through the demons three times.", 30f, 9f),
                HairStyle.Short, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Aggressive,
                new Color(0.28f, 0.2f, 0.14f), new Color(0.4f, 0.16f, 0.12f), new Color(0.2f, 0.14f, 0.1f), new Color(0.95f, 0.75f, 0.4f), new Color(0.46f, 0.32f, 0.22f), 1.25f, 1.35f, false, true, true, null, 0.8f);
            Monster("mossgut_troll", "Mossgut", "Bridge Troll", "Moss Breathing", 5, Element.Beast, Role.Tank, Species.Troll,
                "A huge mossy troll with a tree for a club and a toll for every demon.", "Mossgut has charged a toll at the same bridge for two hundred years. Demons pay in bruises.",
                new[] { Ab("Club Sweep", AbilityShape.Spin, 6f, 1.4f, 2, 0f, 4.6f, "A wide sweep of the club."), Ab("Toll Smash", AbilityShape.Burst, 8f, 2.2f, 1, 0f, 4f, "Brings the club down hard.", 8f, 7f), Ab("Regrow", AbilityShape.Heal, 12f, 0.2f, 1, 0f, 5f, "Moss grows over every wound.") },
                Ab("Bridge Collapse", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 9f, "Hurls a whole stone bridge into the demons.", 28f, 9f),
                HairStyle.Wild, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Steady,
                new Color(0.26f, 0.24f, 0.2f), new Color(0.3f, 0.38f, 0.22f), new Color(0.3f, 0.45f, 0.22f), new Color(0.6f, 0.9f, 0.4f), new Color(0.5f, 0.62f, 0.7f), 1.3f, 1.4f, false, false, true, null, 0.8f);
            Monster("karasu_tengu", "Karasu", "Mountain Tengu", "Crow Wind Breathing", 5, Element.Beast, Role.Burst, Species.Tengu,
                "A long-nosed mountain tengu whose feather fan summons gales.", "Karasu trained a hundred swordsmen on his mountain. He decided it was time to fight the demons himself.",
                new[] { Ab("Gale Fan", AbilityShape.Wave, 4f, 1.5f, 2, 12f, 2f, "Two cutting gusts."), Ab("Crow Step", AbilityShape.Dash, 6f, 1.9f, 3, 8f, 1.8f, "Blinks through the air like a crow."), Ab("Whirlwind Fan", AbilityShape.Spin, 8f, 1.2f, 4, 0f, 4.4f, "A spinning gale.") },
                Ab("Heaven's Gale", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 10f, "A great sweep of the fan; a tornado carries every demon away.", 25f, 9f),
                HairStyle.Long, WeaponKind.Fans, CombatStyle.Technical, MotionStyle.Confident,
                new Color(0.1f, 0.1f, 0.12f), new Color(0.75f, 0.72f, 0.65f), new Color(0.9f, 0.9f, 0.92f), new Color(0.6f, 1f, 0.7f), new Color(0.85f, 0.25f, 0.2f), 1.05f, 1f, true, false, false, new Color(0.9f, 0.2f, 0.2f));
            Monster("hollow_ghost", "Hollow", "Lantern Ghost", "Will-o'-Wisp Breathing", 5, Element.Dark, Role.Support, Species.Ghost,
                "A lonely lantern ghost who floats over the battlefield, healing friends and haunting demons.", "Hollow has haunted the old road for a century, lighting the way for lost travellers. The demons made the road too dangerous. That was a mistake.",
                new[] { Ab("Wisp Bolt", AbilityShape.Wave, 4f, 1.4f, 3, 11f, 1.5f, "Three will-o'-wisps fly out."), Ab("Haunt", AbilityShape.Burst, 8f, 1.3f, 3, 0f, 4.5f, "A chill that stings every demon near."), Ab("Lantern Glow", AbilityShape.Heal, 11f, 0.22f, 1, 0f, 6f, "A warm lantern light heals the team.") },
                Ab("Night Parade", AbilityShape.Burst, 0f, 2.3f, 1, 0f, 9f, "A parade of ghostly lanterns sweeps through every demon.", 20f, 5f),
                HairStyle.Long, WeaponKind.Staff, CombatStyle.Healer, MotionStyle.Graceful,
                new Color(0.8f, 0.86f, 0.95f), new Color(0.6f, 0.72f, 0.9f), new Color(0.85f, 0.9f, 1f), new Color(0.5f, 0.85f, 1f), new Color(0.8f, 0.9f, 0.98f), 1f, 0.9f);
            Monster("obelisk_golem", "Obelisk", "Rune Golem", "Bedrock Breathing", 5, Element.Earth, Role.Tank, Species.Golem,
                "An ancient rune golem with boulder fists and all the patience in the world.", "Obelisk was built to guard a temple that crumbled a thousand years ago. It adopted the slayers as its new temple.",
                new[] { Ab("Boulder Punch", AbilityShape.Dash, 6f, 2.3f, 1, 6f, 2.2f, "A heavy running punch.", 8f, 9f), Ab("Rune Quake", AbilityShape.Burst, 8f, 1.8f, 2, 0f, 5f, "Runes flare and the ground shakes.", 8f, 5f), Ab("Stone Skin", AbilityShape.Heal, 12f, 0.18f, 1, 0f, 5f, "Shares its stone skin with the team.") },
                Ab("Awakening", AbilityShape.Burst, 0f, 2.7f, 1, 0f, 9f, "Every rune on its body blazes; a wave of stone erupts outward.", 30f, 9f),
                HairStyle.Short, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Stoic,
                new Color(0.4f, 0.38f, 0.36f), new Color(0.45f, 0.42f, 0.38f), new Color(0.4f, 0.38f, 0.36f), new Color(0.4f, 0.9f, 1f), new Color(0.56f, 0.54f, 0.5f), 1.3f, 1.4f, false, false, true, null, 0.8f);

            // ---- Legendary
            Monster("vesper_vampire", "Vesper Nightthorn", "Crimson Noble", "Crimson Breathing", 6, Element.Dark, Role.Burst, Species.Vampire,
                "A vampire noble with bat wings and a crimson blade, faster than a heartbeat.", "Vesper has been a gentleman for three hundred years. The demons of the eclipse are terribly rude, so he has decided to be rude back.",
                new[] { Ab("Crimson Waltz", AbilityShape.MultiSlash, 4f, 0.85f, 6, 0f, 3.8f, "A dancing six-cut combo."), Ab("Bat Swarm", AbilityShape.Wave, 6f, 1.6f, 3, 11f, 2f, "Sends a swarm of bats."), Ab("Mist Step", AbilityShape.Dash, 7f, 2f, 2, 9f, 1.8f, "Turns to mist and cuts through.") },
                Ab("Eternal Night", AbilityShape.Burst, 0f, 2.8f, 1, 0f, 10f, "The sky goes dark; a thousand bats and a crimson moon fall on the demons.", 25f, 7f),
                HairStyle.Short, WeaponKind.Katana, CombatStyle.Technical, MotionStyle.Confident,
                new Color(0.08f, 0.06f, 0.1f), new Color(0.4f, 0.05f, 0.12f), new Color(0.12f, 0.1f, 0.14f), new Color(1f, 0.15f, 0.3f), new Color(0.88f, 0.86f, 0.92f), 1.1f, 0.95f, true, false, false, new Color(0.85f, 0.7f, 0.3f), 1.15f);
            Monster("kohaku_kitsune", "Kohaku", "Nine-Tail Trickster", "Foxfire Breathing", 6, Element.Light, Role.Burst, Species.Kitsune,
                "A kitsune trickster whose fans throw blue foxfire.", "Kohaku has five tails and wants all nine. Each tail comes from a great deed. Beating the eclipse should be worth at least two.",
                new[] { Ab("Foxfire Fan", AbilityShape.Wave, 4f, 1.5f, 3, 11f, 1.8f, "Three blue foxfire flames."), Ab("Illusion Step", AbilityShape.Dash, 6f, 1.8f, 3, 9f, 1.8f, "Leaves illusions behind as she dashes."), Ab("Fox Ring", AbilityShape.Spin, 8f, 1.2f, 4, 0f, 4.4f, "A ring of foxfire around her.") },
                Ab("Thousand Foxes", AbilityShape.Burst, 0f, 2.7f, 1, 0f, 10f, "Illusion foxes pour out of her tails and burst into foxfire on every demon.", 25f, 6f),
                HairStyle.Long, WeaponKind.Fans, CombatStyle.Technical, MotionStyle.Sly,
                new Color(0.95f, 0.93f, 0.9f), new Color(0.85f, 0.25f, 0.2f), new Color(0.98f, 0.6f, 0.2f), new Color(0.45f, 0.75f, 1f), new Color(0.98f, 0.88f, 0.8f), 1f, 0.9f, false, false, false, new Color(1f, 0.85f, 0.4f), 1.1f);
            Monster("blizz_yeti", "Blizz", "Summit Yeti", "Avalanche Breathing", 6, Element.Water, Role.DPS, Species.Yeti,
                "A towering yeti whose claws leave frost behind.", "Blizz lived alone on the highest summit until the eclipse froze even his mountain. Now he's come down, and he is cold and cranky.",
                new[] { Ab("Frost Maul", AbilityShape.MultiSlash, 4f, 0.95f, 5, 0f, 3.8f, "Five icy claw swipes."), Ab("Avalanche Roll", AbilityShape.Dash, 7f, 2.4f, 2, 9f, 2.4f, "Rolls through like an avalanche.", 6f, 9f), Ab("Ice Roar", AbilityShape.Burst, 9f, 1.5f, 2, 0f, 5f, "A roar that freezes the air.") },
                Ab("Whiteout", AbilityShape.Burst, 0f, 2.8f, 1, 0f, 10f, "A blizzard sweeps the field; ice spikes erupt under every demon.", 28f, 7f),
                HairStyle.Wild, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Aggressive,
                new Color(0.85f, 0.9f, 0.95f), new Color(0.55f, 0.7f, 0.85f), new Color(0.94f, 0.96f, 1f), new Color(0.6f, 0.9f, 1f), new Color(0.55f, 0.65f, 0.85f), 1.3f, 1.35f, false, false, true, null, 1.05f);
            Monster("jack_pumpkin", "Jack Hollowbright", "Pumpkin Knight", "Harvest Flame Breathing", 6, Element.Flame, Role.DPS, Species.PumpkinKnight,
                "A headless knight with a burning carved pumpkin for a head and a greatsword to match.", "Jack lost his head in a war long ago. The pumpkin he found instead is, in his opinion, a big improvement.",
                new[] { Ab("Harvest Cleave", AbilityShape.Wave, 5f, 2.1f, 1, 9f, 2.2f, "A cleaving wave of harvest fire."), Ab("Lantern Charge", AbilityShape.Dash, 7f, 2.2f, 1, 8f, 2.2f, "Charges with a burning grin.", 6f, 7f), Ab("Seed Burst", AbilityShape.Burst, 9f, 1.4f, 3, 0f, 4.5f, "Fiery pumpkin seeds scatter everywhere.") },
                Ab("Hallow's Blaze", AbilityShape.Burst, 0f, 2.8f, 1, 0f, 10f, "Carved pumpkin lanterns rise all over the field and explode in orange fire.", 26f, 7f),
                HairStyle.Short, WeaponKind.Greatsword, CombatStyle.Balanced, MotionStyle.Confident,
                new Color(0.15f, 0.12f, 0.1f), new Color(0.2f, 0.15f, 0.25f), new Color(0.15f, 0.12f, 0.1f), new Color(1f, 0.55f, 0.1f), new Color(0.9f, 0.8f, 0.7f), 1.15f, 1.1f, true, false, true, new Color(0.95f, 0.5f, 0.1f), 0.95f);

            // ---- Mythic
            Monster("mortis_reaper", "Mortis", "Soul Reaper", "Soul Harvest Breathing", 7, Element.Dark, Role.Burst, Species.Reaper,
                "A floating skeletal reaper with a great scythe, who ferries demons to where they belong.", "Mortis has one job: to collect what should not linger. The eclipse brought him a lot of overtime.",
                new[] { Ab("Soul Reap", AbilityShape.Spin, 4f, 1.3f, 4, 0f, 4.4f, "A wide scythe sweep that hits all around."), Ab("Grave Glide", AbilityShape.Dash, 6f, 2.2f, 2, 10f, 2f, "Glides through the demon line."), Ab("Death's Toll", AbilityShape.Burst, 9f, 1.8f, 3, 0f, 5.5f, "A bell tolls three times; each toll strikes.") },
                Ab("Final Harvest", AbilityShape.Burst, 0f, 3.2f, 1, 0f, 11f, "The scythe splits the sky; ghostly hands drag every demon down.", 30f, 8f),
                HairStyle.Hood, WeaponKind.Spear, CombatStyle.Heavy, MotionStyle.Stoic,
                new Color(0.06f, 0.05f, 0.08f), new Color(0.12f, 0.08f, 0.18f), new Color(0.1f, 0.08f, 0.14f), new Color(0.65f, 0.4f, 1f), new Color(0.93f, 0.9f, 0.84f), 1.15f, 0.95f, true, false, false, new Color(0.6f, 0.4f, 1f), 1f);
        }

        /// <summary>
        /// Monster-folk slayers: a few fighters who aren't human, each fighting the way their kind would — a goblin
        /// with daggers, a skeleton with a bone blade, a demon with a trident, a cyclops with a club, a werewolf with
        /// claws and a mummy with lashing bandages.
        /// </summary>
        static void BuildMonsterfolk()
        {
            var C = new Color(0.1f, 0.1f, 0.12f);
            // Goblin — Common, fast daggers.
            var h = Hero("snik_goblin", "snik", "Snik", "Dagger Goblin", "Pickpocket Breathing", 3, Element.Earth, Role.DPS,
                "A cackling goblin with two rusty daggers and very quick hands.",
                "Snik joined the slayers because they had better snacks than the goblin caves. He stayed because he likes stabbing demons even more than snacks.", C, C, C, C);
            h.species = Species.Goblin; h.attackSpeed = 1.25f; h.comboMultipliers = new[] { 0.5f, 0.5f, 0.55f, 0.6f, 1.5f };
            h.skills = new[] { Ab("Stab Stab Stab", AbilityShape.MultiSlash, 4f, 0.6f, 6, 0f, 3.2f, "Six frantic dagger stabs."), Ab("Sneaky Roll", AbilityShape.Dash, 6f, 1.6f, 2, 7f, 1.6f, "Rolls through enemies, daggers out."), Ab("Pocket Sand", AbilityShape.Burst, 9f, 1.2f, 1, 0f, 4f, "Throws sand, then stabs whoever is blinking.", 4f, 3f) };
            h.ultimate = Ab("Goblin Mob", AbilityShape.MultiSlash, 0f, 0.9f, 10, 0f, 7f, "Whistles for his cousins; a mob of daggers swarms every demon nearby.", 18f, 5f);
            Characters.Add(h);
            Look("snik_goblin", HairStyle.Spiky, WeaponKind.TwinBlades, CombatStyle.Swift, MotionStyle.Sly,
                new Color(0.35f, 0.26f, 0.16f), new Color(0.5f, 0.32f, 0.16f), new Color(0.2f, 0.16f, 0.1f), new Color(0.75f, 0.9f, 0.4f), 0.86f, 0.92f, true);
            h.skinTone = new Color(0.46f, 0.72f, 0.34f);

            // Skeleton — Rare, bone blade and a rattling skull.
            h = Hero("clatter_skeleton", "clatter", "Clatter", "Bone Duelist", "Rattle Breathing", 4, Element.Dark, Role.Burst,
                "A cheerful skeleton duelist who fights with a sword carved from a giant's bone.",
                "Clatter woke up in an old battlefield with no memory and a lot of spare ribs. He decided to become a hero, because heroes get to keep their bones on.", C, C, C, C);
            h.species = Species.Skeleton; h.attackSpeed = 1.1f;
            h.skills = new[] { Ab("Bone Toss", AbilityShape.Wave, 5f, 1.7f, 2, 9f, 1.6f, "Throws two spinning bones."), Ab("Rattle Spin", AbilityShape.Spin, 7f, 1.1f, 4, 0f, 3.8f, "Spins apart and back together, blade out."), Ab("Reassemble", AbilityShape.Heal, 12f, 0.18f, 1, 0f, 5f, "Snaps every bone back into place (heals the team a little).") };
            h.ultimate = Ab("Graveyard Encore", AbilityShape.Burst, 0f, 2.4f, 1, 0f, 8f, "Bony hands burst out of the ground and grab every demon around him.", 22f, 6f);
            Characters.Add(h);
            Look("clatter_skeleton", HairStyle.Hood, WeaponKind.Katana, CombatStyle.Swift, MotionStyle.Light,
                new Color(0.18f, 0.16f, 0.22f), new Color(0.32f, 0.22f, 0.45f), new Color(0.2f, 0.16f, 0.3f), new Color(0.7f, 0.5f, 1f), 1f, 0.86f, true, false, false, false, false, new Color(0.9f, 0.75f, 0.35f));
            h.skinTone = new Color(0.94f, 0.91f, 0.82f);

            // Demon — Epic, trident and hellfire.
            h = Hero("brimm_demon", "brimm", "Brimm", "Horned Rebel", "Hellfire Breathing", 5, Element.Flame, Role.Burst,
                "A horned demon who turned against the demon lords, trident first.",
                "Brimm was born in the ember pits and raised to serve the eclipse. He decided he'd rather be the one the demons are afraid of.", C, C, C, C);
            h.species = Species.Demon;
            h.skills = new[] { Ab("Trident Rush", AbilityShape.Dash, 5f, 2.2f, 3, 8f, 1.8f, "Charges with the trident, skewering a line of demons."), Ab("Hellfire Twirl", AbilityShape.Spin, 7f, 1.2f, 4, 0f, 4f, "Twirls the trident in a ring of fire."), Ab("Horn Butt", AbilityShape.Burst, 8f, 2f, 1, 0f, 3.5f, "A head-first slam with his horns.", 6f, 8f) };
            h.ultimate = Ab("Ember Pit", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 9f, "Stabs the ground and opens a pit of hellfire under every demon nearby.", 25f, 7f);
            Characters.Add(h);
            Look("brimm_demon", HairStyle.Wild, WeaponKind.Spear, CombatStyle.Swift, MotionStyle.Aggressive,
                new Color(0.12f, 0.08f, 0.1f), new Color(0.2f, 0.12f, 0.14f), new Color(0.12f, 0.08f, 0.1f), new Color(1f, 0.45f, 0.1f), 1.05f, 1f, false, true, false, false, false, new Color(0.95f, 0.6f, 0.15f));
            h.skinTone = new Color(0.86f, 0.3f, 0.26f);

            // Cyclops — Epic tank, one eye and a giant club.
            h = Hero("gorro_cyclops", "gorro", "Gorro", "One-Eyed Guardian", "Titan Breathing", 5, Element.Earth, Role.Tank,
                "A gentle one-eyed giant who swings a club the size of a tree.",
                "Gorro guarded a mountain pass for three hundred years. Nobody told him the war ended, so he found a new one.", C, C, C, C);
            h.species = Species.Cyclops; h.attackSpeed = 0.8f; h.comboMultipliers = new[] { 0.9f, 0.95f, 1.05f, 1.1f, 2.1f };
            h.skills = new[] { Ab("Club Smash", AbilityShape.Burst, 6f, 2.4f, 1, 0f, 4.2f, "Brings the club down like a falling tree.", 8f, 8f), Ab("Boulder Throw", AbilityShape.Wave, 8f, 2.2f, 1, 10f, 2.4f, "Hurls a boulder in a straight line."), Ab("Stomp", AbilityShape.Burst, 9f, 1.6f, 2, 0f, 5f, "Stomps twice; the ground jumps.", 7f, 5f) };
            h.ultimate = Ab("Titan's Glare", AbilityShape.Wave, 0f, 3.4f, 1, 18f, 3f, "Opens his great eye wide and sweeps a beam of light across the field.", 25f, 8f);
            Characters.Add(h);
            Look("gorro_cyclops", HairStyle.Short, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Stoic,
                new Color(0.36f, 0.3f, 0.22f), new Color(0.46f, 0.38f, 0.26f), new Color(0.3f, 0.22f, 0.16f), new Color(0.85f, 0.75f, 0.5f), 1.25f, 1.4f, false, false, true);
            h.skinTone = new Color(0.56f, 0.66f, 0.86f);

            // Werewolf — Legendary, claws.
            h = Hero("howl_werewolf", "howl", "Howl", "Moon Fang", "Full Moon Breathing", 6, Element.Beast, Role.DPS,
                "A werewolf slayer who fights with his claws and never, ever loses a scent.",
                "Howl was bitten by a moon demon as a boy. Instead of becoming a monster, he became the one monsters run from.", C, C, C, C);
            h.species = Species.Werewolf; h.attackSpeed = 1.2f; h.comboMultipliers = new[] { 0.6f, 0.6f, 0.7f, 0.8f, 1.7f };
            h.skills = new[] { Ab("Claw Rake", AbilityShape.MultiSlash, 4f, 0.8f, 6, 0f, 3.6f, "Rakes with both claws, over and over."), Ab("Pounce", AbilityShape.Dash, 6f, 2.4f, 1, 9f, 2f, "Leaps onto the nearest demon, claws first.", 5f, 7f), Ab("Moon Howl", AbilityShape.Burst, 9f, 1.4f, 2, 0f, 5f, "A howl that shakes demons off their feet.", 8f, 6f) };
            h.ultimate = Ab("Full Moon Frenzy", AbilityShape.MultiSlash, 0f, 1.2f, 12, 0f, 8f, "The moon rises; he tears through every demon in a blur of claws.", 25f, 6f);
            Characters.Add(h);
            Look("howl_werewolf", HairStyle.Wild, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Aggressive,
                new Color(0.25f, 0.22f, 0.3f), new Color(0.32f, 0.3f, 0.42f), new Color(0.46f, 0.42f, 0.4f), new Color(0.75f, 0.85f, 1f), 1.12f, 1.2f, false, false, true);
            h.skinTone = new Color(0.5f, 0.46f, 0.44f);

            // Mummy — Legendary, lashing bandages.
            h = Hero("tamun_mummy", "tamun", "Tamun", "Wrapped Wanderer", "Sand Breathing", 6, Element.Light, Role.Support,
                "An ancient mummy who fights with the living bandages that wrap him.",
                "Tamun slept for four thousand years under the desert. The eclipse woke him up. He is very grumpy about that, and the demons are paying for it.", C, C, C, C);
            h.species = Species.Mummy;
            h.skills = new[] { Ab("Wrap Lash", AbilityShape.Wave, 4f, 1.5f, 2, 10f, 1.6f, "Two bandages whip out in a straight line."), Ab("Bandage Bind", AbilityShape.Burst, 8f, 1.4f, 3, 0f, 4.5f, "Bandages snake out and bind every demon around him.", 8f, 2f), Ab("Sand Mend", AbilityShape.Heal, 12f, 0.2f, 1, 0f, 6f, "Wraps the team in healing sand.") };
            h.ultimate = Ab("Tomb Unravel", AbilityShape.Burst, 0f, 2.5f, 1, 0f, 9f, "Unravels completely; a storm of bandages and sand sweeps the battlefield.", 22f, 6f);
            Characters.Add(h);
            Look("tamun_mummy", HairStyle.Hood, WeaponKind.Fans, CombatStyle.Ranged, MotionStyle.Stoic,
                new Color(0.82f, 0.76f, 0.6f), new Color(0.22f, 0.4f, 0.6f), new Color(0.8f, 0.74f, 0.58f), new Color(1f, 0.85f, 0.35f), 1.02f, 0.92f, false, true, false, false, false, new Color(0.95f, 0.75f, 0.25f));
            h.skinTone = new Color(0.9f, 0.85f, 0.7f);
        }

        /// <summary>More slayers to collect: four Common, four Rare and three Legendary, each with their own special.</summary>
        static void BuildNewcomers()
        {
            var C = new Color(0.1f, 0.1f, 0.12f);
            // ---- Common
            var h = Hero("taro_rock", "taro", "Taro Ishida", "Village Strongman", "Boulder Breathing", 3, Element.Earth, Role.Tank,
                "The strongest man in Kiriha. He carries the festival drum by himself.", "Taro has never been in a real fight. He has lifted every rock in the valley, though.", C, C, C, C);
            h.skills = new[] { Ab("Shoulder Bash", AbilityShape.Dash, 6f, 1.8f, 1, 6f, 2f, "A heavy running bash.", 5f, 7f), Ab("Ground Pound", AbilityShape.Burst, 8f, 1.6f, 1, 0f, 4f, "Pounds the ground.", 7f, 5f), Ab("Windmill", AbilityShape.Spin, 9f, 1f, 3, 0f, 3.5f, "Spins with fists out.") };
            h.ultimate = Ab("Boulder Toss", AbilityShape.Burst, 0f, 2f, 1, 0f, 5f, "Lifts a boulder and throws it.", 20f, 8f);
            Characters.Add(h);
            h = Hero("nami_bubble", "nami", "Nami Kawase", "River Dancer", "Bubble Breathing", 3, Element.Water, Role.Support,
                "A cheerful river girl who fights with paper fans and a lot of bubbles.", "Nami ferries travellers across the river. The demons learned not to board her boat.", C, C, C, C);
            h.skills = new[] { Ab("Splash Fan", AbilityShape.Wave, 5f, 1.4f, 2, 8f, 1.6f, "Two fans of water."), Ab("Soap Swirl", AbilityShape.Spin, 7f, 0.9f, 3, 0f, 3.5f, "A swirl of bubbles."), Ab("River Rest", AbilityShape.Heal, 12f, 0.15f, 1, 0f, 5f, "Heals the team a little.") };
            h.ultimate = Ab("Bubble Barrage", AbilityShape.Burst, 0f, 1.8f, 1, 0f, 7f, "A flurry of bubbles that pop on every demon.", 15f, 5f);
            Characters.Add(h);
            h = Hero("koji_ember", "koji", "Koji Hinode", "Cook's Apprentice", "Hearth Breathing", 3, Element.Flame, Role.DPS,
                "Learned swordplay from chopping vegetables very, very fast.", "Koji's kitchen burned down during the attack. He's been angry and hungry ever since.", C, C, C, C);
            h.skills = new[] { Ab("Quick Chop", AbilityShape.MultiSlash, 5f, 0.7f, 4, 0f, 3.5f, "Four quick cuts."), Ab("Sizzle Dash", AbilityShape.Dash, 7f, 2f, 1, 7f, 1.8f, "A flaming dash."), Ab("Wok Toss", AbilityShape.Burst, 9f, 1.6f, 1, 0f, 4f, "A burst of flame.") };
            h.ultimate = Ab("Ember Spin", AbilityShape.Burst, 0f, 2f, 1, 0f, 5f, "A spinning tornado of flame.", 15f, 6f);
            Characters.Add(h);
            h = Hero("yuna_gale", "yuna", "Yuna Kaze", "Shrine Archer", "Gale Breathing", 3, Element.Beast, Role.DPS,
                "The shrine's archer. She can hit a falling leaf from the top of the steps.", "Yuna guards the shrine alone since the priests left. She talks to the wind. It answers.", C, C, C, C);
            h.skills = new[] { Ab("Twin Shot", AbilityShape.Wave, 5f, 1.3f, 2, 10f, 1.4f, "Two quick arrows."), Ab("Leaf Step", AbilityShape.Dash, 7f, 1.5f, 1, 6f, 1.6f, "Dashes through, loosing arrows."), Ab("Whirlwind", AbilityShape.Spin, 9f, 0.9f, 3, 0f, 3.8f, "A ring of wind.") };
            h.ultimate = Ab("Gale Arrow", AbilityShape.Wave, 0f, 3f, 1, 18f, 2.5f, "One great arrow wrapped in a storm.", 20f, 6f);
            Characters.Add(h);

            // ---- Rare
            h = Hero("daigo_stone", "daigo", "Daigo Iwakura", "Quarry Guard", "Mountain Breathing", 4, Element.Earth, Role.Tank,
                "A quiet giant who guards the stone quarry with a sword as big as a door.", "Daigo speaks one sentence a day. Usually it's 'Move.'", C, C, C, C);
            h.skills = new[] { Ab("Stone Cleave", AbilityShape.Wave, 6f, 2.2f, 1, 8f, 2.2f, "A cleaving wave of rock.", 6f, 5f), Ab("Quake", AbilityShape.Burst, 8f, 1.8f, 1, 0f, 4.5f, "Shakes the ground.", 8f, 6f), Ab("Boulder Rush", AbilityShape.Dash, 9f, 2f, 1, 7f, 2.4f, "Charges like a rockslide.", 6f, 8f) };
            h.ultimate = Ab("Landslide", AbilityShape.Burst, 0f, 2.2f, 1, 0f, 6f, "Boulders tumble down on the demons.", 22f, 8f);
            Characters.Add(h);
            h = Hero("hotaru_light", "hotaru", "Hotaru Sen", "Firefly Keeper", "Glow Breathing", 4, Element.Light, Role.Support,
                "A gentle healer followed everywhere by fireflies.", "Hotaru's fireflies found her lost in the forest as a child and never left.", C, C, C, C);
            h.skills = new[] { Ab("Glow Mend", AbilityShape.Heal, 11f, 0.2f, 1, 0f, 6f, "Heals the whole team."), Ab("Light Needles", AbilityShape.Wave, 6f, 1.3f, 3, 9f, 1.4f, "Three needles of light."), Ab("Lantern Ring", AbilityShape.Burst, 9f, 1.4f, 2, 0f, 4.5f, "A ring of light.") };
            h.ultimate = Ab("Firefly Swarm", AbilityShape.Heal, 0f, 0.3f, 1, 0f, 8f, "A swarm of fireflies heals friends and stings demons.", 15f, 4f);
            Characters.Add(h);
            h = Hero("kenta_spear", "kenta", "Kenta Arashi", "Storm Lancer", "Spark Breathing", 4, Element.Thunder, Role.DPS,
                "A lancer who charges first and asks questions never.", "Kenta was a royal guard until he charged the wrong way at a parade. He regrets nothing.", C, C, C, C);
            h.skills = new[] { Ab("Thrust", AbilityShape.Dash, 5f, 2.2f, 1, 7f, 1.6f, "A lightning thrust."), Ab("Spark Wheel", AbilityShape.Spin, 7f, 1.1f, 3, 0f, 3.6f, "Twirls the spear."), Ab("Static Field", AbilityShape.Burst, 9f, 1.5f, 3, 0f, 4.5f, "Crackling sparks.") };
            h.ultimate = Ab("Lightning Lance", AbilityShape.Wave, 0f, 3f, 1, 16f, 2f, "Throws the spear as a bolt of lightning.", 20f, 6f);
            Characters.Add(h);
            h = Hero("rin_shadow", "rin", "Rin Kurosawa", "Night Blade", "Shade Breathing", 4, Element.Dark, Role.Burst,
                "A quiet swordswoman who walks in shadows — literally.", "Rin was raised in the palace's secret guard. She left when the Chancellor gave the orders.", C, C, C, C);
            h.skills = new[] { Ab("Shade Cut", AbilityShape.MultiSlash, 5f, 0.8f, 5, 0f, 4f, "Cuts from the shadows."), Ab("Night Step", AbilityShape.Dash, 7f, 2f, 2, 7f, 1.8f, "Steps through the dark."), Ab("Gloom", AbilityShape.Burst, 9f, 1.5f, 2, 0f, 4.5f, "A burst of darkness.") };
            h.ultimate = Ab("Shadow Snare", AbilityShape.Burst, 0f, 2.2f, 1, 0f, 8f, "Shadow hands rise, grab and crush.", 20f, 7f);
            Characters.Add(h);

            // ---- Legendary
            h = Hero("akane_fire", "akane", "Akane Hibana", "Firework Dancer", "Festival Breathing", 6, Element.Flame, Role.Burst,
                "The festival's star dancer. Her fans throw fireworks.", "Akane dances for the dead at every festival. This year, she decided to dance for the living by burning every demon in the valley.", C, C, C, C);
            h.skills = new[] { Ab("Sparkler Fan", AbilityShape.Wave, 5f, 1.6f, 3, 9f, 1.8f, "Three sparkling waves."), Ab("Pinwheel", AbilityShape.Spin, 7f, 1.3f, 4, 0f, 4.2f, "A spinning pinwheel of fire."), Ab("Rocket Step", AbilityShape.Dash, 8f, 2.6f, 1, 9f, 2f, "A rocket-powered dash.") };
            h.ultimate = Ab("Firework Finale", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 9f, "Fireworks burst over the demons and rain down.", 25f, 7f);
            Characters.Add(h);
            h = Hero("tsukasa_storm", "tsukasa", "Tsukasa Raikou", "Thunder Archer", "Thunder Bow Breathing", 6, Element.Thunder, Role.DPS,
                "An archer whose arrows turn into lightning in flight.", "Tsukasa was struck by lightning twice as a boy. The third time, he caught it.", C, C, C, C);
            h.skills = new[] { Ab("Volt Arrow", AbilityShape.Wave, 5f, 1.8f, 2, 11f, 1.6f, "Two charged arrows."), Ab("Thunder Step", AbilityShape.Dash, 7f, 2.2f, 1, 8f, 1.8f, "A lightning step."), Ab("Storm Nest", AbilityShape.Burst, 9f, 1.4f, 4, 0f, 5f, "Four bolts around him.") };
            h.ultimate = Ab("Chain Storm Volley", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 10f, "Arrows find every demon; lightning jumps between them.", 25f, 7f);
            Characters.Add(h);
            h = Hero("mizuki_tide", "mizuki", "Mizuki Shiun", "Maelstrom Blade", "Whirlpool Breathing", 6, Element.Water, Role.DPS,
                "Twin blades and a temper like a storm at sea.", "Mizuki was a pirate hunter on the southern coast. Pirates were boring. Demons are not.", C, C, C, C);
            h.skills = new[] { Ab("Riptide", AbilityShape.MultiSlash, 5f, 0.85f, 6, 0f, 4f, "Six cuts like crashing waves."), Ab("Undertow", AbilityShape.Dash, 7f, 2.2f, 2, 8f, 2f, "Drags through the enemy line."), Ab("Whirlpool", AbilityShape.Spin, 9f, 1.2f, 4, 0f, 4.5f, "A spinning whirlpool.") };
            h.ultimate = Ab("Maelstrom Waltz", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 10f, "Rings of water geysers spread from her dance.", 25f, 7f);
            Characters.Add(h);

            // ---- Premium design test: three new fighters built to the new character-quality standard.
            h = Hero("kaito_gale", "kaito", "Kaito Hayate", "Gale Fang", "Crosswind Breathing", 4, Element.Beast, Role.DPS,
                "A grinning twin-blade striker who fights low and never stops moving.",
                "Kaito ran messages between mountain shrines until he could outrun the wind itself. Now he carries two short blades and a scarf that is always three steps behind him.", C, C, C, C);
            h.skills = new[] { Ab("Crosscut", AbilityShape.MultiSlash, 4f, 0.7f, 6, 0f, 3.6f, "Six crossing cuts from both blades."), Ab("Gale Step", AbilityShape.Dash, 6f, 1.9f, 2, 8f, 1.8f, "Dashes through the enemy line on the wind."), Ab("Whirlwind", AbilityShape.Spin, 8f, 1.1f, 4, 0f, 4f, "A spinning cyclone of blades.") };
            h.ultimate = Ab("Crosswind Cyclone", AbilityShape.Burst, 0f, 2.3f, 1, 0f, 8f, "Leaps into the sky, drags demons into a green cyclone and ends it with an X-cut.", 20f, 6f);
            h.attackSpeed = 1.2f;
            Characters.Add(h);
            h = Hero("oboro_iron", "oboro", "Oboro Tetsugane", "Iron Sun", "Forge Breathing", 5, Element.Flame, Role.Tank,
                "A mountain of a man with a studded iron club that glows like a forge.",
                "Oboro was a smith who hammered blades for slayers for thirty years. When the demons burned his forge, he picked up the heaviest thing left in it.", C, C, C, C);
            h.skills = new[] { Ab("Anvil Drop", AbilityShape.Burst, 6f, 2.2f, 1, 0f, 4.2f, "Brings the club straight down like a hammer."), Ab("Bellows Charge", AbilityShape.Dash, 8f, 2f, 1, 7f, 2.2f, "A shoulder charge that shoves demons aside."), Ab("Slag Sweep", AbilityShape.Spin, 9f, 1.4f, 3, 0f, 4.8f, "A wide sweep that leaves burning slag.") };
            h.ultimate = Ab("Sunbreaker", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 9f, "A leaping slam that splits the ground into rivers of molten light.", 30f, 8f);
            h.attackSpeed = 0.85f;
            Characters.Add(h);
            h = Hero("shion_storm", "shion", "Shion Raiu", "Storm Oracle", "Heavenly Thunder Breathing", 6, Element.Thunder, Role.Burst,
                "A calm oracle who floats above the battlefield and calls lightning with paper charms.",
                "Shion reads the weather the way others read books. The shrine elders say the storms started listening to her before she could walk.", C, C, C, C);
            h.skills = new[] { Ab("Charm Volley", AbilityShape.Wave, 5f, 1.7f, 3, 11f, 1.6f, "Three charms fly out and burst into sparks."), Ab("Thunder Seal", AbilityShape.Burst, 7f, 1.6f, 3, 0f, 4.5f, "A seal on the ground that strikes three times."), Ab("Storm Veil", AbilityShape.Spin, 9f, 1.2f, 4, 0f, 4.5f, "Lightning circles around her.") };
            h.ultimate = Ab("Heavenly Thunder Array", AbilityShape.Burst, 0f, 2.7f, 1, 0f, 10f, "Charms form a six-point seal; lightning strikes every point, then the heavens strike the center.", 25f, 7f);
            Characters.Add(h);

            // ---- Roster design test: six originals built to the Character Design Bible.
            h = Hero("tobi_kazami", "tobi", "Tobi Kazami", "Tailwind Runner", "Tailwind Breathing", 4, Element.Beast, Role.DPS,
                "A cocky courier who fights at full sprint with a short straight blade.",
                "Tobi delivered letters across three mountain passes a day and never lost one. He says demons are just slower letters.", C, C, C, C);
            h.skills = new[] { Ab("Quick Draw", AbilityShape.MultiSlash, 4f, 0.7f, 5, 0f, 3.4f, "Five cuts before the blade is even seen."), Ab("Tailwind Dash", AbilityShape.Dash, 5.5f, 1.8f, 2, 8.5f, 1.7f, "Rides the wind straight through the line."), Ab("Gust Spin", AbilityShape.Spin, 8f, 1.1f, 4, 0f, 3.8f, "A spinning cut that throws up a whirlwind.") };
            h.ultimate = Ab("Hundred-Mile Relay", AbilityShape.Burst, 0f, 2.3f, 1, 0f, 8f, "Blurs between every demon on the field in one breath.", 20f, 6f);
            h.attackSpeed = 1.25f; h.designTest = true;
            Characters.Add(h);
            h = Hero("bunta_okuyama", "bunta", "Bunta Okuyama", "Mountain Anvil", "Kiln Breathing", 5, Element.Flame, Role.Tank,
                "A huge, easygoing quarryman with a war hammer that glows like a kiln.",
                "Bunta split stone for forty temples before a demon split his quarry. He has been politely returning the favour ever since.", C, C, C, C);
            h.skills = new[] { Ab("Kiln Drop", AbilityShape.Burst, 6f, 2.3f, 1, 0f, 4.4f, "Brings the hammer down like a falling boulder."), Ab("Rockslide", AbilityShape.Dash, 8f, 2f, 1, 6.5f, 2.4f, "A shoulder-first charge that bowls demons over."), Ab("Ember Quake", AbilityShape.Spin, 9f, 1.5f, 3, 0f, 5f, "A wide swing that cracks the ground into embers.") };
            h.ultimate = Ab("Mountain's Weight", AbilityShape.Burst, 0f, 2.7f, 1, 0f, 9f, "Leaps and lands with the weight of a mountain; the ground erupts in rings of fire.", 30f, 8f);
            h.attackSpeed = 0.82f; h.designTest = true;
            Characters.Add(h);
            h = Hero("sayo_mikage", "sayo", "Sayo Mikage", "Storm Sight", "Thunderhead Breathing", 5, Element.Thunder, Role.DPS,
                "A cool-headed archer whose tall bow fires arrows that split into lightning.",
                "Sayo guarded the high watchtower of Mikage alone for six winters. She can see a demon's shadow move from two valleys away.", C, C, C, C);
            h.skills = new[] { Ab("Split Bolt", AbilityShape.Wave, 5f, 1.7f, 3, 12f, 1.5f, "One arrow that splits into three bolts."), Ab("Step Back", AbilityShape.Dash, 6f, 1.5f, 1, 6f, 1.6f, "Leaps back, firing as she lands."), Ab("Thunderhead", AbilityShape.Burst, 9f, 1.4f, 4, 0f, 5f, "Arrows rain from a gathering storm cloud.") };
            h.ultimate = Ab("Heaven's Watchtower", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 10f, "Draws the bow to the sky; a lightning arrow falls on every demon in sight.", 25f, 7f);
            h.designTest = true;
            Characters.Add(h);
            h = Hero("nene_hanabusa", "nene", "Nene Hanabusa", "Lantern Keeper", "Lamplight Breathing", 4, Element.Light, Role.Support,
                "A tiny, cheerful shrine keeper whose lantern heals friends and dazzles demons.",
                "Nene lit the thousand lanterns of Hanabusa shrine every night since she was five. Demons hate the light. She thinks that's very rude of them.", C, C, C, C);
            h.skills = new[] { Ab("Lantern Swing", AbilityShape.Wave, 5f, 1.4f, 2, 9f, 1.8f, "Two balls of lantern light."), Ab("Warm Glow", AbilityShape.Heal, 10f, 0.5f, 1, 0f, 5f, "Mends the team in a circle of light."), Ab("Festival Ring", AbilityShape.Spin, 8f, 1.1f, 4, 0f, 4.2f, "Paper lanterns circle her and burst.") };
            h.ultimate = Ab("Thousand Lanterns", AbilityShape.Burst, 0f, 2.1f, 1, 0f, 9f, "A thousand lanterns rise and fall as healing light and dazzling sparks.", 20f, 6f);
            h.designTest = true;
            Characters.Add(h);
            h = Hero("nagi_kurokiri", "nagi", "Nagi Kurokiri", "Black Fog", "Mist Veil Breathing", 6, Element.Dark, Role.Burst,
                "A silent hooded hunter with twin sickles, gone before the demons turn around.",
                "No one knows Nagi's face. The Corps knows only that the worst demon nests go quiet a night after the hooded one is sent.", C, C, C, C);
            h.skills = new[] { Ab("Twin Reap", AbilityShape.MultiSlash, 4.5f, 0.85f, 6, 0f, 3.2f, "Six hooking cuts from both sickles."), Ab("Fog Step", AbilityShape.Dash, 6f, 2.2f, 2, 8f, 1.6f, "Vanishes into fog and reappears behind the target."), Ab("Chain Whirl", AbilityShape.Spin, 8.5f, 1.3f, 4, 0f, 4f, "The sickles spin out on their chains.") };
            h.ultimate = Ab("Moonless Harvest", AbilityShape.Burst, 0f, 2.6f, 1, 0f, 8f, "Black fog covers the field; every demon is cut from the dark at once.", 25f, 6f);
            h.attackSpeed = 1.15f; h.designTest = true;
            Characters.Add(h);
            h = Hero("seiran_mizuchi", "seiran", "Seiran Mizuchi", "Tidal Sovereign", "Deep Current Breathing", 7, Element.Water, Role.Burst,
                "A towering, serene warrior who commands the tide with a water glaive.",
                "Seiran is the last heir of the river-dragon clan. The rivers still bow when she passes, and so, sooner or later, do demons.", C, C, C, C);
            h.skills = new[] { Ab("Current Arc", AbilityShape.Wave, 5f, 1.9f, 2, 10f, 2f, "Two sweeping arcs of water."), Ab("Riverbed Charge", AbilityShape.Dash, 7f, 2.2f, 1, 8f, 2.2f, "Drives forward on a wave."), Ab("Maelstrom Guard", AbilityShape.Spin, 9f, 1.4f, 4, 0f, 5f, "The glaive spins inside a ring of water.") };
            h.ultimate = Ab("River Dragon's Descent", AbilityShape.Burst, 0f, 2.9f, 1, 0f, 11f, "A dragon of water rises behind her and crashes down across the field.", 30f, 8f);
            h.designTest = true;
            Characters.Add(h);
        }

        static void Look(string id, HairStyle hair, WeaponKind weapon, CombatStyle style, MotionStyle motion, Color body, Color outfit, Color hairC, Color blade,
            float h = 1f, float w = 1f, bool scarf = false, bool cape = false, bool pelt = false, bool armor = false, bool bell = false, Color? accent = null)
        {
            var c = Characters.Find(x => x.id == id) ?? Npcs.Find(x => x.id == id);
            if (c == null) return;
            c.hair = hair; c.weapon = weapon; c.style = style; c.motion = motion;
            c.bodyColor = body; c.haoriColor = outfit; c.hairColor = hairC; c.bladeColor = blade;
            c.bodyHeight = h; c.bodyWidth = w; c.scarf = scarf; c.cape = cape; c.pelt = pelt; c.armor = armor; c.bell = bell;
            c.accentColor = accent ?? outfit;
        }

        /// <summary>
        /// Every slayer has their own silhouette (hair, weapon, proportions, accessories), fighting style and
        /// movement personality, in the simple chibi art style of the game.
        /// </summary>
        static void ApplyLooks()
        {
            var ink = new Color(0.08f, 0.08f, 0.1f);
            Look("ren_initiate", HairStyle.Messy, WeaponKind.Katana, CombatStyle.Technical, MotionStyle.Steady,
                ink, new Color(0.08f, 0.42f, 0.44f), new Color(0.8f, 0.12f, 0.12f), new Color(0.35f, 0.8f, 1f));
            Look("ren_sundance", HairStyle.Messy, WeaponKind.Katana, CombatStyle.Technical, MotionStyle.Confident,
                ink, new Color(0.95f, 0.55f, 0.15f), new Color(0.8f, 0.12f, 0.12f), new Color(1f, 0.8f, 0.3f), 1.02f, 1f, false, true);
            Look("sora_initiate", HairStyle.Spiky, WeaponKind.TwinBlades, CombatStyle.Swift, MotionStyle.Nervous,
                new Color(0.12f, 0.12f, 0.15f), new Color(0.45f, 0.47f, 0.52f), ink, new Color(1f, 0.85f, 0.25f), 0.95f, 0.92f, false, false, false, false, false, new Color(0.22f, 0.22f, 0.6f));
            Look("kiba_initiate", HairStyle.Wild, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Aggressive,
                new Color(0.3f, 0.2f, 0.13f), new Color(0.35f, 0.25f, 0.15f), new Color(0.1f, 0.12f, 0.32f), new Color(0.9f, 0.6f, 0.3f), 1.1f, 1.25f, false, false, true);
            Look("hana_healer", HairStyle.Braid, WeaponKind.Staff, CombatStyle.Healer, MotionStyle.Graceful,
                new Color(0.95f, 0.94f, 0.98f), new Color(0.76f, 0.62f, 0.92f), new Color(0.82f, 0.76f, 0.94f), new Color(0.95f, 0.72f, 1f), 0.95f, 0.92f, false, false, false, false, true);
            Look("tetsu_guard", HairStyle.Short, WeaponKind.SwordShield, CombatStyle.Heavy, MotionStyle.Stoic,
                new Color(0.33f, 0.4f, 0.55f), new Color(0.14f, 0.18f, 0.4f), new Color(0.28f, 0.18f, 0.1f), new Color(0.85f, 0.85f, 0.9f), 1.15f, 1.3f, false, true, false, true);
            Look("homura_pillar", HairStyle.Ponytail, WeaponKind.Katana, CombatStyle.Balanced, MotionStyle.Confident,
                ink, new Color(0.6f, 0.08f, 0.1f), new Color(0.85f, 0.15f, 0.1f), new Color(1f, 0.5f, 0.1f), 1.08f, 1f, false, true);
            Look("homura_lastflame", HairStyle.Ponytail, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Confident,
                ink, new Color(0.75f, 0.12f, 0.06f), new Color(1f, 0.35f, 0.08f), new Color(1f, 0.65f, 0.1f), 1.1f, 1.05f, false, true, false, true);
            Look("mina_ember", HairStyle.Long, WeaponKind.Katana, CombatStyle.Swift, MotionStyle.Light,
                ink, new Color(0.25f, 0.18f, 0.35f), new Color(0.95f, 0.95f, 0.97f), new Color(0.75f, 0.4f, 1f), 0.9f, 0.88f);
            Look("rokuro_hunter", HairStyle.Curly, WeaponKind.Bow, CombatStyle.Ranged, MotionStyle.Sly,
                new Color(0.3f, 0.26f, 0.2f), new Color(0.4f, 0.45f, 0.3f), new Color(0.15f, 0.12f, 0.1f), new Color(0.75f, 0.9f, 0.5f), 1.05f, 1.05f, false, false, true, false, false, new Color(0.42f, 0.26f, 0.14f));
            Look("genji_ronin", HairStyle.StrawHat, WeaponKind.Katana, CombatStyle.Technical, MotionStyle.Sly,
                new Color(0.15f, 0.15f, 0.2f), new Color(0.35f, 0.42f, 0.58f), ink, new Color(0.7f, 0.85f, 1f), 1.05f, 1f, false, true, false, false, false, new Color(0.8f, 0.68f, 0.4f));
            Look("yui_tide", HairStyle.Twintails, WeaponKind.Fans, CombatStyle.Ranged, MotionStyle.Graceful,
                new Color(0.1f, 0.12f, 0.2f), new Color(0.2f, 0.55f, 0.85f), new Color(0.2f, 0.4f, 0.8f), new Color(0.45f, 0.85f, 1f), 0.94f, 0.92f);
            Look("raiga_pillar", HairStyle.Crest, WeaponKind.Spear, CombatStyle.Swift, MotionStyle.Light,
                ink, new Color(0.95f, 0.85f, 0.2f), new Color(0.96f, 0.96f, 0.98f), new Color(1f, 0.95f, 0.45f), 1.08f, 0.95f, false, false, false, true);
            Look("kuroe_moon", HairStyle.Bob, WeaponKind.Moon, CombatStyle.Technical, MotionStyle.Sly,
                new Color(0.1f, 0.05f, 0.1f), new Color(0.5f, 0.08f, 0.22f), new Color(0.05f, 0.05f, 0.07f), new Color(1f, 0.2f, 0.4f), 1.05f, 0.95f, false, true);
            Look("seren_starfall", HairStyle.Bun, WeaponKind.Staff, CombatStyle.Ranged, MotionStyle.Graceful,
                new Color(0.95f, 0.93f, 1f), new Color(0.28f, 0.3f, 0.78f), new Color(0.86f, 0.9f, 1f), new Color(1f, 0.92f, 0.55f), 1f, 0.92f, true, false, false, false, true, new Color(1f, 0.85f, 0.4f));
            Look("garou_onyx", HairStyle.Hood, WeaponKind.Cleavers, CombatStyle.Heavy, MotionStyle.Stoic,
                new Color(0.12f, 0.12f, 0.13f), new Color(0.14f, 0.34f, 0.3f), new Color(0.14f, 0.34f, 0.3f), new Color(0.55f, 1f, 0.8f), 1.12f, 1.1f, true, true, false, true, false, new Color(0.5f, 0.95f, 0.75f));
            // Newcomers.
            Look("taro_rock", HairStyle.Bun, WeaponKind.Fists, CombatStyle.Brawler, MotionStyle.Steady,
                new Color(0.4f, 0.3f, 0.2f), new Color(0.55f, 0.4f, 0.25f), new Color(0.12f, 0.08f, 0.05f), new Color(0.8f, 0.6f, 0.35f), 1.12f, 1.3f, false, false, true);
            Look("nami_bubble", HairStyle.Curly, WeaponKind.Fans, CombatStyle.Ranged, MotionStyle.Light,
                new Color(0.2f, 0.3f, 0.5f), new Color(0.45f, 0.75f, 0.95f), new Color(0.3f, 0.55f, 0.9f), new Color(0.6f, 0.9f, 1f), 0.92f, 0.9f);
            Look("koji_ember", HairStyle.Spiky, WeaponKind.Katana, CombatStyle.Balanced, MotionStyle.Aggressive,
                new Color(0.95f, 0.93f, 0.88f), new Color(0.85f, 0.35f, 0.12f), new Color(0.95f, 0.5f, 0.1f), new Color(1f, 0.5f, 0.15f), 0.98f, 1f, true);
            Look("yuna_gale", HairStyle.Ponytail, WeaponKind.Bow, CombatStyle.Ranged, MotionStyle.Graceful,
                new Color(0.95f, 0.95f, 0.97f), new Color(0.8f, 0.15f, 0.2f), new Color(0.55f, 0.35f, 0.2f), new Color(0.6f, 1f, 0.7f), 0.96f, 0.9f);
            Look("daigo_stone", HairStyle.Short, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Stoic,
                new Color(0.3f, 0.3f, 0.32f), new Color(0.45f, 0.42f, 0.38f), new Color(0.2f, 0.2f, 0.2f), new Color(0.75f, 0.7f, 0.6f), 1.2f, 1.35f, false, true, false, true);
            Look("hotaru_light", HairStyle.Long, WeaponKind.Staff, CombatStyle.Healer, MotionStyle.Graceful,
                new Color(0.97f, 0.96f, 0.9f), new Color(0.95f, 0.85f, 0.45f), new Color(0.3f, 0.22f, 0.15f), new Color(1f, 0.95f, 0.5f), 0.94f, 0.9f, false, false, false, false, true);
            Look("kenta_spear", HairStyle.Crest, WeaponKind.Spear, CombatStyle.Swift, MotionStyle.Confident,
                new Color(0.2f, 0.25f, 0.45f), new Color(0.3f, 0.4f, 0.8f), new Color(0.95f, 0.8f, 0.2f), new Color(1f, 0.95f, 0.45f), 1.05f, 1f, true, false, false, true);
            Look("rin_shadow", HairStyle.Bob, WeaponKind.TwinBlades, CombatStyle.Swift, MotionStyle.Sly,
                new Color(0.08f, 0.08f, 0.12f), new Color(0.25f, 0.18f, 0.4f), new Color(0.55f, 0.55f, 0.62f), new Color(0.75f, 0.45f, 1f), 0.97f, 0.9f, true);
            Look("akane_fire", HairStyle.Twintails, WeaponKind.Fans, CombatStyle.Technical, MotionStyle.Light,
                new Color(0.95f, 0.9f, 0.9f), new Color(0.9f, 0.2f, 0.3f), new Color(0.95f, 0.35f, 0.25f), new Color(1f, 0.6f, 0.2f), 0.98f, 0.92f, false, true, false, false, true, new Color(1f, 0.8f, 0.3f));
            Look("tsukasa_storm", HairStyle.Messy, WeaponKind.Bow, CombatStyle.Ranged, MotionStyle.Stoic,
                new Color(0.15f, 0.15f, 0.2f), new Color(0.55f, 0.5f, 0.2f), new Color(0.85f, 0.85f, 0.9f), new Color(1f, 0.95f, 0.4f), 1.08f, 1f, false, true);
            Look("mizuki_tide", HairStyle.Long, WeaponKind.TwinBlades, CombatStyle.Swift, MotionStyle.Aggressive,
                new Color(0.1f, 0.15f, 0.25f), new Color(0.1f, 0.45f, 0.6f), new Color(0.1f, 0.3f, 0.45f), new Color(0.4f, 0.9f, 1f), 1f, 0.94f, true, true);
            // Premium test designs (the builder uses its own palettes; these drive combat style, motion and UI colours).
            Look("kaito_gale", HairStyle.Spiky, WeaponKind.TwinBlades, CombatStyle.Swift, MotionStyle.Light,
                new Color(0.95f, 0.96f, 0.93f), new Color(0.13f, 0.58f, 0.34f), new Color(0.1f, 0.3f, 0.19f), new Color(0.55f, 1f, 0.6f), 1f, 1f, true, false, false, false, false, new Color(0.96f, 0.76f, 0.3f));
            Look("oboro_iron", HairStyle.Short, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Confident,
                new Color(0.1f, 0.08f, 0.09f), new Color(0.66f, 0.08f, 0.09f), new Color(0.12f, 0.08f, 0.08f), new Color(1f, 0.5f, 0.12f), 1.1f, 1.2f, false, true, false, true, false, new Color(1f, 0.5f, 0.12f));
            Look("shion_storm", HairStyle.Long, WeaponKind.Staff, CombatStyle.Ranged, MotionStyle.Graceful,
                new Color(0.1f, 0.12f, 0.32f), new Color(0.6f, 0.32f, 1f), new Color(0.85f, 0.8f, 0.97f), new Color(1f, 0.88f, 0.35f), 1f, 0.92f, false, false, false, false, false, new Color(1f, 0.84f, 0.22f));
            // The six Design Bible reference characters.
            Look("tobi_kazami", HairStyle.Spiky, WeaponKind.Katana, CombatStyle.Swift, MotionStyle.Light,
                new Color(0.2f, 0.25f, 0.3f), new Color(0.62f, 0.86f, 0.2f), new Color(0.95f, 0.62f, 0.22f), new Color(0.5f, 1f, 0.45f), 0.92f, 0.95f, true, false, false, false, false, new Color(1f, 0.52f, 0.12f));
            Look("bunta_okuyama", HairStyle.Short, WeaponKind.Greatsword, CombatStyle.Heavy, MotionStyle.Confident,
                new Color(0.34f, 0.22f, 0.14f), new Color(0.72f, 0.32f, 0.16f), new Color(0.2f, 0.14f, 0.1f), new Color(1f, 0.55f, 0.15f), 1.28f, 1.35f, false, false, false, true, false, new Color(0.86f, 0.66f, 0.28f));
            Look("sayo_mikage", HairStyle.Ponytail, WeaponKind.Bow, CombatStyle.Ranged, MotionStyle.Steady,
                new Color(0.94f, 0.9f, 0.8f), new Color(0.14f, 0.42f, 0.28f), new Color(0.36f, 0.2f, 0.14f), new Color(1f, 0.9f, 0.3f), 1.08f, 0.92f, false, true, false, false, false, new Color(0.78f, 0.12f, 0.2f));
            Look("nene_hanabusa", HairStyle.Bob, WeaponKind.Staff, CombatStyle.Healer, MotionStyle.Graceful,
                new Color(0.98f, 0.95f, 0.88f), new Color(0.72f, 0.62f, 0.92f), new Color(0.98f, 0.78f, 0.55f), new Color(1f, 0.85f, 0.5f), 0.84f, 1f, false, false, false, false, true, new Color(1f, 0.66f, 0.55f));
            Look("nagi_kurokiri", HairStyle.Hood, WeaponKind.TwinBlades, CombatStyle.Technical, MotionStyle.Sly,
                new Color(0.17f, 0.17f, 0.21f), new Color(0.4f, 0.2f, 0.42f), new Color(0.62f, 0.66f, 0.72f), new Color(0.72f, 1f, 0.3f), 0.9f, 0.9f, true, false, false, false, false, new Color(0.7f, 1f, 0.25f));
            Look("seiran_mizuchi", HairStyle.Long, WeaponKind.Spear, CombatStyle.Balanced, MotionStyle.Stoic,
                new Color(0.84f, 0.87f, 0.92f), new Color(0.1f, 0.2f, 0.45f), new Color(0.14f, 0.3f, 0.55f), new Color(0.3f, 0.9f, 0.95f), 1.18f, 1f, false, true, false, false, false, new Color(0.3f, 0.9f, 0.95f));
            // Skin tones: a varied, friendly cast.
            Color porcelain = new Color(0.99f, 0.88f, 0.8f), light = new Color(0.97f, 0.81f, 0.68f), medium = new Color(0.88f, 0.67f, 0.5f),
                tan = new Color(0.78f, 0.56f, 0.4f), brown = new Color(0.6f, 0.41f, 0.28f), deep = new Color(0.44f, 0.29f, 0.2f);
            Skin("ren_initiate", light); Skin("ren_sundance", light);
            Skin("sora_initiate", medium);
            Skin("kiba_initiate", tan);
            Skin("hana_healer", porcelain);
            Skin("tetsu_guard", brown);
            Skin("homura_pillar", medium); Skin("homura_lastflame", medium);
            Skin("mina_ember", porcelain);
            Skin("rokuro_hunter", deep);
            Skin("genji_ronin", tan);
            Skin("yui_tide", light);
            Skin("raiga_pillar", brown);
            Skin("kuroe_moon", porcelain);
            Skin("seren_starfall", tan);
            Skin("taro_rock", brown); Skin("nami_bubble", light); Skin("koji_ember", medium); Skin("yuna_gale", porcelain);
            Skin("daigo_stone", deep); Skin("hotaru_light", light); Skin("kenta_spear", tan); Skin("rin_shadow", porcelain);
            Skin("akane_fire", light); Skin("tsukasa_storm", brown); Skin("mizuki_tide", medium);
            Skin("garou_onyx", brown);
            Skin("kaito_gale", light); Skin("oboro_iron", tan); Skin("shion_storm", porcelain);
            Skin("tobi_kazami", light); Skin("bunta_okuyama", brown); Skin("sayo_mikage", medium);
            Skin("nene_hanabusa", porcelain); Skin("nagi_kurokiri", tan); Skin("seiran_mizuchi", light);
            // Mina's blade burns violet: she fights with dark flames.
            var mina = Characters.Find(x => x.id == "mina_ember");
            if (mina != null) mina.element = Element.Dark;
        }

        static void Skin(string id, Color c)
        {
            var d = Characters.Find(x => x.id == id) ?? Npcs.Find(x => x.id == id);
            if (d != null) d.skinTone = c;
        }

        static void NpcLooks()
        {
            Look("npc_tessai", HairStyle.Bun, WeaponKind.Cane, CombatStyle.Balanced, MotionStyle.Stoic,
                new Color(0.35f, 0.38f, 0.45f), new Color(0.5f, 0.52f, 0.58f), new Color(0.95f, 0.95f, 0.97f), new Color(0.8f, 0.8f, 0.85f), 0.95f);
            Look("npc_villager", HairStyle.StrawHat, WeaponKind.Cane, CombatStyle.Balanced, MotionStyle.Steady,
                new Color(0.45f, 0.35f, 0.25f), new Color(0.6f, 0.5f, 0.35f), new Color(0.15f, 0.1f, 0.08f), new Color(0.5f, 0.4f, 0.3f), 0.95f, 1f, false, false, false, false, false, new Color(0.85f, 0.72f, 0.42f));
            Look("npc_villager2", HairStyle.Cap, WeaponKind.Cane, CombatStyle.Balanced, MotionStyle.Light,
                new Color(0.3f, 0.35f, 0.45f), new Color(0.7f, 0.4f, 0.4f), new Color(0.12f, 0.08f, 0.06f), new Color(0.5f, 0.4f, 0.3f), 0.9f, 1f, false, false, false, false, false, new Color(0.7f, 0.25f, 0.2f));
            Look("npc_soldier", HairStyle.Crest, WeaponKind.Spear, CombatStyle.Balanced, MotionStyle.Stoic,
                new Color(0.28f, 0.32f, 0.42f), new Color(0.2f, 0.3f, 0.65f), new Color(0.75f, 0.75f, 0.8f), new Color(0.8f, 0.8f, 0.85f), 1.05f, 1.1f, false, false, false, true);
            Look("npc_chancellor", HairStyle.Hood, WeaponKind.Cane, CombatStyle.Balanced, MotionStyle.Sly,
                new Color(0.15f, 0.1f, 0.2f), new Color(0.45f, 0.2f, 0.55f), new Color(0.45f, 0.2f, 0.55f), new Color(0.6f, 0.3f, 0.8f), 1.1f, 0.95f, false, true);
            Look("npc_merchant", HairStyle.Cap, WeaponKind.Cane, CombatStyle.Balanced, MotionStyle.Steady,
                new Color(0.4f, 0.3f, 0.2f), new Color(0.8f, 0.6f, 0.2f), new Color(0.2f, 0.15f, 0.1f), new Color(0.5f, 0.4f, 0.3f), 0.95f, 1.2f, false, false, false, false, false, new Color(0.8f, 0.6f, 0.2f));
            Skin("npc_tessai", new Color(0.86f, 0.68f, 0.52f));
            Skin("npc_villager", new Color(0.62f, 0.43f, 0.3f));
            Skin("npc_villager2", new Color(0.97f, 0.82f, 0.7f));
            Skin("npc_soldier", new Color(0.45f, 0.3f, 0.2f));
            Skin("npc_chancellor", new Color(0.93f, 0.8f, 0.7f));
            Skin("npc_merchant", new Color(0.8f, 0.6f, 0.44f));
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

        static EnemyDefinition GetEnemyRaw(string id)
        {
            foreach (var e in Enemies) if (e.id == id) return e;
            return new EnemyDefinition();
        }

        static void BuildEnemies()
        {
            Enemies.Add(new EnemyDefinition { id = "grunt", form = "ghoul", weakness = "Weak to everything — clear them fast with sweeping combos.", displayName = "Lesser Demon", archetype = EnemyArchetype.Normal, element = Element.Beast,
                baseHp = 2200, baseAtk = 160, baseDef = 100, moveSpeed = 3.2f, attackRange = 1.9f, attackCooldown = 2.2f, windup = 0.6f,
                description = "The eclipse's foot soldiers. Weak alone, dangerous in packs." });
            Enemies.Add(new EnemyDefinition { id = "runner", form = "stalker", weakness = "Lunges in straight lines: sidestep, then punish.", displayName = "Swift Stalker", archetype = EnemyArchetype.Fast, element = Element.Thunder,
                baseHp = 1500, baseAtk = 135, baseDef = 80, moveSpeed = 5.8f, attackRange = 3.2f, attackCooldown = 1.8f, windup = 0.4f,
                scale = 0.85f, bodyColor = new Color(0.3f, 0.25f, 0.08f), accentColor = new Color(1f, 0.85f, 0.2f), description = "Lunges fast and dodges your swings." });
            Enemies.Add(new EnemyDefinition { id = "brute", form = "beast", artKey = "blood_beast", weakness = "Charges in a straight line and stumbles after — dodge sideways, then strike its flank.", displayName = "Ember Beast", archetype = EnemyArchetype.Tank, element = Element.Flame,
                baseHp = 6500, baseAtk = 250, baseDef = 400, moveSpeed = 2.1f, attackRange = 2.6f, attackCooldown = 3f, windup = 0.95f,
                poise = 6f, scale = 1.5f, radius = 0.9f, bodyColor = new Color(0.35f, 0.15f, 0.05f), accentColor = new Color(1f, 0.45f, 0.1f), description = "Slow, armoured, hits like a landslide." });
            Enemies.Add(new EnemyDefinition { id = "spitter", form = "void", artKey = "void_demon", weakness = "Floats out of reach and fires dark orbs — dash in; it is fragile up close.", displayName = "Void Demon", archetype = EnemyArchetype.Ranged, element = Element.Water,
                baseHp = 1800, baseAtk = 170, baseDef = 90, moveSpeed = 2.8f, attackRange = 10f, attackCooldown = 2.8f, windup = 0.65f,
                bodyColor = new Color(0.08f, 0.18f, 0.3f), accentColor = new Color(0.3f, 0.7f, 1f), description = "Keeps its distance and spits dark orbs." });
            Enemies.Add(new EnemyDefinition { id = "elite", form = "hunter", weakness = "Leaps before it strikes: move when it jumps.", displayName = "Crescent Hunter", archetype = EnemyArchetype.Elite, element = Element.Dark,
                baseHp = 9000, baseAtk = 290, baseDef = 300, moveSpeed = 3.8f, attackRange = 2.6f, attackCooldown = 2.4f, windup = 0.7f,
                poise = 10f, scale = 1.25f, radius = 0.75f, bodyColor = new Color(0.2f, 0.05f, 0.25f), accentColor = new Color(0.85f, 0.2f, 1f), description = "Elite hunter. Leaps across the field." });
            Enemies.Add(new EnemyDefinition { id = "shadow_demon", form = "shadow", artKey = "shadow_demon", weakness = "Fast claws but thin body: parry its lunge and it staggers.", displayName = "Shadow Demon", archetype = EnemyArchetype.Normal, element = Element.Dark,
                baseHp = 2600, baseAtk = 175, baseDef = 110, moveSpeed = 3.6f, attackRange = 2f, attackCooldown = 2f, windup = 0.55f,
                bodyColor = new Color(0.08f, 0.06f, 0.12f), accentColor = new Color(0.5f, 0.3f, 0.9f), description = "Born from the forest's shadows." });
            Enemies.Add(new EnemyDefinition { id = "forest_beast", form = "forest", artKey = "forest_demon", weakness = "Slow root slams. Flame breathing burns its bark.", displayName = "Forest Demon", archetype = EnemyArchetype.Tank, element = Element.Earth,
                baseHp = 7000, baseAtk = 240, baseDef = 330, moveSpeed = 2.6f, attackRange = 2.6f, attackCooldown = 2.8f, windup = 0.85f,
                poise = 6f, scale = 1.6f, radius = 0.95f, bodyColor = new Color(0.22f, 0.28f, 0.12f), accentColor = new Color(0.6f, 0.9f, 0.3f), description = "A corrupted guardian of the woods." });
            Enemies.Add(new EnemyDefinition { id = "demon_warrior", form = "bone", artKey = "bone_warrior", weakness = "Huge overhead cleaver: guard is useless — dodge, then hit its back. Light damage cracks its bones.", displayName = "Bone Warrior", archetype = EnemyArchetype.Elite, element = Element.Earth,
                baseHp = 8000, baseAtk = 280, baseDef = 280, moveSpeed = 3.6f, attackRange = 2.6f, attackCooldown = 2.3f, windup = 0.65f,
                poise = 8f, scale = 1.2f, radius = 0.7f, bodyColor = new Color(0.35f, 0.08f, 0.06f), accentColor = new Color(1f, 0.3f, 0.15f), description = "A demon trained in human swordplay." });
            Enemies.Add(new EnemyDefinition { id = "frost_oni", form = "oni", weakness = "Ice armour shatters under Flame.", displayName = "Frost Oni", archetype = EnemyArchetype.Tank, element = Element.Water,
                baseHp = 7500, baseAtk = 260, baseDef = 380, moveSpeed = 2.3f, attackRange = 2.7f, attackCooldown = 2.8f, windup = 0.9f,
                poise = 7f, scale = 1.55f, radius = 0.9f, bodyColor = new Color(0.55f, 0.7f, 0.85f), accentColor = new Color(0.8f, 0.95f, 1f), description = "Its breath freezes the air." });
            Enemies.Add(new EnemyDefinition { id = "ice_wraith", form = "wraith", weakness = "Keeps its distance: close the gap with a dash attack.", displayName = "Ice Wraith", archetype = EnemyArchetype.Ranged, element = Element.Water,
                baseHp = 2000, baseAtk = 190, baseDef = 90, moveSpeed = 3f, attackRange = 10f, attackCooldown = 2.5f, windup = 0.6f,
                bodyColor = new Color(0.6f, 0.75f, 0.9f), accentColor = new Color(0.7f, 0.9f, 1f), description = "Hurls shards of ice from afar." });
            Enemies.Add(new EnemyDefinition { id = "lava_imp", form = "imp", weakness = "Comes in packs — spin skills clear them.", displayName = "Lava Imp", archetype = EnemyArchetype.Fast, element = Element.Flame,
                baseHp = 1900, baseAtk = 170, baseDef = 90, moveSpeed = 6f, attackRange = 3.2f, attackCooldown = 1.7f, windup = 0.38f,
                scale = 0.8f, bodyColor = new Color(0.3f, 0.08f, 0.02f), accentColor = new Color(1f, 0.5f, 0.1f), description = "Fast, burning, and always in groups." });
            Enemies.Add(new EnemyDefinition { id = "corrupted_knight", form = "knight", artKey = "demon_knight", weakness = "Shield blocks frontal hits: circle around or use a heavy attack to break its guard.", displayName = "Demon Knight", archetype = EnemyArchetype.Elite, element = Element.Dark,
                baseHp = 11000, baseAtk = 320, baseDef = 380, moveSpeed = 3.4f, attackRange = 2.8f, attackCooldown = 2.4f, windup = 0.7f,
                poise = 12f, scale = 1.35f, radius = 0.8f, bodyColor = new Color(0.12f, 0.12f, 0.16f), accentColor = new Color(0.7f, 0.2f, 0.9f), description = "A royal knight swallowed by the eclipse." });
            Enemies.Add(new EnemyDefinition { id = "castle_sentinel", form = "sentinel", weakness = "Enormous reach, slow recovery. Light breathing cuts through its armour.", displayName = "Eclipse Sentinel", archetype = EnemyArchetype.Tank, element = Element.Dark,
                baseHp = 12000, baseAtk = 300, baseDef = 480, moveSpeed = 2.2f, attackRange = 3f, attackCooldown = 3f, windup = 0.9f,
                poise = 12f, scale = 1.8f, radius = 1f, bodyColor = new Color(0.1f, 0.08f, 0.12f), accentColor = new Color(0.9f, 0.1f, 0.3f), description = "Guards the Demon Lord's halls." });
            Enemies.Add(new EnemyDefinition { id = "flame_beast", form = "lion", artKey = "flame_beast", weakness = "Its mane flares before a fire breath — Water breathing douses it.",
                displayName = "Flame Beast", archetype = EnemyArchetype.Tank, element = Element.Flame,
                baseHp = 8500, baseAtk = 280, baseDef = 320, moveSpeed = 3.4f, attackRange = 2.8f, attackCooldown = 2.5f, windup = 0.75f,
                poise = 8f, scale = 1.45f, radius = 0.95f, bodyColor = new Color(0.12f, 0.08f, 0.07f), accentColor = new Color(1f, 0.45f, 0.08f), description = "A lion of cooled lava with a burning mane." });
            Enemies.Add(new EnemyDefinition { id = "dummy", form = "dummy", weakness = "It doesn't mind.", displayName = "Training Dummy", archetype = EnemyArchetype.Normal, element = Element.Beast,
                baseHp = 30000, baseAtk = 1, baseDef = 50, moveSpeed = 0.8f, attackRange = 1.5f, attackCooldown = 99f, windup = 1f,
                bodyColor = new Color(0.6f, 0.5f, 0.35f), accentColor = new Color(0.8f, 0.7f, 0.5f), description = "Hit it. It doesn't mind." });

            // Distinct behaviours: who hides, who calls for help, who charges.
            GetEnemyRaw("shadow_demon").ambush = true;
            GetEnemyRaw("runner").ambush = true;
            GetEnemyRaw("spitter").summonId = "grunt";
            GetEnemyRaw("forest_beast").summonId = "shadow_demon";
            GetEnemyRaw("castle_sentinel").summonId = "lava_imp";
            GetEnemyRaw("brute").charger = true;
            GetEnemyRaw("flame_beast").charger = true;
            GetEnemyRaw("frost_oni").charger = true;
            GetEnemyRaw("dummy").enrageAt = 0f;

            // ---- Bosses ----
            Boss("boss_gorvath", "Gorvath", "The Horned Butcher of Kiriha", "thousandarm", Element.Beast, 22000, 230, 240, 2.4f, 2f,
                new Color(0.35f, 0.18f, 0.12f), new Color(1f, 0.5f, 0.2f), new[] { 0.5f }, "The first of the Demon Lord's servants to reach Kiriha.");
            Boss("boss_thousandarm", "The Forest Demon", "Heart of the Shadow Forest", "thousandarm", Element.Earth, 34000, 260, 280, 2.1f, 2.2f,
                new Color(0.25f, 0.35f, 0.18f), new Color(0.8f, 1f, 0.4f), new[] { 0.5f }, "Roots and arms without number. Once it protected the forest; now the eclipse rots it from within.");
            Boss("boss_hyoga", "Hyoga", "The Frost Oni", "goken", Element.Water, 36000, 300, 330, 1.35f, 1.3f,
                new Color(0.6f, 0.75f, 0.9f), new Color(0.8f, 0.95f, 1f), new[] { 0.5f }, "It has guarded the mountain pass for a century.");
            Boss("boss_chancellor", "Chancellor Mikado", "The Masked Servant of the Eclipse", "thousandarm", Element.Dark, 45000, 300, 320, 1.6f, 1.6f,
                new Color(0.2f, 0.08f, 0.28f), new Color(0.85f, 0.3f, 1f), new[] { 0.5f }, "Advisor to the King for twenty years. Demon for much longer.");
            Boss("boss_goken", "Goken", "The Crimson Fist", "goken", Element.Flame, 60000, 360, 360, 1.45f, 1.2f,
                new Color(0.85f, 0.55f, 0.5f), new Color(0.35f, 0.6f, 1f), new[] { 0.7f, 0.3f }, "An Upper Crescent. He fights for the joy of it — and knows Ren's name.");
            Boss("boss_seal_guardian", "The Ancient Guardian", "Keeper of the Forgotten Temple", "goken", Element.Light, 62000, 350, 400, 1.7f, 1.3f,
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
                id = id, displayName = name, form = BossForm(id), artKey = BossArt(id), weakness = BossWeakness(id), bossTitle = title, archetype = EnemyArchetype.Boss, element = el, baseHp = hp, baseAtk = atk, baseDef = def,
                moveSpeed = style == "goken" ? 4.2f : 2.8f, attackRange = style == "goken" ? 2.8f : 4f, attackCooldown = cooldown,
                windup = style == "goken" ? 0.6f : 0.8f, poise = 70f, scale = scale, radius = 0.6f * scale + 0.1f, bossStyle = style,
                phaseThresholds = phases, bodyColor = body, accentColor = accent, description = desc
            });
        }

        static string BossForm(string id)
        {
            switch (id)
            {
                case "boss_gorvath": return "brute";
                case "boss_thousandarm": return "forest";
                case "boss_hyoga": return "oni";
                case "boss_chancellor": return "void";
                case "boss_goken": return "general";
                case "boss_seal_guardian": return "guardian";
                case "boss_veyrath": return "lord";
                default: return "general";
            }
        }

        static string BossArt(string id)
        {
            switch (id)
            {
                case "boss_thousandarm": return "forest_demon";
                case "boss_seal_guardian": return "ancient_guardian";
                case "boss_morgrath": case "boss_vex": case "boss_nyx": return "demon_general";
                case "boss_veyrath": return "veyrath_true";
                default: return null;
            }
        }

        static string BossWeakness(string id)
        {
            switch (id)
            {
                case "boss_gorvath": return "Big wind-ups: dodge through the swing and punish.";
                case "boss_thousandarm": return "Flame burns its bark. Watch for hands bursting from the ground.";
                case "boss_hyoga": return "Flame melts its armour. Stay close to avoid the ice volleys.";
                case "boss_chancellor": return "Fragile when casting — close the distance.";
                case "boss_goken": return "After Annihilation he is exhausted: BREAK deals +50% damage.";
                case "boss_seal_guardian": return "Dark breathing cracks the runes. Step inside its ring-blade.";
                case "boss_veyrath": return "Light breathing wounds the eclipse. Survive the transformation — your Awakening follows.";
                default: return "Learn the telegraphs; parry the fast strikes.";
            }
        }

        // ------------------------------------------------------------------ Equipment

        static void Eq(string id, string name, EquipSlot slot, int rarity, StatBlock b, StatBlock per, string desc)
        {
            Equipment.Add(new EquipmentDefinition { id = id, displayName = name, slot = slot, rarity = rarity - 1, baseBonus = b, perLevel = per, description = desc });
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
            Eq("acc_bell", "Spirit Bell", EquipSlot.Accessory, 3, new StatBlock(350, 20, 30), new StatBlock(30, 3, 4), "HP +350. Rings softly near danger.");
            Eq("acc_beads", "Jade Bead Bracelet", EquipSlot.Accessory, 4, new StatBlock(500, 60, 120), new StatBlock(50, 6, 12), "A sturdy guard charm.");
            Eq("acc_ribbon", "Swift Ribbon", EquipSlot.Accessory, 4, new StatBlock(0, 110, 0, 0.03f, 0f, 0.5f), new StatBlock(0, 12, 0, 0.001f, 0f, 0.02f), "SPD +0.5, CRIT +3%.");
            Eq("acc_suncrest", "Sun Crest Pin", EquipSlot.Accessory, 6, new StatBlock(600, 260, 120, 0.06f, 0.25f, 0f, 0.2f), new StatBlock(55, 22, 10, 0.002f, 0.01f, 0f, 0.01f), "Special damage +20%, CRIT DMG +25%.");
            Eq("sword_bamboo", "Bamboo Practice Blade", EquipSlot.Sword, 3, new StatBlock(0, 120, 0, 0.01f), new StatBlock(0, 16, 0), "Light, quick and a little splintery.");
            Eq("haori_ember", "Flameguard Haori", EquipSlot.Haori, 6, new StatBlock(1800, 120, 380, 0.03f), new StatBlock(180, 12, 34, 0.001f), "Woven with fire-thread. Burns demons that grab it.");
            // Mythic gear.
            Eq("sword_moonfall", "Moonfall Edge", EquipSlot.Sword, 7, new StatBlock(600, 1300, 150, 0.1f, 0.4f, 0.2f, 0.2f), new StatBlock(50, 95, 12, 0.003f, 0.015f, 0.01f, 0.01f), "Mythic. A blade forged from a fallen piece of the moon.");
            Eq("haori_starweave", "Starweave Mantle", EquipSlot.Haori, 7, new StatBlock(3200, 260, 620, 0.05f, 0.1f, 0.3f, 0.1f), new StatBlock(260, 20, 50, 0.002f, 0.005f, 0.01f, 0.005f), "Mythic. Stitched from starlight; it glows softly in the dark.");
            Eq("acc_phoenixheart", "Phoenix Heart", EquipSlot.Accessory, 7, new StatBlock(1500, 500, 260, 0.08f, 0.3f, 0.2f, 0.3f), new StatBlock(110, 38, 20, 0.003f, 0.012f, 0.01f, 0.015f), "Mythic. Still warm. Specials hit 30% harder.");
            // Abyssal set: only drops in the Lv 70+ Abyssal Hunts.
            Eq("sword_abyss", "Abyssal Fang", EquipSlot.Sword, 6, new StatBlock(300, 1100, 80, 0.09f, 0.35f, 0.1f, 0.15f), new StatBlock(30, 85, 8, 0.003f, 0.012f), "Legendary. Hunted from the Abyss; it hungers for demons.");
            Eq("haori_abyss", "Abyssal Shroud", EquipSlot.Haori, 6, new StatBlock(2600, 180, 520, 0.04f, 0.1f, 0.2f), new StatBlock(220, 15, 44, 0.002f), "Legendary. Woven from the dark between stars.");
            Eq("acc_abyss_eye", "Eye of the Abyss", EquipSlot.Accessory, 6, new StatBlock(900, 380, 180, 0.07f, 0.3f, 0.2f, 0.2f), new StatBlock(70, 30, 14, 0.003f, 0.012f, 0.01f, 0.01f), "Legendary. It watches every demon at once.");
            Eq("acc_dragonscale", "Dragon Scale Amulet", EquipSlot.Accessory, 7, new StatBlock(1200, 420, 240, 0.08f, 0.35f, 0.3f, 0.25f), new StatBlock(90, 34, 18, 0.003f, 0.015f, 0.01f, 0.012f), "Mythic. Every stat, all at once.");
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
                    // Snow toned down to a soft blue-grey: glowing attacks must stay readable on it.
                    t.ground = new Color(0.62f, 0.68f, 0.78f); t.groundAccent = new Color(0.5f, 0.56f, 0.68f);
                    t.sky = new Color(0.55f, 0.65f, 0.8f); t.fog = new Color(0.66f, 0.72f, 0.84f); t.fogStart = 24f; t.fogEnd = 80f;
                    t.lantern = new Color(1f, 0.8f, 0.5f); t.foliage = new Color(0.15f, 0.3f, 0.25f); t.petals = false; t.night = false;
                    t.sun = new Color(1f, 0.97f, 0.9f); t.sunIntensity = 1.05f;
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
                theme = r != null ? r.theme : new ArenaTheme(), killObjective = 0, parTime = 220f + level * 2f, timeLimit = 600f
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

        public static readonly List<EventDefinition> Events = new List<EventDefinition>();

        /// <summary>The open-world village: walk around freely.</summary>
        public static MissionDefinition OpenWorld
        {
            get
            {
                EnsureBuilt();
                if (openWorld == null)
                {
                    openWorld = new MissionDefinition
                    {
                        id = "OW", name = "Kiriha Village", type = MissionType.Training, regionId = "village", training = true, openWorld = true,
                        storyText = "Walk around the village, talk to everyone, find the hidden chests.", theme = OpenWorldBuilder.Theme(), timeLimit = 999999f
                    };
                }
                return openWorld;
            }
        }
        static MissionDefinition openWorld;

        public static EventDefinition GetEvent(string id) { EnsureBuilt(); return Events.Find(e => e.id == id); }

        /// <summary>One event quest. Difficulty 0 Easy, 1 Medium, 2 Hard.</summary>
        static MissionDefinition Quest(EventDefinition ev, int n, int difficulty, string name, int level, string story, params WaveDefinition[] waves)
        {
            var m = M(null, ev.id + "-" + n, MissionType.Event, ev.regionId, name, level, story, waves);
            m.eventId = ev.id;
            m.theme = EventTheme(ev);
            m.difficulty = difficulty;
            m.firstClearRewards = new RewardBundle { crystals = difficulty == 0 ? 50 : difficulty == 1 ? 100 : 200 };
            m.rewards.coins += difficulty * 1500;
            if (ev.quests.Count > 0) m.requiresMissionId = ev.quests[ev.quests.Count - 1].id;
            ev.quests.Add(m);
            ExtraMissions.Add(m);
            return m;
        }

        /// <summary>
        /// Each event's own look (a copy of its region's, so it never touches the story): the village festival is a
        /// calm lantern-lit dusk instead of the burning night raid, the others keep their region's mood.
        /// </summary>
        static ArenaTheme EventTheme(EventDefinition ev)
        {
            var src = GetRegion(ev.regionId) != null ? GetRegion(ev.regionId).theme : new ArenaTheme();
            var t = new ArenaTheme
            {
                ground = src.ground, groundAccent = src.groundAccent, sky = src.sky, fog = src.fog, lantern = src.lantern, foliage = src.foliage,
                petals = src.petals, kind = src.kind, night = src.night, sun = src.sun, sunIntensity = src.sunIntensity, fogStart = src.fogStart, fogEnd = src.fogEnd,
                burning = false
            };
            switch (ev.id)
            {
                case "E1": // Festival dusk: green fields, warm lanterns, a violet sky.
                    t.ground = new Color(0.34f, 0.5f, 0.28f); t.groundAccent = new Color(0.55f, 0.45f, 0.32f);
                    t.sky = new Color(0.32f, 0.22f, 0.45f); t.fog = new Color(0.45f, 0.35f, 0.55f); t.fogStart = 35f; t.fogEnd = 95f;
                    t.lantern = new Color(1f, 0.6f, 0.25f); t.foliage = new Color(1f, 0.7f, 0.8f); t.petals = true;
                    t.sun = new Color(1f, 0.75f, 0.6f); t.sunIntensity = 1f; t.night = true;
                    break;
                case "E4": // Tournament day in the capital.
                    t.night = false; t.sky = new Color(0.5f, 0.72f, 0.95f); t.fog = new Color(0.7f, 0.8f, 0.92f); t.sunIntensity = 1.15f; t.sun = new Color(1f, 0.96f, 0.88f);
                    break;
                case "E5":
                    t.burning = src.burning;
                    break;
                case "E7": // The treasury at golden hour.
                    t.night = false; t.sky = new Color(0.98f, 0.8f, 0.5f); t.fog = new Color(0.95f, 0.8f, 0.6f); t.sun = new Color(1f, 0.85f, 0.6f); t.sunIntensity = 1.1f;
                    break;
            }
            // Keep the far fog from swallowing everything into black.
            t.fogStart = Mathf.Max(t.fogStart, 30f);
            t.fogEnd = Mathf.Max(t.fogEnd, 85f);
            return t;
        }

        /// <summary>Five events, five quests each (Easy, Easy, Medium, Medium, Hard), each with its own little story.</summary>
        /// <summary>
        /// Environment quality test: one mission per prototype world (Forest, Snow Mountain, Volcano), each a full
        /// route from the gate through a trail, a bridge, ruins or a shrine and a demon camp to a boss arena.
        /// </summary>
        public static MissionDefinition PrototypeMission(string env, int level)
        {
            EnsureBuilt();
            level = Mathf.Clamp(level, 3, 80);
            MissionDefinition m;
            if (env == "collision")
            {
                m = M(null, "PROTO_COLLISION", MissionType.Training, null, "Collision Test", 1,
                    "Walk into the tree, the pole, the rock, the wall and the fence, then around them, and try the narrow gap. The checklist ticks each one off.",
                    Wave("dummy", 2));
                m.training = true;
                m.collisionTest = true;
                m.theme = Theme(EnvironmentKind.Forest);
                m.regionId = null;
                m.timeLimit = 9999f;
                m.rewards = new RewardBundle();
                m.firstClearRewards = new RewardBundle();
                return m;
            }
            switch (env)
            {
                case "snow":
                    m = M(null, "PROTO_SNOW", MissionType.Side, null, "Snow Mountain (Prototype)", level,
                        "Climb past the pine ridge and the frozen river to the shrine at the summit, where the Frost Oni waits.",
                        Wave("runner", 4, "ice_wraith", 1), Wave("frost_oni", 1, "runner", 3), Wave("ice_wraith", 2, "runner", 3), Wave("frost_oni", 2, "ice_wraith", 2));
                    m.theme = Theme(EnvironmentKind.Mountain);
                    m.route.AddRange(new[] { "Mountain Trailhead", "Pine Ridge", "Frozen River Bridge", "Mountain Shrine", "Ice Cave Camp" });
                    m.bossId = "boss_hyoga";
                    m.arenaName = "Frozen Summit Shrine";
                    break;
                case "volcano":
                    m = M(null, "PROTO_VOLCANO", MissionType.Side, null, "Volcano (Prototype)", level,
                        "Cross the ashen fields and the lava river, through the ruined fire temple and the demons' forge, to the crater throne.",
                        Wave("lava_imp", 5, "grunt", 2), Wave("flame_beast", 1, "lava_imp", 3), Wave("demon_warrior", 1, "lava_imp", 4), Wave("brute", 2, "demon_warrior", 1));
                    m.theme = Theme(EnvironmentKind.DemonLand);
                    m.route.AddRange(new[] { "Ashen Gate", "Basalt Path", "Lava River Bridge", "Ruined Fire Temple", "Demon Forge Camp" });
                    m.bossId = "boss_goken";
                    m.arenaName = "Crater Throne";
                    break;
                default:
                    m = M(null, "PROTO_FOREST", MissionType.Side, null, "Forest (Prototype)", level,
                        "Follow the old shrine road through the forest, over the stream and past the moss ruins, to the Great Tree.",
                        Wave("grunt", 4, "shadow_demon", 1), Wave("shadow_demon", 3, "grunt", 2), Wave("forest_beast", 1, "spitter", 2, "grunt", 2), Wave("forest_beast", 2, "shadow_demon", 2));
                    m.theme = Theme(EnvironmentKind.Forest);
                    m.route.AddRange(new[] { "Forest Gate", "Mossy Trail", "Old Stream Bridge", "Moss Ruins", "Demon Camp" });
                    m.bossId = "boss_thousandarm";
                    m.arenaName = "Heart of the Great Tree";
                    break;
            }
            m.prototypeEnv = env == "snow" || env == "volcano" ? env : "forest";
            m.regionId = null;
            m.timeLimit = 900f;
            return m;
        }

        /// <summary>The three co-op gates in Kiriha's plaza (name, tagline, demon strength).</summary>
        public static readonly string[] CoopGateNames = { "Gate of Embers", "Gate of Frost", "Gate of the Abyss" };
        public static readonly string[] CoopGateTiers = { "HARD", "EXPERT", "NIGHTMARE" };
        public static readonly float[] CoopGatePower = { 3f, 4.5f, 6f };

        /// <summary>
        /// A co-op gate mission for a party of three: one of the new worlds, one more wave than usual, demons
        /// several times stronger than normal, and bigger rewards.
        /// </summary>
        public static MissionDefinition CoopMission(int tier, int level)
        {
            tier = Mathf.Clamp(tier, 0, 2);
            string[] env = { "forest", "snow", "volcano" };
            var m = PrototypeMission(env[tier], level + tier * 4);
            m.id = "COOP-" + (tier + 1);
            m.name = CoopGateNames[tier] + " (Co-op)";
            m.type = MissionType.Side;
            m.coopTier = tier;
            m.coopPower = CoopGatePower[tier];
            m.storyText = m.description = "A party of three steps through the " + CoopGateNames[tier] + ". The demons beyond are " + m.coopPower.ToString("0.#") + "x stronger than anywhere else — stay together.";
            m.waves.Add(tier == 0 ? Wave("shadow_demon", 3, "brute", 2) : tier == 1 ? Wave("frost_oni", 2, "ice_wraith", 3) : Wave("demon_warrior", 3, "flame_beast", 2));
            int total = 0;
            foreach (var w in m.waves) total += w.TotalCount;
            m.killObjective = Mathf.Max(3, Mathf.RoundToInt(total * 0.8f));
            float mul = 2f + tier;
            m.rewards.exp = Mathf.RoundToInt(m.rewards.exp * mul);
            m.rewards.coins = Mathf.RoundToInt(m.rewards.coins * mul);
            m.rewards.expScrolls += 2 + tier;
            m.rewards.skillScrolls += 1 + tier;
            m.rewards.ascensionOre += 1 + tier;
            m.firstClearRewards = new RewardBundle { crystals = 100 * (tier + 1) };
            m.timeLimit = 1200f;
            m.recommendedPower = Mathf.RoundToInt(m.recommendedPower * (1f + tier * 0.5f));
            return m;
        }

        static void BuildEvents()
        {
            Events.Clear();
            // 1 — Festival of Lanterns
            var ev = new EventDefinition { id = "E1", title = "Festival of Lanterns", subtitle = "Kiriha's summer festival — with uninvited guests", regionId = "village",
                accent = new Color(1f, 0.55f, 0.2f), bannerEnemy = "boss_gorvath", prizeText = "Mina Kaede + 300 diamonds",
                story = "Every summer, Kiriha floats a thousand paper lanterns down the river to thank the mountain spirits. This year the lanterns started glowing red on their own... and something in the woods is drawn to the light. Mayor Hoshi begs the slayers to keep the festival going — the village needs one happy night." };
            Events.Add(ev);
            Quest(ev, 1, 0, "Light the First Lantern", 4, "Imps are snuffing out the lanterns before the festival even starts. Chase them off.", Wave("grunt", 3), Wave("runner", 3));
            Quest(ev, 2, 0, "Snack Stall Scuffle", 6, "The dumpling stand is under siege. The cook refuses to leave his dumplings.", Wave("runner", 4, "grunt", 2), Wave("grunt", 4));
            Quest(ev, 3, 1, "The Lantern Thief", 10, "Someone is stealing red lanterns and carrying them into the forest. Follow the glow.", Wave("shadow_demon", 4), Wave("shadow_demon", 3, "spitter", 2));
            Quest(ev, 4, 1, "Fireworks over the River", 13, "The fireworks draw every demon for miles. Hold the bridge until the finale.", Wave("grunt", 5, "runner", 3), Wave("brute", 1, "spitter", 3));
            var q = Quest(ev, 5, 2, "The Lantern Oni", 18, "The thief is an old oni who was never invited to the festival. Now he wants all the light for himself.", Wave("brute", 2, "runner", 3));
            q.bossId = "boss_gorvath"; q.firstClearRewards.characterId = "mina_ember"; q.firstClearRewards.crystals = 300;

            // 2 — Hunt of the Silver Stag
            ev = new EventDefinition { id = "E2", title = "Hunt of the Silver Stag", subtitle = "A legend walks the Forest of Shadows", regionId = "forest",
                accent = new Color(0.45f, 0.9f, 0.6f), bannerEnemy = "boss_thousandarm", prizeText = "Rokuro + 400 diamonds",
                story = "Hunters say a silver stag appears once every hundred years, and whoever sees it will never lose their way again. Rokuro has waited his whole life. But the forest demons want the stag too — its antlers hold the moonlight that keeps the woods from turning." };
            Events.Add(ev);
            Quest(ev, 1, 0, "Tracks in the Moss", 8, "Silver hoofprints lead off the path. So do claw marks.", Wave("shadow_demon", 3), Wave("runner", 4));
            Quest(ev, 2, 0, "Hunter's Camp", 10, "Rokuro's camp is surrounded. Clear it so the hunt can begin.", Wave("shadow_demon", 4, "spitter", 1), Wave("shadow_demon", 4));
            Quest(ev, 3, 1, "The Whispering Grove", 14, "The trees here move when you're not looking. Some of them aren't trees.", Wave("forest_beast", 1, "shadow_demon", 3), Wave("forest_beast", 2));
            Quest(ev, 4, 1, "Moonlit Clearing", 17, "The stag drinks from the moon pool. Keep the hunters' traps from finding it first.", Wave("spitter", 3, "shadow_demon", 4), Wave("forest_beast", 2, "spitter", 2));
            q = Quest(ev, 5, 2, "Heart of the Old Wood", 22, "The Thousand-Arm Demon has cornered the stag. Save it — and the forest with it.", Wave("forest_beast", 2, "shadow_demon", 3));
            q.bossId = "boss_thousandarm"; q.firstClearRewards.characterId = "rokuro_hunter"; q.firstClearRewards.crystals = 400;

            // 3 — Frostbloom Expedition
            ev = new EventDefinition { id = "E3", title = "Frostbloom Expedition", subtitle = "A flower that only blooms in a blizzard", regionId = "mountain",
                accent = new Color(0.55f, 0.85f, 1f), bannerEnemy = "boss_hyoga", prizeText = "Genji Hayabusa + 500 diamonds",
                story = "Healers in Solmere need the frostbloom — a flower that cures any fever, found only at the top of Hakuro Pass during the worst storm of the year. The storm is here. So are the ice wraiths, who guard the flowers as if they were their own children." };
            Events.Add(ev);
            Quest(ev, 1, 0, "Base Camp", 12, "Set up camp before the storm hits. The wraiths are already circling.", Wave("ice_wraith", 3), Wave("ice_wraith", 4));
            Quest(ev, 2, 0, "Rope Bridge", 14, "Cross the old bridge — carefully. Something is chewing on the ropes.", Wave("ice_wraith", 3, "runner", 3), Wave("frost_oni", 1, "ice_wraith", 2));
            Quest(ev, 3, 1, "Whiteout", 18, "You can't see your own sword. You can hear the oni breathing.", Wave("frost_oni", 1, "ice_wraith", 4), Wave("frost_oni", 2));
            Quest(ev, 4, 1, "The Frozen Garden", 21, "Rows of blue flowers under the ice — and the guardians who planted them.", Wave("frost_oni", 2, "ice_wraith", 3), Wave("brute", 2, "ice_wraith", 3));
            q = Quest(ev, 5, 2, "Hyoga's Last Winter", 26, "Hyoga has returned to guard the final bloom. He says it is the last thing he loves.", Wave("frost_oni", 2, "ice_wraith", 3));
            q.bossId = "boss_hyoga"; q.firstClearRewards.characterId = "genji_ronin"; q.firstClearRewards.crystals = 500;

            // 4 — Grand Arena Tournament
            ev = new EventDefinition { id = "E4", title = "Grand Arena Tournament", subtitle = "Solmere's champions, one ring, one crown", regionId = "kingdom",
                accent = new Color(1f, 0.82f, 0.3f), bannerEnemy = "boss_chancellor", prizeText = "Sun Crest Pin + 600 diamonds",
                story = "The king has called a tournament to lift the city's spirits. Five rounds, a golden crown, and the whole capital cheering. But the arena's gates keep opening on their own — someone is sending demons in to make sure the champion never leaves the ring." };
            Events.Add(ev);
            Quest(ev, 1, 0, "Opening Ceremony", 16, "The crowd is in its seats. The first challengers are not human.", Wave("grunt", 5), Wave("runner", 4, "grunt", 2));
            Quest(ev, 2, 0, "Round of Sixteen", 18, "Your next opponents fight dirty — and in pairs.", Wave("demon_warrior", 2, "grunt", 3), Wave("spitter", 3, "runner", 3));
            Quest(ev, 3, 1, "Quarterfinal Brawl", 22, "Brutes break down the side gate. The referee keeps counting anyway.", Wave("brute", 2, "demon_warrior", 2), Wave("brute", 2, "spitter", 3));
            Quest(ev, 4, 1, "Semifinal Storm", 25, "The lights go out mid-match. Fight in the dark.", Wave("shadow_demon", 5, "demon_warrior", 2), Wave("corrupted_knight", 1, "shadow_demon", 3));
            q = Quest(ev, 5, 2, "The Masked Champion", 30, "The undefeated champion wears a familiar mask. The crowd goes silent.", Wave("demon_warrior", 2, "corrupted_knight", 1));
            q.bossId = "boss_chancellor"; q.firstClearRewards.equipmentIds.Add("acc_suncrest"); q.firstClearRewards.crystals = 600;

            // 5 — Night of the Blood Moon
            ev = new EventDefinition { id = "E5", title = "Night of the Blood Moon", subtitle = "Once a year, the wastes burn brighter", regionId = "demonland",
                accent = new Color(1f, 0.25f, 0.35f), bannerEnemy = "boss_goken", prizeText = "Dragon Scale Amulet + 1,000 diamonds",
                story = "When the blood moon rises over the Ashen Wastes, every demon grows stronger — and the Crimson Fist holds a tournament of his own. The winner gets a dragon's scale, the rarest treasure in the wastes. Goken has invited the slayers personally. It would be rude not to go." };
            Events.Add(ev);
            Quest(ev, 1, 0, "Under the Red Sky", 22, "The ground is hot enough to cook on. The imps are having a party.", Wave("lava_imp", 5), Wave("lava_imp", 4, "runner", 3));
            Quest(ev, 2, 0, "Ember Road", 25, "Follow the road of burning stones to Goken's arena.", Wave("lava_imp", 4, "flame_beast", 1), Wave("demon_warrior", 2, "lava_imp", 3));
            Quest(ev, 3, 1, "Trial of Flame", 29, "Goken's lieutenants test every challenger. Most don't pass.", Wave("flame_beast", 2, "lava_imp", 3), Wave("elite", 1, "flame_beast", 1));
            Quest(ev, 4, 1, "Crescent Gauntlet", 33, "Three Crescent Hunters, one after another. No rest between.", Wave("elite", 2), Wave("elite", 1, "corrupted_knight", 2));
            q = Quest(ev, 5, 2, "The Crimson Fist", 38, "The blood moon is at its peak. So is Goken.", Wave("elite", 1, "lava_imp", 4));
            q.bossId = "boss_goken"; q.firstClearRewards.equipmentIds.Add("acc_dragonscale"); q.firstClearRewards.crystals = 1000;

            // 6 — XP farm: replay any time for big piles of XP.
            ev = new EventDefinition { id = "E6", title = "Scholar's Trial", subtitle = "XP event · replay for huge piles of XP", regionId = "temple",
                accent = new Color(0.65f, 0.45f, 1f), bannerEnemy = "boss_seal_guardian", prizeText = "Up to 60,000 XP per clear",
                story = "Deep in the Forgotten Temple, the old masters left training spirits that never tire. Every spirit defeated leaves behind a glowing scroll of battle wisdom — XP your slayers can use to grow stronger. The monks say the deeper rooms teach the most." };
            Events.Add(ev);
            int[] xpScrolls = { 8, 14, 24, 36, 60 };
            string[] xpNames = { "Spirit Sparring", "Hall of Echoes", "The Scroll Library", "Trial of Patience", "The Grand Master Spirit" };
            string[] xpStory = { "Gentle spirits for new slayers.", "The echoes fight back a little harder.", "Scrolls everywhere — and their guardians.", "Only the patient reach the inner rooms.", "The oldest spirit teaches the hardest lesson." };
            int[] xpLv = { 6, 14, 22, 30, 38 };
            for (int i = 0; i < 5; i++)
            {
                q = Quest(ev, i + 1, i < 2 ? 0 : i < 4 ? 1 : 2, xpNames[i], xpLv[i], xpStory[i],
                    i < 2 ? Wave("shadow_demon", 4 + i) : i < 4 ? Wave("corrupted_knight", 1 + (i - 2), "shadow_demon", 4) : Wave("castle_sentinel", 1, "shadow_demon", 4));
                q.rewards.expScrolls = xpScrolls[i];
                q.rewards.exp *= 3;
                if (i == 4) q.bossId = "boss_seal_guardian";
            }

            // 8 — Loot farm for high-level slayers: Lv 70+ leads only, guaranteed gear every clear.
            ev = new EventDefinition { id = "E8", title = "Abyssal Hunts", subtitle = "Lv 70+ only · farm legendary & mythic gear", regionId = "demonland",
                accent = new Color(0.75f, 0.3f, 1f), bannerEnemy = "boss_veyrath", prizeText = "Guaranteed gear drops every clear",
                story = "Below the Ashen Wastes lies the Abyss, where the strongest demons hoard what they've taken: blades, mantles, charms of legend. Only slayers of level 70 and above can survive the descent. Every hunt brings loot back up — the deeper, the richer." };
            Events.Add(ev);
            string[] aNames = { "Abyss Gate", "Hollow Barracks", "Hoard of Blades", "The Drowned Cathedral", "Throne of Ash", "Heart of the Abyss" };
            string[] aStory = { "The first descent. The demons here have never seen daylight.", "An army sleeps down here. Wake it up.", "Swords stacked like firewood — and their jealous guards.",
                "A sunken cathedral full of stolen treasure and very angry knights.", "The ash kings sit on thrones of loot.", "The oldest demon guards the deepest hoard." };
            int[] aLv = { 70, 78, 86, 94, 102, 110 };
            for (int i = 0; i < 6; i++)
            {
                q = Quest(ev, i + 1, i < 2 ? 0 : i < 4 ? 1 : 2, aNames[i], aLv[i], aStory[i],
                    i % 3 == 0 ? Wave("elite", 2, "demon_warrior", 3) : i % 3 == 1 ? Wave("corrupted_knight", 2, "flame_beast", 2) : Wave("castle_sentinel", 1, "elite", 2),
                    Wave("elite", 1 + i / 2, "corrupted_knight", 2));
                q.minLevel = 70;
                q.lootRolls = 2 + i / 2;
                q.rewards.coins = 60000 + i * 25000;
                q.rewards.expScrolls = 30 + i * 12;
                q.rewards.crystals = 20 + i * 10;
                q.dropTable.Clear();
                q.dropTable.AddRange(new[] { "sword_abyss", "haori_abyss", "acc_abyss_eye", "sword_abyss", "haori_abyss", "acc_abyss_eye", "sword_akatsuki", "haori_ember", "acc_suncrest", "sword_dawn", "haori_royal", "acc_earrings" });
                if (i >= 3) q.dropTable.AddRange(new[] { "sword_moonfall", "haori_starweave", "acc_phoenixheart", "acc_dragonscale" });
                if (i == 5) q.bossId = "boss_veyrath";
                else if (i == 2) q.bossId = "boss_goken";
            }

            // 7 — Gold farm: replay for gold, and every demon here drops extra coins.
            ev = new EventDefinition { id = "E7", title = "Gold Rush", subtitle = "Gold event · replay to fill your wallet", regionId = "kingdom",
                accent = new Color(1f, 0.8f, 0.2f), bannerEnemy = "boss_chancellor", prizeText = "Up to 150,000 gold per clear",
                story = "Thieving demons broke into the Royal Treasury and stuffed their pockets with the kingdom's gold. The King has a simple deal: whatever you win back, you keep. Every demon here drops three times the usual coins." };
            Events.Add(ev);
            int[] goldRewards = { 15000, 30000, 55000, 90000, 150000 };
            string[] gNames = { "Coin Snatchers", "Vault Corridor", "The Counting Room", "Golden Guards", "The Treasure Tyrant" };
            string[] gStory = { "Little thieves with heavy pockets.", "The hallway to the vault is crawling with them.", "They've started counting the gold. Rude.", "The vault's guards have gone demonic.", "The masked thief sits on the biggest pile of all." };
            int[] gLv = { 6, 14, 22, 30, 38 };
            for (int i = 0; i < 5; i++)
            {
                q = Quest(ev, i + 1, i < 2 ? 0 : i < 4 ? 1 : 2, gNames[i], gLv[i], gStory[i],
                    i < 2 ? Wave("runner", 4 + i, "grunt", 2) : i < 4 ? Wave("demon_warrior", 2, "runner", 4) : Wave("elite", 1, "demon_warrior", 2));
                q.rewards.coins = goldRewards[i];
                if (i == 4) q.bossId = "boss_chancellor";
            }
        }

        static void BuildMissions()
        {
            // ---------------- Chapter 1
            var ch1 = new ChapterDefinition { number = 1, title = "The Fallen Village", regionId = "village", available = true,
                synopsis = "The night the sky turned red, Kiriha burned. Ren's training was over. His war had begun." };
            var m = M(ch1, "1-1", MissionType.Story, "village", "Night of the Red Sky", 1, "Demons pour out of the forest. Protect the village gate.",
                Wave("grunt", 3), Wave("grunt", 4));
            m.parTime = 200f;
            m = M(ch1, "1-2", MissionType.Story, "village", "Burning Streets", 2, "The market square is on fire. Find Master Tessai.",
                Wave("grunt", 4, "runner", 2), Wave("grunt", 3, "runner", 3));
            m.investigate = "the ruined dojo"; m.investigateLine = "Master Tessai's practice sword... snapped in two. These claw marks lead toward the shrine.";
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
            m = M(ch2, "2-2", MissionType.Story, "forest", "The Frightened Swordsman", 7, "Someone is calling for help deeper in the woods.",
                Wave("shadow_demon", 4, "runner", 2), Wave("forest_beast", 1, "shadow_demon", 3));
            m.investigate = "the abandoned village"; m.investigateLine = "Doors left open, rice still warm in the pots... and someone is calling for help in the trees.";
            m.cutsceneAfter = "c2_sora"; m.firstClearRewards.characterId = "sora_initiate";
            m = M(ch2, "2-S", MissionType.Side, "forest", "Hunt: The Forest Demons", 8, "A hunter's guild bounty on walking trees the size of houses.",
                Wave("forest_beast", 2), Wave("forest_beast", 2, "shadow_demon", 3));
            m.questGiver = "Hunter Rokuro";
            m = M(ch2, "2-T", MissionType.Treasure, "forest", "The Lost Blade", 8, "Legends say a slayer's blade lies in the hollow tree.",
                Wave("shadow_demon", 5), Wave("spitter", 3, "runner", 3));
            m.firstClearRewards.equipmentIds.Add("sword_crimson"); m.firstClearRewards.crystals += 30;
            m = M(ch2, "2-3", MissionType.Boss, "forest", "The Forest Demon", 10, "The forest itself has turned. Its rotten heart waits at the Demon Shrine.",
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
            m.investigate = "the sealed warehouse"; m.investigateLine = "Demon sigils painted inside the city walls. Someone in the palace let them in.";
            m.cutsceneAfter = "c3_hana"; m.firstClearRewards.characterId = "hana_healer";
            m = M(ch3, "3-5", MissionType.Boss, "kingdom", "The Masked Chancellor", 16, "The King's own advisor. The mask comes off.",
                Wave("demon_warrior", 2, "shadow_demon", 3)); m.bossId = "boss_chancellor"; m.cutsceneBefore = "c3_betrayal"; m.cutsceneAfter = "c3_end";
            m.firstClearRewards.characterId = "tetsu_guard";
            Chain(ch3);

            // ---------------- Chapter 4
            var ch4 = new ChapterDefinition { number = 4, title = "The Demon Territory", regionId = "demonland", available = true,
                synopsis = "To find the Demon Lord's weakness, Ren must walk where no human has returned from." };
            m = M(ch4, "4-1", MissionType.Story, "demonland", "The Ashen Wastes", 19, "The land burns. A Pillar waits at the border.",
                Wave("lava_imp", 5), Wave("flame_beast", 1, "lava_imp", 3), Wave("lava_imp", 2, "demon_warrior", 2));
            m.cutsceneBefore = "c4_wastes"; m.firstClearRewards.characterId = "homura_pillar";
            M(ch4, "4-2", MissionType.Story, "demonland", "Elite Hunters", 21, "The Demon Lord's hunters have your scent.",
                Wave("corrupted_knight", 1, "lava_imp", 4), Wave("flame_beast", 2), Wave("elite", 2, "demon_warrior", 1));
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
            m = M(ch5, "5-2", MissionType.Story, "temple", "The Guardian's Trial", 27, "The temple tests everyone who seeks the truth.",
                Wave("corrupted_knight", 1, "shadow_demon", 4), Wave("castle_sentinel", 1, "spitter", 3));
            m.investigate = "the hero's mural"; m.investigateLine = "Akatsuki's mural... an eclipse splitting a heart in two. Why does that sword look like mine?";
            m = M(ch5, "5-T", MissionType.Treasure, "temple", "The Hero's Reliquary", 27, "Akatsuki's personal vault lies somewhere below.",
                Wave("shadow_demon", 6), Wave("corrupted_knight", 2)); m.sealPuzzle = true; m.firstClearRewards.equipmentIds.Add("sword_dawn");
            m = M(ch5, "5-3", MissionType.Boss, "temple", "The Ancient Guardian", 28, "The colossus that guards the truth has woken.",
                Wave("corrupted_knight", 1, "shadow_demon", 3)); m.bossId = "boss_seal_guardian"; m.cutsceneAfter = "c5_truth";
            m.firstClearRewards.characterId = "ren_sundance";
            Chain(ch5);

            // ---------------- Chapter 6
            var ch6 = new ChapterDefinition { number = 6, title = "The Fallen Kingdom", regionId = "fallen", available = true,
                synopsis = "While Ren sought the truth, the Demon Lord's army marched on Solmere." };
            m = M(ch6, "6-1", MissionType.Story, "fallen", "The Siege of Solmere", 31, "The outer walls have fallen. Hold the main street with the royal guard.",
                Wave("lava_imp", 6, "grunt", 4), Wave("demon_warrior", 3, "spitter", 3), Wave("corrupted_knight", 2, "lava_imp", 4));
            m.investigate = "the fallen watchtower"; m.investigateLine = "The signal fire never got lit. They didn't even see the army coming.";
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
            m = M(ch7, "7-2", MissionType.Story, "castle", "Hall of Elites", 37, "The Demon Lord's finest wait in the great hall.",
                Wave("elite", 2, "corrupted_knight", 1), Wave("castle_sentinel", 2, "elite", 1), Wave("demon_warrior", 4, "corrupted_knight", 2));
            m.investigate = "the hall of statues"; m.investigateLine = "Every statue is a slayer who came here before us. Every one of them is kneeling.";
            m = M(ch7, "7-3", MissionType.Boss, "castle", "The Twin Generals", 38, "General Vex and General Nyx guard the final stair.",
                Wave("corrupted_knight", 2)); m.preBosses.Add("boss_vex"); m.bossId = "boss_nyx"; m.cutsceneAfter = "c7_truth";
            m = M(ch7, "7-4", MissionType.Boss, "castle", "Veyrath, the Demon Lord", 40, "The Demon Lord of the Eclipse. The other half of your heart.",
                Wave("castle_sentinel", 1, "elite", 1)); m.bossId = "boss_veyrath"; m.cutsceneBefore = "c7_final"; m.cutsceneAfter = "ending";
            m.timeLimit = 780f; m.parTime = 380f; m.firstClearRewards.characterId = "homura_lastflame"; m.firstClearRewards.equipmentIds.Add("sword_akatsuki");
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
            m.preBosses.Add("boss_gorvath"); m.preBosses.Add("boss_chancellor"); m.bossId = "boss_goken"; m.timeLimit = 900f;
            m.firstClearRewards.crystals = 300; m.rewards.crystals = 20; m.requiresMissionId = "3-5";
            ExtraMissions.Add(m);
            BuildEvents();
            m = M(null, "TR", MissionType.Training, "kingdom", "Training Grounds", 1, "Test every skill on training dummies. No rewards, no pressure.",
                Wave("dummy", 3)); m.training = true; m.timeLimit = 9999f; m.rewards = new RewardBundle(); m.firstClearRewards = new RewardBundle();
            ExtraMissions.Add(m);
        }
    }
}
