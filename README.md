# CGDP

Unity 6 (URP) first/third-person shooter sandbox: movement, weapons with procedural
generation, melee, grenades, abilities, status effects, hitboxes, enemy AI, audio and a
runtime-built HUD.

- [FEATURES.md](FEATURES.md) — what the game does
- [SETUP.md](SETUP.md) — how every system is wired into a scene
- [TASKS.md](TASKS.md) — open work
- [CLAUDE.md](CLAUDE.md) — coding guidelines

Open `Assets/Project/Scenes/Sandbox.unity` to play.

## Project layout

```
Assets/
  Project/                  everything owned by this project
    Art/                    Animations, Fonts, Materials, PhysicsMaterials, Shaders, Textures
    Audio/                  Music, SFX
    Data/                   ScriptableObject assets, one folder per feature
      Abilities/  Audio/  CameraEffects/  Combat/  Crafting/  DevTools/  Enemies/  Feedback/  Flow/  Input/  Items/  Loot/  Map/  Meters/
      Player/  Quests/  Stats/  Targeting/  Timing/  UI/  Weapons/
      (generic starting points are named Default<Type>, e.g. DefaultEnemyData)
    Prefabs/                Characters, Environment, Pickups, UI, VFX, Weapons
    Scenes/                 Sandbox.unity (+ its baked NavMesh folder)
    Scripts/                runtime code, one folder per feature (CGD.Runtime assembly)
      Abilities/            ability assets (incl. TargetedAbility) and the player's ability slots
      Audio/                audio pool, sound banks, surface lookup
      CameraEffects/        shake, kicks, FOV punches, lag, view blends (CameraEffectsController), CameraImpulses
      Combat/               Damage/, Health/ (HealthManager, Hitbox, Destructible), StatusEffects/, Actions/, Projectile, Stunnable, Noise
      Core/                 shared utilities (cooldowns, ranges, Culling/, Pooling/ — PrefabPool, IPoolable,
                            Random/ — Seed, RandomStream, SeedVariants; StateMachine/ — StateMachine<T>, IState)
      Crafting/             RecipeDefinition, Crafter (rules), CraftingStation
      DevTools/             dev console: DevConsole (commands), DevCommands (cheats), DevCatalog
      Editor/               editor-only inspectors and tooling (CGD.Editor assembly), incl. Map/ (Map Graph window)
      Enemies/              enemy components, perception, and AI/ (state classes)
      Feedback/             FeedbackBus, FeedbackPreset, FeedbackPlayer (feed, flash, rumble, shake), combat/quest feedback
      Flow/                 GameFlow, GameState/GameStateMachine, settings and UI commands
      Input/                PlayerInputHandler and binding settings
      Interaction/          IInteractable, doors, switches, EventInteractable, highlights, Pickups/
      Items/                item definitions, inventory, equipment, quality rolls, stats
      Loot/                 LootTable, LootDropper, LootContainer
      Map/                  map graph data (Graph/: MapGraph, MapNode, analysis) and Generation/ (constraints, generator passes, validator)
      Meters/               generic resources (stamina, mana, oxygen…): Meter, MeterSet, MeterCost, MeterZone
      Player/               movement, camera, lock-on, health, lifecycle, player audio
      Quests/               quest/objective definitions, QuestLog, QuestTracker, QuestEvents, signals
      Stats/                generic stat modifiers (ModifierSet, Stat, TimedModifiers), CharacterStats, presets
      Targeting/            TargetQuery, TargetFilter and TargetSelector assets (Selectors/)
      Timing/               GameClock (fixed ticks, pause, time scale, scheduling) and GameTime
      UI/                   Common/ (UIFactory), HUD/, Menus/, World/ (popups, enemy bars)
      Weapons/              Ranged/, FireBehaviors/, Generation/, Melee/, Throwables/
      WorldMap/             WorldMapArea (bounds, background, fog of war), MapMarker, MapRevealer, projection
    Settings/               URP assets, volume profiles, project-wide input actions
  ThirdParty/               imported asset packs
  TextMesh Pro/             TMP essentials
```

## Conventions

- **Namespaces** follow the top-level script folder: `CGD.Combat`, `CGD.Weapons`, `CGD.UI`, …
  All runtime scripts compile into `CGD.Runtime` (`Scripts/CGD.Runtime.asmdef`).
  Editor-only scripts live in `Scripts/Editor/` and compile into `CGD.Editor`
  (`Scripts/Editor/CGD.Editor.asmdef`), which references `CGD.Runtime` and is excluded
  from builds. Nothing in `CGD.Runtime` may reference it.
- **Data assets** are named `<Name><Type>` (`PistolCategory`, `HitscanFireBehavior`,
  `TargetDummyEnemyData`) and created from `Create > CGD > <Feature> > …`.
- **Moving files:** do it inside Unity, or move each file together with its `.meta` while
  the editor is closed — otherwise scene and prefab references break.
