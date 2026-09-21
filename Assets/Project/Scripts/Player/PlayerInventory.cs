using System;
using UnityEngine;
using CGD.Items;

namespace CGD.Player
{
    // Thin MonoBehaviour wrapper around the plain-C# Inventory so other player
    // components (WeaponController, pickups, HUDs) can find it via GetComponent.
    // The Inventory itself has no Unity dependencies and is shared with other
    // storage contexts (docked ship, loot containers) later.
    //
    // That independence is why the Inventory itself is not visible in the
    // Inspector: it is a plain class behind a get-only property, so Unity's
    // serializer never sees it. StartingStacks below is the authoring surface
    // instead — it seeds the runtime Inventory once, the same way
    // PlayerWeaponLoadout seeds its starting weapons.
    //
    // MaxSlots and MaxWeight are advisory: the HUD reads them for the capacity
    // and weight readouts, but the Inventory does not currently reject adds that
    // would exceed either — the GDD's extraction loop puts its tension on what
    // the player *risks bringing in*, not on pack space. Enforcement is a later
    // knob if a run-based cap is wanted.
    public class PlayerInventory : MonoBehaviour
    {
        // Stackables only. Gear needs an ItemRoll to instantiate, and starting
        // weapons already belong to PlayerWeaponLoadout.
        [Serializable]
        public struct StartingStack
        {
            public ItemDefinition Definition;
            [Min(1)] public int   Count;
        }

        [Header("Starting contents")]
        [Tooltip("Stackable items granted on Awake. Munitions here are what the player spawns with.")]
        [SerializeField] private StartingStack[] _startingStacks = Array.Empty<StartingStack>();

        [Header("Advisory caps (HUD readout only, not enforced)")]
        [SerializeField, Min(1)]   private int   _maxSlots  = 24;
        [SerializeField, Min(0.1f)] private float _maxWeight = 50f;

        public Inventory Inventory { get; } = new();

        public int   MaxSlots  => _maxSlots;
        public float MaxWeight => _maxWeight;

        private void Awake() => GrantStartingStacks();

        private void GrantStartingStacks()
        {
            foreach (StartingStack stack in _startingStacks)
            {
                if (stack.Definition == null || stack.Count <= 0) continue;
                Inventory.Add(stack.Definition, stack.Count);
            }
        }
    }
}
