using NUnit.Framework;
using CGD.Interaction;

namespace CGD.Tests
{
    public class EffortMeterTests
    {
        private const float Frame = 0.1f;

        [Test]
        public void HoldingFillsTheBarAndCompletesOnce()
        {
            var meter = new EffortMeter();

            Assert.IsFalse(meter.Hold(true, 1f, 0.5f));
            Assert.AreEqual(0.5f, meter.Progress, 0.0001f);
            Assert.IsTrue(meter.Hold(true, 1f, 0.5f));
            Assert.AreEqual(0f, meter.Progress);

            // Still held: it does not complete again until the key is released.
            Assert.IsFalse(meter.Hold(true, 1f, 5f));
        }

        [Test]
        public void ReleasingAHoldLosesItsProgressAndAllowsAnotherCompletion()
        {
            var meter = new EffortMeter();
            meter.Hold(true, 1f, 0.6f);
            meter.Hold(false, 1f, Frame);
            Assert.AreEqual(0f, meter.Progress);

            Assert.IsTrue(meter.Hold(true, 1f, 1f));
            meter.Hold(false, 1f, Frame);
            Assert.IsTrue(meter.Hold(true, 1f, 1f));
        }

        [Test]
        public void EachTapAddsAnEqualShare()
        {
            var meter = new EffortMeter();
            meter.Tap(4);
            Assert.AreEqual(0.25f, meter.Progress, 0.0001f);
            meter.Tap(4);
            Assert.AreEqual(0.5f, meter.Progress, 0.0001f);
        }

        [Test]
        public void TheLastTapCompletesAndStartsOver()
        {
            var meter = new EffortMeter();
            for (int i = 0; i < 4; i++) Assert.IsFalse(meter.Tap(5));

            Assert.IsTrue(meter.Tap(5));
            Assert.AreEqual(0f, meter.Progress);
        }

        [Test]
        public void ThirdsAddUpToOneDespiteFloatingPointError()
        {
            var meter = new EffortMeter();
            Assert.IsFalse(meter.Tap(3));
            Assert.IsFalse(meter.Tap(3));
            Assert.IsTrue(meter.Tap(3));
        }

        [Test]
        public void ASingleTapEffortCompletesOnTheFirstTap()
        {
            Assert.IsTrue(new EffortMeter().Tap(1));
            Assert.IsTrue(new EffortMeter().Tap(0));
        }

        [Test]
        public void TheBarDoesNotDrainRightAfterATap()
        {
            var meter = new EffortMeter();
            meter.Tap(4);

            meter.Drain(1f, EffortMeter.DrainDelay * 0.9f);

            Assert.AreEqual(0.25f, meter.Progress, 0.0001f);
        }

        [Test]
        public void TheBarDrainsOncePlayerStopsTapping()
        {
            var meter = new EffortMeter();
            meter.Tap(2);
            meter.Drain(1f, EffortMeter.DrainDelay);   // the delay has now passed
            meter.Drain(1f, 0.2f);

            Assert.AreEqual(0.3f, meter.Progress, 0.0001f);
        }

        [Test]
        public void AnotherTapRestartsTheDelay()
        {
            var meter = new EffortMeter();
            meter.Tap(4);
            meter.Drain(1f, 1f);
            float afterDrain = meter.Progress;

            meter.Tap(4);
            meter.Drain(1f, EffortMeter.DrainDelay * 0.5f);

            Assert.AreEqual(afterDrain + 0.25f, meter.Progress, 0.0001f);
        }

        [Test]
        public void TheBarNeverDrainsBelowZeroOrWithoutDecay()
        {
            var meter = new EffortMeter();
            meter.Tap(4);
            meter.Drain(0f, 10f);
            Assert.AreEqual(0.25f, meter.Progress, 0.0001f);

            meter.Drain(5f, 10f);
            Assert.AreEqual(0f, meter.Progress);
        }

        [Test]
        public void ResetClearsEverything()
        {
            var meter = new EffortMeter();
            meter.Tap(4);
            meter.Hold(true, 1f, 0.5f);
            meter.Reset();

            Assert.AreEqual(0f, meter.Progress);
        }
    }
}
