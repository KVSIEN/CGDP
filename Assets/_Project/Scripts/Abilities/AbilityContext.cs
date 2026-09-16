using UnityEngine;
using CGD.Combat;
using CGD.Player;

namespace CGD.Abilities
{
    // Data bundle passed to an ability when the player activates it.
    // PlayerAbilities fills this once in Awake and updates MoveInput each frame.
    public class AbilityContext
    {
        public Transform PlayerTransform;
        public Rigidbody PlayerRigidbody;
        public Collider  PlayerCollider;
        public Transform CameraTransform;
        public PlayerHealth Health;
        public DamageSource Source;
        public Vector2 MoveInput;
    }
}
