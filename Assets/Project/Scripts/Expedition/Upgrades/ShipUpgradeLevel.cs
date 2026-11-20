using System;
using UnityEngine;
using CGD.Crafting;
using CGD.Items;

namespace CGD.Expedition
{
    // One step of a ship upgrade: what it costs from the hold and the bonus it adds to the
    // player on every run from then on. Owning a level keeps the bonuses of the ones before it.
    [Serializable]
    public class ShipUpgradeLevel
    {
        [Tooltip("What the level gives, as the ship screen shows it (\"+25 max health\")")]
        public string Summary;
        [Tooltip("Taken from the ship's hold when the level is bought")]
        public ItemAmount[] Cost = Array.Empty<ItemAmount>();
        [Tooltip("Added to the player's stats at the start of every run")]
        public StatModifier[] Bonus = Array.Empty<StatModifier>();
    }
}
