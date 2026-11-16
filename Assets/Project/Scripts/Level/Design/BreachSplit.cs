using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // How a Breach room is shared between its two factions: each zone (the pieces its
    // dividing walls cut it into) is dressed by one of them. The breach faction takes the
    // zones its doorways open into; where none does (or every zone has one), it takes every
    // zone but the largest, which stays the holder's. A room in one piece isn't split.
    public sealed class BreachSplit
    {
        private readonly bool[] _breach;

        private BreachSplit(bool[] breach) => _breach = breach;

        // null when the room is one zone: both factions dress all of it.
        public static BreachSplit Of(RoomStructure structure, IReadOnlyList<Vector2Int> breachEntrances)
        {
            if (structure == null || structure.ZoneCount < 2) return null;

            var breach = new bool[structure.ZoneCount];
            int marked = 0;
            if (breachEntrances != null)
                foreach (Vector2Int entrance in breachEntrances)
                {
                    if (!structure.Footprint.Contains(entrance)) continue;
                    int zone = structure.ZoneOf(entrance);
                    if (breach[zone]) continue;
                    breach[zone] = true;
                    marked++;
                }

            if (marked == 0 || marked == breach.Length)
            {
                int largest = Largest(structure);
                for (int zone = 0; zone < breach.Length; zone++) breach[zone] = zone != largest;
            }
            return new BreachSplit(breach);
        }

        public int ZoneCount => _breach.Length;

        public bool IsBreachZone(int zone) => zone >= 0 && zone < _breach.Length && _breach[zone];

        // The zones one side dresses.
        public HashSet<int> Zones(bool breachSide)
        {
            var zones = new HashSet<int>();
            for (int zone = 0; zone < _breach.Length; zone++)
                if (_breach[zone] == breachSide) zones.Add(zone);
            return zones;
        }

        private static int Largest(RoomStructure structure)
        {
            var sizes = new int[structure.ZoneCount];
            foreach (Vector2Int tile in structure.Footprint.Tiles) sizes[structure.ZoneOf(tile)]++;

            int largest = 0;
            for (int zone = 1; zone < sizes.Length; zone++)
                if (sizes[zone] > sizes[largest]) largest = zone;
            return largest;
        }
    }
}
