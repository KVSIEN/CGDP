namespace CGD.Enemies
{
    // Where an unaware enemy's eyes go during an idle beat or a pause.
    public enum GazePattern
    {
        // Glances at random spots within its arc, turning smoothly.
        Scatter,
        // Sweeps its arc from side to side at a steady rate, like a scanner.
        Sweep,
        // Picks one spot and stares at it, jumping there with no turn in between.
        Fixate,
        // Keeps facing the way it was.
        Hold,
        // Turns its back on the way it was facing, and stays that way.
        Away,
    }
}
