using System;
using System.Collections.Generic;
using Unity.Profiling;
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
        // Editor canvas only: frames and notes for readers, not used at runtime.
        [SerializeField] private List<BtGroup> _groups = new();
        [SerializeField] private List<BtNote>  _notes  = new();
        // Node settings promoted to named knobs that each use of this tree may change.
        [SerializeField] private List<BehaviorSetting> _settings = new();

        [NonSerialized] private ProfilerMarker? _marker;

        public IReadOnlyList<BtNode> Nodes => _nodes;
        public IReadOnlyList<BehaviorSetting> Settings => _settings;

        public BtRoot Root
        {
            get
            {
                foreach (BtNode node in _nodes)
                    if (node is BtRoot root) return root;
                return null;
            }
        }

        // A runnable copy for one agent; null if the tree has no root. `tuning` is the agent
        // type's own layer over its behaviors (switched off, own setting values), if any.
        public BehaviorTree CreateInstance(GameObject owner, object agent, IReadOnlyList<BehaviorTuning> tuning = null) =>
            BehaviorTree.Create(this, owner, agent, tuning);

        // "BehaviorTree.<name>" in the Profiler: the cost of every agent running this tree.
        internal ProfilerMarker Marker => _marker ??= new ProfilerMarker("BehaviorTree." + name);

        // --- Authoring (editor) ----------------------------------------------------

        internal List<BtGroup> Groups => _groups;
        internal List<BtNote>  Notes  => _notes;

        internal void EnsureRoot()
        {
            if (Root == null) _nodes.Insert(0, new BtRoot());
        }

        internal void AddNode(BtNode node) => _nodes.Add(node);

        internal BehaviorSetting FindSetting(string nodeGuid, string field) =>
            _settings.Find(setting => setting.NodeGuid == nodeGuid && setting.Field == field);

        internal void AddSetting(BehaviorSetting setting) => _settings.Add(setting);

        internal void RemoveSetting(BehaviorSetting setting) => _settings.Remove(setting);

        // The distinct setting names, in the order first declared.
        internal List<string> SettingNames()
        {
            var names = new List<string>();
            foreach (BehaviorSetting setting in _settings)
                if (!names.Contains(setting.Name)) names.Add(setting.Name);
            return names;
        }

        // Removes the node and every link to it; its children become unattached.
        internal void RemoveNode(BtNode node)
        {
            if (node is BtRoot) return;

            _nodes.Remove(node);
            _settings.RemoveAll(setting => setting.NodeGuid == node.Guid);
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
