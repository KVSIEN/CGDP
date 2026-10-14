using UnityEngine;
using CGD.Combat;
using CGD.Meters;

namespace CGD.Weapons
{
    // A melee weapon's stats: its combo, heavy attack, bash, speed, stamina cost and guard.
    // Hand-authored for quick melee (fists, a gun's bash, an offhand knife — only the Bash and
    // the guard's parry settings matter there), or generated from a MeleeCategoryData.
    [CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "CGD/Weapons/Melee Weapon Data")]
    public class MeleeWeaponData : ScriptableObject
    {
        public string WeaponName = "Fists";

        [Tooltip("Chained on repeated light-attack taps; wraps back to the first step after the last.")]
        public MeleeAttackStep[] LightCombo = { new() };

        [Tooltip("Triggered by holding the melee action at least HeavyHoldThreshold seconds before releasing.")]
        public MeleeAttackStep HeavyAttack = new();

        [Tooltip("The Melee key, or Attack while the guard is up. A hit from in front landing within the guard's Parry Window of the bash starting is parried")]
        public MeleeAttackStep Bash = new() { Damage = 15f, WindupTime = 0.04f, ActiveTime = 0.1f, RecoveryTime = 0.35f, CancelFrom = 1f,
                                              HitShape = MeleeHitShape.Thrust, Range = 1.6f, Radius = 0.5f };

        [Tooltip("Hold duration before release counts as a heavy attack instead of a light tap.")]
        public float HeavyHoldThreshold = 0.35f;

        [Tooltip("Seconds of no input after a combo ends before the combo index resets to the first step.")]
        public float ComboResetTime = 1.2f;

        [Tooltip("Seconds the next combo step stays open after a dodge, parry, ability, weapon switch or a guard/dodge cancel — longer than ComboResetTime, so weaving keeps a combo alive and idling doesn't")]
        [Min(0f)] public float WeaveWindow = 2.5f;

        public LayerMask HitMask = ~0;
        [Tooltip("How far away enemies hear it (0 = silent)")]
        public float NoiseRadius = 8f;

        [Header("Handling")]
        [Tooltip("Speeds up (above 1) or slows down every swing's wind-up, strike and recovery. Timeline-driven steps keep their authored speed")]
        [Min(0.1f)] public float AttackSpeed = 1f;
        [Tooltip("Paid per light attack; heavy attacks pay HeavyStaminaMultiplier times this. Characters without the meter swing for free")]
        public MeterCost StaminaCost;
        [Min(0f)] public float HeavyStaminaMultiplier = 2f;

        [Header("Guard (alternate action) and parry")]
        [Tooltip("Off for quick-melee data (fists, gun bashes): they parry with their bash but can't block")]
        public bool CanGuard;
        public GuardSettings Guard = GuardSettings.Default;
        [Tooltip("Optional: what a parry does with the parried hit — send it back, fire it where you aim, a riposte slash, heal… Needs a Reflector on the player")]
        public ReflectProfile ParryReflect;
    }
}
