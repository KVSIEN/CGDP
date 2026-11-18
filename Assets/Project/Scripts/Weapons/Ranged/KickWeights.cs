using System;
using UnityEngine;

namespace CGD.Weapons
{
    // How one weapon weights each direction of its shot kick, as multipliers on the baseline
    // profile tuned on WeaponVisuals (1 = the baseline). The kick's size comes from RecoilScale;
    // this only shapes where it goes, so a gun can shove hard into the shoulder without climbing,
    // or climb without any push back.
    [Serializable]
    public struct KickWeights
    {
        [Tooltip("Muzzle rise per degree of vertical kick")]
        [Min(0f)] public float Rise;
        [Tooltip("Sideways swing of the gun per degree of horizontal kick")]
        [Min(0f)] public float Yaw;
        [Tooltip("Roll of the gun per degree of horizontal kick")]
        [Min(0f)] public float Roll;
        [Tooltip("Upward hop of the whole gun per degree of vertical kick")]
        [Min(0f)] public float Hop;
        [Tooltip("Backward shove into the hand per degree of vertical kick, on top of Muzzle Flip's share. 0 = no pushback at all")]
        [Min(0f)] public float Pushback;
        [Tooltip("Degrees the view rolls per degree of horizontal kick (visual only, 0 = none)")]
        [Min(0f)] public float ViewRoll;

        public static KickWeights Identity => new() { Rise = 1f, Yaw = 1f, Roll = 1f, Hop = 1f, Pushback = 1f, ViewRoll = 0f };
    }
}
