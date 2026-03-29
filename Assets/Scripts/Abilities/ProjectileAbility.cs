using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileAbility", menuName = "CGD/Abilities/Projectile")]
public class ProjectileAbility : Ability
{
    public GameObject ProjectilePrefab;

    // Distance in front of the camera to spawn — prevents clipping through geometry directly ahead
    public float SpawnOffset = 1.5f;

    public override bool Execute(AbilityContext ctx)
    {
        if (ProjectilePrefab == null) return false;

        Vector3 spawnPos = ctx.CameraTransform.position + ctx.CameraTransform.forward * SpawnOffset;
        var go = Instantiate(ProjectilePrefab, spawnPos, ctx.CameraTransform.rotation);

        // Prevent the projectile from triggering on the player who fired it
        if (ctx.PlayerCollider != null && go.TryGetComponent<Collider>(out var col))
            Physics.IgnoreCollision(col, ctx.PlayerCollider);

        return true;
    }
}
