using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "CGD/Fire Behaviors/Projectile")]
public class ProjectileFireBehavior : WeaponFireBehavior
{
    [SerializeField] private Projectile _prefab;
    [SerializeField] private float      _speed    = 60f;
    [SerializeField] private float      _lifetime = 5f;

    public override void Execute(FireContext ctx)
    {
        if (_prefab == null) return;

        Vector3    origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
        Projectile p      = Object.Instantiate(_prefab, origin, Quaternion.LookRotation(ctx.Direction));
        p.Speed    = _speed;
        p.Lifetime = _lifetime;
        p.Damage   = ctx.Data.Damage;
    }
}
