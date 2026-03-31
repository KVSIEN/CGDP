# Unity Project – To-Do List

## Combat
-   Implement melee system
    Light/heavy attacks, hit detection colliders, combo string logic with input buffering, currently no animations so add debug scene indicators for now.

-   Add throwable grenades / consumables
    Trajectory arc indicator, throw force tunable via ScriptableObject, integrates with ability or inventory slot

## Movement
-   Implement mantle / vault system
    Triggered by Space while airborne near a ledge; supports low vaults and high mantles; eventually plays matching animation, but no animations yet.

-   Add slide mechanic
    Activates from sprint + crouch input; smooth speed transition into and out of slide; uses existing Rigidbody movement

## Enemy AI
-   Build behavior tree framework
    Reusable node types — Selector, Sequence, Condition, Action; drives all enemy AI states
    Enemy health bars that appear on damage and fade after a few seconds

-   Implement patrol, alert, and chase states
    Patrol follows waypoints; alert investigates last known position; chase closes to attack range

-   Add line-of-sight and hearing detection
    LoS uses a raycast cone with configurable angle and range; hearing uses overlap sphere with radius tunable per enemy type

## HUD / Feedback
-   Add floating damage number popups
    World-space UI that spawns above hit position; headshots show larger/colored number; fades and floats upward

-   Implement hit flash and damage vignette
    Brief screen flash on taking damage; red vignette overlay that fades with health; integrates with existing TakeDamage event

## Systems
-   Build world pickup system
    Press E to collect or interact; supports ammo, health, and weapon pickups or interact; prompt appears on proximity; weapon pickups swap or add to carried weapons
    Interactive doors and switches tied to your Interact (E) key

## Audio
-   Implement surface-aware footstep system
    PhysicsMaterial or surface tag on ground triggers correct audio bank; separate clips for walk, sprint, crouch; randomized pitch variation per step

Claude - Blender Integration:
https://github.com/ahujasid/blender-mcp