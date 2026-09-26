# Art Bible — Blades of Dawn

All art below was generated with **Higgsfield** (GPT Image 2.5 for 2D, Meshy image-to-3D for models) and is
downloaded into the project by `tools/fetch_art.sh` (called automatically by `run.sh`).
Everything is original: no existing anime characters, names, or designs.

## Consistency rules
- **Ren's master image is the style anchor.** Every other character, monster and scene was generated with it
  (or with that character's own master image) as a reference, so faces, proportions and rendering match.
- **Never redesign a character between scenes.** When generating new art of an established character, pass that
  character's master job as the reference (`medias: [{ value: <job id>, role: "image_references" }]`) and describe it as
  "EXACTLY the same character as the reference".
- The 3D models were made from the same master images, so the character in menus, on the world map, in battle
  and in cutscenes is one model everywhere.

## Cast (master images → reference job IDs)
| Character | Look | Palette | Master job |
|---|---|---|---|
| **Ren Kagami** (hero) | Black hair with ember-red tips, short ponytail, amber eyes; high-collar black jacket, long deep-teal sleeveless coat with pale wave-crest hem, white sash, shin wraps; katana with a blue edge | black, deep teal, white, ember red | `d21bda22-4bf5-4e12-8f8a-830b1736aca3` |
| **Veyrath** (Demon Lord) | 2.1 m, silver-white waist-length hair, horn crown, dark veins, crimson slit eyes; black lacquer armour, gold eclipse emblem, tattered crimson cape, hollow in the chest where half his heart is missing; black greatsword with crimson core | black, crimson, gold, silver | `c8a416d1-8af6-4589-ada0-1780372f871a` |
| **Sora Ikazuchi** | Storm-grey hair with a gold streak, anxious yellow eyes, grey kimono jacket with gold lightning embroidery, long indigo scarf, katana on the back | grey, indigo, gold | `95c11af9-3b9b-406f-b16f-537f53b3f790` |
| **Kiba Arashi** | Tall, tanned, spiky navy hair with red band, wolf-pelt mantle, bronze lamellar vest, twin serrated short swords | navy, grey fur, bronze, red | `6e52f60c-7ec0-46c2-99f2-114788abfe5c` |
| **Hana Shiraume** | Silver-lavender braid with wisteria pins, white/lavender layered healer robes, purple obi, medicine satchel, wisteria-guard sword, silver bell | white, lavender, purple | `e634dafc-0b14-4b4b-bdec-5d330379edd2` |
| **Captain Tetsu Ganryu** | Broad, bearded, nose scar; steel-blue plate with lion crest, navy cape, tower shield, broadsword | steel blue, navy, gold | `be3c19ce-475b-4e8c-8049-5f2a40b4c1d9` |
| **Homura Enjoji** (Flame Pillar) | Crimson high ponytail, golden eyes, black lacquer armour, crimson commander coat with gold phoenix embroidery, molten-glowing katana | crimson, black, gold | `78b638ba-4849-44fa-96b9-da786016a99a` |
| **Master Tessai** | Elderly, long white hair and beard, faded blue-grey robes, straw hat on his back, cane-sword | blue-grey, white, straw | `bf217b8d-d531-4d40-9371-5c5e62389667` |

Reference sheets: Ren turnaround (front/side/back) `75970c22-…`, Ren pose sheet (idle/combat/attack/ultimate)
`7599d8bd-…`, Veyrath sheet `921ee96e-…` — saved to `Assets/Art/Reference/`.

## Bestiary
Each enemy has its own body plan in `CharacterVisual.Monsters.cs` (`EnemyDefinition.form`), matching its concept.

| Enemy | Form | Concept | Role / weakness |
|---|---|---|---|
| Shadow Demon | `shadow` | gaunt smoke body, porcelain mask, long blade claws | fast lunges — parry to stagger |
| Blood Beast | `beast` | bull-sized quadruped, ramming skull, glowing veins | charges — sidestep, hit the flank |
| Bone Warrior | `bone` | armoured skeleton, green spirit fire, giant cleaver | unblockable overhead — dodge |
| Forest Demon | `forest` | walking corrupted tree, antlers, root arms, glowing sap | slow slams — Flame burns it |
| Flame Beast | `lion` | lava-rock lion, burning mane, fire-whip tail | fire breath — Water douses it |
| Void Demon | `void` | floating shroud, one magenta eye, rune rings | ranged orbs — close the gap |
| Demon Knight | `knight` | spiked black plate, horned helm, kite shield, halberd | shield — flank or heavy attack |
| Ancient Guardian (boss) | `guardian` | four-armed stone colossus, halo of stones, ring-blade | Dark cracks the runes |
| Demon General (Morgrath/Vex/Nyx) | `general` | ash-grey warlord, war banners, twin blades | learn the patterns, parry |
| Veyrath (final boss) | `lord` → wings | regal demon king; second form unfolds six wings and the eclipse ring | Light wounds the eclipse |

## Regions (key art → mission pages, Journal)
Village (golden hour, farms, shrine) · Forest of Shadows (giant trees, fog, broken bridge) · Hakuro Pass (snow,
cliffs, waterfalls) · Royal Capital Solmere (white walls, blue roofs, arena) · Forgotten Temple (circular court,
seals, sleeping guardian) · Ashen Wastes (lava, bone spires, eclipse) · Castle of the Eclipse (throne room) ·
World map diorama. Each region's art shows the **next** landmark on the horizon (mountain → capital → castle),
which the in-game journeys mirror with distant silhouettes.

## 3D models
| Model | Source | File |
|---|---|---|
| Ren | Ren master → Meshy image-to-3D (textured, PBR, auto-rigged, A-pose) | `Assets/Resources/Characters/ren.glb` |
| Veyrath | Veyrath master → same pipeline | `Assets/Resources/Enemies/boss_veyrath.glb` |

They are imported by glTFast, scaled/grounded automatically (`CharacterVisual.NormalizeModel`) and animated
procedurally (`ProceduralRig`: idle breathing, walk/run cycles, guard, sword arm following the blade). For
final-quality motion, add real clips (Mixamo or Higgsfield rig animations) as described in `ART_PIPELINE.md`.

## Next assets to generate (in priority order, ~35 credits per rigged model)
1. Rigged models for Sora, Kiba, Hana, Tetsu, Homura (use each master job as the image source).
2. Shadow Demon and Blood Beast models (most frequent enemies), then the Ancient Guardian.
3. Animated clips (idle, run, attack) via `enable_animation` + `animation_action_id`, or Mixamo.
