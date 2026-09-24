using System;
using System.Collections.Generic;

namespace CGD.Map
{
    // Turns MapGenerationSettings and a seed into a MapGraph. Deterministic: the same
    // settings, seed and pins always produce the same graph.
    //
    // Runs as separate passes — structure, types, intensity, factions — so each
    // concern can be tuned or replaced without touching the others.
    public class MapGenerator
    {
        private readonly MapGenerationSettings _settings;

        public MapGenerator(MapGenerationSettings settings)
        {
            _settings = settings != null ? settings : throw new ArgumentNullException(nameof(settings));
        }

        public MapGenerationResult Generate(int seed) => Generate(seed, Array.Empty<MapNodePin>());

        public MapGenerationResult Generate(int seed, IReadOnlyList<MapNodePin> pins)
        {
            var context = new MapGenerationContext(_settings, seed);

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
