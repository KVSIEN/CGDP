using NUnit.Framework;
using CGD.Stealth;

namespace CGD.Tests
{
    public class StealthStatusTests
    {
        private const float Frame = 0.1f;

        [Test]
        public void VisibleWithoutCoverOrCloak()
        {
            var status = new StealthStatus();
            status.Tick(Frame, inCover: false);

            Assert.AreEqual(StealthState.Visible, status.State);
        }

        [Test]
        public void CoverHidesAndLeavingCoverRevealsPosition()
        {
            var status = new StealthStatus();

            status.Tick(Frame, inCover: true);
            Assert.AreEqual(StealthState.Hidden, status.State);
            Assert.IsTrue(status.IsHidden);

            status.Tick(Frame, inCover: false);
            Assert.AreEqual(StealthState.Visible, status.State);
        }

        [Test]
        public void NoiseRevealsCoverForAWhileThenHidesAgain()
        {
            var status = new StealthStatus();
            status.Tick(Frame, inCover: true);

            status.OnNoise(0.25f);
            status.Tick(Frame, inCover: true);
            Assert.AreEqual(StealthState.Revealed, status.State);

            status.Tick(0.5f, inCover: true);
            Assert.AreEqual(StealthState.Hidden, status.State);
        }

        [Test]
        public void CloakHidesUntilItRunsOut()
        {
            var status = new StealthStatus();
            status.Cloak(0.3f, breaksOnNoise: false);

            status.Tick(Frame, inCover: false);
            Assert.AreEqual(StealthState.Hidden, status.State);
            Assert.IsTrue(status.IsCloaked);

            status.Tick(0.5f, inCover: false);
            Assert.AreEqual(StealthState.Visible, status.State);
        }

        [Test]
        public void BreakableCloakEndsOnNoiseButSturdyOneDoesNot()
        {
            var breakable = new StealthStatus();
            breakable.Cloak(5f, breaksOnNoise: true);
            breakable.OnNoise(1f);

            var sturdy = new StealthStatus();
            sturdy.Cloak(5f, breaksOnNoise: false);
            sturdy.OnNoise(1f);

            Assert.IsFalse(breakable.IsCloaked);
            Assert.IsTrue(sturdy.IsCloaked);
        }

        [Test]
        public void ChangedReportsPreviousAndNextState()
        {
            var status = new StealthStatus();
            StealthState from = default, to = default;
            status.Changed += (previous, next) => { from = previous; to = next; };

            status.Tick(Frame, inCover: true);

            Assert.AreEqual(StealthState.Visible, from);
            Assert.AreEqual(StealthState.Hidden, to);
        }

        [Test]
        public void SpottedIsRememberedThenForgotten()
        {
            var status = new StealthStatus();
            var changes = new System.Collections.Generic.List<bool>();
            status.SpottedChanged += changes.Add;

            status.OnSpotted(0.2f);
            status.OnSpotted(0.2f);
            Assert.IsTrue(status.IsSpotted);

            status.Tick(0.5f, inCover: false);

            Assert.IsFalse(status.IsSpotted);
            CollectionAssert.AreEqual(new[] { true, false }, changes);
        }

        [Test]
        public void ResetReturnsToVisible()
        {
            var status = new StealthStatus();
            status.Cloak(5f, breaksOnNoise: false);
            status.Tick(Frame, inCover: false);

            status.Reset();

            Assert.AreEqual(StealthState.Visible, status.State);
            Assert.IsFalse(status.IsCloaked);
        }
    }
}
