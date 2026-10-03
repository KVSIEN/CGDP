using System.Collections.Generic;

namespace CGD.Level
{
    // Every shutter of one room, raised and lowered together.
    public class RoomSeal
    {
        private readonly List<RoomShutter> _shutters;

        public RoomSeal(List<RoomShutter> shutters) => _shutters = shutters;

        public bool IsSealed { get; private set; }

        public void Seal()    => Set(true);
        public void Release() => Set(false);

        private void Set(bool closed)
        {
            IsSealed = closed;
            foreach (RoomShutter shutter in _shutters)
                if (shutter != null) shutter.SetClosed(closed);
        }
    }
}
