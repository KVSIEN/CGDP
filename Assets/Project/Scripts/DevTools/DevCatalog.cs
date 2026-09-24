using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Items;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.DevTools
{
    // What the dev console can hand out by name: items, weapon categories, enemy prefabs
    // and buffs. Names match the asset name or display name, ignoring case and spaces;
    // a unique partial match works too ("scrap" → Scrap Metal).
    [CreateAssetMenu(fileName = "DevCatalog", menuName = "CGD/Dev Tools/Dev Catalog")]
    public class DevCatalog : ScriptableObject
    {
        [SerializeField] private ItemDefinition[]     _items      = Array.Empty<ItemDefinition>();
        [SerializeField] private WeaponCategoryData[] _weapons    = Array.Empty<WeaponCategoryData>();
        [SerializeField] private GameObject[]         _enemies    = Array.Empty<GameObject>();
        [SerializeField] private StatModifierPreset[] _buffs      = Array.Empty<StatModifierPreset>();

        public IReadOnlyList<ItemDefinition>     Items   => _items;
        public IReadOnlyList<WeaponCategoryData> Weapons => _weapons;
        public IReadOnlyList<GameObject>         Enemies => _enemies;
        public IReadOnlyList<StatModifierPreset> Buffs   => _buffs;

        // Exact match first, then a single partial match. Null (with a reason) otherwise.
        public static T Find<T>(IReadOnlyList<T> options, string query, Func<T, string> displayName, out string error) where T : UnityEngine.Object
        {
            error = null;
            string key = Normalize(query);
            T partial = null;
            int partialCount = 0;

            foreach (T option in options)
            {
                if (option == null) continue;

                string a = Normalize(option.name), b = Normalize(displayName?.Invoke(option));
                if (a == key || b == key) return option;
                if (!a.Contains(key) && !b.Contains(key)) continue;

                partial = option;
                partialCount++;
            }

            if (partialCount == 1) return partial;
            error = partialCount == 0 ? $"Nothing called '{query}'." : $"'{query}' matches {partialCount} entries — be more specific.";
            return null;
        }

        private static string Normalize(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty : text.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }
}
