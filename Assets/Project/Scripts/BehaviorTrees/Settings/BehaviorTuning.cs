using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // One agent type's own take on a behavior (or on its whole tree): switched off for it
    // alone, and/or with its own setting values. It lets several enemy types share one tree and
    // one library of behaviors yet differ — the archetype is the shared parts plus this layer —
    // instead of each type forking the tree to change a cooldown. Applied when the agent's tree
    // is built; its values win over the ones the tree gives the behavior.
    [Serializable]
    public sealed class BehaviorTuning
    {
        [Tooltip("The behavior (or tree) this tunes")]
        [SerializeField] private BehaviorTreeAsset _behavior;
        [Tooltip("Skips this behavior for this enemy type alone, wherever its tree runs it")]
        [SerializeField] private bool _off;
        [SerializeField] private List<SettingValue> _values = new();

        private BehaviorTuning() { }

        internal BehaviorTuning(BehaviorTreeAsset behavior) => _behavior = behavior;

        public BehaviorTreeAsset           Behavior => _behavior;
        public bool                        IsOff    => _off;
        public IReadOnlyList<SettingValue> Values   => _values;

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

        // Nothing left to say: no switch, no values (the editor drops such entries).
        internal bool IsEmpty => !_off && _values.Count == 0;

        internal void SetOff(bool off) => _off = off;

        internal void SetValue(string setting, float value)
        {
            ClearValue(setting);
            _values.Add(new SettingValue(setting, value));
        }

        internal void ClearValue(string setting) => _values.RemoveAll(entry => entry.Name == setting);

        public static BehaviorTuning Find(IReadOnlyList<BehaviorTuning> tunings, BehaviorTreeAsset behavior)
        {
            if (tunings == null || behavior == null) return null;
            foreach (BehaviorTuning tuning in tunings)
                if (tuning != null && tuning._behavior == behavior) return tuning;
            return null;
        }
    }
}
