using NUnit.Framework;
using UnityEngine;
using CGD.UI;

namespace CGD.Tests
{
    public class DamageIndicatorTests
    {
        private static float Angle(Vector3 attacker, Vector3 forward)
        {
            Assert.IsTrue(DamageDirection.TryAngle(forward, Vector3.zero, attacker, out float degrees));
            return degrees;
        }

        [Test]
        public void AnAttackerAheadIsAtZeroAndOneToTheRightIsNinety()
        {
            Assert.AreEqual(0f, Angle(new Vector3(0f, 0f, 5f), Vector3.forward), 0.01f);
            Assert.AreEqual(90f, Angle(new Vector3(5f, 0f, 0f), Vector3.forward), 0.01f);
        }

        [Test]
        public void LeftIsNegativeAndBehindIsOneEighty()
        {
            Assert.AreEqual(-90f, Angle(new Vector3(-5f, 0f, 0f), Vector3.forward), 0.01f);
            Assert.AreEqual(180f, Mathf.Abs(Angle(new Vector3(0f, 0f, -5f), Vector3.forward)), 0.01f);
        }

        [Test]
        public void TheBearingTurnsWithTheViewer()
        {
            // Looking right, an attacker to the right is straight ahead.
            Assert.AreEqual(0f, Angle(new Vector3(5f, 0f, 0f), Vector3.right), 0.01f);
        }

        [Test]
        public void HeightDoesNotChangeTheBearing()
        {
            Assert.AreEqual(90f, Angle(new Vector3(5f, 40f, 0f), new Vector3(0f, -0.5f, 1f)), 0.01f);
        }

        [Test]
        public void AnAttackerStraightAboveHasNoDirection()
        {
            Assert.IsFalse(DamageDirection.TryAngle(Vector3.forward, Vector3.zero, new Vector3(0f, 10f, 0f), out _));
        }

        [Test]
        public void AViewerLookingStraightDownHasNoDirection()
        {
            Assert.IsFalse(DamageDirection.TryAngle(Vector3.down, Vector3.zero, new Vector3(5f, 0f, 0f), out _));
        }

        [Test]
        public void AHitAddsAnIndicatorThatExpires()
        {
            var set = new DamageIndicators(3, 2f);
            set.Hit("a", Vector3.right, 0.5f);

            Assert.IsTrue(set.IsActive(0));
            set.Tick(1.9f);
            Assert.IsTrue(set.IsActive(0));
            set.Tick(0.2f);
            Assert.IsFalse(set.IsActive(0));
        }

        [Test]
        public void ARepeatHitFromTheSameAttackerRefreshesItsIndicator()
        {
            var set = new DamageIndicators(3, 2f);
            set.Hit("a", Vector3.right, 0.4f);
            set.Tick(1.5f);
            set.Hit("a", Vector3.left, 0.9f);

            int active = 0;
            for (int i = 0; i < set.Capacity; i++) if (set.IsActive(i)) active++;
            Assert.AreEqual(1, active);
            Assert.AreEqual(2f, set[0].Remaining, 0.0001f);
            Assert.AreEqual(0.9f, set[0].Intensity, 0.0001f);
            Assert.AreEqual(Vector3.left, set[0].Position);
        }

        [Test]
        public void ARefreshKeepsTheStrongerIntensity()
        {
            var set = new DamageIndicators(3, 2f);
            set.Hit("a", Vector3.right, 0.9f);
            set.Hit("a", Vector3.right, 0.4f);
            Assert.AreEqual(0.9f, set[0].Intensity, 0.0001f);
        }

        [Test]
        public void DifferentAttackersGetTheirOwnIndicators()
        {
            var set = new DamageIndicators(3, 2f);
            set.Hit("a", Vector3.right, 0.5f);
            set.Hit("b", Vector3.left, 0.5f);
            Assert.IsTrue(set.IsActive(0));
            Assert.IsTrue(set.IsActive(1));
        }

        [Test]
        public void WhenFullTheOneClosestToExpiringIsReplaced()
        {
            var set = new DamageIndicators(2, 2f);
            set.Hit("old", Vector3.right, 0.5f);
            set.Tick(1f);
            set.Hit("newer", Vector3.left, 0.5f);

            set.Hit("newest", Vector3.forward, 0.5f);

            Assert.AreEqual("newest", set[0].Source);
            Assert.AreEqual("newer", set[1].Source);
        }

        [Test]
        public void AnIndicatorHoldsFullStrengthThenFadesOverItsLastPart()
        {
            var set = new DamageIndicators(1, 2f);
            set.Hit("a", Vector3.right, 1f);
            Assert.AreEqual(1f, set.Fade(0), 0.0001f);

            set.Tick(1.6f);                       // 0.4 s left of 2 s: the fade is about to start
            Assert.AreEqual(0.5f, set.Fade(0), 0.0001f);
        }
    }
}
