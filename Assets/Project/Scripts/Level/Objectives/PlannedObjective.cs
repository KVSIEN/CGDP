using System.Collections.Generic;

namespace CGD.Level
{
    // A template assigned to real rooms of one map: the room each step happens in.
    public class PlannedObjective
    {
        public PlannedObjective(MapObjectiveTemplate template, bool isMain, IReadOnlyList<int> nodeIds, int topTier)
        {
            Template = template;
            IsMain   = isMain;
            NodeIds  = nodeIds;
            TopTier  = topTier;
        }

        public MapObjectiveTemplate Template { get; }
        public bool               IsMain    { get; }
        // NodeIds[i] = the room of step i.
        public IReadOnlyList<int> NodeIds   { get; }
        // The highest room tier among the steps; side rewards scale with it.
        public int                TopTier   { get; }
    }
}
