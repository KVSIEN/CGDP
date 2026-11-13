using System.Collections.Generic;
using System.IO;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The behaviors enemy types can be built from: every tree asset in Behaviors/ (one
    // sub-folder per category — Fight, Reactions, Idle… — chosen by whoever saves one) and in
    // Shared/ (regroup, raise alarm, routine). A behavior's description is its Root's comment.
    internal static class BehaviorLibrary
    {
        public const string TreesFolder     = "Assets/Project/Data/Enemies/BehaviorTrees";
        public const string BehaviorsFolder = TreesFolder + "/Behaviors";
        public const string SharedFolder    = TreesFolder + "/Shared";

        public readonly struct Entry
        {
            public readonly BehaviorTreeAsset Asset;
            public readonly string            Category;
            public readonly string            Description;

            public Entry(BehaviorTreeAsset asset, string category, string description)
            {
                Asset       = asset;
                Category    = category;
                Description = description;
            }

            public string MenuPath => string.IsNullOrEmpty(Category) ? Asset.name : $"{Category}/{Asset.name}";
        }

        public static List<Entry> Entries()
        {
            var entries = new List<Entry>();
            foreach (string folder in new[] { BehaviorsFolder, SharedFolder })
            {
                if (!AssetDatabase.IsValidFolder(folder)) continue;
                foreach (string guid in AssetDatabase.FindAssets("t:BehaviorTreeAsset", new[] { folder }))
                {
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    var asset = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(path);
                    if (asset != null) entries.Add(new Entry(asset, CategoryOf(path), Description(asset)));
                }
            }
            entries.Sort((a, b) => string.CompareOrdinal(a.MenuPath, b.MenuPath));
            return entries;
        }

        public static string Description(BehaviorTreeAsset asset) =>
            asset != null && asset.Root != null && !string.IsNullOrWhiteSpace(asset.Root.Comment) ? asset.Root.Comment : "";

        // Every tree that runs this behavior through a Subtree node (switched on or off).
        public static List<BehaviorTreeAsset> TreesUsing(BehaviorTreeAsset behavior)
        {
            var users = new List<BehaviorTreeAsset>();
            foreach (string guid in AssetDatabase.FindAssets("t:BehaviorTreeAsset", new[] { TreesFolder }))
            {
                var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (tree == null || tree == behavior) continue;
                foreach (BtNode node in tree.Nodes)
                    if (node is Subtree subtree && subtree.Tree == behavior) { users.Add(tree); break; }
            }
            return users;
        }

        // Asks where to save a new behavior (starting in Behaviors/); null if cancelled.
        public static string AskForNewPath(string suggestedName, string message)
        {
            EnsureFolder(BehaviorsFolder);
            string path = EditorUtility.SaveFilePanelInProject("Save Behavior", suggestedName, "asset", message, BehaviorsFolder);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        // An empty tree saved at `path`, to fill with a branch.
        public static BehaviorTreeAsset CreateEmpty(string path)
        {
            var asset = ScriptableObject.CreateInstance<BehaviorTreeAsset>();
            asset.EnsureRoot();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        // A name for a branch saved as a behavior: the first words of its comment, else its node type.
        public static string SuggestName(BtNode branch)
        {
            string source = string.IsNullOrWhiteSpace(branch.Comment) ? BtNodeTypes.DisplayName(branch.GetType()) : branch.Comment;
            var name = new System.Text.StringBuilder();
            int words = 0;
            foreach (string word in source.Split(' ', ':', ',', '(', ')', '.', ';', '—', '-', '/', '\''))
            {
                if (word.Length == 0 || !char.IsLetter(word[0])) continue;
                name.Append(char.ToUpperInvariant(word[0])).Append(word.Substring(1));
                if (++words == 3) break;
            }
            return (name.Length > 0 ? name.ToString() : "New") + "Behavior";
        }

        private static string CategoryOf(string path)
        {
            string folder = Path.GetDirectoryName(path)?.Replace('\\', '/') ?? "";
            if (folder.StartsWith(SharedFolder)) return "Shared";
            return folder.Length > BehaviorsFolder.Length ? folder.Substring(BehaviorsFolder.Length + 1) : "";
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(folder));
        }
    }
}
