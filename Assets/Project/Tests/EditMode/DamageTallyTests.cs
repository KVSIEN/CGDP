using NUnit.Framework;
using CGD.FiringRange;

namespace CGD.Tests
{
    public class DamageTallyTests
    {
        [Test]
        public void CountsHitsHeadshotsAndDamage()
        {
            var tally = new DamageTally();
            tally.Record(0f, 20f, false, false);
            tally.Record(0.5f, 40f, true, false);

            Assert.AreEqual(2, tally.Hits);
            Assert.AreEqual(1, tally.Headshots);
            Assert.AreEqual(60f, tally.Damage, 0.001f);
        }

        [Test]
        public void RateLeavesOutTheFinalHit()
        {
            var tally = new DamageTally();
            tally.Record(0f, 10f, false, false);
            tally.Record(1f, 10f, false, false);
            tally.Record(2f, 50f, false, false);

            Assert.AreEqual(10f, tally.DamagePerSecond, 0.001f);
        }

        [Test]
        public void SingleHitHasNoRate()
        {
            var tally = new DamageTally();
            tally.Record(4f, 30f, false, false);

            Assert.AreEqual(0f, tally.DamagePerSecond);
        }

        [Test]
        public void ReportsTimeAndHitsToKill()
        {
            var tally = new DamageTally();
            tally.Record(1f, 50f, false, false);
            tally.Record(1.8f, 50f, false, true);

            Assert.IsTrue(tally.Killed);
            Assert.AreEqual(0.8f, tally.Duration, 0.001f);
            Assert.AreEqual(2, tally.Hits);
        }

        [Test]
        public void HitAfterAKillStartsANewFight()
        {
            var tally = new DamageTally();
            tally.Record(0f, 100f, false, true);
            tally.Record(0.2f, 25f, false, false);

            Assert.AreEqual(1, tally.Hits);
            Assert.AreEqual(25f, tally.Damage, 0.001f);
            Assert.IsFalse(tally.Killed);
        }

        [Test]
        public void PauseInShootingStartsANewFight()
        {
            var tally = new DamageTally();
            tally.Record(0f, 20f, false, false);
            tally.Record(DamageTally.IdleReset + 1f, 20f, false, false);

            Assert.AreEqual(1, tally.Hits);
        }

        [Test]
        public void ClearForgetsTheFight()
        {
            var tally = new DamageTally();
            tally.Record(0f, 20f, true, true);
            tally.Clear();

            Assert.AreEqual(0, tally.Hits);
            Assert.AreEqual(0, tally.Headshots);
            Assert.AreEqual(0f, tally.Damage);
            Assert.IsFalse(tally.Killed);
        }
    }
}
