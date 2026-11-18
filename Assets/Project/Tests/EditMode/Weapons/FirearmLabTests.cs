using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using CGD.Editor;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Tests
{
    // The Firearm Lab's numbers: rolls are credited to the model that made them, weights decide
    // how often a model comes up, and the starting templates give models that can roll.
    public class FirearmLabTests
    {
        private WeaponCategoryData _category;

        [TearDown]
        public void DestroyCategory()
        {
            if (_category != null) Object.DestroyImmediate(_category);
        }

        private WeaponCategoryData CreateCategory(params WeaponModel[] models)
        {
            _category = ScriptableObject.CreateInstance<WeaponCategoryData>();
            _category.Models = models;
            return _category;
        }

        [Test]
        public void EachRollIsCreditedToItsOwnModel()
        {
            var common = new WeaponModel { Name = "Common Gun", Weight = 1f };
            var rare   = new WeaponModel { Name = "Rare Gun",   Weight = 1f };
            WeaponCategoryData category = CreateCategory(common, rare);

            List<FirearmSample> samples = FirearmSampler.Roll(category, ItemTier.Common, 60, 7);

            Assert.AreEqual(60, samples.Count);
            foreach (FirearmSample sample in samples)
                Assert.AreEqual(sample.Model.Name, sample.Name, "The weapon is named after the model it was credited to");
        }

        [Test]
        public void AModelWithNoWeightIsNeverRolled()
        {
            var never  = new WeaponModel { Name = "Never", Weight = 0f };
            var always = new WeaponModel { Name = "Always", Weight = 1f };
            WeaponCategoryData category = CreateCategory(never, always);

            List<FirearmSample> samples = FirearmSampler.Roll(category, ItemTier.Common, 60, 3);

            Assert.IsEmpty(FirearmSampler.Of(samples, never));
            Assert.AreEqual(60, FirearmSampler.Of(samples, always).Count);
        }

        [Test]
        public void TheSameSeedRollsTheSameGuns()
        {
            WeaponCategoryData category = CreateCategory(new WeaponModel { Name = "A" }, new WeaponModel { Name = "B" });

            List<FirearmSample> first  = FirearmSampler.Roll(category, ItemTier.Rare, 20, 11);
            List<FirearmSample> second = FirearmSampler.Roll(category, ItemTier.Rare, 20, 11);

            for (int i = 0; i < first.Count; i++)
                Assert.AreEqual(first[i][FirearmStat.Dps], second[i][FirearmStat.Dps]);
        }

        [Test]
        public void TimeToKillCountsTheShotsNeeded()
        {
            // 4 shots at 25 damage kill 100 health; the first shot lands at once, so 3 intervals pass.
            Assert.AreEqual(3f * 60f / 600f, FirearmSampler.TimeToKill(25f, 600f), 0.0001f);
            Assert.IsTrue(float.IsInfinity(FirearmSampler.TimeToKill(0f, 600f)));
        }

        [Test]
        public void EveryArchetypeIsAModelThatRolls()
        {
            foreach (FirearmTemplates.Archetype archetype in FirearmTemplates.Archetypes)
            {
                WeaponModel model = archetype.Create();
                Assert.IsFalse(string.IsNullOrEmpty(model.Name), archetype.Name);

                WeaponCategoryData category = CreateCategory(model);
                List<FirearmSample> samples = FirearmSampler.Roll(category, ItemTier.Common, 10, 5);
                Assert.AreEqual(10, samples.Count, archetype.Name);
                Object.DestroyImmediate(category);
                _category = null;
            }
        }

        [Test]
        public void ACopiedModelSharesNothingWithItsSource()
        {
            WeaponModel source = FirearmTemplates.Archetypes[1].Create();
            WeaponModel copy   = FirearmTemplates.Copy(source);

            copy.Recoil.Bias = 0.9f;
            copy.FiringStyles[0].Weight = 9f;

            Assert.AreNotEqual(0.9f, source.Recoil.Bias);
            Assert.AreNotEqual(9f, source.FiringStyles[0].Weight);
        }
    }
}
