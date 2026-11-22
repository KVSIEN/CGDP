using NUnit.Framework;
using CGD.Combat;

namespace CGD.Tests
{
    // How hits meet armour plating: resisted with the plates' own armour on top, and whatever
    // breaks through carried on at the character's armour.
    public class PlatingLayerTests
    {
        [Test]
        public void PlatesWithoutArmourOfTheirOwnTakeTheHitAsItIs()
        {
            Assert.AreEqual(1f, PlatingLayer.PlatedShare(new DamageInfo(40f), 100f, 0f, 0f));
        }

        [Test]
        public void PlatingArmourStacksOnTheCharactersArmour()
        {
            // 100 armour lets through 100/200 of a hit; with +50 plating armour 100/250: 0.8 of that.
            Assert.AreEqual(0.8f, PlatingLayer.PlatedShare(new DamageInfo(40f), 100f, 50f, 0f), 0.0001f);
        }

        [Test]
        public void ArmourPenetrationCracksPlatesToo()
        {
            float plain    = PlatingLayer.PlatedShare(new DamageInfo(40f), 100f, 50f, 0f);
            float piercing = PlatingLayer.PlatedShare(new DamageInfo(40f, armorPenetration: 0.5f), 100f, 50f, 0f);
            Assert.Greater(piercing, plain);
        }

        [Test]
        public void TrueDamageIgnoresPlatingArmour()
        {
            Assert.AreEqual(1f, PlatingLayer.PlatedShare(new DamageInfo(40f, type: DamageType.True), 100f, 50f, 0f));
        }

        [Test]
        public void PlatesWithRoomLeftTakeTheWholeHit()
        {
            float left = PlatingLayer.Absorb(10f, 100f, 0.8f, out float absorbed);
            Assert.AreEqual(8f, absorbed, 0.0001f);
            Assert.AreEqual(0f, left, 0.0001f);
        }

        [Test]
        public void WhatBreaksThroughCarriesOnAtTheCharactersArmour()
        {
            // 4 plating stops what would have been 5 at bare armour; the other 5 goes on.
            float left = PlatingLayer.Absorb(10f, 4f, 0.8f, out float absorbed);
            Assert.AreEqual(4f, absorbed, 0.0001f);
            Assert.AreEqual(5f, left, 0.0001f);
        }

        [Test]
        public void NoPlatingLetsEverythingThrough()
        {
            Assert.AreEqual(10f, PlatingLayer.Absorb(10f, 0f, 0.8f, out float absorbed));
            Assert.AreEqual(0f, absorbed);
        }
    }
}
