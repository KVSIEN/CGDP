using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Moves rooms so that the two ends of a direct connection touch: one side is shifted until
    // a doorway of each opens onto the same tile, leaving a single tile between them. Rooms
    // already fitted together form a group that moves as one, so every earlier fit stays valid
    // and a chain or a ring of rooms can be fitted door to door. Two rooms of the same group
    // are only linked when their doorways already face each other. A pair that can't be fitted
    // (too far apart, or in the way of another room) keeps its ordinary hallway and a warning
    // says so.
    public class DirectRoomAligner
    {
        // Empty tiles kept between the two rooms a direct door joins (the doorway tile).
        private const int Clearance = 1;
        // Empty tiles kept from every other room: a hallway and the wall on each side of it.
        private const int CorridorClearance = 3;

        private readonly int _maxShift;

        // maxShift: furthest a group may move, in tiles (Manhattan).
        public DirectRoomAligner(int maxShift) => _maxShift = maxShift;

        public Dictionary<MapConnection, DirectLink> Align(MapGraph graph, LevelLayout layout)
        {
            var links  = new Dictionary<MapConnection, DirectLink>();
            var groups = new Dictionary<int, HashSet<int>>();   // room id → the rooms fitted with it (shared set)

            foreach (MapConnection connection in graph.Connections)
            {
                if (!connection.Direct) continue;
                if (!layout.Rooms.TryGetValue(connection.A, out LevelRoom a) ||
                    !layout.Rooms.TryGetValue(connection.B, out LevelRoom b)) continue;

                HashSet<int> groupA = GroupOf(groups, a), groupB = GroupOf(groups, b);
                if (groupA == groupB)
                {
                    if (TryFit(layout, a, b, groupB, maxShift: 0, out Fit inPlace)) links[connection] = inPlace.Link(fixedIsA: true);
                    else Warn(layout, a, b, "they are already fitted to rooms that hold them apart");
                    continue;
                }

                // The smaller group moves, so the least of the level shifts.
                bool moveB = groupB.Count <= groupA.Count;
                if (!TryMove(layout, a, b, moveB ? groupB : groupA, moveB, links, out Fit fit))
                {
                    moveB = !moveB;
                    if (!TryMove(layout, a, b, moveB ? groupB : groupA, moveB, links, out fit))
                    {
                        Warn(layout, a, b, "no doorways line up without crowding another room");
                        continue;
                    }
                }

                links[connection] = fit.Link(fixedIsA: moveB);
                groupA.UnionWith(groupB);
                foreach (int id in groupB) groups[id] = groupA;
            }
            return links;
        }

        private static HashSet<int> GroupOf(Dictionary<int, HashSet<int>> groups, LevelRoom room)
        {
            if (!groups.TryGetValue(room.Node.Id, out HashSet<int> group))
                groups[room.Node.Id] = group = new HashSet<int> { room.Node.Id };
            return group;
        }

        // Shifts the mover's whole group (and the links inside it) so the pair's doorways meet.
        private bool TryMove(LevelLayout layout, LevelRoom a, LevelRoom b, HashSet<int> moving, bool moveB,
                             Dictionary<MapConnection, DirectLink> links, out Fit fit)
        {
            LevelRoom fixedRoom = moveB ? a : b, mover = moveB ? b : a;
            if (!TryFit(layout, fixedRoom, mover, moving, _maxShift, out fit)) return false;
            if (fit.Offset == Vector2Int.zero) return true;

            var rooms = new List<LevelRoom>(moving.Count);
            foreach (int id in moving) rooms.Add(layout.Rooms[id]);
            layout.MoveRooms(rooms, fit.Offset);
            var shifted = new List<MapConnection>();
            foreach (MapConnection linked in links.Keys)
                if (moving.Contains(linked.A)) shifted.Add(linked);
            foreach (MapConnection linked in shifted) links[linked] = links[linked].Translated(fit.Offset);
            return true;
        }

        // The shortest shift of `moving` that puts one doorway of `mover` face to face with one
        // of `fixedRoom`. Offsets are applied to `mover`'s current sockets.
        private static bool TryFit(LevelLayout layout, LevelRoom fixedRoom, LevelRoom mover, HashSet<int> moving, int maxShift, out Fit best)
        {
            var fits = new List<(int shift, int order, Fit fit)>();
            foreach (DoorSocket f in fixedRoom.Footprint.Sockets)
                foreach (DoorSocket m in mover.Footprint.Sockets)
                {
                    // Rooms put face to face share a straight stretch of wall, never a slanted step.
                    if (m.Outward != -f.Outward || f.Slanted || m.Slanted) continue;

                    Vector2Int offset = f.Outside - m.Outside;
                    int shift = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                    if (shift > maxShift) continue;

                    // The mover must end up on the side the doorway faces.
                    Vector2 toMover = mover.Center + (Vector2)offset - fixedRoom.Center;
                    if (Vector2.Dot(toMover, f.Outward) <= 0f) continue;
                    fits.Add((shift, fits.Count, new Fit(offset, f.Outside, f.Inside, m.Inside + offset)));
                }

            // Equal shifts keep the sockets' order, so a seed lays out the same.
            fits.Sort((x, y) => x.shift != y.shift ? x.shift.CompareTo(y.shift) : x.order.CompareTo(y.order));
            foreach ((int _, int _, Fit fit) in fits)
                if (IsClear(layout, moving, fixedRoom, fit.Offset))
                {
                    best = fit;
                    return true;
                }
            best = default;
            return false;
        }

        // The shifted rooms keep Clearance from the room they are joined to and a corridor's width
        // from every other room outside them, so the hallways still to be routed can pass.
        private static bool IsClear(LevelLayout layout, HashSet<int> moving, LevelRoom fixedRoom, Vector2Int offset)
        {
            foreach (int id in moving)
            {
                RoomFootprint footprint = layout.Rooms[id].Footprint;
                foreach (Vector2Int tile in footprint.Tiles)
                {
                    if (!IsEdge(footprint, tile)) continue;
                    for (int dx = -CorridorClearance; dx <= CorridorClearance; dx++)
                        for (int dy = -CorridorClearance; dy <= CorridorClearance; dy++)
                        {
                            LevelRoom other = layout.RoomAt(tile + offset + new Vector2Int(dx, dy));
                            if (other == null || moving.Contains(other.Node.Id)) continue;
                            int reach = other.Node.Id == fixedRoom.Node.Id ? Clearance : CorridorClearance;
                            if (Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy)) <= reach) return false;
                        }
                }
            }
            return true;
        }

        // A tile with a neighbour, corners included, outside the room: only these can come near another.
        private static bool IsEdge(RoomFootprint footprint, Vector2Int tile)
        {
            for (int dx = -1; dx <= 1; dx++)
                for (int dy = -1; dy <= 1; dy++)
                    if (!footprint.Contains(tile + new Vector2Int(dx, dy))) return true;
            return false;
        }

        private static void Warn(LevelLayout layout, LevelRoom a, LevelRoom b, string reason) =>
            layout.Warnings.Add($"Direct door {a.Node.Type} #{a.Node.Id} – {b.Node.Type} #{b.Node.Id} built as a hallway: {reason}.");

        // Where a pair's doorways meet once the mover has shifted by Offset: the shared tile, and
        // the floor tile of each room it opens from.
        private readonly struct Fit
        {
            public Fit(Vector2Int offset, Vector2Int tile, Vector2Int insideFixed, Vector2Int insideMover)
            {
                Offset      = offset;
                Tile        = tile;
                InsideFixed = insideFixed;
                InsideMover = insideMover;
            }

            public Vector2Int Offset      { get; }
            public Vector2Int Tile        { get; }
            public Vector2Int InsideFixed { get; }
            public Vector2Int InsideMover { get; }

            // The link in the connection's A, B order.
            public DirectLink Link(bool fixedIsA) => fixedIsA
                ? new DirectLink(Tile, InsideFixed, InsideMover)
                : new DirectLink(Tile, InsideMover, InsideFixed);
        }
    }
}
