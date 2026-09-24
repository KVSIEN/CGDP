using UnityEngine;

namespace CGD.Targeting
{
    // Everyone in a sphere — around the caster (novas, auras, party heals) or around the
    // point on the ground the caster is aiming at (AoE strikes, grenades-by-ability).
    [CreateAssetMenu(fileName = "AreaTargetSelector", menuName = "CGD/Targeting/Area")]
    public class AreaTargetSelector : TargetSelector
    {
        public enum AreaCenter
        {
            Caster,
            AimedGround,
        }

        [SerializeField] private AreaCenter _center = AreaCenter.Caster;
        [SerializeField, Min(0f)] private float _radius = 6f;
        [Tooltip("Raises the centre off the ground so line-of-sight checks don't graze the floor")]
        [SerializeField, Min(0f)] private float _centerHeight = 1f;

        [Header("Aimed Ground")]
        [SerializeField, Min(0f)] private float _aimRange = 25f;
        [SerializeField] private LayerMask _groundMask = 1; // Default layer

        protected override void Collect(in TargetingRequest request, in TargetFilter filter, TargetSet result)
        {
            Vector3 center = _center == AreaCenter.AimedGround
                ? AimedPoint(request)
                : request.CasterPosition;

            result.SetPoint(center);
            TargetQuery.Sphere(center + Vector3.up * _centerHeight, _radius, filter, result.Targets);
        }

        private Vector3 AimedPoint(in TargetingRequest request)
        {
            TargetQuery.GroundPoint(request.Origin, request.Direction, _aimRange, _groundMask, out Vector3 point);
            return point;
        }
    }
}
