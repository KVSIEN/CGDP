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
        // The rest of the shot's resolved stats (weapon, its perks and attachments, the wielder's
        // buffs), which fire behaviors use instead of the matching WeaponData fields.
        public float      ArmorPenetration;
        public float      CriticalMultiplier;
        public HitBonuses Bonuses;
        // Multipliers on the weapon's range (optimal, falloff end and max) and projectile speed.
        public float      RangeScale;
        public float      ProjectileSpeedScale;
        // 1 for standard fire modes; 0–1 for Charge mode, reflecting how long the trigger was held.
        // Fire behaviors that ignore charge should treat it as 1.
        public float      Charge;
        // Magnetized: above 0, every shot resolves instantly along a straight line through each
        // target it can damage, keeping this share of its damage per target passed.
        public float      PierceDamageKept;
        // Unit direction a volley's arrows fan out along (sideways or up), perpendicular to the aim.
        public Vector3    VolleyAxis;
        public bool       DebugDraw;
        public Color      DebugHitColor;
        public Color      DebugMissColor;
        public float      DebugLineDuration;
    }
}
