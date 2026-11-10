using System;
using System.Collections.Generic;
using UnityEngine;

namespace CGD.Expedition
{
    // Every upgrade the ship can be given, in the order the ship screen lists them.
    [CreateAssetMenu(fileName = "ShipUpgradeCatalog", menuName = "CGD/Expedition/Ship Upgrade Catalog")]
    public class ShipUpgradeCatalog : ScriptableObject
    {
        [SerializeField] private ShipUpgrade[] _upgrades = Array.Empty<ShipUpgrade>();

        public IReadOnlyList<ShipUpgrade> Upgrades => _upgrades;

        // For tests.
        internal void SetUpgrades(params ShipUpgrade[] upgrades) => _upgrades = upgrades;
    }
}
