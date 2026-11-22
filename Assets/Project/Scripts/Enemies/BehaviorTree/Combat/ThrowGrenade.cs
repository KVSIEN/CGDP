using System;
using UnityEngine;
using CGD.BehaviorTrees;
using CGD.Weapons;

namespace CGD.Enemies
{
    // Lobs a grenade onto where it believes its target is — the way to flush someone out of
    // cover it can't see past. A short wind-up facing the spot, then a high arc aimed so the
    // grenade goes off there after bouncing and rolling, leading a target it sees moving.
    // Fails when the spot is too close (it won't throw at its own feet) or out of throwing
    // range. With an At Key it lands on that blackboard spot instead (smoke on a squadmate's
    // retreat, see Mark Cover For Kin). Put a Cooldown (and a Limit, for a supply) above it. Without a grenade of its own
    // it throws the enemy's kit grenade (EnemyData › Kit › Grenade), so shared behaviors throw
    // whatever each enemy type carries; with neither, it fails.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Throw Grenade", "Lobs a grenade onto the (believed) target position. Fails if too close or out of range.")]
    public sealed class ThrowGrenade : EnemyAction
    {
        [Tooltip("Empty = the enemy's kit grenade (EnemyData › Kit › Grenade)")]
        [SerializeField] private GrenadeData _grenade;
        [Tooltip("Metres: it won't throw at a target closer than this")]
        [SerializeField, Min(0f)] private float _minDistance = 6f;
        [Tooltip("Optional: lands on this blackboard position instead of the target (it still needs a target to be fighting)")]
        [SerializeField, BlackboardKey(BlackboardKeyAccess.Read, BlackboardValueType.Vector, AllowEmpty = true)] private string _atKey;

        [NonSerialized] private bool _throwing;

        public override string Summary =>
            (_grenade != null ? _grenade.GrenadeName : "Kit grenade") + (string.IsNullOrEmpty(_atKey) ? "" : $" at {_atKey}");

        internal bool UsesKit => _grenade == null;

        private GrenadeData Grenade => _grenade != null ? _grenade : Ai.Data.Grenade;

        protected override void OnEnter(BtContext context)
        {
            var perception = Ai.Perception;
            bool atSpot = !string.IsNullOrEmpty(_atKey);
            if (perception.Target == null || (atSpot && !context.Blackboard.Has(_atKey)))
            {
                _throwing = false;
                return;
            }

            Vector3 spot     = atSpot ? context.Blackboard.GetVector(_atKey) : perception.TargetPosition;
            Vector3 velocity = atSpot ? Vector3.zero : perception.TargetVelocity;
            _throwing = (spot - Ai.transform.position).sqrMagnitude >= _minDistance * _minDistance &&
                        Ai.Grenades.TryBegin(Grenade, spot, velocity);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_throwing) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Cast);
            return Ai.Grenades.Tick(context.DeltaTime) ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnAbort(BtContext context) => Ai.Grenades.Cancel();

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;
    }
}
