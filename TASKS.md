# Unity Project – To-Do List

## Open
- Encounter rooms, next steps: real art and sounds for shutters, consoles and the rift; a failsafe if a lockdown's last enemy gets stuck out of reach; enemies reacting to the Holdout uplink by heading for it
- Expedition loop, next steps: save the ship hold to disk (needs an item catalog for gear), a real docked-ship scene, extraction only after the boss (or a timed early extraction)
- Room generation, next steps (starship rooms):
  - Real art for placeholder props and the Reactor Core interior; interior lighting for ceilings
  - Finer curves for large domes; landmark rotation
-   Blender integration for Claude
    https://github.com/ahujasid/blender-mcp

## Done
Completed work is described in [FEATURES.md](FEATURES.md); full task history is in git.

- Quiet rooms (rare, peaceful, always a salvage node / ammo cache / loot cache); room rules can guarantee an offering
- Encounter rooms: Lockdown, Holdout, Puzzle (calibration ring, forward doors), Ambush, Stealth, Rift, Gamble, Emergency Exit (partial extraction), Hazard
- Lock variants: condition locks ("do A and B") for doors — terminals, events, quest signals; generated Terminal locks (2–3 terminals in separate rooms) with editor and validator support
- Expedition core loop: starting-room hub with workbench and random loadout, Exit as extraction, lose everything on death, ship hold + pack screen between runs
- Tier-1 capsule enemies per reality (Maintenance Bot, Scavenger, Husk) at ≈100 effective health, in the faction rosters; placeholder tier 2–3 enemies per faction with random affixes (0/1/2 by tier), tier chances per room type
- MapTest scene (generated levels with the full player setup, blueprint world map and minimap, feedback)
- Modular wall kits (placeholder TECH/BIO/VOID art per the GDD), per-faction
- Smoothly rounded dome and ring walls
- 45° bevelled room corners and corridor bends (shared wall plan for building and blueprint)
- Map Graph Blueprint mode (graph laid out as the level's floor plan)
- Level gate prefabs (locked door + keycard, secret wall, one-way door); saved graphs keep their run modifiers; editable sections; clickable validation issues
- Door signs, one-way shortcuts, Breach rooms, warning/anomaly run modifiers
- Faction mixes (Infested, Balanced, Contested, Warped) rolled per map
- Map styles: guarantee list, pacing, same-type spacing, rooms that want space, Resupply rooms (Supply Depot), ship sections, run modifiers, Analyze report
- Faction assets (TECH/BIO/VOID) owning generated rooms (enemy roster, floor tint), ammo caches, placeholder resource nodes
- Input layout (dodge C, abilities QFGX, items 5–8, shoulder swap H, no extra mouse buttons), hold-for-wheel on weapon/item keys, grenades as items readied from item slots
- Dodge styles as data (sidestep+roll, committed roll, steerable boost, long dash, air dash), i-frames, input buffer, air dodges, stun/mantle cancel
- Multi-cell rooms (2×1, 2×2), landmark rooms (Reactor Core), prop pass per room function with placeholder props, optional ceilings
- Room functions (8 starship room kinds: size, height, shapes, structure), curved dome walls, structure rules (ship-wide pillar grid, column rings, corner columns, dividers, counters), walkways, tile tags and zones
- Room shapes: rooms as tile footprints, RoomShape assets (Rect, L, T, U, Cross, Ring, Dome), door sockets, merged wall runs
- Economy (currency items, price tables, vendors with stock sources, buying/selling/trade-ins) and branching NPC dialogue with actions and conditions; Sandbox Gunsmith vendor
- Settings tabs (audio, video, accessibility), character animation bridge and ragdolls, levels generated from map graphs, per-surface impact effects and decals
- Stealth: hiding in bushes and smoke, stealth ability, proximity reveal, smoke grenade, stealth HUD
- Equipment slots and attachment fitting, quick-use consumables, crafting stations and recipes, dev console with cheats
- Minimap/world map (fog of war, markers, captured/procedural backgrounds) and feedback system (notification feed, hit markers, kill confirms, rumble, flashes, quest announcements)
- Framework systems: deterministic seeds (map layers, weapon rolls, loot), game clock (ticks, pause, time scale), state machine (EnemyAI on it), stat modifiers (CharacterStats, presets, buffs), quests (objectives, chains, rewards, HUD), camera effects (shake, kicks, FOV, lag, blends, lock-on)
- Core systems: object pooling (IPoolable, prewarm, VFX lifetime, pooled enemies), loot tables and drops (rarity, nesting, chests, breakables), game flow states (pause, loading, game over), targeting rules, interaction framework (locks, priority, highlights, event interactables), generic resource meters (stamina, mana, oxygen, rage; shield runs on it)
- Gear perks on weapons and armor, rolled by tier: passive (magazine, multishot, velocity, crit, status, lifesteal, move speed, cooldowns, regen…) and triggered (kill, hit, crit, parry, dodge, ability, damage taken, healed, aim, reload, item, swap)
- Map objectives: main (unlock the boss room) and side (rewards), steps across room tiers
- Room tiers 1–3 (enemy tiers, loot luck, resources) replacing Elite rooms; tier 2–3 enemies with random affixes; Surge abilities
- Reflects (vengeance, deflect, absorb, parry ripostes), combo weaving across dodges/abilities/weapon swaps, swap-dodge draw cancel, optional input buffering
- Melee weapons (dagger, sword, axe, hammer, spear) as rolled loadout weapons: attack speed, stamina, block and parry on the aim input
- Bows (multishot, vertical/horizontal draw) and crossbows (autoloaders)
- Combat: melee combos, throwable grenades, armor/shield mitigation, hitboxes with per-region multipliers, teams, attacker info and on-hit status effects on every attack
- Weapons: per-weapon ammo that survives swaps, weapon swap-drops on full loadout, action gating while stunned/mantling/rolling
- Respawn: systems reset themselves on revive (ammo, cooldowns, momentum, status effects)
- Status effects: stacking modes (refresh / stack / independent), immunities, cached target context, status HUD
- Shared Stunnable component for player and enemies
- Enemy AI: team-based perception, aggro on hit, noise-based hearing, wind-up attacks, state classes, separate state visuals
- Interaction: line-of-sight check, hold-to-interact with progress bar
- Abilities: CanExecute/Execute split, charges, cast times, Shockwave layer mask and damage
- Pooling: projectiles, grenades and damage popups; AudioPool without per-sound coroutines; mixer groups
- Cleanups: HUDManager discovers its elements; damage type and armor penetration on ranged weapons
- Status effects: Bleed, Poison, Fire, Lightning, Ice
- Abilities & actions: shared CooldownTimer, self-driven Dodge/Mantle, "can act" gating, unified cooldown ratios
- Weapon recoil: unified recoil model, authored horizontal dominance, horizontal cap, blended jitter
- Movement: mantle/vault, slide
- Enemy AI: behavior tree, patrol/alert/chase, line-of-sight and hearing, state colors, health bars
- HUD: damage popups, hit flash and damage vignette
- Systems: pickups, doors and switches
- Input: gamepad bindings, name-based saves, conflict detection, native rebinding, unified Move/Look, UGUI settings menu
- Audio: pooled sources and sound banks, weapon/melee/grenade/player/enemy sounds, surface-aware footsteps
- Project structure: `Project` layout, feature folders, namespaces, assembly definition
- Default data set: an asset for every data type, Sniper category, projectile and grenade prefabs
