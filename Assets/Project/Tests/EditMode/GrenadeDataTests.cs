using NUnit.Framework;
using UnityEngine;
using CGD.Weapons;

namespace CGD.Tests
{
    public class GrenadeDataTests
    {
        private GrenadeData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<GrenadeData>();
            _data.MinThrowSpeed = 8f;
            _data.MaxThrowSpeed = 18f;
            _data.ChargeTime = 1f;
            _data.ThrowAngle = 0f;
            _data.InheritVelocity = 0f;
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_data);

        [Test]
        public void ChargeGrowsWithHoldTimeAndCaps()
        {
            Assert.AreEqual(0.25f, _data.Charge(0.25f), 0.001f);
            Assert.AreEqual(1f, _data.Charge(5f), 0.001f);
        }

        [Test]
        public void NoChargeTimeAlwaysThrowsAtFullCharge()
        {
            _data.ChargeTime = 0f;

            Assert.AreEqual(1f, _data.Charge(0f));
        }

        [Test]
        public void SpeedRunsFromMinToMaxWithCharge()
        {
            Vector3 tap  = _data.LaunchVelocity(Vector3.forward, Vector3.right, 0f, Vector3.zero);
            Vector3 full = _data.LaunchVelocity(Vector3.forward, Vector3.right, 1f, Vector3.zero);

            Assert.AreEqual(8f, tap.magnitude, 0.001f);
            Assert.AreEqual(18f, full.magnitude, 0.001f);
        }

        [Test]
        public void ThrowAngleLiftsTheThrowAboveTheAim()
        {
            _data.ThrowAngle = 10f;

            Vector3 velocity = _data.LaunchVelocity(Vector3.forward, Vector3.right, 1f, Vector3.zero);

            Assert.Greater(velocity.y, 0f);
            Assert.AreEqual(18f, velocity.magnitude, 0.001f);
        }

        [Test]
        public void TakesAlongAShareOfTheThrowersVelocity()
        {
            _data.InheritVelocity = 0.5f;

            Vector3 velocity = _data.LaunchVelocity(Vector3.forward, Vector3.right, 1f, new Vector3(4f, 0f, 0f));

            Assert.AreEqual(2f, velocity.x, 0.001f);
        }
    }
}
