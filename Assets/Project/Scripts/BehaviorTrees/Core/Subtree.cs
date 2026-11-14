using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs another behavior tree asset in its place — the same agent, the same blackboard —
    // so a branch used by several trees (regrouping, an idle routine) is built and fixed once.
    // Reports whatever the subtree's branch reports. Conditional aborts pass through: a
    // subtree whose branch aborts lower priority does so from here too.
    // A subtree that would end up containing itself is refused and fails.
    //
    // Switched off, it is skipped: it fails at once and never builds its tree, so a behavior
    // can be turned off for one enemy type and back on without rewiring anything (in a
    // priority Selector, failing simply hands the turn to the next behavior). An agent type can
    // also switch the behavior off for itself alone (BehaviorTuning).
    //
    // Settings: the tree it runs may name some of its node settings (BehaviorSetting); this
    // node can give them its own values, so one behavior runs with a longer cooldown here and
    // a shorter one elsewhere without being copied.
    [Serializable]
    [BtNodeInfo("Subtree", "Runs another behavior tree asset here, sharing this agent and its blackboard.")]
    public sealed class Subtree : BtNode
    {
        [Tooltip("The tree to run here (double-click the node to open it)")]
        [SerializeField] private BehaviorTreeAsset _tree;
        [Tooltip("Skips this behavior (it fails at once) without removing it from the tree")]
        [SerializeField] private bool _off;
        [Tooltip("This use's own values for the behavior's settings (edit them in the Behavior Composer)")]
        [SerializeField] private List<SettingValue> _values = new();

        [NonSerialized] private BtRoot _root;

        public BehaviorTreeAsset Tree => _tree;
        public bool IsOn => !_off;
        public IReadOnlyList<SettingValue> Values => _values;

        public override string Summary => (_off ? "OFF · " : "") + (_tree != null ? _tree.name : "(no tree)");

        // The subtree's own branch (what runs in this node's place).
        internal BtNode Branch => _root?.Branch;

        // --- Authoring (editor) ----------------------------------------------------

        internal void SetTree(BehaviorTreeAsset tree) => _tree = tree;
        internal void SetOn(bool on) => _off = !on;

        internal void SetValue(string setting, float value)
        {
            ClearValue(setting);
            _values.Add(new SettingValue(setting, value));
        }

        internal void ClearValue(string setting) => _values.RemoveAll(entry => entry.Name == setting);

        public bool TryGetValue(string setting, out float value)
        {
            foreach (SettingValue entry in _values)
            {
                if (entry.Name != setting) continue;
                value = entry.Value;
                return true;
            }
            value = 0f;
            return false;
        }

        // Copies share the authored list until the editor changes it, so a duplicate gets its own.
        internal override BtNode Duplicate()
        {
            var copy = (Subtree)base.Duplicate();
            copy._values = new List<SettingValue>(_values);
            return copy;
        }

        internal override bool IsConditional => Branch != null && Branch.IsConditional;
        internal override bool CheckCondition(BtContext context) => Branch.CheckCondition(context);
        internal override bool AbortsLowerPriority => Branch != null && Branch.AbortsLowerPriority;
        internal override bool PreconditionsPass(BtContext context) => Branch.PreconditionsPass(context);

        protected override void OnInitialize(BtContext context)
        {
            if (!_off && !context.Tree.IsSwitchedOff(_tree)) _root = context.Tree.Instantiate(_tree, _values);
        }

        protected override BtStatus OnTick(BtContext context) => _root != null ? _root.Tick(context) : BtStatus.Failure;

        protected override void OnAbort(BtContext context) => _root?.Abort(context);
    }
}
