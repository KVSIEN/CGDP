using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;

namespace CGD.Tests
{
    // Doorways set into an angled room's slanted walls: a socket on each 45° step, the wall opened
    // √2 tiles wide along its diagonal, and the corridor leaving on the diagonal.
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
                Assert.That(Mathf.Abs(socket.Slant.x) == 1 && Mathf.Abs(socket.Slant.y) == 1, Is.True, "a diagonal way out");
                List<Vector2Int> throat = socket.Throat(2);
                Assert.That(throat.Count, Is.EqualTo(5));
                for (int i = 0; i < throat.Count; i++)
                {
                    Assert.That(Diamond(6).Contains(throat[i]), Is.False, "the throat is off the floor");
                    if (i > 0) Assert.That((throat[i] - throat[i - 1]).sqrMagnitude, Is.EqualTo(1));
                }
                Assert.That(throat[4] - throat[0], Is.EqualTo(socket.Slant * 2), "two steps out along the diagonal");
            }
            Assert.That(slanted, Is.GreaterThan(0));

            var curved = new RoomFootprint(new List<Vector2Int>(Diamond(6).Tiles), curvedWalls: true);
            foreach (DoorSocket socket in curved.Sockets)
                Assert.That(socket.Slanted, Is.False, "a curved room keeps its doorways on straight walls");
        }

        [Test]
        public void TheWallOpensAlongItsDiagonalAndClosesEverywhereElse()
        {
            RoomFootprint diamond = Diamond(6);
            foreach (DoorSocket socket in diamond.Sockets)
            {
                if (!socket.Slanted) continue;
                List<(Vector2 from, Vector2 to)> wall = RoomOutline.Build(diamond, (tile, side) => tile == socket.Inside && side == socket.Outward);

                // Every point is the end of two pieces except the opening's two ends, half a tile
                // either way along the diagonal from the step's middle.
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
                    Assert.That(Vector2.Distance(point, doorway), Is.EqualTo(Mathf.Sqrt(0.5f)).Within(1e-4f), $"{socket.Inside}: an open end away from the doorway");
                    Assert.That(Mathf.Abs(Vector2.Dot(point - doorway, socket.Slant)), Is.LessThan(1e-4f), "on the diagonal");
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

        [Test]
        public void APropReachingIntoASlantedWallIsPushedStraightOffIt()
        {
            // A wall from (0, 2) to (2, 0) with the floor on its left: where x + y >= 2.
            var walls = new List<(Vector2, Vector2)> { (new Vector2(0f, 2f), new Vector2(2f, 0f)) };
            Vector2 shift = PropWallClearance.OffSlants(walls, new Vector2(0.6f, 0.6f), new Vector2(1.2f, 1.2f));
            Assert.That(shift.x, Is.EqualTo(shift.y).Within(1e-5f), "straight off the 45° wall");
            Vector2 corner = new Vector2(0.6f, 0.6f) + shift;
            Assert.That((corner.x + corner.y - 2f) / Mathf.Sqrt(2f), Is.EqualTo(PropWallClearance.WallHalfThickness).Within(1e-4f), "just clear of it");

            Assert.That(PropWallClearance.OffSlants(walls, new Vector2(1.5f, 1.5f), new Vector2(2f, 2f)), Is.EqualTo(Vector2.zero), "clear already");
            Assert.That(PropWallClearance.OffSlants(walls, new Vector2(5f, 0.6f), new Vector2(5.5f, 1f)), Is.EqualTo(Vector2.zero), "beside the wall's run, not across from it");
        }
    }
}
