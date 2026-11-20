using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // An enemy's hop over low cover (EnemyVault): it leaves from one end, lands on the other and
    // peaks over the middle.
    public class EnemyVaultTests
    {
        [Test]
        public void TheHopRunsEndToEndAndPeaksOverTheMiddle()
        {
            var from = new Vector3(0f, 0f, 0f);
            var to = new Vector3(2f, 0.2f, 0f);

            Assert.That(EnemyVault.Arc(from, to, 0f, 0.7f), Is.EqualTo(from));
            Assert.That(EnemyVault.Arc(from, to, 1f, 0.7f), Is.EqualTo(to));
            Vector3 middle = EnemyVault.Arc(from, to, 0.5f, 0.7f);
            Assert.That(middle.x, Is.EqualTo(1f).Within(1e-5f));
            Assert.That(middle.y, Is.EqualTo(0.1f + 0.7f).Within(1e-5f));
            Assert.That(EnemyVault.Arc(from, to, 0.25f, 0.7f).y, Is.LessThan(middle.y));
        }
    }
}
