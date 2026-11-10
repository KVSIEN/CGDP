using System.Linq;
using NUnit.Framework;
using UnityEditor;
using CGD.Enemies;
using CGD.Items;

namespace CGD.Tests
{
    // Keeps the authored tier 2 and 3 enemies and the worn gear on the scaling WEAPON_BALANCE.md
    // sets out: tier 1 ≈ 100 effective health, tier 2 ≈ 2.5×, tier 3 ≈ 5×, damage rising each tier.
    public class TierBalanceTests
    {
        private const string EnemyFolder = "Assets/Project/Data/Enemies/";
        private const float Tolerance    = 0.10f;

        private static EnemyData Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + name + ".asset");

        private static readonly (string Faction, string Tier1, string Tier2, string Tier3)[] Factions =
        {
            ("Bio",  "ScavengerEnemyData",      "BioTier2EnemyData",  "BioTier3EnemyData"),
            ("Tech", "MaintenanceBotEnemyData", "TechTier2EnemyData", "TechTier3EnemyData"),
            ("Void", "HuskEnemyData",           "VoidTier2EnemyData", "VoidTier3EnemyData"),
        };

        [Test]
        public void TierOneEnemiesSitAroundOneHundredEffectiveHealth()
        {
            foreach (var faction in Factions)
                Assert.AreEqual(100f, Load(faction.Tier1).EffectiveHealth, 100f * Tolerance, faction.Faction);
        }

        [Test]
        public void TierTwoEnemiesHaveTwoAndAHalfTimesTheEffectiveHealth()
        {
            foreach (var faction in Factions)
                Assert.AreEqual(250f, Load(faction.Tier2).EffectiveHealth, 250f * Tolerance, faction.Faction);
        }

        [Test]
        public void TierThreeEnemiesHaveFiveTimesTheEffectiveHealth()
        {
            foreach (var faction in Factions)
                Assert.AreEqual(500f, Load(faction.Tier3).EffectiveHealth, 500f * Tolerance, faction.Faction);
        }

        [Test]
        public void EnemiesHitHarderWithEveryTier()
        {
            foreach (var faction in Factions)
            {
                Assert.Less(Load(faction.Tier1).AttackDamage, Load(faction.Tier2).AttackDamage, faction.Faction);
                Assert.Less(Load(faction.Tier2).AttackDamage, Load(faction.Tier3).AttackDamage, faction.Faction);
            }
        }

        [Test]
        public void TiersAreMarkedOnTheAssets()
        {
            foreach (var faction in Factions)
            {
                Assert.AreEqual(1, Load(faction.Tier1).Tier, faction.Faction);
                Assert.AreEqual(2, Load(faction.Tier2).Tier, faction.Faction);
                Assert.AreEqual(3, Load(faction.Tier3).Tier, faction.Faction);
            }
        }

        [Test]
        public void EveryEquipmentSlotHasAnArmorPieceThatRolls()
        {
            var pieces = AssetDatabase.FindAssets("t:ArmorDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ArmorDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .ToList();

            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
                Assert.IsTrue(pieces.Any(p => p.Slot == slot && p.StatRanges != null && p.StatRanges.Length > 0), $"No armor piece for the {slot} slot");
        }
    }
}
