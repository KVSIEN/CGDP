using UnityEngine;
using CGD.Factions;
using CGD.Map;

namespace CGD.Level
{
    // A map node turned into floor tiles of some shape, what it is on the ship, and (once
    // doorways are known) its structure.
    public class LevelRoom
    {
        public LevelRoom(MapNode node, RoomFootprint footprint, RoomFunction function, float wallHeight,
            LandmarkRoomDefinition landmark = null, FactionDefinition faction = null,
            FactionDefinition breachFaction = null, int chamfer = 0)
        {
            Chamfer       = chamfer;
            Faction       = faction;
            BreachFaction = breachFaction;
            Landmark   = landmark;
            Node       = node;
            Footprint  = footprint;
            Function   = function;
            WallHeight = wallHeight;
        }

        // The same room shifted by `offset` tiles. Only before its structure and outline are
        // planned, which depend on where the doorways end up.
        public LevelRoom Translated(Vector2Int offset) =>
            new(Node, Footprint.Translated(offset), Function, WallHeight, Landmark, Faction, BreachFaction, Chamfer);

        public MapNode       Node       { get; }
        public RoomFootprint Footprint  { get; }
        public RoomFunction  Function   { get; }    // null for a plain room
        public float         WallHeight { get; }    // metres
        public RoomStructure Structure  { get; private set; }
        // Tiles cut off each outside corner at 45° (0 = sharp corners).
        public int           Chamfer    { get; }
        // The wall line when it leaves the tile edges (curved or chamfered rooms); null otherwise.
        public RoomOutlinePlan Outline  { get; private set; }
        // The hand-built interior placed in this room, if any.
        public LandmarkRoomDefinition Landmark { get; }
        // The faction that holds this room, if any (from the map graph).
        public FactionDefinition Faction { get; }
        // Breach rooms: the second faction bleeding in, whose enemies fight here too.
        public FactionDefinition BreachFaction { get; }

        // Middle of the room's bounds, in tile units.
        public Vector2 Center => Footprint.Center;
        // A point on the floor near the middle (see RoomFootprint.Anchor).
        public Vector2 Anchor => Footprint.Anchor;

        public string DisplayName => Function != null ? Function.DisplayName : Node.Type.ToString();

        // Set once by LevelLayoutBuilder after the corridors fix the doorways.
        // Set once by LevelLayoutBuilder once the doorways are known (no chamfer cuts a doorway).
        public void AttachOutline(RoomOutlinePlan outline) => Outline = outline;

        public void AttachStructure(RoomStructure structure)
        {
            if (Structure != null) throw new System.InvalidOperationException("Room structure is already set.");
            Structure = structure;
        }
    }
}
