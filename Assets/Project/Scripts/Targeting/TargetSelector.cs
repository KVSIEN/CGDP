using UnityEngine;
using CGD.Combat;

namespace CGD.Targeting
{
    // How an ability, attack or effect chooses its targets: one asset per targeting rule
    // (a heal that picks allies around the caster, a cone that picks enemies in front...).
    // Shared and stateless, like WeaponFireBehavior — add a new rule by subclassing.
    public abstract class TargetSelector : ScriptableObject
    {
        [Header("Filter")]
        [SerializeField] private TargetAffiliation _affiliation = TargetAffiliation.Hostiles;
        [Tooltip("Layers characters can be found on")]
        [SerializeField] private LayerMask _targetMask = ~0;
        [Tooltip("Maximum targets kept, nearest first (0 = no limit)")]
        [SerializeField, Min(0)] private int _maxTargets;

        [Header("Line of Sight")]
        [SerializeField] private bool      _requireLineOfSight = true;
        [Tooltip("Geometry that blocks line of sight")]
        [SerializeField] private LayerMask _obstacleMask = 1; // Default layer

        // Clears the set, then fills it.
        public void Select(in TargetingRequest request, TargetSet result)
        {
            result.Clear();
            Collect(request, Filter(request), result);

            if (_maxTargets <= 0 || result.Count <= _maxTargets) return;

            TargetQuery.SortByDistance(result.Targets, result.HasPoint ? result.Point : request.CasterPosition);
            result.Targets.RemoveRange(_maxTargets, result.Count - _maxTargets);
        }

        protected abstract void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result);

        protected TargetFilter Filter(in TargetingRequest request) =>
            new(_affiliation, request.Team, request.Caster, _targetMask, _requireLineOfSight, _obstacleMask);
    }
}
