using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Tests
{
    // A weapon's seed splits into layers like a map's: rerolling one layer must leave
    // everything drawn from the others exactly as it was, and no rerolls must give the
    // same weapon the plain seed always gave.
    public class WeaponSeedLayerTests
    {
        private const int SeedCount = 20;
        private const string StyledCategoryPath = "Assets/Project/Data/Weapons/Categories/ModularPistolCategory.asset";

        private static WeaponCategoryData LoadStyledCategory()
        {
            var category = AssetDatabase.LoadAssetAtPath<WeaponCategoryData>(StyledCategoryPath);
            Assert.IsNotNull(category, $"Missing weapon category at {StyledCategoryPath}");
            return category;
        }

        private static StatRollProfile CreateProfile()
        {
            var profile = ScriptableObject.CreateInstance<StatRollProfile>();
            profile.ApplyStandardFirearmAxes();
            return profile;
        }

        private static SeedVariants Rerolled(string layer)
        {
            var variants = new SeedVariants();
            variants.Reroll(layer);
            return variants;
        }

        private static void AssertSameStats(ItemRoll expected, ItemRoll actual)
        {
            Assert.AreEqual(expected.Quality, actual.Quality);
            foreach (ItemStat stat in Enum.GetValues(typeof(ItemStat)))
                if (stat != ItemStat.None)
                    Assert.AreEqual(expected.Desirability(stat), actual.Desirability(stat), $"{stat} moved");
        }

        private static bool SameGun(WeaponInstance a, WeaponInstance b) =>
            a.Data.WeaponName == b.Data.WeaponName && a.Data.FireMode == b.Data.FireMode
            && a.Data.Damage == b.Data.Damage && a.Data.RoundsPerMinute == b.Data.RoundsPerMinute
            && a.Data.RecoilHeatCooldown == b.Data.RecoilHeatCooldown;

        [Test]
        public void NoVariantsGiveThePlainSeed()
        {
            StatRollProfile profile = CreateProfile();

            for (int i = 0; i < SeedCount; i++)
            {
                Seed seed = Seed.From(i);
                ItemRoll plain = profile.Roll(ItemTier.Rare, seed, null);
                var zeroed = new SeedVariants();
                zeroed.Set(ItemRoll.StatsLayer, 0);

                AssertSameStats(plain, profile.Roll(ItemTier.Rare, seed, new SeedVariants()));
                AssertSameStats(plain, profile.Roll(ItemTier.Rare, seed, zeroed));
                Assert.AreEqual(seed.Derive(WeaponGenerator.CharacterLayer).Stream().NextULong(),
                                plain.StreamFor(WeaponGenerator.CharacterLayer).NextULong());
            }
        }

        [Test]
        public void RerollingStatsKeepsTheQuality()
        {
            StatRollProfile profile = CreateProfile();
            bool anyStatMoved = false;

            for (int i = 0; i < SeedCount; i++)
            {
                Seed seed = Seed.From(i);
                ItemRoll plain    = profile.Roll(ItemTier.Epic, seed, null);
                ItemRoll rerolled = profile.Roll(ItemTier.Epic, seed, Rerolled(ItemRoll.StatsLayer));

                Assert.AreEqual(plain.Quality, rerolled.Quality);
                anyStatMoved |= plain.Desirability(ItemStat.Damage) != rerolled.Desirability(ItemStat.Damage);
            }
            Assert.IsTrue(anyStatMoved, "Rerolling stats never changed a stat");
        }

        [Test]
        public void RerollingQualityStaysInTheTier()
        {
            StatRollProfile profile = CreateProfile();

            for (int i = 0; i < SeedCount; i++)
                Assert.AreEqual(ItemTier.Rare, profile.Roll(ItemTier.Rare, Seed.From(i), Rerolled(ItemRoll.QualityLayer)).Tier);
        }

        [Test]
        public void SameSeedAndVariantsGiveTheSameWeapon()
        {
            WeaponCategoryData category = LoadStyledCategory();

            for (int i = 0; i < SeedCount; i++)
            {
                var a = (WeaponInstance)category.Generate(category.Roll(ItemTier.Common, Seed.From(i), Rerolled(WeaponGenerator.CharacterLayer)));
                var b = (WeaponInstance)category.Generate(category.Roll(ItemTier.Common, Seed.From(i), Rerolled(WeaponGenerator.CharacterLayer)));
                Assert.IsTrue(SameGun(a, b), $"Seed {i} gave two different guns");
            }
        }

        [Test]
        public void RerollingTheCharacterKeepsStatsAndPerks()
        {
            WeaponCategoryData category = LoadStyledCategory();
            bool anyGunChanged = false;

            for (int i = 0; i < SeedCount; i++)
            {
                var weapon   = (WeaponInstance)category.Generate(ItemTier.Common, Seed.From(i));
                ItemRoll roll = category.RerollLayer(weapon, WeaponGenerator.CharacterLayer);
                var rerolled = (WeaponInstance)category.Generate(roll);

                AssertSameStats(category.Roll(ItemTier.Common, Seed.From(i)), roll);
                CollectionAssert.AreEqual(weapon.Perks, rerolled.Perks);
                Assert.AreEqual(1, rerolled.Variants.VariantOf(WeaponGenerator.CharacterLayer));
                anyGunChanged |= !SameGun(weapon, rerolled);
            }
            Assert.IsTrue(anyGunChanged, "Rerolling the character never changed a gun");
        }

        [Test]
        public void RerollingPerksKeepsTheGun()
        {
            WeaponCategoryData category = LoadStyledCategory();

            for (int i = 0; i < SeedCount; i++)
            {
                var weapon   = (WeaponInstance)category.Generate(ItemTier.Common, Seed.From(i));
                var rerolled = (WeaponInstance)category.Generate(category.RerollLayer(weapon, "perks"));

                Assert.IsTrue(SameGun(weapon, rerolled), $"Seed {i}: rerolling perks changed the gun");
            }
        }

        [Test]
        public void RerollLayerRejectsUnknownLayers()
        {
            WeaponCategoryData category = LoadStyledCategory();
            WeaponItem weapon = category.Generate(ItemTier.Common, Seed.From(1));

            Assert.Throws<ArgumentException>(() => category.RerollLayer(weapon, "layout"));
        }

        [Test]
        public void VariantsRoundTripThroughText()
        {
            var variants = new SeedVariants();
            variants.Reroll("stats");
            variants.Reroll("stats");
            variants.Reroll("perks");
            variants.Set("weapon", 0);

            Assert.AreEqual("stats:2 perks:1", variants.ToString());
            Assert.IsTrue(SeedVariants.TryParse(variants.ToString(), out SeedVariants parsed));
            Assert.AreEqual(2, parsed.VariantOf("stats"));
            Assert.AreEqual(1, parsed.VariantOf("perks"));
            Assert.IsFalse(SeedVariants.TryParse("stats", out _));
            Assert.IsTrue(new SeedVariants().IsEmpty);
        }

        [Test]
        public void ItemsKeepTheirOwnCopyOfTheVariants()
        {
            WeaponCategoryData category = LoadStyledCategory();
            SeedVariants variants = Rerolled("perks");
            WeaponItem weapon = category.Generate(category.Roll(ItemTier.Common, Seed.From(3), variants));

            variants.Reroll("perks");
            weapon.Variants.Reroll("perks");

            Assert.AreEqual(1, weapon.Variants.VariantOf("perks"));
        }
    }
}
