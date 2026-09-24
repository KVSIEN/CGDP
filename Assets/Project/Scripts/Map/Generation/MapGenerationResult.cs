using System.Collections.Generic;

namespace CGD.Map
{
    public class MapGenerationResult
    {
        public const int PinNotPlaced = -1;

        public MapGenerationResult(MapGraph graph, IReadOnlyList<int> pinnedNodeIds, IReadOnlyList<string> warnings)
        {
            Graph         = graph;
            PinnedNodeIds = pinnedNodeIds;
            Warnings      = warnings;
        }

        public MapGraph Graph { get; }

        // One entry per pin passed to the generator, in the same order: the node that
        // received it, or PinNotPlaced.
        public IReadOnlyList<int> PinnedNodeIds { get; }

        // Constraints the generator couldn't satisfy. The graph is still usable.
        public IReadOnlyList<string> Warnings { get; }
    }
}
