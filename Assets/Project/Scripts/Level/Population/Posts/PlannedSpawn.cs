using UnityEngine;

namespace CGD.Level
{
    // One enemy RoomEnemyPlanner put in a room: its tile, which of the room's factions it
    // belongs to (Breach: the second one), what its tile offers, and whether it holds the post.
    public readonly struct PlannedSpawn
    {
        public PlannedSpawn(Vector2Int tile, bool breach, RoomPostKinds kinds, bool holds)
        {
            Tile   = tile;
            Breach = breach;
            Kinds  = kinds;
            Holds  = holds;
        }

        public Vector2Int    Tile   { get; }
        public bool          Breach { get; }
        public RoomPostKinds Kinds  { get; }
        // Stands guard on its post; otherwise patrols its side's route.
        public bool          Holds  { get; }
    }
}
