> **Currently disabled.** The game now uses the in-engine chibi style (see ART_BIBLE.md) and
> `GameConfig.UseImportedModels` is `false`. This pipeline is kept for reference only.

# Art pipeline: Higgsfield → rigged, animated characters in game

The game already supports real rigged characters. Drop them in the right folder, run one menu
command, and the combat, skills, ultimates and menus use them automatically. No code changes are needed.
Characters that have no model keep using the built-in placeholder figures.

## 1. Concept art (Higgsfield, image)

Generate one **front-view, full-body A-pose** image per character on a plain background. This is the
input for image-to-3D, so clean silhouettes and arms held away from the body matter more than drama.

Prompt template:

> Full-body 3D anime action-game character model render, neutral A-pose, front view, entire body visible,
> plain light grey background, soft even lighting. Original character: *(age, hair, eyes, outfit, haori
> pattern/colour, weapon and where it's worn)*. Stylised cel-shaded premium anime mobile action RPG look,
> clean readable silhouette, symmetrical pose.

For **consistency** (same face and outfit everywhere), reuse the *same* concept image as the reference for
every later step: 3D model, portraits, UI art and ultimate cut-ins. Never regenerate the character from text.

## 2. 3D model (Higgsfield, image → 3D)

Cheapest good option: **Tripo H3.1 image-to-3D** (textured + PBR, ~9 credits). Meshy with built-in
rigging costs ~35–44 credits; you don't need it, because Mixamo rigs for free in step 3.

- Target ~15–30k triangles for heroes, ~8–15k for demons (mobile budget).
- Download the GLB.

## 3. Rig + animations (free)

1. Open the GLB in **Blender** (free): File → Import → glTF 2.0. Then File → Export → FBX.
2. Go to **mixamo.com**, upload the FBX, place the rig markers, and let it auto-rig.
3. Download the character **"FBX for Unity", "With Skin", T-pose**, and save it as `<Name>.fbx`.
4. Download each animation below as **"FBX for Unity", "Without Skin"**. Tick **"In Place"** for
   movement clips. Rename each file to `<Name>@<State>.fbx`:

| State | Mixamo search suggestions |
|---|---|
| Idle | "sword idle", "great sword idle" |
| Walk / Run / Sprint | "sword walk", "sword run", "sprint" (In Place) |
| Attack1 … Attack5 | "sword slash", "great sword slash", "sword combo" (use a finisher for 5) |
| Heavy | "great sword high spin attack", "stable sword outward slash" |
| DashAttack | "sword dash attack", "stable sword lunge" |
| Skill1 … Skill3 | "spin attack", "sword jump attack", "360 slash" |
| Ultimate | "great sword casting", "sword power up" + any big finisher |
| Dodge | "roll", "dodge back", "sword dodge" |
| Guard | "sword block idle" (loops) |
| Hit | "sword hit reaction", "impact" |
| Knockdown / GetUp | "knocked down", "getting up" |
| Victory / Defeat / Death | "victory", "defeated", "dying" |

Any clips you skip are fine; the game falls back gracefully.

## 4. Into Unity

```
Assets/Art/Characters/<character id>/   e.g. Assets/Art/Characters/ren_initiate/
    Ren.fbx
    Ren@Idle.fbx  Ren@Run.fbx  Ren@Attack1.fbx ...
Assets/Art/Enemies/<enemy id>/          e.g. Assets/Art/Enemies/grunt/
```

The ids are listed in `Assets/Scripts/Data/GameDatabase.cs`: `ren_initiate`, `sora_initiate`,
`kiba_initiate`, `homura_pillar`, `ren_sundance`, `grunt`, `runner`, `brute`, `spitter`, `elite`,
`boss_thousandarm`, `boss_goken`.

Then use the menu **Hashira Chronicles → Build Character Prefabs**. This sets every FBX to Humanoid, builds
an Animator Controller with a blended walk/run/sprint locomotion and all the combat states, and saves
`Assets/Resources/Characters/<id>.prefab`. Press Play.

At runtime the model is re-shaded with the game's cel/outline shader so every character matches.
For a custom weapon-trail position, add an empty child named `WeaponTip` at the blade tip.
By default the trail uses the right hand.

## 5. Other assets worth generating

- **Portraits / UI art:** Higgsfield image, using the concept image as reference, square, for
  collection cards and team portraits.
- **Ultimate cut-ins:** wide 21:9 dramatic shots of each character, for the ultimate banner.
- **Environment props:** Tripo image-to-3D of lanterns, gates and houses to replace the primitive props in
  `ArenaDecor.cs`.
- **Voice:** Higgsfield voice tools for original battle cries (never extract audio from an anime).
