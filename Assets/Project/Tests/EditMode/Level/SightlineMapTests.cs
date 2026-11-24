using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // How far a person can see along each row and column of a room: the open stretch through
    // every tile, cut by pillars, props and inner walls.
    public class SightlineMapTests
    {
        private const int Width = 10, Height = 6, WallX = 5;

        private static RoomStructure Hall() => Room(Width, Height, new Vector2Int(Width - 1, 3));

        // A wall between columns WallX - 1 and WallX in every row but the bottom one (so the room stays whole).
        private static RoomStructure HallWithWall(float wallHeight)
        {
            RoomStructure room = Hall();
            var edges = new List<PartitionEdge>();
            for (int y = 1; y < Height; y++)
                edges.Add(new PartitionEdge(new Vector2Int(WallX - 1, y), new Vector2Int(WallX, y), wallHeight, dividesZones: false));
            Assert.That(room.TryAddPartition(edges), Is.True);
            return room;
        }

        [Test]
        public void AnOpenRoomIsSeenRightAcross()
        {
            var sight = new SightlineMap(Hall());

            Assert.That(sight.Runs(new Vector2Int(0, 0)), Is.EqualTo((Width, Height)));
            Assert.That(sight.Runs(new Vector2Int(6, 4)), Is.EqualTo((Width, Height)));
            Assert.That(sight.LongestRun(new Vector2Int(3, 3)), Is.EqualTo(Width));
        }

        [Test]
        public void ATakenTileCutsTheRowAndColumnItStandsIn()
        {
            RoomStructure room = Hall();
            Assert.That(room.TryClaim(new[] { new Vector2Int(4, 2) }, RoomTileTags.Prop), Is.True);
            var sight = new SightlineMap(room);

            Assert.That(sight.Runs(new Vector2Int(4, 2)), Is.EqualTo((0, 0)), "a blocked tile sees nothing");
            Assert.That(sight.Runs(new Vector2Int(1, 2)), Is.EqualTo((4, Height)), "west of it along the row");
            Assert.That(sight.Runs(new Vector2Int(7, 2)), Is.EqualTo((5, Height)), "east of it along the row");
            Assert.That(sight.Runs(new Vector2Int(4, 0)), Is.EqualTo((Width, 2)), "below it along the column");
            Assert.That(sight.Runs(new Vector2Int(4, 4)), Is.EqualTo((Width, 3)), "above it along the column");
        }

        [Test]
        public void RefreshingAfterAClaimGivesWhatABuildFromScratchWould()
        {
            RoomStructure room = Hall();
            var sight = new SightlineMap(room);
            Assert.That(room.TryClaim(new[] { new Vector2Int(4, 2), new Vector2Int(5, 2) }, RoomTileTags.Prop), Is.True);
            sight.Refresh(new RectInt(4, 2, 2, 1));

            var fresh = new SightlineMap(room);
            foreach (Vector2Int tile in room.Footprint.Tiles)
                Assert.That(sight.Runs(tile), Is.EqualTo(fresh.Runs(tile)), $"tile {tile}");
        }

        [Test]
        public void ATallWallCutsARow()
        {
            var sight = new SightlineMap(HallWithWall(0f));

            Assert.That(sight.Runs(new Vector2Int(2, 3)).alongX, Is.EqualTo(WallX), "cut in two");
            Assert.That(sight.Runs(new Vector2Int(7, 3)).alongX, Is.EqualTo(Width - WallX));
            Assert.That(sight.Runs(new Vector2Int(2, 0)).alongX, Is.EqualTo(Width), "the row with no wall");
            Assert.That(sight.Runs(new Vector2Int(2, 3)).alongY, Is.EqualTo(Height), "columns are not cut");
        }

        [Test]
        public void ALowWallIsSeenOver()
        {
            var sight = new SightlineMap(HallWithWall(0.8f));

            Assert.That(sight.Runs(new Vector2Int(2, 3)).alongX, Is.EqualTo(Width), "a seat row or a counter doesn't hide what is behind it");
        }

        [Test]
        public void AWallAtEyeHeightHidesWhatIsBehindIt()
        {
            var sight = new SightlineMap(HallWithWall(SightBlocking.EyeHeight));

            Assert.That(sight.Runs(new Vector2Int(2, 3)).alongX, Is.EqualTo(WallX));
        }

        [Test]
        public void ARunAndWhatArrivalSightSeesAgree()
        {
            // The same room read two ways: along its rows, and by a line from the way in.
            foreach (float wallHeight in new[] { 0.8f, 3f })
            {
                RoomStructure room = HallWithWall(wallHeight);
                room.SetArrival(new Vector2Int(Width - 1, 3));
                var arrival = new ArrivalSight(room);
                var sight = new SightlineMap(room);

                var behindWall = new Vector2Int(WallX - 2, 3);
                bool seen = arrival.Sees(new RectInt(behindWall, Vector2Int.one));
                Assert.That(sight.Runs(behindWall).alongX == Width, Is.EqualTo(seen), $"wall {wallHeight} m");
            }
        }
    }
}
