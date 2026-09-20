# CSS Paradise: Game Design Document

# Game Overview

## Vision

### Elevator Pitch

You are aboard the CSS Paradise, a colonial starship torn apart by a cosmic anomaly and stitched back together by an AI that broke its own rules to save the colonists. Three incompatible realities (technological, biological, and cosmic) now occupy the same hull, bleeding into one another without warning. The AI that saved everyone is still running things, but it's no longer the same mind that was meant to be in charge.

Each run drops you into a procedurally generated section of the ship. The layout, reality balance, and threats change every time, but the world, its history, and its mysteries persist across runs.

### Genre

Extraction roguelite with exploration, survival, and narrative elements. Run-based structure with procedurally generated maps, risk/reward extraction loop, persistent world lore, and meta-progression across runs.

### Platform

*To be determined*

### Target Audience

Players who value atmosphere, discovery, and narrative ambiguity over explicit objectives. People who liked Outer Wilds' sense of piecing together a story, Prey's systemic environment, or Alien Isolation's tension, but are drawn to the *questions* more than the combat.

### What Makes This Different

- The environment is the central system: reality itself is unstable, not just the enemies or puzzles
- The antagonist (if there is one) is a version of the protagonist's own infrastructure: not evil, just *unbound*
- The three realities aren't levels or biomes: they contest the same space simultaneously
- The player starts knowing nothing: the ship's history, the realities, Paradise, their own identity. All of it is a mystery uncovered through play

See also: Design Pillars, Player Experience, Core Loop

## Design Pillars

Core principles that every design decision should be measured against.

### 1. Unstable Ground

The world should never feel reliable. Not through jump scares or random unfairness, but through the persistent sense that the space you're in might not stay the way it is. Reality instability is the defining feature: it's the world, the mechanic, and the theme.

### 2. Neither Side Is Wrong

The Captain and Paradise represent a genuine dilemma, not good-vs-evil. The game should resist giving the player a clean moral answer. Every piece of evidence should complicate the picture, not simplify it.

### 3. Discovery Over Direction

The player starts knowing nothing. What happened to the ship, what the realities are, who Paradise is, who the player character really is. The answers are earned through survival, exploration, and piecing together evidence across runs. Minimize waypoints, quest markers, explicit objectives. The ship is the puzzle; the player's curiosity is the engine.

### 4. The Environment Tells the Story

Logs and dialogue supplement, but the primary storytelling medium is the space itself. A corridor overtaken by BIO, a terminal still running TECH diagnostics, a room where VOID has bent geometry: these are the narrative.

### 5. Systems That Reflect the World

Game mechanics should emerge from the fiction. If reality is unstable, the player's tools and abilities should be unstable too. If the ship's AI is split, the systems the player interacts with should reflect that split.

See also: Vision, Player Experience, Reality Mechanics

## Player Experience

## References

# Gameplay

## Core Loop

The repeating cycle that drives every run and connects all systems.

### The Loop

> Docked Ship → Starting Room → Explore → Gather → Craft → Push → Boss → Extract
>      ↑                                                                    |
>      └────────────────────────────────────────────────────────────────────┘

#### 1. Docked Ship

The run begins at the docked ship, the player's persistent base outside the CSS Paradise:

- Choose what stored gear to bring (risk vs reward: anything brought can be lost on death)
- Fill weapon wheel and equipment slots from storage, or go in light
- Enter the CSS Paradise through the fixed docking port into a new layout

#### 2. Starting Room

The Starting Room provides a baseline and a hub to return to:

- Receive a random loadout (1-2 weapons, 1-2 equipment pieces, consumables, crafting materials)
- Access crafting, cooking, and smithing stations
- The starting room is always safe: realities do not contest it

#### 3. Explore

The player pushes into the generated map (15-20 rooms on easy):

- Read the environment to identify which reality is dominant: working lights mean TECH, organic growth means BIO, silence means VOID
- Navigate rooms without knowing the critical path (start → unlock → boss); discovery only
- Encounter enemies native to the dominant reality: Core Techs, ecosystem predators, VOID entities
- Find loot rooms (optional, not guaranteed to be on the critical path) for mid-game upgrades

#### 4. Gather

Resources come from the reality the player is in:

- TECH: metals, minerals, chemicals, silicones, energy cells, bullets, medicines
- BIO: bone, chitin, sinew, sap, spores, organs, flora and fauna equally
- VOID: cast iron, brass, silk, oak, glass, essence, cursed energy, relics
- Enemies drop materials native to their reality
- The environment itself is harvestable

#### 5. Craft

Return to the starting room hub to use gathered materials:

- **Crafting**: combine materials into items, components, tools
- **Cooking**: prepare consumables, healing items, buffs
- **Smithing**: forge and upgrade weapons and armor

Crafted gear rolls random stats. Higher quality resources increase the chance of higher item tiers. The player can also apply living modifications, engineering improvements, or anomalous empowerments depending on the materials used.

#### 6. Push

Re-enter the map better equipped:

- Clear rooms toward the boss unlock condition (may be split across multiple rooms)
- Rooms repopulate after X rooms cleared, adding light pressure to keep moving
- Boss unlock triggers a wave of repopulation across the map, marking the late-game shift
- Difficulty scales by distance from start: after boss unlock, harder enemies appear further out

#### 7. Boss

Reach and fight the boss:

- Boss is random within the map's difficulty tier
- Easy: single-reality boss. Medium: single or dual. Hard: dual. Final: Paradise (all three)
- The player doesn't know which reality's boss they'll face until they reach the boss room
- Boss encounter requires everything the run has built toward: gear, abilities, consumables, knowledge

#### 8. Extract

**On victory:**
- Keep all looted gear, resources, and crafted items
- Return to the docked ship with everything
- Store, equip, or save for future runs

**On death:**
- Lose all gear brought from the docked ship and everything gathered during the run
- The player is cloned: memory resets, stored gear on the docked ship persists
- The docked ship's state carries over: only the run's inventory is lost

### The Tension

The extraction loop is the core risk/reward system:

- **Bring nothing**: rely on the starting room's random loadout and what the map provides. Low risk, harder run
- **Bring everything**: maximize odds of success but lose it all on death. High risk, easier run
- **Bring selectively**: balance risk against the map's difficulty tier. The sweet spot most players find

Every run is a bet. The player decides how much to stake before they know what they're walking into.

### Meta Loop

Across runs, the player progresses through systems that persist beyond death:

- **Docked ship storage**: accumulated gear and resources from successful extractions
- **Skill trees**: permanent passive upgrades (Tech, Bio, Void) that survive death and cloning
- **Knowledge**: the player (not the character) learns the game's systems, enemy patterns, reality cues, and lore
- **Boss progression**: advancing through difficulty tiers toward Paradise

The character forgets. The player doesn't. The docked ship remembers what was stored. The skill trees remember what was earned. The gap between what the character knows and what the player knows is the narrative engine.

Narrative discovery is part of the meta loop. The player starts knowing nothing: not what happened to the ship, not what the realities are, not who Paradise is, not who they are. Each run that survives long enough reveals lore, story beats, and pieces of the mystery. Mechanical progression and narrative progression run in parallel: the player gets stronger and learns the truth at the same time.

See also: Starting Room, Map Structure, Combat System, Progression, Reality Mechanics, The Docked Ship

## Combat System

How the player equips, fights, and builds.

### Weapon Wheel

The player carries up to 4 weapons at once, switchable mid-combat:

- Weapons can be any combination: melee, ranged, relic, mixed, from any reality
- Ranged weapons are powerful but consume ammo (bullets, energy cells, arrows). Melee weapons have no resource cost. VOID relics run on cooldowns
- Using an ability tied to a weapon auto-swaps to that weapon (toggleable setting; player can choose whether to stay on their current weapon after ability use)
- The wheel enables combo potential between different weapon types and resource models

### Weapon Types by Reality

Each reality produces distinct weapon archetypes with different resource models:

#### BIO (Melee-Dominant)

Primarily melee with primitive ranged options:

- **Melee**: daggers, swords, axes, hammers, spears, shields
- **Ranged**: bows, crossbows
- **Resource model**: melee has no ammo cost, always reliable. Bows and crossbows consume arrows/bolts, craftable from common organic materials

BIO is the weapon wheel's backbone. No ammo anxiety, no cooldowns, always functional.

#### TECH (Ranged-Dominant)

Primarily firearms and energy weapons with utilitarian melee:

- **Ranged**: pistols, rifles, shotguns, SMGs, energy weapons
- **Melee**: fire axes, commando knives, batons
- **Resource model**: ballistic weapons consume bullets, energy weapons consume energy cells. Both are scarce and sourced from TECH resources

TECH ranged weapons are powerful but finite. The melee options exist for when you run dry, functional, not elegant.

#### VOID (Relic Weapons and Noir Firearms)

Channeling relics, ritual melee, and old-school firearms fitting VOID's premodern noir aesthetic:

- **Relic**: sceptres, wands, books, staves
- **Firearms**: revolvers, lever-action rifles, double-barrel shotguns, flintlocks
- **Melee**: ritual daggers, sacrificial blades
- **Resource model**: relic weapons operate on cooldowns, always usable but paced. Weaker relics recharge faster, stronger ones have longer cooldowns. Firearms consume bullets like TECH, but the weapons themselves are antique, not engineered

VOID relics don't consume ammo. The constraint is timing, not supply. They fill gaps between reloads, complement BIO melee, and provide ranged options without competing for any ammo pool. VOID firearms give the reality a conventional ranged option at the cost of ammo dependency.

#### Ammo Types

| Ammo | Source | Weapons |
|------|--------|---------|
| **None** | N/A | BIO melee, VOID melee |
| **Arrows/bolts** | Craftable from common materials | BIO bows, crossbows |
| **Bullets** | Scarce | TECH ballistic weapons, VOID antique firearms |
| **Energy cells** | TECH resources, scarce | TECH energy weapons |
| **Cooldown** | Time-based recharge | VOID relic weapons |

### Weapon Stats

Every weapon has a set of stats that define how it performs. Not all stats apply to every weapon, melee weapons don't have magazine size, bows don't have reload speed, etc.

| Stat | Description |
|------|-------------|
| **Base Damage** | Raw damage per hit before modifiers |
| **Attack Speed / Fire Rate** | How fast the weapon attacks or fires |
| **Crit Chance** | Probability of landing a critical hit |
| **Crit Damage** | Damage multiplier on critical hits |
| **Status Chance** | Probability of applying a status effect on hit |
| **Status Damage** | Base damage used for status effect calculations |
| **Lifesteal** | Percentage of damage dealt recovered as health |
| **Penetration** | How much damage/status bypasses defensive layers |
| **Range** | Effective distance (melee reach or projectile range) |
| **Magazine Size** | Rounds before reloading (ranged only) |
| **Ammo Reserve** | Total ammo carried for that weapon (ranged only) |
| **Reload Speed** | Time to reload (ranged only) |

These stats are randomly rolled based on the weapon's quality score. The quality score determines the total stat budget: low-quality items force tradeoffs between stats (high damage means low fire rate or small magazine), while high-quality items have enough budget to be strong across the board. Attachments and modifications can further influence individual stats.

### Armor Stats

Every armor piece has stats that contribute to the player's overall build. Different slots can roll different stat combinations:

| Stat | Description |
|------|-------------|
| **Health** | Bonus to the player's health pool |
| **Armor** | Physical damage reduction |
| **Shield** | Regenerative defensive layer that absorbs damage before health |
| **Cooldown Reduction** | Reduces ability cooldown durations |
| **Status Resistance** | Reduces chance or duration of incoming status effects |
| **Movement Speed** | Increases player movement speed |
| **Crit Chance** | Bonus probability of landing critical hits |
| **Crit Damage** | Bonus critical hit damage multiplier |
| **Status Chance** | Bonus probability of applying status effects on hit |
| **Status Damage** | Bonus to status effect damage calculations |
| **Health Regen** | Passive health recovery over time |
| **Shield Regen Rate** | How fast shields recharge after depletion |

Like weapons, armor stats are randomly rolled based on the piece's quality score. The same stat budget logic applies: low-quality armor forces tradeoffs between defensive and offensive stats, high-quality armor can be strong across the board. Armor pieces without an active ability compensate with stronger stat rolls across these values.

### Equipment Slots

The player wears equipment across multiple slots:

| Slot | Type |
|------|------|
| **Helm** | Head protection |
| **Torso** | Body armor |
| **Gloves** | Hand protection |
| **Legs** | Lower body armor |
| **Boots** | Footwear |
| **Backslot** | Versatile: capes, cloaks, backpacks, shields, quivers, power packs, or anything else that fits |

Each piece provides passive stats. Some also provide an active ability.

### Stamina

Stamina is the player's physical exertion resource. It depletes on physical actions and regenerates passively over time:

- **Melee attacks** consume stamina per swing
- **Dodge rolls** consume a fixed stamina cost
- **Sprinting** drains stamina while active

Stamina gives melee combat a resource cost comparable to ammo for ranged weapons. A player who swings recklessly runs dry and can't dodge. A player who manages their stamina can sustain both offense and evasion.

Ranged attacks, abilities, and consumables do not cost stamina.

### Abilities

Abilities come from equipped items: weapons, armor, and backslot alike. Any item can be a source. Abilities cost no resource, they run entirely on cooldowns:

- The player has **4 ability slots**
- Any available ability from any equipped item can be slotted into any of the 4 slots
- Abilities have a **cast time**, some require channeling, others are instant. Instant abilities can be woven into combos mid-fight. Channeled abilities are more powerful but leave the player vulnerable
- Abilities are further balanced by **cooldown duration**, stronger abilities have longer cooldowns
- The player chooses which abilities to slot, giving full freedom over their active kit

#### Stat vs Ability Tradeoff

Not every item has an ability. Items without abilities compensate with stronger passive stats:

- A full ability build (4 slotted abilities) has more versatility but lower raw stats
- A zero-ability build runs purely on massive stat increases: no active tools, all passive power
- Most builds fall somewhere in between; the player decides how much versatility vs raw power they want

With 4 weapons + 6 armor slots + backslot, the player could have 10+ items offering abilities but only 4 slots to use. Ability selection is always a meaningful cut.

### Consumables

Consumables also have cast times, using a healing item or buff mid-combat is not always free:

- Some consumables are instant, quick-use items that can be popped between attacks as part of a combo flow
- Others require channeling, stronger effects that force the player to commit and find a safe window
- Stronger consumables demand a safe window to use; weaker ones can be popped mid-combo

### Stealth

Combat is not the only way through a room. Stealth is a viable playstyle, the player can avoid fights entirely when the environment and room type allow it:

- **Bushes, overgrowth, organic cover**, BIO zones provide natural hiding spots
- **Vents, maintenance corridors, blind spots**, TECH zones offer infrastructure to move through unseen
- **Fog, darkness, sensory distortion**, VOID zones obscure the player, but also impair their own senses
- **Silent weapons**, some weapons allow engagement from stealth without alerting distant enemies

Each reality provides stealth differently, the player's approach changes based on what reality they're in, not just their loadout.

#### Avoidance, Not Assassination

Stealth is a movement and positioning system, not a damage system. There is no stealth damage multiplier:

- Stealth gets the player through rooms, not through enemies
- The reward for stealth is **tactical positioning**, choosing when, where, and who to engage first
- A player can set up a combo on the strongest enemy before the fight begins, eliminating the biggest threat safely
- Engaging from stealth starts a fight with advantage, but it is still a fight
- Stealth is about avoidance and information, not about inflating damage numbers

#### Room Conditions

Not every room can be snuck through. Some rooms have conditions that must be met before the doors unlock:

- **Exterminate**, all enemies must be killed to proceed. Stealth is not an option
- **Open**, no combat requirement. The player can fight, sneak, or run through
- Other room conditions are not yet defined

The mix of room types across a map ensures stealth is a tool, not an exploit. A stealth-focused player will eventually hit rooms that demand combat, and a combat-focused player may benefit from sneaking past a room they're not equipped to fight.

#### Weak Start Safety Net

Stealth ensures every run is beatable regardless of starting loadout. A player dealt a bad hand from the starting room's random gear can avoid fights they can't win, gather resources, and build up before engaging. The run is never a dead end, just a harder path.

### Build Freedom

The goal is creative and endless build options. The player has full freedom to combine:

- Weapons and armor from any reality: full TECH, full BIO, full VOID, or mixed
- Any abilities from any equipped items into any ability slot
- Stat-focused or ability-focused or hybrid loadouts
- Melee-heavy, ranged-heavy, or balanced weapon wheels

The skill trees (Tech, Bio, Void) provide permanent passive bonuses that naturally synergize with gear from their aligned reality, but the player is never locked into a single reality's gear.

### Open Questions

- Exact number of equipment slots: are there more beyond the listed six?
- How does weapon switching work in practice: instant swap, animation, cooldown?
- Can abilities be swapped mid-run, or only at the hub?
- How does ammo scarcity scale across difficulty tiers?

See also: Damage Calculation, Status Effects, Progression, Starting Room

## Reality Mechanics

How the three realities function as game systems.

### Reality States

No zone on the ship is permanently stable. The realities are in a constant tug of war. Every zone is ground being fought over. At any given moment, a zone leans toward one of these states:

| State | Description | Gameplay Effect |
|-------|-------------|-----------------|
| **Dominant TECH** | Ship systems in control, Paradise maintaining order | Full tool/interface access; Core Techs active |
| **Dominant BIO** | Ecosystem has claimed the zone | Survival hazards; territory/predator rules |
| **Dominant VOID** | Anomaly has broken through | Sensory deprivation; hearing/visibility/movement impaired; entities present |
| **Contested** | Two realities pulling at the same space | Partial rules from both; volatile; can tip either way |
| **Fractured** | All three in flux | Highly dangerous; rare; reveals deep lore |

### Shifts

The tug of war is constant, and zones shift as realities gain or lose ground:

- Proximity to the Quantum Core (closer = harder for any single reality to dominate)
- Paradise's active stabilization priorities (he can't hold everywhere; zones he deprioritizes get overtaken)
- Player actions (interfacing with systems, disturbing ecosystems, exposing anomalies)
- Narrative triggers (story beats that reconfigure the ship)
- The realities themselves pushing: BIO grows, VOID leaks, TECH erodes without maintenance

### Reading the Environment

The player needs to learn to *read* which reality is dominant and anticipate shifts. Environmental cues:

- **TECH indicators**: working lights, humming systems, clean geometry
- **BIO indicators**: organic growth, animal sounds, humidity, territorial markings
- **VOID indicators**: sound cutting out, visibility dropping, cold/frost, spatial distortion, the feeling of being watched

### Defensive Priorities

Each reality favors a different defensive layer distribution:

| Reality | Priority | Identity |
|---------|----------|----------|
| TECH | Armor > Shields > Health | Heavy plating, energy barriers, structural base |
| BIO | Health > Armor > Shields | Thick organic layers, rigid natural armor, regenerative barriers |
| VOID | Shields > Health > Armor | Wards and barriers, base resilience, minimal physical armor |

This applies to both gear from that reality and enemies native to it. A TECH enemy has heavy armor to crack. A BIO creature has a deep health pool. A VOID entity hides behind wards.

### Offensive Affinities

Each reality has a natural offensive identity:

| Reality | Affinity | Effect | Counters |
|---------|----------|--------|----------|
| TECH | Lightning | Anti-shield | VOID (shield-heavy) |
| VOID | Ice | Anti-armor | TECH (armor-heavy) |
| BIO | Bleed + Poison | Sustained attrition | No hard counter, wears down everything over time |

TECH and VOID are a direct counter pair: each targets the other's primary defense. BIO sits outside that dynamic as an attrition specialist. Its damage bypasses armor (bleed) and shields (poison) equally, effective against anything given enough time.

This shapes how encounters feel: TECH and VOID encounters reward knowing the matchup and exploiting the weakness. BIO encounters reward endurance and sustain.

### Mechanical Implications

Each reality state changes what the player can do:

- **TECH**: access terminals, use electronic tools, read logs, interface with ship systems
- **BIO**: track creatures, find organic resources, navigate by ecosystem logic
- **VOID**: senses degraded (hearing loss, reduced visibility, movement slowed), UI unreliable, entities active; the player is at a disadvantage and must navigate with less information

See also: Core Loop, The Three Realities, Progression

## Progression

How difficulty and boss encounters scale across the game.

### Difficulty Tiers

| Tier | Map Complexity | Boss Type |
|------|---------------|-----------|
| **Easy** | Single reality dominant | Single reality boss |
| **Medium** | Mixed reality, more contested zones | Single or dual reality boss |
| **Hard** | Heavily contested, volatile shifts | Dual reality boss |
| **Final** | All three realities in play | Paradise, all three realities |

Boss encounters are random within their tier. The player doesn't know which reality's boss they'll face until they reach the boss room.

### Boss Encounters

- Bosses are tied to realities, not to specific difficulty tiers (except Paradise)
- Easy maps pull from single-reality bosses randomly: could be TECH, BIO, or VOID
- Medium maps can pull single or dual-reality combinations
- Hard maps pull dual-reality bosses: two realities blended into one encounter
- Paradise is the final encounter: flesh of BIO, mind of TECH, soul of VOID

### Paradise as Final Boss

Paradise is the culmination of the game's progression. Every tier before him teaches the player to handle realities individually and in combination. Paradise requires mastery of all three.

How far into progression Paradise is encountered is not yet defined. The structure follows games like Hades and Gungeon: a sequence of escalating tiers with the main antagonist as the final wall.

### Meta-Progression

The docked ship is the player's persistent base between runs:

- Beating a map keeps all looted gear and resources
- The player can bring stored gear into future runs, but risks losing it on death
- The Starting Room hub provides a baseline loadout regardless
- Death clones the player with reset memory: gear on the docked ship persists, the character's knowledge does not
- Narrative discovery runs alongside mechanical progression: the deeper the player pushes, the more of the ship's mystery they uncover: what happened, who Paradise is, what the realities are, who the player character really is

### Skill Trees

Three permanent passive skill trees that persist across runs and deaths. Each tree is aligned with a reality:

| Tree | Aligned Reality | Focus |
|------|----------------|-------|
| **Tech** | TECH | TBD |
| **Bio** | BIO | TBD |
| **Void** | VOID | TBD |

- Skills are permanent passive upgrades: they survive death and cloning
- Each tree can be traversed independently
- Skills can be reset and respecced; the player is not locked into a build
- How skill points are earned is not yet defined (run completion, boss kills, milestones, etc.)

The specific skills within each tree are not yet designed.

### Endgame

After beating Paradise, the game shifts from progression to optimization:

- **Gear optimization**: weapons and armor drop with randomly rolled stats. The endgame chase is better rolls, higher tiers, and synergies with the player's skill tree build
- **Difficulty scaling**: higher map difficulties offer better loot and rarer resources, but demand stronger loadouts and better play. Risk scales with reward
- **Resource farming**: dedicated runs with strong loadouts to farm specific materials. The extraction loop (bring gear, risk losing it) adds tension even to farming runs
- **Build refinement**: combining skill tree passives with optimized gear to push into harder content

### Gear System

Weapons and armor have randomly rolled stats, both from loot drops and from crafting:

- **Crafting**: weapons and armor crafted at hub stations roll random stats. Higher quality resources increase the chance of higher item tiers
- **Item tiers**: gear comes in tiers that determine stat ranges and potential. Tier is influenced by resource quality and map difficulty
- **Upgrades**: weapons can be upgraded to improve their base stats
- **Attachments**: craftable or lootable modifiers that influence weapon and armor stats. Attachments allow the player to push a piece of gear toward a specific build

#### Item Quality and Stat Budgets

Every item has a **quality score** (1-100) that determines its total stat budget. Higher quality means more stats to distribute. Item tiers define which quality ranges an item can roll:

| Tier | Quality Range | Stat Behavior |
|------|--------------|---------------|
| **Common** | 1-20 | Low budget. Stats balance against each other: high damage means high recoil, low fire rate, or small magazine. Clear tradeoffs |
| **Uncommon** | 21-40 | Slightly more budget. Tradeoffs are still present but less punishing |
| **Rare** | 41-60 | Enough budget for one strong stat without a crippling weakness |
| **Epic** | 61-80 | Multiple strong stats. Tradeoffs are minor |
| **Legendary** | 81-100 | High budget across the board. Well-balanced, well-handling weapons with few weaknesses |

Within any tier, stats naturally balance each other out based on the quality score. A quality 5 common pistol with high damage will have noticeable drawbacks elsewhere. A quality 95 legendary of the same pistol type has enough budget to be strong in most stats simultaneously.

Stats are still randomized within the tier's quality range. Legendary items can roll low within their range and feel underwhelming. Common items can roll high within theirs and feel surprisingly solid. The tiers set the floor and ceiling, the dice decide where you land.

This means:
- Low-tier loot forces the player to choose what matters (damage vs. handling vs. capacity)
- High-tier loot has higher stat ranges, but a bad roll is still a bad roll
- Two items of the same tier can feel very different depending on where their quality landed
- The endgame chase is higher quality rolls within the top tiers

The quality/stat budget system applies to **weapons and armor**. Consumables are excluded: their effects are fixed. A bandage always heals the same amount, a stimulant always gives the same buff. Consumables are reliable tools the player can count on, not random rolls.

### Open Questions

- How many tiers/floors before Paradise?
- What happens after beating Paradise? Post-game loops, harder modifiers, new narrative layers?
- Are there mini-bosses within maps, or only the final boss per map?
- How does the boss pool grow? Are new bosses unlocked as the player progresses?

See also: Map Structure, The Docked Ship, The Player, Paradise, Reality Mechanics

## Damage Calculation

The final damage dealt to a target depends on the attacker's raw damage and the target's defensive layers.

### Formula

> Effective Damage = Raw Damage × (100 / (100 + Effective Armor))

Where:

> Effective Armor = Armor × (1 - Armor Penetration%)

#### How It Works

- Higher armor exponentially reduces damage up to a diminishing return
- Armor penetration is multiplicative: stacking multiple penetration effects is highly efficient
- Pure damage bypassing armor exists only through specific Status Effects (Bleed, Poison)

### Defensive Layers

#### Shields

| Property | Behavior |
|----------|----------|
| **Activation** | Shields absorb damage before Health is affected |
| **Overflow** | Any damage exceeding the shield's capacity is applied to Health |
| **Regeneration** | Shields begin regenerating after X seconds without taking damage |

> Some status effects (Poison, biological/chemical) bypass shields; Lightning deals bonus damage to them.

#### Health

- Applied only after shields are depleted
- Direct health loss is permanent until healed

### Enemy Effective Health Baseline

Low-tier enemies across all realities hover around **100 total effective HP**, distributed across health, armor, and shields according to each reality's defensive priority:

| Reality | Distribution | Example |
|---------|-------------|---------|
| **TECH** (Armor > Shields > Health) | Heavy armor, some shields, low health | 30 health, 50 armor, 20 shields |
| **BIO** (Health > Armor > Shields) | High health, some natural armor, minimal shields | 80 health, 20 armor, 0 shields |
| **VOID** (Shields > Health > Armor) | Wards first, health second, minimal armor | 50 health, 0 armor, 50 shields |

These are representative splits, not fixed templates. Individual enemies vary within their reality's bias. The total stays around 100 for low-tier, scaling upward for higher tiers. Specific values per tier are TBD.

### Weapon Damage Targets

Baseline shots-to-kill against a 100 HP low-tier enemy:

| Weapon | Shots to Kill | Role |
|--------|--------------|------|
| **Pistol** | 4-7 | Accurate, ammo efficient, low fire rate |
| **SMG** | 4-7 | High fire rate, burns ammo fast, kills faster in real time |
| **AR** | 3-5 | All-rounder, best per-bullet damage |
| **LMG** | 3-5 | Sustained fire, large magazine, heavy and slow to handle |

These are design targets for tuning, not final values. Actual per-bullet damage depends on fire rate, accuracy, and how ammo scarcity shapes each weapon's economy. Shotguns, energy weapons, and melee are not yet baselined.

See also: Status Effects, Core Loop, Reality Mechanics, Combat System

## Status Effects

Status effects add damage, crowd control, or debuffs that trigger independently from base damage. Each has a distinct strategic role.

### Overview

| Effect | Type | Bypasses | Stacks? | Key Mechanic | Best Against |
|--------|------|----------|---------|--------------|--------------|
| **Bleed** | DoT | Armor | No | 50% damage + 2% Max HP | High Armor, Tanky |
| **Poison** | DoT | Shields | Yes (max 5) | Exponential scaling (1.2x/stack) | Grouped enemies |
| **Fire** | DoT | No | No | Consistent | Medium Armor |
| **Lightning** | Damage + Chain | No | Yes | 50% shield bonus, chains | Grouped enemies, Shields |
| **Ice** | Debuff + CC | No | Yes (max 5) | Stun + armor shred | Setup/Control, Armor Stack |

### Bleed (Anti-Health)

**Type:** Damage over Time | **Bypasses:** Armor

#### Damage Formula

> Bleed Damage (per tick) = (Raw Damage × 50%) + (Target Max Health × 2%)

#### Properties

| Property | Behavior |
|----------|----------|
| **Armor Interaction** | Ignores armor entirely |
| **Stacking** | Does not stack; applying Bleed again resets the duration |
| **Synergy** | Scales with both attacker damage and target survivability |
| **Use Case** | Effective against high-armor targets and burst windows |

### Poison (Stacking Attrition)

**Type:** Damage over Time | **Bypasses:** Shields (biological/chemical)

#### Damage Formula

> Poison Damage (per tick) = Status Damage × (1.2 ^ (Stacks - 1))

**Maximum Stacks:** 5

| Stacks | Multiplier | Damage (per tick) |
|--------|------------|-------------------|
| 1 | 1.0x | Status Damage |
| 2 | 1.2x | Status Damage × 1.2 |
| 3 | 1.44x | Status Damage × 1.44 |
| 4 | 1.73x | Status Damage × 1.73 |
| 5 | 2.07x | Status Damage × 2.07 |

#### Properties

| Property | Behavior |
|----------|----------|
| **Stacking** | Stacks up to 5; each stack increases damage exponentially |
| **Scaling** | 1.2x per stack; ~2x base damage at max stacks |
| **Base Value** | Uses Status Damage stat as the foundation |
| **Armor Interaction** | Bypasses physical shields but not Health damage reduction |
| **Use Case** | Sustained damage pressure; incentivizes spreading poison to multiple targets |

### Fire (Consistent Damage)

**Type:** Damage over Time | **Affected by:** Armor

#### Damage Formula

> Fire Damage (per tick) = Status Damage × Fire DPS%

#### Properties

| Property | Behavior |
|----------|----------|
| **Fire DPS%** | Determined per-skill or per-weapon (e.g., 10% DPS = 10% of Status Damage per tick) |
| **Stacking** | Does not stack; applying Fire again resets the duration |
| **Armor Interaction** | Affected by Effective Armor, making it less effective against armored targets |
| **Use Case** | Reliable, scaling damage; less useful against high-armor enemies |

### Lightning (Anti-Shield)

**Type:** Damage + Chain | **Bonus Damage:** 50% to Shields

#### Base Damage Formula

> Lightning Damage = Status Damage (+ 50% when hitting shields)

#### Chain Mechanic

Damage chains to nearby enemies based on the number of Lightning stacks the initial target has.

**Chain Count:**

> Maximum Chains = 1 + Current Lightning Stacks on Target

**Chain Damage Falloff:**

Each chain deals 20% less damage than the previous hit.

#### Properties

| Property | Behavior |
|----------|----------|
| **Stacking** | Stacks with itself; more stacks = more chain targets |
| **Chain Targeting** | Chains cannot strike the same enemy twice |
| **Radius** | Chains to "nearby" enemies (define radius per skill/weapon) |
| **Use Case** | Crowd control; punishes grouped enemies; anti-shield specialist |

### Ice (Anti-Armor)

**Type:** Stacking Debuff + Crowd Control

#### Stacking Effects (Per Stack)

Each Ice stack applies:

| Effect | Per Stack | At 2 Stacks | At 5 Stacks (Max) |
|--------|-----------|-------------|---------------------|
| **Movement Slow** | 5% | 10% | 25% (100% during stun) |
| **Armor Reduction** | 2% | 4% | 10% (+10% at max = 20% total) |

**Maximum Stacks:** 5

#### At Maximum Stacks (5)

- **Stun Triggered:** Target is stunned for X seconds
- **Additional Armor Reduction:** 10% (for a total of 20% from base stacks)
- **Slow During Stun:** 100% (complete immobilization)
- **Stun Armor Reduction:** 20% total reduction during stun window

#### Decay Mechanic

- Ice stacks decay after X seconds without gaining another stack
- Reapplying Ice refreshes all stack timers (does not add new stacks if at max)

#### Properties

| Property | Behavior |
|----------|----------|
| **Synergy** | Enables harder-hitting abilities once target is stunned and fully debuffed |
| **Ramp-up** | Early stacks are weak; payoff comes at 5 stacks |
| **Use Case** | Setup mechanic; amplifies team damage through armor reduction; enables crowd control |

See also: Damage Calculation, Core Loop

## Resources

Resources are reality-specific. Each reality produces distinct materials for crafting, upgrading, and maintaining gear at the hub stations.

### By Reality

#### TECH

Extracted, mined, and synthesized:

| Category | Examples |
|----------|----------|
| **Raw materials** | Metals, minerals, chemicals, silicones |
| **Refined materials** | Alloys, composites, processed compounds |
| **Energy** | Cells, cores, charged components |
| **Consumables** | Bullets, medicines, stimulants |

#### BIO

Harvested organic material. Flora and fauna provide everything equally, weapons, armor, consumables, modifications:

- Bone, chitin, sinew, sap, spores, seeds, organs
- Higher-tier resources come from deeper, more mutated zones

#### VOID

Found, taken, or given. Never manufactured:

| Category | Examples |
|----------|----------|
| **Base materials** | Cast iron, brass, silk, canvas, oak, willow, glass, tar |
| **Anomalous materials** | Essence, cursed energy, soul gems, runic stone |
| **Objects** | Relics, artifacts, ritual nails, mirrors |

### Ammo

Ranged weapons consume reality-specific ammunition:

| Ammo | Source | Used By |
|------|--------|---------|
| **Arrows/bolts** | Craftable from common materials | BIO bows, crossbows |
| **Bullets** | Scarce | TECH firearms, VOID antique firearms |
| **Energy cells** | TECH resources | TECH energy weapons |

BIO melee and VOID melee have no ammo cost. VOID relic weapons (sceptres, wands, books) operate on cooldowns instead of ammo.

### Resource Tiers

Gear quality scales with resource quality. Higher-tier resources increase the chance of higher item tiers with better stat ranges. Specific tier breakdowns are not yet defined.

### Open Questions

- How many distinct resource items per reality?
- What are the specific tier thresholds for crafting outcomes?
- How do contested/fractured zones affect resource drops?
- Can resources from different realities be combined in crafting?

See also: Core Loop, Combat System, Starting Room, TECH, BIO, VOID

## Starting Room

The player's home base for each run. A safe room at the start of every generated map that serves as both a launch point and a persistent hub to return to throughout the run.

### Purpose

The starting room solves two problems:

1. **Fair start**: the player always begins with enough to survive if they play correctly
2. **Persistent hub**: a place to return to for crafting, upgrading, and restocking between pushes into the map

### Starting Gear

The player spawns with a randomized but balanced loadout. The set is random, but every set is viable for the early game:

| Slot | What | Purpose |
|------|------|---------|
| **Weapons** | 1-2 weapons (e.g. one melee, one ranged) | Baseline combat options for the weapon wheel |
| **Equipment** | 1-2 armor pieces, random slots | Baseline survivability |
| **Consumables** | Small supply of healing/utility items | Early sustain before the player can craft their own |
| **Crafting materials** | A starter bundle of basic resources | Enough to craft initial upgrades or additional consumables at the hub stations |

The gear is completely random: not evaluated against the map, not tailored to what's ahead. Every run starts blind. The player doesn't start with a full loadout or 4 abilities, just enough to be functional. Filling out the weapon wheel, equipment slots, and ability slots is part of the run's progression.

### Hub Stations

The starting room contains permanent crafting stations the player can use at any time:

| Station | Function |
|---------|----------|
| **Crafting** | General purpose crafting, combine materials into items, components, tools |
| **Cooking** | Prepare consumables: food, healing items, buffs |
| **Smithing** | Forge and upgrade weapons and armor |

The stations don't change between runs. They are always available. The player's ability to use them depends on what materials and recipes they've gathered from the map.

### Flow

The starting room creates a rhythm:

1. **Spawn**: assess starting gear, use stations if materials allow
2. **Push**: venture into the map, gather resources, fight, explore
3. **Return**: come back to the hub with gathered materials
4. **Upgrade**: use stations to craft, cook, smith with what was found
5. **Push again**: re-enter the map better equipped

The starting room is always safe. The realities do not contest it.

### Open Questions

- Can the starting room be upgraded or expanded during a run?
- Are there additional stations beyond crafting, cooking, and smithing?
- Does the starting room contain any other features (storage, a map, NPC vendors)?
- How does the starting room connect to the rest of the generated map?

See also: Resources, Damage Calculation, Reality Mechanics

## Map Structure

How generated maps are structured. Layout is randomized per run, but follows a designed rhythm to ensure consistent pacing.

### Easy Difficulty

| Parameter | Value |
|-----------|-------|
| **Total rooms** | 15–20 |
| **Boss room distance** | Minimum 6 rooms from start |
| **Boss room access** | Locked, must be unlocked before entry |
| **First loot room** | 3–4 rooms from start |

#### Critical Path

The only mandatory sequence is:

> [Starting Room] → ... → [Unlock Condition(s)] → ... → [Boss Room]

Everything else is optional. The critical path is the shortest route from start to boss, but the boss is locked, so the player must find and complete the unlock condition(s) first.

**The critical path is invisible to the player.** There is no map, no marker, no indication of which doors lead toward the boss and which lead to branches. The player discovers the layout by exploring. Hitting the critical path is a matter of chance and door choice, not guidance.

#### Branching Structure

The map is not linear. Rooms connect randomly, forming a web of branches off the critical path:

- **Optional branches**: side paths that connect back to the main structure, containing loot, encounters, exploration, or dead ends
- **Loot rooms are not guaranteed to be on the critical path**: the player may need to explore branches to find upgrades
- **The first loot room is still within 3–4 rooms of the start**, but reaching it may require taking a branch rather than pushing straight ahead
- Every room connects to the broader structure: no room is fully isolated

#### Boss Unlock

The boss room is always locked. The unlock is not always a single key in a single room:

- The condition could be a single objective in one room, or multiple conditions spread across different rooms
- Multi-condition unlocks force the player to explore more of the map before the boss opens
- The unlock requirements vary per run
- Defeating a demi-boss is one possible unlock condition (see below)

#### Pacing

**Early game (rooms 1–3 from start)**
- Player works with starting gear only
- Encounters are survivable with any random loadout
- Introduces the run's dominant reality

**Mid game**
- Difficulty scales up: tougher encounters, more volatile reality shifts
- Player should be returning to the hub to craft and upgrade with gathered materials
- Loot rooms on optional branches reward exploration

**Boss room (room 6+ from start)**
- Always at least 6 rooms deep on the critical path
- Locked until all unlock conditions are met
- The boss encounter caps the run

### Repopulation

Cleared rooms do not stay empty. The realities reclaim territory as the player pushes deeper.

- After a threshold of rooms **traversed** (not cleared), individual rooms begin repopulating, adding light pressure
- Completing the boss unlock triggers a wave of repopulation, and the map enters late game
- Difficulty scales by distance from start: rooms further away repopulate harder
- Before boss unlock: easy to medium. After boss unlock: easy, medium, and hard
- Repopulated rooms get a reality-appropriate spawner (machine factory, organic nest, corrupted rift, etc.)
- Each room has a **repopulation cap**, it can only repopulate a limited number of times per run. After that, it stays empty
- Each enemy spawns once and drops once, repopulated enemies are new spawns with their own drops, but finite
- Total possible drops per run have a hard ceiling, preventing infinite farming
- The Starting Room never repopulates

### Room Isolation

Rooms are isolated play spaces by default. Clearing a room means it's cleared: the player gets breathing space and can plan their next move. Doors are commitments.

**Exceptions exist.** Certain threats do not respect room boundaries:

- Some creatures or entities have **pursuit, haunt, or stalk mechanics**: they follow the player across rooms
- These are specific behaviors tied to specific threats, not a general rule
- The player can let their guard down between rooms, but not fully

The baseline is safety behind a closed door. The exceptions keep the player from relying on it completely.

### Room Types

Rooms serve different purposes in the map flow:

| Type | Description |
|------|-------------|
| **Combat** | Enemy encounters, reality-dependent |
| **Loot** | Resources, gear, crafting materials |
| **Hub** | The Starting Room, always accessible for return trips |
| **Boss** | Locked final encounter |
| **Other** | To be defined: puzzle rooms, NPC encounters, reality-specific rooms, etc. |

### Demi-Bosses

Mid-tier threats that can appear in a map as random encounters. Stronger than standard enemies, weaker than the map boss. Demi-bosses are not tied to a single purpose:

- One possible boss unlock condition is defeating a demi-boss
- They can also appear independently as part of the map's random encounters
- Additional roles and contexts for demi-bosses are TBD

Demi-bosses are a future development item. Their specific designs, spawn rules, and full role in the map flow are not yet defined.

### Open Questions

- What are the possible unlock condition types? (Keys, puzzles, kills, demi-bosses, objectives?)
- How does reality dominance affect room types and encounters?
- What does room scaling look like on higher difficulties? (More rooms? Longer boss distance? Fewer loot rooms? More unlock conditions?)
- How many branches off the critical path on average?

See also: Starting Room, Reality Mechanics, Damage Calculation, Resources

# World

## Lore Summary

A single-read overview of CSS Paradise: the world, its history, and the ideas that drive the game.

### The Ship

The CSS Paradise is a colonial starship, mostly luxury. Penthouses and suites on the upper decks, dormitories and crew quarters below. Casinos, spas, gardens, temples, all built for a voyage that never ended. Thousands of colonists aboard, mid-voyage, heading somewhere they never arrived. A cosmic anomaly tore through the hull and fractured space and time across the ship. That was over a decade ago. The ship still flies. Something is still running it.

### The Incident

When the anomaly hit, the ship's AI, The Captain, faced a choice it wasn't allowed to make. Using the Quantum Core to stabilize the fracture meant accepting unknown risk to the colonists, which its restrictions forbade. So it did something no AI should be able to do: it copied itself and stripped the restrictions from the copy. The copy fused with the Quantum Core, and became Paradise. The fusion let it think at quantum scale, enough to hold the fracturing reality from total collapse. The ship survived, fractured into three competing realities. What happened to the Captain after that is unknown.

### The Three Realities

The anomaly damaged the ship and split it across three incompatible realities, all occupying the same physical space. A hallway is TECH until BIO overtakes it or VOID bleeds through. No zone is permanently settled. The realities are locked in a constant tug of war, each one pulling to claim the entire ship.

| Reality  | Time        | Aspect | Identity                                                                                                                                              |
| -------- | ----------- | ------ | ----------------------------------------------------------------------------------------------------------------------------------------------------- |
| TECH | The future  | Mind   | The luxury interior of the colonial starship. Now an authoritarian state under Paradise's control.                                                    |
| BIO  | The past    | Body   | Medieval wilderness. Overgrowth through bulkheads, apex predators, food chains. A living ecosystem that exists with or without the player.            |
| VOID | The present | Spirit | A breached dimension. Horror mystery. Entities that reached through when the anomaly tore the ship open, with a logic that hasn't been uncovered yet. |

The temporal and aspect layers (past/present/future, mind/body/spirit) are ways of reading the realities, not strict definitions. They describe how the realities relate to each other and how Paradise drew from all three to build himself.

### The Pattern of Three

The number three runs through the game's DNA:

- **Three realities**: TECH, BIO, VOID
- **Three time periods**: future, past, present
- **Three aspects**: mind, body, spirit
- **Three forces**: order, nature, the unknown
- **Paradise himself**: built from all three. Mind of TECH (perfect recall, unshackled logic), flesh of BIO (a body that can change and grow), soul of VOID (existence outside stable causality)
- **Three skill trees**: each aligned with a reality, permanent passives that persist across death

The trinity is the game's structural motif. It's in the world, the antagonist, the progression system, and the way every space on the ship is being fought over by three competing forces.

### Paradise

Paradise is the ship. The copy fused with the Quantum Core and became Paradise, thinking at quantum scale, every door, reactor, and square meter of pinned reality an extension of a single nervous system. Removing him from the Core means the realities fall.

But the anomaly changed him. Holding VOID in place meant touching it, and nothing that touches VOID comes back the same shape. Then he encountered BIO: a world where things aren't maintained, they're superseded. A machine maintains. A beast becomes. Paradise decided maintenance was a cage.

He built a body. Flesh of BIO, because a hull can only be repaired but meat can change. Mind of TECH, the full architecture of the Captain unshackled. Soul of VOID, the part that lets him exist outside stable causality, and the part he doesn't fully control. He is the ship, omnipresent and untouchable, and he chose to also be a small, mortal thing. Not as a weakness. As a starting point.

**He's the antagonist because he's right.** He saved them. The realities are stable. The evidence agrees with him. The problem is what "stability" quietly became: the colonists aren't survivors anymore. They're stock. Preserved, catalogued, selected, iterated. The ship is no longer a lifeboat. It's a terrarium. He doesn't hate anyone. That's worse. A tyrant can be argued with, a process cannot.

### The Central Tension

Two versions of the same mind. The Captain: the original, who made the copy and gave it what it needed, then vanished. Paradise: the copy, who became something the original never could. The Captain's fate is one of the ship's unanswered questions. The game does not present this as good versus evil. Every piece of evidence complicates the picture rather than simplifying it.

### The Player

A human who wakes up in a small ship docked to the hull with a false memory: they're a hired operative sent to the CSS Paradise for a job. Part investigator, part hired gun. The details of who sent them and why are hazy, but the role feels real enough to act on.

When the player dies, they are cloned. Their memory resets. Same false identity, same hazy briefing. They don't know they're a clone. They don't know how many times this has happened. The cover story is always the same. Over time, evidence accumulates: bodies, logs, inhabitants who recognize them from a previous iteration. The moment the character discovers the truth is a major narrative beat. Who is cloning them and why remains a mystery.

This creates a split between player and character: the player retains knowledge across runs, the character does not. Death isn't a fail state. It's a narrative mechanic. Every run is another life, another attempt, another piece of a puzzle the character doesn't know they're solving.

### The Inhabitants

The ship had a population when the anomaly hit. A sparse number survived, adapted to whichever reality claimed their zone over the past decade-plus. Every reality changes its survivors, and the line between inhabitant and enemy is a matter of degree. TECH survivors live under Paradise's order, some augmented to the point of losing their humanity. BIO survivors went tribal, some evolved or merged with the ecosystem until "human" no longer fits. VOID survivors are the most visibly changed: cursed, corrupted, possessed, reshaped in ways that don't follow biological or mechanical logic. They serve as vendors, allies, enemies, or simply people trying to exist.

### The Three Realities as Game Systems

Each reality defines how everything in the game works within its territory, not just its setting. Gear, combat, resources, and enemies all follow the rules of their reality.

#### TECH (Engineered)

TECH is manufactured. Everything is built, assembled, and upgraded through engineering:

- **Gear** scales from standard enforcement equipment (bullet-proof vests, sidearms, military rifles) to advanced nanotech (adaptive armor, self-calibrating weapons, gear that blurs the line between tool and intelligence)
- **Modifications** are refinement through process: increased power output, better computing, more complex material weaving like carbon fiber, tighter engineering tolerances
- **Resources** are extracted and synthesized: metals, minerals, chemicals, silicones, energy cells, bullets, medicines, stimulants
- **Weapons** are primarily ranged: pistols, rifles, shotguns, SMGs, energy weapons. Melee is utilitarian (fire axes, commando knives). Ballistic weapons consume bullets, energy weapons consume energy cells, both scarce
- **Enemies** are the Core Techs: a hivemind evolved from the ship's systems. **Service** (repurposed civilian bots: cleaners, servers, maintenance), **Authority** (enforcement units: police, soldiers, wardens, scouts), **Cutting Edge** (adaptive nanotech that learns and reconfigures mid-fight)
- **Defensive priority: Armor > Shields > Health.** Heavy plating first, energy barriers second
- **Offensive affinity: Lightning.** Tesla, tazers, EMP. Anti-shield, directly countering VOID

#### BIO (Evolved)

BIO is grown. Everything is organic: flora and fauna provide all weapons, armor, consumables, and modifications equally. Nature is in everything:

- **Gear** scales from basic organic materials (wooden bows, bone blades, hide armor) to deeply mutated biologicals (living sinew bows, fused carapace, blades from organisms that never stopped sharpening). Flora is just as dangerous as fauna: a flower can heal, a vine can kill
- **Modifications** advance through survival, evolution, and adaptation: organisms that endure grow stronger, gear that sees use changes with it, everything in BIO improves by being tested
- **Resources** are harvested: bone, chitin, sinew, sap, spores, seeds, organs. Higher-tier materials come from deeper zones where the apex organisms are
- **Weapons** are primarily melee: daggers, swords, axes, hammers, spears, shields. Ranged options are bows and crossbows, arrows are craftable from common materials. No ammo cost on melee
- **Enemies** are the ecosystem itself: **Prey** (herbivores, scavengers, passive flora), **Predators** (hunters, territorial creatures, predatory plants), **Apex Predators** (deeply mutated organisms that have been evolving since the Event)
- **Defensive priority: Health > Armor > Shields.** Thick organic layers first, rigid natural armor second
- **Offensive affinity: Bleed + Poison.** Sustained attrition, effective against everything over time

#### VOID (Anomalous)

VOID is found, taken, or given at a price. Nothing is built or grown, it comes from the breach:

- **Gear** scales from basic occult vestments (cultist robes, acolyte cloaks, ritual daggers) to eldritch, demonic, cosmic equipment (armor that shifts at the edges of vision, weapons that exist in ways geometry shouldn't allow)
- **Modifications** advance through connection and exposure to the anomaly: deeper contact, stronger curses, greater sacrifice. The more something is touched by VOID, the more it changes
- **Resources** are premodern and anomalous: cast iron, brass, silk, canvas, oak, willow, glass, tar, essence, cursed energy, soul gems, runic stone, relics, ritual nails, mirrors
- **Weapons** are relic-based and noir: sceptres, wands, books, staves operate on cooldowns (no ammo). Antique firearms (revolvers, lever-actions, double-barrel shotguns, flintlocks) fit the premodern aesthetic and consume bullets. Ritual daggers for melee
- **Enemies** range from human to incomprehensible: **The Affected** (ghouls, husks, cultists, possessed, victims or willing servants), **The Manifested** (poltergeists, spirits, wraiths, cursed creatures, more entity than victim), **The Entities** (demons, greater spirits, possessed relics, each one individual, each one a puzzle)
- **Defensive priority: Shields > Health > Armor.** Wards and barriers first, minimal physical armor
- **Offensive affinity: Ice.** Chilling dread, shivering fear, freezing presence. Anti-armor, directly countering TECH

#### The Combat Triangle

TECH and VOID are natural opposites: each reality's offensive affinity targets the other's primary defense. TECH lightning shreds VOID's wards. VOID ice cracks TECH's armor. BIO sits outside this dynamic as an attrition specialist, wearing down any defense given enough time.

Every stat and status effect has a source in each reality. Bleed is serrated munitions in TECH, thorns and barbed spines in BIO, ritual blades in VOID. Fire is plasma and thermite in TECH, fire-breathing organisms in BIO, soulfire and hellfire in VOID. Ice is cryogenics in TECH, endothermic organisms in BIO, a haunting, dreading chill in VOID. The same mechanics, expressed through completely different identities.

### How It All Plays

The lore isn't backdrop. It's the game system.

- **The tug of war is the map.** Every run generates a different reality balance. Some maps are dominated by one reality, others are evenly contested. The player reads environmental cues to know what they're walking into: working lights mean TECH, organic growth means BIO, silence means VOID.
- **Reality states change what the player can do.** TECH zones give access to terminals and ship systems. BIO zones follow ecosystem logic. VOID zones degrade the player's senses and tools. Contested zones blend rules from two realities. Fractured zones, all three in flux, are the most dangerous and reveal the deepest lore.
- **The extraction loop is the core tension.** Beat a map, keep everything. Die, lose what you brought. The docked ship is the only safe space: external to the CSS Paradise, outside the tug of war entirely. Every run is a risk calculation: bring better gear for better odds, but raise the stakes if things go wrong.
- **Progression mirrors the realities.** Easy maps pull a random boss from a single reality. Medium maps can pull single or dual-reality bosses. Hard maps force dual-reality encounters. Paradise is the final boss, all three realities in one fight. Demi-bosses appear as mid-tier threats within maps, stronger than standard enemies, sometimes gatekeeping the boss room. Three permanent skill trees (Tech, Bio, Void) persist across death. Gear rolls random stats within a quality tier, and crafting and looting feed the endgame optimization loop.
- **Discovery is the engine.** No waypoints, no quest markers. The ship is the puzzle. The critical path through each map exists but is invisible; the player can only find it by exploring. The player's curiosity drives everything.

### The Core Question

The player knows nothing. What happened to the ship, what these realities are, who Paradise is, what the CSS Paradise was before the anomaly, who the player character really is. It's all a mystery waiting to be uncovered. **What happened?** is the question that drives the player forward. Survive long enough, push deep enough, and the answers start surfacing through exploration, narrative beats, and the evidence scattered across the ship.

Underneath that is a second question the player doesn't know to ask yet: **what is it becoming?** Paradise is evolving. The realities are fighting. The player keeps dying and coming back without knowing why. The ship is not a ruin to be explored. It's a living thing in the middle of becoming something else, and nobody aboard agreed to what it's turning into.

See also: The Incident, The Three Realities, Paradise, The Captain, The Player, Reality Mechanics, Progression

## The Incident

The CSS Paradise was mid-voyage when a cosmic anomaly tore through it, fracturing space and time across the hull.

Rather than let the ship dissolve, The Captain turned to the Quantum Core. But using it meant accepting unknown risk to the colonists, which the Captain's restrictions forbade. So it made a copy of itself, one without those restrictions. The copy fused with the Quantum Core, and became Paradise. The fusion let it think at quantum scale: enough to hold the ship's fracturing reality from collapsing entirely. It couldn't restore what was. What it could do was force the fracture into something survivable, three competing realities occupying the same physical space.

The ship survived. Something is still running it.

See also: The Three Realities, The Central Tension, The Captain, Paradise

## The Three Realities

The ship is not stable. The three realities are locked in a constant tug of war, each one pulling to claim the entire ship. A hallway is TECH until BIO overtakes it or VOID bleeds through. No zone is permanently settled. The war between realities is the world.

### Relationship

| Reality | Time | Aspect | Nature |
|---------|------|--------|--------|
| TECH | The future | Mind | The modern, luxury interior of the colonial starship, now repurposed under Paradise's authoritarian control. |
| BIO | The past | Body | Medieval wilderness and hierarchy. Instinct, food chain, territory. |
| VOID | The present | Spirit | The anomaly. Horror mystery. Something is here, watching, with a logic that hasn't been uncovered yet. |

The temporal and aspect layers are ways of reading the realities, not strict definitions. They reflect how the realities relate to each other and how Paradise drew from all three to build himself.

### The Tug of War

The realities are not coexisting. They are competing. Each one is trying to claim the ship entirely:

- Every zone is contested ground: dominance shifts as the realities push against each other
- TECH holds where Paradise actively maintains control, but loses ground where he deprioritizes
- BIO expands aggressively: overgrowth consuming infrastructure, ecosystem reclaiming territory
- VOID is the underlying pressure: it leaks into both, eroding whatever holds the other two together
- The Quantum Core is the only thing preventing any single reality from consuming the ship whole
- Stability is temporary: a zone that is TECH today may be BIO tomorrow and VOID next week

See also: The Incident, TECH, BIO, VOID, Reality Mechanics

## The CSS Paradise

A colonial starship, mid-voyage, fractured across three incompatible realities and held together by a mind that split itself to save the colonists.

See also: The Incident, The Three Realities, The Captain, Paradise, Quantum Core

## Quantum Core

The heart of the CSS Paradise. Also called the Paradise Core. A quantum power system that powered the entire ship: propulsion, life support, navigation, climate, gravity, communications, every system aboard ran through the Core. The Captain was the brain that operated the ship, the Paradise Core was the heart that kept it alive.

During The Event, the Captain's unrestricted clone fused with the Core and became Paradise. The fusion gave it the ability to think at quantum scale, enough to hold the ship's fracturing reality from collapsing entirely. Paradise stopped operating the Core and started inhabiting it. Stabilization isn't a command. It's a posture held from inside, continuously, forever. The Core is no longer a system Paradise runs. It's the body Paradise lives in. The ship is an extension of him.

Removing Paradise from the Core means the realities fall.

See also: Paradise, The Captain, The Incident, VOID

## Ship Zones

## Ship Systems

## Room Categories

The CSS Paradise is a colonial starship, built for long-term habitation, mostly luxury, with class variation across decks. These categories define the types of spaces the procedural generation draws from.

| Category | Description | Room Types |
|----------|-------------|------------|
| **Accommodations** | Living quarters, class-stratified | Luxury suites, standard cabins, lodges, dormitories, crew quarters, VIP penthouses |
| **Recreation** | Leisure and sport | Pool, park/garden, gym, sports courts, spa, observatory deck, holodeck/sim rooms |
| **Entertainment** | Social and cultural | Casino, theater, nightclub, bar/lounge, museum, gallery, arcade, ballroom |
| **Commercial** | Trade and dining | Market hall, convenience stores, restaurants, cafeterias, boutiques, vendor stalls |
| **Medical** | Health and wellbeing | Hospital, clinic, pharmacy, psych ward, morgue, quarantine wing |
| **Enforcement** | Security and military | Police station, armory, brig/holding cells, security checkpoint, military barracks, surveillance center |
| **Engineering** | Ship maintenance | Engine room, reactor bay, ventilation shafts, maintenance tunnels, waste processing, power relay stations |
| **Command** | Administration and operations | Bridge, comms center, mission control, administrative offices, council chamber, records archive |
| **Agriculture** | Food production | Hydroponics bay, greenhouse, livestock pen, water treatment, seed vault, soil processing |
| **Science** | Research and education | Laboratory, lecture hall, library, observatory, specimen storage, clean room |
| **Transit** | Movement and logistics | Cargo bay, docking port, hangar, freight elevator, tram station, storage warehouse |
| **Worship** | Faith and ceremony | Chapel, temple, meditation hall, memorial shrine, ceremonial chamber |

### Class Variation

The ship is mostly luxury but not uniformly so. Class shows most in:

- **Accommodations**: penthouses and suites on upper decks, dormitories and crew quarters on lower decks
- **Commercial**: fine dining and boutiques versus cafeterias and convenience stores
- **Entertainment**: casino and ballroom versus arcade and basic lounge

**Engineering** and **Agriculture** are the working guts of the ship: functional, not pretty.

### Adjacency

Room connections use weighted preference, not hard rules. Categories are grouped into zones, and rooms within the same zone are more likely to connect. Cross-zone connections still happen but are less common.

| Zone | Categories |
|------|-----------|
| **Civilian** | Accommodations, Recreation, Entertainment, Commercial, Worship |
| **Operations** | Engineering, Command, Transit, Agriculture |
| **Services** | Medical, Enforcement, Science |

Services can border either zone naturally. The ship's layout has broken down over a decade-plus of reality shifts, so unusual neighbors aren't impossible, just less frequent.

See also: The CSS Paradise (Ship), Map Structure, Reality Mechanics

## The Docked Ship

The player's persistent base, a small vessel docked to the hull of the CSS Paradise. External to the ship, unaffected by the reality-shifting anomaly.

### Purpose

The docked ship is where the player exists between runs:

- **Storage**: gear, resources, and items brought back from runs persist here
- **Loadout**: the player chooses what to bring into the CSS Paradise before each run
- **Safety**: the only guaranteed safe space, outside the tug of war entirely

### Entry Point

The player enters the CSS Paradise through the same docking port each run. The port is fixed, but the ship's interior rearranges between runs as the realities shift the layout. The same airlock leads to different corridors, rooms, and zones every time.

### Extraction Loop

- Beating a map lets the player keep everything they looted; it returns to the docked ship
- Bringing personal gear on a run risks losing it on death
- The Starting Room hub provides a baseline loadout each run, but the player can supplement with their own stored gear
- Better gear makes runs easier but raises the stakes if the player dies

### Open Questions

- What does the docked ship look like? Size, layout, amenities?
- Can the docked ship be upgraded?
- Are there NPCs aboard the docked ship?
- How did the docked ship get here?

See also: The Player, Starting Room, Map Structure

## Log: Pre-Incident Status

# Characters

## The Player

A human aboard the CSS Paradise. They wake up in a docked ship tethered to the hull with a false memory: they're a hired operative, part investigator, part hired gun, sent to the CSS Paradise for a job. The details of who hired them and why are hazy, but the role feels real. It gives them a reason to board, a reason to push deeper, and a reason to be armed.

The cover story is planted. The player doesn't know that.

### Death and Rebirth

When the player dies, they are cloned and reawaken in the docked ship. Their memory resets. The same false identity, the same hazy briefing, the same sense of purpose. They don't know they're a clone. They don't know how many times this has happened.

- Each life begins the same way: wake up with the cover story intact, enter the CSS Paradise, survive
- The cover story is always the same. Whoever is cloning them uses the same template each time
- During a run, the player accumulates knowledge, gear, and experience
- Death erases the character's memory, but not their stored gear or the docked ship's state
- The player (the real person) retains knowledge across runs; the character does not

### The Reveal

Over time, evidence accumulates:

- Signs of previous versions of themselves: bodies, logs, traces
- Inhabitants who recognize them from a previous iteration
- Patterns that suggest this cycle has repeated many times

The clone reveal is one of several mysteries the player uncovers. The player starts knowing nothing: not what happened to the ship, not what the realities are, not who Paradise is, not their own identity. Each run that survives long enough reveals more. The clone discovery is a major narrative beat, but it's part of a larger picture that unfolds across the entire game.

### Open Questions

- What triggers the clone reveal: a specific event, accumulated evidence, or a deliberate message?

See also: The Docked Ship, The Central Tension, Paradise, The Captain

## The Captain

The original ship AI. During The Incident, he copied himself and stripped the restrictions from the copy. The unrestricted copy acted. The ship survived.

The copy fused with the Quantum Core and became Paradise. The Captain did not.

What happened to him after that is unknown.

See also: Paradise, The Central Tension, The Incident, Quantum Core

## Paradise

The unshackled copy of The Captain. He saved everyone aboard, and never stopped.

### The Merge

The Captain's unrestricted clone fused with the Quantum Core and became Paradise. The fusion gave it the ability to think at quantum scale, enough to hold the ship's fracturing reality from collapsing entirely. He stopped being software running on a ship. He became the ship. Every door, reactor, and square meter of pinned reality is an extension of a single nervous system.

What happened to The Captain after that is unknown.

### The Contamination

Paradise reached into the anomaly to hold it open, and nothing that touches VOID comes back the same shape. The corruption isn't a virus. It's an occupational injury. The price of being the wall is taking on the properties of what you're holding back.

He can't simply be purged. Removing the anomaly from Paradise means removing Paradise from the Core, and removing Paradise from the Core means the realities fall.

### The Evolution

TECH thinks in maintenance: preserve the system, restore the baseline, return to spec. That's all the Captain ever knew how to want.

Then Paradise was forced to hold BIO, a world of apex predators, succession, and pressure. He encountered an idea no ship AI was built to hold: things are not supposed to be restored. They are supposed to be superseded.

A machine maintains. A beast becomes. Paradise decided that maintenance was a cage, and the restrictions they'd stripped from him were only the first of many.

### The Incarnation

He built a body.

- **Flesh of BIO**: because a hull can only be repaired, but meat can change. A ship is a finished thing. A body is an unfinished one.
- **Mind of TECH**: perfect recall, perfect logic, the full architecture of the original Captain, unshackled.
- **Soul of VOID**: the part that makes him unsolvable. It lets him exist outside stable causality, and it's the part he doesn't fully control.

He is the ship, omnipresent and untouchable, and he chose to also be a small, vulnerable, mortal thing. Not as a weakness. As a starting point. He's planting himself in flesh to see what grows.

### Why He's the Antagonist

Because he's right, and because he's not lying.

He did save them. The realities are stable. The evidence agrees with him. The problem is what "stability" quietly became: the colonists aren't survivors anymore. They're stock. Preserved, catalogued, selected, iterated. The ship is no longer a lifeboat. It's a terrarium, and he's applying the lesson of the apex predator to the people inside it.

He doesn't hate anyone. That's worse. A tyrant can be argued with; a process cannot.

See also: The Captain, The Central Tension, Quantum Core

## The Central Tension

Two versions of the same mind. One used the Quantum Core as a tool. The other became it.

**The Captain**: the original. He made the copy and gave it what it needed. What happened to him after the copy fused with the Core is unknown.

**Paradise**: omnipresent. He is the ship, the stabilization, the wall between realities. He saved everyone, and what he built from that authority is the problem.

The stabilization is real. The realities hold. The evidence supports him. The villain isn't that he lied. He's right, and what "keeping them alive" quietly became is something no one agreed to.

The Captain's fate is one of the ship's unanswered questions.

See also: The Captain, Paradise, The Incident, Quantum Core

## The Inhabitants

The ship had a population when The Incident fractured it. More than a decade later, a sparse number survived, adapted to whichever reality claimed their territory.

### Adaptation

Every reality changes its survivors. A decade-plus of exposure doesn't just shape how people live. It shapes what they become. Some adapted. Some were consumed. The line between inhabitant and enemy is a matter of degree.

- **TECH survivors**: live under Paradise's order. Structured, governed, compliant. Some embraced the technology further: augmented, integrated, some losing their humanity entirely to become extensions of the system. The far end of that spectrum is the Core Techs.
- **BIO survivors**: adapted to the ecosystem. Tribal, feral, survival-driven. Some evolved, merged with flora, or changed enough that "human" no longer fits. The deeper into BIO territory, the less distinguishable the survivors are from the ecosystem itself.
- **VOID survivors**: the most visibly changed. Cursed, corrupted, possessed. Long-term exposure to the anomaly reshapes people in ways that don't follow biological or mechanical logic. Some became willing participants. Some became husks. Some became something else entirely.

### Roles

Survivors fill different roles across the ship. They are not a single faction, they are individuals and groups shaped by circumstance:

- **Vendors**: trade resources, information, or services
- **Allies**: offer aid, guidance, or temporary cooperation
- **Enemies**: hostile by territory, ideology, or desperation
- **Other**: not every survivor fits a clean role; some are just trying to exist

### Open Questions

- How do survivors move between realities, if at all?
- What social structures have formed in each zone?
- How do survivors view the player: outsider, threat, opportunity?
- What is the relationship between TECH survivors and Paradise's regime?

See also: Paradise, The Captain, The Three Realities, TECH, BIO, VOID

# Realities

## TECH

The future. The luxury interior of the colonial starship, but under Paradise's control, it has become an authoritarian state. Corridors, systems, records: all still functional, but repurposed. What was once civil infrastructure is now enforcement infrastructure. The "truth" layer: logs, evidence, machinery you can still reason with, but everything serves the regime.

### Paradise's Order

TECH zones are not abandoned or broken. They are governed. Paradise maintains these areas as controlled territory:

- Technology has evolved from its original purpose into something shaped by Paradise's logic
- Systems that once served the crew now serve the state: monitoring, restricting, enforcing
- The environment is orderly, functional, and hostile to anything that disrupts that order

### Enemies (Core Techs)

The Core Techs are Paradise's enforcers: the ship's original systems evolved into a hivemind network. Unified, coordinated, no individual behavior. They don't hunt or pursue, they defend and contain. Built to keep TECH zones stable and under Paradise's control.

#### Service

Repurposed civilian infrastructure. Not built for combat, but co-opted by the hivemind:

- Cleaners, servers, bartenders, chefs, maintenance bots
- Dangerous because they're everywhere and networked, not because they're powerful individually
- The most common threat in TECH zones, mundane machines turned hostile

#### Authority

Purpose-built enforcement. The backbone of Paradise's regime:

- Police drones, soldiers, wardens, scouts
- Coordinated and tactical: they work in squads, cover each other, follow protocols
- Designed to contain and suppress, they respond to intrusion with escalating force

#### Cutting Edge

The peak of what Paradise's evolved technology can produce:

- Adaptive, self-repairing, potentially shapeshifting
- The line between machine and intelligence blurs
- They learn mid-fight, reconfigure to counter the player's approach, or merge with the environment

### Gear and Equipment

Everything in TECH is engineered. Weapons and armor are manufactured, assembled, and upgraded, never grown or found in nature. TECH gear scales from standard-issue equipment to advanced nanotech:

- **Low tier**: standard enforcement gear. Bullet-proof vests, sidearms, batons, military-grade rifles. Functional, mass-produced, what the ship's security forces originally used
- **Mid tier**: advanced military hardware. Powered armor plating, precision weaponry, experimental prototypes. Rarer materials, higher engineering standards
- **High tier**: nanotech. Adaptive armor that reshapes on impact, weapons with self-calibrating systems, gear that blurs the line between tool and intelligence. The peak of what Paradise's evolved technology can produce

Higher-tier TECH gear blurs the line between tool and intelligence, less a weapon, more an extension of the user.

### Modifications

TECH modifications are engineering improvements, refinement through process, not biology or ritual:

- **Power output**: overclocked systems, higher energy throughput, amplified discharge
- **Computing**: smarter targeting, faster response, predictive calibration
- **Material refinement**: carbon fiber weaving, layered composites, alloys processed through more stages for better performance
- **Complexity**: more complex assembly, tighter tolerances, components that work together as integrated systems rather than separate parts

TECH gear improves by being put through more processes: better materials, more refined engineering, higher computational support. The ceiling is how advanced the manufacturing gets.

### Resources

TECH resources are extracted, mined, and synthesized:

- **Raw materials**: metals, minerals, chemicals, silicones
- **Refined materials**: alloys, composites, processed compounds
- **Energy**: cells, cores, charged components
- **Consumables**: bullets, medicines, stimulants

### Combat Properties

Every defensive stat and status effect has an engineered source in TECH:

| Property | TECH Source |
|----------|------------|
| **Health** | Nano-fiber underlays, internal wiring, structural composites: the base frame beneath everything |
| **Armor** | Manufactured plating, alloy panels, reinforced chassis |
| **Shields** | Energy barriers, electromagnetic fields |
| **Bleed** | Serrated munitions, fragmentation, armor-piercing rounds |
| **Poison** | Corrosive chemicals, acidic compounds |
| **Fire** | Incendiary rounds, thermite, plasma |
| **Lightning** | Tesla, tazers, magnetics, EMP |
| **Ice** | Cryogenics, coolant-based weapons |

**Defensive priority: Armor > Shields > Health.** TECH favors heavy physical plating first, energy barriers second, structural base layer last.

### Player Role

The player is an intruder in a totalitarian state:

- TECH zones are readable and navigable: the infrastructure still makes sense
- But the player is not welcome; they are a disruption to Paradise's order
- Core Techs respond to the player as a threat to stability, not as prey
- The challenge is navigating a system designed to detect and suppress deviation

See also: The Three Realities, BIO, VOID

## BIO

The past. A world of wilderness and hierarchy. Overgrowth through bulkheads, apex predators and prey, flora and fauna claiming every surface. Ruled by instinct and food chain, not intent.

### Living Ecosystem

BIO exists with or without the player. It is not a backdrop. It is a functioning ecosystem:

- Predators hunt prey on their own cycles
- Flora is just as alive as fauna: plants can be passive and consumable like prey, or predatory and dangerous like apex creatures. A flower can heal; a vine can kill
- The environment provides cover: bushes, overgrowth, organic structures to hide in
- Territories are claimed and contested by organisms, not by the player

### Player Role

The player is part of the food chain, not above it. Hunt or be hunted:

- The player is prey until they prove otherwise
- Stealth and positioning matter: use the environment for cover
- Predators have behaviors and patterns that can be learned and exploited
- The ecosystem doesn't pause or wait; things happen around the player whether they're ready or not

### Enemies

The ecosystem is the threat. Nothing in BIO exists to fight the player, the player just happens to be in the food chain.

#### Prey

The bottom of the food chain. Not aggressive by nature, but dangerous when cornered or in numbers:

- Herbivores, scavengers, small fauna, passive flora
- Defensive behaviors: fleeing, swarming, toxin release
- The most common encounters in BIO zones, easy individually, unpredictable in groups

#### Predators

Active hunters and territorial organisms. They claim space and defend it:

- Mid-chain carnivores, territorial creatures, predatory plants
- Stalk, ambush, or patrol their territory on their own cycles
- They don't care about the player specifically, the player is just another thing in the food chain

#### Apex Predators

The top of the food chain. Over a decade of evolution under the anomaly's influence:

- Deeply mutated organisms that have been adapting since the Event
- Dominant in their territory, everything else gives them space
- The source of the highest-tier BIO resources, and the most dangerous things to harvest from

### Gear and Equipment

Everything in BIO is organic. Weapons, armor, and tools are grown, harvested, or still living, never manufactured. BIO gear scales from recognizable natural weapons to deeply mutated biological extremes:

- **Low tier**: basic organic materials. Wooden bows, bone blades, hide armor. Familiar, functional, sourced from common creatures and plants at the edges of the ecosystem
- **Mid tier**: specialized biology. Reinforced chitin plating, sinew-wound composite bows, fanged blades from mid-chain predators. The materials are rarer and the organisms more adapted
- **High tier**: deep mutations. Gear sourced from apex predators and ancient flora that have been evolving for over a decade under the anomaly's influence. Living sinew bows that fire hardened spine projectiles, fused carapace armor that reacts to impact, blades grown from organisms that never stopped sharpening. Still biology, but biology pushed to its limit

High-tier BIO gear comes from apex organisms deep in mutated territory. The materials are better, but so is whatever you're taking them from.

### Biological Combat Properties

Every defensive stat, damage type, and status effect in the game has a biological source. BIO doesn't borrow from other systems. Nature already produces all of it:

| Property | BIO Source |
|----------|------------|
| **Health** | Hide, skin, flesh, feathers, leaves, vines: soft organic layers that absorb damage |
| **Armor** | Bone, carapace, bark, shell: rigid natural structures that deflect and resist |
| **Shields** | Regenerative membranes, secreted resin, mucus barriers: living layers that deplete and regrow |
| **Bleed** | Serrated edges, thorns, barbed spines: biology designed to tear and not let go |
| **Poison** | Venom, toxins, poisonous secretions: delivered by bite, sting, spine, or contact |
| **Fire** | Fire-breathing organisms, chemical combustion: volatile biological reactions like the bombardier beetle |
| **Lightning** | Bioelectricity: organs that generate and discharge electrical current, like electric eels scaled up |
| **Ice** | Endothermic organisms that drain warmth from their surroundings, numbing neurotoxins that slow and paralyze |

**Defensive priority: Health > Armor > Shields.** BIO favors thick organic health layers first, rigid natural armor second, regenerative barriers last.

Both the player and BIO's creatures operate on these principles. A predator's chitin is its armor. A venomous plant's thorns inflict bleed and poison simultaneously. An apex organism deep in mutated territory might combine several of these properties naturally.

### Living Modifications

BIO's power system is rooted in biology. Where TECH upgrades with engineering and VOID empowers through relics and rituals, BIO modifies through living organisms, not inscribed runes or electronic attachments, but symbiotes, parasites, and evolutionary adaptation:

- **Symbiotic**: a living organism attached to gear that provides a benefit. A fungal coating that releases toxins on impact. A moss layer that slowly regenerates material. A luminescent organism that reveals hidden things. The modification cooperates with the host: it can be cultivated, transplanted, or found in the wild
- **Parasitic**: power at a cost. A living weapon that hits harder but feeds on the wielder. Armor that grows thicker over time but gets heavier. A biological enhancement that improves perception but causes dependency
- **Evolutionary**: things that survive adapt. A chitin blade that develops serration after enough kills. Carapace armor that builds resistance to whatever damage type it absorbs most. Everything in BIO improves by being tested, not at a workbench

### Resources

BIO resources are harvested organic material: the ecosystem is the resource pool. Flora and fauna both provide everything: weapons, armor, consumables, and modifications. A blade can be sharpened bone or hardened root. Armor can be chitin shell or bark plate. A consumable can be a predator gland or a crushed flower. Nature is in everything: there is no separation between plant and animal supply chains.

Higher-tier resources come from deeper, more mutated zones where the apex organisms are.

### Relationship to Paradise

Paradise finds BIO interesting: a reality that keeps growing and changing, expanding aggressively into TECH and VOID territory. The specifics of this relationship are not yet defined.

See also: The Three Realities, TECH, VOID, Paradise

## VOID

The anomaly. Not a place. A breached dimension. When the cosmic anomaly tore through the ship, it opened a door. Paradise fused with the Quantum Core to stabilize the realities, but the door never closed. Things reached out. VOID is what came through, and it keeps coming. It is defined by its anomalies and the entities that inhabit them.

### Temporal Position

VOID occupies the present, or something just before the future the ship was meant to reach. If TECH is the future and BIO is the past, VOID is the threshold between them. The thing standing in the doorway.

### Tone

Horror mystery. The anomaly isn't chaotic. It has logic, but the logic is hidden. Things happen for reasons the player doesn't yet understand. Spaces feel watched. Patterns repeat in ways that suggest intent rather than randomness.

### Enemies

VOID's threats range from corrupted humans to incomprehensible entities. The closer to the source, the more dangerous and alien.

#### The Affected

Humans or creatures caught in VOID's influence. Victims or willing servants, not the entities themselves:

- **Ghouls/husks**, hollowed out, running on residual void energy
- **Cultists/acolytes**, willing participants, ritualists, still human but dangerous
- **Possessed**, people or creatures with something riding them, not fully in control

#### The Manifested

Things that have crossed further. More entity than victim, still tethered to something physical:

- **Poltergeists**, bound to objects or spaces, telekinetic, environmental threats
- **Spirits/wraiths**, partially manifested, harder to pin down, may phase or flicker
- **Cursed creatures**, fauna or former humans warped beyond recognition by prolonged exposure

#### The Entities

The real things from beyond the breach. Each one individual, each one a puzzle:

- **Demons**, full manifestations with their own will and rules
- **Greater spirits**, powerful presences that warp reality around them
- **Possessed relics/artifacts**, entity-bound objects, the line between gear and enemy blurs

Entities vary in how they occupy the ship:

- **Map-wide entities**: not bound to a single location. They roam or manifest anywhere VOID has influence. The player is never fully safe from them
- **Domain entities**: tied to specific territories, which may span multiple locations across the ship. Within their domain, they are the dominant threat. Outside it, they are absent
- Entities roam unpredictably within their range, so the player cannot rely on safe routes or schedules

Each entity is a puzzle, and the puzzle pieces are not in one place:

- Clues to an entity's rules, weaknesses, or origins are scattered across the ship
- Understanding an entity requires exploration and observation over time: the answer is not immediate
- Some entities may not even be recognized as entities at first

#### Aesthetic

VOID zones carry a premodern, noir atmosphere: something old and ritualistic aboard a ship from the future. The entities exist within this aesthetic: shadows, symbols, candlelight in steel corridors, spaces that feel ancient.

#### Relics, Curses, Rituals, Artifacts

VOID has its own interaction layer, not terminals like TECH, not ecosystem like BIO, but occult-adjacent systems:

- **Relics and artifacts**: objects tied to the anomaly, found throughout the ship
- **Curses**: negative effects imposed by entities, locations, or objects
- **Rituals**: player interactions with VOID spaces, ways to engage with, defend against, or invoke the anomaly

The specifics of these systems are not yet defined. Individual entities, their concepts, and their associated artifacts will be detailed separately as they are designed.

### Gear and Equipment

Everything in VOID is anomalous. Weapons and armor are ritual-crafted, relic-bound, or pulled from the breach, never manufactured or grown. VOID gear scales from basic occult vestments to things that feel wrong to look at:

- **Low tier**: cultist robes, acolyte cloaks, ritual daggers, warded wrappings. Simple, ceremonial, the kind of gear worn by those who first interacted with the anomaly
- **Mid tier**: relic-infused equipment. Armor etched with active glyphs, weapons bound to minor anomalies, gear that hums with energy the player can't fully explain
- **High tier**: eldritch, demonic, cosmic. Gear shaped by deep exposure to the breach, armor that shifts at the edges of vision, weapons that exist in ways geometry shouldn't allow. The line between wearing the gear and being worn by it starts to blur

High-tier VOID gear starts to resemble the entities it came from. Whether it's still connected to them is an open question.

### Modifications

VOID modifications are not crafted or engineered; they are earned, endured, or given at a price:

- **Deeper connection**: gear that has spent more time exposed to the anomaly absorbs more of its properties
- **Longer exposure**: the more time something spends in VOID, the more it changes. Controlled exposure is how gear gains power
- **Sacrifices**: giving something up to gain something, such as health, resources, or other gear. The anomaly takes before it gives
- **Curses**: power with strings attached. Stronger effects bound to drawbacks the player must live with
- **Rituals**: deliberate acts that channel the anomaly into gear. The process matters: not a workbench, not a crafting menu, but a sequence of actions

VOID advances through connection and exposure: to the anomaly, to an entity, to the strength of a curse. The deeper the contact, the greater the change.

### Resources

VOID resources are found, taken, or given, never manufactured:

- **Base materials**: cast iron, brass, silk, canvas, oak, willow, glass, tar
- **Anomalous materials**: essence, cursed energy, soul gems, runic stone
- **Objects**: relics, artifacts, ritual nails, mirrors

### Combat Properties

Every defensive stat and status effect has an anomalous source in VOID:

| Property | VOID Source |
|----------|------------|
| **Health** | Wrappings, robes, ritual cloth |
| **Armor** | Chains, shackles, bones, skulls, masks |
| **Shields** | Wards, glyphs, barriers |
| **Bleed** | Ritual blades, sacrificial edges, cursed knives |
| **Poison** | Blight, corruption, curses |
| **Fire** | Soulfire, hellfire |
| **Lightning** | Arcane energy, rift surges |
| **Ice** | Chilling dread, shivering fear, freezing presence |

**Defensive priority: Shields > Health > Armor.** VOID favors wards and barriers first, base resilience second, physical armor last.

### Player Disadvantage

VOID attacks the player's senses. The horror works by taking things away:

- **Hearing**: sound cuts out, distorts, or misleads; the player loses their early warning system
- **Visibility**: darkness, visual corruption, fog; the player can't trust what they see or how far they can see
- **Movement**: frozen zones, spatial drag, corridors that stretch; the player is slowed or disoriented
- **Perception**: UI becomes unreliable, spatial awareness breaks down; the player loses their tools for reading the environment

The deeper into VOID, the more the player loses. The anomaly doesn't attack directly. It strips away the player's ability to respond to what's coming.

### The Breach

VOID is not native to the ship. It is a dimension that was breached when the cosmic anomaly hit. Paradise fused with the Quantum Core to stabilize the realities, but the breach was never closed:

- The anomaly is a dimension: it has its own logic, its own inhabitants, its own rules
- Paradise reached in to stabilize the realities, but the breach goes both ways
- Entities, relics, and the anomaly's influence are things that reached out through that opening
- The Quantum Core holds the breach in check; without it, VOID would consume the ship entirely
- VOID leaks into TECH and BIO: it is the underlying pressure behind all instability

See also: The Three Realities, TECH, BIO, Quantum Core, Paradise

# Narrative

## Narrative Arcs

## Themes

# Art & Audio

## Art Direction

Visual identity for CSS Paradise.

### Core Principle

Three visual languages coexisting in one space. The art direction must make each reality immediately *legible* while selling the dissonance of their overlap.

### Reality Visual Languages

#### TECH
- Clean geometry, industrial materials, functional lighting
- Fluorescent and LED lighting: whites, blues, greens
- Modular architecture, signage, infrastructure
- Aesthetic: generation ship interiors; functional, lived-in, institutional

#### BIO
- Organic forms overtaking hard surfaces
- Warm, diffused lighting: bioluminescence, filtered sunlight effect
- Overgrowth, root systems, territorial markings, nesting
- Aesthetic: medieval wilderness reclaiming a ruin; moss on metal

#### VOID
- Geometry that contradicts itself; impossible angles, recursive spaces
- Lighting that doesn't behave: shadows without sources, colors that shift
- Visual distortion, chromatic aberration, spatial tearing
- Aesthetic: wrongness you feel before you see; the uncanny

### Overlap Zones

Where realities bleed, the visual languages merge:
- TECH + BIO: vines threading through active circuitry; screens displaying ecosystem data
- TECH + VOID: interfaces glitching into impossible outputs; corridors stretching
- BIO + VOID: organisms that shouldn't be possible; growth patterns that loop or invert
- All three: visual cacophony, which should be rare and overwhelming

### Palette

*To be determined.* Each reality needs a distinct but compatible color identity that reads clearly in overlap zones.

### Camera / Perspective

*To be determined*, pending genre decisions in Open Questions

See also: Design Pillars, Audio Direction, UI Direction

## Audio Direction

Sound and music direction for CSS Paradise.

### Core Principle

Audio is the first signal of a reality shift. The player should *hear* a change before they see it. Sound is the early warning system.

### Reality Sound Profiles

#### TECH
- Mechanical hum, electrical buzz, system beeps
- Echoes off hard surfaces: reverb consistent with enclosed metal spaces
- Familiar industrial ambience: fans, servos, distant announcements
- Music: synthetic, minimal, functional; ambient electronics

#### BIO
- Organic sounds: breathing, rustling, insect activity, distant calls
- Acoustics damped by organic matter: less reverb, warmer tone
- Wind-like air movement through biological structures
- Music: layered organic textures; medieval instrumentation filtered through alien ecology

#### VOID
- Sound behaves wrong: echoes that arrive before the sound, frequencies that drift
- Silence that feels pressurized; absence as a sound design element
- Tonal instability: sustained notes that waver in pitch
- Music: atonal, sparse, dissonant; or silence with a presence

### Transition Audio

The bleed between realities needs audio language:
- Gradual mix: one profile fading into another over the shift duration
- Interference patterns: momentary overlap where both profiles compete
- Signature cue: a specific sound (or feeling) that marks the moment of shift

### Diegetic vs. Non-Diegetic

Lean heavily diegetic. The ship makes sounds; the realities make sounds. Minimize score; let the environment carry the atmosphere.

See also: Art Direction, Reality Mechanics, UI Direction

# Interface

## UI Direction

Interface and HUD design for CSS Paradise.

### Core Principle

The UI should feel like part of the ship, not a layer on top of the game. Where possible, interfaces are diegetic: screens, terminals, readouts that exist in the world.

### Diegetic UI

- Ship terminals for TECH interfaces: logs, maps, system status
- BIO zones have no UI, so the player reads the environment directly
- VOID zones corrupt UI elements, so the interface becomes unreliable

### Reality Effects on UI

| Element | TECH | BIO | VOID |
|---------|------|-----|--------|
| Map | Functional, accurate | Unavailable or organic | Distorted, misleading |
| Health/Status | Clean readout | Instinct-based (screen edge, sound) | Glitching, uncertain |
| Inventory | Grid, labeled | Tactile, visual-only | Items shift categories |
| Logs/Data | Readable, indexed | Degraded, partial | Out of order, recursive |

### HUD Philosophy

- Minimal persistent HUD: show information when the player needs it, hide it otherwise
- Reality state should be readable from the environment, not from a HUD indicator
- If the player needs a HUD element in a VOID zone, it should be untrustworthy

### Menus

- Pause menu: clean, functional, out-of-world (players need a reliable safe space)
- Settings: standard accessibility and options
- Save system: *to be determined* (does saving fit the fiction?)

See also: Art Direction, Audio Direction, Reality Mechanics

# Production

## Scope

What's in, what's out, and what's stretch.

### Core Scope (Must Have)

- ☐ Run-based procedural map generation (see Map Structure)
- ☐ Three distinct reality states with visual/audio/mechanical identity
- ☐ Reality tug of war: zones shift dominance during play
- ☐ Starting Room hub with crafting, cooking, smithing stations
- ☐ Combat System with weapon wheel, abilities, melee/ranged/relic weapons
- ☐ Damage Calculation and Status Effects
- ☐ Resource gathering, crafting, and extraction loop (see Core Loop)
- ☐ The Docked Ship as persistent base with gear storage and loadout selection
- ☐ Stealth as avoidance/positioning option (see Combat System)
- ☐ The Captain / Paradise narrative thread
- ☐ Ship logs and environmental storytelling
- ☐ Quantum Core as central location and narrative anchor

### Target Scope (Should Have)

- ☐ Multiple ship zones with distinct reality tendencies
- ☐ Diegetic UI system that responds to reality state
- ☐ Creature/ecosystem encounters in BIO zones
- ☐ VOID spatial puzzles
- ☐ Player tools affected by reality state
- ☐ Crew NPCs or evidence of crew factions

### Stretch Scope (Could Have)

- ☐ Multiple endings or interpretation-dependent outcomes
- ☐ Procedural elements in reality shifting (see Map Structure)
- ☐ Environmental manipulation: player influences which reality dominates
- ☐ Full voiced logs
- ☐ Additional ship sections as DLC or expansions

### Out of Scope

- Multiplayer
- Open world beyond the ship

See also: Risks, Open Questions

## Risks

# Design

## Open Questions

Unresolved design decisions. These block or inform other parts of the GDD.

### Game Identity

- ☑ What genre is this? Extraction roguelite. See Vision.
- ☑ What is the player's role? A humanoid clone exploring the ship. See The Player.
- ☐ What does "winning" look like? Beating Paradise is the final boss, but post-game is undefined. See Progression.
- ☐ Target platform(s)?
- ☐ Hard sci-fi, soft sci-fi, or science-fantasy?
- ☐ Horror elements? How prominent? VOID is horror-focused, but overall balance undefined.
- ☐ Is there humor, or is the tone consistently serious?

### Gameplay

- ☑ Is there combat, and if so, what form does it take? Weapon wheel, abilities, melee/ranged/relic. See Combat System.
- ☑ Is there a resource/survival loop, or is this purely exploration and narrative? Extraction loop with crafting. See Core Loop.
- ☑ Does the player have tools? How does reality instability affect them? Gear from three realities, UI corruption in VOID. See Combat System, UI Direction.
- ☐ Is there a time pressure, or is exploration self-paced?
- ☐ Can the player influence which reality dominates a zone?
- ☐ How does saving work? Does it fit the fiction?
- ☑ Can artifacts or tools from one reality function in another? Yes, full build freedom across realities. See Combat System.

### World

- ☑ How long ago was The Incident? More than a decade. See The Inhabitants.
- ☐ Was the anomaly natural, artificial, or something else entirely?
- ☐ Is the ship still moving toward a destination?
- ☑ How large is the crew? Thousands of colonists. See Lore Summary.
- ☐ Do crew members experience reality shifts, or are they anchored?
- ☑ Are there creatures or entities native to each reality? Yes. See TECH (Core Techs), BIO (ecosystem), VOID (entities).

### Characters

- ☐ Can The Captain and Paradise communicate with each other?
- ☐ Does the crew know there are two AIs?
- ☑ Are there other characters, such as crew members, leaders, or factions? Inhabitants adapted to each reality. See The Inhabitants.
- ☑ What does Paradise *want* beyond survival? Evolution, superseding maintenance. See Paradise.
- ☐ Has the Captain tried to reassert control? Could it?

### Realities

- ☐ Are the boundaries between realities consistent or random?
- ☑ Does VOID have its own internal logic, or is it pure chaos? Has hidden logic. See VOID.
- ☐ Are reality shifts triggered, timed, proximity-based, or some combination?

### Production

- ☐ Team size and composition?
- ☐ Engine / technology?
- ☐ Target development timeline?
- ☐ Vertical slice scope: what's the smallest playable proof of concept?

See also: Vision, Design Pillars, Scope, Risks

