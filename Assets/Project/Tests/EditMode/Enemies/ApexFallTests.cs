using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // A pack's apex holds it together: seeing one fall costs the lesser ones its extra shock.
    public class ApexFallTests
    {
        private const float ProfileLoss = 0.3f;

        private static MoraleDoctrine Doctrine(string json)
        {
            var doctrine = new MoraleDoctrine();
            JsonUtility.FromJsonOverwrite(json, doctrine);
            return doctrine;
        }

        [Test]
        public void ApexFalling_ShakesLesserKinExtra()
        {
            MoraleDoctrine doctrine = Doctrine("{\"_apexTier\":3,\"_apexFallLoss\":0.6}");

            Assert.That(doctrine.AllyDownLoss(ProfileLoss, fallenTier: 3, ownTier: 1), Is.EqualTo(ProfileLoss + 0.6f).Within(1e-5f));
        }

        [Test]
        public void ApexFalling_DoesNotShakeAnotherApexExtra()
        {
            MoraleDoctrine doctrine = Doctrine("{\"_apexTier\":3,\"_apexFallLoss\":0.6}");

            Assert.That(doctrine.AllyDownLoss(ProfileLoss, fallenTier: 3, ownTier: 3), Is.EqualTo(ProfileLoss).Within(1e-5f));
        }

        [Test]
        public void LesserKinFalling_IsNoApexFall()
        {
            MoraleDoctrine doctrine = Doctrine("{\"_apexTier\":3,\"_apexFallLoss\":0.6}");

            Assert.That(doctrine.AllyDownLoss(ProfileLoss, fallenTier: 2, ownTier: 1), Is.EqualTo(ProfileLoss).Within(1e-5f));
        }

        [Test]
        public void NoApexTier_ChangesNothing()
        {
            MoraleDoctrine doctrine = Doctrine("{\"_apexFallLoss\":0.6}");

            Assert.That(doctrine.AllyDownLoss(ProfileLoss, fallenTier: 3, ownTier: 1), Is.EqualTo(ProfileLoss).Within(1e-5f));
        }
    }
}
