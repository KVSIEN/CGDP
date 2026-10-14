using System;
using UnityEngine;
using CGD.Core;

namespace CGD.Weapons
{
    // Multipliers a weapon style applies to its category's stat ranges before the roll
    // samples them, so quality still decides where a stat lands inside the scaled range.
    [Serializable]
    public struct StyleScales
    {
        [Min(0.01f)] public float Damage;
        [Min(0.01f)] public float FireRate;
        [Tooltip("Kick and its accumulated caps")]
        [Min(0.01f)] public float Recoil;
        [Tooltip("Hip-fire cone and bloom")]
        [Min(0.01f)] public float Spread;
        [Tooltip("Optimal range and falloff end")]
        [Min(0.01f)] public float Range;
        [Min(0.01f)] public float MagazineSize;
        [Tooltip("Time to full draw (Charge fire mode): a warbow's slow pull, a horsebow's quick one")]
        [Min(0.01f)] public float ChargeTime;
        [Tooltip("Projectile flight speed: higher flies flatter, like a compound bow's arrow")]
        [Min(0.01f)] public float ProjectileSpeed;
        [Tooltip("Reload time, full and tactical: a crossbow's spanning, a lever's quick pull or a windlass's slow crank")]
        [Min(0.01f)] public float Reload;

        public StyleScales(float damage, float fireRate, float recoil, float spread, float range, float magazineSize,
                           float chargeTime = 1f, float projectileSpeed = 1f, float reload = 1f)
        {
            Damage          = damage;
            FireRate        = fireRate;
            Recoil          = recoil;
            Spread          = spread;
            Range           = range;
            MagazineSize    = magazineSize;
            ChargeTime      = chargeTime;
            ProjectileSpeed = projectileSpeed;
            Reload          = reload;
        }

        public static StyleScales Identity => new(1f, 1f, 1f, 1f, 1f, 1f);

        public static StyleScales operator *(StyleScales a, StyleScales b) =>
            new(a.Damage * b.Damage, a.FireRate * b.FireRate, a.Recoil * b.Recoil,
                a.Spread * b.Spread, a.Range * b.Range, a.MagazineSize * b.MagazineSize,
                a.ChargeTime * b.ChargeTime, a.ProjectileSpeed * b.ProjectileSpeed, a.Reload * b.Reload);

        // The range stretched by `scale`, its odds shifted by `lean` (see StyleLeans).
        public static FloatRange Scale(FloatRange range, float scale, float lean) =>
            new(range.Min * scale, range.Max * scale, Mathf.Clamp(range.Bias + lean, -1f, 1f));

        public static IntRange Scale(IntRange range, float scale, float lean) =>
            new(Mathf.Max(1, Mathf.RoundToInt(range.Min * scale)), Mathf.Max(1, Mathf.RoundToInt(range.Max * scale)),
                Mathf.Clamp(range.Bias + lean, -1f, 1f));
    }
}
