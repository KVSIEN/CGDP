using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A behavior tree as authored in the editor (CGD › Behavior Tree window): the nodes,
    // their settings and how they connect. Shared and never changed at runtime — each
    // agent runs its own copy made by CreateInstance.
    [CreateAssetMenu(fileName = "NewBehaviorTree", menuName = "CGD/AI/Behavior Tree")]
    public class BehaviorTreeAsset : ScriptableObject
    {
        [SerializeReference] private List<BtNode> _nodes = new();

        public IReadOnlyList<BtNode> Nodes => _nodes;

        public BtRoot Root
        {
            get
            {
                foreach (BtNode node in _nodes)
                    if (node is BtRoot root) return root;
                return null;
            }
        }

        // A runnable copy for one agent; null if the tree has no root.
        public BehaviorTree CreateInstance(GameObject owner, object agent) => BehaviorTree.Create(this, owner, agent);

        // --- Authoring (editor) ----------------------------------------------------

        internal void EnsureRoot()
        {
            if (Root == null) _nodes.Insert(0, new BtRoot());
        }

        internal void AddNode(BtNode node) => _nodes.Add(node);

        // Removes the node and every link to it; its children become unattached.
        internal void RemoveNode(BtNode node)
        {
            if (node is BtRoot) return;

            _nodes.Remove(node);
            foreach (BtNode other in _nodes)
                if (other is BtParentNode parent) parent.RemoveChild(node.Guid);
        }

        internal BtNode FindNode(string guid)
        {
            foreach (BtNode node in _nodes)
                if (node != null && node.Guid == guid) return node;
            return null;
        }

        internal BtParentNode FindParent(BtNode child)
        {
            foreach (BtNode node in _nodes)
                if (node is BtParentNode parent)
                    foreach (string guid in parent.ChildGuids)
                        if (guid == child.Guid) return parent;
            return null;
        }

        // New assets start with their root.
        private void Reset() => EnsureRoot();
    }
}
