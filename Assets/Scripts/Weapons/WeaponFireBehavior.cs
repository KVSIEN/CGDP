using UnityEngine;

public struct FireContext
{
    public Vector3    CameraPosition;
    public Vector3    CameraForward;     // un-spread forward; multi-pellet behaviors use this as their cone axis
    public float      SpreadDeg;         // cone half-angle computed by WeaponController
    public Vector3    Direction;         // spread applied once; single-pellet behaviors use this directly
    public Transform  Muzzle;
    public WeaponData Data;
    public bool       DebugDraw;
    public Color      DebugHitColor;
    public Color      DebugMissColor;
    public float      DebugLineDuration;
}

public abstract class WeaponFireBehavior : ScriptableObject
{
    public abstract void Execute(FireContext ctx);

    public static Vector3 ComputeSpreadDirection(Vector3 forward, float spreadDeg)
    {
        if (spreadDeg <= 0f) return forward;
        float radius = Mathf.Tan(spreadDeg * Mathf.Deg2Rad);
        Vector2 offset = Random.insideUnitCircle * radius;
        Vector3 up = Mathf.Abs(Vector3.Dot(forward, Vector3.up)) > 0.99f ? Vector3.right : Vector3.up;
        Quaternion rot = Quaternion.LookRotation(forward, up);
        return (rot * new Vector3(offset.x, offset.y, 1f)).normalized;
    }

    protected static float CalculateDamage(WeaponData data, float distance, bool headshot)
    {
        float t       = Mathf.InverseLerp(data.RangeOptimal, data.RangeFalloffEnd, distance);
        float falloff = Mathf.Lerp(1f, data.DamageFalloffMin, t);
        float dmg     = data.Damage * falloff;
        if (headshot) dmg *= data.HeadshotMultiplier;
        return dmg;
    }

    protected static void ApplyHitDamage(RaycastHit hit, float damage, bool headshot)
    {
        if (hit.collider.TryGetComponent<PlayerStats>(out var ps))
            ps.TakeDamage(damage);
        else if (hit.collider.TryGetComponent<EnemyHealth>(out var eh))
            eh.TakeDamageAt(damage, hit.point, headshot);
    }
}
