using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The line of sight between two tiles: what it crosses, and that between open tiles it
    // reads the same from either end.
    public class SightBlockingTests
    {
        [Test]
        public void ATakenTileIsSeenButSeesNothing()
        {
            RoomStructure room = Room(6, 5, new Vector2Int(5, 2));
            Assert.That(room.TryClaim(new[] { new Vector2Int(3, 2) }, RoomTileTags.Prop), Is.True);
            var blocking = new SightBlocking(room);

            Assert.That(blocking.CanSee(new Vector2Int(0, 2), new Vector2Int(3, 2)), Is.True, "the face of a prop is seen");
            Assert.That(blocking.CanSee(new Vector2Int(3, 2), new Vector2Int(0, 2)), Is.False, "nobody stands inside it");
            Assert.That(blocking.CanSee(new Vector2Int(0, 2), new Vector2Int(5, 2)), Is.False, "and it hides what is behind it");
        }

        [Test]
        public void ATileOutsideTheRoomIsNotSeen()
        {
            var blocking = new SightBlocking(Room(4, 4, new Vector2Int(3, 1)));

            Assert.That(blocking.CanSee(new Vector2Int(0, 0), new Vector2Int(9, 9)), Is.False);
            Assert.That(blocking.CanSee(new Vector2Int(-1, 0), new Vector2Int(2, 0)), Is.False);
        }

        [Test]
        public void ATileSeesItself() => Assert.That(new SightBlocking(Room(4, 4, new Vector2Int(3, 1))).CanSee(new Vector2Int(1, 1), new Vector2Int(1, 1)), Is.True);

        [Test]
        public void ALowWallIsSeenOverAndATallOneIsNot()
        {
            var behind = new Vector2Int(2, 3);
            var across = new Vector2Int(7, 3);

            Assert.That(new SightBlocking(WalledRoom(10, 6, 5, 0.8f, new Vector2Int(9, 3))).CanSee(behind, across), Is.True);
            Assert.That(new SightBlocking(WalledRoom(10, 6, 5, 3f, new Vector2Int(9, 3))).CanSee(behind, across), Is.False);
            Assert.That(new SightBlocking(WalledRoom(10, 6, 5, 3f, new Vector2Int(9, 3))).CanSee(new Vector2Int(2, 0), new Vector2Int(7, 0)), Is.True, "through the gap");
        }

        [Test]
        public void BetweenOpenTilesAnEyeSeesAsMuchFromEitherEnd()
        {
            for (int seed = 1; seed <= 20; seed++)
            {
                RoomStructure room = WalledRoom(10, 7, 5, 3f, new Vector2Int(9, 3));
                var random = new RandomStream(new Seed((ulong)seed));
                for (int i = 0; i < 8; i++)
                    room.TryClaim(new[] { new Vector2Int(random.Range(0, 10), random.Range(0, 7)) }, RoomTileTags.Prop);
                var blocking = new SightBlocking(room);

                foreach (Vector2Int a in room.Footprint.Tiles)
                    foreach (Vector2Int b in room.Footprint.Tiles)
                    {
                        if (blocking.BlocksTile(a) || blocking.BlocksTile(b)) continue;
                        Assert.That(blocking.CanSee(a, b), Is.EqualTo(blocking.CanSee(b, a)), $"seed {seed}: {a} and {b}");
                    }
            }
        }
    }
}
