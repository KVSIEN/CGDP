using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CGD.BehaviorTrees
{
    // What a node is called wherever it is shown — the tree editor's canvas and trace, the
    // spectator's in-game inspector: the last part of its [BtNodeInfo] menu path ("Take
    // Cover"), else its class name spaced out. Cached per type, so reading it allocates nothing.
    public static class BtNodeNames
    {
        private static readonly Dictionary<Type, string> Names = new();

        public static string Of(Type type)
        {
            if (Names.TryGetValue(type, out string name)) return name;

            string path = type.GetCustomAttribute<BtNodeInfoAttribute>()?.Path;
            name = path != null ? path[(path.LastIndexOf('/') + 1)..] : Spaced(type.Name);
            Names[type] = name;
            return name;
        }

        public static string Of(BtNode node) => node != null ? Of(node.GetType()) : string.Empty;

        // "Take Cover · Fight": its name and its settings summary, when it has one.
        public static string WithSummary(BtNode node)
        {
            string summary = node?.Summary;
            return string.IsNullOrEmpty(summary) ? Of(node) : $"{Of(node)} · {summary}";
        }

        // "TakeCover" → "Take Cover".
        internal static string Spaced(string identifier)
        {
            var text = new StringBuilder(identifier.Length + 4);
            for (int i = 0; i < identifier.Length; i++)
            {
                char c = identifier[i];
                if (i > 0 && char.IsUpper(c) && !char.IsUpper(identifier[i - 1])) text.Append(' ');
                text.Append(c);
            }
            return text.ToString();
        }
    }
}
