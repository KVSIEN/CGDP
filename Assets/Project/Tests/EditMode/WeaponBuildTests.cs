using NUnit.Framework;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Tests
{
    // Which of variant, model and category decides each part of a generated weapon:
    // a variant's own settings win, then the model's, then the category's.
    public class WeaponBuildTests
    {
        private const int SeedCount = 40;

        private WeaponCategoryData _category;

        [TearDown]
        public void DestroyCategory()
        {
            if (_category != null) Object.DestroyImmediate(_category);
        }

        // A Glock 18 that fires full-auto, or semi as a "Glock 17" with its own recoil pattern.
        private WeaponCategoryData CreateGlockCategory()
        {
            _category = ScriptableObject.CreateInstance<WeaponCategoryData>();
            _category.Models = new[]
            {
                new WeaponModel
                {
                    Name     = "Glock 18",
                    AmmoType = AmmoType.HeavyRounds,
                    Recoil   = new RecoilPattern { Bias = 0.45f, Drift = HorizontalDriftMode.OneWay, MuzzleFlip = 0.25f },
                    FiringStyles = new[]
                    {
                        new FiringStyle { FireMode = FireMode.Auto },
                        new FiringStyle
                        {
                            FireMode = FireMode.Semi, ModelName = "Glock 17", RoundsPerLoad = 5, OwnRecoil = true,
                            Recoil   = new RecoilPattern { Bias = 0.1f, MuzzleFlip = 0.2f },
                        },
                    },
                },
            };
            return _category;
        }

        private static WeaponData Generate(WeaponCategoryData category, int seed) =>
            WeaponGenerator.Generate(category, Seed.From(seed)).Data;

        [Test]
        public void AVariantsOwnSettingsReplaceTheModels()
        {
            WeaponCategoryData category = CreateGlockCategory();
            bool sawEach = false, sawGlock17 = false, sawGlock18 = false;

            for (int i = 0; i < SeedCount && !sawEach; i++)
            {
                WeaponData d = Generate(category, i);
                Assert.AreEqual(AmmoType.HeavyRounds, d.AmmoType, "The model's ammo applies to every variant");

                if (d.WeaponName == "Glock 17")
                {
                    sawGlock17 = true;
                    Assert.AreEqual(FireMode.Semi, d.FireMode);
                    Assert.AreEqual(5, d.RoundsPerLoad);
                    Assert.AreEqual(0.1f, d.RecoilHorizontalBias);
                    Assert.AreEqual(HorizontalDriftMode.Alternating, d.HorizontalDriftMode);
                    Assert.AreEqual(0.2f, d.MuzzleFlip);
                }
                else
                {
                    sawGlock18 = true;
                    Assert.AreEqual("Glock 18", d.WeaponName);
                    Assert.AreEqual(FireMode.Auto, d.FireMode);
                    Assert.AreEqual(0, d.RoundsPerLoad);
                    Assert.AreEqual(0.45f, d.RecoilHorizontalBias);
                    Assert.AreEqual(HorizontalDriftMode.OneWay, d.HorizontalDriftMode);
                    Assert.AreEqual(0.25f, d.MuzzleFlip);
                }
                sawEach = sawGlock17 && sawGlock18;
            }
            Assert.IsTrue(sawEach, "The seeds never rolled both variants");
        }

        [Test]
        public void WithoutAModelTheCategoryDecides()
        {
            _category = ScriptableObject.CreateInstance<WeaponCategoryData>();
            _category.RecoilHorizontalBias = new FloatRange(-0.2f, 0.2f);

            for (int i = 0; i < SeedCount; i++)
            {
                WeaponData d = Generate(_category, i);
                Assert.AreEqual(_category.Names[0], d.WeaponName);
                Assert.AreEqual(_category.AmmoType, d.AmmoType);
                Assert.That(d.RecoilHorizontalBias, Is.InRange(-0.2f, 0.2f));
                Assert.AreEqual(WeaponData.BalancedMuzzleFlip, d.MuzzleFlip);
            }
        }

        [Test]
        public void AFixedMagazineSizeIgnoresTheRoll()
        {
            _category = ScriptableObject.CreateInstance<WeaponCategoryData>();
            _category.MagazineSize = new IntRange(4, 10);
            _category.Models = new[] { new WeaponModel { Name = "Double Barrel", FixedMagazineSize = 2 } };

            for (int i = 0; i < SeedCount; i++)
                Assert.AreEqual(2, Generate(_category, i).MagazineSize);
        }

        [Test]
        public void TheSameSeedGivesTheSameGun()
        {
            WeaponCategoryData category = CreateGlockCategory();

            for (int i = 0; i < SeedCount; i++)
            {
                WeaponData a = Generate(category, i), b = Generate(category, i);
                Assert.AreEqual(a.WeaponName, b.WeaponName);
                Assert.AreEqual(a.Damage, b.Damage);
                Assert.AreEqual(a.RecoilScale, b.RecoilScale);
                Assert.AreEqual(a.MaxAccumulatedHorizontalRecoil, b.MaxAccumulatedHorizontalRecoil);
            }
        }
    }
}
