using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    public class FacingArcTests
    {
        [Test]
        public void TargetInsideTheArcIsContained()
        {
            Assert.IsTrue(FacingArc.Contains(Vector3.forward, new Vector3(1f, 0f, 1f), 120f));
        }

        [Test]
        public void TargetOutsideTheArcIsNot()
        {
            Assert.IsFalse(FacingArc.Contains(Vector3.forward, Vector3.right, 90f));
            Assert.IsFalse(FacingArc.Contains(Vector3.forward, Vector3.back, 120f));
        }

        [Test]
        public void FullCircleContainsEverything()
        {
            Assert.IsTrue(FacingArc.Contains(Vector3.forward, Vector3.back, 360f));
        }

        [Test]
        public void HeightDifferenceIsIgnored()
        {
            Assert.IsTrue(FacingArc.Contains(Vector3.forward, new Vector3(0f, 50f, 1f), 30f));
        }

        [Test]
        public void TargetOnTopOfTheDefenderCountsAsInside()
        {
            Assert.IsTrue(FacingArc.Contains(Vector3.forward, Vector3.zero, 10f));
        }
    }
}
