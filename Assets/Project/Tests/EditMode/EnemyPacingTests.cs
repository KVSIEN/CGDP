using NUnit.Framework;
using CGD.Director;
using CGD.Enemies;

namespace CGD.Tests
{
    public class EnemyPacingTests
    {
        private static readonly EnemyPacingTuning Tuning = new();

        [Test]
        public void TheUndirectedStateChangesNothing()
        {
            EnemyPacingState state = EnemyPacingState.Undirected;
            Assert.IsFalse(state.IsDirected);
            Assert.AreEqual(PacingPhase.BuildUp, state.Phase);
            Assert.AreEqual(3, state.ScaleAttackers(3));
            Assert.AreEqual(1f, state.SearchScale);
            Assert.AreEqual(1f, state.FallBackScale);
            Assert.Greater(state.BoldMoves, 1000);
        }

        [Test]
        public void ThePeakAllowsMoreBoldMovesThanTheCalm()
        {
            EnemyPacingState peak = Tuning.StateFor(PacingPhase.SustainPeak, 1f, 0.8f);
            EnemyPacingState calm = Tuning.StateFor(PacingPhase.Relax, 1f, 0.1f);
            Assert.Greater(peak.BoldMoves, calm.BoldMoves);
            Assert.Greater(peak.AttackerScale, calm.AttackerScale);
            Assert.Greater(peak.SearchScale, calm.SearchScale);
            Assert.Less(peak.FallBackScale, calm.FallBackScale);
        }

        [Test]
        public void PressureScalesTheBoldMoveBudget()
        {
            int neutral = Tuning.StateFor(PacingPhase.SustainPeak, 1f, 0f).BoldMoves;
            int cruising = Tuning.StateFor(PacingPhase.SustainPeak, 1.35f, 0f).BoldMoves;
            int struggling = Tuning.StateFor(PacingPhase.SustainPeak, 0.7f, 0f).BoldMoves;
            Assert.Greater(cruising, neutral);
            Assert.Less(struggling, neutral);
        }

        [Test]
        public void AttackerLimitsNeverDropBelowOne()
        {
            var state = new EnemyPacingState(PacingPhase.Relax, 1f, 0f, 0, attackerScale: 0.1f, 1f, 1f);
            Assert.AreEqual(1, state.ScaleAttackers(2));
            Assert.AreEqual(0, state.BoldMoves);
        }

        [Test]
        public void ThePhaseAndStressAreCarriedThrough()
        {
            EnemyPacingState state = Tuning.StateFor(PacingPhase.PeakFade, 1.1f, 0.6f);
            Assert.IsTrue(state.IsDirected);
            Assert.AreEqual(PacingPhase.PeakFade, state.Phase);
            Assert.AreEqual(1.1f, state.Pressure, 0.0001f);
            Assert.AreEqual(0.6f, state.Intensity, 0.0001f);
        }
    }
}
