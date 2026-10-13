using CGD.Core;
using CGD.Items;

namespace CGD.Weapons
{
    // What gun a roll makes, before any stat is rolled: the model, its firing variant and the
    // stat style, and everything they decide between them. Each answer is settled here once —
    // variant over model over category — so the stat rolls read it instead of re-deciding.
    internal sealed class WeaponBuild
    {
        public readonly string     Name;
        public readonly FireMode   FireMode;
        public readonly FiringStyle Firing;  // null for a category without firing styles
        // Model × variant × stat style: how far each stat's range is stretched.
        public readonly StyleScales Scales;
        // Toward the gun itself (model × variant, not the stat style); none without a model.
        public readonly StyleLeans  Leans;
        // Null without a model or a variant's own pattern: the category's random lean applies.
        public readonly RecoilPattern Recoil;
        public readonly WeaponFireBehavior FireBehavior;
        public readonly IntRange   PelletCount;
        public readonly FloatRange AdsSpreadDeg;
        public readonly AmmoType   AmmoType;
        public readonly int        RoundsPerLoad;
        public readonly int        FixedMagazineSize;   // 0 = rolled

        // Draws, in order: the model from its own fork (none from `random`), a name when there's
        // no model, a firing style, a fire mode when there's no firing style, a stat style. Seeds
        // depend on this order.
        public static WeaponBuild Pick(WeaponCategoryData cat, RandomStream random)
        {
            WeaponModel model = PickWeighted(cat.Models, m => m.Weight, random.Fork("model"));
            string name       = model != null ? model.Name : random.Pick(cat.Names);
            FiringStyle[] firingStyles = model != null && model.FiringStyles.Length > 0 ? model.FiringStyles : cat.FiringStyles;
            FiringStyle firing = PickWeighted(firingStyles, s => s.Weight, random);
            FireMode fireMode  = firing != null ? firing.FireMode : random.Pick(cat.FireModes);
            StatStyle statStyle = PickWeighted(cat.StatStyles, s => s.Weight, random);
            return new WeaponBuild(cat, model, firing, statStyle, name, fireMode);
        }

        private WeaponBuild(WeaponCategoryData cat, WeaponModel model, FiringStyle firing, StatStyle statStyle,
                            string name, FireMode fireMode)
        {
            Firing   = firing;
            FireMode = fireMode;
            Name     = StyledName(!string.IsNullOrEmpty(firing?.ModelName) ? firing.ModelName : name, firing, statStyle);

            StyleScales gun = (model  != null ? model.Scales  : StyleScales.Identity)
                            * (firing != null ? firing.Scales : StyleScales.Identity);
            Scales = gun * (statStyle != null ? statStyle.Scales : StyleScales.Identity);
            Leans  = model != null ? StyleLeans.From(gun) : default;
            Recoil = firing != null && firing.OwnRecoil ? firing.Recoil : model?.Recoil;

            FireBehavior  = firing != null && firing.FireBehavior != null ? firing.FireBehavior : cat.FireBehavior;
            PelletCount   = firing != null && firing.PelletCount.Max > 0 ? firing.PelletCount : cat.PelletCount;
            AdsSpreadDeg  = firing != null && firing.AdsSpreadDeg.Max > 0f ? firing.AdsSpreadDeg : cat.AdsSpreadDeg;
            AmmoType      = model != null && model.AmmoType != AmmoType.None ? model.AmmoType : cat.AmmoType;
            RoundsPerLoad = firing != null && firing.RoundsPerLoad > 0 ? firing.RoundsPerLoad
                          : model != null ? model.RoundsPerLoad : 0;
            FixedMagazineSize = model != null ? model.FixedMagazineSize : 0;
        }

        public bool IsAutomatic => FireMode == FireMode.Auto || FireMode == FireMode.Burst;

        // Null when there's nothing to pick from, without drawing from the stream.
        private static T PickWeighted<T>(T[] options, System.Func<T, float> weight, RandomStream random) where T : class =>
            options != null && options.Length > 0 ? random.PickWeighted(options, weight) : null;

        // "Marksman Burst Modular Pistol"; a style without a name adds nothing.
        private static string StyledName(string name, FiringStyle firing, StatStyle statStyle)
        {
            if (!string.IsNullOrEmpty(firing?.Name))    name = $"{firing.Name} {name}";
            if (!string.IsNullOrEmpty(statStyle?.Name)) name = $"{statStyle.Name} {name}";
            return name;
        }
    }
}
