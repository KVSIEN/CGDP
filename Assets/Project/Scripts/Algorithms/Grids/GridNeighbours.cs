using System.Collections.Generic;
using UnityEngine;

namespace CGD.Algorithms
{
    // The steps from a tile to its neighbours, for the grid algorithms. Read-only: walking them in
    // a fixed order keeps seeded results repeatable.
    public static class GridNeighbours
    {
        // Up, right, down, left: the tiles sharing a side.
        public static readonly IReadOnlyList<Vector2Int> Four = new[]
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
        };

        // The four sides, then the four corners.
        public static readonly IReadOnlyList<Vector2Int> Eight = new[]
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1),
        };

        public static bool IsDiagonal(Vector2Int step) => step.x != 0 && step.y != 0;

        public static bool InBounds(Vector2Int tile, int width, int height) =>
            tile.x >= 0 && tile.y >= 0 && tile.x < width && tile.y < height;
    }
}
