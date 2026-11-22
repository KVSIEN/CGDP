using System.Linq;
using NUnit.Framework;
using UnityEditor;
using CGD.Enemies;
using CGD.Items;

namespace CGD.Tests
{
    // Keeps the tier 1 enemies and the worn gear on the scaling WEAPON_BALANCE.md sets out: tier 1
    // ≈ 100 effective health. Tiers 2 and 3 are not authored yet (their scaling comes later).
    public class TierBalanceTests
    {
        private const string EnemyFolder = "Assets/Project/Data/Enemies/";
        private const float Tolerance    = 0.10f;

        private static EnemyData Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyData>(EnemyFolder + name + ".asset");

        private static readonly (string Faction, string Tier1)[] Factions =
        {
            ("Bio",  "ScavengerEnemyData"),
            ("Tech", "MaintenanceBotEnemyData"),
            ("Void", "HuskEnemyData"),
        };

        [Test]
        public void TierOneEnemiesSitAroundOneHundredEffectiveHealth()
        {
            foreach (var faction in Factions)
                Assert.AreEqual(100f, Load(faction.Tier1).EffectiveHealth, 100f * Tolerance, faction.Faction);
        }

        // What each faction lives on: TECH on plating (no health), BIO on health with bone plating
        // over it, VOID on a ward (shield) with a little health under it.
        [Test]
        public void EachFactionKeepsItsOwnPrimaryPool()
        {
            foreach (string name in new[] { "MaintenanceBotEnemyData", "AITest/TechTestEnemyData", "AITest/TechSupportEnemyData" })
            {
                EnemyData tech = Load(name);
                Assert.AreEqual(0f, tech.MaxHealth, name);
                Assert.Greater(tech.MaxPlating, 0f, name);
            }
            foreach (string name in new[] { "ScavengerEnemyData", "AITest/BioTestEnemyData", "AITest/BioSupportEnemyData" })
            {
                EnemyData bio = Load(name);
                Assert.Greater(bio.MaxHealth, bio.MaxPlating, name);
                Assert.Greater(bio.MaxPlating, 0f, name);
                Assert.AreEqual(0f, bio.MaxShield, name);
            }
            foreach (string name in new[] { "HuskEnemyData", "AITest/VoidTestEnemyData", "AITest/VoidSupportEnemyData" })
            {
                EnemyData @void = Load(name);
                Assert.GreaterOrEqual(@void.MaxShield, 2f * @void.MaxHealth, name + ": the ward is most of it");
                Assert.Greater(@void.MaxHealth, 0f, name);
                Assert.AreEqual(0f, @void.MaxPlating, name);
            }
        }

        [Test]
        public void TierOneEnemiesAreMarkedTierOne()
        {
            foreach (var faction in Factions)
                Assert.AreEqual(1, Load(faction.Tier1).Tier, faction.Faction);
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
