using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The rules that give a room its composition: three lanes down its length, a hub court
    // round its centre, and pavilions (huts, kiosks) standing in its open floor.
    public class CompositionStructureRuleTests
    {
        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private static void AssertWholeWithRoutesOpen(RoomStructure room, int seed)
        {
            Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}: floor cut off");
            foreach (PartitionEdge wall in room.Partitions)
            {
                Assert.IsFalse(room.Has(wall.Tile, RoomTileTags.Walkway) && room.Has(wall.Other, RoomTileTags.Walkway),
                               $"seed {seed}: a wall across the route at {wall.Tile}");
                Assert.IsFalse(room.Has(wall.Tile, RoomTileTags.NearDoor) || room.Has(wall.Other, RoomTileTags.NearDoor),
                               $"seed {seed}: a wall beside a doorway at {wall.Tile}");
            }
        }

        [Test]
        public void ThreeLanesSplitTheRoomLengthwiseAndStayCrossable()
        {
            var rule = ScriptableObject.CreateInstance<ThreeLanesStructureRule>();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(20, 12, new Vector2Int(0, 6), new Vector2Int(19, 3));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();

                    Assert.Greater(room.Partitions.Count, 10, $"seed {seed}: no lane walls");
                    Assert.AreEqual(1, room.ZoneCount, $"seed {seed}: lanes made zones");
                    AssertWholeWithRoutesOpen(room, seed);

                    // Two wall lines, both running the room's length (walls between rows of tiles).
                    var lines = new HashSet<int>();
                    foreach (PartitionEdge wall in room.Partitions)
                    {
                        Assert.AreEqual(Vector2Int.up, wall.Side, $"seed {seed}: a lane wall across the room");
                        lines.Add(wall.Other.y);
                    }
                    Assert.AreEqual(2, lines.Count, $"seed {seed}: not two lane walls");
                    foreach (int line in lines)
                        Assert.Less(WallsOn(room, line), 20 - 2, $"seed {seed}: lane wall at {line} has no crossing");
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void ThreeLanesLeaveARoomTooNarrowAlone()
        {
            var rule = ScriptableObject.CreateInstance<ThreeLanesStructureRule>();
            try
            {
                RoomStructure room = Room(14, 5, new Vector2Int(0, 2));
                rule.Apply(room, new RandomStream(new Seed(1)));
                Assert.AreEqual(0, room.Partitions.Count);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void HubRingWallsACourtWithALoopRoundIt()
        {
            var rule = ScriptableObject.CreateInstance<HubRingStructureRule>();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(16, 14, new Vector2Int(0, 7), new Vector2Int(8, 13));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();

                    Assert.Greater(room.Partitions.Count, 8, $"seed {seed}: no court");
                    Assert.AreEqual(1, room.ZoneCount, $"seed {seed}: the court made a zone");
                    AssertWholeWithRoutesOpen(room, seed);
                    foreach (PartitionEdge wall in room.Partitions)
                    {
                        Assert.IsTrue(CoverHeights.IsLow(wall.Height), $"seed {seed}: the default rail isn't low cover");
                        // Loop floor between the court and the room's walls on every side.
                        Assert.IsTrue(room.Footprint.IsInterior(wall.Tile, 1) && room.Footprint.IsInterior(wall.Other, 1),
                                      $"seed {seed}: the court crowds the room's wall at {wall.Tile}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void PavilionsStandWalledInOpenFloorAsAnnexes()
        {
            var rule = ScriptableObject.CreateInstance<PavilionStructureRule>();
            var function = ScriptableObject.CreateInstance<RoomFunction>();
            try
            {
                Set(rule, "_kinds", new[] { new PavilionKind { Function = function, Name = "Hut", MinSize = new Vector2Int(2, 3), MaxSize = new Vector2Int(3, 3) } });
                Set(rule, "_count", new IntRange(2, 2));
                Set(rule, "_tilesEach", 60);

                int built = 0;
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(18, 14, new Vector2Int(0, 7), new Vector2Int(17, 3));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();
                    AssertWholeWithRoutesOpen(room, seed);

                    built += room.Spaces.Count;
                    Assert.AreEqual(room.Spaces.Count + 1, room.ZoneCount, $"seed {seed}: each pavilion isn't its own zone");
                    ICollection<int> floor = room.ZonesOutsideAnnexes();
                    foreach (StructureSpace space in room.Spaces)
                    {
                        Assert.IsTrue(space.Annex, $"seed {seed}");
                        Assert.AreSame(function, space.Function, $"seed {seed}");
                        Assert.IsFalse(floor.Contains(room.ZoneOf(space.Tile)), $"seed {seed}: the room furnishes inside a pavilion");
                    }
                    if (room.Spaces.Count > 0) Assert.AreEqual(room.ZoneCount - room.Spaces.Count, floor.Count, $"seed {seed}");
                }
                Assert.Greater(built, Seeds, "pavilions rarely fit a room with plenty of floor");
            }
            finally
            {
                Object.DestroyImmediate(rule);
                Object.DestroyImmediate(function);
            }
        }

        [Test]
        public void ARoomWithoutAnnexesIsFurnishedWhole()
        {
            RoomStructure room = Room(10, 8, new Vector2Int(0, 4));
            room.AssignZones();
            Assert.IsNull(room.ZonesOutsideAnnexes());
        }

        private static int WallsOn(RoomStructure room, int line)
        {
            int count = 0;
            foreach (PartitionEdge wall in room.Partitions)
                if (wall.Other.y == line) count++;
            return count;
        }
    }
}
