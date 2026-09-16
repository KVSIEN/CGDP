using UnityEngine;

namespace CGD.Weapons
{
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
    }
}
