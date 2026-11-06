using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Succeeds when a blackboard number compares as asked, e.g. TargetDistance < 6 or
    // Health ≤ 0.3. A key that isn't set gives the "missing" answer.
    [Serializable]
    [BtNodeInfo("Blackboard/Compare Number", "Succeeds when a blackboard number compares as asked (e.g. TargetDistance < 6).")]
    public sealed class CompareNumber : BtCondition
    {
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Float)] private string _key = "TargetDistance";
        [SerializeField] private NumberComparison _comparison = NumberComparison.Less;
        [SerializeField] private float            _value = 5f;
        [Tooltip("Answer when the key isn't on the blackboard")]
        [SerializeField] private bool             _ifMissing;

        public override string Summary => $"{_key} {Symbol(_comparison)} {_value:0.##}";

        protected override bool Check(BtContext context)
        {
            if (!context.Blackboard.Has(_key)) return _ifMissing;

            float value = context.Blackboard.GetFloat(_key);
            return _comparison switch
            {
                NumberComparison.Less           => value <  _value,
                NumberComparison.LessOrEqual    => value <= _value,
                NumberComparison.Equal          => Mathf.Approximately(value, _value),
                NumberComparison.NotEqual       => !Mathf.Approximately(value, _value),
                NumberComparison.GreaterOrEqual => value >= _value,
                _                               => value >  _value,
            };
        }

        private static string Symbol(NumberComparison comparison) => comparison switch
        {
            NumberComparison.Less           => "<",
            NumberComparison.LessOrEqual    => "≤",
            NumberComparison.Equal          => "=",
            NumberComparison.NotEqual       => "≠",
            NumberComparison.GreaterOrEqual => "≥",
            _                               => ">",
        };
    }
}
