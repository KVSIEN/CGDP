using UnityEngine;
using CGD.Items;

namespace CGD.Player
{
    // Thin MonoBehaviour wrapper around the plain-C# Inventory so other player
    // components (WeaponController, pickups, HUDs) can find it via GetComponent.
    // The Inventory itself has no Unity dependencies and is shared with other
    // storage contexts (docked ship, loot containers) later.
    //
    // MaxSlots and MaxWeight are advisory: the HUD reads them for the capacity
    // and weight readouts, but the Inventory does not currently reject adds that
    // would exceed either — the GDD's extraction loop puts its tension on what
    // the player *risks bringing in*, not on pack space. Enforcement is a later
    // knob if a run-based cap is wanted.
    public class PlayerInventory : MonoBehaviour
    {
        [Header("Advisory caps (HUD readout only, not enforced)")]
        [SerializeField, Min(1)]   private int   _maxSlots  = 24;
        [SerializeField, Min(0.1f)] private float _maxWeight = 50f;

        public Inventory Inventory { get; } = new();

        public int   MaxSlots  => _maxSlots;
        public float MaxWeight => _maxWeight;
    }
}
