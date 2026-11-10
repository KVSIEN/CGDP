using System.Collections.Generic;
using CGD.BehaviorTrees;

namespace CGD.Editor
{
    // Checks a tree for mistakes the runtime would quietly put up with: parents with nothing
    // to run, nodes nothing reaches, subtrees with no tree (or that loop into themselves),
    // nodes that never finish with nothing above able to interrupt them, abort modes with
    // nothing to re-check, branches waiting on a Motive that can't interrupt, keys read that
    // nothing writes, and odd weights or scores. Runs on
    // the asset as authored — no play mode needed — after every edit.
    internal static class BtTreeValidator
    {
        private const int MaxSubtreeDepth = 8;

        public static List<BtIssue> Validate(BehaviorTreeAsset asset)
        {
            var issues = new List<BtIssue>();
            if (asset == null) return issues;

            BtRoot root = asset.Root;
            if (root == null)
            {
                issues.Add(new BtIssue(null, BtIssueSeverity.Error, "The tree has no Root."));
                return issues;
            }

            HashSet<BtNode> reachable = Reachable(asset, root);
            bool asSubtree = BlackboardKeyCatalog.IsUsedAsSubtree(asset);

            foreach (BtNode node in asset.Nodes)
            {
                if (node == null) continue;
                if (!reachable.Contains(node))
                {
                    Add(issues, node, BtIssueSeverity.Warning, "Not connected to the Root, so it never runs.");
                    continue;
                }

                CheckChildren(asset, node, issues);
                CheckSubtree(asset, node, issues);
                CheckAbortMode(asset, node, issues);
                CheckMotiveCanAct(asset, node, issues);
                CheckNeverFinishes(asset, node, asSubtree, issues);
                CheckKeys(asset, node, issues);
                CheckComposite(asset, node, issues);
            }
            return issues;
        }

        // --- Checks -------------------------------------------------------------------

        private static void CheckChildren(BehaviorTreeAsset asset, BtNode node, List<BtIssue> issues)
        {
            if (node is not BtParentNode parent) return;

            if (parent.ChildGuids.Count == 0)
            {
                if (node is BtRoot) Add(issues, node, BtIssueSeverity.Error, "The Root has nothing to run.");
                else Add(issues, node, BtIssueSeverity.Warning, "Has no children: it fails every time.");
            }

            foreach (string guid in parent.ChildGuids)
                if (asset.FindNode(guid) == null)
                    Add(issues, node, BtIssueSeverity.Warning, "Links to a node that no longer exists (reconnect it).");
        }

        private static void CheckSubtree(BehaviorTreeAsset asset, BtNode node, List<BtIssue> issues)
        {
            if (node is not Subtree subtree) return;

            if (subtree.Tree == null)
                Add(issues, node, BtIssueSeverity.Error, "No tree to run: it always fails.");
            else if (Reaches(subtree.Tree, asset, 0))
                Add(issues, node, BtIssueSeverity.Error, $"{subtree.Tree.name} leads back to this tree: it would run itself, so it fails instead.");
            else if (subtree.Tree.Root == null || subtree.Tree.Root.ChildGuids.Count == 0)
                Add(issues, node, BtIssueSeverity.Warning, $"{subtree.Tree.name} has nothing under its Root.");
        }

        private static void CheckAbortMode(BehaviorTreeAsset asset, BtNode node, List<BtIssue> issues)
        {
            AbortMode mode = node switch
            {
                Sequence sequence => sequence.AbortSetting,
                Selector selector => selector.AbortSetting,
                _                 => AbortMode.None,
            };
            if (mode == AbortMode.None) return;

            var parent = (BtParentNode)node;
            bool lower = mode is AbortMode.LowerPriority or AbortMode.Both;
            bool self  = mode is AbortMode.Self or AbortMode.Both;

            if (lower && !IsConditional(asset, FirstChild(asset, parent), 0))
                Add(issues, node, BtIssueSeverity.Warning, "Aborts lower priority but doesn't start with a condition, so it has nothing to check.");
            if (self && !HasConditionalChild(asset, parent))
                Add(issues, node, BtIssueSeverity.Warning, "Aborts self but has no conditions to re-check.");

            BtParentNode above = asset.FindParent(node);
            bool fromSubtreeRoot = above is BtRoot;
            if (lower && above is not Selector && !fromSubtreeRoot)
                Add(issues, node, BtIssueSeverity.Info, "Lower-priority aborts only act directly under a Selector (or from a subtree's root).");
        }

        // A branch in a Selector that waits for a reason (a leading Motive) but can't interrupt:
        // the Selector only asks it once the branches to its right have finished, so the moment
        // its reasons add up goes by unnoticed — and its patience only builds while it is asked.
        private static void CheckMotiveCanAct(BehaviorTreeAsset asset, BtNode node, List<BtIssue> issues)
        {
            if (node is not Sequence sequence || sequence.AbortSetting is AbortMode.LowerPriority or AbortMode.Both) return;
            if (asset.FindParent(node) is not Selector || !LeadsWithMotive(asset, sequence)) return;

            Add(issues, node, BtIssueSeverity.Warning,
                "Waits for a reason (Motive) but can't interrupt the branches to its right, so it is only considered once they finish. Set Abort to Lower Priority so it acts as soon as its reasons add up.");
        }

        private static bool LeadsWithMotive(BehaviorTreeAsset asset, BtParentNode parent)
        {
            foreach (string guid in parent.ChildGuids)
            {
                BtNode child = asset.FindNode(guid);
                if (child is Motive) return true;
                if (!IsConditional(asset, child, 0)) return false;
            }
            return false;
        }

        private static void CheckNeverFinishes(BehaviorTreeAsset asset, BtNode node, bool asSubtree, List<BtIssue> issues)
        {
            if (!node.NeverFinishes || CanBeInterrupted(asset, node)) return;

            if (asSubtree)
                Add(issues, node, BtIssueSeverity.Info, "Never finishes, and nothing in this tree can interrupt it — fine if the trees using it as a subtree can.");
            else
                Add(issues, node, BtIssueSeverity.Warning, "Never finishes, and nothing above can interrupt it: the tree will be stuck here. Add a branch to its left that aborts lower priority.");
        }

        private static void CheckKeys(BehaviorTreeAsset asset, BtNode node, List<BtIssue> issues)
        {
            foreach (BlackboardKeyUse use in BlackboardKeyCatalog.KeysOf(node))
            {
                if (string.IsNullOrEmpty(use.Key))
                {
                    if (!use.Attribute.AllowEmpty) Add(issues, node, BtIssueSeverity.Warning, "No blackboard key set.");
                    continue;
                }

                if (use.Attribute.Access == BlackboardKeyAccess.Read && !BlackboardKeyCatalog.IsWritten(use.Key, asset))
                    Add(issues, node, BtIssueSeverity.Warning, $"Nothing writes '{use.Key}' — a typo? (Pick it from the ▾ list.)");
            }
        }

        private static void CheckComposite(BehaviorTreeAsset asset, BtNode node, List<BtIssue> issues)
        {
            if (node is WeightedRandomSelector weighted)
            {
                int children = weighted.ChildGuids.Count;
                if (weighted.WeightCount > children)
                    Add(issues, node, BtIssueSeverity.Info, $"Has {weighted.WeightCount} weights for {children} children; the extra ones are ignored.");
                else if (weighted.WeightCount > 0 && weighted.WeightCount < children)
                    Add(issues, node, BtIssueSeverity.Info, "Children past the last weight count as weight 1.");
            }

            if (node is UtilitySelector utility)
                foreach (string guid in utility.ChildGuids)
                    if (asset.FindNode(guid) is { } child && child is not Score)
                        Add(issues, child, BtIssueSeverity.Info, "Scores 0 without a Score decorator: it only runs when every other choice fails.");
        }

        // --- Tree structure -------------------------------------------------------------

        private static HashSet<BtNode> Reachable(BehaviorTreeAsset asset, BtNode root)
        {
            var reached = new HashSet<BtNode>();
            var stack   = new Stack<BtNode>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                BtNode node = stack.Pop();
                if (node == null || !reached.Add(node) || node is not BtParentNode parent) continue;
                foreach (string guid in parent.ChildGuids) stack.Push(asset.FindNode(guid));
            }
            return reached;
        }

        // Whether something above the node can stop it while it runs.
        private static bool CanBeInterrupted(BehaviorTreeAsset asset, BtNode node)
        {
            BtNode child = node;
            for (BtParentNode parent = asset.FindParent(child); parent != null; child = parent, parent = asset.FindParent(parent))
            {
                int index = IndexOf(parent, child);
                switch (parent)
                {
                    case Selector selector:
                        bool selfAborts = selector.AbortSetting is AbortMode.Self or AbortMode.Both;
                        for (int i = 0; i < index; i++)
                        {
                            BtNode earlier = asset.FindNode(parent.ChildGuids[i]);
                            if (AbortsLowerPriority(asset, earlier, 0)) return true;
                            if (selfAborts && IsConditional(asset, earlier, 0)) return true;
                        }
                        break;
                    case Sequence sequence when sequence.AbortSetting is AbortMode.Self or AbortMode.Both:
                        for (int i = 0; i < index; i++)
                            if (IsConditional(asset, asset.FindNode(parent.ChildGuids[i]), 0)) return true;
                        break;
                    case Parallel:
                    case UtilitySelector:
                        if (parent.ChildGuids.Count > 1) return true;
                        break;
                    case TimeLimit:
                        return true;
                }
            }
            return false;
        }

        // Mirrors the runtime's IsConditional on authored nodes (whose children aren't linked).
        private static bool IsConditional(BehaviorTreeAsset asset, BtNode node, int depth)
        {
            if (node == null || depth > MaxSubtreeDepth) return false;
            return node switch
            {
                BtCondition         => true,
                Inverter or Score   => IsConditional(asset, FirstChild(asset, (BtParentNode)node), depth),
                BtDecorator gate    => gate.IsGate,
                Subtree subtree     => subtree.Tree != null && IsConditional(subtree.Tree, Branch(subtree.Tree), depth + 1),
                _                   => false,
            };
        }

        private static bool AbortsLowerPriority(BehaviorTreeAsset asset, BtNode node, int depth)
        {
            if (node == null || depth > MaxSubtreeDepth) return false;
            return node switch
            {
                Sequence sequence => sequence.AbortSetting is AbortMode.LowerPriority or AbortMode.Both,
                Selector selector => selector.AbortSetting is AbortMode.LowerPriority or AbortMode.Both,
                Subtree subtree   => subtree.Tree != null && AbortsLowerPriority(subtree.Tree, Branch(subtree.Tree), depth + 1),
                _                 => false,
            };
        }

        private static bool HasConditionalChild(BehaviorTreeAsset asset, BtParentNode parent)
        {
            foreach (string guid in parent.ChildGuids)
                if (IsConditional(asset, asset.FindNode(guid), 0)) return true;
            return false;
        }

        // Whether running `from` can end up running `target` (through Subtree nodes).
        private static bool Reaches(BehaviorTreeAsset from, BehaviorTreeAsset target, int depth)
        {
            if (from == target) return true;
            if (from == null || depth > MaxSubtreeDepth) return false;
            foreach (BtNode node in from.Nodes)
                if (node is Subtree subtree && subtree.Tree != null && Reaches(subtree.Tree, target, depth + 1)) return true;
            return false;
        }

        private static BtNode FirstChild(BehaviorTreeAsset asset, BtParentNode parent) =>
            parent.ChildGuids.Count > 0 ? asset.FindNode(parent.ChildGuids[0]) : null;

        private static BtNode Branch(BehaviorTreeAsset tree) => tree.Root != null ? FirstChild(tree, tree.Root) : null;

        private static int IndexOf(BtParentNode parent, BtNode child)
        {
            for (int i = 0; i < parent.ChildGuids.Count; i++)
                if (parent.ChildGuids[i] == child.Guid) return i;
            return -1;
        }

        private static void Add(List<BtIssue> issues, BtNode node, BtIssueSeverity severity, string message) =>
            issues.Add(new BtIssue(node?.Guid, severity, message));
    }
}
