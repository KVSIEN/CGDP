using UnityEngine;

namespace CGD.Level
{
    // One room (or the hall) of a building's blueprint: the tiles it covers and what furnishes it.
    public readonly struct PlannedSpace
    {
        public PlannedSpace(RectInt rect, RoomFunction function, string name, bool isHall)
        {
            Rect     = rect;
            Function = function;
            Name     = name;
            IsHall   = isHall;
        }

        public RectInt      Rect     { get; }
        public RoomFunction Function { get; }
        public string       Name     { get; }
        public bool         IsHall   { get; }
    }
}
