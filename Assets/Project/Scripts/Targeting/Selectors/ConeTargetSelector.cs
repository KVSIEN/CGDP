using UnityEngine;

namespace CGD.Targeting
{
    // Everyone in front of the aim within a cone: breath attacks, shotgun-style blasts,
    // cleaves, sweeping heals.
    [CreateAssetMenu(fileName = "ConeTargetSelector", menuName = "CGD/Targeting/Cone")]
    public class ConeTargetSelector : TargetSelector
    {
        [SerializeField, Min(0f)] private float _range = 8f;
        [Tooltip("Full cone angle in degrees — 90 means 45° either side of the aim")]
        [SerializeField, Range(1f, 360f)] private float _angle = 60f;

        protected override void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result) =>
            TargetQuery.Cone(request.Origin, request.Direction, _range, _angle, filter, result.Targets);
    }
}
