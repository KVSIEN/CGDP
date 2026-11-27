using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // The structure budget judged by area, the cover walls it aims at the open ones, and columns
    // taken away where a wall passes too close.
    public class StructureBudgetTests
    {
        private const int Seeds = 20;

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);

        private static StructureBudget Budget(RoomStructureRule rule)
        {
            var budget = new StructureBudget();
            SetField(budget, "_options", new List<StructureBudget.Option> { new() { Rule = rule } });
            SetField(budget, "_minTiles", 50);
            SetField(budget, "_tilesPerPoint", 5);
            SetField(budget, "_maxAdditions", 4);
            return budget;
        }

        // Full-height walls across the left third, nothing elsewhere.
        private static RoomStructure LeftSideWalled()
        {
            RoomStructure room = Room(24, 12, new Vector2Int(0, 6));
            var edges = new List<PartitionEdge>();
            for (int x = 1; x < 7; x++)
            {
                edges.Add(new PartitionEdge(new Vector2Int(x, 2), new Vector2Int(x, 3), 0f, dividesZones: false));
                edges.Add(new PartitionEdge(new Vector2Int(x, 9), new Vector2Int(x, 10), 0f, dividesZones: false));
            }
            Assert.IsTrue(room.TryAddPartition(edges));
            return room;
        }

        [Test]
        public void CoverWallsStayInTheirAreaClearOfTheWallsAndDivideNothing()
        {
            var rule = ScriptableObject.CreateInstance<CoverWallsStructureRule>();
            try
            {
                var focus = new RectInt(12, 0, 12, 12);
                int built = 0;
                for (int seed = 1; seed <= Seeds; seed++)
                {
                    RoomStructure room = Room(24, 12, new Vector2Int(0, 6), new Vector2Int(23, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)), focus);
                    built += room.Partitions.Count;
                    foreach (PartitionEdge edge in room.Partitions)
                    {
                        Assert.IsTrue(focus.Contains(edge.Tile), $"seed {seed}: {edge.Tile} is outside the area");
                        Assert.IsTrue(room.Footprint.IsInterior(edge.Tile, 1) && room.Footprint.IsInterior(edge.Other, 1),
                                      $"seed {seed}: a wall against the room's own");
                        Assert.IsFalse(edge.DividesZones);
                    }
                    Assert.IsTrue(room.StaysConnected(System.Array.Empty<Vector2Int>()), $"seed {seed}");
                    room.AssignZones();
                    Assert.AreEqual(1, room.ZoneCount, $"seed {seed}");
                }
                Assert.Greater(built, Seeds, "most rooms get a wall of more than one piece");
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void TheBudgetBuildsUpTheOpenSideOfARoom()
        {
            var rule = ScriptableObject.CreateInstance<CoverWallsStructureRule>();
            try
            {
                StructureBudget budget = Budget(rule);
                int open = 0, walled = 0;
                for (int seed = 1; seed <= Seeds; seed++)
                {
                    RoomStructure before = LeftSideWalled();
                    Assert.IsTrue(budget.Needs(before), "the right of the room has nothing");
                    RoomStructure after = budget.Apply(before, new RandomStream(new Seed((ulong)seed)));
                    for (int i = before.Partitions.Count; i < after.Partitions.Count; i++)
                    {
                        if (after.Partitions[i].Tile.x >= 8) open++;
                        else walled++;
                    }
                }
                Assert.Greater(open, 0);
                Assert.Greater(open, walled * 4, "walls go to the open side");
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void AColumnAWallPassesCloseToIsTakenAway()
        {
            RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
            Assert.IsTrue(room.TryAddPillar(new Vector2(5.5f, 4.5f), 0.8f, null));
            Assert.IsTrue(room.TryAddPillar(new Vector2(8.5f, 9.5f), 0.8f, null));
            // A wall along the top of the tile beside the first column: its end is 0.7 tiles from it.
            Assert.IsTrue(room.TryAddPartition(new[]
            {
                new PartitionEdge(new Vector2Int(6, 4), new Vector2Int(6, 5), 0f, dividesZones: false),
                new PartitionEdge(new Vector2Int(7, 4), new Vector2Int(7, 5), 0f, dividesZones: false),
            }));

            Assert.AreEqual(1, room.RemovePillarsBesideWalls());
            Assert.AreEqual(1, room.Pillars.Count);
            Assert.AreEqual(new Vector2(8.5f, 9.5f), room.Pillars[0].Position);
            Assert.IsTrue(room.IsOpen(new Vector2Int(5, 4)), "the column's tile is floor again");
        }
    }
}
