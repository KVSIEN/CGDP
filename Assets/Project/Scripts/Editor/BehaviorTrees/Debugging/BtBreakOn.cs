using System;

namespace CGD.Editor
{
    // When a breakpoint on a node pauses the game.
    [Flags]
    internal enum BtBreakOn
    {
        None    = 0,
        Start   = 1,
        Failure = 2,
    }
}
