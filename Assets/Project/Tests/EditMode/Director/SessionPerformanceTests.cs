using NUnit.Framework;
using CGD.Core;
using CGD.Director;

namespace CGD.Tests
{
    public class SessionPerformanceTests
    {
        // Pressure 0.5–1.5, easy at 60%+ health, hard at 30% or less, steps of 0.5, death 1.
        private static SessionPerformance Performance() =>
            new(new PerformanceTuning(0.5f, 1.5f, 0.6f, 0.3f, 0.5f, 1f));

        [Test]
        public void StartsNeutral()
        {
            SessionPerformance performance = Performance();

            Assert.AreEqual(0f, performance.Rating);
            Assert.AreEqual(1f, performance.Pressure);
        }

        [Test]
        public void EasyPeaksRaisePressureUpToTheMaximum()
        {
            SessionPerformance performance = Performance();

            performance.RecordPeak(0.9f);
            Assert.AreEqual(1.25f, performance.Pressure, 0.0001f);

            performance.RecordPeak(0.7f);
            performance.RecordPeak(1f);
            Assert.AreEqual(1.5f, performance.Pressure, 0.0001f);
        }

        [Test]
        public void HardPeaksAndDeathsLowerPressureDownToTheMinimum()
        {
            SessionPerformance performance = Performance();

            performance.RecordPeak(0.1f);
            Assert.AreEqual(0.75f, performance.Pressure, 0.0001f);

            performance.RecordDeath();
            Assert.AreEqual(0.5f, performance.Pressure, 0.0001f);
        }

        [Test]
        public void MiddlingPeaksChangeNothing()
        {
            SessionPerformance performance = Performance();

            performance.RecordPeak(0.45f);
            Assert.AreEqual(1f, performance.Pressure);
        }

        [Test]
        public void RelaxScaleStretchesTheNextCalm()
        {
            var cycle = new PacingCycle(new PacingTuning(0.75f, 1f, 0.25f, new FloatRange(10f, 10f), 0f), new RandomStream(new Seed(1)));
            cycle.Start();
            cycle.Tick(0.1f, 0f);  // opening calm over
            cycle.Tick(0.1f, 1f);  // peak
            cycle.Tick(2f, 1f);    // fade

            cycle.RelaxScale = 2f;
            cycle.Tick(0.1f, 0f);  // calm, rolled at 10 × 2
            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);

            cycle.Tick(15f, 0f);
            Assert.AreEqual(PacingPhase.Relax, cycle.Phase);

            cycle.Tick(6f, 0f);
            Assert.AreEqual(PacingPhase.BuildUp, cycle.Phase);
        }
    }
}
