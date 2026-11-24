using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Level;
using CGD.Stealth;
using static CGD.Tests.RoomTestKit;
using Object = UnityEngine.Object;

namespace CGD.Tests
{
    // The readings that can see in every direction instead of along rows and columns
    // (IsovistUse): the traits, the planner that steers by them, where cover goes and where
    // enemies hold chokes and overwatch. Each is off unless asked for.
    public class IsovistReadingTests
    {
        private static readonly Vector2Int Door = new(11, 6);

        // A 2 × 2 cell in the corner of a 9 × 9 room, walled on two sides, with a doorway through its bottom-right.
        private static RoomStructure RoomWithACell()
        {
            RoomStructure room = Room(9, 9, new Vector2Int(8, 4));
            var walls = new List<PartitionEdge>
            {
                new(new Vector2Int(0, 1), new Vector2Int(0, 2), 3f, dividesZones: false),
                new(new Vector2Int(1, 1), new Vector2Int(1, 2), 3f, dividesZones: false),
                new(new Vector2Int(1, 1), new Vector2Int(2, 1), 3f, dividesZones: false),
            };
            Assert.That(room.TryAddPartition(walls), Is.True);
            return room;
        }

        [Test]
        public void NothingIsReadInEveryDirectionUnlessAsked()
        {
            var settings = ScriptableObject.CreateInstance<LevelBuildSettings>();
            try
            {
                Assert.That(settings.IsovistFor, Is.EqualTo(IsovistUse.None));
                Assert.That(settings.UsesIsovist(IsovistUse.Traits | IsovistUse.Posts | IsovistUse.Cover), Is.False);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }

        [Test]
        public void AnOpenHallsLongestSightlineIsItsDiagonal()
        {
            RoomStructure room = Room(12, 12, Door);

            RoomTraitReading rows = RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>());
            RoomTraitReading every = RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>(), isovist: true);

            Assert.That(rows.LongestLane, Is.EqualTo(12));
            Assert.That(every.LongestLane, Is.EqualTo(Mathf.RoundToInt(Mathf.Sqrt(11 * 11 + 11 * 11)) + 1), "corner to corner");
            Assert.That(every.NarrowShare, Is.Zero, "nothing is shut in");
            Assert.That(every.LaneHasCover, Is.False);
        }

        [Test]
        public void CoverBesideTheLongestLineMakesItAFiringLine()
        {
            RoomStructure room = Room(12, 12, Door);
            Assert.That(room.TryClaim(new[] { new Vector2Int(5, 4) }, RoomTileTags.Prop), Is.True, "beside the main diagonal");

            RoomTraitReading every = RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>(), isovist: true);

            Assert.That(every.LaneHasCover, Is.True);
            Assert.That(every.Lane.width, Is.GreaterThan(1));
            Assert.That(every.Lane.height, Is.GreaterThan(1), "the lane is the box the line runs through");
        }

        [Test]
        public void AShutInCornerCountsAsWalls()
        {
            RoomStructure room = RoomWithACell();

            Assert.That(RoomTraitMeasure.Measure(room, Array.Empty<PlannedProp>(), isovist: true).NarrowShare, Is.GreaterThan(0f));
        }

        [Test]
        public void ConcealmentReadsTheSameEitherWay()
        {
            var patch = new GameObject("Patch", typeof(BoxCollider), typeof(ConcealmentZone));
            try
            {
                RoomStructure room = Room(10, 10, new Vector2Int(9, 5));
                var props = new[] { new PlannedProp(patch, new Vector2(1f, 1f), 0f, new RectInt(0, 0, 2, 2), decor: true) };

                float rows = RoomTraitMeasure.Measure(room, props).ConcealedShare;
                float every = RoomTraitMeasure.Measure(room, props, isovist: true).ConcealedShare;

                Assert.That(every, Is.EqualTo(rows).Within(1e-5f));
                Assert.That(every, Is.EqualTo(0.04f).Within(1e-3f));
            }
            finally
            {
                Object.DestroyImmediate(patch);
            }
        }

        [Test]
        public void ThePlannerReadsItsTraitsInEveryDirectionAsPropsMove()
        {
            for (int seed = 1; seed <= 10; seed++)
            {
                RoomStructure room = Room(12, 12, Door);
                var planner = new RoomPropPlanner(room, room.Footprint.Anchor, new RandomStream(new Seed((ulong)seed)), isovist: IsovistUse.Traits);
                planner.Add(Placement(8, 8), refinable: true);
                var random = new RandomStream(new Seed((ulong)seed + 100));
                for (int piece = 0; piece < planner.MovableCount; piece++)
                    planner.TryMove(piece, random, out _);

                RoomTraitReading kept = planner.Traits;
                RoomTraitReading fresh = RoomTraitMeasure.Measure(room, planner.Planned, isovist: true);

                Assert.That(kept.NarrowShare, Is.EqualTo(fresh.NarrowShare).Within(1e-5f), $"seed {seed}");
                Assert.That(kept.LongestLane, Is.EqualTo(fresh.LongestLane), $"seed {seed}");
                Assert.That(kept.LaneHasCover, Is.EqualTo(fresh.LaneHasCover), $"seed {seed}");
            }
        }

        [Test]
        public void CoverGoesWhereItBreaksTheLongestSightlineWhenAsked()
        {
            var corners = new HashSet<Vector2Int> { new(0, 0), new(11, 0), new(0, 11), new(11, 11) };
            for (int seed = 1; seed <= 20; seed++)
            {
                RoomStructure room = Room(12, 12, Door);
                var planner = new RoomPropPlanner(room, room.Footprint.Anchor, new RandomStream(new Seed((ulong)seed)), isovist: IsovistUse.Cover);
                RoomPropPlacement cover = Placement(1, 1);
                cover.Selection = PropSelection.Cover;
                planner.Add(cover);

                Assert.That(planner.Planned, Has.Count.EqualTo(1), $"seed {seed}");
                Assert.That(corners, Does.Contain(TileOf(planner.Planned[0])), $"seed {seed}: the corners see farthest");
            }
        }

        [Test]
        public void EnemiesHoldChokesAndOverwatchByWhatTheyCanSeeWhenAsked()
        {
            RoomStructure cell = RoomWithACell();
            var cellPosts = new RoomPostMap(cell, Array.Empty<PlannedProp>(), isovist: true);
            Assert.That(cellPosts.KindsAt(new Vector2Int(0, 1)) & RoomPostKinds.Choke, Is.EqualTo(RoomPostKinds.Choke), "a tile that sees little");
            Assert.That(cellPosts.KindsAt(new Vector2Int(5, 6)) & RoomPostKinds.Choke, Is.EqualTo(RoomPostKinds.None), "open floor");

            RoomStructure hall = Room(12, 12, Door);
            Assert.That(hall.TryClaim(new[] { new Vector2Int(5, 4) }, RoomTileTags.Prop), Is.True);
            var hallPosts = new RoomPostMap(hall, Array.Empty<PlannedProp>(), isovist: true);
            Assert.That(hallPosts.KindsAt(new Vector2Int(4, 4)) & RoomPostKinds.Overwatch, Is.EqualTo(RoomPostKinds.Overwatch), "cover beside, a long view");
        }
    }
}
