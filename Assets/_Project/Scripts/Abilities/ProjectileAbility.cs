using UnityEngine;
using CGD.Combat;
using CGD.Core;

namespace CGD.Abilities
{
    [CreateAssetMenu(fileName = "ProjectileAbility", menuName = "CGD/Abilities/Projectile")]
    public class ProjectileAbility : Ability
    {
        [Tooltip("Prefab with a Projectile component")]
        public GameObject ProjectilePrefab;

        [Header("Projectile")]
        public float Damage   = 25f;
        public float Speed    = 25f;
        public float Lifetime = 5f;
        public StatusEffectApplication[] OnHitEffects;

        // Distance in front of the camera to spawn — prevents clipping through geometry directly ahead
        public float SpawnOffset = 1.5f;

        public override bool CanExecute(AbilityContext ctx) =>
            ProjectilePrefab != null && ProjectilePrefab.TryGetComponent<Projectile>(out _);

        public override void Execute(AbilityContext ctx)
        {
            Vector3 spawnPos = ctx.CameraTransform.position + ctx.CameraTransform.forward * SpawnOffset;
            var go = PrefabPool.Spawn(ProjectilePrefab, spawnPos, ctx.CameraTransform.rotation);

            // The projectile ignores its source's colliders, so it can't hit the caster.
            var hit = new DamageInfo(Damage, source: ctx.Source, onHitEffects: OnHitEffects);
            go.GetComponent<Projectile>().Launch(hit, Speed, Lifetime);
        }
    }
}
