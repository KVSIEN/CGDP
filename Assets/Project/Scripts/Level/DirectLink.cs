using UnityEngine;

namespace CGD.Level
{
    // Two rooms placed side by side with a single tile between them: the doorway of both.
    // Each room's doorway opens onto Tile, which is the whole of the "hallway".
    public readonly struct DirectLink
    {
        public DirectLink(Vector2Int tile, Vector2Int insideA, Vector2Int insideB)
        {
            Tile    = tile;
            InsideA = insideA;
            InsideB = insideB;
        }

        public Vector2Int Tile    { get; }
        // The floor tiles of the connection's A and B rooms that the doorways stand on.
        public Vector2Int InsideA { get; }
        public Vector2Int InsideB { get; }
    }
}
