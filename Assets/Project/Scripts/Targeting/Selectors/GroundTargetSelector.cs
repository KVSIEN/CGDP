using UnityEngine;

namespace CGD.Targeting
{
    // A point on the ground where the caster aims, with no characters: placing traps,
    // zones, teleport destinations, or ground-targeted ActionTimeline events.
    [CreateAssetMenu(fileName = "GroundTargetSelector", menuName = "CGD/Targeting/Ground")]
    public class GroundTargetSelector : TargetSelector
    {
        [SerializeField, Min(0f)] private float _range = 25f;
        [SerializeField] private LayerMask _groundMask = 1; // Default layer

        protected override void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result)
        {
            TargetQuery.GroundPoint(request.Origin, request.Direction, _range, _groundMask, out Vector3 point);
            result.SetPoint(point);
        }
    }
}
