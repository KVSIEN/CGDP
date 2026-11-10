using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    public class AwarenessMeterTests
    {
        [Test]
        public void StartsUnawareAndBecomesSuspiciousAtTheThreshold()
        {
            var meter = new AwarenessMeter(0.3f);
            Assert.IsFalse(meter.IsSuspicious);

            meter.Raise(0.29f);
            Assert.IsFalse(meter.IsSuspicious);

            meter.Raise(0.02f);
            Assert.IsTrue(meter.IsSuspicious);
            Assert.IsFalse(meter.IsFull);
        }

        [Test]
        public void HearingCeilingKeepsItFromBeingCertain()
        {
            var meter = new AwarenessMeter(0.3f);
            for (int i = 0; i < 20; i++) meter.Raise(0.2f, ceiling: 0.9f);

            Assert.AreEqual(0.9f, meter.Value, 0.0001f);
            Assert.IsFalse(meter.IsFull);
        }

        [Test]
        public void RaisingBelowTheCeilingDoesNotLowerAHigherValue()
        {
            var meter = new AwarenessMeter(0.3f);
            meter.Fill();
            meter.Raise(0.5f, ceiling: 0.9f);

            Assert.IsTrue(meter.IsFull);
        }

        [Test]
        public void DecayCalmsItDownToZero()
        {
            var meter = new AwarenessMeter(0.3f);
            meter.Fill();
            meter.Decay(0.75f);
            Assert.IsFalse(meter.IsSuspicious);

            meter.Decay(1f);
            Assert.AreEqual(0f, meter.Value);
        }

        [Test]
        public void AnAlarmMakesItJustSuspicious()
        {
            var meter = new AwarenessMeter(0.3f);
            meter.RaiseToSuspicious();

            Assert.IsTrue(meter.IsSuspicious);
            Assert.Less(meter.Value, 0.5f);
        }
    }
}
