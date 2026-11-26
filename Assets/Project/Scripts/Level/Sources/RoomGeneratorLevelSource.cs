using CGD.Core;
using CGD.Map;
using UnityEngine;

namespace CGD.Level
{
    // The one made-up room of the RoomGenerator scene: its seed, floor levels and overgrowth are
    // the generator's, and the player (or the spectator) starts in it. Positions are in cells.
    internal sealed class RoomGeneratorLevelSource : LevelSource
    {
        private readonly RoomGenerator _generator;

        public RoomGeneratorLevelSource(RoomGenerator generator) => _generator = generator;

        // The generator's seed, whatever LevelBuilder's field says.
        public override Seed ResolveSeed(string typed) => _generator.Seed;
        public override MapGraph BuildGraph(LevelBuildSettings settings, Seed seed) => _generator.BuildGraph(settings);
        public override MapContentSettings Content => _generator.Content;
        public override LevelLayoutBuilder LayoutBuilder(LevelBuildSettings settings) => _generator.LayoutBuilder(settings);
        public override OvergrowthSource Overgrowth(LevelBuildSettings settings) => _generator.Overgrowth(settings);
        public override int StartRoom(int pinned) => RoomRecipeGraph.RoomId;
        public override void Started(Vector3 centre, float reach) => _generator.FrameRoom(centre, reach);
    }
}
