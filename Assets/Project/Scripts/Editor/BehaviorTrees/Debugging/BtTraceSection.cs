using System;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;

namespace CGD.Editor
{
    // The watched agent's recent decisions, newest first: what started, succeeded, failed,
    // was aborted, or was interrupted — and by what. Click an entry to select its node.
    internal static class BtTraceSection
    {
        private const int Shown = 60;

        public static void Draw(BtTrace trace, Func<BtNode, bool> inThisTree, Action<string> select)
        {
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("Trace (newest first)", EditorStyles.boldLabel);
                if (GUILayout.Button("Clear", EditorStyles.miniButton, GUILayout.Width(48f))) trace.Clear();
            }

            if (trace.Count == 0)
            {
                EditorGUILayout.LabelField("Nothing yet", EditorStyles.miniLabel);
                return;
            }

            for (int i = trace.Count - 1; i >= 0 && i >= trace.Count - Shown; i--)
            {
                BtTraceEntry entry = trace[i];
                string text = $"{entry.Time,6:0.00}  {Icon(entry.Event)} {Name(entry.Node)}";
                if (entry.Event == BtTraceEvent.Interrupted && entry.Cause != null) text += $"  ← {Name(entry.Cause)}";

                bool here = inThisTree(entry.Node);
                using (new EditorGUI.DisabledScope(!here))
                    if (GUILayout.Button(new GUIContent(text, here ? "Select" : "In another tree (a subtree)"), EditorStyles.label))
                        select(entry.Node.Guid);
            }
        }

        private static string Name(BtNode node)
        {
            string name = BtNodeTypes.DisplayName(node.GetType());
            return string.IsNullOrEmpty(node.Comment) ? name : $"{name} — {Shorten(node.Comment)}";
        }

        private static string Shorten(string text) => text.Length <= 40 ? text : text[..40] + "…";

        private static string Icon(BtTraceEvent kind) => kind switch
        {
            BtTraceEvent.Started     => "▶",
            BtTraceEvent.Succeeded   => "✓",
            BtTraceEvent.Failed      => "✗",
            BtTraceEvent.Aborted     => "■",
            _                        => "⇥",
        };
    }
}
