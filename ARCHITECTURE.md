# Architecture

How the systems fit together. [README.md](README.md) lists the folders,
[SETUP.md](SETUP.md) says how to wire a scene, [FEATURES.md](FEATURES.md) says what the
game does. This file explains **who talks to whom, and why** — read it before adding a
system, so it plugs into the existing seams instead of growing a parallel one.

## The idea in one paragraph

Gameplay is built from small components that don't know about each other. They meet in
three ways: a **serialized reference** for things that always belong together (a weapon and
its owner's inventory), a **static event bus** for "something happened, whoever cares can
react" (a hit, a noise, a quest signal), and a **ScriptableObject asset** for tuning and
content. Rules (damage maths, loot rolls, pacing, locks, map generation) live in plain C#
classes with no Unity dependency so they can be unit-tested; MonoBehaviours own the
lifecycle, hold the references and forward the work.

## Layers

```
 Data assets (ScriptableObjects)   WeaponData, EnemyData, LootTable, MapGenerationSettings …
        │ read by
 Rules (plain C#)                  HealthManager maths, LootTable, PacingCycle, LockProgress,
        │ driven by                RoomStructurePlanner, ModifierSet, Conversation …
 Components (MonoBehaviours)       Player*, Enemy*, Weapon*, Door, Pickup … own Unity lifecycle
        │ announce through
 Event buses (static events)       CombatEvents, Noise, ImpactEvents, FeedbackBus, QuestEvents …
        │ listened to by
 Presentation                      HUD, FeedbackPlayer, ImpactSpawner, CameraEffects, DirectorMusic
```

Dependencies point **down and across buses, never up**: a weapon never references the HUD;
the HUD (or `FeedbackPlayer`) listens to a bus. Nothing in `CGD.Runtime` may reference the
`CGD.Editor` assembly.

## Scene start-up

Order matters; `[DefaultExecutionOrder]` makes it explicit:

| Order | Component | Why it goes then |
|---|---|---|
| -1000 | `PlayerInputHandler` | Input is built before anyone reads it. |
| -500 | `LevelBuilder` | The level (rooms, NavMesh, player placement) exists before other components wake. |
| -200 | `GameFlow` | Game state exists before anything asks it to change. |
| -190 | `GameTime` | Pause / time scale are ready. |
| -100 | `VisibilityCullingManager`, `PlayerMantle` | Culling registry and mantle input run ahead of movement. |
| -50 | `WorldMapArea` | Fits itself to the built level. |
| 100 | `CameraEffectsController`, `PlayerDodge`, `ExpeditionRunner` | Run after the player's own setup (starting stacks, loadout). |

Scene-level singletons: `GameFlow`, `GameTime` and `ExpeditionSession` (all on the
`GameFlow` prefab, persisting across scene loads) and `AudioPool` (on `GameManager`).
They are reached through `Instance`, and every caller must tolerate it being absent so a
scene still plays on its own. Each resets its static in a `RuntimeInitializeOnLoadMethod`
so Enter Play Mode without domain reload stays safe. Add a singleton only for something
that genuinely exists once per game.

## Event buses

All are `static event` + a `Report/Emit/Play` method. **Subscribe in `OnEnable`, unsubscribe
in `OnDisable`** (static events outlive scenes).

| Bus | Raised by | Heard by |
|---|---|---|
| `CombatEvents.DamageDealt` | `HealthManager` once a hit is resolved | `CombatFeedback` (hit markers, kills), `PlayerHealth`, `PlayerPerks`, `CombatActions`, `GameDirector` (stress) |
| `Noise.Emitted` | weapons, melee swings, grenades, abilities, enemies | `EnemyAI` perception (hearing), `Stealthable` (reveal) |
| `ImpactEvents.Occurred` | projectiles, hitscan, melee hits | `ImpactSpawner` (decals, sparks, sounds per surface) |
| `FeedbackBus.Requested` | pickups, shop, crafting, encounters, quests, director, perks … | `FeedbackPlayer` (feed, flash, rumble, shake) |
| `QuestEvents.Reported` | enemy deaths, pickups, resource nodes, `QuestSignal` | `QuestTracker` (objectives), `ConditionLock` (door locks) |
| `CameraImpulses.Emitted` | grenades and other explosions | `CameraEffectsController` |
| `ObjectiveFeed.Changed` | Siege / Gauntlet encounters | `ObjectiveHUD` |
| `LevelExit.Used` | the level's exit | `ExpeditionRunner` |
| `Vendor.Opened`, `CraftingStation.Opened`, `Npc.ConversationStarted` | interactables | `ShopPanel`, `CraftingPanel`, `DialoguePanel` |

Rule of thumb: if the sender would otherwise need a reference to the HUD, audio or
camera, use a bus. If two components always travel together, use a serialized reference.

## Following one thing through the code

**A shot lands.** `PlayerInputHandler` (attack bound via `InputBindingSettings`) →
`WeaponController` asks `WeaponTrigger` what the button means → `WeaponFireBehavior`
raycasts/spawns a `Projectile` (pooled) → `Hitbox.TakeDamage` forwards to its
`HealthManager` → damage resolves in one place: region multiplier → armor → interceptors
(a raised guard) → shield → health → `CombatEvents.Report`. Beside that, the shot reports
`ImpactEvents` (surface effects) and `Noise` (enemies hear it). Stats flow in through
`CharacterStats` (base data → equipment → presets → timed buffs).

**An enemy notices you.** `EnemyAI` runs a `StateMachine` of state classes under
`Enemies/AI/`; attacks go through the same `IDamageable` path as the player's.
`EnemyPerception` is the only place an enemy learns anything: it samples sight and
footsteps on a staggered timer, takes `Noise`, hits and `EnemyCallouts` as they happen,
and turns them into an `AwarenessMeter` value, an `AwarenessLevel` and a belief
(`LastKnownPosition`, a guess once the target is out of sight). Brains read
`TargetPosition` — the real position only while the target is sensed — so no state or
tree node can know more than the enemy saw. `EnemySearch` is the shared notice →
approach → look-around routine both brains use to act on a lead. Space works the same
way: a scene's `CoverMap` lists every cover spot (scanned from the NavMesh), but each
enemy's `CoverKnowledge` holds only the spots it has learned (home turf, then what it
sees), and `EnemyTactics` answers cover / flank / hiding-spot questions from that and the
believed threat position.

**An enemy decides with a behavior tree.** When its `EnemyData` has a `BehaviorTreeAsset`,
`EnemyAI` skips its state machine and ticks a `BehaviorTree` — the agent's own copy of the
asset's nodes (the asset is shared and never changes at runtime; per-agent state lives on
the copies, made with `CreateInstance`). Nodes call the same `EnemyAI` capabilities the
states use (movement, `EnemyPerception`, `EnemyRangedCombat`, melee) and report the
`AiState` the visuals show. Stuns pause the tree; death aborts it. The editor
(`Editor/BehaviorTrees/`) is a GraphView over the asset, editing it through an
`internal` authoring API (`InternalsVisibleTo CGD.Editor`) so gameplay code can't rewire a
shared tree. In Play mode it reads the selected `IBehaviorTreeOwner`'s copy for the live view.

**A run.** `GameFlow` (state: Playing / Paused / Victory / GameOver…) is the only thing that
pauses time or loads scenes. `ExpeditionSession` keeps the ship's hold between scenes;
`ExpeditionRunner` hands the packed kit to the player at the start and, when `GameFlow`
reaches Victory (extract) or GameOver (death), settles it through `ExpeditionLedger`.

**A generated level.** `Map/Generation` passes build a `MapGraph` (rooms, connections,
factions, gates, objectives) from a `MapGenerationSettings` style and a seed →
`LevelBuilder` turns it into geometry: `LevelLayoutBuilder` (grid placement, corridors) →
`LevelGeometryBuilder` / `LevelWallBuilder` (floors, walls) → `RoomPopulator` (props,
enemies, loot, encounters per room function and faction) → NavMesh bake → player to the
Start room, `LevelExit` in the Exit room. Same seed ⇒ same level (`Core/Random`).

**Pacing.** `GameDirector` turns damage taken and kills into an intensity and a pacing
phase; `ReinforcementSpawner`, `SupplySpawner` and `DirectorMusic` read its phase and
pressure and decide for themselves what to do. The director never spawns anything.

## Conventions that keep this working

- **One home per rule.** Damage maths live in `HealthManager`, state in `GameFlow`, time in
  `GameTime`, cooldowns in `CooldownTimer`, stat maths in `ModifierSet`. Extend those
  rather than re-implementing them.
- **Data in assets.** New tunables go in a ScriptableObject under `Data/<Feature>/`, not as
  constants in a component.
- **Extension points are subclasses of an asset.** New fire behavior → `WeaponFireBehavior`;
  new artifact kind → `OffhandBehavior` + `OffhandUse`; new encounter → `RoomEncounter`;
  new room look → `FactionDesign` / `RoomFunction`; new dialogue effect → a dialogue
  action or condition; new AI behavior → a `BtAction` / `BtCondition` (enemy ones derive
  `EnemyAction` / `EnemyCondition`) tagged `[BtNodeInfo]`, which the tree editor lists itself.
- **Pool what spawns often** (`PrefabPool`, `IPoolable`) — projectiles, grenades, popups.
- **Tests** cover the plain-C# rules (`Tests/EditMode`); a new rule class should come with one.
- **Docs move with the code:** gameplay change → FEATURES.md, scene wiring change → SETUP.md.
