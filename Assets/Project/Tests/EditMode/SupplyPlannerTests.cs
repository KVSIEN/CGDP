using NUnit.Framework;
using CGD.Director;

namespace CGD.Tests
{
    public class SupplyPlannerTests
    {
        // Healing below 50% health unless 2+ carried; ammo below 2 spare magazines.
        private static readonly SupplyPlanner Planner = new(new SupplyTuning(0.5f, 2, 2f));

        private const float NoAmmoNeeded = float.PositiveInfinity;

        [Test]
        public void NothingWhenThePlayerIsFine() =>
            Assert.AreEqual(SupplyKind.None, Planner.Plan(new SupplyNeeds(0.9f, 0, 5f)));

        [Test]
        public void HealingWhenHurtWithoutHealing() =>
            Assert.AreEqual(SupplyKind.Healing, Planner.Plan(new SupplyNeeds(0.3f, 0, NoAmmoNeeded)));

        [Test]
        public void NoHealingWhenEnoughIsCarried() =>
            Assert.AreEqual(SupplyKind.None, Planner.Plan(new SupplyNeeds(0.1f, 2, NoAmmoNeeded)));

        [Test]
        public void AmmoWhenLowOnSpareMagazines() =>
            Assert.AreEqual(SupplyKind.Ammo, Planner.Plan(new SupplyNeeds(1f, 0, 0.5f)));

        [Test]
        public void TheMostUrgentNeedWins()
        {
            // 40% health is 20% short of the threshold; no spare ammo is 100% short.
            Assert.AreEqual(SupplyKind.Ammo, Planner.Plan(new SupplyNeeds(0.4f, 0, 0f)));
            // 5% health is 90% short; 1.5 magazines is 25% short.
            Assert.AreEqual(SupplyKind.Healing, Planner.Plan(new SupplyNeeds(0.05f, 0, 1.5f)));
        }
    }
}
