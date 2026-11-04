namespace CGD.FiringRange
{
    // How the firing range's dummies move, set by the Movement switch.
    public enum DummyMovement
    {
        Still,
        // Side to side across the lane.
        Sway,
        // Back and forth along the dummy's facing, toward and away from whoever is shooting.
        Advance,
    }
}
