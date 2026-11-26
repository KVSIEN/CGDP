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
    // Rooms split into lanes at different levels: raised walks along the walls, a sunken court,
    // a split deck. Every doorway opens onto level 0, every lane has its stairs, nothing is two
    // levels from its neighbour, and the floor and the walls meet without a gap.
    public class RoomTerracesTests
    {
        private static TerracesStructureRule Rule(bool shift = true, bool railed = true, params (int level, float weight)[] lanes) => Rule(shift, railed, 0, lanes);

        private static TerracesStructureRule Rule(bool shift, bool railed, int bridges, params (int level, float weight)[] lanes)
        {
            var rule = ScriptableObject.CreateInstance<TerracesStructureRule>();
            var serialized = new SerializedObject(rule);
            SerializedProperty list = serialized.FindProperty("_lanes");
            list.arraySize = lanes.Length;
            for (int i = 0; i < lanes.Length; i++)
            {
                list.GetArrayElementAtIndex(i).FindPropertyRelative("Level").intValue = lanes[i].level;
                list.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").floatValue = lanes[i].weight;
            }
            serialized.FindProperty("_shiftToDoorways").boolValue = shift;
            serialized.FindProperty("_railed").boolValue = railed;
            serialized.FindProperty("_bridges").intValue = bridges;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return rule;
        }

        private static TerracesStructureRule RaisedWalks(bool shift = true, bool railed = true) => Rule(shift, railed, (1, 1f), (0, 2f), (1, 1f));

        // A long room, 16 by 10: its lanes run along x, stacked up y (two, six and two tiles wide).
        private static RoomStructure Hall(params Vector2Int[] doors)
        {
            RoomStructure room = Room(16, 10, doors);
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            return room;
        }

        private static readonly Vector2Int[] EndDoors  = { new(15, 5), new(0, 4) };
        private static readonly Vector2Int[] SideDoors = { new(5, 9), new(10, 0) };

        private static void Apply(RoomStructureRule rule, RoomStructure room, ulong seed = 1) => rule.Apply(room, new RandomStream(new Seed(seed)));

        [Test]
        public void DoorwaysAtTheEndsGetRaisedWalksAlongTheSides()
        {
            RoomStructure room = Hall(EndDoors);
            Apply(RaisedWalks(), room);

            Assert.That(room.LevelOf(new Vector2Int(3, 0)), Is.EqualTo(1), "the south walk");
            Assert.That(room.LevelOf(new Vector2Int(3, 9)), Is.EqualTo(1), "the north walk");
            Assert.That(room.LevelOf(new Vector2Int(3, 5)), Is.Zero, "the middle, level with the doorways");
            Assert.That(room.Elevations.Count, Is.EqualTo(2));
            AssertSound(room);
        }

        [Test]
        public void DoorwaysOntoTheSidesTurnTheWalksIntoASunkenCourt()
        {
            RoomStructure room = Hall(SideDoors);
            Apply(RaisedWalks(), room);

            Assert.That(room.LevelOf(new Vector2Int(3, 0)), Is.Zero, "the walks, level with the doorways");
            Assert.That(room.LevelOf(new Vector2Int(12, 5)), Is.EqualTo(-1), "the court sunk below them");
            Assert.That(room.LevelOf(new Vector2Int(5, 7)), Is.Zero, "floor just inside a doorway stays level with it");
            AssertSound(room);
        }

        [Test]
        public void WithoutTheShiftAProfileTheDoorwaysDontFitIsLeftFlat()
        {
            RoomStructure room = Hall(SideDoors);
            Apply(RaisedWalks(shift: false), room);

            Assert.That(room.HasElevation, Is.False);
        }

        [Test]
        public void ARoomTooNarrowForItsLanesIsLeftFlat()
        {
            RoomStructure room = Room(16, 5, new Vector2Int(15, 2));
            room.AllowLevels(RoomElevation.Lowest, RoomElevation.Highest);
            Apply(Rule(true, true, (1, 1f), (0, 1f), (1, 1f), (0, 1f)), room);

            Assert.That(room.HasElevation, Is.False, "four lanes of two tiles need eight");
        }

        [Test]
        public void ARoomThatAllowsNoLevelIsLeftAloneWithNoRandomDrawn()
        {
            RoomStructure room = Room(16, 10, EndDoors);
            var used = new RandomStream(new Seed(9UL));
            var fresh = new RandomStream(new Seed(9UL));

            RaisedWalks().Apply(room, used);

            Assert.That(room.HasElevation, Is.False);
            Assert.That(used.Value, Is.EqualTo(fresh.Value));
        }

        [Test]
        public void LongLedgesGetSeveralFlights()
        {
            RoomStructure room = Hall(EndDoors);
            Apply(RaisedWalks(), room);

            foreach (ElevatedArea area in room.Elevations)
                Assert.That(area.Stairs.Count, Is.GreaterThanOrEqualTo(2), "16 tiles of ledge, a flight for every six");
        }

        [Test]
        public void RailedLedgesAreNotDroppedOffAndOpenOnesAre()
        {
            RoomStructure railed = Hall(EndDoors);
            Apply(RaisedWalks(railed: true), railed);
            Assert.That(railed.CanDropDown(new Vector2Int(8, 1), new Vector2Int(8, 2)), Is.False);

            RoomStructure open = Hall(EndDoors);
            Apply(RaisedWalks(railed: false), open);
            int drops = 0;
            for (int x = 0; x < 16; x++)
                if (open.CanDropDown(new Vector2Int(x, 1), new Vector2Int(x, 2))) drops++;
            Assert.That(drops, Is.GreaterThan(8), "most of the walk's edge, all but its stairs");
        }

        [Test]
        public void EveryProfileSitsSoundlyWhereverTheDoorsAre()
        {
            var rules = new[]
            {
                RaisedWalks(),
                Rule(true, true, (1, 1f), (0, 1f)),
                Rule(true, true, (0, 1f), (1, 1f), (0, 1f)),
                Rule(true, false, (0, 1f), (-1, 2f), (0, 1f)),
            };
            var doorSets = new[] { EndDoors, SideDoors, new[] { new Vector2Int(15, 5), new Vector2Int(7, 0) }, new[] { new Vector2Int(0, 1) } };
            int built = 0;
            foreach (TerracesStructureRule rule in rules)
                foreach (Vector2Int[] doors in doorSets)
                {
                    RoomStructure room = Hall(doors);
                    Apply(rule, room);
                    if (room.HasElevation) built++;
                    AssertSound(room);
                }
            Assert.That(built, Is.GreaterThan(8));
        }

        // Reachable both ways, doorways on level 0, no step of two levels, stairs onto every area.
        private static void AssertSound(RoomStructure room)
        {
            Assert.That(room.StaysConnected(System.Array.Empty<Vector2Int>()), Is.True, "every tile reachable both ways");
            foreach (Vector2Int entrance in room.Entrances)
                Assert.That(room.LevelOf(entrance), Is.Zero, $"the doorway at {entrance} opens onto level 0");
            foreach (Vector2Int tile in room.Footprint.Tiles)
                foreach (Vector2Int side in GridNeighbours.Four)
                    if (room.Contains(tile + side))
                        Assert.That(Mathf.Abs(room.LevelOf(tile) - room.LevelOf(tile + side)), Is.LessThanOrEqualTo(1), $"{tile} beside {tile + side}");
            foreach (ElevatedArea area in room.Elevations)
                Assert.That(area.Stairs.Count, Is.GreaterThan(0));
        }

        [Test]
        public void ABridgeCrossesASunkenCourtLevelWithItsSides()
        {
            RoomStructure room = Hall(SideDoors);
            Apply(Rule(true, true, 1, (0, 1f), (-1, 2f), (0, 1f)), room);

            Assert.That(room.LevelOf(new Vector2Int(8, 5)), Is.Zero, "the bridge, halfway along");
            Assert.That(room.LevelOf(new Vector2Int(7, 5)), Is.EqualTo(-1));
            Assert.That(room.LevelOf(new Vector2Int(9, 5)), Is.EqualTo(-1));
            Assert.That(room.CanStep(new Vector2Int(8, 1), new Vector2Int(8, 2)) && room.CanStep(new Vector2Int(8, 7), new Vector2Int(8, 8)), Is.True, "walked across from walk to walk");
            Assert.That(room.CanDropDown(new Vector2Int(8, 4), new Vector2Int(7, 4)), Is.False, "railed like the court");
            AssertSound(room);
        }

        // --- Geometry and links -----------------------------------------------------------

        [Test]
        public void TheFloorReachesDownToTheLowestFloorBesideIt()
        {
            RoomStructure room = Hall(SideDoors);
            Apply(RaisedWalks(), room);
            const float Thickness = 0.2f;

            (float bottom, float top) rim = ElevationGeometryBuilder.FloorSpan(room, new Vector2Int(12, 1), Thickness);
            Assert.That(rim.top, Is.Zero, "the walk's floor");
            Assert.That(rim.bottom, Is.EqualTo(-RoomElevation.StepHeight - Thickness).Within(1e-4f), "its side closes the court's edge");

            (float bottom, float top) court = ElevationGeometryBuilder.FloorSpan(room, new Vector2Int(12, 5), Thickness);
            Assert.That(court.top, Is.EqualTo(-RoomElevation.StepHeight).Within(1e-4f));

            (float bottom, float top) flat = ElevationGeometryBuilder.FloorSpan(Room(6, 6), new Vector2Int(2, 2), Thickness);
            Assert.That(flat, Is.EqualTo((-Thickness, 0f)), "a flat room keeps its plain slab");
        }

        [Test]
        public void EdgesGatherIntoRunsAlongALine()
        {
            var edges = new List<(Vector2Int, Vector2Int, int)>
            {
                (new Vector2Int(3, 0), Vector2Int.right, 0), (new Vector2Int(3, 1), Vector2Int.right, 0), (new Vector2Int(3, 2), Vector2Int.right, 0),
                (new Vector2Int(3, 4), Vector2Int.right, 0),                       // a gap: a run of its own
                (new Vector2Int(3, 5), Vector2Int.right, 1),                       // another group on the same line
                (new Vector2Int(6, 2), Vector2Int.up, 0), (new Vector2Int(7, 2), Vector2Int.up, 0),
            };

            List<EdgeRuns.Run> runs = EdgeRuns.Of(edges, maxRun: 2);

            Assert.That(runs.Count, Is.EqualTo(5), "3 tiles cut at 2, the lone tile, the other group and the row");
            Assert.That((runs[0].First, runs[0].Last), Is.EqualTo((0, 1)));
            Assert.That((runs[1].First, runs[1].Last), Is.EqualTo((2, 2)));
            (Vector2 from, Vector2 to) = EdgeRuns.Middles(runs[0]);
            Assert.That(from, Is.EqualTo(new Vector2(3.5f, 1f)));
            Assert.That(to, Is.EqualTo(new Vector2(4.5f, 1f)));
        }
    }
}
