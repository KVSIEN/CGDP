using NUnit.Framework;
using UnityEngine;
using CGD.Combat;

namespace CGD.Tests
{
    public class SwingBladeTests
    {
        private static void AssertNear(Vector3 expected, Vector3 actual) =>
            Assert.Less((expected - actual).magnitude, 0.001f, $"expected {expected}, got {actual}");

        [Test]
        public void NeutralPoseRunsStraightAhead()
        {
            var blade = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(0f), 0.5f, 2f);
            AssertNear(new Vector3(0f, 0f, 0.5f), blade.Near);
            AssertNear(new Vector3(0f, 0f, 2f), blade.Far);
        }

        [Test]
        public void PositiveYawTurnsRightAndPositivePitchRaises()
        {
            var right = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(90f), 0f, 1f);
            var up    = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(0f, 90f), 0f, 1f);
            AssertNear(Vector3.right, right.Far);
            AssertNear(Vector3.up, up.Far);
        }

        [Test]
        public void PushSlidesTheWholeBladeAlongItsDirection()
        {
            var blade = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(0f, 0f, 1f), 0.5f, 2f);
            AssertNear(new Vector3(0f, 0f, 1.5f), blade.Near);
            AssertNear(new Vector3(0f, 0f, 3f), blade.Far);
        }

        [Test]
        public void PoseFollowsTheAim()
        {
            Quaternion facingRight = Quaternion.LookRotation(Vector3.right, Vector3.up);
            var blade = SwingBlade.At(Vector3.zero, facingRight, new SwingPose(0f), 0f, 1f);
            AssertNear(Vector3.right, blade.Far);
        }

        [Test]
        public void StationaryBladeNeedsOneStep()
        {
            var blade = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(30f), 0f, 1.5f);
            Assert.AreEqual(1, SwingBlade.StepsBetween(blade, blade, 0.3f, 6));
        }

        [Test]
        public void FartherTravelNeedsMoreStepsUpToTheCap()
        {
            var a = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(60f), 0f, 1.5f);
            var small = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(55f), 0f, 1.5f);
            var wide  = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(0f), 0f, 1.5f);
            var huge  = SwingBlade.At(Vector3.zero, Quaternion.identity, new SwingPose(-120f), 0f, 1.5f);

            Assert.AreEqual(1, SwingBlade.StepsBetween(a, small, 0.4f, 6));
            Assert.Greater(SwingBlade.StepsBetween(a, wide, 0.4f, 6), 1);
            Assert.AreEqual(6, SwingBlade.StepsBetween(a, huge, 0.4f, 6));
        }
    }
}
