using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // A deck crossing a room above its floor, like a footbridge: a straight run of tiles at Level,
    // at least RoomElevation.OverpassClearance above the floor under it, which stays walkable.
    // A flight climbs onto each end from floor a level below the deck (raised walks either side
    // of a lane), on the tile just before the deck; the tile behind each flight is its landing.
    // The deck is railed along both sides. It is a way across, never the only one: the floor
    // either end must already be reachable without it.
    public sealed class Overpass
    {
        // `start` and `end`: the flights' tiles at either end; the deck is every tile between them.
        public Overpass(int level, Vector2Int start, Vector2Int end)
        {
            Level = level;
            Vector2Int span = end - start;
            Along = new Vector2Int(System.Math.Sign(span.x), System.Math.Sign(span.y));

            var deck = new List<Vector2Int>();
            if (span.x == 0 || span.y == 0)
                for (Vector2Int tile = start + Along; tile != end; tile += Along) deck.Add(tile);
            Deck   = deck;
            Stairs = new[] { new StairFlight(start, Along), new StairFlight(end, -Along) };
        }

        public int                        Level  { get; }
        // The way the deck runs, from the first flight to the second (zero for a start and end not in line).
        public Vector2Int                 Along  { get; }
        public IReadOnlyList<Vector2Int>  Deck   { get; }
        // The flight onto each end, climbing toward the deck.
        public IReadOnlyList<StairFlight> Stairs { get; }
    }
}
