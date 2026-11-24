using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Who sees whom in a room, in every direction: open floor, what hides what is behind it,
    // tall and low inner walls, a line through a corner, and keeping the answers current as
    // props come and go.
    public class RoomVisibilityTests
    {
        private static List<Vector2Int> TilesOf(RectInt rect)
        {
            var tiles = new List<Vector2Int>();
            foreach (Vector2Int tile in rect.allPositionsWithin) tiles.Add(tile);
            return tiles;
        }

        private static void Take(RoomStructure room, params Vector2Int[] tiles) =>
            Assert.That(room.TryClaim(tiles, RoomTileTags.Prop), Is.True);

        [Test]
        public void AnOpenRoomSeesEverythingFromEverywhere()
        {
            RoomStructure room = Room(6, 5, new Vector2Int(5, 2));
            var visibility = new RoomVisibility(room);

            foreach (Vector2Int a in room.Footprint.Tiles)
            {
                Assert.That(visibility.Area(a), Is.EqualTo(30), $"from {a}");
                foreach (Vector2Int b in room.Footprint.Tiles)
                    Assert.That(visibility.Sees(a, b), Is.True, $"{a} to {b}");
            }
            Assert.That(visibility.Reach(new Vector2Int(0, 0)), Is.EqualTo(Mathf.Sqrt(5 * 5 + 4 * 4)).Within(1e-4f), "to the far corner");
        }

        [Test]
        public void ATakenTileSeesNothingIsSeenByNoneAndHidesWhatIsBehindIt()
        {
            RoomStructure room = Room(6, 5, new Vector2Int(5, 2));
            Take(room, new Vector2Int(2, 2));
            var visibility = new RoomVisibility(room);

            Assert.That(visibility.Area(new Vector2Int(2, 2)), Is.Zero);
            Assert.That(visibility.Reach(new Vector2Int(2, 2)), Is.Zero);
            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(2, 2)), Is.False);
            Assert.That(visibility.Sees(new Vector2Int(2, 2), new Vector2Int(0, 0)), Is.False);
            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(4, 4)), Is.False, "straight through it, on the diagonal");
            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(4, 0)), Is.True);
            Assert.That(visibility.Area(new Vector2Int(0, 0)), Is.LessThan(29).And.GreaterThan(0));
        }

        [Test]
        public void APillarHidesWhatIsDirectlyBehindIt()
        {
            RoomStructure room = Room(9, 3, new Vector2Int(8, 1));
            Take(room, new Vector2Int(3, 1));
            var visibility = new RoomVisibility(room);

            Assert.That(visibility.Sees(new Vector2Int(0, 1), new Vector2Int(2, 1)), Is.True, "in front of it");
            Assert.That(visibility.Sees(new Vector2Int(0, 1), new Vector2Int(6, 1)), Is.False, "behind it");
            Assert.That(visibility.Sees(new Vector2Int(6, 1), new Vector2Int(0, 1)), Is.False, "and the other way");
            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(6, 0)), Is.True, "past it along the wall");
        }

        [Test]
        public void ATallWallHidesAndALowOneDoesNot()
        {
            var behind = new Vector2Int(2, 3);
            var across = new Vector2Int(7, 3);
            var below = new Vector2Int(2, 0);
            var belowAcross = new Vector2Int(7, 0);

            var tall = new RoomVisibility(WalledRoom(10, 6, 5, 0f, new Vector2Int(9, 3)));
            Assert.That(tall.Sees(behind, across), Is.False);
            Assert.That(tall.Sees(below, belowAcross), Is.True, "through the gap in the bottom row");

            var low = new RoomVisibility(WalledRoom(10, 6, 5, 0.8f, new Vector2Int(9, 3)));
            Assert.That(low.Sees(behind, across), Is.True);

            var eye = new RoomVisibility(WalledRoom(10, 6, 5, SightBlocking.EyeHeight, new Vector2Int(9, 3)));
            Assert.That(eye.Sees(behind, across), Is.False, "a wall as tall as the eye is not looked over");
        }

        [Test]
        public void ALineThroughACornerGrazesOneBlockedTileButNotTwo()
        {
            RoomStructure one = Room(5, 5, new Vector2Int(4, 2));
            Take(one, new Vector2Int(2, 1));
            Assert.That(new RoomVisibility(one).Sees(new Vector2Int(1, 1), new Vector2Int(2, 2)), Is.True, "round the open side");

            RoomStructure both = Room(5, 5, new Vector2Int(4, 2));
            Take(both, new Vector2Int(2, 1), new Vector2Int(1, 2));
            Assert.That(new RoomVisibility(both).Sees(new Vector2Int(1, 1), new Vector2Int(2, 2)), Is.False, "no squeezing between two");
        }

        [Test]
        public void NothingBeyondTheRangeIsSeen()
        {
            RoomStructure room = Room(8, 2, new Vector2Int(7, 0));
            var visibility = new RoomVisibility(room, range: 3);

            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(3, 0)), Is.True);
            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(3, 1)), Is.True);
            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(4, 0)), Is.False);
        }

        [Test]
        public void TilesOutsideTheRoomAreNotSeen()
        {
            var visibility = new RoomVisibility(Room(4, 4, new Vector2Int(3, 1)));

            Assert.That(visibility.Sees(new Vector2Int(0, 0), new Vector2Int(9, 9)), Is.False);
            Assert.That(visibility.Area(new Vector2Int(-3, -3)), Is.Zero);
            Assert.That(visibility.Reach(new Vector2Int(-3, -3)), Is.Zero);
        }

        [Test]
        public void ALineIsClearBothWaysOrNeither()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                RoomStructure room = WalledRoom(10, 7, 5, 3f, new Vector2Int(9, 3));
                var random = new RandomStream(new Seed((ulong)seed));
                for (int i = 0; i < 8; i++)
                    room.TryClaim(new[] { new Vector2Int(random.Range(0, 10), random.Range(0, 7)) }, RoomTileTags.Prop);
                var visibility = new RoomVisibility(room);

                foreach (Vector2Int a in room.Footprint.Tiles)
                    foreach (Vector2Int b in room.Footprint.Tiles)
                        Assert.That(visibility.LineIsClear(a, b), Is.EqualTo(visibility.LineIsClear(b, a)), $"seed {seed}: {a} and {b}");
            }
        }

        [Test]
        public void RefreshingAfterChangesGivesWhatABuildFromScratchWould()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                RoomStructure room = WalledRoom(12, 9, 6, 3f, new Vector2Int(11, 4));
                var random = new RandomStream(new Seed((ulong)seed));
                var visibility = new RoomVisibility(room);
                var taken = new List<RectInt>();

                for (int step = 0; step < 12; step++)
                {
                    RectInt changed;
                    if (taken.Count > 0 && random.Chance(0.35f))
                    {
                        int index = random.Range(0, taken.Count);
                        changed = taken[index];
                        taken.RemoveAt(index);
                        room.Release(TilesOf(changed), RoomTileTags.Prop);
                    }
                    else
                    {
                        changed = new RectInt(random.Range(0, 11), random.Range(0, 8), random.Range(1, 3), random.Range(1, 3));
                        if (!room.TryClaim(TilesOf(changed), RoomTileTags.Prop)) continue;
                        taken.Add(changed);
                    }

                    visibility.Refresh(changed);
                    AssertSame(room, new RoomVisibility(room), visibility, $"seed {seed}, step {step}");
                }
            }
        }

        private static void AssertSame(RoomStructure room, RoomVisibility expected, RoomVisibility actual, string message)
        {
            foreach (Vector2Int a in room.Footprint.Tiles)
            {
                Assert.That(actual.Area(a), Is.EqualTo(expected.Area(a)), $"{message}: area of {a}");
                Assert.That(actual.Reach(a), Is.EqualTo(expected.Reach(a)).Within(1e-4f), $"{message}: reach of {a}");
                foreach (Vector2Int b in room.Footprint.Tiles)
                    Assert.That(actual.Sees(a, b), Is.EqualTo(expected.Sees(a, b)), $"{message}: {a} to {b}");
            }
        }
    }
}
