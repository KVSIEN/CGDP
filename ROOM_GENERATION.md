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
   route (*Off Route*). See *Routes* below.
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
| Concealment | Green (decor, no collider); VOID dark violet | Existing `ConcealmentPatch`, `GrassPatch`, `MossPatch`; `ShadowPocket` (Phase 5) |

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
