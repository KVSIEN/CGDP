using UnityEngine;

[CreateAssetMenu(fileName = "HitscanBehavior", menuName = "CGD/Fire Behaviors/Hitscan")]
public class HitscanFireBehavior : WeaponFireBehavior
{
    public override void Execute(FireContext ctx)
    {
        bool didHit = Physics.Raycast(ctx.CameraPosition, ctx.Direction, out RaycastHit hit,
            ctx.Data.RangeFalloffEnd, ctx.Data.HitMask, QueryTriggerInteraction.Ignore);

        if (ctx.DebugDraw)
        {
            Vector3 origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
            Vector3 end    = didHit ? hit.point : origin + ctx.Direction * ctx.Data.RangeFalloffEnd;
            Debug.DrawLine(origin, end, didHit ? ctx.DebugHitColor : ctx.DebugMissColor, ctx.DebugLineDuration);
        }

        if (!didHit) return;

        bool  headshot = hit.collider.CompareTag("Head");
        float damage   = CalculateDamage(ctx.Data, hit.distance, headshot);
        ApplyHitDamage(hit, damage, headshot);
    }
}
