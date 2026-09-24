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
        // Per-shot base damage after attachments and the wielder's stat modifiers
        // (WeaponController resolves it); fire behaviors use this instead of Data.Damage.
        public float      Damage;
        public DamageSource Source;
        // 1 for standard fire modes; 0–1 for Charge mode, reflecting how long the trigger was held.
        // Fire behaviors that ignore charge should treat it as 1.
        public float      Charge;
        public bool       DebugDraw;
        public Color      DebugHitColor;
        public Color      DebugMissColor;
        public float      DebugLineDuration;
    }
}
