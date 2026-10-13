using UnityEngine;
using CGD.Combat;
using CGD.Player;
using CGD.Stats;
using CGD.Stealth;

namespace CGD.Abilities
{
    // Data bundle passed to an ability when the player activates it.
    // PlayerAbilities fills this once in Awake and updates MoveInput each frame. The
    // player's components that abilities act through are resolved here once, so abilities
    // (shared ScriptableObjects, checked every frame a press waits) never look them up.
    // A component the player doesn't have is null; abilities needing it can't execute.
    public class AbilityContext
    {
        public Transform PlayerTransform;
        public Rigidbody PlayerRigidbody;
        public Collider  PlayerCollider;
        public Transform CameraTransform;
        public PlayerHealth Health;
        public DamageSource Source;
        public Vector2 MoveInput;

        public PlayerMovement        Movement;
        public PlayerDodge           Dodge;
        public TimelineAbilityRunner Timeline;
        public CharacterStats        Stats;
        public Reflector             Reflector;
        public Stealthable           Stealth;

        // Set per cast by resource scaling: damage multiplier and extra on-hit effects.
        public float Power = 1f;
        public StatusEffectApplication[] BonusEffects;
    }
}
