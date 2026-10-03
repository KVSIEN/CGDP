using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Core;
using CGD.Weapons;

namespace CGD.Expedition
{
    // What the starting room hands the player each run (GDD › Starting Room): a random but
    // viable loadout — a few freshly generated weapons from these categories, plus supplies.
    // Random, not tailored to the map: every run starts blind.
    [CreateAssetMenu(fileName = "RunStarterKit", menuName = "CGD/Expedition/Run Starter Kit")]
    public class RunStarterKit : ScriptableObject
    {
        [Tooltip("Categories the starting weapons are generated from; no category twice in one run")]
        [SerializeField] private WeaponCategoryData[] _weaponCategories = Array.Empty<WeaponCategoryData>();
        [SerializeField] private IntRange _weaponCount = new(1, 2);
        [Tooltip("Consumables, ammunition and crafting materials, each rolled within its count")]
        [SerializeField] private List<StarterSupply> _supplies = new();

        public IReadOnlyList<StarterSupply> Supplies => _supplies;

        // WeaponCount distinct categories, in random order.
        public List<WeaponCategoryData> PickWeaponCategories(RandomStream random)
        {
            var pool = new List<WeaponCategoryData>();
            foreach (WeaponCategoryData category in _weaponCategories)
                if (category != null && !pool.Contains(category)) pool.Add(category);

            random.Shuffle(pool);
            int count = Mathf.Clamp(_weaponCount.Evaluate(random), 0, pool.Count);
            return pool.GetRange(0, count);
        }
    }
}
