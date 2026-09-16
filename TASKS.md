# Unity Project – To-Do List

## Open
-   Add a `SniperCategory` weapon category asset
    `WeaponCategoryDefaults` already has Sniper values, but no category asset exists, so snipers never generate.

-   Blender integration for Claude
    https://github.com/ahujasid/blender-mcp

## Done
Completed work is described in [FEATURES.md](FEATURES.md); full task history is in git.

- Combat: melee combos, throwable grenades, armor/shield mitigation, hitboxes with per-region multipliers
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
