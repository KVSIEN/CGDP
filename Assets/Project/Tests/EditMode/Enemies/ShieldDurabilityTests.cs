using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    // When a carried shield breaks and when it is back up.
    public class ShieldDurabilityTests
    {
        [Test]
        public void ANoDurabilityShieldNeverBreaks()
        {
            var shield = new ShieldDurability(0f, 5f);
            Assert.IsFalse(shield.Absorb(10000f, 0f));
            Assert.IsTrue(shield.IsUp(0f));
            Assert.AreEqual(1f, shield.Share(0f));
        }

        [Test]
        public void ItBreaksOnceItHasStoppedItsDurability()
        {
            var shield = new ShieldDurability(100f, 5f);
            Assert.IsFalse(shield.Absorb(60f, 0f));
            Assert.AreEqual(0.4f, shield.Share(0f), 0.0001f);
            Assert.IsTrue(shield.Absorb(40f, 1f), "the hit that empties it breaks it");
            Assert.IsFalse(shield.IsUp(1f));
        }

        [Test]
        public void ABrokenShieldTakesNoMoreHitsUntilItIsBack()
        {
            var shield = new ShieldDurability(10f, 5f);
            shield.Absorb(10f, 0f);
            Assert.IsFalse(shield.Absorb(10f, 1f), "already down: it can't break again");
        }

        [Test]
        public void ItComesBackAtFullStrengthAfterItsDowntime()
        {
            var shield = new ShieldDurability(10f, 5f);
            shield.Absorb(10f, 0f);
            Assert.IsFalse(shield.IsUp(4.9f));
            Assert.IsTrue(shield.IsUp(5f));
            Assert.AreEqual(1f, shield.Share(5f));
        }

        [Test]
        public void RestoreBringsItBackAtOnce()
        {
            var shield = new ShieldDurability(10f, 5f);
            shield.Absorb(10f, 0f);
            shield.Restore();
            Assert.IsTrue(shield.IsUp(0.1f));
        }
    }
}
