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
            var types   = new HashSet<Type>();
            var visited = new HashSet<BehaviorTreeAsset>();
            Collect(tree, types, visited);
            return types;
        }

        public static bool UsesAny(HashSet<Type> types, params Type[] wanted)
        {
            foreach (Type type in wanted)
                if (types.Contains(type)) return true;
            return false;
        }

        private static void Collect(BehaviorTreeAsset tree, HashSet<Type> types, HashSet<BehaviorTreeAsset> visited)
        {
            if (tree == null || tree.Root == null || !visited.Add(tree)) return;

            var reached = new HashSet<BtNode>();
            var stack   = new Stack<BtNode>();
            stack.Push(tree.Root);
            while (stack.Count > 0)
            {
                BtNode node = stack.Pop();
                if (node == null || !reached.Add(node)) continue;

                types.Add(node.GetType());
                if (node is Subtree { IsOn: true } subtree) Collect(subtree.Tree, types, visited);
                if (node is not BtParentNode parent) continue;
                foreach (string guid in parent.ChildGuids) stack.Push(tree.FindNode(guid));
            }
        }
    }
}
