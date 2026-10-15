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

        private readonly Dictionary<Vector2Int, RoomTileTags> _tags = new();
        private readonly Dictionary<Vector2Int, int> _zones = new();
        private readonly List<StructurePillar> _pillars = new();
        private readonly List<PartitionEdge> _partitions = new();
        private readonly HashSet<(Vector2Int, Vector2Int)> _blocked = new();
        private readonly List<Vector2Int> _entrances = new();

        // Floor tiles and the solid ones among them as flat arrays over the footprint's bounds, so the
        // reachability check (run for every prop placed) needs no hashing.
        private readonly RectInt _bounds;
        private readonly bool[] _floor;
        private readonly bool[] _solid;
        private int _solidCount;

        public RoomStructure(RoomFootprint footprint, float wallHeight)
        {
            Footprint  = footprint;
            WallHeight = wallHeight;

            _bounds = footprint.Bounds;
            _floor  = new bool[_bounds.width * _bounds.height];
            _solid  = new bool[_floor.Length];
            foreach (Vector2Int tile in footprint.Tiles) _floor[IndexOf(tile)] = true;
        }

        public RoomFootprint Footprint  { get; }
        public float         WallHeight { get; }

        public IReadOnlyList<StructurePillar> Pillars    => _pillars;
        public IReadOnlyList<PartitionEdge>   Partitions => _partitions;
        // The floor tile inside each doorway.
        public IReadOnlyList<Vector2Int>      Entrances  => _entrances;
        public int ZoneCount { get; private set; } = 1;

        public RoomTileTags TagsAt(Vector2Int tile) => _tags.TryGetValue(tile, out RoomTileTags tags) ? tags : RoomTileTags.None;
        public bool Has(Vector2Int tile, RoomTileTags tags) => (TagsAt(tile) & tags) != 0;

        public void Tag(Vector2Int tile, RoomTileTags tags)
        {
            RoomTileTags before = TagsAt(tile);
            _tags[tile] = before | tags;

            bool becameSolid = (before & Solid) == 0 && (tags & Solid) != 0;
            if (!becameSolid || !InFloor(tile)) return;
            _solid[IndexOf(tile)] = true;
            _solidCount++;
        }

        public void AddEntrance(Vector2Int tile)
        {
            if (!_entrances.Contains(tile)) _entrances.Add(tile);
        }

        // Which part of the room a tile is in, once inner walls have split it (0 when none).
        public int ZoneOf(Vector2Int tile) => _zones.TryGetValue(tile, out int zone) ? zone : 0;

        // Floor nothing has claimed: not a route, doorway, centre spot or pillar.
        public bool IsOpen(Vector2Int tile) => Footprint.Contains(tile) && !Has(tile, KeepClear);

        public bool IsPartitioned(Vector2Int a, Vector2Int b) => _blocked.Contains(Key(a, b));

        // Floor nothing has built on or reserved: room for a new prop.
        public bool IsFree(Vector2Int tile) => Footprint.Contains(tile) && !Has(tile, Taken);

        // Whether a wall (the room's or an inner one) stands on this side of the tile.
        public bool IsWall(Vector2Int tile, Vector2Int side) =>
            !Footprint.Contains(tile + side) || IsPartitioned(tile, tile + side);

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
                if (Contains(claimed, entrance) || Has(entrance, Solid)) return false;

            // `reached` also marks the claimed tiles, so the search never walks onto them.
            var reached = new bool[_floor.Length];
            int free = Footprint.Count - _solidCount;
            foreach (Vector2Int tile in claimed)
            {
                if (!InFloor(tile) || _solid[IndexOf(tile)] || reached[IndexOf(tile)]) continue;
                reached[IndexOf(tile)] = true;
                free--;
            }
            if (free <= 0) return true;

            var open = new Stack<Vector2Int>();
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

            bool partitioned = _blocked.Count > 0;
            while (open.Count > 0)
            {
                Vector2Int tile = open.Pop();
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int next = tile + side;
                    if (!InFloor(next)) continue;

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

        private bool InFloor(Vector2Int tile) =>
            tile.x >= _bounds.xMin && tile.x < _bounds.xMax && tile.y >= _bounds.yMin && tile.y < _bounds.yMax && _floor[IndexOf(tile)];

        // A free-standing pillar takes the tile it stands on, which must be open and not cut
        // the room in two. One set into the walls (freeStanding = false) takes no floor.
        public bool TryAddPillar(Vector2 position, float width, GameObject prefab, bool freeStanding = true)
        {
            var tile = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            if (freeStanding)
            {
                if (!IsOpen(tile) || !TryClaim(new[] { tile }, RoomTileTags.Structure)) return false;
            }
            _pillars.Add(new StructurePillar(position, width, prefab));
            return true;
        }

        private static bool Contains(IReadOnlyList<Vector2Int> tiles, Vector2Int tile)
        {
            for (int i = 0; i < tiles.Count; i++)
                if (tiles[i] == tile) return true;
            return false;
        }

        // Adds the edges only if every floor tile can still reach every other.
        public bool TryAddPartition(IReadOnlyList<PartitionEdge> edges)
        {
            foreach (PartitionEdge edge in edges) _blocked.Add(Key(edge.Tile, edge.Other));
            if (CountZones(assign: false) == 1)
            {
                _partitions.AddRange(edges);
                return true;
            }
            foreach (PartitionEdge edge in edges) _blocked.Remove(Key(edge.Tile, edge.Other));
            return false;
        }

        // Zones are the pieces the partition lines cut the floor into, gaps counted as
        // closed: a kitchen stays its own zone even with an opening into the dining room.
        public void AssignZones() => ZoneCount = CountZones(assign: true);

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
                    if (assign) _zones[tile] = zones;
                    foreach (Vector2Int side in Sides)
                    {
                        Vector2Int next = tile + side;
                        if (!Footprint.Contains(next) || seen.Contains(next)) continue;
                        if (_blocked.Contains(Key(tile, next))) continue;
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
                var along = new Vector2Int(edge.Side.y, edge.Side.x);
                foreach (int step in new[] { 1, -1 })
                    for (Vector2Int a = edge.Tile; Footprint.Contains(a) && Footprint.Contains(a + edge.Side); a += along * step)
                        lines.Add(Key(a, a + edge.Side));
            }
            return lines;
        }

        private static (Vector2Int, Vector2Int) Key(Vector2Int a, Vector2Int b) =>
            a.x < b.x || a.y < b.y ? (a, b) : (b, a);
    }
}
