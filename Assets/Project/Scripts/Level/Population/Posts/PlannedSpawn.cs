using UnityEngine;

namespace CGD.Level
{
    // One enemy RoomEnemyPlanner put in a room: its tile, which of the room's factions it
    // belongs to (Breach: the second one), what its tile offers, whether it holds the post, and
    // whether it starts up on the overpass deck over its tile rather than on the floor.
    public readonly struct PlannedSpawn
    {
        public PlannedSpawn(Vector2Int tile, bool breach, RoomPostKinds kinds, bool holds, bool onDeck = false)
        {
            Tile   = tile;
            OnDeck = onDeck;
            Breach = breach;
            Kinds  = kinds;
            Holds  = holds;
        }

        public Vector2Int    Tile   { get; }
        public bool          OnDeck { get; }
        public StandSpot     Spot   => new(Tile, OnDeck);
        public bool          Breach { get; }
        public RoomPostKinds Kinds  { get; }
        // Stands guard on its post; otherwise patrols its side's route.
        public bool          Holds  { get; }
    }
}
