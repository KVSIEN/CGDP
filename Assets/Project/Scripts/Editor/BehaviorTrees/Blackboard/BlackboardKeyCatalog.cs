using System;
using System.Collections.Generic;
using System.Reflection;
using CGD.BehaviorTrees;
using UnityEditor;

namespace CGD.Editor
{
    // Every blackboard key the editor knows of: the ones agents write themselves
    // ([AgentBlackboardKey] constants such as EnemyBlackboard.Health) and the ones any tree
    // asset in the project writes. Feeds the key picker and the "nothing writes this key"
    // check. Rebuilt lazily after the project changes.
    internal static class BlackboardKeyCatalog
    {
        public const string AgentSource      = "Agent";
        public const string ThisTreeSource   = "This tree";
        public const string OtherTreesSource = "Other trees";

        private static readonly Dictionary<Type, (FieldInfo field, BlackboardKeyAttribute attribute)[]> KeyFields = new();
        private static readonly List<BlackboardKeyInfo> Agent = new();
        private static readonly Dictionary<string, BlackboardKeyInfo> WrittenByTrees = new();
        private static readonly List<BehaviorTreeAsset> Trees = new();
        private static bool _stale = true;

        [InitializeOnLoadMethod]
        private static void Init() => EditorApplication.projectChanged += Invalidate;

        public static void Invalidate() => _stale = true;

        public static IReadOnlyList<BehaviorTreeAsset> AllTrees
        {
            get { Refresh(); return Trees; }
        }

        // The keys a node's [BlackboardKey] fields hold right now.
        public static IEnumerable<BlackboardKeyUse> KeysOf(BtNode node)
        {
            foreach (var (field, attribute) in FieldsOf(node.GetType()))
                yield return new BlackboardKeyUse(field.GetValue(node) as string ?? string.Empty, attribute);
        }

        // Written by the agent, any tree in the project, or this (possibly unsaved) tree.
        public static bool IsWritten(string key, BehaviorTreeAsset current)
        {
            Refresh();
            foreach (BlackboardKeyInfo info in Agent)
                if (info.Name == key) return true;
            return WrittenByTrees.ContainsKey(key) || WritesKey(current, key);
        }

        // What the picker offers for a field: agent keys first, then this tree's, then others'.
        public static List<BlackboardKeyInfo> Suggestions(BehaviorTreeAsset current, BlackboardKeyAttribute wanted)
        {
            Refresh();
            var result = new List<BlackboardKeyInfo>();
            var seen   = new HashSet<string>();

            foreach (BlackboardKeyInfo info in Agent) Offer(info);
            if (current != null)
                foreach (BtNode node in current.Nodes)
                    if (node != null)
                        foreach (BlackboardKeyUse use in KeysOf(node))
                            if (!string.IsNullOrEmpty(use.Key))
                                Offer(new BlackboardKeyInfo(use.Key, use.Attribute.Type, use.Attribute.AnyType, ThisTreeSource, null));
            foreach (BlackboardKeyInfo info in WrittenByTrees.Values) Offer(info);
            return result;

            void Offer(BlackboardKeyInfo info)
            {
                if (info.Fits(wanted) && seen.Add(info.Name)) result.Add(info);
            }
        }

        // Whether another tree in the project runs this one as a subtree (its context then
        // decides what can interrupt it).
        public static bool IsUsedAsSubtree(BehaviorTreeAsset asset)
        {
            foreach (BehaviorTreeAsset tree in AllTrees)
            {
                if (tree == asset || tree == null) continue;
                foreach (BtNode node in tree.Nodes)
                    if (node is Subtree subtree && subtree.Tree == asset) return true;
            }
            return false;
        }

        private static bool WritesKey(BehaviorTreeAsset tree, string key)
        {
            if (tree == null) return false;
            foreach (BtNode node in tree.Nodes)
                if (node != null)
                    foreach (BlackboardKeyUse use in KeysOf(node))
                        if (use.Attribute.Access == BlackboardKeyAccess.Write && use.Key == key) return true;
            return false;
        }

        private static void Refresh()
        {
            if (!_stale) return;
            _stale = false;

            Agent.Clear();
            foreach (FieldInfo field in TypeCache.GetFieldsWithAttribute<AgentBlackboardKeyAttribute>())
            {
                if (!field.IsLiteral || field.GetRawConstantValue() is not string name) continue;
                var info = field.GetCustomAttribute<AgentBlackboardKeyAttribute>();
                Agent.Add(new BlackboardKeyInfo(name, info.Type, false, AgentSource, info.Description));
            }

            Trees.Clear();
            WrittenByTrees.Clear();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(BehaviorTreeAsset)))
            {
                var tree = AssetDatabase.LoadAssetAtPath<BehaviorTreeAsset>(AssetDatabase.GUIDToAssetPath(guid));
                if (tree == null) continue;
                Trees.Add(tree);

                foreach (BtNode node in tree.Nodes)
                    if (node != null)
                        foreach (BlackboardKeyUse use in KeysOf(node))
                            if (use.Attribute.Access == BlackboardKeyAccess.Write && !string.IsNullOrEmpty(use.Key))
                                WrittenByTrees.TryAdd(use.Key, new BlackboardKeyInfo(use.Key, use.Attribute.Type, use.Attribute.AnyType,
                                                                                    OtherTreesSource, $"Written in {tree.name}"));
            }
        }

        private static (FieldInfo, BlackboardKeyAttribute)[] FieldsOf(Type type)
        {
            if (KeyFields.TryGetValue(type, out var cached)) return cached;

            var found = new List<(FieldInfo, BlackboardKeyAttribute)>();
            for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
                foreach (FieldInfo field in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                    if (field.FieldType == typeof(string) && field.GetCustomAttribute<BlackboardKeyAttribute>() is { } attribute)
                        found.Add((field, attribute));

            return KeyFields[type] = found.ToArray();
        }
    }
}
