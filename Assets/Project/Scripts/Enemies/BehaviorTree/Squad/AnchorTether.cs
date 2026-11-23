using System;
using UnityEngine;
using CGD.BehaviorTrees;

namespace CGD.Enemies
{
    // Asks where it stands against its squad's anchor (a unit whose EnemyData › Anchor Radius is
    // above 0, such as the Tech siege tank): stretched beyond the anchor's radius, or within it.
    // Fails when the squad has no anchor, so a tree can carry it without affecting anyone who
    // fights without one.
    [Serializable]
    [BtNodeInfo("Enemy/Squad/Anchor Tether", "Succeeds while it is stretched beyond (or within) its squad anchor's radius. Fails when there is no anchor.")]
    public sealed class AnchorTether : EnemyCondition
    {
        [SerializeField] private AnchorState _state = AnchorState.Stretched;
        [Tooltip("Share of the anchor's radius that counts: below 1 for a tighter leash when calm than the one cover is chosen by")]
        [SerializeField, Range(0.2f, 2f)] private float _radiusShare = 1f;
        [Tooltip("Extra metres on top of the anchor's radius before it counts as stretched: a scout or a leader that is allowed to range further")]
        [SerializeField, Min(0f)] private float _slack;

        public override string Summary => _slack > 0f ? $"{_state} ({_radiusShare:0.##} × radius + {_slack:0.#} m)" : $"{_state} ({_radiusShare:0.##} × radius)";

        protected override bool Check(BtContext context)
        {
            if (!EnemyRoster.TryAnchorOf(Ai, out EnemyAI anchor, out float radius)) return false;

            Vector3 offset = anchor.transform.position - Ai.transform.position;
            offset.y = 0f;
            float limit = radius * _radiusShare + _slack;
            bool stretched = offset.sqrMagnitude > limit * limit;
            return stretched == (_state == AnchorState.Stretched);
        }
    }
}
