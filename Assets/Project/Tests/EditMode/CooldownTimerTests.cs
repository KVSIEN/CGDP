using NUnit.Framework;
using CGD.Core;

namespace CGD.Tests
{
    public class CooldownTimerTests
    {
        [Test]
        public void NewTimerIsReady()
        {
            var timer = new CooldownTimer();

            Assert.IsTrue(timer.IsReady);
            Assert.AreEqual(1f, timer.Ratio);
        }

        [Test]
        public void StartedTimerCountsDownToReady()
        {
            var timer = new CooldownTimer();
            timer.Start(2f);

            Assert.IsFalse(timer.IsReady);
            Assert.AreEqual(0f, timer.Ratio, 0.0001f);

            timer.Tick(1f);
            Assert.AreEqual(0.5f, timer.Ratio, 0.0001f);
            Assert.AreEqual(1f, timer.Remaining, 0.0001f);

            timer.Tick(1.5f);
            Assert.IsTrue(timer.IsReady);
            Assert.AreEqual(0f, timer.Remaining);
        }

        [Test]
        public void ResetMakesItReadyImmediately()
        {
            var timer = new CooldownTimer();
            timer.Start(5f);
            timer.Reset();

            Assert.IsTrue(timer.IsReady);
        }

        [Test]
        public void ZeroDurationIsAlwaysReady()
        {
            var timer = new CooldownTimer();
            timer.Start(0f);

            Assert.IsTrue(timer.IsReady);
            Assert.AreEqual(1f, timer.Ratio);
        }
    }
}
