using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

namespace CGD.Editor
{
    // The searchable "Create Node" menu (Space, or right-click › Create Node): every node
    // type grouped by its BtNodeInfo path.
    internal sealed class BtNodeSearchProvider : ScriptableObject, ISearchWindowProvider
    {
        private Action<Type, Vector2> _create;

        public void Initialize(Action<Type, Vector2> create) => _create = create;

        public List<SearchTreeEntry> CreateSearchTree(SearchWindowContext context)
        {
            var tree   = new List<SearchTreeEntry> { new SearchTreeGroupEntry(new GUIContent("Create Node")) };
            var groups = new HashSet<string>();

            foreach (Type type in BtNodeTypes.Creatable)
            {
                string[] parts = BtNodeTypes.Path(type).Split('/');
                string   group = string.Empty;

                for (int level = 0; level < parts.Length - 1; level++)
                {
                    group += parts[level] + "/";
                    if (groups.Add(group))
                        tree.Add(new SearchTreeGroupEntry(new GUIContent(parts[level]), level + 1));
                }

                tree.Add(new SearchTreeEntry(new GUIContent(parts[^1], BtNodeTypes.Description(type)))
                {
                    level    = parts.Length,
                    userData = type,
                });
            }
            return tree;
        }

        public bool OnSelectEntry(SearchTreeEntry entry, SearchWindowContext context)
        {
            if (entry.userData is not Type type) return false;
            _create?.Invoke(type, context.screenMousePosition);
            return true;
        }
    }
}
