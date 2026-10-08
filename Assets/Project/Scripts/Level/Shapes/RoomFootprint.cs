using System.Collections.Generic;
using UnityEngine;

namespace CGD.Level
{
    // The floor tiles of one room, in level tile coordinates, and where doorways can go.
    // Immutable; built by RoomShapeRasterizer (or as a plain rectangle).
    public class RoomFootprint
    {
        private static readonly Vector2Int[] Sides = { Vector2Int.up, Vector2Int.right, Vector2Int.down, Vector2Int.left };

        private readonly HashSet<Vector2Int> _lookup;
        private readonly List<Vector2Int> _tiles;
        private readonly List<DoorSocket> _sockets = new();
        private readonly bool _hasFixedSockets;

        public RoomFootprint(IEnumerable<Vector2Int> tiles, bool curvedWalls = false)
            : this(tiles, curvedWalls, null) { }

        // fixedSockets replaces the automatic doorway spots (a hand-built room's doorways);
        // entries not on the outline facing out are dropped.
        private RoomFootprint(IEnumerable<Vector2Int> tiles, bool curvedWalls, IEnumerable<DoorSocket> fixedSockets)
        {
            CurvedWalls = curvedWalls;
            _hasFixedSockets = fixedSockets != null;
            _lookup = new HashSet<Vector2Int>(tiles);
            // Sorted so everything iterating the tiles (sockets, spawn spots) is the same for
            // the same seed.
            _tiles = new List<Vector2Int>(_lookup);
            _tiles.Sort((a, b) => a.y != b.y ? a.y.CompareTo(b.y) : a.x.CompareTo(b.x));

            Bounds = ComputeBounds(_tiles);
            Anchor = ComputeAnchor();
            if (fixedSockets == null) FindSockets();
            else AddFixedSockets(fixedSockets);
        }

        public static RoomFootprint Rectangle(RectInt rect)
        {
            var tiles = new List<Vector2Int>(rect.width * rect.height);
            foreach (Vector2Int tile in rect.allPositionsWithin) tiles.Add(tile);
            return new RoomFootprint(tiles);
        }

        public static RoomFootprint Rectangle(RectInt rect, IEnumerable<DoorSocket> sockets)
        {
            var tiles = new List<Vector2Int>(rect.width * rect.height);
            foreach (Vector2Int tile in rect.allPositionsWithin) tiles.Add(tile);
            return new RoomFootprint(tiles, false, sockets);
        }

        // The same room shifted by `offset` tiles. Doorways follow: found afresh for a generated
        // shape, shifted as given for a hand-built one.
        public RoomFootprint Translated(Vector2Int offset)
        {
            var tiles = new List<Vector2Int>(_tiles.Count);
            foreach (Vector2Int tile in _tiles) tiles.Add(tile + offset);
            if (!_hasFixedSockets) return new RoomFootprint(tiles, CurvedWalls);

            var sockets = new List<DoorSocket>(_sockets.Count);
            foreach (DoorSocket socket in _sockets) sockets.Add(new DoorSocket(socket.Inside + offset, socket.Outward));
            return new RoomFootprint(tiles, CurvedWalls, sockets);
        }

        public IReadOnlyList<Vector2Int> Tiles   => _tiles;
        public IReadOnlyList<DoorSocket> Sockets => _sockets;
        public int     Count  => _tiles.Count;
        // Walls follow RoomOutline's smoothed outline instead of the tile edges.
        public bool    CurvedWalls { get; }
        public RectInt Bounds { get; }

        // Middle of the bounds, in tile units — for which way another room lies.
        public Vector2 Center => Bounds.center;

        // A point on the floor as near the middle as possible — where the player, a
        // centrepiece or the exit go. Differs from Center when the middle is cut away (a ring).
        public Vector2 Anchor { get; }

        public bool Contains(Vector2Int tile) => _lookup.Contains(tile);

        // True when every tile within `margin` steps (including diagonally) is floor too.
        public bool IsInterior(Vector2Int tile, int margin)
        {
            for (int dx = -margin; dx <= margin; dx++)
                for (int dy = -margin; dy <= margin; dy++)
                    if (!_lookup.Contains(tile + new Vector2Int(dx, dy))) return false;
            return true;
        }

        private static RectInt ComputeBounds(List<Vector2Int> tiles)
        {
            if (tiles.Count == 0) return new RectInt();

            Vector2Int min = tiles[0], max = tiles[0];
            foreach (Vector2Int tile in tiles)
            {
                min = Vector2Int.Min(min, tile);
                max = Vector2Int.Max(max, tile);
            }
            return new RectInt(min, max - min + Vector2Int.one);
        }

        private Vector2 ComputeAnchor()
        {
            Vector2 centre = Center;
            if (_lookup.Contains(new Vector2Int(Mathf.FloorToInt(centre.x), Mathf.FloorToInt(centre.y)))) return centre;

            Vector2 best = centre;
            float bestDistance = float.MaxValue;
            foreach (Vector2Int tile in _tiles)
            {
                Vector2 middle = tile + new Vector2(0.5f, 0.5f);
                float distance = (middle - centre).sqrMagnitude;
                if (distance >= bestDistance) continue;
                bestDistance = distance;
                best = middle;
            }
            return best;
        }

        // A socket needs a straight wall on both sides of it (no doorway in a corner) and must
        // open onto the space around the room, not into an enclosed courtyard, with room in
        // front for a corridor to leave.
        private void FindSockets()
        {
            HashSet<Vector2Int> outside = OutsideTiles();

            foreach (Vector2Int tile in _tiles)
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int front = tile + side;
                    if (!outside.Contains(front) || !outside.Contains(front + side)) continue;

                    var along = new Vector2Int(side.y, side.x);
                    if (!IsStraightWall(tile + along, side) || !IsStraightWall(tile - along, side)) continue;

                    _sockets.Add(new DoorSocket(tile, side));
                }
        }

        private void AddFixedSockets(IEnumerable<DoorSocket> sockets)
        {
            foreach (DoorSocket socket in sockets)
                if (_lookup.Contains(socket.Inside) && !_lookup.Contains(socket.Outside))
                    _sockets.Add(socket);
        }

        private bool IsStraightWall(Vector2Int neighbour, Vector2Int side) =>
            _lookup.Contains(neighbour) && !_lookup.Contains(neighbour + side);

        // Non-floor tiles reachable from beyond the bounds without crossing the floor.
        private HashSet<Vector2Int> OutsideTiles()
        {
            var area = new RectInt(Bounds.xMin - 2, Bounds.yMin - 2, Bounds.width + 4, Bounds.height + 4);
            var reached = new HashSet<Vector2Int>();
            var open = new Stack<Vector2Int>();
            open.Push(area.min);
            reached.Add(area.min);

            while (open.Count > 0)
            {
                Vector2Int tile = open.Pop();
                foreach (Vector2Int side in Sides)
                {
                    Vector2Int next = tile + side;
                    if (!area.Contains(next) || _lookup.Contains(next) || !reached.Add(next)) continue;
                    open.Push(next);
                }
            }
            return reached;
        }
    }
}
