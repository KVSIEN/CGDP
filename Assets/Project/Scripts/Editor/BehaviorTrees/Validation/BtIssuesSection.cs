using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The validator's findings in the side panel: a node's own issues when one is selected,
    // otherwise every issue in the tree, each clickable to jump to its node.
    internal static class BtIssuesSection
    {
        public static void DrawForNode(IReadOnlyList<BtIssue> issues, string nodeGuid)
        {
            foreach (BtIssue issue in issues)
                if (issue.NodeGuid == nodeGuid) EditorGUILayout.HelpBox(issue.Message, TypeOf(issue.Severity));
        }

        public static void DrawForTree(IReadOnlyList<BtIssue> issues, Func<string, string> nodeName, Action<string> select)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(issues.Count == 0 ? "No issues" : $"Issues ({issues.Count})", EditorStyles.boldLabel);

            foreach (BtIssue issue in issues)
            {
                string where = issue.NodeGuid != null ? nodeName(issue.NodeGuid) + ": " : string.Empty;
                var content = new GUIContent($"{issue.Icon} {where}{issue.Message}", "Click to select the node");
                if (GUILayout.Button(content, EditorStyles.wordWrappedLabel) && issue.NodeGuid != null) select(issue.NodeGuid);
            }
        }

        private static MessageType TypeOf(BtIssueSeverity severity) => severity switch
        {
            BtIssueSeverity.Error   => MessageType.Error,
            BtIssueSeverity.Warning => MessageType.Warning,
            _                       => MessageType.Info,
        };
    }
}
