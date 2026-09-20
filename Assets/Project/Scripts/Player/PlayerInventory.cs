using UnityEngine;
using CGD.Items;

namespace CGD.Player
{
    // Thin MonoBehaviour wrapper around the plain-C# Inventory so other player
    // components (WeaponController, pickups, HUDs) can find it via GetComponent.
    // The Inventory itself has no Unity dependencies and is shared with other
    // storage contexts (docked ship, loot containers) later.
    public class PlayerInventory : MonoBehaviour
    {
        public Inventory Inventory { get; } = new();
    }
}
