# Art bible: Blades of Dawn

The whole game follows one simple, charming look taken from the original reference sheets. When in
doubt, make it simpler, not more detailed. No realism, no painted textures, no imported models.

## Characters (chibi)
- **Head**: one big smooth round head in the character's own skin tone (porcelain, light, medium, tan,
  brown or deep), tall black oval eyes with a shine, brows that show personality, a small smile, rosy cheeks.
- **Rating**: PG. Spooky, never gory: monsters have glowing eyes but no fangs, no bones or blood in the world.
- **Body**: a smooth bell-shaped dark robe with coloured hem trim, crossed collar and a sash with a knot,
  sleeves with cuffs, hands and little shoes under the hem.
- **Lighting**: soft cel shading with a gentle light-to-shadow ramp; light is clamped so faces never wash out.
- **What makes each slayer unique**, set in `GameDatabase.ApplyLooks()`:
  - hair: Messy, Spiky, Long, Ponytail, Braid, Short, Bun, Hood, Cap, Straw Hat, Wild, Crest, Bob, Twintails or Curly
    (every playable slayer has a different one)
  - weapon: Katana, Twin Blades, Greatsword, Sword & Shield, Spear, Staff, Bow, Fans, Cleavers, Cane or Moon.
    Every blade glows.
  - body height and width
  - accessories: scarf, cape, pelt, armour or bell
  - motion personality: Steady, Nervous, Aggressive, Graceful, Stoic, Confident, Sly or Light.
    This sets walk bounce, lean, idle sway and fidgets.
  - combat style: Swift, Heavy, Ranged, Technical or Balanced. This sets timing, follow-ups and the special.
- **Animations** per slayer: idle, walk, run, attack, heavy, special, hit, victory, defeat, front and back.
  Preview them all on the character screen's ANIMATIONS panel.

## Monsters
- Same simple primitives as the heroes, but dark bodies and **slanted glowing eyes with a halo**, a
  jagged grin, a creeping dark aura and a pulsing stain on the ground in the demon's accent colour.
- Each form has its own silhouette: hunched ghoul, lanky stalker, masked shadow, bull beast, bone warrior,
  tree demon, lava lion, floating void/ice wraith, knight, hunter, imp, oni, sentinel.
- Bosses are bigger, have their own arena, intro reveal and a defeat cinematic.

## Worlds
- Flat colours from each region's `ArenaTheme`: ground, road, foliage, fog and lantern light.
- Roads are layered:
  - Verges: region props such as rocks, branches, logs, mushrooms, ruins, rubble, carts, signs, bones,
    spikes and ice.
  - Foreground life: swaying grass, fog puffs and weather.
  - Background: rings of hills, then far mountains fading into fog, plus prowling silhouettes with glowing eyes.
  - Destination: the next region's landmark on the horizon.

## UI
- Flat, rounded, colour-coded tiles with a soft drop shadow and a lighter top band. These are the helpers in `UI/Tiles.cs`.
- Colours:

  | Screen or control | Colour |
  |---|---|
  | PLAY, CLAIM, TRAVEL & PLAY | red |
  | SUMMON, CHANGE TEAM | purple |
  | CHARACTERS | blue |
  | TEAM, CLAIMED | green |
  | EQUIPMENT | orange |
  | MISSIONS | maroon |
  | SHOP | teal |
  | SETTINGS | grey |

- Currency pills top-right; thick yellow progress bars; portraits in element-coloured rounded frames.
