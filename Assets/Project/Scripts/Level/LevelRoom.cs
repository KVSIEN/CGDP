using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A map node turned into floor tiles of some shape.
    public class LevelRoom
    {
        public LevelRoom(MapNode node, RoomFootprint footprint)
        {
            Node      = node;
            Footprint = footprint;
        }

        public MapNode       Node      { get; }
        public RoomFootprint Footprint { get; }

        // Middle of the room's bounds, in tile units.
        public Vector2 Center => Footprint.Center;
        // A point on the floor near the middle (see RoomFootprint.Anchor).
        public Vector2 Anchor => Footprint.Anchor;
    }
}
