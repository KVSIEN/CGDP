using NUnit.Framework;
using CGD.Player;

namespace CGD.Tests
{
    public class RestraintStateTests
    {
        private const float Frame = 0.02f;

        [Test]
        public void ItIsInactiveUntilBegun()
        {
            var state = new RestraintState();
            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(RestraintOutcome.None, state.Tick(Frame, tapped: true));
        }

        [Test]
        public void TappingEnoughTimesBreaksFree()
        {
            var state = new RestraintState();
            state.Begin(tapsRequired: 4, decayPerSecond: 0f, maxSeconds: 0f);

            for (int i = 0; i < 3; i++) Assert.AreEqual(RestraintOutcome.None, state.Tick(Frame, true));
            Assert.IsTrue(state.IsActive);

            Assert.AreEqual(RestraintOutcome.BrokeFree, state.Tick(Frame, true));
            Assert.IsFalse(state.IsActive);
        }

        [Test]
        public void ProgressRisesWithEachTap()
        {
            var state = new RestraintState();
            state.Begin(4, 0f, 0f);
            state.Tick(Frame, true);
            Assert.AreEqual(0.25f, state.Progress, 0.0001f);
        }

        [Test]
        public void StoppingLetsProgressDrainAfterTheGracePeriod()
        {
            var state = new RestraintState();
            state.Begin(4, 1f, 0f);
            state.Tick(Frame, true);
            for (int i = 0; i < 100; i++) state.Tick(Frame, false);     // two seconds without a tap

            Assert.AreEqual(0f, state.Progress);
            Assert.IsTrue(state.IsActive);
        }

        [Test]
        public void ItHoldsForeverWithoutATimeLimit()
        {
            var state = new RestraintState();
            state.Begin(4, 0f, 0f);

            for (int i = 0; i < 5000; i++) Assert.AreEqual(RestraintOutcome.None, state.Tick(Frame, false));
            Assert.IsTrue(state.IsActive);
        }

        [Test]
        public void ItLetsGoWhenTheTimeLimitRunsOut()
        {
            var state = new RestraintState();
            state.Begin(8, 0f, 1f);

            RestraintOutcome outcome = RestraintOutcome.None;
            for (int i = 0; i < 100 && outcome == RestraintOutcome.None; i++) outcome = state.Tick(Frame, false);

            Assert.AreEqual(RestraintOutcome.TimedOut, outcome);
            Assert.IsFalse(state.IsActive);
        }

        [Test]
        public void BeginningAgainStartsFromEmpty()
        {
            var state = new RestraintState();
            state.Begin(4, 0f, 0f);
            state.Tick(Frame, true);

            state.Begin(4, 0f, 0f);

            Assert.AreEqual(0f, state.Progress);
        }

        [Test]
        public void EndingClearsTheRestraint()
        {
            var state = new RestraintState();
            state.Begin(4, 0f, 0f);
            state.Tick(Frame, true);
            state.End();

            Assert.IsFalse(state.IsActive);
            Assert.AreEqual(0f, state.Progress);
        }
    }
}
