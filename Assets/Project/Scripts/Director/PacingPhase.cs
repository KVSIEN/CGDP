namespace CGD.Director
{
    // The director's pacing cycle, after Left 4 Dead's: pressure builds until the player is
    // stressed, holds at the peak for a moment, eases off until they have calmed down, then
    // leaves them alone for a while before building again.
    public enum PacingPhase
    {
        BuildUp,
        SustainPeak,
        PeakFade,
        Relax,
    }
}
