namespace CGD.Enemies
{
    // How an enemy picks its next idle beat.
    public enum IdleOrder
    {
        // One after the other, round and round: a drill.
        Cycle,
        // By weight, at random: whatever the mood takes it to do.
        Weighted,
    }
}
