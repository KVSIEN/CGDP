using UnityEngine;

[CreateAssetMenu(fileName = "EnemyData", menuName = "CGD/Enemy Data")]
public class EnemyData : ScriptableObject
{
    [Header("Health")]
    public float MaxHealth = 100f;

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
    public float AttackRange    = 1.5f;
    public float AttackDamage   = 15f;
    public float AttackCooldown = 1f;

    [Header("Alert")]
    [Tooltip("How long the enemy investigates the last known position before returning to patrol.")]
    public float AlertDuration = 5f;
}
