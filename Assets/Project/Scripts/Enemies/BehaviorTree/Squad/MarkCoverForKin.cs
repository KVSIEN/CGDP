using System;
using UnityEngine;
using UnityEngine.AI;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Picks where to cover a squadmate's retreat: the nearest kin that is worn down and falling
    // back (as Kin In Trouble), and a spot a few metres from it toward the threat, between the two,
    // stored on the blackboard — for Throw Grenade to land smoke on and Move To to walk into.
    // Fails when no kin is in trouble.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Mark Cover For Kin", "Stores a spot just in front of a retreating, worn-down squadmate, toward the threat. Fails if none.")]
    public sealed class MarkCoverForKin : EnemyAction
    {
        [Tooltip("Metres")]
        [SerializeField, Min(1f)] private float _radius = 35f;
        [Tooltip("Share of its health (plating for one that lives on plating) at or below which it counts as in trouble")]
        [SerializeField, Range(0.05f, 1f)] private float _health = 0.4f;
        [SerializeField] private bool _onlyRetreating = true;
        [Tooltip("Metres from the squadmate toward the threat (never more than half way)")]
        [SerializeField, Min(0f)] private float _ahead = 3f;
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Write, BlackboardValueType.Vector)] private string _key = "CoverSpot";

        public override string Summary => $"{_ahead:0.#} m ahead of kin ≤ {_health:P0} → {_key}";

        protected override BtStatus OnTick(BtContext context)
        {
            if (!EnemyRoster.TryKinInTrouble(Ai, _radius, _health, _onlyRetreating, out EnemyAI kin)) return BtStatus.Failure;

            Vector3 spot = kin.transform.position;
            if (Ai.Perception.Target != null)
            {
                Vector3 toThreat = Ai.Perception.TargetPosition - spot;
                toThreat.y = 0f;
                float metres = Mathf.Min(_ahead, toThreat.magnitude * 0.5f);
                if (metres > 0.01f) spot += toThreat.normalized * metres;
            }
            if (NavMesh.SamplePosition(spot, out NavMeshHit hit, 2f, NavMesh.AllAreas)) spot = hit.position;

            context.Blackboard.SetVector(_key, spot);
            return BtStatus.Success;
        }
    }
}
