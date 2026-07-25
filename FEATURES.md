# Project Features

## Player Movement
- Walk, sprint, and crouch with smooth speed transitions
- Rigidbody-based so the player interacts with the physics world (can push and be pushed)
- Jump with coyote time (can jump briefly after walking off a ledge) and jump buffering (jump queues if pressed just before landing)
- Variable jump height — releasing jump early cuts the arc short
- Slope detection — player moves smoothly up and down slopes within a set angle limit
- Step climbing — automatically steps up small ledges without getting stuck
- Moving platform support — player inherits the platform's velocity
- External impulse support — explosions, knockback, jump pads can all push the player via `AddImpulse`
- Dodge — two-phase system inspired by God of War: tap Q for a quick sidestep, then tap Q again within a short window to commit to a full dodge roll in the same direction; if the window expires the roll is cancelled and only a shorter sidestep cooldown applies; completing the full roll uses the longer cooldown; both phases show on the HUD cooldown indicator
- Slide — press crouch while sprinting to slide; launches at a configurable speed then decelerates smoothly; exits when speed drops below a threshold, the timer runs out, crouch is released, or the player leaves the ground
- Vault / Mantle — press Jump while airborne near a ledge to interact with it; low ledges are vaulted over with a velocity boost, taller ledges are mantled by smoothly pulling the player up onto the surface; works identically in first-person and third-person
- All movement values (speeds, jump height, gravity, etc.) are tunable in a ScriptableObject without touching code

## Camera
- Supports both first-person and third-person view
- Switch between views at any time with a keybind (default: V)
- Transition between views is smooth — camera slides between positions rather than snapping
- Aim point stays consistent during the transition — the center crosshair points at the same world direction in both modes
- Third-person camera avoids clipping into walls by pulling in when geometry is close
- Shoulder offset in third-person view, which fades out during the transition to first-person
- Shoulder swap — press X to flip the camera between the right and left shoulder in third-person
- Crosshair turns red when a wall is blocking the player's line of sight to the aimed target in third-person (aim obstruction indicator)
- Player mesh automatically hides when close enough to first-person to avoid it blocking the view
- Sprint FOV kick — field of view widens slightly while sprinting
- Mouse and gamepad both supported with separate sensitivity settings
- Body rotation is snappy in first-person and smooth in third-person
- Aim Down Sights (hold right mouse / right stick) — works in both first and third person
  - Zooms the FOV toward a configurable ADS value
  - In third person: pulls the camera in closer and centers the shoulder offset
  - Reduces look sensitivity while aiming; all transitions are smooth

## HUD / UI
- Modular HUD system — elements (crosshair, stats) are independent and can be shown or hidden individually
- Crosshair drawn entirely in code — no texture needed; updates instantly when settings change
- Crosshair is fully configurable: line length, thickness, center gap, opacity, color, center dot, and outline (color + thickness)
- Health bar that changes color from green to red as health drops, with a numeric readout
- Ammo counter displaying magazine and reserve rounds
- Stats HUD updates automatically whenever health or ammo changes — no polling needed
- All crosshair options live in a ScriptableObject so they can be tweaked in the Inspector and shared across scenes
- Dodge cooldown indicator — a single slot to the right of the ability bar that drains and refills, matching the ability HUD style
- Floating damage numbers — world-space popups appear at the hit point when an enemy takes damage; headshots show a larger gold number; each popup floats upward and fades out
- Hit flash and damage vignette — a red screen flash triggers on taking damage; a persistent red vignette around the screen edges grows stronger as health drops toward zero
- Velocity display — top-right HUD panel showing current movement speed in m/s, updated each frame

## Player Stats
- Health with TakeDamage and Heal methods
- Armor reduces incoming damage (diminishing returns — 100 armor halves damage taken); enemies mitigate the same way
- Shield absorbs damage before Health; any damage left over after a hit depletes the shield carries through to Health; shield regenerates automatically after a few seconds without taking damage; enemies can have shields too
- Ammo with UseAmmo and Reload methods
- Fires a change event so the HUD reacts immediately without polling

## Ability System
- Four ability slots (unbound by default; assign keys via the settings menu)
- Each ability has its own cooldown; activating during cooldown does nothing
- Cooldown starts only if the ability succeeds (e.g. heal won't trigger if already full health)
- Four built-in abilities: Dash, Projectile, Heal, and Shockwave
  - **Dash** — bursts the player horizontally in their move direction (or camera forward if idle)
  - **Projectile** — fires a fast-moving projectile from the camera that deals damage on impact
  - **Heal** — instantly restores a set amount of health
  - **Shockwave** — launches all nearby rigidbodies away from the player
- All ability values (cooldown, force, damage, etc.) are tunable on the ScriptableObject asset
- HUD shows four coloured slots at the bottom of the screen; a dark overlay drains away as each cooldown recovers

## Interaction System
- `IInteractable` interface — any world object can implement it to become interactable
- Player scans for nearby interactables each frame using a zero-allocation sphere overlap (configurable range, default 2.5 m); always selects the closest one
- Press E to trigger the interaction; prompts only appear when something is actually in range
- HUD prompt appears bottom-center of the screen showing a blue "E" key badge and the action label (e.g. "Pick Up  Assault Rifle"); disappears instantly when out of range
- Weapon pickups: place a `WeaponPickup` component on any world object, assign a `WeaponData` asset; picking it up fills the first empty loadout slot and equips it, or replaces the active slot if all four are full; the pickup object is destroyed on collection
- Random weapon pickups: add `RandomWeaponPickup` alongside `WeaponPickup` and assign a `WeaponCategoryData` asset; each time the object spawns a unique weapon is generated with randomised stats drawn from the category's thresholds
- Ammo pickups: `AmmoPickup` adds reserve ammo to whichever weapon the player currently has equipped
- Health pickups: `HealthPickup` restores a set amount of health
- Doors: press E on a `Door` to swing it open or closed (no animation — a plain procedural rotation)
- Switches: press E on a `Switch` to toggle one or more linked doors remotely

## Procedural Weapon Generation
- Six weapon categories: AR, SMG, Pistol, Sniper, LMG, Shotgun — each defined by a `WeaponCategoryData` ScriptableObject
- Every stat (damage, RPM, magazine size, spread, recoil, range, reload time, etc.) is defined as a min/max range with an optional bias value
- Bias < 0 skews the random result toward the minimum (e.g. SMG mags weighted toward 20–30 despite max being 50); bias > 0 skews toward the maximum
- Stat ranges reflect real-world and common game conventions per category:
  - **AR** — 600–850 RPM, 25–40 round mags (weighted 30–40), moderate recoil, 40–60 m effective range
  - **SMG** — 750–1100 RPM, 20–50 round mags (heavily weighted low, P90-style outlier), short range, erratic horizontal recoil
  - **Pistol** — 300–600 RPM semi/auto, 7–20 round mags (weighted low), high per-shot kick, 15–25 m range; wide damage spread (20–55) representing everything from Glock to Desert Eagle
  - **Sniper** — 30–80 RPM semi only, 5–10 rounds, 70–160 damage, 80–200 m optimal range, terrible hipfire, near-zero ADS spread
  - **LMG** — 600–950 RPM, 75–200 round belt/drum (weighted toward 75–120), slow reload (4.5–8 s), wide spread even ADS, high sustained recoil cap
  - **Shotgun** — 60–120 RPM semi/auto, 5–8 shell tube, 8–12 pellets per shot at 10–15 damage each, very short optimal range (8–15 m) with steep falloff, wide pellet cone (8–15° hip, 4–10° ADS), heavy per-shot recoil
- Several stats (ADS bloom, recoil recovery fraction, recovery delay, ADS recoil multiplier, hipfire camera kick) are automatically derived from the category type and fire rate so the weapon feels correct without manual tuning
- Create category assets via **Assets → Create → CGD → Weapon Category**, set the `Type` field, then right-click the asset and choose **Apply Type Defaults** to fill in all thresholds; values can be freely tweaked afterward

## Weapon Loadout
- Four weapon slots on the player; press 1, 2, 3, or 4 to equip the weapon in that slot
- Switching to a slot instantly equips the weapon and resets fire/reload state
- Inventory panel (press I) shows all four loadout slots, highlights the active weapon, and lists each weapon's name; empty slots are shown as "— Empty —"
- Loadout slots are configurable in the Inspector via WeaponData ScriptableObject assets

## Input System
- All actions are defined in one place and can be reconfigured without touching code
- Every action supports a primary and secondary keybind (e.g. LeftCtrl and C both crouch)
- Mouse buttons are also supported as primary or secondary bindings
- Each action can independently be set to one of three modes:
  - Pressed — fires once on the frame the key is pressed
  - Held — fires every frame the key is held down
  - Toggle — pressing the key flips it on or off
- Bindings can be remapped at runtime via the settings menu, driven entirely by the Input System's own interactive rebinding — no hand-rolled key/device translation code
- Every action can be bound to a keyboard key, mouse button, or gamepad button (any mix of two, primary and secondary) — rebinding listens for input from any connected device and picks up whatever is pressed first
- Default actions: Jump, Sprint, Crouch, Dodge, Attack, Melee (G), Grenade (H), Aim, Reload, Interact, Abilities 1–4 (unbound), Weapon slots 1–4 (keys 1–4), Pause, Switch View, Inventory (I), Map, Shoulder Swap (X)
- Move and Look are built the same way as every other action (WASD/arrows + gamepad left stick for Move, mouse delta + gamepad right stick for Look) — one input pipeline for the whole game instead of a separate non-remappable asset just for movement

## Settings Menu
- Built with the same runtime UGUI/TextMeshPro system as the rest of the HUD (not legacy IMGUI) — consistent styling and resolution scaling with everything else
- Press Escape at any time to open or close the settings menu
- Mouse and gamepad sensitivity sliders with live preview; changes are applied and saved on confirmation
- Full keybinding editor — every action shows its primary and secondary slot; click a slot then press any key, mouse, or gamepad button to rebind it
- Duplicate binding protection — rebinding to a key/mouse/gamepad button already used by another action (or the same action's other slot) is rejected with an on-screen warning instead of silently overwriting it
- Reset to Defaults button restores all keybindings to their original values
- All settings (sensitivity and keybindings) are saved to disk and automatically restored on next launch

## Weapon System
- Data-driven weapon setup via ScriptableObject assets — create a new gun by filling in a single asset, no code needed
- Three fire modes: Semi-auto (one shot per press), Full-auto (hold to fire), Burst (fixed burst per press)
- Pluggable fire behavior per weapon — assign a `WeaponFireBehavior` ScriptableObject asset on the `WeaponData` to choose how shots are resolved; three built-in behaviors: **Hitscan** (instant single raycast), **Shotgun** (fires N independent pellet raycasts per shot, count driven by `PelletCount` on the weapon), and **Projectile** (spawns a moving projectile from the muzzle); adding new fire types requires only a new ScriptableObject subclass
- Damage falloff — full damage up to an optimal range, then drops linearly to a configurable minimum at max range
- Headshot multiplier — colliders tagged "Head" receive bonus damage
- Bullet spread / bloom — hip-fire has a wider cone; firing continuously grows the spread; ADS tightens it; spread recovers quickly when not shooting
- Magazine and reserve ammo tracked per weapon; reserve ammo is snapped to full magazine-sized clips so counts stay in whole-mag multiples; ammo display in HUD stays in sync
- Tactical reload (round in chamber) is faster than an empty reload
- Recoil system:
  - Each shot kicks the camera upward (vertical recoil) and slightly sideways (horizontal recoil)
  - Vertical recoil has a small random variation per shot to avoid perfectly predictable patterns
  - Horizontal recoil wanders using a configurable bias (e.g. slight right drift), giving each gun a personality
  - Accumulated recoil is capped so full-auto spray stays controllable
  - ADS reduces all recoil by a configurable multiplier
  - Recovery is tunable per gun: 0 = BF-style (aim stays up, no return), 1 = CoD-style (full return to original aim), values between give a hybrid feel
- All values tunable per weapon: RPM, damage, ranges, spread, recoil amounts, reload times

## Melee Combat
- Tap the melee key for a light attack; hold it past a configurable threshold before releasing for a heavier finisher instead
- Light attacks chain into a combo string — pressing again while the current attack is swinging or recovering queues the next step, which fires the instant the current one finishes; the string resets back to the first step after a short period of no input
- Damage resolves through the same Effective Damage pipeline as guns and abilities (armor, penetration, damage type all apply)
- Independent of the equipped ranged weapon — always available regardless of which gun is out
- No animations yet — a wireframe sphere is drawn at the hit location while the attack is active so timing and reach are visible during testing

## Grenades
- Hold the grenade key to aim — a predicted trajectory arc is drawn from the throw point, accounting for gravity, and stops early at the first surface it would hit
- Release to throw; the grenade flies with real physics (gravity, bouncing) and detonates after a fixed fuse time
- Explosion deals damage in a radius, falling off linearly with distance from the blast center, through the same Effective Damage pipeline as everything else
- Limited carried count, like ammo — throwing decrements it, a pickup or event can restock it via `AddGrenades`
- Independent of the four ability slots and the equipped ranged weapon — its own dedicated key

## Enemy AI
- Behavior tree framework with four reusable node types: Selector (first-success), Sequence (all-must-succeed), Condition (predicate leaf), Action (logic leaf)
- Three AI states: Patrol, Alert, Chase — driven entirely by the behavior tree
- Patrol follows an ordered list of waypoints, looping continuously; idles in place if no waypoints are assigned
- Alert sends the enemy to the last known player position and returns to patrol after a configurable duration
- Chase closes the gap to the player and attacks at melee range with a configurable cooldown; stops moving while attacking
- Line-of-sight detection: raycast cone with tunable range and full-angle FOV; blocked by any geometry on the obstacle mask
- Hearing detection: proximity sphere with tunable radius; always triggers regardless of facing direction
- Losing sight switches the enemy to Alert for investigation; regaining sight immediately re-enters Chase
- State color indicator: mesh tints grey (patrol), yellow (alert), red (chase) via MaterialPropertyBlock — no material instances created
- World-space health bar appears above the enemy on damage and fades out after a configurable delay; billboards toward the camera
- All parameters (health, speeds, sight, hearing, attack, alert duration) are tunable per enemy type via an EnemyData ScriptableObject

## Visibility Culling
- Objects outside the camera frustum have their renderers disabled automatically, reducing draw calls without deactivating GameObjects
- Checks are time-sliced across multiple frames to avoid per-frame performance spikes
- Hysteresis prevents pop-in: objects activate slightly before fully entering the frame, and only deactivate once fully off-screen
- Visibility changes are debounced so edge objects stay stable instead of rapidly toggling on and off
- Objects close to the camera are always rendered regardless of frustum position
- Add `CullableObject` to any world object; place `VisibilityCullingManager` in the scene and assign the camera

## Death & Respawn
- When health reaches zero the player loses control, the HUD hides, and a death screen is shown
- Player automatically respawns after a short delay, returning to the designated spawn point
- Health and ammo are fully restored on respawn
