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
| **Layout**: which arrangement of the place | Its floor plans, size, structure and set pieces (section 12) | `RoomLayout` (`Data/Level/Functions/Layouts/`) |
| **Ship side**: who the room is for | How large, full and decorated it is. Back-of-house doors | `ShipSideDesign` (`Data/Level/Sides/`); section and function sides |
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
| Aisles | `ShelfRows` | Storage, Data Center, Armory, Hydroponics Bay; Library's Stacks layout (its Reading Hall has `WidePillarGrid`, its Rotunda `ColumnRing`) |
| Aisles | `LowRows` | Theatre, Chapel, School, Control Room, Ward, Barracks, Gym, Processing Plant |
| Aisles / Ring / Cells | by layout (section 12) | Casino: Slot Hall `LowRows`, Table Pits `ColumnRing` + `CornerPillars`, High-Roller Salon `CornerPillars` |
| Cells | `OfficeMaze` | Offices, Cabin Row |
| Cells | `DividerWall` | Quarters, Restaurant, Wellness Center, Medical Bay, Laboratory |
| Cells | `CheckpointBarrier` | Security Office |
| Ring | `ColumnRing` | Bridge, Shrine; Pool when it has no layout (its layouts use `CornerPillars`: the water is the centre) |
| Ring | `ColumnRing`, `CornerPillars` | Grand Atrium (unchanged) |
| Ring | `ServiceCounter` | Bar: the counter *is* the ring's centre |
| Ring | landmark | Reactor Core (hand-built) |
| Strip | `CornerPillars` | Promenade, Observation Deck, Escape Pod Bay |
| Strip | none (fallback) | Junction |
| Service | `ServiceCounter`, `CornerPillars` | Kitchen, Pharmacy |
| Service | `CornerPillars` | Engine Room, Life Support, Workshop, Water Treatment, Morgue, Power Distribution, Fuel Depot, Maintenance Hub |
| Hall | `WidePillarGrid` | Waste Reclamation, Coolant Plant (and each layout its own: section 12) |
| Ring | `ColumnRing` | Comms Array |

Rooms left at *none* get the holding reality's fallback, or stay bare when no reality holds them.

## 3. Ship side

This dial is what makes the ship read as a cruise liner rather than a generic base.

| Side | Rooms | Size | Order | Clutter | Doors |
|---|---|---|---|---|---|
| **Guest, first class** | Atrium, casino, theatre, restaurants, pool, park | Largest | Symmetric, anchored on the centre | Low, heavy on decor | Grand, centred |
| **Guest, steerage** | Cabin rows, food court, commons, gym | Medium | Rows | Medium | Plain |
| **Crew and service** | Kitchens, engine room, storage, life support, barracks | Small | Functional | High | An extra service door on a back wall |

The same function can sit on two sides: a first-class restaurant and a crew mess share one
`RoomFunction` and differ only by side.

### How a room gets its side

- **The map section decides, where it can.** Each `MapSectionDefinition` has a side:
  Commerce is guest first class, Habitation is guest steerage, Engineering is crew.
- **Each function lists the sides it can sit on** (`RoomFunction` › *Sides*), its usual side
  first. A room takes its section's side when the function lists it, and otherwise the
  function's usual side. So a restaurant in Engineering becomes a crew mess, while an engine
  room in Commerce stays crew.
- A room with no section takes its function's usual side. Junction has no side.

### What a side changes

Each side has a `ShipSideDesign` (`Data/Level/Sides/`), applied on top of the holding
reality's design:

| | Guest, first class | Guest, steerage | Crew and service |
|---|---|---|---|
| Room size (single-cell rooms) | ×1.1 | ×1 | ×0.85 |
| Fill share | ×0.8 | ×1 | ×1.25 |
| Prop counts | ×0.85 | ×1 | ×1.2 |
| Dressing | Planters in the corners, rugs (decor) | None | A **service door** on a back wall, crates in the corners |

The service door is a placeholder prop, a closed staff door set against a wall. It is not a
passage: real doorways only come from the map graph's connections. It is there to say
"back of house".

Symmetric, centred layouts for first class are left for later; nothing in the order of
placement supports them yet.

The side dressing goes down after the function's furniture and before the room type's
cover, so a crew room's service door is never crowded out.

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
8. **Concealment**: soft cover. It takes no floor and hides whoever stands in it. It goes
   off the walkway but within two steps of a route, so people actually pass it (*Near
   Route*), or, for VOID's shadow pockets, into dead ends at least three steps from any
   route (*Off Route*). BIO is the exception: its reality is overgrown, so its patches
   (3–5 per 64 tiles, scaled with the room) are spread evenly over the whole floor. See
   *Routes* below.
9. **Reality props and decor**: the holding faction's dressing, then the breach faction's.

Big things go before small ones, and required things before optional ones, so clutter never
takes an anchor's spot.

### Routes (Phase 5)

A **route** is where people walk through a room: the walkways tagged from each doorway to the
centre, plus the row of floor in front of any prop placed with *Marks Route* (the crew
service door, so staff traffic counts). The distance of a tile from the routes is counted in
steps over the floor plan, around inner walls and pillars but not around props, so it stays
the same while the room is furnished (`RouteMap`).

Two placement options use it:
- **Near Route** *N*: only where a tile of the prop is within *N* steps of a route. Concealment
  patches use 2: beside the way through, never on it (they still avoid the walkway tag).
- **Off Route** *N*: only where every tile is at least *N* steps from every route: dead ends
  and back corners. VOID's shadow pockets use 3.

A room with no route (no doorway) puts no limit on either. A *Required* placement that finds
no spot drops both rules when it is relaxed, along with its tags and zone. Because the
service door is placed with the side dressing, before the room type's concealment, a crew
Stealth room's patch can gather by the staff door.

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

A Breach room (and a Rift, the other room type held by two realities) is **split by zone**:
the reality that holds it and the one breaching it each dress their own side.

1. **Building.** The holder's design builds the room, as for any room it holds: the
   function's structure (or the holder's), then the holder's additions.
2. **Breach divider.** If the room is still one zone after that, the breaching reality's
   *Breach Divider* (`FactionDesign` › *Structure*) goes across it, a `PartitionStructureRule`
   two-fifths to halfway along the longer side, with openings where walkways cross:
   - BIO: a low root wall (1.1 m, 2-tile openings).
   - TECH: a low checkpoint line (1.1 m, 3-tile openings).
   - VOID: a full-height fold (2-tile openings).
   A room too small for two 8-tile sides stays whole.
3. **Who gets which zone** (`BreachSplit`). A *breach doorway* is one whose corridor comes
   from a room the breaching reality holds. The breaching reality takes every zone with a
   breach doorway, and the holder takes the rest. If no zone has one, or every zone does,
   the holder keeps the largest zone and the breaching reality takes the others.
4. **Dressing by side.** On each side:
   - The room type's cover goes down in that reality's pieces (its *Dressing Swaps*).
   - Then the reality's own props follow.
   - Counts that scale by room size read that side's floor alone.
   Everything before that is shared: the offerings, the function's furniture, the side
   dressing and the room type's *Extra Dressing*. The fill share and prop scale stay the
   holder's.
5. **A room that stays whole** is dressed as before: the holder's cover everywhere, and both
   realities' props anywhere.

The Room Lab shows the split. Its report has a *Breach* row naming each side's zones, and the
tile readout names the side. A Recipe Breach room's first doorway comes from the breaching
reality's territory.

A gradient from the breach doorway (swaps get more likely closer to it) would look more
natural but is harder to read and tune. It stays a possible follow-up.

## 7. Placeholder kit

Until there is art, every structure piece, anchor and prop is a prefab built from Unity's
primitives (cubes, cylinders, spheres, capsules), like the setups in the AISimulator and ShootingRange
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
| Concealment | Green (decor, no collider); VOID dark violet | Existing `ConcealmentPatch`, `GrassPatch`, `MossPatch`; `ShadowPocket` (Phase 5) |
| Ship operations | Orange marks the place's main machine; Yellow machinery and hazard; Red fuel and danger; Blue 2 coolant; Green 2 waste; Cyan live power and screens; Grey 3/4 housings | `Transformer`, `Compactor`, `FuelTank`, `HeatExchanger`, `Pump`, `HazardBarrier` |

The colours are one asset, `Data/Level/Placeholders/DefaultPlaceholderPalette`: each swatch
names its role (Anchor, Machinery, Danger, Coolant, Waste, Screen…). Every part of every prop
and structure piece uses one of its swatches (`PlaceholderKitTests`), so a new colour is added
there first, with what it means.

**Placeholder Prop Builder** (Window › CGD › Placeholder Prop Builder) makes a kit prop:
up to three parts (box, cylinder or sphere), each a palette colour, sized in metres, shown from
above against the footprint it will take with its front edge marked, and warned about when it
sinks into the floor, reaches past its footprint or has nothing solid. *Start from* gives a
wall cabinet, machine, tank or console sized to the footprint. *Rework prop* loads an existing
prop; saving rebuilds it in place, so its root keeps its identity and everything that places it
keeps working.

Rules for placeholder prefabs:
- **At most three primitives**, each in the colour of its role: a body and one or two
  details that say what it is (a desk and its chair, a sofa's seat and back). No legs,
  handles, LEDs or drawer fronts. `PlaceholderKitTests` checks it.
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
- **Already furnished** places keep their props: Restaurant, Quarters, Park, Grand
  Atrium, Cargo Bay, Docking Bay and Storage. Lobby gains its reception desk. Casino, Library
  and Pool are furnished by layout (section 12).
- **Sets, not scatter.** Pieces that belong together are one placement with *Members* (a table
  and its chairs), and pieces that serve another stand *Near* it (section 12).

## 8. Room cards

One line per function. A card is the spec its asset is built to; a room that can't be
described in one line has no clear identity yet. Columns:
- **Side**: G1 is guest first class, G2 guest steerage, C crew and service.
- **Anchor**: the `Required` piece, placed before anything else.
- **Plays as**: what a fight there should feel like before a reality changes it.

The *Now* column says what the asset has today: S is its own structure, P is props, L layouts, K shared kits (section 12). Park and
Farm take the holding reality's structure unless a layout brings its own; Junction stays bare on purpose.

| Room | Category | Side | Pattern | Anchor | Furniture | Plays as | Now |
|---|---|---|---|---|---|---|---|
| Grand Atrium | Entertainment | G1 | Ring | Central fountain or statue | Benches round the ring, planters | Circle the centrepiece; balcony-like edges | S, P, L |
| Casino | Entertainment | G1 | by layout | Cashier cage on a wall | Slot banks, card-table pits, roulette, bar with stools, sofas | Lanes between slot banks; tables as cover at crossings | S, P, L |
| Theatre | Entertainment | G1 | Aisles | Stage on the far wall | Seat rows facing the stage | Lanes down the aisles, stage as high ground | S, P, L |
| Exhibit Hall | Entertainment | G1 | Hall | Centre display | Display cases on a lattice | Open hall, cases as waist cover | S, P, L |
| Bar | Entertainment | G2 | Ring | Bar counter (low partition) | Stools, booths along walls | Counter splits the room; booths for close range | S, P, L |
| Restaurant | Commercial | G1 | Cells | Kitchen wall (divider) | Tables in the dining zone, ovens in the kitchen | Open dining room, tight kitchen | S, P, L |
| Food Court | Commercial | G2 | Hall | Kiosk island | Table clusters, kiosks on walls | Many low cover pieces, no long lanes | S, P, L |
| Kitchen | Commercial | C | Service | Cooking line along a wall | Prep tables, shelves | Narrow gaps between counters | S, P, L |
| Promenade | Commercial | G1 | Strip | — | Shopfronts on the walls, planters down the middle | Long lane broken by planters | S, P, L |
| Cabin Row | Residential | G2 | Cells | — | Bunk, locker per cabin | Corridor with cabin doors; close range | S, P, L |
| Quarters | Residential | G1 | Cells | Divider wall | Bed, desk, wardrobe | Two zones, a doorway between | S, P, L |
| Commons | Residential | G2 | Hall | Seating island | Sofas, tables | Medium cover all round | S, P, L |
| School | Residential | G2 | Aisles | Teacher's desk | Desk rows | Low cover rows facing one wall | S, P, L |
| Pool | Recreation | G1 | by layout | The pool basin (grows with the room) | Lounger pairs facing it, jacuzzis, slide, pool bar, palms | Open water, cover only round the edge | S, P, L |
| Park | Recreation | G1 | Hall | Central tree | Hedges, benches, paths | Hedges hide; long views across lawns | P, L |
| Gym | Recreation | G2 | Aisles | — | Machine rows | Rows of hard cover | S, P, L |
| Observation Deck | Recreation | G1 | Strip | Window wall | Telescopes, benches facing out | Open lane along the window | S, P, L |
| Wellness Center | Recreation | G1 | Cells | Divider wall | Treatment beds, steam booths | Small rooms, soft light | S, P, L |
| Medical Bay | Medical | C | Cells | Operating table | Beds along walls, trolleys | Curtained bays | S, P, L |
| Ward | Medical | C | Aisles | — | Bed rows | Rows of beds as low cover | S, P, L |
| Pharmacy | Medical | G2 | Service | Counter | Shelf rows | Counter in front, shelves behind | S, P, L |
| Morgue | Medical | C | Service | Drawer wall | Slabs on a lattice | Cold, narrow gaps | S, P, L |
| Security Office | Security | C | Cells | Checkpoint barrier | Desks, lockers | Checkpoint funnels in | S, P, L |
| Armory | Security | C | Aisles | Weapon rack wall | Rack rows, crates | Narrow aisles, hard cover | S, P, L |
| Barracks | Security | C | Aisles | — | Bunk rows, lockers | Rows across the room | S, P, L |
| Engine Room | Engineering | C | Service | Turbine block on a wall (*Plant*) | Control station, pipe run; nozzles, pumps, radiators, hazard cordon by layout | Narrow gaps, hazard in the centre | S, P, L, K |
| Reactor Core | Engineering | C | Ring | Reactor | Consoles round it | Circle the core; no sight across | Landmark |
| Life Support | Engineering | C | Service | Scrubber on a wall (*Plant*) | Pipe run; tanks, pumps, scrubber ring, grow racks by layout | Cramped, machine noise | S, P, L, K |
| Workshop | Engineering | C | Service | Workbench line | Tool racks, crates | Medium clutter | S, P, L |
| Processing Plant | Engineering | C | Aisles | Conveyor line (*Plant*) | Service corner; machines on a lattice, smelters by layout | Production lines as lanes | S, P, L, K |
| Power Distribution | Engineering | C | Service | Transformer standing free (*Plant*) | Breakers, control station; breaker walls, capacitor rows, transformer ring by layout | Live panels round the walls, one big block in the middle | S, P, L, K |
| Waste Reclamation | Engineering | C | Hall | Compactor on a wall (*Plant*) | Recycling bins by it, service corner; sorting belt, hoppers, incinerators by layout | Bins and machines as scattered hard cover | S, P, L, K |
| Fuel Depot | Engineering | C | Service | Fuel tank on its side, back to a wall (*Plant*) | Fuel pumps by it, hazard cordon, pipe run; tank rows, bunkers, pump grid by layout | Long tanks make lanes; don't fight near them | S, P, L, K |
| Coolant Plant | Engineering | C | Hall | Heat exchanger on a wall (*Plant*) | Pumps round it, pipe run, control station; radiator rows, pump grid, radiator ring by layout | Radiator rows block sight, pumps as waist cover | S, P, L, K |
| Maintenance Hub | Engineering | C | Service | Tool rack on a wall (*Plant*) | Breakers, pipe run; crates, shelves, workbenches by layout | Small and cluttered; where the crawlways meet | S, P, L, K |
| Bridge | Command | C | Ring | Captain's chair and command table | Console arc | Arc of consoles facing the front | S, P, L |
| Control Room | Command | C | Aisles | Main screen wall | Console rows | Rows facing one wall | S, P, L |
| Comms Array | Command | C | Ring | Transmitter mast standing free (*Plant*) | Breakers; console rows, signal racks, big screen by layout | Racks round the walls, the mast in the middle | S, P, L, K |
| Offices | Command | C | Cells | — | Desks, cubicle walls (`OfficeMaze`) | Maze of short walls | S, P, L |
| Hydroponics Bay | Agriculture | C | Aisles | — | Grow racks on a lattice | Lanes between racks | S, P, L |
| Farm | Agriculture | C | Hall | — | Crop beds, irrigation | Low cover, long views | P, L |
| Water Treatment | Agriculture | C | Service | Tank on a wall (*Plant*) | Pipe run; tank grid, filters, settling basins, pump grid by layout | Hard cover, narrow gaps; open water in the settling hall | S, P, L, K |
| Laboratory | Science | C | Cells | Lab bench island | Benches, fume hoods on walls | Bench islands as cover | S, P, L |
| Data Center | Science | C | Aisles | — | Server racks on a lattice | Narrow aisles of tall racks | S, P, L |
| Library | Science | G1 | by layout | Front desk by the way in | Shelving, reading tables with chairs, carrels, sofas and lamps, card catalogue | Shelves block sight; reading area open | S, P, L |
| Chapel | Worship | G2 | Aisles | Altar | Pew rows | Pews as low cover facing the altar | S, P, L |
| Shrine | Worship | G2 | Ring | Shrine object | Candles, mats | Small, one centrepiece | S, P, L |
| Lobby | Transit | G1 | Hall | Reception desk | Seating, planters | Open hall with a desk | S, P, L |
| Junction | Transit | — | Strip | — | — | A crossing of routes; keep it bare | — |
| Cargo Bay | Transit | C | Hall | Loader | Container stacks | Dense pillar grid of crates | S, P, L |
| Docking Bay | Transit | C | Hall | Airlock | Cargo, fuel lines | Wide grid, long lanes | S, P, L |
| Hangar | Transit | C | Hall | Shuttle | Tool carts, crates | Huge, one big obstacle | S, P, L |
| Escape Pod Bay | Transit | C | Strip | Pod row on a wall | Lockers | Long lane along the pods | S, P, L |
| Storage | Transit | C | Aisles | Shelf wall | Crate stacks | Narrow aisles | S, P, L |

---

## 9. Agents in the room

The map paints faction territory, room generation builds each faction's rooms to its
priorities, and the faction AI fights in them. This section is the seam between the last
two: where a room's enemies stand when it is built, and who patrols and who holds a post.
Before it, enemies stood on random free tiles and patrolled the room's four outermost corners,
whatever the room was made of, and a Breach room's two factions were mixed across both sides.

### Post kinds

Once a room is furnished, every floor tile an enemy may start on (inside the spawn margin,
off pillars, props, doorways and the centre spot) is read for what it offers (`RoomPostMap`):

| Kind | Where | Reads |
|---|---|---|
| **Concealed** | Under a concealment prop (patch, shadow pocket) | Hidden until someone is close |
| **Overwatch** | On a lane of at least 6 open tiles, with hard cover beside it | A firing position down a line of fire |
| **Choke** | At most 2 open tiles across one way | A corridor, aisle or gap between pillars |
| **Dead end** | 3 or more steps from every route | Out of the traffic, a back corner |
| **Route** | On or beside a walkway (doorways to the centre) | Where everyone passes |

A tile can be several kinds at once.

### Who stands where

Each `FactionDesign` has *Posts* (`PostPreference`):
- a **weight** for each kind: how much likelier a tile of that kind is than plain floor (weight 1);
- the kinds that make an enemy **hold** its post (stand guard, scanning) instead of patrolling.

| | BIO | VOID | TECH |
|---|---|---|---|
| Favours | Concealed ×4, choke ×1.5 | Dead end ×3, choke ×2.5 | Overwatch ×4, route ×1.5 |
| Holds on | Concealed: an ambush in the bushes | Dead end: waiting in the dark | Overwatch: covering the lane |

So the priorities a room is built to (section 5) are the ones its enemies use: BIO rooms get
concealment and BIO enemies wait in it.

`RoomEnemyPlanner` picks the posts. It is shared by the level and the Room Lab, like the furnisher.
- **Picking.** Spawns are picked one by one, at random in proportion to their weights, at least
  two tiles apart where the room allows. They come from the room's own seed, so the same seed
  puts enemies in the same places.
- **Breach rooms split by zone** (section 6) put each faction's enemies on its own side, with
  its own preference.
- **Patrols.** Enemies that don't hold their post patrol a loop through the outermost walkable
  tiles of their side. These are never a pillar or a prop, as the old corners could be.
- **Fallback.** A room without a design, or with no tile of the favoured kinds, spreads its
  enemies over plain floor as before.

The Room Lab's **Posts** layer paints the kinds and marks each planned spawn: a ring for one
holding its post, a dot for a patroller, with the patrol loop drawn per side.

### Next steps for the ecosystem

- **Territory depth.** Done: see section 10.
- **Agents using places at runtime** and **watching from their post**: done, but from each
  enemy's own experience rather than the room's post map. See section 11.

## 10. Territory depth (Phase 9)

The map's faction painter grows each territory out from an origin room. Each room gets an
*influence*: 1 at the origin, ×0.85 for every room further out, never below 0.35. Rooms an
anomaly flipped get 0.5. Room generation turns influence into how strongly the holding
faction's design shows, its **strength** (`LevelBuildSettings` › *Territory Depth*):

| Influence | Strength | Reads as |
|---|---|---|
| 0.7 and up (origin and the first two rings) | 1 | **Core**: the faction's architecture at its purest |
| between | rises evenly | **Held** |
| 0.35 and below | 0.4 | **Frontier**: the ship showing through, the faction thinning out |

A room whose node has no influence (0, as in old graphs) is built at full strength.

What strength does (`FactionStyle`, a design at a strength). Each number moves from the
neutral value toward the design's by the strength:
- **Floor plan.** Room size scale and shape weights. A shape the design never uses (weight 0)
  stays unused.
- **Structure.** The design's fallback structure is always built. Each of its *Additions*
  (wall remnants, fold walls) is built with chance equal to the strength.
- **Dressing.** Fill share, prop scale and the concealment step all blend. The room type's
  cover is swapped for the faction's version with chance equal to the strength, decided once
  per room. The faction's own props come in at the strength times their count.
- **Agents.** Post preferences move toward plain floor (×4 at core is ×2.2 at strength 0.4).
  Holding stays as it is.

Rooms at full strength draw no extra random numbers, so core rooms build exactly as before
from the same seed. A Breach room's breaching side is always at full strength: it is the
other reality bleeding in. The Room Lab's recipe has an *Influence* slider, and the report
shows the room's depth and strength.

## 11. Enemies learn their ground (Phase 10)

Section 9 places enemies when a room is built, from the room's own data. What they do
afterwards must come from what *they* know. An enemy is never handed the room's post map.
It learns the ground it walks, the way it already learns cover (`CoverKnowledge`): by being
there and looking. The longer it roams a room, the better it knows it, and it fights from
what it knows.

### What an enemy learns (`EnemyGround`, `PlaceMemory`)

About once a second, while it moves about, an enemy takes stock of where it stands. It sends
eight rays out at eye height (`GroundSense`) and reads the spot:

| Trait | How it tells | Means |
|---|---|---|
| **Hidden** | It is standing inside a concealment zone | A place to lie in wait |
| **Vantage** | One ray runs 15 m or more, and something solid stands within 1.5 m beside it | A firing position down a lane, with cover to duck behind |
| **Tight** | Some pair of opposite rays adds up to 4.5 m or less | A corridor or aisle |

It also spots concealment zones it can see from up to 20 m, and remembers them as hidden
places before it has been in them.

Each place is remembered by a 2.5 m cell, with:
- its traits;
- the way its longest ray ran (a vantage's lane);
- a **familiarity**. Each visit adds 0.25, a sighting from afar 0.15, and it never goes above 1.
  The ground an enemy walks every day becomes the ground it trusts;
- a **danger** level. It is set where the enemy was hurt and where it saw an ally fall, and
  fades over 30 s.

An enemy keeps at most 48 places and forgets the least familiar first. It forgets everything
when it is pooled and spawned again. Kin within 6 m pass on what they know well (familiarity
0.5 and up) every few seconds, at half the familiarity: a patrol that walked the east wing
tells the guard at the door.

### What it does with it

Each faction values the traits differently (`FactionDoctrine` › *Ground*):

| | BIO | VOID | TECH |
|---|---|---|---|
| Hidden / Vantage / Tight | 3 / 0.5 / 1 | 1 / 0.5 / 3 | 0.5 / 3 / 1 |
| Falls back to known ground | yes | yes | yes |
| Danger aversion | 1 | 0.5 | 1.5 |

- **Falling back.** When its morale breaks, an enemy whose doctrine says so runs to the best
  place it knows. A place's score is its value to the faction × familiarity × (1 − danger ×
  aversion) − distance / 25 m. Fresh danger at aversion 1 makes a place worthless until it fades. Only places within 25 m count, at least 6 m from the threat
  and no closer to it than the enemy is now. It holds there as it would in cover. With nothing
  good known, it falls back as before (cover, kin or its post).
- **Watching from its post.** A guard whose post it knows as a vantage faces down that lane
  instead of the way it happened to spawn.

Nothing here reads the level's tile data. A room built with concealment and lanes (sections
5 and 9) is simply ground worth learning: a BIO enemy that has walked its room knows where the
patches are, and one that wandered in from elsewhere doesn't yet.

The spectator's *Paths & plans* overlay shows the inspected agent's known places. Green
means hidden, red vantage, orange tight; larger means more familiar, and a cross marks danger.

### Next

- **Searching from memory.** A belief search could weigh the hidden places it knows (the
  target may be lying where *it* would).
- **Ambush from memory.** BIO hunters could set up in a known hidden place along the
  target's path instead of only where they spawned.

## 12. Layouts and set pieces (Phase 11)

A place should come out in many shapes, sizes and arrangements and still read as that place.
Two things make that work:
- **Layouts** give a place several arrangements.
- **Set pieces** keep each arrangement recognisable at any size.

### Layouts

A `RoomFunction` can list `RoomLayout`s (*Layouts*), and each room of that function picks one
by weight. The pick has its own seed layer, so places without layouts roll exactly as before.
A layout overrides only what it sets: floor plans, cells, size and structure. It also adds its
own set pieces. The function keeps what every arrangement of the place shares. The rule is:

> **The function says what the place is; the layout says how this one is arranged.**

Props go down in this order:
1. The function's anchor: its *Required* pieces, the same in every layout (the casino's
   cashier cage, the library's front desk, the theatre's stage). They go first so the
   layout's pieces can stand *Near* them and face them (desks facing the teacher, tables
   facing the stage).
2. The layout's set pieces: what the arrangement is built around (the pool basin, the
   card-table pits, the seat rows).
3. The function's other props, the same in every layout (the pool's loungers round whatever
   water the layout put down).
4. The function's shared kits (*Sets*, below), the same in every layout. A layout's own
   kits go down right after its set pieces.
5. Everything after that, as before: side dressing, room type cover, faction props.

A function with no layout places its props in their own order.

Every generated place has two to four layouts. Junction stays bare on purpose, and the
Reactor Core is a hand-built landmark. Casino, Library and Pool are written out in full below
as worked examples; the layout assets in `Data/Level/Functions/Layouts/<Place>/` hold the rest.

| Place | Layouts (cells or size · structure) |
|---|---|
| Quarters | **Suite** (9–11 tiles · DividerWall); **Twin Cabin** (7–9 tiles · CornerPillars); **Penthouse** (2×1 · CornerPillars) |
| Cabin Row | **Cabin Corridor** (2×1 · OfficeMaze); **Dormitory** (10–12 tiles · LowRows) |
| Commons | **Lounge** (11–12 tiles · CornerPillars); **Rec Room** (2×1 · WidePillarGrid, GuestPavilions); **Hearth** (10–12 tiles · ColumnRing) |
| School | **Classroom** (9–11 tiles · CornerPillars); **Lecture Hall** (2×1 · AcrossLowRows); **Study Room** (8–10 tiles · CornerPillars) |
| Park | **Garden** (2×1 · the faction’s); **Grove** (2×1 · the faction’s); **Plaza Garden** (10–12 tiles · CornerPillars, HubRing) |
| Gym | **Weight Room** (10–12 tiles · LowRows); **Fitness Hall** (2×1 · WidePillarGrid, GuestPavilions) |
| Observation Deck | **Window Gallery** (2×1 · CornerPillars); **Star Lounge** (10–12 tiles · ColumnRing) |
| Wellness Center | **Treatment Suites** (9–11 tiles · OfficeMaze); **Spa Lounge** (9–11 tiles · CornerPillars) |
| Pool | **Lap Pool** (2×1 · CornerPillars, GuestPerimeterPavilions); **Lagoon** (2×2 · CornerPillars, GuestPerimeterPavilions); **Spa Terrace** (8–11 tiles · CornerPillars) |
| Theatre | **Auditorium** (2×1 · AcrossLowRows); **Cabaret** (10–12 tiles · CornerPillars); **Amphitheatre** (2×1 · AcrossLowRows) |
| Bar & Lounge | **Lounge Bar** (8–10 tiles · ServiceCounter); **Pub** (9–11 tiles · CornerPillars); **Cocktail Club** (9–11 tiles · ColumnRing) |
| Casino | **Slot Hall** (2×1 · LowRows); **Table Pits** (10–12 tiles · ColumnRing, CornerPillars); **High-Roller Salon** (7–9 tiles · CornerPillars) |
| Exhibit Hall | **Gallery** (10–12 tiles · ThreeLanes); **Sculpture Court** (10–12 tiles · HubRing) |
| Grand Atrium | **Fountain Court** (2×2 · CornerPillars, HubRing); **Galleria** (2×2 · LowThreeLanes) |
| Restaurant | **Dining Room** (10–12 tiles · DividerWall); **Bistro** (8–10 tiles · DividerWall); **Banquet Hall** (2×1 · DividerWall) |
| Food Court | **Food Hall** (2×1 · WidePillarGrid, GuestPavilions); **Kiosk Ring** (2×1 · ColumnRing) |
| Kitchen | **Galley** (8–10 tiles · ServiceCounter, CornerPillars); **Line Kitchen** (9–11 tiles · LowRows) |
| Promenade | **Arcade** (2×1 · CornerPillars); **Boulevard** (2×1 · LowThreeLanes) |
| Medical Bay | **Surgery** (9–11 tiles · DividerWall); **Clinic Bays** (2×1 · OfficeMaze) |
| Ward | **Long Ward** (2×1 · LowRows); **Bay Ward** (2×1 · OfficeMaze) |
| Pharmacy | **Counter Shop** (6–8 tiles · ServiceCounter, CornerPillars); **Dispensary Aisles** (8–10 tiles · ShelfRows) |
| Morgue | **Cold Room** (7–9 tiles · CornerPillars); **Autopsy Suite** (8–10 tiles · DividerWall) |
| Security Office | **Checkpoint** (10–12 tiles · CheckpointBarrier); **Holding Cells** (10–12 tiles · OfficeMaze) |
| Armory | **Rack Aisles** (8–10 tiles · ShelfRows); **Vault** (6–8 tiles · CornerPillars) |
| Barracks | **Bunk Rows** (10–12 tiles · LowRows); **Bunk Bays** (2×1 · OfficeMaze) |
| Workshop | **Fab Shop** (10–12 tiles · CornerPillars); **Assembly Bay** (2×1 · ThreeLanes) |
| Processing Plant | **Line Hall** (2×1 · LowRows); **Sorting Floor** (2×1 · ThreeLanes); **Smeltery** (2×1 · WidePillarGrid) |
| Life Support | **Scrubber Hall** (10–12 tiles · CornerPillars); **Plant Room** (9–11 tiles · DensePillarGrid); **Atmosphere Processor** (10–12 tiles · ColumnRing); **Air Garden** (10–12 tiles · WidePillarGrid) |
| Engine Room | **Turbine Hall** (2×1 · CornerPillars); **Drive Gallery** (2×1 · LowRows); **Thruster Bay** (2×1 · CornerPillars); **Drive Core** (2×1 · ColumnRing) |
| Power Distribution | **Switchgear** (9–11 tiles · CornerPillars); **Capacitor Hall** (2×1 · LowRows); **Substation** (10–12 tiles · ColumnRing) |
| Waste Reclamation | **Sorting Line** (2×1 · LowRows); **Compactor Bay** (10–12 tiles · CornerPillars); **Incinerator Hall** (2×1 · WidePillarGrid, ServicePavilions) |
| Fuel Depot | **Tank Row** (2×1 · CornerPillars); **Pump Room** (9–11 tiles · DensePillarGrid); **Bunkers** (2×1 · CornerPillars) |
| Coolant Plant | **Radiator Gallery** (2×1 · ShelfRows); **Pump Hall** (2×1 · WidePillarGrid); **Cryo Loop** (10–12 tiles · ColumnRing) |
| Maintenance Hub | **Crawlway** (7–9 tiles · CornerPillars); **Parts Store** (8–10 tiles · ShelfRows); **Repair Bay** (9–11 tiles · CornerPillars) |
| Comms Array | **Signal Room** (9–11 tiles · AcrossLowRows); **Antenna Gallery** (10–12 tiles · ColumnRing); **Relay Closet** (7–9 tiles · CornerPillars) |
| Bridge | **Command Deck** (2×1 · ColumnRing); **Flight Bridge** (2×1 · AcrossLowRows) |
| Control Room | **Operations** (9–11 tiles · AcrossLowRows); **Monitoring Ring** (10–12 tiles · ColumnRing) |
| Offices | **Cubicles** (9–11 tiles · OfficeMaze); **Open Plan** (2×1 · LowThreeLanes) |
| Hydroponics Bay | **Rack Rows** (2×1 · ShelfRows); **Grow Hall** (2×1 · LowThreeLanes) |
| Farm | **Fields** (2×1 · the faction’s); **Orchard** (2×1 · the faction’s) |
| Water Treatment | **Tank Farm** (2×1 · CornerPillars); **Filter Gallery** (2×1 · LowRows); **Settling Pools** (2×1 · CornerPillars); **Pump Station** (9–11 tiles · DensePillarGrid) |
| Laboratory | **Wet Lab** (9–11 tiles · DividerWall); **Clean Rooms** (2×1 · OfficeMaze) |
| Data Center | **Server Aisles** (8–10 tiles · ShelfRows); **Core Vault** (9–11 tiles · ColumnRing) |
| Library | **Stacks** (10–12 tiles · ShelfRows); **Reading Hall** (2×1 · WidePillarGrid); **Rotunda** (10–12 tiles · ColumnRing) |
| Chapel | **Nave** (9–11 tiles · AcrossLowRows); **Round Chapel** (9–11 tiles · ColumnRing) |
| Shrine | **Alcove Shrine** (6–8 tiles · ColumnRing); **Garden Shrine** (7–9 tiles · CornerPillars) |
| Lobby | **Reception Hall** (1×1 · CornerPillars, HubRing); **Waiting Lounge** (9–11 tiles · LowRows) |
| Cargo Bay | **Container Yard** (2×1 · DensePillarGrid, ServicePavilions); **Loading Dock** (2×1 · ThreeLanes) |
| Docking Bay | **Airlock Hall** (1×1 · WidePillarGrid, ServicePerimeterPavilions); **Fuel Dock** (1×1 · CornerPillars) |
| Hangar | **Flight Deck** (2×2 · CornerPillars, ServicePerimeterPavilions); **Maintenance Bay** (2×1 · CornerPillars, ServicePavilions) |
| Escape Pod Bay | **Pod Gallery** (8–10 tiles · CornerPillars); **Muster Station** (9–11 tiles · LowRows) |
| Storage | **Shelf Aisles** (8–10 tiles · ShelfRows); **Crate Stacks** (8–10 tiles · WideShelfRows) |

**Worked examples.**

| Place | Layout | Cells / size | Shapes | Structure | Set pieces |
|---|---|---|---|---|---|
| Casino | **Slot Hall** | 2×1 | Rectangle, Long Hall, Stepped | `LowRows` (slot banks) | Slot machines lining both faces of the rows, bar (110+ tiles), lounge stage (250+), roulette (200+) |
| | **Table Pits** | 10–12 | Rectangle, Dome, Cross, Bays | `ColumnRing`, `CornerPillars` | Card tables on a 2×2 lattice, mirrored; roulette (90+); rugs among the pits; bar (110+) |
| | **High-Roller Salon** | 7–9 | Rectangle, Dome, Bays | `CornerPillars` | One or two card tables, a bar, sofas with rug and lamp, planters |
| Library | **Stacks** | 10–12 | Rectangle, Long Hall, L | `ShelfRows` | Shelves lining the rows, study carrels on the walls, a reading nook off the routes |
| | **Reading Hall** | 2×1 | Rectangle, Bays, Long Hall | `WidePillarGrid` | Shelves round every wall, reading tables on a 2×3 lattice, mirrored; lamps and rugs among them |
| | **Rotunda** | 10–12 | Dome, Ring | `ColumnRing` | Shelves round the dome, reading tables, sofas facing the middle |
| Pool | **Lap Pool** | 2×1 | Rectangle, Long Hall | `CornerPillars`, `GuestPerimeterPavilions` | A basin 60% × 30% of the hall (it may cross the walkways), one or two jacuzzis by it |
| | **Lagoon** | 2×2 | Dome, Rectangle, Cross | `CornerPillars`, `GuestPerimeterPavilions` | A basin half the room each way, a children's pool (400+), a slide into the water (150+), jacuzzis, palms |
| | **Spa Terrace** | 8–11 | Rectangle, Bays, Dome | `CornerPillars` | A plunge pool, two or three jacuzzis |

What each place keeps in every layout:
- **Casino**: a required cashier cage on a wall, and slot machines along the walls.
- **Library**: a required front desk on a wall within a step of a route (by the way in), and the card catalogue beside it.
- **Pool**: pairs of loungers under a parasol facing whatever is keyed *Pool* (basins and jacuzzis), the pool bar (100+ tiles) and palms in the corners.

In the worked examples, the numbers in brackets are *Min Room Tiles*. A multi-cell layout falls back to one cell
where its neighbours leave no room, and its set pieces still fit, because they grow with the
room or are left out by size.

### Set pieces

These options on `RoomPropPlacement` (every function, dressing and layout has them) keep a
place recognisable at any size:
- **Groups** (*Members*): pieces set round each prop on its tiles, turning with it, each with
  its own chance. Examples are two to four chairs round a card table, stools along a bar and a
  rug and lamp with a sofa. A group with no prefab of its own is just its members, like a pair
  of loungers and a parasol.
- **Relations** (*Key*, *Near*, *Near Distance*, facing *Toward Near*): a placement names what
  it put down, and later ones stand within a few steps of it and can face it. Loungers face the
  pool, the slide runs into the water and the catalogue stands beside the desk. If the key put
  nothing down, nothing is placed; a *Required* one is still placed anywhere.
- **Features that grow** (*Grow*, *Stretch*): the footprint is a share of the room's longer and
  shorter side, tried a few sizes smaller where it doesn't fit. The prefab is built for one tile
  and stretched to the footprint, so one basin fits every pool. Such a feature is part of the
  room's plan: it neither stops at nor uses up the fill share, so the furniture round it keeps
  its share.
- **Pieces for big rooms** (*Min Room Tiles*): left out of a smaller room (or Breach side). That
  is not reported as a shortfall.

### Shared kits (prop sets)

A `RoomPropSet` (`Data/Level/Functions/Sets/`) is a named list of placements that several
places and layouts list under *Sets*, so a kit is tuned once and reads the same in every room
that has it. A kit's pieces can stand *Near* a key the room set before them, so a kit can
gather round whatever a place is built round.

| Kit | What it puts down | Used by |
|---|---|---|
| **Control Station** | One or two consoles facing the *Plant* within 4 steps, each with its chair behind | Engine Room, Power Distribution, Coolant Plant; Life Support's Atmosphere Processor |
| **Pipe Run** | Two to four pipe stacks in a line along a wall | Engine Room, Fuel Depot, Coolant Plant, Maintenance Hub, Life Support, Water Treatment; Kitchen, Laboratory, Hydroponics Bay, Pool, Escape Pod Bay |
| **Hazard Cordon** | Two to four barriers within a step of the *Plant*, facing it (72+ tiles) | Fuel Depot; Engine Room's Drive Gallery and Drive Core, Power's Substation, Processing's Smeltery |
| **Service Corner** | A tool rack on a wall, crates in a corner, perhaps a trolley | Waste Reclamation, Processing Plant; Workshop, Hangar, Docking Bay, Cargo Bay, Storage |
| **Breakers** | One to three breaker panels on the walls | Power Distribution, Maintenance Hub, Comms Array; Control Room, Bridge, Data Center, Security Office, Docking Bay, Hangar |
| **Terminal Bank** | One or two consoles against the walls, floor kept free in front | Control Room, Bridge, Data Center, Offices, Security Office, Laboratory, Docking Bay, Hangar |
| **Supply Stack** | Crate piles in corners and along walls, perhaps a trolley | Storage, Cargo Bay, Docking Bay, Hangar, Armory, Workshop, Kitchen, Pharmacy, Escape Pod Bay, Barracks |
| **Locker Row** | Two to four lockers nose to tail along a wall | Barracks, Gym, Security Office, Armory, Quarters, Cabin Row, Workshop, School, Pool, Wellness Center |
| **Seating Nook** | One or two benches against the walls with floor in front | Lobby, Promenade, Commons, Observation Deck, Wellness Center, Grand Atrium, Food Court, Medical Bay, Docking Bay |
| **Greenery** | One to three planters in corners and along walls | Grand Atrium, Lobby, Promenade, Commons, Offices, Restaurant, Wellness Center, Observation Deck, Chapel |

The first five are tuned for the operations places (the Control Station, Hazard Cordon and the
operations Service Corner gather round the *Plant*); the last five stand Near nothing, so any
place may list them. Every kit's counts scale with the room (`ScaleByRoomSize`) where it is a
filler, so a closet and a hall are equally furnished.

**Growing the ecosystem.** To add a kit: make the asset under `Sets/` (copy one, give it a new
GUID in its `.meta`), then list it under *Sets* on each place or layout that wants it; the Room
Lab and *Check layouts* show it as a *Kit* piece. To add a place: a function, 2+ layouts in
`Layouts/<Place>/`, kits from the table, and its row in section 8. `PropSetTests` keeps the
books: every kit is listed somewhere, no place lists one twice, every generated place has two
layouts, and nothing stands Near a key that isn't set before it.

**Shapes in the wider ship.** Waist and Comb are no longer operations-only: Storage's Aisle
Comb, Barracks' Bunk Comb (bays off a spine) and Cargo Bay's Dock Neck (two halls joined by a
neck) use them, copies of an existing layout of the place on the new shape. A layout that only
changes the shape is the cheapest new arrangement: *Duplicate* it and swap the shape.

### Ship operations

The places that keep a colony ship flying, lit, breathing, watered and clean follow one
convention, so kits and new layouts fit any of them:
- **The main machine is keyed *Plant*.** Each place's Required piece (turbine, transformer,
  compactor, fuel tank, heat exchanger, tool rack, mast, scrubber, tank, conveyor) has *Key*
  `Plant`, and the kits stand *Near* it. A new operations place keys its machine the same way
  and lists the kits it wants.
- **Wall machines, floor machines.** A machine with a back (a compactor, an exchanger) goes
  against a wall; one you walk round (a transformer, a mast) stands free in the largest open
  area with a tile of floor round it.
- **Three or more layouts each**, from a cramped one-cell room to a two-cell hall: rows (aisle
  fights), a grid (scattered hard cover) and a ring or a bay (circling). The *Waist* shape (two
  halls joined by a neck) and the *Comb* shape (bays off both sides of an aisle) are for these.

`PropSetTests` holds them to it: every operations place keys its machine *Plant*, has a kit and
three layouts, and nothing anywhere stands *Near* a key that isn't set before it.

### Authoring helpers

- **New layout**: on a place's Inspector, type a name and *Create*. The layout is made in
  `Data/Level/Functions/Layouts/<Place>/<Name>RoomLayout.asset` and added to the place's list.
  On a layout's Inspector, *Duplicate* makes a variant of it the same way.
- **Check**: on a layout's Inspector, *Check* builds it over a run of seeds (4–48) through the
  level's own pipeline and lists every piece it can get: where it comes from (anchor, set piece,
  kit, place), how often it went down and how many of what it asked for. A piece that sometimes
  goes missing is red; hover it for why. The verdict flags failed rooms, a missing Required
  piece, floor cut off and a set piece that fits less than half the time, as `RoomLayoutTests`
  would. On a place's Inspector, *Check layouts* does every layout at once, one line each.
- **Placeholder props**: the Placeholder Prop Builder (section 7).

### Seeing it

- The **Room Lab** has a *Layout* choice under *Place*. *Open in Room Lab* on a layout asset
  opens its place in it, and the report names the layout.
- The **Map Graph** side panel pins a layout under a pinned place, and regeneration keeps it.
- The **Room Gallery** builds a room per layout (*Every Layout*), and the placard names the layout.

### Next

- A third layout for the places that have two, starting with the guest places seen most.
- Let a faction prefer layouts: VOID the salon's tight cells, TECH the hall's long lanes. That
  could be a weight multiplier per layout on `FactionDesign`, like its shapes.
- Let the check also report the room's traits per layout, so a layout can be tuned toward the
  fight it should play as.
- Placeholders are stretched boxes, so a pool's deck rim thickens with its size. Real art
  needs a basin that can be built to any footprint, or nine-sliced.

## 13. Buildings and towns (Phase 13)

A building (a school, a hospital, a library) has the same interior wherever it stands: a hall
with walled rooms, each furnished by its own place. In a ship room it is one arrangement of the
ordinary place (`School Floor`, `Hospital Floor`, `Library Floor` layouts, whose structure is a
`BuildingStructureRule`); in a town it stands on a lot with a shell, a front door and a roof. A
town is one room (the Town Square place) holding several buildings among open streets. All of
it is made from the pieces every room uses: inner walls, doorways and zones. Nothing changes in
the map graph or the corridor router.

| Piece | Role | Where |
|---|---|---|
| `BuildingPlan` | Data: hall width, the rooms wanted (place, count, width, placement) | `Data/Level/Buildings/` |
| `BuildingLayoutPlanner` | Plain C#: tiles a lot with the hall and rooms, returns a `BuildingBlueprint` (rectangles, doors, front door, rooms that didn't fit) | `Scripts/Level/Buildings/` |
| `BuildingBlueprintWriter` | Writes a blueprint into a `RoomStructure`: a wall between any two spaces, a lintel doorway at each door, each space registered with its place | same |
| `BuildingStructureRule` | The room is the building (a layout's structure) | `Data/Level/Structure/` |
| `TownStructureRule` | The room is streets with a grid of lots, each a walled, roofed building | same |

**Layout.** The hall runs in from the front door, `Entrance` rooms first beside it, then the
required rooms, then the optional, each on whichever side has more frontage left; leftover
frontage is filled by `Fills` rooms or the last room grows. A `Back` room closes the far end.
The plan tiles the whole lot (no floor is left unassigned), and a lot too small for a hall and
a room gets no building.

**Zones and furnishing.** Doorways through a building's walls are added with
`RoomStructure.AddOpening(…, separatesZones: true)`, so each room, the hall and the streets
are zones of their own. Each space is recorded (`RoomStructure.Spaces`) with a tile and its
place; `RoomFurnisher` then furnishes each space from that place's props in its own zone only,
through the same zone argument a Breach room uses. A space's place gives its furniture, not
its own structure rules.

**Safety.** The walls go in through `TryAddPartition`: if they would cut any floor off from the
room's doorways, nothing is built (the Room Lab warns). In a town, lots that would swallow a doorway, the centre spot
or an earlier structure are left as plazas.

**Open items.**
- A room's layout (`BuildingSpaceSpec` › `_layout`, or `_pickLayout`) furnishes it with the
  layout's set pieces and sets; the layout's structure rules (rows of seats built as walls)
  are not applied inside a room. Zone rules in its props are read against the room alone
  (`RoomPropPlacement.ForSingleZone`: "main space" is the whole room, "others" are skipped).
- The Restroom place (`Data/Level/Functions/`, not in the build settings' list) is used by the
  School, Hospital, Library and House plans; it has no stalls, only toilets, sinks and a bin.
- A town's room is open-air: it sits under a sky dome (`SkyDomeBuilder`, shared with parks,
  *Sky domes* in the build settings). Its buildings are roofed (`StructureSpace.Roofed`, set
  by a shell-walled blueprint; `LevelGeometryBuilder.BuildCeilings` roofs those tiles even
  when ceilings are off) and lit by `BuildingLightBuilder`. The town has no sun light or sky
  ambience yet, only the dome to look at.
- The Town Square is in the build settings' place list at a low weight and `_unique`, so the
  ordinary weighted pick gives a map a town only sometimes. Tune the weight from Room Sweep
  and map runs.
- A building in a ship room is the ordinary room behind a door in a corridor wall. A distinct
  frontage on the corridor side (a facade matching the place) is not built: the doorway signs
  already name the place.
- A room with a building layout skips its place's own props (they would land in a hall), and
  a building layout has no set pieces of its own, so the layout tests that count set pieces
  skip it. The Room Lab reports a building that wasn't built (room too small, or its walls
  would cut floor off), the rooms a plan had no space for (`RoomStructure.LeftOut`) and any
  room too small for a piece.
- Not compiled or run in Unity (see the roadmap).

## 14. Composition and props in sets (Phase 14)

A room should read clearly from its floor plan alone, with even the plainest props. Two rules
make that work:
- **Composition comes from structure.** A big open room gets a known map pattern, not just a
  grid of pillars.
- **Props come in sets.** Every piece stands with the pieces it belongs to, or faces what it
  serves. Nothing is strewn about singly at a random angle.

### Composition patterns

| Pattern | Rule | How it plays | Layouts |
|---|---|---|---|
| **Three lanes** | `ThreeLanes` (2.4 m), `LowThreeLanes` (1.1 m) | Two walls down the room: a wide, exposed middle lane and two flanking lanes, joined at the ends and through one or two crossings | Boulevard, Galleria, Open Plan, Grow Hall (low); Gallery, Loading Dock, Assembly Bay, Sorting Floor (high) |
| **Hub** | `HubRing` (1.1 m rail) | A court round the centrepiece, a gap in each side, a loop round it: fights circle the court | Fountain Court, Sculpture Court, Reception Hall, Plaza Garden |
| **Pavilions** | `GuestPavilions`, `ServicePavilions` (anywhere); `…PerimeterPavilions` (along the walls) | Walled blocks you walk round break a hall into pockets; along the walls they make alcoves off an open middle | Food Hall, Rec Room, Fitness Hall, Container Yard, Maintenance Bay, Incinerator Hall (anywhere); Lap Pool, Lagoon, Flight Deck, Airlock Hall (perimeter) |
| **Two halves** | `DividerWall` (existing) | A main floor and a side room through two openings | Restaurant, Medical Bay, Laboratory and others (section 2) |

Rules for authoring them:
- **Lanes replace pillars.** A wall stays open beside a pillar, so a pillar grid would punch
  holes all along the lane walls. A lane layout lists the lane rule *instead of* its grid.
- **Pavilions go after pillars.** A pavilion only stands on open floor, with free floor all
  round it and nothing solid in its margin. List it after the room's pillar rules, so no
  pillar lands inside one.
- **A pavilion is an annex.** It is walled in with one doorway facing the room's middle and
  recorded as a space (`StructureSpace.Annex`). Its kind's place furnishes the inside alone: a
  Restroom Block gets toilets and sinks, a Bar Hut a counter, a Store Shed shelves. The room's
  own furniture, side dressing, cover, concealment and faction props go only on the floor round
  it (`RoomStructure.ZonesOutsideAnnexes`). A room's set pieces, like a pool basin, never
  straddle a pavilion's wall.
- All three keep every walkway and doorway open, and their walls go in through
  `TryAddPartition`, so the room stays one connected space. None of them divides zones, except
  that each pavilion is a zone of its own.

| Pavilion | Kinds (place) | Size |
|---|---|---|
| Guest | Restroom Block (Restroom), Bar Hut (Bar), Kiosk (Pharmacy) | 2×3–3×3, 3×3–3×4, 2×2–2×3 |
| Service | Store Shed (Storage), Guard Booth (Security Office), Tool Shed (Workshop) | 2×3–3×3, 2×2–2×3, 2×3–3×3 |

A room gets one or two, one for every 90 floor tiles at most, 2.6 m walls (high cover).

### Props in sets

The faction dressings, which every room a faction holds gets, are written as sets:

| Reality | Sets |
|---|---|
| **VOID** | Office desks in a **row** along a wall (`Line`), each with a desk lamp half the time; filing cabinets in **runs**; a **sofa** against a wall on a rug, with a lamp; an **interrogation table** with two or three chairs pushed back, and a **street lamp** standing over it (*Near* `VoidTable`, facing it); a safe in a corner |
| **TECH** | Server racks in mirrored **banks** along the walls (`Line`, keyed `TechRack`); an operator's **console** facing each bank with its chair behind (*Near* `TechRack`); a **sentry post** at each doorway behind its own barrier; production lines nose to tail |
| **BIO** | **Undergrowth** gathered round each tree (*Near* `BioTree`); an overgrown **resting spot** against a wall (a bench between two bushes, with a lamp post 60% of the time); ground cover and evenly spread concealment |

Keys used by dressings are prefixed with their reality (`VoidTable`, `TechRack`, `BioTree`), so
they never meet a place's own keys (`Pool`, `Desk`, `Plant`).

**Next.**
- Give more places a composition pattern: Hangar's Maintenance Bay could take lanes, the
  Casino's Slot Hall a hub, and a reality could prefer a pattern (TECH lanes, VOID pavilions).
- A pavilion has no roof or lintel. Real art would want a hut kit (walls, a roof, a sign
  naming the place over the door).
- Cover dressings (barriers and crates) still go down one by one, chosen to cut firing lanes.
  They could become set pieces too: a crate pile with a barrier in front.
- Not yet compiled-and-run in Unity: the rules compile and their tests compile, and a preview
  run outside the editor drew the expected plans.

## Roadmap

| Phase | What | Status |
|---|---|---|
| **1. Priorities steer generation** | Concealment scaled by priority; fight rooms keep the best-fitting of several structure and furnishing tries; the Room Lab shows the kept try | **Done in code, not yet compiled or run** |
| **2. Patterns for every function** | Each function gets its pattern's structure (table in section 2); a *Rows* rule for Aisles rooms; reality *Additions*; pillar prefabs stretched to size; the placeholder kit and its column pieces | **Done in code, not yet compiled or run** |
| **3. Anchors and furniture** | A `Required` anchor and a furniture list for each card, built from the placeholder kit (section 7); rules in *Furnishing rules* | **Done in code, not yet compiled or run** |
| **4. Ship side** | A Guest first class / Guest steerage / Crew dial (the section's side where the function allows it), scaling size, fill and prop counts, with each side's dressing (a service door in crew rooms); the Room Lab picks a section | **Done in code, not yet compiled or run** |
| **5. Concealment by routes, service doors** | *Near Route* and *Off Route* placement options measured from the walkways and route-marking props (the service door); concealment patches stand beside routes, VOID shadow pockets in dead ends | **Done in code, not yet compiled or run** |
| **6. Breach split by zone** | Each zone of a Breach or Rift room dressed by one faction (the breach takes the zones its doorways open into); the breaching faction's divider splits a room left in one piece; the Room Lab shows the sides | **Done in code, not yet compiled or run** |
| **7. Tuning** | Set the trait bands (`RoomTraitMeasure`) and *Priority Tries* from Room Lab runs of each card in each reality. The **Room Sweep** that gathers the runs is built (see *Tuning* below); the values wait on its first report | **Sweep done in code, not yet compiled or run; values not yet tuned** |
| **8. Agents in the room** | Enemies start on posts their faction favours (section 9), hold or patrol by post kind, each Breach faction on its own side; patrol loops over walkable floor; Room Lab *Posts* layer | **Done in code, not yet compiled or run** |
| **9. Territory depth** | The holder's design blends toward neutral with the room's map influence: core rooms purest, frontier rooms at 0.4 (section 10); Room Lab *Influence* | **Done in code, not yet compiled or run** |
| **10. Enemies learn their ground** | Each enemy learns hidden, vantage and tight places by walking and looking, with familiarity and danger, shares them with nearby kin, falls back to the best it knows and watches a known lane from its post (section 11) | **Done in code, not yet compiled or run** |
| **11. Layouts and set pieces** | Places in several arrangements (`RoomLayout`: shapes, cells, size, structure, set pieces) over the function's own props; prop groups, keyed *Near* placements, features that grow with the room and stretch, pieces for big rooms only; every generated place with two or three layouts (Casino, Library and Pool first); the function's Required anchor before the layout's pieces; placeholders cut to at most three primitives; gallery room per layout; Room Lab and Map Graph layout pins (section 12) | **Done in code, not yet compiled or run** |
| **12. Ship operations and authoring kit** | Power Distribution, Waste Reclamation, Fuel Depot, Coolant Plant, Maintenance Hub and Comms Array, each with three layouts; Engine Room, Life Support, Water Treatment and Processing Plant reworked to three or four; shared kits (`RoomPropSet`) round a machine keyed *Plant*; Waist and Comb shapes; 18 operations props; the placeholder palette and Prop Builder; New layout, Duplicate and Check in the Inspector (section 12) | **Done in code, not yet compiled or run** |
| **13. Buildings and towns** | `BuildingPlan` floor plans (hall, rooms, back room) as School, Hospital and Library Floor layouts and on a town's lots, each room its own zone and furnished by its own place; roofs, lights and a sky dome; the Town Square place; Room Lab *Spaces* (section 13) | **Done in code, not yet compiled or run** |
| **14. Composition and props in sets** | Three Lanes, Hub Ring and Pavilion structure rules on the big open layouts; pavilions as annex spaces furnished by their own place, the room's props kept round them; VOID, TECH and BIO dressings rewritten as sets (section 14) | **Done in code and compiled; not yet run in Unity** |

### Tuning (Phase 7)

The bands that turn a room's scores into Low / Medium / High, and how many tries a fight room
gets, are set from what generation really builds, not guessed. The **Room Sweep** (Room Lab ›
*Sweep…*) builds the lab's recipe for every place but the landmarks, held by each faction, over
a run of seeds, and reports the results.

1. In the Room Lab pick *Type* Combat, tier 1, three doorways, no section, *Content*
   `StandardMapContentSettings`. Open *Sweep…*, set 10 seeds, *Run*, then *Copy report*.
2. Read the **By faction** table against each faction's priorities:
   - A trait a faction wants High should read High in most of its rooms, and one it wants
     Low should mostly read Low. Medium should sit between the two.
   - When every faction reads the same level, the band is in the wrong place. Move it to
     between those factions' medians (the raw-score column). For example, if BIO's median
     concealed share is 8% and VOID's 3%, the High band belongs at about 5%.
   - When a faction can't reach its level even on its best tries, the fix is in its assets
     (dressing counts, structure), not the bands.
3. **Priority Tries:** read the *Kept try* column. If the last try is often the one kept,
   more tries would still help. If try 0 is kept almost always, fewer tries cost nothing.
   Weigh it against level build time, since each try is another structure plan and furnishing.
4. The **By place** table and *Worst fits* name the cards that don't fit their faction. Open
   them in the Room Lab with that place, faction and seed.
5. Repeat for the other fight types (Elite, Ambush, Breach) once Combat reads right.

The bands live in `RoomTraitMeasure` (code constants, because the Room Lab's checks and the
generation steering must agree). *Priority Tries* lives in `LevelBuildSettings`.

### Seeing it in play

`Scenes/RoomGallery` builds every place instead of a map. The places come in quiet
rooms, with the realities and ship sections taken in turn; a place that lists several sides
is built once on each, side by side (section 3). Then each reality has a wing: Combat rooms
at influence 0.35 (frontier) and 0.525 (held, strength 0.7), then a core Combat, Stealth and
Breach room (section 10). Each room's placard in the HUD feed names its side, depth and fight
type, and the dev console's `/room` jumps between rooms. The scene's Prop Yard (outside the level, seen in the Scene
view) holds one of every placeholder column and prop.
