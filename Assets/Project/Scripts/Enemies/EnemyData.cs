using UnityEngine;
using CGD.Audio;
using CGD.Combat;

namespace CGD.Enemies
{
    [CreateAssetMenu(fileName = "EnemyData", menuName = "CGD/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Tooltip("Shown in kill confirmations")]
        public string DisplayName = "Enemy";

        [Tooltip("Enemies never damage their own team")]
        public Team Team = Team.Enemy;

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
        [Tooltip("Radius within which the enemy notices a hostile even without line of sight. Gunfire and other noises are heard from their own radius.")]
        public float HearingRadius = 8f;

        [Header("Combat")]
        public EnemyCombatType CombatType = EnemyCombatType.Melee;
        public float AttackRange    = 1.5f;
        public float AttackDamage   = 15f;
        public float AttackCooldown = 1f;
        [Tooltip("Seconds between starting an attack and the hit landing (melee only)")]
        public float AttackWindup   = 0.35f;

        [Header("Ranged Combat")]
        [Tooltip("Distance the enemy tries to maintain from its target")]
        public float PreferredRange = 12f;
        [Tooltip("Spread angle in degrees — 0 is perfect accuracy")]
        public float SpreadAngle    = 3f;
        [Tooltip("Shots fired per burst before the fire cooldown starts")]
        public int   BurstCount     = 1;
        [Tooltip("Delay in seconds between shots within a burst")]
        public float BurstInterval  = 0.1f;
        [Tooltip("How often the enemy picks a new strafe direction (seconds)")]
        public float StrafeInterval = 2f;
        [Tooltip("How far sideways the enemy strafes at preferred range")]
        public float StrafeDistance  = 4f;

        [Header("Alert")]
        [Tooltip("How long the enemy investigates the last known position before returning to patrol.")]
        public float AlertDuration = 5f;

        [Header("Audio")]
        public SoundBank AttackSound;
        public SoundBank RangedAttackSound;
    }
}
