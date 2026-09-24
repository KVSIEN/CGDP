using System;
using System.Collections.Generic;
using CGD.Core;

namespace CGD.Map
{
    // Turns MapGenerationSettings and a seed into a MapGraph. Deterministic: the same
    // settings, seed, layer variants and pins always produce the same graph.
    //
    // Runs as separate passes — structure, types, intensity, factions — each with its
    // own seed layer, so a layer can be rerolled (SeedVariants.Reroll) without changing
    // the layers before it. Later layers read earlier results, so a new layout still
    // changes the types placed on it.
    public class MapGenerator
    {
        public const string LayoutLayer    = "layout";
        public const string TypesLayer     = "types";
        public const string IntensityLayer = "intensity";
        public const string FactionsLayer  = "factions";

        public static readonly IReadOnlyList<string> Layers =
            new[] { LayoutLayer, TypesLayer, IntensityLayer, FactionsLayer };

        private readonly MapGenerationSettings _settings;

        public MapGenerator(MapGenerationSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public MapGenerationResult Generate(Seed seed) => Generate(seed, null, Array.Empty<MapNodePin>());

        public MapGenerationResult Generate(Seed seed, SeedVariants variants, IReadOnlyList<MapNodePin> pins)
        {
            var context = new MapGenerationContext(_settings, seed, variants);

            new MapLayoutBuilder(context).Build();

            var types = new MapTypeAssigner(context);
            types.Assign(pins);

            new MapIntensityPainter(context).Paint();
            ApplyPinnedIntensity(context.Graph, pins, types.PinnedNodeIds);

            new MapFactionPainter(context).Paint();

            return new MapGenerationResult(context.Graph, types.PinnedNodeIds, context.Warnings);
        }

        private static void ApplyPinnedIntensity(MapGraph graph, IReadOnlyList<MapNodePin> pins, IReadOnlyList<int> nodeIds)
        {
            for (int i = 0; i < pins.Count; i++)
                if (graph.TryGetNode(nodeIds[i], out MapNode node))
                    node.Intensity = pins[i].Intensity;
        }
    }
}
