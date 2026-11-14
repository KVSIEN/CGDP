# Scene Setup

How to wire each system into a scene: what goes where, and what to assign.
`Assets/Project/Scenes/Sandbox.unity` is the reference scene.

**Conventions**

- `?` after a component = optional. Everything optional works (silently) when left out.
- Assets live in `Assets/Project/Data/<Feature>/` — see [Assets](#assets).
- HUD elements build their own UI at runtime. Place an empty `RectTransform` with the component; nothing else.
- "Player" below means the Player root GameObject.

---

## Checklist

Build a new playable scene in this order:

1. **Scene root** — GameManager, GameFlow, RespawnPoint, EventSystem, light, volume.
2. **Level** — geometry, a baked **NavMesh Surface** — or a
   [generated level](#generated-levels) built from a map graph.
3. **Player** — the rig below, then its wiring.
4. **HUD** — canvas and the elements you want.
5. **Enemies, pickups, interactables.**
6. **Optional systems** — map, quests, feedback, stealth, camera effects.

**Missing from Sandbox** (add when needed): `TimelineAbilityRunner`, animation and
ragdoll components (no rigged models yet), `FeedbackPlayer`, the world map, stealth,
crafting stations, `GameDirector` and `LevelBuilder`. The firing-range dummies have no
`EnemyAI` (they stand still). `MapTest` adds the map, feedback, director, expedition and
generated-level systems on top of the Sandbox set. `AITest` is the behavior tree arena (see [AI Test Scene](#ai-test-scene)).

---

## Scene Root

```
GameManager        [AudioPool, AudioMixerController?, PoolPrewarmer?]
GameFlow           [GameFlow, GameTime?]        ← own root object, nothing else on it
RespawnPoint       (empty Transform)
WorldMap           [WorldMapArea]?              ← see Map & Minimap
EventSystem        [EventSystem, InputSystemUIInputModule]
Directional Light
Global Volume
```

| Component | Assign | Notes |
|---|---|---|
| **AudioPool** | — | Raise `_initialSize` (16) if sounds cut out. |
| **AudioMixerController**? | `_settings` = `Audio/DefaultAudioMixerSettings` | Drives the AudioMixer and its snapshots — see [Audio Mixer](#audio-mixer). Without it sounds scale their own volume and mix states do nothing. |
| **PoolPrewarmer**? | `_entries` = prefab + count | Pre-spawns pooled objects. Pools grow anyway. |
| **GameFlow** | `_settings`? = `Flow/GameFlowSettings` (its `_difficulty` = `Difficulty/DefaultDifficultyTable`) | See [Game Flow](#game-flow--time). |
| **GameTime**? | `_settings`? = `Timing/GameTimeSettings` | Created automatically at 60 ticks/s if absent. |

**Pooling rules**

- Projectiles, grenades, pickups and damage numbers are pooled automatically — nothing to place.
- Remove pooled objects with `PrefabPool.Release`, never `Destroy`.
- Never add `PooledInstance` by hand. One-shot VFX prefabs get a `PooledLifetime`.

---

## Player

```
Player             [PlayerInputHandler, PlayerHealth, PlayerMovement, PlayerDodge,
                    PlayerMantle, PlayerInteraction, PlayerInventory, PlayerAbilities,
                    WeaponController, PlayerWeaponLoadout, PlayerPerks, MeleeController,
                    ThrowableController, PlayerLifecycle, PlayerFootsteps, PlayerAudio,
                    Stunnable, StatusEffectController, Rigidbody, CapsuleCollider]
                   + optional: MeterSet, TimelineAbilityRunner, CharacterStats,
                     QuestTracker, PlayerLockOn, MapRevealer, FeedbackPlayer,
                     CombatFeedback, QuestFeedback, PlayerEquipment,
                     PlayerItemSlots, DevCommands, Stealthable,
                     GameplayMixStates
  CameraRig
    Main Camera    [Camera, PlayerCamera, CameraEffectsController?]
      WeaponRig    [WeaponVisuals]
        Weapon     [WeaponAimPose]   ← gun mesh, no colliders
          Muzzle   (empty Transform — shots fire from here)
  Player Body      (visual mesh)
  Head Anchor      (empty Transform at eye height)
```

> Only **one** `PlayerInputHandler`. Weapons, grenades and abilities must stay on (or under)
> the Player — that's how their damage gets the Player's team.

### Movement & input

| Component | Assign |
|---|---|
| **PlayerInputHandler** | `_bindings` = `Input/InputBindingSettings` |
| **PlayerMovement** | `_settings` = `Player/PlayerMovementSettings`, `_cameraTransform` = Main Camera, `_playerMesh` = Player Body · owns the body and its settings: PlayerMantle and PlayerAbilities use its settings and camera, so they have none of their own to assign; needs PlayerDodge and PlayerMantle (added automatically) |
| **PlayerDodge** | `_definition` = a dodge from `Player/Dodges/` (default `SidestepRollDodge`), `_doubleTapCrouch` (also dodge by double-tapping Crouch while moving) |
| **PlayerMantle** | nothing — uses PlayerMovement's settings and camera |
| **PlayerCamera** (Main Camera) | `_input`, `_movement` = Player · `_playerBody` = Player Body · `_headAnchor` = Head Anchor · `_settings` = `Player/PlayerCameraSettings` · `_camera` = its own Camera · `_firstPersonHideRenderers` = Player Body renderers |

### Combat

| Component | Assign | Notes |
|---|---|---|
| **WeaponController** | `_input` = Player, `_camera` = PlayerCamera, `_crosshair` = HUD Crosshair, `_muzzle` = Muzzle, `_visuals` = WeaponRig, `_cameraEffects`? = Main Camera | Fires whatever the loadout equips. Reads `PlayerDodge` from the same object (optional) for the swap-dodge draw cancel. |
| **PlayerWeaponLoadout** | `_startingWeapons`? = `WeaponData` assets, `_wheel`? = HUD SlotWheel | Four slots, each a main weapon + an offhand (shield, artifact, one-handed melee weapon); the same item can sit in several slots. Empty slots fill from pickups; offhands and extra mains are assigned in the Character window. Holding 1–4 opens the weapon wheel (other slots + spare weapons in the pack). Raises `Changed` for anything in the loadout, which PlayerOffhand and CharacterPanel follow. |
| **PlayerPerks** | `_camera`? = PlayerCamera (for the Aim Start trigger) | Sets off the triggered perks of the weapon in hand and of worn armor. Reads `PlayerWeaponLoadout`, `WeaponController` (reloads), `MeleeController` (parries), `PlayerDodge`, `PlayerAbilities`, `PlayerItemSlots`, `PlayerEquipment` (worn armor), `PlayerHealth` (damage taken, heals), `MeterSet` from the same object and `CharacterStats` from it or a parent (buffs need it; each other part is optional). Perks come from each gear definition's `_perkPool`; armor passives are applied by `PlayerEquipment`. |
| **Reflector** | `_aim` = Main Camera (PlayerCamera's transform) | Catches hits for reflect abilities and parry reflects. Must be on the **same object as PlayerHealth** (it hooks into damage the way the melee guard does). Beam/slash reflects play their timeline from here — no `TimelineAbilityRunner` needed. |
| **PlayerOffhand** (Player) | `_camera` = PlayerCamera | Runs the active slot's offhand: hands it to the MeleeController and, for an artifact, applies its stats and runs its aim-input use. Needs `PlayerWeaponLoadout` (which stores the offhands) and `PlayerInputHandler` on the same object; reads `MeleeController` (hands it the offhand), `PlayerAbilities` (a tome casts through its context and events — without it spells can't be cast), `Reflector` (a ward — without it wards can't be raised), `MeterSet` (costs and drains), `CombatActions` (Surge charging), `HealthManager` (revive resets cooldowns), `PlayerMovement`/`PlayerDodge` (can-act checks) and `CharacterStats` (main-hand penalty, artifact stats) from the same object or a parent. It sets `PlayerCamera.AimTakenByOffhand`. |
| **MeleeController** | `_camera` = PlayerCamera, `_data` = `Weapons/Melee/DefaultMeleeWeaponData` (fists), `_weaponSocket`? = the weapon's transform (hand or weapon bone; only Swing Hit events set to *Anchor* Socket use it, others follow the aim), `_meleeParryRange` (4 m: parries beyond it are deflected shots), `_rangedParryReflect` = `Combat/Reflect/DeflectReflect` (needs the Reflector) | Quick melee (the Melee key bashes with the gun's `QuickMelee`, a Tactical Knife perk's knife, the melee weapon's guard bash, or fists) and the equipped melee weapon. `PlayerWeaponLoadout` hands it every weapon put in hand, guns included. Must be on the **same object as PlayerHealth**: it parries (bash) and blocks (guard) hits as an `IDamageInterceptor`, which HealthManager only looks for on its own object. Stamina costs need a `MeterSet` with the category's stamina meter on that object too (without one, melee is free). Combo weaving reads `PlayerDodge` and `PlayerAbilities` from the same object (each optional). |
| **ThrowableController** | `_camera` = PlayerCamera, `_preview` = the Player's `TrajectoryPreview` | Throws the grenade an item slot readies, charging with the attack button. Grenades are items (`Items/Throwables/`). |
| **TrajectoryPreview** (Player) | `_material` = `Materials/VFX/ThrowPreview` (unlit) | Draws the aim arc and blast ring; builds its line renderers itself. |
| **PlayerRestraint** (Player prefab) | `_tapsRequired`, `_decayPerSecond`, `_maxSeconds` = the defaults for `Restrain()` | Placeholder for being held in place: a trap or grab calls `Restrain()`; until the player breaks free (tap Interact) or it times out, movement and dodging are blocked and interacting is off. Needs `PlayerInputHandler` and `Stunnable` on the same object. Dev console: `/restrain [taps]` and `/free`. |
| **PlayerAbilities** | `_health` = PlayerHealth, `_slots` = up to 4 ability assets | Aims with PlayerMovement's camera. Abilities act through the player's own components, found once: Dash needs PlayerDodge, Timeline abilities a TimelineAbilityRunner, Reflect a Reflector, Stealth a Stealthable, Stat Buff CharacterStats. |
| **TimelineAbilityRunner**? | — | Needed for `TimelineAbility` assets. |
| **WeaponVisuals** (WeaponRig) | — | Kick and sway are tuned on the component and the `WeaponData`. |
| **WeaponAimPose** (Weapon) | `_camera` = PlayerCamera | Scene pose = hip pose. Set `_adsPosition`/`_adsRotation` for aiming. |

> The gun mesh must have **no colliders** — they block shots while aiming.

### Health, inventory & life

| Component | Assign | Notes |
|---|---|---|
| **PlayerHealth** | health, armor, shield values; `_hitboxProfile`? | |
| **PlayerInventory** | `_startingStacks`? = item + count (e.g. `StandardMunitions` × 60) | No starting ammo unless listed. |
| **PlayerInteraction** | `_forwardReference` = Main Camera | |
| **PlayerLifecycle** | `_health`, `_movement`, `_abilities`, `_input` = Player · `_hud` = HUD · `_spawnPoint` = RespawnPoint · `_deathScreen`? | Tick `_gameOverOnDeath` to end the run instead of respawning. |
| **PlayerFootsteps** | `_surfaces` = `Audio/DefaultSurfaceDatabase`, `_animationCues`? = the body's AnimationEventRelay (steps then follow the clips' `Footstep` cues instead of the timed intervals) | A collider can carry an optional **SurfaceTag** (`_walk` / `_sprint` / `_crouch` = `SoundBank`s) to override the database's footsteps for that surface. |
| **PlayerAudio** | `_health` = PlayerHealth, `_hurtSound`?, `_deathSound`? | |
| **MeterSet** | `_definitions` = meters from `Data/Meters/` (the Player prefab has `StaminaMeter`) | Needed before any stamina/mana cost can be paid; melee swings and blocks spend stamina from it. |
| **CombatActions** | — | Counts the player's combat actions (hits, kills, parries) that fill ability Surge. Parries count when a `MeleeController` is on the same object. Without it Surge abilities never fill. |

**Meter costs** — set `SprintCost` on `PlayerMovementSettings`, `_cost` on a dodge, or `Cost` on an ability.
A cost for a meter the Player doesn't have is **blocked**, not free.

---

## HUD

```
HUD                [Canvas, CanvasScaler (Scale With Screen Size, 1920 × 1080, match 0.5), GraphicRaycaster, HUDManager]
  ScreenFlash      [ScreenFlashHUD]?         ← keep near the top (draws underneath)
  HitEffect        [HitEffect]
  DamageIndicator  [DamageIndicatorHUD]?
  Struggle         [StruggleHUD]?
  Crosshair        [CrosshairHUD]
  Stealth          [StealthHUD]?
  HitMarker        [HitMarkerHUD]?
  Stats            [StatsHUD]
  Weapon           [WeaponHUD]
  Abilities        [AbilityHUD]
  Dodge            [DodgeHUD]
  Offhand          [OffhandHUD]
  StatusEffects    [StatusEffectHUD]
  Meters           [MeterHUD]   (in the HUD prefab)
  Velocity         [VelocityHUD]
  ItemSlots        [ItemSlotsHUD]?
  Minimap          [MinimapHUD]?
  Quests           [QuestHUD]?
  Notifications    [NotificationHUD]?
  Objective        [ObjectiveHUD]
  Interact         [InteractHUD]
  WeaponPickup     [WeaponPickupHUD, CanvasGroup]
  Inventory        [InventoryHUD, CanvasGroup]
  ItemInventory    [ItemInventoryHUD, CanvasGroup]
  WorldMap         [WorldMapHUD, CanvasGroup]?   ← keep near the bottom (draws on top)
  Character        [CharacterPanel, CanvasGroup]?
  Crafting         [CraftingPanel, CanvasGroup]?
  Dialogue         [DialoguePanel, CanvasGroup]?
  Shop             [ShopPanel, CanvasGroup]?
  DevConsole       [DevConsolePanel, CanvasGroup]?
  SettingsMenu     [SettingsMenu]
```

Later children draw on top. `HUDManager` finds every element by itself.

**Key bindings:** every element in the HUD prefab has `_bindings` = `Data/Input/InputBindingSettings` (the same asset the Player's `PlayerInputHandler` uses). Elements that show a key hint (Interact, Dodge, Offhand, ItemSlots, Inventory) read it and redraw when it changes; left empty, hints show —.

**Theme:** every element in the HUD prefab has `_theme` = `Data/UI/HUDTheme` (`Create › CGD › UI › HUD Theme`). It holds every colour the HUD draws with, named by meaning (Panel, Window, TextPrimary, Health, Warning, Danger, Accent…) — restyle the HUD there. An element with no theme assigned uses the built-in defaults, which are the asset's starting values. Elements added in a scene instead of the prefab (Minimap, WorldMap, ExpeditionScreen) set their own colours and don't follow the theme yet. Fonts are not part of it: set the default font asset in TMP Settings.

**Skins:** any element listed below can swap its built-in layout for your own prefab: drag a prefab (its root must be a `RectTransform`) into that element's `_skin` in the HUD prefab. The element instantiates it stretched to fill itself, adopts the prefab root's width and height as its size (a root that stretches to its parent falls back to the built-in size), and then only drives the parts it finds **by name** — anywhere in the hierarchy, first match wins. Everything else about the parts (size, sprite, font, anchors, extra decoration) is yours. If a required part is missing, or has the wrong component, the element logs a warning, discards the skin and builds its built-in layout. Leave `_skin` empty to keep the built-in look.

| Element | Required parts (name → component) | Driven by the element |
|---|---|---|
| **StatsHUD** | `HealthFill` → Image, `HealthText` → TextMeshProUGUI | Fill amount; the fill's colour blends from its own colour (full health) to the theme's *HealthLow*; text |
| **VelocityHUD** | `SpeedText` → TextMeshProUGUI | Text |
| **WeaponHUD** | `WeaponName`, `MagCount`, `Reserve`, `ReloadLabel` → TextMeshProUGUI | Texts; the reload label shows only while reloading; `MagCount` and `Reserve` turn the theme's *Danger* when empty and `MagCount` *Warning* while reloading — otherwise they keep your colours |
| **DodgeHUD** | `Background`, `Cooldown` → Image; `Key`, `Name` → TextMeshProUGUI | `Background` is tinted by state (ready, cooling down, dodging, committed) — use a white sprite; `Cooldown` drains; `Key` shows the bound key; `Name` the dodge move |
| **OffhandHUD** | `Background`, `Cooldown` → Image; `Key`, `Name`, `Charges` → TextMeshProUGUI | As Dodge; the whole skin shows only while an artifact is in use |

Bars and overlays (`HealthFill`, `Cooldown`) are shown either way you build them: an Image with *Type = Filled* (set the fill method and origin; horizontal from the left for bars, vertical from the bottom for overlays; it needs a sprite), or a plain Image anchored to stretch its parent with zero offsets, whose right or top edge the element pulls in. 

**Repeated elements** — for these, the skin is **one item**, and the element makes as many as it needs, places them itself and sizes its panel from the item's size (width and height of the skin root; the layout fields next to `_skin` still set gaps and anchoring). Parts marked *optional* may be left out. A skin here has no panel behind the items, so each item brings its own background.

| Element | The skin is one… | Required parts | Optional parts | Driven by the element |
|---|---|---|---|---|
| **AbilityHUD** | ability slot (four are made, centred, spaced by *Slot Gap*) | `Background`, `Cooldown` → Image; `Charges` → TextMeshProUGUI | `Surge`, `SurgeFill` → Image | `Background` is tinted by the ability's colour (white sprite); `Cooldown` drains/fills vertically from the bottom; `Surge` shows only for a Surge ability and `SurgeFill` fills horizontally |
| **StatusEffectHUD** | effect tile (up to *Max Tiles*, left to right, spaced by *Tile Gap*) | `Icon` → Image | `Remaining` → Image; `Name`, `Stacks` → TextMeshProUGUI | `Icon` gets the effect's sprite (or its colour when it has none); `Remaining` covers the elapsed part from the top |
| **ItemSlotsHUD** | item row (four, top to bottom) | `Background`, `Fill` → Image; `Label` → TextMeshProUGUI | — | `Background` is tinted while its throwable is in hand; `Fill` shows use progress or cooldown from the left |
| **MeterHUD** | meter row (one per meter, top to bottom, spaced by *Row Spacing*) | `Fill` → Image; `Label` → TextMeshProUGUI | — | `Fill` is filled from the left and tinted by the meter's colour (grey while exhausted) — use a white sprite |
| **NotificationHUD** | message row (*Max Visible*, newest on top) | `Text` → TextMeshProUGUI | — | `Text` takes the message and its style colour; rows are stacked by *Row Height* and *Row Gap*, so set those to the skin's height |
| **SlotWheelHUD** | wheel option (up to 12 around the ring) | `Background` → Image; `Label` → TextMeshProUGUI | — | `Background` is tinted when highlighted; the wheel's title in the middle stays built-in |

The remaining HUD elements (crosshair, quests, objective, interact prompt, hit marker, stealth tag, inventory and the other windows) still have only the built-in layout.

| Element | Assign | Notes |
|---|---|---|
| **CrosshairHUD** | `_playerCamera` = PlayerCamera | Draws the player's crosshair style from `GameSettings` (edited on the Settings menu's Crosshair tab). No asset to assign. |
| **DamageIndicatorHUD** | `_playerHealth` = PlayerHealth, `_playerCamera` = PlayerCamera (set on the HUD instance in Sandbox and MapTest) | Arrows toward whoever hurt the player. Listens to `CombatEvents`; stretch-anchored, builds its own arrows. Optional `_sprite` = your own arrow art (pointing up); `_radius`, `_size`, `_duration`, `_maxIndicators` tune it. Off switch: Settings › Accessibility. |
| **StruggleHUD** | `_restraint` = PlayerRestraint (set on the HUD instance in Sandbox and MapTest) | Placeholder prompt below the crosshair while the player is held: the label, "MASH <Interact key>", and a bar that fills per tap. Shows only while restrained. |
| **StatsHUD**, **HitEffect** | `_playerHealth` = PlayerHealth | HitEffect = damage flash + low-health vignette. |
| **WeaponHUD** | `_weapon` = WeaponController | |
| **AbilityHUD** | `_abilities` = PlayerAbilities | |
| **DodgeHUD** | `_dodge` = PlayerDodge | |
| **OffhandHUD** | `_offhand` = PlayerOffhand | The held artifact's slot, left of the ability bar (shown only while an artifact is in use). Sandbox and MapTest are wired. |
| **StatusEffectHUD** | `_target` = Player's StatusEffectController | |
| **MeterHUD** | `_meters` = Player's MeterSet (set on the HUD instance in Sandbox and MapTest) | Hides itself without meters. |
| **VelocityHUD** | `_movement` = PlayerMovement | |
| **InteractHUD** | `_interaction` = PlayerInteraction | |
| **WeaponPickupHUD** | `_interaction` = PlayerInteraction | Shows when aiming at a weapon pickup. |
| **InventoryHUD** | `_input` = Player, `_loadout` = PlayerWeaponLoadout | Opens with I. |
| **ItemInventoryHUD** | `_input` = Player, `_inventory` = PlayerInventory | Opens with I. `_inventory` is required. |
| **QuestHUD** | `_tracker` = QuestTracker | See [Quests](#quests). |
| **MinimapHUD** | `_area` = WorldMapArea, `_viewer` = Main Camera | See [Map](#map--minimap). |
| **WorldMapHUD** | `_input` = Player, `_area` = WorldMapArea, `_viewer` = Main Camera | Opens with M. |
| **NotificationHUD**, **ScreenFlashHUD** | — | Driven by `FeedbackPlayer` (its `_notifications` and `_screenFlash` are set on the Player instance in each scene). Both are in the HUD prefab. |
| **ObjectiveHUD** | — | Top-centre panel for the Siege/Gauntlet in progress and the boss fight (title, detail, countdown, objective or boss health bar). Driven by `ObjectiveFeed`; already in the HUD prefab. |
| **HitMarkerHUD** | — | Driven by `CombatFeedback` through `HitMarkerFeed`; no reference to set. |
| **StealthHUD** | `_stealth` = Player's Stealthable (set on the HUD instance in Sandbox and MapTest) | HIDDEN / REVEALED / SPOTTED under the crosshair. In the HUD prefab. |

Damage numbers and enemy health bars build themselves — don't place them under the HUD. `EnemyHealthBar` also tunes its damage band: `_trailColor`, `_trailHold` (seconds before it shrinks), `_trailSpeed` and `_punch` (swell on a hit).

### Settings Menu

| Assign | Notes |
|---|---|
| `_camera` = PlayerCamera, `_input` = Player, `_bindings` = `InputBindingSettings`, `_hud` = HUD | Stretch its RectTransform to fill the screen. Escape opens it; it doubles as the pause menu when a `GameFlow` exists. |

The Audio, Video, Crosshair and Accessibility tabs need no wiring. They apply to every scene:
- Volume works through `AudioPool` (Master scales everything). Set each `SoundBank`'s `_category` (Effects / Music / Interface) to pick which slider it follows. With an `AudioMixerController` in the scene, the sliders drive the mixer instead — see [Audio Mixer](#audio-mixer).
- Field of view replaces `PlayerCamera._baseFOV`. `_sprintFOV` keeps its authored widening on top.
- Camera shake scales everything `CameraEffectsController` adds. Screen flashes scale `ScreenFlashHUD` and `HitEffect`.

---

## Audio Mixer

Optional. Without it every sound scales its own volume by the settings sliders and plays unrouted, which is how the game works today. With it the sliders drive the mixer, and snapshots can muffle or duck the mix for pause, low health and stun.

**1. Create the mixer** (`Create › Audio Mixer`, e.g. `Audio/GameMixer`):

```
Master
  Effects     ← Attenuation exposed as  EffectsVolume
  Music       ← Attenuation exposed as  MusicVolume
  Interface   ← Attenuation exposed as  InterfaceVolume
```

Expose a parameter by right-clicking the group's *Volume* in the Inspector › *Expose*, then rename it in the mixer's *Exposed Parameters* list. Sub-groups (Footsteps, Weapons, Voice…) can sit under a category group; a `SoundBank` with its own `_mixerGroup` plays through it and still follows its category's slider.

**2. Create snapshots** in the mixer: keep the default (`Snapshot`) as the resting mix, then add one per situation, e.g. `Paused` (low-pass on Effects, Music dipped), `LowHealth` (low-pass + Music ducked), `Stunned` (heavy low-pass, ringing). Snapshots must **not** change the three exposed volumes — the settings menu owns those; shape the mix with other effects or sub-groups.

**3. Create the assets:**

| Asset | Fields |
|---|---|
| `Audio/DefaultAudioMixerSettings` (`CGD › Audio › Mixer Settings`) | `_mixer`, `_defaultSnapshot`, `_effectsGroup` / `_musicGroup` / `_interfaceGroup` and their parameter names (defaults match the layout above) |
| `Audio/PausedMixState`, `LowHealthMixState`, `StunnedMixState` (`CGD › Audio › Mix State`) | `_snapshot`, `_priority` (suggested 100 / 10 / 20 — the highest active one wins), `_fadeTime` |

**4. Wire the scene:** `AudioMixerController` on GameManager (`_settings` as above), and **GameplayMixStates** on the Player (`_paused`, `_lowHealth`, `_stunned`, `_lowHealthThreshold`). It reads the Player's `HealthManager` and `PlayerMovement`, and `GameFlow` for pausing; any state left empty is ignored. Other systems can request a state from code with `AudioMixerController.Push(state)` and `Release()` the returned request.

`DirectorMusic` and `AudioPool` pick their group from the sound's category automatically.

---

## Game Flow & Time

- **One `GameFlow` per scene**, on its own root object. It survives scene loads; later copies delete their **whole GameObject**.
- Scenes loaded by name must be in the Build Profile's scene list.
- UI buttons call a **GameFlowCommands** component (Resume, RestartLevel, LoadMainMenu, StartGame, Quit…) — never `GameFlow` directly.
- **GameStateView** shows a panel only in chosen states: `_target` = the panel, `_visibleIn` = e.g. `Paused`. Put it on an always-active object, not on the panel.
- **Time:** `GameTime` owns `Time.timeScale`. Pause or slow time through `GameTime.Instance.Clock`, never by setting `Time.timeScale`.
- **Expeditions:** the `GameFlow` prefab also carries **ExpeditionSession**, which keeps the ship's hold and the packed kit across scene loads and saves them to disk. A scene without it still plays; its runs just don't carry over.
  - `_catalog` = `Items/DefaultItemCatalog` (without one nothing is saved) · `_saveInEditor` = off (on: the editor saves too, to its own `ship-editor.json`) · `_fileName` = `ship.json` (in `Application.persistentDataPath`).
  - `DefaultItemCatalog` lists every item definition in the project and is kept up to date by the editor on every asset change — never edit it by hand. If it ever looks stale, run **CGD › Items › Sync Item Catalog**. Only one catalog asset is needed.

---

## Enemies

```
Enemy              [NavMeshAgent, EnemyAI, EnemyHealth, EnemyHealthBar, Stunnable,
                    StatusEffectController, EnemyAwarenessIndicator, EnemyStateVisuals?,
                    EnemyAudio?, LootDropper?, DespawnOnDeath?, CharacterStats?, MapMarker?]
  Head / Body / Limbs   [Collider, Hitbox]?
```

| Component | Assign | Notes |
|---|---|---|
| **EnemyAI** | `_data` = an `EnemyData`, `_waypoints`?, `_targetMask`, `_obstacleMask` | Needs a NavMesh (baked, or `RuntimeNavMesh`). Finds targets by team. Runs the data's behavior tree if it has one. Where it starts is its **post**: without `_waypoints` it guards there, and it returns there after searches. |
| **EnemyHealth** | `_data` = same `EnemyData`, `_healthBar` = its EnemyHealthBar, `_hitboxProfile`? | |
| **EnemyAwarenessIndicator** | — | The ?/! over the enemy (on every enemy prefab). Builds its own world-space canvas; colours, height and how long the ! shows are on the component. |
| **EnemyStateVisuals**? | `_renderers` | Tints by AI state. |
| **EnemyAudio**? | `_hurtSound`, `_deathSound` | |
| **LootDropper**? | `_table`, `_itemPickupPrefab`, `_weaponPickupPrefab` | Tick `_dropOnDeath`. |
| **DespawnOnDeath**? | `_delay` | Returns pooled enemies to the pool. |
| **RespawnOnDeath**? | `_delay` (4 s) | Alternative to DespawnOnDeath for things that must keep being shot at: hides the body (all child objects) while dead, then revives in place. Used by the firing-range dummies. |
| **CharacterStats**? | `_presets` = e.g. `Stats/HardDifficultyModifierPreset` | Tougher enemy. |
| **StatusEffectController** | `_immunities`? | Without it, status effects are ignored. |

**Enemy type** is all in its `EnemyData`: team, health, speeds, sight range and angle, hearing range (how far a walking hostile's footsteps carry), melee or ranged, display name (used in kill messages), its **Senses** (a `PerceptionProfile`) and optionally its behavior tree. Ranged types can set *Shot Projectile* (a prefab with a `Projectile`, e.g. `Prefabs/Weapons/TechBolt`) and *Shot Speed* to fire visible rounds instead of hitscan.

**Group & Reactions** (on `EnemyData`): `MaxMeleeAttackers` (2 — melee enemies of this type that may fight one target at once), `MaxShooters` (3), `StaggerThreshold` / `StaggerDuration` / `StaggerCooldown` (a hit of 25% of max health staggers for 0.4 s, at most every 2.5 s), `RaisesAlarm` (runs for an `AlarmPanel` within 25 m of its post on first spotting a hostile). Caution, guarding, lost-interest / victory pauses and morale are tuned on the `PerceptionProfile`.

**Alarm panels** — any object with a collider and an **AlarmPanel** (`_radius` 60 m, `_useTime`, `_rearmTime`, `_disableTime` for the player's hold-Interact, `_team`, `_screen`? = a renderer it tints green / red / dark). Enemies only use panels within 25 m of their post. PlayerInteraction finds it like any interactable.

**Cover** — add a **CoverMap** (one per scene, any object; it's on its own `CoverMap` object in AITest and MapTest): `_spacing` (1.25 m between spots), `_lowHeight` / `_highHeight` (0.8 / 1.6 m: how tall an obstacle must be for low / full cover), `_obstacleMask` = what counts as an obstacle (Default). It scans the NavMesh the first time an enemy asks, so baked and runtime NavMeshes both work; call `Rebuild()` if the NavMesh changes later. Without a CoverMap enemies simply never use cover. Enemies learn cover around their spawn point and `_waypoints`, so give patrols routes through the area they should defend.

**Squads** — put an **EnemySquad** on a parent object; every `EnemyAI` under it joins in `Awake` (spawners call `Join(enemy)` for ones spawned later, `Leave` to remove). `_radioRange` (40 m: callouts reach members through walls; 0 = shouting only), `_leaderLossMorale` (0.25). The first member is the leader. Roles no longer come from the squad: squad members always form an encounter in a fight, which hands out roles from their doctrine (*Encounter* below; the plain doctrine's table is the old rule: nearest ranged suppresses, two nearest melee go in, the rest flank). Factions whose doctrine coordinates form encounters without any squad, so generated levels need nothing placed. (The AITest squads still carry `_suppressors`, `_assaulters` and `_reassignEvery` from before; they are unused.)

**Grenades & abilities** — no extra components: tree nodes **Throw Grenade** (assign a `GrenadeData`, e.g. `Weapons/Throwables/DefaultGrenadeData`; its prefab must be set) and **Use Ability** (assign an `Ability` asset) run on any `EnemyAI`. Abilities aim from an `AbilityAim` child the enemy creates itself at 1.5 m; player-only abilities (that need weapons, stamina, etc.) just fail. The enemy's Animator gets a `Throw` trigger when it throws (add it to the controller to animate it).

**Senses** — `Enemies/Perception/`: `DefaultPerceptionProfile` (Void and the default enemy), `WaryPerceptionProfile` (Tech: slow to notice, long memory, thorough searches), `SkittishPerceptionProfile` (Bio: quick to notice and to give up). Create more with `Create › CGD › Enemies › Perception Profile`; an empty slot uses built-in defaults. A profile tunes how fast sight and sound turn into awareness, memory and guessing (*Prediction*: seconds ahead it guesses a lost target kept going; *Strafe / Backpedal Prediction*: the share of that guess for a target seen strafing or backing away; a target seen standing still is expected to stay put), callout range, and the notice / search / look-around timings. Footstep loudness and how much crouching or standing still hides you are the player's side, on `StealthSettings` (*Noticeability*). Enemies only hear footsteps of characters with a `Stealthable`; others count as moderately loud.

**Kit** — `EnemyData` › *Kit*: `Grenade` (a `GrenadeData`), `Signature`, `Escape`, `Summon` (each an `Ability`), all optional. Throw Grenade nodes with no grenade of their own throw `Grenade`; Use Ability and Ability Ready nodes set to the *Signature* / *Escape* / *Summon* slot (instead of *Own*) use that ability — which is how every behavior in `Behaviors/` stays faction-neutral (`FactionTreeTests` fails if one names an asset itself). The AITest agents carry: Bio — Signature `BioSnareAbility`, Escape `BioAmbushAbility`; Tech — Grenade `TechGrenadeData` (what Flush Out throws), Signature `TechDischargeAbility`; Void — Signature `VoidRiftAbility`, Escape `VoidPhaseShiftAbility`, Summon `RaiseDeadAbility`. Bio's kit now also has Grenade `IncendiaryGrenadeData` (a stand-in for an acid glob: a burning pool), Summon `BioBroodAbility` and Support `BioRallyAbility`. Also *Boost* (Bio: `BioSurgeAbility`, Tech: `TechFortifyAbility`) and *Support* (Tech: `TechRepairAbility`; Support Ally nodes use it). Regular enemies' kits are empty. `Abilities/TechEmpAbility` (EMP Pulse, a `TargetedAbility`: `AimedAreaTargetSelector`, 6 Lightning damage, `Combat/StatusEffects/TechEmpStunEffect` 1 s + `TechDischargeSlowEffect`, 0.6 s cast, 10 s cooldown) is a Signature for hacker types; `Abilities/TechRepairAbility` (Field Repair: `FriendlyAreaTargetSelector`, heals 30, 0.8 s cast, 6 s cooldown) a Support. `Abilities/RaiseDeadAbility` is a `SummonAbility` (`Create › CGD › Abilities › Summon`): `MinionPrefab` = `Prefabs/Characters/Enemies/AITest/VoidMinion` — the minion must be on the caster's team, so give each faction its own copy with its own minion — `Count` 3, `Lifetime` 25 s, `MaxAlive` 4, 1.2 s cast, 18 s cooldown.

**Bush travel** — `EnemyData` › *Movement* › **Bush Detour** (0–2, default 0): when the enemy sneaks — Roam Bushes, Patrol, Investigate, Return To Post, Pursue Belief, and Prowl with *Prefer Bushes* — its route passes through bushes on the way (`ConcealmentZone`s of kind Bush that don't require crouching), the nearest one each time that brings it closer and keeps the walk within (1 + Bush Detour) × the straight line; rushes, dodges, flight and cover runs stay direct. Hazards still come first. The four Bio data assets (`ScavengerEnemyData`, `BioTier2/3EnemyData`, `AITest/BioTestEnemyData`) use 0.6; everyone else 0. Bushes need no setup beyond their `ConcealmentZone` (the AITest arena has eight).

**Faction doctrine** — `EnemyData` › *Doctrine* = a `FactionDoctrine` asset (`Create › CGD › Enemies › Faction Doctrine`), in `Enemies/Doctrine/`: `TechDoctrine`, `BioDoctrine`, `VoidDoctrine`. Every enemy of a faction points at its faction's doctrine: the tier 1–3 enemies (`MaintenanceBot`/`TechTier2`/`TechTier3`, `Scavenger`/`BioTier2`/`BioTier3`, `Husk`/`VoidTier2`/`VoidTier3`) and the AITest agents. Void's minions and echo, and `DefaultEnemyData`, have none (empty = a plain enemy, which behaves as before). It works whichever behavior tree the enemy runs, because it is read by the shared parts (morale, the post, the *Patrol* and *Investigate* nodes, the retreat step, the attack gate), so no tree needs editing. Ten groups, all optional scalings of the `PerceptionProfile`'s numbers:
- *Morale* — how much seeing an ally fall counts (and the extra per tier of rank between them), how much its own wounds count, how fast it recovers, what steadies it (kin close by, a weak target) or wears it down (being outnumbered, a broken shield), where its break and rally lines sit, and where it falls back to (*Cover*, *Kin* or *Den* = its post) and how fast.
- *Escalation* — how far its call carries, whether finding a fallen ally sends its kin to look, whether its morale breaking calls for help.
- *Idle* — a list of *Beats* (each: a gaze — *Scatter*, *Sweep*, *Fixate*, *Hold* or *Away* — for a duration range, a weight, an optional walk from its post, an optional bark such as *Ambient*) worked through in turn (*Cycle*) or by weight (*Weighted*) while it stands watch and at stops on its route; the chance it stops at each waypoint, how often it halts mid-route, patrol speed and pace drift, sweep speed, and how its beats change while it is on edge. No beats = random glances, no stops.
- *Encounter* — whether the faction forms encounters on its own (*Coordinates*; squad members always do), its *Succession* rule (*Rank*: tier then time in the fight; *Strength*: tier and health; *Depth*: tier then shield), its steady role table (*Roles*: slots of a `SquadRole`, a *Count* — 0 = everyone left — who qualifies, *Lead First*, and fit weights for closeness, standing off the target's facing, health, rank, cover and already holding the role) with a *Fallback Role*, and what happens when the lead falls: *On Lead Lost* (*Regroup* for *Regroup Seconds* on the *Regroup Roles* / *Regroup Fallback Role*), how long the successor counts as stepped up and the morale it regains. Encounters need at least two members to hand out roles. Trees read roles with *Has Squad Role* and the encounter on the blackboard (`Posture`, `IsLead`, `SteppedUp`).
- *Overseer* — the faction's own director: whether it oversees, menace rise/decay/max, whether and when it backs off (*Back Off At*, *Back Off Seconds*, *Resume Below*), hints (*Hint After Quiet*, *Hint Every*, *Hint Accuracy* in metres, *Hint Range*, *Hint Members*) and escalation (*Sharpen Per Escalation*, *Max Escalation*). Trees read `Menace` and `Escalation` from the blackboard, and the *Overseer Allows* node gates a branch on an escalation level.
- *Temperament* — *Aggression*, *Caution*, *Cunning* (1 = neutral), published on the blackboard after its situation moves them (regrouping: warier, less eager; stepped up: bolder). A *Score* decorator's *Bias Key* multiplies its score by one of them; Void's tree uses Caution on *Escape and reposition* and *Phase Shift* and Aggression on *Melee dive*.
- *Foresight* — how far ahead it leads a chased target (*Intercept Share*, *Max Lead*) and an ability's aim (*Zone Lead Share*), and how it reads the route to somewhere it has not seen (*Corner Pause*, *Straight Pace* over stretches of at least *Straight Length*, *Listen Stops*). The aim lead is on the *Use Ability* node (*Lead Moving Target*, on by default; older nodes pick that up without being edited).
- *Reflexes* — whether it dodges live grenades, from how far it notices them, its reaction delay, speed, the margin it wants beyond the blast, whether it seeks cover or keeps its eyes on the grenade, and its warning bark (*Warning* in the bark set). Reads `Grenade.Live`, so grenades need nothing extra.
- *Aftermath* — how long it stays on edge after a fruitless search and, as a share of that, after winning a fight (0 = it relaxes at once, as before), how long and in which gaze (*Hold* stares at the body, *Sweep* scans round it, *Scatter* sniffs about it) it stands over a kill.
- *Tells* — the sign it gives on noticing something (and how long it takes it in, and whether it snaps round to it), on committing to a fight (and how long it holds its first attack), and on standing down after being on edge.
Tells show as a pulse (`EnemyStateVisuals`) and as the mark over the head's motion (`EnemyAwarenessIndicator`); both read the colour in `EnemyData › Damage Number Color` (white if clear) and both are already on every enemy prefab.

**Target priorities** — `EnemyData` › *Target Priorities* = a `TargetPriorities` asset (`Create › CGD › Enemies › Target Priorities`), in `Enemies/Perception/`: `PredatorTargetPriorities` (Bio: the most hurt target, `_wounded` 2), `CounterFireTargetPriorities` (Tech: whoever hit it in the last 5 s, `_retaliation` 2.5, and sticks to its target, `_focus` 1.5, the more so the more it has worn it down, `_finish` 3), `OpportunistTargetPriorities` (Void: whoever is fighting someone else, `_distracted` 2). They multiply how strongly it senses each hostile, so they only matter where an enemy can see several hostiles (rival factions). Empty = only `_focus` 1.25 (sticks with its current target), which is how every regular enemy chooses.

### Behavior trees (Enemy)

No scene wiring: every `EnemyData` › **Behavior** › `BehaviorTree` names the `BehaviorTreeAsset` its enemies think with — there is no other brain, so an empty slot leaves the enemy standing still (`EnemyBrainAssetTests` fails for shipped data without one). All the regular types and tiers (and `DefaultEnemyData`) use `MeleeEnemyBehaviorTree`. `_waypoints` on `EnemyAI` feed the tree's **Patrol** node.

**Cost:** an enemy whose senses have nothing (unaware, no lead, not under fire, not attacking) ticks its tree every 0.1 s with a hostile in sensing range and every 0.25 s without (`TreeTickRate`), staggered between enemies; anything noticed brings it back to every frame. In the Profiler, each tree shows as `BehaviorTree.<asset name>` and the senses as `EnemyAI.Senses`.

| Asset | Notes |
|---|---|
| `Enemies/BehaviorTrees/MeleeEnemyBehaviorTree` | Regroup → Raise Alarm → fight while it knows where you are (under fire: dodge half the time, at most every 3 s; strike in range when it's its turn; squad Flank role and further than 6 m: Director Permit → flank; Pacing Is build-up/peak and further than 6 m: flank half the time at most every 8 s, behind a Director Permit; else close in) → Routine. |
| `Enemies/BehaviorTrees/RangedEnemyBehaviorTree` | Below 30% health: hide behind cover or flee (8 s cooldown) → Regroup → Raise Alarm → squad Suppress role, behind a Director Permit: dodge, else take cover and Suppress (or Suppress where it stands) → fight: dodge, else a Utility Selector weighs *from cover* (fixed 0.6: run to fresh cover, shoot 6–9 s) against *in the open* (TargetDistance 6–25 m: hold range and shoot for 3 s) → just lost sight (Knows Target 3 s, not Has Target): Throw Grenade `DefaultGrenadeData` behind a Director Permit, at most every 12 s, 3 per life → Routine. The `EnemyData` should be `CombatType` Ranged for its preferred range, burst and spread values. |
| `Enemies/BehaviorTrees/Shared/RegroupSubtree` | Knows Target → Is Shaken → Regroup; aborts lower priority. |
| `Enemies/BehaviorTrees/Shared/RaiseAlarmSubtree` | Has Target → Can Raise Alarm → Raise Alarm; aborts lower priority. Does nothing for enemies that don't raise alarms. |
| `Enemies/BehaviorTrees/Shared/RoutineSubtree` | Investigate leads (aborts lower priority) → Return To Post → Patrol. |
| `Enemies/BehaviorTrees/BioBehaviorTree` | AITest Bio (close range). Its routine is the behavior `Behaviors/Idle/BushRoamRoutine` (not the shared Routine), and fourth in its fight list — after the ambush, dodge and Surge branches, before *strike in range* — is the behavior `Behaviors/Fight/StalkRushHide` (both below); every fight branch after that only runs when the hunt can't (no target, or none in sight after the stalk). Regroup → Raise Alarm → fight while it knows where the target is (Knows Target 10 s): `BioAmbushAbility` ready and its Motive passes (Health 0.8→0.3 ×0.8, RecentDamage 0→0.3 ×0.5, UnderFire ×0.2, HostilesClose 1→3 ×0.3; no patience): Use Ability (cloak), Stealth Dash 6 m, then Take Cover *Hide* or Flee 12 m, wait 3–4 s → shot at further than 4 m: dodge (70%, at most every 1.5 s) → `BioSurgeAbility` ready and its Motive passes (TargetDistance 10→3 m ×0.7, Health 1→0.4 ×0.3, TargetHealth 1→0.3 ×0.3, RecentDamage 0→0.2 ×0.3, HostilesClose 0→2 ×0.2; patience 30 s): Use Ability (aborts lower priority) → strike in range on its melee turn → within 14 m: chase (aborts lower priority) → `TargetHealth` ≤ 0.5 and `TargetDistance` ≤ 35: chase (aborts lower priority) → Flank behind a Director Permit (at most every 3 s) → chase → out of sight: Take Cover *Hide* unless In Cover, then Overwatch 3–5 s (ambush) → Routine. Right after the strike branch, three predator branches (each aborts lower priority): `TargetExposed` ≥ 0.5 within 18 m: Chase Target (Time Limit 4 s) → `TargetWatching` ≥ 1.5 and not within 5 m: Prowl 2.5–3.5 s, 8–11 m (at most every 8 s) → in sight beyond 6 m with `BioSnareAbility` ready and its Motive passes (TargetSpeed 1→4 m/s ×0.45, TargetDistance 8→16 m ×0.35, TargetHealth 1→0.5 ×0.2; patience 14 s): Use Ability (max 20 m), then Prowl 2–3 s, 7–9 m. |
| `Enemies/BehaviorTrees/Behaviors/Idle/BushRoamRoutine` | The Bio routine: Investigate leads (aborts lower priority) → Return To Post → **Roam Bushes** (25 m round its post: a weighted-random bush, nearer and not just visited likelier, a beat of its doctrine's idle routine in each) → Patrol (only when no bush is in reach). |
| `Enemies/BehaviorTrees/Behaviors/Fight/StalkRushHide` | The Bio hunt, one Sequence: *stalk* — Force Result (succeed) › Sequence (abort self) [not `TargetExposed` ≥ 0.5 → Prowl 3.5–5 s, 8–12 m, *Prefer Bushes* on] → Has Target → *rush* — Force Result (succeed) › Time Limit 4 s › [Chase Target → Melee Attack] → *disengage and hide* — Hide In Bush (≥ 9 m from the target, run ≤ 25 m, hold 2–3.5 s), else Take Cover *Hide* + Wait 1.5–2.5 s, else Flee 12 m. |
| `Enemies/BehaviorTrees/Behaviors/Fight/FlushOut` | The Tech siege on a hidden target, one Sequence aborting self + lower priority: **Target Hiding** (lost under 10 s ago, in a bush or behind cover) → Repeat (until failure) › Selector [not Has Squad Role *Suppress* → Cooldown 7 s › Throw Grenade (`TechGrenadeData`, ≥ 6 m) · Suppress · Has Squad Role *Flank* → Cooldown 6 s › Take Cover *Fight* (5 s) · Overwatch 1.5–2.5 s]. No Director Permit or per-life limit on these grenades: the 7 s cooldown paces them. |
| `Enemies/BehaviorTrees/TechBehaviorTree` | AITest Tech (long range). Right after its in-view fight branch it runs the behavior `Behaviors/Fight/FlushOut` (above), which interrupts the beacon, lost-sight grenade, pursuit and overwatch branches below whenever its target is hiding. Shots and Suppress bursts are only taken along a clear path (scenery may not stop them more than 2.5 m short of the aim point); a grenade only when its simulated flight (bounces and roll included) goes off within 3 m of the spot. Suppress fires only within 4 s of last seeing the target (`_maxBlindTime`). Health below 70% (shield gone): Fall Back, else Take Cover *Hide*, else Flee 10 m, then wait 2.5–3.5 s (at most every 8 s) → Regroup → Raise Alarm → fight: target within 5 m with `TechDischargeAbility` ready and its Motive passes (aborts lower priority) (TargetDistance 5→1.5 m ×0.5, RecentDamage 0→0.2 ×0.4, UnderFire ×0.3, HostilesClose 1→3 ×0.4; patience 4 s): Use Ability (max 6 m), then Fall Back, else Take Cover *Fight*, else Flee 10 m → target within 9 m: Fall Back, else Take Cover *Fight* (cover near its 20 m preferred range), else Flee (at most every 4 s) → in sight with `TechFortifyAbility` ready and its Motive passes (UnderFire ×0.35, RecentDamage 0→0.2 ×0.4, ShieldMissing 0→0.8 ×0.3, TargetDistance 30→12 m ×0.3, HostilesClose 0→2 ×0.2; patience 25 s): Use Ability, then for 8 s a Repeat of [Throw Grenade (`TechGrenadeData`) behind a Director Permit, every 4 s | Suppress] (it never dodges: robots hold their ground) → target over 32 m off (Inverter › Target In Range 32; at most every 10 s; aborts lower priority): Take Cover *Advance* with *Stop At Preferred Range* on (8 s), else Chase Target to 26 m (Time Limit 5 s) → squad Suppress role and it saw the target within 4 s (Knows Target 4 s): Suppress → squad Flank role: Take Cover *Fight* (every 6 s) → in sight: Take Cover *Fight* and shoot 4–6 s, else Hold Range and shoot 3 s → just lost sight: Throw Grenade `Weapons/Throwables/TechGrenadeData` behind a Director Permit (every 12 s, 3 per life) → out of sight: Take Cover *Fight* unless In Cover, then Overwatch 4–6 s (Knows Target 12 s keeps it watching) → Routine. Just before the lost-sight grenade: out of sight with `TechSurveyBeaconAbility` ready and its Motive passes (no reasons, patience 4 s): Use Ability (toward the believed target). |
| `Behaviors/Fight/Sentry` · `Artillery` · `Juggernaut` · `Infiltrator` · `Spotter` · `Marksman` · `Bulwark` · `SwarmDive` · `Engineer` · `Hacker` | The second archetype set (see FEATURES › Enemy Design Tools; each tree's Root comment describes it). Kit needs: Artillery — Grenade (long-range); Juggernaut — Signature (a slam) and Boost; Infiltrator — Escape (a cloak); Spotter — Signature (a beacon); Engineer — Support and Summon; Hacker — Signature (EMP Pulse). Prefab needs: Sentry — `EnemyData` PatrolSpeed and ChaseSpeed 0; Marksman — an **AimLaser** and Burst Count 1; Bulwark — a **FrontalShield** on the root (beside `EnemyHealth`: `_halfArc` 60°, `_reduction` 0.8, `_stopsLightning` off). |
| `Behaviors/Fight/PackHunter` · `Lurker` · `Spitter` · `Broodmother` · `Swarm` · `Regenerator` · `Charger` · `Burrower` · `Infector` · `Lure` · `SporePod` · `Alpha`, `Reactions/Frenzy` | The third archetype set (FEATURES › Enemy Design Tools; each Root comment describes it). Kit needs: Spitter — Grenade; Broodmother — Summon; Regenerator, Frenzy — Boost (a heal for a regenerator); Burrower — Signature `BioBurrowAbility`; Spore Pod — Signature `BioSporeBurstAbility`; Alpha — Support `BioRallyAbility`; Infector — `EnemyData` › *Melee Effects* = `BioInfestEffect`. Pack Hunter and Alpha read the encounter's roles (lead = Assault, others = Flank). Lure's two Make Noise nodes have no sound yet (assign one in a copy). Charger and Spore Pod want tuned speeds (Spore Pod: 0 or slow). |
| `Behaviors/Fight/Bodyguard` · `DefendArea` · `Opportunist` · `Kiter`, `Reactions/Avenger` | Situational behaviors for any faction (FEATURES › Enemy Design Tools). Bodyguard needs a higher-tier ally of its team within 25 m (ties: everyone picks the same one). Defend Area leashes to the enemy's post (where it was placed or spawned) — 12 m — and writes the blackboard key `DefendSpot`; place defenders where they should hold. Avenger uses Kit › Boost if set. All branch on **Is Ranged** (`EnemyData` › Combat Type) to shoot or close in. |
| Bio abilities | `Abilities/BioBroodAbility` (`SummonAbility`: 3 × `Prefabs/Characters/Enemies/AITest/BioSwarmling`, 30 s, at most 6, 1 s cast, 14 s cooldown) · `BioBurrowAbility` (`BurrowAbility`: 1.2 s dig, surfaces within 2 m of a target ≤ 25 m away, 3 m eruption, 20 damage, force 10, 10 s cooldown) · `BioSporeBurstAbility` (`CloudBurstAbility`: the Smoke grenade's lingering cloud at its feet + `PoisonEffect` on everyone within 4 m, 3 damage, 12 s) · `BioRallyAbility` (`RallyAbility`: `Stats/BioSurgeModifierPreset` for 6 s on every ally within 15 m, 25 s) · `Combat/StatusEffects/BioInfestEffect` (`InfestEffect`: poison 6 s, up to 3 stacks; an enemy that dies with it bursts into 2 swarmlings for 30 s — never the player). |
| `Enemies/AITest/BioSwarmlingEnemyData` + `Prefabs/Characters/Enemies/AITest/BioSwarmling` | The brood and infestation creature: a copy of `VoidMinion` (body, scale, stats — still Void-coloured: give it a Bio look) on the `Bio` team, `DisplayName` Swarmling, thinking with `SwarmlingBehaviorTree` (Knows Target 6 s › Subtree `Behaviors/Fight/Swarm`, else the shared Routine). |
| `Behaviors/Reactions/FallBackToRange` · `CallForSupport` · `LockOn` | Has Target → within 9 m → Cooldown 4 s › (Fall Back, else Take Cover *Fight*, else Flee 10 m) · Has Target → Shield Below 5% → Health Below 75% → Cooldown 20 s › Call For Help · [Has Target → Repeat › Shoot Burst] (aborts self), else [Knows Target 3 s → Suppress]. |
| `Behaviors/Fight/ShambleAndLunge` | Selector: [Has Target → `TargetExposed` ≥ 0.5 → Target In Range 10 → Time Limit 3 s › (Chase Target → Melee Attack)] (aborts lower priority) · [Has Target → in attack range → Takes Melee Turn → Melee Attack] (aborts both) · [Has Target → Creep Unwatched to 1.5 m, 30°, 0.6 × chase speed, lurching for 0.5 s after 2.5 s stared at] · [Has Target → Chase Target]. |
| `Behaviors/Fight/HauntUnwatched` | Selector: [Being Watched 35° → Cooldown 2.5 s › (Take Cover *Hide* 4 s, else Flee 8 m)] (aborts lower priority) · [not Being Watched → in attack range → Melee Attack] (aborts self) · [Has Target → Creep Unwatched to 1.2 m] · Overwatch 1–2 s. |
| `Behaviors/Fight/TormentFromHiding` | Take Cover *Hide* → Repeat × 3 › [Wait 1–2.5 s → (Cooldown 5 s › Throw Grenade, kit, ≥ 5 m) else Peek without attacking, 0.3–0.7 s]. Needs Kit › Grenade. |
| `Behaviors/Fight/RaiseMinions` | Selector: [Knows Target 8 s → Ability Ready *Summon* → Use Ability *Summon*, aimed forward] (aborts lower priority) · [Has Target → within 8 m → Cooldown 3 s › (Take Cover *Hide*, else Flee 10 m)] (aborts lower priority) · Subtree `PeekABoo`. Needs Kit › Summon. |
| `Behaviors/Fight/CurseAndVanish` | Selector: [Has Target → Target In Sight → *Signature* ready → Use *Signature* (≤ 22 m) → (*Escape* ready → Use *Escape*) else Take Cover *Hide*] (aborts lower priority) · Subtree `PeekABoo`. Needs Kit › Signature (Escape optional). |
| `Behaviors/Fight/UseSignature` | Has Target → Target In Sight → *Signature* ready → Use *Signature* (≤ 25 m); aborts lower priority. |
| `Behaviors/Reactions/EscapeWhenHurt` | Has Target → Health Below 50% → Cooldown 12 s › [(*Escape* ready → Use *Escape*) else Take Cover *Hide* 6 s else Flee 10 m → Wait 2–3 s]; aborts lower priority. |
| `Behaviors/Reactions/PounceOnOpening` | Has Target → `TargetExposed` ≥ 0.5 → within 14 m → Time Limit 3 s › (Chase Target → Melee Attack); aborts lower priority. |
| `Enemies/BehaviorTrees/Behaviors/Fight/CreepUnwatched` | Has Target → Inverter › Target In Range 14 → **Creep Unwatched** (until 12 m, no lurch; looked at within 35°: stops dead and stares; otherwise 0.9 × chase speed through bushes). |
| `Enemies/BehaviorTrees/Behaviors/Fight/PeekABoo` | Take Cover *Hide* (5 s) → Repeat × 3 (until failure) › [Wait 0.8–2.6 s → **Peek** (1.5–3.5 m out, at most 0.6–1.4 s in view, 1.4 × chase speed, strikes, gives up after 3 s)]. Fails, letting the branches below run, when there is no high cover to hide behind or nowhere to peek from. |
| `Enemies/BehaviorTrees/VoidBehaviorTree` | AITest Void. In its fight list, after the Coven flank branch and before *now and then, a new angle*: the behaviors `Behaviors/Fight/CreepUnwatched` then `Behaviors/Fight/PeekABoo` (above); the new-angle and Engage branches only run when those can't. (medium-range hybrid: shards and melee). Health below 50% (at most every 15 s): hide as Tech does, then wait 2.5–3.5 s (at most every 8 s) → Regroup → Raise Alarm → fight: dodge → in sight with the rift ready and its Motive passes (TargetSpeed 4→0.5 m/s ×0.5, ShieldMissing 0→0.6 ×0.4, TargetDistance 20→8 m ×0.3; patience 15 s): Use Ability `Abilities/VoidRiftAbility` (max 20 m), then half the time Take Cover *Hide* → within 5 m: Flank to its side (Director Permit), else Flee (every 3 s) → target busy elsewhere (Target Distracted): Flank (Director Permit, every 4 s; aborts lower priority) → every 10 s, 50% chance: Flank to a new angle (Director Permit) → Engage (Utility Selector, re-scored every 1 s, 0.15 margin; scores: Escape = within 5 m branch (moved here from the top of the fight list; Flank to its side, else Flee) × TargetDistance 6→1.5 m × TargetHealth curve 0.4→1 × 1.0; Melee dive = Time Limit 6 s [Chase Target → Melee Attack] × TargetDistance 15→2 m × weaker-target curve 0.55→1 × 1.5; Skirmish = Hold Range and shoot 3–4 s, fixed 0.55; Phase Shift = [Ability Ready → Motive (ShieldMissing 0→0.7 ×0.6, RecentDamage 0→0.25 ×0.6, UnderFire ×0.3, Health 1→0.4 ×0.5, TargetDistance 8→2 m ×0.4; patience 40 s) → Use Ability `VoidPhaseShiftAbility`, cooldown 25 s] × `ShieldMissing` 0→0.5 curve 0.45→1 × 1.5, so 0.675 with an intact shield up to 1.5 with half of it gone) → lost sight: Flank toward the last known position (Director Permit, every 4 s) → out of sight: Overwatch 1–1.5 s → Routine. Flank's stand-off is the 13 m preferred range because the data is Ranged. First in the fight list: `TargetFlinch` ≥ 0.5 within 16 m: Time Limit 3 s [Chase Target → Melee Attack] (aborts lower priority). After the rift branch: in sight with `VoidEchoAbility` ready and its Motive passes (UnderFire ×0.35, RecentDamage 0→0.25 ×0.35, TargetDistance 25→10 m ×0.3; patience 30 s): Use Ability (aborts lower priority). |
| `Enemies/BehaviorTrees/VoidMinionBehaviorTree` | The Phase Shift minions (`VoidMinionEnemyData`). Fight while it knows the target (Knows Target 4 s, aborts both): strike in range on its melee turn → more than 6 m off with the target in sight: Take Cover *Advance* (2.5 s), else Flank (2 m stand-off, 3 s; no Director Permit) → Chase Target. Otherwise the shared Routine. No Regroup, dodge or retreat. |
| `Enemies/BehaviorTrees/VoidEchoBehaviorTree` | The Wrong Echo (`VoidEchoEnemyData`). `TargetWatching` > 0: Stop Moving, wait 0.25 s (aborts lower priority) → has a target: Chase Target to 1.5 m (aborts itself) → the shared Routine. It never attacks. |
| Hazard branch (first in the root of `BioBehaviorTree`, `TechBehaviorTree`, `VoidBehaviorTree`) | Sequence, aborts lower priority: **In Hazard** → **Motive** (HazardDanger 0→0.12 ×1, Health 1→0.3 ×0.4, TargetHealth 0.15→1 ×0.3; patience 3 s) → **Leave Hazard** (3 s). Damaging zones list themselves in `Hazards` while open (`RiftZone` does); nothing to wire in the scene. |
| **Fall Back** node (`Enemy/Tactics/Fall Back`, used by `TechBehaviorTree`) | `_giveUpAfter` (8 s) | Withdraws to a planned fallback from `FallbackPlanner`: a known cover spot (needs a `CoverMap` in the scene) that screens it from the target, is at least 8 m (and 3 m more than now) from every hostile it senses, is reached by a route never within 4 m of one, and prefers a short, unexposed run, high cover and the ground near its post. Fails when none qualifies, so put Take Cover / Flee after it in a Selector. |
| `EnemyData` › **Social** / **Pacts** | `Social`: Standard (calls and answers: regular enemies), Network (a union: shares every sighting each second over an 80 m link through walls and favours targets its kin fight), Loner (neither calls nor answers), Herd. `Pacts`: extra teams counted as kin (heard, and herd-mates). Set on the AITest data: Bio Herd, Tech and Void Network (the Void Coven), the Void minion Loner | Callouts only pass between kin (same team plus Pacts). **Kin Near** (`Enemy/Squad/Kin Near`: `_radius`, `_atLeast`) and **Join Herd** (`Enemy/Squad/Join Herd`: `_within` 8 m, `_giveUpAfter`) are the herd's nodes: `BioBehaviorTree` has a straggler branch before the pounce, a pack flank-role branch (needs an `EnemySquad`) and a calm branch before the Routine; `TechBehaviorTree` has Suppress-role and Flank-role (bound to a new firing position) branches ahead of its long-range fight. Everything here needs two or more agents of a faction (and an `EnemySquad` on their parent for the roles) — the AITest scene has two of each |
| **Has Belief** + **Pursue Belief** nodes (`Enemy/Senses/Has Belief`, `Enemy/Tactics/Pursue Belief`; used by all three faction trees) | Pursue Belief: `_style` (Sweep / Overwatch / Haunt), `_look` (seconds spent looking at each place), `_giveUpAfter` | Every enemy keeps an `EnemyBelief` (no setup): once it loses sight of its target, a likelihood over the `CoverMap` spots and the active `ConcealmentZone`s (bushes, smoke) round the last sighting that spreads outward with time and leans the way the target was heading; spots it sees empty (or walks up to) drop out — spots inside a concealment zone only when walked up to; confidence fades over 30 s; noises and calls for help seed a weaker one. Needs a `CoverMap` in the scene. Has Belief succeeds while the target is unseen and the belief is alive; Pursue Belief goes to the next place by style (Sweep: likeliest nearby, then onward; Overwatch: cover overlooking the likeliest spots, holds, then bounds on; Haunt: random in proportion to likelihood), looks, marks it checked, succeeds. It fails when nowhere is left, so the old lost-sight branches and Routine stay as the fallback. In the trees: a branch inside the fight Selector (before the old lost-sight branch) and one before the Routine subtree (aborts lower priority): Tech Overwatch (3 s look), Bio Sweep (0.8 s), Void Haunt (1.2 s). The spectator's *Paths & plans* overlay draws the inspected agent's belief: last sighting and heading in red, rings (bigger = likelier; orange for cover, green for bushes) on the places it is weighing, grey crosses on the ones it has cleared. |
| `Enemies/AITest/BioTestEnemyData`, `TechTestEnemyData`, `VoidTestEnemyData` | The AITest agents, named *Bio Agent*, *Tech Agent* and *Void Agent*. Each is on its faction's team (`Bio` / `Tech` / `Void`), colours the damage numbers it deals (`DamageNumberColor`: green / yellow / violet; clear on regular enemies = white), has a 60 m sight range, uses its faction tree above and its faction's Target Priorities (Predator / CounterFire / Opportunist). Each agent has 100 effective health (health + shield, multiplied by 1 + armour/100) split by faction: Bio 80 health + 25 armour, no shield (health > armour); Tech 20 health + 30 shield + 100 armour (armour > shield > health); Void 40 health + 60 shield + no armour (shield > health), shield regen 20/s after 2.5 s. Bio: melee, 6 m/s chase. Tech: ranged, 20 m preferred range, 3-round bursts of `Prefabs/Weapons/TechBolt` (26 m/s, 9 damage, 1.5° spread); it doesn't raise alarms. Void: ranged with a melee strike (1.6 m reach, 12 damage, 0.6 s wind-up, 1 s cooldown), 13 m preferred range, one heavy bolt per shot instead of a burst (`BurstCount` 1): `Prefabs/Weapons/VoidBolt` at 20 m/s, `RangedAttackDamage` 34, `ShotRadius` 0.4 m so near misses hit, `ShotBlastRadius` 1.6 m, `RangedAttackCooldown` 1.8 s, applying `Combat/StatusEffects/VoidSlowEffect` through `EnemyData › ShotEffects`; its Void Rift comes round every 14 s; 110° sight cone. | `BioSurgeAbility` has a 16 s cooldown and lasts 6 s (`BioSurgeModifierPreset`: FireRate +60% = melee attack speed, Lifesteal +0.4). Bio uses `Enemies/Perception/BioPerceptionProfile` (a copy of `SkittishPerceptionProfile` with *Scent Range* 30 m, *Scent Gain* 0.6); the tier 2–3 Bio enemies and Scavengers keep the skittish one.
| **EnemyAbilityVisuals** (each agent) | `_material` = `Materials/VFX/ExplosionFlash` (transparent unlit, tinted per effect), `_shellMesh` = built-in Capsule, `_ringMesh` = built-in Cylinder, `_model` = Model, `_flash` = `Prefabs/VFX/ExplosionFlash`; `_looks` = one per ability (Bio also `BioAmbushAbility`, a faint green shell; Tech also `TechDischargeAbility`, a 6 m flash): Bio `BioSurgeAbility` (green, cast flash 1.2 m), Tech `TechFortifyAbility` (yellow, cast flash 1.6 m, ground ring), Void `VoidPhaseShiftAbility` (violet, no cast flash) and `VoidRiftAbility` (violet, cast flash 0.8 m) | Any agent with a `Stealthable` also fades its body to `_cloakedOpacity` (0.25) while cloaked, using transparent copies of its materials. Shell, ring and trail last for the ability's buff (`Ability.EffectDuration`). Also bursts at both ends of a Phase Shift (`_relocateFlash`) and pulses on heals such as lifesteal (`_healFlash`, at most every 0.3 s). Any enemy can use it; an ability with no look shows nothing. Also Bio `BioSnareAbility` (green, cast flash 0.8 m) and Tech `TechSurveyBeaconAbility` (yellow, cast flash 0.6 m); Wrong Echo has no look, so it leaves no flash. |
| **FlightEffects** (on `Prefabs/Weapons/TechBolt`, `VoidBolt`, `TechGrenade`) | `_material` = `Materials/VFX/ExplosionFlash`, `_sparkMesh` = built-in Sphere, `_impactFlash` = `Prefabs/VFX/ExplosionFlash` | Builds a `Trail` (TrailRenderer) and a `Sparks` (ParticleSystem, mesh particles left behind per metre flown) child at runtime, tinted per prefab, cleared on every reuse from the pool; on a `Projectile` it also bursts where the shot lands (`Projectile.Impacted`). Trail time 0 or sparks per metre 0 turns that part off. Any pooled projectile or grenade can use it. |
| `Weapons/Throwables/TechGrenadeData` + `Prefabs/Weapons/TechGrenade` | Tech's own copy of `DefaultGrenadeData` / `FragGrenade` (same damage and handling): yellow body, fuse blink and blast flash, and a FlightEffects trail. A copy so the player's frag grenade and the regular ranged enemy's stay as they are. |
| **AimLaser** (`TechTestAgent`) | On the root: `_material` = `Materials/VFX/ExplosionFlash`, `_color` red, `_width` 0.025 m, `_startOpacity` 0.3, `_aimTime` 1 s (match Aimed Shot). Shows only while the agent reports the Aim step, so it does nothing until a tree with Aimed Shot runs. |
| **HorrorPresence** (`VoidTestAgent`) | On the root, beside `EnemyAI`: `_model` = the *Model* child, `_body` = the Body, Visor and Arm `MeshRenderer`s (not ability shells). Glitch: `_watchedAngle` 35°, `_stillSpeed` 0.4 m/s, `_glitchEvery` 0.35–1.4 s, `_glitchFor` 0.04–0.12 s, `_twitch` 0.08 m, `_peekBlinks` 3. Sound: `_peekSound` and `_whisper` empty (assign `SoundBank`s; needs an `AudioPool` in the scene), `_whisperRange` 12 m, `_whisperEvery` 6–12 s. Any enemy prefab can take it; it reads the Peek and Freeze steps the Peek and Creep Unwatched nodes report. |
| **EnemyAttackVisuals** (each agent) | `_flash` = `Prefabs/VFX/ExplosionFlash`, `_material` = `Materials/VFX/ExplosionFlash`; Tech: `_muzzleFlash` 0.5 (yellow); Void: `_muzzleFlash` 0.35 (violet); Bio: `_arm` = Arm, `_strikeFlash` 1.2, `_hitFlash` 0.8 (green) | Muzzle flash per shot (`EnemyAI.Fired`), a claw trail on `_arm` through the swing and a slash as the blow lands (cancelled by a stagger), and a burst on whatever this enemy damages. Each part is off at 0 / empty. |
| `Abilities/VoidPhaseShiftAbility` | `PhaseShiftAbility` (enemies only): 0.5 s cast, 25 s cooldown, reappears at known full-height cover 5–15 m away that no hostile around can see (on the same walkable area), then `Stats/VoidPhaseShiftModifierPreset` for 5 s (ShieldRegenRate +200%). Can't be cast while there is no such spot. Leaves `MinionCount` (2) × `MinionPrefab` (`Prefabs/Characters/Enemies/AITest/VoidMinion`) where it vanished, sent at its target for `MinionLifetime` (15 s) — they need a NavMesh under them and a `Void`-team `EnemyData`. |
| `Abilities/TechFortifyAbility` | `StatBuffAbility`: 0.6 s cast, 18 s cooldown, `Stats/TechFortifyModifierPreset` for 8 s (Armor +60, MovementSpeed −50%, Shield +30). `ChargeAddedShield` on, so the extra 30 shield arrives charged. |
| `Abilities/BioAmbushAbility` | `AmbushAbility`: 0.15 s cast, 25 s cooldown. Cloaks the caster for 6 s through its `Stealthable` (the BioTestAgent prefab has one), applies `Stats/BioAmbushModifierPreset` (+12 health/s) for 6 s and arms its next attack for ×2.5 damage, which also ends the cloak. Needs a `Stealthable` on the caster; `AbilityContext.Ambush` is filled by the enemy. |
| `Abilities/TechDischargeAbility` | `ShieldDischargeAbility`: 0.3 s cast, 22 s cooldown, needs ≥ 10 shield. Spends all the caster's shield: Lightning damage ×1.25 per shield point (30 shield = 37.5) to everything hostile within 6 m, plus `Combat/StatusEffects/TechDischargeSlowEffect` (30% slow per stack, 2 stacks, 4 s), and knocks them back (`KnockbackSpeed` 14, `KnockbackDuration` 0.4 s). `SelfBuff` = `Stats/TechDischargeModifierPreset` (MovementSpeed +40%) for 3 s on the caster. |
| `Abilities/BioSurgeAbility` | `StatBuffAbility`: 0.15 s cast, 16 s cooldown, `Stats/BioSurgeModifierPreset` for 6 s (FireRate +60%: an enemy's melee wind-up and recovery shrink by it; Lifesteal +0.4). No speed or regen. |
| `Abilities/VoidRiftAbility` | `RiftAbility` (enemies only: it lands on the point the caster aims at): 0.6 s cast, 14 s cooldown, spawns `Prefabs/VFX/RiftZone` (a flat cylinder with a `RiftZone` and the transparent `Materials/VFX/ExplosionFlash`, like `FireZone`) of 4 m radius for 6 s. Every 0.5 s each hostile inside takes 4 True damage and `Combat/StatusEffects/VoidSlowEffect` (an Ice effect: 15% slower per stack, 4 stacks, 3 s, no stun or armour loss); the caster recovers 75% of the damage dealt as shield. |
| `Enemies/AITest/VoidMinionEnemyData` + `Prefabs/Characters/Enemies/AITest/VoidMinion` | The Phase Shift minion: a standalone copy of `VoidTestAgent` (not a prefab variant) at 0.65 scale, with this data assigned, on the `Void` team with 15 health, no shield, melee only (no ranged fields set; 5 damage, 1.3 m, 0.7 s cooldown), 9 m/s chase (faster than every agent), 360° sight, using `VoidMinionBehaviorTree`. Not a child of the Agents object, so it isn't watchable, labelled or respawned; its summoner releases it back to the pool. |
| `Abilities/BioSnareAbility` | `SnareAbility` (enemies only): 0.5 s cast, 14 s cooldown, spawns `Prefabs/VFX/SnareZone` (a copy of `RiftZone` with a `SnareZone` instead) of 3 m radius for 5 s on the floor below the aim point. Every 0.5 s each hostile inside takes 1 True damage carrying `Combat/StatusEffects/BioSnareSlowEffect` (Ice: 10% slower per stack, 4 stacks, 1.5 s) and `BioSnareBleedEffect` (Bleed, 6 s); the first time each steps in it makes a noise (`EntryNoiseRadius` 18 m). It is a hazard, so agents step out of it. |
| `Abilities/TechSurveyBeaconAbility` + `Prefabs/Weapons/SurveyBeacon` | `SurveyBeaconAbility` (enemies only): 0.8 s cast, 25 s cooldown, places the beacon on the NavMesh toward the aim point, at most 14 m away (`MaxDeployDistance`). Prefab: root with a `CapsuleCollider` (0.25 × 1.2 m), `DeployableHealth` (60 health; its team is set on deploy) and `SurveyBeacon` (the Interact target for spoofing); child `Body`, a built-in cylinder (0.35 × 0.6 × 0.35) with `Materials/VFX/ExplosionFlash`, tinted per state. Settings on the ability: 15 s lifetime, a pulse every 3 s shown with `Prefabs/VFX/ExplosionFlash`; scan 12 m (stealth stripped 2.5 s, reports to kin within 80 m through walls); repair 15 shield per pulse within 8 m when it finds nobody; spoof hold 2 s, false reports 15 m beyond the beacon. Pooled. |
| `Abilities/VoidEchoAbility` + `Enemies/AITest/VoidEchoEnemyData` + `Prefabs/Characters/Enemies/AITest/VoidEcho` | `EchoAbility`: 0.4 s cast, 30 s cooldown; leaves the echo where the caster stood and relocates it unseen up to 15 m (only when there is somewhere hidden to go: keep `MaxDistance` equal to Phase Shift's so the search is shared). The prefab is a copy of `VoidMinion` at full Void size with this data (Void team, 1 health, 2.2 m/s chase, 360° sight, no voice, `VoidEchoBehaviorTree`), without `EnemyHealthBar` or `EnemyAbilityVisuals`, plus an **EchoBody**: breaks after 1.5 s of being stared at or within 2.5 m of its target, intercepts every hit (it never dies, so no one mourns it as a fallen ally) and tells its summoner it was shot at, and repeats the last hostile noise within 30 m every 3–5 s while unwatched; `_mimicSound` / `_breakSound` are empty (assign SoundBanks), `_breakFlash` = `Prefabs/VFX/ExplosionFlash`. Lifetime 12 s; released with the rest of its summoner's minions. |

**Behaviors (reusable branches):** a behavior is a `BehaviorTreeAsset` whose Root holds one branch, kept in `Data/Enemies/BehaviorTrees/Behaviors/` (sub-folders are categories, e.g. `Behaviors/Fight/`, `Behaviors/Reactions/`; created on first save) or `Shared/`, run from a tree by a **Subtree** node. The Root's comment is its description. A Subtree's *Off* box (or *Switch Behavior Off* on its context menu) skips it: it fails at once and its tree is never built — only use it directly under a Selector (the validator warns elsewhere). To grow the library from the AITest agents: open `BioBehaviorTree` (or Tech / Void) in *CGD › AI › Behavior Composer*, and in the fight list press *Save as Behavior…* on each branch worth reusing (name it, pick a category folder); the agent keeps behaving the same, now through Subtree nodes. To build a new type from them: *CGD › AI › New Enemy Type…* with *Start from a starter tree* (or *New Starter Tree…* in the Composer for an existing EnemyData), then *+ Add Behavior* in its fight list and order them; or copy a faction agent's tree and switch off what it shouldn't do. *Copy* on a behavior row points that one tree at its own copy (`<Tree><Behavior>`), leaving the shared one alone. Undoing *Save as Behavior* puts the branch back but leaves the new asset (delete it by hand).

**Behavior settings and enemy tuning:** a behavior's tunable values live on its asset as *settings* — in the Behavior Tree Editor select a node and tick a value under *Behavior settings* (rename it there; give two nodes' values the same name to drive both). A Subtree keeps its own values for them (set in the Composer's *Settings* foldout on the row; nothing to wire by hand). Each `EnemyData` has an optional **Behavior Tuning** list (under *Behavior*): per behavior asset (or its own tree), *Off* switches it off for that enemy type alone, and its values override the tree's. It is filled by the Composer with *Only <enemy>* picked — leave it empty for an enemy that runs its tree as is. At runtime the enemy's value wins over the Subtree's, which wins over the node's own. Renaming a setting leaves old values pointing at nothing: the tree checks and the enemy checks warn.

**New enemy types (no code):** *CGD › AI › New Enemy Type…* — pick a template prefab (any prefab with `EnemyAI` on its root, e.g. `Husk`, `TechTestAgent`), enter an asset name (PascalCase), display name, tier and team, and tick the parts to copy. It creates `Data/Enemies/<Name>EnemyData` (a copy of the template's data; its tree is shared, copied or a new starter tree `<Name>BehaviorTree`, as chosen) and `<Name>.prefab` beside the template (a prefab variant whose `EnemyAI`, `EnemyHealth` and any other field that held the template's data point at the new one); ticked parts are copied beside their originals as `<Name>PerceptionProfile`, `<Name>TargetPriorities`, `<Name>Doctrine`, `<Name>EnemyBarkSet`. With a *Faction* set, the prefab is appended to that `FactionDefinition`'s `_tier1/2/3` list for its tier. Then tune the data in its inspector, edit the tree, and place the prefab in a scene with waypoints as for any enemy. Compare types in *CGD › AI › Enemy Roster*. Shared assets: the EnemyData inspector's *Mind* section shows how many types use each and *Make Own Copy* splits one off.

**Editing:** *CGD › AI › Behavior Tree Editor*, or double-click a tree asset (`Create › CGD › AI › Behavior Tree` makes a new one with its Root). Space or right-click on the canvas to add nodes, group the selected nodes or add a sticky note; drag from a node's bottom port to another's top port to connect. Double-click a **Subtree** node to open its tree. Key fields have a ▾ picker. Badges on nodes and the toolbar's issue button show what the validator found.

**Debugging:** in Play mode select an enemy to watch its tree (and the subtrees it runs). The side panel then shows its live *Agent* status (state and step, awareness, morale, squad role, melee turn, director phase / pressure / stress, bold moves in use, tick rate — any agent implementing `IBtAgentStatus` can add rows), its live blackboard, its decision trace, and for the selected node what it and its children last reported. *Break when it starts / fails* on a node pauses the game for the watched enemy (breakpoints last for the editor session). Recording only runs for the watched enemy.

**New key-reading nodes:** mark the string field `[BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Float)]` (or `Write`) to get the picker and the "nothing writes this" check. Keys an agent writes itself are `const string` fields marked `[AgentBlackboardKey]` (see `EnemyBlackboard`).

**Voices** — `EnemyData` › *Voice* = an `EnemyBarkSet` (`Create › CGD › Enemies › Bark Set`): one *Line* per `BarkKind` with a `SoundBank` and a *Chance*; kinds without a SoundBank are silent. Every enemy data asset uses `Enemies/Voice/DefaultEnemyBarkSet` (all kinds listed, no sounds yet) — give each faction or archetype its own copy with clips. Barks play at the enemy's head through the `AudioPool` (needs one in the scene). Spacing is level-wide (0.8 s after any bark, 5 s before the same kind, 2.5 s per enemy; urgent kinds skip the first) — constants in `EnemyBarks`. `EnemyBarks.Spoken` reports every bark for captions or HUD hints.

**Enemy blackboard** — refreshed every tick for generic nodes (Compare Number, Score, Motive): `Health`, `ShieldMissing` (0–1 of its shield gone), `RecentDamage` (health and shield lost lately as a share of both maxima, halving every 1.5 s), `UnderFire` (1 or 0), `HazardDanger` (share of its remaining health and shield the hostile zones it stands in take per second; 0 when clear), `HostilesClose` (count within 6 m), `Morale`, `Awareness` (0–1), `TargetSpeed` (m/s while sensed; unset without a target), `TargetDistance` (metres to the believed target position; unset without a target), `TargetHealth` (0–1 of the target's health and shield when last seen; unset without a target), `Cautious` (flag); from the director: `Pacing` (0 build-up, 1 peak, 2 fade, 3 calm; 0 without a director), `Pressure` (1 neutral), `PlayerStress` (0–1).

**Motive node** (`Blackboard/Motive`): succeeds once its weighted reasons (each a blackboard number mapped linearly *From* → *To* and multiplied by its *Weight*) plus impatience (time waited ÷ *Patience*; 0 = never without a reason) reach 1. Put it right after the Ability Ready check, before Use Ability, so the patience only counts while the ability could go. A Utility Selector skips any child whose leading conditions fail, so a Motive that doesn't hold never cuts off the running child.

**Pacing nodes:** **Pacing Is** (a set of phases; no director counts as build-up) and **Director Permit** (decorator: runs its child only if a level-wide bold-move permit is free, holds it until the child ends). Put a Director Permit directly above each bold move (Flank, Throw Grenade, a Suppress branch) — under any Cooldown/Chance, so the chance isn't spent while waiting for a permit.

**Tree rules of thumb:**
- The top **Selector** lists branches most urgent first. Give each branch that should interrupt the ones to its right **Abort: Lower Priority**, and start it with its conditions (`Has Target`, `Is Shaken`…) — only those leading conditions are re-checked.
- Give a branch **Abort: Self** (or **Both**) when it should stop the moment its own conditions break: `[Target In Range → Takes Melee Turn → Melee Attack]`.
- Gates (**Cooldown**, **Limit**) count as conditions: a branch behind one doesn't interrupt until it's ready. **Chance** isn't a gate — put it under the Cooldown, not before it.
- **Patrol**, **Hold Range** and **Repeat** (forever) never finish on their own — end them with a **Parallel** (Require One) next to a **Wait**, or keep them where something above can interrupt.
- Share branches between enemy types as **Subtree** assets; a subtree's abort mode works from where it's placed.
- **Take Cover** *Use*: *Fight* = cover to shoot from near the preferred range, *Hide* = full-height cover only (enemies don't crouch, so low cover doesn't hide them), *Advance* = cover at least 3 m closer to the target, for closing in.
- A fight branch led by **Knows Target** should always have something to do while that memory holds: if all its children fail, the top Selector interrupts the branch to its right (usually Routine) and restarts it every tick. End such a branch with a lost-sight fallback, e.g. `[Inverter(Has Target) → Overwatch]`, and set *Knows Target*'s memory to how long the enemy should stay in the fight.
- **Overwatch** succeeds the moment the target shows itself again, so the Selector re-chooses at once. Put it after a `Force Result (succeed) → Selector [In Cover, Take Cover]` to take cover first only when it isn't already in some.
- **Take Cover** and **Flank** pick a new spot each run: put them under a Cooldown or a timed Parallel so they don't reposition every frame. They need a CoverMap in the scene (Flank only needs the NavMesh).
- Use **Knows Target** instead of **Has Target** above moves that break line of sight (flanking), so losing sight for a moment doesn't end the fight.
- Trees with `Enemy/…` nodes must run on an `EnemyAI`.

---

## AI Test Scene

`Scenes/AITest` — the MapTest scene root, Player and HUD without the map, director and expedition systems, plus:

```
Arena              [NavMeshSurface, RuntimeNavMesh]      ← NavMesh built from its children at load
  Floor, Wall North/South/East/West      [MeshFilter, MeshRenderer, BoxCollider]   ← 60 × 44 m, walls 3 m high
  Gallery Crate 1–4, Bunker W/E, Pillar W/E   ← north: low firing line at z 13, full-height bunkers behind (Tech)
  Stone 1–8                                   ← middle: full-height stones about 6 m apart (Bio)
  Lane Wall W/E South/North, Lane Crate/Pillar W/E   ← side lanes behind long walls at x ±20, gap at z −4…0 (flanking)
  Yard Wall W/E (+ Side), Yard Crate (W/E)    ← south: L-shaped walls and low crates (Void, player start)
Concealment Zones  (empty root)                          ← 8 hiding places, no colliders besides triggers, so agents walk through and the NavMesh ignores them
  Zone <name>      [BoxCollider (trigger, 2.2 m high), ConcealmentZone (Bush)]   ← layer Ignore Raycast; Center (0, 0), West/East Doorway (±16, −2), Gallery West/East (±8, 9.5), Yard (0, −9.5), Corner NW/NE (±26, 13)
    Foliage        [MeshFilter (Cube), MeshRenderer]     ← one box matching the trigger's size; `Art/Materials/Environment/Props/BushFoliage` (transparent green), no collider
CoverMap           [CoverMap]                            ← cover spots found from the NavMesh
Spectator Camera   [Camera (off), UniversalAdditionalCameraData, AudioListener (off), SpectatorCamera]   ← tag MainCamera; (0, 50, −30), 62° down, FOV 46: the overview it starts on
Alarm Panel        [MeshFilter, MeshRenderer, BoxCollider, AlarmPanel]   ← on the north wall between the bunkers; `_screen` = its renderer
Enemies            [EnemyRespawner]                      ← _delay 3, _territoryRadius 6, _alsoAvoid = Player (its PlayerHealth): revives any enemy under it in its own territory, at the safest spot
  Waypoints        Bio 1–4, Tech 1–2, Void 1–4
  Bio Herd         [EnemySquad]                          ← radio and leader; roles from BioDoctrine's encounter table (the alpha and the next strongest go in, the rest flank)
    Bio Agent                    (BioTestAgent, patrol Bio — west lane and stones)
    Bio Agent 2                  (BioTestAgent, at (−27, −14); the same patrol in another order)
  Tech Union       [EnemySquad]                          ← radio and leader; roles from TechDoctrine's encounter table (the lead pins as the suppressor, up to two flank for crossfire and grenades)
    Tech Agent                   (TechTestAgent, patrol Tech — along the gallery)
    Tech Agent 2                 (TechTestAgent, at (10, 16))
  Void Coven        [EnemySquad]                          ← radio and leader; roles from VoidDoctrine's encounter table (the deepest presses on, the rest flank via the tree's Coven branch). `VoidTestEnemyData` Social = Network, like Tech
    Void Agent                   (VoidTestAgent, patrol Void — the south yard)
    Void Agent 2                 (VoidTestAgent, at (20, −19))
  (Squad sizes, G, spawn more of each faction at runtime under this object from the Spectator Camera's `_agentPrefabs`, next to the faction's first agent, with its patrol route, named *Bio Agent 3* and so on, up to four per faction. Nothing extra is placed in the scene)
```

The agents are `Prefabs/Characters/Enemies/AITest/BioTestAgent`, `TechTestAgent` and `VoidTestAgent` — two of each, the second named *Bio Agent 2* and so on through `EnemyHealth` › `_displayName` (an override of the data's name, so labels, stats and scenarios can tell them apart; a scenario's agent name matches the GameObject name). Keys 1–6 watch the first six; Tab and clicking reach the rest. Each pair sits under its own `EnemySquad` (callouts relay inside a squad whatever the team, so one squad per faction):

```
<Agent>   [NavMeshAgent, EnemyHealth, EnemyAI, Stunnable, StatusEffectController, EnemyHealthBar,
           EnemyStateVisuals (_renderers = Visor), EnemyAwarenessIndicator,
           EnemyPlaceholderAnimation (_model = Model, _arm = Arm), CharacterStats,   ← CharacterStats carries the ability buffs
           EnemyAbilityVisuals]   ← builds its Ability Shell / Ring / Trail children at runtime
  Model             ← pivot at the feet; tipped over on death, stood back up on revive
    Body            [MeshFilter capsule, MeshRenderer (<Faction>AgentBody), CapsuleCollider, Hitbox → EnemyHealth]
    Visor           [MeshFilter cube, MeshRenderer]   ← tinted per AI state
    Arm             [MeshFilter cube, MeshRenderer (<Faction>AgentGlow)]   ← hidden until a melee swing or a cast
``` Each prefab sets its data on both `EnemyHealth` and `EnemyAI` (`Enemies/AITest/BioTestEnemyData` / `TechTestEnemyData` / `VoidTestEnemyData`), so scene instances only override position and waypoints. Faction colours come from `Art/Materials/Characters/AITest/`: `BioAgentBody` / `BioAgentGlow` (green), `TechAgentBody` / `TechAgentGlow` (yellow), `VoidAgentBody` / `VoidAgentGlow` (purple). The glow materials also colour the shots: `TechBolt` uses `TechAgentGlow` and `VoidBolt` uses `VoidAgentGlow`. They have no `DespawnOnDeath` (it would destroy the body before `EnemyRespawner` revives it), no affixes and no map marker.

Full-height pieces (white, 2.2 m or more) count as high cover that hides a standing agent. Low pieces (grey, 1 m) can be shot over and don't hide anyone. Keep gaps of 1.5 m or more between pieces so agents can pass. Each test data asset puts its agent on its faction's team (`Bio` / `Tech` / `Void`) with a 60 m sight range, so the factions hunt each other across the arena. The `Alarm Panel`'s `_team` is `Tech`: only enemies of the panel's team use it, and only data with *Raises Alarm* on — currently none of the test agents, so it sits idle until one is turned back on. To watch the factions on their own, deactivate the `Player` and follow along in the Scene view.

| Component | Assign | Notes |
|---|---|---|
| **NavMeshSurface** (Arena) | Collect Objects = *Children*, Use Geometry = *Physics Colliders* | Not baked in the project. |
| **EnemyRespawner** (Enemies) | `_alsoAvoid` = the Player's `PlayerHealth` | Revives an enemy within `_territoryRadius` of where it was placed or of one of its `_waypoints` (so its patrol route is its territory), at the candidate spot furthest from living hostiles and out of their line of sight (`_obstacleMask`, Default). Rivals under the same object always count; `_alsoAvoid` adds characters outside it. Its post becomes the new spot. |
| **SpectatorCamera** (Spectator Camera) | `_camera` / `_listener` = its own, `_gameplayCamera` = the Player's Main Camera, `_agents` = Enemies, `_agentPrefabs` = BioTestAgent, TechTestAgent, VoidTestAgent (what Squad sizes spawns), `_inputSettings` = `Data/DevTools/SpectatorInputSettings`, `_panLimit` 30, `_overlayMaterial` = empty (or any transparent vertex-colour material such as Sprites-Default), `_scenarios` = the `Data/DevTools/Scenarios` assets | Spectator mode runs only while `_gameplayCamera` is off (deactivate the Player). The object's placed position and angle are the overview it starts on and returns to; overview panning stays within `_panLimit` m of where it looks. Every `EnemyAI` under `_agents` can be watched (in hierarchy order, numbered 1…) and gets a label (`_labelHeight`, `_labelSize`, `_fontSize`). It builds its own HUD, agent inspector, settings panel and world overlays at runtime (needs the scene's `EventSystem` for clicks). The overlays draw with `_overlayMaterial`, or a material made from the built-in `Sprites/Default` shader when it is empty; the cover overlay reads the scene's `CoverMap`. Scenarios find agents by GameObject name (or display name) under `_agents` and use where each stood at scene start as its home; agents sitting a scenario out are switched off, and the scene's `EnemyRespawner` leaves switched-off agents alone. Match stats listen to every hit, credited to the damage source's owner. Save the scene with its Camera and AudioListener disabled. Health bars cache the camera they face at first use, so toggle the Player before pressing Play. Any test scene can use it the same way. |
| `DevTools/SpectatorInputSettings` | The spectator's controls (one `SpectatorBinding` per `SpectatorAction`, two slots each) and default feel (look sensitivity, invert, move speed, smoothing, label size, stick look speed). Kept apart from `Input/InputBindingSettings` on purpose: spectator mode runs with the Player off and reuses the player's keys (WASD, Q/E, Tab, 1–4). Viewers' rebinds and preferences are saved to PlayerPrefs (`spectator_bindings`, `spectator_prefs`); *Reset Bindings to Defaults* in the asset's context menu restores the defaults in the asset itself. Settings is always Escape. Sticks and triggers are fixed on top of the bindings. |
| `DevTools/Scenarios/*AIScenario` | Starting situations for the spectator's scenario list: per agent (by name) home or a position and facing, health and shield shares, invulnerable, an agent it starts aware of; whether unlisted agents sit out, whether the match stats reset, whether it starts paused, whom to watch. Create more with *Create › CGD › DevTools › AI Scenario* and add them to the Spectator Camera's `_scenarios`. |
| **RuntimeNavMesh** (Arena) | `_agents` = Enemies | Builds the surface on load, then snaps the agents under `_agents` onto it. Any scene can use it the same way. |

The Player overrides: `PlayerWeaponLoadout._startingWeapons[0]` = `Weapons/Ranged/T1/AK47_T1`, spawn and RespawnPoint at (0, 1, −19), respawns on death. Add an enemy by dropping an enemy prefab under **Enemies** and setting both `EnemyAI._data` and `EnemyHealth._data` to a data asset with a tree.

### Animation & Ragdoll (Player or Enemy)

Characters play without these. Add them once a rigged model with an Animator Controller exists.

```
Enemy / Player     [EnemyAnimator or PlayerAnimator, Ragdoll?]
  Model            [Animator]                  ← rigged mesh
    …bones         [Rigidbody, Collider, Hitbox?]   ← ragdoll bodies (Unity's Ragdoll Wizard)
```

| Component | Assign | Notes |
|---|---|---|
| **PlayerAnimator** (Player) | `_animator` = the body model's Animator, `_camera`? = PlayerCamera | Reads the Player's movement, health, weapons, melee and grenades. |
| **EnemyAnimator** (Enemy) | `_animator` = the model's Animator | Reads EnemyAI, NavMeshAgent and EnemyHealth. |
| **Ragdoll**? | `_animator`, `_bones`? (empty = every child Rigidbody), `_disableOnDeath` = the root movement collider | Bones stay kinematic while alive. |

**Animator parameters** (all optional; the controller only needs the ones it uses):

| Type | Names |
|---|---|
| Float | `Speed`, `ForwardSpeed`, `StrafeSpeed`, `VerticalSpeed`, `Aim` |
| Bool | `Grounded`, `Crouching`, `Sprinting`, `Sliding`, `Mantling`, `Rolling`, `Dashing`, `Stunned`, `Reloading`, `Alerted` (enemies: any state but patrol, return, stunned and dead), `Cautious` (enemies: on edge after a search), `Dead` |
| Int | `AttackIndex` (combo step, −1 = heavy), `EngageStep` (enemies: `EngageStep` — approach, strike, circle, flank, hold, move to cover, fire from cover, keep distance) |
| Trigger | `Attack`, `Fire`, `Throw`, `Hit`, `Equip`, `DryFire` |

Attack timing comes from `EnemyData` / `MeleeAttackStep`, not from the clips. Author the clips to match.

#### Animation cues (sound and effects in step with the clips)

```
Model              [Animator, AnimationEventRelay, AnimationCueAudio?]
```

- Put **AnimationEventRelay** on the same object as the Animator — Unity delivers Animation Events to that object.
- In a clip, add an Animation Event: **Function** `OnAnimationCue`, **String** the cue name (`Footstep`, `Land`, `Draw`, `MagazineOut`, `MagazineIn`, `Chamber`, `Swing`, `Impact`; case doesn't matter). A misspelled name logs a warning in the editor.
- Events from a clip weighing under `_minClipWeight` (0.5) are ignored, so a blend tree or crossfade doesn't fire each footstep twice.
- **AnimationCueAudio**? (same object) `_sounds` = an `AnimationCueSoundSet` (`Create › CGD › Animation › Cue Sound Set`: a list of cue → `SoundBank`), `_origin`? = where sounds play from. Use it for enemies' footsteps and landings. The player's footsteps stay on `PlayerFootsteps` (surface-aware) — assign its `_animationCues` and leave `Footstep` out of the player's set.

#### Weapon viewmodel (first person)

```
WeaponRig          [WeaponVisuals, ViewmodelAnimator]
  Weapon           [WeaponAimPose]
    WeaponModel    [Animator, AnimationEventRelay, AnimationCueAudio?]   ← rigged gun and arms
```

| Component | Assign | Notes |
|---|---|---|
| **ViewmodelAnimator** (WeaponRig) | `_animator` = the model's Animator (its controller is the shared base), `_weapons` = WeaponController, `_camera`? = PlayerCamera, `_movement`? = Player's PlayerMovement, `_cueAudio`? = the model's AnimationCueAudio | Sets `Aim`, `Sprinting`, `Reloading`, and the `Fire`, `DryFire` and `Equip` triggers (`Equip` also on un-holstering). |

Per weapon, on its `WeaponData` **Animation** section: `ViewmodelAnimations`? = an Animator Override Controller made from the shared viewmodel controller (right-click the controller › *Create › Animator Override Controller*, then swap in that weapon's clips); `CueSounds`? = an `AnimationCueSoundSet` for its magazine/chamber cues. A weapon with neither plays the base controller's clips.

### Hitboxes (Player or Enemy)

- Add child colliders with a **Hitbox** and set `_region` (Head / Body / Limb). The owner fills in automatically.
- Hitbox colliders must **not** be triggers.
- Damage per region comes from a **HitboxProfile** asset (`Create › CGD › Combat › Hitbox Profile`). Without one: all ×1, Head still critical.
- No hitboxes? A collider on the character itself takes Body hits.

---

### Firing range (Sandbox)

Three parts, all in `Sandbox`:

- **`Prefabs/Characters/TargetDummy`** — a stationary stand-in for an enemy: `EnemyHealth` (`_data`, `_hitboxProfile` = `TargetDummyHitboxProfile`, `_healthBar`), `StatusEffectController`, `EnemyHealthBar` (`_alwaysVisible` and `_showNumbers` on: the bar stays up with a shield strip and health/shield numbers), `RespawnOnDeath`, `TargetMover` (`_halfWidth` sway and strafe, `_halfDepth` advance, `_speed`; `_strafeSpeed` and `_strafeTurnTime` — shortest and longest run before a random turn — for Strafe; it obeys `Stunnable`, so stun and ice stop or slow it), `Stunnable`, `RangeDummy` (`_body` = `Body`'s renderer, `_head` = `Head`'s renderer, `_critColor`, `_flashColor`, `_flashTime`); children `Body` (capsule mesh and collider, `Hitbox` Body) and `Head` (sphere mesh and collider, `Hitbox` Head, the critical spot). Both use `Art/Materials/Characters/TargetDummyBody` — an untextured white Lit material, so the faction and crit tints fill the whole part (a textured material would only tint where its texture is light). No `EnemyAI`, `NavMeshAgent` or `DespawnOnDeath`. Four instances stand at 25, 50, 75 and 100 m; none overrides `_data`, `RangeControl` sets it.
- **Range shell** (`ShootingRange` object) — floors, a back wall with the door, side walls, an end wall and a `Roof` (shadows off, so the light still reaches the floor) close the range completely. Every wall, floor and the roof carries a `TiledSurface` (`_metersPerTile` = 1), which sets the texture repeat from the object's size so the pattern stays one repeat per metre however the object is scaled; all walls and the roof share `ShootingRangeWallShort`, the floors `ShootingRangeFloorSmall`. A new wall piece only needs a cube with the wall material and a `TiledSurface`.
- **Map** — a `WorldMap` object (`WorldMapArea`, 28 × 122 m centred on the range, fog of war on, `_captureMask` without the `Ceiling` layer) feeds a `Minimap` element added under the HUD (`_area`, `_viewer` = the Player's camera), and a `MapRevealer` added to the Player lifts the fog around it. Both roofs are on the `Ceiling` layer (new, layer 7) so the top-down capture sees the floor instead of the roof.
- **Lane cover** — four low walls (`Lane Cover 1`–`4`, 1.2 m high, wall material with `TiledSurface`) stand in the lane at 10, 12, 36 and 61 m from the firing line, clear of the dummies' paths, for testing line of sight, penetration and vaulting. They are plain scene cubes.
- **Melee room** (east of the booth) — an open doorway in the booth's east wall (4 m wide, 3.5 m high, no door) leads into a closed 12 × 10 m side room with its own floor, walls and roof (same `TiledSurface` setup). One `TargetDummy` instance (`TargetDummyMelee`, 12 m east of the booth's centre, facing the doorway) stands inside. It is the fifth entry in `RangeControl._dummies`, so the switches change it like the others and it sways across the room and advances along it (keep `_halfDepth` inside the room).
- **`RangeControl`** (object `RangeControl`, scene root) — `_factions` = `Factions/TechFaction`, `BioFaction`, `VoidFaction` (order matters for Mixed), `_dummies` = the four lane dummies' `RangeDummy` (nearest first), then the melee-room dummy's. Holds the range's settings (faction or Mixed, tier 1–3, movement Still, Sway, Advance or Strafe, distance, infinite ammo) and applies them at start and on every change; the Ammo setting goes to the `WeaponController` of whoever presses its switch (the Player), so it needs no reference. The object itself stands on the firing line (z 5.6), the point distances are measured from. `_laneDummies` = the four lane dummies (the melee-room dummy isn't in it, so it never moves); `_distanceScales` (1, 0.5, 0.2) are the shares of each lane dummy's placed distance the Distance switch cycles through, so the lane runs 25–100 m, 12.5–50 m or 5–20 m.
- **`LaneRuler`** (object `LaneRuler`, scene root, on the firing line at z 5.6) — writes the distance (`_interval` 25 m, `_marks` 4) on the floor along both walls when the scene starts; nothing to assign.
- **`DamageReadout`** (object `DamageReadout`, scene root) — builds its own panel on a screen-space overlay canvas, halfway up the left edge so it stays clear of the corner HUD (`_size`, `_edgePadding`). `_range` = the `RangeControl`, so the Reset switch clears it. It listens to `CombatEvents` and shows the player's hits on dummies only.
- **`Prefabs/Environment/Level/RangeSwitch`** — a pillar with a `Collider` and `RangeSwitch` (`_range` = the `RangeControl`, `_setting`). Six instances stand on the firing line (z 3.8, 1.2 m apart from x −3 to 3): `Faction`, `Tier`, `Movement`, `Distance`, `Reset`, `Ammo`, each with its own `_color` tint (Ammo yellow).

Starting setup is *Mixed, tier 1*: dummy *n* plays the first faction's enemy, the next dummy the second faction's … (Tech, Bio, Void, Tech, then Bio in the melee room). Choosing a faction makes every dummy play that faction's enemy of the chosen tier, taken from the first prefab on the faction's roster for that tier (`FactionDefinition` tier lists), so retuning an enemy retunes its dummy. The body is tinted with the faction's colour (`FactionDefinition` colour) and the head is always `_critColor`. Placeholder tier 2–3 affixes are not rolled. The Target Practice quest asks for one kill each of the three tier-1 enemies, which Mixed provides.

## Pickups & Interactables

| Object | Components | Assign |
|---|---|---|
| Weapon pickup | trigger Collider, `WeaponPickup` | `_data` = a `WeaponData`, **or** add `RandomWeaponPickup` with `_categories` — firearm or melee categories (+ `_seed`? for a fixed weapon; empty = random, or the level's seed when a generated level places it) |
| Ammo pickup | trigger Collider, `AmmoPickup` | `_munition` = a munition asset, `_amount` |
| Health pickup | trigger Collider, `HealthPickup` | `_amount` |
| Item pickup | trigger Collider, `ItemPickup` | `_item`, `_count` |
| Door | solid Collider, `Door` | Pivot on the hinge. `_openAngle`, `_holdDuration`?, `_key`? to lock it, `_condition`? = a `ConditionLock` to keep it locked until conditions are met |
| Condition lock | `ConditionLock` (on the door's object) | `_label` (shown as "Label 1/2"), `_required` (0 = all `_signals`), `_signals`? = `QuestSignal`s that each count once, `_onOpened`?. Other sources: a `LockTerminal`, or any UnityEvent calling `ConditionLock.Satisfy` with its own object as the argument (e.g. an `EventInteractable`'s `_onInteract`) — each distinct object counts once |
| Terminal | Collider, `LockTerminal` | `_lock` = the door's `ConditionLock`, `_label`, `_holdDuration`, `_indicator`? (Renderer tinted Off/On). Ready-made: `Prefabs/Environment/Level/LockTerminal` |
| Switch | Collider, `Switch` | `_label`, `_doors` |
| Anything else | Collider, `EventInteractable` | `_label`, `_requirement`?, `_onInteract` event; `_holdDuration` to hold the key, or tick `_mash` (with `_tapsRequired`, `_tapDecayPerSecond`) to tap it repeatedly instead |
| Chest | Collider, `LootContainer`, `LootDropper` | Untick the dropper's `_dropOnDeath`. Ready-made by type in `Prefabs/Environment/Loot/` (copies of `RewardCache`, each with its own `_label`, body colour and `_table` from `Data/Loot/Containers/`): `WeaponCrate`, `EquipmentLocker`, `ConsumableCase`, `ResourceCrate`, `MunitionsCrate`, `GlobalChest`; the dropper's `_subtypes` on the first four use the filters in `Data/Loot/Filters/` (none on the resource crate and chest). Place them as a room rule's *Centerpiece*, *Dressing* or *Offerings*, an encounter's reward, or by hand |
| Breakable crate | solid Collider, `Destructible`, `LootDropper`, `DespawnOnDeath` | `_maxHealth`; `_delay` 0 |
| Crafting station | Collider, `CraftingStation` | `_label`, `_recipes` = recipe assets (e.g. all of `Data/Crafting/`) |
| Ammo cache | solid Collider, `AmmoCache` | `_munitions` = all four munition assets, `_uses`, `_magazines`, `_contents`? = the visible ammo. Ready-made: `Prefabs/Environment/Props/AmmoCache`. |
| Resource node | solid Collider, `ResourceNode` | `_resource`? (empty = placeholder, yields nothing), `_amount`, `_harvests`, `_holdDuration` (or tick `_mash` with `_tapsRequired`, `_tapDecayPerSecond` to hack at it by tapping), `_visual`?. Ready-made: `Prefabs/Environment/Props/ResourceNode`. |

- Add **InteractionHighlight**? to any interactable to tint it while aimed at (URP Lit/Unlit).
- Custom interactables just implement `IInteractable` — no extra wiring.

### Prefabs

| Prefab | Needs |
|---|---|
| `Prefabs/Weapons/Projectile` | `Projectile` + a renderer. `TrailRenderer`? is reset on every launch. |
| `Prefabs/Weapons/FragGrenade` | Root: `Rigidbody` (mass 0.4, no linear drag so the trajectory preview stays true, *Use Gravity* on, rotation frozen on all axes so spin can't change a bounce, *Interpolate* on, *Continuous Dynamic* so a thrown grenade can't pass through enemies), solid `SphereCollider` with `Materials/Physics/Grenade` (bounciness 0 and friction 0, both *Minimum* combine, so the physics engine neither bounces nor grips it — the bounce comes from the `GrenadeData`), `Grenade` (`_body` = the Body child's renderer, which takes the grenade's colour and blinks). One child, `Body` — a plain sphere (scale 0.15, matching the collider) with `Materials/Environment/Props/GrenadeBody`, a URP Lit material; the grenade's tint is set on its `_BaseColor`. Shared by every grenade type |
| `Prefabs/VFX/ExplosionFlash` | A sphere with `ExplosionFlash` and the transparent `Materials/VFX/ExplosionFlash`; set as a grenade's `ExplosionEffect`. Pooled automatically |
| Zone prefabs | `PersistentZone` — spawned by an ActionTimeline's zone event |
| One-shot VFX | `PooledLifetime` (`_lifetime` 0 = until particles finish) |

### Loot

- **LootTable** (`Create › CGD › Loot › Loot Table`): guaranteed entries + weighted draws; entries can be items, other tables, or prefabs. New entries start at weight 0 — set them. *Tier Checks* set gear rarity: one row per tier with its chance when no higher tier hit (give the lowest tier 100%), checked from Legendary down whatever the row order; the outermost table with checks decides for every table nested in it, so shared pools carry none; a row's optional *Pool* (another LootTable) is rolled instead of the drawn gear when that tier hits, with all its gear at that tier.
- **LootDropper** needs two shared prefabs: an `ItemPickup` prefab and a `WeaponPickup` prefab (leave its `_data` empty). Optional `_subtypes`: weighted **LootFilter** assets it rolls one of per seed (an entry without a filter = the plain table); a `LootContainer` on the same object shows the rolled filter's label after its own.
- **LootFilter** (`Create › CGD › Loot › Loot Filter`): `_label`, `_kinds` (Firearm, Melee, Armor, Shield, Artifact, Attachment, Healing, Buff, Throwable, Munition, Resource), `_weaponTypes`? and `_ammoTypes`? to narrow firearms and munitions (empty = any). Prefab entries never pass a filter; a filter that allows nothing in the table is ignored.

---

## Equipment, Consumables & Crafting

| Component | Assign | Notes |
|---|---|---|
| **PlayerEquipment** (Player) | — | Worn armor. Needs `CharacterStats` on the Player for armor stats to count. |
| **PlayerItemSlots** (Player) | `_slots`? = starting items for keys 5–8 (consumables or throwables), `_wheel`? = HUD SlotWheel | Holding a key opens the item wheel. Throwables need `ThrowableController`. |
| **CharacterPanel** (HUD) | `_input` = Player, `_inventory`, `_equipment`, `_loadout`, `_itemSlots` = the Player's components | Opens with Tab. |
| **ItemSlotsHUD** (HUD) | `_slots`, `_inventory`, `_throwing`? = the Player's components | |
| **SlotWheelHUD** (HUD, starts inactive) | — | Shared by `PlayerItemSlots._wheel` and `PlayerWeaponLoadout._wheel`. Keep it near the bottom of the HUD so it draws on top. |
| **CraftingPanel** (HUD) | `_input` = Player | Opens when a `CraftingStation` is used. |

- Attachments need a free slot: gear only has slots when its category/definition has a `_rollProfile` (weapons have none yet — see [Assets](#assets)).
- Armor slot restrictions come from the attachment's `_armorSlots` (empty = fits anything, including weapons).

## Vendors & Dialogue

```
Gunsmith           [Collider, Npc, Vendor]      ← any NPC: a Collider and an Npc; add a Vendor to trade
HUD
  Dialogue         [DialoguePanel, CanvasGroup]
  Shop             [ShopPanel, CanvasGroup]
```

| Component | Assign | Notes |
|---|---|---|
| **Npc** | `_displayName`, `_dialogue` = a `DialogueDefinition`, `_faceListener` | Interact to talk. Needs a non-trigger Collider within the Player's interact range. |
| **Vendor**? | `_displayName`, `_catalog` = a `ShopCatalog`, `_seed`? (empty = new stock each play, or the level's seed when a generated level places it), `_restockInterval`? (0 = once) | Opened by a dialogue choice with `OpenShopDialogueAction`, or by anything calling `OpenFor(player)`. |
| **DialoguePanel** (HUD) | `_input` = Player | Choices can also be picked with the weapon-slot keys (1–4). |
| **ShopPanel** (HUD) | `_input` = Player | |
| **PlayerInventory** | `_startingStacks` += `Economy/CreditsCurrency` × amount | Money is an inventory item. Sandbox starts with 1,500 cr. |

**Dialogue assets** (`Create › CGD › Dialogue › …`):
- **Dialogue**: nodes with an `Id`, a line of `Text` and `Choices`.
- **Choices**: each has `Text`, `Next` (a node Id; empty = end), optional `Conditions` and `Actions`.
- **Actions** are shared assets:
  - `OpenShopDialogueAction` opens the speaker's Vendor.
- **Conditions** are one asset per rule:
  - `Has Item` shows a choice only while the player carries an item.

**Shop assets** (`Create › CGD › Economy › …`):
- **Shop Catalog**: the currency, a `PriceTable`, a list of **stock sources**, and whether the vendor buys items and takes trade-ins.
- **Item Stock**: fixed goods. Stackables are sold in bundles; gear rolls fresh on each restock. The price comes from the price table unless overridden.
- **Rolled Weapon Stock**: N weapons from a set of categories and tiers.
- **Price Table**: the quality → value curve, buy markup and sell ratio. Every item's worth starts from its `_baseValue` (on the item or weapon-category asset).

The Sandbox **Gunsmith** stands at (5, 0, −3):
- It uses `GunsmithDialogue` and `GunsmithShopCatalog`: ammo for every caliber, plus 4–6 rolled weapons.
- Rolled weapons are all Common until the weapon categories get a `_rollProfile` (see [Assets](#assets)). The stock's tier range needs one to have any effect.

## Dev Console

| Component | Assign | Notes |
|---|---|---|
| **DevCommands** (Player) | `_catalog` = `DevTools/DevCatalog`, `_aim` = Main Camera, `_quests`? = QuestTracker, `_map`? = WorldMapArea | Development builds only unless `_allowInReleaseBuilds`. |
| **DevConsolePanel** (HUD) | `_input` = Player, `_commands` = the Player's DevCommands | Opens with ` (backquote). |

- Type `/help` for commands (every command starts with `/`). Items and weapon categories come from `DevCatalog`'s `_itemCatalog` (= `Items/DefaultItemCatalog`), so every item in the project can be handed out with no extra step. Enemy prefabs, buffs and dodges are still listed on `DevCatalog` itself — add new ones there.

## Stats & Buffs

- **CharacterStats**? on the Player or an enemy holds buffs, debuffs and permanent presets (e.g. difficulty).
- Health, armor and weapon/attack damage read through it automatically.
- `DamageBoostAbility` (a Stat Buff ability) only works if the Player has `CharacterStats`.

## Quests

| Component | Assign |
|---|---|
| **QuestTracker** (Player) | `_quests` = every quest this scene can run, chained ones included (e.g. `TargetPracticeQuest`, `ResupplyQuest`) |
| **QuestHUD** | `_tracker` = QuestTracker |
| **QuestSignalTrigger**? | trigger Collider + `_signal` — "reach this place" objectives |

- Objectives count **kills** (`EnemyData`), **pickups** (`ItemDefinition`) or **signals** (`QuestSignal`).
- Raise a signal from any UnityEvent (e.g. `EventInteractable`) via the signal asset's `Raise()`.
- Start a non-auto quest from a UnityEvent with `QuestTracker.StartQuest`.

## Grenade data

A grenade type is one `GrenadeData` asset (`Create › CGD › Weapons › Grenade Data`, in `Data/Weapons/Throwables/`), used by a `ThrowableDefinition` item. All its tuning is there; the prefab only draws it.

| Group | Fields |
|---|---|
| Identity | `GrenadePrefab` (any prefab with `Rigidbody`, `Collider`, `Grenade`), `BodyColor` |
| Throw | `MinThrowSpeed` (tap) → `MaxThrowSpeed` (fully charged) in m/s, `ChargeTime` (seconds to full, 0 = always full), `ThrowAngle` (upward lift, degrees), `InheritVelocity` (share of the thrower's movement), `GravityScale` (× world gravity in flight; also used by the preview and enemy aim) |
| Landing | `Bounciness` (share of the speed into a surface it rebounds with; weak rebounds settle), `ImpactSpeedKept` (share of speed along the surface kept per hit), `GroundDrag` (slowing on the ground, m/s²) |
| Fuse | `Fuse` = Timed or OnImpact, `FuseTime` (OnImpact: the longest it may fly), `ArmTime`, `FuseBlinkColor` |
| Explosion | `ExplosionRadius`, `ExplosionDamage`, `EdgeDamage` (share left at the edge), `ArmorPenetration`, `DamageType`, `HitMask`, `NeedsLineOfSight` + `BlockerMask`, `PushForce`, `NoiseRadius` |
| Aftermath and effects | `LingeringZone` (a smoke cloud or `Prefabs/VFX/FireZone`, left where it goes off), `ExplosionEffect` + `FlashColor`, `CameraTrauma` + `ShakeRadius`, `OnHitEffects` (a blast with no damage still applies them), `ThrowSound`, `ExplosionSound` |
| Preview | `TrajectorySteps`, `TrajectoryStepTime`, `PreviewColor` |

Range on flat ground is at most about speed² ÷ (9.8 × `GravityScale`). Every included grenade uses `GravityScale` 2, so the Frag Grenade (fixed, 24 m/s) reaches about 29 m thrown at 45° and about 13 m thrown straight ahead from eye height.

Included types (data in `Data/Weapons/Throwables/`, items in `Data/Items/Throwables/`, all in `ConsumablesPool` and the dev catalog):

| Type | Fuse | Blast | Notes |
|---|---|---|---|
| Frag | Timed 2.5 s | 5 m, 80 | Falls to 10% at the edge |
| Smoke | Timed 1.5 s | 4 m, none | Leaves a smoke cloud |
| Contact | On impact (5 s limit) | 3.5 m, 60 | Faster, flatter throw |
| Incendiary | On impact (4 s limit) | 3.5 m, 30 | Burn status (`IncendiaryBurnEffect`) and a 6 s `FireZone` that burns everything in it |
| Stun | Timed 1.8 s | 6 m, none | Applies `StunEffect` (3 s) to everything in line of sight, full strength across the blast |

## Map & Minimap

| Component | Assign | Notes |
|---|---|---|
| **WorldMapArea** | `_size` = area in metres (X, Z), `_source` | Centre it on the playable area. |
| **MapRevealer** (Player) | `_area` = WorldMapArea | Clears fog around the Player. |
| **MapMarker** | shape, colour, size | Add to enemies, pickups, objectives. Tick `_clampToEdge` for objectives. |
| **MinimapHUD** / **WorldMapHUD** | see [HUD](#hud) | |

Background `_source` options:

- **Capture** — top-down snapshot of `_captureMask` layers at scene start.
- **Texture** — your own top-down image (+Z = up).
- **MapGraph** — draws `_graph` as a room schematic (generate the graph first).
- **None** — plain colour.

## Feedback

| Component | Assign | Notes |
|---|---|---|
| **FeedbackPlayer** (Player prefab) | `_cameraEffects` = Main Camera; `_notifications` = NotificationHUD and `_screenFlash` = ScreenFlashHUD are set on the Player instance in each scene (they live in the HUD prefab) | Without it, no feedback shows. `_vibration` toggles rumble. |
| **CombatFeedback** (Player prefab) | the `Hit`, `CriticalHit`, `Kill`, `DamageTaken`, `LowHealth` presets | The hit marker needs no reference (`HitMarkerFeed`). |
| **QuestFeedback** (Player prefab) | `_tracker` = the Player's QuestTracker + the four `Quest…` presets | |

All presets are in `Data/Feedback/`.

## Stealth

| Component | Assign | Notes |
|---|---|---|
| **Stealthable** (Player prefab) | `_settings` = `Stealth/StealthSettings`, `_spottedFeedback`? = `Feedback/SpottedFeedbackPreset` | Without it, enemies ignore bushes and smoke, and the player's footsteps can't be told apart from standing still (both count as moderately loud). Its settings asset also holds *Noticeability*: crouch and still visibility, crouch and sprint footstep loudness. |
| **ConcealmentZone** (bush) | a Collider sized to the foliage · `_kind` = Bush · `_requiresCrouch`? | Collider becomes a trigger. Put the object on **Ignore Raycast** so shots pass through. |
| **ConcealmentZone** (smoke) | on `Prefabs/VFX/SmokeCloud` — `_kind` = Smoke, `_blocksSight` ticked | Spawned by `Weapons/Throwables/SmokeGrenadeData`, thrown via the `SmokeGrenadeThrowable` item. |
| **StealthAbility** | add `Abilities/StealthAbility` to a PlayerAbilities slot | Needs Stealthable. |

- Enemies need **no** changes — `EnemyPerception` checks stealth automatically. An enemy only hides if its prefab has a **Stealthable** (the three AITest agent prefabs and the minion variant do; `_settings` can stay empty for the defaults). It reads its NavMeshAgent where the player would use PlayerMovement (for footsteps and standing still), and its attacks, grenades and abilities call `Reveal()` (abilities whose `GivesAway` is false — Stealth, Ambush — don't).
- `SmokeCloud` has no visual yet: add a Particle System (and a matching collider radius, default 4 m).

## Camera Effects & Lock-On

| Component | Assign | Notes |
|---|---|---|
| **CameraEffectsController** (Main Camera) | `_settings`? = `CameraEffects/DefaultCameraEffectSettings`, `_shakeOnDamageOf`? = PlayerHealth | Must be on the object with the Camera. |
| **PlayerLockOn** (Player) | `_camera` = PlayerCamera, `_selector` = `Targeting/DefaultConeTargetSelector` | T. |

## Map Graph (editor tool)

Open **Window › CGD › Map Graph**, pick `Map/SandboxMapGraph`, press **Generate**. Scenes use graphs through a [generated level](#generated-levels).

A map **style** (`MapGenerationSettings`) is what a `MapGraphAsset` or `LevelBuilder` references. It wires two kinds of asset together:

```
…MapGenerationSettings (style)    Map/
  _layouts  = one or more MapLayoutSettings + weight   Map/Layouts/   ← shape; each seed picks one by weight
  _content  = one MapContentSettings                   Map/Content/   ← room types, guaranteed rooms, pacing, sections, intensity, factions
  _modifiers = MapRunModifier assets + _modifierCount  Map/Modifiers/ ← optional twists, picked per seed
```

| Asset | Create menu | Assign |
|---|---|---|
| **MapLayoutSettings** | CGD › Map › Map Layout Settings | `_growth` (Path First, or Organic: rooms spread from Start, `_organic._intersectionChance` / `_compactness`, Boss placed last; uses room counts, Min Boss Depth, Max Connections, gates and loops but not direction, winding, branches or Max Spread), sizes, main path (incl. `_direction`: East = left to right, `_maxSpread` = rows around Start's; Any = sets off in any direction, each step further from Start, `_maxSpread` = cells from the main path), branches, loops (incl. `_oneWayShortcutChance`), gates (incl. `_minDepth`, the key preferences, `_terminalLockChance` and `_terminalCount`), `_directDoorChance` (chance a plain link between neighbouring cells is a direct door) |
| **MapContentSettings** | CGD › Map › Map Content Settings | `_nodeRules` (incl. `_minSpacing`, `_wantsSpace`), `_fillType`, `_guarantees` (type + spot: Within Depth / Behind Every Gate / Before Boss, count, depth band), `_pacing` (`_combatTypes`, `_maxCombatInARow`, `_restType`), intensity curve, room tiers by depth from Start (`_tier2Depth` 0.35, `_tier3Depth` 0.7, `_tierJitter`, `_tierOutlierChance` 0.15 for a room a tier off its depth, `_firstRoomsMaxTier` 1 for rooms next to Start), `_sections` = `Map/Sections/` assets in run order, `_factions` = Faction assets from `Data/Factions/`, `_factionMixes` = `Map/FactionMixes/` assets + weight (empty = an even split). With factions listed every room belongs to one — the validator flags rooms without |
| **MapGenerationSettings** | CGD › Map › Map Generation Settings | `_layouts` (at least one, weight 0 = never unless all are 0), `_content` (required), `_modifiers` = `Map/Modifiers/` assets, `_modifierCount` (0–1 on the ready-made styles, 0–2 on Random), `_maxWarnings` / `_maxAnomalies` (2 / 1), `_nodeSpacing` (one room cell in graph units: the level builder's cells and the Map Graph window's snap grid; 220 × 110, also used for a graph without a style) |
| **MapCategorySettings** | CGD › Map › Map Category Settings | `Map/Categories/StandardMapCategorySettings`, assigned to each content's `_categories` (empty = rooms stay uncategorised). `_zones` (share and pockets per ship zone; a zone whose categories are all off is skipped), `_outOfZoneWeight`, `_categories` (weight per category — 0 keeps it out until a `RoomFunction` of that category exists — and `_preferredSections`), `_sectionPreference`, `_repeatDecay`, `_affinities` (pairs: 1 neutral, above 1 likes, below 1 clashes), `_sameCategoryAffinity`, `_directDoorStrength`, `_clashBelow` (the validator's threshold) |
| **MapSectionDefinition** | CGD › Map › Section | `_displayName`, `_color` (Section view mode) |
| **MapRunModifier** | CGD › Map › Run Modifier | name, `_kind` (Warning / Anomaly), description (announced in the level), `_weight`, `_lootLuck` (added to every loot container in the level), per-type adjustments (weight ×, extra min/max), extra rooms/loops/gates, locked/secret chance ×, intensity offset, `_factionMix` override |
| **MapFactionMix** | CGD › Map › Faction Mix | `_dominance` (0 = even split, 1 = one faction holds almost everything), `_scatter` (chance a room is a random faction; 1 = fully random), `_pocketsPerFaction`. Every room always gets a faction |

- **Generate** stays disabled until the style has content and at least one layout.
- **Blueprint** shows the floor plan; its toolbar picks the `LevelBuildSettings` to lay out with (defaults to the project's first one, e.g. `Level/DefaultLevelBuildSettings`) and the fill (room type / faction); *Props* draws each room's furniture. It uses the graph asset's seed, as `LevelBuilder` does with `_seed` empty. Double-click a room (or *Room Lab* in the side panel) to open it in the Room Lab.
- **Analyze** opens the style report for the asset's style (seed count and first seed in its toolbar; the first seed can be a number or any text).
- **Play** opens `Scenes/MapTest`, points its `Level` (LevelBuilder) at the asset with the asset's seed and enters play mode; **Play from here** (side panel) also sets `_startRoom` to the selected node. Nothing is saved to the scene: the previously open scenes are reloaded when play mode ends. It needs a LevelBuilder somewhere in MapTest.
- **Gallery** opens the seed gallery for the asset (`MapSeedGalleryWindow`): previews for a page of seeds counting up from its toolbar's first seed; **Use** on one generates the asset from that seed.
- A room function only prefers sections listed in a style's content; a style without `_sections` gives every room no section.
- A new map style is usually just a new style asset pointing at existing layouts and content.

### Room Lab (editor tool)

Open **Window › CGD › Room Lab**, or *Open in Room Lab* at the top of any `RoomFunction`'s Inspector. Nothing to wire into a scene; it reads assets only.

| Field | Assign | Notes |
|---|---|---|
| **Build with** | a `LevelBuildSettings` (defaults to the project's first, e.g. `Level/DefaultLevelBuildSettings`) | Room size, shapes, functions (the Place list), room content rules (cover and clutter per type), faction designs |
| **Content** (made-up rooms) | a `MapContentSettings` (defaults to the project's first) | Only its faction list is used, for the Holder / Breaching popups |
| **Map graph** (map rooms) | a generated `MapGraphAsset` | Built with the graph's own seed, as `LevelBuilder` does with `_seed` empty |

## Generated Levels

A scene can build its level from a map graph at load instead of using hand-placed geometry.
**`Scenes/MapTest`** is ready-made: the Sandbox systems (Player, HUD, GameManager, GameFlow, EventSystem, Global Volume, RespawnPoint, Directional Light) and a `Level` object generating from `Map/RandomMapGenerationSettings` with `Level/DefaultLevelBuildSettings`, seed 0 (a new map every play and every restart; empty does the same). Change `_generation` or `_seed` on `Level` to test a style or keep a map; set `_graphAsset` to play a hand-edited graph. Its NavMeshSurface collects **Physics Colliders**, so the wall kit's visual pieces stay out of it. It also has the map set up: a `WorldMap` object (`WorldMapArea`, fog of war on) that `LevelBuilder` fits to the level and paints with the level's blueprint; `Minimap`, `WorldMap` (M) and `ScreenFlash` elements added under the HUD; and `MapRevealer` + `FeedbackPlayer` added to the Player (run modifiers and pickups are announced). `Level` has `_objectives` = `Level/Objectives/DefaultMapObjectiveSettings`. The expedition loop is wired too — see *Expedition loop* below — and `Level` has `_encounters` = `Level/DefaultEncounterSettings`, so encounter rooms play out. These are scene additions to the HUD and Player prefab instances — the prefabs themselves (and Sandbox) are unchanged.
For another scene, start from a copy of the Sandbox scene and delete its level geometry, target dummies and NavMesh data.

```
Level              [LevelBuilder, NavMeshSurface]   ← at the origin, unrotated
  (rooms, corridors, doors, props and patrol routes are created here at runtime)
```

| Component | Assign | Notes |
|---|---|---|
| **LevelBuilder** | `_settings` = `Level/DefaultLevelBuildSettings` · `_graphAsset` = a `MapGraphAsset` **or** `_generation` = a `Map/…MapGenerationSettings` style (e.g. `BranchingMapGenerationSettings`, or `RandomMapGenerationSettings` for a different shape each run) · `_seed` (a number or any text; empty or 0 = random; with `_graphAsset`, empty or 0 = the asset's seed) · `_startRoom` (testing: a node id to start and respawn in instead of the Start room; -1 = Start) · `_navMesh` = its NavMeshSurface · `_player` = Player · `_spawnPoint` = RespawnPoint · `_worldMap`? = WorldMapArea · `_blueprintMap` (on: the map shows the level's floor plan — rooms coloured by type, walls, gates — and covers exactly the level; off: the area's own background) · `_encounters`? = `Level/DefaultEncounterSettings` (empty = encounter rooms play as plain rooms) · `_objectives`? = `Level/Objectives/DefaultMapObjectiveSettings` (empty = no map objectives; needs a `QuestTracker` on the player) · `_coverMap`? = the scene's `CoverMap` (found right after the NavMesh is built, instead of on the first enemy's cover search) · `_announceRooms` (on: the first visit to each room is named in the HUD feed; needs a FeedbackPlayer) | With objectives, the Boss room's doors become `_bossDoorPrefab` (empty = the build settings' terminal door; it needs a `ConditionLock`), labelled "Objectives". Logs a warning for every corridor that had to cross another. Announces the run's modifiers through the HUD feed (needs a FeedbackPlayer in the scene). |
| **NavMeshSurface** | Collect Objects = **Current Object Hierarchy** | Rebuilt at runtime. Don't bake it. |

`LevelBuildSettings` holds the grid sizes, wall materials (`_lowCoverMaterial` = Gridbox `Prototype_512x512_Grey2`, as the AI test arena's low cover: structure under 1.7 m — broken columns, wall stubs, counters — is drawn in it, full-height pieces in `_wallMaterial`; empty = the wall material), corner bevels (`_roomChamfer` 0–2 tiles, default 1; `_chamferCorridorBends`), the wall art (`_wallKit` = `Level/WallKits/TechWallKit`, `_factionWallKits` = TECH/BIO/VOID Faction → their `…WallKit`; empty `_wallKit` = plain box walls), `_factionDesigns` (each faction's `Level/Designs/…FactionDesign`; a faction without one is built plainly), how round curved rooms are (`_curveSmoothing` 0–3 passes, default 2; 0 = 45° walls), door prefabs, the key prefab, door signs (`_buildDoorSigns`, `_doorSignMaterial` — empty = the floor material, tinted and made emissive — `_doorSignLabels` (the room beyond named under each sign, with faction and tier; TextMeshPro, default font) and `_dangerIntensity`), the default room **Shapes**, the **Functions** catalogue (`_functions`: every `RoomFunction` in `Level/Functions/` — a room picks one of its own category, whatever its node type; a landmark is used at most once per map) and one **Rooms** entry per room type.
Each Rooms entry lists enemy prefabs (count read at the room's intensity), a centrepiece, a *Dressing* asset (cover and clutter; swapped per faction), an *Extra Dressing* (placed after it and never swapped: the Stealth entry uses `ConcealmentRoomDressing`) and offerings — not where the room is; the Breach entry uses the Combat enemy count (its enemies come from the two factions' rosters).

| Asset | Folder | Sets |
|---|---|---|
| **RoomFunction** (48, one catalogue: Quarters, CabinRow, Commons, School · Park, Pool, Gym, WellnessCenter, ObservationDeck · Casino, Theatre, Bar, ExhibitHall, GrandAtrium (unique) · Promenade, Restaurant, FoodCourt, Kitchen · MedicalBay, Pharmacy, Ward, Morgue · SecurityOffice, Armory, Barracks · Workshop, ProcessingPlant, LifeSupport, EngineRoom, ReactorCore (landmark) · Bridge (unique), ControlRoom, Offices · HydroponicsBay, Farm, WaterTreatment · Laboratory, DataCenter, Library · Lobby, CargoBay, DockingBay, Hangar, EscapePodBay, Storage, Junction · Chapel, Shrine — plain placeholders except the original few) | `Level/Functions/` | `_category` (RoomCategory; a map node with a category only picks functions of that category — Lobby/CrewQuarters Accommodation, Park Recreation, Casino/GrandAtrium Entertainment, Restaurant Commercial, ReactorCore Engineering, CargoBay/DockingBay/SupplyDepot Transit), `_preferredSections` + `_sectionPreference` (weight × inside them), `_chamfer` (-1 = the settings' `_roomChamfer`; GrandAtrium and DockingBay use 2), `_shapes` (empty = the settings' shapes), `_cells` (1–2 each way), `_size` in tiles (single-cell only; 0 = `_roomTiles`), `_wallHeight` (0 = the settings'), `_openCeiling`, `_landmark`?, `_structure` rules in order, `_props` |
| **Enemy prefab** (MaintenanceBot, Scavenger, Husk) | `Prefabs/Characters/Enemies/` | Root: `NavMeshAgent`, `EnemyHealth` (`_data`, `_hitboxProfile` = the target dummy's, `_healthBar`), `EnemyAI` (`_data`, `_obstacleMask` = Default), `Stunnable`, `StatusEffectController`, `EnemyHealthBar`, `EnemyStateVisuals` (`_renderers` = Visor), `DespawnOnDeath`, `MapMarker` (faction colour). Children: `Body` (capsule collider + `Hitbox` Body), `Head` (sphere collider + `Hitbox` Head), `Visor`, faction accents (visual only). Stats in `Data/Enemies/…EnemyData` — keep low tiers near 100 effective health (`EnemyData.EffectiveHealth` = (health + shield) × (1 + armour/100)) |
| **WallKit** (Tech, Bio, Void) | `Level/WallKits/` | `_straight` (repeated along straight walls), `_angled` (bevels and curves; empty = straight), `_post` (corners and wall ends), `_doorFrame` (every doorway, room side), `_pieceLength` / `_pieceHeight` (authored size, 3 × 4 m), `_postMinAngle`. Pieces: origin on the floor at the wall's start, +X along it, +Z toward the room; door frames like door prefabs (+Z out of the room). Visual only — the box walls stay as invisible collision. Placeholders in `Prefabs/Environment/WallKit/` — plain colour-coded blocks, one per faction (a single panel for walls, a column for posts, two jambs and a header for door frames), no decoration; materials in `Art/Materials/Environment/WallKit/` |
| **LandmarkRoomDefinition** (ReactorCore) | `Level/Landmarks/` | `_prefab` (interior; origin at the room's south-west floor corner), `_sizeTiles`, `_doorways` (tile + side), `_occupiedTiles` (furniture) |
| **RoomShape** (Rectangle, L, T, U, Cross, Ring, Dome) | `Level/Shapes/` | parts, `_connections` range, `_curvedWalls` (Dome) |
| **Structure rules** (Wide/Dense Pillar Grid, Broken Column Grid, Column Ring, Corner Pillars, Divider Wall, Service Counter, Checkpoint Barrier, Wall Remnants, Office Maze) | `Level/Structure/` | spacing, sizes, heights (every rolled or set height is pulled into low cover, 0.9–1.2 m, or high cover, 2.2 m and up — `CoverHeights`; 0 = full height); optional `_pillarPrefab` (origin at the base) replaces the plain box; grids: `_missingChance`, `_brokenChance` and `_brokenHeight` (Broken Column Grid: 25% gone, 55% of the rest snapped off at 0.7–2.4 m); partitions: `_minZoneTiles` (8: a wall that would leave less floor than this on either side is skipped; Checkpoint Barrier is one at the middle, 1.1 m high with 3-tile gaps); **WallRemnants**: `_count` per 64 tiles, `_length` in tiles, `_height` rolled per piece (ragged tops; never across a walkway or beside a doorway, never zone dividers); **OfficeMaze**: `_hallTiles`, `_officeDepth`, `_officeWidth`, `_suiteChance`, `_throughChance`, `_doorHeight` (halls along the longer axis, offices between them, a doorway with a lintel onto a hall from every office; rooms under 6×5 tiles are left alone) |
| **FactionDesign** (Tech, Bio, Void) | `Level/Designs/` | `_faction`; *Floor plans*: `_shapes` (a shape asset and a weight multiplier — 0 never, above 1 more often) and `_roomSizeScale` (0.5–1.25 of a function's room size; multi-cell halls and landmarks keep theirs); *Structure*: `_structure` rules, applied only to rooms whose function has none of its own; *Dressing*: `_dressing` (the faction's own props, placed last), `_swaps` (a room type's generic *Original* dressing → this faction's *Replacement*: the same cover and loot clutter in its own pieces), `_fillShare` (share of the floor props may take) and `_propScale` (multiplies every prop count, required ones excepted). Shipped: Tech (rectangles and crosses; `WidePillarGrid` + `CheckpointBarrier`; sparse, 0.3 / 0.9×), Bio (domes and rings; `BrokenColumnGrid` + `WallRemnants`; 0.3 / 1×), Void (L/U/T plans, rooms at 0.8× size; `OfficeMaze`; crowded, 0.55 / 1.4×) |
| **RoomDressing** (CombatCover, Loot) | `Level/Dressing/` | `_props`: the same placements as a function's, placed after its furniture. `CombatCoverRoomDressing` (low `Props/Barrier`s on the longest sightlines, crate piles in corners, wall crates; counts scale with room size) is the *Dressing* of the Combat, Elite, Boss, Breach, Lockdown, Holdout, Stealth, Rift and Hazard rules; `LootRoomDressing` (crate piles, wall crates) that of Treasure and Ambush. `ConcealmentRoomDressing` (1–2 `Props/ConcealmentPatch` per room, by size, as walk-through decor off doorways, the centre and structure) is the Stealth rule's *Extra Dressing*; `BioRoomDressing` adds one patch to about half of Bio rooms. Rules without one get only their function's furniture |

- A rule with no **Functions** gives plain rooms from the default shapes; empty default shapes = square rooms.
- A shape is only used for rooms whose connection count is in its `_connections` range; if nothing fits, the room is a square.
- `_roomTiles` is 12: at 8, doorway clearance fills most of a room and pillars rarely fit.
- Pillars and inner walls use the wall material and go on `_geometryLayer` with the walls.
- A landmark that doesn't fit its cells, or has fewer doorways than the room's connections, falls back to a generated room (with a warning).
- **Props** (`_props` on a function or a *RoomDressing*): placed in the listed order, so list what a player comes for first, then furniture, then cover and clutter. Each placement sets: `Prefabs`, `Count` (`ScaleByRoomSize` multiplies it by floor area ÷ 64 tiles, 0.4×–2×), `Footprint` (floor tiles it covers, width × depth, turning with the prop; 1×1 can take any angle, bigger ones snap to quarter turns), `On` tags (Edge = along walls, Corner…; a big footprint needs one tile to match), `Avoid` tags, `Zone` (Largest = main space, Others = rooms split off by inner walls), `Facing` (Random, Square, AgainstWall, TowardCentre, AcrossSightline — a barrier's front toward the longest open lane through it, AlongRoom — its length (Z) down the room's longer axis, front toward the middle), `Clearance` (tiles of floor kept free in front; nothing else is put there), `Spacing` (free tiles kept between two of the same placement), `Selection` (Random; Spread = evenly spaced, each one as far as possible from what is already placed; Cover = on the longest open line of sight, only where it is 4+ tiles; Cluster = next to what is placed, with the odd new patch; Line = along what is placed down the room, so machines form rows), `Pile` (props per tile for a 1×1, for crate stacks), `Jitter` (how far a 1×1 wanders from its spot) and `Required` (always placed: its picky rules are loosened until it fits), `Lattice` (only on tiles on a grid of this pitch counted from the room's middle: neat rows; 0 or 1 = anywhere), `Symmetric` (each one is mirrored across the room's long axis when its twin fits; quarter turns only) and `Decor` (ground cover with no collider: takes no floor, may lie on walkways and in doorways, never on a pillar or prop, and doesn't count toward the fill share).
  Every placement keeps all doorways and floor reachable, and props together never take more than about 35% of a room's floor (required ones excepted). A prop's visible size should stay inside the footprint it declares; a pile's members must each fit a quarter of a 3 m tile.
  Placeholder props (Crate, Table, Bed, Planter, SlotMachine, Barrier; TECH: TechBarrier, TechContainer, ProductionLine (1×2 tiles), ServerRack, SentryPost; BIO: Bush, OvergrownTree, RuinSlab, FallenLog, MossyCrate, and the collider-free GrassPatch, MossPatch, Rubble; any faction: `ConcealmentPatch` — a 2 × 2-tile trigger (5.6 × 2.2 × 5.6 m, layer Ignore Raycast) with `ConcealmentZone` (Bush) and a `Foliage` cube in `BushFoliage`, no solid collider, like the AI test arena's zones; the zone keeps itself out of the NavMesh; VOID: OfficeDesk (visitor's side +Z, chair behind), Chair, CabinetBank, Sofa, Safe, StreetLamp, OverturnedDesk, NoirCrate — built from primitives, in the faction wall kits' palettes and four noir materials in `Art/Materials/Environment/Props/`) are in `Prefabs/Environment/Props/` — origin at the base (the middle of its footprint), +Z the front. Swap in real art there.
- **Ceilings**: tick `_buildCeilings` (off by default). Interiors then need lights. Put `_ceilingLayer` on its own layer and keep it out of the `WorldMapArea` capture mask; ceilings are left out of the NavMesh automatically.
- **Enemy prefabs** need `EnemyAI`, plus `EnemyAffixes` (`_pool` = `Enemies/DefaultEnemyAffixPool`) to roll affixes. In `Prefabs/Characters/Enemies/`: tier 1 `MaintenanceBot` (TECH), `Scavenger` (BIO) and `Husk` (VOID), and the placeholder tiers `TechTier2`/`TechTier3`, `BioTier2`/`BioTier3`, `VoidTier2`/`VoidTier3`, in their factions' `_tier1`/`_tier2`/`_tier3`. How many spawn comes from each room type's `EnemyCount`; how often each is tier 2 or 3 from the room's tier, via `LevelBuildSettings` › *Room tiers* (`_enemyTierOdds` per room tier 1/2/3, `_lootLuckByTier` added to the room's loot containers and encounter rewards). Resource nodes take the room tier too (`_higherTierResources`, `_bonusPerTier` on `ResourceNode`).
- **Door prefabs**: origin at the doorway centre on the floor, +Z pointing out of the room. The opening is one tile wide (`_tileSize`).
  - `DefaultLevelBuildSettings` uses the ready-made ones in `Prefabs/Environment/Level/` (placeholder boxes in Gridbox colours; swap in real art there):

    | Slot | Prefab | Is |
    |---|---|---|
    | `_doorPrefab` | — (empty) | normal connections stay open passages |
    | `_lockedDoorPrefab` | `LockedLevelDoor` (yellow) | Keycard locks: `Door` on the `Hinge` child, `_key` = `Items/Keycards/SecurityKeycard` ×1, Consume on |
    | `_terminalDoorPrefab` | `TerminalLevelDoor` (orange) | Terminal locks: `Door` + `ConditionLock` on the `Hinge` child, the Door's `_condition` = that lock, no `_key`. The builder sets how many terminals it needs. Empty = `_lockedDoorPrefab` |
    | `_secretDoorPrefab` | `SecretLevelWall` (white, like the walls) | `Destructible` (60 HP) + `DespawnOnDeath` (0 s) on a wall-sized box: shoot it to open |
    | `_oneWayDoorPrefab` | `LevelDoor` (cyan) | plain `Door` on the `Hinge` child; the builder bars it so it only opens from the corridor side. Empty = `_doorPrefab`; with neither, one-way shortcuts are open passages |
    | `_keyPrefab` | `SecurityKeycardPickup` | `ItemPickup` of one `SecurityKeycard` |
    | `_terminalPrefab` | `LockTerminal` | `LockTerminal` on a console box (origin at the base), `_indicator` = its light. One per terminal room, bound to its door's `ConditionLock` at build time |
  - A one-way door prefab needs a `Door` on its root or a child.
  - Closed doors cut the NavMesh, so enemies don't follow the player through them.
- **Keys**: `_keyPrefab` is placed in the room the map graph picks for each Keycard-locked connection. Every key is reachable without opening a door, and each locked door uses one up, so one key item (`SecurityKeycard`) serves every door. For other locks, make the pickup's item match the door's `Door._key` and tick `_key.Consume`.
- **Terminals**: for each Terminal-locked connection, `_terminalPrefab` is placed in every room the graph lists, and each one is bound to the `ConditionLock` found on that connection's door. A missing terminal prefab, or a door without a `ConditionLock`, is logged as a level warning.
- **Exit**: when the Exit room's content has no `LevelExit`, a plain exit pad is added. Using it ("Extract") calls `GameFlow.Victory`.
- **Treasure rooms**: the Treasure entry's centrepiece is `Prefabs/Environment/Loot/GlobalChest`, the same prefab as the encounters' `_rewardPrefab` (an Ambush poses as a Treasure room).
- **Start room (hub)**: the Start entry's centrepiece is `Prefabs/Environment/Level/HubWorkbench` — a `CraftingStation` (`_recipes` = the four `Data/Crafting` recipes) with its bench 2.5 m from its origin, so the player doesn't spawn inside it. The Start entry keeps `EnemyCount` 0.

- **Encounter rooms**: with `_encounters` set, `LevelBuilder` gives every Lockdown, Holdout, Ambush, Stealth, Rift, Puzzle, Hazard, Spawner, Shrine, Siege, Gauntlet and Boss room an encounter object (`<Type>Encounter_<id>` under the Level) after the NavMesh is baked: a `RoomShutter` just inside each of the room's doorways (sunk into the floor until the room seals), spots set aside for arriving enemies and consoles, and the room type's encounter component. Nothing to place by hand. Gamble and Emergency Exit rooms are plain rooms whose rule brings a centrepiece.

  | `EncounterSettings` field | Default asset uses | Is |
  |---|---|---|
  | `_shutterPrefab` | `RoomShutter` | `RoomShutter` on the root, `_slab` = its `Slab` child (a doorway-sized box with a BoxCollider = `_blocker`), origin on the floor at the doorway, +Z out of the room |
  | `_rewardPrefab` | `Loot/GlobalChest` | `LootContainer` + `LootDropper` (`_table` = `Loot/Containers/GlobalChestLoot`, `_dropOnDeath` off, pickups = `LootItemPickup` / `LootWeaponPickup`). The same chest as the Treasure centrepiece, so an Ambush looks like a Treasure room |
  | `_terminalPrefab` | `LockTerminal` | the Holdout uplink and the Hazard vent controls |
  | `_puzzleSwitchPrefab` | `PuzzleSwitch` | `PuzzleSwitch` (`_indicator` = its light) on a console with a collider |
  | `_puzzleDoorPrefab` | `PuzzleLevelDoor` (blue) | like `TerminalLevelDoor`: `Door` + `ConditionLock` (`_label` Calibration, `_required` 1) on the `Hinge` |
  | `_hazardEffects` | Fire, Poison, Ice, Lightning `…Effect` | one per Hazard room; `_hazardMaterial` (tinted per effect) = `WallKit/mat_TechLight` |
  | `_boss` | — (empty) | a `BossDefinition` (`Create › CGD › Enemies › Boss`) for Boss rooms whose faction has none; empty = the built-in boss (see below) |

  Tuning on the same asset: wave size (read at room intensity), spawn spots, Holdout duration/interval/map reveal, Ambush and Rift waves, Spawner prefab/interval/batch/cap/activation range, Shrine altar/boons/offers, Siege objective/duration/wave interval, Gauntlet waves/growth/luck/twists, Stealth bonus luck and alarm waves, puzzle node count and scramble, hazard tick and strength.
- **Encounter room rules** (`DefaultLevelBuildSettings` › Rooms): Lockdown and Stealth rooms get build-time enemies (`EnemyCount` 3–6 and 2–4) that the encounter watches; Holdout, Ambush, Rift and Puzzle start empty (their enemies arrive as waves); Hazard has a few; Spawner starts empty (the structure sends them) and needs no room rule. `EncounterSettings` › `_spawnerPrefab` must be a prefab with `EnemySpawner` + `HealthManager` (and a collider/hitbox so it can be shot); without it Spawner rooms are plain rooms.
- **Shrine / Siege / Gauntlet** (`EncounterSettings`): Shrine needs `_altarPrefab` (`ShrineAltar` + a collider) and `_shrineBoons` (`StatBoon` assets, `CGD/Stats/Boon`: a label plus stat modifiers; negative values make a curse). Siege needs `_siegePrefab` (`SiegeObjective` + collider on a layer enemies target, i.e. the player's). Gauntlet needs nothing but may list `_gauntletTwists` (`StatBoon`s laid on the player one per wave). None of the three needs a room rule; Siege and Gauntlet rooms start empty and use the cover Dressing. Without its prefab/boons a Shrine or Siege room is just a plain room.
- **Boss**: works with no setup. The Boss room's boss is its faction's `FactionDefinition` › `_boss`, else `EncounterSettings` › `_boss`, else a built-in one: a tier 3 enemy from the room's roster (×8 health, +20 armour, ×1.3 size, phases at 66% and 33%, 2 reward caches). A `BossDefinition` sets `_prefab` (any enemy prefab with `EnemyAI` + `EnemyHealth`; empty = the room's tier 3 roster), `_displayName`, toughness (`_healthMultiplier`, `_armorBonus`, `_shieldBonus`, `_scale`), `_phases` (each: `HealthBelow`, `Announcement`, damage / speed / attack-rate multipliers, `Reinforcements` from the room's roster) and the reward (`_rewardCaches` from `_rewardPrefab`, `_rewardLuck`). The Boss room rule's build-time enemies (`EnemyCount` 1) stay as its guards.
- **Workshop** (`DefaultLevelBuildSettings` › Rooms): a normal room whose `Centerpiece` is a crafting bench; the default uses `Prefabs/Environment/Level/HubWorkbench` (a `CraftingStation` with its recipes) — swap in a dedicated bench prefab to give it other recipes. All new room types are in `StandardMapContentSettings`; other map content assets need a rule added by hand. Centrepieces: Rift → `RiftCore`, Gamble → `GambleStation` (a credit machine and a blood machine: `GambleMachine` + `LootDropper` each, `_payment` = Credits or Blood, `_dropper` = that machine's `LootDropper`, `_currency` = `Economy/CreditsCurrency`), Emergency Exit → `EscapePod` (`LevelExit` with `_emergency` on, hold 3 s). An Emergency Exit's pod doesn't stop the real Exit pad from being placed.
- **Junction rooms** use a `LevelBuildSettings` room rule with `Type` = Junction, no enemies; `JunctionRoomFunction` (a small 6–8 tile Transit room) is in the Functions catalogue. In `MapContentSettings` the Junction rule uses `_minConnections` 3, so it only lands on forks. Direct doors need nothing in the scene: `MapLayoutSettings._directDoorChance` (0 = never) sets how often a plain link between neighbouring cells becomes one, and a connection's *Direct door* toggle in the Map Graph window sets it by hand.
- **Quiet rooms** are normal rooms (no encounter): their rule has `EnemyCount` 0 and **Offerings** = `Props/SalvageNode` (a `ResourceNode` yielding `ScrapMetalResource`), `Props/AmmoCache`, `Level/RewardCache`. Any room rule can list `Offerings`; one of them is always placed in every room of that type, in a nook: against a wall, off the walkways and doorways, with a tile of floor in front of it, away from what is already placed.
- Prefabs for all of the above are in `Prefabs/Environment/Level/`.

### Expedition loop (extraction)

```
Expedition         [ExpeditionRunner] [RunStatsRecorder]   ← scene root
HUD (prefab instance)
  ExpeditionScreen [ExpeditionScreen]          ← stretched RectTransform, layer UI, last child (draws on top)
```

| Component | Assign | Notes |
|---|---|---|
| **ExpeditionRunner** | `_inventory` = Player's PlayerInventory · `_weapons` = PlayerWeaponLoadout · `_equipment` = PlayerEquipment · `_input` = PlayerInputHandler · `_starterKit` = `Expedition/DefaultRunStarterKit` · `_level`? = the LevelBuilder (seeds the loadout) · `_upgrades`? = `Expedition/Upgrades/DefaultShipUpgradeCatalog` · `_stats` = Player's CharacterStats · `_health` = Player's PlayerHealth | Settles the run when GameFlow reaches Victory (extract) or GameOver (death). Needs `GameFlow` (with `ExpeditionSession`) in the scene. Adds every owned ship upgrade's bonus to `_stats` at the start of the run; without `_upgrades` the ship screen offers none. |
| **RunStatsRecorder** | `_player` = Player's PlayerHealth · `_runner` = the ExpeditionRunner · `_saveInEditor` (off: the editor writes no lifetime totals) · `_fileName` = `telemetry.json` | Counts the run (time, kills by tier, damage dealt and taken, items collected) from `CombatEvents` and `QuestEvents`; when the runner ends the run it adds it to the lifetime totals on disk. Goes on the `Expedition` object. |
| **ExpeditionScreen** | `_runner` = the ExpeditionRunner · `_telemetry`? = the RunStatsRecorder (adds the run's stats line) | Builds its own hidden panel; shows after a run with the hold, the next-run kit, the ship upgrades and Deploy (restarts the scene). |
| **PlayerLifecycle** (Player) | `_gameOverOnDeath` = on | Death must end the run, or nothing is ever lost. |
| **PlayerInventory** (Player) | `_startingStacks` = empty | The starting room's loadout replaces the Sandbox's dev ammo and credits. |

`RunStarterKit` (`Data/Expedition/`): `_startingWeapon` (a weapon category, rolled fresh each run — `Weapons/Categories/ModularPistolCategory`), `_startingOffhands` (one-handed melee categories; one is rolled into the starting weapon's offhand — default `Weapons/Melee/` Dagger, Sword, Axe; empty = none), `_gear` + `_gearCount` (random extra gear — armor or weapon categories — no piece twice; default `Items/Armor/CombatVestArmor`, 0–1), `_supplies` (item + count range each).
`ShipUpgradeCatalog` (`Create › CGD › Expedition › Ship Upgrade Catalog`) lists `ShipUpgrade`s (`Create › CGD › Expedition › Ship Upgrade`) in ship-screen order. An upgrade has `_id` (names it in the save — never change it once shipped), `_displayName` and `_levels`, each with a `Summary` line, a `Cost` (items taken from the hold, e.g. Credits + Scrap Metal) and a `Bonus` (`StatModifierPreset`; only stats the player reads take effect: Health, Armor, MovementSpeed, Damage, CooldownReduction, …).
`Scenes/MapTest` is wired this way: `Expedition` root, `ExpeditionScreen` added under the HUD instance, and the two Player overrides above as instance overrides (the Player prefab and Sandbox are unchanged).
- **Obstacle masks**: generated geometry goes on `_geometryLayer`. Keep that layer in enemies' `_obstacleMask` and in weapons' hit masks.

### Director (pacing)

```
Director  [GameDirector, ReinforcementSpawner, SupplySpawner, DirectorMusic, PatrolProbes, DirectorNarration]   ← scene root
```

| Component | Assign | Notes |
|---|---|---|
| **GameDirector** | `_settings` = `Director/DefaultDirectorSettings` · `_player` = Player's PlayerHealth | Tracks the player's intensity and performance and runs the pacing cycle. One per gameplay scene; pauses with GameFlow (any state but Playing). The rest below need it. |
| **ReinforcementSpawner** | `_settings` = `Director/DefaultReinforcementSettings` · `_director` = the GameDirector · `_player` = Player's Transform · `_obstacleMask` = geometry that hides a spawn (Default — the level's `_geometryLayer`) | Spawns only while the director allows it. Needs a baked NavMesh; the arrival sound needs an `AudioPool` in the scene. Optional. |
| **SupplySpawner** | `_settings` = `Director/DefaultSupplySettings` · `_director` = the GameDirector · `_health` = Player's PlayerHealth · `_inventory` = PlayerInventory · `_weapons`? = WeaponController (without it only healing is handed out) · `_obstacleMask` = Default | Needs a baked NavMesh. Optional. |
| **DirectorMusic** | `_settings` = `Director/DefaultMusicSettings` · `_director` = the GameDirector | Creates its own 2D AudioSources as children. Layers without a clip are skipped, so it is silent until clips are assigned. Optional. |
| **DirectorNarration** | `_settings` = `Director/DefaultNarrationSettings` · `_director` = the GameDirector · `_player` = Player's Transform · `_enemyMask` = layers enemy colliders are on (Everything) | On each phase change plays that phase's beats: a HUD feed line (needs the HUD's notification feed) and a bark from the nearest non-calm enemy within `_barkRadius`. Optional. |
| **PatrolProbes** | `_settings` = `Director/DefaultPatrolProbeSettings` · `_director` = the GameDirector · `_player` = Player's Transform · `_enemyMask` = layers enemy colliders are on (Everything) | During a build-up, sends a calm enemy 15–45 m away to check a spot 5–10 m from the player, every 12–20 s (at neutral pressure), at most 2 per build-up. Needs a NavMesh. Optional. |

**Enemies and pacing** need no extra wiring: the GameDirector publishes its phase to every enemy (`EnemyPacing`), using `DefaultDirectorSettings` › *Enemies* — one row per phase: *Bold Moves* (level-wide Director Permits at neutral pressure; build-up 2, peak 4, fade 1, calm 1), *Attackers* (× EnemyData's MaxMeleeAttackers / MaxShooters; 1, 1.5, 0.75, 0.5), *Search Time* (× search duration; 1, 1.25, 0.6, 0.4), *Fall Back* (× the PerceptionProfile's retreat threshold; 1, 0.75, 1.25, 1.5). Pressure scales Bold Moves. Scenes without a GameDirector run enemies unscaled and with unlimited permits.

`Scenes/MapTest` is wired this way (Sandbox has no director). Any gameplay scene can add the same root object.

**Music clips:** assign the stems of one piece (same length, they loop together) to `DefaultMusicSettings` → *Layers*: 0 calm bed (always on), 1 tension (fades in from intensity 0.1–0.4), 2 combat (0.35–0.7, silent in the calm), 3 peak (only while the peak is held); plus *Peak Stinger* and *Calm Stinger*. Import music clips with *Load Type* = Streaming. The reinforcement arrival sound goes in `DefaultReinforcementSettings` → *Arrival Sound*.

## Impact Effects

```
ImpactEffects      [ImpactSpawner]   ← one per scene (e.g. under GameManager)
```

| Component | Assign | Notes |
|---|---|---|
| **ImpactSpawner** | `_database` = `Impacts/DefaultImpactDatabase`, `_maxDecals` | Without it, hits leave nothing behind. |

`ImpactDatabase` picks effects per surface and per hit kind (Bullet / Melee):
- Characters (Hitbox or HealthManager) use **Flesh**.
- Other colliders match on their **physics material** (the same ones `SurfaceDatabase` uses).
- Anything else uses **Default**.

Each effect is optional:
- a decal material (transparent; e.g. a bullet-hole texture)
- a VFX prefab (needs `PooledLifetime`)
- a `SoundBank`

The default asset has no decal textures or VFX yet.

---

## Assets

All under `Assets/Project/Data/`. Shared settings are **single assets** — never duplicate them.

| Folder | Assets | Used by |
|---|---|---|
| `Input/` | `InputBindingSettings` (shared) | PlayerInputHandler, SettingsMenu |
| `Player/` | `PlayerMovementSettings` | PlayerMovement (PlayerMantle reads it from there) |
| `Player/` | `PlayerCameraSettings` | PlayerCamera |
| `Player/Dodges/` | `SidestepRoll`, `CommittedRoll`, `SteerableBoost`, `LongDash`, `AirDash`, `AbilityDash` (`…Dodge`; the last is the Dash ability's `Move`) — stages with speed, curve, steering, i-frames, commitment, follow-up press, cooldown | PlayerDodge, DevCatalog |
| `Flow/` | `GameFlowSettings` | GameFlow |
| `Difficulty/` | `DefaultDifficultyTable` — enemy health and damage-taken multipliers for Easy, Normal and Hard (`Create › CGD › Difficulty › Difficulty Table`) | GameFlowSettings (`_difficulty`) |
| `Timing/` | `GameTimeSettings` | GameTime |
| `Enemies/` | `MaintenanceBot`/`Scavenger`/`Husk` `…EnemyData` (tier 1, ≈100 effective health each; the firing-range dummies run on them), `Tech`/`Bio`/`Void` `Tier2`/`Tier3` `…EnemyData` (placeholders, ≈2.5× / ≈5×; *Tier* set on each), `DefaultEnemyData` | EnemyAI, EnemyHealth |
| `Enemies/Perception/` | `DefaultPerceptionProfile`, `WaryPerceptionProfile`, `SkittishPerceptionProfile` | EnemyData › Senses |
| `Enemies/Doctrine/` | `TechDoctrine`, `BioDoctrine`, `VoidDoctrine` | EnemyData › Doctrine |
| `Enemies/BehaviorTrees/`, `Enemies/AITest/` | `MeleeEnemyBehaviorTree`, `RangedEnemyBehaviorTree`, `BioBehaviorTree`, `TechBehaviorTree`, `VoidBehaviorTree`, `Shared/` (`RegroupSubtree`, `RaiseAlarmSubtree`, `RoutineSubtree`); `BioTestEnemyData`, `TechTestEnemyData`, `VoidTestEnemyData` (use the faction trees; AITest scene) | EnemyData › BehaviorTree |
| `Enemies/`, `Enemies/Affixes/` | `DefaultEnemyAffixPool` (affixes per tier: 0 / 1 / 2), `Armored`/`Warded`/`Vital`/`Brutal`/`Swift` `…Affix` | EnemyAffixes |
| `Combat/HitboxProfiles/` | `DefaultHitboxProfile`, `TargetDummyHitboxProfile` | EnemyHealth, PlayerHealth |
| `Combat/StatusEffects/` | Bleed, Fire, Ice, Lightning, Poison | on-hit effect lists |
| `Combat/ActionTimelines/` | `GroundSlamTimeline`, `ArmingSwordSlashLeft/SlashRight/Thrust/HeavySlashTimeline` and `<Sword>Light1…4Timeline` / `<Sword>HeavyTimeline` for every other sword, and `<Axe|Dagger|Hammer|Spear>Light1…3Timeline` / `…HeavyTimeline` (bashes: `GuardBashTimeline` (every melee category's *Bash* and the fists), `PistolWhipBashTimeline`, `RifleBashTimeline`, `TacticalKnifeBashTimeline`, `ShieldBashTimeline` (all three shields); each assigned as `Timeline` on its category's light combo steps and heavy attack; a step's own Damage stays its combo multiplier, the timeline's Swing Hit Damage is 1). A melee slash/thrust timeline adds a *Swing Hit* event (*Keys* = frame + Yaw/Pitch/Push poses, Near/Far Distance, Radius, *Anchor* Aim or Socket, Damage); turn on the weapon's debug draw to see its capsule trail. Scene wiring only for *Anchor* = Socket (see MeleeController) | TimelineAbility, melee attack steps |
| `Weapons/Ranged/` | `DefaultWeaponData`, `T1/` examples per category | Loadout, WeaponPickup |
| `Artifacts/` | `HandMirrorArtifact`, `WhisperingTomeArtifact`, `BoneIdolArtifact`, `OilLanternArtifact` (`ArtifactDefinition`: `_statRanges` (passive stats; a curse is a negative range), `_behavior` (empty = passive only), `_potency` range, `_mainHandPenalty`, `_perkPool`, `_guaranteedPerks`, `_reality` = Void; roll profile = `Items/StatRollProfile`); behaviors: `MirrorWardBehavior` (Ward: `_profile` = `Combat/Reflect/MirrorWardReflect`, `_drainPerSecond` = stamina), `TomeCastBehavior` (Cast: `_ability` = `Abilities/EldritchRendAbility`), `LanternChannelBehavior` (Channel: `_modifiers`, `_drainPerSecond`) — each behavior has a `_usage` line shown in the UI | `DefaultLootTable`, DevCatalog items (`give hand mirror`) |
| `Perks/Artifacts/` | `DefaultArtifactPerkPool` — armor perks that also fit artifacts (*Fits* = Armor + Artifact: Battle Focus, Opportunist, Vampiric, Counterguard, Focused, Bloodthirst, Regenerator, Reactive Plating) | `_perkPool` on the artifact definitions |
| `Weapons/Shields/` | `BucklerShield`, `RiotShield`, `TowerShield` (`ShieldDefinition`: rolled block share and stamina per blocked damage, guard arc/parry window/stun, *Blocks Passively* for the tower shield, bash step + rolled bash damage, `StaminaMeter` = `Meters/StaminaMeter`, *Main Hand Penalty*; roll profile = `Items/StatRollProfile`) | `DefaultLootTable`, DevCatalog items (`give buckler`) |
| `Weapons/Categories/` | one per weapon type (*One Handed* on Pistol, Modular Pistol and SMG; *Min RPM* = 360 on Pistol and Modular Pistol, a floor no model or style can roll below — 0 elsewhere), each with *Quick Melee* = `PistolBashMeleeWeaponData` (pistols) or `RifleBashMeleeWeaponData` (the rest); `ModularPistolCategory` (the starter: Pistol type with *Firing Styles* — Semi / Burst / Machine / Scatter (Shotgun fire behavior, pellets, ADS cone) — and *Stat Styles* — Heavy / Marksman / Rapid / Carbine, each weighted with stat scales; *Guaranteed Perks* = `Perks/Weapons/TacticalKnifePerk`) (incl. `BowCategory`: Volley fire behavior, *Bows* section (`HasDrawStances`, vertical/horizontal draw multipliers, volley spacing), Pellet Count = arrows per shot; `CrossbowCategory`: Projectile fire behavior, `AutomaticMagazineSize` for autoloaders; both use Arrows ammo, with arrow speed, drop and draw time in *Charge & Projectile*) | RandomWeaponPickup, loot |
| `Perks/Weapons/`, `Perks/Armor/` | `DefaultWeaponPerkPool`, `DefaultArmorPerkPool` (weighted perks, each with a *Min Tier* (Common unless set), perks per tier 0/1/1/2/2, 25% extra-perk chance; `MagnetizedPerk` sits in the weapon pool at *Min Tier* Legendary), perk assets (`…Perk`: *Passive* = stat modifiers; triggered: Load Rounds, Timed Buff, Heal, Restore Meter, each with a trigger (Kill, Hit, Critical Hit, Parry, Dodge, Ability Cast, Damage Taken, Healed, Aim Start, Reload Start, Reloaded, Item Used, Weapon Swap), chance and cooldown; *Fits* = Firearm / Melee / Armor; *Magnetized* perks (`MagnetizedPerk`: `_damageKeptPerTarget` = 0.75, *Fits* = Firearm); *Quick Melee* perks (`TacticalKnifePerk`: `_quickMelee` = `Weapons/Melee/TacticalKnifeMeleeWeaponData`, `_weaponTypes` = Pistol, SMG, plus Spread/Recoil modifiers)) — any gear definition can also list `_guaranteedPerks` | `_perkPool` on every weapon category (firearm and melee) and on `Items/Armor/CombatVestArmor` |
| `Weapons/Profiles/` | `FirearmStatRollProfile`, `PrecisionRifleStatRollProfile`, `AutomaticSupportStatRollProfile`, `ShotgunStatRollProfile` (quality curve + tradeoff axes; attachment slots per tier 1/1/2/3/4) | ranged weapon categories (`_rollProfile`) |
| `Weapons/FireBehaviors/` | Hitscan, Projectile, Shotgun, Volley (bows: arrows fanned along the draw orientation; `_prefab` = `Prefabs/Weapons/Projectile`) | weapon data / categories |
| `Weapons/Melee/`, `Weapons/Throwables/` | `DefaultMeleeWeaponData` (fists), `PistolBashMeleeWeaponData`, `RifleBashMeleeWeaponData`, `TacticalKnifeMeleeWeaponData` (quick melee: only *Bash* and the guard's parry settings are used), `DefaultGrenadeData`, `SmokeGrenadeData` | MeleeController, weapon *Quick Melee*, TacticalKnifePerk, throwable items |
| `Weapons/Melee/` | `Dagger`/`Axe`/`Hammer`/`Spear` `…MeleeCategory`; `Swords/` — one `…MeleeCategory` per sword (Arming Sword, Broadsword, Rapier, Sabre, Scimitar, Shamshir, Cutlass, Kukri, Khopesh, Cleaver, and the two-handed Katana, Claymore, Zweihänder; each its own moveset, ranges, `_oneHanded` and *On Hit Effects* = `Combat/StatusEffects/BleedEffect` on the curved and chopping blades; *Parry Reflect* = `RiposteSlashReflect`) (`MeleeCategoryData`: moveset template, rolled damage/speed/reach/penetration, `StaminaMeter` = `Meters/StaminaMeter`, guard, *Bash* (guard bash, relative damage), *One Handed* (Dagger, Sword, Axe) and *Offhand Penalty* (spread/recoil on the main gun while held in the offhand)) | `DefaultLootTable`, `GunsmithWeaponStock` (Sword, Axe, Dagger), DevCatalog weapons (`weapon sword`) |
| `Items/Munitions/` | one per caliber (incl. `ArrowMunition` for bows and crossbows) | AmmoPickup, PlayerInventory |
| `Items/` | `StatRollProfile`, `Keycards/SecurityKeycard` (opens the generated levels' locked doors) | weapon categories, `CombatVestArmor` (see note) |
| `Items/Armor/`, `Attachments/`, `Consumables/`, `Throwables/`, `Resources/` | `CombatVestArmor`, `ExtendedMagazineAttachment`, `BandageConsumable`, `StimConsumable`, `RationConsumable` (instant), `FragGrenadeThrowable`, `SmokeGrenadeThrowable`, `ScrapMetalResource`, `ClothResource` | pickups, loot, recipes, quest rewards |
| `Crafting/` | `BandageRecipe`, `CombatStimRecipe`, `ExtendedMagazineRecipe`, `CombatVestRecipe` | CraftingStation |
| `Level/Objectives/` | `DefaultMapObjectiveSettings` (templates, 1–2 main / 0–3 side per map, console = `Environment/Level/LockTerminal`, retrieve pickup = `LootItemPickup` with `Items/Resources/DataCoreResource`, reward cache = `RewardCache`, credits and cache luck per tier), objective templates (`…Objective`: steps of Clear / Activate / Retrieve, each with a room tier range `MinTier`–`MaxTier`; `_tierOrder` Any / Never Down / Same; in order or not; same faction or not; main / side / either) | LevelBuilder |
| `Expedition/` | `DefaultRunStarterKit` — the starting room's loadout (Modular Pistol + a random Dagger/Sword/Axe offhand, 0–1 random gear, supplies); `Upgrades/DefaultShipUpgradeCatalog` with five `…ShipUpgrade`s (Hull Plating, Reactive Armor, Servo Tuning, Targeting Suite, Ability Coolant) and their per-level bonuses in `Upgrades/Bonuses/` (`…ModifierPreset`) | ExpeditionRunner |
| `Director/` | `DefaultDirectorSettings` (stress per damage taken / nearby kill, combat memory, intensity decay, peak 0.75 held 4 s, calm below 0.25 for 30–45 s, 15 s opening calm; *Performance*: pressure 0.7–1.35, easy peak ≥ 60% health, hard ≤ 30%, step 0.2, death 0.5), `DefaultReinforcementSettings` (Husk, Scavenger, MaintenanceBot; 2–4 every 20–35 s at neutral pressure, max 6 alive, 18–30 m away; arrival message, no sound yet), `DefaultSupplySettings` (`LootItemPickup`; 2 `BandageConsumable` below 50% health with < 2 carried; 3 magazines of the matching munition below 2 spare; 4 s into a calm, 4–9 m away), `DefaultMusicSettings` (4 layers and 2 stingers, no clips yet), `DefaultPatrolProbeSettings` (every 12–20 s in a build-up, 2 per build-up, enemies 15–45 m away, spot 5–10 m from the player), `DefaultNarrationSettings` (peak: "They're pushing!" + Pushing bark; fade: 50% HoldingBack bark; calm: "It's gone quiet..." + HoldingBack bark; bark radius 25 m) | GameDirector, ReinforcementSpawner, SupplySpawner, DirectorMusic, PatrolProbes, DirectorNarration |
| `DevTools/` | `DevCatalog` (`_itemCatalog` = `Items/DefaultItemCatalog` for items and weapon categories; enemy prefabs, buffs, dodges) | DevCommands |
| `Abilities/` | Dash (needs `PlayerDodge`), Heal, Projectile, Shockwave, DamageBoost, ConeBlast (Targeted), GroundSlam (Timeline — needs `TimelineAbilityRunner`), Stealth (needs `Stealthable`) | PlayerAbilities |
| `Abilities/` (instant strike) | `CrushingBlowAbility` (Targeted, `DefaultRaycastTargetSelector`) | PlayerAbilities |
| `Abilities/` (Surge) | `ArcLashAbility` (Targeted, no cooldown, 15% Surge), `OverdriveAbility` (Stat Buff, 25 s + a full, non-draining gauge), `RuptureAbility` (Targeted, *Scaling*: spends 20–100% for up to 4× damage, bleed at 50%+, 30% floor), `StaticRoundsAbility` (Targeted, *Charge Source* = Surge, +25% per action, 3 charges) — each ability's *Surge* section sets gain per action, drain rate, delay and floor; need `CombatActions` on the player | PlayerAbilities |
| `Abilities/` (reflects) | `VengeanceAbility`, `DeflectAbility`, `AbsorbAbility` (Reflect — need a `Reflector` on the player) | PlayerAbilities |
| `Combat/Reflect/` | `VengeanceReflect`, `DeflectReflect` (projectile = `Prefabs/Weapons/Projectile`), `AbsorbReflect`, `RiposteSlashReflect` (timeline = `ActionTimelines/RiposteSlashTimeline`, whose Damage 1 = the parried hit) | reflect abilities, melee *Parry Reflect* |
| `Targeting/` | `Default…TargetSelector`, `AimedArea…`, `FriendlyArea…` | abilities, PlayerLockOn |
| `Meters/` | Stamina, Mana, Oxygen, Rage | MeterSet, costs, MeterZone |
| `Loot/` | `DefaultLootTable` (*Tier Checks*: Legendary 1% → Epic 5% → Rare 15% → Uncommon 35% → Common 100%, no tier pools yet) | LootDropper |
| `Loot/Pools/` | `SwordsPool` (the thirteen swords; two-handers rarer — also the sword entry of `DefaultLootTable`), `WeaponsPool` (every firearm category but the Modular Pistol, plus the dagger, axe, hammer and spear categories and `SwordsPool`), `EquipmentPool` (Combat Vest, the three shields, the four artifacts, Extended Magazine), `ConsumablesPool` (bandage, ration, stim, frag and smoke grenades), `ResourcesPool` (scrap metal, cloth, data core), `MunitionsPool` (all five ammo types) — shared pools with no *Tier Checks* of their own | the container tables below |
| `Loot/Containers/` | `WeaponCrateLoot` (a weapon + 50% a munitions draw), `EquipmentLockerLoot` (an equipment piece + 25% a second), `ConsumableCaseLoot`, `ResourceCrateLoot`, `MunitionsCrateLoot` (2–3 draws from their pool), `GlobalChestLoot` (2–3 draws across all five pools); the weapon, equipment and global tables carry the default *Tier Checks* | the container prefabs in `Prefabs/Environment/Loot/` |
| `Loot/Filters/` | container subtypes (`LootFilter`): weapon — `PistolsFilter`, `SmgsAndArsFilter`, `ShotgunsFilter`, `LongGunsFilter`, `BowsAndCrossbowsFilter` (each its firearm types plus their ammo), `MeleeFilter`; supply — `HealingFilter`, `CombatSuppliesFilter` (buffs + throwables); equipment — `ArmorFilter`, `ShieldsFilter`, `ArtifactsFilter`; munitions — `LightRoundsFilter`, `StandardRoundsFilter`, `HeavyRoundsFilter`, `ShotgunShellsFilter`, `ArrowsFilter` | `_subtypes` on the container prefabs' LootDropper |
| `Stats/` | `HardDifficultyModifierPreset`, `DamageBoostModifierPreset` | CharacterStats, Stat Buff ability |
| `Quests/` | `TargetPracticeQuest` → `ResupplyQuest`, `ShootingRangeClearedQuestSignal` | QuestTracker |
| `Feedback/` | ten `…FeedbackPreset`s | CombatFeedback, QuestFeedback, Stealthable |
| `Stealth/` | `StealthSettings` (shared) | Stealthable |
| `CameraEffects/` | `DefaultCameraEffectSettings` | CameraEffectsController |
| `Map/` | map styles `Linear`/`Branching`/`Hub`/`Labyrinth`/`Balanced`/`Organic`/`Random` `…MapGenerationSettings`, `SandboxMapGraph` (uses Branching); `Layouts/` (`Linear`/`Branching`/`Hub`/`Labyrinth`/`Balanced`/`Organic` `…MapLayoutSettings`), `Content/` (`Standard`/`TreasureHunt`/`Gauntlet` `…MapContentSettings`), `Sections/` (`Habitation`/`Commerce`/`Engineering` `…MapSection`), `Modifiers/` (warnings `Lockdown`/`Infestation`/`Overrun`, anomalies `Scavenger`/`FracturedHull` `…MapRunModifier`), `FactionMixes/` (`Infested`/`Balanced`/`Contested`/`Warped` `…MapFactionMix`) | Map Graph window, LevelBuilder |
| `Audio/` | `DefaultSurfaceDatabase` | PlayerFootsteps |
| `Factions/` | `TechFaction`, `BioFaction`, `VoidFaction` — name, colour (the GDD's signal colours: TECH amber `#FFB23F`, BIO fern `#8CAA4E`, VOID violet `#6E62DA`; used by map chips, the Map Graph's Faction view, blueprint and world map fills, and the floor tint), floor tint, tiered rosters `_tier1` (`MaintenanceBot` / `Scavenger` / `Husk`), `_tier2`, `_tier3` (an empty tier falls back to the one below) | MapContentSettings, LevelBuilder |
| `Level/` | `DefaultEncounterSettings` (encounter rooms), `DefaultLevelBuildSettings` (Gridbox materials, room rules), `Functions/` (48 `…RoomFunction`s, one catalogue picked by category), `Shapes/` (seven `…RoomShape`s), `Structure/` (six `…StructureRule`s), `Landmarks/` (`ReactorCoreLandmarkRoom`), `WallKits/` (`Tech`/`Bio`/`Void` `…WallKit`) | LevelBuilder |
| `Impacts/` | `DefaultImpactDatabase` (empty effects) | ImpactSpawner |
| `Economy/` | `CreditsCurrency`, `DefaultPriceTable`, `Shops/GunsmithShopCatalog`, `Stock/GunsmithAmmoStock`, `Stock/GunsmithWeaponStock` | Vendor, PlayerInventory, DevCatalog |
| `Dialogue/` | `GunsmithDialogue`, `Actions/OpenShopDialogueAction` | Npc |

**Sounds** — every `SoundBank` slot is optional; systems stay silent without one. There are no audio clips in the project yet, so no `SoundBank` assets exist.

**Weapon quality profiles.** Every ranged weapon category has a `_rollProfile` from `Weapons/Profiles/`
(each is a `StatRollProfile` holding one of the **Axes/…** presets): `FirearmStatRollProfile` (Assault Rifle,
Submachine Gun, Pistol, Modular Pistol), `PrecisionRifleStatRollProfile` (Sniper Rifle, Bow, Crossbow),
`AutomaticSupportStatRollProfile` (Light Machine Gun), `ShotgunStatRollProfile` (Shotgun). A category with no
profile rolls at quality 1 / Common with uniformly random stats. If a preset in `StatRollProfile` changes,
re-apply it to the matching asset (right-click → Axes/…); `StatRollProfileAxesTests` fails when they drift.