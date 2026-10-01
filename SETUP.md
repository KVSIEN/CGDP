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
2. **Level** — geometry with `CullableObject`, a baked **NavMesh Surface** — or a
   [generated level](#generated-levels) built from a map graph.
3. **Player** — the rig below, then its wiring.
4. **HUD** — canvas and the elements you want.
5. **Enemies, pickups, interactables.**
6. **Optional systems** — map, quests, feedback, stealth, camera effects.

**Missing from Sandbox** (add when needed): `GameFlow`, `AudioPool`, `MeleeController`,
`GrenadeController`, `PlayerFootsteps`, `PlayerAudio`, `TimelineAbilityRunner`, animation and
ragdoll components (no rigged models yet), and every
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
                     PlayerConsumables, DevCommands, Stealthable
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
  Stealth          [StealthHUD]?
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
  Dialogue         [DialoguePanel, CanvasGroup]?
  Shop             [ShopPanel, CanvasGroup]?
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
| **StealthHUD** | `_stealth` = Player's Stealthable | HIDDEN / REVEALED / SPOTTED under the crosshair. |

Damage numbers and enemy health bars build themselves — don't place them under the HUD.

### Settings Menu

| Assign | Notes |
|---|---|
| `_camera` = PlayerCamera, `_input` = Player, `_bindings` = `InputBindingSettings`, `_hud` = HUD | Stretch its RectTransform to fill the screen. Escape opens it; it doubles as the pause menu when a `GameFlow` exists. |

The Audio, Video and Accessibility tabs need no wiring. They apply to every scene:
- Volume works through `AudioPool` (Master scales everything). Set each `SoundBank`'s `_category` (Effects / Music / Interface) to pick which slider it follows.
- Field of view replaces `PlayerCamera._baseFOV`. `_sprintFOV` keeps its authored widening on top.
- Camera shake scales everything `CameraEffectsController` adds. Screen flashes scale `ScreenFlashHUD` and `HitEffect`.

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

### Animation & Ragdoll (Player or Enemy)

Characters play without these. Add them once a rigged model with an Animator Controller exists.

```
Enemy / Player     [EnemyAnimator or PlayerAnimator, Ragdoll?]
  Model            [Animator]                  ← rigged mesh
    …bones         [Rigidbody, Collider, Hitbox?]   ← ragdoll bodies (Unity's Ragdoll Wizard)
```

| Component | Assign | Notes |
|---|---|---|
| **PlayerAnimator** (Player) | `_animator` = the body model's Animator, `_camera`? = PlayerCamera | Reads the Player's movement, health, weapons, melee and grenades. |
| **EnemyAnimator** (Enemy) | `_animator` = the model's Animator | Reads EnemyAI, NavMeshAgent and EnemyHealth. |
| **Ragdoll**? | `_animator`, `_bones`? (empty = every child Rigidbody), `_disableOnDeath` = the root movement collider | Bones stay kinematic while alive. |

**Animator parameters** (all optional; the controller only needs the ones it uses):

| Type | Names |
|---|---|
| Float | `Speed`, `ForwardSpeed`, `StrafeSpeed`, `VerticalSpeed`, `Aim` |
| Bool | `Grounded`, `Crouching`, `Sprinting`, `Sliding`, `Mantling`, `Rolling`, `Stunned`, `Reloading`, `Alerted`, `Dead` |
| Int | `AttackIndex` (combo step, −1 = heavy) |
| Trigger | `Attack`, `Fire`, `Throw`, `Hit` |

Attack timing comes from `EnemyData` / `MeleeAttackStep`, not from the clips. Author the clips to match.

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

## Vendors & Dialogue

```
Gunsmith           [Collider, Npc, Vendor]      ← any NPC: a Collider and an Npc; add a Vendor to trade
HUD
  Dialogue         [DialoguePanel, CanvasGroup]
  Shop             [ShopPanel, CanvasGroup]
```

| Component | Assign | Notes |
|---|---|---|
| **Npc** | `_displayName`, `_dialogue` = a `DialogueDefinition`, `_faceListener` | Interact to talk. Needs a non-trigger Collider within the Player's interact range. |
| **Vendor**? | `_displayName`, `_catalog` = a `ShopCatalog`, `_seed`? (empty = new stock each play), `_restockInterval`? (0 = once) | Opened by a dialogue choice with `OpenShopDialogueAction`, or by anything calling `OpenFor(player)`. |
| **DialoguePanel** (HUD) | `_input` = Player | Choices can also be picked with the weapon-slot keys (1–4). |
| **ShopPanel** (HUD) | `_input` = Player | |
| **PlayerInventory** | `_startingStacks` += `Economy/CreditsCurrency` × amount | Money is an inventory item. Sandbox starts with 1,500 cr. |

**Dialogue assets** (`Create › CGD › Dialogue › …`):
- **Dialogue**: nodes with an `Id`, a line of `Text` and `Choices`.
- **Choices**: each has `Text`, `Next` (a node Id; empty = end), optional `Conditions` and `Actions`.
- **Actions** are shared assets:
  - `OpenShopDialogueAction` opens the speaker's Vendor.
- **Conditions** are one asset per rule:
  - `Has Item` shows a choice only while the player carries an item.

**Shop assets** (`Create › CGD › Economy › …`):
- **Shop Catalog**: the currency, a `PriceTable`, a list of **stock sources**, and whether the vendor buys items and takes trade-ins.
- **Item Stock**: fixed goods. Stackables are sold in bundles; gear rolls fresh on each restock. The price comes from the price table unless overridden.
- **Rolled Weapon Stock**: N weapons from a set of categories and tiers.
- **Price Table**: the quality → value curve, buy markup and sell ratio. Every item's worth starts from its `_baseValue` (on the item or weapon-category asset).

The Sandbox **Gunsmith** stands at (5, 0, −3):
- It uses `GunsmithDialogue` and `GunsmithShopCatalog`: ammo for every caliber, plus 4–6 rolled weapons.
- Rolled weapons are all Common until the weapon categories get a `_rollProfile` (see [Assets](#assets)). The stock's tier range needs one to have any effect.

## Dev Console

| Component | Assign | Notes |
|---|---|---|
| **DevCommands** (Player) | `_catalog` = `DevTools/DevCatalog`, `_aim` = Main Camera, `_quests`? = QuestTracker, `_map`? = WorldMapArea | Development builds only unless `_allowInReleaseBuilds`. |
| **DevConsolePanel** (HUD) | `_input` = Player, `_commands` = the Player's DevCommands | Opens with ` (backquote). |

- Type `/help` for commands (every command starts with `/`). Anything the console can hand out by name is listed in `DevCatalog` — add new items, weapon categories, enemy prefabs and buffs there.

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

## Stealth

| Component | Assign | Notes |
|---|---|---|
| **Stealthable** (Player) | `_settings` = `Stealth/StealthSettings`, `_spottedFeedback`? = `Feedback/SpottedFeedbackPreset` | Without it, enemies ignore bushes and smoke. |
| **ConcealmentZone** (bush) | a Collider sized to the foliage · `_kind` = Bush · `_requiresCrouch`? | Collider becomes a trigger. Put the object on **Ignore Raycast** so shots pass through. |
| **ConcealmentZone** (smoke) | on `Prefabs/VFX/SmokeCloud` — `_kind` = Smoke, `_blocksSight` ticked | Spawned by `Weapons/Throwables/SmokeGrenadeData` (set it as a GrenadeController's `_data`). |
| **StealthAbility** | add `Abilities/StealthAbility` to a PlayerAbilities slot | Needs Stealthable. |

- Enemies need **no** changes — `EnemyPerception` checks stealth automatically.
- `SmokeCloud` has no visual yet: add a Particle System (and a matching collider radius, default 4 m).

## Camera Effects & Lock-On

| Component | Assign | Notes |
|---|---|---|
| **CameraEffectsController** (Main Camera) | `_settings`? = `CameraEffects/DefaultCameraEffectSettings`, `_shakeOnDamageOf`? = PlayerHealth | Must be on the object with the Camera. |
| **PlayerLockOn** (Player) | `_camera` = PlayerCamera, `_selector` = `Targeting/DefaultConeTargetSelector` | Middle mouse or T. |

## Map Graph (editor tool)

Open **Window › CGD › Map Graph**, pick `Map/SandboxMapGraph`, press **Generate**. Scenes use graphs through a [generated level](#generated-levels).

## Generated Levels

A scene can build its level from a map graph at load instead of using hand-placed geometry.
Start from a copy of the Sandbox scene and delete its level geometry, target dummies and NavMesh data.

```
Level              [LevelBuilder, NavMeshSurface]   ← at the origin, unrotated
  (rooms, corridors, doors, props and patrol routes are created here at runtime)
```

| Component | Assign | Notes |
|---|---|---|
| **LevelBuilder** | `_settings` = `Level/DefaultLevelBuildSettings` · `_graphAsset` = a `MapGraphAsset` **or** `_generation` = `Map/DefaultMapGenerationSettings` · `_seed` (0 = random) · `_navMesh` = its NavMeshSurface · `_player` = Player · `_spawnPoint` = RespawnPoint · `_worldMap`? = WorldMapArea | Logs a warning for every corridor that had to cross another. |
| **NavMeshSurface** | Collect Objects = **Current Object Hierarchy** | Rebuilt at runtime. Don't bake it. |

`LevelBuildSettings` holds the grid sizes, wall materials, door prefabs, the default room **Shapes** and one **Rooms** entry per room type.
Each entry lists its **Functions**, enemy prefabs (count read at the room's intensity), a centrepiece and props.

| Asset | Folder | Sets |
|---|---|---|
| **RoomFunction** (Lobby, Restaurant, Park, Casino, CrewQuarters, CargoBay, GrandAtrium, DockingBay, ReactorCore) | `Level/Functions/` | `_shapes` (empty = the settings' shapes), `_cells` (1–2 each way), `_size` in tiles (single-cell only; 0 = `_roomTiles`), `_wallHeight` (0 = the settings'), `_openCeiling`, `_landmark`?, `_structure` rules in order, `_props` |
| **LandmarkRoomDefinition** (ReactorCore) | `Level/Landmarks/` | `_prefab` (interior; origin at the room's south-west floor corner), `_sizeTiles`, `_doorways` (tile + side), `_occupiedTiles` (furniture) |
| **RoomShape** (Rectangle, L, T, U, Cross, Ring, Dome) | `Level/Shapes/` | parts, `_connections` range, `_curvedWalls` (Dome) |
| **Structure rules** (Wide/Dense Pillar Grid, Column Ring, Corner Pillars, Divider Wall, Service Counter) | `Level/Structure/` | spacing, sizes, heights; optional `_pillarPrefab` (origin at the base) replaces the plain box |

- A rule with no **Functions** gives plain rooms from the default shapes; empty default shapes = square rooms.
- A shape is only used for rooms whose connection count is in its `_connections` range; if nothing fits, the room is a square.
- `_roomTiles` is 12: at 8, doorway clearance fills most of a room and pillars rarely fit.
- Pillars and inner walls use the wall material and go on `_geometryLayer` with the walls.
- A landmark that doesn't fit its cells, or has fewer doorways than the room's connections, falls back to a generated room (with a warning).
- **Props** (`_props` on a function): prefabs, count, `On` tags (Edge = along walls, Corner…), `Avoid` tags, `Zone` (Largest = main space, Others = rooms split off by inner walls), `Facing`, `Spacing`.
  Placeholder props (Crate, Table, Bed, Planter, SlotMachine) are in `Prefabs/Environment/Props/` — origin at the base, +Z the front. Swap in real art there.
- **Ceilings**: tick `_buildCeilings` (off by default). Interiors then need lights. Put `_ceilingLayer` on its own layer and keep it out of the `WorldMapArea` capture mask; ceilings are left out of the NavMesh automatically.
- **Enemy prefabs** need `EnemyAI`. None exist yet, so generated rooms start empty.
- **Door prefabs**: origin at the doorway centre on the floor, +Z pointing out of the room. The opening is one tile wide (`_tileSize`).
  - Normal is optional.
  - Locked is typically a `Door` with a key.
  - Secret is, for example, a `Destructible` fake wall.
  - Closed doors cut the NavMesh, so enemies don't follow the player through them.
- **Exit**: when the Exit room's content has no `LevelExit`, a plain exit pad is added. Using it calls `GameFlow.Victory`.
- **Obstacle masks**: generated geometry goes on `_geometryLayer`. Keep that layer in enemies' `_obstacleMask` and in weapons' hit masks.

## Impact Effects

```
ImpactEffects      [ImpactSpawner]   ← one per scene (e.g. under GameManager)
```

| Component | Assign | Notes |
|---|---|---|
| **ImpactSpawner** | `_database` = `Impacts/DefaultImpactDatabase`, `_maxDecals` | Without it, hits leave nothing behind. |

`ImpactDatabase` picks effects per surface and per hit kind (Bullet / Melee):
- Characters (Hitbox or HealthManager) use **Flesh**.
- Other colliders match on their **physics material** (the same ones `SurfaceDatabase` uses).
- Anything else uses **Default**.

Each effect is optional:
- a decal material (transparent; e.g. a bullet-hole texture)
- a VFX prefab (needs `PooledLifetime`)
- a `SoundBank`

The default asset has no decal textures or VFX yet.

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
| `Weapons/Melee/`, `Weapons/Throwables/` | `DefaultMeleeWeaponData`, `DefaultGrenadeData`, `SmokeGrenadeData` | MeleeController, GrenadeController |
| `Items/Munitions/` | one per caliber | AmmoPickup, PlayerInventory |
| `Items/` | `StatRollProfile` | weapon categories, `CombatVestArmor` (see note) |
| `Items/Armor/`, `Attachments/`, `Consumables/`, `Resources/` | `CombatVestArmor`, `ExtendedMagazineAttachment`, `BandageConsumable`, `StimConsumable`, `ScrapMetalResource`, `ClothResource` | pickups, loot, recipes, quest rewards |
| `Crafting/` | `BandageRecipe`, `CombatStimRecipe`, `ExtendedMagazineRecipe`, `CombatVestRecipe` | CraftingStation |
| `DevTools/` | `DevCatalog` (all items, weapon categories, buffs; no enemy prefabs exist yet) | DevCommands |
| `Abilities/` | Dash, Heal, Projectile, Shockwave, DamageBoost, ConeBlast (Targeted), GroundSlam (Timeline — needs `TimelineAbilityRunner`), Stealth (needs `Stealthable`) | PlayerAbilities |
| `Targeting/` | `Default…TargetSelector`, `AimedArea…`, `FriendlyArea…` | abilities, PlayerLockOn |
| `Meters/` | Stamina, Mana, Oxygen, Rage | MeterSet, costs, MeterZone |
| `Loot/` | `DefaultLootTable` | LootDropper |
| `Stats/` | `HardDifficultyModifierPreset`, `DamageBoostModifierPreset` | CharacterStats, Stat Buff ability |
| `Quests/` | `TargetPracticeQuest` → `ResupplyQuest`, `ShootingRangeClearedQuestSignal` | QuestTracker |
| `Feedback/` | ten `…FeedbackPreset`s | CombatFeedback, QuestFeedback, Stealthable |
| `Stealth/` | `StealthSettings` (shared) | Stealthable |
| `CameraEffects/` | `DefaultCameraEffectSettings` | CameraEffectsController |
| `Map/` | `DefaultMapGenerationSettings`, `SandboxMapGraph` | Map Graph window, WorldMapArea |
| `Audio/` | `DefaultSurfaceDatabase` | PlayerFootsteps |
| `Level/` | `DefaultLevelBuildSettings` (Gridbox materials, room rules without prefabs), `Functions/` (nine `…RoomFunction`s), `Shapes/` (seven `…RoomShape`s), `Structure/` (six `…StructureRule`s), `Landmarks/` (`ReactorCoreLandmarkRoom`) | LevelBuilder |
| `Impacts/` | `DefaultImpactDatabase` (empty effects) | ImpactSpawner |
| `Economy/` | `CreditsCurrency`, `DefaultPriceTable`, `Shops/GunsmithShopCatalog`, `Stock/GunsmithAmmoStock`, `Stock/GunsmithWeaponStock` | Vendor, PlayerInventory, DevCatalog |
| `Dialogue/` | `GunsmithDialogue`, `Actions/OpenShopDialogueAction` | Npc |

**Sounds** — every `SoundBank` slot is optional; systems stay silent without one. There are no audio clips in the project yet, so no `SoundBank` assets exist.

**Weapon quality is off.** No weapon category has a `_rollProfile` yet, so generated weapons
roll at quality 1 / Common. To turn it on: create one `StatRollProfile` per weapon family,
right-click it and pick the matching **Axes/…** preset, then assign it to that family's categories.
