using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A node with children. The asset stores children by id, in the order they run
    // (left to right in the editor); each runtime copy links them to its own child copies.
    [Serializable]
    public abstract class BtParentNode : BtNode
    {
        [SerializeField, HideInInspector] private List<string> _children = new();

        [NonSerialized] private List<BtNode> _childNodes;

        // How many children this node takes: 1 for decorators and the root.
        internal abstract int MaxChildren { get; }

        internal IReadOnlyList<string> ChildGuids => _children;

        // The linked child copies (runtime only); also read by BehaviorTree.CollectRunning.
        protected internal IReadOnlyList<BtNode> Children => _childNodes;

        protected override void OnAbort(BtContext context)
        {
            foreach (BtNode child in _childNodes)
                child.Abort(context);
        }

        // --- Authoring (editor) ----------------------------------------------------

        internal bool CanAddChild => _children.Count < MaxChildren;

        internal void AddChild(string guid)
        {
            if (CanAddChild && !_children.Contains(guid)) _children.Add(guid);
        }

        internal void RemoveChild(string guid) => _children.Remove(guid);

        internal void SetChildOrder(List<string> ordered)
        {
            _children.Clear();
            _children.AddRange(ordered);
        }

        internal override BtNode Duplicate()
        {
            var copy = (BtParentNode)base.Duplicate();
            copy._children = new List<string>();
            return copy;
        }

        // --- Runtime ---------------------------------------------------------------

        internal void LinkChildren(IReadOnlyDictionary<string, BtNode> copiesByGuid)
        {
            _childNodes = new List<BtNode>(_children.Count);
            foreach (string guid in _children)
                if (copiesByGuid.TryGetValue(guid, out BtNode child)) _childNodes.Add(child);
        }
    }
}
