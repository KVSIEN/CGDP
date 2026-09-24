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
- Optional stamina — sprinting can drain a stamina meter and stop when it runs out, and dodges can cost stamina; once emptied, stamina has to recover past a threshold before it can be used again (off unless a stamina cost is set)
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
- Floating damage numbers — every hit gets its own number at the point it landed, so a shotgun blast shows one per pellet. Numbers are white with a heavy black outline; critical hits are a deep orange, bold, drawn larger and held a moment longer, and bigger hits are drawn bigger than chip damage. Each number is thrown up out of the impact and arcs as it slows, popping in past full size before settling
- Damage grouping — hits landing close together form one group, scattered across a small patch around the impact rather than lined up, and the patch widens slightly as a burst continues. Each number is thrown at its own angle and speed, so no two take the same path and the group breaks up as it rises while staying on the target it came from. Numbers overlap freely where they cross — nothing pushes them apart. Numbers already in a group flinch when a new hit lands, so a sustained burst reads as damage piling up. Spacing and size hold the same on screen at any range, and the oldest numbers fade out early once the screen is carrying too many
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
- Abilities can cost a resource (mana, energy...) as well as using a charge; an ability you can't afford doesn't fire and keeps its charge
- Six built-in abilities: Dash, Projectile, Heal, Shockwave, Targeted, and Timeline
  - **Dash** — bursts the player horizontally in their move direction (or camera forward if idle)
  - **Projectile** — fires a projectile from the camera that deals damage (and optional status effects) on impact; configurable speed, lifetime and gravity drop
  - **Heal** — instantly restores a set amount of health
  - **Shockwave** — damages nearby enemies and launches nearby rigidbodies away from the player
  - **Targeted** — damages and/or heals whoever its targeting rule picks: the enemy under the crosshair, everything in a cone, allies around the player, the nearest enemy, or an area where the player aims; it isn't used when it wouldn't affect anyone
  - **Timeline** — runs an ActionTimeline via TimelineAbilityRunner; only one timeline ability can play at a time; can be ground-targeted so its area effects land where the player aims
- All ability values (cooldown, force, damage, etc.) are tunable on the ScriptableObject asset
- HUD shows four coloured slots at the bottom of the screen; a dark overlay drains away as the next charge recovers, and multi-charge abilities show their charge count

## Interaction System
- `IInteractable` interface — any world object can implement it to become interactable
- Player scans for nearby interactables each frame using a zero-allocation sphere overlap (configurable range, default 2.5 m) and picks the one being looked at most directly; objects can be given a higher priority so they win when several overlap
- Optional highlight — the object the player is aiming at can glow so it's obvious which one E will use
- Press E to trigger the interaction; prompts only appear when something is actually in range
- Hold interactions: doors and switches can require holding E for a set time; the prompt shows a progress bar and releasing early cancels
- Interactables behind walls or other solid objects are ignored (line-of-sight check, can be turned off)
- HUD prompt appears bottom-center of the screen showing a blue "E" key badge and the action label (e.g. "Pick Up  Assault Rifle"); disappears instantly when out of range
- Hovering a weapon pickup opens a top-right stat panel with the weapon's name, tier (colored — grey/green/blue/purple/gold for Common through Legendary), quality score, category, and a two-column readout of damage, fire rate, magazine, reload, range, fire mode, ammo type, recoil, spread and draw time; contextual footer adds headshot multiplier, pellet count (shotguns), burst pattern (burst mode), charge time (charge mode) and armor penetration when relevant
- Weapon pickups: place a `WeaponPickup` component on any world object, assign a `WeaponData` asset; picking it up fills the first empty loadout slot and equips it; if all four slots are full, your active weapon is swapped out and left behind in the pickup's place, keeping its remaining ammo
- Random weapon pickups: add `RandomWeaponPickup` alongside `WeaponPickup` and assign a `WeaponCategoryData` asset; each time the object spawns a unique weapon is generated with randomised stats drawn from the category's thresholds
- Ammo pickups: `AmmoPickup` adds a set amount of a specific `MunitionDefinition` to the player's shared inventory — every weapon that draws from that pool benefits at once
- Health pickups: `HealthPickup` restores a set amount of health
- Pickups only show a prompt when they would do something — no health pickup at full health, no ammo pickup without a weapon — so they aren't wasted
- Doors: press E on a `Door` to swing it open or closed (no animation — a plain procedural rotation)
- Locked doors — a door can require an item (e.g. a keycard); without it the prompt reads "Locked (Red Keycard)", with it the prompt reads "Unlock" and the door opens and stays unlocked; the key can be used up or kept
- Switches: press E on a `Switch` to toggle one or more linked doors remotely (switches bypass door locks)
- Generic interactables — terminals, levers, buttons, NPC conversation starters and similar can be set up entirely in the Inspector: a label, optional hold time, optional required item, single use or a cooldown, and events for what happens (and for trying without the required item)
- Item pickups — any inventory item (resources, consumables, munitions, keycards, rolled gear) can lie in the world as a pickup showing its name and count
- Chests and containers — open once to spill their loot; can be locked behind a key

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
- **Starting contents** — the player can be set up to spawn with a list of stackable items (typically a munition stack per caliber they expect to use). Left empty, the player spawns with an empty pack and zero reserve ammo, so the first magazine is all they have until they find a pickup

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
- Each weapon keeps its own loaded magazine; reserve ammo is a shared pool per `AmmoType` — two SMGs share the same LightRounds supply, an SMG + sniper diversify across two pools, a hand cannon + sniper both compete for scarce HeavyRounds
- Inventory panel (press I) shows all four loadout slots, highlights the active weapon, and lists each weapon's name; empty slots are shown as "— Empty —"
- Item inventory panel (also press I, sits to the right of the loadout) shows a flat list of everything carried — stackable items aggregated by definition (so all Light Rounds show as one line with a total count rather than one line per internal 999-cap stack) plus each unique gear piece; a header shows slots-used and total weight against per-player caps that turn orange when exceeded
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
- Pluggable fire behavior per weapon — assign a `WeaponFireBehavior` ScriptableObject asset on the `WeaponData` to choose how shots are resolved; three built-in behaviors: **Hitscan** (instant single raycast), **Shotgun** (fires N independent pellets per shot, count driven by `PelletCount` on the weapon, each pellet either an instant raycast or a travelling round), and **Projectile** (lands close shots instantly, spawns a travelling round beyond that); adding new fire types requires only a new ScriptableObject subclass
- **Projectiles** travel physically through the world with per-weapon speed, lifetime and gravity drop (0 = perfectly straight — bullets, plasma; higher = arrow/mortar arc); they advance every frame and swept-raycast between their previous and next position, so fast rounds move smoothly and can never tunnel past thin colliders; they pass through their shooter and any trigger colliders and stop on the first solid hit, and give up at the weapon's maximum range
- Projectile physics (speed, lifetime, gravity, instant-hit window, charge multipliers) live on the equipped `WeaponData`, so one Projectile fire behavior asset serves every projectile-firing weapon in the game — each weapon carries its own bullet velocity, and an 800 m/s rifle round and a 60 m/s arrow can share one behavior with wildly different feel
- **Instant-hit window on projectile weapons** — close-range shots from a projectile weapon or a projectile-pellet shotgun land the moment the trigger breaks instead of waiting for a round to fly; the window is a flight time, so it scales with the weapon's own muzzle velocity and a fast rifle stays instant much further out than a slow bow. Beyond it the shot becomes a visible travelling round the player has to lead. Damage, range falloff and maximum range are identical either way, so there is no point downrange where a weapon's behaviour visibly changes hands
- **Charge scaling on projectiles** — per-weapon low-charge multipliers scale speed down and gravity up so a weak (short-hold) shot comes out slower and drops harder while a fully charged shot flies faster and straighter; a bow held only briefly lobs an arrow that plummets, while a fully drawn shot flies nearly flat
- **Projectile shotgun pellets** — a shotgun can be authored to throw physical pellets instead of instant raycasts: each pellet in the cone flies at its own muzzle velocity with optional gravity drop, so a distant target has to be led and the cloud visibly travels, while point-blank shells still register instantly through the same instant-hit window. Pellet count, spread, damage and falloff are unchanged either way
- Damage falloff — full damage up to an optimal range, then drops linearly to a configurable minimum at the falloff distance
- Bullets and shotgun pellets keep travelling past the falloff distance (1000 m by default) and still hit there, at the minimum damage
- Headshot multiplier — each weapon's headshot bonus applies when a shot or projectile lands on a critical hitbox (the head by default)
- Damage type and armor penetration per weapon (and per weapon category for generated weapons) — e.g. Lightning rounds hit shields harder
- Weapons can't fire or reload while stunned, mantling, or mid-roll
- Magazine is per weapon; reserve is a shared inventory pool keyed by `AmmoType` (LightRounds / StandardRounds / HeavyRounds / ShotgunShells / Arrows / EnergyCells). Reloading pulls rounds from that pool into the magazine; the HUD reserve display reads live from the inventory
- Weapons come loaded with one full magazine on first pickup; if you drop an empty weapon and pick it back up, it stays empty — the pickup grant only happens once per weapon instance
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
- Design targets for inter-category (AR vs SMG vs Sniper etc.) and intra-tier (Common vs Legendary) balance live in `WEAPON_BALANCE.md` in the project root — TTK targets per category, off-range falloff expectations, tier feel goals, and the derivation from `WeaponCategoryData` FloatRanges
- Hand-authored Tier 1 (Common) reference weapons live under `Data/Weapons/Ranged/T1/` — one per category (M4A1, MP5, Glock 17, Kar98k, M249, M870) plus a Desert Eagle hand-cannon variant that shows the Pistol category's HeavyRounds override. These are the "what Common feels like" baseline for every category

## Melee Combat
- Tap the melee key for a light attack; hold it past a configurable threshold before releasing for a heavier finisher instead
- Light attacks chain into a combo string — pressing again while the current attack is swinging or recovering queues the next step, which fires the instant the current one finishes; the string resets back to the first step after a short period of no input
- Damage resolves through the same Effective Damage pipeline as guns and abilities (armor, penetration, damage type all apply)
- Independent of the equipped ranged weapon — always available regardless of which gun is out
- Hit registration runs continuously during the Active window (every physics tick), not a single-frame check — moving targets are caught mid-swing
- Three hit shapes per attack step:
  - **Thrust** — single SphereCast forward (stabs, pokes); resolves hitbox regions so headshots and limb hits apply their multipliers
  - **Sweep** — fan of SphereCasts across a configurable horizontal arc (slashes, backhands); each ray resolves hitbox regions independently; ray count and arc width are tunable per step
  - **Slam** — OverlapSphere at the impact point (overhead smashes, ground pounds); area damage with no region resolution, hits everything in the radius once
- Each target is damaged at most once per swing regardless of how many ticks or rays touch it
- Per-step critical multiplier — applied when a Thrust or Sweep hits a critical region (head by default); tunable per combo step so a heavy finisher can crit harder than a quick jab
- Debug drawing shows the cast rays (Thrust/Sweep) or overlap sphere (Slam) each physics tick during the Active window
- When a MeleeAttackStep has an ActionTimeline assigned, the Active phase is driven by the timeline system instead of the legacy hit resolver — the timeline controls what shapes fire on which frames, while Windup and Recovery remain time-based

## Action Timeline System
- A data-driven frame-data system for choreographing per-frame hitbox logic for any ability or attack
- An ActionTimeline ScriptableObject defines a sequence of events across integer frame indices (one frame = one FixedUpdate tick at 50 Hz)
- An ActionTimelineRunner ticks through the timeline, activating and deactivating events each frame
- Events are polymorphic ([SerializeReference]) and stateless — all runtime state lives on the runner's ActionContext
- Available event types:
  - **ShapeHitEvent** — physics queries (SphereCast, Arc, Sphere, Box) with configurable damage, dedup, and region resolution; covers melee thrusts, sweeps, slams, AOE circles, and cones
  - **SpawnProjectileEvent** — spawns a projectile prefab via PrefabPool
  - **BeamEvent** — continuous raycast or SphereCast forward each tick (no dedup, intentional per-tick damage)
  - **SpawnZoneEvent** — spawns a persistent zone prefab (trap or lingering AOE) at a resolved position
  - **ForceEvent** — applies a force impulse to the caster or hit targets
  - **SoundEvent** — plays a SoundBank at the action origin
- Each event can resolve its position in one of three coordinate spaces: CameraRelative (melee default), WorldOffset (ground-targeted), or WorldAbsolute
- PersistentZone MonoBehaviour self-manages lifetime, periodic overlap checks, and pool release; supports one-shot traps (damages once then releases) and lingering AOEs (damages periodically until expired)
- Events can use a shared dedup set (one hit per target across the entire timeline) or per-event dedup (each event tracks its own targets independently)

## Grenades
- Hold the grenade key to aim — a predicted trajectory arc is drawn from the throw point, accounting for gravity, and stops early at the first surface it would hit
- Release to throw; the grenade flies with real physics (gravity, bouncing) and detonates after a fixed fuse time
- Explosion deals damage in a radius, falling off linearly with distance from the blast center, through the same Effective Damage pipeline as everything else
- Limited carried count, like ammo — throwing decrements it, a pickup or event can restock it via `AddGrenades`
- Independent of the four ability slots and the equipped ranged weapon — its own dedicated key

## Enemy AI
- Three AI states — Patrol, Alert, Chase — each its own small state class
- Enemies find targets by team: any character on an opposing team can be detected, so no player reference needs wiring; teamless props such as breakable crates are ignored
- Enemies can be spawned from the object pool and reused: a reused enemy comes back at full health and starts patrolling again
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

## Object Pooling
- Frequently spawned objects — projectiles, grenades, zones, pickups, particle effects and enemies — are reused instead of created and destroyed, avoiding hitches and garbage collection
- Pools grow on demand; they can also be pre-filled when a scene starts so the first burst of gunfire or the first enemy wave doesn't stutter
- Reused objects reset themselves (health, AI state, rolled contents), so a recycled object behaves like a fresh one
- Particle effects return themselves to the pool when they finish playing
- Everything still in flight is recalled when a new scene loads, so nothing leaks from one level into the next
- Dead enemies and broken props can be removed after a delay (and reused when they came from a pool)

## Loot & Drops
- Enemies, breakable props and chests drop loot from Loot Table assets
- **Guaranteed drops** — always dropped (a boss key, a quest item)
- **Weighted drops** — a set number of draws (fixed or a range) from a weighted list, with a configurable chance of a draw coming up empty; draws can be made unique so the same entry isn't picked twice
- **Nested tables** — an entry can roll another table, so shared pools like "common ammo" are authored once and reused
- **Rarity** — weapons and gear that drop get a tier (Common → Legendary) from the table's rarity odds, and their stats roll at that tier; a luck value makes empty draws rarer and high tiers likelier
- Drops can be stacks of items, individually rolled weapons and gear, or any world object (health orbs, effects)
- Drops scatter around the source and settle onto the ground; weapons become weapon pickups, everything else an item pickup
- Breakable props — crates and barrels can be given health so any attack breaks them, spilling their loot
- Included: a default enemy loot table dropping ammo of every caliber and, occasionally, a rolled rifle or SMG

## Game Flow
- The game moves between clear states: boot, main menu, loading, playing, paused, level transition, game over and victory; impossible moves (pausing from the main menu, winning while loading) are refused
- Pausing freezes gameplay time and world audio; the settings menu doubles as the pause menu and can't be opened during game over or loading
- Level loading runs in the background with progress for a loading screen, preceded by a short transition for fade-outs or results
- Restart level, return to main menu, start game and quit are available to UI buttons
- The player's death can end the run with a game-over state instead of respawning (per-scene option)
- UI panels can be shown only in certain states (pause panel, game-over screen, loading screen) without code
- The cursor is captured during play and released in menus automatically

## Targeting
- One shared set of targeting rules for abilities, attacks and effects: **Self**, **Raycast** (under the crosshair), **Area** (around the caster or where they aim on the ground), **Cone**, **Nearest** (the N closest), and **Ground** (a point, no characters)
- Each rule picks by relation — self, allies, enemies, neutral, or any mix — and can require clear line of sight and cap the number of targets (nearest first)
- Targeting rules are assets, so a new ability can reuse "enemies in a 60° cone" or "allies within 8 m" without code
- Enemy AI uses the same system to find the nearest visible hostile

## Resources (Meters)
- A generic system for consumable, regenerating values: stamina, mana, energy, rage, oxygen, battery charge, and so on — each defined by an asset with a name, colour, capacity, starting fill and regeneration
- Regeneration can refill (stamina, mana) or decay (rage), and pauses for a moment after the meter is pushed the other way
- Exhaustion — a meter emptied to zero can be locked until it refills past a threshold
- Meters can be spent all-or-nothing (ability costs), drained continuously (sprinting), restored, and have their capacity changed by upgrades
- Resource zones drain or restore a meter while you stand in them — water drains oxygen, a shrine restores mana — and can hurt you once the meter is empty (drowning)
- A HUD panel shows a coloured bar for each of the player's meters
- Shields run on the same regeneration model
- Included meters: Stamina, Mana, Oxygen and Rage

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
- On respawn, health is restored and every carried weapon's magazine is refilled; reserve ammo in the shared inventory pool is not touched (the ability to lose gathered inventory on death lands with the extraction loop). Ability cooldowns reset, and leftover momentum and status effects are cleared

## Map Graph Generation
- Maps start as a pure experience graph — no room geometry yet. Each node is what the player meets there (Start, Combat, Elite, Puzzle, Shop, Event, Treasure, Boss, Exit), and connections say how they link: normal, shortcut, secret or locked
- The generator builds a main path from Start through the Boss to the Exit, adds side branches that either dead-end or rejoin the main path further ahead, and adds shortcuts that skip rooms along the main path. Branch entrances can be secret or locked
- The same seed and settings always give the same map, so a good map can be kept by its seed
- Constraints live in a settings asset: main path length, branch count and length, how often branches rejoin, how many shortcuts, a connection limit per room, and for each room type a minimum and maximum count, a weight, where it can go (anywhere, main path only, branches only), how deep into the run it can appear, whether two of the same type can sit next to each other, and whether it prefers dead ends (for example, treasure as a reward for exploring)
- Room types with the tightest placement rules are placed first, so broad types like Combat can't take the only rooms a Treasure could use. Constraints that can't be met are reported instead of failing silently
- Each room gets an intensity from a difficulty curve over the run (rising, a breather before the boss, then the boss at full intensity), adjusted per room type with a little variation
- Factions each claim a starting room and their influence fades with every connection away from it; each room belongs to the strongest faction there, if any
- **Map Graph editor** (Window › CGD › Map Graph): generate from a seed or roll a new seed, pan and zoom, click to select, drag nodes around, shift-drag between nodes to connect them, right-click to add, retype, lock, disconnect or delete, and edit a node's type, intensity, faction and connections in the side panel. Every edit can be undone
- View modes colour the graph by room type, intensity, faction, required vs optional (rooms every route to the Exit must pass through), or branch. Selecting a node highlights the route to it from Start, and can dim everything outside its branch
- The generated graph and your hand edits are kept separately: edited rooms are marked, edits can be reverted to the generated version, and regenerating asks first when there are edits
- Locked nodes survive regeneration: the new map keeps a room of the same type at a similar depth on the same kind of route (main path or branch), with the same intensity
- The side panel checks the graph as you edit: exactly one Start and Exit, a Boss, the Exit reachable, every room reachable, and every room-type rule (counts, placement, depth, neighbours, connection limit) still met
