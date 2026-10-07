using System;
using UnityEngine;

namespace CGD.Weapons
{
    // Where a weapon of a styled category sits in its stat band — e.g. a marksman build that
    // hits hard and kicks, or a rapid one that fires fast for less. Rolled independently of
    // the FiringStyle, by Weight (see WeaponCategoryData).
    [Serializable]
    public class StatStyle
    {
        [Tooltip("Put in front of the weapon's name, e.g. \"Marksman\". Empty = nothing")]
        public string Name;
        [Min(0f)] public float Weight = 1f;
        public StyleScales Scales = StyleScales.Identity;
    }
}
