using UnityEngine;

// Launches all nearby rigidbodies away from the player.
[CreateAssetMenu(fileName = "ShockwaveAbility", menuName = "CGD/Abilities/Shockwave")]
public class ShockwaveAbility : Ability
{
    public float Radius = 6f;
    public float Force  = 18f;

    public override bool Execute(AbilityContext ctx)
    {
        Collider[] hits = Physics.OverlapSphere(ctx.PlayerTransform.position, Radius);

        foreach (var col in hits)
        {
            var rb = col.attachedRigidbody;
            if (rb == null || rb == ctx.PlayerRigidbody) continue;

            Vector3 dir = (col.transform.position - ctx.PlayerTransform.position).normalized;
            rb.AddForce(dir * Force, ForceMode.VelocityChange);
        }

        return true;
    }
}
