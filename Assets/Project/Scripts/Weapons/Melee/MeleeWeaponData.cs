using UnityEngine;
using CGD.Meters;

namespace CGD.Weapons
{
    // A melee weapon's stats: its combo, heavy attack, speed, stamina cost and guard.
    // Hand-authored for quick melee (fists), or generated from a MeleeCategoryData.
    [CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "CGD/Weapons/Melee Weapon Data")]
    public class MeleeWeaponData : ScriptableObject
    {
        public string WeaponName = "Fists";

        [Tooltip("Chained on repeated light-attack taps; wraps back to the first step after the last.")]
        public MeleeAttackStep[] LightCombo = { new() };

        [Tooltip("Triggered by holding the melee action at least HeavyHoldThreshold seconds before releasing.")]
        public MeleeAttackStep HeavyAttack = new();

        [Tooltip("Hold duration before release counts as a heavy attack instead of a light tap.")]
        public float HeavyHoldThreshold = 0.35f;

        [Tooltip("Seconds of no input after a combo ends before the combo index resets to the first step.")]
        public float ComboResetTime = 1.2f;

        public LayerMask HitMask = ~0;
        [Tooltip("How far away enemies hear it (0 = silent)")]
        public float NoiseRadius = 8f;

        [Header("Handling")]
        [Tooltip("Speeds up (above 1) or slows down every swing's wind-up, strike and recovery. Timeline-driven steps keep their authored speed")]
        [Min(0.1f)] public float AttackSpeed = 1f;
        [Tooltip("Paid per light attack; heavy attacks pay HeavyStaminaMultiplier times this. Characters without the meter swing for free")]
        public MeterCost StaminaCost;
        [Min(0f)] public float HeavyStaminaMultiplier = 2f;

        [Header("Guard (alternate action)")]
        [Tooltip("Off for quick-melee data such as fists, which can't block")]
        public bool CanGuard;
        public GuardSettings Guard = GuardSettings.Default;
    }
}
