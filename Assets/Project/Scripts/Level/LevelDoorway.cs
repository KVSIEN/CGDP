using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Where a corridor meets a room: the room's edge tile and the corridor tile just outside it.
    public class LevelDoorway
    {
        public LevelDoorway(LevelRoom room, MapConnection connection, Vector2Int roomTile, Vector2Int outsideTile, bool hasGate)
        {
            Room        = room;
            Connection  = connection;
            RoomTile    = roomTile;
            OutsideTile = outsideTile;
            HasGate     = hasGate;
        }

        public LevelRoom     Room        { get; }
        public MapConnection Connection  { get; }
        public Vector2Int    RoomTile    { get; }
        public Vector2Int    OutsideTile { get; }
        // Each connection gets one door or gate, on the end nearer the Start room, so a
        // locked branch is locked from the side the player arrives from.
        public bool          HasGate     { get; }

        public Vector2Int Outward => OutsideTile - RoomTile;
    }
}
