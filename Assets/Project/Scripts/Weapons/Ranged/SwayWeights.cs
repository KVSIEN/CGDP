using System;
using UnityEngine;

namespace CGD.Weapons
{
    // How one weapon weights each axis of its sway, as multipliers on the amounts in WeaponData
    // (Look Sway Amount, Idle Sway Amount): 1 = even, 0 = that axis stays still. A long barrel
    // can lag sideways but barely bob, a bullpup the other way round.
    [Serializable]
    public struct SwayWeights
    {
        [Tooltip("Lag up and down when the view turns up or down")]
        [Min(0f)] public float LookPitch;
        [Tooltip("Lag left and right when the view turns sideways")]
        [Min(0f)] public float LookYaw;
        [Tooltip("Breathing drift up and down")]
        [Min(0f)] public float IdlePitch;
        [Tooltip("Breathing drift left and right")]
        [Min(0f)] public float IdleYaw;

        public static SwayWeights Identity => new() { LookPitch = 1f, LookYaw = 1f, IdlePitch = 1f, IdleYaw = 1f };
    }
}
