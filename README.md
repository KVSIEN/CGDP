# CGDP

Unity 6 (URP) first/third-person shooter sandbox: movement, weapons with procedural
generation, melee, grenades, abilities, status effects, hitboxes, enemy AI, audio and a
runtime-built HUD.

- [FEATURES.md](FEATURES.md) — what the game does
- [SETUP.md](SETUP.md) — how every system is wired into a scene
- [TASKS.md](TASKS.md) — open work
- [CLAUDE.md](CLAUDE.md) — coding guidelines

Open `Assets/Project/Scenes/Sandbox.unity` to play, or `MapTest.unity` for a freshly generated level.

## Project layout

```
Assets/
  Project/                  everything owned by this project
    Art/                    Animations, Fonts, Materials, PhysicsMaterials, Shaders, Textures
    Audio/                  Music, SFX
    Data/                   ScriptableObject assets, one folder per feature
      Abilities/  Artifacts/  Audio/  CameraEffects/  Combat/  Crafting/  DevTools/  Dialogue/  Economy/  Enemies/  Expedition/  Factions/  Feedback/  Flow/
      Impacts/  Input/  Items/  Level/  Loot/  Map/  Meters/  Perks/  Player/  Quests/  Stats/  Stealth/  Targeting/  Timing/  UI/  Weapons/
      (generic starting points are named Default<Type>, e.g. DefaultEnemyData)
    Prefabs/                Characters (+ Enemies/ — tier-1 capsule enemies), Environment (Props/, Landmarks/, Level/ — doors (keycard, terminal, puzzle), secret wall, keycard, terminal, hub workbench, encounter pieces (shutter, reward cache, loot pickups, puzzle switch, rift core, gamble station, escape pod); WallKit/ — placeholder wall art), Pickups, UI, VFX, Weapons
    Scenes/                 Sandbox.unity (+ its baked NavMesh folder), MapTest.unity (generated level)
    Scripts/                runtime code, one folder per feature (CGD.Runtime assembly)
      Abilities/            ability assets (incl. TargetedAbility) and the player's ability slots
      Artifacts/            offhand artifacts: ArtifactDefinition/Instance (rolled stats, passives, perks), OffhandBehavior (+ OffhandUse state) with
                            Cast, Ward and Channel behaviors; add a new artifact kind by subclassing OffhandBehavior and OffhandUse
      Animation/            Animator parameter bridge (AnimatorBridge, AnimatorParams), PlayerAnimator, EnemyAnimator, Ragdoll
      Audio/                audio pool, sound banks, surface lookup
      CameraEffects/        shake, kicks, FOV punches, lag, view blends (CameraEffectsController), CameraImpulses
      Combat/               Damage/, Health/ (HealthManager, Hitbox, Destructible), StatusEffects/, Actions/, Reflect/ (Reflector, ReflectProfile), Projectile, Stunnable, Noise
      Core/                 shared utilities (cooldowns, ranges, Culling/, Pooling/ — PrefabPool, IPoolable,
                            Random/ — Seed, RandomStream, SeedVariants; StateMachine/ — StateMachine<T>, IState)
      Crafting/             RecipeDefinition, Crafter (rules), CraftingStation
      DevTools/             dev console: DevConsole (commands), DevCommands (cheats), DevCatalog
      Dialogue/             DialogueDefinition (nodes, choices), Conversation (runtime), Npc, Actions/, Conditions/
      Economy/              CurrencyDefinition, Wallet, PriceTable, Vendor, GambleMachine (+ GambleOdds); Shops/ — Shop (buy/sell/trade-in),
                            ShopCatalog, stock sources (ItemStock, RolledWeaponStock), listings
      Editor/               editor-only inspectors and tooling (CGD.Editor assembly), incl. Map/ (Map Graph window, style report)
      Enemies/              enemy components, perception, and AI/ (state classes)
      Expedition/           the extraction loop: ExpeditionLedger (ship hold, packed kit, extract/die rules),
                            ExpeditionSession (keeps it across loads), ExpeditionRunner (a run in a level), RunStarterKit
      Factions/             FactionDefinition (name, colour, enemy roster) — used by the map graph and generated levels
      Feedback/             FeedbackBus, FeedbackPreset, FeedbackPlayer (feed, flash, rumble, shake), combat/quest feedback
      Flow/                 GameFlow, GameState/GameStateMachine, settings and UI commands
      Impacts/              ImpactEvents (hit broadcast), ImpactDatabase (effects per surface), ImpactSpawner, DecalPool
      Input/                PlayerInputHandler and binding settings
      Interaction/          IInteractable, doors, switches, EventInteractable, highlights, Pickups/,
                            Locks/ — ConditionLock (multi-condition door locks), LockProgress (rule), LockTerminal
      Items/                item definitions, inventory, equipment, quality rolls, stats
      Level/                map graph → level: LevelLayoutBuilder (rooms, CorridorRouter), LevelGeometryBuilder,
                            RoomPopulator, LevelBuilder (scene entry point), LevelExit, LevelBuildSettings;
                            Shapes/ — RoomShape assets, RoomShapeRasterizer, RoomFootprint (tiles + door sockets)
                            RoomOutline (curved walls), LevelWallBuilder; Functions/ — RoomFunction;
                            Structure/ — RoomStructurePlanner, RoomStructure (tile tags, pillars, partitions, zones), Rules/
                            RoomPlacer (cells, multi-cell rooms); Landmarks/ — hand-built rooms; Props/ — RoomPropPlanner
                            Objectives/ — map objectives: templates, MapObjectivePlanner, MapObjectiveRunner;
                            Encounters/ — encounter rooms: RoomEncounter + one per room type, RoomEncounterBuilder,
                            RoomShutter/RoomSeal, EncounterSpawner, rules (RoomArea, EnemyGroup, WaveSchedule, LightsOutPuzzle)
      Loot/                 LootTable, LootDropper, LootContainer
      Map/                  map graph data (Graph/: MapGraph, MapNode, analysis) and Generation/ (layout, content and style settings, sections, run modifiers, faction mixes, generator passes, validator, style report)
      Meters/               generic resources (stamina, mana, oxygen…): Meter, MeterSet, MeterCost, MeterZone
      Perks/                gear perks (passive and triggered), perk pools, PerkDispatcher
      Player/               movement, camera, lock-on, health, lifecycle, player audio; Dodge/ (DodgeDefinition stages, DodgeMotion)
      Quests/               quest/objective definitions, QuestLog, QuestTracker, QuestEvents, signals
      Settings/             GameSettings (audio, video, accessibility preferences) and SettingsData
      Stats/                generic stat modifiers (ModifierSet, Stat, TimedModifiers), CharacterStats, presets
      Stealth/              Stealthable (hide state), StealthStatus (rules), ConcealmentZone (bushes, smoke), StealthSettings
      Targeting/            TargetQuery, TargetFilter and TargetSelector assets (Selectors/)
      Timing/               GameClock (fixed ticks, pause, time scale, scheduling) and GameTime
      UI/                   Common/ (UIFactory), HUD/, Menus/, World/ (popups, enemy bars)
      Weapons/              WeaponItem (what a loadout slot holds), WeaponCategory (firearm or melee, rolls WeaponItems), Ranged/, FireBehaviors/, Generation/, Melee/ (controller, guard, melee categories and generator), Throwables/
      WorldMap/             WorldMapArea (bounds, background, fog of war), MapMarker, MapRevealer, projection
    Settings/               URP assets, volume profiles, project-wide input actions
    Tests/EditMode/         NUnit tests for the Unity-independent logic (CGD.Tests.EditMode assembly)
  ThirdParty/               imported asset packs
  TextMesh Pro/             TMP essentials
```

## Conventions

- **Namespaces** follow the top-level script folder: `CGD.Combat`, `CGD.Weapons`, `CGD.UI`, …
  All runtime scripts compile into `CGD.Runtime` (`Scripts/CGD.Runtime.asmdef`).
  Editor-only scripts live in `Scripts/Editor/` and compile into `CGD.Editor`
  (`Scripts/Editor/CGD.Editor.asmdef`), which references `CGD.Runtime` and is excluded
  from builds. Nothing in `CGD.Runtime` may reference it.
- **Tests** live in `Assets/Project/Tests/EditMode/` (`CGD.Tests.EditMode`, editor-only, references
  `CGD.Runtime`). Run them from **Window > General > Test Runner > EditMode**. Cover plain C#
  logic (rules, state machines, random streams). `MapGenerationTests` also runs every
  shipped map generation setting over 40 seeds: exit reachable, same seed = same map, time
  per map (printed in the test output), validator issues as warnings. CI runs the suite on
  every push, see `.github/workflows/tests.yml`.
- **Data assets** are named `<Name><Type>` (`PistolCategory`, `HitscanFireBehavior`,
  `TargetDummyEnemyData`) and created from `Create > CGD > <Feature> > …`.
- **Moving files:** do it inside Unity, or move each file together with its `.meta` while
  the editor is closed — otherwise scene and prefab references break.
