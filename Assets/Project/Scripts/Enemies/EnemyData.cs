using UnityEngine;
using CGD.Audio;
using CGD.BehaviorTrees;
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

        [Tooltip("1 = rank and file; higher tiers are tougher and roll more random affixes (see EnemyAffixPool)")]
        [Range(1, 3)] public int Tier = 1;

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
        [Tooltip("How far it hears a walking hostile's footsteps (crouching quieter, sprinting further, standing still silent; walls halve it). Gunfire and other noises carry their own radius.")]
        public float HearingRadius = 8f;

        [Tooltip("How its senses, memory and reactions are tuned. Empty = built-in defaults")]
        public PerceptionProfile Senses;

        [Header("Behavior")]
        [Tooltip("Optional. Decides what the enemy does instead of the built-in patrol / alert / chase states " +
                 "(edit it in CGD › AI › Behavior Tree Editor)")]
        public BehaviorTreeAsset BehaviorTree;

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

        [Header("Group & Reactions")]
        [Tooltip("It won't step in to strike while this many allies (itself included) are already in melee with the same target — it circles instead")]
        [Min(1)] public int MaxMeleeAttackers = 2;
        [Tooltip("It holds fire while this many allies are already firing bursts at the same target")]
        [Min(1)] public int MaxShooters = 3;
        [Tooltip("A single hit taking at least this share of max health staggers it (interrupting a wind-up)")]
        [Range(0.01f, 1f)] public float StaggerThreshold = 0.25f;
        [Tooltip("Seconds a stagger lasts")]
        [Min(0f)] public float StaggerDuration = 0.4f;
        [Tooltip("Seconds before it can be staggered again")]
        [Min(0f)] public float StaggerCooldown = 2.5f;
        [Tooltip("On first spotting a hostile, runs to an alarm panel near its post to raise the alarm, if it knows one")]
        public bool RaisesAlarm;

        [Header("Alert")]
        [Tooltip("Longest it spends investigating something before it gives up and goes back to patrol (2.5× when hunting a hostile it knows is about).")]
        public float AlertDuration = 5f;

        [Header("Audio")]
        public SoundBank AttackSound;
        public SoundBank RangedAttackSound;

        // Raw damage it takes to kill from full: shield and health together, both behind
        // armor (Effective Damage = Raw × 100 / (100 + Armor), GDD › Damage Calculation).
        // Low-tier enemies sit around 100; each reality spreads it differently.
        public float EffectiveHealth => (MaxHealth + MaxShield) * (1f + Mathf.Max(0f, Armor) / 100f);
    }
}
