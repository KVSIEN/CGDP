using UnityEngine;
using CGD.Combat;

namespace CGD.Impacts
{
    // Which effects a hit produces, looked up by what was hit: characters (anything with a
    // Hitbox or HealthManager) use Flesh, other colliders are matched by physics material,
    // and everything else falls back to Default.
    [CreateAssetMenu(fileName = "ImpactDatabase", menuName = "CGD/Impacts/Impact Database")]
    public class ImpactDatabase : ScriptableObject
    {
        [SerializeField] private SurfaceImpacts[] _surfaces = System.Array.Empty<SurfaceImpacts>();
        [Tooltip("Surfaces with no matching physics material")]
        [SerializeField] private SurfaceImpacts _default = new();
        [Tooltip("Characters and other damageable targets")]
        [SerializeField] private SurfaceImpacts _flesh = new();

        public ImpactEffect Resolve(in Impact impact)
        {
            Collider collider = impact.Collider;
            if (collider.TryGetComponent(out Hitbox _) || collider.TryGetComponent(out HealthManager _))
                return _flesh.For(impact.Kind);

            PhysicsMaterial material = collider.sharedMaterial;
            if (material != null)
                foreach (SurfaceImpacts surface in _surfaces)
                    if (surface.Material == material) return surface.For(impact.Kind);

            return _default.For(impact.Kind);
        }
    }
}
