namespace CGD.Map
{
    // Where a node sits in the generated structure. Only lives for one generation run;
    // after that, MapGraphAnalysis derives the same facts from the graph itself.
    internal class MapSlot
    {
        public MapSlot(int nodeId, float progress, bool onMainPath, bool isStructural)
        {
            NodeId       = nodeId;
            Progress     = progress;
            OnMainPath   = onMainPath;
            IsStructural = isStructural;
        }

        public int   NodeId       { get; }
        public float Progress     { get; }
        public bool  OnMainPath   { get; }
        public bool  IsStructural { get; }
        public bool  IsDeadEnd    { get; set; }
    }
}
