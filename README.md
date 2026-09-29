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

### Owner unlock (0.32.1)
- `bash tools/unlock_all.sh` puts an `unlock_all` file next to this Mac's save; at launch the game then adds every
  playable slayer to that save (new ones too). The file lives only on that computer, so no one else's game
  changes. `bash tools/unlock_all.sh off` removes it (already-unlocked slayers stay).

### Monster-folk slayers, blind summons, close-up posters (0.32.0)
- **Six monster-folk slayers** (most of the roster stays human), each fighting the way their kind would:
  | Slayer | Kind | Rarity | Fights with |
  |---|---|---|---|
  | Snik | Goblin | Common | two short daggers; pointed ears, long nose, fangs |
  | Clatter | Skeleton | Rare | a bone blade; skull face with glowing sockets and a toothy grin |
  | Brimm | Demon | Epic | a hellfire trident; curved horns, fangs, spade tail |
  | Gorro | Cyclops | Epic | a spiked tree-trunk club; one big blinking eye |
  | Howl | Werewolf | Legendary | claws; muzzle, wolf ears, bushy tail |
  | Tamun | Mummy | Legendary | living bandages that lash out; wrapped head to toe, one glowing eye |
  Their strikes have their own effects: claw rakes, bandage lashes, dagger sparks, club shockwaves, bone chips and
  embers. Their skills and specials are named for their kind (Pounce, Full Moon Frenzy, Wrap Lash, Tomb Unravel,
  Goblin Mob, Graveyard Encore, Titan's Glare, Ember Pit...). Fur gets the hair-strand texture and wraps the cloth
  weave.
- **Blind summons**: nothing gives the rarity away before the big reveal. The chest takes a random 1–3 taps,
  flickers through random colours while it spins, and bursts the same golden way every time. The flying cards are
  all the same sealed colour, and each landing uses one neutral colour with a random amount of drama (lightning
  can happen for anyone). The rarity colour, and the thunder for the big ones, arrive at the reveal itself.
- **Posters** are framed close on the face (just above the hair down to the shoulders).
- **No visible neck**: the head sits on the shoulders again.

### Human cartoon look, textured surfaces, environment posters (0.31.0)
- **Clearly human, not blocky**: the literal shape pieces are gone (block pauldrons, cone spikes, cube and ball
  hair, ruffs, pom-poms, floating crystals). Shape language now works underneath, only nudging proportions, hair
  volume, poses and victories. Proportions are human-cartoon: head about 1.1–1.2×, limbs 1.1–1.25×, hands and
  feet a little large, near-human leg and torso length, and slightly smaller eyes.
- **Neck**: a human-thickness neck with a soft slope into the shoulders.
- **Texture** (Toon shader `_TexAmt` / `_TexKind`): clothes get a fine weave and soft folds, hair fine strands and
  clumps, skin a faint warmth variation. It is painted in each mesh's own space, so it doesn't swim while the
  character moves, and fine detail fades when smaller than a pixel. Weapons and glowing parts stay clean.
- **Card posters** (`PortraitStudio.HeroPoster`, `PosterEnv`): a new poster render framed from the head itself,
  so the head is always in frame, head and upper body, every slayer looking straight at the viewer with their own
  expression and pose. Each poster has its own environment and lighting from the slayer's element: sunset hills,
  moonlit night, sakura dusk, sunny ocean, deep forest, snowy peaks, volcano, thunderstorm or lantern rooftops.
  The painted backdrop matches the key light, the coloured rim light and the ambient on the slayer, plus drifting
  petals, snow, embers, rain, fireflies or twinkling stars (and lightning flashes in the storm).

### Summon chest, poster faces, U-shaped heads (0.30.0)
- **Heads**: smaller (about 12% less than 0.29), a "U" shape — a jaw keeps the cheeks wide lower down and rounds
  off at the bottom (the pointed chin is gone) — and the head is lifted onto a visible neck.
- **Summon chest** (`SummonStage.Chest.cs`), replacing the sword-draw intro and the shrine rite: a treasure chest
  slams onto the altar, lifts and spins faster and faster, upgrading through the rarity colours up to the best
  slayer in the pull, slows to face you and rattles. TAP TO OPEN (1 tap, 2 for Epic+, 3 for Legendary+); each tap
  jolts it and cracks the lid. The lid bursts open with a light pillar, shockwave and (for the big ones)
  lightning, and the cards fly out as before. SKIP still skips everything.
- **Poster cards**: zoomed out to show more of the slayer, and every card has its own expression — smile, smirk,
  confident grin, angry shout, big happy laugh or determined frown (mouth, eyes and brows) — and one of four poses
  (hero stance, charge, cool lean, playful tilt), chosen from their personality.
- **Enemies no longer float away in long combos**: a juggled demon is lifted less the higher it is, never above
  2.2 m, and stops being lifted after 1.6 s in the air, then falls. A demon knocked out of the air state some other
  way (a parry stagger) now falls back down too.
- **Co-op has no pause button** (and Esc / P don't pause there).

### Stylized hero redesign (0.29.0)
Original characters, redesigned around mobile-hero design principles (exaggerated proportions, strong
silhouettes, simple shapes, bold colour blocking). Nothing is copied from any other game.
- **Shape language** (`CharacterVisual.Stylized.cs`): every slayer has a dominant shape — Circle (support,
  friendly), Square (tank, heavy), Triangle (fast, aggressive) or Diamond (magical, elegant) — from their role,
  style, weapon and manner. It drives their proportions, silhouette pieces, hair, weapon details and victory.
- **Proportions per shape**: Square is wide with huge hands and boots; Triangle is a V with wide shoulders over a
  narrow waist; Circle has the biggest head and a short round body; Diamond is tall and slim. All get shorter
  torsos, chunkier limbs, bigger hands, shoes and weapons, and bigger showcase poses.
- **Silhouette pieces**: 2–4 big clothing shapes in the slayer's shape (block pauldrons and a heavy collar;
  shoulder spikes, a sharp popped collar and a pointed sash tail; puffed shoulders, a ruff and a pom-pom; a tall
  diamond collar and a floating element crystal).
- **Hair**: existing hair pushed bigger, then large stylised clumps (swept spikes, round puffs, a flat top or a
  tall crest), anime side locks and a bouncy cowlick. Clumps and locks sway with movement.
- **Faces**: eyes now have a dark iris rim, a shadowed upper iris, a glowing lower iris, three highlights, a
  winged lash line and a lid crease; brows taper from a thick inner end; a soft defined chin. Face parts are no
  longer merged into the head mesh, so brows and mouth can animate with expressions (this also fixes the 0.27
  brow/mouth changes, which had no effect).
- **Signature weapons**: a guard, head or crown in the slayer's shape, a glowing element line down the blade and
  a streaming pommel ribbon; bows get bladed tips, shields an emblem, gauntlets shaped knuckles.
- **Colour**: vivid colours with hair, outfit and under-layer pushed apart; fewer tiny details (one bold leg wrap
  instead of four thin bands, one stud instead of fifteen).
- **Animation**: stronger anticipation with a held beat before each swing, an impact pop, and a springy
  overshooting recovery; hits snap away, knock back and rebound; a signature ultimate pose (gather, leap with
  the weapon raised, power hold, drop); skill flourishes per shape; victories per shape (spinning leap, heavy
  stomp and flex, happy double hop, elegant floating twirl); defeat staggers back, drops to the knees and
  slumps, still breathing.
- **Cards by rarity**: Rare adds rivets; Epic adds corner gems and a light sweep; Legendary adds gold corner
  brackets, a crest with a glowing gem and twinkling sparkles; Mythic and GOD add holographic foil, rising
  embers and a colour-shifting animated rim.

### Chunky hero style (0.28.0)
- **Chunkier proportions** in the style of mobile hero games: bigger heads (×1.42), shorter and thicker legs,
  broader torsos, thicker limbs, big hands and boots, and bigger weapons. Demons get a milder version.
- **Bold hero faces**: thicker brows tilted into a confident, ready-to-fight look and wider mouths, built from
  each design's existing face parts, so the talking and mood expressions still work.
- **Smoother toy shading**: wider soft transitions between light and shadow, a stronger soft rim, and more top-down
  volume on bodies. Weapons stay crisp.
- Portraits are framed a little wider so the bigger heads aren't cropped.
- All designs remain original; only the art style is inspired by those games.

### Collectible cards and toy-figure shading (0.27.0)
- **Character list cards fixed**: the diagonal "light rays" on the posters were rotated rectangles, which IMGUI
  can't clip, so they poked out of the cards as long colour sticks. The posters now use no rotation at all: an
  element gradient, pulsing glows and the slayer's action art with a dark ink edge, all clipped to the card.
- **Collectible-card style roster**: every slayer is a thick, bevelled card in their rarity colour (lit from the
  top, dark ink edge), a portrait window, a rarity ribbon, a level plate (GOD/MAX highlighted), stars, an element
  gem in the corner and a team badge. The selected card floats and glows. Locked slayers use the same frame in grey
  with a lock over a shadowed portrait.
- **Toy-figure shading**: bodies get a soft sky light from above and a gentle darker underside, so the chunky shapes
  read as smooth 3D figures, plus a slightly broader soft sheen. Weapons keep their crisp cel look.
- All characters remain original designs; the style is inspired by mobile hero games, nothing is copied.

### Mythic cinematics, slayer sheets, sticker posters (0.26.0)
- **Fixed: the player floating and looking giant in co-op.**
  - Specials lift the model off the ground (up to 8 m for Raiga's). When a move was interrupted, `StopAction` never put the model back down, so it hovered near the camera.
  - Interrupting a move now resets the lift, and any leftover lift eases back to the feet whenever no move is running.
- **Fixed: the end of the Gate of Frost map.**
  - Background mountain ranges were placed in rings 150 m from the middle of the route, but routes are about 255 m long, so 4 of 36 mountains cut into the last places (Ice Cave Camp, Summit Pass and the summit arena).
  - Each range is now pushed out until it clears the road and every place; the check in the harness now finds 0 overlaps on every map.
- **Co-op teammates:** they dash nearly nonstop (every 0.3–0.55 s), swing about twice as fast, and their strong attacks (combo finishers, specials, ultimates) hit **10×** harder. They no longer chat during co-op missions.
- **Gamer names:** simulated players use a shared list of real-world-style, silly, cute, sassy and powerful names (JakeTheSnake, NoodleNinja, MochiMuffin, QueenOfCrits, StormbreakerX...).
- **Missions:** 12 daily and 10 weekly. New kinds: co-op clears, arena matches and wins, event quests, 3-star clears, ultimates, skills, perfect dodges and messaging a friend.
- **Mythic slayers:**
  - Stats: +20% HP, +22% ATK, +15% DEF, +12% crit rate, +40% crit damage and +25% special damage.
  - Their strong attack becomes a short cinematic: the camera swings in and time slows while the element gathers, the blow lands, then three rolling aftershocks tear outward.
  - Their special gets an encore: element strikes rain on nearby demons before a closing burst.
- **Summon slayer sheet:** DETAILS on the banner, or tapping any pulled slayer, shows:
  - action art, element, role and weapon, and the element matchup;
  - stats at Lv.1 next to fully starred, awakened max level;
  - every skill and the special, with what it does, its damage and its cooldown.
- **Character list posters:** each card is a 2D sticker poster: an element-colour burst with slowly turning rays and a diagonal stripe, and the slayer in their action pose with a white sticker outline and a dark ink edge.
- **Cooler sounds:** dash, crit, slam, impact, the ultimate start and finish, and every element (plus a new Earth rumble) are rebuilt with a whip-crack transient, a doppler body, a sub drop, a metallic shimmer and a room tail.
- **Livelier, richer slayers:** a big sparkle in every eye (it blinks with the eye), rosy cheeks, richer saturated body colours, and little happy hops and wiggles when idle.

### Elements, faster co-op, softer slayers (0.25.0)
- **Element cycle:** Water › Fire › Earth › Wind › Thunder › Water.
  - Hitting the element you beat deals **+15%**, and hitting the one that beats you deals **−15%**. Light and Dark are neutral.
  - **Earth** is a new element with its own colour, leaf icon, filter tab, promo theme, slash, impact and finisher effects, and sounds.
  - Tetsu (Stone), Taro (Boulder) and Daigo (Mountain) are now Earth slayers. The Forest Demon, Forest Demon boss and Bone Warrior are Earth monsters.
- **Quest matchups:** every quest page shows each demon element's matchup (e.g. *FIRE demons: Water types +15% · Earth types −15%*) and how each slayer on your team fares against the main demon (▲15% / ▼15% / —).
- **Co-op teammates:**
  - they run about 30% faster and dash-lunge at demons 3–16 m away (only along a clear line);
  - they swing about 30% faster with shorter recovery, and use specials about a third sooner and ultimates 30% sooner;
  - they hunt toward the objective instead of lingering by you.
- **Characters screen:**
  - the NEW DESIGNS button is gone;
  - cards are bigger, with faces zoomed in and soft pastel element tints;
  - the selected card bobs, and a sparkle twinkles on each card.
- **Softer, squishier slayers:**
  - gentle shading steps instead of hard bands, a matte finish (no hard shine), and a softer rim and saturation; weapons keep their crisp edges and glint;
  - heads are a softer, slightly wider and flatter shape instead of a ball;
  - a springy squash-and-stretch body: it breathes at rest, hops on each step, and jiggles on attacks, hits and dodges.

### Ground placement and AI rebuild (0.24.0)
**Characters sinking into or hovering over the ground: root cause and fix.**
- **Cause:** all movement code works on a flat plane. `BattleController.ClampToArena` forced `y = 0`, and about 75 other places write positions assuming flat ground. The new worlds aren't flat: river trenches dip beside the bridges, bridge decks sit above the riverbed, and banks rise at the edges.
- **Measured** with the real `Journey` and terrain code in an offline harness: on walkable ground, feet at y=0 floated up to 1.4 m on the dips next to the bridges and were buried up to 0.1 m at the edges. The ground under the bridge planks is up to 3.2 m lower, so the deck needs to count as ground.
- **Fix: `World/Ground.cs`.** The world builder hands over the exact terrain height grid it meshed. Heights are read with the same triangle split the mesh uses, so they match the drawn surface exactly. Bridge decks are registered as platforms, with their arch or sag.
- **Fix: `World/GroundFollower.cs`,** on every fighter (added in `Combatant.OnEnable`).
  - Before gameplay runs each frame it takes the ground height out, so every movement script keeps working on the flat "height above ground" plane it was written for. That covers walking, dodges, lunges, knockback, leaps, specials and respawns.
  - After gameplay, and before the camera, it adds the real ground height under the feet.
  - Recovery: bad (NaN) or flung positions go back to the last good spot, anyone below the ground plane stands back up, and anyone inside a solid or off the walkable area is eased out to the nearest valid ground. Health and state are untouched.
- **Spawns are validated:** enemies (`SpawnEnemy`) and party slayers are placed on walkable ground, never inside scenery. Telegraphs, demon auras and range rings sit on the real ground too.

**AI navigation (`Missions/NavAgent.cs`),** used by party slayers and demons.
- It routes along the road when the straight line is blocked (over the bridge, not into the river bank).
- Short feelers slide it around rocks and walls, keeping to one side so it doesn't dither at corners.
- It detects being stuck (barely moved for 1.2 s) or circling (no progress for 3 s) and takes the best reachable detour. It re-routes as the target moves.
- **Tested offline** with the real `Journey`, `Obstacles` and `NavAgent` code, on snow, forest and volcano maps with the bridge banks and roadside rocks. Obstacles for the volcano's lava moat and side river weren't modelled.
  - From all around the near end of a bridge to the far side (the screenshot situation), the old straight-line movement failed 19% of runs (stuck against the invisible bank), and the new agent failed 0%.
  - On 400+ random routes, the old movement reached 93–96% of targets, and the new agent 100%.

**Party AI rebuilt as a state machine** (`PartySlayer`).
- It runs Observe → Decide → Act several times a second. States: Search, Navigate, Position, Attack, Special, Ultimate, Reposition, Dodge, Retreat, Help Ally, Defend, Recover.
- It observes target health, its own health and recent damage, cooldowns, enemy groups, hurt allies, red zones and walkable lines.
- **Roles:**
  - The Vanguard holds the line between enemies and the weakest ally, leaps in when 2+ enemies are bunched (only with a clear landing), and braces with Iron Wall when it's taking heavy damage.
  - The Duelist flanks, prefers weak or isolated targets, and only Flash Steps along a clear line (otherwise it repositions for a better angle).
  - The Skirmisher keeps 6–8 m, backs off when rushed, and fires its volley only with 2+ enemies in the fan.
  - The Support stays with whoever needs it, keeps out of melee, heals hurt allies, and saves Sanctuary for when 2+ allies are low.
  - Ultimates need a payoff (3+ enemies close, or an elite/boss) and are cancelled if the target is gone.
- **Personalities:** Aggressive, Balanced, Defensive and Tactical change retreat thresholds, spacing, how many enemies a special needs, and target focus.
- **Reactions:** dodging out of red zones to spots not inside another zone, sidestepping after heavy hits, retreating when low, responding when an ally or the player goes down, smooth turning and walk/run blending.

### Friends who text back, dangerous banners, meaner demons (0.23.0)
- **Friend chat rebuilt** (`Meta/FriendChat.cs`). Friends reply in real time, anywhere from 3 minutes to 12 days: usually minutes to hours, sometimes days.
  - Pending replies are saved, so they still arrive after you quit and come back. A long wait comes with a "sorry, just saw this!".
  - They read everything you sent since their last reply and answer it together, in order.
  - They understand slang (wyd, wdym, hbu, idk...), ask things back and understand your answer to their question, and explain what they meant when you ask.
  - **Learning:** each friend remembers (in your save) who you main, what you like, what you pulled or cleared, how you've been feeling and what to call you. They bring it up later ("feeling better?", "how's Seren treating u?").
  - The more you talk, the warmer and faster they reply. They also message you first every few hours to few days.
  - Asked if they're a bot, they say honestly that they're simulated players until real online is ready.
- **Event banners:**
  - painted with themed gradients, rays and particles;
  - the event's demon huge on the right with a red-hot silhouette;
  - claw slashes, glowing cracks, a dark vignette, a pulsing red rim and a THREAT ☠ rating;
  - the chosen event's header gets the same treatment, and so do limited banners on the home carousel.
- **Summon banners:**
  - the featured Mythic is shown in an action pose (their battle stance, turned three-quarters, from a low heroic angle) with an element halo;
  - the other featured Mythics stand behind in shadow;
  - switching banners slides the slayer in with a flash, and a blade gleam sweeps across every few seconds.
- **Demons attack more:**
  - roughly a quarter less time between attacks, and 4 can attack at once instead of 3;
  - normal demons mix 1- and 2-swipe combos with pounces;
  - fast demons also claw, tanks also combo, archers fire 3-shot volleys, and elites do 3-hit combos.
- **Demon visuals:**
  - a star glint pops over a demon's head the moment it starts an attack;
  - every demon has a pulsing ground aura in its colour that flares red while winding up, plus drifting embers;
  - they burst out of the ground on spawn and burst apart in their colour on death.

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
