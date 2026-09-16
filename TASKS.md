# Unity Project – To-Do List

## Open
-   Status effect polish
    One explicit stacking setting (refresh / stack / independent), cached target context instead of per-tick lookups, optional immunities, status icons on the HUD.

-   Shared stun/slow component
    PlayerMovement and EnemyAI each implement IStunnable; move it into one component both read.

-   Enemy AI upgrade
    Team-based perception instead of per-enemy player wiring, aggro when damaged, noise events for hearing, wind-up attacks through Hitbox.ApplyHit, state classes instead of a switch, state colors in their own component.

-   Interaction extras
    Optional line-of-sight check and hold-to-interact.

-   Abilities
    Split CanExecute from Execute, optional charges and cast time, Shockwave layer mask and damage through the hit pipeline.

-   Pooling and allocations
    Pool damage popups, projectiles and grenades; AudioPool without a coroutine per sound, plus mixer groups.

-   Smaller cleanups
    HUDManager collects HUDElement children instead of three hard-coded lists; one shared weapon stats definition so a new stat isn't added in three places; a DamageType on ranged weapons.

-   Blender integration for Claude
    https://github.com/ahujasid/blender-mcp

## Done
Completed work is described in [FEATURES.md](FEATURES.md); full task history is in git.

- Combat: melee combos, throwable grenades, armor/shield mitigation, hitboxes with per-region multipliers, teams, attacker info and on-hit status effects on every attack
- Weapons: per-weapon ammo that survives swaps, weapon swap-drops on full loadout, action gating while stunned/mantling/rolling
- Respawn: systems reset themselves on revive (ammo, cooldowns, momentum, status effects)
- Status effects: Bleed, Poison, Fire, Lightning, Ice
- Abilities & actions: shared CooldownTimer, self-driven Dodge/Mantle, "can act" gating, unified cooldown ratios
- Weapon recoil: unified recoil model, authored horizontal dominance, horizontal cap, blended jitter
- Movement: mantle/vault, slide
- Enemy AI: behavior tree, patrol/alert/chase, line-of-sight and hearing, state colors, health bars
- HUD: damage popups, hit flash and damage vignette
- Systems: pickups, doors and switches
- Input: gamepad bindings, name-based saves, conflict detection, native rebinding, unified Move/Look, UGUI settings menu
- Audio: pooled sources and sound banks, weapon/melee/grenade/player/enemy sounds, surface-aware footsteps
- Project structure: `_Project` layout, feature folders, namespaces, assembly definition
- Default data set: an asset for every data type, Sniper category, projectile and grenade prefabs
