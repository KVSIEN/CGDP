using UnityEngine;

namespace CGD.Level
{
    // A flight of stairs taking one tile, climbing one level toward Up. The tile is at the lower
    // level; it is walked onto from its foot (the tile behind it, at the same level) and leads
    // to its head (the tile ahead, one level up). Its sides are closed, except onto a flight
    // beside it climbing the same way from the same level: a wide flight is walked across.
    // `Slope`: a smooth ramp of ground (a mound's side, a crater's) rather than built steps.
    public readonly struct StairFlight
    {
        public StairFlight(Vector2Int tile, Vector2Int up, bool slope = false)
        {
            Tile  = tile;
            Up    = up;
            Slope = slope;
        }

        public Vector2Int Tile  { get; }
        public Vector2Int Up    { get; }
        public bool       Slope { get; }
        // The landing at the bottom and the one at the top, both kept clear.
        public Vector2Int Foot => Tile - Up;
        public Vector2Int Head => Tile + Up;
    }
}
