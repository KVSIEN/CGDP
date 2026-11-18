using NUnit.Framework;
using CGD.Enemies;

namespace CGD.Tests
{
    public class RecoveryPoolTests
    {
        [Test]
        public void Bank_OwesAShareOfTheDamage()
        {
            var pool = new RecoveryPool(share: 0.5f, ratePerSecond: 10f, cap: 100f);
            pool.Bank(40f);
            Assert.That(pool.Owed, Is.EqualTo(20f).Within(0.001f));
        }

        [Test]
        public void Bank_StopsAtTheCap()
        {
            var pool = new RecoveryPool(share: 1f, ratePerSecond: 10f, cap: 30f);
            pool.Bank(25f);
            pool.Bank(25f);
            Assert.That(pool.Owed, Is.EqualTo(30f).Within(0.001f));
        }

        [Test]
        public void Pay_GivesTheRateOverTimeThenStops()
        {
            var pool = new RecoveryPool(share: 1f, ratePerSecond: 10f, cap: 100f);
            pool.Bank(15f);
            Assert.That(pool.Pay(1f), Is.EqualTo(10f).Within(0.001f));
            Assert.That(pool.Pay(1f), Is.EqualTo(5f).Within(0.001f));
            Assert.That(pool.Pay(1f), Is.EqualTo(0f).Within(0.001f));
        }

        [Test]
        public void Clear_ForgetsWhatIsOwed()
        {
            var pool = new RecoveryPool(share: 1f, ratePerSecond: 10f, cap: 100f);
            pool.Bank(30f);
            pool.Clear();
            Assert.That(pool.Owed, Is.Zero);
        }
    }
}
