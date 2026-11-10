using System;
using UnityEngine;

namespace CGD.Difficulty
{
    // What one difficulty level changes. 1 = the game as balanced; each multiplier only
    // reshapes that one thing, so a level can be tuned without touching the others.
    [Serializable]
    public class DifficultyTuning
    {
        [Tooltip("Scales every enemy's health and shield (the effective health the balance docs assume at 1)")]
        [SerializeField, Min(0.1f)] private float _enemyHealth = 1f;
        [Tooltip("Scales all damage the player takes from anything but the player's own team (enemies, hazards, their grenades)")]
        [SerializeField, Min(0.1f)] private float _damageTaken = 1f;

        public float EnemyHealth => _enemyHealth;
        public float DamageTaken => _damageTaken;

        public static DifficultyTuning None { get; } = new();

        public DifficultyTuning() { }

        public DifficultyTuning(float enemyHealth, float damageTaken)
        {
            _enemyHealth = Mathf.Max(0.1f, enemyHealth);
            _damageTaken = Mathf.Max(0.1f, damageTaken);
        }
    }
}
