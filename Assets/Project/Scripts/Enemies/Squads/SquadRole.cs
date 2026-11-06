namespace CGD.Enemies
{
    // A squad member's job in the current fight.
    public enum SquadRole
    {
        // Not in a squad, or the squad isn't fighting.
        None,
        // Goes straight at the target.
        Assault,
        // Circles round to the target's side while others keep it busy.
        Flank,
        // Keeps the target's head down with fire, from cover if it has some.
        Suppress,
    }
}
