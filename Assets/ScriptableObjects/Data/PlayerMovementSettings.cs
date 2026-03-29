using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMovementSettings", menuName = "CGD/Player Movement Settings")]
public class PlayerMovementSettings : ScriptableObject
{
    [Header("Speed")]
    public float WalkSpeed = 5f;
    public float SprintSpeed = 9f;
    public float CrouchSpeed = 2.5f;
    public float Acceleration = 20f;
    public float Deceleration = 25f;
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
}
