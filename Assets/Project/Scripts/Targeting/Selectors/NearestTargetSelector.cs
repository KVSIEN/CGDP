using UnityEngine;

namespace CGD.Targeting
{
    // The N closest valid characters around the caster: homing shots, auto-target,
    // "heal the nearest ally", chain starts.
    [CreateAssetMenu(fileName = "NearestTargetSelector", menuName = "CGD/Targeting/Nearest")]
    public class NearestTargetSelector : TargetSelector
    {
        [SerializeField, Min(0f)] private float _radius = 15f;
        [SerializeField, Min(1)] private int _count = 1;
        [Tooltip("Raises the search origin off the ground so line-of-sight checks don't graze the floor")]
        [SerializeField, Min(0f)] private float _originHeight = 1f;

        protected override void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result)
        {
            Vector3 origin = request.CasterPosition + Vector3.up * _originHeight;
            if (TargetQuery.Sphere(origin, _radius, filter, result.Targets) == 0) return;

            TargetQuery.SortByDistance(result.Targets, request.CasterPosition);
            if (result.Count > _count) result.Targets.RemoveRange(_count, result.Count - _count);
        }
    }
}
