using UnityEngine;

[CreateAssetMenu(fileName = "DashAbility", menuName = "CGD/Abilities/Dash")]
public class DashAbility : Ability
{
    public float Force = 12f;

    public override bool Execute(AbilityContext ctx)
    {
        // Dash in the move direction, or camera forward if the player is standing still
        Vector3 dashDir;
        if (ctx.MoveInput.magnitude > 0.1f)
        {
            Vector3 camForward = Vector3.ProjectOnPlane(ctx.CameraTransform.forward, Vector3.up).normalized;
            Vector3 camRight   = Vector3.ProjectOnPlane(ctx.CameraTransform.right,   Vector3.up).normalized;
            dashDir = (camForward * ctx.MoveInput.y + camRight * ctx.MoveInput.x).normalized;
        }
        else
        {
            dashDir = Vector3.ProjectOnPlane(ctx.CameraTransform.forward, Vector3.up).normalized;
        }

        ctx.PlayerRigidbody.AddForce(dashDir * Force, ForceMode.VelocityChange);
        return true;
    }
}
