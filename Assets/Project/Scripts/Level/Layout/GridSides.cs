using UnityEngine;

namespace CGD.Level
{
    public static class GridSides
    {
        // The four tile neighbours, clockwise from up. Seeded generation walks them in this order,
        // so it must not change; never write into it.
        public static readonly Vector2Int[] All = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };
    }
}
