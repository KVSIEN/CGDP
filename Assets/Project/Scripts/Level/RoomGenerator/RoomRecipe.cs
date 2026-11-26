using System;
using CGD.Map;

namespace CGD.Level
{
    // Everything that decides what one made-up room is, as the room generator scene and the
    // Room Lab both state it. Left blank (no place, no faction…) the generator picks as a
    // level would. RoomRecipeGraph turns it into the map graph the level builder lays out; the
    // floor levels go to the LevelLayoutBuilder. The Room Lab keeps one as the room it makes up.
    [Serializable]
    public sealed class RoomRecipe
    {
        public MapNodeType Type = MapNodeType.Combat;
        // Pinned place and layout; null lets the level builder pick from the category.
        public RoomFunction Place;
        public RoomLayout Layout;
        public RoomCategory Category;
        public int Faction = MapNode.NoFaction;
        // The faction sharing the room with the holder (a Breach's breacher, or a mixed room's
        // second): it comes in through the first doorway and holds that side.
        public int BreachFaction = MapNode.NoFaction;
        // How firmly the faction holds the room: full design at 0.7 and up, thinner toward the frontier.
        public float Influence = 1f;
        public int Section = MapNode.NoSection;
        public int Tier = 1;
        public float Intensity = 0.5f;
        // The floor levels the room may be raised or sunk to, within the build settings' own.
        public int LowestLevel = RoomElevation.Lowest;
        public int HighestLevel = RoomElevation.Highest;
        public bool DoorNorth = true;
        public bool DoorEast = true;
        public bool DoorSouth = true;
        public bool DoorWest;

        // The layout builder for the room: its floor levels held to the recipe's.
        public LevelLayoutBuilder LayoutBuilder(LevelBuildSettings settings) => new(settings, LowestLevel, HighestLevel);

        // A copy to change without touching this one.
        public RoomRecipe Clone() => (RoomRecipe)MemberwiseClone();

        public int DoorCount => (DoorNorth ? 1 : 0) + (DoorEast ? 1 : 0) + (DoorSouth ? 1 : 0) + (DoorWest ? 1 : 0);
    }
}
