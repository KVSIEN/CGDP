using System;
using UnityEngine;
using CGD.Abilities;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // A trapper's move: picks a spot for a trap — in the way between it and its target, or where
    // the target is believed to be — and casts its trap ability (a Trap ability, e.g. Proximity
    // Mine or Jaw Trap) there. Never lays one within Spacing of another of its own, so its traps
    // spread out into a field instead of piling up. Succeeds once the trap is down; fails
    // without a target, a ready ability, or a free spot.
    [Serializable]
    [BtNodeInfo("Enemy/Combat/Lay Trap", "Lays its trap ability in the way between it and its target, or where the target is believed to be, away from its other traps.")]
    public sealed class LayTrap : EnemyAction
    {
        [Tooltip("Which ability lays the trap: its own (below) or one from the enemy's kit")]
        [SerializeField] private AbilitySlot _slot = AbilitySlot.Signature;
        [SerializeField] private Ability _ability;
        [SerializeField] private TrapPlacement _placement = TrapPlacement.InTheWay;
        [Tooltip("In The Way: metres out from itself toward the target, at most halfway there")]
        [SerializeField, Min(1f)] private float _ahead = 4f;
        [Tooltip("Metres from its other traps a new one must be")]
        [SerializeField, Min(0f)] private float _spacing = 3f;

        [NonSerialized] private Ability _resolved;
        [NonSerialized] private Vector3 _spot;
        [NonSerialized] private bool    _casting;

        public override string Summary => $"{_placement}, {_spacing:0.#} m apart";

        internal AbilitySlot Slot => _slot;
        // The ability named on the node itself (used only with the Own slot).
        internal Ability Named => _ability;

        protected override void OnInitialize(BtContext context)
        {
            base.OnInitialize(context);
            _resolved = Ai.Data.KitAbility(_slot, _ability);
            Ai.Abilities.Register(_resolved);
        }

        protected override void OnEnter(BtContext context)
        {
            _casting = TryPickSpot(out _spot) && Ai.Abilities.TryBegin(_resolved, _spot);
        }

        protected override BtStatus OnTick(BtContext context)
        {
            if (!_casting) return BtStatus.Failure;

            Ai.ReportStep(EngageStep.Cast);
            Ai.Agent.isStopped = true;
            return Ai.Abilities.TickCast(_spot, context.DeltaTime) ? BtStatus.Success : BtStatus.Running;
        }

        protected override void OnAbort(BtContext context) => Ai.Abilities.Cancel();

        protected override void OnExit(BtContext context) => Ai.Agent.isStopped = false;

        private bool TryPickSpot(out Vector3 spot)
        {
            spot = default;
            if (Ai.Perception.Target == null) return false;

            Vector3 target = Ai.Perception.TargetPosition;
            if (_placement == TrapPlacement.WhereTargetIs) spot = target;
            else
            {
                Vector3 self   = Ai.transform.position;
                Vector3 toward = target - self;
                toward.y = 0f;
                float distance = toward.magnitude;
                if (distance < 0.5f) return false;
                spot = self + toward / distance * Mathf.Min(_ahead, distance * 0.5f);
            }
            return !Trap.AnyOwnedNear(Ai.gameObject, spot, _spacing);
        }
    }
}
