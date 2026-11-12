using System;
using UnityEngine;

namespace CGD.Enemies
{
    // One job an encounter hands out (F.E.A.R.-style squad slots): which role, how many may
    // hold it, who qualifies, and what makes one member a better fit than another. A fit is
    // a weighted sum (utility): closeness to the target, standing off to its side, health,
    // rank, being in cover, and already holding the role, so roles stay put unless someone
    // is clearly better.
    [Serializable]
    public sealed class RoleSlot
    {
        [SerializeField] private SquadRole _role = SquadRole.Assault;
        [Tooltip("How many may hold it. 0 = everyone still without a role")]
        [SerializeField, Min(0)] private int _count = 1;
        [SerializeField] private RoleFilter _who = RoleFilter.Anyone;
        [Tooltip("The encounter's lead takes this slot first, if it qualifies")]
        [SerializeField] private bool _leadFirst;

        [Header("Fit (weights)")]
        [Tooltip("Nearest to the target")]
        [SerializeField] private float _closeness = 1f;
        [Tooltip("Standing off to the target's side or behind it")]
        [SerializeField] private float _offAxis;
        [SerializeField] private float _health;
        [Tooltip("Higher tier")]
        [SerializeField] private float _rank;
        [Tooltip("Already in cover from the target")]
        [SerializeField] private float _cover;
        [Tooltip("Already holding this role")]
        [SerializeField] private float _stickiness = 0.3f;

        public RoleSlot() { }

        public RoleSlot(SquadRole role, int count, RoleFilter who, float closeness = 1f, bool leadFirst = false)
        {
            _role      = role;
            _count     = count;
            _who       = who;
            _closeness = closeness;
            _leadFirst = leadFirst;
        }

        public SquadRole Role      => _role;
        public int       Count     => _count;
        public bool      LeadFirst => _leadFirst;

        public bool Accepts(in RoleCandidate candidate) => _who switch
        {
            RoleFilter.Ranged => candidate.IsRanged,
            RoleFilter.Melee  => !candidate.IsRanged,
            _                 => true,
        };

        // How well a member fits, given the furthest any member stands from the target.
        public float Fit(in RoleCandidate candidate, float furthest)
        {
            float closeness = furthest > 0.01f ? 1f - Mathf.Clamp01(candidate.Distance / furthest) : 1f;
            return _closeness * closeness
                 + _offAxis   * candidate.OffAxis
                 + _health    * candidate.HealthShare
                 + _rank      * candidate.Tier / 3f
                 + _cover     * (candidate.InCover ? 1f : 0f)
                 + (candidate.CurrentRole == _role ? _stickiness : 0f);
        }
    }
}
