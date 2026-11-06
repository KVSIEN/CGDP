namespace CGD.BehaviorTrees
{
    public enum BtTraceEvent
    {
        Started,
        Succeeded,
        Failed,
        // Stopped from above (its parent finished, or the agent died).
        Aborted,
        // Stopped because something else took over: Cause interrupted Node.
        Interrupted,
    }
}
