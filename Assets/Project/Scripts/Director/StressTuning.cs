using System;
using UnityEngine;

namespace CGD.Director
{
    // What stresses the player, and how much (intensity runs from 0 to 1).
    [Serializable]
    public class StressTuning
    {
        [Tooltip("Intensity added by losing the player's whole health (shield included); smaller hits add their share")]
        [SerializeField, Min(0f)] private float _damageTaken = 1.2f;
        [Tooltip("Intensity added by an enemy dying right next to the player, less the further away it dies")]
        [SerializeField, Min(0f)] private float _nearbyKill = 0.08f;
        [Tooltip("Enemies dying further away than this add nothing")]
        [SerializeField, Min(0.1f)] private float _nearbyKillRadius = 10f;
        [Tooltip("Seconds after the player last dealt or took damage during which they still count as fighting")]
        [SerializeField, Min(0f)] private float _combatMemory = 5f;

        public float DamageTaken      => _damageTaken;
        public float NearbyKill       => _nearbyKill;
        public float NearbyKillRadius => _nearbyKillRadius;
        public float CombatMemory     => _combatMemory;
    }
}
