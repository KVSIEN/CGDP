using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // A room's flow from the doorway a player arrives by (RoomFlowPlanner): the threshold just
    // inside it and the goal at the far end, the flank round the main lane (RoomFlank), the
    // placements that read them (Band, Showcase, the offerings' nook) and enemies kept past the
    // threshold.
    public class RoomFlowTests
    {
        private const int Width = 14, Height = 9, DividerX = 7;
        private static readonly Vector2Int WestDoor = new(0, 4);
        private static readonly Vector2Int EastDoor = new(Width - 1, 4);

        // A hall entered from the west with a way on to the east, and optionally a full-height
        // wall down the middle with gaps at the given rows.
        private static RoomStructure Hall(params int[] gaps) => Hall(0f, gaps);

        private static RoomStructure Hall(float wallHeight, params int[] gaps)
        {
            RoomStructure room = Room(Width, Height, WestDoor, EastDoor);
            if (gaps.Length > 0)
            {
                var edges = new List<PartitionEdge>();
                for (int y = 0; y < Height; y++)
                    if (System.Array.IndexOf(gaps, y) < 0)
                        edges.Add(new PartitionEdge(new Vector2Int(DividerX - 1, y), new Vector2Int(DividerX, y), wallHeight, dividesZones: false));
                Assert.That(room.TryAddPartition(edges), Is.True);
            }
            RoomFlowPlanner.Mark(room, WestDoor);
            return room;
        }

        [Test]
        public void TheThresholdIsByTheWayInAndTheGoalAtTheFarEnd()
        {
            RoomStructure room = Hall();

            Assert.That(room.Arrival, Is.EqualTo(WestDoor));
            Assert.That(room.Has(WestDoor, RoomTileTags.Threshold), Is.True);
            Assert.That(room.Has(EastDoor, RoomTileTags.Goal), Is.True);
            foreach (Vector2Int tile in room.Footprint.Tiles)
            {
                Assert.That(room.Has(tile, RoomTileTags.Threshold) && room.Has(tile, RoomTileTags.Goal), Is.False, $"tile {tile}");
                if (room.Has(tile, RoomTileTags.Threshold)) Assert.That(tile.x, Is.LessThan(Width / 2), $"tile {tile}");
                if (room.Has(tile, RoomTileTags.Goal))      Assert.That(tile.x, Is.GreaterThanOrEqualTo(Width / 2), $"tile {tile}");
            }
            Assert.That(TilesWith(room, RoomTileTags.Threshold), Is.GreaterThan(0));
            Assert.That(TilesWith(room, RoomTileTags.Goal), Is.GreaterThan(0));
        }

        [Test]
        public void ARoomNothingLeadsBackIntoHasNoFlow()
        {
            RoomStructure room = Room(Width, Height, WestDoor, EastDoor);

            Assert.That(room.Arrival.HasValue, Is.False);
            Assert.That(TilesWith(room, RoomTileTags.Threshold | RoomTileTags.Goal), Is.Zero);
            foreach (RoomFlowBand band in System.Enum.GetValues(typeof(RoomFlowBand)))
                Assert.That(RoomFlowPlanner.InBand(room, new Vector2Int(3, 3), band), Is.True, band.ToString());
            Assert.That(RoomFlank.Has(room), Is.True);
        }

        [Test]
        public void TheBandsAreMeasuredRoundInnerWalls()
        {
            // Just behind the wall's single gap is near as the crow flies but far on foot.
            RoomStructure room = Hall(Height - 1);
            var behind = new Vector2Int(DividerX, 0);
            Assert.That(room.Has(behind, RoomTileTags.Threshold), Is.False);
            Assert.That(room.Has(behind, RoomTileTags.Goal), Is.True);
        }

        [Test]
        public void AnOpenHallHasAFlank() => Assert.That(RoomFlank.Has(Hall()), Is.True);

        [Test]
        public void ADividerWithOneGapLeavesNoFlank() => Assert.That(RoomFlank.Has(Hall(4)), Is.False);

        [Test]
        public void ADividerWithTwoGapsFarApartIsALoop() => Assert.That(RoomFlank.Has(Hall(1, 7)), Is.True);

        [Test]
        public void FurnitureCanCloseAFlank()
        {
            // A crate in one of the two gaps leaves a single way through.
            RoomStructure room = Hall(1, 7);
            Assert.That(room.TryClaim(new[] { new Vector2Int(DividerX, 7) }, RoomTileTags.Prop), Is.True);

            Assert.That(RoomFlank.Has(room), Is.False);
        }

        [Test]
        public void AReservedFlankSurvivesFurnitureOnEveryOtherTile()
        {
            RoomStructure room = Hall(1, 7);
            RoomFlank.Reserve(room);
            Assert.That(TilesWith(room, RoomTileTags.FlankLane), Is.GreaterThan(0));

            foreach (Vector2Int tile in room.Footprint.Tiles)
                if (tile != room.Arrival && !room.Has(tile, RoomTileTags.FlankLane | RoomTileTags.Structure))
                    room.TryClaim(new[] { tile }, RoomTileTags.Prop);

            Assert.That(RoomFlank.Has(room), Is.True);
        }

        [Test]
        public void ARoomWithNoFlankGetsNoneReserved()
        {
            RoomStructure room = Hall(4);
            RoomFlank.Reserve(room);
            Assert.That(TilesWith(room, RoomTileTags.FlankLane), Is.Zero);
        }

        [Test]
        public void ARoomTooSmallForAFlankIsNotJudged()
        {
            RoomStructure room = Room(6, 6, new Vector2Int(0, 3));
            RoomFlowPlanner.Mark(room, new Vector2Int(0, 3));
            Assert.That(room.Footprint.Count, Is.LessThan(RoomFlank.MinFloorTiles));
            Assert.That(RoomFlank.Has(room), Is.True);
        }

        [Test]
        public void AnOpenHallIsReadFromTheDoorAndAWallHidesWhatIsBehindIt()
        {
            RoomFlowReading open = RoomFlowReading.Of(Hall());
            Assert.That(open.HasArrival, Is.True);
            Assert.That(open.Revealed, Is.EqualTo(1f));
            Assert.That(open.Filled, Is.Zero);
            Assert.That(open.Flank, Is.True);

            RoomFlowReading walled = RoomFlowReading.Of(Hall(4));
            Assert.That(walled.Revealed, Is.GreaterThan(0.4f).And.LessThan(0.8f));
            Assert.That(walled.Flank, Is.False);

            Assert.That(RoomFlowReading.Of(Room(Width, Height, WestDoor, EastDoor)).HasArrival, Is.False);
        }

        [Test]
        public void ACopyKeepsTheArrival() => Assert.That(Hall().Copy().Arrival, Is.EqualTo(WestDoor));

        [Test]
        public void AGoalPlacementStandsWhollyInTheGoal()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Hall();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(4, 4);
                placement.Band = RoomFlowBand.Goal;
                planner.Add(placement);

                Assert.That(planner.Planned, Is.Not.Empty, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                    foreach (Vector2Int tile in prop.Tiles.allPositionsWithin)
                        Assert.That(room.Has(tile, RoomTileTags.Goal), Is.True, $"seed {seed}, tile {tile}");
            }
        }

        [Test]
        public void AThresholdPlacementStandsByTheWayIn()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Hall();
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                RoomPropPlacement placement = Placement(1, 1);
                placement.Band = RoomFlowBand.Threshold;
                planner.Add(placement);

                foreach (PlannedProp prop in planner.Planned)
                    Assert.That(room.Has(TileOf(prop), RoomTileTags.Threshold), Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void TheOfferingsNookIsAtTheFarEnd()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = Hall();
                RoomPropPlanner planner = Planner(room, seed);
                planner.Add(RoomPropPlacement.Nook(new GameObject[1]));

                Assert.That(planner.Planned, Has.Count.EqualTo(1), $"seed {seed}");
                Assert.That(room.Has(TileOf(planner.Planned[0]), RoomTileTags.Goal), Is.True, $"seed {seed}");
            }
        }

        [Test]
        public void AShowcaseIsSeenFromTheWayIn()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                // Behind the wall is hidden from the arrival except through its gap.
                RoomStructure room = Hall(4);
                RoomPropPlanner planner = Planner(room, seed);
                RoomPropPlacement placement = Placement(1, 1);
                placement.Selection = PropSelection.Showcase;
                placement.Avoid = RoomTileTags.Walkway | RoomTileTags.NearDoor | RoomTileTags.Structure | RoomTileTags.Prop;
                planner.Add(placement);

                Assert.That(planner.Planned, Has.Count.EqualTo(1), $"seed {seed}");
                RectInt tiles = planner.Planned[0].Tiles;
                Assert.That(new ArrivalSight(room).Sees(tiles), Is.True, $"seed {seed}, at {tiles.min}");
                // Only the walkway (kept clear) is seen through the gap, so the deepest seen floor is the
                // near half's far corners: a showcase goes there, not by the door.
                Assert.That(Vector2.Distance(tiles.center, WestDoor + Vector2.one * 0.5f), Is.GreaterThan(4f), $"seed {seed}, at {tiles.min}");
            }
        }

        [Test]
        public void LowWallsAreSeenOverAndTallOnesAreNot()
        {
            var farSide = new RectInt(DividerX + 2, 0, 1, 1);
            Assert.That(new ArrivalSight(Hall(1.1f, 4)).Sees(farSide), Is.True);
            Assert.That(new ArrivalSight(Hall(0f, 4)).Sees(farSide), Is.False);
        }

        [Test]
        public void EnemiesStartAndPatrolPastTheThreshold()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure structure = Hall();
                MapNode node = new MapGraph().AddNode(MapNodeType.Combat, Vector2.zero);
                var room = new LevelRoom(node, structure.Footprint, null, 4f);
                room.AttachStructure(structure);

                RoomEnemyPlan plan = RoomEnemyPlanner.Plan(room, System.Array.Empty<PlannedProp>(), 4,
                                                           new RandomStream(new Seed((ulong)seed)), null, null, null, 1);
                Assert.That(plan.Spawns, Has.Count.EqualTo(4), $"seed {seed}");
                foreach (PlannedSpawn spawn in plan.Spawns)
                    Assert.That(structure.Has(spawn.Tile, RoomTileTags.Threshold), Is.False, $"seed {seed}, spawn {spawn.Tile}");
                foreach (StandSpot stop in plan.PatrolOf(false))
                    Assert.That(structure.Has(stop.Tile, RoomTileTags.Threshold), Is.False, $"seed {seed}, patrol {stop.Tile}");
            }
        }

        [Test]
        public void TheWayInIsAnOrdinaryDoorwayBackTowardStart()
        {
            var room = new LevelRoom(new MapGraph().AddNode(MapNodeType.Combat, Vector2.zero), RoomFootprint.Rectangle(new RectInt(0, 0, 9, 9)), null, 4f);
            var shortcut = new LevelDoorway(room, new MapConnection(0, 1, ConnectionType.Shortcut), new Vector2Int(0, 4), new Vector2Int(-1, 4), false, DoorwayDirection.Back);
            var normal   = new LevelDoorway(room, new MapConnection(0, 2, ConnectionType.Normal), new Vector2Int(4, 0), new Vector2Int(4, -1), false, DoorwayDirection.Back);
            var onward   = new LevelDoorway(room, new MapConnection(0, 3, ConnectionType.Normal), new Vector2Int(8, 4), new Vector2Int(9, 4), true, DoorwayDirection.Onward);

            Assert.That(RoomFlowPlanner.ArrivalOf(room, new[] { onward, shortcut, normal }), Is.EqualTo(normal.RoomTile));
            Assert.That(RoomFlowPlanner.ArrivalOf(room, new[] { onward, shortcut }), Is.EqualTo(shortcut.RoomTile));
            Assert.That(RoomFlowPlanner.ArrivalOf(room, new[] { onward }), Is.Null);
        }
    }
}
