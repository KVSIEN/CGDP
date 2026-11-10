using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Expedition
{
    // A permanent improvement to the ship, bought between runs with what extractions brought
    // home (credits, materials). It survives death, so every run, even a failed one, can
    // work toward the next level.
    [CreateAssetMenu(fileName = "ShipUpgrade", menuName = "CGD/Expedition/Ship Upgrade")]
    public class ShipUpgrade : ScriptableObject
    {
        [Tooltip("Names the upgrade in the save; never change it once players own levels. Empty = the asset's name")]
        [SerializeField] private string _id;
        [SerializeField] private string _displayName = "Upgrade";
        [SerializeField] private ShipUpgradeLevel[] _levels = Array.Empty<ShipUpgradeLevel>();

        public string Id          => string.IsNullOrEmpty(_id) ? name : _id;
        public string DisplayName => string.IsNullOrEmpty(_displayName) ? name : _displayName;
        public int    MaxLevel    => _levels.Length;
        public IReadOnlyList<ShipUpgradeLevel> Levels => _levels;

        // `level` is 1-based, as owned levels are counted.
        public ShipUpgradeLevel Level(int level) => level >= 1 && level <= _levels.Length ? _levels[level - 1] : null;

        // For tests: an upgrade built in code rather than authored.
        internal void SetUp(string id, params ShipUpgradeLevel[] levels)
        {
            _id     = id;
            _levels = levels;
        }
    }
}
