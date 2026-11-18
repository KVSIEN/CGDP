using System.Collections.Generic;
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
    public class RoomStructure
    {
        private const RoomTileTags KeepClear = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure;
        // What can't be walked through.
        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;
        // What nothing new may be put on.
        private const RoomTileTags Taken = Solid | RoomTileTags.Access;

        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly List<StructurePillar> _pillars = new();
        private readonly List<PartitionEdge> _partitions = new();
        private readonly List<PartitionEdge> _openings = new();
        private readonly List<Vector2Int> _entrances = new();
        private readonly List<StructureSpace> _spaces = new();
        // Doorways that still divide zones: the gap in a building's wall opens onto the next
        // room, but the two stay separate places.
        private readonly HashSet<(Vector2Int, Vector2Int)> _sealedOpenings = new();

        // Everything known per tile, as flat arrays over the footprint's bounds: the prop planner,
        // the sightlines and the reachability check (run for every prop tried) read these
        // thousands of times a room, so they need no hashing, and a copy is a few array copies.
        // Inner walls are stored on the tile below or left of them: _wallEast[i] is the edge
        // between tile i and the one east of it, _wallNorth[i] the one north of it.
        private readonly RectInt _bounds;
        private readonly bool[] _floor;
        private readonly RoomTileTags[] _tags;
        private readonly int[] _zones;
        private readonly bool[] _solid;
        private readonly bool[] _wallEast;
        private readonly bool[] _wallNorth;
        private int _solidCount;
        private int _blockedCount;
        // The reachability search's scratch space, kept between calls: it runs for every prop tried.
        private readonly bool[] _reached;
        private readonly Stack<Vector2Int> _open = new();

        public RoomStructure(RoomFootprint footprint, float wallHeight)
        {
            Footprint  = footprint;
            WallHeight = wallHeight;

            _bounds = footprint.Bounds;
            _floor     = new bool[_bounds.width * _bounds.height];
            _tags      = new RoomTileTags[_floor.Length];
            _zones     = new int[_floor.Length];
            _solid     = new bool[_floor.Length];
            _wallEast  = new bool[_floor.Length];
            _wallNorth = new bool[_floor.Length];
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
        public int ZoneCount { get; private set; } = 1;
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
            copy._sealedOpenings.UnionWith(_sealedOpenings);
            System.Array.Copy(_zones, copy._zones, _zones.Length);
            System.Array.Copy(_wallEast, copy._wallEast, _wallEast.Length);
            System.Array.Copy(_wallNorth, copy._wallNorth, _wallNorth.Length);
            copy._blockedCount = _blockedCount;
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
            while (open.Count > 0)
            {
                Vector2Int tile = open.Pop();
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int next = tile + side;
                    if (!Contains(next)) continue;

                    int index = IndexOf(next);
                    if (reached[index] || _solid[index] || (partitioned && IsPartitioned(tile, next))) continue;
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

        private int IndexOf(Vector2Int tile) => (tile.y - _bounds.yMin) * _bounds.width + (tile.x - _bounds.xMin);

        private bool InBounds(Vector2Int tile) =>
            tile.x >= _bounds.xMin && tile.x < _bounds.xMax && tile.y >= _bounds.yMin && tile.y < _bounds.yMax;

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
        public void AddSpace(Vector2Int tile, RoomFunction function, string name, RoomLayout layout = null) =>
            _spaces.Add(new StructureSpace(tile, function, name, layout));

        // Whether an inner wall can't stand on the edge between two tiles: a route crosses it, or
        // a doorway or pillar is right beside it.
        public bool EdgeMustStayOpen(Vector2Int a, Vector2Int b)
        {
            const RoomTileTags Beside = RoomTileTags.NearDoor | RoomTileTags.Structure;
            return (Has(a, RoomTileTags.Walkway) && Has(b, RoomTileTags.Walkway)) || Has(a, Beside) || Has(b, Beside);
        }

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
                    foreach (Vector2Int side in Sides)
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
