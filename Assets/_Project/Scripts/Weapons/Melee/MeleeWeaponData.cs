using System;
using UnityEngine;

// One step of a melee attack — either a rung of the light combo or the heavy finisher.
[Serializable]
public class MeleeAttackStep
{
    [Header("Damage")]
    public float Damage = 20f;
    [Range(0f, 1f)] public float ArmorPenetration = 0f;
    public DamageType DamageType = DamageType.Physical;

    [Header("Timing")]
    [Tooltip("Delay before the hit registers.")]
    public float WindupTime = 0.1f;
    [Tooltip("How long after the windup the hit registers is active for (single check, not continuous).")]
    public float ActiveTime = 0.1f;
    [Tooltip("Delay after the hit before another attack can start, unless a combo input was buffered.")]
    public float RecoveryTime = 0.25f;

    [Header("Hitbox")]
    [Tooltip("Distance in front of the camera the hit sphere is cast to.")]
    public float Range = 1.8f;
    public float Radius = 0.7f;
    public Color DebugColor = Color.white;

    [Header("Audio")]
    public SoundBank SwingSound;
    public SoundBank HitSound;
}

[CreateAssetMenu(fileName = "NewMeleeWeapon", menuName = "CGD/Melee Weapon Data")]
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
