using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    // Which bush a sneaking enemy passes through next on its way somewhere.
    public class BushHopsTests
    {
        private static readonly Vector3 From = Vector3.zero;
        private static readonly Vector3 Goal = new(20f, 0f, 0f);

        [Test]
        public void NoBushesMeansStraightThere()
        {
            Assert.IsFalse(BushHops.TryPick(From, Goal, 0.5f, new List<Vector3>(), out _));
        }

        [Test]
        public void ABushOnTheWayIsTaken()
        {
            var bushes = new List<Vector3> { new(10f, 0f, 2f) };

            Assert.IsTrue(BushHops.TryPick(From, Goal, 0.5f, bushes, out Vector3 hop));
            Assert.AreEqual(bushes[0], hop);
        }

        [Test]
        public void TheNearestBushOnTheWayComesFirst()
        {
            var bushes = new List<Vector3> { new(14f, 0f, 1f), new(6f, 0f, -1f), new(10f, 0f, 0f) };

            BushHops.TryPick(From, Goal, 0.5f, bushes, out Vector3 hop);

            Assert.AreEqual(bushes[1], hop);
        }

        [Test]
        public void ABushBehindItIsNotOnTheWay()
        {
            var bushes = new List<Vector3> { new(-6f, 0f, 0f) };

            Assert.IsFalse(BushHops.TryPick(From, Goal, 2f, bushes, out _));
        }

        [Test]
        public void TheDetourAllowanceLimitsHowFarOffTheLineItGoes()
        {
            var bushes = new List<Vector3> { new(10f, 0f, 9f) };   // ~26.9 m through it vs 20 m straight

            Assert.IsFalse(BushHops.TryPick(From, Goal, 0.25f, bushes, out _));
            Assert.IsTrue(BushHops.TryPick(From, Goal, 0.5f, bushes, out _));
        }

        [Test]
        public void TheBushItIsInIsNotANextHop()
        {
            var bushes = new List<Vector3> { new(1f, 0f, 0f) };

            Assert.IsFalse(BushHops.TryPick(From, Goal, 0.5f, bushes, out _));
        }

        [Test]
        public void NoAllowanceOrAShortWalkGoesStraight()
        {
            var bushes = new List<Vector3> { new(10f, 0f, 0f), new(2.5f, 0f, 0f) };

            Assert.IsFalse(BushHops.TryPick(From, Goal, 0f, bushes, out _));
            Assert.IsFalse(BushHops.TryPick(From, new Vector3(4f, 0f, 0f), 0.5f, bushes, out _));
        }

        [Test]
        public void HoppingBushToBushEndsAtTheGoal()
        {
            var bushes = new List<Vector3> { new(4f, 0f, 1f), new(8f, 0f, -1f), new(12f, 0f, 1f), new(16f, 0f, 0f), new(9f, 0f, 1f) };
            Vector3 at = From;
            int hops = 0;

            while (BushHops.TryPick(at, Goal, 0.5f, bushes, out Vector3 hop))
            {
                Assert.Less(Vector3.Distance(hop, Goal), Vector3.Distance(at, Goal), "every hop gets closer");
                at = hop;
                Assert.LessOrEqual(++hops, bushes.Count);
            }

            Assert.Greater(hops, 1);
        }
    }
}
