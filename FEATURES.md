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
- Dodge (C, or optionally a double-tap of Crouch while moving) — several dodge styles, one active at a time:
  - **Sidestep + roll** (default, inspired by God of War): a quick sidestep; press again within a short window (or during the step) to follow up with a full roll in the direction you're holding. Skipping the roll only costs the shorter sidestep cooldown
  - **Committed roll**: one longer roll that can't be steered once started
  - **Steerable boost**: a long burst you can curve toward where you're steering, and keep shooting during; usable once in the air
  - **Long dash**: a fast, long dash straight in the direction you're holding (no input, no dash)
  - **Air dash**: a short dash that holds your height, usable once per jump
- Rolls and dashes have brief invulnerability (i-frames) and lock out attacks and abilities until they finish; sidesteps and boosts don't
- Dodging with no movement input steps backward (or forward, for dashes)
- A dodge pressed just before it's ready (end of cooldown, landing) still comes out
- Being stunned or starting a mantle cancels a dodge
- The HUD dodge indicator names the current move (STEP, ROLL, BOOST, DASH), turns green while committed and refills during the cooldown
- Optional stamina — sprinting can drain a stamina meter and stop when it runs out, and dodges can cost stamina; once emptied, stamina has to recover past a threshold before it can be used again (off unless a stamina cost is set)
- Slide — press crouch while sprinting to slide; launches at a configurable speed then decelerates smoothly; exits when speed drops below a threshold, the timer runs out, crouch is released, or the player leaves the ground
- Mantle — hold forward (W) and press Jump facing a ledge to pull up onto it, from the ground or mid-jump (a jump that rises to a ledge still catches it for a moment); only ledges between a minimum and maximum height can be mantled — lower obstacles are simply jumped over, and there must be room to stand on top; the pull-up speed scales with ledge height (low ledges are quick, tall ones slower, tunable per height); works identically in first-person and third-person
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
  - ADS Zoom setting (Video → Camera): **Gradual** narrows the view along with the aim-in (Battlefield style); **Snap** keeps the view as is until the aim-in finishes, then snaps to the zoom (Modern Warfare style) and back out the moment you let go. Aim sensitivity follows the zoom; accuracy and aim-in time are the same either way
  - ADS Field of View setting (Video → Camera): **Independent** zooms every weapon to its own fixed FOV whatever your field of view is; **Affected** keeps each weapon's magnification relative to your field of view, so a wider FOV also aims wider (at the default 70° both are identical — e.g. a 45° sight becomes about 80° at a 110° FOV)
  - In third person: pulls the camera in closer and centers the shoulder offset
  - The held weapon smoothly raises from its hip position to a centred aim position and back
  - Reduces look sensitivity while aiming; all transitions are smooth
  - Movement slows to the ADS walk speed (a Player Movement Settings value) while aiming; it caps walking and sprinting but never speeds up crouching

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
- Stuns and slows from different sources combine sensibly: a new stun never cuts a longer one short, and the strongest slow applies — one slow wearing off doesn't cancel another
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
- Abilities can cost a resource (mana, energy...) or Surge as well as using a charge; an ability you can't afford doesn't fire and keeps its charge, and its slot is dimmed until you can
- **Surge** (uncommon): most abilities only use a cooldown, but a few unique, potentially strong ones have their own Surge gauge (0–100%, a thin bar under the ability's slot). It fills from combat actions — landing hits (counted at most four times a second, so fast guns don't outpace slow ones), kills and parries — at the ability's own rate (e.g. 10% or 25% per action), and starts empty each life. When the fighting stops it drains after a short delay, again per ability: some drain all the way to 0%, some keep a reserve (e.g. 30%), some never drain and can be banked for the next elite
- Surge can be used three ways, mixed per ability: a **Surge cost** (e.g. 15% per use, with or without a cooldown — some have none at all and can be used for as long as you keep fighting well); **Surge scaling** (the cast spends everything from its cost up to its maximum and hits harder the more it spends, with extra effects past set amounts — fire early for a modest hit or unload a full gauge on an elite); and **Surge charges** (each time the gauge fills it becomes a charge, up to the ability's maximum)
- Six built-in abilities: Dash, Projectile, Heal, Shockwave, Targeted, and Timeline
  - **Dash** — a quick committed dash in the move direction (or camera forward if idle), performed like a dodge (same steering, animation and air limit); it uses the ability's cooldown, not the dodge's
  - **Projectile** — fires a projectile from the camera that deals damage (and optional status effects) on impact; configurable speed, lifetime and gravity drop
  - **Heal** — instantly restores a set amount of health
  - **Shockwave** — damages nearby enemies and launches nearby rigidbodies away from the player
  - **Targeted** — damages and/or heals whoever its targeting rule picks: the enemy under the crosshair, everything in a cone, allies around the player, the nearest enemy, or an area where the player aims; it isn't used when it wouldn't affect anyone
  - **Timeline** — runs an ActionTimeline via TimelineAbilityRunner; only one timeline ability can play at a time; can be ground-targeted so its area effects land where the player aims
  - **Stat Buff** — applies a set of stat modifiers to the player for a while (included: Damage Boost, +30% damage for 8 seconds)
  - **Reflect** — puts up a reflect (see Reflects below)
- Included ready-made: **Cone Blast** (a targeted blast hitting enemies in front of you), **Ground Slam** (a timeline ability that smashes the area in front of you and knocks targets back) **Crushing Blow** (instant: 55 damage, 50% armour penetration, to the enemy under the crosshair up to 30 m — burst damage to stack with weapon hits), and four Surge abilities: **Arc Lash** (no cooldown; +10% per action, drains to 0%; 15% per use: 20 lightning damage to the enemy under the crosshair), **Overdrive** (25 s cooldown; +10% per action, never drains; needs a full gauge: +30% damage for 12 s), **Rupture** (6 s cooldown; +10% per action, drains to a 30% reserve; spends 20–100%: a crosshair strike from 25 damage up to 4× that, with bleed at 50%+) and **Static Rounds** (no cooldown; +25% per action, drains to 0%; each full gauge becomes a charge, up to 3, each 18 lightning damage to the enemy under the crosshair)
- All ability values (cooldown, force, damage, etc.) are tunable on the ScriptableObject asset

### Reflects
- A reflect catches incoming hits and turns their damage into something else. It can be put up by an ability or triggered by a melee parry
- What it catches: for how long, how many hits (one, or everything until it ends), from which directions (all around or only in front), and how much of each caught hit is stopped (all of it, or none like a vengeance that lets you take the hit)
- What the caught damage becomes — any combination of:
  - **back to sender**: dealt straight to the attacker, wherever they are
  - **a projectile**: fired where you aim (aim-based deflect), straight back at the attacker, or bounced off your facing like a mirror
  - **a beam or slash**: any action timeline, scaled to the caught damage
  - **healing** and/or a **temporary buff**
  - **status effects** and/or a **stun** on the attacker
- Reflects keep the hit's damage type (a reflected fire hit burns). They only catch hits with an attacker (not damage over time or hazards), work on the damage that gets past your guard (so a parried hit isn't caught twice), and reflected damage can never be reflected again
- Included abilities (all instant cast): **Vengeance** (the next hit within 30 s is taken in full, 75% of it goes back to the attacker), **Deflect** (for 0.6 s, hits from the front are negated and fired back as projectiles where you aim) and **Absorb** (for 1.5 s, hits are negated and half of them heals you)
- Melee weapons can give their parry a reflect: **swords** answer a parry with a **Riposte Slash**, a wide slash toward the attacker dealing 150% of the parried hit (set any category's *Parry Reflect* to give other weapons one)
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
- Ammo caches: a resupply crate with a limited number of uses (3 by default). Each use tops up the reserve ammo of every weapon you carry to three full magazines; a use is only spent if it actually adds ammo ("Ammo already topped up" otherwise). The ammo inside disappears when it's empty. Cargo bays and docking bays always have one along a wall, lobbies sometimes
- Resource nodes (placeholder): hold Interact to harvest a few times before it's spent. Which resource a node gives isn't decided yet, so the included node can be found and harvested but yields nothing. Parks have one or two
- Health pickups: `HealthPickup` restores a set amount of health
- Pickups only show a prompt when they would do something — no health pickup at full health, no ammo pickup without a weapon — so they aren't wasted
- Pickups (weapons, ammo, health, items) can be picked up even when hidden behind a wall or sunk into the floor, as long as they are within range and inside the camera view (anything off-screen is ignored); doors, switches and containers still need a clear line of sight
- Doors: press E on a `Door` to swing it open or closed (no animation — a plain procedural rotation)
- Locked doors — a door can require an item (e.g. a keycard); without it the prompt reads "Locked (Red Keycard)", with it the prompt reads "Unlock" and the door opens and stays unlocked; the key can be used up or kept
- Condition locks — a door can stay locked until several different things are done: "switch on both terminals", "raise the reactor and the cooling signals", "defeat the guard and pull the lever". The prompt shows progress ("Locked (Terminals 1/2)"), each step is announced ("Terminals 1/2"), and the door unlocks once enough have been done ("Terminals 2/2 — door unlocked"). Doing the same thing twice only counts once. Steps can be terminals, anything with an event (a lever, a boss's death) or quest signals
- Terminals: hold E for a moment to switch one on; its light turns from red to green and it counts toward its door
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
- **Included baseline items** — one of each kind to copy from: Combat Vest (torso armor rolling armor and bonus health), Extended Magazine (attachment: +25% magazine, 10% slower reload), Bandage (consumable), Scrap Metal (crafting resource)
- **Inventory** — holds counted stacks and unique items side by side, with no capacity limit
- **Starting contents** — the player can be set up to spawn with a list of stackable items (typically a munition stack per caliber they expect to use). Left empty, the player spawns with an empty pack and zero reserve ammo, so the first magazine is all they have until they find a pickup

## Procedural Weapon Generation
- Eight weapon categories: AR, SMG, Pistol, Sniper, LMG, Shotgun, Bow and Crossbow — each defined by a `WeaponCategoryData` ScriptableObject
- Every stat (damage, RPM, magazine size, spread, recoil, range, reload time, etc.) is defined as a min/max range with an optional bias value
- Bias < 0 skews the random result toward the minimum (e.g. SMG mags weighted toward 20–30 despite max being 50); bias > 0 skews toward the maximum
- Stat ranges reflect real-world and common game conventions per category:
  - **AR** — 600–850 RPM, 25–40 round mags (weighted 30–40), moderate recoil, 40–60 m effective range
  - **SMG** — 750–1100 RPM, 20–50 round mags (heavily weighted low, P90-style outlier), short range, erratic horizontal recoil
  - **Pistol** — 300–600 RPM semi/auto, 7–20 round mags (weighted low), high per-shot kick, 15–25 m range; wide damage spread (20–55) representing everything from Glock to Desert Eagle
  - **Sniper** — 30–80 RPM semi only, 5–10 rounds, 70–160 damage, 80–200 m optimal range, terrible hipfire, near-zero ADS spread
  - **LMG** — 600–950 RPM, 75–200 round belt/drum (weighted toward 75–120), slow reload (4.5–8 s), wide hipfire, high sustained recoil cap
  - **Shotgun** — 60–120 RPM semi/auto, 5–8 shell tube, 8–12 pellets per shot at 10–15 damage each, very short optimal range (8–15 m) with steep falloff, wide pellet cone (8–15° hip, 2–4° ADS), heavy per-shot recoil
  - **Bow** — hold to draw, release to loose an arrow that flies and drops with real physics. A short draw shoots slower, drops faster and hits softer; tapping without drawing wastes nothing. One arrow is nocked at a time (keep holding attack to nock the next), barely any recoil, and nearly silent — enemies hear it from only ~8 m. Band: shortbow (quick draw, light) → warbow (slow draw, heavy hitter)
    - **Multishot**: some bows loose 2–3 arrows at once (single-arrow bows are the most common). A volley costs one arrow; each arrow deals less than a single shot, so landing several pays off
    - **Vertical / horizontal draw** (Weapon Mode key, B): every bow has two draws. **Vertical** draws slower but hits harder (+15%) with a much tighter aim, and stacks a volley's arrows in a vertical line. **Horizontal** draws about a third faster but hits softer (−20%) and looser, and fans a volley sideways. Switching drops the current draw; each bow remembers its own
  - **Crossbow** — point and shoot: bolts hit harder and pierce more armour than arrows and fly flatter, but cranking the next bolt takes 1.5–3 s. Most are single-shot; rare repeating crossbows hold up to five bolts. Less commonly (about one in three), a crossbow has an **autoloader**: three-round burst or full-auto, fed from a 6–15 bolt magazine. Quiet (~10 m)
- **Modular Pistol** — the starting weapon: still a pistol, but the most versatile gun in the game, at lower damage than the specialists. Each one rolls two things independently on top of its stats:
  - **How it shoots**: semi-auto (most common), three-round **Burst**, full-auto **Machine** pistol (faster, lighter hits, bigger magazine), or **Scatter** — a pellet spread with short range, like a sawn-off shotgun (rarest)
  - **How it hits**: balanced (most common), **Heavy** (hits harder, fires slower), **Marksman** (DMR-style: hard hits, long range, tight cone, heavy kick, slow), **Rapid** (SMG-style: fast fire, lighter hits) or **Carbine** (rifle-style: a bit more damage and range, easier recoil, slower)
  - The two show in its name, e.g. *Marksman Burst Modular Pistol*; a plain one is just *Modular Pistol*. It always comes with the **Tactical Knife** perk (see Gear Perks)
- Any category can be given firing styles and stat styles like these (weighted), so shotguns could later roll burst or full-auto too
- Several stats (ADS bloom, recoil recovery fraction, recovery delay, ADS recoil multiplier, hipfire camera kick) are automatically derived from the category type and fire rate so the weapon feels correct without manual tuning
- Bows and crossbows both draw from the shared **Arrows** pool. Draw speed rolls with quality, so a better bow draws faster
- Create category assets via **Assets → Create → CGD → Weapon Category**, set the `Type` field, or duplicate an existing category of a similar type to start from its thresholds; values can be freely tweaked afterward
- Generated weapons now roll a quality score and tier, which decides where each stat lands inside its category range — an SMG still rolls SMG damage, just high or low within it
- Stats that carry a weapon's *power* (damage, fire rate, magazine, reload, recoil, spread, range, draw, sway) are driven by quality and the tradeoff axes. Stats that only give it *character* (recoil recovery, heat behaviour, burst timing) stay random, so high-tier weapons don't all start feeling the same
- Every generated weapon has a seed: the same category, tier and seed rebuild exactly the same weapon — name, fire mode, quality and every stat. A random weapon pickup can be given a fixed seed so it always offers the same gun

## Gear Perks
- On top of their stat rolls and attachment slots, generated weapons (guns and melee) and armor roll **perks**: fixed traits that are part of the item for its life and part of its seed (same definition, tier and seed → same perks)
- **How many**: by tier — Common 0, Uncommon 1, Rare 1, Epic 2, Legendary 2 — plus a 25% chance of one more. An item never rolls the same perk twice and only rolls perks that fit it (no reload perks on a sword, armor perks only on armor). Every weapon can parry with its quick melee, so parry perks (Riposte, Second Wind) roll on guns too. Weapons and armor each have their own perk pool
- Some gear always comes with a perk on top of what it rolls (the Modular Pistol's Tactical Knife)
- **Tactical Knife** (pistols and SMGs only — one-handed guns): quick melee becomes a fast offhand knife stab that hits much harder than a pistol whip, but holding the gun one-handed costs accuracy and recoil control (35% more spread, 30% more recoil). The knife parries like any bash
- **Passive perks** are always on:
  - On a **weapon** they change that weapon, like an attachment: magazine size, reload speed, multishot (a chance per shot to fire an extra one, or more), projectile velocity, crit chance and crit damage, status chance and status strength, armor penetration, range or melee reach, lifesteal, recoil, spread, draw speed, damage, fire rate / attack speed
  - On **armor** they change you while it's worn: move speed, ability cooldowns, health regeneration, lifesteal, status resistance, armor, health, shield, and weapon stats for every weapon (crit chance, reload speed)
- **Triggered perks** go off when something happens: a **kill**, a **hit**, a **critical hit**, a **parry**, a **dodge**, **casting an ability**, **taking damage**, **being healed**, **aiming down sights**, **starting a reload**, **finishing a reload** (the rounds going in), **using a consumable**, or **swapping weapons**
  - A weapon's triggered perks only answer while it's in hand, and its kill/hit/crit perks only for hits it dealt (grenades, abilities and an arrow still flying from a weapon you swapped away from don't count). "Swapping weapons" on a weapon means swapping *to* it
  - Armor's triggered perks answer to everything, whatever weapon you hold or whatever dealt the hit
  - Each can have a chance to go off, and a cooldown. A perk with nothing to do (a full magazine, full health) doesn't go off and doesn't use its cooldown. Cooldowns are per perk, so two items with the same perk can't be swapped between to trigger it twice
  - Perks can't set each other off: a heal from a kill perk doesn't fire "being healed" perks, so they can never loop
- **Buffs**: a weapon's buffs end when you swap away from it, so a perk never powers up the next weapon of a combo; armor's buffs run their full time. Triggering a buff again restarts its timer rather than stacking
- **Crits**: crit chance gives any hit a chance to count as a critical hit wherever it lands (a body shot hits like a headshot; a headshot doesn't double up). Crit damage raises the critical multiplier
- **Status**: status chance makes a weapon's on-hit effects likelier to apply; status strength makes them hit harder; status resistance makes them less likely to take hold on you
- **Included weapon perks** (placeholders to tune):
  - Triggered: Evasive Reload, Slip Feed (dodge reloads); Kill Feed, Quick Hands, Adrenaline, Reap (kills); Riposte, Second Wind (parries); Dancer (dodge, melee); Eagle Eye (aim → crit chance); Fresh Mag (reload done → damage); Precision Feed (crits refund a round); Arcane Loader (ability → full reload); Quick Switch (swapped to → fire rate); Leech (melee hits → stamina); Threat Response (hurt → faster reloads); Run and Gun (reload start → move speed); Stim Loader (consumable → half a magazine)
  - Passive: Extended Mag, Speed Loader, Split Shot (multishot), Hot Loads (velocity), Keen (crit chance), Deadeye (crit damage), Infectious (status chance), Virulent (status strength), Piercing, Long Barrel, Long Reach, Siphon (lifesteal), Stabilized, Choked, Quickdraw, Heavy Hitter, Rapid
- **Included armor perks** (rolled by the Combat Vest):
  - Triggered: Reactive Plating (hurt → armor), Battle Focus (ability → damage), Evasion (dodge → speed), Adrenal Gland (healed → damage), Scavenger (kill → loads the held gun), Stim Rush (consumable → speed), Bloodthirst (kill → heal), Counterguard (parry → armor), Tactical Rhythm (reload done → stamina), Opportunist (crit → crit damage), Steady Breath (aim → less recoil), Quick Holster (swap → speed)
  - Passive: Fleet, Focused (cooldowns), Regenerator, Vampiric, Warded, Bulwark, Vital, Shield Emitter, Marksman, Deft Hands
- A short notification names each triggered perk as it goes off. Perks are listed under each worn item and weapon in the Character window, after the name for pack armor and pickups, and in shop listings
- Buffs and debuffs on the player reach every weapon stat above, plus move speed, ability cooldowns, health regeneration, shield capacity and lifesteal. Not yet wired to anything: sway, ammo reserve and shield regen rate

## Weapon Loadout
- Four loadout slots on the player; press 1, 2, 3, or 4 to equip that slot. A slot is a **main** weapon plus an **offhand** (a shield, artifact or one-handed melee weapon): switching slots swaps both, so *sword + shield → shotgun → pistol + mirror* is one key press each
- **One item can fill several slots**: the same shield behind a sword in slot 1 and a pistol in slot 3, or the same pistol with a knife in one slot and a shield in another. Slots just point at carried items; an item leaves the pack when first slotted and returns when no slot uses it, and picking up a weapon never throws out one another slot still uses. Moving between two slots that share a main doesn't redraw it — only the offhand changes
- Set slots in the Character window (Tab): click a weapon, shield or artifact (in the pack or already in a slot), then click a slot's **Main** or **Offhand** line; click an offhand to empty it. A slot shows as e.g. *Glock 17 (+Tactical Knife)* in the HUD, the inventory panel and the weapon wheel; a slot with only an offhand uses fists
- The weapon wheel (hold a slot key) puts a spare weapon from the pack in that slot's main place, or swaps the whole slot with another
- The scroll wheel cycles through the filled slots (down = next, up = previous), skipping empty ones and wrapping around
- **Last Weapon** swaps back to the slot used before the current one, and pressing it again swaps back — a quick toggle between two loadouts. It follows the slot if the wheel swaps it elsewhere, and does nothing once that slot is empty. Unbound by default; set a key in Settings → Controls
- Switching to a slot with a different main equips it and cancels any reload in progress
- **Reload cancels**: a reload loads its rounds at a commit point (80% of the way through by default, per weapon) — the ammo counter refilling is the cue — and the rest is just the end of the animation. Anything that interrupts you cancels the reload — breaking into a sprint, dodging, swapping away, using an ability or consumable, or losing control (stunned, mantling): before the commit point the reload is lost and has to be started again; after it the rounds are already in and the rest is skipped. Swapping away after the commit point and back within 1 s brings the weapon back without a draw, so a well-timed sprint, dodge or swap-back is slightly faster than letting the reload finish. Abilities and consumables follow the same rule, but the weapon still waits for them to finish, so they mostly save time rather than getting a shot off sooner
- Each weapon keeps its own loaded magazine; reserve ammo is a shared pool per `AmmoType` — two SMGs share the same LightRounds supply, an SMG + sniper diversify across two pools, a hand cannon + sniper both compete for scarce HeavyRounds
- Inventory panel (press I) shows all four loadout slots, highlights the active weapon, and lists each weapon's name; empty slots are shown as "— Empty —"
- Item inventory panel (also press I, sits to the right of the loadout) shows a flat list of everything carried — stackable items aggregated by definition (so all Light Rounds show as one line with a total count rather than one line per internal 999-cap stack) plus each unique gear piece; a header shows slots-used and total weight against per-player caps that turn orange when exceeded
- Starting weapons are configurable in the Inspector via WeaponData ScriptableObject assets
- A slot can hold a melee weapon instead of a gun; it is equipped, swapped, picked up, bought, packed and dropped exactly like a firearm (melee weapons have no ammo, so ammo caches and revives skip them)

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
- Default keys (keyboard plus left/right mouse and the scroll wheel — no extra mouse buttons, since not every mouse has them): Jump (Space), Sprint (Shift), Crouch (Ctrl), Dodge (C), Attack (left mouse), Aim (right mouse), Reload (R), Interact (E), Melee (V), Abilities 1–4 (Q, F, G, X), Weapon slots 1–4 (1–4), Next / Previous Weapon (scroll down / up), Last Weapon (unbound), Item slots 1–4 (5–8), Shoulder Swap (H), Weapon Mode (B — bow draw), Lock-on (T), Switch View (F5), Inventory (I), Character (Tab), Map (M), Pause (Esc), Console (`)
- Hold any weapon key (1–4) or item key (5–8) to open a selection wheel for that slot: move the mouse (or stick) toward a choice and release to pick it; release in the middle to cancel. The camera holds still while the wheel is open
- Move and Look are built the same way as every other action (WASD/arrows + gamepad left stick for Move, mouse delta + gamepad right stick for Look) — one input pipeline for the whole game instead of a separate non-remappable asset just for movement

## Settings Menu
- Built with the same runtime UGUI/TextMeshPro system as the rest of the HUD (not legacy IMGUI) — consistent styling and resolution scaling with everything else
- Press Escape at any time to open or close the settings menu
- Mouse and gamepad sensitivity sliders with live preview; changes are applied and saved on confirmation
- Full keybinding editor — every action shows its primary and secondary slot; click a slot then press any key, mouse, or gamepad button to rebind it
- While a slot is listening, tapping Escape cancels and leaves the binding as it was; holding Escape clears the slot so it is unbound
- Duplicate binding warning — the same key, mouse button or gamepad button can be bound to several actions; when that happens a brief orange notice names the other actions, and every slot sharing a control stays highlighted orange in the keybinding list until one of them is changed
- Reset to Defaults button restores all keybindings to their original values
- The menu has four tabs: Controls (sensitivity and keybindings), Audio, Video and Accessibility. Reset Defaults resets only the tab you're on
- **Audio** — Master, Effects, Music and Interface volume sliders, heard straight away
- **Video** — resolution, window mode (fullscreen, borderless, windowed), quality level, VSync, frame rate limit (when VSync is off), field of view (60–110°), ADS zoom (gradual or snap) and ADS field of view (independent or affected). Sprinting still widens the view on top of the chosen FOV. Resolution and window mode apply in builds, not in the editor's Game view
- **Accessibility** — camera shake & kicks strength (0% turns off shake, weapon kicks and FOV punches), screen flash strength (hit flash and feedback flashes), and the low-health red vignette on or off. Hold vs. toggle is set per action on the Controls tab
- **Input Buffering** (Accessibility tab, off by default): a press made while you can't act on it yet is remembered for 0.4 s and happens as soon as you can, like queued inputs in MOBAs. Covers firing while a gun is drawing, reloading, between shots or during a dodge (so *swap, roll, fire* shoots the moment the roll ends), melee swings and bashes tapped during a dodge or stun (or a bash pressed while a swing or bash is still playing), abilities pressed while another ability is casting or going off, during a dodge or while stunned (so abilities 1→2 in quick succession both fire, even on the same frame), and consumables pressed while another is being used, on cooldown or during a dodge. A press is never carried over to a different weapon; raising the guard drops a queued swing
- Audio, video and accessibility changes preview live and are saved when the menu closes
- All settings are saved to disk and automatically restored on next launch, applied before the first scene appears

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
- **The Melee key (V) is the other hand.** What it does depends on the offhand:
  - **An offhand weapon** (a one-handed dagger, sword or axe): tap V for its light combo, hold V for its heavy attack. Its **opening strike parries** like a bash; the rest of the string doesn't. Aiming stays on the right mouse, so a pistol keeps its sights
  - **A shield**: V is a **shield bash** that parries
  - **Nothing**: V bashes with the weapon in hand (below)
  - Holding something in the offhand costs the main gun some accuracy and recoil control: a dagger a little (+15% spread and recoil), a sword more (+25%), an axe most (+35%); shields from +10% (buckler) to +35% (tower shield). An offhand item replaces a gun's Tactical Knife
- **Quick melee** (Melee key, V, with an empty offhand) is a **bash** with whatever is in hand: a pistol whip with a pistol, a rifle butt with the other guns, a guard bash with a melee weapon, a punch with empty hands. A gun with the Tactical Knife perk stabs with an offhand knife instead
- **Parrying is done with the bash**, with every weapon: a hit from in front landing within the parry window from the start of a bash (0.2 s for gun bashes and fists, per weapon for melee weapons) is deflected completely — no damage, no on-hit effects. Parries set off parry perks
  - **Up close** (within ~4 m) a parry **staggers** the attacker briefly, and the weapon's parry reflect answers it (a sword's riposte)
  - **A parried shot** from further away is **deflected**: fired back where you aim, with the shot's damage
  - **Parrying too early is punished**: if the parry window closes with nothing parried, you're **exposed** for half a second — you can't guard or bash again, so whatever comes next lands. A bash is committed too: it can't be cancelled into another bash, so parrying is a timing gamble, not something to spam
- A bash doesn't stop the gun from firing, and kills with a gun's bash count for that gun's perks
- With a melee weapon: tap Attack for a light attack; hold it past a configurable threshold before releasing for a heavier finisher instead
- Light attacks chain into a combo string — pressing again while the current attack is swinging or recovering queues the next step, which fires the instant the current one finishes; the string resets back to the first step after a short period of no input
- Damage resolves through the same Effective Damage pipeline as guns and abilities (armor, penetration, damage type all apply)
- Hit registration runs continuously during the Active window (every physics tick), not a single-frame check — moving targets are caught mid-swing
- Three hit shapes per attack step:
  - **Thrust** — single SphereCast forward (stabs, pokes); resolves hitbox regions so headshots and limb hits apply their multipliers
  - **Sweep** — fan of SphereCasts across a configurable horizontal arc (slashes, backhands); each ray resolves hitbox regions independently; ray count and arc width are tunable per step
  - **Slam** — OverlapSphere at the impact point (overhead smashes, ground pounds); area damage with no region resolution, hits everything in the radius once
- Each target is damaged at most once per swing regardless of how many ticks or rays touch it
- Per-step critical multiplier — applied when a Thrust or Sweep hits a critical region (head by default); tunable per combo step so a heavy finisher can crit harder than a quick jab
- Debug drawing shows the cast rays (Thrust/Sweep) or overlap sphere (Slam) each physics tick during the Active window
- When a MeleeAttackStep has an ActionTimeline assigned, the Active phase is driven by the timeline system instead of the legacy hit resolver — the timeline controls what shapes fire on which frames, while Windup and Recovery remain time-based

### Melee Weapons
- Five melee weapon types, rolled like guns (quality, tier, seed → the same weapon every time): **Dagger**, **Sword**, **Axe**, **Hammer** and **Spear**
- Found like guns: as (rarer) loot drops, and on the Gunsmith's shelf (swords, axes and daggers)
- With a melee weapon in the active slot, **Attack** (left mouse) swings it: tap for the light combo, hold for the heavy attack. No ammo, reload or recoil
- Each type has its own combo and feel:
  - **Dagger** — very fast stab-slash-stab, short reach, a long parry window but a flimsy block
  - **Sword** — two wide slashes into a thrust, a broad sweeping heavy; the all-rounder
  - **Axe** — slower chop into an overhead slam, bites through armour
  - **Hammer** — slow area slams that crush armour; the sturdiest block, the tightest parry
  - **Spear** — long-reach thrusts and a wide sweep; a narrow guard
- Rolled stats: damage, **attack speed** (how fast every swing winds up, strikes and recovers) and armour penetration follow quality; reach, stamina cost and guard strength vary per weapon
- Swings cost **stamina** (heavy attacks cost double), shown as a bar above the health panel; a swing that can't be paid doesn't start. Stamina refills after a short pause. (A character without a stamina meter swings for free)
- **Block** (Aim, right mouse, between swings or in a swing's cancel window): raising the guard slows you to aiming speed and stops most of the damage from hits in front of you; what it stops costs stamina, and running out **breaks the guard** (the hit lands in full). Hits from behind or the side, and damage over time, ignore the guard
- **Shields** are the blockers: with one held, the right mouse raises the **shield** instead of the weapon's guard. It lets far less through (about 3–30% depending on the shield and its roll), costs little stamina per blocked hit, covers a wide arc, and blocks gunfire as well as blows. A melee weapon's own guard is for parrying; it blocks worse
  - **With a gun**, raising the shield replaces aiming down sights (no zoom, hip-fire accuracy); the gun can still fire from behind it, and V bashes
  - **Buckler**: small, light on the gun, blocks least, longest parry window. **Riot Shield**: the widest arc and the strongest block. **Tower Shield**: blocks hits from in front **on its own** without being raised — so a gun can still aim — but every blocked hit costs stamina, it weighs most on the gun, and it stops covering you for a moment after a missed parry
  - Shields roll a tier like other gear: better ones let less through and bash harder. Found as loot (rarer than guns)
- **Guard bash**: Attack while the guard is up throws a block-breaking bash that parries (see Quick melee above); the Melee key does the same bash in one press, without raising the guard first. Bashing costs a light attack's stamina. Raising the guard on its own only blocks; it no longer parries
- **Parry** (bash timing, per weapon): daggers have the longest window, hammers the tightest
- With a melee weapon equipped the right mouse never zooms the camera
- **Cancel windows**: once a swing has struck, the later part of its recovery can be cut short by raising the guard, bashing or dodging. The hit has already landed, and the combo carries on: slash, guard (or bash-parry), then the next press continues with the second step, as long as it comes within the combo's reset time. Combo steps can be cancelled early in their recovery; finishers and heavy attacks only near the end, so committing to them still has a cost
- Dodging is never blocked by a swing. Dodging before a swing's cancel window wastes that swing (its strike doesn't happen if it hadn't yet) and restarts the combo
- **Combo weaving**: a dodge, a parry, an ability, a consumable or switching weapons between combo steps keeps the next step waiting for the weapon's **weave window** (2–3 s; daggers longest, hammers shortest) instead of the short idle reset, counted from when the dodge ends. Doing nothing still resets the combo quickly. Weaving only holds the combo open; the buildup comes from the combo steps themselves (later steps hit harder or carry their own effects)
- Abilities fired in a swing's cancel window cut its recovery short like a dodge; fired earlier, the swing simply carries on
- **Swap-dodge cancel**: swapping to a gun and then dodging skips the rest of its draw, so the gun is ready as soon as the dodge ends. Order matters: swapping while a dodge is already moving draws in full, so swap first, then roll. Melee weapons need no draw and come out instantly. Together with combo weaving this lets combos like *sword, sword → swap, roll → shotgun blast → swap → sword* land as one burst
- **Combos survive weapon switches**: each melee weapon remembers its own place in its combo. Switch to a gun, shoot, and switch back within the weave window to continue with the next step (switching away before a swing's cancel window drops that combo). Heavy attacks always restart the combo

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
- Grenades are items: they're looted, bought, crafted and picked up like anything else, carried in the pack (Frag ×3, Smoke ×2 per stack) and put on an item slot (5–8)
- Pressing a grenade's item key puts it in your hand (the gun is put away); hold the attack button to aim — a predicted trajectory arc is drawn from the throw point, accounting for gravity, and stops early at the first surface it would hit
- Release to throw; the grenade flies with real physics (gravity, bouncing) and detonates after a fixed fuse time. Your weapon comes back afterwards
- Aim, a weapon key, or the same item key again puts the grenade away without throwing
- Explosion deals damage in a radius, falling off linearly with distance from the blast center, through the same Effective Damage pipeline as everything else
- Included: Frag Grenade and Smoke Grenade (a smoke cloud that hides you and blocks sight lines)

## Enemy AI
- AI states — Patrol, Alert, Chase, Stunned, Dead — run on the shared state machine; Patrol, Alert and Chase each decide where to go next, a stun interrupts any of them and hands back to whatever was interrupted, and death is final until the enemy respawns
- Enemies find targets by team: any character on an opposing team can be detected, so no player reference needs wiring; teamless props such as breakable crates are ignored
- Enemies can be spawned from the object pool and reused: a reused enemy comes back at full health and starts patrolling again
- **Enemy tiers**: every faction has three tiers of regular enemies. Tier 1 is the everyday roster (~100 effective health); tiers 2 and 3 are tougher (about 2.5× and 5× the effective health, harder hits) and a little bigger. For now tiers 2–3 are stat-only placeholders built on their faction's tier 1; their own looks and behaviour come later
- How often higher tiers turn up depends on the room's tier (see Map Graph Generation): tier-1 rooms are almost all tier 1 enemies (10% tier 2), tier-2 rooms mix in tier 2s (45%) and a few tier 3s (10%), and tier-3 rooms are mostly tier 2 and 3 (45% each). Encounter waves (Holdout, Rift, Lockdown) follow the same odds
- **Random affixes**: tier 2 enemies roll one random affix and tier 3 two (tier 1 none), named in front of them ("Brutal Warded Void Tier 3"): **Armored** (+40 armour), **Warded** (+40 shield, even on factions that normally have none), **Vital** (+60% health), **Brutal** (+40% damage) and **Swift** (+30% speed). Each spawn rolls afresh
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
- State color indicator: mesh tints grey (patrol), yellow (alert), red (chase), blue (stunned), dark (dead) via MaterialPropertyBlock — no material instances created
- World-space health bar appears above the enemy on damage and fades out after a configurable delay; billboards toward the camera
- All parameters (health, speeds, sight, hearing, combat type, attack, ranged stats, alert duration) are tunable per enemy type via an EnemyData ScriptableObject
- **Tier-1 enemy per reality** (placeholder capsules), each holding about 100 effective health — the raw damage it takes to kill one — spread the way its reality defends (GDD): armour cuts every hit before shields and health, so armour multiplies how long the rest lasts
  - **Maintenance Bot** (TECH, Service tier): a white service chassis with a lit waist band and antenna. Armour first — 50 armour over 37 shield and 30 health. Slow but hard to crack; its shield comes back after 4 seconds
  - **Scavenger** (BIO, Prey tier): a low, hunched creature with a mossy back and glowing pods. Health first — 80 health under 25 natural armour, no shield. Fast and quick to strike but light-hitting, with wide vision and keen hearing
  - **Husk** (VOID, Affected tier): a near-black hollow figure with frost at its feet and a shard hanging over it. Wards first — 55 shield over 45 health, no armour; its ward recovers after 3 seconds. Slow, short-sighted, hits hard
  - Each faction's rooms in a generated level are fought over by its enemy; Breach rooms mix two. Headshots land on a separate head hitbox, a coloured visor shows the AI state, and enemies show on the minimap and map once their area is explored

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
- Loot rolls are seedable: rolling a table from the same seed gives the same drops, and every weapon or piece of gear in them rolls the same stats

## Game Flow
- The game moves between clear states: boot, main menu, loading, playing, paused, level transition, game over and victory; impossible moves (pausing from the main menu, winning while loading) are refused
- Pausing freezes gameplay time and world audio; the settings menu doubles as the pause menu and can't be opened during game over or loading
- Level loading runs in the background with progress for a loading screen, preceded by a short transition for fade-outs or results
- Restart level, return to main menu, start game and quit are available to UI buttons
- The player's death can end the run with a game-over state instead of respawning (per-scene option)
- UI panels can be shown only in certain states (pause panel, game-over screen, loading screen) without code
- The cursor is captured during play and released in menus automatically

## Expeditions (Core Loop)
- A run follows the GDD loop: start in the safe starting room, explore and gather, craft, push to the boss and extract — or die trying
- The starting room is a hub: no enemies spawn there, and a **Hub Workbench** (all crafting recipes) stands beside the spawn spot, so you can come back to craft between pushes
- Every run starts with a freshly rolled **Modular Pistol** (its firing style and stats differ each run) and nothing else in the weapon slots, plus random gear — sometimes a Combat Vest — ammunition, bandages, sometimes a combat stim, a frag grenade or two and some scrap metal and cloth. The loadout comes from the level's seed, so a kept seed keeps its loadout
- The Exit room's pad is the **extraction point** (hold Interact on "Extract"): everything you carry — the pack, your weapons and worn armour — goes into the ship's hold
- **Dying loses everything** you carried: what you brought from the ship and what you found. The ship's hold itself is never at risk
- After every run the ship screen shows how it went (EXTRACTED, or KILLED IN ACTION with what was lost), the ship's hold, and the kit for the next run: click an entry to pack it or put it back. **Deploy** starts a fresh run with the packed kit on top of the new starting loadout — armour is worn straight away when its slot is free, weapons fill free slots
- Leaving a level mid-run (restart or main menu) counts as not making it out
- The ship's hold lasts for the play session (it is not saved to disk yet)
- An **Emergency Exit** room's escape pod is an early way out (hold Interact on "Emergency extract"): the run ends, your weapons and worn armour make it home, but only half of every stack in your pack fits and loose gear in the pack is left behind. The ship screen reads EMERGENCY EXTRACTION and says how much didn't fit

## Encounter Rooms
Rooms with rules of their own, mixed into generated maps alongside the usual fights, shops and treasure. They are the exception: in normal rooms (Combat, Breach and the rest) the doors never lock and the enemies simply patrol. Each one says what it is on its door sign (except the Ambush), and the map's validity checks still hold for all of them.
- **Lockdown** — step inside and blast shutters slam down over every doorway. They only open once every enemy in the room is dead, and a reward cache drops. Enemies that wandered out before the doors closed are shut outside and don't count; an empty room calls in a wave instead
- **Holdout** — an uplink terminal. Starting it (hold Interact) seals the room and enemies pour in every ~18 seconds while it uploads (75 s, with a countdown). When it finishes the shutters open, a reward drops, and the whole ship map is downloaded to your world map
- **Puzzle** — the doors leading deeper are locked blue bulkheads until the room's calibration ring is solved: 4–5 consoles, each flipping itself and its two neighbours (the prompt says which), and every light must be green. The way back stays open, so you can always leave and return. A puzzle room with nowhere deeper to go drops a reward instead
- **Ambush** — looks exactly like a Treasure room, down to the door sign and the cache in the middle. Open the cache and the doors seal while two waves drop in. The loot is real
- **Stealth** — a guarded vault room. While no guard has spotted you, the vault holds extra loot. The first guard to give chase raises the alarm: the room seals, the bonus is gone, and the guards plus reinforcements must be beaten before the shutters open
- **Rift** — a tear between the room's two realities around a glowing core. The room seals and waves come through from one reality, then the other, the last from both at once; when the last falls the rift collapses and a reward drops
- **Gamble** — two machines: one takes credits, the other takes blood (a share of your health, never enough to kill you). Each pull busts, wins a normal drop or hits the jackpot (a drop with much better luck); every pull costs more than the last, and each machine runs dry after three
- **Hazard** — a leak (fire, toxin, coolant or live wiring) that keeps afflicting everyone on the room's glowing floor — you and the enemies alike — until you find and switch on the vent controls somewhere in the room
- **Emergency Exit** — an escape pod off the main path (see Expeditions)
- **Quiet** — once in a while (at most two per map, never next to each other) a room is simply peaceful: no enemies at all, and always something worth stopping for — a salvage node (scrap metal), an ammo cache or a loot cache
- Lockdown, Holdout and Rift rooms carry the red danger marker on their door signs; a sealed room announces itself, and every wave and countdown is called out in the HUD feed

## Map Objectives
- Every generated level hands out **main objectives** (1–2) and **side objectives** (0–3), rolled from objective templates with the level's seed — the same seed always gives the same objectives in the same rooms
- An objective is 1–3 steps, each in a specific room of the map: **Clear** it (kill every enemy there), **Activate** a console placed in it, or **Retrieve** a Data Core placed in it. Steps can go in order (the next one appears when the last is done) or all at once
- Each step asks for a **range** of room tiers, so every map rolls its own combination — a three-step objective can come out 1-1-3, 1-2-2, 2-2-3, 3-1-2… A template can also say how the tiers relate: **any**, **never going down** (each step at least as tough as the last), or **all the same tier** (1-1-1 or 3-3-3). Fixed shapes work too: a strict climb 1 → 2 → 3, all in tier-1 rooms, all in tier-3 rooms, or all in rooms of **one reality**
- **Main objectives** lock the Boss room: its doors show "Objectives 0/2" and open once every main objective is done
- **Side objectives** are optional and pay out when done: credits (25 / 50 / 100 by the highest room tier involved) and a reward cache in the last step's room, with better odds the higher the tier — a tier-3 side objective is the risky, rich one
- Objectives show in the quest tracker ("Main: …", "Side: …") and each active step's room is marked on the world map and minimap (even unexplored, pinned to the edge when out of view)
- Only rooms reachable before the Boss are used (no secret rooms, Start, Exit or Shop), no room hosts two steps, and objectives that don't fit a map are skipped. Rooms already cleared before a Clear step unlocks count straight away
- Included templates — main: Escalation (clear tier 1 → 2 → 3), Override Sequence (three consoles anywhere, any order), Secure the Route (clear a tier 2 then a tier 3); either: Data Trail (console tier 1 → data core tier 2 → console tier 3); Mixed Ops (clear, console, data core, each no easier than the last — 1-1-3, 1-2-2…); side: Salvage Run (two data cores in tier-1 rooms), Deep Recovery (a data core in a tier-3 room), Purge (two tier-3 rooms of one reality), Faction Sweep (three rooms of one reality), Supply Line (two data cores in tier 1–2 rooms, then a console in a tougher tier 2–3 room), Wildcard (three rooms of one tier, whichever the map rolls)

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
- Each SoundBank follows one of the settings menu's volume sliders (Effects, Music or Interface), on top of Master

## Death & Respawn
- When health reaches zero the player loses control, the HUD hides, and a death screen is shown
- Player automatically respawns after a short delay, returning to the designated spawn point
- On respawn, health is restored and every carried weapon's magazine is refilled; reserve ammo in the shared inventory pool is not touched. Where a scene ends the run on death instead (the generated levels), everything carried is lost — see Expeditions. Ability cooldowns reset, and leftover momentum and status effects are cleared

## Map Graph Generation
- Maps start as a pure experience graph — rooms and connections without geometry; a [generated level](#generated-levels) turns one into a playable level. Each node is what the player meets there (Start, Combat, Puzzle, Shop, Event, Treasure, Resupply, Breach, Boss, Exit, and the [encounter rooms](#encounter-rooms) Lockdown, Holdout, Ambush, Stealth, Rift, Gamble, Emergency Exit and Hazard, plus the occasional peaceful Quiet room), and connections say how they link: normal, shortcut, secret or locked — and any open passage can be one-way
- One generator covers many styles of map: a near-straight corridor, a branching spine, a hub around the Start room, a winding labyrinth, or anything in between
- A map style is made of two independent parts that mix freely: a **layout** (the map's shape) and **content** (what fills it). The same Treasure Hunt content works on a hub or a labyrinth, and the same labyrinth can be a treasure hunt or a gauntlet
- A style can list several layouts with weights, so its shape changes from run to run (a hub one time, a labyrinth the next) while what fills it stays the same
- Every map has a main path from Start to the Boss and Exit, which can run straight or wind. On top of the main path, each layout adds a set number of optional rooms off it: short side rooms or long wings, straight or twisting, some forking off other branches into deeper areas, some starting from the Start room to make it a hub, and some looping back into a neighbouring room instead of dead-ending. Every map gets its optional rooms however long its main path turns out
- Loops link neighbouring rooms into a web, and shortcuts let the player jump from a shallow room to a deeper one nearby. The Boss always keeps a single entrance and is never closer to Start than the set minimum, whichever route is taken
- How compact a map is can be limited: tightly packed maps tangle into more loops, unbounded ones sprawl
- Optional areas with only one way in can get a locked or secret entrance; since there's no other way in, a gate can't be walked around. Gates can be kept away from the start of the map, so the first few rooms are always open
- Every locked door has a key, placed in a room the player can reach without opening any door or finding any secret, so a map can never lock the player out. Keys sit no deeper into the map than the door they open, and prefer side rooms off the main path so getting into a locked area means exploring first
- Locks come in two kinds: a **keycard** (one key in one room) or **terminals** (2–3 terminals, each in a different room, all of which must be switched on). Terminals follow the same rules as keys, so they're always reachable before their door, and no two locks share a room. How often a lock uses terminals, and how many, is set per layout
- Guaranteed rooms are a list, each a room type, where it goes and how many: a reward in every locked or secret area, at its far end, so finding the key or the hidden wall always pays off (a Treasure by default); a room right before the Boss as a last stop (for example a Shop); or a number of rooms within a depth band from Start (for example an early Treasure 2–3 rooms in, or two Events within the first four rooms)
- **Room tiers**: every room is tier 1, 2 or 3 by how deep it lies between Start and the Boss, with a little randomness — rooms near Start are tier 1, the middle of the map turns tier 2 and the stretch before the Boss is mostly tier 3, so the map gets harder as you push on. Depth is a guideline, not a rule: now and then (15%) a room lands a tier above or below what its depth suggests, so a tough room can turn up early or a calm one late. Rooms right next to Start are always tier 1, so every map eases in (Start, Exit, Boss and Shop rooms stay tier 1; harder-run modifiers push tiers up). A room's tier decides how often its enemies are tier 2 or 3, gives its chests, caches and encounter rewards slightly better odds (+0.5 / +1 loot luck at tier 2 / 3), and makes its resource nodes yield more — and a rarer resource where one is set. Tier 3 replaces the old Elite rooms; a hand-made map that still has an Elite room treats it as a tier-3 fight. Tiers show in the map editor and can be set by hand
- Pacing along the main path: never more than a set number of fights in a row (3 by default), whatever their tier — tier-3 fights can come back to back. When no calm room type fits, a rest room (an Event by default) is used, and the rare case where that goes over its limit is reported
- Room types can keep their distance: a minimum number of connections between two rooms of the same type (Treasure rooms are at least three apart), and types that want space (Combat) prefer rooms with open space beside them, so big arenas like cargo bays can stretch out
- Resupply rooms: calm stops on the main path, midway through the run, with an ammo cache
- Breach rooms: fights where two realities overlap — the room belongs to one faction, a neighbouring one bleeds in, and enemies from both rosters fill it. They count as fights for pacing and come no earlier than a third of the way in
- One-way shortcuts: some shortcuts are barred from the near side and can only be opened from their deep end, Dark Souls style. They're no way ahead, but once found from the far side they're a permanent quick route back toward Start — after a death, straight back to where you were
- Ship sections: the run passes through Habitation, then Commerce, then Engineering, from Start to the Boss. Each room belongs to the section of its stretch of the run, so crew quarters and lobbies cluster in Habitation, restaurants, casinos and parks in Commerce, and cargo bays, docking bays, the reactor and supply depots in Engineering
- Run modifiers come in two kinds. **Warnings** make a run harder and pay for it with luckier loot in every chest and cache of the level: Lockdown (more locked doors and gates), Infestation (one faction overruns most of the ship, a little tougher) and Overrun (more fights, tougher overall and one extra tier-3 room). **Anomalies** are twists that change how a run plays without making it harder: Scavenger (more Treasure, fewer Shops) and Fractured Hull (extra rooms, more loops and more secret passages). A map gets at most two warnings and one anomaly. They're announced when the level starts (warnings in warning colours), and a modifier is part of the seed, so the same seed always gets the same twist
- Ready-made layouts: Linear, Branching, Hub and Labyrinth. Ready-made content: Standard (a balanced mix), Treasure Hunt (more Treasure and Puzzle rooms, an early Treasure, a gentler difficulty curve) and Gauntlet (mostly fights, little loot, a Shop right before the Boss, a steeper difficulty curve). All of them use the TECH, BIO and VOID factions (Faction assets with a name, colour and enemy roster)
- Ready-made styles: `LinearMapGenerationSettings`, `BranchingMapGenerationSettings`, `HubMapGenerationSettings` and `LabyrinthMapGenerationSettings` (each layout with Standard content), and `RandomMapGenerationSettings`, which picks Branching or Hub most often and Labyrinth or Linear now and then. Each run has a chance of one run modifier
- The same seed and settings always give the same map, so a good map can be kept by its seed
- Each generation layer — run modifiers, layout, room types, intensity, factions — can be rerolled on its own from the editor: reroll the room types and the layout stays exactly the same; reroll factions and types and intensity stay the same. Rerolling the layout can also switch a multi-layout style to another of its shapes. A new seed starts every layer fresh
- A layout sets the number of optional rooms, minimum boss depth, how far the map may spread, a connection limit per room, main path length and winding, branch length, winding, fork, hub and rejoin chances, how many loops and shortcuts (and how far a shortcut reaches and skips), gate chances, how many gates and how deep the first may be, and where keys go
- Content sets, for each room type, a minimum and maximum count, a weight, where it can go (anywhere, main path only, branches only), how deep into the run it can appear, whether two of the same type can sit next to each other, how far apart two of them must be, whether it wants space around it, and whether it prefers dead ends (for example, treasure as a reward for exploring); plus the guaranteed rooms, the pacing, the ship sections, the difficulty curve and the factions
- Room types with the tightest placement rules are placed first, so broad types like Combat can't take the only rooms a Treasure could use. Guaranteed rooms count toward their type's limits. Constraints that can't be met are reported instead of failing silently
- Each room gets an intensity from a difficulty curve over the run (rising, a breather before the boss, then the boss at full intensity), adjusted per room type with a little variation
- The colony ship has been warped by an anomaly into three realities — the TECH, BIO and VOID factions — and it splits the ship between them differently every run. Each map rolls a faction mix:
  - **Infested** — one faction, different each time, holds nearly the whole ship, with the others clinging to a room or two
  - **Balanced** — the factions split the ship evenly into territories of their own
  - **Contested** — uneven territories broken into many small pockets, so the realities interleave across the ship
  - **Warped** — no territories at all: every room is a random faction
- Even in a territory, the anomaly flips the odd room to a random faction, so a run can always surprise. Territories grow out from rooms spread across the map, and a faction cut off by another's rooms takes the nearest free rooms past them as enclaves
- Content decides how often each mix comes up (Gauntlet leans Infested, Treasure Hunt leans Warped), and the Infestation modifier always makes a map Infested
- Every room is always one of the three realities — there are no neutral rooms. A faction sets how a room looks and whose enemies appear there, never what the room is: a Treasure room, a Resupply stop or a peaceful Event in BIO territory is still loot, ammo or calm, just in BIO's style. In a generated level a room's enemies come from its faction's roster (how many still depends on the room type, so peaceful rooms stay empty) and its floor takes on the faction's colour, so territory is visible in the world and on the captured map
- **Map Graph editor** (Window › CGD › Map Graph): generate from a seed or roll a new seed, pan and zoom, click to select, drag nodes around, shift-drag between nodes to connect them, right-click to add, retype, lock, disconnect or delete, and edit a node's type, intensity, faction (and a Breach room's second faction), ship section and connections in the side panel, a locked door's lock kind and its key room or terminal rooms, whether a passage is one-way, and the map's run modifiers. Every edit can be undone
- **Analyze** (Map Graph window) generates a style for many seeds (100 by default) and reports averages and ranges for room count, main path, branches, dead ends, gates and loops; how often each room type appears and how often it has space beside it; faction territory, how much of a map its biggest faction holds, faction mixes, sections, layouts and modifiers picked; and every warning and broken rule, with how many maps had it. The report can be copied as text
- A saved map keeps the run modifiers and faction mix it was generated with, so changing a style's lists later never quietly changes a hand-tuned map; regenerating rolls them afresh
- Clicking a validation issue that names a room or connection selects it and centres the view on it
- Locked doors are labelled with their key room or terminal rooms on the graph; the side panel shows the run's modifiers and faction mix, which layout the map was built on (and its room range), whether a room sits behind a gate, and which door's key or terminal it holds
- **Blueprint** mode (Map Graph window) shows the graph as the level it becomes: a top-down floor plan laid out exactly as a generated level would be — room shapes and sizes, multi-cell halls, corridors, walls and inner walls, pillars and landmark furniture, and coloured markers on locked (yellow), secret (purple) and one-way (cyan) doorways. Rooms are filled by room type or faction and labelled with their place on the ship. It updates as the graph is edited; clicking a room selects its node, so it can be edited from the side panel while watching the plan change. Layout problems (such as crossing corridors) show along the bottom
- A level built from a saved map graph uses the graph's seed by default, so it matches the blueprint
- View modes colour the graph by room type, intensity, faction, ship section, required vs optional (rooms every route to the Exit must pass through), or branch. Selecting a node highlights the route to it from Start, and can dim everything outside its branch
- The generated graph and your hand edits are kept separately: edited rooms are marked, edits can be reverted to the generated version, and regenerating asks first when there are edits
- Locked nodes survive regeneration: the new map keeps a room of the same type at a similar depth on the same kind of route (main path or branch), with the same intensity
- The side panel checks the graph as you edit: exactly one Start and Exit, a Boss, the Exit reachable, every room reachable, every room-type rule (counts as the run's modifiers change them, placement, depth, neighbours, spacing, connection limit) still met, the pacing kept, every guaranteed room still in place, and every locked door still openable — it plays the map through, collecting keys, switching on terminals and opening doors, and flags any door whose key or terminal is missing, behind the door itself or behind another locked door (and a keycard lock given more than one key room)

## Seeds
- Everything generated has its own deterministic seed, branching from a parent: a map's layout and room types, each loot roll, each weapon. The same seed always produces the same result on every machine
- A layer's seed depends only on its parent and its name, never on what else was generated first, so changing how one system rolls can't shift another
- Seeds can be typed as numbers or any text ("banana" is a valid seed)
- Things generated without a chosen seed still get a random one and remember it, so any weapon can be reproduced afterwards

## Game Time
- A central game clock counts time in fixed ticks — 60 per game-second by default (one tick = 1/60 s) — independent of frame rate, for systems that need deterministic timing
- Pausing, slow motion and other time scaling all go through the clock, and Unity's physics, animation and movement follow it
- Several things can pause at once (pause menu, cutscene): time resumes only when all of them let go
- Speed changes stack: slow motion during a hit-stop is slower still; a speed change can last a fixed real-time duration (e.g. a 0.1 s hit-stop) and ends by itself
- Physics steps shrink with slow motion so it stays smooth
- Callbacks can be scheduled a number of game seconds or ticks ahead
- A long frame hitch runs at most a few ticks instead of freezing to catch up
- Quest time limits and temporary buffs run on game time, so they pause with the game and slow down in slow motion

## State Machines
- One reusable state machine for anything with states — characters, enemies, weapons, bosses, doors, machines, quests
- States switch when a condition becomes true, when a state or event asks, or forcibly (respawn/reset)
- "From any state" transitions cover interrupts like Stunned or Dead in one line instead of in every state
- A rule can forbid moves outright (nothing leaves Dead, a finished quest can't fail)
- Tracks the previous state and time spent in the current one
- Small machines can be built from plain callbacks, without a class per state

## Stats & Modifiers
- A generic modifier system for any stat: flat bonuses, percentage bonuses that add together (+10% and +10% = +20%), multipliers that compound (+10% and +10% = +21%), and overrides that fix a value outright
- Modifiers remember where they came from, so removing an attachment, a buff or a difficulty setting removes exactly its bonuses
- A character's damage, health and armor combine: base value → equipment (weapon attachments) → permanent presets (e.g. difficulty) → temporary buffs and debuffs
- Temporary modifiers expire on game time and are cleared when the character dies and revives
- Presets group modifiers into one asset: included are Hard Difficulty (enemies +50% health, +25% damage) and Damage Boost (+30% damage)
- Enemies with the Hard Difficulty preset get tougher without touching their enemy data

## Quests
- Quests are assets made of objectives: kill a number of a given enemy type, collect a number of an item, or wait for a scene event (talking to someone, reaching a place, pressing a switch)
- Objectives can be optional, or unlock one at a time in order
- Quests move through Locked → Available → Active → Completed or Failed; impossible moves (failing a finished quest) are refused
- Chains: a quest unlocks when all its prerequisite quests are complete, and can start automatically
- Time limits in game seconds; a failed quest can be allowed to retry
- Rewards (items or freshly rolled gear) go straight into the inventory on completion
- A quest panel on the HUD lists active quests with their objectives, progress counts, finished objectives struck through and a countdown for timed quests
- Included: a two-quest chain for the sandbox — Target Practice (destroy 3 target dummies, pays 60 rounds) unlocks Resupply (pick up 30 rounds)

## Camera Effects
- Screen shake from "trauma": hits and explosions add trauma, which fades over time; many small hits blend into one shake instead of fighting each other
- Explosions shake every nearby camera, fading with distance
- Taking damage shakes the camera in proportion to the hit
- Each shot gives the view a springy kick on top of the weapon's aim recoil
- FOV punches that ease back (for dashes, boosts, explosions)
- Optional camera lag that makes the view trail sudden movement slightly (off by default — it's uncomfortable in first person)
- Camera transitions: blend the view smoothly to another viewpoint (death cam, door, boss intro) and back
- Effects are visual only — shake never moves where your shots go
- Lock-on (T): locks the aim onto the enemy nearest the centre of view and keeps it there; press again to release. The lock drops when the target dies or gets too far away

## Equipment
- Press Tab for the Character window: worn armor per slot (helm, torso, gloves, legs, boots, backslot), carried weapons, and what's in the pack
- Click armor in the pack to wear it (whatever was in that slot goes back to the pack); click worn armor to take it off
- Worn armor adds everything it rolled — armor, health, resistances — to the player, including the effect of any attachments fitted to it
- **Offhand**: every loadout slot has an offhand next to its main weapon (see Weapon Loadout). Shields, one-handed melee weapons and artifacts go there. It only counts while that slot's main is one-handed — pistols, SMGs, daggers, swords and axes — or empty. Under a two-handed main it stays in the slot, shown as unused (grayed out), and comes back when the slot gets a one-handed main. Shields found on a run go straight into the first free offhand
- **Artifacts** (relics, tomes, mirrors, lanterns — the Void's offhand items) also go in the offhand slot. They do two things, together or alone:
  - **Passive**: while held they give their rolled stats to you like worn armor — and a curse is just a negative stat (the Bone Idol sharpens your crits but drains your maximum health). Their perks work too: passive ones always, triggered ones (like armor's) answer to everything while it's held
  - **Active**: use it on the **right mouse**. An active artifact takes the right mouse, so a gun can't aim while it's held, the same trade shields make (and a melee weapon's own guard gives way to it). A passive-only artifact leaves the right mouse alone, so you keep your sights. V stays the weapon bash either way, so you can always parry
  - Three ways to use one: **tap** casts a spell, **hold** keeps something up while draining stamina (a ward), **hold** keeps a channel running (a buff). Hold uses drop when the stamina runs out or you can't act (dodging, stunned); a cast in progress is lost the same way
  - Artifacts roll a tier like other gear: better ones land better stats and are more **potent** (spells hit harder, channels give more). Cooldowns and charges only recharge while the artifact is held
  - The held artifact shows in a slot left of the ability bar: its name, whether right mouse taps or holds, how close it is to ready (or the stamina left while a hold is up), and its charges
  - **Hand Mirror**: hold to raise it; hits from in front are negated and thrown back at whoever made them. Drains stamina quickly
  - **Whispering Tome**: right mouse rends the enemy under your crosshair (45 damage, pierces armor). Two charges, a short cast, recharging over time or faster as you hit, kill and parry. Also adds a little status strength
  - **Bone Idol**: no active use. More crit chance and crit damage, less maximum health
  - **Oil Lantern**: hold to light it; steadier hands (25% less spread) and status resistance while it burns, draining stamina slowly
  - Found as loot (rare), in the Void's reality; held artifacts count as carried on a run, and one you're handed goes straight into an empty offhand
- Attachments: pick one from the pack, then click the armor or weapon to fit it; click a fitted attachment to take it back off. Some attachments only fit certain armor slots
- Attachments on weapons change the weapon for real: magazine size, reload time, fire rate and damage all follow them (e.g. Extended Magazine: +25% magazine, slower reload)

## Consumables
- Four item slots (keys 5–8) for consumables and throwables, shown bottom-right with how many of each are left; the row of a grenade in hand lights up
- **Channelled items** take their use time, shown as a filling bar, and start when the key is tapped; they're only spent once the use finishes. Taking damage interrupts items that need concentration (the Bandage), as do being stunned, mantling or rolling; tapping a channelled item again cancels
- **Instant items** go off the moment the key goes down, so they can be woven into combos (eat between hits, or in the same moment as an ability). Each one puts **every** consumable on a short shared cooldown (shown as a grey bar draining over the consumable rows). Channelled items set no cooldown, so an instant item can follow straight after one finishes; pressing an instant item while channelling doesn't cancel the channel
- Using a consumable mid-combo holds the melee combo open, like an ability
- Effects: healing, removing all status effects, a timed stat buff, and restoring a resource meter
- Included: Bandage (1.5 s, heals 35 and stops bleeding and other effects), Combat Stim (0.5 s, +30% damage for 15 s and 50 stamina) and Ration (instant, heals 20, 1.5 s shared cooldown)
- Choose what sits in each item slot by holding its key (item wheel) or from the Character window. Holding an instant item's key uses one before the wheel opens; the **Hold Instant Item for Wheel** setting (Accessibility tab, on by default) turns the wheel off for instant items so that never happens

## Crafting
- Crafting stations in the world open a crafting window listing their recipes, what each needs and how much of it you have
- Recipes you can afford can be crafted instantly; the ingredients are taken and the result goes into the pack (armor is rolled fresh)
- Included recipes: Bandage ×2 (2 Cloth), Combat Stim (4 Scrap Metal, 1 Cloth), Extended Magazine (12 Scrap Metal), Combat Vest (20 Scrap Metal, 6 Cloth)

## Dev Console
- Backquote (`) opens a console in development builds; commands start with a slash (`/help`), with history (Up/Down) and Tab completion. Escape closes it
- While the console is open every other keybind is muted, so typing a letter never triggers a hotkey like the inventory, map or weapon slots
- `/give <item> [count]`, `/weapon <category> [seed] [tier]` (same seed and tier = same gun), `/spawn <enemy> [count]`
- `/god`, `/heal`, `/buff <preset> [seconds]`, `/timescale <x>`, `/revealmap`, `/cloak [seconds]`, `/dodge [type]` (switch dodge style to try them out)
- `/quest list`, `/quest start|complete <quest>`, `/quest step <quest> <n>` to jump straight to any objective
- Names can be partial ("/give scrap 20")

## Minimap & World Map
- A round minimap in the top-right corner shows the area around the player, turning with the view so the player arrow always points up (or fixed north-up, per setting)
- Press M for the full world map: the whole area, every marker and the player's position and heading; the game pauses and controls lock while it's open
- Fog of war: the map starts dark and is uncovered around the player as they explore; things in unexplored areas stay hidden
- Markers for anything placed in the world — enemies, pickups, objectives — in a chosen shape (circle, square, diamond, or an arrow that shows facing), colour and size; markers can be minimap-only or world-map-only, disappear when their enemy dies, and objective markers stay pinned to the minimap's edge when out of range
- The map picture can be a top-down snapshot of the level taken automatically when the scene starts, a hand-made image, or — for procedurally generated runs — the map graph drawn as a schematic of rooms and connections in each room type's colour
- Explored areas can be saved and restored

## Feedback & Notifications
- One place for how the game responds to events: messages, screen flashes, controller vibration, camera shake and sounds are bundled into reusable feedback presets, so each reaction is tuned in one asset
- Message feed at the top of the screen for pickups ("+30 Standard Rounds", "Picked up …"), kills, quest updates and warnings, coloured by kind (info, success, reward, warning, danger); repeated messages stack into "×3" instead of flooding the feed, and old messages fade out
- Hit markers around the crosshair confirm every hit you land: white for hits, orange for critical hits, a bigger red marker for kills — also for grenade and ability damage
- Kill confirmation: "Eliminated Target Dummy"
- Controller vibration for dealing and taking damage — bigger hits rumble harder, overlapping hits build up, and vibration stops while paused or when the game loses focus; can be turned off
- A low-health warning (message, red pulse, rumble) the moment health drops below 30%, re-armed once you heal back above it
- Quest announcements: new quest, objective complete, quest complete (with a golden flash), quest failed

## Stealth
- Step into a bush or a smoke cloud, or use the Stealth ability, and you become hidden: a HIDDEN label appears under the crosshair
- Hidden players can only be noticed by enemies right next to them (about 2.5 m); an enemy standing inside the same bush or smoke sees you from further away (about 6 m)
- Crouching makes you harder to notice, sprinting easier
- Some bushes only hide you while crouching
- Smoke also blocks enemies' line of sight through it, so you can cross open ground behind a smoke grenade
- Shooting, swinging or using abilities gives your position away for a moment (REVEALED); silenced weapons don't
- The Stealth ability hides you anywhere for 6 seconds; attacking ends it early
- If an enemy spots you while you're trying to hide, a "Spotted!" warning and a quick rumble let you know
- Enemies can still hear noise and come to investigate even when they can't see you
- Smoke grenade: bursts into a smoke cloud that lasts 15 seconds and deals no damage

## Generated Levels
- A scene can build its whole level from a map graph when it loads: every room in the graph becomes a room, and every connection becomes a corridor between the two rooms
- Rooms come in different floor plans: rectangles, L, T and U shapes, crosses, rings around an enclosed courtyard, and round domes. Each room varies the proportions (arm widths, courtyard size) and may be turned or mirrored, so no two rooms of the same shape look identical
- Every room is a place on the colony ship — Lobby, Restaurant, Park, Casino, Crew Quarters, Cargo Bay, Grand Atrium, Docking Bay or Supply Depot — chosen to fit the room's role: fights happen in cargo bays, quarters, parks and restaurants, shops are restaurants, casinos or lobbies, resupply rooms are supply depots with an ammo cache against the wall, the boss waits in the Grand Atrium and the way out is a docking bay
- Places match the ship section they're in: the same Combat room is more likely crew quarters in Habitation, a restaurant or park in Commerce and a cargo bay in Engineering
- Each kind of place has its own floor plans, size and height: towering parks and atriums, low-ceilinged crew quarters, compact restaurants and casinos
- Big places take up more of the ship when there's space around them: cargo bays and parks can stretch across two rooms' worth of space, and the Grand Atrium can be a vast four-room hall — never at the cost of a neighbouring room
- Landmark rooms: hand-built places such as the Reactor Core (a towering reactor ringed by control consoles) can appear in place of a generated room, with doorways only where their design allows
- Rooms are furnished to suit them: dining tables in restaurant halls with supply crates in the kitchen, beds along crew-quarter walls, slot machines lining the casino, crates stacked along cargo bay walls, planters in parks, lobbies and atriums. Furniture faces sensibly (backed against walls, lined up with the room) and never blocks doorways or walkways
- Optional ceilings close every room and corridor at its own height; parks stay open to the sky
- Shapes suit their place in the map: hub rooms with many connections favour crosses and domes. Rooms are always one connected space, with every passage at least two tiles (6 m) wide
- Domes and rings have smooth, rounded walls: the stair-steps of a round floor plan become an arc of short wall pieces with gentle bends (about 11° each by default), never moving the walls beside a doorway
- Room corners are bevelled at 45° by default — the ship-interior look — one tile deep, two in the Grand Atrium and Docking Bay. Only outside corners are cut (never inside ones), never where a doorway is close by, and nothing is placed in the cut-off space; corner props move to the spots on either side of the bevel. Corridor bends are bevelled on their outside corner too. Both can be turned off, and each kind of room can set its own depth
- Walls wear a modular art kit instead of plain boxes: wall panels repeated along every wall, angled pieces on bevels and rounded walls, posts at corners and on either side of each doorway, and a frame around every doorway. Each reality has its own placeholder kit, after the GDD's art direction:
  - **TECH** (the ship's own luxury interior, also used for corridors): clean light panels, dark trims and fluorescent white-blue light strips
  - **BIO**: weathered metal overtaken by moss and roots, with glowing bioluminescent pods
  - **VOID**: near-black panels with slabs set at impossible angles, violet cracks of light and frost along the floor
  A room wears the kit of the faction that holds it, so the reality is readable from the walls alone
- The Map Graph window's blueprint shows the bevels and the curved walls exactly as they're built
- Rooms have structure: rows of support columns in lobbies, cargo and docking bays (lined up across the whole ship), colonnades circling atriums and casinos, columns framing the inside corners of L, T and cross rooms, kitchen walls in restaurants, cabin walls in crew quarters and a bar counter in casinos
- A clear walkway always runs from every doorway to the middle of the room: no column, wall or prop is ever placed on it, and inner walls always leave gaps where it passes, so no part of a room is ever walled off
- Doorways sit on straight stretches of wall — at the end of an arm rather than tucked into a corner — never open into a ring's courtyard, and face the room they lead to
- Rooms sit where the graph places them, so the main path runs left to right and branches sit above and below it. Corridors are routed around rooms, and two different connections never join up. The rare map where two corridors have to cross is reported as a warning
- Locked and secret connections get their gate (a locked door, a breakable fake wall) on the side the player arrives from, so a locked branch is locked from the main path
- Generated levels have working gates out of the box: locked doors are yellow bulkheads that take a Security Keycard (one is placed for every locked door, always reachable before it, and used up on opening), secret passages are hidden behind a fake wall that looks like the others and breaks when shot, and one-way shortcuts are cyan doors. Ordinary passages stay open
- A one-way shortcut's door is barred from the near side ("Barred from the other side") and opens for a player coming from the deep end; after that it opens both ways
- Door signs: a glowing bar over every doorway, in the colour of the room it leads to (the same colours as the maps), so every fork is a readable choice — a Shop, a Treasure room, a Resupply stop. Doorways into a tier-3 room, the Boss, a Lockdown, Holdout or Rift, or any high-intensity room also show a red danger marker; an Ambush's sign shows Treasure. Secret passages get no sign
- Each locked door's key is placed in the room the map chose for it. Terminal-locked doors are orange bulkheads, with a terminal console (red light until switched on) in each room the map chose; the door unlocks once all of them are on
- Rooms are filled by type: enemies (more in higher-intensity rooms, patrolling out to the far ends of the room), a centrepiece (for example a chest in Treasure rooms, a crafting station in Shops, a boss) and scattered props that stay off columns, doorways and walkways
- The player starts in the Start room, a safe hub with a workbench. The Exit room has the extraction pad that ends the run in victory (hold Interact to extract)
- The MapTest scene plays a freshly generated level every time it starts or restarts, with the player in the Start room, a minimap, the world map (M) and HUD notifications. It runs the full expedition loop: the player starts with only the starting loadout (no dev ammo), death ends the run, and the ship screen between runs leads into the next one
- A level can use a fixed, hand-checked map graph or generate a fresh one each play. The same seed always gives the same map, the same rooms (kind, shape and structure) and the same room contents
- The world map and minimap resize to fit the generated level and show its floor plan: room shapes coloured by room type, walls, bevels, curves and gate markers, uncovered by fog of war as the player explores
- A generated level announces the run's modifiers when it starts

## Character Animation
- The player and enemies drive their model's animations from what they're actually doing: walking and strafing speed, sprinting, crouching, sliding, mantling, rolling, jumping and falling, aiming, reloading, firing, melee combo steps, grenade throws, getting hit, being stunned and dying
- Enemies switch to an alert stance once they notice a target, and play their attack as the wind-up starts
- Animations are cosmetic: attacks land on the same timing with or without a rigged model, and characters without one play exactly as before
- Respawned players and reused enemies start from a clean pose instead of the end of their death animation

## Impacts & Ragdolls
- Bullets, projectiles, enemy shots and melee hits leave per-surface impact effects: a decal (bullet hole), a burst of particles and an impact sound, each chosen by what was hit (concrete, metal, wood… by physics material). Characters use a separate "flesh" set
- Melee and bullets can look and sound different on the same surface
- Decals stick to moving objects such as doors, and disappear with objects that break. Only the newest ones are kept (128 by default), so long fights never pile up
- Characters with a ragdoll go limp when they die and are knocked away from whoever landed the killing blow. They get back up in a clean pose when revived or reused

## Economy & Shops
- Money is carried like any other item. Credits are the standard currency, and more currencies can exist side by side (each shop names the one it takes). Anything that can hand out items can hand out money: loot, pickups, quest rewards, the dev console (`/give credits 500`)
- Vendors sell from a shelf that is stocked when the level starts, and optionally restocked on a timer. The same vendor seed always gives the same shelf
- Goods can be bundles of stackable items (30 rounds, 2 bandages), rolled gear, or freshly generated weapons, each vendor mixing whatever its catalog lists
- Prices follow each item's worth: stackables per unit, rolled gear and weapons scaled by quality (a Legendary is worth several times a Common of the same kind). Vendors can charge a markup
- Items can be limited in number (sold out until the next restock) or unlimited
- A bought weapon goes straight into a free weapon slot. With every slot full it replaces the weapon in hand, which the vendor takes in trade and credits against the price. The shop shows this before you buy
- The player can sell items from their pack for a fraction of their worth. Cheap bulk goods like ammo sell in the smallest bundle worth a coin (Shift sells the whole stack). Nothing can be bought and sold back for a profit
- The shop window lists the shelf in tier colours with prices, what's out of reach (can't afford, no free slot) and the player's balance

## Dialogue & NPCs
- NPCs can be talked to with the Interact key. They turn to face the player and open a dialogue window with their name, their line and the player's replies
- Conversations branch: replies lead to other lines, loop back, or end the talk. The first four replies can also be picked with the number keys
- Replies can do things as well as move the talk along, such as opening the NPC's shop. Replies can also be shown only when a condition holds (for example, only while the player carries a certain item)
- Closing the window walks away from the conversation at any point
- In the Sandbox, the **Gunsmith** greets the player. "Let's trade" opens a shop with ammo for every caliber and a rotating selection of randomly rolled weapons. The player starts with 1,500 credits
