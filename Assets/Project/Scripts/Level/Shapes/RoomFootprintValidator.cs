using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Rejects generated floor plans that don't play: split into pieces, or with passages too
    // narrow to walk (and for the NavMesh) — every tile must sit in a minWidth × minWidth
    // block of floor.
    public static class RoomFootprintValidator
    {
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        public static bool IsValid(RoomFootprint footprint, int minWidth) =>
            footprint.Count > 0 && IsConnected(footprint) && IsWideEnough(footprint, minWidth);

        public static bool IsConnected(RoomFootprint footprint)
        {
            if (footprint.Count == 0) return false;

            var reached = new HashSet<Vector2Int> { footprint.Tiles[0] };
            var open = new Stack<Vector2Int>();
            open.Push(footprint.Tiles[0]);

            while (open.Count > 0)
            {
                Vector2Int tile = open.Pop();
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int next = tile + side;
                    if (footprint.Contains(next) && reached.Add(next)) open.Push(next);
                }
            }
            return reached.Count == footprint.Count;
        }

        public static bool IsWideEnough(RoomFootprint footprint, int minWidth)
        {
            if (minWidth <= 1) return true;

            foreach (Vector2Int tile in footprint.Tiles)
                if (!InAnyBlock(footprint, tile, minWidth)) return false;
            return true;
        }

        private static bool InAnyBlock(RoomFootprint footprint, Vector2Int tile, int size)
        {
            for (int ox = 0; ox < size; ox++)
                for (int oy = 0; oy < size; oy++)
                    if (IsBlock(footprint, tile - new Vector2Int(ox, oy), size)) return true;
            return false;
        }

        private static bool IsBlock(RoomFootprint footprint, Vector2Int corner, int size)
        {
            for (int x = 0; x < size; x++)
                for (int y = 0; y < size; y++)
                    if (!footprint.Contains(corner + new Vector2Int(x, y))) return false;
            return true;
        }
    }
}
