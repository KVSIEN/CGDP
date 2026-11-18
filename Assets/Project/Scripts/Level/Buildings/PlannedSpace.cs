using UnityEngine;

namespace CGD.Level
{
    // One room (or the hall) of a building's blueprint: the tiles it covers and what furnishes it.
    public readonly struct PlannedSpace
    {
        public PlannedSpace(RectInt rect, RoomFunction function, string name, bool isHall, RoomLayout layout = null)
        {
            Rect     = rect;
            Function = function;
            Name     = name;
            IsHall   = isHall;
            Layout   = layout;
        }

        public RectInt      Rect     { get; }
        public RoomFunction Function { get; }
        public string       Name     { get; }
        public bool         IsHall   { get; }
        // Which of the function's arrangements furnishes it (null = the function's own props).
        public RoomLayout   Layout   { get; }
    }
}
