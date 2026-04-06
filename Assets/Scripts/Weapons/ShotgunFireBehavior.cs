using UnityEngine;

[CreateAssetMenu(fileName = "ShotgunBehavior", menuName = "CGD/Fire Behaviors/Shotgun")]
public class ShotgunFireBehavior : WeaponFireBehavior
{
    public override void Execute(FireContext ctx)
    {
        int pellets = Mathf.Max(1, ctx.Data.PelletCount);
        for (int i = 0; i < pellets; i++)
            FirePellet(ctx);
    }

    private static void FirePellet(FireContext ctx)
    {
        Vector3 dir = ComputeSpreadDirection(ctx.CameraForward, ctx.SpreadDeg);

        bool didHit = Physics.Raycast(ctx.CameraPosition, dir, out RaycastHit hit,
            ctx.Data.RangeFalloffEnd, ctx.Data.HitMask, QueryTriggerInteraction.Ignore);

        if (ctx.DebugDraw)
        {
            Vector3 origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
            Vector3 end    = didHit ? hit.point : origin + dir * ctx.Data.RangeFalloffEnd;
            Debug.DrawLine(origin, end, didHit ? ctx.DebugHitColor : ctx.DebugMissColor, ctx.DebugLineDuration);
        }

        if (!didHit) return;

        bool  headshot = hit.collider.CompareTag("Head");
        float damage   = CalculateDamage(ctx.Data, hit.distance, headshot);
        ApplyHitDamage(hit, damage, headshot);
    }
}
