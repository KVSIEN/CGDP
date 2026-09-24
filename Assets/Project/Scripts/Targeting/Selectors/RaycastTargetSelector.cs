using UnityEngine;

namespace CGD.Targeting
{
    // The character under the crosshair: first solid hit along the aim. The hit point is
    // kept even when it isn't a valid target, so aimed effects still know where they landed.
    [CreateAssetMenu(fileName = "RaycastTargetSelector", menuName = "CGD/Targeting/Raycast")]
    public class RaycastTargetSelector : TargetSelector
    {
        [SerializeField, Min(0f)] private float _range = 30f;

        protected override void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result)
        {
            if (!TargetQuery.Raycast(request.Origin, request.Direction, _range, filter, out var target, out var hit)) return;

            result.SetPoint(hit.point);
            if (target != null) result.Targets.Add(target);
        }
    }
}
