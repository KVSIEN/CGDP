# Unity Project – To-Do List

## Open
-   Blender integration for Claude
    https://github.com/ahujasid/blender-mcp

## Done
Completed work is described in [FEATURES.md](FEATURES.md); full task history is in git.

- Equipment slots and attachment fitting, quick-use consumables, crafting stations and recipes, dev console with cheats
- Minimap/world map (fog of war, markers, captured/procedural backgrounds) and feedback system (notification feed, hit markers, kill confirms, rumble, flashes, quest announcements)
- Framework systems: deterministic seeds (map layers, weapon rolls, loot), game clock (ticks, pause, time scale), state machine (EnemyAI on it), stat modifiers (CharacterStats, presets, buffs), quests (objectives, chains, rewards, HUD), camera effects (shake, kicks, FOV, lag, blends, lock-on)
- Core systems: object pooling (IPoolable, prewarm, VFX lifetime, pooled enemies), loot tables and drops (rarity, nesting, chests, breakables), game flow states (pause, loading, game over), targeting rules, interaction framework (locks, priority, highlights, event interactables), generic resource meters (stamina, mana, oxygen, rage; shield runs on it)
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
