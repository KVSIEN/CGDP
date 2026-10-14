using System;
using UnityEngine;

namespace CGD.Weapons
{
    // One named melee weapon a type rolls (a Sabre, a Claymore), picked per weapon by Weight
    // (see MeleeCategoryData): where it sits in its type's stat ranges and how it's held, so
    // every weapon of that name fights like it. The type's moveset stays its own.
    [Serializable]
    public class MeleeModel
    {
        public string Name;
        [Min(0f)] public float Weight = 1f;
        [Tooltip("Where this model sits in its type's ranges, e.g. a Claymore hits harder and swings slower. Its stats also tend toward that side of each range")]
        public MeleeScales Scales = MeleeScales.Identity;
        public MeleeGrip Grip;
    }
}
