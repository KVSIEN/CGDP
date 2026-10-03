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
`PlayerFootsteps`, `PlayerAudio`, `TimelineAbilityRunner`, animation and
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
                    ThrowableController, PlayerLifecycle, PlayerFootsteps, PlayerAudio,
                    Stunnable, StatusEffectController, Rigidbody, CapsuleCollider]
                   + optional: MeterSet, TimelineAbilityRunner, CharacterStats,
                     QuestTracker, PlayerLockOn, MapRevealer, FeedbackPlayer,
                     CombatFeedback, QuestFeedback, PlayerEquipment,
                     PlayerItemSlots, DevCommands, Stealthable
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
| **PlayerDodge** | `_definition` = a dodge from `Player/Dodges/` (default `SidestepRollDodge`), `_doubleTapCrouch` (also dodge by double-tapping Crouch while moving) |
| **PlayerMantle** | `_settings` = same `PlayerMovementSettings`, `_cameraTransform` = Main Camera |
| **PlayerCamera** (Main Camera) | `_input`, `_movement` = Player · `_playerBody` = Player Body · `_headAnchor` = Head Anchor · `_camera` = its own Camera · `_firstPersonHideRenderers` = Player Body renderers |

### Combat

| Component | Assign | Notes |
|---|---|---|
| **WeaponController** | `_input` = Player, `_camera` = PlayerCamera, `_crosshair` = HUD Crosshair, `_muzzle` = Muzzle, `_visuals` = WeaponRig, `_cameraEffects`? = Main Camera | Fires whatever the loadout equips. |
| **PlayerWeaponLoadout** | `_startingWeapons`? = `WeaponData` assets, `_wheel`? = HUD SlotWheel | Empty slots fill from pickups. Holding 1–4 opens the weapon wheel (other slots + spare weapons in the pack). |
| **MeleeController** | `_camera` = PlayerCamera, `_data` = `Weapons/Melee/DefaultMeleeWeaponData` | |
| **ThrowableController** | `_camera` = PlayerCamera | Throws the grenade an item slot readies. Grenades are items (`Items/Throwables/`). |
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

**Meter costs** — set `SprintCost` on `PlayerMovementSettings`, `_cost` on a dodge, or `Cost` on an ability.
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
  ItemSlots        [ItemSlotsHUD]?
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
| Bool | `Grounded`, `Crouching`, `Sprinting`, `Sliding`, `Mantling`, `Rolling`, `Dashing`, `Stunned`, `Reloading`, `Alerted`, `Dead` |
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
| Ammo cache | solid Collider, `AmmoCache` | `_munitions` = all four munition assets, `_uses`, `_magazines`, `_contents`? = the visible ammo. Ready-made: `Prefabs/Environment/Props/AmmoCache`. |
| Resource node | solid Collider, `ResourceNode` | `_resource`? (empty = placeholder, yields nothing), `_amount`, `_harvests`, `_holdDuration`, `_visual`?. Ready-made: `Prefabs/Environment/Props/ResourceNode`. |

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
| **PlayerItemSlots** (Player) | `_slots`? = starting items for keys 5–8 (consumables or throwables), `_wheel`? = HUD SlotWheel | Holding a key opens the item wheel. Throwables need `ThrowableController`. |
| **CharacterPanel** (HUD) | `_input` = Player, `_inventory`, `_equipment`, `_loadout`, `_itemSlots` = the Player's components | Opens with Tab. |
| **ItemSlotsHUD** (HUD) | `_slots`, `_inventory`, `_throwing`? = the Player's components | |
| **SlotWheelHUD** (HUD, starts inactive) | — | Shared by `PlayerItemSlots._wheel` and `PlayerWeaponLoadout._wheel`. Keep it near the bottom of the HUD so it draws on top. |
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

- Type `/help` for commands (every command starts with `/`). Anything the console can hand out by name is listed in `DevCatalog` — add new items, weapon categories, enemy prefabs, buffs and dodges there.

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
| **ConcealmentZone** (smoke) | on `Prefabs/VFX/SmokeCloud` — `_kind` = Smoke, `_blocksSight` ticked | Spawned by `Weapons/Throwables/SmokeGrenadeData`, thrown via the `SmokeGrenadeThrowable` item. |
| **StealthAbility** | add `Abilities/StealthAbility` to a PlayerAbilities slot | Needs Stealthable. |

- Enemies need **no** changes — `EnemyPerception` checks stealth automatically.
- `SmokeCloud` has no visual yet: add a Particle System (and a matching collider radius, default 4 m).

## Camera Effects & Lock-On

| Component | Assign | Notes |
|---|---|---|
| **CameraEffectsController** (Main Camera) | `_settings`? = `CameraEffects/DefaultCameraEffectSettings`, `_shakeOnDamageOf`? = PlayerHealth | Must be on the object with the Camera. |
| **PlayerLockOn** (Player) | `_camera` = PlayerCamera, `_selector` = `Targeting/DefaultConeTargetSelector` | T. |

## Map Graph (editor tool)

Open **Window › CGD › Map Graph**, pick `Map/SandboxMapGraph`, press **Generate**. Scenes use graphs through a [generated level](#generated-levels).

A map **style** (`MapGenerationSettings`) is what a `MapGraphAsset` or `LevelBuilder` references. It wires two kinds of asset together:

```
…MapGenerationSettings (style)    Map/
  _layouts  = one or more MapLayoutSettings + weight   Map/Layouts/   ← shape; each seed picks one by weight
  _content  = one MapContentSettings                   Map/Content/   ← room types, guaranteed rooms, pacing, sections, intensity, factions
  _modifiers = MapRunModifier assets + _modifierCount  Map/Modifiers/ ← optional twists, picked per seed
```

| Asset | Create menu | Assign |
|---|---|---|
| **MapLayoutSettings** | CGD › Map › Map Layout Settings | sizes, main path, branches, loops (incl. `_oneWayShortcutChance`), gates (incl. `_minDepth` and the key preferences) |
| **MapContentSettings** | CGD › Map › Map Content Settings | `_nodeRules` (incl. `_minSpacing`, `_wantsSpace`), `_fillType`, `_guarantees` (type + spot: Within Depth / Behind Every Gate / Before Boss, count, depth band), `_pacing` (`_combatTypes`, `_maxCombatInARow`, `_restAfterElite`, `_restType`), intensity curve, `_sections` = `Map/Sections/` assets in run order, `_factions` = Faction assets from `Data/Factions/`, `_factionMixes` = `Map/FactionMixes/` assets + weight (empty = an even split). With factions listed every room belongs to one — the validator flags rooms without |
| **MapGenerationSettings** | CGD › Map › Map Generation Settings | `_layouts` (at least one, weight 0 = never unless all are 0), `_content` (required), `_modifiers` = `Map/Modifiers/` assets, `_modifierCount` (0–1 on the ready-made styles, 0–2 on Random), `_maxWarnings` / `_maxAnomalies` (2 / 1), `_nodeSpacing` |
| **MapSectionDefinition** | CGD › Map › Section | `_displayName`, `_color` (Section view mode) |
| **MapRunModifier** | CGD › Map › Run Modifier | name, `_kind` (Warning / Anomaly), description (announced in the level), `_weight`, `_lootLuck` (added to every loot container in the level), per-type adjustments (weight ×, extra min/max), extra rooms/loops/gates, locked/secret chance ×, intensity offset, `_factionMix` override |
| **MapFactionMix** | CGD › Map › Faction Mix | `_dominance` (0 = even split, 1 = one faction holds almost everything), `_scatter` (chance a room is a random faction; 1 = fully random), `_pocketsPerFaction`. Every room always gets a faction |

- **Generate** stays disabled until the style has content and at least one layout.
- **Blueprint** shows the floor plan; its toolbar picks the `LevelBuildSettings` to lay out with (defaults to the project's first one, e.g. `Level/DefaultLevelBuildSettings`) and the fill (room type / faction). It uses the graph asset's seed, as `LevelBuilder` does with `_seed` 0.
- **Analyze** opens the style report for the asset's style (seed count and first seed in its toolbar).
- A room function only prefers sections listed in a style's content; a style without `_sections` gives every room no section.
- A new map style is usually just a new style asset pointing at existing layouts and content.

## Generated Levels

A scene can build its level from a map graph at load instead of using hand-placed geometry.
**`Scenes/MapTest`** is ready-made: the Sandbox systems (Player, HUD, GameManager, GameFlow, EventSystem, Global Volume, RespawnPoint, Directional Light) and a `Level` object generating from `Map/RandomMapGenerationSettings` with `Level/DefaultLevelBuildSettings`, seed 0 (a new map every play and every restart). Change `_generation` or `_seed` on `Level` to test a style or keep a map; set `_graphAsset` to play a hand-edited graph. Its NavMeshSurface collects **Physics Colliders**, so the wall kit's visual pieces stay out of it. It also has the map set up: a `WorldMap` object (`WorldMapArea`, fog of war on) that `LevelBuilder` fits to the level and paints with the level's blueprint; `Minimap`, `WorldMap` (M) and `ScreenFlash` elements added under the HUD; and `MapRevealer` + `FeedbackPlayer` added to the Player (run modifiers and pickups are announced). These are scene additions to the HUD and Player prefab instances — the prefabs themselves (and Sandbox) are unchanged.
For another scene, start from a copy of the Sandbox scene and delete its level geometry, target dummies and NavMesh data.

```
Level              [LevelBuilder, NavMeshSurface]   ← at the origin, unrotated
  (rooms, corridors, doors, props and patrol routes are created here at runtime)
```

| Component | Assign | Notes |
|---|---|---|
| **LevelBuilder** | `_settings` = `Level/DefaultLevelBuildSettings` · `_graphAsset` = a `MapGraphAsset` **or** `_generation` = a `Map/…MapGenerationSettings` style (e.g. `BranchingMapGenerationSettings`, or `RandomMapGenerationSettings` for a different shape each run) · `_seed` (0 = random; with `_graphAsset`, 0 = the asset's seed) · `_navMesh` = its NavMeshSurface · `_player` = Player · `_spawnPoint` = RespawnPoint · `_worldMap`? = WorldMapArea · `_blueprintMap` (on: the map shows the level's floor plan — rooms coloured by type, walls, gates — and covers exactly the level; off: the area's own background) | Logs a warning for every corridor that had to cross another. Announces the run's modifiers through the HUD feed (needs a FeedbackPlayer in the scene). |
| **NavMeshSurface** | Collect Objects = **Current Object Hierarchy** | Rebuilt at runtime. Don't bake it. |

`LevelBuildSettings` holds the grid sizes, wall materials, corner bevels (`_roomChamfer` 0–2 tiles, default 1; `_chamferCorridorBends`), the wall art (`_wallKit` = `Level/WallKits/TechWallKit`, `_factionWallKits` = TECH/BIO/VOID Faction → their `…WallKit`; empty `_wallKit` = plain box walls), how round curved rooms are (`_curveSmoothing` 0–3 passes, default 2; 0 = 45° walls), door prefabs, the key prefab, door signs (`_buildDoorSigns`, `_doorSignMaterial` — empty = the floor material, tinted and made emissive — and `_dangerIntensity`), the default room **Shapes** and one **Rooms** entry per room type.
The Resupply entry uses `SupplyDepotRoomFunction` (an `AmmoCache` prop against a wall); the Breach entry uses the Combat functions and enemy count (its enemies come from the two factions' rosters).
Each entry lists its **Functions**, enemy prefabs (count read at the room's intensity), a centrepiece and props.

| Asset | Folder | Sets |
|---|---|---|
| **RoomFunction** (Lobby, Restaurant, Park, Casino, CrewQuarters, CargoBay, GrandAtrium, DockingBay, ReactorCore, SupplyDepot) | `Level/Functions/` | `_preferredSections` + `_sectionPreference` (weight × inside them), `_chamfer` (-1 = the settings' `_roomChamfer`; GrandAtrium and DockingBay use 2), `_shapes` (empty = the settings' shapes), `_cells` (1–2 each way), `_size` in tiles (single-cell only; 0 = `_roomTiles`), `_wallHeight` (0 = the settings'), `_openCeiling`, `_landmark`?, `_structure` rules in order, `_props` |
| **Enemy prefab** (MaintenanceBot, Scavenger, Husk) | `Prefabs/Characters/Enemies/` | Root: `NavMeshAgent`, `EnemyHealth` (`_data`, `_hitboxProfile` = the target dummy's, `_healthBar`), `EnemyAI` (`_data`, `_obstacleMask` = Default), `Stunnable`, `StatusEffectController`, `EnemyHealthBar`, `EnemyStateVisuals` (`_renderers` = Visor), `DespawnOnDeath`, `MapMarker` (faction colour). Children: `Body` (capsule collider + `Hitbox` Body), `Head` (sphere collider + `Hitbox` Head), `Visor`, faction accents (visual only). Stats in `Data/Enemies/…EnemyData` — keep low tiers near 100 effective health (`EnemyData.EffectiveHealth` = (health + shield) × (1 + armour/100)) |
| **WallKit** (Tech, Bio, Void) | `Level/WallKits/` | `_straight` (repeated along straight walls), `_angled` (bevels and curves; empty = straight), `_post` (corners and wall ends), `_doorFrame` (every doorway, room side), `_pieceLength` / `_pieceHeight` (authored size, 3 × 4 m), `_postMinAngle`. Pieces: origin on the floor at the wall's start, +X along it, +Z toward the room; door frames like door prefabs (+Z out of the room). Visual only — the box walls stay as invisible collision. Placeholders in `Prefabs/Environment/WallKit/`, materials in `Art/Materials/Environment/WallKit/` |
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
- **Enemy prefabs** need `EnemyAI`. The tier-1 ones are `Prefabs/Characters/Enemies/` `MaintenanceBot` (TECH), `Scavenger` (BIO) and `Husk` (VOID), in their factions' `_enemies`; how many spawn still comes from each room type's `EnemyCount`.
- **Door prefabs**: origin at the doorway centre on the floor, +Z pointing out of the room. The opening is one tile wide (`_tileSize`).
  - `DefaultLevelBuildSettings` uses the ready-made ones in `Prefabs/Environment/Level/` (placeholder boxes in Gridbox colours; swap in real art there):

    | Slot | Prefab | Is |
    |---|---|---|
    | `_doorPrefab` | — (empty) | normal connections stay open passages |
    | `_lockedDoorPrefab` | `LockedLevelDoor` (yellow) | `Door` on the `Hinge` child, `_key` = `Items/Keycards/SecurityKeycard` ×1, Consume on |
    | `_secretDoorPrefab` | `SecretLevelWall` (white, like the walls) | `Destructible` (60 HP) + `DespawnOnDeath` (0 s) on a wall-sized box: shoot it to open |
    | `_oneWayDoorPrefab` | `LevelDoor` (cyan) | plain `Door` on the `Hinge` child; the builder bars it so it only opens from the corridor side. Empty = `_doorPrefab`; with neither, one-way shortcuts are open passages |
    | `_keyPrefab` | `SecurityKeycardPickup` | `ItemPickup` of one `SecurityKeycard` |
  - A one-way door prefab needs a `Door` on its root or a child.
  - Closed doors cut the NavMesh, so enemies don't follow the player through them.
- **Keys**: `_keyPrefab` is placed in the room the map graph picks for each Locked connection. Every key is reachable without opening a door, and each locked door uses one up, so one key item (`SecurityKeycard`) serves every door. For other locks, make the pickup's item match the door's `Door._key` and tick `_key.Consume`.
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
| `Player/Dodges/` | `SidestepRoll`, `CommittedRoll`, `SteerableBoost`, `LongDash`, `AirDash`, `AbilityDash` (`…Dodge`; the last is the Dash ability's `Move`) — stages with speed, curve, steering, i-frames, commitment, follow-up press, cooldown | PlayerDodge, DevCatalog |
| `UI/` | `CrosshairSettings` | CrosshairHUD |
| `Flow/` | `GameFlowSettings` | GameFlow |
| `Timing/` | `GameTimeSettings` | GameTime |
| `Enemies/` | `MaintenanceBot`/`Scavenger`/`Husk` `…EnemyData` (tier 1, ≈100 effective health each), `DefaultEnemyData`, `TargetDummyEnemyData` | EnemyAI, EnemyHealth |
| `Combat/HitboxProfiles/` | `DefaultHitboxProfile`, `TargetDummyHitboxProfile` | EnemyHealth, PlayerHealth |
| `Combat/StatusEffects/` | Bleed, Fire, Ice, Lightning, Poison | on-hit effect lists |
| `Combat/ActionTimelines/` | `GroundSlamTimeline` | TimelineAbility, melee attack steps |
| `Weapons/Ranged/` | `DefaultWeaponData`, `T1/` examples per category | Loadout, WeaponPickup |
| `Weapons/Categories/` | one per weapon type | RandomWeaponPickup, loot |
| `Weapons/FireBehaviors/` | Hitscan, Projectile, Shotgun | weapon data / categories |
| `Weapons/Melee/`, `Weapons/Throwables/` | `DefaultMeleeWeaponData`, `DefaultGrenadeData`, `SmokeGrenadeData` | MeleeController, throwable items |
| `Items/Munitions/` | one per caliber | AmmoPickup, PlayerInventory |
| `Items/` | `StatRollProfile`, `Keycards/SecurityKeycard` (opens the generated levels' locked doors) | weapon categories, `CombatVestArmor` (see note) |
| `Items/Armor/`, `Attachments/`, `Consumables/`, `Throwables/`, `Resources/` | `CombatVestArmor`, `ExtendedMagazineAttachment`, `BandageConsumable`, `StimConsumable`, `FragGrenadeThrowable`, `SmokeGrenadeThrowable`, `ScrapMetalResource`, `ClothResource` | pickups, loot, recipes, quest rewards |
| `Crafting/` | `BandageRecipe`, `CombatStimRecipe`, `ExtendedMagazineRecipe`, `CombatVestRecipe` | CraftingStation |
| `DevTools/` | `DevCatalog` (all items, weapon categories, buffs; no enemy prefabs exist yet) | DevCommands |
| `Abilities/` | Dash (needs `PlayerDodge`), Heal, Projectile, Shockwave, DamageBoost, ConeBlast (Targeted), GroundSlam (Timeline — needs `TimelineAbilityRunner`), Stealth (needs `Stealthable`) | PlayerAbilities |
| `Targeting/` | `Default…TargetSelector`, `AimedArea…`, `FriendlyArea…` | abilities, PlayerLockOn |
| `Meters/` | Stamina, Mana, Oxygen, Rage | MeterSet, costs, MeterZone |
| `Loot/` | `DefaultLootTable` | LootDropper |
| `Stats/` | `HardDifficultyModifierPreset`, `DamageBoostModifierPreset` | CharacterStats, Stat Buff ability |
| `Quests/` | `TargetPracticeQuest` → `ResupplyQuest`, `ShootingRangeClearedQuestSignal` | QuestTracker |
| `Feedback/` | ten `…FeedbackPreset`s | CombatFeedback, QuestFeedback, Stealthable |
| `Stealth/` | `StealthSettings` (shared) | Stealthable |
| `CameraEffects/` | `DefaultCameraEffectSettings` | CameraEffectsController |
| `Map/` | map styles `Linear`/`Branching`/`Hub`/`Labyrinth`/`Random` `…MapGenerationSettings`, `SandboxMapGraph` (uses Branching); `Layouts/` (`Linear`/`Branching`/`Hub`/`Labyrinth` `…MapLayoutSettings`), `Content/` (`Standard`/`TreasureHunt`/`Gauntlet` `…MapContentSettings`), `Sections/` (`Habitation`/`Commerce`/`Engineering` `…MapSection`), `Modifiers/` (warnings `Lockdown`/`Infestation`/`Overrun`, anomalies `Scavenger`/`FracturedHull` `…MapRunModifier`), `FactionMixes/` (`Infested`/`Balanced`/`Contested`/`Warped` `…MapFactionMix`) | Map Graph window, LevelBuilder |
| `Audio/` | `DefaultSurfaceDatabase` | PlayerFootsteps |
| `Factions/` | `TechFaction`, `BioFaction`, `VoidFaction` — name, colour, floor tint, `_enemies` roster (`MaintenanceBot` / `Scavenger` / `Husk`) | MapContentSettings, LevelBuilder |
| `Level/` | `DefaultLevelBuildSettings` (Gridbox materials, room rules without prefabs), `Functions/` (ten `…RoomFunction`s), `Shapes/` (seven `…RoomShape`s), `Structure/` (six `…StructureRule`s), `Landmarks/` (`ReactorCoreLandmarkRoom`), `WallKits/` (`Tech`/`Bio`/`Void` `…WallKit`) | LevelBuilder |
| `Impacts/` | `DefaultImpactDatabase` (empty effects) | ImpactSpawner |
| `Economy/` | `CreditsCurrency`, `DefaultPriceTable`, `Shops/GunsmithShopCatalog`, `Stock/GunsmithAmmoStock`, `Stock/GunsmithWeaponStock` | Vendor, PlayerInventory, DevCatalog |
| `Dialogue/` | `GunsmithDialogue`, `Actions/OpenShopDialogueAction` | Npc |

**Sounds** — every `SoundBank` slot is optional; systems stay silent without one. There are no audio clips in the project yet, so no `SoundBank` assets exist.

**Weapon quality is off.** No weapon category has a `_rollProfile` yet, so generated weapons
roll at quality 1 / Common. To turn it on: create one `StatRollProfile` per weapon family,
right-click it and pick the matching **Axes/…** preset, then assign it to that family's categories.
