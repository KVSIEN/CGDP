using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // RoomStructure's floor bookkeeping: copies are independent, a furnished copy can be taken
    // back, inner walls sit only between neighbours, and a refused partition leaves the walls
    // that stood before it.
    public class RoomStructureTests
    {
        [Test]
        public void ACopyIsIndependentAndCanBeTakenBack()
        {
            RoomStructure room = Room(6, 6, new Vector2Int(0, 3));
            RoomStructure copy = room.Copy();
            var tile = new Vector2Int(4, 1);

            Assert.IsTrue(copy.TryClaim(new[] { tile }, RoomTileTags.Prop));
            Assert.IsFalse(room.Has(tile, RoomTileTags.Prop), "claiming on the copy touched the room");

            room.TakeFloorFrom(copy);
            Assert.IsTrue(room.Has(tile, RoomTileTags.Prop));
            Assert.IsFalse(room.IsFree(tile));
            Assert.IsFalse(room.TryClaim(new[] { tile }, RoomTileTags.Prop), "the taken tile is still free");
        }

        [Test]
        public void InnerWallsStandOnlyBetweenNeighbours()
        {
            RoomStructure room = Room(6, 6);
            var a = new Vector2Int(2, 2);
            var b = new Vector2Int(3, 2);
            Assert.IsTrue(room.TryAddPartition(new[] { new PartitionEdge(a, b, 3f) }));

            Assert.IsTrue(room.IsPartitioned(a, b));
            Assert.IsTrue(room.IsPartitioned(b, a));
            Assert.IsFalse(room.IsPartitioned(a, new Vector2Int(4, 2)), "not neighbours");
            Assert.IsFalse(room.IsPartitioned(a, a + Vector2Int.up));
        }

        [Test]
        public void ARefusedPartitionKeepsTheWallsAlreadyThere()
        {
            RoomStructure room = Room(3, 3);
            var kept = new PartitionEdge(new Vector2Int(0, 0), new Vector2Int(1, 0), 3f);
            Assert.IsTrue(room.TryAddPartition(new[] { kept }));

            // A full line across the room would cut it in two: refused, but the first wall stays.
            var line = new[]
            {
                kept,
                new PartitionEdge(new Vector2Int(0, 1), new Vector2Int(1, 1), 3f),
                new PartitionEdge(new Vector2Int(0, 2), new Vector2Int(1, 2), 3f),
            };
            Assert.IsFalse(room.TryAddPartition(line));
            Assert.IsTrue(room.IsPartitioned(kept.Tile, kept.Other));
            Assert.IsFalse(room.IsPartitioned(new Vector2Int(0, 1), new Vector2Int(1, 1)));
        }

        [Test]
        public void TilesOutsideTheRoomCarryNothing()
        {
            RoomStructure room = Room(4, 4);
            var outside = new Vector2Int(-1, 2);
            room.Tag(outside, RoomTileTags.Prop);

            Assert.IsFalse(room.Contains(outside));
            Assert.AreEqual(RoomTileTags.None, room.TagsAt(outside));
            Assert.AreEqual(0, room.ZoneOf(outside));
        }

        [Test]
        public void TheLargestZoneIsKnownOnceZonesAreAssigned()
        {
            RoomStructure room = Room(10, 4);
            var edges = new PartitionEdge[4];
            for (int y = 0; y < 4; y++) edges[y] = new PartitionEdge(new Vector2Int(2, y), new Vector2Int(3, y), 3f);
            // One gap so the room stays whole; zones count the line as closed.
            Assert.IsTrue(room.TryAddPartition(new[] { edges[0], edges[1], edges[2] }));
            room.AssignZones();

            Assert.AreEqual(2, room.ZoneCount);
            Assert.AreEqual(room.ZoneOf(new Vector2Int(8, 1)), room.LargestZone);
        }
    }
}
