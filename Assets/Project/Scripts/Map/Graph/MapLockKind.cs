namespace CGD.Map
{
    // What opens a Locked connection. Every kind keeps its parts in key rooms the player
    // must reach first.
    public enum MapLockKind
    {
        // One key item, lying in the connection's single key room.
        Keycard,
        // A terminal in every key room; the door opens once all of them are switched on.
        Terminals
    }
}
