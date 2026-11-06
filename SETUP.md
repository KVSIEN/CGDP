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
2. **Level** — geometry with `CullableObject`, a baked **NavMesh Surface** — or a
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
GameManager        [VisibilityCullingManager, AudioPool, AudioMixerController?, PoolPrewarmer?]
GameFlow           [GameFlow, GameTime?]        ← own root object, nothing else on it
RespawnPoint       (empty Transform)
WorldMap           [WorldMapArea]?              ← see Map & Minimap
EventSystem        [EventSystem, InputSystemUIInputModule]
Directional Light
Global Volume
```

| Component | Assign | Notes |
|---|---|---|
| **VisibilityCullingManager** | `_camera` = Main Camera | Other fields: defaults are fine. |
| **AudioPool** | — | Raise `_initialSize` (16) if sounds cut out. |
| **AudioMixerController**? | `_settings` = `Audio/DefaultAudioMixerSettings` | Drives the AudioMixer and its snapshots — see [Audio Mixer](#audio-mixer). Without it sounds scale their own volume and mix states do nothing. |
| **PoolPrewarmer**? | `_entries` = prefab + count | Pre-spawns pooled objects. Pools grow anyway. |
| **GameFlow** | `_settings`? = `Flow/GameFlowSettings` | See [Game Flow](#game-flow--time). |
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
| **TrajectoryPreview** (Player) | `_material` = `Materials/ThrowPreview` (unlit) | Draws the aim arc and blast ring; builds its line renderers itself. |
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
| **ObjectiveHUD** | — | Top-centre panel for the Siege/Gauntlet in progress (title, detail, countdown, objective health bar). Driven by `ObjectiveFeed`; already in the HUD prefab. |
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
- **Expeditions:** the `GameFlow` prefab also carries **ExpeditionSession** (no fields), which keeps the ship's hold and the packed kit across scene loads. A scene without it still plays; its runs just don't carry over.

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

**Enemy type** is all in its `EnemyData`: team, health, speeds, sight range and angle, hearing range (how far a walking hostile's footsteps carry), melee or ranged, display name (used in kill messages), its **Senses** (a `PerceptionProfile`) and optionally its behavior tree.

**Group & Reactions** (on `EnemyData`): `MaxMeleeAttackers` (2 — melee enemies of this type that may fight one target at once), `MaxShooters` (3), `StaggerThreshold` / `StaggerDuration` / `StaggerCooldown` (a hit of 25% of max health staggers for 0.4 s, at most every 2.5 s), `RaisesAlarm` (runs for an `AlarmPanel` within 25 m of its post on first spotting a hostile). Caution, guarding, lost-interest / victory pauses and morale are tuned on the `PerceptionProfile`.

**Alarm panels** — any object with a collider and an **AlarmPanel** (`_radius` 60 m, `_useTime`, `_rearmTime`, `_disableTime` for the player's hold-Interact, `_team`, `_screen`? = a renderer it tints green / red / dark). Enemies only use panels within 25 m of their post. PlayerInteraction finds it like any interactable.

**Cover** — add a **CoverMap** (one per scene, any object; it's on its own `CoverMap` object in AITest and MapTest): `_spacing` (1.25 m between spots), `_lowHeight` / `_highHeight` (0.8 / 1.6 m: how tall an obstacle must be for low / full cover), `_obstacleMask` = what counts as an obstacle (Default). It scans the NavMesh the first time an enemy asks, so baked and runtime NavMeshes both work; call `Rebuild()` if the NavMesh changes later. Without a CoverMap enemies simply never use cover. Enemies learn cover around their spawn point and `_waypoints`, so give patrols routes through the area they should defend.

**Squads** — put an **EnemySquad** on a parent object; every `EnemyAI` under it joins in `Awake` (spawners call `Join(enemy)` for ones spawned later, `Leave` to remove). `_radioRange` (40 m: callouts reach members through walls; 0 = shouting only), `_suppressors` (1 ranged member pins the target down; other ranged members flank), `_assaulters` (2 melee members go straight in; other melee members flank), `_reassignEvery` (2 s), `_leaderLossMorale` (0.25). The first member is the leader. Enemies outside a squad behave as before.

**Grenades & abilities** — no extra components: tree nodes **Throw Grenade** (assign a `GrenadeData`, e.g. `Weapons/Throwables/DefaultGrenadeData`; its prefab must be set) and **Use Ability** (assign an `Ability` asset) run on any `EnemyAI`. Abilities aim from an `AbilityAim` child the enemy creates itself at 1.5 m; player-only abilities (that need weapons, stamina, etc.) just fail. The enemy's Animator gets a `Throw` trigger when it throws (add it to the controller to animate it).

**Senses** — `Enemies/Perception/`: `DefaultPerceptionProfile` (Void and the default enemy), `WaryPerceptionProfile` (Tech: slow to notice, long memory, thorough searches), `SkittishPerceptionProfile` (Bio: quick to notice and to give up). Create more with `Create › CGD › Enemies › Perception Profile`; an empty slot uses built-in defaults. A profile tunes how fast sight and sound turn into awareness, memory and guessing, callout range, and the notice / search / look-around timings. Footstep loudness and how much crouching or standing still hides you are the player's side, on `StealthSettings` (*Noticeability*). Enemies only hear footsteps of characters with a `Stealthable`; others count as moderately loud.

### Behavior trees (Enemy)

No scene wiring: every `EnemyData` › **Behavior** › `BehaviorTree` names the `BehaviorTreeAsset` its enemies think with — there is no other brain, so an empty slot leaves the enemy standing still (`EnemyBrainAssetTests` fails for shipped data without one). All the regular types and tiers (and `DefaultEnemyData`) use `MeleeEnemyBehaviorTree`. `_waypoints` on `EnemyAI` feed the tree's **Patrol** node.

**Cost:** an enemy whose senses have nothing (unaware, no lead, not under fire, not attacking) ticks its tree every 0.1 s with a hostile in sensing range and every 0.25 s without (`TreeTickRate`), staggered between enemies; anything noticed brings it back to every frame. In the Profiler, each tree shows as `BehaviorTree.<asset name>` and the senses as `EnemyAI.Senses`.

| Asset | Notes |
|---|---|
| `Enemies/BehaviorTrees/MeleeEnemyBehaviorTree` | Regroup → Raise Alarm → fight while it knows where you are (under fire: dodge half the time, at most every 3 s; strike in range when it's its turn; squad Flank role and further than 6 m: flank; further than 6 m: flank half the time at most every 8 s; else close in) → Routine. |
| `Enemies/BehaviorTrees/RangedEnemyBehaviorTree` | Below 30% health: hide behind cover or flee (8 s cooldown) → Regroup → Raise Alarm → squad Suppress role: dodge, else take cover and Suppress (or Suppress where it stands) → fight: dodge, else a Utility Selector weighs *from cover* (fixed 0.6: run to fresh cover, shoot 6–9 s) against *in the open* (TargetDistance 6–25 m: hold range and shoot for 3 s) → just lost sight (Knows Target 3 s, not Has Target): Throw Grenade `DefaultGrenadeData`, at most every 12 s, 3 per life → Routine. The `EnemyData` should be `CombatType` Ranged for its preferred range, burst and spread values. |
| `Enemies/BehaviorTrees/Shared/RegroupSubtree` | Knows Target → Is Shaken → Regroup; aborts lower priority. |
| `Enemies/BehaviorTrees/Shared/RaiseAlarmSubtree` | Has Target → Can Raise Alarm → Raise Alarm; aborts lower priority. Does nothing for enemies that don't raise alarms. |
| `Enemies/BehaviorTrees/Shared/RoutineSubtree` | Investigate leads (aborts lower priority) → Return To Post → Patrol. |
| `Enemies/AITest/MeleeTestEnemyData`, `RangedTestEnemyData` | Scavenger and Maintenance Bot copies that use the trees above (the ranged one fires 3-round bursts and raises alarms). |

**Editing:** *CGD › AI › Behavior Tree Editor*, or double-click a tree asset (`Create › CGD › AI › Behavior Tree` makes a new one with its Root). Space or right-click on the canvas to add nodes, group the selected nodes or add a sticky note; drag from a node's bottom port to another's top port to connect. Double-click a **Subtree** node to open its tree. Key fields have a ▾ picker. Badges on nodes and the toolbar's issue button show what the validator found.

**Debugging:** in Play mode select an enemy to watch its tree (and the subtrees it runs). The side panel then shows its live blackboard, its decision trace, and for the selected node what it and its children last reported. *Break when it starts / fails* on a node pauses the game for the watched enemy (breakpoints last for the editor session). Recording only runs for the watched enemy.

**New key-reading nodes:** mark the string field `[BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Float)]` (or `Write`) to get the picker and the "nothing writes this" check. Keys an agent writes itself are `const string` fields marked `[AgentBlackboardKey]` (see `EnemyBlackboard`).

**Enemy blackboard** — refreshed every tick for generic nodes (Compare Number, Score): `Health`, `Morale`, `Awareness` (0–1), `TargetDistance` (metres to the believed target position; unset without a target), `Cautious` (flag).

**Tree rules of thumb:**
- The top **Selector** lists branches most urgent first. Give each branch that should interrupt the ones to its right **Abort: Lower Priority**, and start it with its conditions (`Has Target`, `Is Shaken`…) — only those leading conditions are re-checked.
- Give a branch **Abort: Self** (or **Both**) when it should stop the moment its own conditions break: `[Target In Range → Takes Melee Turn → Melee Attack]`.
- Gates (**Cooldown**, **Limit**) count as conditions: a branch behind one doesn't interrupt until it's ready. **Chance** isn't a gate — put it under the Cooldown, not before it.
- **Patrol**, **Hold Range** and **Repeat** (forever) never finish on their own — end them with a **Parallel** (Require One) next to a **Wait**, or keep them where something above can interrupt.
- Share branches between enemy types as **Subtree** assets; a subtree's abort mode works from where it's placed.
- **Take Cover** and **Flank** pick a new spot each run: put them under a Cooldown or a timed Parallel so they don't reposition every frame. They need a CoverMap in the scene (Flank only needs the NavMesh).
- Use **Knows Target** instead of **Has Target** above moves that break line of sight (flanking), so losing sight for a moment doesn't end the fight.
- Trees with `Enemy/…` nodes must run on an `EnemyAI`.

---

## AI Test Scene

`Scenes/AITest` — the MapTest scene root, Player and HUD without the map, director and expedition systems, plus:

```
Arena              [NavMeshSurface, RuntimeNavMesh]      ← NavMesh built from its children at load
  Floor, Wall North/South/East/West, Cover Center/Left/Right, Pillar NW/NE, Crate Low A–D   [MeshFilter, MeshRenderer, BoxCollider]
CoverMap           [CoverMap]                            ← cover spots found from the NavMesh
Alarm Panel        [MeshFilter, MeshRenderer, BoxCollider, AlarmPanel]   ← on the north wall; `_screen` = its renderer
Enemies            [EnemySquad]                          ← all four enemies are one squad
  Waypoints        Melee A 1–4, Melee B 1–4, Ranged 1–2
  Melee Enemy A, Melee Enemy B   (Scavenger prefab, _data = MeleeTestEnemyData, patrol Melee A / Melee B)
  Ranged Enemy                   (Maintenance Bot prefab, _data = RangedTestEnemyData, patrol Ranged; RaisesAlarm on)
  Guard Enemy                    (Husk prefab, its own HuskEnemyData — melee tree, no route: guards facing west)
```

| Component | Assign | Notes |
|---|---|---|
| **NavMeshSurface** (Arena) | Collect Objects = *Children*, Use Geometry = *Physics Colliders* | Not baked in the project. |
| **RuntimeNavMesh** (Arena) | `_agents` = Enemies | Builds the surface on load, then snaps the agents under `_agents` onto it. Any scene can use it the same way. |

The Player overrides: `PlayerWeaponLoadout._startingWeapons[0]` = `Weapons/Ranged/T1/AK47_T1`, spawn and RespawnPoint at (0, 1, −16), respawns on death. Add an enemy by dropping an enemy prefab under **Enemies** and setting both `EnemyAI._data` and `EnemyHealth._data` to a data asset with a tree.

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

- **`Prefabs/Characters/TargetDummy`** — a stationary stand-in for an enemy: `EnemyHealth` (`_data`, `_hitboxProfile` = `TargetDummyHitboxProfile`, `_healthBar`), `StatusEffectController`, `EnemyHealthBar` (`_alwaysVisible` and `_showNumbers` on: the bar stays up with a shield strip and health/shield numbers), `RespawnOnDeath`, `TargetMover` (`_halfWidth` sway, `_halfDepth` advance, `_speed`; it obeys `Stunnable`, so stun and ice stop or slow it), `Stunnable`, `RangeDummy` (`_body` = `Body`'s renderer, `_head` = `Head`'s renderer, `_critColor`, `_flashColor`, `_flashTime`); children `Body` (capsule mesh and collider, `Hitbox` Body) and `Head` (sphere mesh and collider, `Hitbox` Head, the critical spot). Both use `Art/Materials/Characters/TargetDummyBody` — an untextured white Lit material, so the faction and crit tints fill the whole part (a textured material would only tint where its texture is light). No `EnemyAI`, `NavMeshAgent` or `DespawnOnDeath`. Four instances stand at 25, 50, 75 and 100 m; none overrides `_data`, `RangeControl` sets it.
- **Range shell** (`ShootingRange` object) — floors, a back wall with the door, side walls, an end wall and a `Roof` (shadows off, so the light still reaches the floor) close the range completely. Every wall, floor and the roof carries a `TiledSurface` (`_metersPerTile` = 1), which sets the texture repeat from the object's size so the pattern stays one repeat per metre however the object is scaled; all walls and the roof share `ShootingRangeWallShort`, the floors `ShootingRangeFloorSmall`. A new wall piece only needs a cube with the wall material and a `TiledSurface`.
- **Map and culling** — a `WorldMap` object (`WorldMapArea`, 28 × 122 m centred on the range, fog of war on, `_captureMask` without the `Ceiling` layer) feeds a `Minimap` element added under the HUD (`_area`, `_viewer` = the Player's camera), and a `MapRevealer` added to the Player lifts the fog around it. Both roofs are on the `Ceiling` layer (new, layer 7) so the top-down capture sees the floor instead of the roof. The new wall pieces and cover carry `CullableObject` like the older walls, so the `GameManager`'s `VisibilityCullingManager` hides them out of view; roofs and floors don't.
- **Lane cover** — four low walls (`Lane Cover 1`–`4`, 1.2 m high, wall material with `TiledSurface`) stand in the lane at 10, 12, 36 and 61 m from the firing line, clear of the dummies' paths, for testing line of sight, penetration and vaulting. They are plain scene cubes.
- **Melee room** (east of the booth) — an open doorway in the booth's east wall (4 m wide, 3.5 m high, no door) leads into a closed 12 × 10 m side room with its own floor, walls and roof (same `TiledSurface` setup). One `TargetDummy` instance (`TargetDummyMelee`, 12 m east of the booth's centre, facing the doorway) stands inside. It is the fifth entry in `RangeControl._dummies`, so the switches change it like the others and it sways across the room and advances along it (keep `_halfDepth` inside the room).
- **`RangeControl`** (object `RangeControl`, scene root) — `_factions` = `Factions/TechFaction`, `BioFaction`, `VoidFaction` (order matters for Mixed), `_dummies` = the four lane dummies' `RangeDummy` (nearest first), then the melee-room dummy's. Holds the range's settings (faction or Mixed, tier 1–3, movement Still, Sway or Advance, distance) and applies them at start and on every change. The object itself stands on the firing line (z 5.6), the point distances are measured from. `_laneDummies` = the four lane dummies (the melee-room dummy isn't in it, so it never moves); `_distanceScales` (1, 0.5, 0.2) are the shares of each lane dummy's placed distance the Distance switch cycles through, so the lane runs 25–100 m, 12.5–50 m or 5–20 m.
- **`LaneRuler`** (object `LaneRuler`, scene root, on the firing line at z 5.6) — writes the distance (`_interval` 25 m, `_marks` 4) on the floor along both walls when the scene starts; nothing to assign.
- **`DamageReadout`** (object `DamageReadout`, scene root) — builds its own panel on a screen-space overlay canvas, halfway up the left edge so it stays clear of the corner HUD (`_size`, `_edgePadding`). `_range` = the `RangeControl`, so the Reset switch clears it. It listens to `CombatEvents` and shows the player's hits on dummies only.
- **`Prefabs/Environment/Level/RangeSwitch`** — a pillar with a `Collider` and `RangeSwitch` (`_range` = the `RangeControl`, `_setting`). Five instances stand on the firing line (z 3.8): `Faction`, `Tier`, `Movement`, `Distance`, `Reset`, each with its own `_color` tint.

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
- Add **CullableObject** to world meshes that should be culled off-screen.
- Custom interactables just implement `IInteractable` — no extra wiring.

### Prefabs

| Prefab | Needs |
|---|---|
| `Prefabs/Weapons/Projectile` | `Projectile` + a renderer. `TrailRenderer`? is reset on every launch. |
| `Prefabs/Weapons/FragGrenade` | Root: `Rigidbody` (mass 0.4, no linear drag so the trajectory preview stays true, angular damping 2 so it settles instead of rolling on, *Interpolate* on, *Continuous Dynamic* so a thrown grenade can't pass through enemies), solid `SphereCollider` with `Physics Materials/Grenade` (bounciness 0.4 with *Maximum* combine, so it bounces the same off any surface; friction 0.6), `Grenade` (`_body` = the Body child's renderer, which takes the grenade's colour and blinks). Children `Body` (`Materials/Environment/Props/GrenadeBody`, a URP Lit material — the grenade's tint is set on its `_BaseColor`), `Cap`, `Lever` (`NoirSteel`) are visual only. Shared by every grenade type |
| `Prefabs/VFX/ExplosionFlash` | A sphere with `ExplosionFlash` and the transparent `Materials/ExplosionFlash`; set as a grenade's `ExplosionEffect`. Pooled automatically |
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

- Type `/help` for commands (every command starts with `/`). Anything the console can hand out by name is listed in `DevCatalog` — add new items, weapon categories, enemy prefabs, buffs and dodges there.

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
| Throw | `MinThrowSpeed` (tap) → `MaxThrowSpeed` (fully charged) in m/s, `ChargeTime` (seconds to full, 0 = always full), `ThrowAngle` (upward lift, degrees), `InheritVelocity` (share of the thrower's movement) |
| Fuse | `Fuse` = Timed or OnImpact, `FuseTime` (OnImpact: the longest it may fly), `ArmTime`, `FuseBlinkColor` |
| Explosion | `ExplosionRadius`, `ExplosionDamage`, `EdgeDamage` (share left at the edge), `ArmorPenetration`, `DamageType`, `HitMask`, `NeedsLineOfSight` + `BlockerMask`, `PushForce`, `NoiseRadius` |
| Aftermath and effects | `LingeringZone` (a smoke cloud or `Prefabs/VFX/FireZone`, left where it goes off), `ExplosionEffect` + `FlashColor`, `CameraTrauma` + `ShakeRadius`, `OnHitEffects` (a blast with no damage still applies them), `ThrowSound`, `ExplosionSound` |
| Preview | `TrajectorySteps`, `TrajectoryStepTime`, `PreviewColor` |

Range on flat ground is about speed² ÷ 9.8, so the Frag Grenade throws from about 6 m (a tap) to about 33 m (full charge).

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

- Enemies need **no** changes — `EnemyPerception` checks stealth automatically.
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
- **Blueprint** shows the floor plan; its toolbar picks the `LevelBuildSettings` to lay out with (defaults to the project's first one, e.g. `Level/DefaultLevelBuildSettings`) and the fill (room type / faction). It uses the graph asset's seed, as `LevelBuilder` does with `_seed` empty.
- **Analyze** opens the style report for the asset's style (seed count and first seed in its toolbar; the first seed can be a number or any text).
- **Play** opens `Scenes/MapTest`, points its `Level` (LevelBuilder) at the asset with the asset's seed and enters play mode; **Play from here** (side panel) also sets `_startRoom` to the selected node. Nothing is saved to the scene: the previously open scenes are reloaded when play mode ends. It needs a LevelBuilder somewhere in MapTest.
- **Gallery** opens the seed gallery for the asset (`MapSeedGalleryWindow`): previews for a page of seeds counting up from its toolbar's first seed; **Use** on one generates the asset from that seed.
- A room function only prefers sections listed in a style's content; a style without `_sections` gives every room no section.
- A new map style is usually just a new style asset pointing at existing layouts and content.

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
| **LevelBuilder** | `_settings` = `Level/DefaultLevelBuildSettings` · `_graphAsset` = a `MapGraphAsset` **or** `_generation` = a `Map/…MapGenerationSettings` style (e.g. `BranchingMapGenerationSettings`, or `RandomMapGenerationSettings` for a different shape each run) · `_seed` (a number or any text; empty or 0 = random; with `_graphAsset`, empty or 0 = the asset's seed) · `_startRoom` (testing: a node id to start and respawn in instead of the Start room; -1 = Start) · `_navMesh` = its NavMeshSurface · `_player` = Player · `_spawnPoint` = RespawnPoint · `_worldMap`? = WorldMapArea · `_blueprintMap` (on: the map shows the level's floor plan — rooms coloured by type, walls, gates — and covers exactly the level; off: the area's own background) · `_encounters`? = `Level/DefaultEncounterSettings` (empty = encounter rooms play as plain rooms) · `_objectives`? = `Level/Objectives/DefaultMapObjectiveSettings` (empty = no map objectives; needs a `QuestTracker` on the player) | With objectives, the Boss room's doors become `_bossDoorPrefab` (empty = the build settings' terminal door; it needs a `ConditionLock`), labelled "Objectives". Logs a warning for every corridor that had to cross another. Announces the run's modifiers through the HUD feed (needs a FeedbackPlayer in the scene). |
| **NavMeshSurface** | Collect Objects = **Current Object Hierarchy** | Rebuilt at runtime. Don't bake it. |

`LevelBuildSettings` holds the grid sizes, wall materials, corner bevels (`_roomChamfer` 0–2 tiles, default 1; `_chamferCorridorBends`), the wall art (`_wallKit` = `Level/WallKits/TechWallKit`, `_factionWallKits` = TECH/BIO/VOID Faction → their `…WallKit`; empty `_wallKit` = plain box walls), `_factionDesigns` (each faction's `Level/Designs/…FactionDesign`; a faction without one is built plainly), how round curved rooms are (`_curveSmoothing` 0–3 passes, default 2; 0 = 45° walls), door prefabs, the key prefab, door signs (`_buildDoorSigns`, `_doorSignMaterial` — empty = the floor material, tinted and made emissive — and `_dangerIntensity`), the default room **Shapes**, the **Functions** catalogue (`_functions`: every `RoomFunction` in `Level/Functions/` — a room picks one of its own category, whatever its node type; a landmark is used at most once per map) and one **Rooms** entry per room type.
Each Rooms entry lists enemy prefabs (count read at the room's intensity), a centrepiece, a *Dressing* asset (cover and clutter) and offerings — not where the room is; the Breach entry uses the Combat enemy count (its enemies come from the two factions' rosters).

| Asset | Folder | Sets |
|---|---|---|
| **RoomFunction** (48, one catalogue: Quarters, CabinRow, Commons, School · Park, Pool, Gym, WellnessCenter, ObservationDeck · Casino, Theatre, Bar, ExhibitHall, GrandAtrium (unique) · Promenade, Restaurant, FoodCourt, Kitchen · MedicalBay, Pharmacy, Ward, Morgue · SecurityOffice, Armory, Barracks · Workshop, ProcessingPlant, LifeSupport, EngineRoom, ReactorCore (landmark) · Bridge (unique), ControlRoom, Offices · HydroponicsBay, Farm, WaterTreatment · Laboratory, DataCenter, Library · Lobby, CargoBay, DockingBay, Hangar, EscapePodBay, Storage, Junction · Chapel, Shrine — plain placeholders except the original few) | `Level/Functions/` | `_category` (RoomCategory; a map node with a category only picks functions of that category — Lobby/CrewQuarters Accommodation, Park Recreation, Casino/GrandAtrium Entertainment, Restaurant Commercial, ReactorCore Engineering, CargoBay/DockingBay/SupplyDepot Transit), `_preferredSections` + `_sectionPreference` (weight × inside them), `_chamfer` (-1 = the settings' `_roomChamfer`; GrandAtrium and DockingBay use 2), `_shapes` (empty = the settings' shapes), `_cells` (1–2 each way), `_size` in tiles (single-cell only; 0 = `_roomTiles`), `_wallHeight` (0 = the settings'), `_openCeiling`, `_landmark`?, `_structure` rules in order, `_props` |
| **Enemy prefab** (MaintenanceBot, Scavenger, Husk) | `Prefabs/Characters/Enemies/` | Root: `NavMeshAgent`, `EnemyHealth` (`_data`, `_hitboxProfile` = the target dummy's, `_healthBar`), `EnemyAI` (`_data`, `_obstacleMask` = Default), `Stunnable`, `StatusEffectController`, `EnemyHealthBar`, `EnemyStateVisuals` (`_renderers` = Visor), `DespawnOnDeath`, `MapMarker` (faction colour). Children: `Body` (capsule collider + `Hitbox` Body), `Head` (sphere collider + `Hitbox` Head), `Visor`, faction accents (visual only). Stats in `Data/Enemies/…EnemyData` — keep low tiers near 100 effective health (`EnemyData.EffectiveHealth` = (health + shield) × (1 + armour/100)) |
| **WallKit** (Tech, Bio, Void) | `Level/WallKits/` | `_straight` (repeated along straight walls), `_angled` (bevels and curves; empty = straight), `_post` (corners and wall ends), `_doorFrame` (every doorway, room side), `_pieceLength` / `_pieceHeight` (authored size, 3 × 4 m), `_postMinAngle`. Pieces: origin on the floor at the wall's start, +X along it, +Z toward the room; door frames like door prefabs (+Z out of the room). Visual only — the box walls stay as invisible collision. Placeholders in `Prefabs/Environment/WallKit/` — plain colour-coded blocks, one per faction (a single panel for walls, a column for posts, two jambs and a header for door frames), no decoration; materials in `Art/Materials/Environment/WallKit/` |
| **LandmarkRoomDefinition** (ReactorCore) | `Level/Landmarks/` | `_prefab` (interior; origin at the room's south-west floor corner), `_sizeTiles`, `_doorways` (tile + side), `_occupiedTiles` (furniture) |
| **RoomShape** (Rectangle, L, T, U, Cross, Ring, Dome) | `Level/Shapes/` | parts, `_connections` range, `_curvedWalls` (Dome) |
| **Structure rules** (Wide/Dense Pillar Grid, Broken Column Grid, Column Ring, Corner Pillars, Divider Wall, Service Counter, Checkpoint Barrier, Wall Remnants, Office Maze) | `Level/Structure/` | spacing, sizes, heights; optional `_pillarPrefab` (origin at the base) replaces the plain box; grids: `_missingChance`, `_brokenChance` and `_brokenHeight` (Broken Column Grid: 25% gone, 55% of the rest snapped off at 0.7–2.4 m); partitions: `_minZoneTiles` (8: a wall that would leave less floor than this on either side is skipped; Checkpoint Barrier is one at the middle, 1.1 m high with 3-tile gaps); **WallRemnants**: `_count` per 64 tiles, `_length` in tiles, `_height` rolled per piece (ragged tops; never across a walkway or beside a doorway, never zone dividers); **OfficeMaze**: `_hallTiles`, `_officeDepth`, `_officeWidth`, `_suiteChance`, `_throughChance`, `_doorHeight` (halls along the longer axis, offices between them, a doorway with a lintel onto a hall from every office; rooms under 6×5 tiles are left alone) |
| **FactionDesign** (Tech, Bio, Void) | `Level/Designs/` | `_faction`; *Floor plans*: `_shapes` (a shape asset and a weight multiplier — 0 never, above 1 more often) and `_roomSizeScale` (0.5–1.25 of a function's room size; multi-cell halls and landmarks keep theirs); *Structure*: `_structure` rules, applied only to rooms whose function has none of its own; *Dressing*: `_dressing` (the faction's own props, placed last), `_swaps` (a room type's generic *Original* dressing → this faction's *Replacement*: the same cover and loot clutter in its own pieces), `_fillShare` (share of the floor props may take) and `_propScale` (multiplies every prop count, required ones excepted). Shipped: Tech (rectangles and crosses; `WidePillarGrid` + `CheckpointBarrier`; sparse, 0.3 / 0.9×), Bio (domes and rings; `BrokenColumnGrid` + `WallRemnants`; 0.3 / 1×), Void (L/U/T plans, rooms at 0.8× size; `OfficeMaze`; crowded, 0.55 / 1.4×) |
| **RoomDressing** (CombatCover, Loot) | `Level/Dressing/` | `_props`: the same placements as a function's, placed after its furniture. `CombatCoverRoomDressing` (low `Props/Barrier`s on the longest sightlines, crate piles in corners, wall crates; counts scale with room size) is the *Dressing* of the Combat, Elite, Boss, Breach, Lockdown, Holdout, Stealth, Rift and Hazard rules; `LootRoomDressing` (crate piles, wall crates) that of Treasure and Ambush. Rules without one get only their function's furniture |

- A rule with no **Functions** gives plain rooms from the default shapes; empty default shapes = square rooms.
- A shape is only used for rooms whose connection count is in its `_connections` range; if nothing fits, the room is a square.
- `_roomTiles` is 12: at 8, doorway clearance fills most of a room and pillars rarely fit.
- Pillars and inner walls use the wall material and go on `_geometryLayer` with the walls.
- A landmark that doesn't fit its cells, or has fewer doorways than the room's connections, falls back to a generated room (with a warning).
- **Props** (`_props` on a function or a *RoomDressing*): placed in the listed order, so list what a player comes for first, then furniture, then cover and clutter. Each placement sets: `Prefabs`, `Count` (`ScaleByRoomSize` multiplies it by floor area ÷ 64 tiles, 0.4×–2×), `Footprint` (floor tiles it covers, width × depth, turning with the prop; 1×1 can take any angle, bigger ones snap to quarter turns), `On` tags (Edge = along walls, Corner…; a big footprint needs one tile to match), `Avoid` tags, `Zone` (Largest = main space, Others = rooms split off by inner walls), `Facing` (Random, Square, AgainstWall, TowardCentre, AcrossSightline — a barrier's front toward the longest open lane through it, AlongRoom — its length (Z) down the room's longer axis, front toward the middle), `Clearance` (tiles of floor kept free in front; nothing else is put there), `Spacing` (free tiles kept between two of the same placement), `Selection` (Random; Spread = evenly spaced, each one as far as possible from what is already placed; Cover = on the longest open line of sight, only where it is 4+ tiles; Cluster = next to what is placed, with the odd new patch; Line = along what is placed down the room, so machines form rows), `Pile` (props per tile for a 1×1, for crate stacks), `Jitter` (how far a 1×1 wanders from its spot) and `Required` (always placed: its picky rules are loosened until it fits), `Lattice` (only on tiles on a grid of this pitch counted from the room's middle: neat rows; 0 or 1 = anywhere), `Symmetric` (each one is mirrored across the room's long axis when its twin fits; quarter turns only) and `Decor` (ground cover with no collider: takes no floor, may lie on walkways and in doorways, never on a pillar or prop, and doesn't count toward the fill share).
  Every placement keeps all doorways and floor reachable, and props together never take more than about 35% of a room's floor (required ones excepted). A prop's visible size should stay inside the footprint it declares; a pile's members must each fit a quarter of a 3 m tile.
  Placeholder props (Crate, Table, Bed, Planter, SlotMachine, Barrier; TECH: TechBarrier, TechContainer, ProductionLine (1×2 tiles), ServerRack, SentryPost; BIO: Bush, OvergrownTree, RuinSlab, FallenLog, MossyCrate, and the collider-free GrassPatch, MossPatch, Rubble; VOID: OfficeDesk (visitor's side +Z, chair behind), Chair, CabinetBank, Sofa, Safe, StreetLamp, OverturnedDesk, NoirCrate — built from primitives, in the faction wall kits' palettes and four noir materials in `Art/Materials/Environment/Props/`) are in `Prefabs/Environment/Props/` — origin at the base (the middle of its footprint), +Z the front. Swap in real art there.
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

- **Encounter rooms**: with `_encounters` set, `LevelBuilder` gives every Lockdown, Holdout, Ambush, Stealth, Rift, Puzzle, Hazard, Spawner, Shrine, Siege and Gauntlet room an encounter object (`<Type>Encounter_<id>` under the Level) after the NavMesh is baked: a `RoomShutter` just inside each of the room's doorways (sunk into the floor until the room seals), spots set aside for arriving enemies and consoles, and the room type's encounter component. Nothing to place by hand. Gamble and Emergency Exit rooms are plain rooms whose rule brings a centrepiece.

  | `EncounterSettings` field | Default asset uses | Is |
  |---|---|---|
  | `_shutterPrefab` | `RoomShutter` | `RoomShutter` on the root, `_slab` = its `Slab` child (a doorway-sized box with a BoxCollider = `_blocker`), origin on the floor at the doorway, +Z out of the room |
  | `_rewardPrefab` | `Loot/GlobalChest` | `LootContainer` + `LootDropper` (`_table` = `Loot/Containers/GlobalChestLoot`, `_dropOnDeath` off, pickups = `LootItemPickup` / `LootWeaponPickup`). The same chest as the Treasure centrepiece, so an Ambush looks like a Treasure room |
  | `_terminalPrefab` | `LockTerminal` | the Holdout uplink and the Hazard vent controls |
  | `_puzzleSwitchPrefab` | `PuzzleSwitch` | `PuzzleSwitch` (`_indicator` = its light) on a console with a collider |
  | `_puzzleDoorPrefab` | `PuzzleLevelDoor` (blue) | like `TerminalLevelDoor`: `Door` + `ConditionLock` (`_label` Calibration, `_required` 1) on the `Hinge` |
  | `_hazardEffects` | Fire, Poison, Ice, Lightning `…Effect` | one per Hazard room; `_hazardMaterial` (tinted per effect) = `WallKit/mat_TechLight` |

  Tuning on the same asset: wave size (read at room intensity), spawn spots, Holdout duration/interval/map reveal, Ambush and Rift waves, Spawner prefab/interval/batch/cap/activation range, Shrine altar/boons/offers, Siege objective/duration/wave interval, Gauntlet waves/growth/luck/twists, Stealth bonus luck and alarm waves, puzzle node count and scramble, hazard tick and strength.
- **Encounter room rules** (`DefaultLevelBuildSettings` › Rooms): Lockdown and Stealth rooms get build-time enemies (`EnemyCount` 3–6 and 2–4) that the encounter watches; Holdout, Ambush, Rift and Puzzle start empty (their enemies arrive as waves); Hazard has a few; Spawner starts empty (the structure sends them) and needs no room rule. `EncounterSettings` › `_spawnerPrefab` must be a prefab with `EnemySpawner` + `HealthManager` (and a collider/hitbox so it can be shot); without it Spawner rooms are plain rooms.
- **Shrine / Siege / Gauntlet** (`EncounterSettings`): Shrine needs `_altarPrefab` (`ShrineAltar` + a collider) and `_shrineBoons` (`StatBoon` assets, `CGD/Stats/Boon`: a label plus stat modifiers; negative values make a curse). Siege needs `_siegePrefab` (`SiegeObjective` + collider on a layer enemies target, i.e. the player's). Gauntlet needs nothing but may list `_gauntletTwists` (`StatBoon`s laid on the player one per wave). None of the three needs a room rule; Siege and Gauntlet rooms start empty and use the cover Dressing. Without its prefab/boons a Shrine or Siege room is just a plain room.
- **Workshop** (`DefaultLevelBuildSettings` › Rooms): a normal room whose `Centerpiece` is a crafting bench; the default uses `Prefabs/Environment/Level/HubWorkbench` (a `CraftingStation` with its recipes) — swap in a dedicated bench prefab to give it other recipes. All new room types are in `StandardMapContentSettings`; other map content assets need a rule added by hand. Centrepieces: Rift → `RiftCore`, Gamble → `GambleStation` (a credit machine and a blood machine: `GambleMachine` + `LootDropper` each, `_payment` = Credits or Blood, `_dropper` = that machine's `LootDropper`, `_currency` = `Economy/CreditsCurrency`), Emergency Exit → `EscapePod` (`LevelExit` with `_emergency` on, hold 3 s). An Emergency Exit's pod doesn't stop the real Exit pad from being placed.
- **Junction rooms** use a `LevelBuildSettings` room rule with `Type` = Junction, no enemies; `JunctionRoomFunction` (a small 6–8 tile Transit room) is in the Functions catalogue. In `MapContentSettings` the Junction rule uses `_minConnections` 3, so it only lands on forks. Direct doors need nothing in the scene: `MapLayoutSettings._directDoorChance` (0 = never) sets how often a plain link between neighbouring cells becomes one, and a connection's *Direct door* toggle in the Map Graph window sets it by hand.
- **Quiet rooms** are normal rooms (no encounter): their rule has `EnemyCount` 0 and **Offerings** = `Props/SalvageNode` (a `ResourceNode` yielding `ScrapMetalResource`), `Props/AmmoCache`, `Level/RewardCache`. Any room rule can list `Offerings`; one of them is always placed in every room of that type, in a nook: against a wall, off the walkways and doorways, with a tile of floor in front of it, away from what is already placed.
- Prefabs for all of the above are in `Prefabs/Environment/Level/`.

### Expedition loop (extraction)

```
Expedition         [ExpeditionRunner]          ← scene root
HUD (prefab instance)
  ExpeditionScreen [ExpeditionScreen]          ← stretched RectTransform, layer UI, last child (draws on top)
```

| Component | Assign | Notes |
|---|---|---|
| **ExpeditionRunner** | `_inventory` = Player's PlayerInventory · `_weapons` = PlayerWeaponLoadout · `_equipment` = PlayerEquipment · `_input` = PlayerInputHandler · `_starterKit` = `Expedition/DefaultRunStarterKit` · `_level`? = the LevelBuilder (seeds the loadout) | Settles the run when GameFlow reaches Victory (extract) or GameOver (death). Needs `GameFlow` (with `ExpeditionSession`) in the scene. |
| **ExpeditionScreen** | `_runner` = the ExpeditionRunner | Builds its own hidden panel; shows after a run with the hold, the next-run kit and Deploy (restarts the scene). |
| **PlayerLifecycle** (Player) | `_gameOverOnDeath` = on | Death must end the run, or nothing is ever lost. |
| **PlayerInventory** (Player) | `_startingStacks` = empty | The starting room's loadout replaces the Sandbox's dev ammo and credits. |

`RunStarterKit` (`Data/Expedition/`): `_startingWeapon` (a weapon category, rolled fresh each run — `Weapons/Categories/ModularPistolCategory`), `_startingOffhands` (one-handed melee categories; one is rolled into the starting weapon's offhand — default `Weapons/Melee/` Dagger, Sword, Axe; empty = none), `_gear` + `_gearCount` (random extra gear — armor or weapon categories — no piece twice; default `Items/Armor/CombatVestArmor`, 0–1), `_supplies` (item + count range each).
`Scenes/MapTest` is wired this way: `Expedition` root, `ExpeditionScreen` added under the HUD instance, and the two Player overrides above as instance overrides (the Player prefab and Sandbox are unchanged).
- **Obstacle masks**: generated geometry goes on `_geometryLayer`. Keep that layer in enemies' `_obstacleMask` and in weapons' hit masks.

### Director (pacing)

```
Director  [GameDirector, ReinforcementSpawner, SupplySpawner, DirectorMusic]   ← scene root
```

| Component | Assign | Notes |
|---|---|---|
| **GameDirector** | `_settings` = `Director/DefaultDirectorSettings` · `_player` = Player's PlayerHealth | Tracks the player's intensity and performance and runs the pacing cycle. One per gameplay scene; pauses with GameFlow (any state but Playing). The rest below need it. |
| **ReinforcementSpawner** | `_settings` = `Director/DefaultReinforcementSettings` · `_director` = the GameDirector · `_player` = Player's Transform · `_obstacleMask` = geometry that hides a spawn (Default — the level's `_geometryLayer`) | Spawns only while the director allows it. Needs a baked NavMesh; the arrival sound needs an `AudioPool` in the scene. Optional. |
| **SupplySpawner** | `_settings` = `Director/DefaultSupplySettings` · `_director` = the GameDirector · `_health` = Player's PlayerHealth · `_inventory` = PlayerInventory · `_weapons`? = WeaponController (without it only healing is handed out) · `_obstacleMask` = Default | Needs a baked NavMesh. Optional. |
| **DirectorMusic** | `_settings` = `Director/DefaultMusicSettings` · `_director` = the GameDirector | Creates its own 2D AudioSources as children. Layers without a clip are skipped, so it is silent until clips are assigned. Optional. |

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
| `Timing/` | `GameTimeSettings` | GameTime |
| `Enemies/` | `MaintenanceBot`/`Scavenger`/`Husk` `…EnemyData` (tier 1, ≈100 effective health each; the firing-range dummies run on them), `Tech`/`Bio`/`Void` `Tier2`/`Tier3` `…EnemyData` (placeholders, ≈2.5× / ≈5×; *Tier* set on each), `DefaultEnemyData` | EnemyAI, EnemyHealth |
| `Enemies/Perception/` | `DefaultPerceptionProfile`, `WaryPerceptionProfile`, `SkittishPerceptionProfile` | EnemyData › Senses |
| `Enemies/BehaviorTrees/`, `Enemies/AITest/` | `MeleeEnemyBehaviorTree`, `RangedEnemyBehaviorTree`, `Shared/` (`RegroupSubtree`, `RaiseAlarmSubtree`, `RoutineSubtree`); `MeleeTestEnemyData`, `RangedTestEnemyData` (use the trees; AITest scene) | EnemyData › BehaviorTree |
| `Enemies/`, `Enemies/Affixes/` | `DefaultEnemyAffixPool` (affixes per tier: 0 / 1 / 2), `Armored`/`Warded`/`Vital`/`Brutal`/`Swift` `…Affix` | EnemyAffixes |
| `Combat/HitboxProfiles/` | `DefaultHitboxProfile`, `TargetDummyHitboxProfile` | EnemyHealth, PlayerHealth |
| `Combat/StatusEffects/` | Bleed, Fire, Ice, Lightning, Poison | on-hit effect lists |
| `Combat/ActionTimelines/` | `GroundSlamTimeline`, `ArmingSwordSlashLeft/SlashRight/Thrust/HeavySlashTimeline` and `<Sword>Light1…4Timeline` / `<Sword>HeavyTimeline` for every other sword, and `<Axe|Dagger|Hammer|Spear>Light1…3Timeline` / `…HeavyTimeline` (bashes: `GuardBashTimeline` (every melee category's *Bash* and the fists), `PistolWhipBashTimeline`, `RifleBashTimeline`, `TacticalKnifeBashTimeline`, `ShieldBashTimeline` (all three shields); each assigned as `Timeline` on its category's light combo steps and heavy attack; a step's own Damage stays its combo multiplier, the timeline's Swing Hit Damage is 1). A melee slash/thrust timeline adds a *Swing Hit* event (*Keys* = frame + Yaw/Pitch/Push poses, Near/Far Distance, Radius, *Anchor* Aim or Socket, Damage); turn on the weapon's debug draw to see its capsule trail. Scene wiring only for *Anchor* = Socket (see MeleeController) | TimelineAbility, melee attack steps |
| `Weapons/Ranged/` | `DefaultWeaponData`, `T1/` examples per category | Loadout, WeaponPickup |
| `Artifacts/` | `HandMirrorArtifact`, `WhisperingTomeArtifact`, `BoneIdolArtifact`, `OilLanternArtifact` (`ArtifactDefinition`: `_statRanges` (passive stats; a curse is a negative range), `_behavior` (empty = passive only), `_potency` range, `_mainHandPenalty`, `_perkPool`, `_guaranteedPerks`, `_reality` = Void; roll profile = `Items/StatRollProfile`); behaviors: `MirrorWardBehavior` (Ward: `_profile` = `Combat/Reflect/MirrorWardReflect`, `_drainPerSecond` = stamina), `TomeCastBehavior` (Cast: `_ability` = `Abilities/EldritchRendAbility`), `LanternChannelBehavior` (Channel: `_modifiers`, `_drainPerSecond`) — each behavior has a `_usage` line shown in the UI | `DefaultLootTable`, DevCatalog items (`give hand mirror`) |
| `Perks/Artifacts/` | `DefaultArtifactPerkPool` — armor perks that also fit artifacts (*Fits* = Armor + Artifact: Battle Focus, Opportunist, Vampiric, Counterguard, Focused, Bloodthirst, Regenerator, Reactive Plating) | `_perkPool` on the artifact definitions |
| `Weapons/Shields/` | `BucklerShield`, `RiotShield`, `TowerShield` (`ShieldDefinition`: rolled block share and stamina per blocked damage, guard arc/parry window/stun, *Blocks Passively* for the tower shield, bash step + rolled bash damage, `StaminaMeter` = `Meters/StaminaMeter`, *Main Hand Penalty*; roll profile = `Items/StatRollProfile`) | `DefaultLootTable`, DevCatalog items (`give buckler`) |
| `Weapons/Categories/` | one per weapon type (*One Handed* on Pistol, Modular Pistol and SMG), each with *Quick Melee* = `PistolBashMeleeWeaponData` (pistols) or `RifleBashMeleeWeaponData` (the rest); `ModularPistolCategory` (the starter: Pistol type with *Firing Styles* — Semi / Burst / Machine / Scatter (Shotgun fire behavior, pellets, ADS cone) — and *Stat Styles* — Heavy / Marksman / Rapid / Carbine, each weighted with stat scales; *Guaranteed Perks* = `Perks/Weapons/TacticalKnifePerk`) (incl. `BowCategory`: Volley fire behavior, *Bows* section (`HasDrawStances`, vertical/horizontal draw multipliers, volley spacing), Pellet Count = arrows per shot; `CrossbowCategory`: Projectile fire behavior, `AutomaticMagazineSize` for autoloaders; both use Arrows ammo, with arrow speed, drop and draw time in *Charge & Projectile*) | RandomWeaponPickup, loot |
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
| `Expedition/` | `DefaultRunStarterKit` — the starting room's loadout (Modular Pistol + a random Dagger/Sword/Axe offhand, 0–1 random gear, supplies) | ExpeditionRunner |
| `Director/` | `DefaultDirectorSettings` (stress per damage taken / nearby kill, combat memory, intensity decay, peak 0.75 held 4 s, calm below 0.25 for 30–45 s, 15 s opening calm; *Performance*: pressure 0.7–1.35, easy peak ≥ 60% health, hard ≤ 30%, step 0.2, death 0.5), `DefaultReinforcementSettings` (Husk, Scavenger, MaintenanceBot; 2–4 every 20–35 s at neutral pressure, max 6 alive, 18–30 m away; arrival message, no sound yet), `DefaultSupplySettings` (`LootItemPickup`; 2 `BandageConsumable` below 50% health with < 2 carried; 3 magazines of the matching munition below 2 spare; 4 s into a calm, 4–9 m away), `DefaultMusicSettings` (4 layers and 2 stingers, no clips yet) | GameDirector, ReinforcementSpawner, SupplySpawner, DirectorMusic |
| `DevTools/` | `DevCatalog` (all items, weapon categories, buffs; no enemy prefabs exist yet) | DevCommands |
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