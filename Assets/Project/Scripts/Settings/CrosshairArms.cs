using System;

namespace CGD.Settings
{
    [Flags]
    public enum CrosshairArms
    {
        None   = 0,
        Top    = 1,
        Bottom = 2,
        Left   = 4,
        Right  = 8,

        Horizontal = Left | Right,
        Vertical   = Top | Bottom,
        All        = Horizontal | Vertical,
    }
}
