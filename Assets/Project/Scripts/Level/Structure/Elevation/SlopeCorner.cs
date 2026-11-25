using UnityEngine;

namespace CGD.Level
{
    // The corner of a mound or crater, where the slopes on two sides meet: a tile of ground
    // falling away diagonally, climbing one level toward Up (a diagonal step such as (1, 1)) from
    // the tile's own level. Too steep across to walk, so it is solid (RoomTileTags.Structure).
    public readonly struct SlopeCorner
    {
        public SlopeCorner(Vector2Int tile, Vector2Int up)
        {
            Tile = tile;
            Up   = up;
        }

        public Vector2Int Tile { get; }
        public Vector2Int Up   { get; }
    }
}
