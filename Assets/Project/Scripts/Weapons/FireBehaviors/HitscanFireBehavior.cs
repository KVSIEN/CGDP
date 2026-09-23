using UnityEngine;

namespace CGD.Weapons
{
    [CreateAssetMenu(fileName = "HitscanBehavior", menuName = "CGD/Weapons/Fire Behaviors/Hitscan")]
    public class HitscanFireBehavior : WeaponFireBehavior
    {
        public override void Execute(FireContext ctx) => FireHitscanRay(ctx, ctx.Direction);
    }
}
