using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CGD.Core;
using CGD.Items;
using CGD.Loot;
using CGD.Weapons;

namespace CGD.Tests
{
    // Rarity checks run from the highest tier down; a tier with a pool swaps the drawn gear
    // for a roll of that pool, all at that tier.
    public class LootTableTests
    {
        private readonly List<Object> _created = new();

        [TearDown]
        public void DestroyCreated()
        {
            foreach (Object o in _created) Object.DestroyImmediate(o);
            _created.Clear();
        }

        private T Create<T>() where T : ScriptableObject
        {
            var o = ScriptableObject.CreateInstance<T>();
            _created.Add(o);
            return o;
        }

        // A table that draws `content` (an item, or another table to roll) once per roll, with
        // these tier checks.
        private LootTable CreateTable(Object content, params (ItemTier tier, float chance, LootTable pool)[] checks)
        {
            var table      = Create<LootTable>();
            var serialized = new SerializedObject(table);

            SerializedProperty pool = serialized.FindProperty("_pool");
            pool.arraySize = 1;
            SerializedProperty entry = pool.GetArrayElementAtIndex(0);
            bool nested = content is LootTable;
            entry.FindPropertyRelative("Kind").enumValueIndex = (int)(nested ? LootEntryKind.Table : LootEntryKind.Item);
            entry.FindPropertyRelative(nested ? "Table" : "Item").objectReferenceValue = content;
            entry.FindPropertyRelative("Weight").floatValue        = 1f;

            SerializedProperty tiers = serialized.FindProperty("_tierChecks");
            tiers.arraySize = checks.Length;
            for (int i = 0; i < checks.Length; i++)
            {
                SerializedProperty check = tiers.GetArrayElementAtIndex(i);
                check.FindPropertyRelative("Tier").enumValueIndex      = (int)checks[i].tier;
                check.FindPropertyRelative("Chance").floatValue        = checks[i].chance;
                check.FindPropertyRelative("Pool").objectReferenceValue = checks[i].pool;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        // A table that draws once from `contents` (items or tables), each equally likely.
        private LootTable CreatePool(params Object[] contents)
        {
            var table      = Create<LootTable>();
            var serialized = new SerializedObject(table);
            SerializedProperty pool = serialized.FindProperty("_pool");
            pool.arraySize = contents.Length;
            for (int i = 0; i < contents.Length; i++)
            {
                SerializedProperty entry = pool.GetArrayElementAtIndex(i);
                bool nested = contents[i] is LootTable;
                entry.FindPropertyRelative("Kind").enumValueIndex = (int)(nested ? LootEntryKind.Table : LootEntryKind.Item);
                entry.FindPropertyRelative(nested ? "Table" : "Item").objectReferenceValue = contents[i];
                entry.FindPropertyRelative("Weight").floatValue = 1f;
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return table;
        }

        private WeaponCategoryData CreateCategory(WeaponType type)
        {
            var category = Create<WeaponCategoryData>();
            category.Type = type;
            return category;
        }

        private LootFilter CreateFilter(LootItemKinds kinds, params WeaponType[] weaponTypes)
        {
            var filter     = Create<LootFilter>();
            var serialized = new SerializedObject(filter);
            serialized.FindProperty("_kinds").intValue = (int)kinds;
            SerializedProperty types = serialized.FindProperty("_weaponTypes");
            types.arraySize = weaponTypes.Length;
            for (int i = 0; i < weaponTypes.Length; i++)
                types.GetArrayElementAtIndex(i).enumValueIndex = (int)weaponTypes[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return filter;
        }

        private static LootDrop RollOne(LootTable table, int seed, LootFilter filter = null)
        {
            var drops = new List<LootDrop>();
            table.Roll(drops, 0f, Seed.From(seed), filter);
            Assert.AreEqual(1, drops.Count);
            return drops[0];
        }

        [Test]
        public void ChecksRunFromTheTopTierDown()
        {
            var rifle = Create<WeaponCategoryData>();
            // Listed out of order on purpose: Epic is still checked before Common.
            LootTable table = CreateTable(rifle, (ItemTier.Common, 1f, null), (ItemTier.Epic, 0.5f, null));

            int epics = 0;
            const int rolls = 400;
            for (int i = 0; i < rolls; i++)
            {
                ItemTier tier = RollOne(table, i).Instance.Tier;
                Assert.That(tier, Is.EqualTo(ItemTier.Epic).Or.EqualTo(ItemTier.Common));
                if (tier == ItemTier.Epic) epics++;
            }
            Assert.That(epics / (float)rolls, Is.InRange(0.4f, 0.6f));
        }

        [Test]
        public void ATiersPoolReplacesTheDrawnGearAtThatTier()
        {
            var rifle  = Create<WeaponCategoryData>();
            var unique = Create<WeaponCategoryData>();
            LootTable legendaryPool = CreateTable(unique, (ItemTier.Common, 1f, null));
            LootTable table         = CreateTable(rifle, (ItemTier.Legendary, 1f, legendaryPool));

            for (int i = 0; i < 20; i++)
            {
                LootDrop drop = RollOne(table, i);
                Assert.AreSame(unique, drop.Definition, "The pool's own checks don't apply");
                Assert.AreEqual(ItemTier.Legendary, drop.Instance.Tier);
            }
        }

        [Test]
        public void TheOutermostTablesChecksDecideForNestedPools()
        {
            var rifle = Create<WeaponCategoryData>();
            LootTable plainPool   = CreateTable(rifle);
            LootTable checkedPool = CreateTable(rifle, (ItemTier.Common, 1f, null));
            LootTable container   = CreateTable(plainPool, (ItemTier.Epic, 1f, null));
            LootTable overriding  = CreateTable(checkedPool, (ItemTier.Epic, 1f, null));

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(ItemTier.Epic, RollOne(container, i).Instance.Tier, "A pool without checks rolls at its container's");
                Assert.AreEqual(ItemTier.Epic, RollOne(overriding, i).Instance.Tier, "The container's checks win over the pool's own");
            }
        }

        [Test]
        public void AFilterOnlyLetsThroughWhatItAllows()
        {
            WeaponCategoryData rifle  = CreateCategory(WeaponType.AR);
            WeaponCategoryData pistol = CreateCategory(WeaponType.Pistol);
            // The rifle sits in a nested table, which is skipped when it holds nothing allowed.
            LootTable table   = CreatePool(CreatePool(rifle), pistol);
            LootFilter pistols = CreateFilter(LootItemKinds.Firearm, WeaponType.Pistol);

            for (int i = 0; i < 20; i++)
                Assert.AreSame(pistol, RollOne(table, i, pistols).Definition);
        }

        [Test]
        public void AFilterThatAllowsNothingIsIgnored()
        {
            WeaponCategoryData rifle = CreateCategory(WeaponType.AR);
            LootTable table = CreatePool(rifle);
            LootFilter melee = CreateFilter(LootItemKinds.Melee);

            for (int i = 0; i < 20; i++)
                Assert.AreSame(rifle, RollOne(table, i, melee).Definition);
        }

        [Test]
        public void WithoutChecksGearKeepsItsOwnTier()
        {
            var rifle = Create<WeaponCategoryData>();
            LootTable table = CreateTable(rifle);

            for (int i = 0; i < 20; i++)
                Assert.AreEqual(rifle.Tier, RollOne(table, i).Instance.Tier);
        }

        [Test]
        public void TheSameSeedGivesTheSameDrop()
        {
            var rifle = Create<WeaponCategoryData>();
            LootTable table = CreateTable(rifle, (ItemTier.Legendary, 0.2f, null), (ItemTier.Common, 1f, null));

            for (int i = 0; i < 20; i++)
                Assert.AreEqual(RollOne(table, i).Instance.Tier, RollOne(table, i).Instance.Tier);
        }
    }
}
