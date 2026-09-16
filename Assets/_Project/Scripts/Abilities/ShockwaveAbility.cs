using UnityEngine;

// Launches all nearby rigidbodies away from the player.
[CreateAssetMenu(fileName = "ShockwaveAbility", menuName = "CGD/Abilities/Shockwave")]
public class ShockwaveAbility : Ability
{
    public float Radius = 6f;
    public float Force  = 18f;

    private static readonly Collider[] _hitBuffer = new Collider[32];

    public override bool Execute(AbilityContext ctx)
    {
        int count = Physics.OverlapSphereNonAlloc(ctx.PlayerTransform.position, Radius, _hitBuffer);

        for (int i = 0; i < count; i++)
        {
            var rb = _hitBuffer[i].attachedRigidbody;
            if (rb == null || rb == ctx.PlayerRigidbody) continue;

            Vector3 dir = (_hitBuffer[i].transform.position - ctx.PlayerTransform.position).normalized;
            rb.AddForce(dir * Force, ForceMode.VelocityChange);
        }

        return true;
    }
}
