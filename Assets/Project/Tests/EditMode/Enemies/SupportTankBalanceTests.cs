using NUnit.Framework;
using UnityEditor;
using CGD.Abilities;
using CGD.Enemies;

namespace CGD.Tests
{
    // Keeps the three factions' supports and tanks on one power scale: tanks around 550 effective
    // health (the tier 3 band in WEAPON_BALANCE.md), supports around 190, and every support's
    // Support ability restoring about the same per second, whatever pools it refills.
    public class SupportTankBalanceTests
    {
        private const string Folder = "Assets/Project/Data/Enemies/AITest/";

        private static readonly string[] Tanks    = { "BioTank", "TechTank", "VoidTank" };
        private static readonly string[] Supports = { "BioSupport", "TechSupport", "VoidSupport" };

        private static EnemyData Load(string name) => AssetDatabase.LoadAssetAtPath<EnemyData>(Folder + name + "EnemyData.asset");

        [Test]
        public void TanksAreAboutFiveHundredAndFiftyEffectiveHealth()
        {
            foreach (string name in Tanks)
                Assert.AreEqual(550f, Load(name).EffectiveHealth, 50f, name);
        }

        [Test]
        public void SupportsAreAboutOneHundredAndNinetyEffectiveHealth()
        {
            foreach (string name in Supports)
                Assert.AreEqual(190f, Load(name).EffectiveHealth, 20f, name);
        }

        [Test]
        public void SupportAbilitiesRestoreAboutSixPerSecond()
        {
            foreach (string name in Supports)
            {
                var heal = Load(name).Support as TargetedAbility;
                Assert.IsNotNull(heal, name + " has no targeted Support ability");
                float perSecond = (heal.Heal + heal.Shield + heal.Plating) / heal.Cooldown;
                Assert.AreEqual(6f, perSecond, 0.5f, name);
            }
        }
    }
}
