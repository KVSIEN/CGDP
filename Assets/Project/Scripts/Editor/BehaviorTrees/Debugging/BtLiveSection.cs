using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // "Why (not)?" for the selected node while an agent is watched: what the agent's copy
    // last reported and how long ago, and how each of its children last answered — the
    // first child that stopped a Sequence (or the one a Selector settled on) is marked.
    internal static class BtLiveSection
    {
        public static void Draw(BehaviorTree tree, BehaviorTreeAsset asset, BtNode node)
        {
            if (!tree.TryGetNode(asset, node.Guid, out BtNode live)) return;

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Live", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Now", live.IsRunning ? "▶ Running" : Describe(tree, live));

            if (node is not BtParentNode parent || parent.ChildGuids.Count == 0) return;

            bool marked = false;
            foreach (string guid in parent.ChildGuids)
            {
                if (!tree.TryGetNode(asset, guid, out BtNode child)) continue;

                string name = BtNodeTypes.DisplayName(child.GetType());
                string line = child.IsRunning ? "▶ running" : Describe(tree, child);
                if (!marked && Decides(node, child))
                {
                    marked = true;
                    line += "   ← decided here";
                }
                EditorGUILayout.LabelField(name, line);
            }
        }

        // The child whose answer settled the parent: the first failure of a Sequence, the
        // first non-failure of a Selector, or whatever is running.
        private static bool Decides(BtNode parent, BtNode child)
        {
            if (child.IsRunning) return true;
            if (child.LastTick < 0) return false;
            return parent is Sequence ? child.LastStatus == BtStatus.Failure : child.LastStatus != BtStatus.Failure;
        }

        private static string Describe(BehaviorTree tree, BtNode node)
        {
            if (node.LastTick < 0) return "not run yet";

            string result = node.LastStatus switch
            {
                BtStatus.Success => "✓ succeeded",
                BtStatus.Failure => "✗ failed",
                _                => "▶ running",
            };
            int ticksAgo = tree.Context.TickCount - node.LastTick;
            return ticksAgo == 0 ? $"{result} (this frame)" : $"{result} ({ticksAgo} ticks ago)";
        }
    }
}
