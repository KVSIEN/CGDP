# Room Generation Rules

How the colony ship's rooms are generated, and the plan for getting every room there. The
ship is a luxury liner big enough to hold a whole colony, with three realities (BIO, VOID,
TECH) blended into it. Each section below is a rule the generator follows, or a rule it will
follow once its roadmap phase is done. The [Roadmap](#roadmap) at the end shows the status of
each phase.

The main principle is: **build the ship first, then let the realities change it.** Every room
has an ordinary cruise-ship version. The reality that holds it changes that version in a
fixed, recognisable way.

---

## 1. Layers

Each layer decides one thing and nothing else. A rule that seems to belong to two layers is
two rules.

| Layer | Decides | Data |
|---|---|---|
| **Function**: what the room is on the ship | Size, floor plan, layout pattern, anchor and furniture | `RoomFunction` (`Data/Level/Functions/`) |
| **Ship side**: who the room is for | How large, ordered and decorated it is. Back-of-house doors | *Phase 4* |
| **Reality**: who holds it now | Shape bias, size scale, extra structure, dressing swaps, how full it gets, priorities | `FactionDesign` (`Data/Level/Designs/`) |
| **Content**: what you do there | Cover, concealment, enemies, loot, objectives | `RoomContentRule` plus the dressings (`Data/Level/Dressing/`) |
| **Checks**: did it work | Doorways fit, all floor reachable, nothing came up short, priorities met | Room Lab (`Window/CGD/Room Lab`) |

A casino is always a casino; BIO never decides that. BIO only decides that its slot rows are
overgrown.

## 2. Layout patterns

The 48 functions share six layout patterns. A function picks one pattern and adds its own
anchor and furniture. Each pattern is made from structure rules (`Data/Level/Structure/`).

| Pattern | Structure | How it plays |
|---|---|---|
| **Hall**: large, with a pillar grid | `WidePillarGrid`, `DensePillarGrid`, `BrokenColumnGrid` | Long lanes with pillar cover. Flanking around columns |
| **Aisles**: parallel rows | Furniture on a `Lattice`, or a *Rows* structure rule (Phase 2) | Lanes along the rows; crossing between rows is risky |
| **Cells**: small rooms off one spine | `OfficeMaze`, `DividerWall` | Close range, corners, doorways |
| **Ring**: a feature in the middle | `ColumnRing` | Fights circle the centre; the anchor blocks sight across the room |
| **Strip**: long and thin | Long floor plans, `CornerPillars` | Transit space and ambush points; cover at the ends |
| **Service**: machines along the walls | `CornerPillars`, `ServiceCounter`, wall props | Narrow gaps, hazards, a cramped centre |

A function's own structure always wins. A reality's structure is only used when the function
has none (`RoomStructurePlanner`), so a restaurant keeps its kitchen wall in every reality.

## 3. Ship side (Phase 4)

This dial is what makes the ship read as a cruise liner rather than a generic base.

| Side | Rooms | Size | Order | Clutter | Doors |
|---|---|---|---|---|---|
| **Guest, first class** | Atrium, casino, theatre, restaurants, pool, park | Largest | Symmetric, anchored on the centre | Low, heavy on decor | Grand, centred |
| **Guest, steerage** | Cabin rows, food court, commons, gym | Medium | Rows | Medium | Plain |
| **Crew and service** | Kitchens, engine room, storage, life support, barracks | Small | Functional | High | An extra service door on a back wall |

The same function can sit on two sides: a first-class restaurant and a crew mess share one
`RoomFunction` and differ only by side.

## 4. Placement order

Every room runs through the same steps. Each step keeps clear what later steps need, and
whatever is placed has to keep every free tile reachable from every doorway
(`RoomStructure.TryClaim`).

1. **Outline**: floor plan, size and bevels. The reality scales size and biases the shape.
2. **Doorways and walkways**: tagged first and kept clear (`Walkway`, `NearDoor`, `Centre`).
3. **Structure**: the pattern's inner walls and pillars, or the reality's when the function has none.
4. **Offerings**: what the player comes for (a cache or resource node), placed in a nook.
5. **Anchor**: the one or two `Required` pieces that say what the room is (bar counter, reactor, operating table).
6. **Furniture**: the function's own props, placed by tile tag, zone, lattice and facing.
7. **Cover**: the room type's dressing, in the reality's own pieces (`DressingSwap`).
   Hard cover blocks movement and sight; low cover blocks movement only. Cover set
   *along* a lane makes a firing line, cover set *across* it breaks the lane up.
8. **Concealment**: soft cover. It takes no floor and hides whoever stands in it. It goes by
   walls and corners, off the walkway, and within two tiles of a route so people actually
   pass it (the rule is in Phase 5).
9. **Reality props and decor**: the holding faction's dressing, then the breach faction's.

Big things go before small ones, and required things before optional ones, so clutter never
takes an anchor's spot.

## 5. Realities

| | BIO | VOID | TECH |
|---|---|---|---|
| **Priorities** (concealment / walls / lines of fire) | High / Medium / Low | Medium / High / Low | Low / Medium / High |
| **Outline** | Favours domes and rings (curved walls) | Smaller rooms (×0.8) | Favours rectangles |
| **Structure** (where the function has none) | Broken column grid and wall remnants: a ruin grown over | Office maze: folds the room into corridors | Wide pillar grid and a checkpoint barrier: ordered lanes |
| **Dressing swaps** | Furniture becomes overgrown mounds; rows break up | Furniture becomes jagged shards that block sight | Clutter becomes barricades and turrets |
| **Concealment** | Many patches beside routes | Shadow pockets in dead ends | Next to none: open, lit floor |
| **Fill** | Medium (0.3), mostly soft | High (0.55), hard | Low (0.3, props ×0.9): the lanes stay open |

### Priorities and generation

The priorities say what a reality's **fight rooms** (room types with enemies) should offer.
`RoomTraitMeasure` reads each trait off the finished floor:
- **Concealment**: the share of walkable floor under a `ConcealmentZone`.
- **Walls**: the share of walkable floor at most 2 tiles across.
- **Lines of fire**: the longest open lane, and whether hard cover stands beside it.

Generation follows the priorities in two ways (Phase 1):
- **Steering.** Concealment placements are scaled by the concealment priority: ×0.5 for
  Low, ×1 for Medium, ×1.5 for High.
- **Pick the best fit.** A fight room's structure, and then its furnishing, are each tried
  from several seeds (`LevelBuildSettings` › *Priority Tries*). The try whose measured levels
  are closest to the priorities is kept.
  - Try 0 is the seed the room always had, and another try is kept only if it fits strictly
    better. So a room that already fit doesn't change.
  - The structure step is scored on walls and lines of fire only, because concealment comes
    from props.

Concealment a room *type* needs whoever holds it (a Stealth room's `ExtraDressing`) is never
scaled. The Room Lab shows what each trait wants and what the room has, and which furnishing
try was kept. Where no try fits, the check names the setting to change.

## 6. Breach rooms (Phase 6)

A Breach room is held by two realities. For now it is built by the first faction's design,
and both factions bring their props. The plan is to **split it by zone**: when the structure
divides the room (a partition), each zone follows one faction's dressing. The faction whose
corridor leads in gets the zone by that doorway. An undivided room gets a breach divider
first, made from wall remnants in the second faction's style.

A gradient from the breach doorway (swaps get more likely closer to it) would look more
natural but is harder to read and tune. It stays a possible follow-up.

## 7. Room cards

One line per function. A card is the spec its asset is built to; a room that can't be
described in one line has no clear identity yet. Columns:
- **Side**: G1 is guest first class, G2 guest steerage, C crew and service.
- **Anchor**: the `Required` piece, placed before anything else.
- **Plays as**: what a fight there should feel like before a reality changes it.

The *Now* column says what the asset has today: S is structure, P is props, — is nothing yet.
Most functions are still bare; Phases 2 and 3 fill them in from these cards.

| Room | Category | Side | Pattern | Anchor | Furniture | Plays as | Now |
|---|---|---|---|---|---|---|---|
| Grand Atrium | Entertainment | G1 | Ring | Central fountain or statue | Benches round the ring, planters | Circle the centrepiece; balcony-like edges | S, P |
| Casino | Entertainment | G1 | Aisles | Cashier cage on a wall | Slot rows, card tables in the main zone | Lanes between rows, tables as cover at crossings | S, P |
| Theatre | Entertainment | G1 | Aisles | Stage on the far wall | Seat rows facing the stage | Lanes down the aisles, stage as high ground | — |
| Exhibit Hall | Entertainment | G1 | Hall | Centre display | Display cases on a lattice | Open hall, cases as waist cover | — |
| Bar | Entertainment | G2 | Ring | Bar counter (low partition) | Stools, booths along walls | Counter splits the room; booths for close range | — |
| Restaurant | Commercial | G1 | Cells | Kitchen wall (divider) | Tables in the dining zone, ovens in the kitchen | Open dining room, tight kitchen | S, P |
| Food Court | Commercial | G2 | Hall | Kiosk island | Table clusters, kiosks on walls | Many low cover pieces, no long lanes | — |
| Kitchen | Commercial | C | Service | Cooking line along a wall | Prep tables, shelves | Narrow gaps between counters | — |
| Promenade | Commercial | G1 | Strip | — | Shopfronts on the walls, planters down the middle | Long lane broken by planters | — |
| Cabin Row | Residential | G2 | Cells | — | Bunk, locker per cabin | Corridor with cabin doors; close range | — |
| Quarters | Residential | G1 | Cells | Divider wall | Bed, desk, wardrobe | Two zones, a doorway between | S, P |
| Commons | Residential | G2 | Hall | Seating island | Sofas, tables | Medium cover all round | — |
| School | Residential | G2 | Aisles | Teacher's desk | Desk rows | Low cover rows facing one wall | — |
| Pool | Recreation | G1 | Ring | Pool (open floor or water) | Loungers round it | Open centre, cover only round the edge | — |
| Park | Recreation | G1 | Hall | Central tree | Hedges, benches, paths | Hedges hide; long views across lawns | P |
| Gym | Recreation | G2 | Aisles | — | Machine rows | Rows of hard cover | — |
| Observation Deck | Recreation | G1 | Strip | Window wall | Telescopes, benches facing out | Open lane along the window | — |
| Wellness Center | Recreation | G1 | Cells | Divider wall | Treatment beds, steam booths | Small rooms, soft light | — |
| Medical Bay | Medical | C | Cells | Operating table | Beds along walls, trolleys | Curtained bays | — |
| Ward | Medical | C | Aisles | — | Bed rows | Rows of beds as low cover | — |
| Pharmacy | Medical | G2 | Service | Counter | Shelf rows | Counter in front, shelves behind | — |
| Morgue | Medical | C | Service | Drawer wall | Slabs on a lattice | Cold, narrow gaps | — |
| Security Office | Security | C | Cells | Checkpoint barrier | Desks, lockers | Checkpoint funnels in | — |
| Armory | Security | C | Aisles | Weapon rack wall | Rack rows, crates | Narrow aisles, hard cover | — |
| Barracks | Security | C | Aisles | — | Bunk rows, lockers | Rows across the room | — |
| Engine Room | Engineering | C | Service | Turbine blocks | Pipes, catwalk rails | Narrow gaps, hazard in the centre | — |
| Reactor Core | Engineering | C | Ring | Reactor | Consoles round it | Circle the core; no sight across | — |
| Life Support | Engineering | C | Service | Scrubber units on walls | Tanks, pipes | Cramped, machine noise | — |
| Workshop | Engineering | C | Service | Workbench line | Tool racks, crates | Medium clutter | — |
| Processing Plant | Engineering | C | Aisles | Conveyor line | Machines on a lattice | Production lines as lanes | — |
| Bridge | Command | C | Ring | Captain's chair and command table | Console arc | Arc of consoles facing the front | — |
| Control Room | Command | C | Aisles | Main screen wall | Console rows | Rows facing one wall | — |
| Offices | Command | C | Cells | — | Desks, cubicle walls (`OfficeMaze`) | Maze of short walls | — |
| Hydroponics Bay | Agriculture | C | Aisles | — | Grow racks on a lattice | Lanes between racks | — |
| Farm | Agriculture | C | Hall | — | Crop beds, irrigation | Low cover, long views | — |
| Water Treatment | Agriculture | C | Service | Tanks | Pipes, pumps | Hard cover, narrow gaps | — |
| Laboratory | Science | C | Cells | Lab bench island | Benches, fume hoods on walls | Bench islands as cover | — |
| Data Center | Science | C | Aisles | — | Server racks on a lattice | Narrow aisles of tall racks | — |
| Library | Science | G1 | Aisles | Reading table | Shelf rows | Shelves block sight; reading area open | — |
| Chapel | Worship | G2 | Aisles | Altar | Pew rows | Pews as low cover facing the altar | — |
| Shrine | Worship | G2 | Ring | Shrine object | Candles, mats | Small, one centrepiece | — |
| Lobby | Transit | G1 | Hall | Reception desk | Seating, planters | Open hall with a desk | S, P |
| Junction | Transit | — | Strip | — | — | A crossing of routes; keep it bare | — |
| Cargo Bay | Transit | C | Hall | Loader | Container stacks | Dense pillar grid of crates | S, P |
| Docking Bay | Transit | C | Hall | Airlock | Cargo, fuel lines | Wide grid, long lanes | S, P |
| Hangar | Transit | C | Hall | Shuttle | Tool carts, crates | Huge, one big obstacle | — |
| Escape Pod Bay | Transit | C | Strip | Pod row on a wall | Lockers | Long lane along the pods | — |
| Storage | Transit | C | Aisles | Shelf wall | Crate stacks | Narrow aisles | P |

---

## Roadmap

| Phase | What | Status |
|---|---|---|
| **1. Priorities steer generation** | Concealment scaled by priority; fight rooms keep the best-fitting of several structure and furnishing tries; the Room Lab shows the kept try | **Done in code, not yet compiled or run** |
| **2. Patterns for every function** | Give each function its card's structure. Add a *Rows* structure rule for Aisles rooms (parallel low partitions with gaps) | Next |
| **3. Anchors and furniture** | A `Required` anchor and furniture list for each card, starting with placeholder boxes until art exists | After 2 |
| **4. Ship side** | A Guest first class / Guest steerage / Crew dial (on the section or the function) that scales size, fill and decor, and adds a service door on crew rooms | After 3 |
| **5. Concealment by routes, service doors** | A placement option to put props within N tiles of a walkway; concealment dressings use it | Any time |
| **6. Breach split by zone** | Each zone of a Breach room dressed by one faction; a breach divider for undivided rooms | After 2 |
| **7. Tuning** | Set the trait bands (`RoomTraitMeasure`) and *Priority Tries* from Room Lab runs of each card in each reality | Ongoing |
