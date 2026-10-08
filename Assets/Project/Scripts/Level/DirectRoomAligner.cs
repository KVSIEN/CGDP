using System.Collections.Generic;
using UnityEngine;
using CGD.Map;

namespace CGD.Level
{
    // Moves rooms so that the two ends of a direct connection touch: one room is shifted
    // until a doorway of each opens onto the same tile, leaving a single tile between them.
    // A room is shifted at most once and never after another room has been fitted to it,
    // so earlier fits stay valid. A pair that can't be fitted (too far apart, or in the way
    // of another room) keeps its ordinary hallway and a warning says so.
    public class DirectRoomAligner
    {
        // Rooms need at least this many empty tiles between them, doorway tile excepted.
        private const int Clearance = 1;

        private readonly int _maxShift;

        // maxShift: furthest a room may move, in tiles (Manhattan).
        public DirectRoomAligner(int maxShift) => _maxShift = maxShift;

        public Dictionary<MapConnection, DirectLink> Align(MapGraph graph, LevelLayout layout)
        {
            var links   = new Dictionary<MapConnection, DirectLink>();
            var settled = new HashSet<int>();

            foreach (MapConnection connection in graph.Connections)
            {
                if (!connection.Direct) continue;
                if (!layout.Rooms.TryGetValue(connection.A, out LevelRoom a) ||
                    !layout.Rooms.TryGetValue(connection.B, out LevelRoom b)) continue;

                // Prefer moving B; A moves only when B is already fitted to something else.
                bool moveB = !settled.Contains(b.Node.Id);
                if (!moveB && settled.Contains(a.Node.Id))
                {
                    Warn(layout, a, b, "both rooms are already fitted to others");
                    continue;
                }

                LevelRoom fixedRoom = moveB ? a : b, mover = moveB ? b : a;
                if (!TryFit(layout, fixedRoom, mover, out Vector2Int offset, out DoorSocket fixedSocket, out DoorSocket moverSocket))
                {
                    Warn(layout, a, b, "no doorways line up without crowding another room");
                    continue;
                }

                layout.MoveRoom(mover, offset);
                settled.Add(fixedRoom.Node.Id);
                settled.Add(mover.Node.Id);

                Vector2Int moverInside = moverSocket.Inside + offset;
                links[connection] = moveB
                    ? new DirectLink(fixedSocket.Outside, fixedSocket.Inside, moverInside)
                    : new DirectLink(fixedSocket.Outside, moverInside, fixedSocket.Inside);
            }
            return links;
        }

        // The shortest shift that puts one doorway of `mover` face to face with one of `fixedRoom`.
        private bool TryFit(LevelLayout layout, LevelRoom fixedRoom, LevelRoom mover, out Vector2Int bestOffset,
                            out DoorSocket bestFixed, out DoorSocket bestMover)
        {
            bestOffset = default;
            bestFixed  = default;
            bestMover  = default;
            int bestShift = int.MaxValue;

            foreach (DoorSocket f in fixedRoom.Footprint.Sockets)
                foreach (DoorSocket m in mover.Footprint.Sockets)
                {
                    if (m.Outward != -f.Outward) continue;

                    Vector2Int offset = f.Outside - m.Outside;
                    int shift = Mathf.Abs(offset.x) + Mathf.Abs(offset.y);
                    if (shift >= bestShift || shift > _maxShift) continue;

                    // The mover must end up on the side the doorway faces.
                    Vector2 toMover = mover.Center + (Vector2)offset - fixedRoom.Center;
                    if (Vector2.Dot(toMover, f.Outward) <= 0f) continue;
                    if (!IsClear(layout, mover, offset)) continue;

                    bestShift  = shift;
                    bestOffset = offset;
                    bestFixed  = f;
                    bestMover  = m;
                }
            return bestShift != int.MaxValue;
        }

        // No tile of the shifted room may come within Clearance tiles of another room.
        private static bool IsClear(LevelLayout layout, LevelRoom mover, Vector2Int offset)
        {
            foreach (Vector2Int tile in mover.Footprint.Tiles)
                for (int dx = -Clearance; dx <= Clearance; dx++)
                    for (int dy = -Clearance; dy <= Clearance; dy++)
                    {
                        LevelRoom other = layout.RoomAt(tile + offset + new Vector2Int(dx, dy));
                        if (other != null && other != mover) return false;
                    }
            return true;
        }

        private static void Warn(LevelLayout layout, LevelRoom a, LevelRoom b, string reason) =>
            layout.Warnings.Add($"Direct door {a.Node.Type} #{a.Node.Id} – {b.Node.Type} #{b.Node.Id} built as a hallway: {reason}.");
    }
}
