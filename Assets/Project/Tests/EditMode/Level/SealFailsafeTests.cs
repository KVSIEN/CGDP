using System.Collections.Generic;
using NUnit.Framework;
using CGD.Level;

namespace CGD.Tests
{
    // A sealed room must not stay shut for good: an enemy stranded out of reach is let go after
    // a while, and a fight with no kills for long enough lets everyone go.
    public class SealFailsafeTests
    {
        private static readonly List<string> Two = new() { "stuck", "free" };

        [Test]
        public void AStrandedMemberIsLetGoOnlyAfterItsTime()
        {
            var failsafe = new SealFailsafe<string>(strandedSeconds: 20f, stallSeconds: 1000f);
            for (int second = 1; second < 20; second++)
                Assert.IsEmpty(failsafe.Advance(1f, Two, m => m == "stuck"), $"let go after {second} s");

            CollectionAssert.AreEqual(new[] { "stuck" }, failsafe.Advance(1f, Two, m => m == "stuck"));
            Assert.IsFalse(failsafe.Overridden);
        }

        [Test]
        public void ReachingTheEnemyAgainRestartsItsClock()
        {
            var failsafe = new SealFailsafe<string>(10f, 1000f);
            for (int i = 0; i < 9; i++) failsafe.Advance(1f, Two, m => m == "stuck");
            failsafe.Advance(1f, Two, _ => false);   // reachable for a moment

            for (int i = 0; i < 9; i++) Assert.IsEmpty(failsafe.Advance(1f, Two, m => m == "stuck"));
            Assert.AreEqual(1, failsafe.Advance(1f, Two, m => m == "stuck").Count);
        }

        [Test]
        public void ReachableEnemiesAreNeverLetGoWhileTheFightProgresses()
        {
            var failsafe = new SealFailsafe<string>(10f, 60f);
            var alive = new List<string> { "a", "b", "c", "d", "e" };
            for (int second = 0; second < 200 && alive.Count > 1; second++)
            {
                Assert.IsEmpty(failsafe.Advance(1f, alive, _ => false));
                if (second % 30 == 29) alive.RemoveAt(0);   // a kill every 30 s
            }
            Assert.IsFalse(failsafe.Overridden);
        }

        [Test]
        public void NoKillsForTheStallTimeLetsEveryoneGo()
        {
            var failsafe = new SealFailsafe<string>(1000f, 90f);
            for (int second = 1; second < 90; second++) Assert.IsEmpty(failsafe.Advance(1f, Two, _ => false));

            List<string> released = failsafe.Advance(1f, Two, _ => false);
            CollectionAssert.AreEquivalent(Two, released);
            Assert.IsTrue(failsafe.Overridden);
        }

        [Test]
        public void AKillRestartsTheStallClock()
        {
            var failsafe = new SealFailsafe<string>(1000f, 90f);
            for (int second = 0; second < 80; second++) failsafe.Advance(1f, Two, _ => false);

            var one = new List<string> { "free" };
            for (int second = 0; second < 80; second++) Assert.IsEmpty(failsafe.Advance(1f, one, _ => false));
            Assert.IsFalse(failsafe.Overridden);
        }

        [Test]
        public void NothingToWaitOnReleasesNothing()
        {
            var failsafe = new SealFailsafe<string>(5f, 30f);
            Assert.IsEmpty(failsafe.Advance(100f, new List<string>(), _ => true));
            Assert.IsFalse(failsafe.Overridden);
        }
    }
}
