using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "CGD/Player Movement Settings")]
public class PlayerMovementSettings : ScriptableObject
{
    [Header("Speed")]
    public float WalkSpeed = 5f;
    public float SprintSpeed = 9f;
    public float CrouchSpeed = 2.5f;
    public float Acceleration = 25f;
    public float Deceleration = 40f;
    public float AirControl = 0.35f;

    [Header("Jump")]
    public float JumpHeight = 1.2f;
    public float GravityScale = 2.5f;
    public float FallGravityScale = 4f;
    public float LowJumpGravityScale = 3f;
    public float MaxFallSpeed = 25f;
    public float CoyoteTime = 0.12f;
    public float JumpBufferTime = 0.15f;

    [Header("Crouch")]
    public float StandHeight = 2f;
    public float CrouchHeight = 1f;
    public float CrouchTransitionSpeed = 12f;

    [Header("Ground")]
    public float GroundCheckDistance = 0.06f;
    public LayerMask GroundMask = ~0;
    public float MaxSlopeAngle = 46f;

    [Header("Step Climbing")]
    public float MaxStepHeight = 0.3f;
    public float StepCheckDistance = 0.45f;
    public float StepClimbSpeed = 8f;

    [Header("Dodge")]
    [Tooltip("Impulse of the first phase — a short sidestep")]
    public float SidestepForce = 7f;
    [Tooltip("How long after the sidestep the player can press Dodge again to roll")]
    public float RollWindowDuration = 0.35f;
    [Tooltip("Cooldown applied when the sidestep is used but the roll window expires unused")]
    public float SidestepCooldown = 0.7f;
    [Tooltip("Impulse of the full roll (second phase)")]
    public float DodgeForce = 12f;
    [Tooltip("Brief phase duration after the roll before the cooldown starts")]
    public float RollDuration = 0.2f;
    public float DodgeCooldown = 1.5f;

    [Header("Slide")]
    public float SlideSpeed = 11f;
    [Tooltip("Extra speed added on top of sprint speed when a slide starts")]
    public float SlideBoost = 3f;
    public float SlideDeceleration = 6f;
    public float SlideMinSpeed = 3f;
    public float SlideDuration = 1.2f;

    [Header("Mantle / Vault")]
    [Tooltip("Horizontal distance in front of the player to check for a ledge")]
    public float MantleReach = 0.9f;
    [Tooltip("Height above feet to cast the wall-detection ray (approx chest)")]
    public float MantleDetectHeight = 1.3f;
    [Tooltip("Ledge tops at or below this height relative to feet trigger a vault")]
    public float VaultMaxHeight = 1.1f;
    [Tooltip("Maximum ledge top height relative to feet that the player can mantle")]
    public float MantleMaxHeight = 2.2f;
    [Tooltip("Speed at which the player is moved to the mantle target (m/s)")]
    public float MantleSpeed = 6f;
    [Tooltip("Maximum time allowed to complete a mantle before it is cancelled")]
    public float MantleTimeout = 0.8f;
    [Tooltip("How far past the ledge edge the player is placed after mantling")]
    public float MantleStepOver = 0.4f;
    [Tooltip("Upward velocity impulse applied on a vault")]
    public float VaultUpImpulse = 5f;
    [Tooltip("Forward velocity impulse applied on a vault")]
    public float VaultForwardImpulse = 5f;
}
