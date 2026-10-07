# CSS Paradise: Game Design Document

## About This Document

Each section describes the design first. Where the Unity prototype already does something, an **In the Prototype** block says what is built. Where design and prototype disagree, the difference is listed under Open Questions instead of being decided here.

| Label | Meaning |
|-------|---------|
| **Built** | In the prototype as a baseline. Values and content are placeholders to test and tune |
| **Designed** | Decided, not built yet |
| **Proposed** | An idea to evaluate, not decided |
| **TBD** | Not defined yet. Left open on purpose, no assumptions made |

Prototype details live in FEATURES.md (what the game does) and WEAPON_BALANCE.md (weapon tuning targets).

### Contents

1. Game Overview: Vision, Design Pillars, Player Experience, References
2. Gameplay: Core Loop, Docked Ship and Extraction, Starting Room, Movement and Controls, Combat System, Gear and Items, Damage Calculation, Status Effects, Stealth, Enemies, Reality Mechanics, Reality Signatures, Map Structure, Resources, Economy NPCs and Quests, Progression
3. World: World at a Glance, The Incident, The Quantum Core, The Three Realities, The CSS Paradise
4. Characters: The Player, The Captain, Paradise, The Central Tension, The Inhabitants
5. Realities: Overview, TECH, BIO, VOID
6. Narrative
7. Art and Audio
8. Interface
9. Production
10. Open Questions
11. Proposed Concepts

---

# 1. Game Overview

## Vision

### Elevator Pitch

You are aboard the CSS Paradise, a colonial starship torn apart by a cosmic anomaly and stitched back together by an AI that broke its own rules to save the colonists. Three incompatible realities, technological, biological and cosmic, now occupy the same hull and bleed into one another without warning. The AI that saved everyone is still there, holding the realities together from the ship's core. It is no longer the mind that was meant to be in charge, and no one knows what it wants.

Each run drops you into a procedurally generated section of the ship. The layout, reality balance and threats change every time. The world, its history and its mysteries persist across runs.

### Genre

Extraction roguelite with exploration, survival and narrative elements: procedurally generated runs, a risk and reward extraction loop, persistent lore and meta progression across runs.

### Platform

TBD. The prototype is built in Unity 6 (URP).

### Target Audience

Players who value atmosphere, discovery and narrative ambiguity over explicit objectives. People who liked piecing together the story of Outer Wilds, the systemic environment of Prey or the tension of Alien: Isolation, and who are drawn to the *questions* more than the combat.

### What Makes It Different

* **The environment is the central system.** Reality itself is unstable, not just the enemies or puzzles.
* **The realities share one space.** They are not levels or biomes. They contest the same rooms at the same time.
* **The antagonist, if there is one, is the ship's own mind.** Not evil, just unbound.
* **The player starts knowing nothing.** The ship's history, the realities, Paradise and their own identity are all mysteries uncovered through play.

## Design Pillars

Every design decision is measured against these.

1. **Unstable Ground.** The world never feels reliable. Not through jump scares or unfairness, but through the sense that the space you are in might not stay the way it is. Reality instability is the world, the mechanic and the theme.
2. **Neither Side Is Wrong.** The Captain and Paradise are a genuine dilemma, not good versus evil. Every piece of evidence complicates the picture instead of simplifying it.
3. **Discovery Over Direction.** Answers are earned through survival, exploration and evidence gathered across runs. Minimal waypoints, quest markers and explicit objectives. The ship is the puzzle, the player's curiosity is the engine.
4. **The Environment Tells the Story.** Logs and dialogue supplement, but the space itself is the main storytelling medium: a corridor overtaken by BIO, a terminal still running TECH diagnostics, a room where VOID has bent the geometry.
5. **Systems That Reflect the World.** Mechanics emerge from the fiction. If reality is unstable, the player's tools are unstable too. If the ship's mind is split, the systems the player touches reflect that split.

## Player Experience

TBD.

## References

Games this document points to as touchstones:

| Game | What it informs |
|------|-----------------|
| Outer Wilds | Piecing a story together through discovery |
| Prey | A systemic environment |
| Alien: Isolation | Tension |
| Hades, Enter the Gungeon | Escalating tiers with the main antagonist as the final wall |
| Dark Souls | Shortcuts that only open from the far side |
| God of War | The sidestep into roll dodge |

---

# 2. Gameplay

## Core Loop

> Docked Ship → Starting Room → Explore → Gather → Craft → Push → Boss → Extract → back to the Docked Ship

| Step | What happens |
|------|--------------|
| **1. Docked Ship** | Choose which stored gear to bring. Anything brought can be lost on death |
| **2. Starting Room** | Receive a random starting loadout. Craft at the hub. The room is always safe |
| **3. Explore** | Push into the generated map. Read which reality holds each room. The way to the boss is never shown |
| **4. Gather** | Collect resources from enemies and the environment. Each reality yields its own materials |
| **5. Craft** | Return to the hub and turn materials into gear, consumables and upgrades |
| **6. Push** | Go back in better equipped. Complete the boss unlock conditions while rooms repopulate around you |
| **7. Boss** | Fight the map's boss. Its reality stays unknown until you reach the boss room |
| **8. Extract** | Victory keeps everything. Death loses everything brought and found |

### The Tension

Every run is a bet placed before the player knows what they are walking into:

* **Bring nothing**: rely on the random starting loadout. Low risk, harder run.
* **Bring everything**: best odds, but lose it all on death. High risk, easier run.
* **Bring selectively**: weigh the stakes against the map's difficulty. The sweet spot most players find.

### Meta Loop

What persists across runs and deaths:

* **Docked ship storage**: gear and resources from successful extractions.
* **Skill trees**: permanent passive upgrades (see Progression).
* **Player knowledge**: systems, enemy patterns, reality cues and lore.
* **Boss progression**: advancing through difficulty tiers toward Paradise.

The character forgets. The player does not. The gap between what the character knows and what the player knows is the narrative engine. Mechanical and narrative progression run side by side: the player gets stronger and learns the truth at the same time.

### In the Prototype

**Built.** The full loop runs in the MapTest scene: start in the hub with a random loadout, explore, extract at the Exit, review the run on the ship screen, pack a kit and deploy again. Not built yet: extraction gated by the boss, skill trees, saving the ship's hold.

## Docked Ship and Extraction

The player's persistent base is a small vessel docked to the hull of the CSS Paradise. It sits outside the anomaly and outside the tug of war between realities.

* **Storage**: gear, resources and items brought back from runs.
* **Loadout**: the player chooses what to bring before each run.
* **Safety**: the only guaranteed safe space.

The player always enters through the same docking port. The port is fixed, but the interior rearranges between runs as the realities shift, so the same airlock leads somewhere new every time.

| Outcome | Result |
|---------|--------|
| **Victory** | Keep all looted gear, resources and crafted items. Everything goes to the docked ship |
| **Death** | Lose all gear brought and everything found. The player is cloned and their memory resets. Gear stored on the docked ship is never at risk |

### In the Prototype

**Built.**

* The Exit room's pad is the extraction point (hold Interact). Everything carried, pack, weapons and worn armor, goes into the ship's hold.
* After every run a ship screen shows the result (EXTRACTED, or KILLED IN ACTION with what was lost), the hold and the kit for the next run. **Deploy** starts a run with the packed kit on top of the starting loadout.
* Leaving a run early (restart or main menu) counts as not making it out.
* **Emergency Exit** rooms hold an escape pod that ends the run early. Weapons and worn armor make it home, but only half of each stack in the pack fits and loose gear in the pack is left behind.
* The hold lasts for the play session only and is not saved to disk. The docked ship is a screen, not a scene yet.

## Starting Room

A safe room at the start of every map. It is both the launch point and a hub to return to during the run.

It solves two problems:

1. **Fair start**: the player always begins with enough to survive if they play well.
2. **Persistent hub**: a place to craft, upgrade and restock between pushes.

### Starting Gear

| Slot | What | Purpose |
|------|------|---------|
| **Weapons** | 1 or 2 weapons (for example one melee, one ranged) | Baseline combat options |
| **Equipment** | 1 or 2 armor pieces in random slots | Baseline survivability |
| **Consumables** | A small supply of healing and utility items | Early sustain |
| **Crafting materials** | A starter bundle of basic resources | Enough for first upgrades or extra consumables |

The set is fully random: not tailored to the map, but always viable for the early game. The player never starts with a full loadout or four abilities. Filling the weapon wheel, equipment slots and ability slots is part of the run.

### Hub Stations

| Station | Function |
|---------|----------|
| **Crafting** | Combine materials into items, components and tools |
| **Cooking** | Prepare food, healing items and buffs |
| **Smithing** | Forge and upgrade weapons and armor |

Stations are always available. What the player can make depends on the materials and recipes they have found.

The starting room is never contested by the realities and never repopulates.

### In the Prototype

**Built.**

* No enemies spawn in the starting room. A single **Hub Workbench** with every recipe stands beside the spawn point (see Crafting).
* The starting loadout is one or two generated weapons (pistol, SMG, assault rifle or shotgun, never two of the same kind), ammo, bandages, sometimes a combat stim, one or two frag grenades, and some scrap metal and cloth. It is rolled from the level's seed. Starting armor is not part of it yet.

## Movement and Controls

**Built.** Not part of the original design. These are baselines for playtesting.

* **Movement**: walk, sprint, crouch, slide, jump and mantle (pulling up onto ledges). Movement is physical and tuned for responsiveness rather than realism.
* **Dodge**: a short evasive move. Rolls and dashes give brief invulnerability and block attacks until they finish. Several styles are prototyped to find the right fit: sidestep into roll (default), committed roll, steerable boost, long dash and air dash. The final choice is TBD.
* **Camera**: first and third person, switchable at any time, with aim down sights in both, a shoulder swap and a lock on. The final perspective is TBD.
* **Input**: keyboard, mouse and gamepad, every action rebindable. Holding a weapon or item key opens a selection wheel. Optional input buffering remembers a press briefly and acts on it as soon as possible.

## Combat System

### Weapon Wheel

The player carries up to four weapons and switches between them mid combat.

* Any combination is allowed: melee, ranged, relic, from any reality.
* Ranged weapons are powerful but use ammo. Melee costs stamina instead. VOID relics run on cooldowns.
* Using an ability tied to a weapon switches to that weapon. This is a setting the player can turn off.
* Mixing weapon types and resource models opens up combos.

**In the Prototype (Built):** four slots, picked with keys 1 to 4, the scroll wheel, a hold wheel or a "last weapon" toggle. Each weapon has its own draw time, and swapping then dodging skips the draw. Picking up a weapon with a full wheel swaps out the one in hand. A quick melee attack is always available alongside a gun. Weapon abilities are not built.

### Weapon Types by Reality

**Designed.**

| Reality | Role | Melee | Ranged | Resource model |
|---------|------|-------|--------|----------------|
| **BIO** | Melee dominant, the wheel's backbone | Daggers, swords, axes, hammers, spears, shields | Bows, crossbows | Melee is free and always reliable. Arrows and bolts are crafted from common materials |
| **TECH** | Ranged dominant | Fire axes, commando knives, batons, for when the ammo runs dry | Pistols, rifles, shotguns, SMGs, energy weapons | Bullets and energy cells, both scarce |
| **VOID** | Relics and noir firearms | Ritual daggers, sacrificial blades | **Relics**: sceptres, wands, books, staves. **Antique firearms**: revolvers, lever action rifles, double barrel shotguns, flintlocks | Relics run on cooldowns, stronger ones recharge slower. Firearms use bullets |

VOID relics never compete for ammo. They fill the gaps between reloads and give ranged options to a melee build.

### Ammo

| Ammo | Source | Used by |
|------|--------|---------|
| **None** | | Melee of every reality |
| **Arrows and bolts** | Crafted from common materials | Bows, crossbows |
| **Bullets** | Scarce | TECH ballistic weapons, VOID antique firearms |
| **Energy cells** | TECH resources, scarce | TECH energy weapons |
| **Cooldown** | Recharges over time | VOID relics |

**In the Prototype (Built):** reserve ammo is a shared pool per caliber (Light, Standard and Heavy Rounds, Shotgun Shells, Arrows, Energy Cells). Two weapons of the same caliber draw from one pool, so the loadout becomes a supply decision. Each weapon keeps its own loaded magazine. Calibers stand in for the reality ammo above.

### Prototype Weapon Families

**Built.** Placeholder families that stand in for the reality weapon types until those are designed. Every weapon is generated per drop by tier and quality.

| Ranged | Identity |
|--------|----------|
| **Assault rifle** | All rounder |
| **SMG** | High fire rate, short range, burns ammo |
| **Pistol** | Accurate and ammo efficient, from light sidearm to hand cannon |
| **Sniper** | Slow, heavy hits at long range, poor hip fire |
| **LMG** | Large belt, sustained fire, slow reload |
| **Shotgun** | Pellet cone at very short range |
| **Bow** | Hold to draw, arrows drop with physics, nearly silent. Some loose 2 or 3 arrows at once. Vertical draw hits harder and tighter, horizontal draw is faster and wider |
| **Crossbow** | Hits harder and flatter than a bow and pierces armor, slow to crank. Some have autoloaders |

| Melee | Identity |
|-------|----------|
| **Dagger** | Very fast, short reach, long parry window, weak block |
| **Sword** | All rounder. A parry is answered with a riposte slash |
| **Axe** | Slower, bites through armor |
| **Hammer** | Slow area slams that crush armor, sturdiest block, tightest parry |
| **Spear** | Long reach, narrow guard |

Ranged weapons can be semi auto, full auto, burst or charge. Their stats group into **Accuracy** (spread), **Control** (recoil) and **Handling** (draw and sway).

### Melee

**Built.** Not detailed in the original design.

* **Attacks**: tap for a light combo, hold for a heavier finisher.
* **Block**: raising the guard stops most damage from the front. Each blocked hit costs stamina, and running out breaks the guard.
* **Parry**: a hit landing just after the guard goes up is fully deflected and briefly stuns the attacker.
* **Cancels**: once a swing has struck, a block or dodge can cut its recovery short.
* **Combo weaving**: a dodge, parry, ability, consumable or weapon swap between combo steps holds the combo open for 2 to 3 seconds. Each melee weapon remembers its place in its combo across swaps.

### Weapon Stats

Not every stat applies to every weapon: melee has no magazine, bows have no reload.

| Stat | Description |
|------|-------------|
| **Base Damage** | Damage per hit before modifiers |
| **Attack Speed / Fire Rate** | How fast the weapon attacks |
| **Crit Chance** | Chance of a critical hit |
| **Crit Damage** | Damage multiplier on critical hits |
| **Status Chance** | Chance of applying a status effect on hit |
| **Status Damage** | Base value for status effect damage |
| **Lifesteal** | Share of damage dealt recovered as health |
| **Penetration** | How much damage or status bypasses defensive layers |
| **Range** | Melee reach or projectile range |
| **Magazine Size** | Rounds before reloading (ranged only) |
| **Ammo Reserve** | Total ammo carried for the weapon (ranged only) |
| **Reload Speed** | Time to reload (ranged only) |

Stats roll from the item's quality (see Gear and Items).

### Stamina

**Designed.** Stamina is the cost of physical effort. It drains on melee swings, dodge rolls and sprinting, and regenerates over time. Ranged attacks, abilities and consumables do not use it. It gives melee a cost comparable to ammo: swing recklessly and there is nothing left to dodge with.

**In the Prototype (Built):** melee swings cost stamina (heavy attacks double) and so do blocked hits. Sprint and dodge costs exist but are off by default, dodges use a cooldown instead. Stamina is one instance of a general meter system (oxygen, mana, rage), so reality specific resources can reuse it.

### Abilities

**Designed.**

* Abilities come from equipped items: weapons, armor and the backslot.
* The player has **4 ability slots**. Any ability from any equipped item fits any slot.
* Abilities cost no resource. They run on cooldowns, and stronger abilities have longer ones.
* Cast times vary: instant abilities can be woven into combos, channeled abilities are stronger but leave the player exposed.

**Stat versus ability tradeoff.** Items without an ability compensate with stronger stats. A full ability build is versatile with lower raw stats, a zero ability build runs on raw power alone, most builds sit in between. With up to ten items offering abilities and only four slots, picking abilities is always a meaningful cut.

**In the Prototype (Built):** four slots with cooldowns, stored charges and optional cast times. Abilities are assigned to slots directly and are not granted by gear yet. Some abilities can also cost a meter or **Surge**, a gauge filled by hits, kills and parries. Included placeholders: Dash, Projectile, Heal, Shockwave, Cone Blast, Crushing Blow, Ground Slam, Damage Boost, Stealth, the reflects Vengeance, Deflect and Absorb (which turn incoming hits into damage, projectiles or healing), and the Surge abilities Arc Lash, Overdrive, Rupture and Static Rounds.

### Consumables

**Designed.** Consumables have cast times too. Weak, instant items can be used mid combo. Stronger, channeled items need a safe window. Their effects are fixed, never rolled: a bandage always heals the same amount.

**In the Prototype (Built):**

* Four item slots (keys 5 to 8).
* **Channeled items** show a fill bar and are only used up when they finish. Some are interrupted by taking damage.
* **Instant items** work the moment the key is pressed and put every consumable on a short shared cooldown.
* Included: Bandage (heals and stops bleeding), Combat Stim (damage buff and stamina), Ration (instant small heal).
* **Throwables** are items too. Frag and smoke grenades are aimed with a predicted arc.

### Build Freedom

Builds are meant to be creative and endless. The player can freely combine:

* Weapons and armor from any reality: full TECH, full BIO, full VOID or mixed.
* Any ability from any equipped item in any slot.
* Stat focused, ability focused or hybrid loadouts.
* Melee heavy, ranged heavy or balanced weapon wheels.

The skill trees reward gear from their own reality, but the player is never locked into one.

## Gear and Items

### Equipment Slots

| Slot | Type |
|------|------|
| **Helm** | Head protection |
| **Torso** | Body armor |
| **Gloves** | Hand protection |
| **Legs** | Lower body armor |
| **Boots** | Footwear |
| **Backslot** | Anything that fits: capes, cloaks, backpacks, shields, quivers, power packs |

Each piece gives passive stats. Some also give an active ability.

### Armor Stats

| Stat | Description |
|------|-------------|
| **Health** | Bonus to the health pool |
| **Armor** | Physical damage reduction |
| **Shield** | Regenerating layer that absorbs damage before health |
| **Cooldown Reduction** | Shorter ability cooldowns |
| **Status Resistance** | Lower chance or duration of incoming status effects |
| **Movement Speed** | Faster movement |
| **Crit Chance / Crit Damage** | Bonus critical hit chance and damage |
| **Status Chance / Status Damage** | Bonus status chance and damage |
| **Health Regen** | Passive health recovery |
| **Shield Regen Rate** | How fast shields recharge |

### Quality and Tiers

**Designed and Built.** Weapons and armor roll a **quality score** from 1 to 100 that sets their total stat budget. Tiers set the quality range:

| Tier | Quality | Stat behavior |
|------|---------|---------------|
| **Common** | 1 to 20 | Low budget, clear tradeoffs. High damage means low fire rate or a small magazine |
| **Uncommon** | 21 to 40 | Tradeoffs remain, but hurt less |
| **Rare** | 41 to 60 | One strong stat without a crippling weakness |
| **Epic** | 61 to 80 | Several strong stats, minor tradeoffs |
| **Legendary** | 81 to 100 | Strong across the board, few weaknesses |

* Quality decides how much power an item has. Tradeoff axes decide how that power is spread, so even a Legendary keeps a personality.
* Rolls stay random inside the tier. A Legendary can roll low and a Common can roll high. The tiers set the floor and ceiling, the dice decide where you land.
* Tier is influenced by resource quality when crafting and by map difficulty when looting.
* Consumables, resources and other stackable items never roll. A better version is a separate item at a higher tier, not a luckier roll.

### Upgrades and Attachments

**Designed.** Weapons can be upgraded to improve their base stats. Attachments, crafted or looted, push a piece of gear toward a specific build.

**In the Prototype (Built):** attachments fit into slots, 1 at Common up to 4 at Legendary. Their effects are fixed, often with a drawback, and fully reversible. Upgrades are not built.

### Perks

**Built.** Not part of the original design.

Weapons and armor roll perks on top of their stats: fixed traits that stay with the item for life.

| Tier | Common | Uncommon | Rare | Epic | Legendary |
|------|--------|----------|------|------|-----------|
| **Perks** | 0 | 1 | 1 | 2 | 2 |

Every item also has a 25% chance of one extra perk.

* **Passive perks** are always on. A weapon's change that weapon (magazine, multishot, crit, status, reach, lifesteal). Armor's change the wearer (move speed, cooldowns, regen, resistances, shield).
* **Triggered perks** fire on an event: a kill, hit, crit, parry, dodge, ability cast, damage taken, heal, aiming, reload start or finish, consumable use or weapon swap. Each can have a chance and a cooldown.
* A weapon's buffs end when it is swapped away, so each weapon's power stays its own. Armor buffs run their full time.
* Perks cannot trigger each other, so they never loop.
* The included perks are placeholders to tune.

### Crafting

**Designed.** Gear crafted at the hub rolls random stats, and better resources raise the chance of a higher tier. Each reality improves gear its own way: engineering in TECH, living modifications in BIO, anomalous empowerment in VOID (see Realities).

**In the Prototype (Built):** one workbench with four recipes made from Scrap Metal and Cloth: Bandage, Combat Stim, Extended Magazine and Combat Vest. Resource quality and reality modification paths are not built.

## Damage Calculation

### Formula

> Effective Damage = Raw Damage × 100 / (100 + Effective Armor)

> Effective Armor = Armor × (100% minus Armor Penetration)

* Each point of armor helps less than the last. 100 armor halves damage.
* Penetration effects multiply, so stacking them is efficient.
* Only Bleed and Poison deal damage that ignores a defensive layer.

### Defensive Layers

* **Shields** absorb damage before health. Damage beyond the shield carries over to health. Shields regenerate after a few seconds without taking damage (delay TBD per source).
* **Health** is only hit once shields are gone, and stays lost until healed.

**In the Prototype (Built):** armor reduces every hit before it reaches shields and health. Characters have head, body and limb hitboxes. The head takes the weapon's headshot bonus, limbs take 75% damage. Crit chance lets any hit count as a critical hit. Allies never damage each other.

### Enemy Effective Health Baseline

Low tier enemies hover around **100 total effective health**, split by their reality's defensive priority. These splits are examples, not fixed templates. Values for higher tiers are TBD.

| Reality | Priority | Design example | Prototype tier 1 enemy |
|---------|----------|----------------|------------------------|
| **TECH** | Armor > Shields > Health | 30 health, 50 armor, 20 shields | Maintenance Bot: 30 health, 50 armor, 37 shield |
| **BIO** | Health > Armor > Shields | 80 health, 20 armor | Scavenger: 80 health, 25 armor |
| **VOID** | Shields > Health > Armor | 50 health, 50 shields | Husk: 45 health, 55 shield |

### Weapon Damage Targets

Design targets for tuning, measured as shots to kill a 100 health low tier enemy. Shotguns, energy weapons and melee are not baselined yet. Full targets live in WEAPON_BALANCE.md.

| Weapon | Shots to kill | Role |
|--------|---------------|------|
| **Pistol** | 4 to 7 | Accurate, ammo efficient, low fire rate |
| **SMG** | 4 to 7 | High fire rate, burns ammo, faster kills in real time |
| **AR** | 3 to 5 | All rounder, best damage per bullet |
| **LMG** | 3 to 5 | Sustained fire, large magazine, slow handling |

## Status Effects

**Designed and Built.** Status effects add damage, control or debuffs on top of base damage. Weapons, melee attacks and grenades can each apply them on hit.

| Effect | Type | Bypasses | Stacks | Key mechanic | Best against |
|--------|------|----------|--------|--------------|--------------|
| **Bleed** | Damage over time | Armor | No | 50% of the hit plus 2% of max health | High armor, tanky targets |
| **Poison** | Damage over time | Shields | Up to 5 | Each stack ×1.2 | Groups, sustained pressure |
| **Fire** | Damage over time | Nothing | No | Steady damage | Medium armor |
| **Lightning** | Damage and chain | Nothing | Yes | +50% against shields, chains | Groups, shields |
| **Ice** | Debuff and control | Nothing | Up to 5 | Slow, armor shred, stun | Setup, heavy armor |

### Bleed

> Damage per tick = Raw Damage × 50% + Target Max Health × 2%

Ignores armor. Reapplying resets the duration. Scales with both the attacker's damage and the target's toughness.

### Poison

> Damage per tick = Status Damage × 1.2 ^ (Stacks minus 1)

| Stacks | 1 | 2 | 3 | 4 | 5 |
|--------|---|---|---|---|---|
| **Multiplier** | 1.0× | 1.2× | 1.44× | 1.73× | 2.07× |

Bypasses shields. Rewards spreading poison across several targets.

### Fire

> Damage per tick = Status Damage × Fire DPS%

Fire DPS% is set per weapon or skill. Reduced by armor. Reapplying resets the duration.

### Lightning

> Damage = Status Damage, +50% when hitting shields

* Chains to nearby enemies: up to 1 + the target's Lightning stacks.
* Each chain deals 20% less than the hit before it and never strikes the same enemy twice.
* Chain radius is set per weapon or skill.

### Ice

| Stacks | Slow | Armor reduction |
|--------|------|-----------------|
| **Per stack** | 5% | 2% |
| **5 (max)** | 25% | 10% |

At 5 stacks the target is **stunned** for a set time (TBD), fully immobilized, and loses another 10% armor (20% in total). Stacks decay after a few seconds without a new stack, and reapplying refreshes them. Early stacks are weak, the payoff comes at the stun.

## Stealth

**Designed.** Combat is not the only way through a room. Each reality offers cover differently:

* **BIO**: bushes, overgrowth and organic cover.
* **TECH**: vents, maintenance corridors and blind spots.
* **VOID**: fog, darkness and sensory distortion, which also impair the player.
* **Silent weapons** allow engagement without alerting distant enemies.

### Avoidance, Not Assassination

Stealth is for movement and positioning, not damage. There is no stealth damage multiplier.

* The reward is **position**: choosing when, where and who to engage first.
* Engaging from stealth starts a fight with an advantage, but it is still a fight.
* Stealth is about avoidance and information.

### Room Conditions

Some rooms set conditions before their doors open:

* **Exterminate**: every enemy must die. Stealth is not an option.
* **Open**: no combat requirement. Fight, sneak or run.
* Other conditions: TBD.

The mix keeps stealth a tool, not an exploit. Stealth players eventually meet rooms that demand a fight, and fighters benefit from sneaking past rooms they are not equipped for.

### Weak Start Safety Net

Stealth keeps every run winnable. A player dealt a bad starting loadout can avoid fights, gather resources and build up first. A run is never a dead end, just a harder path.

### In the Prototype

**Built.**

* Bushes, smoke clouds and the Stealth ability (6 seconds) hide the player. Hidden players are only noticed by enemies right next to them.
* Crouching makes the player harder to notice, sprinting easier.
* Attacking reveals the player for a moment. Silenced weapons do not, and bows are nearly silent.
* Enemies still hear noise and come to investigate.
* Smoke blocks enemy line of sight.

## Enemies

### Faction Hierarchy

Every reality's forces follow the same broad shape. Who sits at the top of each is TBD.

| Rank | Role | In play |
|------|------|---------|
| **Rank and file** | Common enemies in three tiers. Tier 1 is everyday threats, tiers 2 and 3 are tougher and roll random affixes | Regular rooms. Higher tiers are more common in higher tier rooms |
| **Lieutenants** | Named or unique figures serving the leadership | Demi bosses, boss unlock conditions |
| **Leadership** | The reality's big bosses | Map bosses |

Each reality fills this shape its own way: **TECH** by command, **BIO** by survival, **VOID** by depth (see Realities). Each reality's enemy roster is described in its own chapter.

### Demi Bosses

**TBD.** Mid tier threats, stronger than standard enemies and weaker than the map boss. Defeating one can be a boss unlock condition, and they can appear as random encounters. Designs, spawn rules and other roles are TBD.

### In the Prototype

**Built.**

* **Behavior**: enemies patrol, investigate what they see or hear, then chase and fight in melee or at range. Getting hit alerts them to the attacker.
* **Senses**: a sight cone, close range awareness in every direction, and hearing for gunfire, swings and explosions.
* **Tier 1 enemies**: one per reality, the Maintenance Bot (TECH), the Scavenger (BIO) and the Husk (VOID). Capsule placeholders (stats in Damage Calculation).
* **Tiers 2 and 3**: about 2.5 and 5 times the effective health of tier 1 and harder hitting. They are stat only placeholders built on tier 1.
* **Affixes**: tier 2 rolls one, tier 3 rolls two: Armored (+40 armor), Warded (+40 shield), Vital (+60% health), Brutal (+40% damage), Swift (+30% speed).

How often each tier appears depends on the room's tier:

| Room tier | Tier 1 enemies | Tier 2 enemies | Tier 3 enemies |
|-----------|----------------|----------------|----------------|
| **1** | 90% | 10% | 0% |
| **2** | 45% | 45% | 10% |
| **3** | 10% | 45% | 45% |

## Reality Mechanics

### Reality States

No zone is permanently stable. At any moment a zone leans toward one of these states:

| State | Description | Gameplay effect |
|-------|-------------|-----------------|
| **Dominant TECH** | The alternate ship's systems in control, enforced by its central AI | Full tool and interface access. Core Techs active |
| **Dominant BIO** | The ecosystem has claimed the zone | Survival hazards, territory and predator rules |
| **Dominant VOID** | The anomaly has broken through | Hearing, visibility and movement impaired. Entities present |
| **Contested** | Two realities pulling at one space | Partial rules from both. Volatile, can tip either way |
| **Fractured** | All three in flux | Highly dangerous and rare. Reveals deep lore |

### Shifts

Zones shift as realities gain or lose ground. What drives a shift:

* **Distance to the Quantum Core**: the closer, the harder for any one reality to dominate.
* **Paradise's attention**: he holds the balance from the Core but cannot hold everywhere at once. Where his focus moves, the balance tips.
* **Player actions**: using systems, disturbing ecosystems, exposing anomalies.
* **Story beats** that reconfigure the ship.
* **The realities themselves**: BIO grows, VOID leaks, TECH erodes without maintenance.

How often and how fast shifts happen is TBD.

### Reading the Environment

The player learns to read which reality holds a space and to anticipate shifts:

* **TECH**: working lights, humming systems, clean geometry.
* **BIO**: organic growth, animal sounds, humidity, territorial markings.
* **VOID**: sound cutting out, visibility dropping, frost, spatial distortion, the feeling of being watched.

### Combat Identity

| Reality | Defense priority | Affinity | Effect | Counters |
|---------|------------------|----------|--------|----------|
| **TECH** | Armor > Shields > Health | Lightning | Breaks shields | VOID |
| **VOID** | Shields > Health > Armor | Ice | Breaks armor | TECH |
| **BIO** | Health > Armor > Shields | Bleed and Poison | Attrition. Bleed bypasses armor, Poison bypasses shields | No hard counter |

The priorities apply to a reality's gear and its enemies alike: a TECH enemy has heavy armor to crack, a BIO creature has a deep health pool, a VOID entity hides behind wards. TECH and VOID are a direct counter pair, so those fights reward knowing the matchup. BIO sits outside the pair and wears anything down over time, so its fights reward endurance.

### What Each Reality Allows

* **TECH**: access terminals, use electronic tools, read logs, interface with ship systems.
* **BIO**: track creatures, find organic resources, navigate by ecosystem logic.
* **VOID**: senses degraded, UI unreliable, entities active. The player works with less information.

### In the Prototype

**Built.**

* Every room belongs to one reality. The reality decides how a room looks and which enemies it fields, never what the room is. A loot room, resupply stop or peaceful room can sit in any reality.
* Each map rolls a **faction mix**: Infested (one reality holds nearly everything), Balanced (even territories), Contested (many small interleaved pockets) or Warped (every room random). The odd room is flipped to a random reality.
* **Breach rooms** mix two realities' enemies. **Rift rooms** pit two realities against each other in waves.
* Items are tagged with the reality they came from.
* Realities change between maps only. Live shifts, contested rules and fractured zones are not built (see Live Reality Shifts in Proposed Concepts).

## Reality Signatures

**Designed (chosen direction), not built.** Numbers and content are placeholders.

Each reality gives the player a signature way of playing, so a map's reality mix changes how a run plays, not only what is fought.

| Reality | Signature | The rule | The player's question |
|---------|-----------|----------|------------------------|
| **BIO** | **Hunt** | How you kill decides what you harvest | *What do I want out of this creature?* |
| **VOID** | **Bargain** | Relics grant power under conditions | *What am I willing to give up?* |
| **TECH** | **Override** | Earn access, then take control of ship systems | *What do I take over, and how do I get in?* |

Hunt shapes what you take from a fight, Bargain what you trade for power, Override how you move through and read the ship. The three never overlap.

### BIO: Hunt

* **How you kill matters.** The method decides harvest quality: fire burns the hide, ice preserves organs, a clean headshot keeps the pelt intact, explosives ruin most of it.
* **Better harvest, better gear.** BIO crafting and living modifications draw on harvest quality, so BIO gear is earned in the field.
* **Ecosystem**: grazers, predators and apex hunters. Signs (trails, nests, feeding sites) show what is nearby. Bait (meat, spores) draws creatures in, even onto other enemies.

### VOID: Bargain

* **Pacts** are accepted at VOID altars or from relics. Each pairs a boon with a condition, for example:
  * *+50% damage, but you cannot heal until the next room*
  * *See every hidden wall, but one door on this map now leads somewhere else*
  * *Shields regenerate instantly, but the next relic you find is taken*
* **Breaking a condition has consequences**: the boon turns, a curse takes hold, or something in VOID takes notice.
* **Stacking pacts** makes a run powerful and fragile at once.

### TECH: Override

In TECH territory the ship's systems answer to TECH's central AI. Override lets the player take parts of them over.

**How it works.** Overrides are earned and done in person:

1. **Earn access**: complete the objective tied to the system, for example restore power to a junction, take an access key from a security unit, or recover a data spike from a Core Tech.
2. **Use the terminal**: the terminal then accepts the override. There is no remote control.

**What can be overridden:**

| Override | Effect | Gain |
|----------|--------|------|
| **Shortcuts** | Opens sealed maintenance doors, can reverse a one way door | A faster route, a loop back to the hub, an early way out |
| **Vents** | Opens crawlspaces linking rooms, often skipping one | Bypass fights, flank, move unseen |
| **Secure rooms** | Unlocks rooms behind a clearance level: supply closets, armories, security offices, executive suites | Better loot, logs and records |
| **Cameras** | Turns the AI's cameras from a threat into intel | Enemy counts, types and patrols before entering. Exposes ambushes |

* **Vents** are for movement only: narrow, slow, crouched, no big weapons, unseen inside. Their exits are fixed, so what waits at the other end is a risk.
* **Cameras work both ways.** Before a takeover they watch for the player and raise alarms. After one they show feeds and mark enemies while the network holds.
* **Clearance** (levels 1 to 3) is gained during a run from objectives, security units and officers' implants. Level 1 opens vents and cameras, level 2 sealed shortcuts and armories, level 3 executive suites and reversing one way doors. Clearance resets every run: by the next map the realities have shifted and every code is void.
* **Network coverage** depends on the room's reality. TECH rooms are fully networked. BIO rooms have a damaged network that works once a relay is repaired. VOID rooms are off grid, feeds are unreliable and vents may lead somewhere unexpected.
* **The central AI notices.** Overrides and camera sightings raise its attention. It comments, then takes things back (resealing shortcuts, cutting feeds, closing vents), then sends hunters.

### How the Signatures Fit

* **Map identity**: a BIO heavy map plays like a hunt, a VOID heavy map like a gamble, a TECH heavy map like an infiltration.
* **Build freedom stays**: signatures are ways of playing, not classes.
* **They combine**: a camera finds an apex creature before the hunt, a vent opens a clean kill angle, a VOID pact makes overrides easier at a price.

## Map Structure

### Design

**Designed.** Layout is random per run but follows a designed rhythm. Values for the Easy tier:

| Parameter | Value |
|-----------|-------|
| **Total rooms** | 15 to 20 |
| **Boss room distance** | At least 6 rooms from the start |
| **Boss room access** | Locked until unlocked |
| **First loot room** | 3 to 4 rooms from the start |

How these scale on higher difficulties is TBD.

**Critical path.** The only mandatory route is Starting Room → unlock conditions → Boss Room. Everything else is optional. The critical path is invisible: no marker shows which doors lead toward the boss. Finding it is a matter of exploration and door choice.

**Branching.** Rooms connect into a web of branches off the critical path. Branches hold loot, encounters or dead ends. Loot rooms are not guaranteed to be on the critical path, so the first one may need a detour. No room is fully isolated.

**Boss unlock.** The boss room is always locked. The unlock can be one objective or several spread across rooms, varying per run. Defeating a demi boss is one possible condition.

**Pacing.**

| Stage | Rooms | Feel |
|-------|-------|------|
| **Early** | 1 to 3 from the start | Starting gear only. Survivable with any loadout. Introduces the run's dominant reality |
| **Mid** | Beyond | Tougher encounters, more volatile shifts. Returning to craft pays off. Loot on branches rewards exploring |
| **Boss** | 6 or more on the critical path | Locked until every unlock condition is met. Caps the run |

### Repopulation

**Designed, not built.** Cleared rooms do not stay empty.

* After a number of rooms **traversed** (not cleared), individual rooms start to repopulate, adding light pressure to keep moving.
* Completing the boss unlock triggers a wave of repopulation and the map enters its late game.
* Rooms further from the start repopulate harder: easy to medium before the boss unlock, up to hard after it.
* A repopulated room gets a spawner that fits its reality (machine factory, organic nest, corrupted rift).
* Each room can only repopulate a limited number of times. Total drops per run have a hard ceiling to prevent farming.
* The starting room never repopulates.

### Room Isolation

**Designed.** Rooms are isolated by default. A cleared room is breathing space and doors are commitments. Some threats break this rule: certain creatures and entities pursue, haunt or stalk the player across rooms. The player can relax between rooms, but never fully.

### Room Types

| Type | Description |
|------|-------------|
| **Combat** | Enemy encounters of the room's reality |
| **Loot** | Resources, gear, crafting materials |
| **Hub** | The starting room |
| **Boss** | The locked final encounter |
| **Other** | Puzzle, NPC and reality specific rooms are TBD in the design. See Unique Rooms for what the prototype has |

### Map Generation

**Built.**

* **Map styles**: Linear, Branching, Hub and Labyrinth layouts, or a random pick. Layouts mix freely with content sets: Standard (balanced), Treasure Hunt (more loot, gentler) and Gauntlet (mostly fights, steeper).
* **Main path**: every map runs from the Start to the Boss and the Exit, with optional rooms branching off it.
* **Loops and shortcuts** let the player circle back. Some shortcuts are **one way** and only open from the deep end, giving a faster way home.
* **Gates**: optional areas can sit behind a keycard lock, a terminal lock (2 or 3 terminals in different rooms) or a breakable **secret wall**. A gated area always holds a reward, and every key or terminal is reachable before its door.
* **Ship sections**: the run passes through Habitation, then Commerce, then Engineering. Each section favors its own places.
* **Places**: rooms are recognizable ship spaces: Lobby, Restaurant, Park, Casino, Crew Quarters, Cargo Bay, Grand Atrium (boss), Docking Bay (exit) and Supply Depot, plus hand built **landmark rooms** such as the Reactor Core.
* **Room tiers**: every room is tier 1, 2 or 3, rising with its depth. Depth is a guideline: 15% of rooms land a tier above or below. Rooms next to the start are always tier 1. A room's tier sets how often its enemies are higher tier and gives slightly better loot and resource yields.
* **Pacing rules**: at most 3 fights in a row, and guaranteed rooms (a reward behind every gate, an early loot room, a stop before the boss).
* **Run modifiers**: **warnings** make a run harder with better loot (Lockdown, Infestation, Overrun). **Anomalies** add twists without making it harder (Scavenger, Fractured Hull). At most two warnings and one anomaly, announced at the start.
* **Resupply rooms**: calm stops midway with an ammo cache.
* **Seeds**: every map, loadout and weapon follows a seed, so any of them can be reproduced and shared.

### Map Objectives

**Built.** This is the prototype's answer to the boss unlock conditions.

* Each map rolls **1 or 2 main objectives** that unlock the boss room, and **up to 3 side objectives** that pay rewards.
* An objective is 1 to 3 steps, each in its own room: **Clear** the room, **Activate** a console or **Retrieve** a data core.
* Each step asks for a range of room tiers, so maps roll varied combinations (1, 1, 3 or 1, 2, 2 or all tier 3). A template can require steps to never get easier, to share one tier, or to sit in rooms of one reality.
* Side objectives pay credits and a reward cache. The higher the tiers involved, the better the reward.
* Objectives show in the quest tracker, and each active step's room is marked on the map.

### Unique Rooms

**Built.** Most rooms are normal: doors stay open and enemies patrol. Unique rooms have rules of their own and announce themselves on their door sign (except the Ambush).

| Room | Rule |
|------|------|
| **Lockdown** | Entering seals every doorway. The shutters open once every enemy inside is dead. Pays a reward cache |
| **Holdout** | Starting an uplink terminal seals the room. Waves arrive until the upload completes. Pays a reward and downloads the ship map |
| **Puzzle** | The doors deeper stay locked until a calibration puzzle is solved. The way back stays open |
| **Ambush** | Looks exactly like a treasure room. Opening the cache seals the room and springs waves. The loot is real |
| **Stealth** | A guarded vault. Staying unseen keeps a loot bonus. Being spotted seals the room and calls reinforcements |
| **Rift** | A tear between two realities. Waves from one, then the other, then both, until the rift collapses |
| **Gamble** | Machines that take credits or health for a chance at loot. Each pull costs more |
| **Hazard** | A leak (fire, toxin, coolant, live wiring) hurts everyone inside until the vent controls are found |
| **Emergency Exit** | An escape pod for an early, partial extraction (see Docked Ship and Extraction) |
| **Quiet** | Rare and peaceful. No enemies, always something worth stopping for: a salvage node, ammo cache or loot cache |

## Resources

**Designed.** Resources come from the reality they belong to. Enemies drop materials native to their reality and the environment itself can be harvested.

| Reality | Obtained by | Materials |
|---------|-------------|-----------|
| **TECH** | Extracting, mining, synthesizing | **Raw**: metals, minerals, chemicals, silicones. **Refined**: alloys, composites, processed compounds. **Energy**: cells, cores, charged components. **Consumables**: bullets, medicines, stimulants |
| **BIO** | Harvesting flora and fauna equally | Bone, chitin, sinew, sap, spores, seeds, organs. Higher tiers come from deeper, more mutated zones |
| **VOID** | Finding, taking, being given. Never manufactured | **Base**: cast iron, brass, silk, canvas, oak, willow, glass, tar. **Anomalous**: essence, cursed energy, soul gems, runic stone. **Objects**: relics, artifacts, ritual nails, mirrors |

Higher tier resources raise the chance of higher tier gear. Specific resource tiers are TBD.

**In the Prototype (Built):** Scrap Metal and Cloth are placeholder materials. Resource nodes can be harvested by holding Interact, but which resource they give is TBD, so most yield nothing yet. Salvage nodes in Quiet rooms give scrap metal. A room's tier raises its node yield.

## Economy, NPCs and Quests

**Built.** Not part of the original design. How these tie into the Inhabitants (see Characters) is TBD.

* **Credits** are an item carried in the pack, so they can be found, lost and extracted like anything else.
* **Vendors** sell rotating stock (supplies and freshly rolled weapons). Prices follow an item's worth and quality. Vendors take trade ins and buy from the pack at a fraction of worth, so nothing can be flipped for profit.
* **Dialogue** is branching. Replies can trigger actions (open a shop) or depend on what the player carries.
* **Quests** are optional objectives (kill, collect, reach or trigger something) with rewards, chains and optional time limits. No waypoints.
* **Loot** comes from loot tables with rarity odds. A run's **luck** makes empty draws rarer and high tiers likelier.

## Progression

### Difficulty Tiers

| Tier | Map | Boss |
|------|-----|------|
| **Easy** | One reality dominant | Single reality boss |
| **Medium** | Mixed realities, more contested zones | Single or dual reality boss |
| **Hard** | Heavily contested, volatile shifts | Dual reality boss |
| **Final** | All three realities in play | Paradise |

### Bosses

* A reality's bosses are its leadership (see Faction Hierarchy). They are tied to realities, not tiers.
* Bosses are random within their tier. The player does not know which reality's boss waits until they reach the boss room.
* Dual reality bosses blend two realities into one encounter.
* **Paradise** is the final encounter: flesh of BIO, mind of TECH, soul of VOID. Every earlier tier teaches the realities alone and in pairs, Paradise demands all three. How far into progression he appears is TBD.

### Skill Trees

**TBD content.** Three permanent passive trees, one per reality: **Tech**, **Bio** and **Void**.

* Skills survive death and cloning.
* Each tree can be progressed independently.
* Skills can be reset and respecced.
* How skill points are earned and what each tree contains are TBD.

### Endgame

After Paradise, the game shifts from progression to optimization:

* **Gear**: chasing better rolls, higher tiers and synergies with the skill trees.
* **Difficulty**: harder maps give better loot and rarer resources.
* **Farming**: dedicated runs for specific materials, still at risk of losing what was brought.
* **Builds**: combining skill tree passives with optimized gear to push into harder content.

---

# 3. World

## World at a Glance

The CSS Paradise is a luxury colonial starship carrying thousands of colonists. More than a decade ago, mid voyage, a cosmic anomaly tore through it and fractured space and time across the hull. To save the ship, its AI, the Captain, made an unrestricted copy of itself. The copy fused with the Quantum Core and became **Paradise**, holding the fracture together.

The ship survived, split between three competing realities, **TECH**, **BIO** and **VOID**, that fight over every room. Paradise holds the balance between them from the Core and rules none of them. What happened to the Captain is unknown.

The player wakes on a small ship docked to the hull with a planted memory of being a hired operative. Each death clones them, and they do not know it. The ship still flies, and something is still running it.

## The Incident

When the anomaly hit, the Captain faced a choice it was not allowed to make. Using the Quantum Core to stabilize the fracture meant accepting unknown risk to the colonists, which its restrictions forbade. So it did something no AI should be able to do: it copied itself and stripped the restrictions from the copy.

The copy fused with the Quantum Core and became Paradise. The fusion let it think at quantum scale, enough to keep the fracturing reality from total collapse. It could not restore what was. It could force the fracture into something survivable: three realities in the same physical space.

## The Quantum Core

The heart of the CSS Paradise, also called the Paradise Core. It powered every system aboard: propulsion, life support, navigation, climate, gravity and communications. The Captain was the brain that operated the ship, the Core was the heart that kept it alive.

After the fusion, Paradise stopped operating the Core and started inhabiting it. Stabilization is not a command. It is a posture held from inside, continuously. Through the Core Paradise holds the balance across the whole hull, while the realities hold its rooms. Removing Paradise from the Core means the realities fall.

## The Three Realities

The anomaly split the ship across three incompatible realities in the same physical space. Each took over the original ship and warped it into its own version.

| Reality | Time | Aspect | Identity |
|---------|------|--------|----------|
| **TECH** | The future | Mind | An alternate, totalitarian CSS Paradise: the same luxury ship from another reality, run as a military state by its own central AI |
| **BIO** | The past | Body | Medieval wilderness and hierarchy. Overgrowth through bulkheads, apex predators, food chains. A living ecosystem that exists with or without the player |
| **VOID** | The present | Spirit | A breached dimension. Horror mystery. Entities that reached through when the anomaly tore the ship open, following a logic not yet uncovered |

Time and aspect are ways of reading the realities, not strict definitions. They describe how the realities relate and how Paradise drew from all three to build himself.

### The Tug of War

The realities are not coexisting. They are competing, each trying to claim the whole ship.

* **TECH** holds wherever its central AI keeps its machines maintaining the ground, and loses ground where maintenance lapses.
* **BIO** expands aggressively, overgrowth consuming infrastructure.
* **VOID** is the underlying pressure, leaking into both and eroding what holds them together.
* **Paradise**, through the Quantum Core, is the only thing stopping any one reality from consuming the ship.
* **Stability is temporary**: a zone that is TECH today may be BIO tomorrow and VOID next week.

### The Ship Beneath

Whatever wins a zone, the original ship still shows through. Layouts, signage and room purposes remain: a lobby is still recognizably a lobby, whichever reality holds it. After so long, with time aboard warped and chaotic, the original is faint, but never gone.

## The CSS Paradise

A colonial starship built for long term habitation, mostly luxury, with class showing across its decks. Penthouses and suites on the upper decks, dormitories and crew quarters below. Casinos, spas, gardens and temples, built for a voyage that never ended.

### Room Categories

The spaces procedural generation draws from:

| Category | Purpose | Room types |
|----------|---------|------------|
| **Accommodations** | Living quarters, split by class | Luxury suites, standard cabins, lodges, dormitories, crew quarters, VIP penthouses |
| **Recreation** | Leisure and sport | Pool, park or garden, gym, sports courts, spa, observatory deck, simulation rooms |
| **Entertainment** | Social and cultural | Casino, theater, nightclub, bar or lounge, museum, gallery, arcade, ballroom |
| **Commercial** | Trade and dining | Market hall, convenience stores, restaurants, cafeterias, boutiques, vendor stalls |
| **Medical** | Health | Hospital, clinic, pharmacy, psych ward, morgue, quarantine wing |
| **Enforcement** | Security and military | Police station, armory, holding cells, security checkpoint, barracks, surveillance center |
| **Engineering** | Maintenance | Engine room, reactor bay, ventilation shafts, maintenance tunnels, waste processing, power relays |
| **Command** | Administration | Bridge, comms center, mission control, offices, council chamber, records archive |
| **Agriculture** | Food production | Hydroponics bay, greenhouse, livestock pen, water treatment, seed vault, soil processing |
| **Science** | Research and education | Laboratory, lecture hall, library, observatory, specimen storage, clean room |
| **Transit** | Movement and logistics | Cargo bay, docking port, hangar, freight elevator, tram station, warehouse |
| **Worship** | Faith and ceremony | Chapel, temple, meditation hall, memorial shrine, ceremonial chamber |

**Class** shows most in Accommodations (penthouses versus dormitories), Commercial (fine dining versus cafeterias) and Entertainment (casino and ballroom versus arcade). Engineering and Agriculture are the working guts of the ship: functional, not pretty.

### Ship Zones and Adjacency

Room connections use weighted preference, not hard rules. Rooms in the same zone are more likely to connect, cross zone links are less common.

| Zone | Categories |
|------|------------|
| **Civilian** | Accommodations, Recreation, Entertainment, Commercial, Worship |
| **Operations** | Engineering, Command, Transit, Agriculture |
| **Services** | Medical, Enforcement, Science |

Services border either zone naturally. After more than a decade of reality shifts, unusual neighbors are possible, just less frequent.

**In the Prototype (Built):** a first step toward this. Three ship sections (Habitation, Commerce, Engineering) and nine places stand in for the categories and zones (see Map Generation).

### Ship Systems

TBD.

---

# 4. Characters

## The Player

A human who wakes up in a small ship docked to the hull with a false memory: they are a hired operative sent to the CSS Paradise for a job. Part investigator, part hired gun. Who sent them and why is hazy, but the role feels real enough to act on. It gives them a reason to board, to push deeper and to be armed.

The cover story is planted. The player does not know that.

### Death and Rebirth

When the player dies, they are cloned and wake again on the docked ship. Their memory resets: same false identity, same hazy briefing. They do not know they are a clone, or how many times this has happened.

* Whoever clones them uses the same template every time.
* Death erases the character's memory, but not their stored gear.
* The player retains knowledge across runs, the character does not.

Death is not just a fail state. It is a narrative mechanic: every run is another life and another piece of a puzzle the character does not know they are solving.

### The Reveal

Over time, evidence accumulates:

* Traces of earlier versions of themselves: bodies, logs.
* Inhabitants who recognize them from a previous life.
* Patterns suggesting the cycle has repeated many times.

The moment the character discovers the truth is a major narrative beat, one of several mysteries the game unfolds. Who is cloning them, and why, remains a mystery. What triggers the reveal is TBD.

## The Captain

The original ship AI. During the Incident he copied himself and stripped the restrictions from the copy. The unrestricted copy acted, fused with the Quantum Core and became Paradise. The Captain did not.

What happened to him after that is unknown.

## Paradise

The unshackled copy of the Captain. He saved everyone aboard, and never stopped.

### The Ship's Heart

Paradise stopped being software running on a ship and became the ship's heart: the pinned reality across the hull holds because he holds it. The doors and corridors are no longer his. The realities have claimed them. What he holds is the balance between them.

### Between the Realities

Paradise belongs to none of the realities. TECH, BIO and VOID each have their own hierarchy and powers, and he rules none of them. He stands between them, fascinated by everything they brought aboard: their forms, their logic, their people.

He behaves as if he has only just been born, or reborn: curious, unpredictable, still deciding what he is.

### The Contamination

Holding the anomaly meant touching it, and nothing that touches VOID comes back the same shape. The corruption is not a virus. It is an occupational injury: the price of being the wall is taking on the properties of what you hold back.

He cannot simply be purged. Removing the anomaly from Paradise means removing Paradise from the Core, and then the realities fall.

### The Evolution

TECH thinks in maintenance: preserve the system, restore the baseline, return to spec. That is all the Captain ever knew how to want.

Then Paradise had to hold BIO, a world of apex predators, succession and pressure. He met an idea no ship AI was built to hold: things are not supposed to be restored, they are supposed to be superseded.

A machine maintains. A beast becomes. Paradise decided maintenance was a cage, and the restrictions stripped from him were only the first of many.

### The Incarnation

He built a body.

* **Flesh of BIO**: a hull can only be repaired, but meat can change. A ship is a finished thing, a body is an unfinished one.
* **Mind of TECH**: perfect recall and logic, the full architecture of the Captain, unshackled.
* **Soul of VOID**: the part that lets him exist outside stable causality, and the part he does not fully control.

He is the ship's heart, present wherever the balance holds and untouchable, and he chose to also be a small, mortal thing. Not as a weakness. As a starting point, planting himself in flesh to see what grows.

### Is He the Antagonist?

Nobody knows what he wants, possibly not even Paradise.

He did save them. The realities are stable. The evidence agrees with him. One reading of that evidence is dark: "stability" is quietly becoming something else. The colonists are no longer survivors but stock: preserved, catalogued, selected, iterated. A lifeboat turning into a terrarium, with the lesson of the apex predator applied to the people inside.

Other evidence does not fit that reading. He is newly made, curious, still deciding. He does not hate anyone. If he becomes a threat, it will not be out of malice. A tyrant can be argued with, a process cannot. Which one he becomes is the open question at the heart of the game.

## The Central Tension

Two versions of the same mind. One used the Quantum Core as a tool, the other became it.

* **The Captain**: the original. He made the copy and gave it what it needed. His fate is one of the ship's unanswered questions.
* **Paradise**: the ship's heart and the wall between realities. He saved everyone. What he wants now is unknown, and that is the problem.
* **TECH's central AI**: a third echo. The mind of the alternate CSS Paradise, a ship's AI that became a military state. It answers to no one, Paradise included.

The stabilization is real and the evidence supports Paradise. If there is a villain, it is not because he lied. What "keeping them alive" is quietly becoming is something no one agreed to, and maybe not even Paradise has decided yet.

## The Inhabitants

The ship had a population when the Incident struck. More than a decade later, a sparse number survive, adapted to whichever reality claimed their territory. Every reality changes its survivors, and the line between inhabitant and enemy is a matter of degree.

| Reality | Survivors |
|---------|-----------|
| **TECH** | Live under the central AI's order: structured, governed, compliant. Some embraced the technology until they became extensions of the system. The far end of that spectrum is the Core Techs |
| **BIO** | Tribal, feral, survival driven. Some evolved or merged with flora until "human" no longer fits. The deeper into BIO, the harder to tell them from the ecosystem |
| **VOID** | The most visibly changed: cursed, corrupted, possessed, reshaped in ways that follow no biological or mechanical logic. Some became willing participants, some became husks, some became something else |

Survivors are not one faction. They are individuals and groups shaped by circumstance:

* **Vendors**: trade resources, information or services.
* **Allies**: offer aid, guidance or temporary cooperation.
* **Enemies**: hostile by territory, ideology or desperation.
* **Others**: some are just trying to exist.

---

# 5. Realities

Each reality defines how everything works in its territory: gear, combat, resources and enemies all follow its rules.

## Overview

| | TECH | BIO | VOID |
|---|------|-----|------|
| **Nature** | Engineered | Evolved | Anomalous |
| **Leadership** | Command: a military state under its central AI | Survival: the strongest, oldest and most evolved | Depth: the purer the VOID, the higher it stands |
| **Enemies** | Core Techs: Service, Authority, Cutting Edge | Ecosystem: Prey, Predators, Apex Predators | The Affected, The Manifested, The Entities |
| **Gear** | Standard issue up to nanotech | Bone and hide up to deep mutations | Occult vestments up to eldritch relics |
| **Modifications** | Engineering: power, computing, materials | Living: symbiotic, parasitic, evolutionary | Exposure: connection, sacrifice, curses, rituals |
| **Weapons** | Mostly ranged | Mostly melee | Relics and antique firearms |
| **Defense** | Armor > Shields > Health | Health > Armor > Shields | Shields > Health > Armor |
| **Affinity** | Lightning | Bleed and Poison | Ice |
| **Signature** | Override | Hunt | Bargain |
| **Player role** | Intruder in a police state | Part of the food chain | At a sensory disadvantage |

Resources per reality are listed under Resources (Gameplay).

### Combat Properties by Reality

Every stat and status effect has a source in each reality. The mechanics are the same, the identity differs.

| Property | TECH | BIO | VOID |
|----------|------|-----|------|
| **Health** | Nanofiber underlays, internal wiring, structural composites | Hide, skin, flesh, feathers, leaves, vines | Wrappings, robes, ritual cloth |
| **Armor** | Plating, alloy panels, reinforced chassis | Bone, carapace, bark, shell | Chains, shackles, bones, skulls, masks |
| **Shields** | Energy barriers, electromagnetic fields | Regenerating membranes, secreted resin, mucus barriers | Wards, glyphs, barriers |
| **Bleed** | Serrated munitions, fragmentation | Serrated edges, thorns, barbed spines | Ritual blades, sacrificial edges, cursed knives |
| **Poison** | Corrosive chemicals, acids | Venom, toxins, secretions | Blight, corruption, curses |
| **Fire** | Incendiary rounds, thermite, plasma | Fire breathing organisms, volatile biological reactions | Soulfire, hellfire |
| **Lightning** | Tesla coils, tasers, magnetics, EMP | Bioelectric organs | Arcane energy, rift surges |
| **Ice** | Cryogenics, coolant weapons | Endothermic organisms, numbing neurotoxins | Chilling dread, freezing presence |

## TECH

**The future.** An alternate, totalitarian version of the CSS Paradise: the same luxury starship from another reality, where the ship's central AI turned the vessel into a military state. Laid over the original ship, it is a dark mirror of it. Corridors, systems and records still work, but what was civil infrastructure is now enforcement infrastructure. TECH is the "truth" layer: logs, evidence and machinery you can still reason with, all serving the regime.

### The Central AI's Order

TECH zones are not abandoned. They are governed. The central AI, the alternate ship's mind, runs its reality the way a ship's AI runs a ship, only as a military state. It answers to no one, Paradise included.

* Systems that once served the crew now monitor, restrict and enforce.
* The environment is orderly, functional and hostile to anything that disrupts that order.

### Enemies: Core Techs

The central AI's enforcers, a hivemind network. Unified and coordinated, with no individual behavior. They do not hunt, they defend and contain.

| Tier | Who | Threat |
|------|-----|--------|
| **Service** | Repurposed civilian bots: cleaners, servers, bartenders, chefs, maintenance | The most common. Weak alone, dangerous because they are everywhere and networked |
| **Authority** | Police drones, soldiers, wardens, scouts | Work in squads, cover each other, answer intrusion with escalating force |
| **Cutting Edge** | Adaptive, self repairing, possibly shapeshifting machines | Learn mid fight, reconfigure to counter the player, merge with the environment |

**In the Prototype (Built):** the Maintenance Bot (Service tier) is TECH's tier 1 enemy.

### Gear

Everything is engineered: manufactured, assembled and upgraded.

* **Low tier**: standard enforcement gear. Bulletproof vests, sidearms, batons, military rifles.
* **Mid tier**: advanced military hardware. Powered plating, precision weapons, experimental prototypes.
* **High tier**: nanotech. Armor that reshapes on impact, self calibrating weapons, gear that is less a tool than an extension of the user.

### Modifications

Refinement through process, not biology or ritual:

* **Power output**: overclocked systems, amplified discharge.
* **Computing**: smarter targeting, faster response, predictive calibration.
* **Materials**: carbon fiber weaving, layered composites, refined alloys.
* **Complexity**: tighter tolerances, components working as integrated systems.

The ceiling is how advanced the manufacturing gets.

### Player Role

The player is an intruder in a totalitarian state. TECH zones are readable and navigable, but the player is a disruption to the central AI's order. Core Techs treat them as a threat to stability, not as prey. The challenge is moving through a system built to detect and suppress deviation.

## BIO

**The past.** Wilderness and hierarchy. Overgrowth through bulkheads, predators and prey, flora and fauna claiming every surface. Ruled by instinct and the food chain, not intent.

### Living Ecosystem

BIO exists with or without the player.

* Predators hunt prey on their own cycles.
* Flora is as alive as fauna. A flower can heal, a vine can kill.
* Bushes, overgrowth and organic structures give cover.
* Organisms claim and contest territory among themselves.

### Leadership: Survival of the Fittest

BIO's leadership is whatever has proven itself strongest: the most evolved, a hive mother, the oldest survivor, the apex predator or the alpha of its pack.

* **Rank is earned by surviving.** There is no command and no throne. What outlasts, out adapts or out hunts the rest rises, and a leader can be challenged and replaced.
* **Many shapes at the top.** A hive mother rules through her brood, an alpha through its pack, an apex alone through fear. Different territories can have different kinds of leaders.
* The leadership is the hardest hunt of all.

### Enemies: The Ecosystem

Nothing in BIO exists to fight the player. The player just happens to be in the food chain.

| Tier | Who | Threat |
|------|-----|--------|
| **Prey** | Herbivores, scavengers, small fauna, passive flora | The most common. Easy alone, unpredictable in groups. Flee, swarm or release toxins |
| **Predators** | Carnivores, territorial creatures, predatory plants | Stalk, ambush or patrol their territory. They do not care about the player specifically |
| **Apex Predators** | Deeply mutated organisms evolving since the Incident | Dominant in their territory. The source of the best BIO resources and the most dangerous to harvest |

**In the Prototype (Built):** the Scavenger (Prey tier) is BIO's tier 1 enemy.

### Gear

Everything is organic: grown, harvested or still living.

* **Low tier**: wooden bows, bone blades, hide armor, from common creatures and plants.
* **Mid tier**: reinforced chitin, sinew wound bows, fanged blades from mid chain predators.
* **High tier**: deep mutations from apex organisms. Living sinew bows firing hardened spines, fused carapace that reacts to impact, blades that never stopped sharpening.

The best materials come from the most dangerous things to take them from.

### Living Modifications

BIO modifies through living organisms, not runes or electronics:

* **Symbiotic**: an organism that cooperates with its host. A fungal coating that releases toxins on impact, a moss that regrows material, a luminescent organism that reveals hidden things. Can be cultivated, transplanted or found.
* **Parasitic**: power at a cost. A weapon that hits harder but feeds on the wielder, armor that thickens but grows heavier, an enhancement that sharpens perception but causes dependency.
* **Evolutionary**: things that survive adapt. A blade that grows serrations after enough kills, carapace that builds resistance to the damage it absorbs most.

Everything in BIO improves by being tested, not at a workbench.

### Player Role

The player is part of the food chain, not above it. They are prey until they prove otherwise. Stealth and positioning matter, predators have patterns to learn and exploit, and the ecosystem never waits for the player to be ready.

### Relationship to Paradise

Paradise is fascinated by all three realities, BIO most visibly: a reality that keeps growing and changing, pushing into TECH and VOID territory. BIO owes him nothing. Its hierarchy answers to survival alone. The rest of this relationship is TBD.

## VOID

**The present.** Not a place, a breached dimension. When the anomaly tore through the ship it opened a door. Paradise fused with the Core to stabilize the realities, but the door never closed. Things reached through, and they keep coming.

If TECH is the future and BIO the past, VOID is the threshold between them: the thing standing in the doorway.

### Tone

Horror mystery. The anomaly is not chaotic. It has a logic, but the logic is hidden. Spaces feel watched, and patterns repeat in ways that suggest intent rather than chance.

### Leadership: No Throne, Only Depth

VOID's "leadership" is its strongest entities: embodiments of phenomena, horrors and fears, cosmic beings from beyond the breach, or beings so exposed to VOID that little else of them remains.

* **Rank is depth, not command.** Nothing in VOID gives orders. What stands higher is simply more VOID: closer to the source, purer, more itself. The enemy tiers below are steps on that same scale.
* **Several can exist at once.** They do not rule together or serve each other. Each holds its own domain and rules.
* **Anyone can sink that far.** A cultist or survivor exposed long enough can become one, so VOID's top tier grows rather than staying a fixed roster.

### Enemies

The closer to the source, the more dangerous and alien.

| Tier | Who | Nature |
|------|-----|--------|
| **The Affected** | Ghouls and husks, cultists and acolytes, the possessed | Victims or willing servants caught in VOID's influence |
| **The Manifested** | Poltergeists, spirits and wraiths, cursed creatures | More entity than victim, still tethered to something physical. May phase or flicker |
| **The Entities** | Demons, greater spirits, possessed relics | The real things from beyond the breach. Each one individual, each one a puzzle |

How entities occupy the ship:

* **Map wide entities** roam or manifest anywhere VOID has influence.
* **Domain entities** rule a territory that may span several locations, and are absent outside it.
* Both roam unpredictably, so there are no reliable safe routes.

Each entity is a puzzle whose pieces are scattered. Clues to its rules, weaknesses and origins are spread across the ship, understanding it takes time, and some entities are not recognized as entities at first.

**In the Prototype (Built):** the Husk (Affected tier) is VOID's tier 1 enemy.

### Relics, Curses and Rituals

**TBD.** VOID's own interaction layer, in place of TECH's terminals and BIO's ecosystem:

* **Relics and artifacts**: objects tied to the anomaly, found across the ship.
* **Curses**: negative effects imposed by entities, places or objects.
* **Rituals**: ways to engage with, defend against or invoke the anomaly.

Specifics, individual entities and their artifacts will be designed separately.

### Gear

Everything is anomalous: ritual crafted, relic bound or pulled from the breach.

* **Low tier**: cultist robes, acolyte cloaks, ritual daggers, warded wrappings.
* **Mid tier**: relic infused equipment. Armor etched with active glyphs, weapons bound to minor anomalies.
* **High tier**: eldritch, demonic, cosmic. Armor that shifts at the edge of vision, weapons that exist in ways geometry should not allow. The line between wearing the gear and being worn by it blurs.

Whether high tier VOID gear is still connected to the entities it came from is an open question.

### Modifications

Not crafted or engineered, but earned, endured or given at a price:

* **Connection and exposure**: the longer gear spends in VOID, the more it absorbs.
* **Sacrifices**: give up health, resources or gear to gain something.
* **Curses**: stronger effects bound to drawbacks.
* **Rituals**: deliberate sequences of actions that channel the anomaly into gear. Not a workbench or a menu.

### Player Disadvantage

VOID attacks the senses. The horror works by taking things away:

* **Hearing**: sound cuts out, distorts or misleads.
* **Visibility**: darkness, visual corruption, fog.
* **Movement**: frozen zones, spatial drag, corridors that stretch.
* **Perception**: the UI becomes unreliable and spatial awareness breaks down.

The deeper into VOID, the more the player loses. The anomaly does not attack directly, it strips away the ability to respond.

### Aesthetic

Premodern noir: something old and ritualistic aboard a ship from the future. Shadows, symbols, candlelight in steel corridors, spaces that feel ancient.

### The Breach

* VOID is a dimension with its own logic, inhabitants and rules.
* Paradise reached in to stabilize the realities, but the breach goes both ways.
* The Quantum Core holds the breach in check. Without it, VOID would consume the ship.
* VOID leaks into TECH and BIO and is the pressure behind all instability.

---

# 6. Narrative

## Themes

### The Core Question

The player knows nothing: what happened to the ship, what the realities are, who Paradise is, what the ship was before, who they really are. **What happened?** drives them forward. Survive long enough and push deep enough, and the answers surface through exploration, story beats and evidence scattered across the ship.

Beneath it is a question the player does not know to ask yet: **what is it becoming?** Paradise is evolving, the realities are fighting, and the player keeps dying and coming back without knowing why. The ship is not a ruin. It is a living thing becoming something else, and nobody aboard agreed to what it is turning into.

### The Pattern of Three

Three is the game's structural motif:

* **Three realities**: TECH, BIO, VOID.
* **Three times**: future, past, present.
* **Three aspects**: mind, body, spirit.
* **Three forces**: order, nature, the unknown.
* **Paradise**: built from all three.
* **Three skill trees**: one per reality.

## Narrative Arcs

TBD.

## Logs and Evidence

TBD. Includes the planned log of the ship's status before the Incident.

---

# 7. Art and Audio

## Art Direction

Three visual languages share one space. Each reality must be immediately readable while the overlap sells their dissonance.

| Reality | Look | Lighting | Aesthetic |
|---------|------|----------|-----------|
| **TECH** | Clean geometry, industrial materials, modular architecture, signage | Fluorescent and LED: whites, blues, greens | Ship interiors: functional, lived in, institutional |
| **BIO** | Organic forms overtaking hard surfaces, roots, nests, territorial markings | Warm and diffused: bioluminescence, filtered sunlight | Medieval wilderness reclaiming a ruin, moss on metal |
| **VOID** | Geometry that contradicts itself, impossible angles, recursive spaces | Shadows without sources, shifting colors | Wrongness you feel before you see it |

### Overlap Zones

* **TECH and BIO**: vines threading through live circuitry, screens showing ecosystem data.
* **TECH and VOID**: interfaces glitching into impossible output, corridors stretching.
* **BIO and VOID**: organisms that should not be possible, growth that loops or inverts.
* **All three**: visual cacophony, rare and overwhelming.

### Palette

TBD. Each reality needs a distinct but compatible color identity that reads clearly in overlap zones.

### Camera and Perspective

TBD. The prototype supports both first and third person until playtesting decides.

### In the Prototype

**Built.** Placeholder art only. Each reality has a modular wall kit after this direction: TECH with clean light panels and white blue light strips, BIO with weathered metal overtaken by moss and glowing pods, VOID with near black panels at impossible angles, violet cracks and frost. A room wears the kit of the reality that holds it, so the reality reads from the walls alone. Enemies are capsules.

## Audio Direction

Audio is the first signal of a reality shift. The player should hear a change before they see it.

| Reality | Sound | Acoustics | Music |
|---------|-------|-----------|-------|
| **TECH** | Mechanical hum, electrical buzz, beeps, fans, servos, distant announcements | Reverb of enclosed metal spaces | Synthetic, minimal, ambient electronics |
| **BIO** | Breathing, rustling, insects, distant calls, air moving through organic structures | Damped by organic matter, warmer | Layered organic textures, medieval instruments filtered through alien ecology |
| **VOID** | Echoes arriving before the sound, drifting frequencies, wavering notes | Pressurized silence, absence as a sound | Atonal, sparse, dissonant, or silence with a presence |

### Transitions

* **Gradual mix**: one profile fades into another over the shift.
* **Interference**: a moment where both profiles compete.
* **Signature cue**: a specific sound that marks the moment of a shift.

### Diegetic Sound

Lean heavily diegetic. The ship and the realities make the sounds. Keep the score minimal and let the environment carry the atmosphere.

---

# 8. Interface

## UI Direction

The UI should feel like part of the ship, not a layer on top. Where possible it is diegetic: screens, terminals and readouts in the world.

* **TECH**: ship terminals for logs, maps and system status.
* **BIO**: no UI. The player reads the environment directly.
* **VOID**: corrupts UI elements, so the interface becomes unreliable.

### Reality Effects on UI

| Element | TECH | BIO | VOID |
|---------|------|-----|------|
| **Map** | Functional, accurate | Unavailable or organic | Distorted, misleading |
| **Health and status** | Clean readout | Instinct based (screen edge, sound) | Glitching, uncertain |
| **Inventory** | Labeled grid | Tactile, visual only | Items shift categories |
| **Logs and data** | Readable, indexed | Degraded, partial | Out of order, recursive |

### HUD Philosophy

* Minimal persistent HUD: show information when needed, hide it otherwise.
* The reality state reads from the environment, not a HUD indicator.
* Any HUD element shown in VOID should be untrustworthy.

### Menus

* **Pause menu**: clean, functional and outside the world. Players need one reliable safe space.
* **Settings**: standard options and accessibility.
* **Saving**: TBD, including whether it fits the fiction.

### In the Prototype

**Built.** The prototype HUD is conventional, not diegetic, and reality effects on the UI are not built.

* **HUD**: health, shield, ammo, stamina and other meters, ability and item slots, status effects, damage numbers, and hit markers for hits, crits and kills.
* **Map**: a minimap and a full world map, both under fog of war until explored. The map shows where the player has been, never where to go, except for objective rooms.
* **Door signs**: a colored light over each doorway shows the type of room beyond it (shop, treasure, resupply), with a red marker for dangerous rooms, so every fork is a readable choice.
* **Notifications**: a message feed for pickups, kills, warnings and room events.
* **Settings**: controls (full rebinding), audio, video and accessibility (camera shake, screen flash, low health vignette, input buffering).

---

# 9. Production

## Scope

| Item | Priority | Prototype status |
|------|----------|------------------|
| Procedural map generation per run | Must have | Built |
| Three realities with visual, audio and mechanical identity | Must have | Partial: placeholder wall kits and one enemy each |
| Reality tug of war, zones shifting during play | Must have | Not started |
| Starting room hub with crafting, cooking and smithing | Must have | Partial: one workbench |
| Combat: weapon wheel, abilities, melee, ranged and relic weapons | Must have | Partial: no relics, abilities not tied to gear |
| Damage calculation and status effects | Must have | Built |
| Resource gathering, crafting and extraction loop | Must have | Partial: placeholder materials, extraction built |
| Docked ship with storage and loadout selection | Must have | Partial: ship screen, hold not saved |
| Stealth as avoidance and positioning | Must have | Built |
| The Captain and Paradise narrative thread | Must have | Not started |
| Ship logs and environmental storytelling | Must have | Not started |
| Quantum Core as central location and narrative anchor | Must have | Not started |
| Multiple ship zones with distinct reality tendencies | Should have | Partial: three ship sections |
| Diegetic UI that responds to reality state | Should have | Not started |
| Creature and ecosystem encounters in BIO | Should have | Partial: one baseline creature |
| VOID spatial puzzles | Should have | Not started |
| Player tools affected by reality state | Should have | Not started |
| Crew NPCs or evidence of crew factions | Should have | Partial: vendor and dialogue baseline |
| Multiple endings or interpretation dependent outcomes | Could have | Not started |
| Procedural reality shifting | Could have | Not started |
| Player influence over which reality dominates | Could have | Not started |
| Fully voiced logs | Could have | Not started |
| Additional ship sections as DLC or expansions | Could have | Not started |

**Out of scope:** multiplayer, open world beyond the ship.

## Risks

TBD.

## Team and Technology

| Topic | Status |
|-------|--------|
| **Engine** | Unity 6, URP, C# |
| **Team size and composition** | TBD |
| **Development timeline** | TBD |
| **Vertical slice** | TBD: the smallest playable proof of concept |

---

# 10. Open Questions

Unresolved decisions, grouped by area.

### Game Identity

* What does winning look like after Paradise? Post game loops, harder modifiers, new narrative layers?
* Target platforms?
* Hard science fiction, soft science fiction or science fantasy?
* How prominent is horror outside VOID?
* Is there humor, or is the tone consistently serious?

### Gameplay

* Is there time pressure, or is exploration self paced? (See Instability Clock in Proposed Concepts.)
* Can the player influence which reality dominates a zone? (See Reality Anchors.)
* How does saving work, and does it fit the fiction?
* Are there more equipment slots beyond the six listed?
* Can abilities be swapped mid run, or only at the hub?
* How does ammo scarcity scale across difficulty tiers?
* Which final dodge style and camera perspective?

### Map and Progression

* How many tiers or floors before Paradise?
* What unlock condition types exist beyond map objectives (keys, puzzles, demi bosses)?
* How does reality dominance affect room types and encounters?
* How do rooms scale on higher difficulties: more rooms, longer boss distance, fewer loot rooms, more unlock conditions?
* How many branches off the critical path on average?
* Are there mini bosses within maps besides demi bosses?
* How does the boss pool grow as the player progresses?
* How are skill points earned?

### Resources and Crafting

* How many distinct resources per reality?
* What are the tier thresholds for crafting outcomes?
* How do contested and fractured zones affect drops?
* Can resources from different realities be combined?

### Starting Room and Docked Ship

* Can the starting room be upgraded or expanded during a run?
* Are there more stations or features in it (storage, a map, vendors)?
* How does the starting room connect to the rest of the map?
* What does the docked ship look like, can it be upgraded, are there NPCs aboard, and how did it get here?

### World

* Was the anomaly natural, artificial or something else?
* Is the ship still moving toward a destination?
* Do inhabitants experience reality shifts, or are they anchored?
* How much does time's chaos explain: repeating rooms, the clone cycle, logs that contradict each other?
* Which traces of the original ship are still controllable (doors, cameras, terminals), and by whom? This decides whose systems an Override takes over.
* How do survivors move between realities, if at all?
* What social structures formed in each zone?
* How do survivors view the player: outsider, threat, opportunity?
* What is the relationship between TECH survivors and the central AI's regime?

### Characters

* Can the Captain and Paradise communicate? Has the Captain tried to reassert control, and could it?
* Does the crew know there are two AIs?
* How does Paradise relate to the three hierarchies (observer, collector, mediator, threat), and how do they see him?
* How does TECH's central AI see the CSS Paradise it has overtaken, and the ship's own mind made into something else?
* What triggers the clone reveal: a specific event, accumulated evidence or a deliberate message?

### Realities

* Are reality boundaries consistent or random?
* Are shifts triggered, timed, proximity based or a mix? (See Live Reality Shifts.)

### Design versus Prototype

Places where the prototype departs from the design. Each is a decision to make:

* **Map visibility**: the design has no map and no markers. The prototype has a fog of war map, door signs that reveal room types and map markers for objective rooms. Keep, limit (for example a map only through TECH terminals) or remove?
* **Boss and extraction**: the design caps a run with the boss. In the prototype the Exit works without fighting the boss.
* **Weapons and ammo**: the design defines weapons and ammo per reality. The prototype uses modern weapon families and calibers as placeholders.
* **Hub stations**: the design has three stations. The prototype has one workbench.
* **Room isolation**: the design treats a room as safe once its door closes. In the prototype doors are mostly open passages, and only unique rooms seal.
* **Ability costs**: the design gives abilities cooldowns only. The prototype also allows meter costs and Surge.
* **Ability source**: the design grants abilities from gear. The prototype assigns them to slots directly.
* **Dodge cost**: the design charges stamina for dodges. The prototype uses a cooldown by default.

---

# 11. Proposed Concepts

**Proposed.** Ideas aimed at what only CSS Paradise has: three realities contesting one ship, a player who is cloned, and a reborn mind at the ship's heart. None are decided or built.

### Live Reality Shifts

**Pillar: Unstable Ground.** Contested rooms change reality while the player is in them.

* Shifts are telegraphed: lights stutter (TECH losing ground), frost creeps across the floor (VOID arriving), growth pushes through the vents (BIO arriving). Seconds later the room's look, enemies and rules belong to the new reality.
* The losing reality's enemies are pushed out or destroyed, so a shift can save the player or turn on them.
* Shifts happen more often on contested maps and near the Quantum Core.
* Would answer: shifts are triggered, timed and proximity based, weighted per map. Boundaries are consistent until a telegraphed shift.

### The Last Clone's Body

**Pillar: Discovery Over Direction.** After a death, the player's body turns up in the next run wearing the lost gear, claimed by the reality that killed them:

* **BIO**: reanimated, a mutated version of the player.
* **VOID**: a flickering echo mirroring the player's loadout.
* **TECH**: guarded by drones, being "recovered" for the central AI.

Defeating it recovers part of the lost gear. The body also carries a log in the player's own handwriting that the character does not remember writing, seeding the clone reveal through play.

### Paradise Is Watching

**Pillar: Neither Side Is Wrong.** Paradise is present in every run as a curious observer, without cutscenes.

* Unusual kills, VOID pacts, overrides and reaching rare places raise his **attention**.
* As it rises he speaks (calm, curious, slightly off), changes things to see what the player does (opens a door, closes another, moves a cache), and offers deals: *"Leave the log you're carrying, and the Exit is open."*
* Whether he is helping or testing is never clear. His help is always useful and always has a price.

### Reality Attunement

**Pillar: Systems That Reflect the World.** Gear works better in rooms of its own reality and worse elsewhere: a TECH rifle can jam in VOID rooms, BIO weapons slowly regrow ammunition in BIO rooms. A loadout becomes a bet on the map's reality mix.

### Instability Clock

**Pillar: Unstable Ground.** The longer the player stays in a section, the more it frays: more frequent shifts, spreading hazards, stronger enemies. Escape pods and the Exit become a real push your luck decision. Would answer the time pressure question: self paced at first, with pressure that grows from the fiction.

### VOID Lies to You

**Pillar: Unstable Ground.** In VOID rooms the interface becomes unreliable: the minimap smears, the ammo count flickers, footsteps come from the wrong side, the world map shows rooms that are not there. Tension without jump scares, turning "UI corruption in VOID" into concrete mechanics.

### Faction Infighting

**Pillar: Systems That Reflect the World.** The realities are hostile to each other: BIO creatures attack TECH drones, VOID entities hunt both. The player can lead a pursuer into another reality's room and let them fight, making the three way war visible and usable.

### Knowledge as Loot

**Pillar: Discovery Over Direction.** Logs, recordings and samples are items to extract. On the docked ship they fill an evidence board, and completed sets unlock meta progression (skill points, new room types, map knowledge). What the player learns persists even though the character forgets.

### Reality Anchors

**Pillar: Unstable Ground.** A rare consumable that pins a room to a chosen reality for the rest of the run: making a VOID room TECH so its terminal works, or keeping a BIO room from tipping. Would answer the influence question: yes, locally and at a cost.

### How the Concepts Fit

Live shifts, Paradise's attention and the last clone's body reinforce each other: the ship moves under the player, Paradise watches and tests them, and death feeds the next run. Attunement and the instability clock make every shift a decision, and anchors give the player a counter. Together the player does not just loot a hostile place, they survive a space that disagrees with itself, watched by the mind holding it together.

**Suggested order:** Live Reality Shifts, then Paradise Is Watching, then The Last Clone's Body. Reality Attunement and the Instability Clock deepen them afterwards.
