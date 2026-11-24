using UnityEngine;

namespace CGD.Level
{
    // What a player sees from the doorway they arrive by: a straight line over the floor plan
    // from the arrival tile, stopped by pillars, props and inner walls taller than eye height
    // (seat rows and low counters are seen over), and by the room's own walls.
    internal sealed class ArrivalSight
    {
        // How far the line moves per check, in tiles: fine enough not to slip past a corner.
        private const float Step = 0.2f;

        private readonly RoomStructure _structure;
        private readonly SightBlocking _blocking;

        // `blocking`: the room's sight rules, when something else already holds them.
        public ArrivalSight(RoomStructure structure, SightBlocking blocking = null)
        {
            _structure = structure;
            _blocking  = blocking ?? new SightBlocking(structure);
        }

        // Whether any part of `target` can be seen from the arrival (false when the room has none).
        public bool Sees(RectInt target)
        {
            if (!_structure.Arrival.HasValue) return false;

            Vector2 from = _structure.Arrival.Value + Vector2.one * 0.5f;
            Vector2 to = target.center;
            int steps = Mathf.CeilToInt(Vector2.Distance(from, to) / Step);
            Vector2Int previous = _structure.Arrival.Value;
            for (int i = 1; i <= steps; i++)
            {
                Vector2 point = Vector2.Lerp(from, to, (float)i / steps);
                var tile = new Vector2Int(Mathf.FloorToInt(point.x), Mathf.FloorToInt(point.y));
                if (tile == previous) continue;
                if (target.Contains(tile)) return !Blocked(previous, tile);
                if (_blocking.BlocksTile(tile) || Blocked(previous, tile)) return false;
                previous = tile;
            }
            return true;
        }

        // A tall inner wall between two tiles; a diagonal step checks both ways round the corner
        // and is open if either is.
        private bool Blocked(Vector2Int a, Vector2Int b)
        {
            if (a.x == b.x || a.y == b.y) return _blocking.BlocksEdge(a, b);

            var viaX = new Vector2Int(b.x, a.y);
            var viaY = new Vector2Int(a.x, b.y);
            bool openViaX = _structure.Contains(viaX) && !_blocking.BlocksEdge(a, viaX) && !_blocking.BlocksEdge(viaX, b);
            bool openViaY = _structure.Contains(viaY) && !_blocking.BlocksEdge(a, viaY) && !_blocking.BlocksEdge(viaY, b);
            return !openViaX && !openViaY;
        }
    }
}
