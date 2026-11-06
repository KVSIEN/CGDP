namespace CGD.Enemies
{
    // How much an enemy believes is going on, from what it has actually sensed.
    public enum AwarenessLevel
    {
        // Nothing noticed (or it has calmed down and forgotten).
        Unaware,
        // Noticed something — a glimpse, a footstep, a noise — and wants a look.
        Suspicious,
        // Senses a hostile right now and is fighting it.
        Engaged,
        // Knows a hostile is about (lost sight of it, was hit, heard an alarm) and is hunting.
        Searching,
    }
}
