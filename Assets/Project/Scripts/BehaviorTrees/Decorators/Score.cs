using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // How much a Utility Selector wants its child right now: a blackboard number, scaled
    // into 0..1 between Min and Max, shaped by the curve and multiplied by the weight. Put
    // several on one child to multiply them (wants cover AND is hurt). An empty key scores
    // just the weight — a fixed preference. Otherwise passes its child straight through.
    [Serializable]
    [BtNodeInfo("Decorators/Score", "Scores its child for a Utility Selector from a blackboard number, curve and weight. Stack to multiply.")]
    public sealed class Score : BtDecorator
    {
        [Tooltip("Blackboard number to read (e.g. Health, Morale, Awareness, TargetDistance). Empty = a fixed score")]
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Float, AllowEmpty = true)] private string _key = "Health";
        [Tooltip("Value that counts as 0")]
        [SerializeField] private float _min;
        [Tooltip("Value that counts as 1")]
        [SerializeField] private float _max = 1f;
        [Tooltip("Shapes the 0..1 value into a score (x: value, y: score)")]
        [SerializeField] private AnimationCurve _curve = AnimationCurve.Linear(0f, 0f, 1f, 1f);
        [SerializeField, Min(0f)] private float _weight = 1f;

        public override string Summary => string.IsNullOrEmpty(_key) ? $"Fixed {_weight:0.##}" : $"{_key} × {_weight:0.##}";

        internal override bool IsConditional => Child != null && Child.IsConditional;
        internal override bool CheckCondition(BtContext context) => Child.CheckCondition(context);

        public float Evaluate(BtContext context)
        {
            float own = _weight;
            if (!string.IsNullOrEmpty(_key))
            {
                float t = Mathf.InverseLerp(_min, _max, context.Blackboard.GetFloat(_key, _min));
                own *= Mathf.Max(0f, _curve.Evaluate(t));
            }
            return Child is Score inner ? own * inner.Evaluate(context) : own;
        }

        protected override BtStatus OnTick(BtContext context) => Child != null ? Child.Tick(context) : BtStatus.Failure;
    }
}
