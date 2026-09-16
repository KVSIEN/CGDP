using UnityEngine;

// Data bundle passed to an ability when the player activates it.
// PlayerAbilities fills this once in Awake and updates MoveInput each frame.
public class AbilityContext
{
    public Transform PlayerTransform;
    public Rigidbody PlayerRigidbody;
    public Collider  PlayerCollider;
    public Transform CameraTransform;
    public PlayerStats Stats;
    public Vector2 MoveInput;
}
