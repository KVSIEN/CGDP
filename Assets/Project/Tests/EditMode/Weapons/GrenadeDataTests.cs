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

        [Test]
        public void GravityScalesTheWorldsGravity()
        {
            _data.GravityScale = 2f;

            Assert.AreEqual(Physics.gravity.y * 2f, _data.Gravity.y, 0.001f);
        }

        [Test]
        public void BounceReboundsAndKeepsAShareOfSpeedAlongTheSurface()
        {
            _data.Bounciness = 0.5f;
            _data.ImpactSpeedKept = 0.5f;

            Vector3 bounced = _data.BounceVelocity(new Vector3(10f, -10f, 0f), Vector3.up);

            Assert.AreEqual(5f, bounced.x, 0.001f);
            Assert.AreEqual(5f, bounced.y, 0.001f);
        }

        [Test]
        public void WeakBounceSettlesOnTheSurface()
        {
            _data.Bounciness = 0.3f;
            Vector3 gentle = new(4f, -2f, 0f);

            Vector3 bounced = _data.BounceVelocity(gentle, Vector3.up);

            Assert.IsTrue(_data.Settles(gentle, Vector3.up));
            Assert.AreEqual(0f, bounced.y, 0.001f);
            Assert.Greater(bounced.x, 0f);
        }

        [Test]
        public void MovingAwayFromTheSurfaceIsLeftAlone()
        {
            Vector3 leaving = new(3f, 2f, 0f);

            Assert.AreEqual(leaving, _data.BounceVelocity(leaving, Vector3.up));
        }
    }
}
