namespace CGD.Enemies
{
    // How an enemy follows up on its belief about an unseen target (Pursue Belief).
    public enum BeliefStyle
    {
        // Straight to the likeliest place, check it, and on to the next: hunting along the route.
        Sweep,
        // To cover that overlooks the likeliest places and hold there watching; when they are
        // empty, bound to the next vantage.
        Overwatch,
        // To a place picked at random in proportion to how likely it is: erratic, never aimless.
        Haunt,
    }
}
