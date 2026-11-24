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
    // the room is built, so the tall ones are found once. CanSee walks a straight line between
    // two tiles over those rules: the one place a line of sight is traced.
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

        // Whether an eye in the middle of `from` sees the middle of `to`: the straight line between
        // them crosses only open floor and no tall inner wall. `from` must be open (nobody stands
        // inside a pillar); `to` may be taken, since the face of a prop is seen like any other
        // floor. Between two open tiles the answer is the same both ways.
        //
        // Walks the tiles the line passes through. Where it leaves a tile exactly through a corner,
        // the diagonal step is open if either way round the corner is: a line grazes a pillar's
        // corner but doesn't squeeze between two.
        public bool CanSee(Vector2Int from, Vector2Int to)
        {
            if (BlocksTile(from) || !_structure.Contains(to)) return false;

            int nx = Mathf.Abs(to.x - from.x), ny = Mathf.Abs(to.y - from.y);
            int stepX = to.x >= from.x ? 1 : -1, stepY = to.y >= from.y ? 1 : -1;
            Vector2Int tile = from;
            int doneX = 0, doneY = 0;
            while (doneX < nx || doneY < ny)
            {
                // Which edge the line crosses first: a vertical one at (doneX + ½) / nx of the way,
                // or a horizontal one at (doneY + ½) / ny. Both at once is a corner.
                long order = (1L + 2 * doneX) * ny - (1L + 2 * doneY) * nx;
                Vector2Int next;
                if (order == 0)
                {
                    next = new Vector2Int(tile.x + stepX, tile.y + stepY);
                    if (!CanCutCorner(tile, next)) return false;
                    doneX++;
                    doneY++;
                }
                else
                {
                    bool acrossX = order < 0;
                    next = acrossX ? new Vector2Int(tile.x + stepX, tile.y) : new Vector2Int(tile.x, tile.y + stepY);
                    if (acrossX) doneX++; else doneY++;
                    if (BlocksEdge(tile, next)) return false;
                }
                if (next != to && BlocksTile(next)) return false;
                tile = next;
            }
            return true;
        }

        private bool CanCutCorner(Vector2Int from, Vector2Int to) =>
            OpenVia(from, new Vector2Int(to.x, from.y), to) || OpenVia(from, new Vector2Int(from.x, to.y), to);

        private bool OpenVia(Vector2Int from, Vector2Int via, Vector2Int to) =>
            !BlocksTile(via) && !BlocksEdge(from, via) && !BlocksEdge(via, to);
    }
}
