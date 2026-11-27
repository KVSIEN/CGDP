using System.Collections.Generic;
using UnityEngine;
using CGD.Algorithms;
using CGD.Map;

namespace CGD.Level
{
    // Moves rooms so that the two ends of a direct connection touch: one side is shifted until
    // a doorway of each opens onto the same tile, leaving a single tile between them. Rooms
    // already fitted together form a group that moves as one, so every earlier fit stays valid
    // and a chain or a ring of rooms can be fitted door to door. Two rooms of the same group
    // are only linked when their doorways already face each other. Where the shapes can't sit
    // that close, the pair is pulled together as far as they allow and joined by a straight
    // run of tiles: every pair that can touch is fitted first, then every pair one tile further
    // apart, and so on. A pair no such run can join keeps an ordinary hallway and a warning
    // says so.
    public class DirectRoomAligner
    {
        // Empty tiles kept between the two rooms a direct door joins (the doorway tile).
        private const int Clearance = 1;
        // Empty tiles kept from every other room: a hallway and the wall on each side of it.
        private const int CorridorClearance = 3;

        private readonly int _maxShift;
        // Most tiles a pair is held apart beyond the single doorway tile.
        private readonly int _maxGap;

        // maxShift: furthest a group may move, in tiles (Manhattan). Two rooms further apart
        // than that may close the distance between them, plus a third of it sideways.
        public DirectRoomAligner(int maxShift)
        {
            _maxShift = maxShift;
            _maxGap   = maxShift / 2;
        }

        public Dictionary<MapConnection, DirectLink> Align(MapGraph graph, LevelLayout layout)
        {
            var links  = new Dictionary<MapConnection, DirectLink>();
            var groups = new Dictionary<int, HashSet<int>>();   // room id → the rooms fitted with it (shared set)

            var waiting = new List<MapConnection>();
            foreach (MapConnection connection in graph.Connections)
                if (connection.Direct && layout.Rooms.ContainsKey(connection.A) && layout.Rooms.ContainsKey(connection.B))
                    waiting.Add(connection);

            for (int gap = 0; gap <= _maxGap && waiting.Count > 0; gap++)
                for (int i = 0; i < waiting.Count; i++)
                    if (TryJoin(layout, waiting[i], gap, groups, links))
                        waiting.RemoveAt(i--);

            foreach (MapConnection connection in waiting)
                Warn(layout, layout.Rooms[connection.A], layout.Rooms[connection.B]);
            return links;
        }

        // Links the connection's rooms `gap` tiles further apart than touching, moving one
        // side's group if they aren't already in the same one.
        private bool TryJoin(LevelLayout layout, MapConnection connection, int gap, Dictionary<int, HashSet<int>> groups,
                             Dictionary<MapConnection, DirectLink> links)
        {
            LevelRoom a = layout.Rooms[connection.A], b = layout.Rooms[connection.B];
            HashSet<int> groupA = GroupOf(groups, a), groupB = GroupOf(groups, b);
            if (groupA == groupB)
            {
                if (!TryFit(layout, a, b, groupB, maxShift: 0, gap, links, out Fit inPlace)) return false;
                links[connection] = inPlace.Link(fixedIsA: true);
                return true;
            }

            // The smaller group moves, so the least of the level shifts.
            bool moveB = groupB.Count <= groupA.Count;
            if (!TryMove(layout, a, b, moveB ? groupB : groupA, moveB, gap, links, out Fit fit))
            {
                moveB = !moveB;
                if (!TryMove(layout, a, b, moveB ? groupB : groupA, moveB, gap, links, out fit)) return false;
            }

            links[connection] = fit.Link(fixedIsA: moveB);
            groupA.UnionWith(groupB);
            foreach (int id in groupB) groups[id] = groupA;
            return true;
        }

        private static HashSet<int> GroupOf(Dictionary<int, HashSet<int>> groups, LevelRoom room)
        {
            if (!groups.TryGetValue(room.Node.Id, out HashSet<int> group))
                groups[room.Node.Id] = group = new HashSet<int> { room.Node.Id };
            return group;
        }

        // Shifts the mover's whole group (and the links inside it) so the pair's doorways meet.
        private bool TryMove(LevelLayout layout, LevelRoom a, LevelRoom b, HashSet<int> moving, bool moveB, int gap,
                             Dictionary<MapConnection, DirectLink> links, out Fit fit)
        {
            LevelRoom fixedRoom = moveB ? a : b, mover = moveB ? b : a;
            int reach = Mathf.Max(_maxShift, Apart(fixedRoom, mover) + _maxShift / 3);
            if (!TryFit(layout, fixedRoom, mover, moving, reach, gap, links, out fit)) return false;
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

        // Tiles between the two rooms' bounds, both axes together.
        private static int Apart(LevelRoom a, LevelRoom b)
        {
            RectInt first = a.Footprint.Bounds, second = b.Footprint.Bounds;
            return Mathf.Max(0, first.xMin - second.xMax, second.xMin - first.xMax)
                 + Mathf.Max(0, first.yMin - second.yMax, second.yMin - first.yMax);
        }

        // The shortest shift of `moving` that puts one doorway of `mover` face to face with one
        // of `fixedRoom`, `gap` tiles further apart than touching. Offsets are applied to
        // `mover`'s current sockets.
        private static bool TryFit(LevelLayout layout, LevelRoom fixedRoom, LevelRoom mover, HashSet<int> moving, int maxShift, int gap,
                                   Dictionary<MapConnection, DirectLink> links, out Fit best)
        {
            var fits = new List<(int shift, float aside, int order, DoorSocket f, DoorSocket m, Vector2Int offset)>();
            foreach (DoorSocket f in fixedRoom.Footprint.Sockets)
                foreach (DoorSocket m in mover.Footprint.Sockets)
                {
                    // Rooms put face to face share a straight stretch of wall, never a slanted step.
                    if (m.Outward != -f.Outward || f.Slanted || m.Slanted) continue;

                    Vector2Int offset = f.Outside + f.Outward * gap - m.Outside;
                    int shift = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                    if (shift > maxShift) continue;

                    // The mover must end up on the side the doorway faces.
                    Vector2 toMover = mover.Center + (Vector2)offset - fixedRoom.Center;
                    if (Vector2.Dot(toMover, f.Outward) <= 0f) continue;

                    // How far the doorway is from the middle of the fixed room's wall.
                    var along = new Vector2(f.Outward.y, f.Outward.x);
                    float aside = Mathf.Abs(Vector2.Dot(f.Inside + Vector2.one * 0.5f - fixedRoom.Center, along));
                    fits.Add((shift, aside, fits.Count, f, m, offset));
                }

            // The shortest shift, then the most central doorway; equal ones keep the sockets'
            // order, so a seed lays out the same.
            fits.Sort((x, y) => x.shift != y.shift ? x.shift.CompareTo(y.shift)
                              : x.aside != y.aside ? x.aside.CompareTo(y.aside)
                              : x.order.CompareTo(y.order));

            var movingLinks = new HashSet<Vector2Int>();
            var fixedLinks  = new HashSet<Vector2Int>();
            foreach (var (connection, link) in links)
                (moving.Contains(connection.A) ? movingLinks : fixedLinks).UnionWith(link.Tiles);

            // Many socket pairs share an offset, and whether the rooms fit there depends on it alone.
            var clearAt = new Dictionary<Vector2Int, bool>();
            foreach (var (_, _, _, f, m, offset) in fits)
            {
                var tiles = new Vector2Int[gap + 1];
                for (int i = 0; i <= gap; i++) tiles[i] = f.Outside + f.Outward * i;
                if (!IsRunClear(layout, moving, fixedRoom, mover, offset, tiles, movingLinks, fixedLinks)) continue;

                if (!clearAt.TryGetValue(offset, out bool clear))
                    clearAt[offset] = clear = IsClear(layout, moving, fixedRoom, offset, movingLinks, fixedLinks);
                if (!clear) continue;

                best = new Fit(offset, tiles, f.Inside, m.Inside + offset);
                return true;
            }
            best = default;
            return false;
        }

        // Whether the tiles between the two doorways keep a wall to every room (touching only
        // the room each end opens from) and stay off the other links' tiles and their sides.
        private static bool IsRunClear(LevelLayout layout, HashSet<int> moving, LevelRoom fixedRoom, LevelRoom mover, Vector2Int offset,
                                       Vector2Int[] tiles, HashSet<Vector2Int> movingLinks, HashSet<Vector2Int> fixedLinks)
        {
            for (int i = 0; i < tiles.Length; i++)
            {
                if (RoomAfter(layout, moving, offset, tiles[i]) != null || HasLink(tiles[i], offset, movingLinks, fixedLinks)) return false;
                foreach (Vector2Int step in GridNeighbours.Eight)
                {
                    Vector2Int near = tiles[i] + step;
                    int? room = RoomAfter(layout, moving, offset, near);
                    bool ownDoorway = (i == 0 && room == fixedRoom.Node.Id) || (i == tiles.Length - 1 && room == mover.Node.Id);
                    if (room != null && !ownDoorway) return false;
                    if (!GridNeighbours.IsDiagonal(step) && HasLink(near, offset, movingLinks, fixedLinks)) return false;
                }
            }
            return true;
        }

        // The id of the room on a tile once the moving rooms have shifted by `offset`.
        private static int? RoomAfter(LevelLayout layout, HashSet<int> moving, Vector2Int offset, Vector2Int tile)
        {
            LevelRoom shifted = layout.RoomAt(tile - offset);
            if (shifted != null && moving.Contains(shifted.Node.Id)) return shifted.Node.Id;
            LevelRoom standing = layout.RoomAt(tile);
            return standing != null && !moving.Contains(standing.Node.Id) ? standing.Node.Id : null;
        }

        private static bool HasLink(Vector2Int tile, Vector2Int offset, HashSet<Vector2Int> movingLinks, HashSet<Vector2Int> fixedLinks) =>
            fixedLinks.Contains(tile) || movingLinks.Contains(tile - offset);

        // The shifted rooms keep Clearance from the room they are joined to and a corridor's width
        // from every other room outside them, so the hallways still to be routed can pass. Neither
        // they nor the links moving with them may come up against a link or room left behind.
        private static bool IsClear(LevelLayout layout, HashSet<int> moving, LevelRoom fixedRoom, Vector2Int offset,
                                    HashSet<Vector2Int> movingLinks, HashSet<Vector2Int> fixedLinks)
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
                            Vector2Int near = tile + offset + new Vector2Int(dx, dy);
                            int distance = Mathf.Max(Mathf.Abs(dx), Mathf.Abs(dy));
                            if (distance <= Clearance && fixedLinks.Contains(near)) return false;

                            LevelRoom other = layout.RoomAt(near);
                            if (other == null || moving.Contains(other.Node.Id)) continue;
                            if (distance <= (other.Node.Id == fixedRoom.Node.Id ? Clearance : CorridorClearance)) return false;
                        }
                }
            }

            foreach (Vector2Int tile in movingLinks)
                foreach (Vector2Int step in GridNeighbours.Eight)
                {
                    Vector2Int near = tile + offset + step;
                    LevelRoom other = layout.RoomAt(near);
                    if (other != null && !moving.Contains(other.Node.Id)) return false;
                    if (!GridNeighbours.IsDiagonal(step) && fixedLinks.Contains(near)) return false;
                }
            return true;
        }

        // A tile with a neighbour, corners included, outside the room: only these can come near another.
        private static bool IsEdge(RoomFootprint footprint, Vector2Int tile)
        {
            foreach (Vector2Int step in GridNeighbours.Eight)
                if (!footprint.Contains(tile + step)) return true;
            return false;
        }

        private static void Warn(LevelLayout layout, LevelRoom a, LevelRoom b) =>
            layout.Warnings.Add($"Direct door {a.Node.Type} #{a.Node.Id} – {b.Node.Type} #{b.Node.Id} built as a hallway: no doorways line up without crowding another room.");

        // Where a pair's doorways meet once the mover has shifted by Offset: the tiles between
        // them, from the fixed room's doorway to the mover's, and the floor tile of each room.
        private readonly struct Fit
        {
            private readonly Vector2Int[] _tiles;
            private readonly Vector2Int _insideFixed;
            private readonly Vector2Int _insideMover;

            public Fit(Vector2Int offset, Vector2Int[] tiles, Vector2Int insideFixed, Vector2Int insideMover)
            {
                Offset       = offset;
                _tiles       = tiles;
                _insideFixed = insideFixed;
                _insideMover = insideMover;
            }

            public Vector2Int Offset { get; }

            // The link in the connection's A, B order.
            public DirectLink Link(bool fixedIsA)
            {
                if (fixedIsA) return new DirectLink(_tiles, _insideFixed, _insideMover);
                var reversed = (Vector2Int[])_tiles.Clone();
                System.Array.Reverse(reversed);
                return new DirectLink(reversed, _insideMover, _insideFixed);
            }
        }
    }
}
