using UnityEngine;
using CGD.Combat;
using CGD.Player;
using CGD.Stats;
using CGD.Stealth;

namespace CGD.Abilities
{
    // Data bundle passed to an ability when its caster uses it — the player (PlayerAbilities
    // fills this once in Awake and updates MoveInput each frame) or an enemy (EnemyAbilities,
    // for the behavior tree's Use Ability node). The caster's components that abilities act
    // through are resolved here once, so abilities (shared ScriptableObjects, checked every
    // frame a press waits) never look them up. A component the caster doesn't have is null;
    // abilities needing it can't execute (an enemy has no dodge, reflector or stealth).
    public class AbilityContext
    {
        public Transform CasterTransform;
        public Rigidbody CasterRigidbody;
        public Collider  CasterCollider;
        // Where the ability comes from and points: the player's camera, an enemy's eyes.
        public Transform AimTransform;
        public HealthManager Health;
        public DamageSource Source;
        public Vector2 MoveInput;

        public PlayerMovement        Movement;
        public PlayerDodge           Dodge;
        public TimelineAbilityRunner Timeline;
        public CharacterStats        Stats;
        public Reflector             Reflector;
        public Stealthable           Stealth;
        // Enemies only: slips the caster somewhere its hostiles can't see (Phase Shift).
        public IUnseenRelocator      Relocator;

        // Set per cast by resource scaling: damage multiplier and extra on-hit effects.
        public float Power = 1f;
        public StatusEffectApplication[] BonusEffects;
    }
}
