using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // A named knob on a behavior: one node setting (a Cooldown's seconds, a range, a chance)
    // that every place using the behavior may change without copying it — the way Unreal's
    // StateTree promotes a property to a parameter, or Behavior Designer lets each tree
    // reference set its own variable values. Several settings may share a name: one knob then
    // drives the same value on several nodes (one "Reach" for a check and a move).
    // Only numbers, whole numbers, on/off flags and choices (enums) can be settings.
    [Serializable]
    public sealed class BehaviorSetting
    {
        [SerializeField] private string _name;
        [SerializeField] private string _nodeGuid;
        [SerializeField] private string _field;   // the node's serialized field, e.g. "_seconds"

        private BehaviorSetting() { }

        internal BehaviorSetting(string name, string nodeGuid, string field)
        {
            _name     = name;
            _nodeGuid = nodeGuid;
            _field    = field;
        }

        public string Name     => _name;
        public string NodeGuid => _nodeGuid;
        public string Field    => _field;

        internal void Rename(string name) => _name = name;
    }
}
