using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEditor;

namespace CGD.Editor
{
    // The tree checks' warnings and errors for the Behavior Composer, gathered per row: those on
    // the row's own nodes, and for a behavior those inside its asset (and the values its use
    // gives settings it doesn't have). Validating is too slow to redo every repaint, so the
    // Composer refreshes this when the tree changes.
    internal sealed class BehaviorRowProblems
    {
        private const int Shown = 3;

        private readonly Dictionary<string, List<BtIssue>> _byRow = new();

        public void Refresh(BehaviorTreeAsset tree)
        {
            _byRow.Clear();
            if (tree == null) return;

            var byNode = new Dictionary<string, List<BtIssue>>();
            foreach (BtIssue issue in BtTreeValidator.Validate(tree)) Add(byNode, issue.NodeGuid, issue);

            var insideBehaviors = new Dictionary<BehaviorTreeAsset, List<BtIssue>>();
            foreach (BehaviorEditing.BehaviorList list in BehaviorEditing.Lists(tree))
            foreach (string guid in list.Selector.ChildGuids)
            {
                BtNode row = tree.FindNode(guid);
                if (row == null) continue;

                foreach (BtNode node in BehaviorEditing.Branch(tree, row))
                    if (byNode.TryGetValue(node.Guid, out List<BtIssue> own))
                        foreach (BtIssue issue in own) Add(_byRow, row.Guid, issue);

                if (row is not Subtree { Tree: not null } subtree) continue;
                if (!insideBehaviors.TryGetValue(subtree.Tree, out List<BtIssue> inside))
                    insideBehaviors[subtree.Tree] = inside = BtTreeValidator.Validate(subtree.Tree);
                foreach (BtIssue issue in inside) Add(_byRow, row.Guid, issue);
            }
        }

        public void Draw(BtNode row)
        {
            if (!_byRow.TryGetValue(row.Guid, out List<BtIssue> problems)) return;

            bool error = problems.Exists(issue => issue.Severity == BtIssueSeverity.Error);
            var lines = new List<string>();
            for (int i = 0; i < problems.Count && i < Shown; i++) lines.Add(problems[i].Message);
            if (problems.Count > Shown) lines.Add($"… and {problems.Count - Shown} more (Open to see them on the nodes).");
            EditorGUILayout.HelpBox(string.Join("\n", lines), error ? MessageType.Error : MessageType.Warning);
        }

        private static void Add(Dictionary<string, List<BtIssue>> problems, string guid, BtIssue issue)
        {
            if (guid == null || issue.Severity == BtIssueSeverity.Info) return;
            if (!problems.TryGetValue(guid, out List<BtIssue> list)) problems[guid] = list = new List<BtIssue>();
            list.Add(issue);
        }
    }
}
