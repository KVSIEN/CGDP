using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // What stops a person seeing across a room, in one place so every way of reading sight
    // (the runs along rows and columns, a straight line over the floor plan, who sees whom)
    // agrees: the room's own walls, tiles taken by a pillar or a prop, and inner walls as tall
    // as the eye. Seat rows and low counters are seen over.
    //
    // Tiles are asked live, so it follows props as they go down; inner walls never change once
    // the room is built, so the tall ones are found once.
    internal sealed class SightBlocking
    {
        // Tiles with one of these tags are in the way.
        public const RoomTileTags Tiles = RoomTileTags.Structure | RoomTileTags.Prop;
        // An inner wall lower than this is looked over.
        public const float EyeHeight = 1.6f;

        private readonly RoomStructure _structure;
        private readonly HashSet<(Vector2Int, Vector2Int)> _tallEdges = new();

        public SightBlocking(RoomStructure structure)
        {
            _structure = structure;
            foreach (PartitionEdge edge in structure.Partitions)
                if (edge.Height <= 0f || edge.Height >= EyeHeight) _tallEdges.Add((edge.Tile, edge.Other));
        }

        // Outside the floor plan, or taken by a pillar or a prop.
        public bool BlocksTile(Vector2Int tile) => !_structure.Contains(tile) || _structure.Has(tile, Tiles);

        // An inner wall tall enough to hide behind stands between two neighbouring tiles.
        public bool BlocksEdge(Vector2Int a, Vector2Int b)
        {
            if (!_structure.IsPartitioned(a, b)) return false;

            bool aFirst = a.x < b.x || a.y < b.y;
            return _tallEdges.Contains(aFirst ? (a, b) : (b, a));
        }
    }
}
