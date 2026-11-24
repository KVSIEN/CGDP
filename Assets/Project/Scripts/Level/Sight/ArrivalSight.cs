using UnityEngine;

namespace CGD.Level
{
    // What a player sees from the doorway they arrive by: a straight line over the floor plan
    // from the arrival tile, stopped by pillars, props and inner walls as tall as the eye (seat
    // rows and low counters are seen over) and by the room's own walls (SightBlocking). It reads
    // the room as it is now, so it needs no refreshing as props go down.
    internal sealed class ArrivalSight
    {
        private readonly RoomStructure _structure;
        private readonly SightBlocking _blocking;

        // `blocking`: the room's sight rules, when something else already holds them.
        public ArrivalSight(RoomStructure structure, SightBlocking blocking = null)
        {
            _structure = structure;
            _blocking  = blocking ?? new SightBlocking(structure);
        }

        // Whether any tile of `target` can be seen from the arrival (false when the room has none).
        // A tile a prop stands on is seen if its face is.
        public bool Sees(RectInt target)
        {
            if (!_structure.Arrival.HasValue) return false;

            foreach (Vector2Int tile in target.allPositionsWithin)
                if (_blocking.CanSee(_structure.Arrival.Value, tile)) return true;
            return false;
        }
    }
}
