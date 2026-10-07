namespace CGD.Artifacts
{
    // How an offhand artifact is used with the aim input (right mouse).
    public enum OffhandUseMode
    {
        // Only its passive stats: the aim input stays free, so a gun keeps its sights.
        Passive,
        // One use per press (a spell, an invocation). Takes the aim input.
        Tap,
        // Active while held (a ward, a channel). Takes the aim input.
        Hold,
    }
}
