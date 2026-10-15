using System;
using UnityEngine;
using CGD.Core;
using CGD.Interaction;
using CGD.Items;

namespace CGD.Director
{
    // Tuning for the SupplySpawner: when the player counts as low, what they get and where
    // it is put down.
    [CreateAssetMenu(fileName = "SupplySettings", menuName = "CGD/Director/Supply Settings")]
    public class SupplySettings : ScriptableObject
    {
        [SerializeField] private SupplyTuning _needs = new();

        [Header("Supplies")]
        [Tooltip("Spawned and filled with the supply")]
        [SerializeField] private ItemPickup _pickupPrefab;
        [Tooltip("Handed out when healing is needed; also what counts as healing the player carries")]
        [SerializeField] private ItemDefinition _healingItem;
        [SerializeField, Min(1)] private int _healingCount = 2;
        [Tooltip("One per ammo type; the one matching the weapon in hand is handed out")]
        [SerializeField] private MunitionDefinition[] _munitions = Array.Empty<MunitionDefinition>();
        [Tooltip("Magazines' worth of the weapon in hand's ammo handed out")]
        [SerializeField, Min(0.5f)] private float _ammoMagazines = 3f;

        [Header("Timing & placement")]
        [Tooltip("Seconds into a calm before the player's needs are checked (once per calm)")]
        [SerializeField, Min(0f)] private float _delayIntoCalm = 4f;
        [Tooltip("How far from the player (straight line) supplies are put down, in sight if possible")]
        [SerializeField] private FloatRange _distance = new(4f, 9f);
        [SerializeField, Min(1)] private int _spotAttempts = 12;
        [Tooltip("Shown in the HUD feed when supplies are put down. Empty = no message")]
        [SerializeField] private string _message = "Supplies spotted nearby";

        public SupplyTuning   Needs         => _needs;
        public ItemPickup     PickupPrefab  => _pickupPrefab;
        public ItemDefinition HealingItem   => _healingItem;
        public int            HealingCount  => _healingCount;
        public float          AmmoMagazines => _ammoMagazines;
        public float          DelayIntoCalm => _delayIntoCalm;
        public FloatRange     Distance      => _distance;
        public int            SpotAttempts  => _spotAttempts;
        public string         Message       => _message;

        public MunitionDefinition MunitionFor(AmmoType ammoType)
        {
            foreach (MunitionDefinition munition in _munitions)
                if (munition != null && munition.AmmoType == ammoType) return munition;
            return null;
        }
    }
}
