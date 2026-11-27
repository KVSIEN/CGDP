using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;

namespace CGD.Tests
{
    // Doorways on an angled room's slanted walls: a socket on each 45° step, and the wall squared
    // off round the step the doorway takes, so the door stands on a tile edge like any other.
    public class SlantedDoorwayTests
    {
        private static RoomFootprint Diamond(int radius)
        {
            var tiles = new List<Vector2Int>();
            for (int x = -radius; x <= radius; x++)
                for (int y = -radius; y <= radius; y++)
                    if (Mathf.Abs(x) + Mathf.Abs(y) <= radius) tiles.Add(new Vector2Int(x + radius, y + radius));
            return new RoomFootprint(tiles, curvedWalls: true, sharpDiagonals: true);
        }

        [Test]
        public void AnAngledRoomTakesDoorwaysOnItsSlantedSteps()
        {
            int slanted = 0;
            foreach (DoorSocket socket in Diamond(6).Sockets)
            {
                if (!socket.Slanted) continue;
                slanted++;
                Assert.That(Diamond(6).Contains(socket.Outside) || Diamond(6).Contains(socket.Lead), Is.False, "the corridor's first two tiles are off the floor");
            }
            Assert.That(slanted, Is.GreaterThan(0));

            var curved = new RoomFootprint(new List<Vector2Int>(Diamond(6).Tiles), curvedWalls: true);
            foreach (DoorSocket socket in curved.Sockets)
                Assert.That(socket.Slanted, Is.False, "a curved room keeps its doorways on straight walls");
        }

        [Test]
        public void TheWallRunsRoundASlantedDoorwayAndClosesEverywhereElse()
        {
            RoomFootprint diamond = Diamond(6);
            foreach (DoorSocket socket in diamond.Sockets)
            {
                if (!socket.Slanted) continue;
                List<(Vector2 from, Vector2 to)> wall = RoomOutline.Build(diamond, (tile, side) => tile == socket.Inside && side == socket.Outward);

                // Every point is the end of two pieces except the doorway's two corners.
                var ends = new Dictionary<Vector2, int>();
                foreach (var (from, to) in wall)
                    foreach (Vector2 point in new[] { from, to })
                        ends[point] = ends.TryGetValue(point, out int n) ? n + 1 : 1;
                Vector2 doorway = socket.Inside + Vector2.one * 0.5f + (Vector2)socket.Outward * 0.5f;
                int open = 0;
                foreach (var (point, uses) in ends)
                {
                    if (uses % 2 == 0) continue;
                    open++;
                    Assert.That(Vector2.Distance(point, doorway), Is.EqualTo(0.5f).Within(1e-4f), $"{socket.Inside}: an open end away from the doorway");
                }
                Assert.That(open, Is.EqualTo(2));

                foreach (var (from, to) in wall)
                {
                    Vector2 run = to - from;
                    bool square = Mathf.Approximately(run.x, 0f) || Mathf.Approximately(run.y, 0f);
                    Assert.That(square || Mathf.Approximately(Mathf.Abs(run.x), Mathf.Abs(run.y)), Is.True, "walls square or 45°");
                }
            }
        }
    }
}
