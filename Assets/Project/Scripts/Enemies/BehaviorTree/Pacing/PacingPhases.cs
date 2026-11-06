using System;

namespace CGD.Enemies
{
    // Any combination of the director's pacing phases, for Pacing Is.
    [Flags]
    public enum PacingPhases
    {
        None    = 0,
        BuildUp = 1 << 0,
        Peak    = 1 << 1,
        Fade    = 1 << 2,
        Calm    = 1 << 3,
        Pushing = BuildUp | Peak,
        Easing  = Fade | Calm,
    }
}
