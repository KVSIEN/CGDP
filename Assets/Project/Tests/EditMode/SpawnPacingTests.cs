using NUnit.Framework;
using CGD.Level;

namespace CGD.Tests
{
    public class SpawnPacingTests
    {
        [Test]
        public void FirstBatchIsDueStraightAway()
        {
            var pacing = new SpawnPacing(interval: 5f, batch: 2, cap: 6);
            Assert.AreEqual(2, pacing.Advance(0.016f, alive: 0));
        }

        [Test]
        public void NextBatchWaitsForTheInterval()
        {
            var pacing = new SpawnPacing(5f, 2, 6);
            pacing.Advance(0.016f, 0);

            Assert.AreEqual(0, pacing.Advance(4f, 2));
            Assert.AreEqual(2, pacing.Advance(1.1f, 2));
        }

        [Test]
        public void NeverSendsPastTheCap()
        {
            var pacing = new SpawnPacing(1f, 4, 5);

            Assert.AreEqual(4, pacing.Advance(0.016f, 0));
            Assert.AreEqual(1, pacing.Advance(1.1f, 4));
            Assert.AreEqual(0, pacing.Advance(10f, 5));
        }

        [Test]
        public void TimerPausesWhileAtTheCap()
        {
            var pacing = new SpawnPacing(5f, 2, 2);
            pacing.Advance(0.016f, 0);

            Assert.AreEqual(0, pacing.Advance(100f, alive: 2));
            // Room has opened, but the 100 s spent at the cap didn't count towards the interval.
            Assert.AreEqual(0, pacing.Advance(0.016f, alive: 1));
        }
    }
}
