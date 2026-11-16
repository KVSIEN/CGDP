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
| **Aisles**: parallel rows | `ShelfRows` (tall), `LowRows` (seats, pews, benches) | Lanes along the rows; crossing between rows is risky |
| **Cells**: small rooms off one spine | `OfficeMaze`, `DividerWall` | Close range, corners, doorways |
| **Ring**: a feature in the middle | `ColumnRing` | Fights circle the centre; the anchor blocks sight across the room |
| **Strip**: long and thin | Long floor plans, `CornerPillars` | Transit space and ambush points; cover at the ends |
| **Service**: machines along the walls | `CornerPillars`, `ServiceCounter`, wall props | Narrow gaps, hazards, a cramped centre |

Structure is applied in two steps (`RoomStructurePlanner`):
- **The room's own structure.** The function's structure if it has any, otherwise the
  reality's fallback (`FactionDesign` › *Structure*). A restaurant keeps its kitchen wall in
  every reality.
- **The reality's additions**, applied to every room the reality holds (`FactionDesign` ›
  *Additions*). This is what still marks a room as BIO or VOID once the function brings its own
  layout:
  - BIO adds wall remnants: a ruin grown over.
  - VOID adds full-height folds that pinch the floor into corridors.
  - TECH adds nothing, which keeps its lanes open.
- Every rule keeps the walkways open and the floor connected, so the additions can't block a room.

### Structure for each function (Phase 2)

| Pattern | Structure | Functions |
|---|---|---|
| Hall | `WidePillarGrid` | Exhibit Hall, Food Court, Lobby, Docking Bay |
| Hall | `DensePillarGrid` | Cargo Bay |
| Hall | `CornerPillars` | Commons, Hangar |
| Hall | none (fallback) | Park, Farm: their trees and crop beds are furniture |
| Aisles | `ShelfRows` | Library, Storage, Data Center, Armory, Hydroponics Bay |
| Aisles | `LowRows` | Theatre, Chapel, School, Control Room, Ward, Barracks, Gym, Processing Plant |
| Aisles | `ColumnRing`, `CornerPillars`, `ServiceCounter` | Casino (unchanged) |
| Cells | `OfficeMaze` | Offices, Cabin Row |
| Cells | `DividerWall` | Quarters, Restaurant, Wellness Center, Medical Bay, Laboratory |
| Cells | `CheckpointBarrier` | Security Office |
| Ring | `ColumnRing` | Pool, Bridge, Shrine |
| Ring | `ColumnRing`, `CornerPillars` | Grand Atrium (unchanged) |
| Ring | `ServiceCounter` | Bar: the counter *is* the ring's centre |
| Ring | landmark | Reactor Core (hand-built) |
| Strip | `CornerPillars` | Promenade, Observation Deck, Escape Pod Bay |
| Strip | none (fallback) | Junction |
| Service | `ServiceCounter`, `CornerPillars` | Kitchen, Pharmacy |
| Service | `CornerPillars` | Engine Room, Life Support, Workshop, Water Treatment, Morgue |

Rooms left at *none* get the holding reality's fallback, or stay bare when no reality holds them.

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
| **Structure** (where the function has none) | Broken growth-column grid: a ruin grown over | Office maze: folds the room into corridors | Pylon grid and a checkpoint barrier: ordered lanes |
| **Additions** (every room it holds) | Wall remnants | Fold walls: full-height runs that pinch the floor | None: keeps the lanes open |
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

## 7. Placeholder kit

Until there is art, every structure piece, anchor and prop is a prefab built from Unity's
primitives (cubes, cylinders, spheres, capsules), like the setups in the AITest and Sandbox
scenes. Each one is coloured by its role with the Gridbox prototype materials, so a room reads
at a glance in play and in the Room Lab. When a real model arrives, it replaces the
placeholder inside the same prefab, so no rule or asset has to change.

| Role | Colour (Gridbox material) | Examples |
|---|---|---|
| Columns and neutral structure | White, Grey 1 | Round and square columns |
| Built inner walls and pillars | The level's wall material; low cover in its low-cover material | Rows, dividers, counters |
| BIO pieces | Green 1–3 | Growth column, root mound |
| TECH pieces | Cyan, Blue 1–3 | Console, barricade |
| VOID pieces | Purple, Magenta | Shard, fold |
| Anchors | Orange | Bar counter, operating table, reactor (Phase 3) |
| Furniture | Brown, Grey 3 | Shelves, pews, beds (Phase 3) |
| Machines | Yellow, Grey 4 | Turbines, pumps, conveyors (Phase 3) |
| Concealment | Green (decor, no collider) | Existing `ConcealmentPatch`, `GrassPatch`, `MossPatch` |

Rules for placeholder prefabs:
- **Origin** at the base. For props, the origin is the centre of the footprint and +Z is the front.
- **Pillar prefabs** (`_pillarPrefab` on the structure rules) are 1 m wide and 1 m tall. The
  builder stretches each one to its pillar's width and height, so one column prefab fits any
  room height, and broken columns too.
- **Prop size** matches the placement's footprint in tiles (3 m a tile).
- **Colliders:** a collider on every solid part; none on decor.
- **Location:** `Prefabs/Environment/Structure/` for structure pieces, `Prefabs/Environment/Props/` for props.

### Furnishing rules (Phase 3)

These are how each card becomes a function's `_props` list:
- **Anchor first.** It is `Required` and placed before any furniture, against a wall
  (`AgainstWall`) with a tile of floor in front of it. The middle of the room belongs to the
  room type's centrepiece (a chest, a boss, a terminal), so anchors stay off it.
- **Furniture after it.** Counts scale with the room's size (`ScaleByRoomSize`) and stay
  modest, because the room type's cover dressing comes next and needs floor too. The
  faction's fill share caps everything.
- **Aisles rooms** put their furniture against the rows (`AgainstWall` with no `On` tag finds
  inner walls too), so shelves line the shelving rows and seats line the seat rows.
- **Reuse before making.** Props that already exist (`Table`, `Bed`, `Chair`, `Sofa`,
  `OfficeDesk`, `ServerRack`, `ProductionLine`, `Planter`, `StreetLamp`, `CabinetBank`,
  `Crate`) are used where they fit. New placeholders are added only for what is missing.
- **Already furnished** places keep their props: Casino, Restaurant, Quarters, Park, Grand
  Atrium, Cargo Bay, Docking Bay and Storage. Lobby gains its reception desk.

## 8. Room cards

One line per function. A card is the spec its asset is built to; a room that can't be
described in one line has no clear identity yet. Columns:
- **Side**: G1 is guest first class, G2 guest steerage, C crew and service.
- **Anchor**: the `Required` piece, placed before anything else.
- **Plays as**: what a fight there should feel like before a reality changes it.

The *Now* column says what the asset has today: S is its own structure, P is props. Park and
Farm take the holding reality's structure; Junction stays bare on purpose.

| Room | Category | Side | Pattern | Anchor | Furniture | Plays as | Now |
|---|---|---|---|---|---|---|---|
| Grand Atrium | Entertainment | G1 | Ring | Central fountain or statue | Benches round the ring, planters | Circle the centrepiece; balcony-like edges | S, P |
| Casino | Entertainment | G1 | Aisles | Cashier cage on a wall | Slot rows, card tables in the main zone | Lanes between rows, tables as cover at crossings | S, P |
| Theatre | Entertainment | G1 | Aisles | Stage on the far wall | Seat rows facing the stage | Lanes down the aisles, stage as high ground | S, P |
| Exhibit Hall | Entertainment | G1 | Hall | Centre display | Display cases on a lattice | Open hall, cases as waist cover | S, P |
| Bar | Entertainment | G2 | Ring | Bar counter (low partition) | Stools, booths along walls | Counter splits the room; booths for close range | S, P |
| Restaurant | Commercial | G1 | Cells | Kitchen wall (divider) | Tables in the dining zone, ovens in the kitchen | Open dining room, tight kitchen | S, P |
| Food Court | Commercial | G2 | Hall | Kiosk island | Table clusters, kiosks on walls | Many low cover pieces, no long lanes | S, P |
| Kitchen | Commercial | C | Service | Cooking line along a wall | Prep tables, shelves | Narrow gaps between counters | S, P |
| Promenade | Commercial | G1 | Strip | — | Shopfronts on the walls, planters down the middle | Long lane broken by planters | S, P |
| Cabin Row | Residential | G2 | Cells | — | Bunk, locker per cabin | Corridor with cabin doors; close range | S, P |
| Quarters | Residential | G1 | Cells | Divider wall | Bed, desk, wardrobe | Two zones, a doorway between | S, P |
| Commons | Residential | G2 | Hall | Seating island | Sofas, tables | Medium cover all round | S, P |
| School | Residential | G2 | Aisles | Teacher's desk | Desk rows | Low cover rows facing one wall | S, P |
| Pool | Recreation | G1 | Ring | Pool (open floor or water) | Loungers round it | Open centre, cover only round the edge | S, P |
| Park | Recreation | G1 | Hall | Central tree | Hedges, benches, paths | Hedges hide; long views across lawns | P |
| Gym | Recreation | G2 | Aisles | — | Machine rows | Rows of hard cover | S, P |
| Observation Deck | Recreation | G1 | Strip | Window wall | Telescopes, benches facing out | Open lane along the window | S, P |
| Wellness Center | Recreation | G1 | Cells | Divider wall | Treatment beds, steam booths | Small rooms, soft light | S, P |
| Medical Bay | Medical | C | Cells | Operating table | Beds along walls, trolleys | Curtained bays | S, P |
| Ward | Medical | C | Aisles | — | Bed rows | Rows of beds as low cover | S, P |
| Pharmacy | Medical | G2 | Service | Counter | Shelf rows | Counter in front, shelves behind | S, P |
| Morgue | Medical | C | Service | Drawer wall | Slabs on a lattice | Cold, narrow gaps | S, P |
| Security Office | Security | C | Cells | Checkpoint barrier | Desks, lockers | Checkpoint funnels in | S, P |
| Armory | Security | C | Aisles | Weapon rack wall | Rack rows, crates | Narrow aisles, hard cover | S, P |
| Barracks | Security | C | Aisles | — | Bunk rows, lockers | Rows across the room | S, P |
| Engine Room | Engineering | C | Service | Turbine blocks | Pipes, catwalk rails | Narrow gaps, hazard in the centre | S, P |
| Reactor Core | Engineering | C | Ring | Reactor | Consoles round it | Circle the core; no sight across | Landmark |
| Life Support | Engineering | C | Service | Scrubber units on walls | Tanks, pipes | Cramped, machine noise | S, P |
| Workshop | Engineering | C | Service | Workbench line | Tool racks, crates | Medium clutter | S, P |
| Processing Plant | Engineering | C | Aisles | Conveyor line | Machines on a lattice | Production lines as lanes | S, P |
| Bridge | Command | C | Ring | Captain's chair and command table | Console arc | Arc of consoles facing the front | S, P |
| Control Room | Command | C | Aisles | Main screen wall | Console rows | Rows facing one wall | S, P |
| Offices | Command | C | Cells | — | Desks, cubicle walls (`OfficeMaze`) | Maze of short walls | S, P |
| Hydroponics Bay | Agriculture | C | Aisles | — | Grow racks on a lattice | Lanes between racks | S, P |
| Farm | Agriculture | C | Hall | — | Crop beds, irrigation | Low cover, long views | P |
| Water Treatment | Agriculture | C | Service | Tanks | Pipes, pumps | Hard cover, narrow gaps | S, P |
| Laboratory | Science | C | Cells | Lab bench island | Benches, fume hoods on walls | Bench islands as cover | S, P |
| Data Center | Science | C | Aisles | — | Server racks on a lattice | Narrow aisles of tall racks | S, P |
| Library | Science | G1 | Aisles | Reading table | Shelf rows | Shelves block sight; reading area open | S, P |
| Chapel | Worship | G2 | Aisles | Altar | Pew rows | Pews as low cover facing the altar | S, P |
| Shrine | Worship | G2 | Ring | Shrine object | Candles, mats | Small, one centrepiece | S, P |
| Lobby | Transit | G1 | Hall | Reception desk | Seating, planters | Open hall with a desk | S, P |
| Junction | Transit | — | Strip | — | — | A crossing of routes; keep it bare | — |
| Cargo Bay | Transit | C | Hall | Loader | Container stacks | Dense pillar grid of crates | S, P |
| Docking Bay | Transit | C | Hall | Airlock | Cargo, fuel lines | Wide grid, long lanes | S, P |
| Hangar | Transit | C | Hall | Shuttle | Tool carts, crates | Huge, one big obstacle | S, P |
| Escape Pod Bay | Transit | C | Strip | Pod row on a wall | Lockers | Long lane along the pods | S, P |
| Storage | Transit | C | Aisles | Shelf wall | Crate stacks | Narrow aisles | S, P |

---

## Roadmap

| Phase | What | Status |
|---|---|---|
| **1. Priorities steer generation** | Concealment scaled by priority; fight rooms keep the best-fitting of several structure and furnishing tries; the Room Lab shows the kept try | **Done in code, not yet compiled or run** |
| **2. Patterns for every function** | Each function gets its pattern's structure (table in section 2); a *Rows* rule for Aisles rooms; reality *Additions*; pillar prefabs stretched to size; the placeholder kit and its column pieces | **Done in code, not yet compiled or run** |
| **3. Anchors and furniture** | A `Required` anchor and a furniture list for each card, built from the placeholder kit (section 7); rules in *Furnishing rules* | **Done in code, not yet compiled or run** |
| **4. Ship side** | A Guest first class / Guest steerage / Crew dial (on the section or the function) that scales size, fill and decor, and adds a service door on crew rooms | After 3 |
| **5. Concealment by routes, service doors** | A placement option to put props within N tiles of a walkway; concealment dressings use it | Any time |
| **6. Breach split by zone** | Each zone of a Breach room dressed by one faction; a breach divider for undivided rooms | After 2 |
| **7. Tuning** | Set the trait bands (`RoomTraitMeasure`) and *Priority Tries* from Room Lab runs of each card in each reality | Ongoing |
