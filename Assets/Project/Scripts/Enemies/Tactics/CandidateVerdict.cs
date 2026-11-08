namespace CGD.Enemies
{
    // What became of one spot a tactical search looked at.
    public enum CandidateVerdict
    {
        Picked,
        Passed,       // fine, but a better one won
        Exposed,      // cover the threat could still shoot over or past
        Taken,        // an ally got there first
        Seen,         // someone around could see it (Phase Shift)
        Unreachable,  // no complete path there
        TooFar,       // the route is too long a detour
    }
}
