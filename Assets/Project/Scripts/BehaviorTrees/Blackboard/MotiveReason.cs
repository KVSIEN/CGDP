using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // One reason a Motive counts towards acting: a blackboard number mapped linearly from
    // From (no reason at all) to To (the full weight). To may be below From, e.g. Health
    // 0.8 → 0.3 grows as health falls. A key that isn't set adds nothing.
    [Serializable]
    public sealed class MotiveReason
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Float)] private string _key = "Health";
        [Tooltip("Value that adds nothing")]
        [SerializeField] private float _from = 1f;
        [Tooltip("Value that adds the full weight")]
        [SerializeField] private float _to;
        [SerializeField, Min(0f)] private float _weight = 0.5f;

        public string Key => _key;

        public float Evaluate(Blackboard blackboard)
        {
            if (string.IsNullOrEmpty(_key) || !blackboard.Has(_key)) return 0f;
            return _weight * Mathf.InverseLerp(_from, _to, blackboard.GetFloat(_key));
        }
    }
}
