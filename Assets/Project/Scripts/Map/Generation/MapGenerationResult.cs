using System.Collections.Generic;

namespace CGD.Map
{
    public class MapGenerationResult
    {
        public const int PinNotPlaced = -1;

        public MapGenerationResult(MapGraph graph, MapLayoutSettings layout, IReadOnlyList<int> pinnedNodeIds,
                                   IReadOnlyList<string> warnings, IReadOnlyList<MapRunModifier> modifiers = null)
        {
            Modifiers     = modifiers ?? System.Array.Empty<MapRunModifier>();
            Graph         = graph;
            Layout        = layout;
            PinnedNodeIds = pinnedNodeIds;
            Warnings      = warnings;
        }

        public MapGraph Graph { get; }

        // The layout the style picked for this seed.
        public MapLayoutSettings Layout { get; }

        // One entry per pin passed to the generator, in the same order: the node that
        // received it, or PinNotPlaced.
        public IReadOnlyList<int> PinnedNodeIds { get; }

        // Constraints the generator couldn't satisfy. The graph is still usable.
        public IReadOnlyList<string> Warnings { get; }

        // The run modifiers this map was generated with.
        public IReadOnlyList<MapRunModifier> Modifiers { get; }
    }
}
