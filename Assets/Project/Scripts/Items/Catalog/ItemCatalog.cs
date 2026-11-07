using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Items
{
    // Every item definition under a stable id, so saved data can name an item without holding
    // a reference to its asset. The id is the asset's GUID: renaming or moving an asset keeps
    // it, deleting the asset retires it.
    //
    // The editor keeps the list in sync with the project (ItemCatalogSync) — no hand upkeep.
    [CreateAssetMenu(fileName = "DefaultItemCatalog", menuName = "CGD/Items/Item Catalog")]
    public class ItemCatalog : ScriptableObject
    {
        [Serializable]
        internal struct Entry
        {
            public string         Id;
            public ItemDefinition Definition;
        }

        [SerializeField] private List<Entry> _entries = new();

        private Dictionary<string, ItemDefinition> _byId;
        private Dictionary<ItemDefinition, string> _byDefinition;
        private List<ItemDefinition>               _definitions;

        // Every listed definition, in the catalog's order (by asset path).
        public IReadOnlyList<ItemDefinition> Definitions
        {
            get
            {
                EnsureLookup();
                return _definitions;
            }
        }

        public bool TryGet(string id, out ItemDefinition definition)
        {
            EnsureLookup();
            definition = null;
            return !string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out definition);
        }

        // Null for a definition the catalog doesn't list (e.g. one created at runtime).
        public string IdOf(ItemDefinition definition)
        {
            EnsureLookup();
            return definition != null && _byDefinition.TryGetValue(definition, out string id) ? id : null;
        }

        internal IReadOnlyList<Entry> Entries => _entries;

        // For the editor sync and tests.
        internal void SetEntries(IEnumerable<(string id, ItemDefinition definition)> entries)
        {
            _entries.Clear();
            foreach ((string id, ItemDefinition definition) in entries)
                _entries.Add(new Entry { Id = id, Definition = definition });
            _byId = null;
        }

        // The serialized list may change under us in the editor (sync, undo, inspector edits).
        private void OnValidate() => _byId = null;

        private void EnsureLookup()
        {
            if (_byId != null) return;

            _byId         = new Dictionary<string, ItemDefinition>(_entries.Count);
            _byDefinition = new Dictionary<ItemDefinition, string>(_entries.Count);
            _definitions  = new List<ItemDefinition>(_entries.Count);
            foreach (Entry entry in _entries)
            {
                if (string.IsNullOrEmpty(entry.Id) || entry.Definition == null) continue;
                _byId[entry.Id] = entry.Definition;
                if (_byDefinition.TryAdd(entry.Definition, entry.Id)) _definitions.Add(entry.Definition);
            }
        }
    }
}
