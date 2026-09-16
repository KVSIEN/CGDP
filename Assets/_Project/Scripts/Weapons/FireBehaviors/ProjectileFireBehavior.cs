using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Weapons
{
    [CreateAssetMenu(fileName = "ProjectileBehavior", menuName = "CGD/Weapons/Fire Behaviors/Projectile")]
    public class ProjectileFireBehavior : WeaponFireBehavior
    {
        [SerializeField] private Projectile _prefab;
        [SerializeField] private float      _speed    = 60f;
        [SerializeField] private float      _lifetime = 5f;

        public override void Execute(FireContext ctx)
        {
            if (_prefab == null) return;

            Vector3    origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
            GameObject go     = PrefabPool.Spawn(_prefab.gameObject, origin, Quaternion.LookRotation(ctx.Direction));

            WeaponData data = ctx.Data;
            var hit = new DamageInfo(data.Damage, data.ArmorPenetration, data.DamageType,
                data.HeadshotMultiplier, ctx.Source, data.OnHitEffects);
            go.GetComponent<Projectile>().Launch(hit, _speed, _lifetime);
        }
    }
}
