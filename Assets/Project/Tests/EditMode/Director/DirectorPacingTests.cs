using System.Collections.Generic;
using NUnit.Framework;
using CGD.Core;
using CGD.Director;

namespace CGD.Tests
{
    public class DirectorPacingTests
    {
        private static IntensityMeter Meter() => new(new IntensityTuning(decayDelay: 2f, decayRate: 0.5f));

        // Peak at 0.75, held 4 s, calm below 0.25 for exactly 10 s, opening calm 5 s.
        private static PacingCycle Cycle() =>
            new(new PacingTuning(0.75f, 4f, 0.25f, new FloatRange(10f, 10f), 5f), new RandomStream(new Seed(1)));

        [Test]
        public void StressAddsUpToFull()
        {
            IntensityMeter meter = Meter();

            meter.Add(0.4f);
            meter.Add(0.4f);
            Assert.AreEqual(0.8f, meter.Value, 0.0001f);

            meter.Add(5f);
            Assert.AreEqual(1f, meter.Value);
        }

        [Test]
        public void IntensityHoldsUntilTheDecayDelayHasPassed()
        {
            IntensityMeter meter = Meter();
            meter.Add(1f);

            meter.Tick(1.5f, inCombat: false);
            Assert.AreEqual(1f, meter.Value);

            meter.Tick(1f, inCombat: false);
            Assert.AreEqual(0.5f, meter.Value, 0.0001f);

            meter.Tick(10f, inCombat: false);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void IntensityNeverFallsMidFight()
        {
            IntensityMeter meter = Meter();
            meter.Add(1f);

            meter.Tick(10f, inCombat: true);
            Assert.AreEqual(1f, meter.Value);
        }

        [Test]
        public void NewStressRestartsTheDecayDelay()
        {
            IntensityMeter meter = Meter();
            meter.Add(0.5f);
            meter.Tick(1.5f, inCombat: false);

            meter.Add(0.1f);
            meter.Tick(1.5f, inCombat: false);
            Assert.AreEqual(0.6f, meter.Value, 0.0001f);
        }

        [Test]
        public void SessionOpensWithACalm()
        {
            PacingCycle cycle = Cycle();
            cycle.Start();

            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);
            Assert.IsFalse(cycle.AllowsPopulation);

            cycle.Tick(4f, 0f);
            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);

            cycle.Tick(1.5f, 0f);
            Assert.AreEqual(PacingPhase.BuildUp, cycle.Phase);
            Assert.IsTrue(cycle.AllowsPopulation);
        }

        [Test]
        public void RunsTheWholeCycleFromIntensity()
        {
            PacingCycle cycle = Cycle();
            var phases = new List<PacingPhase>();
            cycle.PhaseChanged += (_, next) => phases.Add(next);
            cycle.Start();

            cycle.Tick(6f, 0f);    // opening calm over
            cycle.Tick(1f, 0.5f);  // still building
            cycle.Tick(1f, 0.8f);  // peak reached
            Assert.IsTrue(cycle.AllowsPopulation);

            cycle.Tick(5f, 0.9f);  // held long enough
            Assert.AreEqual(PacingPhase.PeakFade, cycle.Phase);
            Assert.IsFalse(cycle.AllowsPopulation);

            cycle.Tick(30f, 0.5f); // fading never ends while the player is still stressed
            Assert.AreEqual(PacingPhase.PeakFade, cycle.Phase);

            cycle.Tick(1f, 0.2f);
            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);

            cycle.Tick(11f, 0f);
            CollectionAssert.AreEqual(
                new[] { PacingPhase.Relax, PacingPhase.BuildUp, PacingPhase.SustainPeak, PacingPhase.PeakFade, PacingPhase.Relax, PacingPhase.BuildUp },
                phases);
        }

        [Test]
        public void RestartReturnsToAFreshCalm()
        {
            PacingCycle cycle = Cycle();
            cycle.Start();
            cycle.Tick(6f, 0f);
            cycle.Tick(1f, 0.9f);
            Assert.AreEqual(PacingPhase.SustainPeak, cycle.Phase);

            cycle.Restart();
            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);
            Assert.AreEqual(0f, cycle.TimeInPhase);

            cycle.Tick(9f, 0f);
            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);
        }
    }
}
