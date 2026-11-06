using System;
using UnityEngine;

namespace CGD.BehaviorTrees
{
    // Runs its child only some of the time — rolled once each time it starts — and fails
    // otherwise. Keeps an enemy from doing the same thing at every opportunity.
    [Serializable]
    [BtNodeInfo("Decorators/Chance", "Runs its child with the given probability (rolled when it starts); fails otherwise.")]
    public sealed class Chance : BtDecorator
    {
        [SerializeField, Range(0f, 1f)] private float _probability = 0.5f;

        [NonSerialized] private bool _rolledIn;

        public override string Summary => $"{_probability:P0}";

        protected override void OnEnter(BtContext context) => _rolledIn = UnityEngine.Random.value < _probability;

        protected override BtStatus OnTick(BtContext context) =>
            _rolledIn && Child != null ? Child.Tick(context) : BtStatus.Failure;
    }
}
