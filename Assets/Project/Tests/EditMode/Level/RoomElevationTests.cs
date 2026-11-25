using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Floor a level up or down: reached by its stairs both ways, dropped off where its ledge is
    // open, never covering a doorway or a route, and never more than one level from its neighbour.
    public class RoomElevationTests
    {
        // A 10 by 8 room entered from the east: its walkway runs along row 4 to the centre.
        private static readonly Vector2Int Door = new(9, 4);

        private static RoomStructure Hall(bool levels = true)
        {
            RoomStructure room = Room(10, 8, Door);
            if (levels) room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            return room;
        }

        private static List<Vector2Int> Block(int x, int y, int width, int height)
        {
            var tiles = new List<Vector2Int>();
            for (int dy = 0; dy < height; dy++)
                for (int dx = 0; dx < width; dx++)
                    tiles.Add(new Vector2Int(x + dx, y + dy));
            return tiles;
        }

        // Two tiles out from the west wall, four along it, with a flight climbing west onto it from (2, 1).
        private static ElevatedArea WestPlatform(bool railed = false, params StairFlight[] stairs) =>
            new(ElevationKind.Platform, 1, Block(0, 0, 2, 4), stairs.Length > 0 ? stairs : new[] { new StairFlight(new Vector2Int(2, 1), Vector2Int.left) }, railed);

        [Test]
        public void AFlatRoomWalksAsItAlwaysDid()
        {
            RoomStructure room = Hall();

            Assert.That(room.HasElevation, Is.False);
            Assert.That(room.LevelOf(new Vector2Int(3, 3)), Is.Zero);
            Assert.That(room.CanStep(new Vector2Int(0, 0), new Vector2Int(1, 0)), Is.True);
            Assert.That(room.CanStep(new Vector2Int(0, 0), new Vector2Int(1, 1)), Is.False, "not neighbours");
            Assert.That(room.CanStep(new Vector2Int(9, 0), new Vector2Int(10, 0)), Is.False, "off the floor");
        }

        [Test]
        public void ARoomStaysFlatUntilItsLevelsAreAllowed()
        {
            RoomStructure room = Hall(levels: false);

            Assert.That(room.TryAddElevation(WestPlatform()), Is.False);
            Assert.That(room.LevelOf(new Vector2Int(0, 0)), Is.Zero);
            Assert.That(room.HasElevation, Is.False);
        }

        [Test]
        public void APlatformIsReachedByItsStairsAndNothingElse()
        {
            RoomStructure room = Hall();
            Assert.That(room.TryAddElevation(WestPlatform()), Is.True);

            Assert.That(room.LevelOf(new Vector2Int(1, 3)), Is.EqualTo(1));
            Assert.That(room.CanStep(new Vector2Int(3, 1), new Vector2Int(2, 1)), Is.True, "onto the foot of the stairs");
            Assert.That(room.CanStep(new Vector2Int(2, 1), new Vector2Int(1, 1)), Is.True, "up the stairs");
            Assert.That(room.CanStep(new Vector2Int(1, 1), new Vector2Int(2, 1)), Is.True, "and back down");
            Assert.That(room.CanStep(new Vector2Int(2, 2), new Vector2Int(1, 2)), Is.False, "up the ledge");
            Assert.That(room.CanStep(new Vector2Int(2, 2), new Vector2Int(2, 1)), Is.False, "onto the stairs from the side");
            Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True);

            Assert.That(room.Has(new Vector2Int(2, 1), RoomTileTags.Stairs), Is.True);
            Assert.That(room.Has(new Vector2Int(3, 1), RoomTileTags.Landing), Is.True, "the foot");
            Assert.That(room.Has(new Vector2Int(1, 1), RoomTileTags.Landing), Is.True, "the head");
            Assert.That(room.TryGetStairs(new Vector2Int(2, 1), out StairFlight stairs), Is.True);
            Assert.That(stairs.Up, Is.EqualTo(Vector2Int.left));
            Assert.That(room.AreaOf(new Vector2Int(0, 0)), Is.Not.Null);
            Assert.That(room.AreaOf(new Vector2Int(5, 5)), Is.Null);
        }

        [Test]
        public void FloorWithNoWayUpIsRefusedAndLeftFlat()
        {
            RoomStructure room = Hall();
            var noStairs = new ElevatedArea(ElevationKind.Platform, 1, Block(0, 0, 2, 4), System.Array.Empty<StairFlight>(), false);

            Assert.That(room.TryAddElevation(noStairs), Is.False);
            Assert.That(room.LevelOf(new Vector2Int(0, 0)), Is.Zero);
            Assert.That(room.HasElevation, Is.False);
            Assert.That(room.Has(new Vector2Int(2, 1), RoomTileTags.Stairs), Is.False);
        }

        [Test]
        public void StairsMustClimbOntoOrOffTheirArea()
        {
            Assert.That(Hall().TryAddElevation(WestPlatform(false, new StairFlight(new Vector2Int(2, 1), Vector2Int.right))), Is.False, "climbing away from it");
            Assert.That(Hall().TryAddElevation(WestPlatform(false, new StairFlight(new Vector2Int(5, 6), Vector2Int.left))), Is.False, "nowhere near it");
            Assert.That(Hall().TryAddElevation(WestPlatform(false, new StairFlight(new Vector2Int(2, 1), new Vector2Int(-1, -1)))), Is.False, "diagonal");
        }

        [Test]
        public void FloorInFrontOfADoorwayIsNeverRaised()
        {
            var atTheDoor = new ElevatedArea(ElevationKind.Platform, 1, Block(8, 6, 2, 2), new[] { new StairFlight(new Vector2Int(7, 6), Vector2Int.right) }, false);
            Assert.That(Hall().TryAddElevation(atTheDoor), Is.False);
        }

        [Test]
        public void ARouteMayBeRaisedWhenStairsCarryItOn()
        {
            // The walkway runs along row 4 from the doorway to the centre: raise a stretch of it.
            RoomStructure room = Hall();
            var acrossTheRoute = new ElevatedArea(ElevationKind.Platform, 1, Block(6, 4, 1, 2), new[] { new StairFlight(new Vector2Int(6, 3), Vector2Int.up) }, false);

            Assert.That(room.TryAddElevation(acrossTheRoute), Is.True);
            Assert.That(room.LevelOf(new Vector2Int(6, 4)), Is.EqualTo(1));
            Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True);
        }

        [Test]
        public void AreasAddedTogetherStandOrFallTogether()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            var west = new ElevatedArea(ElevationKind.Terrace, 1, Block(0, 0, 2, 4), new[] { new StairFlight(new Vector2Int(2, 1), Vector2Int.left) }, false);
            var stranded = new ElevatedArea(ElevationKind.Terrace, 1, Block(0, 7, 2, 3), System.Array.Empty<StairFlight>(), false);

            Assert.That(room.TryAddElevations(new[] { west, stranded }), Is.False, "the second has no way up");
            Assert.That(room.HasElevation, Is.False, "so neither stands");
            Assert.That(room.LevelOf(new Vector2Int(0, 0)), Is.Zero);
            Assert.That(room.Has(new Vector2Int(2, 1), RoomTileTags.Stairs), Is.False);

            Assert.That(room.TryAddElevations(new[] { west }), Is.True);
        }

        [Test]
        public void NoTileIsTwoLevelsFromItsNeighbour()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            var pit = new ElevatedArea(ElevationKind.SunkenFloor, -1, Block(3, 1, 2, 3), new[] { new StairFlight(new Vector2Int(3, 1), Vector2Int.down) }, false);
            Assert.That(room.TryAddElevation(pit), Is.True);

            var platformBeside = new ElevatedArea(ElevationKind.Platform, 1, Block(1, 1, 2, 3), new[] { new StairFlight(new Vector2Int(1, 4), Vector2Int.down) }, false);
            Assert.That(room.TryAddElevation(platformBeside), Is.False);
        }

        [Test]
        public void YouCanDropOffAnOpenLedgeButNotClimbIt()
        {
            RoomStructure open = Hall();
            Assert.That(open.TryAddElevation(WestPlatform()), Is.True);
            Assert.That(open.CanDropDown(new Vector2Int(1, 2), new Vector2Int(2, 2)), Is.True, "off the platform");
            Assert.That(open.CanDropDown(new Vector2Int(2, 2), new Vector2Int(1, 2)), Is.False, "up onto it");
            Assert.That(open.CanDropDown(new Vector2Int(1, 1), new Vector2Int(2, 1)), Is.False, "the stairs are walked, not dropped");
            Assert.That(open.IsLedge(new Vector2Int(1, 2), new Vector2Int(2, 2)), Is.True);
            Assert.That(open.IsLedge(new Vector2Int(1, 1), new Vector2Int(2, 1)), Is.False);

            RoomStructure railed = Hall();
            Assert.That(railed.TryAddElevation(WestPlatform(railed: true)), Is.True);
            Assert.That(railed.CanDropDown(new Vector2Int(1, 2), new Vector2Int(2, 2)), Is.False, "a railing stops it");
        }

        [Test]
        public void NothingIsBuiltOnStairsOrTheirLandings()
        {
            RoomStructure room = Hall();
            Assert.That(room.TryAddElevation(WestPlatform()), Is.True);

            Assert.That(room.TryClaim(new[] { new Vector2Int(2, 1) }, RoomTileTags.Prop), Is.False, "the stairs");
            Assert.That(room.TryClaim(new[] { new Vector2Int(3, 1) }, RoomTileTags.Prop), Is.False, "the foot");
            Assert.That(room.TryClaim(new[] { new Vector2Int(1, 1) }, RoomTileTags.Prop), Is.False, "the head");
            Assert.That(room.TryClaim(new[] { new Vector2Int(0, 3) }, RoomTileTags.Prop), Is.True, "the platform's own floor");
        }

        [Test]
        public void AWallAcrossTheOnlyFlightIsRefused()
        {
            RoomStructure room = Hall();
            Assert.That(room.TryAddElevation(WestPlatform()), Is.True);

            Assert.That(room.TryAddPartition(new[] { new PartitionEdge(new Vector2Int(2, 1), new Vector2Int(1, 1), 0f) }), Is.False);
        }

        [Test]
        public void ACopyKeepsTheLevelsAndStairs()
        {
            RoomStructure room = Hall();
            Assert.That(room.TryAddElevation(WestPlatform()), Is.True);

            RoomStructure copy = room.Copy();
            Assert.That(copy.LevelOf(new Vector2Int(0, 0)), Is.EqualTo(1));
            Assert.That(copy.TryGetStairs(new Vector2Int(2, 1), out _), Is.True);
            Assert.That(copy.Elevations.Count, Is.EqualTo(1));
            Assert.That(copy.HighestLevel, Is.EqualTo(RoomElevation.Highest));
            Assert.That(copy.CanStep(new Vector2Int(2, 2), new Vector2Int(1, 2)), Is.False);
        }

        [Test]
        public void TheWayAcrossARoomGoesByTheStairs()
        {
            RoomStructure room = Hall();
            Assert.That(room.TryAddElevation(WestPlatform()), Is.True);

            var steps = new FloorSteps(room, new Vector2Int(3, 3), RoomTileTags.Structure);
            Assert.That(steps.At(new Vector2Int(1, 3)), Is.EqualTo(6), "down to (3, 1), onto the stairs, up and back along the platform");
            List<Vector2Int> path = steps.PathTo(new Vector2Int(1, 3), room);
            Assert.That(path, Does.Contain(new Vector2Int(2, 1)));
        }

        // --- The rule -------------------------------------------------------------------

        private static ElevationStructureRule Rule(ElevationKind kind, bool railed = false, bool bridge = false, int minLength = 3)
        {
            var rule = ScriptableObject.CreateInstance<ElevationStructureRule>();
            var serialized = new SerializedObject(rule);
            serialized.FindProperty("_kind").enumValueIndex = (int)kind;
            serialized.FindProperty("_railed").boolValue = railed;
            serialized.FindProperty("_bridge").boolValue = bridge;
            serialized.FindProperty("_length").FindPropertyRelative("Min").intValue = minLength;
            serialized.FindProperty("_length").FindPropertyRelative("Max").intValue = Mathf.Max(minLength, 5);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        private static IEnumerable<RoomStructure> Rooms()
        {
            yield return Room(12, 10, new Vector2Int(11, 5));
            yield return Room(16, 12, new Vector2Int(15, 6), new Vector2Int(7, 0));
            yield return Room(9, 9, new Vector2Int(4, 8));
        }

        [Test]
        public void EveryPlatformAndSunkenFloorIsSound([Values(ElevationKind.Platform, ElevationKind.SunkenFloor)] ElevationKind kind)
        {
            ElevationStructureRule rule = Rule(kind);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
                foreach (RoomStructure room in Rooms())
                {
                    room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    built += room.Elevations.Count;
                    string at = $"seed {seed}, {room.Footprint.Bounds.size}";

                    Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"{at}: every tile reachable both ways");
                    foreach (Vector2Int entrance in room.Entrances)
                        Assert.That(room.LevelOf(entrance), Is.Zero, $"{at}: a doorway opens onto the doorways' level");

                    int taken = 0;
                    foreach (ElevatedArea area in room.Elevations)
                    {
                        taken += area.Tiles.Count;
                        Assert.That(area.Stairs.Count, Is.GreaterThan(0), $"{at}: a way up and down");
                        Assert.That(area.Level, Is.EqualTo(kind == ElevationKind.Platform ? 1 : -1), at);
                        foreach (StairFlight stairs in area.Stairs)
                        {
                            Assert.That(room.Has(stairs.Foot, RoomTileTags.Landing) && room.Has(stairs.Head, RoomTileTags.Landing), Is.True, $"{at}: landings kept clear");
                            Assert.That(room.LevelOf(stairs.Head), Is.EqualTo(room.LevelOf(stairs.Tile) + 1), $"{at}: a flight climbs one level");
                        }

                        if (kind == ElevationKind.Platform)
                            Assert.That(area.Tiles, Has.Some.Matches<Vector2Int>(tile => TouchesTheRoomsWall(room, tile)), $"{at}: a platform backs onto a wall");
                        else
                            foreach (Vector2Int tile in area.Tiles)
                                Assert.That(room.Footprint.IsInterior(tile, 2), Is.True, $"{at}: a sunken floor has floor all round it");
                    }
                    Assert.That(taken, Is.LessThanOrEqualTo(Mathf.FloorToInt(room.Footprint.Count * 0.25f)), $"{at}: within its share of the floor");
                }
            Assert.That(built, Is.GreaterThan(Seeds), "most rooms got one");
        }

        private static bool TouchesTheRoomsWall(RoomStructure room, Vector2Int tile)
        {
            foreach (Vector2Int side in GridSides.All)
                if (!room.Contains(tile + side)) return true;
            return false;
        }

        [Test]
        public void ARoomThatDoesntTakeTheLevelIsLeftAloneWithNoRandomDrawn()
        {
            ElevationStructureRule rule = Rule(ElevationKind.SunkenFloor);
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            room.AllowLevels(0, RoomElevation.Highest);   // raised floor only
            var used = new RandomStream(new Seed(5UL));
            var fresh = new RandomStream(new Seed(5UL));

            rule.Apply(room, used);

            Assert.That(room.HasElevation, Is.False);
            Assert.That(used.Value, Is.EqualTo(fresh.Value), "the room's other rules draw what they always did");
        }

        [Test]
        public void PropsStandOnOneLevelAndNeverOnStairs()
        {
            ElevationStructureRule rule = Rule(ElevationKind.Platform);
            for (int seed = 1; seed <= 20; seed++)
            {
                RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
                room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                rule.Apply(room, new RandomStream(new Seed((ulong)seed)));

                RoomPropPlanner planner = Planner(room, seed);
                RoomPropPlacement benches = Placement(6, 10);
                benches.Footprint = new Vector2Int(2, 1);
                planner.Add(benches);

                foreach (PlannedProp prop in planner.Planned)
                {
                    int level = room.LevelOf(prop.Tiles.min);
                    foreach (Vector2Int tile in prop.Tiles.allPositionsWithin)
                    {
                        Assert.That(room.LevelOf(tile), Is.EqualTo(level), $"seed {seed}: a prop straddles a ledge");
                        Assert.That(room.Has(tile, RoomTileTags.Stairs | RoomTileTags.Landing), Is.False, $"seed {seed}: a prop on the stairs");
                    }
                }
            }
        }

        // --- Slopes, mounds, craters and bridges -------------------------------------------

        [Test]
        public void SlopesSideBySideAreWalkedAcrossButNotEnteredFromTheSide()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            var bank = new ElevatedArea(ElevationKind.Mound, 1, Block(2, 2, 2, 1),
                new[] { new StairFlight(new Vector2Int(2, 1), Vector2Int.up, slope: true), new StairFlight(new Vector2Int(3, 1), Vector2Int.up, slope: true) }, false);

            Assert.That(room.TryAddElevation(bank), Is.True);
            Assert.That(room.CanStep(new Vector2Int(2, 1), new Vector2Int(3, 1)), Is.True, "across the bank");
            Assert.That(room.CanStep(new Vector2Int(1, 1), new Vector2Int(2, 1)), Is.False, "onto it from the side");
            Assert.That(room.TryGetStairs(new Vector2Int(2, 1), out StairFlight slope) && slope.Slope, Is.True);
        }

        [Test]
        public void ACornerPieceIsSolidAndGivenBackWhenItsAreaIsRefused()
        {
            RoomStructure room = Room(12, 10, new Vector2Int(11, 5));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            var corner = new SlopeCorner(new Vector2Int(1, 1), new Vector2Int(1, 1));
            var stranded = new ElevatedArea(ElevationKind.Mound, 1, Block(2, 2, 1, 1), System.Array.Empty<StairFlight>(), false, new[] { corner });

            Assert.That(room.TryAddElevation(stranded), Is.False, "no way up");
            Assert.That(room.Has(new Vector2Int(1, 1), RoomTileTags.Structure), Is.False, "the corner is given back");
            Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True);

            var reached = new ElevatedArea(ElevationKind.Mound, 1, Block(2, 2, 1, 1),
                new[] { new StairFlight(new Vector2Int(3, 2), Vector2Int.left, slope: true) }, false, new[] { corner });
            Assert.That(room.TryAddElevation(reached), Is.True);
            Assert.That(room.Has(new Vector2Int(1, 1), RoomTileTags.Structure), Is.True);
            Assert.That(room.TryClaim(new[] { new Vector2Int(1, 1) }, RoomTileTags.Prop), Is.False);
        }

        private static IEnumerable<RoomStructure> RoomyRooms()
        {
            yield return Room(16, 12, new Vector2Int(15, 6), new Vector2Int(7, 0));
            yield return Room(20, 16, new Vector2Int(19, 8));
        }

        [Test]
        public void MoundsAndCratersSlopeOnEverySide([Values(ElevationKind.Mound, ElevationKind.Crater)] ElevationKind kind)
        {
            ElevationStructureRule rule = Rule(kind);
            int built = 0;
            for (int seed = 1; seed <= Seeds; seed++)
                foreach (RoomStructure room in RoomyRooms())
                {
                    room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    built += room.Elevations.Count;
                    string at = $"seed {seed}, {room.Footprint.Bounds.size}";

                    Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"{at}: every tile reachable both ways");
                    foreach (ElevatedArea area in room.Elevations)
                    {
                        Assert.That(area.Stairs, Is.Not.Empty, at);
                        Assert.That(area.Stairs, Has.All.Matches<StairFlight>(flight => flight.Slope), $"{at}: slopes, not steps");
                        Assert.That(area.Corners, Is.Not.Empty, $"{at}: rounded off");
                        Assert.That(area.Railed, Is.False);
                    }

                    // No open ledge: wherever the level changes, it is up a slope or beside a solid corner.
                    foreach (Vector2Int tile in room.Footprint.Tiles)
                        foreach (Vector2Int side in GridSides.All)
                            if (room.IsLedge(tile, tile + side))
                                Assert.That(room.Has(tile, RoomTileTags.Structure) || room.Has(tile + side, RoomTileTags.Structure), Is.True,
                                            $"{at}: a bare ledge between {tile} and {tile + side}");
                }
            Assert.That(built, Is.GreaterThan(Seeds / 2), "most rooms got one");
        }

        [Test]
        public void ABridgedSunkenFloorHasStairsIntoBothHalves()
        {
            ElevationStructureRule rule = Rule(ElevationKind.SunkenFloor, bridge: true, minLength: 5);
            int bridged = 0;
            for (int seed = 1; seed <= Seeds; seed++)
                foreach (RoomStructure room in RoomyRooms())
                {
                    room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, $"seed {seed}");

                    foreach (ElevatedArea area in room.Elevations)
                    {
                        var tiles = new HashSet<Vector2Int>(area.Tiles);
                        int halves = 0;
                        foreach (Vector2Int tile in area.Tiles)
                            if (!tiles.Contains(tile + Vector2Int.left) && !tiles.Contains(tile + Vector2Int.down)) halves++;
                        if (halves < 2) continue;
                        bridged++;
                        Assert.That(area.Stairs.Count, Is.GreaterThanOrEqualTo(2), $"seed {seed}: a flight into each half");
                    }
                }
            Assert.That(bridged, Is.GreaterThan(0));
        }
    }
}
