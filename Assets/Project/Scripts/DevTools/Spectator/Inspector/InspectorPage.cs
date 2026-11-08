namespace CGD.DevTools
{
    // What the inspected agent's panel shows (Inspector Page cycles through them in order).
    public enum InspectorPage
    {
        Overview,    // status, abilities, whom it weighs up and the tree nodes running now
        Blackboard,  // every value on its tree's blackboard
        Decisions,   // its latest tree decisions and tactical searches
        Hidden,
    }
}
