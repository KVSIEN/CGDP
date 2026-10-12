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

        public StyleScales(float damage, float fireRate, float recoil, float spread, float range, float magazineSize)
        {
            Damage       = damage;
            FireRate     = fireRate;
            Recoil       = recoil;
            Spread       = spread;
            Range        = range;
            MagazineSize = magazineSize;
        }

        public static StyleScales Identity => new(1f, 1f, 1f, 1f, 1f, 1f);

        public static StyleScales operator *(StyleScales a, StyleScales b) =>
            new(a.Damage * b.Damage, a.FireRate * b.FireRate, a.Recoil * b.Recoil,
                a.Spread * b.Spread, a.Range * b.Range, a.MagazineSize * b.MagazineSize);

        // The range stretched by `scale`, its odds shifted by `lean` (see StyleLeans).
        public static FloatRange Scale(FloatRange range, float scale, float lean) =>
            new(range.Min * scale, range.Max * scale, Mathf.Clamp(range.Bias + lean, -1f, 1f));

        public static IntRange Scale(IntRange range, float scale, float lean) =>
            new(Mathf.Max(1, Mathf.RoundToInt(range.Min * scale)), Mathf.Max(1, Mathf.RoundToInt(range.Max * scale)),
                Mathf.Clamp(range.Bias + lean, -1f, 1f));
    }
}
