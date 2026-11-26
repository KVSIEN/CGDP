using System.Collections.Generic;
using CGD.Algorithms;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // How raised and sunken floor is guarded and reached for what it is for: half walls stop
    // crossing and sight and shelter both sides, a perch is walled along its front only, a
    // bastion all round and reached by two flights on different sides.
    public class ElevationAccessTests
    {
        private const int Seeds = 40;

        private static List<Vector2Int> Block(int x, int y, int width, int height)
        {
            var tiles = new List<Vector2Int>();
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    tiles.Add(new Vector2Int(x + dx, y + dy));
            return tiles;
        }

        // A 10 by 8 room entered from the east, with two tiles out from the west wall, four along
        // it, a level up, reached by a flight climbing west from (2, 1).
        private static RoomStructure HallWithPlatform(LedgeGuards guards)
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            var platform = new ElevatedArea(ElevationKind.Platform, 1, Block(0, 0, 2, 4),
                                            new[] { new StairFlight(new Vector2Int(2, 1), Vector2Int.left) }, guards);
            Assert.That(room.TryAddElevation(platform), Is.True);
            return room;
        }

        [Test]
        public void GuardsAreSetSideBySide()
        {
            LedgeGuards guards = LedgeGuards.None.With(Vector2Int.right, LedgeGuard.HalfWall);
            Assert.That(guards.On(Vector2Int.right), Is.EqualTo(LedgeGuard.HalfWall));
            Assert.That(guards.On(Vector2Int.up), Is.EqualTo(LedgeGuard.None));
            Assert.That(guards.On(Vector2Int.left), Is.EqualTo(LedgeGuard.None));
            Assert.That(guards.Any, Is.True);
            Assert.That(LedgeGuards.None.Any, Is.False);
        }

        [Test]
        public void AHalfWallIsNotDroppedOffAndShieldsBothSides()
        {
            RoomStructure room = HallWithPlatform(LedgeGuards.All(LedgeGuard.HalfWall));
            Vector2Int top = new(1, 2), below = new(2, 2);

            Assert.That(room.GuardOn(top, below), Is.EqualTo(LedgeGuard.HalfWall));
            Assert.That(room.GuardOn(below, top), Is.EqualTo(LedgeGuard.HalfWall), "the same ledge, seen from below");
            Assert.That(room.CanDropDown(top, below), Is.False);
            Assert.That(room.GuardOn(new Vector2Int(1, 1), new Vector2Int(2, 1)), Is.EqualTo(LedgeGuard.None), "the stairs are no ledge");
            Assert.That(RoomCover.Shelters(room, top, below), Is.True, "crouched behind it up top");
            Assert.That(RoomCover.Shelters(room, below, top), Is.True, "against its foot below");
        }

        [Test]
        public void AHalfWallHidesWhatARailingShows()
        {
            RoomStructure walled = HallWithPlatform(LedgeGuards.All(LedgeGuard.HalfWall));
            RoomStructure railed = HallWithPlatform(LedgeGuards.All(LedgeGuard.Railing));
            Vector2Int top = new(1, 2), below = new(2, 2);

            Assert.That(new SightBlocking(walled).BlocksEdge(below, top), Is.True, "from below, 2 m of ledge and wall");
            Assert.That(new SightBlocking(railed).BlocksEdge(below, top), Is.False, "a railing is seen through");
            Assert.That(new SightBlocking(walled, SightBlocking.CrouchingEyeHeight).CanSee(new Vector2Int(0, 2), new Vector2Int(5, 2)), Is.False,
                        "crouched behind the wall up top");
            Assert.That(new SightBlocking(railed, SightBlocking.CrouchingEyeHeight).CanSee(new Vector2Int(0, 2), new Vector2Int(5, 2)), Is.True);
        }

        [Test]
        public void APerchIsWalledAlongItsFrontAndOpenAtItsEnds()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(9, 4));
            List<Vector2Int> gallery = Block(0, 1, 2, 5);   // against the west wall
            LedgeGuards guards = ElevationAccess.Guards(ElevationRole.Perch, room, gallery, LedgeGuard.Railing, LedgeGuard.HalfWall);

            Assert.That(ElevationAccess.Front(room, gallery), Is.EqualTo(Vector2Int.right));
            Assert.That(guards.On(Vector2Int.right), Is.EqualTo(LedgeGuard.HalfWall));
            Assert.That(guards.On(Vector2Int.up), Is.EqualTo(LedgeGuard.None), "an end is a level, mantled");
            Assert.That(guards.On(Vector2Int.down), Is.EqualTo(LedgeGuard.None));
        }

        [Test]
        public void EachRoleAsksForItsWaysUp()
        {
            Assert.That(ElevationAccess.Ways(ElevationRole.Perch, 3, ElevationAccess.SmallPerch - 1), Is.EqualTo(2), "never more than two");
            Assert.That(ElevationAccess.Ways(ElevationRole.Perch, 1, ElevationAccess.SmallPerch), Is.EqualTo(2), "a big one gets a flank");
            Assert.That(ElevationAccess.Ways(ElevationRole.Bastion, 1, 6), Is.EqualTo(2));
            Assert.That(ElevationAccess.Ways(ElevationRole.Bastion, 6, 60), Is.EqualTo(ElevationAccess.MostBastionWays), "still held at its stairs");
            Assert.That(ElevationAccess.Ways(ElevationRole.Pit, 1, 6), Is.EqualTo(2));
            Assert.That(ElevationAccess.Ways(ElevationRole.SplitLevel, 1, 40), Is.EqualTo(1));

            Assert.That(ElevationAccess.Required(ElevationRole.Bastion, LedgeGuards.All(LedgeGuard.HalfWall)), Is.EqualTo(2));
            Assert.That(ElevationAccess.Required(ElevationRole.Pit, LedgeGuards.None), Is.EqualTo(1), "an open rim is climbed out of");
            Assert.That(ElevationAccess.Required(ElevationRole.Pit, LedgeGuards.All(LedgeGuard.Railing)), Is.EqualTo(2));
            Assert.That(ElevationAccess.Fit(ElevationRole.Pit, LedgeGuards.All(LedgeGuard.Railing), 1), Is.EqualTo(LedgeGuards.None),
                        "a railed pit with room for one flight keeps its rim open instead");
            Assert.That(ElevationAccess.Fit(ElevationRole.Bastion, LedgeGuards.All(LedgeGuard.HalfWall), 1), Is.Null, "never a dead end");
        }

        [Test]
        public void ASecondFlightClimbsFromAnotherSide()
        {
            // Three flights along the front, most wanted first, then one at an end.
            var front1 = new StairFlight(new Vector2Int(2, 1), Vector2Int.left);
            var front2 = new StairFlight(new Vector2Int(2, 5), Vector2Int.left);
            var front3 = new StairFlight(new Vector2Int(2, 9), Vector2Int.left);
            var end    = new StairFlight(new Vector2Int(0, 10), Vector2Int.down);
            var preferred = new[] { front1, front2, front3, end };

            List<StairFlight> two = ElevationAccess.Pick(preferred, new List<HashSet<Vector2Int>>(), (_, _) => false, null, 2, 3);
            Assert.That(two, Is.EqualTo(new[] { front1, end }));

            List<StairFlight> three = ElevationAccess.Pick(preferred, new List<HashSet<Vector2Int>>(), (_, _) => false, null, 3, 3);
            Assert.That(three[2], Is.EqualTo(front2), "then the front flight farthest from both");
        }

        [Test]
        public void FlightsKeepTheirGapAndEveryPieceGetsOne()
        {
            var a = new StairFlight(new Vector2Int(2, 1), Vector2Int.left);
            var tooClose = new StairFlight(new Vector2Int(2, 2), Vector2Int.up);
            var onTheOtherPiece = new StairFlight(new Vector2Int(2, 8), Vector2Int.left);
            var pieces = new List<HashSet<Vector2Int>> { new() { new Vector2Int(1, 1) }, new() { new Vector2Int(1, 8) } };

            List<StairFlight> chosen = ElevationAccess.Pick(new[] { a, tooClose, onTheOtherPiece }, pieces,
                                                            (stairs, piece) => piece.Contains(stairs.Head), null, 2, 3);
            Assert.That(chosen, Is.EqualTo(new[] { a, onTheOtherPiece }));
        }

        // --- The rules ------------------------------------------------------------------

        private static ElevationStructureRule PlatformRule(ElevationRole role)
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)ElevationKind.Platform;
            serialized.FindProperty("_role").enumValueIndex = (int)role;
            serialized.FindProperty("_length").FindPropertyRelative("Min").intValue = 5;
            serialized.FindProperty("_length").FindPropertyRelative("Max").intValue = 7;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        private static RaisedSectionStructureRule SectionRule(ElevationRole role)
        {
            var rule = ScriptableObject.CreateInstance<RaisedSectionStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_role").enumValueIndex = (int)role;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        private static IEnumerable<RoomStructure> Rooms()
        {
            yield return Room(12, 10, new Vector2Int(11, 5));
            yield return Room(16, 12, new Vector2Int(15, 6), new Vector2Int(7, 0));
            yield return Room(14, 14, new Vector2Int(7, 13));
        }

        // Every guarded ledge of the room's areas, as (higher tile, lower tile).
        private static IEnumerable<(Vector2Int high, Vector2Int low)> Ledges(RoomStructure room, ElevatedArea area)
        {
            foreach (Vector2Int tile in area.Tiles)
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (room.IsLedge(tile, tile + side) && !room.IsPartitioned(tile, tile + side))
                        yield return room.LevelOf(tile) > room.LevelOf(tile + side) ? (tile, tile + side) : (tile + side, tile);
        }

        [Test]
        public void ABastionIsOnlyReachedByItsStairs([Values] bool section)
        {
            RoomStructureRule rule = section ? SectionRule(ElevationRole.Bastion) : PlatformRule(ElevationRole.Bastion);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
                foreach (RoomStructure room in Rooms())
                {
                    room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    string at = $"seed {seed}, {room.Footprint.Bounds.size}";
                    Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"{at}: every tile reachable both ways");

                    foreach (ElevatedArea area in room.Elevations)
                    {
                        built++;
                        Assert.That(area.Stairs.Count, Is.GreaterThanOrEqualTo(2), $"{at}: two ways on");
                        var sides = new HashSet<Vector2Int>();
                        foreach (StairFlight stairs in area.Stairs)
                        {
                            sides.Add(stairs.Up);
                        }
                        Assert.That(sides.Count > 1 || Spread(area.Stairs) >= 4, Is.True, $"{at}: the second flight is a flank, not a neighbour");
                        foreach (var (high, low) in Ledges(room, area))
                        {
                            Assert.That(room.GuardOn(high, low), Is.EqualTo(LedgeGuard.HalfWall), $"{at}: walled at {high}");
                            Assert.That(room.CanDropDown(high, low), Is.False, $"{at}: not dropped off at {high}");
                        }
                    }
                }
            Assert.That(built, Is.GreaterThan(Seeds), "most rooms got one");
        }

        [Test]
        public void APerchIsWalledOnOneSideOnly()
        {
            ElevationStructureRule rule = PlatformRule(ElevationRole.Perch);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
                foreach (RoomStructure room in Rooms())
                {
                    room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    foreach (ElevatedArea area in room.Elevations)
                    {
                        built++;
                        int walled = 0;
                        foreach (Vector2Int side in GridNeighbours.Four)
                            if (area.Guards.On(side) == LedgeGuard.HalfWall) walled++;
                        Assert.That(walled, Is.EqualTo(1), $"seed {seed}: its front");
                        Assert.That(area.Stairs.Count, Is.InRange(1, 2), $"seed {seed}");
                    }
                }
            Assert.That(built, Is.GreaterThan(Seeds));
        }

        // The farthest apart two flights stand.
        private static int Spread(IReadOnlyList<StairFlight> stairs)
        {
            int widest = 0;
            foreach (StairFlight a in stairs)
                foreach (StairFlight b in stairs)
                    widest = Mathf.Max(widest, Mathf.Max(Mathf.Abs(a.Tile.x - b.Tile.x), Mathf.Abs(a.Tile.y - b.Tile.y)));
            return widest;
        }
    }
}
