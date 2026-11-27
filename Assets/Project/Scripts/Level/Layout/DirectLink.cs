using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // Two rooms the aligner put together: normally side by side with a single tile between
    // them, the doorway of both; where their shapes can't sit that close, a straight run of
    // tiles as short as they allow.
    public readonly struct DirectLink
    {
        public DirectLink(IReadOnlyList<Vector2Int> tiles, Vector2Int insideA, Vector2Int insideB)
        {
            Tiles   = tiles;
            InsideA = insideA;
            InsideB = insideB;
        }

        // The tiles between the two rooms, from A's doorway to B's.
        public IReadOnlyList<Vector2Int> Tiles { get; }
        // The floor tiles of the connection's A and B rooms that the doorways stand on.
        public Vector2Int InsideA { get; }
        public Vector2Int InsideB { get; }

        // The tiles each room's doorway opens onto: the same one when the rooms touch.
        public Vector2Int OutsideA => Tiles[0];
        public Vector2Int OutsideB => Tiles[Tiles.Count - 1];

        // The same link after both its rooms moved by `offset`.
        public DirectLink Translated(Vector2Int offset)
        {
            var tiles = new Vector2Int[Tiles.Count];
            for (int i = 0; i < tiles.Length; i++) tiles[i] = Tiles[i] + offset;
            return new DirectLink(tiles, InsideA + offset, InsideB + offset);
        }
    }
}
