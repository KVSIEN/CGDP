namespace CGD.Weapons
{
    // Per-type bands for the "Apply Type Defaults" context menu. Step damage is relative
    // to the rolled Damage. Light weapons trade damage for speed, reach and a long parry
    // window; heavy ones hit hard and pierce armour but swing slowly and parry poorly.
    public static class MeleeCategoryDefaults
    {
        // Share of the recovery before a guard or dodge can cut it short (see MeleeAttackStep.CancelFrom).
        // Combo steps cancel early so parries can be woven in; finishers and heavies commit.
        private const float ComboStep = 0.4f;
        private const float Finisher  = 0.75f;

        public static void Apply(MeleeCategoryData c)
        {
            switch (c.Type)
            {
                case MeleeWeaponType.Dagger: ApplyDagger(c); break;
                case MeleeWeaponType.Sword:  ApplySword(c);  break;
                case MeleeWeaponType.Axe:    ApplyAxe(c);    break;
                case MeleeWeaponType.Hammer: ApplyHammer(c); break;
                case MeleeWeaponType.Spear:  ApplySpear(c);  break;
            }
        }

        // Fast stabs; the best parry window, but a thin guard that drains stamina.
        private static void ApplyDagger(MeleeCategoryData c)
        {
            c.Names = new[] { "Combat Knife", "Stiletto", "Kukri", "Trench Knife", "Dirk" };

            c.LightCombo = new[]
            {
                Step(MeleeHitShape.Thrust, 1.0f, 0.06f, 0.10f, 0.16f, 1.6f, 0.32f),
                Step(MeleeHitShape.Sweep,  1.0f, 0.06f, 0.10f, 0.18f, 1.5f, 0.32f, arc: 80f),
                Step(MeleeHitShape.Thrust, 1.4f, 0.10f, 0.12f, 0.28f, 1.7f, 0.32f, cancel: Finisher),
            };
            c.HeavyAttack        = Step(MeleeHitShape.Thrust, 2.5f, 0.20f, 0.12f, 0.40f, 1.8f, 0.35f, crit: 2f, cancel: Finisher);
            c.HeavyHoldThreshold = 0.3f;
            c.ComboResetTime     = 1.0f;

            c.Damage           = new(14f,  22f);
            c.AttackSpeed      = new(1.2f, 1.5f);
            c.Reach            = new(0.9f, 1.0f);
            c.ArmorPenetration = new(0f,   0.1f);
            c.StaminaCost      = new(5f,   8f);

            c.BlockDamageMultiplier = new(0.4f, 0.5f);
            c.ParryWindow           = new(0.25f, 0.3f);
            c.Guard                 = new(0.45f, 0.6f, 100f, 0.28f, 1.6f);
        }

        // All-rounder: wide slashes into a finishing thrust; a steady guard.
        private static void ApplySword(MeleeCategoryData c)
        {
            c.Names = new[] { "Machete", "Gladius", "Sabre", "Longsword", "Cutlass" };

            c.LightCombo = new[]
            {
                Step(MeleeHitShape.Sweep,  1.00f, 0.10f, 0.14f, 0.24f, 1.9f, 0.40f, arc: 100f),
                Step(MeleeHitShape.Sweep,  1.05f, 0.10f, 0.14f, 0.24f, 1.9f, 0.40f, arc: 100f),
                Step(MeleeHitShape.Thrust, 1.40f, 0.14f, 0.12f, 0.34f, 2.2f, 0.40f, cancel: Finisher),
            };
            c.HeavyAttack        = Step(MeleeHitShape.Sweep, 2.4f, 0.30f, 0.18f, 0.45f, 2.1f, 0.45f, arc: 140f, cancel: Finisher);
            c.HeavyHoldThreshold = 0.35f;
            c.ComboResetTime     = 1.2f;

            c.Damage           = new(22f,  32f);
            c.AttackSpeed      = new(1.0f, 1.2f);
            c.Reach            = new(1.0f, 1.1f);
            c.ArmorPenetration = new(0f,   0.15f);
            c.StaminaCost      = new(8f,   12f);

            c.BlockDamageMultiplier = new(0.25f, 0.35f);
            c.ParryWindow           = new(0.18f, 0.22f);
            c.Guard                 = new(0.3f, 0.5f, 120f, 0.2f, 1.2f);
        }

        // Heavy chops that bite through armour; a narrow, stamina-hungry guard.
        private static void ApplyAxe(MeleeCategoryData c)
        {
            c.Names = new[] { "Hatchet", "Fire Axe", "Bearded Axe", "Tomahawk", "Breaching Axe" };

            c.LightCombo = new[]
            {
                Step(MeleeHitShape.Sweep, 1.0f, 0.16f, 0.14f, 0.30f, 1.9f, 0.45f, arc: 90f),
                Step(MeleeHitShape.Slam,  1.3f, 0.22f, 0.10f, 0.40f, 1.6f, 0.80f, cancel: Finisher),
            };
            c.HeavyAttack        = Step(MeleeHitShape.Slam, 2.8f, 0.40f, 0.12f, 0.55f, 1.7f, 1.0f, cancel: Finisher);
            c.HeavyHoldThreshold = 0.4f;
            c.ComboResetTime     = 1.3f;

            c.Damage           = new(28f,  40f);
            c.AttackSpeed      = new(0.85f, 1.0f);
            c.Reach            = new(1.0f, 1.1f);
            c.ArmorPenetration = new(0.1f, 0.25f);
            c.StaminaCost      = new(10f,  15f);

            c.BlockDamageMultiplier = new(0.35f, 0.45f);
            c.ParryWindow           = new(0.13f, 0.17f);
            c.Guard                 = new(0.4f, 0.55f, 110f, 0.15f, 1.2f);
        }

        // Slow area slams that crush armour; the sturdiest block, the tightest parry.
        private static void ApplyHammer(MeleeCategoryData c)
        {
            c.Names = new[] { "Sledgehammer", "Maul", "War Hammer", "Breaching Hammer", "Mace" };

            c.LightCombo = new[]
            {
                Step(MeleeHitShape.Slam, 1.0f, 0.24f, 0.12f, 0.42f, 1.7f, 0.9f),
                Step(MeleeHitShape.Slam, 1.2f, 0.28f, 0.12f, 0.48f, 1.7f, 1.0f, cancel: Finisher),
            };
            c.HeavyAttack        = Step(MeleeHitShape.Slam, 3.0f, 0.50f, 0.14f, 0.65f, 1.8f, 1.2f, cancel: Finisher);
            c.HeavyHoldThreshold = 0.45f;
            c.ComboResetTime     = 1.4f;

            c.Damage           = new(35f,  55f);
            c.AttackSpeed      = new(0.65f, 0.85f);
            c.Reach            = new(1.0f, 1.1f);
            c.ArmorPenetration = new(0.25f, 0.45f);
            c.StaminaCost      = new(14f,  20f);

            c.BlockDamageMultiplier = new(0.2f, 0.3f);
            c.ParryWindow           = new(0.1f, 0.14f);
            c.Guard                 = new(0.25f, 0.65f, 120f, 0.12f, 1.5f);
        }

        // Long-reach thrusts that keep enemies at bay; a narrow frontal guard.
        private static void ApplySpear(MeleeCategoryData c)
        {
            c.Names = new[] { "Spear", "Pike", "Glaive", "Trident", "Halberd" };

            c.LightCombo = new[]
            {
                Step(MeleeHitShape.Thrust, 1.0f, 0.12f, 0.12f, 0.26f, 2.4f, 0.35f),
                Step(MeleeHitShape.Thrust, 1.1f, 0.12f, 0.12f, 0.26f, 2.4f, 0.35f),
                Step(MeleeHitShape.Sweep,  0.9f, 0.16f, 0.16f, 0.32f, 2.2f, 0.40f, arc: 140f, cancel: Finisher),
            };
            c.HeavyAttack        = Step(MeleeHitShape.Thrust, 2.5f, 0.32f, 0.14f, 0.45f, 2.8f, 0.40f, cancel: Finisher);
            c.HeavyHoldThreshold = 0.35f;
            c.ComboResetTime     = 1.2f;

            c.Damage           = new(24f,  36f);
            c.AttackSpeed      = new(0.95f, 1.15f);
            c.Reach            = new(1.3f, 1.6f);
            c.ArmorPenetration = new(0.15f, 0.3f);
            c.StaminaCost      = new(9f,   13f);

            c.BlockDamageMultiplier = new(0.3f, 0.4f);
            c.ParryWindow           = new(0.18f, 0.22f);
            c.Guard                 = new(0.35f, 0.5f, 90f, 0.2f, 1.2f);
        }

        private static MeleeAttackStep Step(MeleeHitShape shape, float damage, float windup, float active,
                                            float recovery, float range, float radius, float arc = 90f, float crit = 1.5f,
                                            float cancel = ComboStep)
        {
            return new MeleeAttackStep
            {
                HitShape           = shape,
                Damage             = damage,
                WindupTime         = windup,
                ActiveTime         = active,
                RecoveryTime       = recovery,
                CancelFrom         = cancel,
                Range              = range,
                Radius             = radius,
                SweepArcDeg        = arc,
                CriticalMultiplier = crit,
            };
        }
    }
}
