using NUnit.Framework;
using UnityEngine;
using CGD.Enemies;

namespace CGD.Tests
{
    public class CoverPointTests
    {
        // A spot with its wall to the north (+z).
        private static readonly CoverPoint NorthWall = new(Vector3.zero, Vector3.forward, isHigh: true);

        [Test]
        public void ProtectsFromAThreatBehindTheObstacle()
        {
            Assert.IsTrue(NorthWall.Faces(new Vector3(0f, 0f, 10f), 0.35f));
            Assert.IsTrue(NorthWall.Faces(new Vector3(4f, 0f, 10f), 0.35f));
        }

        [Test]
        public void DoesNotProtectFromTheSideOrBehind()
        {
            Assert.IsFalse(NorthWall.Faces(new Vector3(10f, 0f, 0f), 0.35f));
            Assert.IsFalse(NorthWall.Faces(new Vector3(0f, 0f, -10f), 0.35f));
        }

        [Test]
        public void IgnoresHeightDifferences()
        {
            Assert.IsTrue(NorthWall.Faces(new Vector3(0f, 5f, 10f), 0.35f));
        }

        [Test]
        public void AThreatOnTheSpotItselfIsNotScreened()
        {
            Assert.IsFalse(NorthWall.Faces(Vector3.zero, 0.35f));
        }
    }
}
