using System;
using System.Collections.Generic;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CGD.Editor
{
    // The side panel. With a node selected: its description, settings (edited with Undo like
    // any inspector), breakpoints, the validator's findings on it and — while an agent is
    // watched — its live "why" view. With nothing selected: every issue in the tree. While
    // watching, the agent's blackboard and decision trace follow.
    internal sealed class BtInspectorPanel : VisualElement
    {
        private const string Help =
            "Space or right-click: create a node, group the selection, add a sticky note.\n" +
            "Drag from a node's bottom port to a node's top port to connect them.\n" +
            "Children run left to right; the number on a node is its order.\n" +
            "Select an agent in Play mode to watch its tree live.";

        private readonly IMGUIContainer _gui;
        private BehaviorTreeAsset _asset;
        private SerializedObject  _serialized;
        // Kept by id: an undo or an applied edit may hand the asset fresh node objects.
        private string            _nodeGuid;
        private BehaviorTree      _watched;
        private IReadOnlyList<BtIssue> _issues = Array.Empty<BtIssue>();
        private Vector2           _scroll;

        public BtInspectorPanel()
        {
            style.minWidth = 260f;
            _gui = new IMGUIContainer(OnGUI);
            _gui.style.flexGrow = 1f;
            Add(_gui);
        }

        // Raised after the selected node's settings were edited here.
        public event Action<BtNode> NodeChanged;
        // Asked to select a node (from the issue list or the trace).
        public event Action<string> SelectRequested;
        // A breakpoint was set or cleared.
        public event Action BreakpointsChanged;

        public void SetAsset(BehaviorTreeAsset asset)
        {
            _asset      = asset;
            _serialized = asset != null ? new SerializedObject(asset) : null;
            _nodeGuid   = null;
            Repaint();
        }

        public void SetNode(BtNode node)
        {
            _nodeGuid = node?.Guid;
            Repaint();
        }

        public void SetWatched(BehaviorTree tree)
        {
            _watched = tree;
            Repaint();
        }

        public void SetIssues(IReadOnlyList<BtIssue> issues)
        {
            _issues = issues;
            Repaint();
        }

        public void Repaint() => _gui.MarkDirtyRepaint();

        private void OnGUI()
        {
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_asset == null)
            {
                EditorGUILayout.HelpBox("Pick a Behavior Tree asset in the toolbar, or double-click one in the Project window.", MessageType.Info);
            }
            else
            {
                BtNode node = _nodeGuid != null ? _asset.FindNode(_nodeGuid) : null;
                if (node != null)
                {
                    DrawNode(node);
                }
                else
                {
                    EditorGUILayout.HelpBox(Help, MessageType.None);
                    BtIssuesSection.DrawForTree(_issues, NodeName, guid => SelectRequested?.Invoke(guid));
                }

                if (_watched != null) DrawWatched();
            }
            EditorGUILayout.EndScrollView();
        }

        private void DrawNode(BtNode node)
        {
            int index = IndexOf(node);
            if (index < 0) return;

            Type type = node.GetType();
            EditorGUILayout.LabelField(BtNodeTypes.DisplayName(type), EditorStyles.boldLabel);
            string description = BtNodeTypes.Description(type);
            if (!string.IsNullOrEmpty(description))
                EditorGUILayout.LabelField(description, EditorStyles.wordWrappedMiniLabel);
            BtIssuesSection.DrawForNode(_issues, node.Guid);
            EditorGUILayout.Space();

            DrawSettings(index);
            DrawBreakpoints(node);
            if (_watched != null) BtLiveSection.Draw(_watched, _asset, node);
        }

        private void DrawSettings(int index)
        {
            _serialized.Update();
            SerializedProperty element  = _serialized.FindProperty("_nodes").GetArrayElementAtIndex(index);
            SerializedProperty property = element.Copy();
            SerializedProperty end      = element.GetEndProperty();

            bool enterChildren = true;
            while (property.NextVisible(enterChildren) && !SerializedProperty.EqualContents(property, end))
            {
                enterChildren = false;
                EditorGUILayout.PropertyField(property, true);
            }

            if (_serialized.ApplyModifiedProperties()) NodeChanged?.Invoke(_asset.FindNode(_nodeGuid));
        }

        private void DrawBreakpoints(BtNode node)
        {
            BtBreakOn current = BtBreakpoints.Get(node.Guid);
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.PrefixLabel(new GUIContent("Break when it", "Pause the game when the watched agent's copy of this node starts or fails"));
                bool onStart   = GUILayout.Toggle(current.HasFlag(BtBreakOn.Start),   "starts",  EditorStyles.miniButtonLeft);
                bool onFailure = GUILayout.Toggle(current.HasFlag(BtBreakOn.Failure), "fails",   EditorStyles.miniButtonRight);

                BtBreakOn wanted = (onStart ? BtBreakOn.Start : 0) | (onFailure ? BtBreakOn.Failure : 0);
                if (wanted == current) return;
                BtBreakpoints.Set(node.Guid, wanted);
                BreakpointsChanged?.Invoke();
            }
        }

        private void DrawWatched()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blackboard (live)", EditorStyles.boldLabel);
            if (_watched.Blackboard.Values.Count == 0) EditorGUILayout.LabelField("Empty", EditorStyles.miniLabel);
            foreach (var entry in _watched.Blackboard.Values)
                EditorGUILayout.LabelField(entry.Key, entry.Value.ToString());

            if (_watched.Trace != null)
                BtTraceSection.Draw(_watched.Trace, node => _asset.FindNode(node.Guid) != null, guid => SelectRequested?.Invoke(guid));
        }

        private string NodeName(string guid)
        {
            BtNode node = _asset.FindNode(guid);
            return node != null ? BtNodeTypes.DisplayName(node.GetType()) : "?";
        }

        private int IndexOf(BtNode node)
        {
            var nodes = _asset.Nodes;
            for (int i = 0; i < nodes.Count; i++)
                if (ReferenceEquals(nodes[i], node)) return i;
            return -1;
        }
    }
}
