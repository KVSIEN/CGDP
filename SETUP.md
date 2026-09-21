# Scene Setup Guide

How to wire every system into a playable scene. This describes the *intended* setup —
component hierarchy, required references, and which ScriptableObject assets each
system needs. Cross-check against `Assets/Project/Scenes/Sandbox.unity` for a working
reference scene built to this layout.

ScriptableObject assets live under `Assets/Project/Data/<Feature>/` (see the table at
the end). Folder layout is described in `README.md`.

## Scene Root

```
GameManager                  [VisibilityCullingManager, AudioPool]
RespawnPoint                 (empty transform — spawn point for PlayerLifecycle)
EventSystem                  (Unity default: EventSystem, InputSystemUIInputModule)
Directional Light            (Light + UniversalAdditionalLightData)
Global Volume                (URP post-processing)
```

- `GameManager` hosts `VisibilityCullingManager` — set `[DefaultExecutionOrder(-100)]` so it registers before `CullableObject.OnEnable()` runs elsewhere. Assign the scene's main `Camera` to its `_camera` field. `_batchFrames`, `_activationMargin`, `_deactivationMargin`, `_alwaysVisibleDistance` are tunable; defaults are fine to start.
- `GameManager` also hosts `AudioPool`. `_initialSize` defaults to 16 pooled AudioSources; increase if many sounds play simultaneously (rapid-fire weapons, crowds). Optional: assign an Audio Mixer group on each `SoundBank`.
- Projectiles, grenades and damage numbers are pooled at runtime under `DontDestroyOnLoad` roots (`PrefabPool`, `DamagePopups`) — nothing to place in the scene. Projectile and grenade prefabs are spawned through the pool, so don't destroy them manually.
- `Prefabs/Weapons/Projectile` needs a `Projectile` component and a renderer; the `Rigidbody` and `SphereCollider` it still carries are leftovers and are forced kinematic/trigger at runtime. A `TrailRenderer` on the same GameObject is optional — `Projectile` finds it in `Awake` and clears it on every launch, which pooled instances need or a reused round streaks a trail in from wherever the last one died. Recommended settings for a rifle round: `Time` 0.05, `Min Vertex Distance` 0.1, width ~0.05 tapering to 0, an unlit/additive material.
- `RespawnPoint` just needs a `Transform` — assign it to `PlayerLifecycle._spawnPoint`.

## Player Rig

```
Player                       [PlayerInputHandler, PlayerHealth, PlayerMovement,
                               PlayerDodge, PlayerMantle, PlayerAbilities,
                               PlayerInteraction, PlayerInventory,
                               WeaponController, PlayerWeaponLoadout,
                               MeleeController, GrenadeController,
                               PlayerLifecycle, PlayerFootsteps, PlayerAudio,
                               Stunnable, StatusEffectController]
  Rigidbody + CapsuleCollider on the Player root (required by PlayerMovement/PlayerDodge/PlayerMantle)
  - CameraRig
    - Main Camera             [Camera, UniversalAdditionalCameraData, PlayerCamera]
      - WeaponRig              [WeaponVisuals]
        - Weapon               [WeaponAimPose] (gun mesh — no colliders on it or its children)
          - Muzzle             (empty transform — fire origin)
  - Player Body                (visual mesh — hidden in first-person via PlayerCamera._firstPersonHideRenderers)
  - Head Anchor                (empty transform at eye height — first-person camera position)
```

**One `PlayerInputHandler` only.** Every other player component fetches it via `GetComponent` in `Awake`, so a duplicate on the same GameObject silently doubles input-processing overhead without anyone noticing — check for this before shipping a scene.

Wiring, by component:

- **PlayerInputHandler** — assign `_bindings` = `InputBindingSettings.asset`.
- **PlayerMovement** — assign `_settings` = `PlayerMovementSettings.asset`, `_cameraTransform` = Main Camera, `_playerMesh` = Player Body.
- **PlayerDodge** — assign `_settings` = same `PlayerMovementSettings.asset`.
- **PlayerMantle** — assign `_settings` = same `PlayerMovementSettings.asset`, `_cameraTransform` = Main Camera.
- **PlayerCamera** (on Main Camera) — assign `_input` = Player, `_movement` = Player, `_playerBody` = Player Body, `_headAnchor` = Head Anchor, `_camera` = the Camera component on the same object, `_firstPersonHideRenderers` = Player Body's renderer(s).
- **PlayerAbilities** — assign `_health` = Player's `PlayerHealth`, `_cameraTransform` = Main Camera, and `_slots[0..3]` = ability assets (`DashAbility.asset`, `HealAbility.asset`, `ProjectileAbility.asset`, `ShockwaveAbility.asset`, or `None` for an empty slot).
- **PlayerInteraction** — assign `_forwardReference` = Main Camera. `_requireLineOfSight` (default on) ignores interactables behind anything on `_occlusionMask`.
- **PlayerInventory** — assign `_startingStacks[]` = the stackable items the player spawns with, each a `Definition` (e.g. a `MunitionDefinition` such as `StandardMunitions`) plus a `Count`. Leave it empty to spawn with nothing — there is no implicit starting ammo, so a weapon's reserve reads 0 until a munition stack is listed here or an `AmmoPickup` is collected. Holds the shared `Inventory` that ammo pools, loot drops and consumables all flow through. The `Inventory` itself is a plain C# class and cannot appear in the Inspector; its live contents are shown in play mode by `PlayerInventoryEditor`, which also offers a debug "Add to Inventory" control.
- **WeaponController** — assign `_input` = Player, `_camera` = PlayerCamera, `_crosshair` = HUD's Crosshair object, `_muzzle` = Muzzle, `_visuals` = WeaponVisuals on WeaponRig. `_inventory` auto-resolves via `GetComponentInParent` if not wired. It fires whatever `PlayerWeaponLoadout` equips — no weapon asset is assigned here.
- **PlayerWeaponLoadout** — assign `_startingWeapons[0..3]` = `WeaponData` assets, e.g. `DefaultWeaponData` (optional — empty slots are filled by pickups). Must share the GameObject with `WeaponController` and `PlayerHealth` (it refills weapons when the player is revived).
- **MeleeController** — assign `_camera` = PlayerCamera, `_data` = a `MeleeWeaponData` asset. No other wiring — resolves `PlayerInputHandler`/`PlayerMovement` via `GetComponent` on the same object.
- **GrenadeController** — assign `_camera` = PlayerCamera, `_data` = a `GrenadeData` asset (which in turn needs a `GrenadePrefab` — see below). No other wiring — resolves `PlayerInputHandler`/`PlayerMovement`/`Collider` via `GetComponent` on the same object.
- **WeaponVisuals** (on WeaponRig) — no references to wire; `WeaponController` calls `AddKick()` for firing recoil, `Configure()` on weapon swap, and pushes look/movement/ADS state into it each frame for sway. Kick feel (hip spring, ADS timed kick) is tuned on the component; sway amounts (look, idle, move) live on the equipped `WeaponData`.
- **WeaponAimPose** (on Weapon, the child of WeaponRig) — assign `_camera` = PlayerCamera. The Weapon's local position/rotation in the scene is its hip pose; set `_adsPosition` / `_adsRotation` to the aimed pose (tune in Play mode while aiming, then copy the values back). Must not sit on WeaponRig itself — WeaponVisuals drives that transform.
- **Weapon model colliders** — the gun mesh under WeaponRig must have no colliders (remove the BoxCollider that Unity adds to primitives). Shots are raycast from the camera centre, so while aiming a collider on the sights blocks every shot; colliders there would also become part of the Player Rigidbody's collision shape.
- **PlayerFootsteps** — assign `_surfaces` = `SurfaceDatabase.asset`. Step intervals (`_walkInterval`, `_sprintInterval`, `_crouchInterval`) and `_groundMask` are tunable; defaults are fine to start. No other wiring — resolves `PlayerMovement` via `GetComponent`.
- **PlayerAudio** — assign `_health` = Player's `PlayerHealth`, `_hurtSound` / `_deathSound` = `SoundBank` assets (optional — silent when unassigned).
- **PlayerLifecycle** — assign `_health`, `_movement`, `_abilities`, `_input` = the matching Player components, `_hud` = HUD's `HUDManager`, `_spawnPoint` = `RespawnPoint`, `_deathScreen` = a death-screen UI object if one exists (optional).

## HUD Canvas

```
HUD                           [Canvas, CanvasScaler, GraphicRaycaster, HUDManager]
  - Crosshair                  [CrosshairHUD]
  - Stats                      [StatsHUD]
  - Weapon                     [WeaponHUD]
  - Abilities                  [AbilityHUD]
  - Dodge                      [DodgeHUD]
  - Inventory                  [InventoryHUD]
  - ItemInventory              [ItemInventoryHUD]
  - Interact                   [InteractHUD]
  - WeaponPickup               [WeaponPickupHUD]
  - HitEffect                  [HitEffect]
  - Velocity                   [VelocityHUD]
  - StatusEffects              [StatusEffectHUD]
```

All HUD elements build their own visuals at runtime via `UIFactory` — no child UI
objects need to be pre-built, just the empty GameObject with a `RectTransform` and the
matching component. Damage popups (`DamagePopup`) and enemy health bars
(`EnemyHealthBar`) are **not** placed here — they're spawned/self-built at runtime by
`DamagePopup.Spawn()` and by the `EnemyHealthBar` component on each enemy, respectively.
Don't leave stray instances of either parented under the HUD canvas.

- **HUDManager** — no references to wire; it finds every `HUDElement` among its children when the scene starts, so new HUD elements only need to be placed under `HUD`.
- **CrosshairHUD** — assign `_settings` = `CrosshairSettings.asset`, `_playerCamera` = Main Camera's `PlayerCamera`.
- **StatsHUD** / **HitEffect** — assign `_playerHealth` = Player's `PlayerHealth`.
- **StatusEffectHUD** — assign `_target` = Player's `StatusEffectController`.
- **WeaponHUD** — assign `_weapon` = Player's `WeaponController`.
- **AbilityHUD** — assign `_abilities` = Player's `PlayerAbilities`.
- **DodgeHUD** — assign `_dodge` = Player's `PlayerDodge`.
- **InventoryHUD** — assign `_input` = Player, `_loadout` = Player's `PlayerWeaponLoadout`. Requires a `CanvasGroup` on the same object (used to fade the panel in/out).
- **ItemInventoryHUD** — assign `_input` = Player, `_inventory` = Player's `PlayerInventory` (auto-resolved by scene lookup if left unset). Requires a `CanvasGroup` on the same object. Opens/closes on the same Inventory action as `InventoryHUD` — both panels sit as siblings under the HUD canvas.
- **InteractHUD** — assign `_interaction` = Player's `PlayerInteraction`. Builds its own world-space prompt via `DamagePopup.GetOrCreateOverlayCamera()` — no manual camera setup needed.
- **WeaponPickupHUD** — assign `_interaction` = Player's `PlayerInteraction`. Requires a `CanvasGroup`. Screen-anchored top-right; builds itself in `Awake` and only shows when the current interactable is a `WeaponPickup`. No wiring per pickup — stats are read from the pickup's `WeaponInstance` directly.

`InventoryHUD`, `ItemInventoryHUD`, `InteractHUD` and `WeaponPickupHUD` are excluded from `HUDManager.ShowAll()`/it only
shows a fixed subset — see `HUDManager.cs` before assuming every element reacts to
show/hide the same way.

## Settings Menu

```
SettingsMenu                  [RectTransform, SettingsMenu]   (must be under the HUD Canvas)
```

- Must live under a `Canvas` with a `GraphicRaycaster` (e.g. directly under `HUD`) — its buttons/sliders are real UGUI now and won't receive clicks otherwise.
- Its own `RectTransform` must be stretched to fill the screen (anchors `(0,0)`–`(1,1)`, zero offsets) — it builds a centered 680×520 window and a full-screen rebind-listening overlay inside itself at runtime via `UIFactory`, same convention as the rest of the HUD.
- Assign `_camera` = Main Camera's `PlayerCamera`, `_input` = Player, `_bindings` = the same `InputBindingSettings.asset` used by `PlayerInputHandler`, `_hud` = HUD's `HUDManager`.
- No child objects need to be pre-built — the window, sensitivity sliders/fields, the scrollable keybinding list (one row per entry in `InputBindingSettings.Bindings`), and the rebind overlay are all constructed in `Awake()`.
- Escape toggles it; it disables `PlayerInputHandler.InputEnabled` and unlocks the cursor while open.

## Enemy

```
Enemy                         [NavMeshAgent, EnemyAI, EnemyStateVisuals, EnemyHealth, EnemyHealthBar,
                               EnemyAudio, Stunnable, StatusEffectController]
```

- Requires baked NavMesh (`NavMesh Surface` in the scene, baked over the walkable ground).
- **EnemyAI** — assign `_data` = the enemy's `EnemyData` asset (e.g. `DefaultEnemyData`), `_waypoints` = patrol point transforms (optional — idles if empty), `_targetMask` = layers hostile characters are on (default Everything), `_obstacleMask` = geometry layers that block line-of-sight. No player reference — targets are found by team. Requires `Stunnable` (added automatically). Set `CombatType` to `Melee` or `Ranged` on the `EnemyData` asset — ranged enemies use additional fields (`PreferredRange`, `SpreadAngle`, `BurstCount`, `BurstInterval`, `StrafeInterval`, `StrafeDistance`, `RangedAttackSound`).
- **EnemyStateVisuals** (optional) — assign `_renderers` = the enemy's renderer(s) for the patrol/alert/chase color tint.
- **EnemyHealth** — assign `_data` = the same `EnemyData` asset, `_healthBar` = the `EnemyHealthBar` on the same object, `_hitboxProfile` = a `HitboxProfile` asset, e.g. `DefaultHitboxProfile` (optional — see Hitboxes).
- **EnemyHealthBar** — no references required; it builds its own world-space canvas in `Awake`.
- **EnemyAudio** — assign `_hurtSound` / `_deathSound` = `SoundBank` assets (optional — silent when unassigned). No other wiring — resolves `EnemyHealth` via `GetComponent`.
- **StatusEffectController** (on the Player and the target dummy in the reference scene) — lets status effects apply to that character. Must sit on the same GameObject as its `PlayerHealth`/`EnemyHealth`; optionally list `_immunities`. Without it, on-hit status effects are ignored for that character.
- **Stunnable** — required by `PlayerMovement` and `EnemyAI` (Unity adds it automatically); holds stun and slow state that status effects write to. No references to wire.
- **Teams** — `PlayerHealth` is always on the Player team; each enemy's team comes from `EnemyData.Team` (default Enemy). Attackers take their team from the `HealthManager` on their own GameObject or a parent, so weapon, melee, grenade and ability components must live on (or under) the character that owns them.
- **On-hit effects** — fill `OnHitEffects` (effect asset + chance) on a `WeaponData`/`WeaponCategoryData`, a `MeleeWeaponData` attack step, or a `GrenadeData`, using the assets in `Data/Combat/StatusEffects/`.

### Hitboxes (optional, Player or Enemy)

```
Enemy                         [EnemyHealth (or PlayerHealth) — the HealthManager]
  - Head                      [Collider (non-trigger), Hitbox (Region = Head)]
  - Body                      [Collider (non-trigger), Hitbox (Region = Body)]
  - LeftArm / RightLeg / ...  [Collider (non-trigger), Hitbox (Region = Limb)]
```

- `EnemyHealth` and `PlayerHealth` are both `HealthManager`s; every `Hitbox` forwards damage to one and the `HealthManager` does the math.
- **Hitbox** — set `_region`. `_owner` auto-fills from the nearest parent `HealthManager` when the component is added (or at runtime if empty); assign it manually only when the hitbox isn't under its owner in the hierarchy.
- Hitbox colliders must **not** be triggers — weapon raycasts ignore triggers. Parent them to the matching bones/child transforms so they follow the model.
- If the owner also has its own collider (e.g. a movement capsule), shots hitting it count as Body. To make only hitboxes receive hits, put the hitboxes on their own layer and remove the capsule's layer from each weapon's `HitMask`; use the physics collision matrix to stop hitboxes blocking movement if needed.
- **HitboxProfile** (`Create > CGD > Combat > Hitbox Profile`, one per character type) — per-region `DamageMultiplier` and `IsCritical` (critical regions also apply the weapon's headshot multiplier). Without a profile every region is ×1 and Head is still critical.
- Characters with no `Hitbox` components keep working — a collider on the `HealthManager` object itself takes Body hits.

## Interactables & Pickups

```
WeaponPickup (any name)       [Collider (isTrigger), WeaponPickup, CullableObject?]
AmmoPickup (any name)         [Collider (isTrigger), AmmoPickup, CullableObject?]
HealthPickup (any name)       [Collider (isTrigger), HealthPickup, CullableObject?]
Door (any name)               [Collider (solid — blocks the player when closed), Door]
Switch (any name)             [Collider (isTrigger), Switch]
```

- **WeaponPickup** — assign `_data` = a fixed `WeaponData` asset, **or**
- add **RandomWeaponPickup** alongside it and assign `_categories` = one or more `WeaponCategoryData` assets (`AssaultRifleCategory`, `SubMachineGunCategory`, `PistolCategory`, `SniperCategory`, `ShotgunCategory`, `LightMachineGunCategory` — the scene's pickups don't list `SniperCategory` yet) plus `_fixedIndex = -1` for a random pick.
- **AmmoPickup** — assign `_munition` = a `MunitionDefinition` asset (this decides which shared `AmmoType` pool the rounds land in) and `_amount` (rounds granted; default 30). Adds to the interacting player's `PlayerInventory`, not to the equipped weapon — any weapon drawing that caliber sees the rounds.
- **HealthPickup** — assign `_amount` (health restored; default 25). No reference wiring — finds `PlayerHealth` via `GetComponent` on the interacting player.
- **Door** — place the GameObject's pivot at the hinge edge, not the center (the whole object rotates in place). Assign `_openAngle`/`_openSpeed` as needed, and `_holdDuration` > 0 to require holding E. Directly interactable with E; a `Switch` can also toggle it via `Toggle()`.
- **Switch** — assign `_label` and `_doors[]` = one or more `Door` components to toggle when interacted with (`_holdDuration` works like on `Door`). Doesn't need to be near the doors it controls.
- Any other world object that should respond to the Interact key just needs a component implementing `IInteractable` (`InteractLabel`, `Interact(GameObject)`, and optionally `CanInteract(GameObject)` to hide the prompt when it wouldn't do anything) — no other wiring required, `PlayerInteraction` finds it via an `OverlapSphere` scan.
- Environment meshes that should be frustum-culled need a `CullableObject` component — leave `_renderers` empty to auto-collect from children, or assign explicitly for multi-renderer objects.

## Required ScriptableObject Assets

| Asset | Used by |
|---|---|
| `Input/InputBindingSettings` | `PlayerInputHandler`, `SettingsMenu` |
| `Player/PlayerMovementSettings` | `PlayerMovement`, `PlayerDodge`, `PlayerMantle` |
| `UI/CrosshairSettings` | `CrosshairHUD` |
| `Enemies/<Name>EnemyData` (one per enemy type — `DefaultEnemyData`, `TargetDummyEnemyData`) | `EnemyAI`, `EnemyHealth` |
| `Combat/HitboxProfiles/<Name>HitboxProfile` (optional, one per character type — `DefaultHitboxProfile`, `TargetDummyHitboxProfile`) | `EnemyHealth`, `PlayerHealth` |
| `Combat/StatusEffects/` (`BleedEffect`, `FireEffect`, `IceEffect`, `LightningEffect`, `PoisonEffect`) | referenced by whatever applies the effect via `StatusEffectController` |
| `Weapons/Ranged/<Name>WeaponData` (per weapon — `DefaultWeaponData` is the generic starter; hand-authored Common-tier examples live under `Weapons/Ranged/T1/`: `M4A1_T1`, `MP5_T1`, `Glock17_T1`, `DesertEagle_T1`, `Kar98k_T1`, `M249_T1`, `M870_T1` — one per category, `DesertEagle_T1` shows the AmmoType override to HeavyRounds) | `PlayerWeaponLoadout`, `WeaponController`, `WeaponPickup` |
| `Weapons/Categories/<Name>Category` (per category) — assign `_rollProfile` = one of the family-specific `StatRollProfile` assets below | `RandomWeaponPickup`, `WeaponGenerator` |
| `Items/Profiles/<Family>Profile` (one per weapon family — `StandardFirearmProfile`, `PrecisionRifleProfile`, `AutomaticSupportProfile`, `ShotgunProfile`) — right-click the asset and pick the matching `Axes/...` preset | `GearDefinition._rollProfile` on each `WeaponCategoryData` |
| `Items/Munitions/<Name>Munitions` (one per caliber in use — `StandardMunitions`, plus Light/Heavy/ShotgunShells as weapons need them) — each sets the `AmmoType` pool it feeds | `AmmoPickup._munition`, `PlayerInventory._startingStacks` |
| `Items/DefaultStatRollProfile` (optional — assign to each `WeaponCategoryData`'s `RollProfile`; without one, stats roll uniformly and quality is ignored) | `WeaponCategoryData`, `ArmorDefinition` |
| `Weapons/FireBehaviors/` (`HitscanFireBehavior`, `ShotgunFireBehavior`, `ProjectileFireBehavior` → `Prefabs/Weapons/Projectile`) | assigned on each `WeaponCategoryData` / `WeaponData` `FireBehavior` |
| `Abilities/` (`DashAbility`, `HealAbility`, `ProjectileAbility` → `Prefabs/Weapons/Projectile`, `ShockwaveAbility`) — each has `MaxCharges` and `CastTime` | `PlayerAbilities._slots` |
| `Weapons/Melee/DefaultMeleeWeaponData` | `MeleeController` |
| `Weapons/Throwables/DefaultGrenadeData` (its `GrenadePrefab` — `Prefabs/Weapons/FragGrenade` — needs a `Rigidbody` + non-trigger `Collider` + `Grenade` component) | `GrenadeController` |
| `Audio/DefaultSurfaceDatabase` (empty until surface `SoundBank`s exist) | `PlayerFootsteps` |
| `SoundBank` assets (per sound — weapon fire/reload/empty, melee swing/hit, grenade throw/explosion, player hurt/death, enemy hurt/death/attack/ranged-attack, footstep walk/sprint/crouch per surface) | Various — all optional; systems work silently without them |

### Stat Roll Profile

`StatRollProfile` (**Assets → Create → CGD → Items → Stat Roll Profile**) is the one
asset that defines how item quality converts into stats, so it is meant to be a
**single shared asset** assigned to every `WeaponCategoryData` and `ArmorDefinition`.
Right-click it and choose **Apply Default Firearm Axes** to fill in the tradeoff axes,
the same way `WeaponCategoryData` has **Apply Type Defaults**.

Leaving `RollProfile` empty is supported and reproduces the previous behaviour exactly:
every stat rolls uniformly within its authored range, and the item falls back to
quality 1 / Common with no attachment slots.

`InputBindingSettings` and `PlayerMovementSettings` are each a **single shared asset**
referenced by multiple components — don't accidentally create per-component duplicates,
settings changes (and saved keybind rebinds) need to land in the one instance everyone reads.
