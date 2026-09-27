# CLAUDE.md

Unity (C#) mobile 2.5D action RPG prototype. See README.md for features, controls and roadmap.

## Ground rules
- Everything is constructed from code at runtime (`GameBootstrap` → `GameManager`). Do not add
  scene objects or prefabs unless replacing placeholder art; keep the project runnable from an empty scene.
- Content (characters, abilities, enemies, missions, equipment) lives in `Assets/Scripts/Data/GameDatabase.cs`.
  Prefer adding data over adding code. Keep all names/characters original — no copyrighted IP.
- Namespace `HashiraChronicles`. One MonoBehaviour per file, file name = class name.
- Target C# 7.3-compatible syntax that works on Unity 2022.3 LTS and Unity 6; avoid APIs obsolete in
  Unity 6 (`FindObjectOfType`, `Rigidbody.velocity`) and never use `??` / `?.` on UnityEngine.Object.
- No physics: hit detection goes through `CombatSystem.Query/HitArc/HitRadius` over `Combatant.All`;
  movement is clamped with `BattleController.ClampToArena`.
- `Time.timeScale` is owned by `TimeController` (pause, slow motion, hit-stop) — don't set it elsewhere.
- UI is IMGUI in a 1080-tall virtual space (`HudLayout`); battle touch hit-areas and visuals share `HudLayout`.
- Screen flow lives in `GameManager.GoTo`: each screen maps to one 3D stage (HomeStage, MapStage, SummonStage,
  or none for battle/cutscene). Stages share the world origin and only one is active at a time.
  Missions start through `GameManager.BeginMission` (travel → first-time cutscene → battle); story scenes are
  scripts in `Story/CutsceneDatabase.cs`.
- Saves carry `PlayerData.saveVersion`; bump `PlayerData.CurrentVersion` only when old saves can't be migrated.

## Verifying changes without the Unity editor
Compile the runtime scripts against Unity's reference assemblies (NuGet `UnityEngine.Modules`) with Mono:

```sh
nuget_dir=/tmp/unityref && mkdir -p $nuget_dir && cd $nuget_dir && \
  curl -sSL https://api.nuget.org/v3-flatcontainer/unityengine.modules/2021.3.33/unityengine.modules.2021.3.33.nupkg -o m.nupkg && unzip -qo m.nupkg
cd <repo> && REFS=$(ls /tmp/unityref/lib/net45/*.dll | grep -v 'UnityEngine.dll$' | sed 's/^/-r:/' | tr '\n' ' ')
mcs -langversion:Latest -target:library -out:/tmp/game.dll $REFS $(find Assets/Scripts -name '*.cs' -not -path '*/Editor/*')
```
Editor scripts can be checked too, against the monolithic 2021.1 references in NuGet `Unity3D.SDK`
(`lib/UnityEngine.dll` + `lib/UnityEditor.dll`) by compiling *all* scripts together. `NamedBuildTarget`
(2021.2+) is the one expected error there. Unity 6.6 turns some obsolete APIs into errors (e.g.
`Object.GetInstanceID`): avoid obsolete members even when these older references accept them.

Art direction: simple chibi figures built in code (`CharacterVisual.Heroes.cs`, `CharacterVisual.Monsters.cs`),
flat low-poly worlds, portraits rendered live by `PortraitStudio`. Follow docs/ART_BIBLE.md and keep that
simplicity. Imported models are disabled (`GameConfig.UseImportedModels = false`).
