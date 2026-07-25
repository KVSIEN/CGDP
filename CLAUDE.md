# Unity Project Guidelines

## Primary Goal

Prioritize maintainability over cleverness.

When implementing a feature, optimize in this order:

1. Correctness
2. Readability
3. Architecture
4. Extensibility
5. Performance (only when measurable)

Never sacrifice architecture simply to satisfy a guideline.

If multiple approaches satisfy these goals, choose the simplest one.

---

# Before Coding

Before making changes:

- Understand the existing architecture.
- Reuse existing systems before creating new ones.
- Match the surrounding codebase unless it is clearly problematic.
- Prefer extending existing abstractions over introducing parallel systems.
- Keep changes localized when possible.

Avoid introducing unnecessary complexity.

---

# Project

Unity URP

Language: C#

Source folders:

- Assets/
- ProjectSettings/

---

# Reading Rules

Only inspect:

- .cs
- .unity
- .prefab
- .asset
- .inputactions
- ProjectSettings/

Never inspect:

- Library/
- Temp/
- Logs/
- obj/
- Packages/
- binary assets

When locating references:

- search scenes/prefabs for component names
- avoid reading large YAML files in full
- inspect only relevant sections

---

# Architecture

Favor clean separation of responsibilities.

## MonoBehaviours

MonoBehaviours should primarily:

- own Unity lifecycle
- expose serialized references
- forward work to plain C# classes
- coordinate components

Avoid putting large amounts of game logic inside MonoBehaviours.

---

## Game Logic

Prefer plain C# classes for:

- gameplay rules
- calculations
- state transitions
- AI
- inventories
- combat
- progression

Keep these classes Unity-independent whenever practical.

---

## Data

Use ScriptableObjects for:

- configuration
- tuning values
- shared immutable data

Avoid static mutable state unless the project already uses it consistently.

---

## Dependencies

Prefer explicit dependencies.

Use:

- serialized references
- constructor injection (non-MonoBehaviour)
- initialization methods

Avoid hidden dependencies.

Never use:

- Find()
- FindObjectOfType()
- SendMessage()

unless the project already relies on them.

---

# Components

Prefer:

[SerializeField] private

over public fields.

Cache expensive lookups.

Call GetComponent in:

- Awake
- Start
- initialization

Never repeatedly call GetComponent in Update.

Use TryGetComponent whenever failure is expected.

---

# Input

Use the Unity Input System.

Use existing generated InputSystem_Actions.

Do not introduce the legacy Input API unless already required.

---

# Performance

Write readable code first.

Avoid unnecessary allocations in Update, FixedUpdate, or LateUpdate.

Examples:

- LINQ
- new collections
- boxing
- avoidable string allocations

Only optimize beyond this when profiling indicates a bottleneck.

Prefer object pooling for frequently spawned objects.

---

# Coroutines / Async

Prefer coroutines for gameplay flows.

Only introduce async/await or UniTask if the project already uses them.

---

# Code Style

Prefer:

- early returns
- small methods
- descriptive names
- low nesting
- self-documenting code

Avoid:

- empty Unity event methods
- unnecessary regions
- unused usings
- excessive comments that restate code

Comments should explain "why", not "what".

---

# File Organization

Prefer organizing by feature rather than by type when practical.

Example:

Assets/
    Gameplay/
        Combat/
            CombatController.cs
            DamageSystem.cs
            DamageConfig.asset
        Inventory/
        Crafting/

rather than:

Scripts/
    Managers/
    Controllers/
    Utils/

Keep related assets together.

Avoid dumping unrelated scripts into generic folders like Helpers or Managers.

---

# Code Structure

Aim for:

One class = one responsibility.

Large classes should usually be split before exceeding roughly 300-500 lines if distinct responsibilities emerge.

Prefer composition over inheritance.

Avoid "God objects."

---

# API Design

Public APIs should be:

- minimal
- explicit
- difficult to misuse

Hide implementation details whenever possible.

Favor immutable data where practical.

---

# Refactoring

When modifying existing code:

- leave it cleaner than you found it
- reduce duplication
- remove dead code
- improve naming when safe

Do not perform unrelated refactors unless requested.

---

# Feature Tracking

FEATURES.md in the project root documents implemented gameplay features.

Whenever gameplay functionality changes:

- update FEATURES.md
- describe user-facing behavior
- avoid technical implementation details

---

# Setup Tracking

SETUP.md in the project root documents how every system is wired into the scene —
GameObject hierarchy, required components, serialized references, and which
ScriptableObject assets each system needs.

Whenever a change affects how a system is wired in the scene:

- update SETUP.md in the same change
- this includes: adding/renaming/moving a MonoBehaviour that scenes must reference, changing which fields need to be assigned in the Inspector, changing a required GameObject hierarchy or component combination, adding or renaming a required ScriptableObject asset
- describe the resulting hierarchy/wiring, not the implementation behind it — that belongs in code comments or FEATURES.md
- prefer verifying against the actual scene (search GameObject/component names) over assuming wiring from the C# side alone

---

# Decision Making

If a rule conflicts with a better architectural solution:

Prioritize:

correctness → maintainability → architecture → project consistency → optimization.

Do not follow rules mechanically.

Explain significant architectural decisions when introducing new systems.

