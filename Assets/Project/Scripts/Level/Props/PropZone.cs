namespace CGD.Level
{
    // Which part of a room divided by inner walls a prop goes in.
    public enum PropZone
    {
        Any,
        Largest,   // the main space (a restaurant's dining room)
        Others,    // everything but the main space (its kitchen); none in an undivided room
        Raised,    // only on floor above the doorways' level (a mezzanine's or platform's); none in a flat room
    }
}
