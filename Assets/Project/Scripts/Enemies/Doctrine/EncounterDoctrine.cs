using System;
using UnityEngine;

namespace CGD.Enemies
{
    // How a faction fights as a group (Halo-style encounters, F.E.A.R.-style roles): kin of one
    // faction fighting the same target form an encounter on their own — no squad needs placing
    // — with a lead picked by its succession rule and roles handed out from a table. When the
    // lead falls, the one who succeeds it steps up (its nerve steadies and it takes the lead's
    // job) and the rest may shift posture to regroup on a second table for a while. The
    // defaults match how squads always worked: the nearest ranged member suppresses, the two
    // nearest melee members go in, everyone else flanks, and nothing changes when a lead falls.
    [Serializable]
    public sealed class EncounterDoctrine
    {
        [Tooltip("Forms encounters even without an authored squad (squad members always do)")]
        [SerializeField] private bool _coordinates;
        [SerializeField] private SuccessionRule _succession = SuccessionRule.Rank;

        [Header("Steady")]
        [SerializeField] private RoleSlot[] _roles =
        {
            new(SquadRole.Suppress, 1, RoleFilter.Ranged),
            new(SquadRole.Assault,  2, RoleFilter.Melee),
        };
        [Tooltip("Role for anyone the slots leave out")]
        [SerializeField] private SquadRole _fallbackRole = SquadRole.Flank;

        [Header("When the lead falls")]
        [Tooltip("Steady = carry on as before; Regroup = hold and re-form on the regroup roles")]
        [SerializeField] private EncounterPosture _onLeadLost = EncounterPosture.Steady;
        [Tooltip("Seconds the regroup lasts")]
        [SerializeField, Min(0f)] private float _regroupSeconds = 8f;
        [SerializeField] private RoleSlot[] _regroupRoles = Array.Empty<RoleSlot>();
        [SerializeField] private SquadRole _regroupFallbackRole = SquadRole.None;
        [Tooltip("Seconds the successor counts as having stepped up (bolder, see Temperament)")]
        [SerializeField, Min(0f)] private float _steppedUpSeconds;
        [Tooltip("Morale the successor regains as it steps up")]
        [SerializeField, Range(0f, 1f)] private float _successorNerve;

        public bool Coordinates                => _coordinates;
        public SuccessionRule Succession       => _succession;
        public EncounterPosture OnLeadLost     => _onLeadLost;
        public float RegroupSeconds            => _regroupSeconds;
        public float SteppedUpSeconds          => _steppedUpSeconds;
        public float SuccessorNerve            => _successorNerve;

        public RoleSlot[] RolesFor(EncounterPosture posture) => posture == EncounterPosture.Regroup ? _regroupRoles : _roles;
        public SquadRole FallbackFor(EncounterPosture posture) => posture == EncounterPosture.Regroup ? _regroupFallbackRole : _fallbackRole;
    }
}
