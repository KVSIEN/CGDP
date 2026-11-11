namespace CGD.Enemies
{
    // Where an enemy whose morale broke falls back to.
    public enum RetreatDestination
    {
        // Cover that hides it from its target (its post if it knows none).
        Cover,
        // The nearest kin that still has its nerve: it rejoins the line.
        Kin,
        // Its post: home ground, where it recovers.
        Den,
    }
}
