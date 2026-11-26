using UnityEngine;

namespace CGD.Algorithms
{
    // The steps from a tile to its neighbours, for the grid algorithms and the level's rooms.
    // Arrays, so walking them allocates nothing in the hot searches; never write into them or
    // reorder them: seeded generation walks them in this order.
    public static class GridNeighbours
    {
        // Up, right, down, left: the tiles sharing a side.
        public static readonly Vector2Int[] Four =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
        };

        // The four sides, then the four corners.
        public static readonly Vector2Int[] Eight =
        {
            Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left,
            new Vector2Int(1, 1), new Vector2Int(1, -1), new Vector2Int(-1, -1), new Vector2Int(-1, 1),
        };

        // Which of Four the step is (0 up, 1 right, 2 down, 3 left), or -1.
        public static int IndexOf(Vector2Int side) => System.Array.IndexOf(Four, side);

        public static bool IsDiagonal(Vector2Int step) => step.x != 0 && step.y != 0;

        public static bool InBounds(Vector2Int tile, int width, int height) =>
            tile.x >= 0 && tile.y >= 0 && tile.x < width && tile.y < height;
    }
}
