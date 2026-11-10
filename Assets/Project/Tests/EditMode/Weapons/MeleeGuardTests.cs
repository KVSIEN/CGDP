using NUnit.Framework;
using UnityEngine;
using CGD.Weapons;

namespace CGD.Tests
{
    public class MeleeGuardTests
    {
        // Block lets 30% through at 0.5 stamina per damage stopped; parry window 0.2s, exposure 0.5s.
        private static readonly GuardSettings Settings = new(0.3f, 0.5f, 120f, 0.2f, 1.2f, 0.5f);

        private static readonly Vector3 Facing  = Vector3.forward;
        private static readonly Vector3 InFront = Vector3.forward * 2f;
        private static readonly Vector3 Behind  = Vector3.back * 2f;

        private static bool Pays(float cost) => true;
        private static bool CannotPay(float cost) => false;

        private static MeleeGuard RaisedGuard()
        {
            var guard = new MeleeGuard();
            guard.SetBlock(Settings, passive: false);
            guard.Raise();
            return guard;
        }

        [Test]
        public void NoGuardTakesFullDamage()
        {
            var guard = new MeleeGuard();

            var outcome = guard.Resolve(0f, Facing, InFront, 100f, Pays, out float through);

            Assert.AreEqual(GuardOutcome.Open, outcome);
            Assert.AreEqual(100f, through);
        }

        [Test]
        public void RaisedGuardBlocksFrontHitsAtAStaminaCost()
        {
            var guard = RaisedGuard();
            float paid = 0f;

            var outcome = guard.Resolve(0f, Facing, InFront, 100f, cost => { paid = cost; return true; }, out float through);

            Assert.AreEqual(GuardOutcome.Blocked, outcome);
            Assert.AreEqual(30f, through, 0.001f);
            Assert.AreEqual(35f, paid, 0.001f, "70 damage stopped at 0.5 stamina each");
        }

        [Test]
        public void GuardDoesNotCoverHitsFromBehind()
        {
            var guard = RaisedGuard();

            var outcome = guard.Resolve(0f, Facing, Behind, 100f, Pays, out float through);

            Assert.AreEqual(GuardOutcome.Open, outcome);
            Assert.AreEqual(100f, through);
        }

        [Test]
        public void UnpayableBlockBreaksTheGuardAndTheHitLands()
        {
            var guard = RaisedGuard();

            var outcome = guard.Resolve(0f, Facing, InFront, 100f, CannotPay, out float through);

            Assert.AreEqual(GuardOutcome.Broken, outcome);
            Assert.AreEqual(100f, through);
            Assert.IsFalse(guard.IsRaised);
        }

        [Test]
        public void HitInsideTheParryWindowIsParriedWithoutAGuard()
        {
            var guard = new MeleeGuard();
            guard.OpenParry(10f, Settings);

            var outcome = guard.Resolve(10.1f, Facing, InFront, 100f, Pays, out float through);

            Assert.AreEqual(GuardOutcome.Parried, outcome);
            Assert.AreEqual(0f, through);
        }

        [Test]
        public void HitAfterTheParryWindowIsNotParried()
        {
            var guard = new MeleeGuard();
            guard.OpenParry(10f, Settings);

            var outcome = guard.Resolve(10.3f, Facing, InFront, 100f, Pays, out _);

            Assert.AreEqual(GuardOutcome.Open, outcome);
        }

        [Test]
        public void WhiffedParryLeavesThePlayerExposedForAWhile()
        {
            var guard = new MeleeGuard();
            guard.OpenParry(10f, Settings);

            Assert.IsFalse(guard.IsExposed(10.1f), "still inside the window");
            Assert.IsTrue(guard.IsExposed(10.4f), "window closed with nothing parried");
            Assert.IsFalse(guard.IsExposed(10.8f), "exposure wore off");
        }

        [Test]
        public void SuccessfulParryNeverExposesThePlayer()
        {
            var guard = new MeleeGuard();
            guard.OpenParry(10f, Settings);
            guard.Resolve(10.1f, Facing, InFront, 100f, Pays, out _);

            Assert.IsFalse(guard.IsExposed(10.4f));
        }

        [Test]
        public void PassiveShieldBlocksWithoutBeingRaisedUnlessExposed()
        {
            var guard = new MeleeGuard();
            guard.SetBlock(Settings, passive: true);

            Assert.AreEqual(GuardOutcome.Blocked, guard.Resolve(0f, Facing, InFront, 100f, Pays, out _));

            guard.OpenParry(10f, Settings);
            Assert.AreEqual(GuardOutcome.Open, guard.Resolve(10.4f, Facing, InFront, 100f, Pays, out _),
                "a whiffed parry drops a passive shield too");
        }

        [Test]
        public void RemovingTheBlockLowersTheGuard()
        {
            var guard = RaisedGuard();
            guard.SetBlock(null, passive: false);

            Assert.IsFalse(guard.CanBlock);
            Assert.IsFalse(guard.IsRaised);
        }
    }
}
