using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Map;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // A Breach room shared by zone: a room in one piece gets the breaching faction's divider,
    // the breach takes the zones its doorways open into (else all but the largest), and each
    // side's props stay on that side.
    public class BreachSplitTests
    {
        private static readonly Vector2Int WestDoor = new(0, 4);
        private static readonly Vector2Int EastDoor = new(9, 4);

        // A 10 × 10 room cut by a wall between columns 3 and 4 (an opening at the walkway):
        // zone of x < 4 is 40 tiles, the east one 60.
        private static RoomStructure SplitRoom()
        {
            RoomStructure room = Room(10, 10, WestDoor, EastDoor);
            var edges = new List<PartitionEdge>();
            for (int y = 0; y < 10; y++)
                if (!room.EdgeMustStayOpen(new Vector2Int(3, y), new Vector2Int(4, y)))
                    edges.Add(new PartitionEdge(new Vector2Int(3, y), new Vector2Int(4, y), 3f));
            Assert.IsTrue(room.TryAddPartition(edges));
            room.AssignZones();
            Assert.AreEqual(2, room.ZoneCount);
            return room;
        }

        [Test]
        public void ARoomInOnePieceIsNotSplit()
        {
            Assert.IsNull(BreachSplit.Of(Room(10, 10, WestDoor), new[] { WestDoor }));
        }

        [Test]
        public void TheBreachTakesTheZoneItsDoorwayOpensInto()
        {
            RoomStructure room = SplitRoom();
            BreachSplit split = BreachSplit.Of(room, new[] { WestDoor });

            Assert.IsTrue(split.IsBreachZone(room.ZoneOf(WestDoor)));
            Assert.IsFalse(split.IsBreachZone(room.ZoneOf(EastDoor)));
        }

        [Test]
        public void WithoutABreachDoorwayTheHolderKeepsTheLargestZone()
        {
            RoomStructure room = SplitRoom();
            BreachSplit split = BreachSplit.Of(room, System.Array.Empty<Vector2Int>());

            Assert.IsFalse(split.IsBreachZone(room.ZoneOf(EastDoor)), "the east zone is the larger");
            Assert.IsTrue(split.IsBreachZone(room.ZoneOf(WestDoor)));
        }

        [Test]
        public void WithBreachDoorwaysEverywhereTheHolderKeepsTheLargestZone()
        {
            RoomStructure room = SplitRoom();
            BreachSplit split = BreachSplit.Of(room, new[] { WestDoor, EastDoor });

            Assert.IsFalse(split.IsBreachZone(room.ZoneOf(EastDoor)));
            Assert.IsTrue(split.IsBreachZone(room.ZoneOf(WestDoor)));
        }

        [Test]
        public void PropsGivenZonesStayInThem()
        {
            for (int seed = 0; seed < Seeds; seed++)
            {
                RoomStructure room = SplitRoom();
                int west = room.ZoneOf(WestDoor);
                RoomPropPlanner planner = Planner(room, seed, fillShare: 0.8f);
                planner.Add(new[] { Placement(5, 5) }, zones: new HashSet<int> { west });

                Assert.That(planner.Planned, Is.Not.Empty, $"seed {seed}");
                foreach (PlannedProp prop in planner.Planned)
                    Assert.AreEqual(west, room.ZoneOf(TileOf(prop)), $"seed {seed}");
            }
        }

        [Test]
        public void RoomSizedCountsReadOnlyTheirZonesFloor()
        {
            RoomStructure room = SplitRoom();
            RoomPropPlacement placement = Placement(16, 16);
            placement.ScaleByRoomSize = true;
            RoomPropPlanner planner = Planner(room, 3, fillShare: 0.8f);
            planner.Add(new[] { placement }, zones: new HashSet<int> { room.ZoneOf(WestDoor) });

            // 16 × 40 / 64 tiles = 10 asked for, not the 25 the whole room would ask.
            Assert.AreEqual(10, planner.Outcomes[0].Wanted);
        }

        [Test]
        public void AnOpenBreachRoomGetsTheDivider()
        {
            var divider = ScriptableObject.CreateInstance<PartitionStructureRule>();
            try
            {
                RoomStructure structure = PlanBreachRoom(divider);
                Assert.That(structure.ZoneCount, Is.GreaterThanOrEqualTo(2));
            }
            finally
            {
                Object.DestroyImmediate(divider);
            }
        }

        [Test]
        public void EachFactionHasAZoneDividingBreachDivider()
        {
            foreach (string faction in new[] { "Bio", "Tech", "Void" })
            {
                var design = AssetDatabase.LoadAssetAtPath<FactionDesign>($"Assets/Project/Data/Level/Designs/{faction}FactionDesign.asset");
                Assert.IsInstanceOf<PartitionStructureRule>(design.BreachDivider, $"{faction} has no breach divider");
            }
        }

        private static RoomStructure PlanBreachRoom(RoomStructureRule divider)
        {
            var graph = new MapGraph();
            MapNode node = graph.AddNode(MapNodeType.Breach, Vector2.zero);
            var room = new LevelRoom(node, RoomFootprint.Rectangle(new RectInt(0, 0, 12, 8)), null, 4f);
            var random = new RandomStream(new Seed(7UL));
            return new RoomStructurePlanner().Plan(room, System.Array.Empty<LevelDoorway>(), random, breachDivider: divider);
        }
    }
}
