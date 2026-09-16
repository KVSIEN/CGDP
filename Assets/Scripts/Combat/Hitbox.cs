using UnityEngine;

// A damage-receiving collider that forwards hits to its owning HealthManager, which
// does all the damage math. Usually a child of the owner, but any HealthManager can
// be referenced (e.g. a detached weak point).
[RequireComponent(typeof(Collider))]
public class Hitbox : MonoBehaviour
{
    [Tooltip("Auto-filled from the nearest parent HealthManager when left empty")]
    [SerializeField] private HealthManager _owner;
    [SerializeField] private HitboxRegion  _region;

    public HealthManager Owner  => _owner;
    public HitboxRegion  Region => _region;

    private void Reset() => _owner = GetComponentInParent<HealthManager>();

    private void Awake()
    {
        if (_owner == null) _owner = GetComponentInParent<HealthManager>();
        if (_owner == null) Debug.LogError($"{name}: Hitbox has no HealthManager owner.", this);
    }

    // Point hits (raycasts, projectiles): a Hitbox applies its region multiplier, a
    // bare HealthManager collider counts as Body, any other IDamageable takes flat damage.
    public static void ApplyHit(Collider collider, DamageInfo info, Vector3 point)
    {
        if (collider.TryGetComponent(out Hitbox hitbox))
        {
            if (hitbox._owner != null) hitbox._owner.TakeHit(info, hitbox._region, point);
            return;
        }

        if (collider.TryGetComponent(out HealthManager health))
            health.TakeHit(info, HitboxRegion.Body, point);
        else if (collider.TryGetComponent(out IDamageable damageable))
            damageable.TakeDamage(info);
    }

    // Area hits (explosions, melee sweeps) should damage each owner once, not once
    // per overlapping hitbox — callers dedupe on the returned target.
    public static IDamageable FindDamageable(Collider collider)
    {
        if (collider.TryGetComponent(out Hitbox hitbox))
            return hitbox._owner != null ? hitbox._owner : null;

        return collider.TryGetComponent(out IDamageable damageable) ? damageable : null;
    }
}
