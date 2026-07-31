using UnityEngine;

public enum EnemyCombatType { Melee, Ranged }

[CreateAssetMenu(fileName = "EnemyData", menuName = "CGD/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Health")]
    public float MaxHealth = 100f;
    public float Armor     = 0f;

    [Header("Shield")]
    public float MaxShield = 0f;
    [Tooltip("Seconds without taking damage before shield starts regenerating")]
    public float ShieldRegenDelay = 5f;
    [Tooltip("Shield points restored per second once regen starts")]
    public float ShieldRegenRate = 10f;

    [Header("Movement")]
    public float PatrolSpeed = 2f;
    public float ChaseSpeed  = 5f;

    [Header("Detection")]
    [Tooltip("Maximum sight distance in metres.")]
    public float SightRange = 15f;
    [Tooltip("Full cone angle in degrees — e.g. 90 means 45° either side of forward.")]
    public float SightAngle = 90f;
    [Tooltip("Radius at which the enemy hears the player regardless of line-of-sight.")]
    public float HearingRadius = 8f;

    [Header("Combat")]
    public EnemyCombatType CombatType = EnemyCombatType.Melee;
    public float AttackRange    = 1.5f;
    public float AttackDamage   = 15f;
    public float AttackCooldown = 1f;

    [Header("Ranged Combat")]
    [Tooltip("Distance the enemy tries to maintain from the player when using ranged attacks.")]
    public float PreferredRange = 12f;
    [Tooltip("Spread angle in degrees — 0 is perfect accuracy, higher values miss more.")]
    public float SpreadAngle = 3f;
    [Tooltip("Shots fired per burst before the fire cooldown starts.")]
    public int BurstCount = 1;
    [Tooltip("Delay in seconds between shots within a burst.")]
    public float BurstInterval = 0.1f;
    [Tooltip("How often the enemy picks a new strafe direction (seconds).")]
    public float StrafeInterval = 2f;
    [Tooltip("How far sideways the enemy strafes at preferred range.")]
    public float StrafeDistance = 4f;

    [Header("Alert")]
    [Tooltip("How long the enemy investigates the last known position before returning to patrol.")]
    public float AlertDuration = 5f;

    [Header("Audio")]
    public SoundBank AttackSound;
    public SoundBank RangedAttackSound;
    public SoundBank DeathSound;
}
