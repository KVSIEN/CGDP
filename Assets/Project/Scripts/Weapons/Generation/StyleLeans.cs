using System;
using UnityEngine;

namespace CGD.Weapons
{
    // Which way a model's stats tend inside their windows: + toward the top of a stat's
    // range (more damage, more recoil), − toward the bottom, 0 = anywhere alike. Added to the
    // category range's own bias, so the window stays the same and only the odds shift, making
    // a model more likely to play like itself (an M14 usually hits hard and fires slowly).
    [Serializable]
    public struct StyleLeans
    {
        [Range(-1f, 1f)] public float Damage;
        [Range(-1f, 1f)] public float FireRate;
        [Tooltip("Kick and its accumulated caps")]
        [Range(-1f, 1f)] public float Recoil;
        [Tooltip("Hip-fire cone and bloom")]
        [Range(-1f, 1f)] public float Spread;
        [Tooltip("Optimal range and falloff end")]
        [Range(-1f, 1f)] public float Range;
        [Range(-1f, 1f)] public float MagazineSize;
    }
}
