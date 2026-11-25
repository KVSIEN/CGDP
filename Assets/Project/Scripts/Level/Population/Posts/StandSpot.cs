using System;
using UnityEngine;

namespace CGD.Level
{
    // Where someone stands in a room: on a floor tile, or on the overpass deck over it
    // (RoomStructure.OverpassAbove). LevelLayout.SpotToFloor gives the point.
    public readonly struct StandSpot : IEquatable<StandSpot>
    {
        public StandSpot(Vector2Int tile, bool onDeck = false)
        {
            Tile   = tile;
            OnDeck = onDeck;
        }

        public Vector2Int Tile   { get; }
        public bool       OnDeck { get; }

        public bool Equals(StandSpot other) => Tile == other.Tile && OnDeck == other.OnDeck;
        public override bool Equals(object obj) => obj is StandSpot other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Tile, OnDeck);
    }
}
