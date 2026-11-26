using System;
using System.Collections.Generic;
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
            FactionDefinition breachFaction = null, int chamfer = 0, ShipSide side = ShipSide.None, float designStrength = 1f,
            RoomLayout roomLayout = null, int landmarkTurns = 0)
        {
            LandmarkTurns = landmarkTurns;
            RoomLayout    = roomLayout;
            DesignStrength = Mathf.Clamp01(designStrength);
            Side          = side;
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
            new(Node, Footprint.Translated(offset), Function, WallHeight, Landmark, Faction, BreachFaction, Chamfer, Side, DesignStrength, RoomLayout, LandmarkTurns);

        public MapNode       Node       { get; }
        public RoomFootprint Footprint  { get; }
        public RoomFunction  Function   { get; }    // null for a plain room
        // Which of the function's arrangements this room is (null = the function has none).
        public RoomLayout    RoomLayout { get; }
        public float         WallHeight { get; }    // metres
        public RoomStructure Structure  { get; private set; }
        // The growth lying on its floor, if it is overgrown (OvergrowthPlanner); null otherwise.
        public OvergrowthField Overgrowth { get; private set; }
        // Tiles cut off each outside corner at 45° (0 = sharp corners).
        public int           Chamfer    { get; }
        // The wall line when it leaves the tile edges (curved or chamfered rooms); null otherwise.
        public RoomOutlinePlan Outline  { get; private set; }
        // The hand-built interior placed in this room, if any.
        public LandmarkRoomDefinition Landmark { get; }
        // Quarter turns, clockwise, the landmark was placed with (LandmarkTurn).
        public int LandmarkTurns { get; }
        // The faction that holds this room, if any (from the map graph).
        public FactionDefinition Faction { get; }
        // Breach rooms: the second faction bleeding in, whose enemies fight here too.
        public FactionDefinition BreachFaction { get; }
        // Who the room is for: guests in first class or steerage, or the crew.
        public ShipSide Side { get; }
        // How strongly the holding faction's design shows here: 1 in the core of its territory,
        // less toward the frontier (TerritoryDepth). See LevelBuildSettings.StyleFor.
        public float DesignStrength { get; }
        // Breach rooms: the floor tiles inside the doorways whose corridors lead from a room
        // the breach faction holds. Its side of the room is the one they open into.
        public IReadOnlyList<Vector2Int> BreachEntrances { get; private set; } = Array.Empty<Vector2Int>();

        // Middle of the room's bounds, in tile units.
        public Vector2 Center => Footprint.Center;
        // A point on the floor near the middle (see RoomFootprint.Anchor).
        public Vector2 Anchor => Footprint.Anchor;

        public string DisplayName => Function != null ? Function.DisplayName : Node.Type.ToString();
        // The place and its arrangement, "Casino: Table Pits", or just the place.
        public string LayoutName => RoomLayout != null ? $"{DisplayName}: {RoomLayout.DisplayName}" : DisplayName;

        // What this room's structure rules, set pieces and furniture come from.
        public IReadOnlyList<RoomStructureRule> StructureRules =>
            Function != null ? Function.StructureWith(RoomLayout) : Array.Empty<RoomStructureRule>();

        // What this room is furnished with, in order (RoomFunction.PropsWith).
        public IEnumerable<RoomPropPlacement> FunctionProps =>
            Function != null ? Function.PropsWith(RoomLayout) : Array.Empty<RoomPropPlacement>();

        // Set once by LevelLayoutBuilder once the doorways are known (no chamfer cuts a doorway).
        // Short notes the overview label shows under the room's details (an encounter's hazard).
        public IReadOnlyList<string> Tags => _tags;
        private readonly List<string> _tags = new();

        public void AddTag(string tag)
        {
            if (!string.IsNullOrEmpty(tag) && !_tags.Contains(tag)) _tags.Add(tag);
        }

        public void AttachOutline(RoomOutlinePlan outline) => Outline = outline;

        public void AttachBreachEntrances(IReadOnlyList<Vector2Int> entrances) => BreachEntrances = entrances ?? Array.Empty<Vector2Int>();

        public void AttachStructure(RoomStructure structure)
        {
            if (Structure != null) throw new InvalidOperationException("Room structure is already set.");
            Structure = structure;
        }

        // Set once the room is furnished, so the growth knows which floor its furniture stands on.
        public void AttachOvergrowth(OvergrowthField overgrowth)
        {
            if (Overgrowth != null) throw new InvalidOperationException("Room overgrowth is already set.");
            Overgrowth = overgrowth;
        }
    }
}
