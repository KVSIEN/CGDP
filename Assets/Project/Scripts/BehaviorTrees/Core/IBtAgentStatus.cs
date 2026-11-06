using System.Collections.Generic;

namespace CGD.BehaviorTrees
{
    // Optional for a tree's agent: what it can say about itself beyond the blackboard (its
    // state, what it is allowed to do right now, how often it thinks), shown in the Behavior
    // Tree Editor's live view. Called only by the editor, while that agent is watched.
    public interface IBtAgentStatus
    {
        // Adds (label, value) rows, in display order.
        void DescribeStatus(List<(string Label, string Value)> rows);
    }
}
