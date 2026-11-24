using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // A room read by what can be seen from everywhere in it: how exposed it is, how much of it is
    // shut in, and its longest line of sight.
    public class RoomSightReadingTests
    {
        [Test]
        public void AnOpenHallIsSeenAtAGlance()
        {
            RoomSightReading reading = RoomSightReading.Of(Room(6, 5, new Vector2Int(5, 2)));

            Assert.That(reading.Exposure, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(reading.Enclosed, Is.Zero);
            Assert.That(reading.Sightline, Is.EqualTo(Mathf.Sqrt(5 * 5 + 4 * 4)).Within(1e-4f), "corner to corner, on the diagonal");
        }

        [Test]
        public void WallsAndPillarsMakeARoomLessExposed()
        {
            float open = RoomSightReading.Of(Room(10, 6, new Vector2Int(9, 3))).Exposure;

            RoomStructure walled = WalledRoom(10, 6, 5, 3f, new Vector2Int(9, 3));
            RoomSightReading wallReading = RoomSightReading.Of(walled);
            Assert.That(wallReading.Exposure, Is.LessThan(open).And.GreaterThan(0f));

            RoomStructure pillared = Room(10, 6, new Vector2Int(9, 3));
            Assert.That(pillared.TryClaim(new[] { new Vector2Int(3, 2), new Vector2Int(6, 4), new Vector2Int(5, 1) }, RoomTileTags.Prop), Is.True);
            Assert.That(RoomSightReading.Of(pillared).Exposure, Is.LessThan(open));
        }

        [Test]
        public void AShutInCornerReadsAsEnclosed()
        {
            // A 2 × 2 cell in the corner, walled on two sides, with a doorway through its bottom-right.
            RoomStructure room = Room(9, 9, new Vector2Int(8, 4));
            var walls = new List<PartitionEdge>
            {
                new(new Vector2Int(0, 1), new Vector2Int(0, 2), 3f, dividesZones: false),
                new(new Vector2Int(1, 1), new Vector2Int(1, 2), 3f, dividesZones: false),
                new(new Vector2Int(1, 1), new Vector2Int(2, 1), 3f, dividesZones: false),
            };
            Assert.That(room.TryAddPartition(walls), Is.True);

            Assert.That(RoomSightReading.Of(room).Enclosed, Is.GreaterThan(0f));
        }

        [Test]
        public void ARoomReadsTheSameFromAVisibilityKeptCurrent()
        {
            RoomStructure room = Room(10, 6, new Vector2Int(9, 3));
            var visibility = new RoomVisibility(room);
            var taken = new RectInt(4, 2, 2, 2);
            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in taken.allPositionsWithin) tiles.Add(tile);
            Assert.That(room.TryClaim(tiles, RoomTileTags.Prop), Is.True);
            visibility.Refresh(taken);

            RoomSightReading kept = RoomSightReading.Of(room, visibility);
            RoomSightReading fresh = RoomSightReading.Of(room);
            Assert.That(kept.Exposure, Is.EqualTo(fresh.Exposure).Within(1e-5f));
            Assert.That(kept.Enclosed, Is.EqualTo(fresh.Enclosed).Within(1e-5f));
            Assert.That(kept.Sightline, Is.EqualTo(fresh.Sightline).Within(1e-4f));
        }
    }
}
