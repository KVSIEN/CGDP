using UnityEngine;

namespace CGD.Level
{
    // A flight of stairs taking one tile, climbing one level toward Up. The tile is at the lower
    // level; it is walked onto from its foot (the tile behind it, at the same level) and leads
    // to its head (the tile ahead, one level up). Its sides are closed: a flight is entered
    // along its length, never from the side.
    public readonly struct StairFlight
    {
        public StairFlight(Vector2Int tile, Vector2Int up)
        {
            Tile = tile;
            Up   = up;
        }

        public Vector2Int Tile { get; }
        public Vector2Int Up   { get; }
        // The landing at the bottom and the one at the top, both kept clear.
        public Vector2Int Foot => Tile - Up;
        public Vector2Int Head => Tile + Up;
    }
}
