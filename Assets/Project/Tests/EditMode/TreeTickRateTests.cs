using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    public class TreeTickRateTests
    {
        [Test]
        public void EngagedEnemiesThinkEveryFrame()
        {
            Assert.AreEqual(0f, TreeTickRate.IntervalFor(engaged: true, hostilesNearby: false));
            Assert.AreEqual(0f, TreeTickRate.IntervalFor(engaged: true, hostilesNearby: true));
        }

        [Test]
        public void CalmEnemiesThinkLessOftenTheFurtherAwayEveryoneIs()
        {
            float nearby = TreeTickRate.IntervalFor(engaged: false, hostilesNearby: true);
            float remote = TreeTickRate.IntervalFor(engaged: false, hostilesNearby: false);
            Assert.Greater(nearby, 0f);
            Assert.Greater(remote, nearby);
        }
    }
}
