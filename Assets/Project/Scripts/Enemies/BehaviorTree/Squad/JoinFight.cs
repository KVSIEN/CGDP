using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A squadmate is fighting something this one hasn't seen yet: walk up toward it, stopping
    // short, until its own senses find the target and the fight branch takes over. A tank uses
    // it so it goes to its squad's fight instead of idling beside whoever started it. Keeps
    // running while a kin within reach has a target; fails when none has.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Join Fight", "Walks toward the target one of its kin is fighting, stopping short of it. Fails when no kin nearby has a target.")]
    public sealed class JoinFight : EnemyAction
    {
        [Tooltip("Metres from itself within which a kin's fight counts")]
        [SerializeField, Min(1f)] private float _radius = 40f;
        [Tooltip("Metres from the target at which it stops and waits for its own senses to find it")]
        [SerializeField, Min(1f)] private float _stopDistance = 15f;
        [Tooltip("Speed, as a share of chase speed")]
        [SerializeField, Range(0.2f, 1.5f)] private float _speed = 1f;

        [Tooltip("Walks the way its faction sneaks (Void keeps to walls, Bio slips through bushes) instead of the shortest way")]
        [SerializeField] private bool _sneak;

        [NonSerialized] private StationKeeper _station;

        public override string Summary => $"Within {_radius:0.#} m, stops {_stopDistance:0.#} m short";

        internal override bool NeverFinishes => true;

        protected override void OnEnter(BtContext context)
        {
            _station ??= new StationKeeper(retarget: 3f, arrive: 1.5f, depart: 3f);
            _station.Reset();
            Ai.Agent.isStopped = false;
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!EnemyRoster.TryKinTarget(Ai, _radius, out Vector3 at)) return BtStatus.Failure;

            Vector3 self = Ai.transform.position;
            Vector3 away = self - at;
            away.y = 0f;
            Vector3 stand = away.sqrMagnitude > 0.01f ? at + away.normalized * _stopDistance : at;
            _station.Update(away.sqrMagnitude <= _stopDistance * _stopDistance ? self : stand, self);

            Ai.ReportState(EnemyAI.AiState.Chase);
            Ai.Motion.SetSpeed(Ai.Data.ChaseSpeed * _speed);
            if (_station.Moving) Ai.Routing.SetDestination(_station.Goal, WalkStyle(_sneak));
            else
            {
                Ai.Routing.Stop();
                Ai.Motion.FaceTowards(at, context.DeltaTime);
            }
            return BtStatus.Running;
        }
    }
}
