namespace CGD.BehaviorTrees
{
    // One recorded decision: what happened to which node, when, and (for interruptions) why.
    public readonly struct BtTraceEntry
    {
        public readonly float        Time;
        public readonly int          Tick;
        public readonly BtTraceEvent Event;
        public readonly BtNode       Node;
        // Interrupted: the branch that took over, or the condition that stopped holding.
        public readonly BtNode       Cause;

        public BtTraceEntry(float time, int tick, BtTraceEvent kind, BtNode node, BtNode cause)
        {
            Time = time; Tick = tick; Event = kind; Node = node; Cause = cause;
        }
    }
}
