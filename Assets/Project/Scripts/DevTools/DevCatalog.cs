using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using CGD.Items;
using CGD.Player;
using CGD.Stats;
using CGD.Weapons;

namespace CGD.DevTools
{
    // What the dev console can hand out by name: items, weapon categories, enemy prefabs,
    // buffs and dodge types. Items and weapon categories come from the ItemCatalog, so every
    // item in the project can be handed out without listing it here. Names match the asset name or display name, ignoring case and spaces;
    // a unique partial match works too ("scrap" → Scrap Metal).
    [CreateAssetMenu(fileName = "DevCatalog", menuName = "CGD/Dev Tools/Dev Catalog")]
    public class DevCatalog : ScriptableObject
    {
        [SerializeField] private ItemCatalog          _itemCatalog;
        [SerializeField] private GameObject[]         _enemies    = Array.Empty<GameObject>();
        [SerializeField] private StatModifierPreset[] _buffs      = Array.Empty<StatModifierPreset>();
        [SerializeField] private DodgeDefinition[]    _dodges     = Array.Empty<DodgeDefinition>();

        // Everything but weapon categories, which /weapon rolls instead (see Weapons).
        public IReadOnlyList<ItemDefinition>     Items   => FromItemCatalog<ItemDefinition>(d => d is not WeaponCategory);
        public IReadOnlyList<WeaponCategory>     Weapons => FromItemCatalog<WeaponCategory>(_ => true);
        public IReadOnlyList<GameObject>         Enemies => _enemies;
        public IReadOnlyList<StatModifierPreset> Buffs   => _buffs;
        public IReadOnlyList<DodgeDefinition>    Dodges  => _dodges;

        // Built on each call rather than cached: it's only read while typing in the console, and
        // the catalog can change under a cache in the editor.
        private List<T> FromItemCatalog<T>(Func<T, bool> include) where T : ItemDefinition
        {
            var found = new List<T>();
            if (_itemCatalog == null) return found;

            foreach (ItemDefinition definition in _itemCatalog.Definitions)
                if (definition is T match && include(match)) found.Add(match);
            return found;
        }

        // Exact match first, then a single partial match. Null (with a reason) otherwise.
        public static T Find<T>(IReadOnlyList<T> options, string query, Func<T, string> displayName, out string error) where T : UnityEngine.Object
        {
            error = null;
            string key = Normalize(query);
            T partial = null;
            var partialNames = new List<string>();

            foreach (T option in options)
            {
                if (option == null) continue;

                string a = Normalize(option.name), b = Normalize(displayName?.Invoke(option));
                if (a == key || b == key) return option;
                if (!a.Contains(key) && !b.Contains(key)) continue;

                partial = option;
                partialNames.Add(Token(option, displayName));
            }

            if (partialNames.Count == 1) return partial;
            error = partialNames.Count == 0
                ? $"Nothing called '{query}'."
                : $"'{query}' matches {partialNames.Count} entries ({string.Join(", ", partialNames.Take(6))}{(partialNames.Count > 6 ? ", …" : "")}) — be more specific.";
            return null;
        }

        // The one-word form of an option — what the console completes to and /list prints.
        // Spaces are dropped because Find ignores them, so it never needs quoting.
        public static string Token<T>(T option, Func<T, string> displayName) where T : UnityEngine.Object
        {
            string display = displayName?.Invoke(option);
            return (string.IsNullOrEmpty(display) ? option.name : display).Replace(" ", "");
        }

        public static List<string> Tokens<T>(IReadOnlyList<T> options, Func<T, string> displayName) where T : UnityEngine.Object
        {
            var tokens = new List<string>();
            if (options == null) return tokens;

            foreach (T option in options)
                if (option != null) tokens.Add(Token(option, displayName));
            return tokens;
        }

        private static string Normalize(string text) =>
            string.IsNullOrEmpty(text) ? string.Empty : text.Replace(" ", "").Replace("_", "").ToLowerInvariant();
    }
}
