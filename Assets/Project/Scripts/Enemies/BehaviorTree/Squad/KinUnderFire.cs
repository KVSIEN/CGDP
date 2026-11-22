using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Succeeds while a squadmate within the radius is taking fire (it has lost at least this share
    // of its maximum lately): the moment a shield-bearer should put itself between the threat and
    // its allies, and a quiet one is the moment to take cover and wait.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Kin Under Fire", "Succeeds while a squadmate within the radius is taking fire.")]
    public sealed class KinUnderFire : EnemyCondition
    {
        [Tooltip("Metres")]
        [SerializeField, Min(1f)] private float _radius = 25f;
        [Tooltip("Share of its maximum a squadmate must have lost lately to count as under fire")]
        [SerializeField, Range(0f, 1f)] private float _minLoss = 0.05f;

        public override string Summary => $"Within {_radius:0.#} m, lost ≥ {_minLoss:P0}";

        protected override bool Check(BtContext context) => EnemyRoster.TryKinUnderFire(Ai, _radius, _minLoss, out _);
    }
}
