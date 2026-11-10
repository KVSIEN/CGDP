using NUnit.Framework;
using UnityEditor;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Tests
{
    // A generated weapon kicks like its model: every Uzi climbs up and to the right however
    // its stats roll, rather than drifting whichever way the category's random lean lands.
    public class WeaponRecoilPatternTests
    {
        private const string SmgCategoryPath = "Assets/Project/Data/Weapons/Categories/SubMachineGunCategory.asset";
        private const int SeedCount = 400;

        [Test]
        public void EveryUziClimbsUpAndToTheRight()
        {
            var category = AssetDatabase.LoadAssetAtPath<WeaponCategoryData>(SmgCategoryPath);
            Assert.IsNotNull(category, $"Missing weapon category at {SmgCategoryPath}");
            int uzis = 0;

            for (int i = 0; i < SeedCount; i++)
            {
                var weapon = (WeaponInstance)category.Generate(ItemTier.Common, Seed.From(i));
                if (!weapon.Data.WeaponName.EndsWith("Uzi")) continue;

                uzis++;
                Assert.Greater(weapon.Data.RecoilHorizontalBias, 0f, "An Uzi leaned left");
                Assert.AreEqual(HorizontalDriftMode.OneWay, weapon.Data.HorizontalDriftMode);
            }
            Assert.Greater(uzis, 0, "No seed rolled an Uzi");
        }
    }
}
