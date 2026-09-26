# Hashira Chronicles — vertical slice

A mobile 2.5D anime action RPG prototype in **Unity (C#)**, inspired by the structure of
collectible action RPGs: real-time hack-and-slash with a three-slayer team, breathing-style skills,
cinematic ultimates, multi-phase bosses, and a progression loop:

**Collect → Upgrade → Build Team → Fight → Earn Rewards → Upgrade → Repeat**

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
| Sprint | Keep moving ~0.5 s | Keep moving |
| Breathing forms 1–3 | 1 / 2 / 3 | 1 2 3 or U I O |
| Ultimate | ULT (when gauge is full) | R / L |
| Switch slayer | Tap a portrait | Q / E / Tab |
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
- Rigged-model support: drop Higgsfield/Mixamo characters in `Assets/Art/…` and run **Hashira Chronicles → Build Character Prefabs**.
  See [docs/ART_PIPELINE.md](docs/ART_PIPELINE.md).

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
- **Story**: Chapter 1 *Trial of Wisteria Hill* and Chapter 2 *The Lantern Quarter* (6 missions,
  2–4 min each) with waves, bosses and three objectives each (defeat N demons / nobody falls / par time).
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
                 MaterialFactory, MeshFactory, MenuStage
    Audio/       AudioManager
    UI/          UIManager (+ CharacterScreens, BattleHUD), MobileControls, HudLayout, UIStyles
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
- **Phase 3 – Live systems**: summoning with pity, events & event currency, daily missions, shop,
  larger collection.
- **Phase 4 – Online**: accounts, cloud save (Firebase/PlayFab), 4-player co-op raids, 3v3 PvP by
  power rating, analytics, purchases.
- **Phase 5 – Ship**: store builds for iOS (App Store) and Android (Google Play).
