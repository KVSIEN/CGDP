using System.Collections.Generic;
using CGD.Algorithms;
using UnityEngine;

namespace CGD.Level
{
    // The large-scale layout inside one room: tile tags, pillars, inner walls and the zones
    // they divide the room into. Filled by RoomStructurePlanner and the room function's
    // RoomStructureRules; read by the geometry and the room populator.
    //
    // Everything that takes floor (pillars, props) goes through TryClaim, which refuses a
    // claim that would seal off part of the room or block an entrance: whatever the dressing
    // does, every free tile stays reachable from every doorway.
    //
    // Parts of the floor can stand a level up or down (TryAddElevation), joined to the floor
    // round them by stairs. Walking (CanStep) crosses a change of level only by a flight of
    // stairs, so "reachable" means reachable both ways; dropping off an open ledge (CanDropDown)
    // is a way down only, and never counts toward it.
    public class RoomStructure
    {
        private const RoomTileTags KeepClear = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure | RoomTileTags.Stairs | RoomTileTags.Overpass;
        // What can't be walked through.
        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;
        // What nothing new may be put on.
        private const RoomTileTags Taken = Solid | RoomTileTags.Access | RoomTileTags.Stairs | RoomTileTags.Landing | RoomTileTags.Overpass;

        private readonly List<StructurePillar> _pillars = new();
        private readonly List<PartitionEdge> _partitions = new();
        private readonly List<PartitionEdge> _openings = new();
        private readonly List<Vector2Int> _entrances = new();
        private readonly List<StructureSpace> _spaces = new();
        private readonly List<string> _leftOut = new();
        // Doorways that still divide zones: the gap in a building's wall opens onto the next
        // room, but the two stay separate places.
        private readonly HashSet<(Vector2Int, Vector2Int)> _sealedOpenings = new();

        // Everything known per tile, as flat arrays over the footprint's bounds: the prop planner,
        // the sightlines and the reachability check (run for every prop tried) read these
        // thousands of times a room, so they need no hashing, and a copy is a few array copies.
        // Inner walls are stored on the tile below or left of them: _wallEast[i] is the edge
        // between tile i and the one east of it, _wallNorth[i] the one north of it.
        private readonly TileGrid _grid;
        private readonly bool[] _floor;
        private readonly RoomTileTags[] _tags;
        private readonly int[] _zones;
        private readonly bool[] _solid;
        private readonly bool[] _wallEast;
        private readonly bool[] _wallNorth;
        // The floor's levels, stairs, raised and sunken areas and decks.
        private readonly RoomLevels _levels;
        // How tall the prop on a tile stands above its floor (SetPropHeight), so low ones are seen
        // over (SightBlocking); infinite until said, as a prop of unknown height may be any height.
        private readonly float[] _propHeight;
        private int _solidCount;
        private int _blockedCount;
        // The reachability search's scratch space, kept between calls: it runs for every prop tried.
        private readonly bool[] _reached;
        private readonly Stack<Vector2Int> _open = new();

        public RoomStructure(RoomFootprint footprint, float wallHeight)
        {
            Footprint  = footprint;
            WallHeight = wallHeight;

            _grid      = new TileGrid(footprint.Bounds);
            _floor     = new bool[_grid.Count];
            _tags      = new RoomTileTags[_floor.Length];
            _zones     = new int[_floor.Length];
            _solid     = new bool[_floor.Length];
            _wallEast  = new bool[_floor.Length];
            _wallNorth = new bool[_floor.Length];
            _levels    = new RoomLevels(_grid, wallHeight);
            _propHeight = new float[_floor.Length];
            System.Array.Fill(_propHeight, float.PositiveInfinity);
            _reached   = new bool[_floor.Length];
            foreach (Vector2Int tile in footprint.Tiles) _floor[IndexOf(tile)] = true;
        }

        public RoomFootprint Footprint  { get; }
        public float         WallHeight { get; }

        public IReadOnlyList<StructurePillar> Pillars    => _pillars;
        public IReadOnlyList<PartitionEdge>   Partitions => _partitions;
        // Doorways cut through inner walls, framed by a lintel (Height = the clear height).
        public IReadOnlyList<PartitionEdge>   Openings   => _openings;
        // The floor tile inside each doorway.
        public IReadOnlyList<Vector2Int>      Entrances  => _entrances;
        // The walled-off places inside the room (rooms of a building, the streets round it), each
        // furnished by its own function. Empty for an ordinary room.
        public IReadOnlyList<StructureSpace> Spaces => _spaces;
        // Rooms a building's plan asked for that its floor had no space for (the Room Lab says so).
        public IReadOnlyList<string> LeftOut => _leftOut;
        // The floor tile inside the doorway a player comes in by (RoomFlowPlanner): null when no
        // doorway leads back toward Start (the Start room itself), so nothing reads the room's flow.
        public Vector2Int? Arrival { get; private set; }
        public int ZoneCount { get; private set; } = 1;
        // The levels this room's floor may take (AllowLevels); both 0 until allowed, so a room stays
        // flat. Decks above the floor (overpasses) aren't held to it, only to the ceiling (AllowsDeck).
        public int LowestLevel  => _levels.LowestLevel;
        public int HighestLevel => _levels.HighestLevel;
        // The raised and sunken parts of the floor, with their stairs.
        public IReadOnlyList<ElevatedArea> Elevations => _levels.Elevations;
        public bool HasElevation => _levels.Elevations.Count > 0;
        // Decks crossing the room above its floor (TryAddOverpass).
        public IReadOnlyList<Overpass> Overpasses => _levels.Overpasses;
        // The zone with the most floor (0 when the room is one zone).
        public int LargestZone { get; private set; }

        // Whether the tile is part of this room's floor (the footprint's tiles, without hashing).
        public bool Contains(Vector2Int tile) => InBounds(tile) && _floor[IndexOf(tile)];

        // An independent copy, to try a furnishing on without touching this structure.
        public RoomStructure Copy()
        {
            var copy = new RoomStructure(Footprint, WallHeight);
            copy._pillars.AddRange(_pillars);
            copy._partitions.AddRange(_partitions);
            copy._openings.AddRange(_openings);
            copy._entrances.AddRange(_entrances);
            copy._spaces.AddRange(_spaces);
            copy._leftOut.AddRange(_leftOut);
            _levels.CopyTo(copy._levels);
            copy._sealedOpenings.UnionWith(_sealedOpenings);
            System.Array.Copy(_zones, copy._zones, _zones.Length);
            System.Array.Copy(_wallEast, copy._wallEast, _wallEast.Length);
            System.Array.Copy(_wallNorth, copy._wallNorth, _wallNorth.Length);
            copy._blockedCount = _blockedCount;
            copy.Arrival       = Arrival;
            copy.ZoneCount     = ZoneCount;
            copy.LargestZone   = LargestZone;
            copy.TakeFloorFrom(this);
            return copy;
        }

        // Takes on another structure's tags and taken floor: a copy of this room that was
        // furnished (or tried) on its own, kept as the real thing. Walls, pillars and zones are
        // left as they are, so `other` must have been copied from this structure.
        public void TakeFloorFrom(RoomStructure other)
        {
            if (other._tags.Length != _tags.Length) throw new System.ArgumentException("Not a copy of this room.", nameof(other));
            System.Array.Copy(other._tags, _tags, _tags.Length);
            System.Array.Copy(other._solid, _solid, _solid.Length);
            System.Array.Copy(other._propHeight, _propHeight, _propHeight.Length);
            _solidCount = other._solidCount;
        }

        public RoomTileTags TagsAt(Vector2Int tile) => InBounds(tile) ? _tags[IndexOf(tile)] : RoomTileTags.None;
        public bool Has(Vector2Int tile, RoomTileTags tags) => (TagsAt(tile) & tags) != 0;

        // Tiles outside the footprint's bounds carry no tags: tagging one does nothing.
        public void Tag(Vector2Int tile, RoomTileTags tags)
        {
            if (!InBounds(tile)) return;
            int index = IndexOf(tile);
            RoomTileTags before = _tags[index];
            _tags[index] = before | tags;

            bool becameSolid = (before & Solid) == 0 && (tags & Solid) != 0;
            if (!becameSolid || !_floor[index]) return;
            _solid[index] = true;
            _solidCount++;
        }

        public void AddEntrance(Vector2Int tile)
        {
            if (!_entrances.Contains(tile)) _entrances.Add(tile);
        }

        // One of the entrances, set once the structure is built (RoomFlowPlanner.Mark).
        public void SetArrival(Vector2Int entrance)
        {
            if (!_entrances.Contains(entrance)) throw new System.ArgumentException("The arrival must be one of the room's entrances.", nameof(entrance));
            Arrival = entrance;
        }

        // Which part of the room a tile is in, once inner walls have split it (0 when none).
        public int ZoneOf(Vector2Int tile) => InBounds(tile) ? _zones[IndexOf(tile)] : 0;

        // Floor nothing has claimed: not a route, doorway, centre spot or pillar.
        public bool IsOpen(Vector2Int tile) => Contains(tile) && !Has(tile, KeepClear);

        // Whether an inner wall stands between two neighbouring tiles (false for tiles that
        // aren't neighbours).
        public bool IsPartitioned(Vector2Int a, Vector2Int b)
        {
            if (_blockedCount == 0 || !TryEdge(a, b, out bool[] walls, out int index)) return false;
            return walls[index];
        }

        // Floor nothing has built on or reserved: room for a new prop.
        public bool IsFree(Vector2Int tile) => Contains(tile) && !Has(tile, Taken);

        // Whether a wall (the room's or an inner one) stands on this side of the tile.
        public bool IsWall(Vector2Int tile, Vector2Int side) =>
            !Contains(tile + side) || IsPartitioned(tile, tile + side);

        // Takes the tiles for `tag` (Structure or Prop) if they are all free and the room stays
        // whole: every other free tile still reachable from every doorway, no doorway blocked.
        public bool TryClaim(IReadOnlyList<Vector2Int> tiles, RoomTileTags tag)
        {
            foreach (Vector2Int tile in tiles)
                if (!IsFree(tile)) return false;
            if (!StaysConnected(tiles)) return false;

            foreach (Vector2Int tile in tiles) Tag(tile, tag);
            return true;
        }

        // Gives tiles back that a prop stood on, so it can be moved. Frees them for good: unlike
        // TryClaim there is nothing to check, as taking floor away is the only thing that can cut a room.
        public void Release(IReadOnlyList<Vector2Int> tiles, RoomTileTags tag)
        {
            foreach (Vector2Int tile in tiles)
            {
                if (!InBounds(tile)) continue;
                int index = IndexOf(tile);
                _tags[index] &= ~tag;
                if ((tag & RoomTileTags.Prop) != 0) _propHeight[index] = float.PositiveInfinity;
                if (!_solid[index] || (_tags[index] & Solid) != 0) continue;
                _solid[index] = false;
                _solidCount--;
            }
        }

        // How tall the prop on a tile stands above its floor: 0 with no prop, infinite when nobody
        // said (SetPropHeight).
        public float PropHeightAt(Vector2Int tile) =>
            Has(tile, RoomTileTags.Prop) ? _propHeight[IndexOf(tile)] : 0f;

        // Says how tall the prop on a tile stands (PropHeights), once it has claimed the tile.
        public void SetPropHeight(Vector2Int tile, float height)
        {
            if (InBounds(tile)) _propHeight[IndexOf(tile)] = Mathf.Max(0f, height);
        }

        // Whether every free tile can still be walked to from the doorways if `claimed` were
        // taken too (a room with no doorway starts from its first free tile).
        public bool StaysConnected(IReadOnlyList<Vector2Int> claimed)
        {
            foreach (Vector2Int entrance in _entrances)
                if (Includes(claimed, entrance) || Has(entrance, Solid)) return false;

            // `reached` also marks the claimed tiles, so the search never walks onto them.
            bool[] reached = _reached;
            System.Array.Clear(reached, 0, reached.Length);
            int free = Footprint.Count - _solidCount;
            foreach (Vector2Int tile in claimed)
            {
                if (!Contains(tile) || _solid[IndexOf(tile)] || reached[IndexOf(tile)]) continue;
                reached[IndexOf(tile)] = true;
                free--;
            }
            if (free <= 0) return true;

            Stack<Vector2Int> open = _open;
            open.Clear();
            int visited = 0;
            void Visit(Vector2Int tile)
            {
                int index = IndexOf(tile);
                if (reached[index]) return;
                reached[index] = true;
                visited++;
                open.Push(tile);
            }

            if (_entrances.Count == 0) Visit(FirstFree(reached));
            foreach (Vector2Int entrance in _entrances) Visit(entrance);

            bool partitioned = _blockedCount > 0;
            bool elevated = HasElevation;
            while (open.Count > 0)
            {
                Vector2Int tile = open.Pop();
                foreach (Vector2Int side in GridNeighbours.Four)
                {
                    Vector2Int next = tile + side;
                    if (!Contains(next)) continue;

                    int index = IndexOf(next);
                    if (reached[index] || _solid[index] || (partitioned && IsPartitioned(tile, next))) continue;
                    if (elevated && !_levels.Join(tile, next)) continue;
                    Visit(next);
                }
            }
            return visited == free;
        }

        private Vector2Int FirstFree(bool[] taken)
        {
            foreach (Vector2Int tile in Footprint.Tiles)
                if (!_solid[IndexOf(tile)] && !taken[IndexOf(tile)]) return tile;
            return Footprint.Tiles[0];
        }

        private int IndexOf(Vector2Int tile) => _grid.IndexOf(tile);

        private bool InBounds(Vector2Int tile) => _grid.Contains(tile);

        // The wall array and index holding the edge between two neighbouring tiles.
        private bool TryEdge(Vector2Int a, Vector2Int b, out bool[] walls, out int index)
        {
            Vector2Int low = a.x < b.x || a.y < b.y ? a : b;
            Vector2Int step = (a.x < b.x || a.y < b.y ? b : a) - low;
            walls = step == Vector2Int.right ? _wallEast : step == Vector2Int.up ? _wallNorth : null;
            index = walls != null && InBounds(low) ? IndexOf(low) : -1;
            return index >= 0;
        }

        private void SetPartitioned(Vector2Int a, Vector2Int b, bool blocked)
        {
            if (!TryEdge(a, b, out bool[] walls, out int index) || walls[index] == blocked) return;
            walls[index] = blocked;
            _blockedCount += blocked ? 1 : -1;
        }

        // A free-standing pillar takes the tile it stands on, which must be open and not cut
        // the room in two. One set into the walls (freeStanding = false) takes no floor. A height
        // under the room's wall height makes a broken stump.
        public bool TryAddPillar(Vector2 position, float width, GameObject prefab, bool freeStanding = true, float height = 0f)
        {
            var tile = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            if (freeStanding)
            {
                if (!IsOpen(tile) || !TryClaim(new[] { tile }, RoomTileTags.Structure)) return false;
            }
            _pillars.Add(new StructurePillar(position, width, prefab, height));
            return true;
        }

        private static bool Includes(IReadOnlyList<Vector2Int> tiles, Vector2Int tile)
        {
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i] == tile) return true;
            return false;
        }

        // Adds the edges only if the room stays whole: every free tile (pillars and props counted
        // as in the way) still reachable from every doorway.
        public bool TryAddPartition(IReadOnlyList<PartitionEdge> edges)
        {
            // Only edges that weren't walled already are taken back if the room would split.
            var added = new List<PartitionEdge>(edges.Count);
            foreach (PartitionEdge edge in edges)
            {
                if (IsPartitioned(edge.Tile, edge.Other)) continue;
                SetPartitioned(edge.Tile, edge.Other, true);
                added.Add(edge);
            }
            if (StaysConnected(System.Array.Empty<Vector2Int>()))
            {
                _partitions.AddRange(edges);
                return true;
            }
            foreach (PartitionEdge edge in added) SetPartitioned(edge.Tile, edge.Other, false);
            return false;
        }

        // `separatesZones`: the doorway joins two places that stay separate zones (a classroom and
        // its hall), unlike a gap in a divider, which keeps the room's two halves one space.
        public void AddOpening(PartitionEdge opening, bool separatesZones = false)
        {
            _openings.Add(opening);
            if (separatesZones) _sealedOpenings.Add(Key(opening.Tile, opening.Other));
        }

        // Marks the zone holding `tile` as a place of its own, to be furnished by `function`.
        // Call before AssignZones or after: the zone is looked up from the tile when needed.
        // `area`: the tiles it covers, when it is a rectangle (shown as its own label in the overview).
        // `roofed`: the space is a building standing in an open-air room, so it gets a roof.
        // `annex`: a hut or kiosk standing in a room that stays its own place round it.
        public void AddSpace(Vector2Int tile, RoomFunction function, string name, RoomLayout layout = null, bool roofed = false,
                             bool annex = false, RectInt? area = null) =>
            _spaces.Add(new StructureSpace(tile, function, name, layout, roofed, annex, area));

        // The place with a known area that covers this tile (the smallest first), or null.
        public StructureSpace? SpaceAt(Vector2Int tile)
        {
            StructureSpace? found = null;
            foreach (StructureSpace space in _spaces)
                if (space.Covers(tile) && (!found.HasValue || space.Area.width * space.Area.height < found.Value.Area.width * found.Value.Area.height))
                    found = space;
            return found;
        }

        // The zones of the room's own floor when annexes stand in it: every zone but theirs.
        // Null when there is no annex (the whole room). Needs the zones assigned.
        public ICollection<int> ZonesOutsideAnnexes()
        {
            HashSet<int> annexZones = null;
            foreach (StructureSpace space in _spaces)
                if (space.Annex) (annexZones ??= new HashSet<int>()).Add(ZoneOf(space.Tile));
            if (annexZones == null) return null;

            var open = new HashSet<int>();
            for (int zone = 0; zone < ZoneCount; zone++)
                if (!annexZones.Contains(zone)) open.Add(zone);
            return open;
        }

        public void NoteLeftOut(string room) => _leftOut.Add(room);

        // Whether any space has a roof of its own.
        public bool HasRoofedSpaces
        {
            get
            {
                foreach (StructureSpace space in _spaces)
                    if (space.Roofed) return true;
                return false;
            }
        }

        // Whether the tile is in a space with a roof of its own. Needs the zones assigned.
        public bool IsRoofed(Vector2Int tile)
        {
            if (_spaces.Count == 0 || !Contains(tile)) return false;

            int zone = ZoneOf(tile);
            foreach (StructureSpace space in _spaces)
                if (space.Roofed && ZoneOf(space.Tile) == zone) return true;
            return false;
        }

        // Whether an inner wall can't stand on the edge between two tiles: a route crosses it, or
        // a doorway or pillar is right beside it.
        public bool EdgeMustStayOpen(Vector2Int a, Vector2Int b)
        {
            const RoomTileTags Beside = RoomTileTags.NearDoor | RoomTileTags.Structure | RoomTileTags.Stairs | RoomTileTags.Landing;
            return (Has(a, RoomTileTags.Walkway) && Has(b, RoomTileTags.Walkway)) || Has(a, Beside) || Has(b, Beside);
        }

        // --- Levels ---------------------------------------------------------------------

        // Lets the floor take levels from `lowest` to `highest` (clamped to RoomElevation's), so
        // elevation rules may raise or sink parts of it. Level 0 is always allowed.
        public void AllowLevels(int lowest, int highest) => _levels.Allow(lowest, highest);

        // Whether the room's floor can stand at `level`: one the room allows, with Headroom under
        // the ceiling. Every rule that raises or sinks floor goes through it.
        public bool AllowsFloor(int level) => _levels.AllowsFloor(level);

        // Whether a deck can stand at `level`: anywhere the ceiling leaves Headroom over it. Decks
        // are a layer of their own over the floor (footbridges, and later balconies and upper
        // decks), so a room whose floor keeps within a level of its doorways can still have them
        // higher up, as high as the room is tall.
        public bool AllowsDeck(int level) => _levels.AllowsDeck(level);

        // The most tiers a feature stepping `direction` (+1 up, -1 down) from the doorways' level
        // can have here, up to `wanted`; 0 when not even one level that way stands.
        public int TiersAllowed(int direction, int wanted) => _levels.TiersAllowed(direction, wanted);

        // The tile's floor level (0 off the floor).
        public int LevelOf(Vector2Int tile) => _levels.LevelOf(tile);

        // The flight of stairs on a tile, if there is one.
        public bool TryGetStairs(Vector2Int tile, out StairFlight stairs) => _levels.TryGetStairs(tile, out stairs);

        // The elevated area a tile is part of; null on the doorways' level.
        public ElevatedArea AreaOf(Vector2Int tile) => _levels.AreaOf(tile);

        // Whether someone can walk between two neighbouring floor tiles, both ways: no wall
        // between them, and the same level, or a flight of stairs leading from one to the other.
        // What stands on the tiles is not looked at.
        public bool CanStep(Vector2Int a, Vector2Int b) =>
            AreNeighbours(a, b) && Contains(a) && Contains(b) && !IsPartitioned(a, b) && _levels.Join(a, b);

        // Whether someone can drop from `from` to the floor a level below it: an open ledge with
        // no wall or railing on it. A way down only: climbing back up needs the stairs.
        public bool CanDropDown(Vector2Int from, Vector2Int to)
        {
            if (!AreNeighbours(from, to) || !Contains(from) || !Contains(to) || IsPartitioned(from, to)) return false;
            return _levels.IsOpenDrop(from, to);
        }

        // Whether the floor changes level between two neighbouring tiles where no stairs lead:
        // the edge of a platform or the rim of a sunken floor.
        public bool IsLedge(Vector2Int a, Vector2Int b) =>
            AreNeighbours(a, b) && Contains(a) && Contains(b) && LevelOf(a) != LevelOf(b) && !_levels.Join(a, b);

        // Raises or sinks `area`'s tiles to its level with its stairs, if the room stays sound
        // (see TryAddElevations).
        public bool TryAddElevation(ElevatedArea area) => area != null && TryAddElevations(new[] { area });

        // Raises or sinks every area together, or none of them, if the room stays sound:
        //   - each level is one the room allows (AllowsFloor), and the tiles are flat, free floor
        //     (a Section also takes what pillars and cut-off floor stand on, and lifts it with
        //     the rest), none of it in front of a doorway (so everyone arrives on the doorways' level), no tile
        //     in two areas; areas added together may be tiers of one feature, each standing on the
        //     one below it (the lower tier's tiles carrying the upper tier's stairs);
        //   - no tile ends up more than one level from its neighbour;
        //   - each flight climbs one level from its foot to its head, with nothing on either
        //     landing and no wall across it, stands clear of a doorway's front and the centre
        //     spot, and leads onto or off its own area;
        //   - every free tile is still reachable from every doorway, both ways.
        // Routes and the centre spot may be raised or sunk: the stairs carry a route on, and
        // the rules choose whether to (a sunken court round the centrepiece, a lane the route
        // runs along). Adding them together lets terraces lean on each other's stairs.
        public bool TryAddElevations(IReadOnlyList<ElevatedArea> areas)
        {
            if (areas == null || areas.Count == 0) return false;

            var claimed = new HashSet<Vector2Int>();
            foreach (ElevatedArea area in areas)
            {
                if (area == null || area.Tiles.Count == 0 || area.Level == 0) return false;
                if (!AllowsFloor(area.Level)) return false;
                foreach (Vector2Int tile in area.Tiles)
                    if (!CanChangeLevel(area.Kind, tile) || Has(tile, RoomTileTags.NearDoor) || LevelOf(tile) != 0 || !claimed.Add(tile)) return false;
            }

            // Each flight and corner piece takes a tile of its own; landings may be shared (two
            // slopes side by side onto one mound), but nothing stands on another's landing.
            const RoomTileTags NotOnStairs = RoomTileTags.NearDoor | RoomTileTags.Centre;
            var flights = new HashSet<Vector2Int>();
            var landings = new HashSet<Vector2Int>();
            foreach (ElevatedArea area in areas)
            {
                foreach (StairFlight stairs in area.Stairs)
                {
                    if (Mathf.Abs(stairs.Up.x) + Mathf.Abs(stairs.Up.y) != 1 || Has(stairs.Tile, NotOnStairs)) return false;
                    if (!IsFree(stairs.Tile) || !IsFree(stairs.Foot) || !IsFree(stairs.Head) || !flights.Add(stairs.Tile)) return false;
                    if (IsPartitioned(stairs.Foot, stairs.Tile) || IsPartitioned(stairs.Tile, stairs.Head)) return false;
                    landings.Add(stairs.Foot);
                    landings.Add(stairs.Head);
                }
                foreach (SlopeCorner corner in area.Corners)
                    if (Mathf.Abs(corner.Up.x) != 1 || Mathf.Abs(corner.Up.y) != 1 || !IsFree(corner.Tile)
                        || Has(corner.Tile, RoomTileTags.NearDoor) || !flights.Add(corner.Tile)) return false;
            }
            foreach (Vector2Int tile in flights)
                if (landings.Contains(tile)) return false;

            _levels.Add(areas);
            foreach (ElevatedArea area in areas) TagStairs(area, true);
            if (StepsAreSound(areas) && StaysConnected(System.Array.Empty<Vector2Int>())) return true;

            for (int i = areas.Count - 1; i >= 0; i--) TagStairs(areas[i], false);
            _levels.Remove(areas);
            return false;
        }

        // Lays a deck across the room above its floor (Overpass), if it fits:
        //   - its level has headroom under the ceiling (AllowsDeck), whatever the floor's range;
        //   - the deck runs straight over floor at least OverpassClearance levels below it, free,
        //     not in front of a doorway or over the centre spot, under no other deck and with no
        //     wall along it;
        //   - each flight stands on free, flat floor one level below the deck, clear of a doorway's
        //     front and the centre, with its landing behind it on the same level and no wall across;
        //   - the room stays whole with the flights' tiles taken, so the deck is never the only way.
        // The flights' tiles become solid (no one walks the floor under a flight), their landings
        // Landing, and the floor under the deck Overpass: walked under, but nothing is put there.
        public bool TryAddOverpass(Overpass overpass)
        {
            if (overpass == null || overpass.Deck.Count == 0 || !AllowsDeck(overpass.Level)) return false;

            const RoomTileTags NotUnder = RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Overpass;
            Vector2Int previous = overpass.Stairs[0].Tile;
            foreach (Vector2Int tile in overpass.Deck)
            {
                if (!IsFree(tile) || Has(tile, NotUnder) || IsPartitioned(previous, tile)) return false;
                if (overpass.Level - LevelOf(tile) < RoomElevation.OverpassClearance) return false;
                previous = tile;
            }
            if (IsPartitioned(previous, overpass.Stairs[1].Tile)) return false;

            const RoomTileTags NotOnStairs = RoomTileTags.NearDoor | RoomTileTags.Centre;
            var flights = new Vector2Int[overpass.Stairs.Count];
            for (int i = 0; i < overpass.Stairs.Count; i++)
            {
                StairFlight stairs = overpass.Stairs[i];
                int below = overpass.Level - 1;
                if (!IsFree(stairs.Tile) || Has(stairs.Tile, NotOnStairs) || _levels.HasStairs(stairs.Tile)) return false;
                if (LevelOf(stairs.Tile) != below || !Contains(stairs.Foot) || LevelOf(stairs.Foot) != below) return false;
                if (Has(stairs.Foot, RoomTileTags.Structure | RoomTileTags.Prop | RoomTileTags.Stairs) || IsPartitioned(stairs.Foot, stairs.Tile)) return false;
                flights[i] = stairs.Tile;
            }
            if (!StaysConnected(flights)) return false;

            foreach (Vector2Int tile in overpass.Deck) Tag(tile, RoomTileTags.Overpass);
            foreach (StairFlight stairs in overpass.Stairs)
            {
                Tag(stairs.Tile, RoomTileTags.Structure);
                Tag(stairs.Foot, RoomTileTags.Landing);
            }
            _levels.AddOverpass(overpass);
            return true;
        }

        // The overpass whose deck crosses above a tile, if one does.
        public Overpass OverpassAbove(Vector2Int tile)
        {
            return Has(tile, RoomTileTags.Overpass) ? _levels.OverpassAbove(tile) : null;
        }

        // Once raised: no tile two levels from a neighbour, and every flight climbing one level onto or off its area.
        // Free floor; for a section also floor only structure stands on, which rises with it.
        private bool CanChangeLevel(ElevationKind kind, Vector2Int tile) =>
            IsFree(tile) || (kind == ElevationKind.Section && Contains(tile) && (TagsAt(tile) & Taken) == RoomTileTags.Structure);

        private bool StepsAreSound(IReadOnlyList<ElevatedArea> areas)
        {
            foreach (ElevatedArea area in areas)
            {
                foreach (Vector2Int tile in area.Tiles)
                    foreach (Vector2Int side in GridNeighbours.Four)
                        if (Contains(tile + side) && Mathf.Abs(area.Level - LevelOf(tile + side)) > 1) return false;

                foreach (StairFlight stairs in area.Stairs)
                {
                    int low = LevelOf(stairs.Tile);
                    if (LevelOf(stairs.Foot) != low || LevelOf(stairs.Head) != low + 1) return false;
                    if (AreaOf(stairs.Tile) != area && AreaOf(stairs.Head) != area) return false;
                }

                foreach (SlopeCorner corner in area.Corners)
                    if (LevelOf(corner.Tile + corner.Up) != LevelOf(corner.Tile) + 1) return false;
            }
            return true;
        }

        // Tags (or takes back) an area's stairs, their landings and its corner pieces; its levels
        // are set in RoomLevels.
        private void TagStairs(ElevatedArea area, bool adding)
        {
            foreach (StairFlight stairs in area.Stairs)
            {
                foreach (Vector2Int tile in new[] { stairs.Foot, stairs.Head })
                    SetTag(tile, RoomTileTags.Landing, adding);
                SetTag(stairs.Tile, RoomTileTags.Stairs, adding);
            }

            // A corner piece is solid ground, like a pillar.
            foreach (SlopeCorner corner in area.Corners)
            {
                if (adding) Tag(corner.Tile, RoomTileTags.Structure);
                else Release(new[] { corner.Tile }, RoomTileTags.Structure);
            }
        }

        // Stairs and landings are never solid, so they can be untagged without the solid count.
        private void SetTag(Vector2Int tile, RoomTileTags tag, bool on)
        {
            if (on) Tag(tile, tag);
            else _tags[IndexOf(tile)] &= ~tag;
        }

        private static bool AreNeighbours(Vector2Int a, Vector2Int b) => Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;

        // Zones are the pieces the partition lines cut the floor into, gaps counted as
        // closed: a kitchen stays its own zone even with an opening into the dining room.
        public void AssignZones()
        {
            ZoneCount = CountZones(assign: true);
            LargestZone = FindLargestZone();
        }

        private int FindLargestZone()
        {
            var sizes = new int[ZoneCount];
            foreach (Vector2Int tile in Footprint.Tiles) sizes[ZoneOf(tile)]++;

            int largest = 0;
            for (int zone = 1; zone < sizes.Length; zone++)
                if (sizes[zone] > sizes[largest]) largest = zone;
            return largest;
        }

        // Connected pieces of floor. With assign, a partition's gap is treated as closed too
        // (the whole partition line separates zones); without it only the walls block.
        private int CountZones(bool assign)
        {
            var lines = assign ? PartitionLines() : null;
            var seen = new HashSet<Vector2Int>();
            int zones = 0;

            foreach (Vector2Int start in Footprint.Tiles)
            {
                if (!seen.Add(start)) continue;
                var open = new Stack<Vector2Int>();
                open.Push(start);
                while (open.Count > 0)
                {
                    Vector2Int tile = open.Pop();
                    if (assign) _zones[IndexOf(tile)] = zones;
                    foreach (Vector2Int side in GridNeighbours.Four)
                    {
                        Vector2Int next = tile + side;
                        if (!Contains(next) || seen.Contains(next)) continue;
                        if (IsPartitioned(tile, next)) continue;
                        if (_sealedOpenings.Contains(Key(tile, next))) continue;
                        if (lines != null && lines.Contains(Key(tile, next))) continue;
                        seen.Add(next);
                        open.Push(next);
                    }
                }
                zones++;
            }
            return zones;
        }

        // Every edge on a partition's line across the room, gaps included.
        private HashSet<(Vector2Int, Vector2Int)> PartitionLines()
        {
            var lines = new HashSet<(Vector2Int, Vector2Int)>();
            foreach (PartitionEdge edge in _partitions)
            {
                if (!edge.DividesZones) continue;

                var along = new Vector2Int(edge.Side.y, edge.Side.x);
                foreach (int step in new[] { 1, -1 })
                    for (Vector2Int a = edge.Tile; Contains(a) && Contains(a + edge.Side); a += along * step)
                        lines.Add(Key(a, a + edge.Side));
            }
            return lines;
        }

        private static (Vector2Int, Vector2Int) Key(Vector2Int a, Vector2Int b) =>
            a.x < b.x || a.y < b.y ? (a, b) : (b, a);
    }
}
