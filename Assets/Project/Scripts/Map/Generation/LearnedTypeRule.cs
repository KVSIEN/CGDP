namespace CGD.Map
{
    // One node type as measured across the example maps (see MapStyleLearner).
    public class LearnedTypeRule
    {
        public MapNodeType  Type;
        // False when no example map has this type: it gets a maximum of 0.
        public bool         Seen;
        public int          Min;
        public int          Max;
        // Average count per map, as the rule's relative weight.
        public float        Weight;
        public MapPlacement Placement = MapPlacement.Anywhere;
        public float        MinDepth;
        public float        MaxDepth = 1f;
        public bool         PreferDeadEnds;
        // Whether two rooms of this type ever sat side by side.
        public bool         SeenAdjacent;
        // Fewest connections between two rooms of this type in one map; 0 = fewer than two in every map.
        public int          ClosestSpacing;
    }
}
