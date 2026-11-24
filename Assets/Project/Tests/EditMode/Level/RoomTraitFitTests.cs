using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Stealth;
using static CGD.Tests.RoomTestKit;
using Object = UnityEngine.Object;

namespace CGD.Tests
{
    // Generation follows a faction's priorities: concealment props scale with the concealment
    // priority, and the try that misses the priorities least is kept.
    public class RoomTraitFitTests
    {
        // No concealment, all narrow floor, a long covered lane: Low / High / High.
        private static readonly RoomTraitReading CoveredCorridor = new(0f, 1f, 12, new RectInt(0, 0, 1, 12), true);

        [Test]
        public void MissCountsLevelsOffTarget()
        {
            Assert.AreEqual(0, RoomTraitFit.Miss(CoveredCorridor, new RoomTraitPriorities(TraitPriority.Low, TraitPriority.High, TraitPriority.High)));
            Assert.AreEqual(6, RoomTraitFit.Miss(CoveredCorridor, new RoomTraitPriorities(TraitPriority.High, TraitPriority.Low, TraitPriority.Low)));
            Assert.AreEqual(1, RoomTraitFit.Miss(CoveredCorridor, new RoomTraitPriorities(TraitPriority.Medium, TraitPriority.High, TraitPriority.High)));
        }

        [Test]
        public void AGapIsZeroExactlyWhenNothingMisses()
        {
            RoomTraitPriorities[] wants =
            {
                new(TraitPriority.Low, TraitPriority.High, TraitPriority.High),
                new(TraitPriority.High, TraitPriority.Low, TraitPriority.Low),
                new(TraitPriority.Medium, TraitPriority.Medium, TraitPriority.Medium),
            };
            RoomTraitReading[] readings =
            {
                CoveredCorridor,
                new(0.06f, 0.15f, 6, default, false),     // each trait exactly on the edge of its level
                new(0.05f, 0.34f, 8, new RectInt(0, 0, 8, 1), true),
                new(0f, 0f, 0, default, false),
            };
            foreach (RoomTraitReading reading in readings)
                foreach (RoomTraitPriorities wanted in wants)
                    Assert.AreEqual(RoomTraitFit.Miss(reading, wanted) == 0, RoomTraitFit.Gap(reading, wanted) == 0f,
                                    $"miss {RoomTraitFit.Miss(reading, wanted)}, gap {RoomTraitFit.Gap(reading, wanted)}");
        }

        [Test]
        public void AGapShrinksAsATraitNearsItsWantedLevel()
        {
            var wantsLanes = new RoomTraitPriorities(TraitPriority.Low, TraitPriority.Low, TraitPriority.High);
            float previous = float.MaxValue;
            foreach (int lane in new[] { 0, 3, 5, 6, 8, 9 })   // Low, Low, Low, Medium, Medium, High: always with cover
            {
                var reading = new RoomTraitReading(0f, 0f, lane, new RectInt(0, 0, lane, 1), true);
                float gap = RoomTraitFit.Gap(reading, wantsLanes);
                Assert.Less(gap, previous, $"a lane of {lane}");
                previous = gap;
            }
            Assert.AreEqual(0f, previous);
        }

        [Test]
        public void CoverBesideALongLaneClosesTheGapToAFiringLine()
        {
            var wanted = new RoomTraitPriorities(TraitPriority.Low, TraitPriority.Low, TraitPriority.High);
            var killingField = new RoomTraitReading(0f, 0f, 12, new RectInt(0, 0, 12, 1), false);
            var firingLine = new RoomTraitReading(0f, 0f, 12, new RectInt(0, 0, 12, 1), true);

            Assert.Greater(RoomTraitFit.Gap(killingField, wanted), RoomTraitFit.Gap(firingLine, wanted));
            Assert.AreEqual(0f, RoomTraitFit.Gap(firingLine, wanted));
        }

        [Test]
        public void ALevelIsTheWholePartOfItsPosition()
        {
            RoomTraitReading[] readings =
            {
                CoveredCorridor,
                new(0.06f, 0.15f, 6, default, false),
                new(0.03f, 0.2f, 7, default, true),
                new(1f, 1f, 40, default, true),
                new(0f, 0f, 0, default, false),
            };
            foreach (RoomTraitReading reading in readings)
                foreach (RoomTrait trait in new[] { RoomTrait.Concealment, RoomTrait.Walls, RoomTrait.LinesOfFire })
                {
                    float position = reading.PositionOf(trait);
                    Assert.GreaterOrEqual(position, 0f);
                    Assert.Less(position, 3f);
                    Assert.AreEqual((int)Mathf.Floor(position), (int)reading.LevelOf(trait));
                }
        }

        [Test]
        public void ABareStructureIsNotScoredOnConcealment()
        {
            var wanted = new RoomTraitPriorities(TraitPriority.High, TraitPriority.High, TraitPriority.High);
            Assert.AreEqual(0, RoomTraitFit.Miss(CoveredCorridor, wanted, countConcealment: false));
        }

        [Test]
        public void TheFirstTryIsTheRoomsOwnSeed()
        {
            Seed seed = Seed.From(42);
            Assert.AreEqual(seed, RoomTraitFit.Try(seed, 0));
            Assert.AreNotEqual(seed, RoomTraitFit.Try(seed, 1));
            Assert.AreNotEqual(RoomTraitFit.Try(seed, 1), RoomTraitFit.Try(seed, 2));
        }

        [Test]
        public void OnlyRoomsWithEnemiesAreFightRooms()
        {
            Assert.IsFalse(RoomTraitFit.IsFightRoom(null));
            Assert.IsFalse(RoomTraitFit.IsFightRoom(new RoomContentRule { EnemyCount = new IntRange(0, 0) }));
            Assert.IsTrue(RoomTraitFit.IsFightRoom(new RoomContentRule { EnemyCount = new IntRange(0, 2) }));
        }

        [Test]
        public void ACopyCanBeFurnishedWithoutTouchingTheOriginal()
        {
            RoomStructure room = Room(8, 8, new Vector2Int(7, 4));
            RoomStructure copy = room.Copy();

            Assert.IsTrue(copy.TryClaim(new[] { new Vector2Int(1, 1) }, RoomTileTags.Prop));
            Assert.IsTrue(copy.Has(new Vector2Int(1, 1), RoomTileTags.Prop));
            Assert.IsFalse(room.Has(new Vector2Int(1, 1), RoomTileTags.Prop));
            Assert.AreEqual(room.Entrances.Count, copy.Entrances.Count);
            Assert.AreEqual(TilesWith(room, RoomTileTags.Walkway), TilesWith(copy, RoomTileTags.Walkway));
        }

        [TestCase(TraitPriority.Low, 2)]
        [TestCase(TraitPriority.Medium, 4)]
        [TestCase(TraitPriority.High, 6)]
        public void ConcealmentPropsScaleWithTheConcealmentPriority(TraitPriority priority, int wanted)
        {
            var patch = new GameObject("Patch", typeof(BoxCollider), typeof(ConcealmentZone));
            try
            {
                RoomStructure room = Room(12, 12, new Vector2Int(11, 6));
                var planner = new RoomPropPlanner(room, room.Footprint.Anchor, Seed.From(1).Stream(),
                                                  concealmentScale: RoomTraitFit.ConcealmentScale(priority));
                planner.Add(new RoomPropPlacement { Prefabs = new[] { patch }, Count = new IntRange(4, 4), Decor = true });

                Assert.AreEqual(wanted, planner.Outcomes[0].Wanted);
            }
            finally
            {
                Object.DestroyImmediate(patch);
            }
        }

        [Test]
        public void WhatARoomTypeNeedsKeepsItsConcealment()
        {
            var patch = new GameObject("Patch", typeof(BoxCollider), typeof(ConcealmentZone));
            try
            {
                RoomStructure room = Room(12, 12, new Vector2Int(11, 6));
                var planner = new RoomPropPlanner(room, room.Footprint.Anchor, Seed.From(1).Stream(), concealmentScale: 0.5f);
                planner.Add(new RoomPropPlacement { Prefabs = new[] { patch }, Count = new IntRange(4, 4), Decor = true }, exactConcealment: true);

                Assert.AreEqual(4, planner.Outcomes[0].Wanted);
            }
            finally
            {
                Object.DestroyImmediate(patch);
            }
        }

        [Test]
        public void OtherPropsIgnoreTheConcealmentPriority()
        {
            RoomStructure room = Room(12, 12, new Vector2Int(11, 6));
            var planner = new RoomPropPlanner(room, room.Footprint.Anchor, Seed.From(1).Stream(), concealmentScale: 1.5f);
            planner.Add(Placement(4, 4));

            Assert.AreEqual(4, planner.Outcomes[0].Wanted);
        }
    }
}
