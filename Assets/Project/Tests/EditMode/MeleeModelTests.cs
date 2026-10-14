using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Tests
{
    // A melee model names the weapon, stretches its type's ranges and can change its grip;
    // a type without models rolls from its names and keeps its own grip.
    public class MeleeModelTests
    {
        private const int SeedCount = 20;

        private MeleeCategoryData _category;

        // A one-handed type whose stats roll to fixed values.
        [SetUp]
        public void CreateCategory()
        {
            _category = ScriptableObject.CreateInstance<MeleeCategoryData>();
            _category.Damage      = new FloatRange(20f, 20f);
            _category.AttackSpeed = new FloatRange(1f, 1f);
            _category.Reach       = new FloatRange(1f, 1f);

            var serialized = new SerializedObject(_category);
            serialized.FindProperty("_oneHanded").boolValue = true;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void DestroyCategory() => Object.DestroyImmediate(_category);

        private MeleeWeaponInstance Generate(int seed) => MeleeWeaponGenerator.Generate(_category, Seed.From(seed));

        [Test]
        public void AModelScalesItsTypeAndSetsItsGrip()
        {
            MeleeScales scales = MeleeScales.Identity;
            scales.Damage      = 1.5f;
            scales.AttackSpeed = 0.8f;
            scales.Reach       = 1.2f;
            _category.Models = new[] { new MeleeModel { Name = "Claymore", Scales = scales, Grip = MeleeGrip.TwoHanded } };

            for (int i = 0; i < SeedCount; i++)
            {
                MeleeWeaponInstance weapon = Generate(i);
                Assert.AreEqual("Claymore", weapon.Data.WeaponName);
                Assert.AreEqual(30f, weapon.Data.LightCombo[0].Damage, 1e-3f);
                Assert.AreEqual(0.8f, weapon.Data.AttackSpeed, 1e-4f);
                Assert.AreEqual(_category.LightCombo[0].Range * 1.2f, weapon.Data.LightCombo[0].Range, 1e-4f);
                Assert.IsFalse(weapon.IsOneHanded, "A two-handed model of a one-handed type");
            }
        }

        [Test]
        public void AModelHeldAsItsTypeKeepsTheTypesGrip()
        {
            _category.Models = new[] { new MeleeModel { Name = "Sabre", Scales = MeleeScales.Identity } };

            for (int i = 0; i < SeedCount; i++)
                Assert.IsTrue(Generate(i).IsOneHanded);
        }

        [Test]
        public void WithoutModelsTheTypeDecides()
        {
            for (int i = 0; i < SeedCount; i++)
            {
                MeleeWeaponInstance weapon = Generate(i);
                Assert.AreEqual(_category.Names[0], weapon.Data.WeaponName);
                Assert.AreEqual(20f, weapon.Data.LightCombo[0].Damage, 1e-3f);
                Assert.IsTrue(weapon.IsOneHanded);
            }
        }
    }
}
