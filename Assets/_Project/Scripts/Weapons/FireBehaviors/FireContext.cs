using UnityEngine;
using CGD.Combat;

namespace CGD.Weapons
{
    public struct FireContext
    {
        public Vector3    CameraPosition;
        public Vector3    CameraForward;     // un-spread forward; multi-pellet behaviors use this as their cone axis
        public float      SpreadDeg;         // cone half-angle computed by WeaponController
        public Vector3    Direction;         // spread applied once; single-pellet behaviors use this directly
        public Transform  Muzzle;
        public WeaponData Data;
        public DamageSource Source;
        public bool       DebugDraw;
        public Color      DebugHitColor;
        public Color      DebugMissColor;
        public float      DebugLineDuration;
    }
}
