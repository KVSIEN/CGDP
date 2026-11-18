using UnityEngine;

namespace CGD.Level
{
    // A hand-built room turned by quarter turns, clockwise seen from above (north becomes east),
    // so one landmark can face any way. Plain maths on tiles: the room's size, its tiles and
    // walls, and where its interior prefab goes. A turn of 0 changes nothing; the count wraps.
    //
    // Tile coordinates are the room's own ((0, 0) = its south-west tile before the turn); a
    // turned room's are the same, measured from its own south-west corner.
    public static class LandmarkTurn
    {
        // Width and depth in tiles: swapped by an odd number of turns.
        public static Vector2Int Size(Vector2Int size, int turns) => (Wrap(turns) & 1) == 0 ? size : new Vector2Int(size.y, size.x);

        // Where a tile of a room `size` tiles across ends up.
        public static Vector2Int Tile(Vector2Int tile, Vector2Int size, int turns)
        {
            for (int i = Wrap(turns); i > 0; i--)
            {
                tile = new Vector2Int(tile.y, size.x - 1 - tile.x);
                size = new Vector2Int(size.y, size.x);
            }
            return tile;
        }

        public static RoomSide Side(RoomSide side, int turns) => (RoomSide)(((int)side + Wrap(turns)) % 4);

        // Where the interior prefab's origin goes, in tiles from the turned room's south-west
        // corner. The prefab is turned by Yaw about its origin, which swings its far corners
        // out of the room: this brings it back.
        public static Vector2 PrefabOffset(Vector2Int size, int turns) => Wrap(turns) switch
        {
            1 => new Vector2(0f, size.x),
            2 => new Vector2(size.x, size.y),
            3 => new Vector2(size.y, 0f),
            _ => Vector2.zero,
        };

        // The prefab's turn about the vertical axis, in degrees.
        public static float Yaw(int turns) => Wrap(turns) * 90f;

        private static int Wrap(int turns) => ((turns % 4) + 4) % 4;
    }
}
