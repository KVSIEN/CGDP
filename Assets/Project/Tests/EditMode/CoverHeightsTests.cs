using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using static CGD.Tests.RoomTestKit;

namespace CGD.Tests
{
    // Generated cover reads as one of two kinds: low (shoot over it) or high (hides you
    // standing), never the chest-high in-between.
    public class CoverHeightsTests
    {
        private const int Seeds = 20;

        private static void SetField(object target, string name, object value) =>
            target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);

        private static void AssertReadable(float height, string context)
        {
            bool low  = height >= CoverHeights.LowMin && height <= CoverHeights.LowMax;
            bool high = height >= CoverHeights.HighMin;
            Assert.IsTrue(low || high, $"{context}: {height:0.00} m is neither low nor high cover");
        }

        [Test]
        public void HeightsLandInABand()
        {
            Assert.AreEqual(0f, CoverHeights.Readable(0f), "full height stays full height");
            Assert.AreEqual(1.1f, CoverHeights.Readable(1.1f), 1e-4f, "a counter is already low cover");
            Assert.AreEqual(CoverHeights.LowMin, CoverHeights.Readable(0.5f), 1e-4f);
            Assert.AreEqual(CoverHeights.LowMax, CoverHeights.Readable(1.5f), 1e-4f);
            Assert.AreEqual(CoverHeights.HighMin, CoverHeights.Readable(1.9f), 1e-4f);
            Assert.AreEqual(2.8f, CoverHeights.Readable(2.8f), 1e-4f);

            for (float h = 0.05f; h < 4f; h += 0.05f) AssertReadable(CoverHeights.Readable(h), $"{h:0.00} m");
        }

        [Test]
        public void LowMeansShootOver()
        {
            Assert.IsFalse(CoverHeights.IsLow(0f), "0 is a full-height piece");
            Assert.IsTrue(CoverHeights.IsLow(CoverHeights.LowMax));
            Assert.IsFalse(CoverHeights.IsLow(CoverHeights.HighMin));
        }

        [Test]
        public void WallRemnantsAreLowOrHigh()
        {
            var rule = ScriptableObject.CreateInstance<WallRemnantsStructureRule>();
            try
            {
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    foreach (PartitionEdge wall in room.Partitions) AssertReadable(wall.Height, $"seed {seed}");
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }

        [Test]
        public void BrokenColumnsAreLowOrHigh()
        {
            var rule = ScriptableObject.CreateInstance<GridPillarsStructureRule>();
            try
            {
                SetField(rule, "_brokenChance", 1f);
                for (int seed = 0; seed < Seeds; seed++)
                {
                    RoomStructure room = Room(12, 12, new Vector2Int(0, 6));
                    rule.Apply(room, new RandomStream(new Seed((ulong)seed)));
                    foreach (StructurePillar pillar in room.Pillars) AssertReadable(pillar.Height, $"seed {seed}");
                }
            }
            finally
            {
                Object.DestroyImmediate(rule);
            }
        }
    }
}
