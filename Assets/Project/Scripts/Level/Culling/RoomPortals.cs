using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // A generated level as rooms and the doorways between them, for working out which rooms
    // the camera could see into (cells and portals). From the camera's room, a room is in view
    // when a doorway leading to it is in view, and so on through that room's doorways, a few
    // rooms deep. Conservative: it never narrows the view through a doorway, so it can count a
    // room that is in fact hidden, but never misses one seen through a chain of open doorways.
    public sealed class RoomPortals
    {
        // A doorway box this thick across the opening, so a camera right at it still finds it.
        private const float PortalDepth = 0.5f;

        private readonly struct Portal
        {
            public Portal(int to, Bounds box)
            {
                To  = to;
                Box = box;
            }

            public int    To  { get; }
            public Bounds Box { get; }
        }

        private readonly LevelLayout _layout;
        private readonly Dictionary<int, List<Portal>> _portals = new();
        private readonly Dictionary<Vector2Int, MapConnection> _corridorTiles = new();
        private readonly Queue<(int Room, int Depth)> _open = new();

        // `toWorld` turns a box in the level's own space into world space (null = the same).
        public RoomPortals(LevelLayout layout, Func<Bounds, Bounds> toWorld = null)
        {
            _layout = layout;
            foreach (LevelDoorway doorway in layout.Doorways)
            {
                int from = doorway.Room.Node.Id;
                int to = doorway.Connection.A == from ? doorway.Connection.B : doorway.Connection.A;
                Bounds box = Box(doorway);
                if (!_portals.TryGetValue(from, out List<Portal> portals)) _portals[from] = portals = new List<Portal>();
                portals.Add(new Portal(to, toWorld != null ? toWorld(box) : box));
                foreach (Vector2Int tile in layout.CorridorOf(doorway.Connection)) _corridorTiles[tile] = doorway.Connection;
            }
        }

        // The rooms the camera could see into from `local` (the camera's position in the level's
        // own space), given what is in its view; false when it is in no room or corridor, so
        // the caller should show everything. `fallback` is a room to start from instead (the
        // player's) when the camera is in neither.
        public bool TryVisible(Vector3 local, Predicate<Bounds> inView, int maxDepth, HashSet<int> visible, int fallback = -1)
        {
            visible.Clear();
            _open.Clear();
            LevelRoom room = _layout.RoomAtLocal(local);
            var tile = new Vector2Int(Mathf.FloorToInt(local.x / _layout.TileSize), Mathf.FloorToInt(local.z / _layout.TileSize));
            if (room != null) Start(room.Node.Id);
            else if (_corridorTiles.TryGetValue(tile, out MapConnection corridor))
            {
                Start(corridor.A);
                Start(corridor.B);
            }
            else if (fallback >= 0) Start(fallback);
            else return false;

            while (_open.Count > 0)
            {
                (int current, int depth) = _open.Dequeue();
                if (depth >= maxDepth || !_portals.TryGetValue(current, out List<Portal> portals)) continue;
                foreach (Portal portal in portals)
                    if (!visible.Contains(portal.To) && inView(portal.Box))
                    {
                        visible.Add(portal.To);
                        _open.Enqueue((portal.To, depth + 1));
                    }
            }
            return true;

            void Start(int id)
            {
                if (visible.Add(id)) _open.Enqueue((id, 0));
            }
        }

        // The opening between a doorway's room tile and the tile outside it, floor to the
        // room's wall height.
        private Bounds Box(LevelDoorway doorway)
        {
            float tile = _layout.TileSize;
            float height = Mathf.Max(doorway.Room.WallHeight, 1f);
            Vector3 middle = _layout.DoorwayMiddle(doorway);
            // The opening as a thin slab across the doorway, turned with it (45° in a slanted wall),
            // and the box round that.
            Vector2 facing = doorway.Facing, across = new(-facing.y, facing.x);
            Vector2 extent = new Vector2(Mathf.Abs(across.x), Mathf.Abs(across.y)) * (tile * doorway.Width)
                           + new Vector2(Mathf.Abs(facing.x), Mathf.Abs(facing.y)) * PortalDepth;
            return new Bounds(middle + Vector3.up * (height * 0.5f), new Vector3(extent.x, height, extent.y));
        }
    }
}
