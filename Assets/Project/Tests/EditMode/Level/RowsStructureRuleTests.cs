using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Shelving, racks and seat rows: parallel walls along the room with aisles between them,
    // broken so the aisles can be crossed, never across a route or beside a doorway.
    public class RowsStructureRuleTests
    {
        private static RowsStructureRule Rule(float height = 2.4f, bool across = false)
        {
            var rule = ScriptableObject.CreateInstance<RowsStructureRule>();
            Set(rule, "_height", height);
            Set(rule, "_across", across);
            return rule;
        }

        private static void Set(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        [Test]
        public void RowsLeaveTheRoomWholeAndItsRoutesOpen()
        {
            RowsStructureRule rule = Rule();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(14, 10, new Vector2Int(0, 5), new Vector2Int(13, 2));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    room.AssignZones();

                    Assert.Greater(room.Partitions.Count, 6, $"seed {seed}: no rows");
                    Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}");
                    Assert.AreEqual(1, room.ZoneCount, $"seed {seed}: rows made zones");
                    foreach (PartitionEdge wall in room.Partitions)
                    {
                        Assert.IsFalse(wall.DividesZones);
                        Assert.IsFalse(room.Has(wall.Tile, RoomTileTags.Walkway) && room.Has(wall.Other, RoomTileTags.Walkway),
                                       $"seed {seed}: a row across the route at {wall.Tile}");
                        Assert.IsFalse(room.Has(wall.Tile, RoomTileTags.NearDoor) || room.Has(wall.Other, RoomTileTags.NearDoor),
                                       $"seed {seed}: a row beside a doorway at {wall.Tile}");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [TestCase(false, 0, 1)]   // along the longer side (x): walls between rows of tiles
        [TestCase(true, 1, 0)]    // across it
        public void RowsRunAlongTheLongerSideUnlessSetAcross(bool across, int sideX, int sideY)
        {
            RowsStructureRule rule = Rule(across: across);
            try
            {
                RoomStructure room = Room(16, 10, new Vector2Int(0, 5));
                rule.Apply(room, new RandomStream(new Seed(3)));

                Assert.Greater(room.Partitions.Count, 0);
                foreach (PartitionEdge wall in room.Partitions) Assert.AreEqual(new Vector2Int(sideX, sideY), wall.Side);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void RowsBreakSoTheAislesCanBeCrossed()
        {
            RowsStructureRule rule = Rule();
            try
            {
                RoomStructure room = Room(16, 10, new Vector2Int(0, 5));
                rule.Apply(room, new RandomStream(new Seed(5)));

                // No row stands unbroken from one end of the room to the other.
                var perLine = new System.Collections.Generic.Dictionary<int, int>();
                foreach (PartitionEdge wall in room.Partitions)
                    perLine[wall.Other.y] = perLine.TryGetValue(wall.Other.y, out int n) ? n + 1 : 1;
                foreach (int count in perLine.Values) Assert.Less(count, 16 - 2);
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void LowRowsAreLowCover()
        {
            RowsStructureRule rule = Rule(height: 1.4f);
            try
            {
                RoomStructure room = Room(14, 10, new Vector2Int(0, 5));
                rule.Apply(room, new RandomStream(new Seed(1)));

                Assert.Greater(room.Partitions.Count, 0);
                foreach (PartitionEdge wall in room.Partitions) Assert.IsTrue(CoverHeights.IsLow(wall.Height), $"{wall.Height} m");
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }
    }
}
