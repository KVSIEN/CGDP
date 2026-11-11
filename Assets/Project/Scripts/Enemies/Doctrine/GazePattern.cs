namespace CGD.Enemies
{
    // How an unaware enemy lets its eyes wander while it stands watch or waits at a stop.
    public enum GazePattern
    {
        // Looks at a random spot within its arc now and then, turning smoothly.
        Scatter,
        // Sweeps its arc from side to side at a steady rate, like a scanner.
        Sweep,
        // Stares at one spot for a long while, then snaps to another with no turn in between.
        Fixate,
    }
}
