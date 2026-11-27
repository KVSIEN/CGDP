using UnityEngine;

namespace CGD.Level
{
    // A place on a room's outline where a doorway fits: a floor tile on a straight stretch of
    // wall (or a step of a slanted one), and the direction out of the room.
    public readonly struct DoorSocket
    {
        public DoorSocket(Vector2Int inside, Vector2Int outward, bool slanted = false)
        {
            Inside  = inside;
            Outward = outward;
            Slanted = slanted;
        }

        public Vector2Int Inside  { get; }
        public Vector2Int Outward { get; }
        // On a step of a 45° wall (an angled room's slanted side): the wall squares off round it
        // (RoomOutline), and the corridor's first tile beyond Outside sits by the next step, so it
        // leaves straight out past it (Lead).
        public bool       Slanted { get; }
        public Vector2Int Lead    => Outside + Outward;
        public Vector2Int Outside => Inside + Outward;
    }
}
