# Hashira Chronicles — Character Design Bible

The visual standard for every slayer. All characters are original to Hashira Chronicles. Reference material sets
the *level* of variety and polish only; no names, designs, outfits, weapons or poses are taken from it.

**The one-line rule:** simple enough to read in half a second, detailed enough to feel like a premium collectible.

---

## 1. Silhouette first

A character must be identifiable as a solid black shape. Every design is checked in silhouette before colour.

Each design picks **at least three** strong silhouette drivers, and no two roster members may share the same set:

| Driver | Options |
|---|---|
| Height | tiny (0.8) · short (0.9) · standard (1.0) · tall (1.1) · towering (1.25) |
| Mass | wisp · slim · athletic · broad · heavy |
| Head | big-round · standard · small-on-big-body · narrow-oval |
| Hair mass | close-cropped · top-knot · ponytail · bob · long fall · hood |
| Upper body | bare arms · fitted jacket · wide sleeves · cape · pauldron · big collar |
| Lower body | shorts + wraps · hakama · long robe · armoured skirt |
| Weapon | short blade · twin small · huge blunt · tall bow · long polearm · staff with hanging object |
| Headwear / extras | headband tails · hood point · top-knot · crest · flower pin · scarf |

**Check:** in the lineup's silhouette mode, a player should be able to name all six test characters.

## 2. Proportions

Stylised chibi-heroic, on the jointed rig (`PremiumRig`). Heads are big, but the **head-to-body ratio changes with
the character**, and that change is part of their identity.

| Type | Scale | Head : body | Legs | Shoulders |
|---|---|---|---|---|
| Small / young / support | 0.8–0.9 | 1 : 1.9 | short | narrow |
| Fast | 0.9–0.95 | 1 : 2.2 | long for the size | narrow |
| Standard | 1.0 | 1 : 2.4 | medium | medium |
| Tall / ranged / elegant | 1.05–1.2 | 1 : 2.8 | long | narrow–medium |
| Heavy / tank | 1.2–1.3 | 1 : 3.0 (small head on a big body) | short, thick | very broad |

Rules:
- Hands and feet are slightly oversized (readability). Heavy characters get bigger hands still.
- Knees and elbows always bend naturally; nobody stands with locked, straight limbs or deep crouches at rest.
- Heavy bodies carry their mass in the chest and shoulders, not the belly.

## 3. Heads and faces

- The head is an ellipsoid whose **shape varies**: round, wide, narrow-oval, or with a square jaw.
- Eyes are clean shapes: white, iris (element-tinted), pupil, two highlights, a lash line. **No lid geometry.**
- Eye character comes from width, height, tilt, iris size and the lash line:
  - round and big → young, kind, playful
  - narrow and tilted up → sharp, fast, confident
  - half-height, level → calm, sleepy, serious
  - small iris, wide white → intense, aggressive
- Brows carry the expression: angle, thickness and length are set per character.
- Mouths vary: soft smile, grin with a fang, flat line, small "o", smirk, or covered by a mask.
- Optional marks: freckles, a scar, cheek paint, blush. Use at most one per character.

## 4. Hair

Built with the sculpted hair system (`HairGeometry`, `HairStyles`):
- It grows from a scalp shell with a real hairline, so there are no helmets and no gaps.
- Hair is made of layered locks that follow the head and then fall with weight.
- Bangs always stop above the brows.
- Every style has a shade colour, a highlight and swaying sections.

Every character's hairstyle is **chosen for their personality** and adds to the silhouette. Spikes must be grouped,
curved and connected to the scalp, never random cones.

## 5. Clothing

Clothing tells the role at a glance:

| Role | Clothing language |
|---|---|
| Fast | cropped, sleeveless or fitted; trailing scarf or headband tails for motion |
| Heavy / tank | thick layers, one oversized pauldron, rope belt, bare powerful arms, heavy boots |
| Ranged | long legs showing, asymmetric (one shoulder guard, half cape), quiver |
| Support | soft round shapes, wide sleeves, big obi bow, gentle colours |
| Assassin | hood and mask, close-fitting, dark with a single sharp accent colour |
| Elemental / power | tall collar, long cape, flowing hem, ornaments that glow |

Keep pieces **big and few**: one hero piece (cape, pauldron, hood, sleeves) per character, a secondary piece, and
small trims. No noise of tiny details.

## 6. Weapons

A weapon is half of a character's identity and must have its own silhouette:

| Weapon | Where it sits at rest |
|---|---|
| Short straight blade | reverse or forward grip at the side |
| Twin sickles | low at both sides, mirrored |
| War hammer / huge blunt | planted head-down, or on the shoulder |
| Longbow (tall, asymmetric) | held low in the bow hand, upright |
| Lantern staff | upright, lantern hanging from the crook |
| Glaive / polearm | upright at an angle, butt on the ground |

Glow is used only for accents (edge lines, orbs, charms). Weapons are never just glowing sticks.

## 7. Colour

Each character has a **primary / secondary / accent** palette plus hair and skin:
- the primary covers the largest area (outer layer);
- the secondary covers the under layer and pants;
- the accent is small: trims, sash and weapon details.

Rules:
- No two characters in the roster share the same primary + accent pair.
- Every design needs at least one warm and one cool value, or a clear light/dark split.
- Palettes avoid the default black / red / blue unless it is the character's point.

Element colours are used for rim light, iris tint, trails and glow accents only:

| Element | Colour |
|---|---|
| Water | cyan-blue |
| Flame | orange |
| Beast / wind | green |
| Thunder | yellow |
| Light | warm white-gold |
| Dark | violet |

## 8. Rarity presentation

| Rarity | Presentation |
|---|---|
| Common / Rare | clean design, accent trims |
| Epic | + gold trims, one ornament |
| Legendary | + glowing element crest, gold cords |
| Mythic | + halo and orbiting element motes |

Rarity never changes the silhouette. It is decoration on top of it.

## 9. Personality in the body

Each character sets rig personality values:
- stance width, crouch, lean
- walk tempo, stride and foot lift
- idle bounce, weight shift
- twist into swings, swing dip and lunge
- attack speed

| Personality | Body language |
|---|---|
| Confident | upright, weight on one leg, weapon casual |
| Serious | square, still, slow breath |
| Aggressive | forward lean, heavy steps |
| Calm | small motions, hovering or gliding |
| Playful | bouncy idle, quick head motion |
| Mysterious | low, minimal motion, face partly hidden |
| Powerful | slow tempo, big stride, weapon heavy |
| Fast | short quick steps, high lift, forward lean |

## 10. Animation states

Every character has separate states:
- team-screen idle
- combat idle
- walk, run
- attack, heavy attack
- dodge, guard
- special, ultimate
- victory, defeat

The team-screen idle is always relaxed and natural: upright, feet planted, weapon held the way a person would hold
it standing still, with subtle breathing, weight shift, glances and blinks. It is never a combat pose.

## 11. Presentation

- Line-ups vary height, pose and colour.
- Each character has a showcase pose that is **natural and intentional**: weapon on the shoulder, hand on the hip, weapon planted, both hands resting on a staff, or a raised palm holding an element orb.
- No extreme action poses and no mannequin poses.

## 12. The six reference characters

These are the quality standard for the rest of the roster:

| # | Name | Role | Element | Silhouette drivers | Palette (primary / secondary / accent) |
|---|---|---|---|---|---|
| 1 | **Tobi Kazami** | fast melee | Beast (wind) | short, long legs, swept-back hair, headband tails, sleeveless jacket, short straight blade | lime / slate / orange |
| 2 | **Bunta Okuyama** | heavy | Flame | towering, huge shoulders, small head, top-knot, one great pauldron, war hammer | rust / bark brown / brass |
| 3 | **Sayo Mikage** | ranged | Thunder | tall and slim, long legs, high ponytail, half cape, tall asymmetric longbow | forest green / cream / crimson |
| 4 | **Nene Hanabusa** | support | Light | tiny and round, big head, bob with flower pin, huge sleeves, obi bow, lantern staff | lavender / cream / peach |
| 5 | **Nagi Kurokiri** | assassin | Dark | small, hood point, masked face, fitted wraps, twin sickles | charcoal / plum / acid green |
| 6 | **Seiran Mizuchi** | elemental powerhouse | Water | tallest, long hair tied low, high collar, long cape, water glaive, orb hand | deep sea blue / silver / aqua |
