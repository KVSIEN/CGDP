using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Where a corridor meets a room: the room's edge tile and the corridor tile just outside it.
    public class LevelDoorway
    {
        public LevelDoorway(LevelRoom room, MapConnection connection, Vector2Int roomTile, Vector2Int outsideTile, bool hasGate,
                            DoorwayDirection direction = DoorwayDirection.Level, Vector2Int slant = default)
        {
            Slant       = slant;
            Room        = room;
            Connection  = connection;
            RoomTile    = roomTile;
            OutsideTile = outsideTile;
            HasGate     = hasGate;
            Direction   = direction;
        }

        public LevelRoom     Room        { get; }
        public MapConnection Connection  { get; }
        public Vector2Int    RoomTile    { get; }
        public Vector2Int    OutsideTile { get; }
        // Each connection gets one door or gate, on the end nearer the Start room, so a
        // locked branch is locked from the side the player arrives from.
        public bool          HasGate     { get; }
        // Whether it leads back toward Start, onward, or neither (RoomFlowPlanner reads it).
        public DoorwayDirection Direction { get; }

        public Vector2Int Outward => OutsideTile - RoomTile;

        // Set into a 45° wall (DoorSocket.Slant): the wall's way out, a diagonal step; zero on a
        // straight wall. A slanted doorway stands across the wall's diagonal, centred on the same
        // point as a straight one would, √2 tiles wide.
        public Vector2Int Slant   { get; }
        public bool       Slanted => Slant != Vector2Int.zero;

        // Where everything built at a doorway stands (door, gate, shutter, sign, lintel, frame,
        // culling portal): the middle of the opening on the floor plan (tile units), the way out of
        // the room it faces, and how wide it is in tiles.
        public Vector2 Middle => RoomTile + Vector2.one * 0.5f + (Vector2)Outward * 0.5f;
        public Vector2 Facing => Slanted ? ((Vector2)Slant).normalized : Outward;
        public float   Width  => Slanted ? Mathf.Sqrt(2f) : 1f;

        // The opening's two ends, tile units.
        public (Vector2, Vector2) Ends
        {
            get
            {
                var across = new Vector2(-Facing.y, Facing.x) * (Width * 0.5f);
                return (Middle - across, Middle + across);
            }
        }
    }
}
