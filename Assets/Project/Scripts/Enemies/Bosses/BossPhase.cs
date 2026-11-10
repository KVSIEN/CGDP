using System;
using UnityEngine;

namespace CGD.Enemies
{
    // One escalation of a boss fight: once the boss's health falls below HealthBelow it hits
    // harder, moves and attacks faster, and calls in help from the room's roster.
    [Serializable]
    public class BossPhase
    {
        [Tooltip("The phase starts once the boss's health falls below this share of its maximum")]
        [Range(0.05f, 0.95f)] public float HealthBelow = 0.5f;
        [Tooltip("Shown to the player as the phase begins")]
        public string Announcement = "It grows furious";
        [Min(0f)] public float DamageMultiplier = 1.25f;
        [Tooltip("× patrol and chase speed")]
        [Min(0f)] public float SpeedMultiplier = 1.1f;
        [Tooltip("× attack rate: 1.5 attacks half again as often")]
        [Min(0.1f)] public float AttackRateMultiplier = 1.2f;
        [Tooltip("Enemies called in from the boss room's roster as the phase begins")]
        [Min(0)] public int Reinforcements = 2;

        // Phases stack: each one scales what the previous ones left.
        public void ApplyTo(EnemyData data)
        {
            data.AttackDamage       *= DamageMultiplier;
            data.RangedAttackDamage *= DamageMultiplier;
            data.PatrolSpeed        *= SpeedMultiplier;
            data.ChaseSpeed         *= SpeedMultiplier;

            float rate = Mathf.Max(0.1f, AttackRateMultiplier);
            data.AttackCooldown       /= rate;
            data.RangedAttackCooldown /= rate;
            data.AttackWindup         /= rate;
        }
    }
}
