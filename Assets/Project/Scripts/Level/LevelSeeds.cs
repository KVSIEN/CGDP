using CGD.Core;

namespace CGD.Level
{
    // The branches of a level's seed that each part of the build draws from. LevelBuilder builds
    // with them, and the editor's previews (the Room Lab, the map blueprint) take the same ones,
    // so what they show is what the level gets.
    public static class LevelSeeds
    {
        public static Seed Map(Seed level)              => level.Derive("map");
        // Furniture, enemies and the rest of what goes in the rooms (RoomPopulator, RoomFurnisher).
        public static Seed Contents(Seed level)         => level.Derive("contents");
        public static Seed Overgrowth(Seed level)       => level.Derive("overgrowth");
        public static Seed Encounters(Seed level)       => level.Derive("encounters");
        public static Seed SearchFinds(Seed level)      => level.Derive("search finds");
        public static Seed Objectives(Seed level)       => level.Derive("objectives");
        public static Seed ObjectiveRewards(Seed level) => level.Derive("objective rewards");
    }
}
