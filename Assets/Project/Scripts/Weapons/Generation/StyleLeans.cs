using UnityEngine;

namespace CGD.Weapons
{
    // Which way a model's stats tend inside their windows: + toward the top of a stat's
    // range (more damage, more recoil), − toward the bottom, 0 = anywhere alike. Added to the
    // category range's own bias, so the window stays the same and only the odds shift, making
    // a model more likely to play like itself (an M14 usually hits hard and fires slowly).
    // Derived from the style's scales rather than authored, so the two can never disagree.
    public readonly struct StyleLeans
    {
        // A scale of 1.2 leans +0.3; capped so a stat still lands anywhere in its window, and
        // scales within 5% of the category's middle stay neutral.
        private const float Strength = 1.5f;
        private const float Cap      = 0.6f;
        private const float DeadZone = 0.05f;

        public readonly float Damage;
        public readonly float FireRate;
        public readonly float Recoil;
        public readonly float Spread;
        public readonly float Range;
        public readonly float MagazineSize;
        public readonly float ChargeTime;
        public readonly float ProjectileSpeed;
        public readonly float Reload;

        private StyleLeans(StyleScales s)
        {
            Damage          = LeanOf(s.Damage);
            FireRate        = LeanOf(s.FireRate);
            Recoil          = LeanOf(s.Recoil);
            Spread          = LeanOf(s.Spread);
            Range           = LeanOf(s.Range);
            MagazineSize    = LeanOf(s.MagazineSize);
            ChargeTime      = LeanOf(s.ChargeTime);
            ProjectileSpeed = LeanOf(s.ProjectileSpeed);
            Reload          = LeanOf(s.Reload);
        }

        // Scales are relative to the category's middle, so how far one sits from 1 is how
        // strongly the style tends that way.
        public static StyleLeans From(StyleScales scales) => new(scales);

        // The lean for one scale; melee models lean their own stats with it too.
        public static float LeanOf(float scale)
        {
            float offset = scale - 1f;
            return Mathf.Abs(offset) < DeadZone ? 0f : Mathf.Clamp(Strength * offset, -Cap, Cap);
        }
    }
}
