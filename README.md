# Hashira Chronicles — Blades of Dawn

A mobile 2.5D **story-driven** anime action RPG in **Unity (C#)**: real-time hack-and-slash with a
four-slayer team, breathing-style skills, cinematic ultimates, boss events, an in-engine story told
through cutscenes, a walkable world map, summoning, and a connected progression loop:

**Story → Travel → Cutscene → Fight → Rewards → Upgrade / Summon / Build Team → Next Mission**

### The story
Ren Kagami trains under Master Tessai in the mountain village of Kiriha. One evening the sky turns
red, the Demon Lord Veyrath's army burns the village, and a mysterious power wakes in Ren's blade.
Across seven chapters — *The Fallen Village, The Forest of Shadows, The Kingdom, The Demon Territory,
The Forgotten Temple, The Fallen Kingdom* and the final chapter at the *Castle of the Eclipse* — Ren
gathers allies (Sora, Kiba, Hana, Captain Tetsu, the Flame Pillar Homura), uncovers a betrayal in the
royal court, learns in the temple that his power is the dawn half of the Demon Lord's own heart, loses
people he loves, and finally faces Veyrath in two forms.

### Every mission is a journey
Missions are no longer a single arena. Each one is a road through its region — e.g. *Village Entrance →
Forest Path → Broken Bridge → Demon Camp → Demon Shrine* — that you physically walk: roadside lanterns, torches,
prayer flags and signposts show the way; ambushes wait on longer stretches; fights happen in clearings sealed by a
spirit barrier; some missions have a clue to investigate (and a trap); the temple has a seal puzzle. The last
stretch before a boss goes quiet and dark, then the boss gets a camera reveal (wide shot → the ground shakes →
it rises → close-up → title card → roar) in its own arena (burning shrine, rotten forest heart, storm summit,
temple court, throne of the eclipse…), and the arena turns hostile when it enrages (lightning, falling rocks, fire).
The HUD shows the current objective with live distance/kill counts, a route strip and a waypoint arrow, and the
next region's landmark stands on the horizon. On the world map, travel can trigger random encounters (ambushes,
rare monsters, wandering bosses, merchants, travellers, treasure, caves, a mysterious stranger), and the camera
swoops down into the destination before the mission begins.

### Look & feel: simple chibi heroes, glowing blades
Everything is built in-engine from code, in the style of the original reference sheets: white round
heads with dot eyes, simple dark rounded bodies, glowing weapons and flat low-poly worlds. Each slayer
still reads as their own person: hair style, outfit colours, weapon, body shape, accessories (scarf,
cape, pelt, armour, bell), movement personality (steady, nervous, aggressive, graceful, stoic, confident,
sly, light) and idle fidgets. Portraits for every menu are rendered live from the same models
(`PortraitStudio`), so the art is always one cohesive set. See [docs/ART_BIBLE.md](docs/ART_BIBLE.md).

### Every slayer fights differently
- **Swift** (Sora, Mina, Raiga): fast multi-hit strings, double cuts, a 5-cut flurry finisher, long dash strikes, short recovery.
- **Heavy** (Kiba, Tetsu): slower swings, big knockback, a ground-slam finisher, super armour while attacking.
- **Ranged** (Hana, Rokuro, Yui): bolts and a five-way spread, area blasts on the charged attack; supports heal on hits.
- **Technical** (Ren, Genji, Kuroe): a wider parry window that triggers an instant **COUNTER**, and after a parry or
  perfect dodge the next swings throw crescent waves.
- **Balanced** (Homura): elemental bursts on every finisher.

Specials are long and cinematic: the fight freezes, the camera cuts to a character-specific angle, energy
gathers in the slayer's element, then a release unique to their style (chain dashes, triple slams, a shot
storm, a dragon spiral, orbiting crescents…) ends in a launching blow and a final shockwave.

### Monsters
Demons have glowing slanted eyes, jagged grins and a creeping dark aura. Beyond rushing, flanking,
dodging and shooting, some **hide as a shadow puddle and ambush** you, some **summon** smaller demons,
some **charge** across the field (and stumble after), and every regular demon **enrages** when badly hurt.
Bosses keep their camera reveal and phases and now fall in a slow-motion **defeat cinematic**.

### The game loop
- **Opening cinematic** on first launch (peaceful village → the sky changes → the attack → the power
  awakens → *THE JOURNEY BEGINS*) flows straight into the first battle.
- **Animated home screen**: the team leader breathes, looks around and practises forms in a living
  village (villagers who walk and chat in speech bubbles, birds, clouds, a campfire, falling leaves).
  Big colour tiles: PLAY · STORY · JOURNAL · SUMMON · CHARACTERS · TEAM · EQUIPMENT · MISSIONS · SHOP · SETTINGS,
  currency pills top-right and a team power panel with the four slayers' faces.
- **Team screen**: TEAM 1-4 presets, a full-body line-up, AUTO SET, EDIT TEAM, a roster grid with element
  filters and a detail card (HP / ATK / DEF) with SELECT.
- **World map**: eight connected locations with paths, landmarks and markers (locked / you are here /
  boss / side / treasure / complete). Your current region opens as an **area map**: numbered mission stops on
  a dotted trail with your leader standing on the next one, and a mission list with stars, locks, the BOSS
  row and CONTINUE. Picking a mission elsewhere makes the leader **walk the road**
  there, camera following, before the mission starts.
- **Mission pages**: number, title, story hook, quest giver, recommended level vs your team, enemy
  types, boss info, objectives, rewards and first-clear rewards, and a big animated PLAY.
- **Story cutscenes** in-engine: camera cuts/dollies/orbits, dialogue with typewriter text and
  speaker name plates, title cards, fades, sky changes and music — skippable and replayable from the Journal.
- **End-of-mission screen**: MISSION COMPLETE, S/A/B/C rating, counted-up EXP and gold, items,
  equipment, level-ups and new allies; NEXT MISSION / REPLAY / RETURN TO MAP / CHARACTERS.
- **Summoning as an event**: the shrine goes dark, energy gathers, the circle "upgrades" colour through
  the rarities, lightning for Legendary+, a portal opens, a silhouette steps out, then the reveal.
  Common · Rare · Epic · Legendary · Mythic, published rates, ×10 Epic guarantee and pity.
- **Collection viewer**: rotate the 3D model, test each skill, jump into the Training Grounds.
- **Missions board** (daily/weekly with rewards, side stories, events, training) and a **shop**.
- **Boss events**: cinematic entrance with a name card and a line of dialogue, enrage with the arena
  crumbling, twin generals fought back to back, allied soldiers in sieges, a temple seal puzzle, and
  the Demon Lord's transformation at 50% that triggers the team's **Awakening** and a final strike.

> All characters, names, story and assets here are **original placeholders**. The data layer keeps
> content separate from gameplay code so licensed characters and art can be swapped in later.
> Using any real IP (e.g. Demon Slayer) in a commercial release requires a license.

## Running it

1. Install **Unity 2022.3 LTS or Unity 6** (Built-in Render Pipeline, the default for projects
   without URP) with Android and/or iOS build support.
2. In Unity Hub: **Add → Add project from disk** → select this folder.
3. On first open, `ProjectSetup` creates `Assets/Scenes/Main.unity`, adds it to Build Settings and
   sets landscape orientation. (Re-run via menu **Hashira Chronicles → Setup Main Scene and Build Settings**.)
4. Press **Play**. There are no prefabs or scene objects: `GameBootstrap` builds the camera, lights,
   UI, audio and every character from code in any scene.

To test on a phone, switch the platform to Android/iOS in **File → Build Settings**, then **Build and Run**.

### Controls

| Action | Touch | Keyboard |
|---|---|---|
| Move | Floating joystick (left half) | WASD / arrows |
| Attack combo (5 hits) | ATTACK | J |
| Charged attack | Hold ATTACK, release | Hold J |
| Dodge (i-frames, **perfect dodge** slows time) | DODGE | Space / K |
| Dash attack | ATTACK right after a dodge | J after Space |
| Guard (hold) / **Parry** (tap just before a hit) | GUARD | F / Left Shift |
| Lock-on (tap again for the next target) | LOCK | T / middle mouse |
| Sprint | Keep moving ~0.5 s | Keep moving |
| Breathing forms 1–3 | 1 / 2 / 3 | 1 2 3 or U I O |
| Ultimate | ULT (when gauge is full) | R / L |
| Switch slayer (team of up to 4) | Tap a portrait | Q / E / Tab |
| Pause | II | Esc / P |

## What's in the slice

**Polish pass (v0.2):**
- Input buffering and cancel windows (combo → skill/dodge/guard/move), dash attacks, sprint, guard and parry,
  hit stun, knockdown with quick-rise, super armour on skills.
- Demons flank using surround slots, fast/elite ones dodge your swings, ranged ones flee, "!" detection pops,
  launches and air juggles, knockdowns, parry staggers, elemental weakness breaks guard faster.
- Camera frames enemies and the boss, punches in on heavy hits, and switches to an orbiting cinematic angle for ultimates and victories.
- Impact frames, speed lines, radial blur, bloom and colour grading (High/Ultra), impact lights, dust, per-element ability flourishes.
- Arenas get houses, a torii gate, flickering stone lanterns, grass, fireflies and breakable crates/barrels (sometimes heal), all static-batched.
- Adaptive music that crossfades explore → combat → boss → victory/defeat, a low-HP heartbeat, ducking under ultimates, plus new SFX.
- Settings: Low/Medium/High/Ultra graphics (auto-detected), music/SFX volume, camera shake, damage numbers.
- Rigged-model support is still in the code but switched off (`GameConfig.UseImportedModels`); see
  [docs/ART_PIPELINE.md](docs/ART_PIPELINE.md).

- **Combat**: responsive 5-hit combos with aim assist and lunges, charged attack, dodge cancel,
  i-frames and perfect-dodge slow motion, hit-stop, screen shake, sword trails, damage numbers,
  crits and element "WEAK!" callouts.
- **Breathing forms**: data-driven abilities (Dash, Spin, Wave, Burst, MultiSlash) with
  per-level scaling; each character has 3 forms + an ultimate.
- **Cinematic ultimates**: camera zoom, time slow, cut-in banner, then **TOTAL DAMAGE**.
- **Three-slayer team**: tag in/out with a switch-in attack; benched slayers regenerate HP; if the
  active slayer falls, the next one is forced in.
- **Elements**: Water ▶ Flame ▶ Beast ▶ Thunder ▶ Water (×1.5 / ×0.75), Light ◀▶ Dark.
- **Demons**: Normal, Fast (lunge), Tank (slam, super armor), Ranged (dodgeable orbs), Elite (leap slam).
  Every attack is telegraphed on the ground; an attack-token system prevents unfair dogpiles.
- **Bosses with mechanics**
  - *The Thousand-Arm Demon* — Grasp, Ground Hands, Summon; enraged at 50% (+ Sweep).
  - *Goken, the Crimson Fist* — 3 phases: Flurry, Shockwave, Needle Rush → Double Shockwave →
    **Destructive Mode** at 50% (Scatter Blossoms) → **Annihilation** arena blast followed by a
    **BREAK** window (+50% damage taken).
- **Story**: 7 chapters, ~30 story, boss, side and treasure missions plus an event boss rush and a
  training ground, each with three objectives (defeat N demons / nobody falls / par time).
- **Progression**: EXP & levels (cap by stars), EXP scrolls, ascension ★→★★★★★★, skill levels 1–10,
  an ability tree, equipment (Sword / Haori / Accessory) with upgrades, power rating.
- **Rewards**: EXP, coins, crystals, scrolls, ascension ore, equipment drops, first-clear rewards,
  +crystals for each newly completed objective, new slayers from boss clears.
- **Character versions**: e.g. *Ren Kagami — Initiate* (Water ★4) and *Ren Kagami — Dawn Dance* (Light ★5)
  with different stats, forms and ultimate.
- **Save system**: JSON in `persistentDataPath` with backup and validation/migration.
- **Presentation**: cel-shaded toon shader with outlines and rim light, procedural particles,
  and fully **synthesised audio** (SFX + taiko/shamisen-style music) — no external assets needed.

## Architecture

```
Assets/
  Resources/Shaders/     Toon (cel + outline), UnlitAdditive, UnlitTransparent
  Scripts/
    Core/        GameBootstrap, GameManager (screen flow), GameEvents, TimeController, GameConfig
    Data/        Enums, StatBlock, ElementChart, Definitions, GameDatabase (all placeholder content)
    Save/        PlayerData, SaveSystem
    Progression/ ExperienceSystem, CharacterSystem (stats/upgrades), SkillTree,
                 InventorySystem + EquipmentSystem, RewardSystem (+ BattleResult)
    Combat/      Combatant, HealthSystem, CombatSystem (+ DamageCalculator), AbilitySystem, WaveProjectile
    Player/      PlayerCharacter, PlayerController, TeamSystem, InputState
    Enemies/     EnemyController (EnemyAI archetypes), BossController (BossAI), EnemyProjectile
    Missions/    BattleController, MissionSystem, ArenaBuilder
    Visuals/     CharacterVisual, CameraController, VFX, FlashFx, Telegraph, DamageNumbers,
                 MaterialFactory, MeshFactory
    World/       HomeStage (animated home + 3D viewer), MapStage (world map + travel), SummonStage,
                 NpcWalker, BirdFlock, CloudDrift, EnvFx, Sway, Spinner
    Story/       Cutscene (script builder), CutsceneDatabase (the story), CutscenePlayer
    Meta/        SummonSystem, QuestSystem (daily/weekly), ShopSystem
    Audio/       AudioManager
    UI/          UIManager (+ MenuScreens, MetaScreens, CharacterScreens, BattleHUD, SettingsScreen),
                 MobileControls, HudLayout, UIStyles
    Editor/      ProjectSetup
```

Key design choices:

- **Content is data.** Characters, abilities, demons, missions and equipment live in `GameDatabase`
  as plain C# definitions. Adding a slayer or mission needs no new gameplay code.
- **No physics dependency.** Hit detection queries a `Combatant` registry by distance and arc, and
  movement is clamped to the arena — deterministic and cheap on mobile.
- **Placeholder art behind one seam.** `CharacterVisual` exposes `Swing / Spin / Flash / SetCharge /
  PlayDeath`; replace its builders with rigged models + an Animator without touching combat.
- **UI is immediate-mode (IMGUI)** in a 1080p virtual space that respects the safe area. Each screen
  is one method, easy to port to uGUI/UI Toolkit when final UI art exists.

## Roadmap

- **Phase 1 – Prototype** ✅ movement, combo, 3 skills, ultimate, health, enemy AI, boss, mission
  completion, rewards, XP, mobile controls.
- **Phase 2 – Core RPG** ✅ 3-slayer teams & switching, stats, equipment, skill upgrades, multiple
  missions & bosses, save system. *Next:* link-slot passive bonuses, more versions per character.
- **Phase 3 – Story & live systems** ✅ 7-chapter story with cutscenes, world map, summoning with pity,
  daily/weekly missions, shop, events. *Next:* voiced lines, event currency, link-slot passives.
- **Phase 4 – Online**: accounts, cloud save (Firebase/PlayFab), 4-player co-op raids, 3v3 PvP by
  power rating, analytics, purchases.
- **Phase 5 – Ship**: store builds for iOS (App Store) and Android (Google Play).
