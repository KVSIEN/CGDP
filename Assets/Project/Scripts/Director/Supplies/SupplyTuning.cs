using System;
using UnityEngine;

namespace CGD.Director
{
    // When the player counts as needing supplies (see SupplyPlanner).
    [Serializable]
    public class SupplyTuning
    {
        [Tooltip("Healing is needed below this share of health…")]
        [SerializeField, Range(0f, 1f)] private float _healthBelow = 0.5f;
        [Tooltip("…unless the player already carries at least this many healing items")]
        [SerializeField, Min(1)] private int _healingCarriedEnough = 2;
        [Tooltip("Ammo is needed below this many spare magazines for the weapon in hand")]
        [SerializeField, Min(0f)] private float _magazinesBelow = 2f;

        public SupplyTuning() { }

        public SupplyTuning(float healthBelow, int healingCarriedEnough, float magazinesBelow)
        {
            _healthBelow          = healthBelow;
            _healingCarriedEnough = healingCarriedEnough;
            _magazinesBelow       = magazinesBelow;
        }

        public float HealthBelow          => _healthBelow;
        public int   HealingCarriedEnough => _healingCarriedEnough;
        public float MagazinesBelow       => _magazinesBelow;
    }
}
