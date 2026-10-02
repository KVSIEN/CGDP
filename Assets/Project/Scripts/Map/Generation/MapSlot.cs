namespace CGD.Map
{
    // Where a node sits in the generated structure. Only lives for one generation run;
    // after that, MapGraphAnalysis derives the same facts from the graph itself.
    internal class MapSlot
    {
        public MapSlot(int nodeId, int depth, float progress, bool onMainPath, bool isStructural)
        {
            NodeId       = nodeId;
            Depth        = depth;
            Progress     = progress;
            OnMainPath   = onMainPath;
            IsStructural = isStructural;
        }

        public int   NodeId       { get; }
        // Rooms from Start, not counting shortcuts.
        public int   Depth        { get; }
        public float Progress     { get; }
        public bool  OnMainPath   { get; }
        public bool  IsStructural { get; }
        public bool  IsDeadEnd    { get; set; }
        // Empty grid cells next to the room (see MapNodeTypeRule.WantsSpace).
        public int   FreeNeighbors { get; set; }
        // Position along the main path, or -1 off it.
        public int   MainPathIndex { get; set; } = -1;
    }
}
