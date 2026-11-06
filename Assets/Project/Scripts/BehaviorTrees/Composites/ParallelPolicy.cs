namespace CGD.BehaviorTrees
{
    // When a Parallel node finishes.
    public enum ParallelPolicy
    {
        // Succeeds once every child has succeeded; fails as soon as one fails.
        RequireAll,
        // Succeeds as soon as one child succeeds; fails once every child has failed.
        RequireOne,
    }
}
