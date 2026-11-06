using System;
using CGD.BehaviorTrees;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace CGD.Editor
{
    // The side panel: the selected node's description and settings (edited with Undo like
    // any inspector), and while an agent is watched, its blackboard.
    internal sealed class BtInspectorPanel : VisualElement
    {
        private const string Help =
            "Space or right-click: create a node.\n" +
            "Drag from a node's bottom port to a node's top port to connect them.\n" +
            "Children run left to right; the number on a node is its order.\n" +
            "Select an agent in Play mode to watch its tree live.";

        private BehaviorTreeAsset _asset;
        private SerializedObject  _serialized;
        // Kept by id: an undo or an applied edit may hand the asset fresh node objects.
        private string            _nodeGuid;
        private BehaviorTree      _watched;
        private Vector2           _scroll;
        private readonly IMGUIContainer _gui;

        public BtInspectorPanel()
        {
            style.minWidth = 240f;
            _gui = new IMGUIContainer(OnGUI);
            _gui.style.flexGrow = 1f;
            Add(_gui);
        }

        // Raised after the selected node's settings were edited here.
        public event Action<BtNode> NodeChanged;

        public void SetAsset(BehaviorTreeAsset asset)
        {
            _asset      = asset;
            _serialized = asset != null ? new SerializedObject(asset) : null;
            _nodeGuid   = null;
            _gui.MarkDirtyRepaint();
        }

        public void SetNode(BtNode node)
        {
            _nodeGuid = node?.Guid;
            _gui.MarkDirtyRepaint();
        }

        public void SetWatched(BehaviorTree tree)
        {
            _watched = tree;
            _gui.MarkDirtyRepaint();
        }

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
                if (node != null) DrawNode(node);
                else              EditorGUILayout.HelpBox(Help, MessageType.None);

                if (_watched != null) DrawBlackboard();
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
            EditorGUILayout.Space();

            _serialized.Update();
            SerializedProperty element = _serialized.FindProperty("_nodes").GetArrayElementAtIndex(index);
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

        private void DrawBlackboard()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Blackboard (live)", EditorStyles.boldLabel);
            if (_watched.Blackboard.Values.Count == 0)
            {
                EditorGUILayout.LabelField("Empty", EditorStyles.miniLabel);
                return;
            }

            foreach (var entry in _watched.Blackboard.Values)
                EditorGUILayout.LabelField(entry.Key, entry.Value.ToString());
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
