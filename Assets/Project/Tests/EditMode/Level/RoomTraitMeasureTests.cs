using System;
using NUnit.Framework;
using UnityEngine;
using CGD.Level;
using CGD.Stealth;
using static CGD.Tests.RoomTestKit;
using Object = UnityEngine.Object;

namespace CGD.Tests
{
    // A room's concealment, walls and lines of fire are read off its floor: narrow floor for
    // walls, long lanes with cover beside them for lines of fire, concealment patches for
    // concealment.
    public class RoomTraitMeasureTests
    {
        [Test]
        public void ACorridorIsAllWalls()
        {
            RoomStructure room = Room(2, 12, new Vector2Int(0, 0));
            RoomTraitReading reading = RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>());

            Assert.AreEqual(1f, reading.NarrowShare, 0.001f);
            Assert.AreEqual(TraitPriority.High, reading.LevelOf(RoomTrait.Walls));
        }

        [Test]
        public void AnEmptyHallHasALongLaneButNothingToShootFrom()
        {
            RoomStructure room = Room(12, 12, new Vector2Int(11, 6));
            RoomTraitReading reading = RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>());

            Assert.AreEqual(12, reading.LongestLane);
            Assert.IsFalse(reading.LaneHasCover);
            Assert.AreEqual(TraitPriority.Medium, reading.LevelOf(RoomTrait.LinesOfFire), "an uncovered lane counts a level lower");
            Assert.AreEqual(TraitPriority.Low, reading.LevelOf(RoomTrait.Walls));
            Assert.AreEqual(TraitPriority.Low, reading.LevelOf(RoomTrait.Concealment));
        }

        [Test]
        public void APillarBesideALongLaneMakesItALineOfFire()
        {
            RoomStructure room = Room(12, 12, new Vector2Int(11, 6));
            Assert.IsTrue(room.TryAddPillar(new Vector2(2.5f, 2.5f), 0.8f, null));   // off the walkway along row 6

            RoomTraitReading reading = RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>());

            Assert.AreEqual(12, reading.LongestLane);
            Assert.IsTrue(reading.LaneHasCover);
            Assert.AreEqual(TraitPriority.High, reading.LevelOf(RoomTrait.LinesOfFire));
        }

        [Test]
        public void ConcealmentPatchesCountAsConcealedFloor()
        {
            var patch = new GameObject("Patch", typeof(BoxCollider), typeof(ConcealmentZone));
            try
            {
                RoomStructure room = Room(10, 10, new Vector2Int(9, 5));
                var props = new[] { new PlannedProp(patch, new Vector2(1f, 1f), 0f, new RectInt(0, 0, 2, 2), decor: true) };

                RoomTraitReading reading = RoomTraitMeasure.Measure(room, props);

                Assert.AreEqual(0.04f, reading.ConcealedShare, 0.001f);
                Assert.AreEqual(TraitPriority.Medium, reading.LevelOf(RoomTrait.Concealment));
            }
            finally
            {
                Object.DestroyImmediate(patch);
            }
        }

        [Test]
        public void PropsWithoutConcealmentDontHideAnyone()
        {
            var crate = new GameObject("Crate");
            try
            {
                RoomStructure room = Room(10, 10, new Vector2Int(9, 5));
                var props = new[] { new PlannedProp(crate, new Vector2(1f, 1f), 0f, new RectInt(0, 0, 2, 2), decor: true) };

                Assert.AreEqual(0f, RoomTraitMeasure.Measure(room, props).ConcealedShare);
            }
            finally
            {
                Object.DestroyImmediate(crate);
            }
        }
    }
}
