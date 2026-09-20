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
  - Zoom (FOV) and transition speed are per-weapon: snipers scope deep and slowly, pistols barely zoom and snap in, ARs sit in the middle
  - In third person: pulls the camera in closer and centers the shoulder offset
  - The held weapon smoothly raises from its hip position to a centred aim position and back
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

## Player Health
- Health with TakeDamage and Heal methods
- Armor reduces incoming damage (diminishing returns — 100 armor halves damage taken); enemies mitigate the same way
- Shield absorbs damage before Health; any damage left over after a hit depletes the shield carries through to Health; shield regenerates automatically after a few seconds without taking damage; enemies can have shields too
- Hitboxes — characters can be split into head, body, and limb hitboxes; each region deals its own damage multiplier (defaults: head ×1 plus the weapon's headshot bonus, body ×1, limbs ×0.75), tunable per character type via a Hitbox Profile asset
- Explosions and melee swings damage a character once, no matter how many of its hitboxes they overlap
- Teams — the player and enemies never damage their own side, so your own grenades and projectiles can't hurt you; damage without a team (hazards, status ticks) hits everyone
- Fires a change event so the HUD reacts immediately without polling

## Status Effects
- Weapons, melee attacks and grenades can each list on-hit status effects, each with its own chance to apply per hit
- **Bleed** — damage over time that ignores armor; reapplying refreshes it
- **Poison** — stacks up to 5; each stack makes the ticks much stronger
- **Fire** — damage over time based on the hit that applied it, still reduced by armor
- **Ice** — each stack slows and lowers armor; at 5 stacks the target is stunned; stacks wear off one at a time
- **Lightning** — jumps from the target to nearby characters on the same side, weaker with each jump; more stacks mean more jumps
- Each effect chooses how repeat hits stack: refresh the timer, add stacks on one timer, or give every stack its own timer
- Characters can be made immune to specific effects
- Status HUD — a row of tiles above the ability bar shows the player's active effects with their stack count and remaining time
- All active effects are removed when a character dies

## Ability System
- Four ability slots (unbound by default; assign keys via the settings menu)
- Each ability has its own cooldown; activating it with no charge left does nothing
- Abilities can store several charges; spent charges recharge one after another
- Optional cast time: the ability fires after a short delay and is cancelled if the player is stunned, mantling or rolling; the slot fills up while casting
- An ability that wouldn't do anything isn't used (e.g. Heal at full health), so no charge is spent
- Four built-in abilities: Dash, Projectile, Heal, and Shockwave
  - **Dash** — bursts the player horizontally in their move direction (or camera forward if idle)
  - **Projectile** — fires a projectile from the camera that deals damage (and optional status effects) on impact; configurable speed, lifetime and gravity drop
  - **Heal** — instantly restores a set amount of health
  - **Shockwave** — damages nearby enemies and launches nearby rigidbodies away from the player
- All ability values (cooldown, force, damage, etc.) are tunable on the ScriptableObject asset
- HUD shows four coloured slots at the bottom of the screen; a dark overlay drains away as the next charge recovers, and multi-charge abilities show their charge count

## Interaction System
- `IInteractable` interface — any world object can implement it to become interactable
- Player scans for nearby interactables each frame using a zero-allocation sphere overlap (configurable range, default 2.5 m); always selects the closest one
- Press E to trigger the interaction; prompts only appear when something is actually in range
- Hold interactions: doors and switches can require holding E for a set time; the prompt shows a progress bar and releasing early cancels
- Interactables behind walls or other solid objects are ignored (line-of-sight check, can be turned off)
- HUD prompt appears bottom-center of the screen showing a blue "E" key badge and the action label (e.g. "Pick Up  Assault Rifle"); disappears instantly when out of range
- Weapon pickups: place a `WeaponPickup` component on any world object, assign a `WeaponData` asset; picking it up fills the first empty loadout slot and equips it; if all four slots are full, your active weapon is swapped out and left behind in the pickup's place, keeping its remaining ammo
- Random weapon pickups: add `RandomWeaponPickup` alongside `WeaponPickup` and assign a `WeaponCategoryData` asset; each time the object spawns a unique weapon is generated with randomised stats drawn from the category's thresholds
- Ammo pickups: `AmmoPickup` adds reserve ammo to whichever weapon the player currently has equipped
- Health pickups: `HealthPickup` restores a set amount of health
- Pickups only show a prompt when they would do something — no health pickup at full health, no ammo pickup without a weapon — so they aren't wasted
- Doors: press E on a `Door` to swing it open or closed (no animation — a plain procedural rotation)
- Switches: press E on a `Switch` to toggle one or more linked doors remotely

## Items

- Every item the player can hold shares one definition type, split into two families:
  - **Stackable** — resources, consumables, munitions and attachments. These have no per-item variation at all: a better version is a separate item at a higher tier, not a luckier roll. Wood is wood; ironwood is its own item
  - **Rolled** — weapons and armor. Every piece is unique, with its own quality score and stats
- Items are tagged with the reality they came from (Tech, Bio, Void, or neutral)
- **Quality and tiers** — rolled gear has a quality score from 1 to 100, banded into five tiers: Common (1–20), Uncommon (21–40), Rare (41–60), Epic (61–80), Legendary (81–100)
- **Tradeoffs** — quality decides how much total power a piece has; tradeoff axes decide how that power is spread. High damage is paid for with fire rate and recoil, a large magazine is paid for with reload time, tight spread is paid for with draw speed and sway
  - At low quality the tradeoff is brutal: a hard-hitting Common fires like a musket
  - At high quality the same tradeoff still shapes the weapon's character, but the cost side stays perfectly usable — a Legendary is strong across the board and still has a personality
  - A bad roll is possible at every tier, and a good Common can outperform a poorly rolled Rare
- **Attachments** — craftable modifiers fitted into a weapon or armor piece's slots. Their effects are fixed, not rolled, and many carry a drawback alongside their benefit
  - Fitting is fully reversible: attachments sit on top of the item's own roll rather than overwriting it, so removing one restores the original
  - Higher-tier gear comes with more attachment slots (1 at Common through 4 at Legendary)
- **Inventory** — holds counted stacks and unique items side by side, with no capacity limit

## Procedural Weapon Generation
- Six weapon categories: AR, SMG, Pistol, Sniper, LMG, Shotgun — each defined by a `WeaponCategoryData` ScriptableObject
- Every stat (damage, RPM, magazine size, spread, recoil, range, reload time, etc.) is defined as a min/max range with an optional bias value
- Bias < 0 skews the random result toward the minimum (e.g. SMG mags weighted toward 20–30 despite max being 50); bias > 0 skews toward the maximum
- Stat ranges reflect real-world and common game conventions per category:
  - **AR** — 600–850 RPM, 25–40 round mags (weighted 30–40), moderate recoil, 40–60 m effective range
  - **SMG** — 750–1100 RPM, 20–50 round mags (heavily weighted low, P90-style outlier), short range, erratic horizontal recoil
  - **Pistol** — 300–600 RPM semi/auto, 7–20 round mags (weighted low), high per-shot kick, 15–25 m range; wide damage spread (20–55) representing everything from Glock to Desert Eagle
  - **Sniper** — 30–80 RPM semi only, 5–10 rounds, 70–160 damage, 80–200 m optimal range, terrible hipfire, near-zero ADS spread
  - **LMG** — 600–950 RPM, 75–200 round belt/drum (weighted toward 75–120), slow reload (4.5–8 s), wide hipfire, high sustained recoil cap
  - **Shotgun** — 60–120 RPM semi/auto, 5–8 shell tube, 8–12 pellets per shot at 10–15 damage each, very short optimal range (8–15 m) with steep falloff, wide pellet cone (8–15° hip, 2–4° ADS), heavy per-shot recoil
- Several stats (ADS bloom, recoil recovery fraction, recovery delay, ADS recoil multiplier, hipfire camera kick) are automatically derived from the category type and fire rate so the weapon feels correct without manual tuning
- Create category assets via **Assets → Create → CGD → Weapon Category**, set the `Type` field, then right-click the asset and choose **Apply Type Defaults** to fill in all thresholds; values can be freely tweaked afterward
- Generated weapons now roll a quality score and tier, which decides where each stat lands inside its category range — an SMG still rolls SMG damage, just high or low within it
- Stats that carry a weapon's *power* (damage, fire rate, magazine, reload, recoil, spread, range, draw, sway) are driven by quality and the tradeoff axes. Stats that only give it *character* (recoil recovery, heat behaviour, burst timing) stay random, so high-tier weapons don't all start feeling the same

## Weapon Loadout
- Four weapon slots on the player; press 1, 2, 3, or 4 to equip the weapon in that slot
- Switching to a slot equips the weapon and cancels any reload in progress
- Each weapon keeps its own magazine and reserve ammo — switching away and back doesn't refill it
- Inventory panel (press I) shows all four loadout slots, highlights the active weapon, and lists each weapon's name; empty slots are shown as "— Empty —"
- Starting weapons are configurable in the Inspector via WeaponData ScriptableObject assets

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
- Four fire modes: Semi-auto (one shot per press), Full-auto (hold to fire), Burst (fixed burst per press), Charge (hold to charge, release to fire; releasing below a per-weapon minimum charge cancels the shot with no ammo spent)
- Pluggable fire behavior per weapon — assign a `WeaponFireBehavior` ScriptableObject asset on the `WeaponData` to choose how shots are resolved; three built-in behaviors: **Hitscan** (instant single raycast), **Shotgun** (fires N independent pellet raycasts per shot, count driven by `PelletCount` on the weapon), and **Projectile** (spawns a moving projectile from the muzzle); adding new fire types requires only a new ScriptableObject subclass
- **Projectiles** travel physically through the world with configurable speed, lifetime and gravity drop (0 = perfectly straight — bullets, plasma; higher = arrow/mortar arc); each step swept-raycasts between its previous and next position so fast projectiles can't tunnel past thin colliders between frames; they pass through their shooter and any trigger colliders and stop on the first solid hit
- **Charge scaling on projectiles** — a projectile fire behavior can be authored so weak (short-hold) shots come out slower and drop harder while fully charged shots fly faster and straighter; a bow held only briefly lobs an arrow that plummets, while a fully drawn shot flies nearly flat
- Damage falloff — full damage up to an optimal range, then drops linearly to a configurable minimum at the falloff distance
- Bullets and shotgun pellets keep travelling past the falloff distance (1000 m by default) and still hit there, at the minimum damage
- Headshot multiplier — each weapon's headshot bonus applies when a shot or projectile lands on a critical hitbox (the head by default)
- Damage type and armor penetration per weapon (and per weapon category for generated weapons) — e.g. Lightning rounds hit shields harder
- Weapons can't fire or reload while stunned, mantling, or mid-roll
- Magazine and reserve ammo tracked per weapon; reserve ammo is snapped to full magazine-sized clips so counts stay in whole-mag multiples; ammo display in HUD stays in sync
- Tactical reload (round in chamber) is faster than an empty reload
- Auto-reload: pulling the trigger on an empty magazine, or keeping it held as the magazine runs dry, starts a reload; with no reserve ammo left it plays the empty click instead
- Weapon stats are grouped into three user-facing families that map directly to how the weapon *feels* — Accuracy, Control, Handling

### Accuracy (where bullets land)
- Hip-fire cone opens wide; ADS tightens it dramatically (each cone is a per-weapon degree value)
- Aiming down sights is pinpoint accurate (0.01° cone) and removes bloom entirely on every weapon type except shotguns; shotguns keep some bloom while aimed, which grows as the weapon heats up
- Aiming a shotgun tightens its pellet cone to roughly a quarter of the hip-fire cone
- **Bloom** — each shot adds to the spread cone up to a per-weapon cap; spread starts recovering a moment after each shot, so slow-firing weapons like shotguns and snipers tighten back up between rounds while sprays from automatic weapons still build up
- At maximum spread the crosshair still pops open with each shot and settles back, instead of sitting still at full size (accuracy itself stays at the maximum)

### Control (recoil and its buildup)
- Each shot kicks the camera upward (vertical) and slightly sideways (horizontal)
- Vertical kick has small per-shot jitter so patterns aren't perfectly predictable; horizontal drifts using a configurable left/right bias, giving each gun a personality
- **Horizontal drift mode** per weapon — *Alternating* (default) ping-pongs between the caps for a classic swaying spray, *OneWay* respects the bias direction the whole way to the cap for signature always-one-side pulls (AK-style hard right, e.g.)
- **Recoil buildup / heat** — sustained fire raises a per-weapon heat value that makes each shot kick harder and less predictably, up to a per-weapon maximum; heat cools off gradually once you stop firing, so short controlled bursts kick less than long sprays
- Accumulated recoil is capped per burst so full-auto spray stays controllable — once the gun reaches its maximum climb it stops rising, but every shot still kicks the view up and it settles back before the next round; sideways drift either ping-pongs between its limits (Alternating) or pins at the bias-side cap (OneWay); caps reset the moment the trigger is released
- The held weapon model kicks too: from the hip it visibly rears up and rolls; while aiming it drives back into the shoulder with a small hop that always settles before the next round can fire, so the sights are centred on the crosshair every time a shot leaves — if the sights were on target, the shot goes there
- ADS reduces recoil by a per-weapon multiplier and can hold a separate recovery fraction from hip fire
- Recovery is tunable per gun: 0 = BF-style (aim stays up, no return), 1 = CoD-style (full return to original aim); values between give a hybrid feel
- Player counterplay — if the player deliberately pulls down against active recoil, the recovery origin shifts to their new aim, so recovery never fights against intentional aim adjustments

### Handling (how the weapon moves in hand)
- **Draw time** — swapping to a slot has a per-weapon ready animation window during which the weapon can't fire or reload
- **Look sway** — the weapon lags behind mouse/stick input and springs back into place; heavier weapons lag further and settle slower, lighter weapons snap back instantly
- **Idle sway** — a subtle Perlin-driven breathing motion is always present at hip fire, so the weapon never feels frozen
- **Move sway** — walking and sprinting cause the weapon to bob in a figure-8 pattern that scales with movement speed and stops as soon as the player is airborne or standing still
- **Steady when aiming** — all sway (look, idle, move) fades out while aiming down sights, so the sights always line up with where shots go
- All handling values are tunable per weapon: draw time, look-sway amount and recovery, idle amplitude and speed, move-sway amount

### Tuning
- All values tunable per weapon: RPM, damage, ranges, Accuracy (spreads, bloom, recovery, cap), Control (kick, jitter, drift, buildup, recovery), Handling (draw, sway), reload times

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
- Three AI states — Patrol, Alert, Chase — each its own small state class
- Enemies find targets by team: any character on another team can be detected, so no player reference needs wiring
- Patrol follows an ordered list of waypoints, looping continuously; idles in place if no waypoints are assigned
- Alert sends the enemy to the last known position and returns to patrol after a configurable duration or on arrival
- Two combat types per enemy: Melee or Ranged, selectable on the EnemyData asset
- **Melee** enemies close the gap and attack at melee range: stop, turn to face the target, wind up, then strike everything hostile in front; configurable wind-up and cooldown
- **Ranged** enemies maintain a preferred engagement distance — advance when too far, retreat when too close, and strafe sideways while at range; fire hitscan shots at the target when they have line of sight, with configurable spread; burst fire is supported (multiple shots per trigger pull with a configurable interval between them); shots resolve through the full damage pipeline (armor, shield, hitbox regions all apply)
- Line-of-sight detection: raycast cone with tunable range and full-angle FOV; blocked by any geometry on the obstacle mask
- Proximity detection: hostiles within a tunable radius are noticed regardless of facing direction
- Hearing: gunfire, melee swings and explosions make noise with their own range; enemies within range go to investigate the source
- Getting hit alerts an enemy to the attacker, even from behind
- Losing the target switches the enemy to Alert for investigation; detecting it again immediately re-enters Chase
- Stuns and slows (e.g. from Ice) stop or slow enemies the same way they affect the player
- State color indicator: mesh tints grey (patrol), yellow (alert), red (chase) via MaterialPropertyBlock — no material instances created
- World-space health bar appears above the enemy on damage and fades out after a configurable delay; billboards toward the camera
- All parameters (health, speeds, sight, hearing, combat type, attack, ranged stats, alert duration) are tunable per enemy type via an EnemyData ScriptableObject

## Visibility Culling
- Objects outside the camera frustum have their renderers disabled automatically, reducing draw calls without deactivating GameObjects
- Checks are time-sliced across multiple frames to avoid per-frame performance spikes
- Hysteresis prevents pop-in: objects activate slightly before fully entering the frame, and only deactivate once fully off-screen
- Visibility changes are debounced so edge objects stay stable instead of rapidly toggling on and off
- Objects close to the camera are always rendered regardless of frustum position
- Add `CullableObject` to any world object; place `VisibilityCullingManager` in the scene and assign the camera

## Audio System
- All audio plays through a pooled AudioSource system — no per-shot Instantiate/Destroy, sources are reused automatically
- Each SoundBank can route to an Audio Mixer group so categories (SFX, footsteps, ...) can be balanced separately
- Sound data is driven by SoundBank ScriptableObject assets: assign multiple AudioClip variants with pitch and volume randomization so repeated sounds (gunfire, footsteps) never sound robotic
- Weapon audio: each WeaponData asset has optional SoundBank fields for fire, reload, and empty-magazine click; sounds play automatically at the muzzle position
- Melee audio: each MeleeAttackStep has optional swing and hit SoundBanks; swing plays on attack start, hit plays only when a target is struck
- Grenade audio: throw sound on release, explosion sound on detonation (plays via the pool so it persists after the grenade object is destroyed)
- Enemy audio: EnemyData has an optional attack SoundBank played on each melee strike; an EnemyAudio component plays hurt and death sounds
- Player feedback audio: a PlayerAudio component subscribes to the player's health events and plays hurt/death sounds
- Surface-aware footsteps: PlayerFootsteps raycasts downward each step to identify the ground surface; a SurfaceDatabase ScriptableObject maps PhysicMaterials to separate walk, sprint, and crouch SoundBanks; individual surfaces can override via a SurfaceTag component; step interval scales with movement speed
- All sound fields are optional — systems work silently when no SoundBank is assigned, same as before

## Death & Respawn
- When health reaches zero the player loses control, the HUD hides, and a death screen is shown
- Player automatically respawns after a short delay, returning to the designated spawn point
- On respawn, health and every carried weapon's ammo are restored, ability cooldowns reset, and leftover momentum and status effects are cleared
