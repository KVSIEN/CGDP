namespace CGD.Enemies
{
    // What an enemy points an ability at.
    public enum AbilityAim
    {
        // Where it believes its target is (the chest).
        Target,
        // Straight ahead.
        Forward,
        // Directly away from where it believes its target is: it turns its back to cast (a
        // forward dash used to get away).
        Away,
    }
}
