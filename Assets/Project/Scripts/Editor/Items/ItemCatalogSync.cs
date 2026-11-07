using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using CGD.Items;

namespace CGD.Editor
{
    // Keeps every ItemCatalog listing every ItemDefinition in the project, keyed by asset GUID,
    // so a new item is saveable the moment it is created and nobody maintains the list by hand.
    // Runs after asset imports, deletions and moves; writes only when the list actually changed.
    public class ItemCatalogSync : AssetPostprocessor
    {
        private static bool _scheduled;

        private static void OnPostprocessAllAssets(string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            if (_scheduled || !AnyAsset(imported, deleted, moved)) return;

            // After the import finishes: loading assets mid-import isn't reliable.
            _scheduled = true;
            EditorApplication.delayCall += () =>
            {
                _scheduled = false;
                SyncAll();
            };
        }

        [MenuItem("CGD/Items/Sync Item Catalog")]
        public static void SyncAll()
        {
            List<(string id, ItemDefinition definition)> entries = FindDefinitions();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ItemCatalog)))
            {
                var catalog = AssetDatabase.LoadAssetAtPath<ItemCatalog>(AssetDatabase.GUIDToAssetPath(guid));
                if (catalog == null || Matches(catalog, entries)) continue;

                catalog.SetEntries(entries);
                EditorUtility.SetDirty(catalog);
                AssetDatabase.SaveAssetIfDirty(catalog);
            }
        }

        // Ordered by path, so the catalog asset diffs cleanly in version control.
        private static List<(string id, ItemDefinition definition)> FindDefinitions()
        {
            var entries = new List<(string id, ItemDefinition definition, string path)>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(ItemDefinition)))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(path);
                if (definition != null) entries.Add((guid, definition, path));
            }
            return entries.OrderBy(e => e.path, StringComparer.Ordinal).Select(e => (e.id, e.definition)).ToList();
        }

        private static bool Matches(ItemCatalog catalog, List<(string id, ItemDefinition definition)> entries)
        {
            IReadOnlyList<ItemCatalog.Entry> current = catalog.Entries;
            if (current.Count != entries.Count) return false;

            for (int i = 0; i < entries.Count; i++)
                if (current[i].Id != entries[i].id || current[i].Definition != entries[i].definition) return false;
            return true;
        }

        private static bool AnyAsset(params string[][] pathLists) =>
            pathLists.Any(paths => paths.Any(p => p.EndsWith(".asset", StringComparison.OrdinalIgnoreCase)));
    }
}
