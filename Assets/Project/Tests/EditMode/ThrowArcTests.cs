using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    public class ThrowArcTests
    {
        private const float Gravity = 9.81f;

        // Where a throw with this velocity is, height-wise, once it has covered the flat distance.
        private static float HeightAtTarget(Vector3 from, Vector3 to, Vector3 velocity)
        {
            float distance = new Vector2(to.x - from.x, to.z - from.z).magnitude;
            float time     = distance / new Vector2(velocity.x, velocity.z).magnitude;
            return from.y + velocity.y * time - 0.5f * Gravity * time * time;
        }

        [Test]
        public void LandsOnTheTargetOnFlatGround()
        {
            Vector3 from = new(0f, 1.6f, 0f), to = new(10f, 0f, 5f);
            Assert.IsTrue(ThrowArc.TrySolve(from, to, 40f, 1f, 50f, Gravity, out Vector3 velocity));
            Assert.AreEqual(to.y, HeightAtTarget(from, to, velocity), 0.01f);
        }

        [Test]
        public void ThrowsTowardsTheTargetAndUpwards()
        {
            Assert.IsTrue(ThrowArc.TrySolve(Vector3.zero, new Vector3(0f, 0f, 8f), 40f, 1f, 50f, Gravity, out Vector3 velocity));
            Assert.Greater(velocity.z, 0f);
            Assert.Greater(velocity.y, 0f);
            Assert.AreEqual(0f, velocity.x, 0.0001f);
        }

        [Test]
        public void ReachesAHigherLedge()
        {
            Vector3 from = Vector3.zero, to = new(8f, 3f, 0f);
            Assert.IsTrue(ThrowArc.TrySolve(from, to, 40f, 1f, 50f, Gravity, out Vector3 velocity));
            Assert.AreEqual(to.y, HeightAtTarget(from, to, velocity), 0.01f);
        }

        [Test]
        public void FailsBeyondTheSpeedLimit()
        {
            Assert.IsFalse(ThrowArc.TrySolve(Vector3.zero, new Vector3(60f, 0f, 0f), 40f, 1f, 15f, Gravity, out _));
        }

        [Test]
        public void FailsWhenTheTargetIsAboveTheArc()
        {
            // A 40° throw can never rise more than tan(40°) ≈ 0.84 m per metre travelled.
            Assert.IsFalse(ThrowArc.TrySolve(Vector3.zero, new Vector3(2f, 5f, 0f), 40f, 1f, 50f, Gravity, out _));
        }

        [Test]
        public void FailsOnTopOfItself()
        {
            Assert.IsFalse(ThrowArc.TrySolve(Vector3.zero, Vector3.up, 40f, 1f, 50f, Gravity, out _));
        }
    }
}
