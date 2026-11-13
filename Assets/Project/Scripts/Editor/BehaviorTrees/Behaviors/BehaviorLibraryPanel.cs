using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The Behavior Composer's library section: every behavior by category, searchable, each with
    // its description, when it acts, the kit pieces it needs (flagged when the enemy being
    // composed lacks them) and how many trees use it. Reading the library walks every tree
    // asset, so it is cached and refreshed when the Composer regains focus or edits it.
    internal sealed class BehaviorLibraryPanel
    {
        private readonly Dictionary<BehaviorTreeAsset, int>           _users = new();
        private readonly Dictionary<BehaviorTreeAsset, List<KitNeed>> _needs = new();
        private readonly Dictionary<BehaviorTreeAsset, string>        _when  = new();
        private bool   _expanded;
        private string _search = "";

        public List<BehaviorLibrary.Entry> Entries { get; private set; } = new();

        public void Refresh()
        {
            Entries = BehaviorLibrary.Entries();
            _users.Clear();
            _needs.Clear();
            _when.Clear();
            foreach (BehaviorLibrary.Entry entry in Entries)
            {
                _users[entry.Asset] = BehaviorLibrary.TreesUsing(entry.Asset).Count;
                _needs[entry.Asset] = KitNeed.Of(EnemyTreeContents.Nodes(entry.Asset));
                _when[entry.Asset]  = BehaviorTrigger.Describe(entry.Asset, entry.Asset.Root);
            }
        }

        // How many trees run this behavior (0 for one outside the library).
        public int Users(BehaviorTreeAsset behavior) =>
            behavior != null && _users.TryGetValue(behavior, out int count) ? count : 0;

        public void Draw(EnemyData enemy)
        {
            EditorGUILayout.Space(10f);
            _expanded = EditorGUILayout.Foldout(_expanded, $"Behavior library ({Entries.Count})", true, EditorStyles.foldoutHeader);
            if (!_expanded) return;

            EditorGUILayout.LabelField(
                $"Every tree in {BehaviorLibrary.BehaviorsFolder.Replace("Assets/Project/Data/", "")} (sub-folders are categories) and Shared/. " +
                "A behavior's description is its Root's comment.", EditorStyles.wordWrappedMiniLabel);
            _search = EditorGUILayout.TextField(new GUIContent("Search", "Name, category or description"), _search);

            string category = null;
            foreach (BehaviorLibrary.Entry entry in Entries)
            {
                if (!Matches(entry, _search)) continue;
                if (entry.Category != category)
                {
                    category = entry.Category;
                    EditorGUILayout.LabelField(string.IsNullOrEmpty(category) ? "Uncategorised" : category, EditorStyles.miniBoldLabel);
                }
                DrawEntry(entry, enemy);
            }
        }

        private void DrawEntry(BehaviorLibrary.Entry entry, EnemyData enemy)
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.VerticalScope())
                {
                    if (GUILayout.Button(entry.Asset.name, EditorStyles.linkLabel)) BehaviorTreeWindow.Open(entry.Asset);
                    if (entry.Description.Length > 0) GUILayout.Label(entry.Description, EditorStyles.wordWrappedMiniLabel);
                    if (_when.TryGetValue(entry.Asset, out string when)) GUILayout.Label("When: " + when, EditorStyles.wordWrappedMiniLabel);
                    DrawNeeds(entry.Asset, enemy);
                }
                int users = Users(entry.Asset);
                GUILayout.Label($"in {users} tree{(users == 1 ? "" : "s")}", EditorStyles.miniLabel, GUILayout.Width(60f));
            }
        }

        private void DrawNeeds(BehaviorTreeAsset behavior, EnemyData enemy)
        {
            if (!_needs.TryGetValue(behavior, out List<KitNeed> needs) || needs.Count == 0) return;

            string line = "Needs kit: " + KitNeed.Names(needs);
            if (enemy != null)
            {
                List<KitNeed> missing = KitNeed.Missing(enemy, needs);
                if (missing.Count > 0) line += $"   ⚠ {enemy.name} has no {KitNeed.Names(missing)}";
            }
            GUILayout.Label(line, EditorStyles.wordWrappedMiniLabel);
        }

        private static bool Matches(BehaviorLibrary.Entry entry, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return true;
            string term = search.Trim();
            return Contains(entry.Asset.name, term) || Contains(entry.Category, term) || Contains(entry.Description, term);
        }

        private static bool Contains(string text, string term) =>
            !string.IsNullOrEmpty(text) && text.IndexOf(term, StringComparison.OrdinalIgnoreCase) >= 0;
    }
}
