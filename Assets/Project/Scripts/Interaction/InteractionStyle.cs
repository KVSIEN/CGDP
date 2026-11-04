namespace CGD.Interaction
{
    // How the Interact key is used on a target.
    public enum InteractionStyle
    {
        // One press.
        Press,
        // Hold the key for the target's HoldDuration.
        Hold,
        // Tap the key repeatedly: the bar fills a little per tap and drains while you stop.
        Mash,
    }
}
