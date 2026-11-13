using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;
using CGD.Enemies;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace CGD.Editor
{
    // The Behavior Composer's "+ Add Behavior" list: the library by category, with a search box
    // (the library runs to dozens of behaviors). A behavior already in the list is greyed out,
    // and one needing kit pieces the enemy leaves empty says so — it can still be added, since
    // the designer may be about to fill the kit in.
    internal sealed class BehaviorPicker : AdvancedDropdown
    {
        private readonly List<BehaviorLibrary.Entry>  _entries;
        private readonly HashSet<BehaviorTreeAsset>    _present;
        private readonly EnemyData                     _enemy;
        private readonly Action<BehaviorTreeAsset>     _picked;
        private readonly Action                        _newEmpty;

        private sealed class Item : AdvancedDropdownItem
        {
            public readonly BehaviorTreeAsset Behavior;   // null for "New Empty Behavior…"

            public Item(string name, BehaviorTreeAsset behavior) : base(name) => Behavior = behavior;
        }

        public BehaviorPicker(List<BehaviorLibrary.Entry> entries, HashSet<BehaviorTreeAsset> present, EnemyData enemy,
                              Action<BehaviorTreeAsset> picked, Action newEmpty)
            : base(new AdvancedDropdownState())
        {
            _entries  = entries;
            _present  = present;
            _enemy    = enemy;
            _picked   = picked;
            _newEmpty = newEmpty;
            minimumSize = new Vector2(320f, 380f);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Add Behavior");
            root.AddChild(new Item("New Empty Behavior…", null));
            root.AddSeparator();

            var categories = new Dictionary<string, AdvancedDropdownItem>();
            foreach (BehaviorLibrary.Entry entry in _entries)
            {
                string category = string.IsNullOrEmpty(entry.Category) ? "Uncategorised" : entry.Category;
                if (!categories.TryGetValue(category, out AdvancedDropdownItem folder))
                {
                    folder = new AdvancedDropdownItem(category);
                    categories[category] = folder;
                    root.AddChild(folder);
                }
                folder.AddChild(ItemFor(entry));
            }
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is not Item picked) return;
            if (picked.Behavior == null) _newEmpty();
            else _picked(picked.Behavior);
        }

        private Item ItemFor(BehaviorLibrary.Entry entry)
        {
            string name = entry.Asset.name;
            if (_present.Contains(entry.Asset))
                return new Item(name + "   (already in this list)", entry.Asset) { enabled = false };

            if (_enemy != null)
            {
                List<KitNeed> missing = KitNeed.Missing(_enemy, KitNeed.Of(EnemyTreeContents.Nodes(entry.Asset)));
                if (missing.Count > 0) name += $"   ⚠ needs {KitNeed.Names(missing)}";
            }
            return new Item(name, entry.Asset);
        }
    }
}
