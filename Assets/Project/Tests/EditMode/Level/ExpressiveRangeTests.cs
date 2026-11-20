using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Editor;
using CGD.Level;

namespace CGD.Tests
{
    // The Room Sweep's expressive range: rooms land in the cells their measures put them in,
    // the spread counts the cells reached, and overlap says how alike two sets of rooms are.
    public class ExpressiveRangeTests
    {
        private static RoomSweepResult Room(float concealed, int lane) =>
            new("Place", "Faction", "seed", new RoomTraitReading(concealed, 0f, lane, default, false), null, 0, 1, default, null);

        [Test]
        public void RoomsLandInTheCellsTheirMeasuresPutThemIn()
        {
            var range = new ExpressiveRange(new[] { Room(0.05f, 0), Room(0.95f, RoomMeasures.LaneCap * 3), Room(0.05f, 1) },
                                            RoomMeasure.Concealed, RoomMeasure.Lane);

            Assert.That(range.Total, Is.EqualTo(3));
            Assert.That(range.Count(0, 0), Is.EqualTo(2));
            Assert.That(range.Count(ExpressiveRange.Bins - 1, ExpressiveRange.Bins - 1), Is.EqualTo(1), "a lane past the cap plots at the top");
            Assert.That(range.Max, Is.EqualTo(2));
            Assert.That(range.Coverage, Is.EqualTo(2f / (ExpressiveRange.Bins * ExpressiveRange.Bins)).Within(1e-6f));
        }

        [Test]
        public void TheSameSpreadOverlapsFullyAndASeparateOneNotAtAll()
        {
            var low = new List<RoomSweepResult> { Room(0.05f, 0), Room(0.3f, 2) };
            var lowAgain = new List<RoomSweepResult> { Room(0.06f, 0), Room(0.31f, 2), Room(0.07f, 0), Room(0.32f, 2) };
            var high = new List<RoomSweepResult> { Room(0.9f, 16) };

            ExpressiveRange Range(IEnumerable<RoomSweepResult> rooms) => new(rooms, RoomMeasure.Concealed, RoomMeasure.Lane);
            Assert.That(ExpressiveRange.Overlap(Range(low), Range(lowAgain)), Is.EqualTo(1f).Within(1e-5f));
            Assert.That(ExpressiveRange.Overlap(Range(low), Range(high)), Is.Zero);
            Assert.That(ExpressiveRange.Overlap(Range(low), Range(new List<RoomSweepResult>())), Is.Zero);
        }

        [Test]
        public void ARoomWithNoWayInIsLeftOffWhatTheDoorwayReveals()
        {
            var range = new ExpressiveRange(new[] { Room(0.5f, 4) }, RoomMeasure.Concealed, RoomMeasure.Revealed);
            Assert.That(range.Total, Is.Zero);
        }
    }
}
