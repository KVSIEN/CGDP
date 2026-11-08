using NUnit.Framework;
using CGD.DevTools;

namespace CGD.Tests
{
    public class MatchStatsTests
    {
        [Test]
        public void HitsCountForTheDealerAndAgainstTheTarget()
        {
            var stats = new MatchStats(2);
            stats.Hit(0, 1, 30f, killed: false, now: 1f);
            stats.Hit(0, 1, 20f, killed: false, now: 2f);

            Assert.AreEqual(2, stats[0].Hits);
            Assert.AreEqual(50f, stats[0].DamageDealt);
            Assert.AreEqual(50f, stats[1].DamageTaken);
            Assert.AreEqual(0, stats[1].Deaths);
        }

        [Test]
        public void AKillCountsOnceAndTimesFromTheVictimsFirstHit()
        {
            var stats = new MatchStats(2);
            stats.Hit(0, 1, 40f, false, now: 10f);
            stats.Hit(0, 1, 60f, true,  now: 13f);

            Assert.AreEqual(1, stats[0].Kills);
            Assert.AreEqual(1, stats[1].Deaths);
            Assert.AreEqual(3f, stats[0].AverageTimeToKill, 1e-4f);

            // Its next life starts untouched.
            stats.Hit(0, 1, 50f, false, now: 20f);
            stats.Hit(0, 1, 50f, true,  now: 21f);
            Assert.AreEqual(2f, stats[0].AverageTimeToKill, 1e-4f);
        }

        [Test]
        public void SomeoneElsesDamageGoesToOthers()
        {
            var stats = new MatchStats(1);
            stats.Hit(MatchStats.Nobody, 0, 25f, true, now: 5f);

            Assert.AreEqual(25f, stats.Others.DamageDealt);
            Assert.AreEqual(1, stats.Others.Kills);
            Assert.AreEqual(1, stats[0].Deaths);
            Assert.AreEqual(0f, stats.Others.AverageTimeToKill);   // only agents' kills are timed
        }

        [Test]
        public void AbilitiesAreCountedAndAResetClearsEverything()
        {
            var stats = new MatchStats(1);
            stats.AbilityUsed(0);
            stats.Hit(0, MatchStats.Nobody, 10f, false, now: 1f);

            stats.Reset(now: 30f);

            Assert.AreEqual(0, stats[0].AbilityUses);
            Assert.AreEqual(0f, stats[0].DamageDealt);
            Assert.AreEqual(0f, stats.Others.DamageTaken);
            Assert.AreEqual(5f, stats.Duration(35f), 1e-4f);
        }

        [Test]
        public void ZeroDamageHitsAreIgnored()
        {
            var stats = new MatchStats(2);
            stats.Hit(0, 1, 0f, false, now: 1f);
            Assert.AreEqual(0, stats[0].Hits);
        }
    }
}
