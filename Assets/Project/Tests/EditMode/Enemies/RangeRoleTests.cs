using NUnit.Framework;
using UnityEditor;
using CGD.Enemies;

namespace CGD.Tests
{
    // Each faction spreads its squad over the fight by giving attackers, tanks and supports
    // different ranges (FEATURES.md, Range roles):
    //   BIO  — attacker close, tank mid,   support long
    //   TECH — attacker mid,   tank long,  support close
    //   VOID — attacker long,  tank close, support mid
    public class RangeRoleTests
    {
        private const string Folder = "Assets/Project/Data/Enemies/AITest/";

        [TestCase("BioTest",     CombatRange.Close)]
        [TestCase("BioTank",     CombatRange.Mid)]
        [TestCase("BioSupport",  CombatRange.Long)]
        [TestCase("TechTest",    CombatRange.Mid)]
        [TestCase("TechTank",    CombatRange.Long)]
        [TestCase("TechSupport", CombatRange.Close)]
        [TestCase("VoidTest",    CombatRange.Long)]
        [TestCase("VoidTank",    CombatRange.Close)]
        [TestCase("VoidSupport", CombatRange.Mid)]
        public void EachAgentFightsFromItsRolesRange(string name, CombatRange expected)
        {
            var data = AssetDatabase.LoadAssetAtPath<EnemyData>(Folder + name + "EnemyData.asset");
            Assert.AreEqual(expected, data.Range, $"{name}: engagement range {data.EngagementRange} m");
        }

        [Test]
        public void TheBandsAreOrderedAndTheTypicalRangesFallInThem()
        {
            Assert.Less(CombatRanges.MidFrom, CombatRanges.LongFrom);
            foreach (CombatRange range in System.Enum.GetValues(typeof(CombatRange)))
                Assert.AreEqual(range, CombatRanges.Of(CombatRanges.Typical(range)), range.ToString());
        }

        // The long-range supports must be able to reach the allies they heal from where they stand.
        [Test]
        public void LongRangeSupportsAreRangedAndShoot()
        {
            var bio = AssetDatabase.LoadAssetAtPath<EnemyData>(Folder + "BioSupportEnemyData.asset");
            Assert.AreEqual(EnemyCombatType.Ranged, bio.CombatType);
            Assert.IsNotNull(bio.ShotProjectile, "it spits from its long range");
        }
    }
}
