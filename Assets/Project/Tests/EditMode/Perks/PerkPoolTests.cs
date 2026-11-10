using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Perks;
using CGD.Weapons;

namespace CGD.Tests
{
    public class PerkPoolTests
    {
        private const int SeedCount = 20;

        private MagnetizedPerk     _perk;
        private PerkPool           _pool;
        private WeaponCategoryData _category;

        // A pool holding only a Legendary-only perk, one perk per item at every tier.
        [SetUp]
        public void CreatePool()
        {
            _perk = ScriptableObject.CreateInstance<MagnetizedPerk>();

            _pool = ScriptableObject.CreateInstance<PerkPool>();
            var pool = new SerializedObject(_pool);
            SerializedProperty perks = pool.FindProperty("_perks");
            perks.arraySize = 1;
            SerializedProperty entry = perks.GetArrayElementAtIndex(0);
            entry.FindPropertyRelative("Perk").objectReferenceValue = _perk;
            entry.FindPropertyRelative("Weight").floatValue        = 1f;
            entry.FindPropertyRelative("MinTier").enumValueIndex   = (int)ItemTier.Legendary;
            SerializedProperty perTier = pool.FindProperty("_perksPerTier");
            for (int i = 0; i < perTier.arraySize; i++) perTier.GetArrayElementAtIndex(i).intValue = 1;
            pool.FindProperty("_extraPerkChance").floatValue = 0f;
            pool.ApplyModifiedPropertiesWithoutUndo();

            _category = ScriptableObject.CreateInstance<WeaponCategoryData>();
            var category = new SerializedObject(_category);
            category.FindProperty("_perkPool").objectReferenceValue = _pool;
            category.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void Destroy()
        {
            Object.DestroyImmediate(_category);
            Object.DestroyImmediate(_pool);
            Object.DestroyImmediate(_perk);
        }

        private WeaponInstance Generate(ItemTier tier, int seed) =>
            WeaponGenerator.Generate(_category, _category.Roll(tier, Seed.From(seed)));

        [Test]
        public void APerkNeverRollsBelowItsMinimumTier()
        {
            for (int i = 0; i < SeedCount; i++)
                Assert.IsEmpty(Generate(ItemTier.Epic, i).Perks);
        }

        [Test]
        public void APerkRollsAtItsMinimumTier()
        {
            for (int i = 0; i < SeedCount; i++)
            {
                WeaponInstance weapon = Generate(ItemTier.Legendary, i);
                CollectionAssert.AreEqual(new GearPerk[] { _perk }, weapon.Perks);
                Assert.AreEqual(_perk.DamageKeptPerTarget, weapon.PierceDamageKept);
            }
        }
    }
}
