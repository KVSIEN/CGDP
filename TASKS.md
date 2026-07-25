# Unity Project – To-Do List

## Combat
-   Implement melee system
    Light/heavy attacks, hit detection colliders, combo string logic with input buffering, currently no animations so add debug scene indicators for now.

-   Add throwable grenades / consumables
    Trajectory arc indicator, throw force tunable via ScriptableObject, integrates with ability or inventory slot

## Ability & Action System
-   ~~Extract a shared CooldownTimer / activation abstraction~~ ✓ Done
    PlayerAbilities (per-slot), PlayerDodge (`_dodgeCooldownTimer`), WeaponController (`_fireCooldown`), and EnemyAI (`_attackCooldown`) each hand-roll the same "if (timer > 0) timer -= Time.deltaTime" pattern, and PlayerAbilities.GetReadyRatio / PlayerDodge.DodgeReadyRatio independently compute the same `1 − timer/max` ready-ratio formula; pull this into one reusable CooldownTimer struct (Start/Tick/IsReady/Ratio) so abilities and actions share a single cooldown model instead of four parallel ones

-   ~~Give Dodge and Mantle the same self-contained lifecycle Abilities have~~ ✓ Done
    Ability1-4 are driven by a dedicated orchestrator (PlayerAbilities) that owns their input, cooldown, and execution; Dodge and Mantle instead get manually pumped from inside PlayerMovement.FixedUpdate() (`_mantle.Tick(); ... _dodge.Tick();`), coupling their execution to PlayerMovement staying enabled rather than being independently driven actions in their own right

-   ~~Gate ability execution against exclusive movement states~~ ✓ Done
    PlayerAbilities only checks its own cooldown before calling Execute() — nothing stops an ability firing while Mantling (mid-mantle the Rigidbody is kinematic, so e.g. DashAbility's AddForce silently does nothing but the ability still consumes its cooldown) or mid-dodge-roll; add a shared "can act" guard (e.g. IsMantling / dodge-roll lock) that both the ability and action systems check before activating

-   ~~Unify the cooldown-ratio API HUD elements read~~ ✓ Done
    AbilityHUD reads `_abilities.GetReadyRatio(i)`, DodgeHUD reads `_dodge.DodgeReadyRatio` — two different shapes for the same concept; expose a common read-only cooldown accessor (paired with the CooldownTimer extraction above) so HUD code, and any future action or ability, don't each need a bespoke ratio property

## Weapon Recoil
-   ~~Unify vertical and horizontal recoil under one recoilScale model~~ ✓ Done
    Vertical kick is `RecoilVerticalMax ± RecoilVerticalBias` — a consistent base with a small random variance, so it reads as predictable; horizontal kick is `Random(-RecoilHorizontalMax, +RecoilHorizontalMax) + drift`, which is almost entirely random with no stable base — the two axes use different shapes for what should be the same concept; replace both with `axisScale × (pattern + jitter)` so vertical and horizontal share one predictable-base-plus-randomness model, driven by a single `RecoilScale` (horizontal, vertical) on WeaponData

-   ~~Make horizontal dominance an authored, felt property instead of an emergent random walk~~ ✓ Done
    `RecoilHorizontalBias` (-1 full left … 1 full right) is meant to express a gun's natural lean, but it only reaches the shot through `_recoilDriftDir`, a value that random-walks by ±0.4 per shot and is clamped to [-1, 1] — on short bursts or semi-auto it barely moves off 0 before the burst ends, so the authored bias is barely felt; apply the dominant direction directly to each shot's horizontal kick (`horizontalScale × (DominantDirection + smallRandomJitter)`) so left/right-dominant weapons read as consistently biased from the first shot, not after several rounds of drift

-   ~~Cap accumulated horizontal recoil the way vertical already is~~ ✓ Done
    Vertical kick is bounded against `MaxAccumulatedRecoil` so a full-auto spray stays controllable; horizontal has no equivalent — `PlayerCamera._recoilYaw` can accumulate with no ceiling over a long sustained burst (LMGs especially); add a matching horizontal accumulation cap so both axes stay predictable at high fire rates

-   ~~Blend jitter instead of sampling flat uniform randomness~~ ✓ Done
    Both axes currently sample `Random.Range` directly, which reads as slightly jittery/flat shot to shot; averaging two uniform samples (or similar) produces a more natural, center-weighted spread for the "bit of randomness" layer without changing the authored pattern strength

## Damage, Armor & Shields
-   ~~Add Armor and damage mitigation~~ ✓ Done
    Effective Damage = Raw Damage × (100 / (100 + Effective Armor)); Effective Armor = Armor × (1 − Armor Penetration%); replace the flat `TakeDamage(float)` on PlayerStats/EnemyHealth with a shared DamageInfo (raw damage, penetration%, damage type) resolved through one mitigation step so weapons, abilities, and status ticks all mitigate the same way

-   ~~Add Shield system~~ ✓ Done
    Shield absorbs damage before Health; damage remaining after Shield depletes in the same hit carries over to Health; Shield regenerates after X seconds without taking damage (tunable per PlayerStats/EnemyData); pairs with the Lightning bonus-vs-shield rule below

## Status Effects
-   ~~Build a stacking/duration status effect framework~~ ✓ Done
    Reusable runtime component (e.g. StatusEffectController) that ticks active effects, tracks stacks/duration per effect type, and resolves damage-over-time through the same Effective Damage pipeline as direct hits; each effect below plugs in as its own strategy/ScriptableObject, consistent with the existing WeaponFireBehavior pattern

-   ~~Bleed~~ ✓ Done
    Bleed Damage (per tick) = (Raw Damage × 50%) + (Target Max Health × 2%); ignores Armor entirely; does not stack — reapplying Bleed refreshes its duration instead

-   ~~Poison~~ ✓ Done
    Poison Damage (per tick) = Effective Damage × (1.2 ^ (Stacks − 1)) (tune multiplier/cap, e.g. 1.5 with a 5-stack max); stacks infinitely or to a defined cap; each stack raises damage exponentially; uses Effective Damage (post-armor) as its base

-   ~~Fire~~ ✓ Done
    Fire Damage (per tick) = Effective Damage × Fire DPS%; Fire DPS% comes from whichever weapon/ability applied it; reapplying Fire refreshes duration; scales from Effective Damage so Armor still reduces it

-   ~~Lightning~~ ✓ Done
    Deals 50% bonus damage to Shields; chains to nearby enemies — Chain Count = 1 + Current Lightning Stacks; each chain hit deals 20% less than the previous (100% / 80% / 64% / 51.2% / 40.96%); a chain cannot strike the same enemy twice per cast

-   ~~Ice~~ ✓ Done
    Each stack grants 5% Movement Slow and 2% Armor Reduction, up to 5 stacks; at 5 stacks the target is Stunned for X seconds with an extra 10% Armor Reduction; during Stun totals reach 100% Slow and 20% Armor Reduction; stacks decay after X seconds without gaining a new one

## Movement
-   Implement mantle / vault system ✓ Done
    Triggered by Space while airborne near a ledge; supports low vaults and high mantles; eventually plays matching animation, but no animations yet.

-   ~~Add slide mechanic~~ ✓ Done

## Enemy AI ✓ Done
-   ~~Build behavior tree framework~~
    Reusable node types — Selector, Sequence, Condition, Action; drives all enemy AI states
    Enemy health bars that appear on damage and fade after a few seconds

-   ~~Implement patrol, alert, and chase states~~
    Patrol follows waypoints; alert investigates last known position; chase closes to attack range

-   ~~Add line-of-sight and hearing detection~~
    LoS uses a raycast cone with configurable angle and range; hearing uses overlap sphere with radius tunable per enemy type

-   ~~Indicate AI state with agent color change~~
    Enemy mesh color shifts per state (e.g. grey = patrol, yellow = alert, red = chase); no materials needed, drives renderer color directly; placeholder until proper animations/VFX exist

## HUD / Feedback ✓ Done
-   ~~Add floating damage number popups~~
    World-space UI that spawns above hit position; headshots show larger/colored number; fades and floats upward

-   ~~Implement hit flash and damage vignette~~
    Brief screen flash on taking damage; red vignette overlay that fades with health; integrates with existing TakeDamage event

## Systems
-   ~~Build world pickup system~~ ✓ Done
    Press E to collect or interact; supports ammo, health, and weapon pickups or interact; prompt appears on proximity; weapon pickups swap or add to carried weapons

-   ~~Interactive doors and switches tied to your Interact (E) key~~ ✓ Done

## Input System
-   Add gamepad support for discrete actions
    `ActionBinding` only stores Key/Mouse fields — Jump, Attack, Interact, Abilities, weapon slots, etc. currently cannot be bound to a gamepad button at all (only Move/Look axes get gamepad input, via the separate InputSystem_Actions asset); add a gamepad-button field per binding slot and extend BuildActions/Remap/PollForRebind to read and write it

-   ~~Store keybind saves by name, not enum ordinal~~ ✓ Done
    SettingsSave currently serializes GameAction/Key/InputMouseButton as raw `(int)` ordinals into PlayerPrefs; inserting or reordering a value in any of those enums silently remaps existing players' saved bindings to the wrong action or key; switch BindingEntry to store enum names (or a stable id) instead

-   Detect and warn on duplicate bindings
    WriteRebind() in SettingsMenu overwrites a slot without checking whether that key/mouse button is already bound to another action (or to the binding's own other slot); add a conflict check that blocks or warns before committing a new binding

-   Move keybind rebinding onto native Input System APIs
    KeyPath()/MousePath() hand-translate the Key enum into control path strings via a casing heuristic (already fragile enough to need a special-cased Digit-key exception); replacing the custom InputAction rebuild + rebind-polling loop with Unity's built-in `InputActionRebindingExtensions.PerformInteractiveRebinding` removes that translation layer entirely and picks up gamepad/any-device support for free

-   Unify Move/Look with the GameAction binding system
    Move and Look are driven by the generated InputSystem_Actions asset with fixed, non-remappable bindings, while every other action is a hand-built runtime InputAction driven by InputBindingSettings — two separate input pipelines that can't be edited from the same place; fold Move/Look into InputBindingSettings (or generate GameAction bindings from a single .inputactions asset) so there's one source of truth

-   Rebuild the settings menu in UGUI instead of OnGUI
    SettingsMenu draws itself with legacy IMGUI (OnGUI) — the only screen in the project not built on the same runtime UGUI/TextMeshPro system as the rest of the HUD; porting it over gets consistent styling, resolution scaling, and controller/gamepad navigation for free

## Audio
-   Implement surface-aware footstep system
    PhysicsMaterial or surface tag on ground triggers correct audio bank; separate clips for walk, sprint, crouch; randomized pitch variation per step

Claude - Blender Integration:
https://github.com/ahujasid/blender-mcp