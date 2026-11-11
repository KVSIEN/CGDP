namespace CGD.Enemies
{
    // A readable sign that an enemy has noticed something, made up its mind, or stood down.
    // Each faction has its own: visuals (the mark over its head, its pulse) play them differently.
    public enum TellKind
    {
        None,
        // Noticed something: a quick sweep of its sensors.
        Scan,
        // Noticed something: nose up, testing the air.
        Sniff,
        // Noticed something: turns to look and does not blink.
        Stare,
        // Committed to a fight: locks on.
        LockOn,
        // Committed to a fight: a roar that carries to the herd.
        Roar,
        // Stood down after a fight: a quiet all-clear.
        AllClear,
        // Stood down after a fight: the tension goes out of it.
        Settle,
        // Stood down after a fight: it simply loses interest and fades.
        Fade,
    }
}
