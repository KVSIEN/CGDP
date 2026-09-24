namespace CGD.Map
{
    public enum ConnectionType
    {
        Normal,
        // Skips ahead along the main path, creating an alternate route.
        Shortcut,
        // Hidden until discovered.
        Secret,
        // Gated behind a key, switch or other requirement.
        Locked
    }
}
