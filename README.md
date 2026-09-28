# Blade Legends

A mobile 2.5D **story-driven** anime action RPG in **Unity (C#)**: real-time hack-and-slash with a
three-slayer team, breathing-style skills, cinematic ultimates, boss events, an in-engine story told
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

### Co-op and snow fixes (0.22.2)
- **Co-op teammates play to clear fast.** They hunt demons up to 45 m away instead of staying near you. They sprint everywhere, dash-lunge to close gaps, and run ahead to the next objective when nothing is in sight.
- **Co-op is leader-only.** You bring just your leader, with no switching; the other two slots are your teammates.
- **Co-op gates ask "JOIN A CO-OP GAME?"** when you walk up. **YES** gathers players, fills three portrait slots as they join, shows "ALL PLAYERS READY!" and starts the game by itself. CANCEL stops the search.
- **Snow is toned down.** The ground is blue-grey instead of paper white. The sun is softer and higher, the ambient light is lower, bloom only catches real highlights, and shadows are lighter, so attacks read against the snow.
- **Line glitch:**
  - The high sun keeps shadows short.
  - Tall, thin props (poles, posts, banners) no longer cast long line shadows.
  - The ranged teammate's throw shows three short crescents instead of a 9 m line flash.

### Sound and fixes (0.22.1)
- The home screen no longer shows speech boxes. Villagers still stroll through the village.
- **Blade swings** are rebuilt in layers: a whoosh that rises and falls as the blade passes, an airy low body, a bright steel ring and a short room tail. Each weapon has its own tuning.
- **Impacts** get a sharp crack, a body thump, a sub-boom and a short reverb.
- The **battle theme** is now 140 BPM, with war-drum fills, a pulsing bass line, a dark string drone and a melody doubled an octave up in the second half.
- Fixes:
  - PvP is a true 3v3: you bring only your leader.
  - Rigged models no longer stay stuck in the death pose after a respawn.
  - PvP results offer PLAY AGAIN, ARENA and HOME instead of story buttons.
  - The PvP roster no longer overlaps the portraits.
  - The minimap no longer covers the kill feed.

### Arena PvP, living story scenes, evening look (0.22.0)
- **Arena (3v3)** — the home **ARENA** shortcut opens a rank card (Bronze → Silver → Gold → Platinum → Diamond → Master, three divisions each below Master), trophies, a tier ladder, wins and losses, three modes and a Ranked/Casual toggle. **FIND MATCH** shows *SEARCHING FOR OPPONENT...*, then a **YOUR TEAM vs OPPONENT TEAM** screen with portraits, then the match.
  - **3v3 Battle**: first to 10 knockouts. **Moon Crystal**: hold the centre zone alone to score, first to 60. **Boss Rush**: a boss both teams can hit, and the most damage (plus knockouts) wins.
  - In the match you get blue and red bases, respawns after 5 s, a 2:30 clock, a scoreboard, a roster with health bars, name tags in team colours, a kill feed and a respawn countdown. Ranked wins and losses move your trophies.
  - **Not online yet.** There is no game server, so the other five slayers are AI (`PartySlayer`, now team-aware), and the UI says so. The match rules, scoring, ranks and screens are real. A server (Photon, Unity Netcode + Relay/Lobby, or similar) would replace the bots with people.
- **Story scenes** are now staged in the new worlds with palette lighting:
  - speakers change expression (happy, angry, sad, surprised, determined) and their mouths move while their lines type;
  - listeners turn to the speaker, and characters walk in and out;
  - the camera pushes in on dramatic lines, with shake and breath effects;
  - the dialogue box is chunky, with a portrait that pops.
- **Village chat engine**: about 220 templates in 20 categories, filled from live context (regions, bosses, events, your recent clears, banners, time of day). It uses weights, per-category cooldowns and a recent-lines memory, so it doesn't repeat itself. Questions get answers from other players.
- **Evening look**: dusk sky in blue and violet, a low sun, deeper toon shadows, lantern halos and glow pools, rose-stone paving, bold roofs.
- **Banners**: element-themed promo art with light rays, glow and particles, LIMITED/NEW chips, rarity stars and a weekly countdown.
- **Combat feel**:
  - a comic impact star on every hit;
  - speed streaks and dust on hard knockbacks;
  - slightly longer hit-stop and shake;
  - bigger, bouncier damage numbers with CRIT! tags;
  - an element-coloured ground zone showing your charged attack's reach as it charges;
  - a bigger ultimate finish (pillar, star, wide shockwave).

### Cartoon style, tutorials (0.21.0)
The whole game moves to a stylised cartoon look (switch: `GameConfig.CartoonStyle`), keeping its own characters
and world — references were used only for the level of exaggeration, colour and readability:
- **Characters** (`CharacterVisual.Cartoon.cs`): bigger heads and eyes, shorter chunkier bodies, thicker limbs,
  oversized hands, boots and weapons, a livelier idle; demons get a milder version.
- **Animation**: bigger wind-ups and overshoot on every swing, squash on the wind-up and stretch through the strike.
- **Rendering** (`Toon.shader`, `ToonWorld.shader`): three flat tones with crisp edges, a hard highlight and rim,
  strong ambient light, a saturation boost and thicker dark outlines.
- **World**: Kiriha at a vivid twilight (blue-to-pink sky, big sunset disc), bright green grass, big warm
  flagstones, bold roof colours on chunkier houses, simple bold cherry trees; brighter forest and a purple-red volcano.
- **UI**: every button is chunky — thick dark outline, raised face on a darker slab, gloss, drop shadow, sinks when
  pressed and bounces on release; bold outlined lettering; a huge breathing PLAY button.
- **Tutorials** (`TutorialSystem`, `Tutorials.cs`), each shown once and skippable: a coached first battle (move,
  combo, dodge, skill, special — demons wait until you've got the basics), a home tour and a world-map guide.
- Fixed: the slayer sometimes moved slowly at the start of a battle (a quick click during a slow loading frame left
  ATTACK "held", so the slayer was charging a heavy attack at 40% speed).

### Social, achievements and a smarter party (0.20.0)
- **Co-op party that plays like people** (`PartySlayer`): each partner has a play style from their slayer —
  Vanguard (dives in and slams), Duelist (fast combos, flash-steps), Skirmisher (keeps distance, throws crescents)
  or Support (heals) — and two partners never share one. They pick their own targets, keep their own space (no more
  stacking on each other), dodge out of red zones, use specials and a big ultimate, chat while they fight, go down
  and get back up. In the village they hang around you instead of glued to your back.
- **Chat that sounds like players** (`ChatBrain`): lowercase, slang (u, rn, ngl), typos with "*corrections",
  text faces (:D ^_^ T_T), replies to what you actually say, asks back, remembers the slayer you like, and learns
  your own words and slang (saved with your profile) — all local, no server or AI model. Bad words are blurred.
- **Profile** (tap your name, top-left): change your name once every 7 days, your unique gamer code with COPY,
  stats and medals. **Friends** (FRIENDS on the left): add by code or from suggestions, requests, who's online, and
  private chats. Friends and their messages are simulated until there's a server.
- **Achievements**: 30 goals with Bronze/Silver/Gold/Platinum medals, rewards paid instantly, and a popup.
- Summon: the previous pull's card no longer flashes during a new pull; the banner shows featured Legendaries
  instead of drop rates. Currency numbers shrink to fit. Test play (TRY) happens in the forest.
- Sound: every effect gets a finishing pass (soft saturation, a small room reverb, click-free fades); new UI tap
  and reward chime. Home: flagstone paving, fuller cherry trees, villagers who stop and chat to each other.

### Kiriha Village hub (0.19.0)
**The village, rebuilt.** Kiriha is now built on the same standard as the forest, snow and volcano worlds
(`PrototypeWorld.Village.cs`, `Kind.Village`): a moonlit night with stars and a haloed moon, a timber village gate
with plastered walls, a street of townhouses (glowing paper windows, noren curtains that sway, tiled gable roofs,
some two storeys, a second row behind), a lantern-lit market, the plaza under a great cherry tree with a petal
carpet, a stone lantern ring, a well and a quest board, an old water mill whose wheel turns in the river by the
bridge, a tea stop, and a shrine at the top with guardians, a purification pavilion and a curved-roof hall. Cherry
petals and fireflies drift through it. The **home screen** stands your leader in the street looking up at the plaza,
and the **open world** is the same village (`OpenWorldBuilder` → `PrototypeWorld.BuildVillage`).

**Players, chat and co-op gates.** The plaza is a hub (`VillageHub`, `VillageHubUI.cs`): other slayers walk around
with name tags, there is a village chat (type and press Enter or SEND), and the HUD shows how many players are online.
Walk up to someone and tap **INVITE**, or tap **FIND PARTY**, to form a party of three; party members follow you.
The three **co-op gates** (Embers — Hard, Frost — Expert, Abyss — Nightmare) only open for a full party of three.
Beyond them the demons are 3x / 4.5x / 6x stronger (HP ×power, attack and defence scaled up too) with an extra wave,
the party fights beside you, and the rewards are 2–4x bigger with diamonds on the first clear.

> **Online is a simulated preview.** There is no game server yet: the other players are AI, the chat replies and
> the online count are generated locally (`VillageOnline`), and in a co-op gate your party members are AI slayers.
> The UI says so. Real multiplayer needs a backend (for example Photon, or Unity Netcode with a relay/lobby
> service); `VillageOnline` and the party/chat calls in `VillageHub` are the seams it would plug into.

Also in 0.19.0: the game is now called **Blade Legends** (old saves are migrated), the home banner carousel can be
swiped, the home slayer can be turned by dragging, and the summon reveal no longer overlaps the name card.

### Premium characters, demons and worlds
**Characters.** Every slayer, villager and soldier is built on a jointed rig (`CharacterVisual.Roster.cs` +
`PremiumRig`): shoulders, elbows, hips and knees driven by two-bone IK (feet stay planted, the weapon hand follows
every swing), real hands and boots, layered outfits (long haori over hakama, a striker's cropped jacket, armour or a
layered caster robe), a detailed weapon per kind, anime faces (irises, highlights, lashes, brows) chosen by
personality, their own hairstyle, element-tinted rim light, and more gold and ornaments the rarer they are (glowing
crests for Legendary, a halo and orbiting motes for Mythic). Kaito, Oboro and Shion are the hand-designed originals
(`CharacterVisual.Premium.cs`).

**Demons.** The humanoid demons (ghoul, stalker, shadow, hunter, bone warrior, knight, general, oni, sentinel,
imp, and the bosses) use the same rig with taller, meaner proportions, claws, horns, glowing slit-pupil eyes and
detailed weapons (`CharacterVisual.PremiumDemons.cs`). Beasts and walking trees get walking legs (`MonsterGait`),
smooth shapes and accent rim light.

**Worlds (quality test).** Three prototype environments (`PrototypeWorld*.cs`), playable from
**CHARACTERS → ★ NEW DESIGNS → WORLDS**: Forest (the Great Tree), Snow Mountain (the Frozen Summit Shrine) and
Volcano (the Crater Throne). Each has a sculpted, vertex-painted terrain with the road and clearings kept flat, a
foreground of grass, flowers and rocks, midground set pieces at every place (gate, trail, bridge, ruins or shrine,
demon camp), a background of forest walls and distant ranges, flowing water or lava, wind in the foliage, weather,
drifting fog, clouds and birds, per-world lighting and a gradient sky. Scenery is combined into a few
vertex-coloured meshes (`WorldKit`, `ToonWorld.shader`) so it stays cheap on mobile. The rest of the world
will move to this standard once the prototypes are approved.

**Collision.** Solid scenery has real Unity colliders: capsules for trunks, poles and rocks (rocks get a row of
capsules along their long axis), boxes for walls, fences, rails and buildings, sized from each prop's own mesh
before static batching renames it (`Obstacles.cs`). Slayers move with a `CharacterController`
(`PlayerCharacter.Collision.cs`). Walking, sprinting, dodges, lunges, knockback and every special-move dash or blink
go through `MoveTo`, which sweeps the capsule, so nothing can tunnel through a thin pole, and a gap narrower than a
body can't be squeezed through. Demons and allies use the same shapes analytically, with swept steps. The training
arena is included. **WORLDS → COLLISION TEST** opens a yard with a tree, a pole, a rock, a wall, a fence, a 0.6 m
gap and a 1.6 m gap. A live checklist ticks each test off.

**Team screen.** The line-up uses its own TEAM idle state (`CharacterVisual.Showcase.cs` + `PremiumRig`). It is
separate from combat idle, attacks, specials, victory and defeat. Each slayer:
- stands upright with nearly straight legs and feet flat on the podium;
- has relaxed arms, and carries the weapon naturally for its kind: a sword lowered at the side, a greatsword on the
  shoulder, a spear or staff upright with its butt on the ground, a cane planted, twin weapons mirrored;
- breathes, shifts weight, glances around and blinks.

Personality shows only in the head angle and which leg takes the weight. Everyone faces the camera with the same
slight three-quarter turn toward the group.

**Summon.**
1. The screen fades to black.
2. A sheathed sword appears across the screen.
3. A hand grips the hilt.
4. CLANG: the blade is drawn in a flash of sparks.
5. A diagonal slash splits the black open onto a glowing shrine.
6. A huge moon rises overhead.
7. Energy swirls around the altar.
8. The summon seal draws itself and erupts upward.
9. Cards carrying slayer silhouettes fly out and land in an arc.
10. Each slayer drops from the light and lands with an impact before their reveal.

Rarity colours the slash, seal, cards and effects. Legendary and Mythic pulls get a much bigger slash with
lightning along the cut, lightning strikes and heavy camera shake.

**Whole roster to the design standard.**
- **Hair:** every slayer has sculpted hair in their own hairstyle, varied per character, plus buns, twin tails and braids. Hats keep their hat.
- **Proportions:** leg length follows how they fight, head size their age and manner, and shoulders their build.
- **Faces:** eye shape, brows and mouth follow their manner, with personal variation and at most one mark.
- **Silhouette:** a role hero piece (pauldron for tanks, half-cape for ranged fighters, sash bow for healers).
- **Pose:** their own natural team-screen pose.

Standing legs are nearly straight (a soft knee), with deeper bends only when walking, guarding or attacking.
On the TEAM screen, drag a slayer to turn them around.

**Worlds.** Every mission now uses the new-standard worlds, varied per mission: forests, villages and roads become
the Forest world, mountains and temples Snow Mountain, and demon lands, castles and fallen cities the Volcano.

**Home screen.** The lobby is laid out like a premium anime action RPG (`HomeScreen.cs`):
- **Top-left:** profile (portrait, level, name, EXP bar) and a compact shortcut column (Event, Notice, Ranking, Friends).
- **Upper-left:** the logo, and under it a large banner carousel that slides every few seconds, with dots (limited event, featured summon, explore Kiriha, forge).
- **Top-right:** gold, diamond and XP counters, plus mail, gift and settings.
- **Right:** the TEAM POWER panel with portraits and a member list.
- **Bottom:** navigation with a large glowing PLAY button, then SUMMON, CHARACTERS, TEAM, MISSIONS, SHOP and INVENTORY.

Behind it, Kiriha at night: moonlight, strings of flickering paper lanterns, cherry blossoms, fog and villagers
walking. The team leader stands right of centre in their natural idle, with a soft contact shadow and a rim light.

**Smoothness pass.**
- **Shapes:** every character part is built from smooth shapes: rounded boxes (`MeshFactory.RoundedCube`), soft-edged cylinders, capsules and spheres. Lathe meshes (limbs, sleeves, robes, bands) have analytic normals with no seams.
- **Shading:** the toon shader uses a wide, eased light ramp, a soft form gradient and a faint broad sheen; cast shadows are soft and lighter.
- **Rig blending:** the rig blends between animation states. The body root, hips, chest and hands ease toward their targets: fast during swings, softer otherwise.
- **Attacks:** swings have anticipation, an eased strike with follow-through, and a smooth settle.
- **Cloth and hair:** they lag behind movement and turns and spring back.
- **Camera:** it eases between modes (into specials and boss entrances, and out of cutscenes) instead of snapping, and follows with a damped spring.

**Roster design system.** `docs/CHARACTER_DESIGN_BIBLE.md` is the visual standard for every slayer. It covers:
- silhouette drivers
- proportions by type
- faces, hair, clothing by role, weapons and colour rules
- element colours and rarity presentation
- personality in the body, animation states and presentation

Six original reference characters are built to it (`CharacterVisual.Designs.cs`), each with their own proportions,
head shape, face, sculpted hairstyle, outfit, weapon, palette, movement personality and natural showcase pose:
- **Tobi** — fast melee, short blade
- **Bunta** — heavy, war hammer
- **Sayo** — ranged, tall longbow
- **Nene** — support, lantern staff
- **Nagi** — assassin, hooded and masked, twin sickles
- **Seiran** — elemental power, water glaive and orb

**Designs → ROSTER** shows all six in a promotional line-up with design sheets. **SILHOUETTE** turns them into black
shapes to check they read apart. **VIEW 3D** and **TRY** open the viewer or a trial. They are playable but never
appear in summons.

**Hair (quality standard).** A new sculpted hair system (`HairGeometry`, `HairStyles`). Each style has:
- a scalp shell cut along a real hairline;
- tapered, flattened locks that grow from the scalp, hug the head and then fall with weight;
- bangs that always stop above the brows;
- shade and highlight colours, and per-section secondary motion (`HairSway`).

Three reference styles: Ren (short and messy), Mina (long and flowing, side part) and Sora (controlled, grouped
spikes). The rest of the roster keeps its current hair until these are approved.

**Collision.** Solid scenery (trees, poles, rocks, walls, fences, buildings, bridge rails, gates, tents) blocks
movement through invisible circles and boxes (`Obstacles`), including gaps too narrow to pass. Set pieces hide and
show as a whole when they come between the camera and the slayer (`OcclusionGroup`).

**Sound.** Every sound is synthesised at startup with filters, FM rings and reverb (`AudioManager.Design.cs`):
per-weapon swings and impacts, element textures, a five-part special-attack sequence (activation, element
build-up, whoosh, release, finishing boom), softer UI sounds, a mission-clear fanfare, and a home-screen theme
(pad, koto and bamboo flute) over a bed of wind, birds, leaves and a distant stream.

Characters are merged per joint at build time (`CharacterVisual.Optimize.cs`) so a detailed fighter still renders
in a few dozen draw calls.

### Every slayer fights differently
- **Swift** (Sora, Mina, Raiga): fast multi-hit strings, double cuts, a 5-cut flurry finisher, long dash strikes, short recovery.
- **Heavy** (Tetsu, Homura — Last Flame): slower swings, big knockback, a ground-slam finisher, super armour while attacking.
- **Brawler** (Kiba): hand-to-hand. Jab-cross strings that step in with every punch, a launching uppercut,
  a leaping ground-pound, and a hundred-fist barrage special.
- **Ranged** (Rokuro, Yui): bolts and a five-way spread, area blasts on the charged attack.
- **Healer** (Hana): casting that mends the team with every volley, a sanctuary circle that heals and burns,
  and a special that restores everyone while light rains on the demons.
- **Technical** (Ren, Genji, Kuroe): a wider parry window that triggers an instant **COUNTER**, and after a parry or
  perfect dodge the next swings throw crescent waves.
- **Balanced** (Homura): elemental bursts on every finisher.

Every swing shows its element: Water throws blue arcs and spray, Flame leaves burning arcs and rising fire,
Thunder cracks with forked lightning, Wind (Beast) whips green gusts and leaves, Light glitters gold and Dark
bleeds violet smoke; finishers erupt (water column, fire pillar, lightning from the sky, whirlwind…).
The combat buttons carry icons: a sword (or fist, arrow, staff) for ATTACK, the skill shapes with element
badges, a shield for GUARD and the element symbol on the SPECIAL.

Every slayer has a signature special: Ren summons a water dragon that coils around him and breathes a torrent
(a golden sun dragon in his Dawn form), Homura calls a flame dragon, Homura (Last Flame) drops a meteor storm,
Sora zig-zags through every demon as lightning, Raiga leaps into the sky and falls as a thunder spear, Tetsu raises
rings of stone spikes, Mina's four shadow clones strike from every side, Rokuro rains glowing arrows, Genji stops
time and cuts every demon at once, Yui sends a tidal wave across the field, and Kuroe's crescents orbit before the
moon itself falls. Strong (charged) attacks always hit at least three times as hard as a regular hit.

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
  currency pills top-right and a team power panel with the three slayers' faces.
- **Team screen**: TEAM 1-4 presets, a full-body line-up, AUTO SET, EDIT TEAM, a roster grid with element
  filters and a detail card (HP / ATK / DEF) with SELECT.
- **World map**: eight connected locations with paths, landmarks and markers (locked / you are here /
  boss / side / treasure / complete). Your current region opens as its own **area map**, a bright low-poly diorama
  (pine meadow with torii and river, icy cliffs with shrine and portal, crimson valley under a red moon, a coast
  with ship, village and lighthouse): mission stops on stone pedestals along a stepping-stone trail, locks and
  number plates, your leader on a glowing ring, and a mission list with stars, locks, the BOSS row and CONTINUE. Picking a mission elsewhere makes the leader **walk the road**
  there, camera following, before the mission starts.
- **Mission preparation** (after the reference): a story panel (breadcrumb, title, hook, recommended / your / enemy
  level, enemy portraits, objectives with crystal bonuses, reward and first-clear lines), your leader standing on the
  trail in 3D in the middle, and a YOUR TEAM panel with power, roles, character cards, CHANGE TEAM and TRAVEL & PLAY.
- **Characters screen**: element filters (ALL, WATER, FLAME, THUNDER, WIND, DARK, LIGHT), a card grid, and a detail
  card with power, HP / ATK / DEF and SELECT, UPGRADE, EQUIPMENT and SKILLS.
- **Combat HUD**: party health cards on the left, objective and progress top-left, a minimap and pause top-right,
  ATTACK, three skills, DODGE, GUARD and the SPECIAL on the right. No jump, no lock button (keyboard T still locks on).
- **Story cutscenes** in-engine: camera cuts/dollies/orbits, dialogue with typewriter text and
  speaker name plates, title cards, fades, sky changes and music — skippable and replayable from the Journal.
- **End-of-mission screen**: MISSION COMPLETE, S/A/B/C rating, counted-up EXP and gold, items,
  equipment, level-ups and new allies; NEXT MISSION / REPLAY / RETURN TO MAP / CHARACTERS.
- **Summoning as an event**: the limited banner features three Mythics (Seren, Kuroe, Garou) with their
  art. ×10 pulls open with ten orbs rising in their rarity colours; each pull, energy spirals into a sealed
  orb that you **tap to crack** (the colour can climb with each crack), it shatters, the braziers ignite,
  lightning for Legendary+, a portal opens, a silhouette steps out, then the reveal.
  Common · Rare · Epic · Legendary · Mythic, published rates, ×10 Epic guarantee and pity.
- **Daily login reward**: a random 500–5,000 gold and 50–200 diamonds every day.
- **Three Mythic banners** (Starfall Oracle · Crimson Moon · Onyx Tempest), one Mythic each, with a
  **60-second trial**: play that Mythic at max power (all stars purple, max level and skills, special ready)
  against endless demons.
- **Step-up summons**: ×10 costs FREE → 250 → 500 → 500 (double Mythic rate) → 400, then repeats. Rates are
  Common 50% · Rare 30% · Epic 14% · Legendary 4% · Mythic 2%. Each paid ×10 gives a token; 30 tokens buy a featured Mythic in the
  Shop's **Mythic Exchange**.
- **Awakening** (Legendary and Mythic only): a duplicate of the same slayer awakens them — one star turns
  bright purple, +30 max level and +30 levels, +20% stats per purple star. All six purple and maxed out =
  **GOD** status. Duplicates of other slayers give double EXP when fed to that same slayer.
- **Shop**: diamond packs (250 $2.99 … 10,000 $74.99), supplies, accessories and the Mythic exchange. Store
  billing isn't connected in this build, so pack buttons grant the diamonds directly for testing.
- Slayers go by their first names only.
- **Player profile**: on first launch the game asks your name (a filter blocks inappropriate names), age and
  birthday. Every year once your birthday passes you get 500 diamonds and 10,000 gold. Each time you open the
  game your leader greets you by name.
- **Inventory** (bag button on the home screen): all your gear with painted icons, rarity, level and who wears
  it — upgrade it or equip it on a team member. The **Gear Shop** sells gear from Common to Legendary plus
  three featured Mythic pieces (Moonfall Edge, Starweave Mantle, Phoenix Heart).
- **27 slayers**: newcomers Taro, Nami, Koji, Yuna (Common), Daigo, Hotaru, Kenta, Rin (Rare) and Akane,
  Tsukasa, Mizuki (Legendary), each with their own special. The Characters screen shows every slayer —
  the ones you don't have yet are greyed out with a lock.
- **Scarier demons**: horns, back spikes, glowing cracks, an ember heart, flickering eyes and rising motes;
  bosses and elites add shoulder spikes and a turning sigil.
- **Events** (home screen): five events, each with its own mini story and five quests from Easy to Hard,
  with an event prize for the Hard quest.
- **Open world**: walk around a peaceful Kiriha Village — houses, market, dojo, shrine, river, pond, farms,
  groves, villagers to talk to (TALK / T) and hidden treasure chests.
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
| Switch slayer (team of up to 3) | Tap a portrait | Q / E / Tab |
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
- **Currencies**: **Gold** (levels, ascension, skills, accessories), **Diamonds** (summons; earned from
  mission stars: 1★ = 1, 2★ = 2, 3★ = 5, daily login, and sometimes dropped by demons) and **XP**
  (levels, skills, ascension, the ability tree).
- **Strong attacks by rarity**: Common slayers have 1 strong attack + their special, Rare 2, Epic and up 3.
  Rarer slayers hit harder and their attacks look bigger and flashier. Every slayer has their own special
  set piece (no two share one) and their own finishing touch on every strong attack.
- **World map**: drag to scroll, scroll to zoom, tap a land (or ‹ ›) and the leader walks there.
- **Rarity & level caps**: ★2 Common (Lv 30), ★3 Rare (60), ★4 Epic (80), ★5 Legendary (100),
  ★6 Mythic (120). Ascending at the cap raises the rarity, the cap and all stats. Maxed slayers get a red aura.
- **Progression**: level up with gold + XP, feed XP, feed duplicate slayers as EXP or sell them for gold,
  skill levels 1–10, an ability tree, equipment (Sword / Haori / Accessory; accessories sold for gold), power rating.
  Every upgrade raises in-battle stats (the upgrade page previews the gain).
- **Rewards**: EXP, gold (plus coins dropped by demons, pulled into your slayer), diamonds for stars,
  XP, equipment drops, first-clear rewards, new slayers from boss clears. The results screen shows each
  team member's EXP bar filling and levelling up.
- **Character versions**: e.g. *Ren Kagami — Initiate* (Water ★4) and *Ren Kagami — Dawn Dance* (Light ★5)
  with different stats, forms and ultimate.
- **Events**: five story events plus **Scholar's Trial** (XP event: big XP rewards) and **Gold Rush** (gold farming:
  large gold rewards, and demons drop far more coins).
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
- **Phase 4 – Online**: the village hub, chat, parties of three and co-op gates are in as a local simulation
  (0.19.0). *Next:* a real backend for presence, chat and party matchmaking, accounts, cloud save
  (Firebase/PlayFab), 3v3 PvP by power rating, analytics, purchases.
- **Phase 5 – Ship**: store builds for iOS (App Store) and Android (Google Play).
