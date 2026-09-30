using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The large-scale layout inside one room: tile tags, pillars, inner walls and the zones
    // they divide the room into. Filled by RoomStructurePlanner and the room function's
    // RoomStructureRules; read by the geometry and the room populator.
    public class RoomStructure
    {
        private const RoomTileTags KeepClear = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Centre | RoomTileTags.Structure;

        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly Dictionary<Vector2Int, RoomTileTags> _tags = new();
        private readonly Dictionary<Vector2Int, int> _zones = new();
        private readonly List<StructurePillar> _pillars = new();
        private readonly List<PartitionEdge> _partitions = new();
        private readonly HashSet<(Vector2Int, Vector2Int)> _blocked = new();

        public RoomStructure(RoomFootprint footprint, float wallHeight)
        {
            Footprint  = footprint;
            WallHeight = wallHeight;
        }

        public RoomFootprint Footprint  { get; }
        public float         WallHeight { get; }

        public IReadOnlyList<StructurePillar> Pillars    => _pillars;
        public IReadOnlyList<PartitionEdge>   Partitions => _partitions;
        public int ZoneCount { get; private set; } = 1;

        public RoomTileTags TagsAt(Vector2Int tile) => _tags.TryGetValue(tile, out RoomTileTags tags) ? tags : RoomTileTags.None;
        public bool Has(Vector2Int tile, RoomTileTags tags) => (TagsAt(tile) & tags) != 0;
        public void Tag(Vector2Int tile, RoomTileTags tags) => _tags[tile] = TagsAt(tile) | tags;

        // Which part of the room a tile is in, once inner walls have split it (0 when none).
        public int ZoneOf(Vector2Int tile) => _zones.TryGetValue(tile, out int zone) ? zone : 0;

        // Floor nothing has claimed: not a route, doorway, centre spot or pillar.
        public bool IsOpen(Vector2Int tile) => Footprint.Contains(tile) && !Has(tile, KeepClear);

        public bool IsPartitioned(Vector2Int a, Vector2Int b) => _blocked.Contains(Key(a, b));

        // A free-standing pillar takes the tile it stands on, which must be open. One set
        // into the walls (freeStanding = false) takes no floor.
        public bool TryAddPillar(Vector2 position, float width, GameObject prefab, bool freeStanding = true)
        {
            var tile = new Vector2Int(Mathf.FloorToInt(position.x), Mathf.FloorToInt(position.y));
            if (freeStanding)
            {
                if (!IsOpen(tile)) return false;
                Tag(tile, RoomTileTags.Structure);
            }
            _pillars.Add(new StructurePillar(position, width, prefab));
            return true;
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
