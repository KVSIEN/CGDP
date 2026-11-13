using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // Which node types an enemy's tree can actually run: everything connected to the Root,
    // following the Subtree nodes (behaviors) that are switched on into their own trees. Lets
    // the enemy tools answer questions like "does this enemy ever shoot?" without opening the graph.
    internal static class EnemyTreeContents
    {
        public static HashSet<Type> NodeTypes(BehaviorTreeAsset tree)
        {
            var types = new HashSet<Type>();
            foreach (BtNode node in Nodes(tree)) types.Add(node.GetType());
            return types;
        }

        // The nodes themselves, for checks that need their settings (which kit slot an ability uses).
        public static List<BtNode> Nodes(BehaviorTreeAsset tree) => Nodes(tree, tree != null ? tree.Root : null);

        // What one branch of `tree` can run (a row in the Behavior Composer), subtrees followed.
        public static List<BtNode> Nodes(BehaviorTreeAsset tree, BtNode top)
        {
            var nodes   = new List<BtNode>();
            var visited = new HashSet<BehaviorTreeAsset>();
            if (tree != null && top != null && visited.Add(tree)) CollectFrom(tree, top, nodes, visited);
            return nodes;
        }

        public static bool UsesAny(HashSet<Type> types, params Type[] wanted)
        {
            foreach (Type type in wanted)
                if (types.Contains(type)) return true;
            return false;
        }

        private static void Collect(BehaviorTreeAsset tree, List<BtNode> nodes, HashSet<BehaviorTreeAsset> visited)
        {
            if (tree == null || tree.Root == null || !visited.Add(tree)) return;
            CollectFrom(tree, tree.Root, nodes, visited);
        }

        private static void CollectFrom(BehaviorTreeAsset tree, BtNode top, List<BtNode> nodes, HashSet<BehaviorTreeAsset> visited)
        {
            var reached = new HashSet<BtNode>();
            var stack   = new Stack<BtNode>();
            stack.Push(top);
            while (stack.Count > 0)
            {
                BtNode node = stack.Pop();
                if (node == null || !reached.Add(node)) continue;

                nodes.Add(node);
                if (node is Subtree { IsOn: true } subtree) Collect(subtree.Tree, nodes, visited);
                if (node is not BtParentNode parent) continue;
                foreach (string guid in parent.ChildGuids) stack.Push(tree.FindNode(guid));
            }
        }
    }
}
