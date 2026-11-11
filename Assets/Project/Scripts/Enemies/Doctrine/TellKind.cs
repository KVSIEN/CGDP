namespace CGD.Enemies
{
    // A readable sign that an enemy has noticed something or made up its mind. Visuals and
    // audio react to it; each faction shows it its own way.
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
    }
}
