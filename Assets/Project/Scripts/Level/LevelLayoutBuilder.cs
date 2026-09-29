using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Turns a MapGraph into a LevelLayout:
    //   1. Each node gets a grid cell from its editor position (the generator already lays
    //      nodes out on a column/lane grid; hand-moved nodes snap to the nearest free cell).
    //   2. Each cell holds one square room, with a gap around it for corridors.
    //   3. Each connection becomes a corridor from a doorway on the facing side of one room
    //      to the other, routed around every room.
    // Corridors keep a wall's width apart so two connections never merge into one — the
    // level has exactly the routes the graph has. When that's impossible (crossing
    // shortcuts) the corridors cross and a warning says so.
    public class LevelLayoutBuilder
    {
        // Doorways on one side of a room sit this many tiles apart, so neighbouring
        // corridors still have a wall between them.
        private const int DoorSpacing = 2;

        private readonly LevelBuildSettings _settings;

        public LevelLayoutBuilder(LevelBuildSettings settings) => _settings = settings;

        public LevelLayout Build(MapGraph graph, Vector2 nodeSpacing)
        {
            var layout = new LevelLayout(_settings.TileSize);
            if (graph == null || graph.Nodes.Count == 0)
            {
                layout.Warnings.Add("The map graph is empty.");
                return layout;
            }

            PlaceRooms(graph, nodeSpacing, layout);
            BuildCorridors(graph, layout);
            return layout;
        }

        // --- Rooms ---------------------------------------------------------------------

        private void PlaceRooms(MapGraph graph, Vector2 nodeSpacing, LevelLayout layout)
        {
            var taken = new HashSet<Vector2Int>();
            Vector2 spacing = new(Mathf.Max(1f, nodeSpacing.x), Mathf.Max(1f, nodeSpacing.y));
            int cell = _settings.CellTiles;
            int inset = _settings.GapTiles / 2;

            foreach (MapNode node in graph.Nodes)
            {
                // The editor's y grows downward; the level's tile y is world +z.
                var preferred = new Vector2Int(
                    Mathf.RoundToInt(node.Position.x / spacing.x),
                    -Mathf.RoundToInt(node.Position.y / spacing.y));
                Vector2Int coords = NearestFreeCell(preferred, taken);
                taken.Add(coords);

                var tiles = new RectInt(coords.x * cell + inset, coords.y * cell + inset, _settings.RoomTiles, _settings.RoomTiles);
                layout.AddRoom(new LevelRoom(node, tiles));
            }
        }

        private static Vector2Int NearestFreeCell(Vector2Int preferred, HashSet<Vector2Int> taken)
        {
            if (!taken.Contains(preferred)) return preferred;

            for (int radius = 1; ; radius++)
                for (int dx = -radius; dx <= radius; dx++)
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) != radius) continue;
                        var candidate = preferred + new Vector2Int(dx, dy);
                        if (!taken.Contains(candidate)) return candidate;
                    }
        }

        // --- Corridors -------------------------------------------------------------------

        private void BuildCorridors(MapGraph graph, LevelLayout layout)
        {
            var analysis = new MapGraphAnalysis(graph);
            var blocked  = new HashSet<Vector2Int>();   // rooms plus a one-tile ring around each
            var reserved = new HashSet<Vector2Int>();   // corridors plus a one-tile ring around each
            var usedDoors = new Dictionary<int, HashSet<Vector2Int>>();

            foreach (LevelRoom room in layout.Rooms.Values)
            {
                usedDoors[room.Node.Id] = new HashSet<Vector2Int>();
                RectInt ring = Expand(room.Tiles, 1);
                foreach (Vector2Int tile in ring.allPositionsWithin) blocked.Add(tile);
            }

            var router = new CorridorRouter(Expand(Bounds(layout), _settings.GapTiles));

            // Short connections first: they have the fewest ways around each other.
            var connections = new List<MapConnection>(graph.Connections);
            connections.Sort((x, y) => Distance(layout, x).CompareTo(Distance(layout, y)));

            foreach (MapConnection connection in connections)
            {
                if (!layout.Rooms.TryGetValue(connection.A, out LevelRoom a) ||
                    !layout.Rooms.TryGetValue(connection.B, out LevelRoom b)) continue;

                List<Door> doorsA = DoorCandidates(a, b, usedDoors[a.Node.Id]);
                List<Door> doorsB = DoorCandidates(b, a, usedDoors[b.Node.Id]);
                if (doorsA.Count == 0 || doorsB.Count == 0)
                {
                    layout.Warnings.Add($"No free doorway for the connection {Describe(a)} – {Describe(b)}.");
                    continue;
                }

                if (!TryRoute(router, doorsA, doorsB, t => !blocked.Contains(t) && !reserved.Contains(t), out Route route))
                {
                    if (!TryRoute(router, doorsA, doorsB, t => !blocked.Contains(t), out route))
                    {
                        layout.Warnings.Add($"No corridor route between {Describe(a)} and {Describe(b)}.");
                        continue;
                    }
                    layout.Warnings.Add($"The corridor {Describe(a)} – {Describe(b)} crosses another corridor.");
                }

                usedDoors[a.Node.Id].Add(route.A.Outside);
                usedDoors[b.Node.Id].Add(route.B.Outside);
                Reserve(route.Path, reserved);
                layout.AddCorridor(route.Path);

                bool gateAtA = analysis.Progress(a.Node.Id) <= analysis.Progress(b.Node.Id);
                layout.Doorways.Add(new LevelDoorway(a, connection, route.A.Inside, route.A.Outside, gateAtA));
                layout.Doorways.Add(new LevelDoorway(b, connection, route.B.Inside, route.B.Outside, !gateAtA));
            }
        }

        private readonly struct Door
        {
            public Door(Vector2Int inside, Vector2Int outside) { Inside = inside; Outside = outside; }
            public Vector2Int Inside  { get; }
            public Vector2Int Outside { get; }
        }

        private readonly struct Route
        {
            public Route(Door a, Door b, List<Vector2Int> path) { A = a; B = b; Path = path; }
            public Door A { get; }
            public Door B { get; }
            public List<Vector2Int> Path { get; }
        }

        // Tries the doorway pairs from the facing sides first and keeps the shortest route;
        // only when none of those connect are the rooms' other sides tried. Picking a
        // doorway blindly can send a corridor the long way round the whole map, walling in
        // rooms that later corridors then have to cross into.
        private static bool TryRoute(CorridorRouter router, List<Door> doorsA, List<Door> doorsB,
            System.Func<Vector2Int, bool> isPassable, out Route best)
        {
            best = default;
            for (int limit = 2; limit <= 4; limit += 2)
            {
                bool found = false;
                for (int i = 0; i < Mathf.Min(limit, doorsA.Count); i++)
                    for (int j = 0; j < Mathf.Min(limit, doorsB.Count); j++)
                    {
                        if (limit > 2 && i < 2 && j < 2) continue;   // already tried

                        List<Vector2Int> path = router.FindPath(doorsA[i].Outside, doorsB[j].Outside, isPassable);
                        if (path == null || (found && path.Count >= best.Path.Count)) continue;

                        best  = new Route(doorsA[i], doorsB[j], path);
                        found = true;
                    }
                if (found) return true;
            }
            return false;
        }

        // One free doorway per side of `room`, the side facing `other` first.
        private static List<Door> DoorCandidates(LevelRoom room, LevelRoom other, HashSet<Vector2Int> used)
        {
            Vector2 toOther = other.Center - room.Center;
            Vector2Int primary = Mathf.Abs(toOther.x) >= Mathf.Abs(toOther.y)
                ? new Vector2Int(toOther.x >= 0f ? 1 : -1, 0)
                : new Vector2Int(0, toOther.y >= 0f ? 1 : -1);
            Vector2Int secondary = primary.x != 0
                ? new Vector2Int(0, toOther.y >= 0f ? 1 : -1)
                : new Vector2Int(toOther.x >= 0f ? 1 : -1, 0);

            var doors = new List<Door>(4);
            foreach (Vector2Int side in new[] { primary, secondary, -secondary, -primary })
                if (TryPickDoorOnSide(room.Tiles, side, toOther, used, out Door door))
                    doors.Add(door);
            return doors;
        }

        // Slots go centre first, then outward — toward the other room before away from it —
        // keeping clear of the corners.
        private static bool TryPickDoorOnSide(RectInt tiles, Vector2Int side, Vector2 toOther, HashSet<Vector2Int> used, out Door door)
        {
            bool horizontal = side.y != 0;
            int length = horizontal ? tiles.width : tiles.height;
            int centre = length / 2;
            int toward = (horizontal ? toOther.x : toOther.y) >= 0f ? 1 : -1;

            for (int step = 0; step < length; step++)
            {
                int offset = (step + 1) / 2 * DoorSpacing * (step % 2 == 1 ? toward : -toward);
                int along = centre + offset;
                if (along < 1 || along > length - 2) continue;

                Vector2Int inside = horizontal
                    ? new Vector2Int(tiles.xMin + along, side.y > 0 ? tiles.yMax - 1 : tiles.yMin)
                    : new Vector2Int(side.x > 0 ? tiles.xMax - 1 : tiles.xMin, tiles.yMin + along);
                Vector2Int outside = inside + side;

                if (IsNearUsed(outside, used)) continue;
                door = new Door(inside, outside);
                return true;
            }

            door = default;
            return false;
        }

        private static bool IsNearUsed(Vector2Int tile, HashSet<Vector2Int> used)
        {
            foreach (Vector2Int door in used)
                if (Mathf.Abs(door.x - tile.x) + Mathf.Abs(door.y - tile.y) < DoorSpacing) return true;
            return false;
        }

        private static void Reserve(List<Vector2Int> path, HashSet<Vector2Int> reserved)
        {
            foreach (Vector2Int tile in path)
            {
                reserved.Add(tile);
                reserved.Add(tile + Vector2Int.up);
                reserved.Add(tile + Vector2Int.down);
                reserved.Add(tile + Vector2Int.left);
                reserved.Add(tile + Vector2Int.right);
            }
        }

        private static float Distance(LevelLayout layout, MapConnection connection) =>
            layout.Rooms.TryGetValue(connection.A, out LevelRoom a) && layout.Rooms.TryGetValue(connection.B, out LevelRoom b)
                ? Vector2.Distance(a.Center, b.Center)
                : float.MaxValue;

        private static RectInt Bounds(LevelLayout layout)
        {
            Vector2Int min = new(int.MaxValue, int.MaxValue), max = new(int.MinValue, int.MinValue);
            foreach (LevelRoom room in layout.Rooms.Values)
            {
                min = Vector2Int.Min(min, room.Tiles.min);
                max = Vector2Int.Max(max, room.Tiles.max);
            }
            return new RectInt(min, max - min);
        }

        private static RectInt Expand(RectInt rect, int by) =>
            new(rect.xMin - by, rect.yMin - by, rect.width + by * 2, rect.height + by * 2);

        private static string Describe(LevelRoom room) => $"{room.Node.Type} #{room.Node.Id}";
    }
}
