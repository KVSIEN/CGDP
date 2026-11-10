using System.Collections.Generic;

namespace CGD.Map
{
    // An optional area closed off by one gate. Only lives for one generation run, so the
    // type pass can put a reward behind each gate.
    internal class MapGatedArea
    {
        public MapGatedArea(MapConnection gate, int outsideId, IReadOnlyList<int> rooms)
        {
            Gate      = gate;
            OutsideId = outsideId;
            Rooms     = rooms;
        }

        public MapConnection      Gate      { get; }
        // The room on Start's side of the gate.
        public int                OutsideId { get; }
        // The rooms behind the gate.
        public IReadOnlyList<int> Rooms     { get; }
    }
}
