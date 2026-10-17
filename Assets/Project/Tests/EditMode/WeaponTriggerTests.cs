using NUnit.Framework;
using CGD.Weapons;

namespace CGD.Tests
{
    public class WeaponTriggerTests
    {
        private static TriggerAction Read(WeaponTrigger trigger, FireMode mode, int magazine = 10, bool held = false,
            bool pressed = false, bool canReload = true, bool burstPending = false, float now = 0f, float dt = 0.1f,
            float chargeTime = 1f, float minCharge = 0.2f) =>
            trigger.Read(mode, minCharge, magazine, canReload, burstPending, held, pressed, now, dt, chargeTime, out _);

        [Test]
        public void SemiFiresOnPressNotOnHold()
        {
            var trigger = new WeaponTrigger();
            Assert.AreEqual(TriggerAction.None, Read(trigger, FireMode.Semi, held: true));
            Assert.AreEqual(TriggerAction.Fire, Read(trigger, FireMode.Semi, held: true, pressed: true));
        }

        [Test]
        public void AutoFiresWhileHeld()
        {
            Assert.AreEqual(TriggerAction.Fire, Read(new WeaponTrigger(), FireMode.Auto, held: true));
        }

        [Test]
        public void BurstWaitsForTheCurrentBurst()
        {
            var trigger = new WeaponTrigger();
            Assert.AreEqual(TriggerAction.None, Read(trigger, FireMode.Burst, pressed: true, burstPending: true));
            Assert.AreEqual(TriggerAction.FireBurst, Read(trigger, FireMode.Burst, pressed: true));
        }

        [Test]
        public void BufferedPressFiresLater()
        {
            var trigger = new WeaponTrigger();
            trigger.BufferPress(1f);
            Assert.AreEqual(TriggerAction.Fire, Read(trigger, FireMode.Semi, now: 1.2f));
            Assert.AreEqual(TriggerAction.None, Read(trigger, FireMode.Semi, now: 1.3f), "a buffered press is used up once");
        }

        [Test]
        public void EmptyMagazineReloadsIfItCanOtherwiseClicks()
        {
            var trigger = new WeaponTrigger();
            Assert.AreEqual(TriggerAction.Reload, Read(trigger, FireMode.Semi, magazine: 0, pressed: true));
            Assert.AreEqual(TriggerAction.DryFire, Read(trigger, FireMode.Semi, magazine: 0, pressed: true, canReload: false));
            Assert.AreEqual(TriggerAction.None, Read(trigger, FireMode.Auto, magazine: 0, held: true, canReload: false), "holding doesn't spam the click");
        }

        [Test]
        public void ChargeFiresOnReleaseOnceDrawnFarEnough()
        {
            var trigger = new WeaponTrigger();
            for (int i = 0; i < 5; i++) Read(trigger, FireMode.Charge, held: true);

            TriggerAction action = trigger.Read(FireMode.Charge, 0.2f, 10, true, false, false, false, 0f, 0.1f, 1f, out float charge);
            Assert.AreEqual(TriggerAction.Fire, action);
            Assert.AreEqual(0.5f, charge, 0.001f);
        }

        [Test]
        public void TappingAChargeWeaponWastesNoAmmo()
        {
            var trigger = new WeaponTrigger();
            Read(trigger, FireMode.Charge, held: true, dt: 0.05f);

            Assert.AreEqual(TriggerAction.None, Read(trigger, FireMode.Charge));
        }

        [Test]
        public void ResetDropsAChargeInProgress()
        {
            var trigger = new WeaponTrigger();
            for (int i = 0; i < 5; i++) Read(trigger, FireMode.Charge, held: true);
            trigger.Reset();

            Assert.AreEqual(0f, trigger.ChargeTimer);
            Assert.AreEqual(TriggerAction.None, Read(trigger, FireMode.Charge));
        }
    }
}
