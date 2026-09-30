using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A map node turned into floor tiles of some shape, what it is on the ship, and (once
    // doorways are known) its structure.
    public class LevelRoom
    {
        public LevelRoom(MapNode node, RoomFootprint footprint, RoomFunction function, float wallHeight)
        {
            Node       = node;
            Footprint  = footprint;
            Function   = function;
            WallHeight = wallHeight;
        }

        public MapNode       Node       { get; }
        public RoomFootprint Footprint  { get; }
        public RoomFunction  Function   { get; }    // null for a plain room
        public float         WallHeight { get; }    // metres
        public RoomStructure Structure  { get; private set; }

        // Middle of the room's bounds, in tile units.
        public Vector2 Center => Footprint.Center;
        // A point on the floor near the middle (see RoomFootprint.Anchor).
        public Vector2 Anchor => Footprint.Anchor;

        public string DisplayName => Function != null ? Function.DisplayName : Node.Type.ToString();

        // Set once by LevelLayoutBuilder after the corridors fix the doorways.
        public void AttachStructure(RoomStructure structure)
        {
            if (Structure != null) throw new System.InvalidOperationException("Room structure is already set.");
            Structure = structure;
        }
    }
}
