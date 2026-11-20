using UnityEngine;

namespace CGD.Level
{
    // How a furnished room plays from its way in, for room tools to compare rooms by:
    //   - Revealed: the share of open floor seen from the arrival. Low is a room that keeps
    //     its secrets until you are in it; high one you read at a glance from the door.
    //   - Filled: the share of the floor props stand on.
    //   - Flank: whether a way round the main lane is left (RoomFlank).
    // A room with no way in (the Start room) reveals nothing: HasArrival is false.
    public readonly struct RoomFlowReading
    {
        private const RoomTileTags Solid = RoomTileTags.Structure | RoomTileTags.Prop;

        private RoomFlowReading(bool hasArrival, float revealed, float filled, bool flank)
        {
            HasArrival = hasArrival;
            Revealed   = revealed;
            Filled     = filled;
            Flank      = flank;
        }

        public bool  HasArrival { get; }
        public float Revealed   { get; }
        public float Filled     { get; }
        public bool  Flank      { get; }

        public static RoomFlowReading Of(RoomStructure structure)
        {
            int floor = 0, open = 0, seen = 0, props = 0;
            ArrivalSight sight = structure.Arrival.HasValue ? new ArrivalSight(structure) : null;
            foreach (Vector2Int tile in structure.Footprint.Tiles)
            {
                floor++;
                if (structure.Has(tile, RoomTileTags.Prop)) props++;
                if (structure.Has(tile, Solid)) continue;
                open++;
                if (sight != null && sight.Sees(new RectInt(tile, Vector2Int.one))) seen++;
            }
            return new RoomFlowReading(sight != null, open > 0 ? seen / (float)open : 0f,
                                       floor > 0 ? props / (float)floor : 0f, RoomFlank.Has(structure));
        }
    }
}
