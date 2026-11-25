using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // Bresenham's line algorithm (Jack Bresenham, 1962): the tiles a straight line from one tile
    // to another passes through, with no gaps and no doubled-up tiles.
    //
    // How it works: walk from the start toward the end one tile at a time. Along the line's long
    // axis every step moves one tile; along the short axis it moves only sometimes. A running
    // "error" keeps count of how far the drawn tiles have drifted from the true line, and when it
    // passes half a tile, a short-axis step catches up. Only whole-number adding and comparing,
    // so it is fast and exact.
    //
    // Why it's useful: tile-based line of sight (can the guard on this tile see that tile?), the
    // tiles a shot, a laser or a thrown object crosses, drawing straight walls, roads or
    // corridors into a grid, and tracing a ray across a map for a quick check before a costly
    // physics raycast.
    public static class GridLine
    {
        // Every tile from `from` to `to`, both included, in order.
        public static List<Vector2Int> Between(Vector2Int from, Vector2Int to)
        {
            var tiles = new List<Vector2Int>();
            int dx = Mathf.Abs(to.x - from.x), stepX = from.x < to.x ? 1 : -1;
            int dy = -Mathf.Abs(to.y - from.y), stepY = from.y < to.y ? 1 : -1;
            int error = dx + dy;

            Vector2Int tile = from;
            while (true)
            {
                tiles.Add(tile);
                if (tile == to) return tiles;

                int doubled = 2 * error;
                if (doubled >= dy) { error += dy; tile.x += stepX; }   // time for a step along x
                if (doubled <= dx) { error += dx; tile.y += stepY; }   // time for a step along y
            }
        }

        // Whether nothing blocks the line between the two tiles (the end tiles themselves aren't asked).
        public static bool IsClear(Vector2Int from, Vector2Int to, Func<Vector2Int, bool> blocks)
        {
            List<Vector2Int> tiles = Between(from, to);
            for (int i = 1; i < tiles.Count - 1; i++)
                if (blocks(tiles[i])) return false;
            return true;
        }
    }
}
