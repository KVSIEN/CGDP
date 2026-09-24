using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Shared state for the passes of one MapGenerator run.
    internal class MapGenerationContext
    {
        private readonly Seed         _seed;
        private readonly SeedVariants _variants;

        public MapGenerationContext(MapGenerationSettings settings, Seed seed, SeedVariants variants)
        {
            Settings  = settings;
            _seed     = seed;
            _variants = variants;
        }

        public MapGenerationSettings Settings { get; }
        public MapGraph              Graph    { get; } = new();
        public List<MapSlot>         Slots    { get; } = new();
        public List<string>          Warnings { get; } = new();

        public MapSlot GetSlot(int nodeId) => Slots.Find(s => s.NodeId == nodeId);

        // Each pass draws from its own stream, so rerolling one layer (a new variant)
        // leaves the numbers every other layer sees untouched.
        public RandomStream StreamFor(string layer) =>
            (_variants != null ? _variants.Resolve(_seed, layer) : _seed.Derive(layer)).Stream();
    }
}
