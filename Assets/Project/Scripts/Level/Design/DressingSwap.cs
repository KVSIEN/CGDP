using System;

namespace CGD.Level
{
    // A faction's own version of a room type's dressing: wherever a room type lists
    // `Original` (generic cover, generic loot clutter), this faction's rooms get `Replacement`.
    [Serializable]
    public class DressingSwap
    {
        public RoomDressing Original;
        public RoomDressing Replacement;
    }
}
