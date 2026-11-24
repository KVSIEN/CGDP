using CGD.Map;

namespace CGD.Level
{
    // Everything that decides what one made-up room is, as the room generator scene and the
    // Room Lab both state it. Left blank (no place, no faction…) the generator picks as a
    // level would. RoomRecipeGraph turns it into the map graph the level builder lays out.
    public sealed class RoomRecipe
    {
        public MapNodeType Type { get; set; } = MapNodeType.Combat;
        // Pinned place and layout; null lets the level builder pick from the category.
        public RoomFunction Place { get; set; }
        public RoomLayout Layout { get; set; }
        public RoomCategory Category { get; set; }
        public int Faction { get; set; } = MapNode.NoFaction;
        public int BreachFaction { get; set; } = MapNode.NoFaction;
        // How firmly the faction holds the room: full design at 0.7 and up, thinner toward the frontier.
        public float Influence { get; set; } = 1f;
        public int Section { get; set; } = MapNode.NoSection;
        public int Tier { get; set; } = 1;
        public float Intensity { get; set; } = 0.5f;
        public bool DoorNorth { get; set; } = true;
        public bool DoorEast { get; set; } = true;
        public bool DoorSouth { get; set; } = true;
        public bool DoorWest { get; set; }

        public int DoorCount => (DoorNorth ? 1 : 0) + (DoorEast ? 1 : 0) + (DoorSouth ? 1 : 0) + (DoorWest ? 1 : 0);
    }
}
