# Scene Setup

How to wire each system into a scene: what goes where, and what to assign.
`Assets/Project/Scenes/Sandbox.unity` is the reference scene.

**Conventions**

- `?` after a component = optional. Everything optional works (silently) when left out.
- Assets live in `Assets/Project/Data/<Feature>/` — see [Assets](#assets).
- HUD elements build their own UI at runtime. Place an empty `RectTransform` with the component; nothing else.
- "Player" below means the Player root GameObject.

---

## Checklist

Build a new playable scene in this order:

1. **Scene root** — GameManager, GameFlow, RespawnPoint, EventSystem, light, volume.
2. **Level** — geometry with `CullableObject`, a baked **NavMesh Surface**.
3. **Player** — the rig below, then its wiring.
4. **HUD** — canvas and the elements you want.
5. **Enemies, pickups, interactables.**
6. **Optional systems** — map, quests, feedback, camera effects.

**Missing from Sandbox** (add when needed): `GameFlow`, `AudioPool`, `MeleeController`,
`GrenadeController`, `PlayerFootsteps`, `PlayerAudio`, `TimelineAbilityRunner`, and every
system from [Stats](#stats--buffs) onwards. The target dummies have no `EnemyAI` (they stand still).

---

## Scene Root

```
GameManager        [VisibilityCullingManager, AudioPool, PoolPrewarmer?]
GameFlow           [GameFlow, GameTime?]        ← own root object, nothing else on it
RespawnPoint       (empty Transform)
WorldMap           [WorldMapArea]?              ← see Map & Minimap
EventSystem        [EventSystem, InputSystemUIInputModule]
Directional Light
Global Volume
```

| Component | Assign | Notes |
|---|---|---|
| **VisibilityCullingManager** | `_camera` = Main Camera | Other fields: defaults are fine. |
| **AudioPool** | — | Raise `_initialSize` (16) if sounds cut out. |
| **PoolPrewarmer**? | `_entries` = prefab + count | Pre-spawns pooled objects. Pools grow anyway. |
| **GameFlow** | `_settings`? = `Flow/GameFlowSettings` | See [Game Flow](#game-flow--time). |
| **GameTime**? | `_settings`? = `Timing/GameTimeSettings` | Created automatically at 60 ticks/s if absent. |

**Pooling rules**

- Projectiles, grenades, pickups and damage numbers are pooled automatically — nothing to place.
- Remove pooled objects with `PrefabPool.Release`, never `Destroy`.
- Never add `PooledInstance` by hand. One-shot VFX prefabs get a `PooledLifetime`.

---

## Player

```
Player             [PlayerInputHandler, PlayerHealth, PlayerMovement, PlayerDodge,
                    PlayerMantle, PlayerInteraction, PlayerInventory, PlayerAbilities,
                    WeaponController, PlayerWeaponLoadout, MeleeController,
                    GrenadeController, PlayerLifecycle, PlayerFootsteps, PlayerAudio,
                    Stunnable, StatusEffectController, Rigidbody, CapsuleCollider]
                   + optional: MeterSet, TimelineAbilityRunner, CharacterStats,
                     QuestTracker, PlayerLockOn, MapRevealer, FeedbackPlayer,
                     CombatFeedback, QuestFeedback, PlayerEquipment,
                     PlayerConsumables, DevCommands
  CameraRig
    Main Camera    [Camera, PlayerCamera, CameraEffectsController?]
      WeaponRig    [WeaponVisuals]
        Weapon     [WeaponAimPose]   ← gun mesh, no colliders
          Muzzle   (empty Transform — shots fire from here)
  Player Body      (visual mesh)
  Head Anchor      (empty Transform at eye height)
```

> Only **one** `PlayerInputHandler`. Weapons, grenades and abilities must stay on (or under)
> the Player — that's how their damage gets the Player's team.

### Movement & input

| Component | Assign |
|---|---|
| **PlayerInputHandler** | `_bindings` = `Input/InputBindingSettings` |
| **PlayerMovement** | `_settings` = `Player/PlayerMovementSettings`, `_cameraTransform` = Main Camera, `_playerMesh` = Player Body |
| **PlayerDodge** | `_settings` = same `PlayerMovementSettings` |
| **PlayerMantle** | `_settings` = same `PlayerMovementSettings`, `_cameraTransform` = Main Camera |
| **PlayerCamera** (Main Camera) | `_input`, `_movement` = Player · `_playerBody` = Player Body · `_headAnchor` = Head Anchor · `_camera` = its own Camera · `_firstPersonHideRenderers` = Player Body renderers |

### Combat

| Component | Assign | Notes |
|---|---|---|
| **WeaponController** | `_input` = Player, `_camera` = PlayerCamera, `_crosshair` = HUD Crosshair, `_muzzle` = Muzzle, `_visuals` = WeaponRig, `_cameraEffects`? = Main Camera | Fires whatever the loadout equips. |
| **PlayerWeaponLoadout** | `_startingWeapons`? = `WeaponData` assets | Empty slots fill from pickups. |
| **MeleeController** | `_camera` = PlayerCamera, `_data` = `Weapons/Melee/DefaultMeleeWeaponData` | |
| **GrenadeController** | `_camera` = PlayerCamera, `_data` = `Weapons/Throwables/DefaultGrenadeData` | |
| **PlayerAbilities** | `_health` = PlayerHealth, `_cameraTransform` = Main Camera, `_slots` = up to 4 ability assets | |
| **TimelineAbilityRunner**? | — | Needed for `TimelineAbility` assets. |
| **WeaponVisuals** (WeaponRig) | — | Kick and sway are tuned on the component and the `WeaponData`. |
| **WeaponAimPose** (Weapon) | `_camera` = PlayerCamera | Scene pose = hip pose. Set `_adsPosition`/`_adsRotation` for aiming. |

> The gun mesh must have **no colliders** — they block shots while aiming.

### Health, inventory & life

| Component | Assign | Notes |
|---|---|---|
| **PlayerHealth** | health, armor, shield values; `_hitboxProfile`? | |
| **PlayerInventory** | `_startingStacks`? = item + count (e.g. `StandardMunitions` × 60) | No starting ammo unless listed. |
| **PlayerInteraction** | `_forwardReference` = Main Camera | |
| **PlayerLifecycle** | `_health`, `_movement`, `_abilities`, `_input` = Player · `_hud` = HUD · `_spawnPoint` = RespawnPoint · `_deathScreen`? | Tick `_gameOverOnDeath` to end the run instead of respawning. |
| **PlayerFootsteps** | `_surfaces` = `Audio/DefaultSurfaceDatabase` | |
| **PlayerAudio** | `_health` = PlayerHealth, `_hurtSound`?, `_deathSound`? | |
| **MeterSet**? | `_definitions` = meters from `Data/Meters/` | Needed before any stamina/mana cost can be paid. |

**Meter costs** — set `SprintCost`/`DodgeCost` on `PlayerMovementSettings`, or `Cost` on an ability.
A cost for a meter the Player doesn't have is **blocked**, not free.

---

## HUD

```
HUD                [Canvas, CanvasScaler, GraphicRaycaster, HUDManager]
  ScreenFlash      [ScreenFlashHUD]?         ← keep near the top (draws underneath)
  HitEffect        [HitEffect]
  Crosshair        [CrosshairHUD]
  HitMarker        [HitMarkerHUD]?
  Stats            [StatsHUD]
  Weapon           [WeaponHUD]
  Abilities        [AbilityHUD]
  Dodge            [DodgeHUD]
  StatusEffects    [StatusEffectHUD]
  Meters           [MeterHUD]?
  Velocity         [VelocityHUD]
  QuickUse         [QuickUseHUD]?
  Minimap          [MinimapHUD]?
  Quests           [QuestHUD]?
  Notifications    [NotificationHUD]?
  Interact         [InteractHUD]
  WeaponPickup     [WeaponPickupHUD, CanvasGroup]
  Inventory        [InventoryHUD, CanvasGroup]
  ItemInventory    [ItemInventoryHUD, CanvasGroup]
  WorldMap         [WorldMapHUD, CanvasGroup]?   ← keep near the bottom (draws on top)
  Character        [CharacterPanel, CanvasGroup]?
  Crafting         [CraftingPanel, CanvasGroup]?
  DevConsole       [DevConsolePanel, CanvasGroup]?
  SettingsMenu     [SettingsMenu]
```

Later children draw on top. `HUDManager` finds every element by itself.

| Element | Assign | Notes |
|---|---|---|
| **CrosshairHUD** | `_settings` = `UI/CrosshairSettings`, `_playerCamera` = PlayerCamera | |
| **StatsHUD**, **HitEffect** | `_playerHealth` = PlayerHealth | HitEffect = damage flash + low-health vignette. |
| **WeaponHUD** | `_weapon` = WeaponController | |
| **AbilityHUD** | `_abilities` = PlayerAbilities | |
| **DodgeHUD** | `_dodge` = PlayerDodge | |
| **StatusEffectHUD** | `_target` = Player's StatusEffectController | |
| **MeterHUD** | `_meters` = Player's MeterSet | Hides itself without meters. |
| **VelocityHUD** | `_movement` = PlayerMovement | |
| **InteractHUD** | `_interaction` = PlayerInteraction | |
| **WeaponPickupHUD** | `_interaction` = PlayerInteraction | Shows when aiming at a weapon pickup. |
| **InventoryHUD** | `_input` = Player, `_loadout` = PlayerWeaponLoadout | Opens with I. |
| **ItemInventoryHUD** | `_input` = Player, `_inventory` = PlayerInventory | Opens with I. `_inventory` is required. |
| **QuestHUD** | `_tracker` = QuestTracker | See [Quests](#quests). |
| **MinimapHUD** | `_area` = WorldMapArea, `_viewer` = Main Camera | See [Map](#map--minimap). |
| **WorldMapHUD** | `_input` = Player, `_area` = WorldMapArea, `_viewer` = Main Camera | Opens with M. |
| **NotificationHUD**, **ScreenFlashHUD** | — | Driven by `FeedbackPlayer`. |
| **HitMarkerHUD** | — | Driven by `CombatFeedback`. |

Damage numbers and enemy health bars build themselves — don't place them under the HUD.

### Settings Menu

| Assign | Notes |
|---|---|
| `_camera` = PlayerCamera, `_input` = Player, `_bindings` = `InputBindingSettings`, `_hud` = HUD | Stretch its RectTransform to fill the screen. Escape opens it; it doubles as the pause menu when a `GameFlow` exists. |

---

## Game Flow & Time

- **One `GameFlow` per scene**, on its own root object. It survives scene loads; later copies delete their **whole GameObject**.
- Scenes loaded by name must be in the Build Profile's scene list.
- UI buttons call a **GameFlowCommands** component (Resume, RestartLevel, LoadMainMenu, StartGame, Quit…) — never `GameFlow` directly.
- **GameStateView** shows a panel only in chosen states: `_target` = the panel, `_visibleIn` = e.g. `Paused`. Put it on an always-active object, not on the panel.
- **Time:** `GameTime` owns `Time.timeScale`. Pause or slow time through `GameTime.Instance.Clock`, never by setting `Time.timeScale`.

---

## Enemies

```
Enemy              [NavMeshAgent, EnemyAI, EnemyHealth, EnemyHealthBar, Stunnable,
                    StatusEffectController, EnemyStateVisuals?, EnemyAudio?,
                    LootDropper?, DespawnOnDeath?, CharacterStats?, MapMarker?]
  Head / Body / Limbs   [Collider, Hitbox]?
```

| Component | Assign | Notes |
|---|---|---|
| **EnemyAI** | `_data` = an `EnemyData`, `_waypoints`?, `_targetMask`, `_obstacleMask` | Needs a baked NavMesh. Finds targets by team. |
| **EnemyHealth** | `_data` = same `EnemyData`, `_healthBar` = its EnemyHealthBar, `_hitboxProfile`? | |
| **EnemyStateVisuals**? | `_renderers` | Tints by AI state. |
| **EnemyAudio**? | `_hurtSound`, `_deathSound` | |
| **LootDropper**? | `_table`, `_itemPickupPrefab`, `_weaponPickupPrefab` | Tick `_dropOnDeath`. |
| **DespawnOnDeath**? | `_delay` | Returns pooled enemies to the pool. |
| **CharacterStats**? | `_presets` = e.g. `Stats/HardDifficultyModifierPreset` | Tougher enemy. |
| **StatusEffectController** | `_immunities`? | Without it, status effects are ignored. |

**Enemy type** is all in its `EnemyData`: team, health, speeds, senses, melee or ranged, display name (used in kill messages).

### Hitboxes (Player or Enemy)

- Add child colliders with a **Hitbox** and set `_region` (Head / Body / Limb). The owner fills in automatically.
- Hitbox colliders must **not** be triggers.
- Damage per region comes from a **HitboxProfile** asset (`Create › CGD › Combat › Hitbox Profile`). Without one: all ×1, Head still critical.
- No hitboxes? A collider on the character itself takes Body hits.

---

## Pickups & Interactables

| Object | Components | Assign |
|---|---|---|
| Weapon pickup | trigger Collider, `WeaponPickup` | `_data` = a `WeaponData`, **or** add `RandomWeaponPickup` with `_categories` (+ `_seed`? for a fixed weapon) |
| Ammo pickup | trigger Collider, `AmmoPickup` | `_munition` = a munition asset, `_amount` |
| Health pickup | trigger Collider, `HealthPickup` | `_amount` |
| Item pickup | trigger Collider, `ItemPickup` | `_item`, `_count` |
| Door | solid Collider, `Door` | Pivot on the hinge. `_openAngle`, `_holdDuration`?, `_key`? to lock it |
| Switch | Collider, `Switch` | `_label`, `_doors` |
| Anything else | Collider, `EventInteractable` | `_label`, `_requirement`?, `_onInteract` event |
| Chest | Collider, `LootContainer`, `LootDropper` | Untick the dropper's `_dropOnDeath` |
| Breakable crate | solid Collider, `Destructible`, `LootDropper`, `DespawnOnDeath` | `_maxHealth`; `_delay` 0 |
| Crafting station | Collider, `CraftingStation` | `_label`, `_recipes` = recipe assets (e.g. all of `Data/Crafting/`) |

- Add **InteractionHighlight**? to any interactable to tint it while aimed at (URP Lit/Unlit).
- Add **CullableObject** to world meshes that should be culled off-screen.
- Custom interactables just implement `IInteractable` — no extra wiring.

### Prefabs

| Prefab | Needs |
|---|---|
| `Prefabs/Weapons/Projectile` | `Projectile` + a renderer. `TrailRenderer`? is reset on every launch. |
| `Prefabs/Weapons/FragGrenade` | `Rigidbody`, solid Collider, `Grenade` |
| Zone prefabs | `PersistentZone` — spawned by an ActionTimeline's zone event |
| One-shot VFX | `PooledLifetime` (`_lifetime` 0 = until particles finish) |

### Loot

- **LootTable** (`Create › CGD › Loot › Loot Table`): guaranteed entries + weighted draws; entries can be items, other tables, or prefabs. New entries start at weight 0 — set them.
- **LootDropper** needs two shared prefabs: an `ItemPickup` prefab and a `WeaponPickup` prefab (leave its `_data` empty).

---

## Equipment, Consumables & Crafting

| Component | Assign | Notes |
|---|---|---|
| **PlayerEquipment** (Player) | — | Worn armor. Needs `CharacterStats` on the Player for armor stats to count. |
| **PlayerConsumables** (Player) | `_slots`? = starting quick-use items (e.g. `BandageConsumable`) | Z / B use them. Slots can be changed in the Character panel. |
| **CharacterPanel** (HUD) | `_input` = Player, `_inventory`, `_equipment`, `_loadout`, `_consumables` = the Player's components | Opens with Tab. |
| **QuickUseHUD** (HUD) | `_consumables`, `_inventory` = the Player's components | |
| **CraftingPanel** (HUD) | `_input` = Player | Opens when a `CraftingStation` is used. |

- Attachments need a free slot: gear only has slots when its category/definition has a `_rollProfile` (weapons have none yet — see [Assets](#assets)).
- Armor slot restrictions come from the attachment's `_armorSlots` (empty = fits anything, including weapons).

## Dev Console

| Component | Assign | Notes |
|---|---|---|
| **DevCommands** (Player) | `_catalog` = `DevTools/DevCatalog`, `_aim` = Main Camera, `_quests`? = QuestTracker, `_map`? = WorldMapArea | Development builds only unless `_allowInReleaseBuilds`. |
| **DevConsolePanel** (HUD) | `_input` = Player, `_commands` = the Player's DevCommands | Opens with ` (backquote). |

- Type `help` for commands. Anything the console can hand out by name is listed in `DevCatalog` — add new items, weapon categories, enemy prefabs and buffs there.

## Stats & Buffs

- **CharacterStats**? on the Player or an enemy holds buffs, debuffs and permanent presets (e.g. difficulty).
- Health, armor and weapon/attack damage read through it automatically.
- `DamageBoostAbility` (a Stat Buff ability) only works if the Player has `CharacterStats`.

## Quests

| Component | Assign |
|---|---|
| **QuestTracker** (Player) | `_quests` = every quest this scene can run, chained ones included (e.g. `TargetPracticeQuest`, `ResupplyQuest`) |
| **QuestHUD** | `_tracker` = QuestTracker |
| **QuestSignalTrigger**? | trigger Collider + `_signal` — "reach this place" objectives |

- Objectives count **kills** (`EnemyData`), **pickups** (`ItemDefinition`) or **signals** (`QuestSignal`).
- Raise a signal from any UnityEvent (e.g. `EventInteractable`) via the signal asset's `Raise()`.
- Start a non-auto quest from a UnityEvent with `QuestTracker.StartQuest`.

## Map & Minimap

| Component | Assign | Notes |
|---|---|---|
| **WorldMapArea** | `_size` = area in metres (X, Z), `_source` | Centre it on the playable area. |
| **MapRevealer** (Player) | `_area` = WorldMapArea | Clears fog around the Player. |
| **MapMarker** | shape, colour, size | Add to enemies, pickups, objectives. Tick `_clampToEdge` for objectives. |
| **MinimapHUD** / **WorldMapHUD** | see [HUD](#hud) | |

Background `_source` options:

- **Capture** — top-down snapshot of `_captureMask` layers at scene start.
- **Texture** — your own top-down image (+Z = up).
- **MapGraph** — draws `_graph` as a room schematic (generate the graph first).
- **None** — plain colour.

## Feedback

| Component | Assign | Notes |
|---|---|---|
| **FeedbackPlayer** (Player) | `_notifications` = NotificationHUD, `_screenFlash` = ScreenFlashHUD, `_cameraEffects` = Main Camera | Without it, no feedback shows. `_vibration` toggles rumble. |
| **CombatFeedback** (Player) | `_hitMarker` = HitMarkerHUD + the `Hit`, `CriticalHit`, `Kill`, `DamageTaken`, `LowHealth` presets | |
| **QuestFeedback**? | `_tracker` = QuestTracker + the four `Quest…` presets | |

All presets are in `Data/Feedback/`.

## Camera Effects & Lock-On

| Component | Assign | Notes |
|---|---|---|
| **CameraEffectsController** (Main Camera) | `_settings`? = `CameraEffects/DefaultCameraEffectSettings`, `_shakeOnDamageOf`? = PlayerHealth | Must be on the object with the Camera. |
| **PlayerLockOn** (Player) | `_camera` = PlayerCamera, `_selector` = `Targeting/DefaultConeTargetSelector` | Middle mouse or T. |

## Map Graph (editor tool)

Not used by scenes yet. Open **Window › CGD › Map Graph**, pick `Map/SandboxMapGraph`, press **Generate**.

---

## Assets

All under `Assets/Project/Data/`. Shared settings are **single assets** — never duplicate them.

| Folder | Assets | Used by |
|---|---|---|
| `Input/` | `InputBindingSettings` (shared) | PlayerInputHandler, SettingsMenu |
| `Player/` | `PlayerMovementSettings` (shared) | PlayerMovement, PlayerDodge, PlayerMantle |
| `UI/` | `CrosshairSettings` | CrosshairHUD |
| `Flow/` | `GameFlowSettings` | GameFlow |
| `Timing/` | `GameTimeSettings` | GameTime |
| `Enemies/` | `DefaultEnemyData`, `TargetDummyEnemyData` | EnemyAI, EnemyHealth |
| `Combat/HitboxProfiles/` | `DefaultHitboxProfile`, `TargetDummyHitboxProfile` | EnemyHealth, PlayerHealth |
| `Combat/StatusEffects/` | Bleed, Fire, Ice, Lightning, Poison | on-hit effect lists |
| `Combat/ActionTimelines/` | `GroundSlamTimeline` | TimelineAbility, melee attack steps |
| `Weapons/Ranged/` | `DefaultWeaponData`, `T1/` examples per category | Loadout, WeaponPickup |
| `Weapons/Categories/` | one per weapon type | RandomWeaponPickup, loot |
| `Weapons/FireBehaviors/` | Hitscan, Projectile, Shotgun | weapon data / categories |
| `Weapons/Melee/`, `Weapons/Throwables/` | `DefaultMeleeWeaponData`, `DefaultGrenadeData` | MeleeController, GrenadeController |
| `Items/Munitions/` | one per caliber | AmmoPickup, PlayerInventory |
| `Items/` | `StatRollProfile` | weapon categories, `CombatVestArmor` (see note) |
| `Items/Armor/`, `Attachments/`, `Consumables/`, `Resources/` | `CombatVestArmor`, `ExtendedMagazineAttachment`, `BandageConsumable`, `StimConsumable`, `ScrapMetalResource`, `ClothResource` | pickups, loot, recipes, quest rewards |
| `Crafting/` | `BandageRecipe`, `CombatStimRecipe`, `ExtendedMagazineRecipe`, `CombatVestRecipe` | CraftingStation |
| `DevTools/` | `DevCatalog` (all items, weapon categories, buffs; no enemy prefabs exist yet) | DevCommands |
| `Abilities/` | Dash, Heal, Projectile, Shockwave, DamageBoost, ConeBlast (Targeted), GroundSlam (Timeline — needs `TimelineAbilityRunner`) | PlayerAbilities |
| `Targeting/` | `Default…TargetSelector`, `AimedArea…`, `FriendlyArea…` | abilities, PlayerLockOn |
| `Meters/` | Stamina, Mana, Oxygen, Rage | MeterSet, costs, MeterZone |
| `Loot/` | `DefaultLootTable` | LootDropper |
| `Stats/` | `HardDifficultyModifierPreset`, `DamageBoostModifierPreset` | CharacterStats, Stat Buff ability |
| `Quests/` | `TargetPracticeQuest` → `ResupplyQuest`, `ShootingRangeClearedQuestSignal` | QuestTracker |
| `Feedback/` | nine `…FeedbackPreset`s | CombatFeedback, QuestFeedback |
| `CameraEffects/` | `DefaultCameraEffectSettings` | CameraEffectsController |
| `Map/` | `DefaultMapGenerationSettings`, `SandboxMapGraph` | Map Graph window, WorldMapArea |
| `Audio/` | `DefaultSurfaceDatabase` | PlayerFootsteps |

**Sounds** — every `SoundBank` slot is optional; systems stay silent without one. There are no audio clips in the project yet, so no `SoundBank` assets exist.

**Weapon quality is off.** No weapon category has a `_rollProfile` yet, so generated weapons
roll at quality 1 / Common. To turn it on: create one `StatRollProfile` per weapon family,
right-click it and pick the matching **Axes/…** preset, then assign it to that family's categories.
