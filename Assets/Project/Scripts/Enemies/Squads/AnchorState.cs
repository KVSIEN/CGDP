namespace CGD.Enemies
{
    // Where it stands against its squad's anchor, for the Anchor Tether condition.
    public enum AnchorState
    {
        // Farther from the anchor than its radius (plus slack): a stray to call back.
        Stretched,
        // Within the anchor's radius (plus slack): where it should be.
        Within
    }
}
