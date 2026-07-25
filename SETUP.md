# Scene Setup Guide

How to wire every system into a playable scene. This describes the *intended* setup —
component hierarchy, required references, and which ScriptableObject assets each
system needs. Cross-check against `Assets/Scenes/SampleScene.unity` for a working
reference scene built to this layout.

Asset paths referenced below live under `Assets/ScriptableObjects/Settings/` and
`Assets/ScriptableObjects/Data/` unless noted otherwise.

## Scene Root

```
GameManager                  [VisibilityCullingManager, AudioPool]
RespawnPoint                 (empty transform — spawn point for PlayerLifecycle)
EventSystem                  (Unity default: EventSystem, InputSystemUIInputModule)
Directional Light            (Light + UniversalAdditionalLightData)
Global Volume                (URP post-processing)
```

- `GameManager` hosts `VisibilityCullingManager` — set `[DefaultExecutionOrder(-100)]` so it registers before `CullableObject.OnEnable()` runs elsewhere. Assign the scene's main `Camera` to its `_camera` field. `_batchFrames`, `_activationMargin`, `_deactivationMargin`, `_alwaysVisibleDistance` are tunable; defaults are fine to start.
- `GameManager` also hosts `AudioPool`. `_initialSize` defaults to 16 pooled AudioSources; increase if many sounds play simultaneously (rapid-fire weapons, crowds).
- `RespawnPoint` just needs a `Transform` — assign it to `PlayerLifecycle._spawnPoint`.

## Player Rig

```
Player                       [PlayerInputHandler, PlayerStats, PlayerMovement,
                               PlayerDodge, PlayerMantle, PlayerAbilities,
                               PlayerInteraction, WeaponController,
                               PlayerWeaponLoadout, MeleeController,
                               GrenadeController, PlayerLifecycle,
                               PlayerFootsteps, PlayerAudio]
  Rigidbody + CapsuleCollider on the Player root (required by PlayerMovement/PlayerDodge/PlayerMantle)
  - CameraRig
    - Main Camera             [Camera, UniversalAdditionalCameraData, PlayerCamera]
      - WeaponRig              [WeaponVisuals]
        - Weapon               (gun mesh)
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
- **PlayerAbilities** — assign `_stats` = Player's PlayerStats, `_cameraTransform` = Main Camera, and `_slots[0..3]` = ability assets (`DashAbility.asset`, `HealAbility.asset`, `ProjectileAbility.asset`, `ShockwaveAbility.asset`, or `None` for an empty slot).
- **PlayerInteraction** — assign `_forwardReference` = Main Camera.
- **WeaponController** — assign `_input` = Player, `_camera` = PlayerCamera, `_crosshair` = HUD's Crosshair object, `_muzzle` = Muzzle, `_visuals` = WeaponVisuals on WeaponRig, `_data` = starting `WeaponData` asset (optional — `PlayerWeaponLoadout.Start()` equips slot 0 anyway).
- **PlayerWeaponLoadout** — assign `_slots[0..3]` = `WeaponData` assets (e.g. `AssaultRifle.asset`, `Shotgun.asset`, `SubMachineGun.asset`, `LightMachineGun.asset`, `Pistol.asset`).
- **MeleeController** — assign `_camera` = PlayerCamera, `_data` = a `MeleeWeaponData` asset. No other wiring — resolves `PlayerInputHandler`/`PlayerMovement` via `GetComponent` on the same object.
- **GrenadeController** — assign `_camera` = PlayerCamera, `_data` = a `GrenadeData` asset (which in turn needs a `GrenadePrefab` — see below). No other wiring — resolves `PlayerInputHandler`/`PlayerMovement`/`Collider` via `GetComponent` on the same object.
- **WeaponVisuals** (on WeaponRig) — no references to wire; `WeaponController` calls `AddKick()` on it directly.
- **PlayerFootsteps** — assign `_surfaces` = `SurfaceDatabase.asset`. Step intervals (`_walkInterval`, `_sprintInterval`, `_crouchInterval`) and `_groundMask` are tunable; defaults are fine to start. No other wiring — resolves `PlayerMovement` via `GetComponent`.
- **PlayerAudio** — assign `_stats` = Player's `PlayerStats`, `_hurtSound` / `_deathSound` = `SoundBank` assets (optional — silent when unassigned).
- **PlayerLifecycle** — assign `_stats`, `_movement`, `_abilities`, `_input`, `_weapon` = the matching Player components, `_hud` = HUD's `HUDManager`, `_spawnPoint` = `RespawnPoint`, `_deathScreen` = a death-screen UI object if one exists (optional).

## HUD Canvas

```
HUD                           [Canvas, CanvasScaler, GraphicRaycaster, HUDManager]
  - Crosshair                  [CrosshairHUD]
  - Stats                      [StatsHUD]
  - Weapon                     [WeaponHUD]
  - Abilities                  [AbilityHUD]
  - Dodge                      [DodgeHUD]
  - Inventory                  [InventoryHUD]
  - Interact                   [InteractHUD]
  - HitEffect                  [HitEffect]
  - Velocity                   [VelocityHUD]
```

All HUD elements build their own visuals at runtime via `HudUIFactory` — no child UI
objects need to be pre-built, just the empty GameObject with a `RectTransform` and the
matching component. Damage popups (`DamagePopup`) and enemy health bars
(`EnemyHealthBar`) are **not** placed here — they're spawned/self-built at runtime by
`DamagePopup.Spawn()` and by the `EnemyHealthBar` component on each enemy, respectively.
Don't leave stray instances of either parented under the HUD canvas.

- **HUDManager** — assign every child reference (`_crosshair`, `_stats`, `_weapon`, `_abilities`, `_dodge`, `_hitEffect`, `_velocity`, `_inventory`, `_interact`) to its matching child object above.
- **CrosshairHUD** — assign `_settings` = `CrosshairSettings.asset`, `_playerCamera` = Main Camera's `PlayerCamera`.
- **StatsHUD** / **HitEffect** — assign `_playerStats` = Player's `PlayerStats`.
- **WeaponHUD** — assign `_weapon` = Player's `WeaponController`.
- **AbilityHUD** — assign `_abilities` = Player's `PlayerAbilities`.
- **DodgeHUD** — assign `_dodge` = Player's `PlayerDodge`.
- **InventoryHUD** — assign `_input` = Player, `_loadout` = Player's `PlayerWeaponLoadout`. Requires a `CanvasGroup` on the same object (used to fade the panel in/out).
- **InteractHUD** — assign `_interaction` = Player's `PlayerInteraction`. Builds its own world-space prompt via `DamagePopup.GetOrCreateOverlayCamera()` — no manual camera setup needed.

`InventoryHUD` and `InteractHUD` are excluded from `HUDManager.ShowAll()`/it only
shows a fixed subset — see `HUDManager.cs` before assuming every element reacts to
show/hide the same way.

## Settings Menu

```
SettingsMenu                  [RectTransform, SettingsMenu]   (must be under the HUD Canvas)
```

- Must live under a `Canvas` with a `GraphicRaycaster` (e.g. directly under `HUD`) — its buttons/sliders are real UGUI now and won't receive clicks otherwise.
- Its own `RectTransform` must be stretched to fill the screen (anchors `(0,0)`–`(1,1)`, zero offsets) — it builds a centered 680×520 window and a full-screen rebind-listening overlay inside itself at runtime via `HudUIFactory`, same convention as the rest of the HUD.
- Assign `_camera` = Main Camera's `PlayerCamera`, `_input` = Player, `_bindings` = the same `InputBindingSettings.asset` used by `PlayerInputHandler`, `_hud` = HUD's `HUDManager`.
- No child objects need to be pre-built — the window, sensitivity sliders/fields, the scrollable keybinding list (one row per entry in `InputBindingSettings.Bindings`), and the rebind overlay are all constructed in `Awake()`.
- Escape toggles it; it disables `PlayerInputHandler.InputEnabled` and unlocks the cursor while open.

## Enemy

```
Enemy                         [NavMeshAgent, EnemyAI, EnemyHealth, EnemyHealthBar, EnemyAudio]
```

- Requires baked NavMesh (`NavMesh Surface` in the scene, baked over the walkable ground).
- **EnemyAI** — assign `_data` = `EnemyData.asset`, `_playerTransform` = Player, `_playerStats` = Player's `PlayerStats`, `_waypoints` = patrol point transforms (optional — idles if empty), `_obstacleMask` = geometry layers that block line-of-sight, `_stateRenderers` = the enemy's renderer(s) for the patrol/alert/chase color tint.
- **EnemyHealth** — assign `_data` = same `EnemyData.asset`, `_healthBar` = the `EnemyHealthBar` on the same object.
- **EnemyHealthBar** — no references required; it builds its own world-space canvas in `Awake`.
- **EnemyAudio** — assign `_hurtSound` / `_deathSound` = `SoundBank` assets (optional — silent when unassigned). No other wiring — resolves `EnemyHealth` via `GetComponent`.
- **StatusEffectController** (optional, not yet in the reference scene) — add to the Player and/or an Enemy to let status effects (Bleed, Poison, Fire, ...) apply to it. No references to wire; it resolves its `IDamageable` target via `GetComponent` in `Awake`, so it only needs `PlayerStats` or `EnemyHealth` present on the same GameObject.

## Interactables & Pickups

```
WeaponPickup (any name)       [Collider (isTrigger), WeaponPickup, CullableObject?]
AmmoPickup (any name)         [Collider (isTrigger), AmmoPickup, CullableObject?]
HealthPickup (any name)       [Collider (isTrigger), HealthPickup, CullableObject?]
Door (any name)               [Collider (solid — blocks the player when closed), Door]
Switch (any name)             [Collider (isTrigger), Switch]
```

- **WeaponPickup** — assign `_data` = a fixed `WeaponData` asset, **or**
- add **RandomWeaponPickup** alongside it and assign `_categories` = one or more `WeaponCategoryData` assets (`AssaultRifle`, `SubMachineGun`, `Pistol`, `Shotgun`, `LightMachineGun` — generate a `Sniper` category asset too if that category is needed, it isn't present yet) plus `_fixedIndex = -1` for a random pick.
- **AmmoPickup** — assign `_amount` (reserve ammo added to the player's currently equipped weapon; default 30). No reference wiring — finds `WeaponController` via `GetComponent` on the interacting player.
- **HealthPickup** — assign `_amount` (health restored; default 25). No reference wiring — finds `PlayerStats` via `GetComponent` on the interacting player.
- **Door** — place the GameObject's pivot at the hinge edge, not the center (the whole object rotates in place). Assign `_openAngle`/`_openSpeed` as needed. Directly interactable with E; a `Switch` can also toggle it via `Toggle()`.
- **Switch** — assign `_label` and `_doors[]` = one or more `Door` components to toggle when interacted with. Doesn't need to be near the doors it controls.
- Any other world object that should respond to the Interact key just needs a component implementing `IInteractable` (`InteractLabel`, `Interact(GameObject)`) — no other wiring required, `PlayerInteraction` finds it via an `OverlapSphere` scan.
- Environment meshes that should be frustum-culled need a `CullableObject` component — leave `_renderers` empty to auto-collect from children, or assign explicitly for multi-renderer objects.

## Required ScriptableObject Assets

| Asset | Used by |
|---|---|
| `InputBindingSettings.asset` | `PlayerInputHandler`, `SettingsMenu` |
| `PlayerMovementSettings.asset` | `PlayerMovement`, `PlayerDodge`, `PlayerMantle` |
| `CrosshairSettings.asset` | `CrosshairHUD` |
| `EnemyData.asset` (one per enemy type) | `EnemyAI`, `EnemyHealth` |
| `WeaponData` assets (per weapon) | `PlayerWeaponLoadout`, `WeaponController`, `WeaponPickup` |
| `WeaponCategoryData` assets (per category) | `RandomWeaponPickup`, `WeaponGenerator` |
| `WeaponFireBehavior` assets (`HitscanBehavior`, `ShotgunBehavior`, projectile behavior) | assigned on each `WeaponData.FireBehavior` |
| Ability assets (`DashAbility`, `HealAbility`, `ProjectileAbility`, `ShockwaveAbility`) | `PlayerAbilities._slots` |
| `MeleeWeaponData` asset | `MeleeController` |
| `GrenadeData` asset (its `GrenadePrefab` needs a `Rigidbody` + non-trigger `Collider` + `Grenade` component) | `GrenadeController` |
| `SurfaceDatabase.asset` | `PlayerFootsteps` |
| `SoundBank` assets (per sound — weapon fire/reload/empty, melee swing/hit, grenade throw/explosion, player hurt/death, enemy hurt/death/attack, footstep walk/sprint/crouch per surface) | Various — all optional; systems work silently without them |

`InputBindingSettings` and `PlayerMovementSettings` are each a **single shared asset**
referenced by multiple components — don't accidentally create per-component duplicates,
settings changes (and saved keybind rebinds) need to land in the one instance everyone reads.
