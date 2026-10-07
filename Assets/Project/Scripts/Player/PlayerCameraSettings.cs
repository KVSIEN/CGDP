using UnityEngine;

namespace CGD.Player
{
    [CreateAssetMenu(fileName = "PlayerCameraSettings", menuName = "CGD/Player/Player Camera Settings")]
    public class PlayerCameraSettings : ScriptableObject
    {
        [Header("Pitch Limits")]
        public float MinPitch = -80f;
        public float MaxPitch = 80f;

        [Header("Rotation Smoothing")]
        public float RotationSmoothing = 0.05f;

        [Header("Third Person")]
        public float TpDistance = 4f;
        public float TpMinDistance = 0.5f;
        public float ShoulderOffset = 0.5f;
        public float CollisionRadius = 0.2f;
        public LayerMask CollisionMask = ~0;

        [Header("Transition")]
        public float TransitionSmoothTime = 0.12f;

        [Header("Body Rotation")]
        public float FpBodySmoothTime = 0.02f;
        public float TpBodySmoothTime = 0.12f;

        [Header("Crouch")]
        public float CrouchHeadLower = 0.65f;
        public float CrouchHeadSmoothTime = 0.08f;

        [Header("ADS")]
        [Tooltip("Fallback ADS FOV used before a weapon is equipped. WeaponData.AdsFovDeg overrides at runtime via SetAdsProfile().")]
        public float AdsFov = 45f;
        public float AdsTpDistance = 1.5f;
        [Tooltip("Shoulder offset while ADS in third-person. Keep non-zero so the camera stays beside the player, not behind their head.")]
        public float AdsTpShoulderOffset = 0.25f;
        public float AdsSensitivityMult = 0.5f;
        [Tooltip("Fallback ADS transition speed used before a weapon is equipped. WeaponData.AdsSpeed overrides at runtime via SetAdsProfile().")]
        public float AdsSpeed = 10f;

        [Header("FOV")]
        public float BaseFov = 70f;
        public float SprintFov = 80f;
        public float FovSpeed = 8f;

        [Header("Lock-On")]
        [Tooltip("Degrees per second the view turns toward a locked target")]
        public float LockOnTurnSpeed = 360f;

        [Header("Recoil")]
        [Tooltip("Degrees of deliberate downward pull that move the recoil recovery origin")]
        public float CounterplayThreshold = 2f;
    }
}
