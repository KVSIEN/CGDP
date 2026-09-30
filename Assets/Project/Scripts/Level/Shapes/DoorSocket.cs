using UnityEngine;

namespace CGD.Level
{
    // A place on a room's outline where a doorway fits: a floor tile on a straight stretch of
    // wall, and the direction out of the room.
    public readonly struct DoorSocket
    {
        public DoorSocket(Vector2Int inside, Vector2Int outward)
        {
            Inside  = inside;
            Outward = outward;
        }

        public Vector2Int Inside  { get; }
        public Vector2Int Outward { get; }
        public Vector2Int Outside => Inside + Outward;
    }
}
