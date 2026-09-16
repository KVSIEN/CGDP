# CGDP

Unity 6 (URP) first/third-person shooter sandbox: movement, weapons with procedural
generation, melee, grenades, abilities, status effects, hitboxes, enemy AI, audio and a
runtime-built HUD.

- [FEATURES.md](FEATURES.md) — what the game does
- [SETUP.md](SETUP.md) — how every system is wired into a scene
- [TASKS.md](TASKS.md) — open work
- [CLAUDE.md](CLAUDE.md) — coding guidelines

Open `Assets/_Project/Scenes/Sandbox.unity` to play.

## Project layout

```
Assets/
  _Project/                 everything owned by this project
    Art/                    Animations, Fonts, Materials, PhysicsMaterials, Shaders, Textures
    Audio/                  Music, SFX
    Data/                   ScriptableObject assets, one folder per feature
      Abilities/  Audio/  Combat/  Enemies/  Input/  Player/  UI/  Weapons/
      (generic starting points are named Default<Type>, e.g. DefaultEnemyData)
    Prefabs/                Characters, Environment, Pickups, UI, VFX, Weapons
    Scenes/                 Sandbox.unity (+ its baked NavMesh folder)
    Scripts/                runtime code, one folder per feature (CGD.Runtime assembly)
      Abilities/            ability assets and the player's ability slots
      Audio/                audio pool, sound banks, surface lookup
      Combat/               Damage/, Health/ (HealthManager, Hitbox), StatusEffects/, Projectile, Stunnable, Noise
      Core/                 shared utilities (cooldowns, ranges, settings save, Culling/)
      Enemies/              enemy components, perception, and AI/ (state classes)
      Input/                PlayerInputHandler and binding settings
      Interaction/          IInteractable, doors, switches, Pickups/
      Player/               movement, camera, health, lifecycle, player audio
      UI/                   Common/ (UIFactory), HUD/, Menus/, World/ (popups, enemy bars)
      Weapons/              Ranged/, FireBehaviors/, Generation/, Melee/, Throwables/
    Settings/               URP assets, volume profiles, project-wide input actions
  ThirdParty/               imported asset packs
  TextMesh Pro/             TMP essentials
```

## Conventions

- **Namespaces** follow the top-level script folder: `CGD.Combat`, `CGD.Weapons`, `CGD.UI`, …
  All runtime scripts compile into `CGD.Runtime` (`Scripts/CGD.Runtime.asmdef`).
- **Data assets** are named `<Name><Type>` (`PistolCategory`, `HitscanFireBehavior`,
  `TargetDummyEnemyData`) and created from `Create > CGD > <Feature> > …`.
- **Moving files:** do it inside Unity, or move each file together with its `.meta` while
  the editor is closed — otherwise scene and prefab references break.
