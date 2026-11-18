using System;
using System.Collections.Generic;
using UnityEngine;
using CGD.Items;
using CGD.Weapons;

namespace CGD.Editor
{
    // Starting points for a new firearm model, so nobody fills a blank one in from scratch:
    // recoil feels (the shapes real guns kick in) and whole-model archetypes built from them.
    // Scales are relative to the category's own ranges, so any archetype can be dropped into
    // any category and then tuned in the Firearm Lab.
    internal static class FirearmTemplates
    {
        internal readonly struct RecoilPreset
        {
            public readonly string        Name;
            public readonly RecoilPattern Pattern;
            public RecoilPreset(string name, RecoilPattern pattern) { Name = name; Pattern = pattern; }
        }

        internal readonly struct Archetype
        {
            public readonly string Name;
            public readonly Func<WeaponModel> Create;
            public Archetype(string name, Func<WeaponModel> create) { Name = name; Create = create; }
        }

        public static readonly IReadOnlyList<RecoilPreset> RecoilPresets = new[]
        {
            new RecoilPreset("Straight up (Vector, MP5)", new RecoilPattern { Bias = 0f, Jitter = 0.1f, Horizontal = 0.3f, Reach = 0.5f }),
            new RecoilPreset("Climbs right (Uzi)",        new RecoilPattern { Bias = 0.35f, Drift = HorizontalDriftMode.OneWay, Jitter = 0.25f, Horizontal = 1.2f, Reach = 1.3f }),
            new RecoilPreset("Climbs left (SCAR-H)",      new RecoilPattern { Bias = -0.35f, Drift = HorizontalDriftMode.OneWay, Jitter = 0.25f, Horizontal = 1.2f, Reach = 1.3f }),
            new RecoilPreset("Side to side (AK-47)",      new RecoilPattern { Bias = 0.5f, Jitter = 0.2f, Horizontal = 1.3f, Reach = 0.6f }),
            new RecoilPreset("Wild (MAC-10)",             new RecoilPattern { Bias = 0f, Jitter = 0.9f, Horizontal = 1.5f, Reach = 1.5f }),
            new RecoilPreset("Snap up (semi-auto)",       new RecoilPattern { Bias = 0.1f, Jitter = 0.3f, Horizontal = 0.6f, MuzzleFlip = 0.8f }),
            new RecoilPreset("Shoves back (Glock)",       new RecoilPattern { Bias = 0.1f, Jitter = 0.3f, Horizontal = 0.8f, MuzzleFlip = 0.1f,
                                                                              Kick = new KickWeights { Rise = 0.6f, Yaw = 1f, Roll = 1f, Hop = 0.6f, Pushback = 2.5f } }),
            new RecoilPreset("Heavy flip (Desert Eagle)", new RecoilPattern { Bias = 0.2f, Jitter = 0.3f, Horizontal = 0.7f, MuzzleFlip = 1f,
                                                                              Kick = new KickWeights { Rise = 1.3f, Yaw = 1f, Roll = 1.2f, Hop = 1.5f, Pushback = 0.5f } }),
        };

        public static readonly IReadOnlyList<Archetype> Archetypes = new[]
        {
            new Archetype("Blank model",           () => Model("New Model")),
            new Archetype("Semi-auto pistol",      () => Model("New Pistol",  Semi(), new StyleScales(1f, 1f, 0.9f, 1f, 0.8f, 0.7f),  Preset(5))),
            new Archetype("Machine pistol / SMG",  () => Model("New SMG",     Auto(), new StyleScales(0.8f, 1.25f, 0.9f, 1.1f, 0.7f, 1.2f), Preset(0))),
            new Archetype("Assault rifle (auto)",  () => Model("New Rifle",   Auto(), StyleScales.Identity, Preset(3))),
            new Archetype("Marksman rifle (semi)", () => Model("New Marksman", Semi(), new StyleScales(1.5f, 0.6f, 1.2f, 0.7f, 1.4f, 0.6f), Preset(5))),
            new Archetype("Bolt-action rifle",     () => Bolt()),
            new Archetype("Pump shotgun",          () => Pump()),
            new Archetype("Revolver",              () => Revolver()),
        };

        // A pattern of its own, so a model never shares one with the preset it started from.
        public static RecoilPattern Copy(RecoilPattern pattern) => JsonUtility.FromJson<RecoilPattern>(JsonUtility.ToJson(pattern));

        private static RecoilPattern Preset(int index) => Copy(RecoilPresets[index].Pattern);

        // A model of its own: nothing in it is shared with the model it was copied from.
        public static WeaponModel Copy(WeaponModel model) => JsonUtility.FromJson<WeaponModel>(JsonUtility.ToJson(model));

        private static FiringStyle Semi() => new() { FireMode = FireMode.Semi };
        private static FiringStyle Auto() => new() { FireMode = FireMode.Auto };

        private static WeaponModel Model(string name, FiringStyle firing = null, StyleScales? scales = null, RecoilPattern recoil = null) => new()
        {
            Name         = name,
            FiringStyles = firing != null ? new[] { firing } : Array.Empty<FiringStyle>(),
            Scales       = scales ?? StyleScales.Identity,
            Recoil       = recoil ?? new RecoilPattern(),
        };

        private static WeaponModel Bolt()
        {
            WeaponModel model = Model("New Bolt Rifle", Semi(), new StyleScales(2f, 0.35f, 1.4f, 0.5f, 1.6f, 0.3f), Preset(5));
            model.RoundsPerLoad = 1;
            return model;
        }

        private static WeaponModel Pump()
        {
            WeaponModel model = Model("New Shotgun", Semi(), new StyleScales(1f, 0.4f, 1.5f, 1.4f, 0.4f, 0.4f), Preset(5));
            model.RoundsPerLoad = 1;
            return model;
        }

        private static WeaponModel Revolver()
        {
            WeaponModel model = Model("New Revolver", Semi(), new StyleScales(1.6f, 0.5f, 1.3f, 0.8f, 1f, 0.25f), Preset(7));
            model.FixedMagazineSize = 6;
            model.AmmoType          = AmmoType.HeavyRounds;
            return model;
        }
    }
}
