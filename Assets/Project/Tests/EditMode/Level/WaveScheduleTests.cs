using NUnit.Framework;
using CGD.Level;

namespace CGD.Tests
{
    public class WaveScheduleTests
    {
        [Test]
        public void FirstWaveComesAtTheStart()
        {
            var schedule = new WaveSchedule(duration: 60f, interval: 15f);
            Assert.AreEqual(1, schedule.Advance(0.016f));
        }

        [Test]
        public void LongFrameDoesNotSkipWaves()
        {
            var schedule = new WaveSchedule(60f, 15f);
            Assert.AreEqual(3, schedule.Advance(30f));
        }

        [Test]
        public void NoWavesAfterTheEnd()
        {
            var schedule = new WaveSchedule(30f, 15f);
            schedule.Advance(30f);

            Assert.IsTrue(schedule.IsFinished);
            Assert.AreEqual(0, schedule.Advance(10f));
            Assert.AreEqual(schedule.TotalWaves, schedule.Spawned);
        }

        [Test]
        public void RemainingCountsDown()
        {
            var schedule = new WaveSchedule(60f, 15f);
            schedule.Advance(20f);
            Assert.AreEqual(40f, schedule.Remaining, 0.001f);
        }
    }
}
