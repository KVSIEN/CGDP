using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Picks the squadmate to put a barrier over: whoever is taking the most fire right now, or
    // failing that the one nearest the threat (the front of the line), and stores where it
    // stands on the blackboard — for Throw Grenade to land a barrier dome on. Fails with no kin
    // near, or with no threat when nobody is under fire.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Mark Kin Under Fire", "Stores the position of the squadmate taking the most fire (else the one nearest the threat). Fails if there is none.")]
    public sealed class MarkKinUnderFire : EnemyAction
    {
        [Tooltip("Metres")]
        [SerializeField, Min(1f)] private float _radius = 25f;
        [Tooltip("Share of its maximum a squadmate must have lost lately to count as under fire")]
        [SerializeField, Range(0f, 1f)] private float _minLoss = 0.05f;
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Write, BlackboardValueType.Vector)] private string _key = "KinSpot";

        public override string Summary => $"Kin under fire within {_radius:0.#} m → {_key}";

        protected override BtStatus OnTick(BtContext context)
        {
            if (!EnemyRoster.TryKinUnderFire(Ai, _radius, _minLoss, out EnemyAI kin) &&
                !(Ai.Perception.Target != null && EnemyRoster.TryKinNearest(Ai, _radius, Ai.Perception.TargetPosition, out kin)))
                return BtStatus.Failure;

            context.Blackboard.SetVector(_key, kin.transform.position);
            return BtStatus.Success;
        }
    }
}
