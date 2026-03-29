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
- All movement values (speeds, jump height, gravity, etc.) are tunable in a ScriptableObject without touching code

## Camera
- Supports both first-person and third-person view
- Switch between views at any time with a keybind (default: V)
- Transition between views is smooth — camera slides between positions rather than snapping
- Aim point stays consistent during the transition — the center crosshair points at the same world direction in both modes
- Third-person camera avoids clipping into walls by pulling in when geometry is close
- Shoulder offset in third-person view, which fades out during the transition to first-person
- Player mesh automatically hides when close enough to first-person to avoid it blocking the view
- Sprint FOV kick — field of view widens slightly while sprinting
- Mouse and gamepad both supported with separate sensitivity settings
- Body rotation is snappy in first-person and smooth in third-person

## HUD / UI
- Modular HUD system — elements (crosshair, stats) are independent and can be shown or hidden individually
- Crosshair drawn entirely in code — no texture needed; updates instantly when settings change
- Crosshair is fully configurable: line length, thickness, center gap, opacity, color, center dot, and outline (color + thickness)
- Health bar that changes color from green to red as health drops, with a numeric readout
- Ammo counter displaying magazine and reserve rounds
- Stats HUD updates automatically whenever health or ammo changes — no polling needed
- All crosshair options live in a ScriptableObject so they can be tweaked in the Inspector and shared across scenes

## Player Stats
- Health with TakeDamage and Heal methods
- Ammo with UseAmmo and Reload methods
- Fires a change event so the HUD reacts immediately without polling

## Ability System
- Four ability slots assignable to keys 1, 2, 3, 4
- Each ability has its own cooldown; activating during cooldown does nothing
- Cooldown starts only if the ability succeeds (e.g. heal won't trigger if already full health)
- Four built-in abilities: Dash, Projectile, Heal, and Shockwave
  - **Dash** — bursts the player horizontally in their move direction (or camera forward if idle)
  - **Projectile** — fires a fast-moving projectile from the camera that deals damage on impact
  - **Heal** — instantly restores a set amount of health
  - **Shockwave** — launches all nearby rigidbodies away from the player
- All ability values (cooldown, force, damage, etc.) are tunable on the ScriptableObject asset
- HUD shows four coloured slots at the bottom of the screen; a dark overlay drains away as each cooldown recovers

## Input System
- All actions are defined in one place and can be reconfigured without touching code
- Every action supports a primary and secondary keybind (e.g. LeftCtrl and C both crouch)
- Mouse buttons are also supported as primary or secondary bindings
- Each action can independently be set to one of three modes:
  - Pressed — fires once on the frame the key is pressed
  - Held — fires every frame the key is held down
  - Toggle — pressing the key flips it on or off
- Bindings can be remapped at runtime (e.g. from a settings screen)
- Default actions: Jump, Sprint, Crouch, Dodge, Attack, Aim, Reload, Interact, Abilities 1–4, Pause, Switch View, Inventory, Map
