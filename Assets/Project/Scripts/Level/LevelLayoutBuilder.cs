using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Map;

namespace CGD.Level
{
    // Turns a MapGraph into a LevelLayout:
    //   1. RoomPlacer gives each node a room: cells, function, size and floor plan.
    //   2. Each connection becomes a corridor from a door socket facing one room to one
    //      facing the other, routed around every room.
    //   3. With the doorways fixed, each room's structure (walkways, pillars, dividers) is
    //      planned by RoomStructurePlanner.
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

        // `seed` varies the room shapes; the same seed and graph give the same layout.
        public LevelLayout Build(MapGraph graph, Vector2 nodeSpacing, Seed seed)
        {
            var layout = new LevelLayout(_settings.TileSize);
            if (graph == null || graph.Nodes.Count == 0)
            {
                layout.Warnings.Add("The map graph is empty.");
                return layout;
            }

            new RoomPlacer(_settings).Place(graph, nodeSpacing, seed.Derive("rooms"), layout);
            BuildCorridors(graph, layout);
            PlanStructures(seed.Derive("structure"), layout);
            return layout;
        }

        private static void PlanStructures(Seed seed, LevelLayout layout)
        {
            var planner = new RoomStructurePlanner();
            foreach (LevelRoom room in layout.Rooms.Values)
                room.AttachStructure(planner.Plan(room, layout.Doorways, seed.Derive(room.Node.Id).Stream()));
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
                foreach (Vector2Int tile in room.Footprint.Tiles)
                    for (int dx = -1; dx <= 1; dx++)
                        for (int dy = -1; dy <= 1; dy++)
                            blocked.Add(tile + new Vector2Int(dx, dy));
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
                if (TryPickSocket(room, side, toOther, used, out Door door))
                    doors.Add(door);
            return doors;
        }

        // Of the free sockets facing `side`: the outermost first (the end of a T's arm, not
        // the notch beside it), then the one nearest the middle of that side, leaning toward
        // the other room.
        private static bool TryPickSocket(LevelRoom room, Vector2Int side, Vector2 toOther, HashSet<Vector2Int> used, out Door door)
        {
            var across = new Vector2(side.y, side.x);
            float toward = Mathf.Sign(Vector2.Dot(toOther, across));
            float bestScore = float.MinValue;
            door = default;

            foreach (DoorSocket socket in room.Footprint.Sockets)
            {
                if (socket.Outward != side || IsNearUsed(socket.Outside, used)) continue;

                Vector2 offset = socket.Inside + new Vector2(0.5f, 0.5f) - room.Center;
                float sideways = Vector2.Dot(offset, across);
                float score = Vector2.Dot(offset, side) * 100f - Mathf.Abs(sideways)
                            + (Mathf.Sign(sideways) == toward ? 0.1f : 0f);
                if (score <= bestScore) continue;

                bestScore = score;
                door = new Door(socket.Inside, socket.Outside);
            }
            return bestScore > float.MinValue;
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
                min = Vector2Int.Min(min, room.Footprint.Bounds.min);
                max = Vector2Int.Max(max, room.Footprint.Bounds.max);
            }
            return new RectInt(min, max - min);
        }

        private static RectInt Expand(RectInt rect, int by) =>
            new(rect.xMin - by, rect.yMin - by, rect.width + by * 2, rect.height + by * 2);

        private static string Describe(LevelRoom room) => $"{room.Node.Type} #{room.Node.Id}";
    }
}
