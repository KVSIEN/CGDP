using UnityEngine;

namespace CGD.Weapons
{
    [CreateAssetMenu(fileName = "HitscanBehavior", menuName = "CGD/Weapons/Fire Behaviors/Hitscan")]
    public class HitscanFireBehavior : WeaponFireBehavior
    {
        public override void Execute(FireContext ctx)
        {
            float range  = ctx.Data.EffectiveMaxRange;
            bool  didHit = Physics.Raycast(ctx.CameraPosition, ctx.Direction, out RaycastHit hit,
                range, ctx.Data.HitMask, QueryTriggerInteraction.Ignore);

            if (ctx.DebugDraw)
            {
                Vector3 origin = ctx.Muzzle != null ? ctx.Muzzle.position : ctx.CameraPosition;
                Vector3 end    = didHit ? hit.point : origin + ctx.Direction * range;
                Debug.DrawLine(origin, end, didHit ? ctx.DebugHitColor : ctx.DebugMissColor, ctx.DebugLineDuration);
            }

            if (didHit) ApplyHitDamage(hit, ctx);
        }
    }
}
