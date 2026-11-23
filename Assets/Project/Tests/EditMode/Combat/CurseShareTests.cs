using NUnit.Framework;
using CGD.Combat;

namespace CGD.Tests
{
    // The share of a hit a Curse turns into true damage lets that part through armour.
    public class CurseShareTests
    {
        [Test]
        public void ItsShareIgnoresArmourAndTheRestIsMitigated()
        {
            var hit = new DamageInfo(100f);
            Assert.AreEqual(50f, hit.ResolveDamage(100f), 0.001f);
            Assert.AreEqual(75f, hit.ResolveDamage(100f, 0.5f), 0.001f);
            Assert.AreEqual(100f, hit.ResolveDamage(100f, 1f), 0.001f);
        }

        [Test]
        public void TheShareIsClampedAndTrueDamageIsUnchanged()
        {
            Assert.AreEqual(100f, new DamageInfo(100f).ResolveDamage(100f, 3f), 0.001f);
            Assert.AreEqual(50f, new DamageInfo(100f).ResolveDamage(100f, -1f), 0.001f);
            Assert.AreEqual(100f, new DamageInfo(100f, type: DamageType.True).ResolveDamage(100f, 0.5f), 0.001f);
        }
    }
}
