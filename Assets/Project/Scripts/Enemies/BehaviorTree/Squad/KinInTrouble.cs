using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while one of its kin close by is worn down and falling back (fleeing, falling back
    // to cover, hiding): the cue to cover its retreat — smoke, a counter-push.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Kin In Trouble", "Succeeds while kin within the radius is at or below the health share and (optionally) retreating.")]
    public sealed class KinInTrouble : EnemyCondition
    {
        [Tooltip("Metres")]
        [SerializeField, Min(1f)] private float _radius = 35f;
        [Tooltip("Share of its health (plating for one that lives on plating) at or below which it counts as in trouble")]
        [SerializeField, Range(0.05f, 1f)] private float _health = 0.4f;
        [Tooltip("Only while it is retreating, not merely hurt")]
        [SerializeField] private bool _onlyRetreating = true;

        public override string Summary => $"≤ {_health:P0} within {_radius:0.#} m{(_onlyRetreating ? ", retreating" : "")}";

        protected override bool Check(BtContext context) =>
            EnemyRoster.TryKinInTrouble(Ai, _radius, _health, _onlyRetreating, out _);
    }
}
